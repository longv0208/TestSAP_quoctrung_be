namespace FURPMS.Application.DTOs.Settlements;

public class SettlementDto
{
    public int Id { get; set; }
    public Guid ContractId { get; set; }
    public decimal TotalContractedAmount { get; set; }
    public decimal TotalDisbursedAmount { get; set; }
    public decimal TotalReturnedAmount { get; set; }
    public string? ProductsSubmittedSummary { get; set; }
    public DateOnly? AccountingClearedAt { get; set; }
    public DateOnly? AssetsClearedAt { get; set; }
    public DateTime? SettlementSignedAt { get; set; }
    public Guid? SideASigneeId { get; set; }
    public string? SideASigneeName { get; set; }
    public DateOnly? SettlementDeadline { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSettlementRequest
{
    public decimal TotalContractedAmount { get; set; }
    public decimal TotalDisbursedAmount { get; set; }
    public decimal TotalReturnedAmount { get; set; }
    public string? ProductsSubmittedSummary { get; set; }
    public DateOnly? SettlementDeadline { get; set; }
    public string? Notes { get; set; }
}

public class SignSettlementRequest
{
    public Guid SideASigneeId { get; set; }
}

public class MarkClearedRequest
{
    public DateOnly ClearedDate { get; set; }
}
