namespace FURPMS.Application.Interfaces.Services;

public interface IDocumentExportService
{
    Task<(byte[] Content, string FileName)> ExportScientificDocAsync(Guid proposalId);
    Task<(byte[] Content, string FileName)> ExportBudgetDocAsync(Guid proposalId);
}
