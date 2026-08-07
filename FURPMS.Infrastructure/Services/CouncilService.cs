using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Councils;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.AI;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Review;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class CouncilService : ICouncilService
{
    private readonly IReviewRepository _review;
    private readonly IProposalRepository _proposals;
    private readonly ISystemSettingService _settings;
    private readonly INotificationRepository _notifications;
    private readonly IEmailService _email;

    public CouncilService(IReviewRepository review, IProposalRepository proposals,
        ISystemSettingService settings,
        INotificationRepository notifications,
        IEmailService email)
    {
        _review = review;
        _proposals = proposals;
        _settings = settings;
        _notifications = notifications;
        _email = email;
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

        // Gate (rule tuần 10): đủ Chủ tịch + Thư ký + đã có LỊCH HỌP (ngày/giờ + địa điểm/link) mới cho gửi.
        bool HasRole(params string[] roles) => council.Members.Any(m =>
            m.MemberRole != null && roles.Any(r => m.MemberRole.Trim().Equals(r, StringComparison.OrdinalIgnoreCase)));
        if (!HasRole("Chair", "Chairman"))
            throw new InvalidOperationException("Chưa có Chủ tịch hội đồng — không thể gửi thư mời.");
        if (!HasRole("Secretary"))
            throw new InvalidOperationException("Chưa có Thư ký hội đồng — không thể gửi thư mời.");
        if (!await _review.Meetings.AnyAsync(mt => mt.CouncilId == councilId))
            throw new InvalidOperationException("Chưa có lịch họp — đặt ngày/giờ + địa điểm trước khi gửi thư mời.");

        var inviteDays = await _settings.GetIntAsync(
            SystemSettingKeys.CouncilInviteDeadlineDays, SystemSettingKeys.DefaultCouncilInviteDeadlineDays);
        var deadline = confirmDeadline ?? DateTime.UtcNow.AddDays(inviteDays);

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

        // Báo cho người được mời — trước đây nút "Gửi thư mời" chỉ đổi trạng thái trong DB,
        // không ai được thông báo nên reviewer phải tự mò vào trang Lời mời mới biết.
        var invitedUserIds = pending.Select(m => m.UserId).ToList();
        var invitedUsers = await _review.CouncilMembers
            .Where(m => invitedUserIds.Contains(m.UserId))
            .Include(m => m.User)
            .Select(m => new { m.UserId, m.User!.Email, m.User.FullName })
            .Distinct()
            .ToListAsync();

        const string title = "Thư mời tham gia hội đồng đánh giá";
        var body = $"Bạn được mời tham gia một hội đồng đánh giá đề tài. " +
                   $"Vui lòng xác nhận hoặc từ chối trước {deadline:dd/MM/yyyy}.";

        foreach (var u in invitedUsers)
        {
            await _notifications.AddAsync(new Notification
            {
                UserId = u.UserId,
                NotificationType = "COUNCIL_INVITATION",
                Title = title,
                Body = body,
                ActionUrl = "/invitations",
                RelatedEntityType = "ReviewCouncil",
                RelatedEntityId = councilId.ToString(),
                Priority = "HIGH"
            });

            if (!string.IsNullOrWhiteSpace(u.Email))
                await _email.SendAsync(u.Email, title, body, "COUNCIL_INVITATION", u.UserId);
        }
        await _notifications.SaveChangesAsync();

        return pending.Count;
    }

    public async Task<IEnumerable<MyMembershipDto>> GetMyMembershipsAsync(Guid userId)
    {
        var memberships = await _review.CouncilMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Council)
                .ThenInclude(c => c.Round)
            .Include(m => m.Council)
                .ThenInclude(c => c.Meetings)
            .Include(m => m.Council)
                .ThenInclude(c => c.ProjectAssignments)
                    .ThenInclude(a => a.Project)
                        .ThenInclude(p => p.PiUser)
            .Include(m => m.Council)
                .ThenInclude(c => c.ProjectAssignments)
                    .ThenInclude(a => a.Project)
                        .ThenInclude(p => p.CycleTrack)
                            .ThenInclude(ct => ct.Track)
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
                // projectId để FE truyền cho SaveMinutes (biên bản neo theo PROJECT, không phải proposal).
                // Thiếu field này khiến FE truyền nhầm proposalId → "Đề tài không thuộc phạm vi chấm".
                ProjectId = firstProject?.Id ?? Guid.Empty,
                ProposalTitleVI = firstProject?.TitleVi ?? string.Empty,
                ProposalStatus = firstProject?.Status ?? string.Empty,
                PiName = firstProject?.PiUser?.FullName,
                TrackName = firstProject?.CycleTrack?.Track?.Name,
                CreatedAt = m.Council.CreatedAt,
                NextMeetingAt = m.Council.Meetings.OrderBy(mt => mt.ScheduledAt).Select(mt => (DateTime?)mt.ScheduledAt).FirstOrDefault()
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
            throw new ForbiddenException("You can only respond to your own invitations.");

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

    // Staff/Admin bấm "Xác nhận thay" — reviewer đã đồng ý ngoài hệ thống (điện thoại/email),
    // hoặc tiện demo. Chuyển ASSIGNED/INVITED → CONFIRMED, không cần đăng nhập tài khoản reviewer.
    public async Task<CouncilMemberResponse> ConfirmMemberOnBehalfAsync(Guid memberId)
    {
        var member = await _review.CouncilMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId)
            ?? throw new KeyNotFoundException($"Council membership {memberId} not found.");

        if (member.Status == CouncilMemberStatus.Declined)
            throw new InvalidOperationException("Thành viên đã từ chối lời mời — không thể xác nhận thay.");

        member.Status = CouncilMemberStatus.Confirmed;
        member.ConfirmedAt = DateTime.UtcNow;
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

    // Xóa hội đồng — chỉ khi CHƯA có việc chấm (không phiếu/biên bản/nghiệm thu); con NoAction nên gỡ tay.
    public async Task DeleteCouncilAsync(Guid councilId)
    {
        var council = await _review.Query().FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException($"Council {councilId} not found.");

        if (await _review.ReviewScores.AnyAsync(s => s.CouncilId == councilId))
            throw new InvalidOperationException("Hội đồng đã có phiếu chấm — không thể xóa.");
        if (await _review.Decisions.AnyAsync(d => d.CouncilId == councilId))
            throw new InvalidOperationException("Hội đồng đã có biên bản — không thể xóa.");
        if (await _review.AcceptanceEvaluations.AnyAsync(a => a.CouncilId == councilId))
            throw new InvalidOperationException("Hội đồng đã có đánh giá nghiệm thu — không thể xóa.");

        var meetings = await _review.Meetings.Where(m => m.CouncilId == councilId).ToListAsync();
        var meetingIds = meetings.Select(m => m.Id).ToList();
        var attendances = await _review.MeetingAttendances.Where(a => meetingIds.Contains(a.MeetingId)).ToListAsync();
        var members = await _review.CouncilMembers.Where(m => m.CouncilId == councilId).ToListAsync();
        var assignments = await _review.ProjectAssignments.Where(a => a.CouncilId == councilId).ToListAsync();

        _review.RemoveAttendancesRange(attendances);
        _review.RemoveMeetingsRange(meetings);
        _review.RemoveMembersRange(members);
        _review.RemoveProjectAssignmentsRange(assignments);
        _review.Remove(council);
        await _review.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(Guid memberId)
    {
        var member = await _review.CouncilMembers
            .FirstOrDefaultAsync(m => m.Id == memberId)
            ?? throw new KeyNotFoundException($"Council member {memberId} not found.");

        _review.RemoveMember(member);
        await _review.SaveChangesAsync();
    }

    // Gán 1 đề tài vào hội đồng có sẵn (dropdown ở màn Hội đồng & Chấm).
    // Mỗi đề tài ↔ 1 hội đồng trong CÙNG vòng → gỡ khỏi hội đồng khác của vòng trước (nếu chưa chấm), rồi gán.
    public async Task AssignProjectToCouncilAsync(Guid councilId, Guid projectId)
    {
        var council = await _review.Query()
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException($"Council {councilId} not found.");

        if (council.RoundId == null)
            throw new InvalidOperationException("Hội đồng không gắn với vòng chấm nào.");

        // Đề tài phải đang THAM GIA vòng của hội đồng này (project_round).
        var joined = await _review.ProjectRounds
            .AnyAsync(pr => pr.RoundId == council.RoundId.Value && pr.ProjectId == projectId);
        if (!joined)
            throw new ArgumentException("Đề tài chưa tham gia vòng chấm của hội đồng này.");

        // COI (rule #5): thành viên hội đồng không được là PI/thành viên của đề tài.
        var memberUserIds = council.Members.Select(m => m.UserId).ToList();
        await ReviewShared.AssertNoCoiAsync(_proposals, new[] { projectId }, memberUserIds);

        // Gỡ khỏi các hội đồng KHÁC của cùng vòng (đảm bảo 1 đề tài chỉ 1 hội đồng/vòng).
        var otherCouncilIds = await _review.Query()
            .Where(c => c.RoundId == council.RoundId.Value && c.Id != councilId)
            .Select(c => c.Id)
            .ToListAsync();
        if (otherCouncilIds.Count > 0)
        {
            var scoredElsewhere = await _review.ReviewScores
                .AnyAsync(s => s.ProjectId == projectId && otherCouncilIds.Contains(s.CouncilId));
            if (scoredElsewhere)
                throw new InvalidOperationException("Đề tài đã được chấm ở hội đồng khác trong vòng này — không thể chuyển.");

            var oldAssignments = await _review.ProjectAssignments
                .Where(a => a.ProjectId == projectId && otherCouncilIds.Contains(a.CouncilId))
                .ToListAsync();
            if (oldAssignments.Count > 0)
                _review.RemoveProjectAssignmentsRange(oldAssignments);
        }

        var already = await _review.ProjectAssignments
            .AnyAsync(a => a.CouncilId == councilId && a.ProjectId == projectId);
        if (!already)
            await _review.AddProjectAssignmentAsync(new CouncilProjectAssignment { CouncilId = councilId, ProjectId = projectId });

        await _review.SaveChangesAsync();
    }

    public async Task RemoveProjectFromCouncilAsync(Guid councilId, Guid projectId)
    {
        var assignment = await _review.ProjectAssignments
            .FirstOrDefaultAsync(a => a.CouncilId == councilId && a.ProjectId == projectId)
            ?? throw new KeyNotFoundException("Đề tài không được gán cho hội đồng này.");

        var scored = await _review.ReviewScores.AnyAsync(s => s.CouncilId == councilId && s.ProjectId == projectId);
        if (scored)
            throw new InvalidOperationException("Đề tài đã được chấm ở hội đồng này — không thể gỡ.");

        _review.RemoveProjectAssignmentsRange(new[] { assignment });
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

    public async Task<IEnumerable<ScheduleConflictDto>> GetScheduleConflictsAsync(Guid councilId)
    {
        // Khoảng thời gian họp của chính hội đồng này (chưa họp thì không có gì để đối chiếu).
        var myMeetings = await _review.Meetings
            .Where(m => m.CouncilId == councilId)
            .Select(m => new { m.ScheduledAt, m.DurationMinutes })
            .ToListAsync();
        if (myMeetings.Count == 0)
            return Enumerable.Empty<ScheduleConflictDto>();

        // Thành viên của hội đồng này (userId + tên).
        var myMembers = await _review.CouncilMembers
            .Where(cm => cm.CouncilId == councilId)
            .Select(cm => new { cm.UserId, Name = cm.User.FullName })
            .ToListAsync();
        var myUserIds = myMembers.Select(m => m.UserId).ToHashSet();
        if (myUserIds.Count == 0)
            return Enumerable.Empty<ScheduleConflictDto>();

        // Lịch họp của các hội đồng KHÁC kèm danh sách userId thành viên của hội đồng đó.
        var otherMeetings = await _review.Meetings
            .Where(m => m.CouncilId != councilId)
            .Select(m => new
            {
                m.CouncilId,
                m.ScheduledAt,
                m.DurationMinutes,
                CouncilType = m.Council.CouncilType,
                MemberUserIds = m.Council.Members.Select(x => x.UserId).ToList()
            })
            .ToListAsync();

        static bool Overlap(DateTime a, int da, DateTime b, int db) =>
            a < b.AddMinutes(db) && b < a.AddMinutes(da);

        var conflicts = new List<ScheduleConflictDto>();
        foreach (var om in otherMeetings)
        {
            var sharedUserIds = om.MemberUserIds.Where(myUserIds.Contains).Distinct();
            foreach (var uid in sharedUserIds)
            {
                var clash = myMeetings.FirstOrDefault(mm =>
                    Overlap(mm.ScheduledAt, mm.DurationMinutes, om.ScheduledAt, om.DurationMinutes));
                if (clash == null) continue;
                conflicts.Add(new ScheduleConflictDto
                {
                    MemberUserId = uid,
                    MemberName = myMembers.First(m => m.UserId == uid).Name,
                    OtherCouncilId = om.CouncilId,
                    OtherCouncilType = om.CouncilType,
                    ThisMeetingAt = clash.ScheduledAt,
                    OtherMeetingAt = om.ScheduledAt
                });
            }
        }
        return conflicts;
    }

    // ── Slot theo đề tài (rule tuần 10) ──────────────────────────────────────
    /// <summary>
    /// Trả kèm KHUNG GIỜ buổi họp + tổng phút đã xếp, để màn lịch chấm hiện "đã xếp 60/90 phút".
    /// Trước đây chỉ trả danh sách slot: Staff phải tự cộng nhẩm, gán thêm đề tài vào hội đồng
    /// cũng chẳng ai nhắc là buổi họp có còn đủ giờ hay không.
    /// </summary>
    public async Task<CouncilSlotBoardDto> GetCouncilSlotBoardAsync(Guid councilId)
    {
        var slots = (await GetCouncilSlotsAsync(councilId)).ToList();
        var meeting = await _review.Meetings
            .Where(m => m.CouncilId == councilId)
            .OrderBy(m => m.ScheduledAt)
            .FirstOrDefaultAsync();

        return new CouncilSlotBoardDto
        {
            MeetingId = meeting?.Id,
            MeetingStartAt = meeting?.ScheduledAt,
            MeetingDurationMinutes = meeting?.DurationMinutes,
            AssignedMinutes = slots.Sum(x => x.SlotDurationMinutes ?? 0),
            Slots = slots
        };
    }

    public async Task<IEnumerable<CouncilSlotDto>> GetCouncilSlotsAsync(Guid councilId)
    {
        var council = await _review.Query()
            .Include(c => c.ProjectAssignments).ThenInclude(a => a.Project)
            .FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException($"Council {councilId} not found.");

        return council.ProjectAssignments
            .OrderBy(a => a.SlotOrder ?? int.MaxValue)
            .ThenBy(a => a.SlotStartAt ?? DateTime.MaxValue)
            .Select(a => new CouncilSlotDto
            {
                ProjectId = a.ProjectId,
                ProjectTitle = a.Project.TitleVi,
                MeetingId = a.MeetingId,
                SlotStartAt = a.SlotStartAt,
                SlotDurationMinutes = a.SlotDurationMinutes,
                SlotOrder = a.SlotOrder
            })
            .ToList();
    }

    public async Task SaveCouncilSlotsAsync(Guid councilId, SaveSlotsRequest request)
    {
        var council = await _review.Query()
            .Include(c => c.ProjectAssignments)
            .Include(c => c.Meetings)
            .FirstOrDefaultAsync(c => c.Id == councilId)
            ?? throw new KeyNotFoundException($"Council {councilId} not found.");

        // Slot con thuộc buổi họp sớm nhất của hội đồng (thường 1 buổi cho vòng xét duyệt).
        var meeting = council.Meetings.OrderBy(m => m.ScheduledAt).FirstOrDefault();
        if (meeting == null)
            throw new InvalidOperationException(
                "Hội đồng chưa có buổi họp nào — hãy đặt lịch họp trước rồi mới chia khung giờ cho từng đề tài.");

        /*
         * Khung giờ chấm từng đề tài phải nằm TRONG buổi họp (rule tuần 10 #17, thầy nhắc lại
         * 05/08). Trước đây lưu nguyên xi mọi giá trị: đặt slot 7h sáng cho buổi họp 14h chiều,
         * hay slot dài 3 tiếng trong buổi họp 2 tiếng, đều lọt — lịch in ra vô nghĩa.
         */
        var windowStart = meeting.ScheduledAt;
        var windowEnd = meeting.ScheduledAt.AddMinutes(meeting.DurationMinutes);

        var byProject = council.ProjectAssignments.ToDictionary(a => a.ProjectId);
        var placed = new List<(DateTime Start, DateTime End, Guid ProjectId)>();

        foreach (var e in request.Entries)
        {
            if (!byProject.TryGetValue(e.ProjectId, out var a)) continue;

            if (e.SlotStartAt.HasValue)
            {
                var duration = e.SlotDurationMinutes ?? 0;
                if (duration <= 0)
                    throw new ArgumentException("Khung giờ chấm phải có thời lượng lớn hơn 0 phút.");

                var start = e.SlotStartAt.Value;
                var end = start.AddMinutes(duration);

                if (start < windowStart || end > windowEnd)
                    throw new ArgumentException(
                        $"Khung giờ chấm ({start:dd/MM HH:mm}–{end:HH:mm}) nằm ngoài buổi họp " +
                        $"({windowStart:dd/MM HH:mm}–{windowEnd:HH:mm}). " +
                        "Sửa lại khung giờ hoặc kéo dài buổi họp.");

                // Hội đồng không thể chấm 2 đề tài cùng một lúc.
                var clash = placed.FirstOrDefault(p => start < p.End && p.Start < end);
                if (clash != default)
                    throw new ArgumentException(
                        $"Khung giờ chấm ({start:HH:mm}–{end:HH:mm}) chồng lên khung của đề tài khác " +
                        $"({clash.Start:HH:mm}–{clash.End:HH:mm}) — hội đồng chỉ chấm được một đề tài tại một thời điểm.");
                placed.Add((start, end, e.ProjectId));
            }

            a.MeetingId = meeting.Id;
            a.SlotStartAt = e.SlotStartAt;
            a.SlotDurationMinutes = e.SlotDurationMinutes;
            a.SlotOrder = e.SlotOrder;
        }
        await _review.SaveChangesAsync();
    }
}
