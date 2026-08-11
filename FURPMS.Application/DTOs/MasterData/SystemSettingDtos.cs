namespace FURPMS.Application.DTOs.MasterData;

public class SystemSettingResponse
{
    public int Id { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    /// <summary>Mức khuyến cáo — UI hiện kèm để Admin biết đâu là giá trị chuẩn.</summary>
    public string RecommendedValue { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateSystemSettingRequest
{
    public string Value { get; set; } = null!;
}

/// <summary>Chính sách upload đã giải mã sẵn — FE dùng để validate trước khi gửi file.</summary>
/// <summary>
/// Cấu hình liên quan tới CHẤM ĐIỂM mà mọi người dùng đã đăng nhập cần đọc được.
/// Tách khỏi <c>GET /system-settings</c> (chỉ Admin) vì màn chấm điểm của hội đồng phải biết
/// bước nhảy điểm để dựng ô nhập — trước đây gọi endpoint Admin nên reviewer luôn ăn 403, rơi
/// về mặc định số nguyên, và cấu hình cho phép thập phân của Admin **không có tác dụng gì**.
/// </summary>
public class ScoringPolicyResponse
{
    /// <summary>0 = chỉ số nguyên; 1 = cho 0.5 / 7.5…</summary>
    public int ScoreDecimalPlaces { get; set; }
}

public class UploadPolicyResponse
{
    public int MaxFileSizeMb { get; set; }
    public int RecommendedMaxFileSizeMb { get; set; }
    public int MinAllowedMb { get; set; }
    public int MaxAllowedMb { get; set; }
    public IEnumerable<string> AllowedExtensions { get; set; } = Array.Empty<string>();
}
