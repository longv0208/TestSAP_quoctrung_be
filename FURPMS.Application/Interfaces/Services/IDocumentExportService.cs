namespace FURPMS.Application.Interfaces.Services;

public interface IDocumentExportService
{
    Task<(byte[] Content, string FileName)> ExportScientificDocAsync(Guid proposalId);
    Task<(byte[] Content, string FileName)> ExportBudgetDocAsync(Guid proposalId);
    // BM05 — sinh Word hợp đồng (rule tuần 10): bốc dữ liệu điền mẫu → xuất .docx để ký ngoài.
    Task<(byte[] Content, string FileName)> ExportContractDocAsync(Guid contractId);

    /// <summary>
    /// **Phụ lục hợp đồng** cho một đề nghị điều chỉnh đã được duyệt (F4).
    /// Hợp đồng đã ký thì không sửa đè lên bản gốc — mỗi thay đổi (gia hạn, đổi nội dung, đổi
    /// kinh phí…) phải có một văn bản riêng đính kèm, dẫn chiếu hợp đồng gốc và ghi rõ
    /// <em>trước → sau</em>. Đây chính là "phụ lục" trong ghi chú của nhóm.
    /// </summary>
    Task<(byte[] Content, string FileName)> ExportAmendmentDocAsync(Guid amendmentId);
}
