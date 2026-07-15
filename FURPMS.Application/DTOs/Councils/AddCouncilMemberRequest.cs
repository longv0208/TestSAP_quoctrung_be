namespace FURPMS.Application.DTOs.Councils;

public class AddCouncilMemberRequest
{
    public Guid UserId { get; set; }
    public string MemberRole { get; set; } = null!;
    public bool IsExternal { get; set; }
}
