using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Councils;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Review;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class CouncilService : ICouncilService
{
    private readonly IReviewRepository _review;
    private readonly IProposalRepository _proposals;

    public CouncilService(IReviewRepository review, IProposalRepository proposals)
    {
        _review = review;
        _proposals = proposals;
    }

    public async Task<CouncilResponse> CreateCouncilAsync(CreateCouncilRequest request, Guid createdBy)
    {
        var reqProposal = await _proposals.Query().IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == request.ProposalId)
            ?? throw new KeyNotFoundException($"Proposal {request.ProposalId} not found.");

        var round = await _review.GetRoundByIdAsync(request.RoundId)
            ?? throw new KeyNotFoundException($"Review round {request.RoundId} not found.");

        // Phase B: đề tài phải đang THAM GIA round (project_round) mới lập council chấm nó.
        var joined = await _review.ProjectRounds
            .AnyAsync(pr => pr.RoundId == request.RoundId && pr.ProjectId == reqProposal.ProjectId);
        if (!joined)
            throw new ArgumentException("The project has not joined the specified review round.");

        var council = new ReviewCouncil
        {
            Id = Guid.NewGuid(),
            RoundId = request.RoundId,
            CouncilType = request.CouncilType,
            EstablishmentDecisionNo = request.EstablishmentDecisionNo,
            EstablishedAt = request.EstablishedAt,
            MeetingDeadline = request.MeetingDeadline,
            MinMembersRequired = request.MinMembersRequired,
            MaxMembersAllowed = request.MaxMembersAllowed,
            Status = CouncilStatus.Forming,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _review.AddAsync(council);
        await _review.AddProjectAssignmentAsync(new CouncilProjectAssignment
        {
            CouncilId = council.Id,
            ProjectId = reqProposal.ProjectId
        });
        await _review.SaveChangesAsync();

        return MapToResponse(council, reqProposal.Id);
    }

    public async Task<CouncilMemberResponse> AddMemberAsync(Guid councilId, AddCouncilMemberRequest request)
    {
        var council = await _review.Query()
            .Include(c => c.Members)
            .Include(c => c.ProjectAssignments)
            .FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException($"Council {councilId} not found.");

        // COI (rule #5) — check với TẤT CẢ đề tài council này được gán chấm.
        var assignedProjectIds = council.ProjectAssignments.Select(a => a.ProjectId).ToList();
        await ReviewShared.AssertNoCoiAsync(_proposals, assignedProjectIds, new[] { request.UserId });

        if (council.Members.Any(m => m.UserId == request.UserId))
            throw new InvalidOperationException("This user is already a member of the council.");

        // Chỉ GÁN, chưa gửi thư mời — Staff gán hết rồi bấm "Gửi thư mời" 1 lần (tránh spam, rule #13).
        var member = new CouncilMember
        {
            Id = Guid.NewGuid(),
            CouncilId = councilId,
            UserId = request.UserId,
            MemberRole = request.MemberRole,
            IsExternal = request.IsExternal,
            Status = CouncilMemberStatus.Assigned,
            InvitationSentAt = null
        };

        await _review.AddMemberAsync(member);
        await _review.SaveChangesAsync();

        var memberWithUser = await _review.CouncilMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == member.Id);

        return MapMember(memberWithUser ?? member);
    }

    // Gán reviewer hết rồi gửi thư mời ĐỒNG LOẠT (1 nút) — set deadline xác nhận cho từng người.
    public async Task<int> SendInvitationsAsync(Guid councilId, DateTime? confirmDeadline)
    {
        var council = await _review.Query().Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException($"Council {councilId} not found.");

        var deadline = confirmDeadline ?? DateTime.UtcNow.AddDays(7);

        // Chỉ gửi cho người CHƯA gửi (ASSIGNED hoặc chưa có InvitationSentAt), bỏ người đã từ chối.
        var pending = council.Members
            .Where(m => m.Status != CouncilMemberStatus.Declined
                        && (m.Status == CouncilMemberStatus.Assigned || m.InvitationSentAt == null))
            .ToList();

        foreach (var m in pending)
        {
            m.InvitationSentAt = DateTime.UtcNow;
            m.TokenExpiresAt = deadline;
            m.InvitationToken = Guid.NewGuid().ToString("N");
            m.Status = CouncilMemberStatus.Invited;
        }

        await _review.SaveChangesAsync();
        return pending.Count;
    }

    public async Task<IEnumerable<MyMembershipDto>> GetMyMembershipsAsync(Guid userId)
    {
        var memberships = await _review.CouncilMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Council)
                .ThenInclude(c => c.Round)
            .Include(m => m.Council)
                .ThenInclude(c => c.ProjectAssignments)
                    .ThenInclude(a => a.Project)
                        .ThenInclude(p => p.Proposals.Where(x => x.IsCurrent))
            .ToListAsync();

        return memberships.Select(m =>
        {
            var firstProject = m.Council.ProjectAssignments.FirstOrDefault()?.Project;
            var currentProposal = firstProject?.Proposals.FirstOrDefault();
            return new MyMembershipDto
            {
                MemberId = m.Id,
                CouncilId = m.CouncilId,
                RoundId = m.Council.RoundId,
                RoundType = m.Council.Round?.RoundType ?? m.Council.CouncilType,
                RoundStatus = m.Council.Round?.Status ?? m.Council.Status,
                MemberRole = m.MemberRole,
                Status = m.Status,
                // FE điều hướng /api/proposals/{id} → phải là id BẢN ĐỀ CƯƠNG hiện hành, không phải projectId
                ProposalId = currentProposal?.Id ?? Guid.Empty,
                ProposalTitleVI = firstProject?.TitleVi ?? string.Empty,
                ProposalStatus = firstProject?.Status ?? string.Empty
            };
        });
    }

    public async Task<CouncilMemberResponse> RespondToMembershipAsync(Guid memberId, Guid userId, bool accept, string? declineReason)
    {
        var member = await _review.CouncilMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId)
            ?? throw new KeyNotFoundException($"Council membership {memberId} not found.");

        if (member.UserId != userId)
            throw new UnauthorizedAccessException("You can only respond to your own invitations.");

        if (member.Status == CouncilMemberStatus.Declined)
            throw new InvalidOperationException("Bạn đã từ chối lời mời này.");

        // Enforce deadline xác nhận: quá hạn → đánh dấu EXPIRED, Staff đi tìm người thay (rule #13).
        if (accept && member.TokenExpiresAt.HasValue && DateTime.UtcNow > member.TokenExpiresAt.Value)
        {
            member.Status = CouncilMemberStatus.Expired;
            await _review.SaveChangesAsync();
            throw new InvalidOperationException("Thư mời đã quá hạn xác nhận.");
        }

        if (accept)
        {
            member.Status = CouncilMemberStatus.Confirmed;
            member.ConfirmedAt = DateTime.UtcNow;
        }
        else
        {
            member.Status = CouncilMemberStatus.Declined;
            member.DeclinedAt = DateTime.UtcNow;
            member.DeclineReason = declineReason;
        }

        await _review.SaveChangesAsync();
        return MapMember(member);
    }

    public async Task<IEnumerable<CouncilMemberResponse>> GetMembersAsync(Guid councilId)
    {
        _ = await _review.Query().FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException($"Council {councilId} not found.");

        var members = await _review.CouncilMembers
            .Where(m => m.CouncilId == councilId)
            .Include(m => m.User)
            .ToListAsync();

        return members.Select(MapMember);
    }

    public async Task RemoveMemberAsync(Guid memberId)
    {
        var member = await _review.CouncilMembers
            .FirstOrDefaultAsync(m => m.Id == memberId)
            ?? throw new KeyNotFoundException($"Council member {memberId} not found.");

        _review.RemoveMember(member);
        await _review.SaveChangesAsync();
    }

    private static CouncilResponse MapToResponse(ReviewCouncil c, Guid projectId) => new()
    {
        Id = c.Id,
        ProposalId = projectId,   // id bản đề cương (điều hướng FE); tham số giữ tên cũ
        RoundId = c.RoundId,
        CouncilType = c.CouncilType,
        EstablishmentDecisionNo = c.EstablishmentDecisionNo,
        EstablishedAt = c.EstablishedAt,
        MeetingDeadline = c.MeetingDeadline,
        MinMembersRequired = c.MinMembersRequired,
        MaxMembersAllowed = c.MaxMembersAllowed,
        Status = c.Status,
        CreatedAt = c.CreatedAt
    };

    private static CouncilMemberResponse MapMember(CouncilMember m) => ReviewShared.MapMember(m);
}
