using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Councils;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

/// <inheritdoc cref="ICouncilCandidateService"/>
public class CouncilCandidateService : ICouncilCandidateService
{
    /// <summary>Ai đủ tư cách đứng tên hội đồng — cùng luật với bộ lọc bên giao diện.</summary>
    private static readonly string[] EligibleRoles = { "Faculty", "ReviewCommittee" };

    private readonly FURPMSDbContext _db;

    public CouncilCandidateService(FURPMSDbContext db)
    {
        _db = db;
    }

    public async Task<CouncilCandidatesResponse> GetCandidatesAsync(
        Guid? projectId, int? trackId, Guid? councilId)
    {
        // Lĩnh vực suy từ đề tài nếu có; không thì lấy tham số truyền vào.
        var resolvedTrackId = trackId;
        var piUserIds = new List<Guid>();
        var teamUserIds = new List<Guid>();

        // Gọi từ màn "Thêm ủy viên" thì chỉ biết mỗi hội đồng. Suy ngược ra các đề tài hội đồng
        // đang chấm — không thì hộp thoại đó mất sạch cả xếp hạng chuyên môn lẫn cờ xung đột lợi
        // ích, đúng hai thứ nó cần nhất.
        var projectIds = new List<Guid>();
        if (projectId is { } explicitPid) projectIds.Add(explicitPid);
        else if (councilId is { } cidForProjects)
            projectIds = await _db.CouncilProjectAssignments
                .Where(a => a.CouncilId == cidForProjects)
                .Select(a => a.ProjectId)
                .ToListAsync();

        foreach (var pid in projectIds)
        {
            var info = await _db.Projects
                .IgnoreQueryFilters()
                .Where(p => p.Id == pid)
                .Select(p => new { p.PiUserId, TrackId = (int?)p.CycleTrack.TrackId })
                .FirstOrDefaultAsync();

            if (info == null) continue;

            resolvedTrackId ??= info.TrackId;
            piUserIds.Add(info.PiUserId);
            teamUserIds.AddRange(await _db.ProjectMembers
                .Where(m => m.ProjectId == pid && m.UserId != null)
                .Select(m => m.UserId!.Value)
                .ToListAsync());
        }

        var trackName = resolvedTrackId is { } tid
            ? await _db.ResearchTracks.Where(t => t.Id == tid).Select(t => t.Name).FirstOrDefaultAsync()
            : null;

        var eligibleUserIds = await _db.UserRoles
            .Where(ur => EligibleRoles.Contains(ur.Role.Name))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync();

        var users = await _db.Users
            .Where(u => eligibleUserIds.Contains(u.Id) && u.Status == UserStatus.Active)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                AcademicTitle = _db.AcademicProfiles
                    .Where(a => a.UserId == u.Id).Select(a => a.AcademicTitle).FirstOrDefault(),
                UnitName = _db.OrganizationalUnits
                    .Where(o => o.Id == u.UnitId).Select(o => o.Name).FirstOrDefault()
            })
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();

        // Lĩnh vực đã khai của từng người — một truy vấn cho cả danh sách.
        var tracksByUser = (await _db.UserResearchTracks
                .Where(x => userIds.Contains(x.UserId))
                .Select(x => new { x.UserId, x.TrackId, TrackName = x.Track.Name })
                .ToListAsync())
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var inCouncil = councilId is { } cid
            ? (await _db.CouncilMembers.Where(m => m.CouncilId == cid).Select(m => m.UserId).ToListAsync())
                .ToHashSet()
            : new HashSet<Guid>();

        // Đang bận ở bao nhiêu hội đồng chưa chốt — để Phòng QLKH không dồn hết vào một người.
        var busyCounts = (await _db.CouncilMembers
                .Where(m => userIds.Contains(m.UserId) && m.Council.Status != CouncilStatus.Decided)
                .Select(m => m.UserId)
                .ToListAsync())
            .GroupBy(x => x)
            .ToDictionary(g => g.Key, g => g.Count());

        var coi = piUserIds.Concat(teamUserIds).ToHashSet();

        var list = users.Select(u =>
        {
            var myTracks = tracksByUser.GetValueOrDefault(u.Id) ?? new();
            return new CouncilCandidateDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                AcademicTitle = u.AcademicTitle,
                UnitName = u.UnitName,
                Tracks = myTracks.Select(t => t.TrackName).ToList(),
                MatchesTrack = resolvedTrackId is { } rt && myTracks.Any(t => t.TrackId == rt),
                ExpertiseUnknown = myTracks.Count == 0,
                HasConflictOfInterest = coi.Contains(u.Id),
                AlreadyInCouncil = inCouncil.Contains(u.Id),
                ActiveCouncilCount = busyCounts.GetValueOrDefault(u.Id)
            };
        }).ToList();

        // Thứ tự là phần trả lời, không phải trang trí.
        //
        // CHỌN ĐƯỢC HAY KHÔNG xếp trước cả chuyên môn: người vướng xung đột lợi ích hoặc đã có tên
        // trong hội đồng thì bấm vào cũng không được, nên dù họ đúng ngành cũng không được chiếm
        // mấy dòng đầu — đó là chỗ đắt nhất của danh sách. (Chạy thử 26/08 trên dữ liệu demo lộ
        // đúng lỗi này: ba người đúng ngành nhưng đã ở trong hội đồng đẩy hết người chọn được
        // xuống dưới.) Họ VẪN hiện, chỉ nằm cuối — giấu đi thì Phòng QLKH không hiểu vì sao tìm
        // mãi không thấy một cái tên.
        var ranked = list
            .OrderBy(c => c.HasConflictOfInterest || c.AlreadyInCouncil)
            .ThenByDescending(c => c.MatchesTrack)
            .ThenBy(c => c.ExpertiseUnknown)
            .ThenBy(c => c.ActiveCouncilCount)
            .ThenBy(c => c.FullName)
            .ToList();

        return new CouncilCandidatesResponse
        {
            TrackId = resolvedTrackId,
            TrackName = trackName,
            MatchingCount = ranked.Count(c => c.MatchesTrack),
            TotalCount = ranked.Count,
            Candidates = ranked
        };
    }
}
