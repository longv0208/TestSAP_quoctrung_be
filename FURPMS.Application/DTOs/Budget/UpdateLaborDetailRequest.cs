namespace FURPMS.Application.DTOs.Budget;

public class UpdateLaborDetailRequest
{
    public decimal? WorkDays { get; set; }
    public decimal? Coefficient { get; set; }
    public decimal TotalResearchHours { get; set; }
    public decimal HourlyRate { get; set; }
    public int Sequence { get; set; }
}
