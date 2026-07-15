namespace FURPMS.Domain.Entities.AI;

public class SemanticSearchVector
{
    public int Id { get; set; }
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? ContentSnapshot { get; set; }
    public string ContentHash { get; set; } = null!;
    public string? ElasticsearchDocId { get; set; }
    public DateTime LastIndexedAt { get; set; } = DateTime.UtcNow;
    public string IndexStatus { get; set; } = "PENDING";
}
