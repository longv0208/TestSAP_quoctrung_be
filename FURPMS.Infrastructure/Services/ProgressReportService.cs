using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Progress;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Progress;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class ProgressReportService : IProgressReportService
{
    private readonly IContractRepository _contracts;
    private readonly IProposalRepository _proposals;
    // Dùng IClock (không phải DateTime.UtcNow) để test tua được thời gian — các service khác
    // trong dự án đã theo quy ước này.
    private readonly IClock _clock;

    public ProgressReportService(IContractRepository contracts, IProposalRepository proposals, IClock clock)
    {
        _contracts = contracts;
        _proposals = proposals;
        _clock = clock;
    }

    // QĐ543 Điều 10.1: Ứng dụng 2 kỳ, Cơ bản 1 kỳ. Applied = loại có đặt hàng (RequireOrderingUnit).
    private static int RequiredRounds(FURPMS.Domain.Entities.MasterData.ResearchType? type) =>
        type?.RequireOrderingUnit == true ? 2 : 1;

    // Staff sinh sẵn các kỳ báo cáo — chia đều theo mốc hợp đồng; bỏ qua kỳ đã có.
    // roundCount: Staff tự chọn số kỳ; bỏ trống → mặc định theo loại (Ứng dụng 2 / Cơ bản 1).
    public async Task<IEnumerable<ProgressReportSummaryDto>> GenerateScheduledRoundsAsync(Guid contractId, int? roundCount = null)
    {
        var contract = await _contracts.Query()
            .Include(c => c.Project).ThenInclude(p => p.ResearchType)
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        if (roundCount is < 1 or > 12)
            throw new ArgumentException("Số kỳ báo cáo phải từ 1 đến 12.");

        var required = roundCount ?? RequiredRounds(contract.Project.ResearchType);
        var existing = await _contracts.ProgressReports
            .Where(r => r.ContractId == contractId)
            .Select(r => r.ReportRound)
            .ToListAsync();

        var start = contract.StartDate;
        var end = contract.EndDate > start ? contract.EndDate : start.AddMonths(6);
        var totalDays = end.DayNumber - start.DayNumber;
        var chunk = required > 0 ? totalDays / required : totalDays;

        for (int round = 1; round <= required; round++)
        {
            if (existing.Contains(round)) continue;
            var pStart = start.AddDays(chunk * (round - 1));
            var pEnd = round == required ? end : start.AddDays(chunk * round);
            await _contracts.AddProgressReportAsync(new ProgressReport
            {
                ContractId = contractId,
                ReportRound = round,
                ReportingPeriodStart = pStart,
                ReportingPeriodEnd = pEnd,
                DueDate = pEnd,
                CompletedContent = "",   // NOT NULL — PI điền sau; để rỗng cho kỳ vừa mở
                Status = ProgressReportStatus.Draft
            });
        }
        await _contracts.SaveChangesAsync();
        return await GetByContractAsync(contractId);
    }

    public async Task<IEnumerable<ProgressReportSummaryDto>> GetByContractAsync(Guid contractId)
    {
        _ = await _contracts.Query().FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        var reports = await _contracts.ProgressReports
            .Where(r => r.ContractId == contractId)
            .OrderBy(r => r.ReportRound)
            .ToListAsync();

        return reports.Select(MapSummary);
    }

    public async Task<ProgressReportDto> GetByIdAsync(Guid reportId)
    {
        var report = await _contracts.ProgressReports
            .Include(r => r.Items).ThenInclude(i => i.Activity)
            .FirstOrDefaultAsync(r => r.Id == reportId)
            ?? throw new KeyNotFoundException($"Progress report {reportId} not found.");

        return MapDetail(report);
    }

    public async Task<ProgressReportDto> CreateAsync(Guid contractId, CreateProgressReportRequest request, Guid userId)
    {
        var contract = await _contracts.Query()
            .Include(c => c.Project).ThenInclude(p => p.Proposals.Where(x => x.IsCurrent))
            .Include(c => c.Project).ThenInclude(p => p.ResearchType)
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        if (contract.Project.PiUserId != userId)
            throw new ForbiddenException("Only the PI of this contract may create progress reports.");

        // QĐ543 Điều 10.1 gợi ý Ứng dụng 2 / Cơ bản 1 kỳ, NHƯNG không chặn cứng (thầy 29/07:
        // Staff phải chỉnh được số lần) — chỉ dùng làm mặc định khi sinh kỳ tự động.

        if (!DateOnly.TryParse(request.ReportingPeriodStart, out var periodStart))
            throw new ArgumentException("ReportingPeriodStart must be a valid date (yyyy-MM-dd).");
        if (!DateOnly.TryParse(request.ReportingPeriodEnd, out var periodEnd))
            throw new ArgumentException("ReportingPeriodEnd must be a valid date (yyyy-MM-dd).");
        if (periodEnd <= periodStart)
            throw new ArgumentException("ReportingPeriodEnd must be after ReportingPeriodStart.");
        if (request.OverallCompletionPct is < 0 or > 100)
            throw new ArgumentException("OverallCompletionPct must be between 0 and 100.");

        var nextRound = await _contracts.ProgressReports
            .Where(r => r.ContractId == contractId)
            .CountAsync() + 1;

        var report = new ProgressReport
        {
            ContractId = contractId,
            ReportRound = nextRound,
            ReportingPeriodStart = periodStart,
            ReportingPeriodEnd = periodEnd,
            CompletedContent = request.CompletedContent ?? "",   // NOT NULL
            PendingContent = request.PendingContent,
            OverallCompletionPct = request.OverallCompletionPct,
            ExpenditureToDate = request.ExpenditureToDate,
            NextPeriodPlan = request.NextPeriodPlan,
            PiRecommendations = request.PiRecommendations,
            Status = ProgressReportStatus.Draft
        };

        await _contracts.AddProgressReportAsync(report);
        await _contracts.SaveChangesAsync();

        if (request.Items.Count > 0)
        {
            var activityIds = request.Items.Select(i => i.ActivityId).ToHashSet();
            var activities = await _proposals.Activities
                .Where(a => a.Proposal.ProjectId == contract.ProjectId && activityIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id);

            _contracts.AddProgressReportItemsRange(request.Items.Select(i =>
            {
                if (!activities.ContainsKey(i.ActivityId))
                    throw new KeyNotFoundException($"Activity {i.ActivityId} not found for this proposal.");
                return new ProgressReportItem
                {
                    ReportId = report.Id,
                    ActivityId = i.ActivityId,
                    CompletionRate = i.CompletionRate,
                    CompletionStatus = i.CompletionStatus,
                    EvidenceDescription = i.EvidenceDescription,
                    Notes = i.Notes
                };
            }));
            await _contracts.SaveChangesAsync();
        }

        return await GetByIdAsync(report.Id);
    }

    public async Task<ProgressReportDto> UpdateAsync(Guid reportId, UpdateProgressReportRequest request, Guid userId)
    {
        var report = await _contracts.ProgressReports
            .Include(r => r.Contract).ThenInclude(c => c.Project)
            .FirstOrDefaultAsync(r => r.Id == reportId)
            ?? throw new KeyNotFoundException($"Progress report {reportId} not found.");

        if (report.Contract.Project.PiUserId != userId)
            throw new ForbiddenException("Only the PI may edit this report.");

        // Cho sửa đến khi Staff ĐÃ ĐÁNH GIÁ, không phải khoá ngay lúc nộp.
        // Khoá ngay lúc nộp là bất nhất với sản phẩm (cho nộp lại tới khi ĐẠT) và báo cáo
        // tổng kết (cho nộp lại + yêu cầu chỉnh sửa) — PI lỡ sai một chữ là kẹt luôn.
        if (!string.IsNullOrWhiteSpace(report.EvaluationResult))
            throw new InvalidOperationException(
                "Kỳ báo cáo này đã được đánh giá — không sửa được nữa. Liên hệ phòng QLKH nếu cần điều chỉnh.");

        if (request.OverallCompletionPct is < 0 or > 100)
            throw new ArgumentException("OverallCompletionPct must be between 0 and 100.");

        report.CompletedContent = request.CompletedContent ?? "";   // NOT NULL
        report.PendingContent = request.PendingContent;
        report.OverallCompletionPct = request.OverallCompletionPct;
        report.ExpenditureToDate = request.ExpenditureToDate;
        report.NextPeriodPlan = request.NextPeriodPlan;
        report.PiRecommendations = request.PiRecommendations;
        // Bỏ trống thì GIỮ link cũ — nộp lại mà không dán lại link không được mất bản cũ.
        if (!string.IsNullOrWhiteSpace(request.ReportFileUrl))
            report.ReportFileUrl = request.ReportFileUrl.Trim();

        // Bảng tiến độ theo hoạt động (BM06): gửi lên thì THAY toàn bộ bảng cũ.
        if (request.Items != null)
        {
            var old = await _contracts.ProgressReportItems.Where(i => i.ReportId == report.Id).ToListAsync();
            _contracts.RemoveProgressReportItemsRange(old);
            _contracts.AddProgressReportItemsRange(request.Items.Select(i => new ProgressReportItem
            {
                ReportId = report.Id,
                ActivityId = i.ActivityId,
                CompletionRate = i.CompletionRate,
                CompletionStatus = string.IsNullOrWhiteSpace(i.CompletionStatus) ? "IN_PROGRESS" : i.CompletionStatus,
                EvidenceDescription = i.EvidenceDescription,
                Notes = i.Notes
            }));
        }

        await _contracts.SaveChangesAsync();
        return await GetByIdAsync(report.Id);
    }

    public async Task<ProgressReportDto> SubmitAsync(Guid reportId, Guid userId)
    {
        var report = await _contracts.ProgressReports
            .Include(r => r.Contract).ThenInclude(c => c.Project)
            .FirstOrDefaultAsync(r => r.Id == reportId)
            ?? throw new KeyNotFoundException($"Progress report {reportId} not found.");

        if (report.Contract.Project.PiUserId != userId)
            throw new ForbiddenException("Only the PI may submit this report.");

        if (report.Status != ProgressReportStatus.Draft)
            throw new InvalidOperationException($"Report is '{report.Status}'; only DRAFT reports can be submitted.");

        /*
         * QĐ543 Điều 10.1: báo cáo tiến độ là báo cáo **ĐỊNH KỲ** — kỳ sau chỉ có nghĩa khi kỳ
         * trước đã chốt. Thầy 05/08: *"nộp báo cáo phải được duyệt xong phải qua 1 thời gian mới
         * cho nộp lần 2, hiện tại đang có thể nộp 2 lần cùng lúc"*.
         *
         * Trước đây `SubmitAsync` chỉ kiểm DRAFT ⇒ PI nộp kỳ 1 xong **nộp dồn luôn kỳ 2, kỳ 3**
         * dù kỳ trước Staff chưa đánh giá và kỳ sau còn chưa tới ngày bắt đầu.
         *
         * Chặn theo KỲ chứ không theo số ngày — QĐ543 không quy định khoảng cách ngày nào cả.
         */
        var earlier = await _contracts.ProgressReports
            .Where(r => r.ContractId == report.ContractId && r.ReportRound < report.ReportRound)
            .OrderByDescending(r => r.ReportRound)
            .FirstOrDefaultAsync();

        if (earlier != null && string.IsNullOrWhiteSpace(earlier.EvaluationResult))
        {
            var name = earlier.RoundName ?? $"Kỳ {earlier.ReportRound}";
            throw new InvalidOperationException(
                earlier.SubmittedAt == null
                    ? $"Chưa nộp {name} — phải nộp và được đánh giá xong kỳ trước rồi mới nộp kỳ này."
                    : $"{name} đã nộp nhưng phòng QLKH chưa đánh giá — chờ có kết quả kỳ trước rồi mới nộp kỳ này.");
        }

        // Chưa tới kỳ thì cũng chưa có gì để báo cáo.
        var today = DateOnly.FromDateTime(_clock.UtcNow);
        if (report.ReportingPeriodStart > today)
            throw new InvalidOperationException(
                $"Kỳ báo cáo này bắt đầu từ {report.ReportingPeriodStart:dd/MM/yyyy} — chưa tới kỳ, chưa nộp được.");

        report.Status = ProgressReportStatus.Submitted;
        report.SubmittedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;
        await _contracts.SaveChangesAsync();

        return await GetByIdAsync(reportId);
    }

    public async Task<bool> IsPiOfReportAsync(Guid reportId, Guid userId)
    {
        return await _contracts.ProgressReports
            .Include(r => r.Contract).ThenInclude(c => c.Project)
            .AnyAsync(r => r.Id == reportId && r.Contract.Project.PiUserId == userId);
    }

    public async Task<ProgressReportDto> EvaluateAsync(Guid reportId, EvaluateProgressReportRequest request, Guid staffId)
    {
        // QĐ543 Điều 10 / BM06: kết quả đánh giá tiến độ = Đạt / Không đạt / Có điều kiện.
        // (Trước đây BE nhận SATISFACTORY/… còn FE gửi APPROVED/… → Staff bấm đánh giá luôn 400.)
        var validResults = new[] { "PASS", "FAIL", "CONDITIONAL" };
        if (!validResults.Contains(request.EvaluationResult))
            throw new ArgumentException($"EvaluationResult must be one of: {string.Join(", ", validResults)}.");

        var report = await _contracts.ProgressReports
            .FirstOrDefaultAsync(r => r.Id == reportId)
            ?? throw new KeyNotFoundException($"Progress report {reportId} not found.");

        if (report.Status != ProgressReportStatus.Submitted)
            throw new InvalidOperationException($"Report is '{report.Status}'; only SUBMITTED reports can be evaluated.");

        report.Status = ProgressReportStatus.Evaluated;
        report.EvaluatedBy = staffId;
        report.EvaluatedAt = DateTime.UtcNow;
        report.EvaluationResult = request.EvaluationResult;
        report.EvaluationComments = request.EvaluationComments;
        report.UpdatedAt = DateTime.UtcNow;
        await _contracts.SaveChangesAsync();

        return await GetByIdAsync(reportId);
    }

    public async Task<ProgressReportDto> ScheduleAsync(Guid reportId, ScheduleProgressReportRequest request)
    {
        var report = await _contracts.ProgressReports.FirstOrDefaultAsync(r => r.Id == reportId)
            ?? throw new KeyNotFoundException($"Progress report {reportId} not found.");

        if (!string.IsNullOrWhiteSpace(request.DueDate))
        {
            if (!DateOnly.TryParse(request.DueDate, out var due))
                throw new ArgumentException("DueDate phải là ngày hợp lệ (yyyy-MM-dd).");
            report.DueDate = due;
        }
        if (!string.IsNullOrWhiteSpace(request.ScheduledMeetingAt))
        {
            if (!DateTime.TryParse(request.ScheduledMeetingAt, out var at))
                throw new ArgumentException("ScheduledMeetingAt phải là thời điểm hợp lệ.");
            report.ScheduledMeetingAt = at.ToUniversalTime();
        }
        report.MeetingLink = request.MeetingLink;
        if (request.RoundName != null)
            report.RoundName = string.IsNullOrWhiteSpace(request.RoundName) ? null : request.RoundName.Trim();
        report.UpdatedAt = DateTime.UtcNow;
        await _contracts.SaveChangesAsync();

        return await GetByIdAsync(reportId);
    }

    private static ProgressReportSummaryDto MapSummary(ProgressReport r) => new()
    {
        Id = r.Id,
        ContractId = r.ContractId,
        ReportRound = r.ReportRound,
        RoundName = r.RoundName,
        ReportFileUrl = r.ReportFileUrl,
        ReportingPeriodStart = r.ReportingPeriodStart.ToString("yyyy-MM-dd"),
        ReportingPeriodEnd = r.ReportingPeriodEnd.ToString("yyyy-MM-dd"),
        OverallCompletionPct = r.OverallCompletionPct,
        ExpenditureToDate = r.ExpenditureToDate,
        Status = r.Status,
        SubmittedAt = r.SubmittedAt,
        CreatedAt = r.CreatedAt,
        DueDate = r.DueDate?.ToString("yyyy-MM-dd"),
        ScheduledMeetingAt = r.ScheduledMeetingAt,
        MeetingLink = r.MeetingLink
    };

    private static ProgressReportDto MapDetail(ProgressReport r) => new()
    {
        Id = r.Id,
        ContractId = r.ContractId,
        ReportRound = r.ReportRound,
        RoundName = r.RoundName,
        // Phải map ở CẢ HAI mapper. Thiếu ở đây thì danh sách trả link mà chi tiết trả null —
        // dialog đánh giá của Staff đọc chi tiết nên báo "PI chưa nộp file" và khoá nút lưu,
        // dù PI đã nộp link hẳn hoi.
        ReportFileUrl = r.ReportFileUrl,
        ReportingPeriodStart = r.ReportingPeriodStart.ToString("yyyy-MM-dd"),
        ReportingPeriodEnd = r.ReportingPeriodEnd.ToString("yyyy-MM-dd"),
        CompletedContent = r.CompletedContent,
        PendingContent = r.PendingContent,
        OverallCompletionPct = r.OverallCompletionPct,
        ExpenditureToDate = r.ExpenditureToDate,
        NextPeriodPlan = r.NextPeriodPlan,
        PiRecommendations = r.PiRecommendations,
        Status = r.Status,
        SubmittedAt = r.SubmittedAt,
        EvaluationResult = r.EvaluationResult,
        EvaluationComments = r.EvaluationComments,
        EvaluatedAt = r.EvaluatedAt,
        CreatedAt = r.CreatedAt,
        DueDate = r.DueDate?.ToString("yyyy-MM-dd"),
        ScheduledMeetingAt = r.ScheduledMeetingAt,
        MeetingLink = r.MeetingLink,
        Items = r.Items.Select(i => new ProgressReportItemDto
        {
            Id = i.Id,
            ActivityId = i.ActivityId,
            ActivityName = i.Activity?.ActivityName ?? "—",
            CompletionRate = i.CompletionRate,
            CompletionStatus = i.CompletionStatus,
            EvidenceDescription = i.EvidenceDescription,
            Notes = i.Notes
        }).ToList()
    };
}
