namespace FURPMS.Application.DTOs.Councils;

public class RespondMembershipRequest
{
    public bool Accept { get; set; }
    public string? DeclineReason { get; set; }
}
