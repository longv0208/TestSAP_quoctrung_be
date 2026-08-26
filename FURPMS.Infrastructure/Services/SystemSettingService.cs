using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.MasterData;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.MasterData;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class SystemSettingService : ISystemSettingService
{
    private readonly IMasterDataRepository _masterData;

    public SystemSettingService(IMasterDataRepository masterData)
    {
        _masterData = masterData;
    }

    public async Task<IEnumerable<SystemSettingResponse>> GetAllAsync()
    {
        var list = await _masterData.SystemSettings.OrderBy(s => s.Key).ToListAsync();
        return list.Select(Map);
    }

    public async Task<SystemSettingResponse> UpdateAsync(string key, string value, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Giá trị không được để trống.");

        var entity = await _masterData.SystemSettings.FirstOrDefaultAsync(s => s.Key == key)
            ?? throw new KeyNotFoundException($"Không có cấu hình '{key}'.");

        entity.Value = Validate(key, value.Trim());
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = updatedBy;
        _masterData.Update(entity);
        await _masterData.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<ScoringPolicyResponse> GetScoringPolicyAsync() => new()
    {
        ScoreDecimalPlaces = await GetIntAsync(
            SystemSettingKeys.ScoreDecimalPlaces, SystemSettingKeys.DefaultScoreDecimalPlaces)
    };

    public async Task<CouncilPolicyResponse> GetCouncilPolicyAsync() => new()
    {
        AllowRespondOnBehalf = await GetBoolAsync(
            SystemSettingKeys.CouncilAllowRespondOnBehalf,
            SystemSettingKeys.DefaultCouncilAllowRespondOnBehalf)
    };

    public async Task<UploadPolicyResponse> GetUploadPolicyAsync()
    {
        var settings = await _masterData.SystemSettings
            .Where(s => s.Key == SystemSettingKeys.UploadMaxFileSizeMb
                        || s.Key == SystemSettingKeys.UploadAllowedExtensions)
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        var sizeMb = SystemSettingKeys.RecommendedMaxFileSizeMb;
        if (settings.TryGetValue(SystemSettingKeys.UploadMaxFileSizeMb, out var raw)
            && int.TryParse(raw, out var parsed))
        {
            sizeMb = Math.Clamp(parsed, SystemSettingKeys.MinAllowedFileSizeMb, SystemSettingKeys.MaxAllowedFileSizeMb);
        }

        var extRaw = settings.TryGetValue(SystemSettingKeys.UploadAllowedExtensions, out var e) && !string.IsNullOrWhiteSpace(e)
            ? e
            : SystemSettingKeys.DefaultAllowedExtensions;

        return new UploadPolicyResponse
        {
            MaxFileSizeMb = sizeMb,
            RecommendedMaxFileSizeMb = SystemSettingKeys.RecommendedMaxFileSizeMb,
            MinAllowedMb = SystemSettingKeys.MinAllowedFileSizeMb,
            MaxAllowedMb = SystemSettingKeys.MaxAllowedFileSizeMb,
            AllowedExtensions = ParseExtensions(extRaw)
        };
    }

    private async Task<string?> RawAsync(string key) => await _masterData.SystemSettings
        .Where(s => s.Key == key)
        .Select(s => s.Value)
        .FirstOrDefaultAsync();

    public async Task<int> GetIntAsync(string key, int fallback)
        => int.TryParse(await RawAsync(key), out var v) ? v : fallback;

    public async Task<bool> GetBoolAsync(string key, bool fallback)
        => bool.TryParse(await RawAsync(key), out var v) ? v : fallback;

    public async Task<string> GetStringAsync(string key, string fallback)
    {
        var raw = await RawAsync(key);
        return string.IsNullOrWhiteSpace(raw) ? fallback : raw;
    }

    public async Task<IReadOnlyList<int>> GetIntListAsync(string key, IReadOnlyList<int> fallback)
    {
        var raw = await RawAsync(key);
        if (string.IsNullOrWhiteSpace(raw)) return fallback;

        var parsed = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var n) ? n : (int?)null)
            .Where(n => n.HasValue).Select(n => n!.Value)
            .Distinct().OrderByDescending(n => n).ToList();

        return parsed.Count > 0 ? parsed : fallback;
    }

    /// <summary>Chuẩn hóa danh sách đuôi file: bỏ rỗng, ép chữ thường, luôn có dấu chấm đầu.</summary>
    public static IReadOnlyList<string> ParseExtensions(string raw) =>
        raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
           .Select(x => x.StartsWith('.') ? x.ToLowerInvariant() : "." + x.ToLowerInvariant())
           .Distinct()
           .ToList();

    private static string Validate(string key, string value)
    {
        switch (key)
        {
            case SystemSettingKeys.UploadMaxFileSizeMb:
                if (!int.TryParse(value, out var mb))
                    throw new ArgumentException("Dung lượng tối đa phải là số nguyên (MB).");
                if (mb < SystemSettingKeys.MinAllowedFileSizeMb || mb > SystemSettingKeys.MaxAllowedFileSizeMb)
                    throw new ArgumentException(
                        $"Dung lượng tối đa phải trong khoảng {SystemSettingKeys.MinAllowedFileSizeMb}–{SystemSettingKeys.MaxAllowedFileSizeMb} MB " +
                        $"(khuyến cáo {SystemSettingKeys.RecommendedMaxFileSizeMb} MB).");
                return mb.ToString();

            case SystemSettingKeys.UploadAllowedExtensions:
                var exts = ParseExtensions(value);
                if (exts.Count == 0)
                    throw new ArgumentException("Phải có ít nhất 1 định dạng được phép.");
                return string.Join(",", exts);

            case SystemSettingKeys.CouncilInviteDeadlineDays:
                if (!int.TryParse(value, out var days) || days < 1 || days > 60)
                    throw new ArgumentException("Hạn xác nhận lời mời phải từ 1 đến 60 ngày.");
                return days.ToString();

            case SystemSettingKeys.DeadlineReminderDays:
                var marks = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => int.TryParse(x, out var n) ? n : -1).ToList();
                if (marks.Count == 0 || marks.Any(n => n < 1 || n > 365))
                    throw new ArgumentException("Mốc nhắc hạn phải là các số ngày 1–365, cách nhau bằng dấu phẩy (vd: 30,14,7).");
                return string.Join(",", marks.Distinct().OrderByDescending(n => n));

            case SystemSettingKeys.EmailEnabled:
                if (!bool.TryParse(value, out var enabled))
                    throw new ArgumentException("Giá trị phải là true hoặc false.");
                return enabled.ToString().ToLowerInvariant();

            case SystemSettingKeys.ContractSideARepresentative:
                if (value.Length > 200)
                    throw new ArgumentException("Tên người đại diện quá dài.");
                return value;

            // Sáu khoá "số ngày" của các giai đoạn — cùng một luật kiểm, gộp một nhánh.
            case SystemSettingKeys.ScoringWindowDays:
            case SystemSettingKeys.RevisionDeadlineDays:
            case SystemSettingKeys.FinalReportLeadDays:
            case SystemSettingKeys.ContractSignWindowDays:
            case SystemSettingKeys.ArchivalLeadDays:
            case SystemSettingKeys.MeetingDeadlineWorkingDays:
                if (!int.TryParse(value, out var stageDays) || stageDays < 1
                    || stageDays > SystemSettingKeys.MaxStageWindowDays)
                    throw new ArgumentException(
                        $"Số ngày phải trong khoảng 1–{SystemSettingKeys.MaxStageWindowDays}.");
                return stageDays.ToString();

            case SystemSettingKeys.ReviewPassThresholdPct:
                if (!int.TryParse(value, out var pct) || pct < 0 || pct > 100)
                    throw new ArgumentException("Ngưỡng điểm đạt phải là phần trăm từ 0 đến 100.");
                return pct.ToString();

            default:
                return value;
        }
    }

    private static SystemSettingResponse Map(SystemSetting s) => new()
    {
        Id = s.Id,
        Key = s.Key,
        Value = s.Value,
        RecommendedValue = s.RecommendedValue,
        Description = s.Description,
        UpdatedAt = s.UpdatedAt
    };
}
