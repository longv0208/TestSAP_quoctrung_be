namespace FURPMS.Domain.Entities.Financial;

public class RubricCriterion
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public string CriterionName { get; set; } = null!;
    public decimal MaxScore { get; set; }
    public int Sequence { get; set; }
    public bool IsActive { get; set; } = true;

    public RubricTemplate Template { get; set; } = null!;
}
