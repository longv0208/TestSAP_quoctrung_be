namespace FURPMS.Application.DTOs.MasterData;

public class BudgetExpenseCategoryResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Sequence { get; set; }

    /// <summary>Tỷ lệ tối đa trên tổng dự toán (QĐ543 Điều 15.1); null = hạng mục cũ, không soi.</summary>
    public decimal? MaxPercentage { get; set; }

    public bool IsActive { get; set; }
}
