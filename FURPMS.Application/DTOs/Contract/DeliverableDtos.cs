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
    public DateOnly? DueDate { get; set; }
    public string? AcceptanceStatus { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? FileUrl { get; set; }
    public string? QualityAssessment { get; set; }
}

public class SubmitDeliverableRequest
{
    public string FileUrl { get; set; } = null!;
    public string? Description { get; set; }
}

public class EvaluateDeliverableRequest
{
    public string AcceptanceStatus { get; set; } = null!;   // PASSED / FAILED
    public string? QualityAssessment { get; set; }
}
