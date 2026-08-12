using FURPMS.Application.DTOs.AI;
using FURPMS.Application.DTOs.Proposals;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.AI;
using FURPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FURPMS.Infrastructure.Services;

// Tóm tắt AI cho đề xuất: gọi Gemini, lưu kết quả vào bảng llm_outputs (tận dụng entity sẵn có).
public class AiSummaryService : IAiSummaryService
{
    private const string EntityType = "Proposal";
    private const string OutputType = "SUMMARY";

    private readonly FURPMSDbContext _db;
    private readonly IProposalService _proposals;
    private readonly IGeminiService _gemini;
    private readonly IProposalDocumentService _documents;

    public AiSummaryService(FURPMSDbContext db, IProposalService proposals, IGeminiService gemini,
        IProposalDocumentService documents)
    {
        _db = db;
        _proposals = proposals;
        _gemini = gemini;
        _documents = documents;
    }

    public async Task<AiSummaryDto?> GetAsync(Guid proposalId)
    {
        var output = await LatestAsync(proposalId);
        return output == null ? null : Map(output);
    }

    public async Task<AiSummaryDto> GenerateAsync(Guid proposalId, Guid userId, IEnumerable<string> roles)
    {
        var p = await _proposals.GetProposalByIdAsync(proposalId, userId, roles); // 404 nếu không có, 403 nếu không có quyền

        /*
         * Thầy 05/08 (cả 2 bản note): AI phải **đọc FILE đề cương PI nộp**, không chỉ đọc các
         * trường gõ trên web — và **đối chiếu** hai bên. Trước đây prompt chỉ ghép từ form, DTO
         * trả `source: "textFields"`: PI nộp theo đường upload mà form điền sơ sài thì tóm tắt
         * nghèo nàn, vì AI không hề nhìn thấy file.
         *
         * Ống dẫn file đã có sẵn (dùng cho /ai/consistency): PDF gửi thẳng bytes, .docx bóc text.
         */
        var file = await _documents.GetLatestProposalFileAsync(proposalId);
        var prompt = BuildPrompt(p, file?.FileName);

        var summary = file == null
            ? await _gemini.GenerateTextAsync(prompt)
            : await GeminiFileInput.AskAboutFileAsync(
                _gemini, file.Value.Content, file.Value.FileName, file.Value.ContentType, prompt);

        // Vô hiệu hoá bản tóm tắt cũ
        var prev = await _db.LlmOutputs
            .Where(o => o.EntityType == EntityType && o.EntityId == proposalId.ToString() && o.OutputType == OutputType && o.IsActive)
            .ToListAsync();
        foreach (var o in prev) o.IsActive = false;

        var output = new LlmOutput
        {
            EntityType = EntityType,
            EntityId = proposalId.ToString(),
            OutputType = OutputType,
            ModelUsed = "gemini",
            PromptVersion = "v2",          // v2 = đọc file + trả JSON có ưu/nhược
            // Gộp cả nguồn vào JSON để không phải thêm cột (LlmOutput không có SourceFileName).
            Content = PackContent(summary, file?.FileName),
            GeneratedAt = DateTime.UtcNow,
            IsReviewedByHuman = false,
            IsActive = true
        };
        _db.LlmOutputs.Add(output);
        await _db.SaveChangesAsync();
        return Map(output);
    }

    public async Task<AiSummaryDto> UpdateAsync(Guid proposalId, string editedText, Guid userId)
    {
        var output = await LatestAsync(proposalId)
            ?? throw new KeyNotFoundException("Chưa có tóm tắt AI cho đề xuất này. Hãy tạo tóm tắt trước khi sửa.");

        output.ReviewNotes = editedText;
        output.IsReviewedByHuman = true;
        output.ReviewedBy = userId;
        await _db.SaveChangesAsync();
        return Map(output);
    }

    private Task<LlmOutput?> LatestAsync(Guid proposalId) =>
        _db.LlmOutputs
            .Where(o => o.EntityType == EntityType && o.EntityId == proposalId.ToString() && o.OutputType == OutputType && o.IsActive)
            .OrderByDescending(o => o.GeneratedAt)
            .FirstOrDefaultAsync();

    /// <summary>
    /// Gemini hay bọc JSON trong ```json … ``` — gỡ rào rồi nhét thêm nguồn vào cùng object.
    /// Nếu model trả không phải JSON (v1 hoặc model đi lạc) thì giữ nguyên văn bản, `Map` vẫn đọc được.
    /// </summary>
    private static string PackContent(string raw, string? fileName)
    {
        var text = raw.Trim();
        if (text.StartsWith("```"))
        {
            text = text.Trim('`').Trim();
            if (text.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                text = text[4..].Trim();
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            var node = doc.RootElement.Clone();
            var dict = new Dictionary<string, object?>();
            foreach (var prop in node.EnumerateObject())
                dict[prop.Name] = prop.Value.Clone();
            dict["sourceFileName"] = fileName;
            dict["source"] = fileName == null ? "textFields" : "file+form";
            return JsonSerializer.Serialize(dict);
        }
        catch (JsonException)
        {
            return text;   // model không trả JSON — vẫn hiện được dạng văn xuôi
        }
    }

    private static AiSummaryDto Map(LlmOutput o)
    {
        var dto = new AiSummaryDto
        {
            Id = o.Id.ToString(),
            ProposalId = o.EntityId,
            SummaryText = o.Content,
            IsEditedByHuman = o.IsReviewedByHuman,
            EditedText = o.ReviewNotes,
            GeneratedAt = o.GeneratedAt,
            Source = "textFields"
        };

        // Bản v2 lưu JSON; bản v1 (và các bản cũ) lưu văn xuôi — đọc được cả hai.
        try
        {
            using var doc = JsonDocument.Parse(o.Content);
            var r = doc.RootElement;
            if (r.ValueKind != JsonValueKind.Object) return dto;

            if (r.TryGetProperty("summary", out var sum) && sum.ValueKind == JsonValueKind.String)
                dto.SummaryText = sum.GetString() ?? o.Content;
            if (r.TryGetProperty("title", out var ti) && ti.ValueKind == JsonValueKind.String)
                dto.Title = ti.GetString();
            if (r.TryGetProperty("source", out var src) && src.ValueKind == JsonValueKind.String)
                dto.Source = src.GetString();
            if (r.TryGetProperty("sourceFileName", out var fn) && fn.ValueKind == JsonValueKind.String)
                dto.SourceFileName = fn.GetString();
            dto.Strengths = ReadList(r, "strengths");
            dto.Weaknesses = ReadList(r, "weaknesses");
        }
        catch (JsonException) { /* v1: văn xuôi, giữ nguyên SummaryText */ }

        return dto;
    }

    private static List<string> ReadList(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return new List<string>();
        return arr.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString()!)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    /// <summary>
    /// Prompt v2 — thầy 05/08: *"prompt lại tóm tắt AI phải đưa ra response tốt hơn bao gồm
    /// tên đề tài, tóm tắt thông tin, ưu điểm, nhược điểm"*. Trả JSON để FE render thành mục,
    /// thay vì một khối văn xuôi 5–7 câu như v1.
    /// </summary>
    private static string BuildPrompt(ProposalDto p, string? fileName)
    {
        var members = string.Join(", ", p.Members.Select(m => $"{m.FullName} ({m.Role})"));

        // Nêu ĐÍCH DANH tên file trong prompt: chủ nhiệm thường đính kèm nhiều tệp (thuyết minh +
        // lý lịch khoa học — QĐ543 Điều 6.4), nên bản tóm tắt phải nói rõ nó đọc tệp nào, và AI
        // cũng cần biết để nếu tệp không phải đề cương thì nói thẳng ra.
        var fileNote = fileName != null
            ? $@"Kèm theo là FILE ""{fileName}"" — đây được coi là bản thuyết minh đề cương.
Hãy đọc file làm nguồn CHÍNH, và ĐỐI CHIẾU với phần biểu mẫu bên dưới; nếu hai bên lệch nhau, nêu điểm lệch trong ""weaknesses"".
Nếu nội dung file RÕ RÀNG KHÔNG PHẢI đề cương của đề tài này (ví dụ là tài liệu khác, lý lịch khoa học, hay đề tài khác hẳn),
hãy nói thẳng điều đó ở ý ĐẦU TIÊN của ""weaknesses"" và vẫn tóm tắt dựa trên phần biểu mẫu."
            : @"KHÔNG có file đề cương đính kèm — chỉ dựa vào phần biểu mẫu bên dưới.";

        return
$@"Bạn là trợ lý khoa học của hội đồng xét duyệt đề tài NCKH cấp trường.
{fileNote}

Trả về DUY NHẤT một JSON object, không markdown, không giải thích thêm:
{{""title"":""<tên đề tài>"",""summary"":""<tóm tắt 4–6 câu: vấn đề/tính cấp thiết, mục tiêu, phương pháp, sản phẩm dự kiến>"",""strengths"":[""<ưu điểm ngắn gọn>""],""weaknesses"":[""<nhược điểm / điểm cần làm rõ>""]}}
strengths và weaknesses mỗi mục 2–4 ý, viết bằng tiếng Việt, mỗi ý một câu.

Tên đề tài: {p.TitleVI}
Loại nghiên cứu: {p.ResearchType}
Thời gian thực hiện: {p.DurationMonths} tháng
Mục tiêu: {p.Objectives}
Phương pháp/nội dung: {p.Methodology}
Sản phẩm dự kiến: {p.ExpectedOutput}
Thành viên: {members}
Tổng kinh phí: {p.TotalBudget:#,##0} VND";
    }
}
