namespace FURPMS.Application.DTOs.Contract;

public class DeliverableResponse
{
    public int Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ContractId { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string ProductName { get; set; } = null!;
    public string? Description { get; set; }
    /// <summary>Yêu cầu khoa học của sản phẩm — chủ nhiệm khai lúc đăng ký, là căn cứ nghiệm thu.</summary>
    public string? ScientificRequirements { get; set; }
    public string? Notes { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? AcceptanceStatus { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? FileUrl { get; set; }
    /// <summary>Minh chứng thử nghiệm (QĐ543 Điều 13.1).</summary>
    public string? TrialEvidenceUrl { get; set; }
    public string? QualityAssessment { get; set; }
}

public class SubmitDeliverableRequest
{
    /// <summary>Đường dẫn tải bản sản phẩm — nay là URL download của BE sau khi upload.</summary>
    public string FileUrl { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>
    /// Minh chứng thử nghiệm (QĐ543 Điều 13.1). Entity đã có cột này từ đầu nhưng form
    /// chưa bao giờ cho nhập ⇒ hồ sơ nghiệm thu thiếu.
    /// </summary>
    public string? TrialEvidenceUrl { get; set; }
}

// Staff định nghĩa 1 sản phẩm phải nộp cho hợp đồng (đề cương không có trường sản phẩm cấu trúc → nhập tay).
public class CreateDeliverableRequest
{
    public string ProductName { get; set; } = null!;
    public int? CategoryId { get; set; }
    public string? DueDate { get; set; }   // yyyy-MM-dd
    public string? Description { get; set; }
}

public class EvaluateDeliverableRequest
{
    public string AcceptanceStatus { get; set; } = null!;   // PASSED / FAILED
    public string? QualityAssessment { get; set; }
}
