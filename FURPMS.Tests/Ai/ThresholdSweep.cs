using System.Globalization;
using System.Text;

namespace FURPMS.Tests.Ai;

/// <summary>Precision / Recall / F1 tại một ngưỡng cụ thể.</summary>
public record MetricPoint(double Threshold, int Tp, int Fp, int Fn, int Tn)
{
    public double Precision => Tp + Fp == 0 ? 0 : (double)Tp / (Tp + Fp);
    public double Recall => Tp + Fn == 0 ? 0 : (double)Tp / (Tp + Fn);

    public double F1 => Precision + Recall == 0
        ? 0
        : 2 * Precision * Recall / (Precision + Recall);

    /// <summary>
    /// F2 — coi Recall quan trọng gấp đôi Precision.
    ///
    /// <para>Với rà trùng lặp, bỏ sót một cặp trùng thật tốn kém hơn nhiều so với báo động giả:
    /// báo giả thì Phòng QLKH mất hai phút đọc rồi bấm "không trùng"; bỏ sót thì một đề tài trùng
    /// đi thẳng vào hội đồng và có thể được cấp kinh phí.</para>
    /// </summary>
    public double F2
    {
        get
        {
            const double beta2 = 4d;   // β = 2
            var denom = beta2 * Precision + Recall;
            return denom == 0 ? 0 : (1 + beta2) * Precision * Recall / denom;
        }
    }
}

/// <summary>
/// Quét ngưỡng trên bộ gán nhãn để chọn <c>AI_DUPLICATE_THRESHOLD</c>.
///
/// <para><b>Vì sao phải có:</b> cẩm nang chống trượt phạt nặng tính năng AI *"không có metric,
/// không giải thích được ngưỡng ở đâu ra"*. Con số 0.78 trong <c>system_settings</c> phải là kết
/// quả đo được, không phải số bịa cho có.</para>
///
/// <para>Thuần tính toán, không phụ thuộc gì — chạy được offline miễn là bộ gán nhãn đã có vector.</para>
/// </summary>
public static class ThresholdSweep
{
    /// <summary>Các ngưỡng đem thử. Bước 0.02 đủ mịn để thấy chỗ P và R đổi chiều.</summary>
    public static IReadOnlyList<double> DefaultGrid { get; } =
        Enumerable.Range(0, 16).Select(i => Math.Round(0.60 + i * 0.02, 2)).ToList();

    /// <param name="scored">Từng cặp: điểm tương đồng và nhãn người gán.</param>
    public static List<MetricPoint> Run(
        IReadOnlyList<(double Similarity, bool IsDuplicate)> scored,
        IReadOnlyList<double>? grid = null)
    {
        grid ??= DefaultGrid;
        var points = new List<MetricPoint>();

        foreach (var tau in grid)
        {
            int tp = 0, fp = 0, fn = 0, tn = 0;
            foreach (var (sim, label) in scored)
            {
                var flagged = sim >= tau;
                if (label && flagged) tp++;
                else if (!label && flagged) fp++;
                else if (label) fn++;
                else tn++;
            }
            points.Add(new MetricPoint(tau, tp, fp, fn, tn));
        }

        return points;
    }

    /// <summary>
    /// Ngưỡng tốt nhất theo F2 (thiên về recall — xem chú thích ở <see cref="MetricPoint.F2"/>).
    /// Hoà thì chọn ngưỡng CAO hơn: cùng chất lượng thì ít báo động giả hơn.
    /// </summary>
    public static MetricPoint Best(IEnumerable<MetricPoint> points) =>
        points.OrderByDescending(p => p.F2).ThenByDescending(p => p.Threshold).First();

    /// <summary>Bảng kết quả dạng chữ — dán thẳng được vào tài liệu và báo cáo.</summary>
    public static string Format(IEnumerable<MetricPoint> points)
    {
        var c = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("| Ngưỡng | TP | FP | FN | TN | Precision | Recall |    F1 |    F2 |");
        sb.AppendLine("|-------:|---:|---:|---:|---:|----------:|-------:|------:|------:|");
        foreach (var p in points)
            sb.AppendLine(
                $"| {p.Threshold.ToString("0.00", c),6} " +
                $"| {p.Tp,2} | {p.Fp,2} | {p.Fn,2} | {p.Tn,2} " +
                $"| {p.Precision.ToString("0.000", c),9} " +
                $"| {p.Recall.ToString("0.000", c),6} " +
                $"| {p.F1.ToString("0.000", c),5} " +
                $"| {p.F2.ToString("0.000", c),5} |");
        return sb.ToString();
    }
}
