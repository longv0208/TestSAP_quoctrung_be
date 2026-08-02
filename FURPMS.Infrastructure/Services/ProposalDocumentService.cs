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
    private readonly ISystemSettingService _settings;
    private readonly string _root;

    public ProposalDocumentService(
        IDocumentRepository docs,
        IProposalRepository proposals,
        ISystemSettingService settings,
        IConfiguration config)
    {
        _docs = docs;
        _proposals = proposals;
        _settings = settings;
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

        // Giới hạn do Admin đặt trong system_settings (mặc định khuyến cáo 10 MB) — đọc mỗi lần upload
        // để đổi cấu hình có hiệu lực ngay, không phải restart app.
        var policy = await _settings.GetUploadPolicyAsync();
        var maxBytes = (long)policy.MaxFileSizeMb * 1024 * 1024;

        if (length > maxBytes)
            throw new ArgumentException(
                $"File quá lớn ({length / 1024d / 1024d:0.#} MB). Tối đa {policy.MaxFileSizeMb} MB.");

        var ext = Path.GetExtension(fileName);
        var allowed = policy.AllowedExtensions.ToList();
        if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Định dạng '{ext}' không được phép. Chỉ nhận: {string.Join(", ", allowed)}.");

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

    // ── Minh chứng giải ngân (rule tuần 10) ─────────────────────────────────────
    private const string EntityTypeDisbursement = "Disbursement";

    public async Task<ProposalDocumentDto> UploadForDisbursementAsync(
        int disbursementId, Stream content, string fileName, string contentType, long length, Guid uploadedBy)
    {
        if (length <= 0) throw new ArgumentException("File rỗng.");

        var policy = await _settings.GetUploadPolicyAsync();
        var maxBytes = (long)policy.MaxFileSizeMb * 1024 * 1024;
        if (length > maxBytes)
            throw new ArgumentException($"File quá lớn ({length / 1024d / 1024d:0.#} MB). Tối đa {policy.MaxFileSizeMb} MB.");

        var ext = Path.GetExtension(fileName);
        var allowed = policy.AllowedExtensions.ToList();
        if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Định dạng '{ext}' không được phép. Chỉ nhận: {string.Join(", ", allowed)}.");

        var blobName = $"disbursements/{disbursementId}/{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_root, blobName.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            await content.CopyToAsync(fs);

        var doc = new Document
        {
            EntityType = EntityTypeDisbursement,
            EntityId = disbursementId.ToString(),
            DocumentCategory = "EVIDENCE",
            OriginalFileName = fileName,
            FileSizeBytes = length,
            MimeType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            StorageContainer = "local",
            StorageBlobName = blobName,
            UploadedBy = uploadedBy
        };
        doc.StorageUrl = $"/api/disbursements/{disbursementId}/evidence/{doc.Id}/download";

        await _docs.AddAsync(doc);
        await _docs.SaveChangesAsync();
        return Map(doc);
    }

    public async Task<IEnumerable<ProposalDocumentDto>> ListForDisbursementAsync(int disbursementId)
    {
        var docs = await _docs.Query()
            .Where(d => d.EntityType == EntityTypeDisbursement && d.EntityId == disbursementId.ToString() && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
        return docs.Select(Map);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadEvidenceAsync(Guid documentId)
    {
        var doc = await _docs.Query()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.EntityType == EntityTypeDisbursement && !d.IsDeleted)
            ?? throw new KeyNotFoundException("Không tìm thấy minh chứng.");

        var fullPath = Path.Combine(_root, doc.StorageBlobName.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            throw new KeyNotFoundException("File không còn trên storage.");

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return (stream, doc.MimeType, doc.OriginalFileName);
    }

    // ── File báo cáo tiến độ (BM06) ────────────────────────────────────────────
    // PI upload PDF báo cáo; Staff mở xem rồi mới đánh giá Đạt/Không đạt (góp ý thầy 29/07).
    private const string EntityTypeProgressReport = "ProgressReport";

    public async Task<ProposalDocumentDto> UploadForProgressReportAsync(
        Guid reportId, Stream content, string fileName, string contentType, long length, Guid uploadedBy)
    {
        var ext = await AssertUploadAllowedAsync(fileName, length);

        var blobName = $"progress-reports/{reportId}/{Guid.NewGuid():N}{ext}";
        await SaveToDiskAsync(blobName, content);

        var doc = new Document
        {
            EntityType = EntityTypeProgressReport,
            EntityId = reportId.ToString(),
            DocumentCategory = "PROGRESS_REPORT",
            OriginalFileName = fileName,
            FileSizeBytes = length,
            MimeType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            StorageContainer = "local",
            StorageBlobName = blobName,
            UploadedBy = uploadedBy
        };
        doc.StorageUrl = $"/api/progress-reports/{reportId}/documents/{doc.Id}/download";

        await _docs.AddAsync(doc);
        await _docs.SaveChangesAsync();
        return Map(doc);
    }

    public async Task<IEnumerable<ProposalDocumentDto>> ListForProgressReportAsync(Guid reportId)
    {
        var docs = await _docs.Query()
            .Where(d => d.EntityType == EntityTypeProgressReport && d.EntityId == reportId.ToString() && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
        return docs.Select(Map);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadProgressReportDocAsync(Guid documentId)
    {
        var doc = await _docs.Query()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.EntityType == EntityTypeProgressReport && !d.IsDeleted)
            ?? throw new KeyNotFoundException("Không tìm thấy file báo cáo.");
        return OpenStored(doc);
    }

    // ── File báo cáo tổng kết (BM09) ───────────────────────────────────────────
    private const string EntityTypeFinalReport = "FinalReport";

    public async Task<ProposalDocumentDto> UploadForFinalReportAsync(
        Guid contractId, Stream content, string fileName, string contentType, long length, Guid uploadedBy)
    {
        var ext = await AssertUploadAllowedAsync(fileName, length);

        var blobName = $"final-reports/{contractId}/{Guid.NewGuid():N}{ext}";
        await SaveToDiskAsync(blobName, content);

        var doc = new Document
        {
            EntityType = EntityTypeFinalReport,
            EntityId = contractId.ToString(),
            DocumentCategory = "FINAL_REPORT",
            OriginalFileName = fileName,
            FileSizeBytes = length,
            MimeType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            StorageContainer = "local",
            StorageBlobName = blobName,
            UploadedBy = uploadedBy
        };
        doc.StorageUrl = $"/api/final-reports/{contractId}/documents/{doc.Id}/download";

        await _docs.AddAsync(doc);
        await _docs.SaveChangesAsync();
        return Map(doc);
    }

    public async Task<IEnumerable<ProposalDocumentDto>> ListForFinalReportAsync(Guid contractId)
    {
        var docs = await _docs.Query()
            .Where(d => d.EntityType == EntityTypeFinalReport && d.EntityId == contractId.ToString() && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
        return docs.Select(Map);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadFinalReportDocAsync(Guid documentId)
    {
        var doc = await _docs.Query()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.EntityType == EntityTypeFinalReport && !d.IsDeleted)
            ?? throw new KeyNotFoundException("Không tìm thấy file báo cáo tổng kết.");
        return OpenStored(doc);
    }

    // Dùng chung cho các luồng upload (kiểm dung lượng + phần mở rộng theo SystemSetting).
    private async Task<string> AssertUploadAllowedAsync(string fileName, long length)
    {
        if (length <= 0) throw new ArgumentException("File rỗng.");

        var policy = await _settings.GetUploadPolicyAsync();
        var maxBytes = (long)policy.MaxFileSizeMb * 1024 * 1024;
        if (length > maxBytes)
            throw new ArgumentException($"File quá lớn ({length / 1024d / 1024d:0.#} MB). Tối đa {policy.MaxFileSizeMb} MB.");

        var ext = Path.GetExtension(fileName);
        var allowed = policy.AllowedExtensions.ToList();
        if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Định dạng '{ext}' không được phép. Chỉ nhận: {string.Join(", ", allowed)}.");
        return ext;
    }

    private async Task SaveToDiskAsync(string blobName, Stream content)
    {
        var fullPath = Path.Combine(_root, blobName.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fs);
    }

    private (Stream Stream, string ContentType, string FileName) OpenStored(Document doc)
    {
        var fullPath = Path.Combine(_root, doc.StorageBlobName.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            throw new KeyNotFoundException("File không còn trên storage.");
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return (stream, doc.MimeType, doc.OriginalFileName);
    }

    // ── Hồ sơ hợp đồng (BM05 — bản ký) ─────────────────────────────────────────
    private const string EntityTypeContract = "Contract";

    public async Task<ProposalDocumentDto> UploadForContractAsync(
        Guid contractId, Stream content, string fileName, string contentType, long length, Guid uploadedBy)
    {
        if (length <= 0) throw new ArgumentException("File rỗng.");

        var policy = await _settings.GetUploadPolicyAsync();
        var maxBytes = (long)policy.MaxFileSizeMb * 1024 * 1024;
        if (length > maxBytes)
            throw new ArgumentException($"File quá lớn ({length / 1024d / 1024d:0.#} MB). Tối đa {policy.MaxFileSizeMb} MB.");

        var ext = Path.GetExtension(fileName);
        var allowed = policy.AllowedExtensions.ToList();
        if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Định dạng '{ext}' không được phép. Chỉ nhận: {string.Join(", ", allowed)}.");

        var blobName = $"contracts/{contractId}/{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_root, blobName.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            await content.CopyToAsync(fs);

        var doc = new Document
        {
            EntityType = EntityTypeContract,
            EntityId = contractId.ToString(),
            DocumentCategory = "SIGNED_CONTRACT",
            OriginalFileName = fileName,
            FileSizeBytes = length,
            MimeType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            StorageContainer = "local",
            StorageBlobName = blobName,
            UploadedBy = uploadedBy
        };
        doc.StorageUrl = $"/api/contracts/{contractId}/documents/{doc.Id}/download";

        await _docs.AddAsync(doc);
        await _docs.SaveChangesAsync();
        return Map(doc);
    }

    public async Task<IEnumerable<ProposalDocumentDto>> ListForContractAsync(Guid contractId)
    {
        var docs = await _docs.Query()
            .Where(d => d.EntityType == EntityTypeContract && d.EntityId == contractId.ToString() && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
        return docs.Select(Map);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadContractDocAsync(Guid documentId)
    {
        var doc = await _docs.Query()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.EntityType == EntityTypeContract && !d.IsDeleted)
            ?? throw new KeyNotFoundException("Không tìm thấy tài liệu hợp đồng.");

        var fullPath = Path.Combine(_root, doc.StorageBlobName.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            throw new KeyNotFoundException("File không còn trên storage.");

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return (stream, doc.MimeType, doc.OriginalFileName);
    }
}
