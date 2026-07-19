using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Progress;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Progress;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class ProgressReportService : IProgressReportService
{
    private readonly IContractRepository _contracts;
    private readonly IProposalRepository _proposals;

    public ProgressReportService(IContractRepository contracts, IProposalRepository proposals)
    {
        _contracts = contracts;
        _proposals = proposals;
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
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        if (contract.Project.PiUserId != userId)
            throw new UnauthorizedAccessException("Only the PI of this contract may create progress reports.");

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
            CompletedContent = request.CompletedContent,
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

    public async Task<ProgressReportDto> SubmitAsync(Guid reportId, Guid userId)
    {
        var report = await _contracts.ProgressReports
            .Include(r => r.Contract).ThenInclude(c => c.Project)
            .FirstOrDefaultAsync(r => r.Id == reportId)
            ?? throw new KeyNotFoundException($"Progress report {reportId} not found.");

        if (report.Contract.Project.PiUserId != userId)
            throw new UnauthorizedAccessException("Only the PI may submit this report.");

        if (report.Status != ProgressReportStatus.Draft)
            throw new InvalidOperationException($"Report is '{report.Status}'; only DRAFT reports can be submitted.");

        report.Status = ProgressReportStatus.Submitted;
        report.SubmittedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;
        await _contracts.SaveChangesAsync();

        return await GetByIdAsync(reportId);
    }

    public async Task<ProgressReportDto> EvaluateAsync(Guid reportId, EvaluateProgressReportRequest request, Guid staffId)
    {
        var validResults = new[] { "SATISFACTORY", "UNSATISFACTORY", "NEEDS_IMPROVEMENT" };
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
        report.UpdatedAt = DateTime.UtcNow;
        await _contracts.SaveChangesAsync();

        return await GetByIdAsync(reportId);
    }

    private static ProgressReportSummaryDto MapSummary(ProgressReport r) => new()
    {
        Id = r.Id,
        ContractId = r.ContractId,
        ReportRound = r.ReportRound,
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
