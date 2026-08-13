using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.ChangeRequests;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Projects;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

// Yêu cầu thay đổi đề tài: PI gửi (gia hạn/nội dung/nhân sự/kinh phí/tạm dừng)
// → Staff/Admin duyệt. Neo PROJECT; route nhận proposalId (FE giữ nguyên).
public class ChangeRequestService : IChangeRequestService
{
    // Khớp CHANGE_TYPE bên FE (types/changeRequest.ts)
    private static readonly Dictionary<int, string> TypeNames = new()
    {
        [1] = "ExtendTime",
        [2] = "ContentChange",
        [3] = "PersonnelChange",
        [4] = "BudgetChange",
        [5] = "Suspend",
    };

    private readonly IProposalRepository _proposals;

    public ChangeRequestService(IProposalRepository proposals)
    {
        _proposals = proposals;
    }

    public async Task<ChangeRequestDto> CreateAsync(Guid proposalId, CreateChangeRequestRequest request, Guid requestedBy)
    {
        if (!TypeNames.ContainsKey(request.Type))
            throw new ArgumentException("Loại đề nghị phải từ 1 đến 5 (gia hạn / đổi nội dung / đổi nhân sự / đổi kinh phí / tạm dừng).");
        if (string.IsNullOrWhiteSpace(request.Description))
            throw new ArgumentException("Phải mô tả nội dung đề nghị.");

        var proposal = await _proposals.Query().IgnoreQueryFilters()
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new KeyNotFoundException("Không tìm thấy đề cương.");

        var project = proposal.Project;
        if (project.PiUserId != requestedBy)
            throw new ForbiddenException("Chỉ chủ nhiệm đề tài mới gửi được đề nghị điều chỉnh.");

        if (project.Status is ProjectStatus.Completed or ProjectStatus.Cancelled or ProjectStatus.Terminated)
            throw new InvalidOperationException($"Đề tài đã ở trạng thái '{StatusText.Vi(project.Status)}' — không thể gửi yêu cầu thay đổi.");

        var entity = new ProposalChangeRequest
        {
            ProjectId = project.Id,
            Type = request.Type,
            Description = request.Description.Trim(),
            NewValue = request.NewValue,
            Status = "Pending",
            RequestedBy = requestedBy,
            RequestedAt = DateTime.UtcNow
        };
        _proposals.AddChangeRequest(entity);
        await _proposals.SaveChangesAsync();

        return Map(entity, proposalId, project.TitleVi);
    }

    public async Task<IEnumerable<ChangeRequestDto>> GetByProposalAsync(Guid proposalId)
    {
        var proposal = await _proposals.Query().IgnoreQueryFilters()
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new KeyNotFoundException("Không tìm thấy đề cương.");

        var items = await _proposals.ChangeRequests
            .Where(cr => cr.ProjectId == proposal.ProjectId)
            .OrderByDescending(cr => cr.RequestedAt)
            .ToListAsync();

        return items.Select(cr => Map(cr, proposalId, proposal.Project.TitleVi));
    }

    public async Task<IEnumerable<ChangeRequestDto>> GetPendingAsync()
    {
        var items = await _proposals.ChangeRequests
            .Where(cr => cr.Status == "Pending")
            .Include(cr => cr.Project).ThenInclude(p => p.Proposals.Where(x => x.IsCurrent))
            .OrderBy(cr => cr.RequestedAt)
            .ToListAsync();

        return items.Select(cr => Map(
            cr,
            cr.Project.Proposals.FirstOrDefault()?.Id ?? Guid.Empty,
            cr.Project.TitleVi));
    }

    public async Task<ChangeRequestDto> ReviewAsync(Guid id, ReviewChangeRequestRequest request, Guid reviewedBy)
    {
        var entity = await _proposals.ChangeRequests
            .Include(cr => cr.Project).ThenInclude(p => p.Proposals.Where(x => x.IsCurrent))
            .FirstOrDefaultAsync(cr => cr.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu thay đổi.");

        if (entity.Status != "Pending")
            throw new InvalidOperationException($"Yêu cầu đã được xử lý ('{StatusText.Vi(entity.Status)}') — không thể duyệt lại.");

        entity.Status = request.Approved ? "Approved" : "Rejected";
        entity.AdminNote = request.AdminNote;
        entity.ReviewedBy = reviewedBy;
        entity.ReviewedAt = DateTime.UtcNow;
        await _proposals.SaveChangesAsync();

        return Map(entity, entity.Project.Proposals.FirstOrDefault()?.Id ?? Guid.Empty, entity.Project.TitleVi);
    }

    private static ChangeRequestDto Map(ProposalChangeRequest cr, Guid proposalId, string titleVi) => new()
    {
        Id = cr.Id,
        ProposalId = proposalId,
        ProposalTitleVI = titleVi,
        Type = TypeNames.GetValueOrDefault(cr.Type, "ContentChange"),
        Description = cr.Description,
        NewValue = cr.NewValue,
        Status = cr.Status,
        AdminNote = cr.AdminNote,
        RequestedAt = cr.RequestedAt,
        ReviewedAt = cr.ReviewedAt
    };
}
