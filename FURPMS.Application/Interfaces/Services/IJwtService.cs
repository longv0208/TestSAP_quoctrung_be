using FURPMS.Domain.Entities.Users;

namespace FURPMS.Application.Interfaces.Services;

public interface IJwtService
{
    string GenerateToken(User user, IEnumerable<string> roles);
    int ExpiryMinutes { get; }
}
