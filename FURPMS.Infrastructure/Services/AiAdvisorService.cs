using System.Text.Json;
using FURPMS.Application.DTOs.AI;
using FURPMS.Application.Common;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.AI;
using FURPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

/// <inheritdoc cref="IAiAdvisorService"/>
public class AiAdvisorService : IAiAdvisorService
{
    private const string ProposalEntity = "Proposal";
    private const string FeedbackOutput = "FEEDBACK";

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly FURPMSDbContext _db;
    private readonly IProposalService _proposals;
    private readonly IGeminiService _gemini;
    private readonly IRubricResolver _rubrics;
    private readonly IProposalDocumentService _documents;

    public AiAdvisorService(
        FURPMSDbContext db,
        IProposalService proposals,
        IGeminiService gemini,
        IRubricResolver rubrics,
        IProposalDocumentService documents)
    {
        _db = db;
        _proposals = proposals;
        _gemini = gemini;
        _rubrics = rubrics;
        _documents = documents;
    }

    // ── Góp ý đề cương cho PI ────────────────────────────────────────────────

    public async Task<IReadOnlyList<AiFeedbackDto>?> GetProposalFeedbackAsync(
        Guid proposalId, Guid userId, IEnumerable<string> roles)
    {
        _ = await _proposals.GetProposalByIdAsync(proposalId, userId, roles);  // 404/403

        var cached = await LatestFeedbackAsync(proposalId);
        return cached == null ? null : Parse(cached.Content);
    }

    public async Task<IReadOnlyList<AiFeedbackDto>> GenerateProposalFeedbackAsync(
        Guid proposalId, Guid userId, IEnumerable<string> roles)
    {
        var p = await _proposals.GetProposalByIdAsync(proposalId, userId, roles);

        var raw = await _gemini.GenerateTextAsync($@"Bạn là phản biện khoa học. Đọc đề cương dưới đây và nêu 4–6 GÓP Ý cụ thể để tác giả chỉnh sửa cho tốt hơn.
Trả về DUY NHẤT một mảng JSON, không kèm markdown, không giải thích thêm, dạng:
[{{""category"":""<nhóm vấn đề>"",""suggestion"":""<gợi ý cụ thể, 1-2 câu>""}}]
Nhóm vấn đề chọn trong: Mục tiêu, Phương pháp, Sản phẩm dự kiến, Tính khả thi, Kinh phí, Trình bày.

Tên đề tài: {p.TitleVI}
Loại nghiên cứu: {p.ResearchType}
Thời gian: {p.DurationMonths} tháng
Mục tiêu: {p.Objectives}
Phương pháp: {p.Methodology}
Sản phẩm dự kiến: {p.ExpectedOutput}
Tổng kinh phí: {p.TotalBudget:#,##0} VND");

        var items = Parse(raw);
        if (items.Count == 0)
            throw new InvalidOperationException("AI không trả về góp ý đọc được. Hãy thử lại.");

        foreach (var old in await _db.LlmOutputs
                     .Where(o => o.EntityType == ProposalEntity && o.EntityId == proposalId.ToString()
                                 && o.OutputType == FeedbackOutput && o.IsActive)
                     .ToListAsync())
            old.IsActive = false;

        _db.LlmOutputs.Add(new LlmOutput
        {
            EntityType = ProposalEntity,
            EntityId = proposalId.ToString(),
            OutputType = FeedbackOutput,
            ModelUsed = "gemini",
            PromptVersion = "v1",
            Content = JsonSerializer.Serialize(items),
            GeneratedAt = DateTime.UtcNow,
            IsActive = true
        });
        await _db.SaveChangesAsync();

        return items;
    }

    // ── Đối chiếu form ↔ file đề cương ───────────────────────────────────────

    public async Task<AiConsistencyResultDto> CheckConsistencyAsync(
        Guid proposalId, Guid userId, IEnumerable<string> roles)
    {
        var p = await _proposals.GetProposalByIdAsync(proposalId, userId, roles);  // 404/403

        var file = await _documents.GetLatestProposalFileAsync(proposalId);
        if (file == null)
            return new AiConsistencyResultDto { HasFile = false };

        var raw = await _gemini.GenerateFromInlineDataAsync(
            file.Value.Content,
            file.Value.ContentType,
            $@"Đây là FILE đề cương gốc do chủ nhiệm đề tài nộp. Dưới đây là thông tin họ đã ĐIỀN vào biểu mẫu trên hệ thống.
Hãy đối chiếu và chỉ ra những chỗ THIẾU hoặc LỆCH giữa biểu mẫu và file.
Trả về DUY NHẤT một mảng JSON, không markdown, dạng:
[{{""field"":""<tên trường tiếng Việt>"",""kind"":""MISSING|MISMATCH|EXTRA"",""detail"":""<mô tả ngắn, nêu rõ file ghi gì và form ghi gì>""}}]
MISSING = file có nhưng form bỏ trống/thiếu ý. MISMATCH = hai bên khác nhau. EXTRA = form có mà file không đề cập.
Nếu không phát hiện sai lệch nào, trả về mảng rỗng [].

BIỂU MẪU ĐÃ ĐIỀN:
Tên đề tài (VI): {p.TitleVI}
Thời gian thực hiện: {p.DurationMonths} tháng
Mục tiêu: {p.Objectives}
Phương pháp: {p.Methodology}
Sản phẩm dự kiến: {p.ExpectedOutput}
Tổng kinh phí: {p.TotalBudget:#,##0} VND");

        var issues = TryDeserialize<List<AiConsistencyIssueDto>>(raw)
            ?.Where(i => !string.IsNullOrWhiteSpace(i.Detail)).ToList()
            ?? new List<AiConsistencyIssueDto>();

        return new AiConsistencyResultDto
        {
            HasFile = true,
            FileName = file.Value.FileName,
            Issues = issues
        };
    }

    // ── Gợi ý chấm điểm cho thành viên hội đồng ──────────────────────────────

    public async Task<IReadOnlyList<AiScoreSuggestionDto>> SuggestScoresAsync(
        Guid councilId, Guid proposalId, Guid userId, bool isStaffOrAdmin)
    {
        if (!isStaffOrAdmin)
        {
            var isMember = await _db.CouncilMembers
                .AnyAsync(m => m.CouncilId == councilId && m.UserId == userId);
            if (!isMember)
                throw new ForbiddenException("Bạn không phải thành viên hội đồng này.");
        }

        var template = await _rubrics.ResolveForCouncilAsync(councilId)
            ?? throw new KeyNotFoundException(
                "Chưa có bộ tiêu chí nào áp dụng cho hội đồng này — hãy gắn bộ tiêu chí trước khi dùng gợi ý AI.");

        var criteria = template.Criteria.Where(c => c.IsActive).OrderBy(c => c.Sequence).ToList();
        if (criteria.Count == 0)
            throw new InvalidOperationException($"Bộ tiêu chí \"{template.Name}\" chưa có tiêu chí nào.");

        var proposal = await _db.Proposals.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == proposalId)
            ?? throw new KeyNotFoundException("Không tìm thấy đề cương để AI đọc.");

        var criteriaText = string.Join("\n",
            criteria.Select(c => $"- id={c.Id} | {c.CriterionName} | điểm tối đa {c.MaxScore}"));

        var raw = await _gemini.GenerateTextAsync($@"Bạn hỗ trợ thành viên hội đồng chấm đề tài nghiên cứu. Với TỪNG tiêu chí dưới đây, hãy đề xuất một mức điểm và lý do ngắn (1-2 câu) dựa trên nội dung đề cương.
Trả về DUY NHẤT một mảng JSON, không markdown, dạng:
[{{""criterionId"":<id>,""suggestedScore"":<số>,""comment"":""<lý do>""}}]
Điểm phải nằm trong khoảng 0 đến điểm tối đa của tiêu chí đó.

TIÊU CHÍ:
{criteriaText}

ĐỀ CƯƠNG:
Tên: {proposal.TitleVi}
Mục tiêu: {proposal.ResearchObjectives}
Phương pháp: {proposal.Methodology}
Sản phẩm dự kiến: {proposal.ExpectedOutput}
Thời gian: {proposal.DurationMonths} tháng");

        var parsed = ParseScores(raw);

        // Luôn trả ĐỦ tiêu chí theo đúng thứ tự bộ tiêu chí — AI thiếu cái nào thì để 0 kèm
        // ghi chú, để người chấm biết chỗ nào AI không đọc được thay vì mất tiêu chí.
        return criteria.Select(c =>
        {
            var hit = parsed.FirstOrDefault(x => x.CriterionId == c.Id);
            return new AiScoreSuggestionDto
            {
                CriterionId = c.Id,
                CriterionName = c.CriterionName,
                MaxScore = c.MaxScore,
                SuggestedScore = hit == null ? 0m : Math.Clamp(hit.SuggestedScore, 0m, c.MaxScore),
                Comment = hit?.Comment ?? "AI chưa đưa ra nhận định cho tiêu chí này."
            };
        }).ToList();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Task<LlmOutput?> LatestFeedbackAsync(Guid proposalId) =>
        _db.LlmOutputs
            .Where(o => o.EntityType == ProposalEntity && o.EntityId == proposalId.ToString()
                        && o.OutputType == FeedbackOutput && o.IsActive)
            .OrderByDescending(o => o.GeneratedAt)
            .FirstOrDefaultAsync();

    private static List<AiFeedbackDto> Parse(string raw) =>
        TryDeserialize<List<AiFeedbackDto>>(raw)
            ?.Where(x => !string.IsNullOrWhiteSpace(x.Suggestion)).ToList()
        ?? new List<AiFeedbackDto>();

    private static List<RawScore> ParseScores(string raw) =>
        TryDeserialize<List<RawScore>>(raw) ?? new List<RawScore>();

    /// <summary>
    /// Gemini hay bọc JSON trong ```json ... ``` hoặc thêm câu dẫn — cắt lấy đúng phần
    /// mảng rồi mới parse, thay vì để cả tính năng hỏng vì mấy ký tự thừa.
    /// </summary>
    private static T? TryDeserialize<T>(string raw) where T : class
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var start = raw.IndexOf('[');
        var end = raw.LastIndexOf(']');
        if (start < 0 || end <= start) return null;

        try
        {
            return JsonSerializer.Deserialize<T>(raw[start..(end + 1)], JsonOpts);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class RawScore
    {
        public int CriterionId { get; set; }
        public decimal SuggestedScore { get; set; }
        public string? Comment { get; set; }
    }
}
