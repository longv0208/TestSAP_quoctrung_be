namespace FURPMS.Application.DTOs.Budget;

public class BudgetItemDto
{
    public int? Id { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryCode { get; set; }
    public string? CategoryName { get; set; }
    public decimal Amount { get; set; }
    public decimal SourceKhoan { get; set; }
    public decimal SourceNgoaiKhoan { get; set; }
    public decimal SourceNsnn { get; set; }
    public decimal SourceOther { get; set; }
    public int Sequence { get; set; }
}
