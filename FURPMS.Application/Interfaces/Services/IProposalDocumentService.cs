using FURPMS.Application.DTOs.Proposals;

namespace FURPMS.Application.Interfaces.Services;

// Quản lý file đính kèm của đề cương (cả 2 đường: nhập tay & upload+AI đều giữ file gốc).
// File lưu trên đĩa (storage), DB chỉ giữ metadata (entity Document).
public interface IProposalDocumentService
{
    Task<ProposalDocumentDto> UploadAsync(
        Guid proposalId, Stream content, string fileName, string contentType,
        long length, string? documentType, Guid uploadedBy);

    Task<IEnumerable<ProposalDocumentDto>> ListForProposalAsync(Guid proposalId);

    // Kho tài liệu toàn cục (Admin/Staff) — kèm context đề tài/PI.
    Task<IEnumerable<ProposalDocumentDto>> ListAllAsync();

    Task<(Stream Stream, string ContentType, string FileName)> DownloadAsync(Guid proposalId, Guid documentId);

    /// <summary>
    /// File đính kèm MỚI NHẤT của đề cương, đọc sẵn ra byte để đưa cho AI đối chiếu.
    /// <c>null</c> khi đề cương chưa đính kèm file nào — gọi bên ngoài tự xử, không ném lỗi.
    /// </summary>
    Task<(byte[] Content, string ContentType, string FileName)?> GetLatestProposalFileAsync(Guid proposalId);

    Task DeleteAsync(Guid proposalId, Guid documentId);

    // Minh chứng giải ngân (rule tuần 10): Staff upload file hợp đồng/chứng từ gắn 1 đợt giải ngân.
    // Dùng chung entity Document (polymorphic EntityType="Disbursement").
    Task<ProposalDocumentDto> UploadForDisbursementAsync(
        int disbursementId, Stream content, string fileName, string contentType, long length, Guid uploadedBy);
    Task<IEnumerable<ProposalDocumentDto>> ListForDisbursementAsync(int disbursementId);
    Task<(Stream Stream, string ContentType, string FileName)> DownloadEvidenceAsync(Guid documentId);

    // BM06 — file báo cáo tiến độ: PI upload PDF; Staff phải xem được file rồi mới đánh giá Đạt/Không đạt.
    Task<ProposalDocumentDto> UploadForProgressReportAsync(
        Guid reportId, Stream content, string fileName, string contentType, long length, Guid uploadedBy);
    Task<IEnumerable<ProposalDocumentDto>> ListForProgressReportAsync(Guid reportId);
    Task<(Stream Stream, string ContentType, string FileName)> DownloadProgressReportDocAsync(Guid documentId);

    // BM09 — file báo cáo tổng kết: PI upload PDF thay vì dán URL (góp ý thầy 29/07).
    Task<ProposalDocumentDto> UploadForFinalReportAsync(
        Guid contractId, Stream content, string fileName, string contentType, long length, Guid uploadedBy);
    Task<IEnumerable<ProposalDocumentDto>> ListForFinalReportAsync(Guid contractId);
    Task<(Stream Stream, string ContentType, string FileName)> DownloadFinalReportDocAsync(Guid documentId);

    // BM05 — hồ sơ hợp đồng: bản Word đã ký/scan upload lên (Document polymorphic EntityType="Contract").
    Task<ProposalDocumentDto> UploadForContractAsync(
        Guid contractId, Stream content, string fileName, string contentType, long length, Guid uploadedBy);
    Task<IEnumerable<ProposalDocumentDto>> ListForContractAsync(Guid contractId);
    Task<(Stream Stream, string ContentType, string FileName)> DownloadContractDocAsync(Guid documentId);
}
