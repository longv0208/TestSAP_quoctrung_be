using FURPMS.Application.DTOs.Proposals;

namespace FURPMS.Application.Interfaces.Services;

// Đường B: PI upload Word/PDF → AI đọc & trích xuất field cấu trúc để prefill form.
// Luôn degrade an toàn — lỗi/không cấu hình AI → trả Warning, FE vẫn cho nhập tay.
public interface IProposalExtractionService
{
    Task<ExtractedProposalDto> ExtractAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
}
