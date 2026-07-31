using FURPMS.Application.DTOs.AI;
using FURPMS.Application.DTOs.Proposals;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.AI;
using FURPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

// Tóm tắt AI cho đề xuất: gọi Gemini, lưu kết quả vào bảng llm_outputs (tận dụng entity sẵn có).
public class AiSummaryService : IAiSummaryService
{
    private const string EntityType = "Proposal";
    private const string OutputType = "SUMMARY";

    private readonly FURPMSDbContext _db;
    private readonly IProposalService _proposals;
    private readonly IGeminiService _gemini;

    public AiSummaryService(FURPMSDbContext db, IProposalService proposals, IGeminiService gemini)
    {
        _db = db;
        _proposals = proposals;
        _gemini = gemini;
    }

    public async Task<AiSummaryDto?> GetAsync(Guid proposalId)
    {
        var output = await LatestAsync(proposalId);
        return output == null ? null : Map(output);
    }

    public async Task<AiSummaryDto> GenerateAsync(Guid proposalId, Guid userId)
    {
        var p = await _proposals.GetProposalByIdAsync(proposalId); // ném 404 nếu không có
        var summary = await _gemini.GenerateTextAsync(BuildPrompt(p));

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
            PromptVersion = "v1",
            Content = summary,
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

    private static AiSummaryDto Map(LlmOutput o) => new()
    {
        Id = o.Id.ToString(),
        ProposalId = o.EntityId,
        SummaryText = o.Content,
        IsEditedByHuman = o.IsReviewedByHuman,
        EditedText = o.ReviewNotes,
        GeneratedAt = o.GeneratedAt,
        Source = "textFields",
        SourceFileName = null
    };

    private static string BuildPrompt(ProposalDto p)
    {
        var members = string.Join(", ", p.Members.Select(m => $"{m.FullName} ({m.Role})"));
        return
$@"Bạn là trợ lý khoa học của hệ thống quản lý đề tài nghiên cứu. Hãy viết một bản TÓM TẮT ngắn gọn bằng tiếng Việt (5–7 câu) cho hội đồng đọc nhanh, nêu rõ: vấn đề/tính cấp thiết, mục tiêu, phương pháp, sản phẩm dự kiến và điểm nổi bật. Chỉ trả về đoạn tóm tắt, không thêm tiêu đề hay markdown.

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
