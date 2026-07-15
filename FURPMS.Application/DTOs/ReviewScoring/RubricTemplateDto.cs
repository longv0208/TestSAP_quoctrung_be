namespace FURPMS.Application.DTOs.ReviewScoring;

public class RubricCriterionDto
{
    public int Id { get; set; }
    public string CriterionName { get; set; } = null!;
    public decimal MaxScore { get; set; }
    public int Sequence { get; set; }
}

public class RubricTemplateDto
{
    public int Id { get; set; }
    public string TemplateType { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal MaxTotalScore { get; set; }
    public bool IsActive { get; set; }
    public List<RubricCriterionDto> Criteria { get; set; } = new();
}
