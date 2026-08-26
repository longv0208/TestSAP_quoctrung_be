namespace FURPMS.Application.DTOs.Ai;

/// <summary>Mức nghiêm trọng của một cặp nghi trùng — <b>để xếp thứ tự xem, không phải để chặn</b>.</summary>
public static class DuplicateSeverity
{
    /// <summary>Dưới ngưỡng cảnh báo — chỉ liệt kê cho đủ ngữ cảnh.</summary>
    public const string Low = "LOW";
    /// <summary>Vượt ngưỡng cảnh báo — Phòng QLKH nên xem.</summary>
    public const string Warn = "WARN";
    /// <summary>Gần như trùng khít — xem trước tiên. Vẫn KHÔNG tự chặn nộp.</summary>
    public const string High = "HIGH";
}

/// <summary>Kết luận của Phòng QLKH sau khi xem cảnh báo.</summary>
public static class DuplicateVerdict
{
    public const string NotDuplicate = "NOT_DUPLICATE";
    public const string NeedsRevision = "NEEDS_REVISION";
    public const string Duplicate = "DUPLICATE";
}

/// <summary>Một đề tài trong kho bị chấm là giống với đề cương đang xét.</summary>
public class DuplicateMatchDto
{
    public Guid ProposalId { get; set; }
    public Guid ProjectId { get; set; }
    public string? ProjectCode { get; set; }
    public string TitleVi { get; set; } = null!;
    public string? PiName { get; set; }
    public int? CycleYear { get; set; }
    public string? ProjectStatus { get; set; }

    /// <summary>Cosine similarity trong [0, 1] — <b>tất định</b>, cùng cặp luôn ra cùng số.</summary>
    public double Similarity { get; set; }
    public string Severity { get; set; } = DuplicateSeverity.Low;
}

/// <summary>Kết quả rà trùng lặp cho một đề cương.</summary>
public class DuplicateCheckResponse
{
    public Guid ProposalId { get; set; }
    public string TitleVi { get; set; } = null!;

    /// <summary>Đã vector hoá được đề cương này chưa. <c>false</c> = chưa chạy hoặc chưa cấu hình AI.</summary>
    public bool Indexed { get; set; }

    /// <summary>Số đề tài trong kho đã có vector để đối chiếu — cho biết kết quả đáng tin tới đâu.</summary>
    public int CorpusSize { get; set; }

    public decimal WarnThreshold { get; set; }
    public decimal HighThreshold { get; set; }

    public List<DuplicateMatchDto> Matches { get; set; } = new();

    /// <summary>
    /// Giải thích do mô hình sinh chữ viết (tầng 2) — <c>null</c> nếu chưa chạy hoặc không cần.
    ///
    /// <para>Chỉ chạy khi có cặp vượt ngưỡng, hoặc khi Phòng QLKH bấm yêu cầu. Tầng 1 đã đủ để
    /// xếp thứ tự; tầng 2 tốn quota nên không chạy vô cớ.</para>
    /// </summary>
    public string? Explanation { get; set; }
    public DateTime? ExplanationGeneratedAt { get; set; }
    public string? ExplanationModel { get; set; }

    /// <summary>Kết luận của người xem xét — <c>null</c> = chưa ai xem.</summary>
    public string? Verdict { get; set; }
    public string? VerdictNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedByName { get; set; }
}

/// <summary>Phòng QLKH chốt kết luận sau khi xem cảnh báo trùng lặp.</summary>
public class ReviewDuplicateRequest
{
    /// <summary>Một trong <see cref="DuplicateVerdict"/>.</summary>
    public string Verdict { get; set; } = null!;
    public string? Note { get; set; }
}

/// <summary>
/// Cờ trùng lặp RÚT GỌN cho một đề cương — dùng ở màn DANH SÁCH, không phải màn chi tiết.
///
/// <para>Chỉ đủ để vẽ một badge: có vượt ngưỡng không và mức cao nhất. Không kèm danh sách các đề
/// tài giống — đó là việc của <see cref="DuplicateCheckResponse"/> khi mở chi tiết một đề cương.</para>
/// </summary>
public class DuplicateFlagDto
{
    public bool Indexed { get; set; }
    /// <summary>Mức nghiêm trọng CAO NHẤT trong các đề tài đối chiếu được — null nếu không có gì vượt LOW.</summary>
    public string? MaxSeverity { get; set; }
    public double? MaxSimilarity { get; set; }
}

/// <summary>Kết quả một lần vector hoá lại toàn kho.</summary>
public class ReindexEmbeddingsResponse
{
    public int Scanned { get; set; }
    public int Embedded { get; set; }
    /// <summary>Bỏ qua vì nội dung không đổi kể từ lần vector hoá trước (so theo <c>ContentHash</c>).</summary>
    public int Unchanged { get; set; }
    public int Failed { get; set; }
    public string? Model { get; set; }
}
