namespace FURPMS.Application.DTOs.MasterData;

public class UpsertPersonnelRoleTypeRequest
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal? DefaultCoefficient { get; set; }
    public bool IsActive { get; set; } = true;
}
