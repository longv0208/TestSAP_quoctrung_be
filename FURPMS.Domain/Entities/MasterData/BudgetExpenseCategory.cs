namespace FURPMS.Domain.Entities.MasterData;

public class BudgetExpenseCategory
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Sequence { get; set; }

    /// <summary>
    /// Tỷ lệ tối đa của hạng mục này tính trên TỔNG kinh phí đề tài — QĐ543 <b>Điều 15.1</b>
    /// (thù lao 100 · thiết bị 60 · thuê ngoài 60 · hội thảo 30 · VPP &amp; chi khác 20 ·
    /// phát sinh/SHTT 10). <c>null</c> = hạng mục cũ ngoài quy định, không soi tỷ lệ.
    /// </summary>
    public decimal? MaxPercentage { get; set; }

    public bool IsActive { get; set; } = true;
}
