using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.AI;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string DocumentCategory { get; set; } = null!;
    public string OriginalFileName { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; } = null!;
    public string StorageContainer { get; set; } = null!;
    public string StorageBlobName { get; set; } = null!;
    public string StorageUrl { get; set; } = null!;
    public bool IsConfidential { get; set; }
    public Guid UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    public User UploadedByUser { get; set; } = null!;
}
