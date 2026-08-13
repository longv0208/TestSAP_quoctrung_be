using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.TeamMembers;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Projects;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

// Thành viên giờ thuộc PROJECT (Review 2 điểm a). Route vẫn theo proposalId
// (FE giữ nguyên) — service tự resolve proposal → project.
public class TeamMemberService : ITeamMemberService
{
    private readonly IProposalRepository _proposals;
    private readonly IMasterDataRepository _masterData;

    public TeamMemberService(IProposalRepository proposals, IMasterDataRepository masterData)
    {
        _proposals = proposals;
        _masterData = masterData;
    }

    private async Task<Guid> ResolveProjectIdAsync(Guid proposalId)
    {
        var proposal = await _proposals.Query().IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new KeyNotFoundException("Không tìm thấy đề cương.");
        return proposal.ProjectId;
    }

    public async Task<IEnumerable<TeamMemberResponse>> GetTeamMembersAsync(Guid proposalId)
    {
        var projectId = await ResolveProjectIdAsync(proposalId);

        var members = await _proposals.ProjectMembers
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.Sequence)
            .ToListAsync();

        return members.Select(Map);
    }

    public async Task<TeamMemberResponse> AddTeamMemberAsync(Guid proposalId, CreateTeamMemberRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Phải nhập họ tên thành viên.");
        if (string.IsNullOrWhiteSpace(request.WorkContent))
            throw new ArgumentException("Phải nhập nội dung công việc của thành viên.");

        var projectId = await ResolveProjectIdAsync(proposalId);

        decimal? coefficient = request.SalaryCoefficient;
        if (coefficient == null && !string.IsNullOrWhiteSpace(request.MemberRoleCode))
        {
            var roleType = await _masterData.PersonnelRoleTypes
                .FirstOrDefaultAsync(r => r.Code == request.MemberRoleCode && r.IsActive);
            coefficient = roleType?.DefaultCoefficient;
        }

        var maxSeq = await _proposals.ProjectMembers
            .Where(m => m.ProjectId == projectId)
            .MaxAsync(m => (int?)m.Sequence) ?? 0;

        var member = new ProjectMember
        {
            ProjectId = projectId,
            UserId = request.UserId,
            FullName = request.FullName,
            AcademicTitle = request.AcademicTitle,
            UnitName = request.UnitName,
            WorkContent = request.WorkContent,
            WorkMonths = request.WorkMonths,
            IsPi = request.IsPi,
            IsSecretary = request.IsSecretary,
            MemberRoleCode = request.MemberRoleCode,
            SalaryCoefficient = coefficient,
            Sequence = maxSeq + 1
        };

        _proposals.AddProjectMember(member);
        await _proposals.SaveChangesAsync();
        return Map(member);
    }

    public async Task<TeamMemberResponse> UpdateTeamMemberAsync(
        Guid proposalId, int memberId, CreateTeamMemberRequest request, Guid actingUserId)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Phải nhập họ tên thành viên.");
        if (string.IsNullOrWhiteSpace(request.WorkContent))
            throw new ArgumentException("Phải nhập nội dung công việc của thành viên.");

        var (member, _) = await LoadEditableMemberAsync(proposalId, memberId, actingUserId, "sửa");

        decimal? coefficient = request.SalaryCoefficient;
        if (coefficient == null && !string.IsNullOrWhiteSpace(request.MemberRoleCode))
        {
            var roleType = await _masterData.PersonnelRoleTypes
                .FirstOrDefaultAsync(r => r.Code == request.MemberRoleCode && r.IsActive);
            coefficient = roleType?.DefaultCoefficient;
        }

        member.UserId = request.UserId;
        member.FullName = request.FullName;
        member.AcademicTitle = request.AcademicTitle;
        member.UnitName = request.UnitName;
        member.WorkContent = request.WorkContent;
        member.WorkMonths = request.WorkMonths;
        member.IsSecretary = request.IsSecretary;
        member.MemberRoleCode = request.MemberRoleCode;
        member.SalaryCoefficient = coefficient;
        // IsPi KHÔNG cho sửa qua đây — chủ nhiệm là cột neo của đề tài (Project.PiUserId),
        // đổi người thì phải đi qua đề nghị thay đổi nhân sự (BM07), không sửa lén ở danh sách.

        await _proposals.SaveChangesAsync();
        return Map(member);
    }

    public async Task DeleteTeamMemberAsync(Guid proposalId, int memberId, Guid actingUserId)
    {
        var (member, _) = await LoadEditableMemberAsync(proposalId, memberId, actingUserId, "xoá");

        if (member.IsPi)
            throw new InvalidOperationException(
                "Không xoá được chủ nhiệm đề tài khỏi danh sách thành viên — mọi đề tài đều phải có chủ nhiệm.");

        // Thành viên đang được tính công trong dự toán thì xoá sẽ để lại dòng lương mồ côi.
        var hasLabor = await _proposals.Query().IgnoreQueryFilters()
            .Where(p => p.Id == proposalId)
            .SelectMany(p => p.Budget!.LaborDetails)
            .AnyAsync(l => l.ProjectMemberId == memberId);
        if (hasLabor)
            throw new InvalidOperationException(
                $"Thành viên \"{member.FullName}\" đang có dòng thuê khoán chuyên môn trong dự toán " +
                "— xoá dòng dự toán đó trước rồi mới xoá được thành viên.");

        _proposals.RemoveProjectMembersRange(new[] { member });
        await _proposals.SaveChangesAsync();
    }

    /// <summary>
    /// Chỉ **chủ nhiệm** được sửa danh sách nhóm, và chỉ khi đề cương còn **nháp**: đã nộp rồi mà
    /// vẫn thêm/bớt người thì hội đồng chấm một danh sách, hồ sơ lưu một danh sách khác.
    /// Sau khi nộp, muốn đổi nhân sự thì đi qua đề nghị điều chỉnh (BM07).
    /// </summary>
    private async Task<(ProjectMember member, Guid projectId)> LoadEditableMemberAsync(
        Guid proposalId, int memberId, Guid actingUserId, string action)
    {
        var proposal = await _proposals.Query().IgnoreQueryFilters()
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new KeyNotFoundException($"Không tìm thấy đề cương {proposalId}.");

        if (proposal.Project.PiUserId != actingUserId)
            throw new ForbiddenException($"Chỉ chủ nhiệm đề tài mới {action} được thành viên nhóm nghiên cứu.");

        if (proposal.Status != ProposalStatus.Draft && proposal.Status != ProposalStatus.RevisionRequired)
            throw new InvalidOperationException(
                $"Đề cương đang ở trạng thái {StatusText.Vi(proposal.Status)} — chỉ {action} được thành viên khi đề cương " +
                "còn là bản nháp hoặc đang chờ chỉnh sửa. Đã nộp thì gửi đề nghị thay đổi nhân sự (BM07).");

        var member = await _proposals.ProjectMembers
            .FirstOrDefaultAsync(m => m.Id == memberId && m.ProjectId == proposal.ProjectId)
            ?? throw new KeyNotFoundException($"Không tìm thấy thành viên {memberId} trong đề tài này.");

        return (member, proposal.ProjectId);
    }

    private static TeamMemberResponse Map(ProjectMember m) => new()
    {
        Id = m.Id,
        UserId = m.UserId,
        FullName = m.FullName,
        AcademicTitle = m.AcademicTitle,
        UnitName = m.UnitName,
        WorkContent = m.WorkContent,
        WorkMonths = m.WorkMonths,
        IsPi = m.IsPi,
        IsSecretary = m.IsSecretary,
        MemberRoleCode = m.MemberRoleCode,
        SalaryCoefficient = m.SalaryCoefficient,
        Sequence = m.Sequence
    };
}
