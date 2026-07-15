namespace FURPMS.Domain.Entities.MasterData;

public class AmendmentCategory
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
