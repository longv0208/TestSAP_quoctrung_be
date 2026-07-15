namespace FURPMS.Domain.Entities.MasterData;

public class LlmConfig
{
    public int Id { get; set; }
    public string ConfigCode { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string ModelName { get; set; } = null!;
    public string SystemPrompt { get; set; } = null!;
    public decimal Temperature { get; set; } = 0.7m;
    public bool IsActive { get; set; } = true;
}
