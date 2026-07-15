namespace FURPMS.Domain.Entities.Financial;

public class CouncilRemunerationRate
{
    public int Id { get; set; }
    public string CouncilType { get; set; } = null!;
    public string RoleInCouncil { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public bool IsActive { get; set; } = true;
}
