namespace FURPMS.Domain.Entities.MasterData;

/// <summary>
/// Cấu hình vận hành dạng key-value (Admin chỉnh trong app, không cần deploy lại).
/// Khác <see cref="SystemFinancialConfig"/> — bảng kia chỉ chứa hệ số TÀI CHÍNH (decimal, có hiệu lực theo ngày).
/// </summary>
public class SystemSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    /// <summary>Giá trị KHUYẾN CÁO — hiện trên UI để Admin biết mức chuẩn, và là giá trị fallback khi Value hỏng.</summary>
    public string RecommendedValue { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
}
