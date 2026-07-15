namespace FURPMS.Domain.Entities.MasterData;

public class ResearchTrack
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? OwnerId { get; set; }
}
