namespace FURPMS.Application.DTOs.MasterData;

public class UpsertSystemFinancialConfigRequest
{
    public string Code { get; set; } = null!;
    public decimal Value { get; set; }
    public string? Description { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public bool IsActive { get; set; } = true;
}
