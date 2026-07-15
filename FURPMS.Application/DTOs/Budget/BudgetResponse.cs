namespace FURPMS.Application.DTOs.Budget;

public class BudgetResponse
{
    public int BudgetId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal LaborAmount { get; set; }
    public decimal EquipmentAmount { get; set; }
    public decimal ExternalServiceAmount { get; set; }
    public decimal ConferenceAmount { get; set; }
    public decimal OfficeSuppliesAmount { get; set; }
    public decimal IncidentalIpAmount { get; set; }
    public IEnumerable<BudgetItemDto> Items { get; set; } = new List<BudgetItemDto>();
}
