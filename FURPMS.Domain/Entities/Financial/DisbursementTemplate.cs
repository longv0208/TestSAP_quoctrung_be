using FURPMS.Domain.Entities.MasterData;

namespace FURPMS.Domain.Entities.Financial;

public class DisbursementTemplate
{
    public int Id { get; set; }
    public int ResearchTypeId { get; set; }
    public int RoundNumber { get; set; }
    public decimal Percentage { get; set; }
    public string ConditionDescription { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    public ResearchType ResearchType { get; set; } = null!;
}
