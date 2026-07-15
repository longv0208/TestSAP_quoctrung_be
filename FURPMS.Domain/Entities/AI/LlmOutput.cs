using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.AI;

public class LlmOutput
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string OutputType { get; set; } = null!;
    public string ModelUsed { get; set; } = null!;
    public string PromptVersion { get; set; } = null!;
    public string Content { get; set; } = null!;
    public int? TokensInput { get; set; }
    public int? TokensOutput { get; set; }
    public int? LatencyMs { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsReviewedByHuman { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
    public bool IsActive { get; set; } = true;

    public User? ReviewedByUser { get; set; }
}
