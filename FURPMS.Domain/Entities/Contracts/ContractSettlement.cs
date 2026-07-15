using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Contracts;

public class ContractSettlement
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
    public DateOnly? SettlementDeadline { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Contract Contract { get; set; } = null!;
    public User? SideASignee { get; set; }
}
