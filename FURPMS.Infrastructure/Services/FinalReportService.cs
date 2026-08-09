using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Progress;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Progress;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class FinalReportService : IFinalReportService
{
    private readonly IContractRepository _contracts;
    private readonly IClock _clock;

    public FinalReportService(IContractRepository contracts,
        IClock clock)
    {
        _contracts = contracts;
        _clock = clock;
    }

    public async Task<FinalReportDto?> GetByContractAsync(Guid contractId)
    {
        var contract = await _contracts.Query()
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");
        var report = await _contracts.FinalReports
            .FirstOrDefaultAsync(r => r.ProjectId == contract.ProjectId);
        return report == null ? null : Map(report);
    }

    public async Task<FinalReportDto> SubmitAsync(Guid contractId, SubmitFinalReportRequest request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.ReportFileUrl))
            throw new ArgumentException("Phải nộp file báo cáo tổng kết.");

        var contract = await _contracts.Query()
            .Include(c => c.Project)
            .FirstOrDefaultAsync(c => c.Id == contractId)
            ?? throw new KeyNotFoundException($"Contract {contractId} not found.");

        if (contract.Project.PiUserId != userId)
            throw new ForbiddenException("Chỉ chủ nhiệm đề tài mới nộp được báo cáo tổng kết.");

        var existing = await _contracts.FinalReports
            .FirstOrDefaultAsync(r => r.ProjectId == contract.ProjectId);

        if (existing != null)
        {
            if (existing.Status == FinalReportStatus.Accepted || existing.Status == FinalReportStatus.Archived)
                throw new InvalidOperationException($"Báo cáo tổng kết đang ở trạng thái {existing.Status} — không sửa được nữa.");

            // Resubmission after revision
            existing.ReportFileUrl = request.ReportFileUrl;
            existing.SummaryFileUrl = request.SummaryFileUrl;
            existing.Language = request.Language;
            existing.FinalSubmittedAt = _clock.UtcNow;
            existing.Status = FinalReportStatus.Submitted;
            await _contracts.SaveChangesAsync();
            return Map(existing);
        }

        contract.Project.Status = ProjectStatus.Acceptance;
        contract.Project.UpdatedAt = DateTime.UtcNow;

        var report = new FinalReport
        {
            ProjectId = contract.ProjectId,
            ReportFileUrl = request.ReportFileUrl,
            SummaryFileUrl = request.SummaryFileUrl,
            Language = request.Language,
            SubmittedAt = _clock.UtcNow,
            Status = FinalReportStatus.Submitted
        };

        await _contracts.AddFinalReportAsync(report);
        await _contracts.SaveChangesAsync();
        return Map(report);
    }

    public async Task<FinalReportDto> RequestRevisionAsync(Guid reportId, RequestRevisionRequest request, Guid staffId)
    {
        if (string.IsNullOrWhiteSpace(request.RevisionNotes))
            throw new ArgumentException("Phải ghi rõ nội dung cần chỉnh sửa.");

        var report = await _contracts.FinalReports
            .FirstOrDefaultAsync(r => r.Id == reportId)
            ?? throw new KeyNotFoundException($"Final report {reportId} not found.");

        if (report.Status != FinalReportStatus.Submitted)
            throw new InvalidOperationException($"Báo cáo đang ở trạng thái {report.Status} — chỉ yêu cầu chỉnh sửa được với báo cáo đã nộp.");

        report.Status = FinalReportStatus.RevisionRequired;
        report.RevisionNotes = request.RevisionNotes;
        report.RevisionRequestedAt = _clock.UtcNow;
        await _contracts.SaveChangesAsync();
        return Map(report);
    }

    public async Task<FinalReportDto> AcceptAsync(Guid reportId, Guid staffId)
    {
        var report = await _contracts.FinalReports
            .FirstOrDefaultAsync(r => r.Id == reportId)
            ?? throw new KeyNotFoundException($"Final report {reportId} not found.");

        if (report.Status != FinalReportStatus.Submitted)
            throw new InvalidOperationException($"Báo cáo đang ở trạng thái {report.Status} — chỉ chấp nhận được báo cáo đã nộp.");

        report.Status = FinalReportStatus.Accepted;
        report.ArchivalDeadline = DateOnly.FromDateTime(_clock.UtcNow.AddMonths(3));
        await _contracts.SaveChangesAsync();
        return Map(report);
    }

    public async Task<FinalReportDto> ArchiveAsync(Guid reportId, Guid staffId)
    {
        var report = await _contracts.FinalReports
            .FirstOrDefaultAsync(r => r.Id == reportId)
            ?? throw new KeyNotFoundException($"Final report {reportId} not found.");

        if (report.Status != FinalReportStatus.Accepted)
            throw new InvalidOperationException($"Báo cáo đang ở trạng thái {report.Status} — chỉ lưu trữ được báo cáo đã được chấp nhận.");

        report.Status = FinalReportStatus.Archived;
        report.ArchivedAt = _clock.UtcNow;
        await _contracts.SaveChangesAsync();
        return Map(report);
    }

    private static FinalReportDto Map(FinalReport r) => new()
    {
        Id = r.Id,
        ProjectId = r.ProjectId,
        Status = r.Status,
        ReportFileUrl = r.ReportFileUrl,
        SummaryFileUrl = r.SummaryFileUrl,
        Language = r.Language,
        Deadline = r.Deadline?.ToString("yyyy-MM-dd"),
        SubmittedAt = r.SubmittedAt,
        RevisionNotes = r.RevisionNotes,
        RevisionRequestedAt = r.RevisionRequestedAt,
        FinalSubmittedAt = r.FinalSubmittedAt,
        ArchivalDeadline = r.ArchivalDeadline?.ToString("yyyy-MM-dd"),
        ArchivedAt = r.ArchivedAt
    };
}
