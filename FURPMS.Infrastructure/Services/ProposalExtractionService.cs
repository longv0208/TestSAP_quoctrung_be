using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using FURPMS.Application.DTOs.Proposals;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Services;

namespace FURPMS.Infrastructure.Services;

public class ProposalExtractionService : IProposalExtractionService
{
    private readonly IGeminiService _gemini;

    public ProposalExtractionService(IGeminiService gemini)
    {
        _gemini = gemini;
    }

    private const string Prompt =
        "Bạn là trợ lý trích xuất đề cương nghiên cứu khoa học (tiếng Việt). " +
        "Đọc nội dung tài liệu và trả về DUY NHẤT một JSON (không giải thích, không markdown) với các khoá: " +
        "titleVi (tên đề tài tiếng Việt), titleEn (tên tiếng Anh), abstractVi (tóm tắt), " +
        "researchObjectives (mục tiêu nghiên cứu), methodology (phương pháp), expectedOutput (sản phẩm dự kiến), " +
        "urgency (tổng quan và tính cấp thiết), novelty (tính mới và sáng tạo), " +
        "applicationPotential (khả năng ứng dụng), transferPotential (khả năng chuyển giao), " +
        "facilities (cơ sở vật chất và thiết bị sẵn có), " +
        "durationMonths (số nguyên, số tháng thực hiện), totalBudget (số nguyên VND, không kèm ký hiệu hay dấu phân cách), " +
        "budgetItems (mảng các khoản kinh phí; mỗi phần tử có category và amount là số nguyên VND). " +
        "Chỉ dùng đúng một trong 6 category sau theo nội dung khoản chi: " +
        "LABOR=thù lao nghiên cứu; EQUIPMENT=thiết bị/vật tư/nguyên liệu; OUTSOURCED=thuê ngoài; " +
        "CONFERENCE=hội nghị/hội thảo/seminar; OFFICE_OTHER=văn phòng phẩm/chi khác; " +
        "INCIDENTAL_IP=phát sinh/sở hữu trí tuệ. Không có chi tiết thì budgetItems để []. " +
        "teamMembers (mảng thành viên tham gia, KHÔNG gồm chủ nhiệm; mỗi phần tử gồm fullName, email, " +
        "department, academicTitle, role, workMonths, isSecretary). Email hay trường nào không có thì để null. " +
        "Không suy đoán nội dung không có trong tài liệu. Khoá nào không tìm thấy thì để giá trị null.";

    private static readonly HashSet<string> BudgetCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "LABOR", "EQUIPMENT", "OUTSOURCED", "CONFERENCE", "OFFICE_OTHER", "INCIDENTAL_IP"
    };

    public async Task<ExtractedProposalDto> ExtractAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        // Copy sang MemoryStream để seekable (OpenXml + đọc nhiều lần).
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        ms.Position = 0;

        if (!_gemini.IsConfigured)
            return new ExtractedProposalDto
            {
                Warning = "AI chưa được cấu hình (GeminiAI:ApiKey). Vui lòng nhập tay — file vẫn được lưu làm tài liệu đính kèm."
            };

        try
        {
            var raw = await GeminiFileInput.AskAboutFileAsync(
                _gemini, ms.ToArray(), fileName, contentType, Prompt, ct);

            return ParseJson(raw);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Không đẩy nguyên văn lỗi provider/model ra giao diện: vừa khó hiểu, vừa làm lộ chi
            // tiết hạ tầng. AI vẫn chỉ là prefill nên lỗi không được chặn luồng nhập tay.
            return new ExtractedProposalDto
            {
                Warning = FriendlyWarning(ex.Message)
            };
        }
    }

    private static ExtractedProposalDto ParseJson(string raw)
    {
        var json = StripFences(raw);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            return new ExtractedProposalDto
            {
                TitleVi = GetString(r, "titleVi"),
                TitleEn = GetString(r, "titleEn"),
                AbstractVi = GetString(r, "abstractVi"),
                ResearchObjectives = GetString(r, "researchObjectives"),
                Methodology = GetString(r, "methodology"),
                ExpectedOutput = GetString(r, "expectedOutput"),
                Urgency = GetString(r, "urgency"),
                Novelty = GetString(r, "novelty"),
                ApplicationPotential = GetString(r, "applicationPotential"),
                TransferPotential = GetString(r, "transferPotential"),
                Facilities = GetString(r, "facilities"),
                DurationMonths = GetInt(r, "durationMonths"),
                TotalBudget = GetDecimal(r, "totalBudget"),
                BudgetItems = GetBudgetItems(r),
                TeamMembers = GetTeamMembers(r)
            };
        }
        catch
        {
            return new ExtractedProposalDto
            {
                Warning = "AI trả về dữ liệu không đúng định dạng. Vui lòng nhập tay — file vẫn được lưu đính kèm."
            };
        }
    }

    // Gemini hay bọc JSON trong ```json ... ``` — bóc fence ra.
    private static string StripFences(string s)
    {
        s = s.Trim();
        if (s.StartsWith("```"))
        {
            var firstNl = s.IndexOf('\n');
            if (firstNl >= 0) s = s[(firstNl + 1)..];
            if (s.EndsWith("```")) s = s[..^3];
        }
        return s.Trim();
    }

    private static string? GetString(JsonElement r, string key)
        => r.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
            ? (string.IsNullOrWhiteSpace(v.GetString()) ? null : v.GetString())
            : null;

    private static int? GetInt(JsonElement r, string key)
    {
        if (!r.TryGetProperty(key, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n)) return n;
        if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var s)) return s;
        return null;
    }

    private static decimal? GetDecimal(JsonElement r, string key)
    {
        if (!r.TryGetProperty(key, out var v)) return null;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var n)) return n;
        if (v.ValueKind == JsonValueKind.String) return ParseMoney(v.GetString());
        return null;
    }

    private static decimal? ParseMoney(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var raw = value.Trim();
        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var direct))
            return direct;

        // Gemini đôi khi vẫn trả "145.000.000 đ" hoặc "145,000,000 VND" dù prompt yêu cầu số.
        // Tiền VND trong biểu mẫu là số nguyên nên bỏ các dấu phân cách nhóm một cách an toàn.
        var digits = Regex.Replace(raw, @"[^0-9-]", string.Empty);
        return decimal.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var normalized)
            ? normalized
            : null;
    }

    private static List<ExtractedBudgetItemDto> GetBudgetItems(JsonElement root)
    {
        if (!root.TryGetProperty("budgetItems", out var items) || items.ValueKind != JsonValueKind.Array)
            return [];

        return items.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.Object)
            .Select(x => new
            {
                Category = GetString(x, "category")?.ToUpperInvariant(),
                Amount = GetDecimal(x, "amount")
            })
            .Where(x => x.Category != null && BudgetCategories.Contains(x.Category) && x.Amount > 0)
            .GroupBy(x => x.Category!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ExtractedBudgetItemDto
            {
                Category = g.Key,
                Amount = g.Sum(x => x.Amount!.Value)
            })
            .ToList();
    }

    private static List<ExtractedProposalMemberDto> GetTeamMembers(JsonElement root)
    {
        if (!root.TryGetProperty("teamMembers", out var members) || members.ValueKind != JsonValueKind.Array)
            return [];

        return members.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.Object)
            .Select(x => new ExtractedProposalMemberDto
            {
                FullName = GetString(x, "fullName") ?? string.Empty,
                Email = GetString(x, "email"),
                Department = GetString(x, "department"),
                AcademicTitle = GetString(x, "academicTitle"),
                Role = GetString(x, "role"),
                WorkMonths = GetDecimal(x, "workMonths"),
                IsSecretary = GetBool(x, "isSecretary")
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.FullName))
            .GroupBy(x => $"{x.FullName.Trim().ToUpperInvariant()}|{x.Email?.Trim().ToUpperInvariant()}")
            .Select(g => g.First())
            .ToList();
    }

    private static bool GetBool(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var value)) return false;
        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False) return value.GetBoolean();
        return value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var result) && result;
    }

    private static string FriendlyWarning(string message)
    {
        var lower = message.ToLowerInvariant();
        if (lower.Contains("429") || lower.Contains("quota") || lower.Contains("hết hạn mức"))
            return "AI đang quá tải hoặc tạm hết hạn mức. Vui lòng chờ một lát rồi thử lại — file vẫn được lưu đính kèm.";
        if (lower.Contains("503") || lower.Contains("quá tải") || lower.Contains("high demand"))
            return "AI đang xử lý nhiều yêu cầu. Vui lòng chờ một lát rồi thử lại — file vẫn được lưu đính kèm.";
        if (lower.Contains("404") || lower.Contains("model") || lower.Contains("mô hình"))
            return "Mô hình AI tạm thời không khả dụng. Vui lòng nhập tay hoặc báo quản trị viên — file vẫn được lưu đính kèm.";
        return "AI chưa trích xuất được nội dung. Vui lòng thử lại hoặc nhập tay — file vẫn được lưu đính kèm.";
    }
}
