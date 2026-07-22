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
            ?? throw new KeyNotFoundException($"Proposal {proposalId} not found.");
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
            throw new ArgumentException("FullName is required.");
        if (string.IsNullOrWhiteSpace(request.WorkContent))
            throw new ArgumentException("WorkContent is required.");

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
