namespace FURPMS.Application.DTOs.Contract;

public class AmendmentListResponse
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string ChangeDescription { get; set; } = null!;
    public string Justification { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime RequestedAt { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    /// <summary>
    /// Lý do Staff duyệt/từ chối. Nằm ở DANH SÁCH chứ không chỉ ở chi tiết: màn "Đơn của tôi" của
    /// PI chỉ gọi danh sách, thiếu field này thì đơn bị từ chối mà PI không biết vì sao.
    /// </summary>
    public string? ReviewerComments { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class AmendmentDetailResponse : AmendmentListResponse
{
    public decimal? ChangePercentage { get; set; }
    public bool RequiresRectorApproval { get; set; }
}

public class CreateAmendmentRequest
{
    public int CategoryId { get; set; }
    public string ChangeDescription { get; set; } = null!;
    public string Justification { get; set; } = null!;
    public decimal? ChangePercentage { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public bool RequiresRectorApproval { get; set; }
}

public class ReviewAmendmentRequest
{
    public string? ReviewerComments { get; set; }
}
