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
public class UploadPolicyResponse
{
    public int MaxFileSizeMb { get; set; }
    public int RecommendedMaxFileSizeMb { get; set; }
    public int MinAllowedMb { get; set; }
    public int MaxAllowedMb { get; set; }
    public IEnumerable<string> AllowedExtensions { get; set; } = Array.Empty<string>();
}
