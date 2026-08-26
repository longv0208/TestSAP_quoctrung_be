namespace FURPMS.Application.DTOs.Settlements;

public class SettlementDto
{
    public int Id { get; set; }
    public Guid ContractId { get; set; }
    public decimal TotalContractedAmount { get; set; }
    public decimal TotalDisbursedAmount { get; set; }
    public decimal TotalReturnedAmount { get; set; }
    public string? ProductsSubmittedSummary { get; set; }
    public DateOnly? AccountingClearedAt { get; set; }
    public DateOnly? AssetsClearedAt { get; set; }
    public DateTime? SettlementSignedAt { get; set; }
    public Guid? SideASigneeId { get; set; }
    public string? SideASigneeName { get; set; }
    public DateOnly? SettlementDeadline { get; set; }
    /// <summary>Số ngày còn lại tới hạn quyết toán, âm = quá hạn. Máy chủ tính.</summary>
    public int? DaysLeft { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSettlementRequest
{
    public decimal TotalContractedAmount { get; set; }
    public decimal TotalDisbursedAmount { get; set; }
    public decimal TotalReturnedAmount { get; set; }
    public string? ProductsSubmittedSummary { get; set; }
    public DateOnly? SettlementDeadline { get; set; }
    public string? Notes { get; set; }
}

public class SignSettlementRequest
{
    public Guid SideASigneeId { get; set; }
}

public class MarkClearedRequest
{
    /// <summary>Bỏ trống = lấy ngày hiện tại của SERVER (tôn trọng đồng hồ test), không lấy ngày máy người dùng.</summary>
    public DateOnly? ClearedDate { get; set; }

    /// <summary><c>false</c> = <b>BỎ đánh dấu</b> (đường lui khi bấm nhầm). Mặc định <c>true</c>.</summary>
    public bool Clear { get; set; } = true;
}
