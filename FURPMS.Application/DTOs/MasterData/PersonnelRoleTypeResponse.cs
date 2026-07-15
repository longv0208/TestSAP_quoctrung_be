namespace FURPMS.Application.DTOs.MasterData;

public class PersonnelRoleTypeResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal? DefaultCoefficient { get; set; }
    public bool IsActive { get; set; }
}
