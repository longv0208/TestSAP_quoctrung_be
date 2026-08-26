using FURPMS.Application.DTOs.MasterData;

namespace FURPMS.Application.Interfaces.Services;

public interface ISystemSettingService
{
    Task<IEnumerable<SystemSettingResponse>> GetAllAsync();
    Task<SystemSettingResponse> UpdateAsync(string key, string value, Guid updatedBy);
    /// <summary>Giới hạn upload hiện hành (đã clamp trong khoảng cho phép).</summary>
    Task<UploadPolicyResponse> GetUploadPolicyAsync();
    Task<ScoringPolicyResponse> GetScoringPolicyAsync();
    Task<CouncilPolicyResponse> GetCouncilPolicyAsync();

    // ── Đọc có kiểu ───────────────────────────────────────────────────────────
    // Giá trị thiếu/hỏng trong DB đều rơi về `fallback` — cấu hình sai không được
    // phép làm chết nghiệp vụ.
    Task<int> GetIntAsync(string key, int fallback);
    Task<bool> GetBoolAsync(string key, bool fallback);
    Task<string> GetStringAsync(string key, string fallback);
    /// <summary>Đọc số thực. Luôn phân tích theo <c>InvariantCulture</c> — máy đặt ngôn ngữ tiếng
    /// Việt mà đọc theo văn hoá máy thì "0.78" thành 78.</summary>
    Task<decimal> GetDecimalAsync(string key, decimal fallback);
    /// <summary>Đọc danh sách số ngăn bằng dấu phẩy, ví dụ "30,14,7".</summary>
    Task<IReadOnlyList<int>> GetIntListAsync(string key, IReadOnlyList<int> fallback);
}
