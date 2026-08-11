using FURPMS.Application.DTOs.Progress;

namespace FURPMS.Application.Interfaces.Services;

public interface IProgressReportService
{
    Task<IEnumerable<ProgressReportSummaryDto>> GetByContractAsync(Guid contractId);
    /// <summary>Staff sinh sẵn các kỳ báo cáo. roundCount bỏ trống → mặc định theo loại (Ứng dụng 2 / Cơ bản 1).</summary>
    Task<IEnumerable<ProgressReportSummaryDto>> GenerateScheduledRoundsAsync(Guid contractId, int? roundCount = null);
    Task<ProgressReportDto> GetByIdAsync(Guid reportId);
    Task<ProgressReportDto> CreateAsync(Guid contractId, CreateProgressReportRequest request, Guid userId);
    /// <summary>PI sửa nội dung báo cáo khi CHƯA nộp (còn DRAFT). Nộp rồi thì khoá.</summary>
    Task<ProgressReportDto> UpdateAsync(Guid reportId, UpdateProgressReportRequest request, Guid userId);
    Task DeleteAsync(Guid reportId, Guid actingUserId, bool isStaff);
    Task<ProgressReportDto> SubmitAsync(Guid reportId, Guid userId);
    Task<ProgressReportDto> EvaluateAsync(Guid reportId, EvaluateProgressReportRequest request, Guid staffId);
    Task<ProgressReportDto> ScheduleAsync(Guid reportId, ScheduleProgressReportRequest request);
    /// <summary>Người gọi có phải PI của đề tài chứa báo cáo này không (dùng gác quyền upload file BM06).</summary>
    Task<bool> IsPiOfReportAsync(Guid reportId, Guid userId);
}
