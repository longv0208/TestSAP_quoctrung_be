using FURPMS.Application.DTOs.Progress;

namespace FURPMS.Application.Interfaces.Services;

public interface IProgressReportService
{
    Task<IEnumerable<ProgressReportSummaryDto>> GetByContractAsync(Guid contractId);
    Task<ProgressReportDto> GetByIdAsync(Guid reportId);
    Task<ProgressReportDto> CreateAsync(Guid contractId, CreateProgressReportRequest request, Guid userId);
    Task<ProgressReportDto> SubmitAsync(Guid reportId, Guid userId);
    Task<ProgressReportDto> EvaluateAsync(Guid reportId, EvaluateProgressReportRequest request, Guid staffId);
    Task<ProgressReportDto> ScheduleAsync(Guid reportId, ScheduleProgressReportRequest request);
}
