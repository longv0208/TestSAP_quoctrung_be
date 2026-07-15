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

    Task DeleteAsync(Guid proposalId, Guid documentId);
}
