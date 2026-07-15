namespace FURPMS.Application.DTOs.Notifications;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? ActionUrl { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public string Priority { get; set; } = "NORMAL";
    public DateTime CreatedAt { get; set; }
}

public class NotificationCountDto
{
    public int Unread { get; set; }
    public int Total { get; set; }
}
