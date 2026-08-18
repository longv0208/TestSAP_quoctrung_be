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

    /// <summary>Xoá mềm tài khoản. Chặn nếu người này còn ràng buộc nghiệp vụ (chủ nhiệm đề tài, ủy viên hội đồng).</summary>
    Task DeleteUserAsync(Guid userId, Guid deletedBy);
}
