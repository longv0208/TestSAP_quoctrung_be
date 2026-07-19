using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Proposals;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FURPMS.Infrastructure.Services;

public class ProposalDocumentService : IProposalDocumentService
{
    private const string EntityTypeProposal = "Proposal";

    private readonly IDocumentRepository _docs;
    private readonly IProposalRepository _proposals;
    private readonly string _root;

    public ProposalDocumentService(
        IDocumentRepository docs,
        IProposalRepository proposals,
        IConfiguration config)
    {
        _docs = docs;
        _proposals = proposals;
        // Thư mục lưu file; config "DocumentStorage:RootPath" hoặc mặc định App_Data/uploads.
        _root = config["DocumentStorage:RootPath"]
                ?? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "uploads");
    }

    public async Task<ProposalDocumentDto> UploadAsync(
        Guid proposalId, Stream content, string fileName, string contentType,
        long length, string? documentType, Guid uploadedBy)
    {
        var exists = await _proposals.Query().IgnoreQueryFilters()
            .AnyAsync(p => p.Id == proposalId);
        if (!exists)
            throw new KeyNotFoundException($"Đề tài {proposalId} không tồn tại.");

        if (length <= 0)
            throw new ArgumentException("File rỗng.");

        var ext = Path.GetExtension(fileName);
        var blobName = $"proposals/{proposalId}/{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_root, blobName.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            await content.CopyToAsync(fs);
        }

        var doc = new Document
        {
            EntityType = EntityTypeProposal,
            EntityId = proposalId.ToString(),
            DocumentCategory = string.IsNullOrWhiteSpace(documentType) ? "ATTACHMENT" : documentType,
            OriginalFileName = fileName,
            FileSizeBytes = length,
            MimeType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            StorageContainer = "local",
            StorageBlobName = blobName,
            UploadedBy = uploadedBy
        };
        doc.StorageUrl = $"/api/proposals/{proposalId}/documents/{doc.Id}/download";

        await _docs.AddAsync(doc);
        await _docs.SaveChangesAsync();

        return Map(doc);
    }

    public async Task<IEnumerable<ProposalDocumentDto>> ListForProposalAsync(Guid proposalId)
    {
        var docs = await _docs.Query()
            .Where(d => d.EntityType == EntityTypeProposal
                        && d.EntityId == proposalId.ToString()
                        && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return docs.Select(Map);
    }

    public async Task<IEnumerable<ProposalDocumentDto>> ListAllAsync()
    {
        var docs = await _docs.Query()
            .Where(d => d.EntityType == EntityTypeProposal && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        var ids = docs
            .Select(d => Guid.TryParse(d.EntityId, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue).Select(g => g!.Value).Distinct().ToList();

        var proposals = await _proposals.Query().IgnoreQueryFilters()
            .Include(p => p.Project).ThenInclude(pr => pr.PiUser)
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        return docs.Select(d =>
        {
            var dto = Map(d);
            if (Guid.TryParse(d.EntityId, out var pid) && proposals.TryGetValue(pid, out var p))
            {
                dto.ProposalId = p.Id;
                dto.ProposalTitle = p.TitleVi;
                dto.PrincipalInvestigatorName = p.Project?.PiUser?.FullName;
            }
            return dto;
        });
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadAsync(Guid proposalId, Guid documentId)
    {
        var doc = await _docs.Query()
            .FirstOrDefaultAsync(d => d.Id == documentId
                                      && d.EntityId == proposalId.ToString()
                                      && !d.IsDeleted)
            ?? throw new KeyNotFoundException("Không tìm thấy tài liệu.");

        var fullPath = Path.Combine(_root, doc.StorageBlobName.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            throw new KeyNotFoundException("File không còn trên storage.");

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return (stream, doc.MimeType, doc.OriginalFileName);
    }

    public async Task DeleteAsync(Guid proposalId, Guid documentId)
    {
        var doc = await _docs.Query()
            .FirstOrDefaultAsync(d => d.Id == documentId
                                      && d.EntityId == proposalId.ToString()
                                      && !d.IsDeleted)
            ?? throw new KeyNotFoundException("Không tìm thấy tài liệu.");

        doc.IsDeleted = true;
        _docs.Update(doc);
        await _docs.SaveChangesAsync();
    }

    private static ProposalDocumentDto Map(Document d) => new()
    {
        Id = d.Id,
        FileName = d.OriginalFileName,
        DocumentType = d.DocumentCategory,
        FileSizeBytes = d.FileSizeBytes,
        UploadedAt = d.UploadedAt,
        DownloadUrl = d.StorageUrl
    };
}
