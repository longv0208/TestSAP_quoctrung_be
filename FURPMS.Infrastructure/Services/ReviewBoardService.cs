using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Councils;
using FURPMS.Application.DTOs.ReviewRounds;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Review;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

// Màn "Hội đồng & Chấm" cấp Track — thay cho phân công rải rác theo từng đề xuất
// (redesign theo góp ý user + tham khảo mô hình EasyChair/CMT: track chair mở vòng cho
// cả track → lập panel chấm nhóm bài → mời reviewer → chấm → chốt từng bài qua biên bản).
public class ReviewBoardService : IReviewBoardService
{
    private readonly IReviewRepository _review;
    private readonly IProposalRepository _proposals;
    private readonly ICycleRepository _cycles;

    public ReviewBoardService(IReviewRepository review, IProposalRepository proposals, ICycleRepository cycles)
    {
        _review = review;
        _proposals = proposals;
        _cycles = cycles;
    }

    public async Task<ReviewBoardDto> GetReviewBoardAsync(int cycleId, int trackId)
    {
        var cycleTrack = await _cycles.CycleTracks
            .FirstOrDefaultAsync(ct => ct.CycleId == cycleId && ct.TrackId == trackId)
            ?? throw new KeyNotFoundException("Lĩnh vực chưa được gắn vào đợt này.");

        // Projection: chỉ lấy 4 field cần cho board (tránh nạp full Proposal ~13 cột text — board
        // bị refetch sau mỗi thao tác nên đây là hot path).
        var trackProposals = await _proposals.Query().IgnoreQueryFilters()
            .Where(p => p.IsCurrent && p.Project.CycleTrackId == cycleTrack.Id && !p.Project.IsDeleted
                        && p.Status != ProposalStatus.Draft && p.Status != ProposalStatus.Rejected)
            .Select(p => new ReviewBoardProjectDto
            {
                ProjectId = p.ProjectId,
                ProposalId = p.Id,
                TitleVi = p.TitleVi,
                ProjectStatus = p.Project.Status
            })
            .ToListAsync();

        var titleByProjectId = trackProposals.ToDictionary(p => p.ProjectId, p => p.TitleVi);

        var rounds = await _review.ReviewRounds
            .Include(r => r.ProjectRounds)
            .Where(r => r.CycleTrackId == cycleTrack.Id)
            .OrderBy(r => r.Sequence)
            .ToListAsync();

        var roundIds = rounds.Select(r => r.Id).ToList();

        // Đề tài từng vào vòng nhưng nay không còn "hiện hành hợp lệ" (vd bị REJECTED) — vẫn cần
        // tên để hiển thị lịch sử vòng, bổ sung riêng thay vì lọc lại toàn bộ danh sách trên.
        var missingProjectIds = rounds.SelectMany(r => r.ProjectRounds.Select(pr => pr.ProjectId))
            .Where(pid => !titleByProjectId.ContainsKey(pid))
            .Distinct()
            .ToList();
        if (missingProjectIds.Count > 0)
        {
            var extra = await _proposals.Query().IgnoreQueryFilters()
                .Where(p => p.IsCurrent && missingProjectIds.Contains(p.ProjectId))
                .Select(p => new { p.ProjectId, p.TitleVi })
                .ToListAsync();
            foreach (var p in extra)
                titleByProjectId[p.ProjectId] = p.TitleVi;
        }

        var councils = await _review.Query()
            .Where(c => c.RoundId != null && roundIds.Contains(c.RoundId.Value))
            .Include(c => c.Members).ThenInclude(m => m.User)
            .Include(c => c.ProjectAssignments)
            .ToListAsync();

        var councilsByRound = councils.GroupBy(c => c.RoundId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        var boardRounds = rounds.Select(r =>
        {
            var roundCouncils = councilsByRound.GetValueOrDefault(r.Id) ?? new List<ReviewCouncil>();
            var canDelete = roundCouncils.Count == 0
                && r.ProjectRounds.All(pr => pr.Status == ReviewRoundStatus.Pending && pr.FinalizedAt == null);

            return new ReviewBoardRoundDto
            {
                Id = r.Id,
                RoundNumber = r.RoundNumber,
                Dimension = r.Dimension,
                RoundType = r.RoundType,
                Status = r.Status,
                Result = r.Result,
                RubricTemplateId = r.RubricTemplateId,
                CanDelete = canDelete,
                Projects = r.ProjectRounds.Select(pr => new ReviewBoardProjectRoundDto
                {
                    ProjectId = pr.ProjectId,
                    TitleVi = titleByProjectId.GetValueOrDefault(pr.ProjectId, "—"),
                    Status = pr.Status,
                    Result = pr.Result
                }).ToList(),
                Councils = roundCouncils.Select(c => new ReviewBoardCouncilDto
                {
                    Id = c.Id,
                    Status = c.Status,
                    ProjectIds = c.ProjectAssignments.Select(a => a.ProjectId).ToList(),
                    Members = c.Members.Select(MapMember).ToList()
                }).ToList()
            };
        }).ToList();

        return new ReviewBoardDto
        {
            Projects = trackProposals,   // đã là List<ReviewBoardProjectDto> từ projection ở trên
            Rounds = boardRounds
        };
    }

    public async Task<ReviewRoundResponse> CreateRoundForTrackAsync(int cycleId, int trackId, CreateTrackRoundRequest request)
    {
        // Rule #16 (thầy chốt tuần 10): bỏ hẳn phương diện TÀI CHÍNH — chỉ còn 2 hội đồng
        // (xét duyệt đề cương + nghiệm thu), báo cáo giữa kỳ do Staff duyệt thẳng.
        if (string.IsNullOrWhiteSpace(request.Dimension) || request.Dimension != ReviewRoundDimension.Science)
            throw new ArgumentException(
                "Phương diện chấm chỉ còn SCIENCE (khoa học). Phương diện TÀI CHÍNH đã bỏ theo " +
                "kết luận tuần 10 — báo cáo tiến độ giữa kỳ do Phòng QLKH duyệt trực tiếp, không lập hội đồng.");

        if (string.IsNullOrWhiteSpace(request.RoundType) ||
            !new[] { "REVIEW", "ACCEPTANCE" }.Contains(request.RoundType))
            throw new ArgumentException("RoundType chỉ nhận REVIEW hoặc ACCEPTANCE (rule #16: chỉ 2 hội đồng).");

        var cycleTrack = await _cycles.CycleTracks
            .FirstOrDefaultAsync(ct => ct.CycleId == cycleId && ct.TrackId == trackId)
            ?? throw new KeyNotFoundException("Lĩnh vực chưa được gắn vào đợt này.");

        if (request.PrerequisiteRoundId.HasValue)
        {
            var prereq = await _review.GetRoundByIdAsync(request.PrerequisiteRoundId.Value)
                ?? throw new KeyNotFoundException($"Prerequisite round {request.PrerequisiteRoundId} not found.");
            if (prereq.CycleTrackId != cycleTrack.Id)
                throw new ArgumentException("Vòng tiên quyết không thuộc lĩnh vực này.");
        }

        // DÙNG CHUNG round theo track (helper chung với ReviewRoundService).
        var round = await ReviewShared.GetOrCreateOpenRoundAsync(
            _review, cycleTrack.Id, request.Dimension, request.RoundType, request.RubricTemplateId, request.PrerequisiteRoundId);

        // Tập đề tài đưa vào vòng: chỉ định rõ, hoặc mặc định gom mọi đề tài SUBMITTED/REVISION_REQUIRED
        // của track chưa vào vòng này (rule #7-10: đề tài tự do, 1 track có thể nhiều đề tài cùng vòng).
        List<Guid> targetProjectIds;
        if (request.ProjectIds != null && request.ProjectIds.Count > 0)
        {
            targetProjectIds = request.ProjectIds;
        }
        else
        {
            targetProjectIds = await _proposals.Query().IgnoreQueryFilters()
                .Where(p => p.IsCurrent && p.Project.CycleTrackId == cycleTrack.Id
                            && (p.Status == ProposalStatus.Submitted || p.Status == ProposalStatus.RevisionRequired))
                .Select(p => p.ProjectId)
                .ToListAsync();
        }

        var alreadyLinked = (await _review.ProjectRounds
            .Where(pr => pr.RoundId == round.Id)
            .Select(pr => pr.ProjectId)
            .ToListAsync()).ToHashSet();

        // Rule #2 per-project: nạp MỘT LẦN tập đề tài đã ĐẠT vòng tiên quyết (thay cho query trong vòng lặp).
        HashSet<Guid>? prereqPassed = null;
        if (request.Dimension == ReviewRoundDimension.Finance && round.PrerequisiteRoundId.HasValue)
        {
            prereqPassed = (await _review.ProjectRounds
                .Where(pr => pr.RoundId == round.PrerequisiteRoundId.Value && pr.Status == ReviewRoundStatus.Passed)
                .Select(pr => pr.ProjectId)
                .ToListAsync()).ToHashSet();
        }

        foreach (var projectId in targetProjectIds.Distinct())
        {
            if (alreadyLinked.Contains(projectId)) continue;

            // Bỏ qua (không fail cả batch) đề tài chưa ĐẠT vòng tiên quyết.
            if (prereqPassed != null && !prereqPassed.Contains(projectId)) continue;

            await _review.AddProjectRoundAsync(new ProjectRound
            {
                ProjectId = projectId,
                RoundId = round.Id,
                Status = ReviewRoundStatus.Pending
            });
        }

        await _review.SaveChangesAsync();

        return MapToResponse(round);
    }

    public async Task DeleteRoundAsync(Guid roundId)
    {
        var round = await _review.ReviewRounds
            .Include(r => r.ProjectRounds)
            .FirstOrDefaultAsync(r => r.Id == roundId)
            ?? throw new KeyNotFoundException($"Round {roundId} not found.");

        var hasCouncil = await _review.Query().AnyAsync(c => c.RoundId == roundId);
        if (hasCouncil)
            throw new InvalidOperationException("Vòng đã có hội đồng — không thể xoá.");

        if (round.ProjectRounds.Any(pr => pr.Status != ReviewRoundStatus.Pending || pr.FinalizedAt != null))
            throw new InvalidOperationException("Vòng đã có đề tài được chấm/chốt kết quả — không thể xoá.");

        _review.RemoveProjectRoundsRange(round.ProjectRounds);
        _review.RemoveRound(round);
        await _review.SaveChangesAsync();
    }

    public async Task AddProjectToRoundAsync(Guid roundId, Guid projectId)
    {
        var round = await _review.ReviewRounds
            .FirstOrDefaultAsync(r => r.Id == roundId)
            ?? throw new KeyNotFoundException($"Round {roundId} not found.");

        var project = await _proposals.Projects.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == projectId)
            ?? throw new KeyNotFoundException($"Project {projectId} not found.");

        if (project.CycleTrackId != round.CycleTrackId)
            throw new ArgumentException("Đề tài không thuộc lĩnh vực của vòng này.");

        var existingLink = await _review.ProjectRounds
            .AnyAsync(pr => pr.ProjectId == projectId && pr.RoundId == roundId);
        if (existingLink)
            throw new InvalidOperationException("Đề tài đã tham gia vòng chấm này rồi.");

        // Vòng nào có vòng tiên quyết thì đề tài phải ĐẠT vòng đó mới được vào.
        // Trước đây điều kiện là `Dimension == FINANCE`, mà FINANCE đã bỏ theo rule #16 ⇒ nhánh này
        // thành CODE CHẾT: đề tài trượt vòng xét duyệt vẫn gán được vào vòng nghiệm thu.
        if (round.PrerequisiteRoundId.HasValue)
        {
            var prereqLink = await _review.ProjectRounds
                .FirstOrDefaultAsync(pr => pr.ProjectId == projectId && pr.RoundId == round.PrerequisiteRoundId.Value);
            if (prereqLink == null || prereqLink.Status != ReviewRoundStatus.Passed)
                throw new InvalidOperationException(
                    $"Đề tài chưa ĐẠT vòng tiên quyết (hiện: {prereqLink?.Status ?? "chưa tham gia"}) " +
                    "— chưa thể đưa vào vòng này.");
        }

        await _review.AddProjectRoundAsync(new ProjectRound
        {
            ProjectId = projectId,
            RoundId = roundId,
            Status = ReviewRoundStatus.Pending
        });
        await _review.SaveChangesAsync();
    }

    public async Task RemoveProjectFromRoundAsync(Guid roundId, Guid projectId)
    {
        var link = await _review.ProjectRounds
            .FirstOrDefaultAsync(pr => pr.RoundId == roundId && pr.ProjectId == projectId)
            ?? throw new KeyNotFoundException("Đề tài không tham gia vòng này.");

        if (link.Status != ReviewRoundStatus.Pending || link.FinalizedAt != null)
            throw new InvalidOperationException("Đề tài đã có kết quả trong vòng này — không thể gỡ.");

        var councilIdsInRound = await _review.Query()
            .Where(c => c.RoundId == roundId)
            .Select(c => c.Id)
            .ToListAsync();
        var isAssignedToCouncil = await _review.ProjectAssignments
            .AnyAsync(a => a.ProjectId == projectId && councilIdsInRound.Contains(a.CouncilId));
        if (isAssignedToCouncil)
            throw new InvalidOperationException("Đề tài đã được gán vào hội đồng của vòng này — gỡ khỏi hội đồng trước.");

        _review.RemoveProjectRoundsRange(new[] { link });
        await _review.SaveChangesAsync();
    }

    public async Task<ReviewBoardCouncilDto> CreateCouncilPackageAsync(Guid roundId, CreateCouncilPackageRequest request, Guid createdBy)
    {
        // Cho phép tạo hội đồng CHƯA có đề tài (gán sau qua dropdown ở board).
        if (request.Members.Count == 0)
            throw new ArgumentException("Phải gán ít nhất 1 thành viên hội đồng.");

        var validRoles = new[] { "Member", "Chair", "Secretary", "Opponent" };
        foreach (var m in request.Members)
            if (!validRoles.Contains(m.MemberRole))
                throw new ArgumentException("Vai trong hội đồng chỉ nhận: Thành viên, Chủ tịch, Thư ký hoặc Phản biện.");

        // 1 người chỉ giữ 1 vị trí / hội đồng — không được vừa Chủ tịch vừa Thư ký/Thành viên
        // (trước đây loop add thẳng nên gán trùng 1 người cho 3 vai vẫn lọt). AddMemberAsync đã chặn
        // dup, đây chặn nốt luồng tạo hội đồng.
        if (request.Members.GroupBy(m => m.UserId).Any(g => g.Count() > 1))
            throw new ArgumentException("Mỗi người chỉ được giữ 1 vị trí trong hội đồng.");

        // Rule #12: kết quả chốt qua biên bản Thư ký soạn → Chủ tịch duyệt. Thiếu 1 trong 2 vai trò
        // thì hội đồng KHÔNG bao giờ chốt được → bắt buộc có đủ ngay khi tạo (tránh hội đồng "chết").
        bool Has(string role) => request.Members.Any(m => string.Equals(m.MemberRole, role, StringComparison.OrdinalIgnoreCase));
        if (!Has("Chair"))
            throw new ArgumentException("Hội đồng phải có Chủ tịch (Chair).");
        if (!Has("Secretary"))
            throw new ArgumentException("Hội đồng phải có Thư ký (Secretary).");

        var round = await _review.ReviewRounds
            .FirstOrDefaultAsync(r => r.Id == roundId)
            ?? throw new KeyNotFoundException($"Round {roundId} not found.");

        var joinedProjectIds = await _review.ProjectRounds
            .Where(pr => pr.RoundId == roundId)
            .Select(pr => pr.ProjectId)
            .ToListAsync();
        var notJoined = request.ProjectIds.Except(joinedProjectIds).ToList();
        if (notJoined.Count > 0)
            throw new ArgumentException("Có đề tài chưa tham gia vòng này — hãy thêm vào vòng trước khi tạo hội đồng.");

        // COI (rule #5) — check TRƯỚC khi Add gì vào context (vi phạm → không tạo nửa vời).
        await ReviewShared.AssertNoCoiAsync(_proposals, request.ProjectIds, request.Members.Select(m => m.UserId).ToList());

        var council = new ReviewCouncil
        {
            Id = Guid.NewGuid(),
            RoundId = roundId,
            CouncilType = request.CouncilType ?? round.RoundType,
            MinMembersRequired = 3,
            MaxMembersAllowed = 5,
            Status = CouncilStatus.Forming,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _review.AddAsync(council);

        foreach (var projectId in request.ProjectIds.Distinct())
            await _review.AddProjectAssignmentAsync(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = projectId });

        // Chỉ GÁN, chưa gửi thư mời — Staff bấm "Gửi thư mời" sau (tránh spam, rule #13).
        foreach (var m in request.Members)
        {
            await _review.AddMemberAsync(new CouncilMember
            {
                Id = Guid.NewGuid(),
                CouncilId = council.Id,
                UserId = m.UserId,
                MemberRole = m.MemberRole,
                IsExternal = m.IsExternal,
                Status = CouncilMemberStatus.Assigned,
                InvitationSentAt = null
            });
        }

        await _review.SaveChangesAsync();

        var membersWithUser = await _review.CouncilMembers
            .Include(m => m.User)
            .Where(m => m.CouncilId == council.Id)
            .ToListAsync();

        return new ReviewBoardCouncilDto
        {
            Id = council.Id,
            Status = council.Status,
            ProjectIds = request.ProjectIds.Distinct().ToList(),
            Members = membersWithUser.Select(MapMember).ToList()
        };
    }

    private static CouncilMemberResponse MapMember(CouncilMember m) => ReviewShared.MapMember(m);

    private static ReviewRoundResponse MapToResponse(ReviewRound r) => new()
    {
        Id = r.Id,
        RoundNumber = r.RoundNumber,
        Dimension = r.Dimension,
        RoundType = r.RoundType,
        RubricTemplateId = r.RubricTemplateId,
        Sequence = r.Sequence,
        PrerequisiteRoundId = r.PrerequisiteRoundId,
        Status = r.Status,
        OpenedAt = r.OpenedAt,
        ClosedAt = r.ClosedAt,
        Result = r.Result,
        CouncilId = null
    };
}
