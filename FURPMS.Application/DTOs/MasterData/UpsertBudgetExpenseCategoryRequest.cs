namespace FURPMS.Application.DTOs.MasterData;

public class UpsertBudgetExpenseCategoryRequest
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Sequence { get; set; }

    /// <summary>Tỷ lệ tối đa trên tổng dự toán (QĐ543 Điều 15.1); để trống = không soi tỷ lệ.</summary>
    public decimal? MaxPercentage { get; set; }

    public bool IsActive { get; set; } = true;
}
