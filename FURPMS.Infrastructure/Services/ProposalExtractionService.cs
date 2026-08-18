using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
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
        "durationMonths (số nguyên, số tháng thực hiện), totalBudget (số, tổng kinh phí VND). " +
        "Khoá nào không tìm thấy thì để giá trị null.";

    public async Task<ExtractedProposalDto> ExtractAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        // Copy sang MemoryStream để seekable (OpenXml + đọc nhiều lần).
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        ms.Position = 0;

        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (!_gemini.IsConfigured)
            return new ExtractedProposalDto
            {
                Warning = "AI chưa được cấu hình (GeminiAI:ApiKey). Vui lòng nhập tay — file vẫn được lưu làm tài liệu đính kèm."
            };

        try
        {
            string raw = ext switch
            {
                ".pdf" => await _gemini.GenerateFromInlineDataAsync(ms.ToArray(), "application/pdf", Prompt, ct),
                ".docx" => await _gemini.GenerateTextAsync($"{Prompt}\n\n--- NỘI DUNG ---\n{ExtractDocxText(ms)}", ct),
                ".txt" => await _gemini.GenerateTextAsync($"{Prompt}\n\n--- NỘI DUNG ---\n{Encoding.UTF8.GetString(ms.ToArray())}", ct),
                _ => throw new ArgumentException("Chỉ hỗ trợ trích xuất AI cho PDF, DOCX hoặc TXT.")
            };

            return ParseJson(raw);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // AI lỗi (quota/timeout/parse) → không chặn, trả Warning để FE nhập tay.
            return new ExtractedProposalDto
            {
                Warning = $"AI không trích xuất được ({ex.Message}). Vui lòng nhập tay — file vẫn được lưu đính kèm."
            };
        }
    }

    private static string ExtractDocxText(Stream s)
    {
        using var doc = WordprocessingDocument.Open(s, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;

        var sb = new StringBuilder();
        foreach (var para in body.Descendants<Paragraph>())
            sb.AppendLine(para.InnerText);
        return sb.ToString();
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
                DurationMonths = GetInt(r, "durationMonths"),
                TotalBudget = GetDecimal(r, "totalBudget")
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
        if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(), out var s)) return s;
        return null;
    }
}
