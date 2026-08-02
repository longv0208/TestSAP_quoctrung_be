namespace FURPMS.Application.DTOs.Contract;

public class DisbursementResponse
{
    public int Id { get; set; }
    public Guid ContractId { get; set; }
    public int RoundNumber { get; set; }
    public decimal Percentage { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public string ConditionDescription { get; set; } = null!;
    public DateTime? ConditionMetAt { get; set; }
    public DateTime? DisbursedAt { get; set; }
    public string? BankReference { get; set; }
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
    public int? DeliverableId { get; set; }
}

// Rule tuần 10: hệ thống KHÔNG quản tiền — "xác nhận" = đánh dấu đã giải ngân (kèm ghi chú/minh chứng).
// ActualAmount/BankReference giữ optional để tương thích dữ liệu cũ, KHÔNG bắt buộc nhập.
public class ConfirmDisbursementRequest
{
    public decimal? ActualAmount { get; set; }
    public string? BankReference { get; set; }
    public string? Notes { get; set; }
}
