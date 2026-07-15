using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Progress;

public class AmendmentRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContractId { get; set; }
    public int CategoryId { get; set; }
    public string ChangeDescription { get; set; } = null!;
    public decimal? ChangePercentage { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Justification { get; set; } = null!;
    public Guid RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public bool RequiresRectorApproval { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? ReviewerComments { get; set; }

    public Contract Contract { get; set; } = null!;
    public AmendmentCategory Category { get; set; } = null!;
    public User RequestedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
