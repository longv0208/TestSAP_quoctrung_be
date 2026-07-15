namespace FURPMS.Domain.Entities.Financial;

public class RubricTemplate
{
    public int Id { get; set; }
    public string TemplateType { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal MaxTotalScore { get; set; } = 100m;
    public int? TrackId { get; set; }    // Review 2 (d): template tiêu chí scope theo track — nullable
    public int? OrderId { get; set; }    // Review 2 (d): scope theo research order — nullable
    public bool IsActive { get; set; } = true;

    public ICollection<RubricCriterion> Criteria { get; set; } = new List<RubricCriterion>();
}
