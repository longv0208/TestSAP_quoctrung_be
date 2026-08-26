using System.Text.Json;
using FURPMS.Application.Common;
using Xunit.Abstractions;

namespace FURPMS.Tests.Ai;

/// <summary>
/// Đo chất lượng tầng 1 của rà trùng lặp trên bộ gán nhãn — <b>metric nằm trong bộ test, không
/// phải slide suông</b>.
///
/// <para>Cẩm nang chống trượt phạt nặng tính năng AI *"không có metric, không giải thích được
/// ngưỡng ở đâu ra"*. Con số <c>AI_DUPLICATE_THRESHOLD = 0.78</c> phải là kết quả đo được, và
/// phải đo lại được bất cứ lúc nào bằng <c>dotnet test</c>.</para>
///
/// <para><b>Chạy hoàn toàn offline.</b> Vector nằm sẵn trong <c>duplicate-eval-set.json</c>, sinh
/// một lần bằng model thật qua <c>POST /api/admin/embed-eval-set</c>. Test không gọi mạng nên luôn
/// xanh trong CI và cho cùng một con số giữa hai lần chạy.</para>
/// </summary>
public class DuplicateThresholdEvaluationTests
{
    private readonly ITestOutputHelper _out;
    public DuplicateThresholdEvaluationTests(ITestOutputHelper output) => _out = output;

    /// <summary>Mục tiêu F2 tối thiểu — dưới mức này thì ngưỡng không dùng được, phải xem lại.</summary>
    private const double MinF2 = 0.80;

    private sealed record EvalText(string Id, string Title, string Abstract, string Objectives);
    private sealed record EvalPair(string A, string B, bool IsDuplicate, string Kind, string Note);

    private sealed class EvalSet
    {
        public string? EmbeddingModel { get; init; }
        public List<EvalText> Texts { get; init; } = new();
        public List<EvalPair> Pairs { get; init; } = new();
        public Dictionary<string, float[]> Vectors { get; init; } = new();
    }

    private static string EvalSetPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Ai", "duplicate-eval-set.json");
            if (File.Exists(candidate)) return candidate;
            candidate = Path.Combine(dir.FullName, "FURPMS.Tests", "Ai", "duplicate-eval-set.json");
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException("Không tìm thấy duplicate-eval-set.json.");
    }

    private static EvalSet Load() =>
        JsonSerializer.Deserialize<EvalSet>(
            File.ReadAllText(EvalSetPath()),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    // ══════════════════════════════════════════════════════════════════════
    // Bộ gán nhãn phải lành lặn — chạy luôn, không cần vector
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void BoGanNhan_DuThanhPhan_VaKhongTuMauThuan()
    {
        var set = Load();

        Assert.True(set.Pairs.Count >= 30, "Bộ gán nhãn quá nhỏ để nói được điều gì về metric.");

        var dup = set.Pairs.Count(p => p.IsDuplicate);
        var hard = set.Pairs.Count(p => p.Kind == "HARD_NEGATIVE");

        Assert.True(dup >= 8, "Quá ít cặp trùng thật — Recall đo ra sẽ không có ý nghĩa thống kê.");

        // Cặp KHÓ là phần đắt nhất của bộ gán nhãn. Thiếu nó thì mọi ngưỡng đều trông đẹp: chỉ cần
        // phân biệt "cùng ngành" với "khác ngành" là đủ ăn điểm cao, mà đó không phải bài toán thật.
        Assert.True(hard >= 8, "Thiếu cặp cùng lĩnh vực khác đề tài — bộ đo sẽ dễ quá mức.");

        var ids = set.Texts.Select(t => t.Id).ToHashSet();
        foreach (var p in set.Pairs)
        {
            Assert.True(ids.Contains(p.A), $"Cặp trỏ tới văn bản không tồn tại: {p.A}");
            Assert.True(ids.Contains(p.B), $"Cặp trỏ tới văn bản không tồn tại: {p.B}");
            Assert.NotEqual(p.A, p.B);
        }

        // Một cặp gán hai nhãn khác nhau ở hai dòng là lỗi gán nhãn, và sẽ âm thầm kéo metric xuống.
        var seen = new HashSet<string>();
        foreach (var p in set.Pairs)
        {
            var key = string.CompareOrdinal(p.A, p.B) < 0 ? $"{p.A}|{p.B}" : $"{p.B}|{p.A}";
            Assert.True(seen.Add(key), $"Cặp bị lặp trong bộ gán nhãn: {key}");
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Bộ quét ngưỡng phải tính đúng — chạy luôn, dùng dữ liệu dựng sẵn
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void QuetNguong_TinhDungPrecisionRecall()
    {
        // Dựng tay để biết trước đáp án: tại τ=0.80 có 2 cặp trùng vượt ngưỡng (TP), 1 cặp không
        // trùng vượt ngưỡng (FP), 1 cặp trùng bị lọt (FN), 1 cặp không trùng nằm dưới (TN).
        var scored = new List<(double, bool)>
        {
            (0.95, true), (0.85, true), (0.70, true),
            (0.82, false), (0.40, false)
        };

        var point = ThresholdSweep.Run(scored, new[] { 0.80 }).Single();

        Assert.Equal(2, point.Tp);
        Assert.Equal(1, point.Fp);
        Assert.Equal(1, point.Fn);
        Assert.Equal(1, point.Tn);
        Assert.Equal(2d / 3d, point.Precision, 6);
        Assert.Equal(2d / 3d, point.Recall, 6);
        Assert.Equal(2d / 3d, point.F1, 6);
    }

    [Fact]
    public void ChonNguong_ThienVeRecall()
    {
        // Hai ngưỡng cùng F1 nhưng khác cán cân: F2 phải chọn cái bắt được nhiều cặp trùng hơn,
        // vì bỏ sót một đề tài trùng tốn kém hơn nhiều so với một lần báo động giả.
        var scored = new List<(double, bool)>
        {
            (0.90, true), (0.75, true), (0.74, true), (0.73, false), (0.30, false), (0.20, false)
        };

        var points = ThresholdSweep.Run(scored, new[] { 0.74, 0.90 });
        var best = ThresholdSweep.Best(points);

        Assert.Equal(0.74, best.Threshold);
        Assert.Equal(3, best.Tp);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Đo thật trên vector của model — chỉ chạy khi bộ gán nhãn đã có vector
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public void QuetNguongTrenModelThat_DatMucTieuF2()
    {
        var set = Load();

        if (set.Vectors.Count == 0)
        {
            // KHÔNG giả vờ xanh bằng dữ liệu bịa. Chưa vector hoá thì chưa đo được, và test nói
            // thẳng ra như vậy kèm cách chạy — số metric phải đến từ model thật hoặc không có.
            _out.WriteLine(
                "BỎ QUA: bộ gán nhãn chưa có vector.\n" +
                "Chạy BE (Development) rồi gọi POST /api/admin/embed-eval-set với tài khoản Quản trị,\n" +
                "sau đó chạy lại `dotnet test` để có bảng quét ngưỡng.");
            return;
        }

        var scored = new List<(double Similarity, bool IsDuplicate)>();
        foreach (var p in set.Pairs)
        {
            if (!set.Vectors.TryGetValue(p.A, out var va) || !set.Vectors.TryGetValue(p.B, out var vb))
                continue;
            scored.Add((VectorMath.CosineSimilarity(va, vb), p.IsDuplicate));
        }

        Assert.True(scored.Count >= 30, "Thiếu vector cho nhiều cặp — hãy vector hoá lại bộ gán nhãn.");

        var points = ThresholdSweep.Run(scored);
        var best = ThresholdSweep.Best(points);

        _out.WriteLine($"Model nhúng: {set.EmbeddingModel}");
        _out.WriteLine($"Số cặp đo: {scored.Count}");
        _out.WriteLine("");
        _out.WriteLine(ThresholdSweep.Format(points));
        _out.WriteLine($"→ Ngưỡng tốt nhất theo F2: {best.Threshold:0.00} " +
                       $"(P={best.Precision:0.000}, R={best.Recall:0.000}, F1={best.F1:0.000}, F2={best.F2:0.000})");

        Assert.True(best.F2 >= MinF2,
            $"F2 tốt nhất chỉ đạt {best.F2:0.000} < {MinF2:0.00}. " +
            "Ngưỡng hiện tại không dùng được — xem lại nội dung đem vector hoá hoặc đổi model.");
    }

    /// <summary>
    /// Cặp <b>cùng lĩnh vực khác đề tài</b> phải cho điểm thấp hơn cặp trùng thật.
    ///
    /// <para>Đây là tính chất quan trọng hơn cả con số F1: nếu hai nhóm này chồng lên nhau thì
    /// không tồn tại ngưỡng nào dùng được, và mọi con số metric chỉ là ảo giác của một bộ dữ liệu dễ.</para>
    /// </summary>
    [Fact]
    public void CapTrungThat_PhaiCaoHonCapCungLinhVuc()
    {
        var set = Load();
        if (set.Vectors.Count == 0) return;   // chưa vector hoá — đã báo ở test trên

        double Sim(EvalPair p) =>
            set.Vectors.TryGetValue(p.A, out var a) && set.Vectors.TryGetValue(p.B, out var b)
                ? VectorMath.CosineSimilarity(a, b)
                : double.NaN;

        var dup = set.Pairs.Where(p => p.IsDuplicate).Select(Sim).Where(x => !double.IsNaN(x)).ToList();
        var hard = set.Pairs.Where(p => p.Kind == "HARD_NEGATIVE").Select(Sim).Where(x => !double.IsNaN(x)).ToList();
        if (dup.Count == 0 || hard.Count == 0) return;

        _out.WriteLine($"Trùng thật:      trung bình {dup.Average():0.000}, thấp nhất {dup.Min():0.000}");
        _out.WriteLine($"Cùng lĩnh vực:   trung bình {hard.Average():0.000}, cao nhất  {hard.Max():0.000}");

        Assert.True(dup.Average() > hard.Average(),
            "Cặp trùng thật không tách được khỏi cặp cùng lĩnh vực — tầng 1 chưa dùng được.");
    }
}
