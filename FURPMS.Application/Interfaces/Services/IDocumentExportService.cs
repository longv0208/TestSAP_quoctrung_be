namespace FURPMS.Application.Interfaces.Services;

public interface IDocumentExportService
{
    Task<(byte[] Content, string FileName)> ExportScientificDocAsync(Guid proposalId);
    Task<(byte[] Content, string FileName)> ExportBudgetDocAsync(Guid proposalId);
    // BM05 — sinh Word hợp đồng (rule tuần 10): bốc dữ liệu điền mẫu → xuất .docx để ký ngoài.
    Task<(byte[] Content, string FileName)> ExportContractDocAsync(Guid contractId);
}
