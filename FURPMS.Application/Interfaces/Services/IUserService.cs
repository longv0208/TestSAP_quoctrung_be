using FURPMS.Application.DTOs.Users;

namespace FURPMS.Application.Interfaces.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetUsersAsync();
    Task<UserDto> GetUserByIdAsync(Guid userId);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, Guid createdBy);
    Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task<UserDto> ToggleActiveAsync(Guid userId);
    Task ResetPasswordAsync(Guid userId);
}
