namespace FURPMS.Application.DTOs.Budget;

public class LaborDetailResponse
{
    public int Id { get; set; }
    public int TeamMemberId { get; set; }
    public string? TeamMemberName { get; set; }
    public decimal TotalResearchHours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? WorkDays { get; set; }
    public decimal? Coefficient { get; set; }
    public decimal? DailyRate { get; set; }
    public decimal? ComputedDailyTotal { get; set; }
    public int Sequence { get; set; }
}
