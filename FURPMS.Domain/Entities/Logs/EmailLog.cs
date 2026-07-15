using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Logs;

public class EmailLog
{
    public long Id { get; set; }
    public Guid? RecipientUserId { get; set; }
    public string RecipientEmail { get; set; } = null!;
    public string EmailType { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string? TemplateName { get; set; }
    public DateTime? SentAt { get; set; }
    public string Status { get; set; } = "QUEUED";
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? RecipientUser { get; set; }
}
