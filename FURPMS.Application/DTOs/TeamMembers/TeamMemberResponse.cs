namespace FURPMS.Application.DTOs.TeamMembers;

public class TeamMemberResponse
{
    public int Id { get; set; }
    public Guid? UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string? AcademicTitle { get; set; }
    public string? UnitName { get; set; }
    public string WorkContent { get; set; } = null!;
    public decimal WorkMonths { get; set; }
    public bool IsPi { get; set; }
    public bool IsSecretary { get; set; }
    public string? MemberRoleCode { get; set; }
    public decimal? SalaryCoefficient { get; set; }
    public int Sequence { get; set; }
}
