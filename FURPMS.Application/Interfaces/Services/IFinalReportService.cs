using FURPMS.Application.DTOs.Progress;

namespace FURPMS.Application.Interfaces.Services;

public interface IFinalReportService
{
    Task<FinalReportDto?> GetByContractAsync(Guid contractId);
    Task<FinalReportDto> SubmitAsync(Guid contractId, SubmitFinalReportRequest request, Guid userId);
    Task<FinalReportDto> RequestRevisionAsync(Guid reportId, RequestRevisionRequest request, Guid staffId);
    Task<FinalReportDto> AcceptAsync(Guid reportId, Guid staffId);
    Task<FinalReportDto> ArchiveAsync(Guid reportId, Guid staffId);
}
