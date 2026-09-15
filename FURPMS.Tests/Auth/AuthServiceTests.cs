using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Auth;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.Auth;

public class AuthServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static async Task<(FURPMSDbContext db, User user)> SeedAsync(string status = UserStatus.Active)
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var role = new Role { Id = 1, Name = "Staff" };
        var user = new User
        {
            Id = UserId, Email = $"login-{Guid.NewGuid():N}@test.com", FullName = "Người dùng",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CurrentPassword1"), Status = status
        };
        db.Roles.Add(role);
        db.Users.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();
        return (db, user);
    }

    private static AuthService Service(FURPMSDbContext db) =>
        new(new UserRepository(db), new StubJwtService(), TestNotifier.Create(db));

    [Fact]
    public async Task Login_ActiveUser_ReturnsTokenRolesAndLastLogin()
    {
        var (db, user) = await SeedAsync();

        var result = await Service(db).LoginAsync(new LoginRequest
        {
            Email = user.Email, Password = "CurrentPassword1"
        });

        Assert.Equal("test-token", result.AccessToken);
        Assert.Equal(900, result.ExpiresIn);
        Assert.Contains("Staff", result.User.Roles);
        Assert.NotNull((await db.Users.FindAsync(user.Id))!.LastLoginAt);
    }

    [Fact]
    public async Task Login_UnknownEmailOrWrongPassword_UsesInvalidCredentialsError()
    {
        var (db, user) = await SeedAsync();

        var unknown = await Assert.ThrowsAsync<AppException>(() => Service(db).LoginAsync(new LoginRequest
        {
            Email = "unknown@test.com", Password = "CurrentPassword1"
        }));
        Assert.Equal(ErrorCodes.InvalidCredentials, unknown.Code);

        var wrong = await Assert.ThrowsAsync<AppException>(() => Service(db).LoginAsync(new LoginRequest
        {
            Email = user.Email, Password = "WrongPassword1"
        }));
        Assert.Equal(ErrorCodes.InvalidCredentials, wrong.Code);
    }

    [Fact]
    public async Task Login_InactiveUser_UsesAccountInactiveError()
    {
        var (db, user) = await SeedAsync("INACTIVE");

        var ex = await Assert.ThrowsAsync<AppException>(() => Service(db).LoginAsync(new LoginRequest
        {
            Email = user.Email, Password = "CurrentPassword1"
        }));

        Assert.Equal(ErrorCodes.AccountInactive, ex.Code);
        Assert.Null((await db.Users.FindAsync(user.Id))!.LastLoginAt);
    }

    [Fact]
    public async Task ChangePassword_WithCurrentPassword_ReplacesHash()
    {
        var (db, user) = await SeedAsync();
        var oldHash = user.PasswordHash;

        await Service(db).ChangePasswordAsync(user.Id, new ChangePasswordRequest
        {
            CurrentPassword = "CurrentPassword1", NewPassword = "NewPassword123"
        });

        var saved = await db.Users.FindAsync(user.Id);
        Assert.NotEqual(oldHash, saved!.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword123", saved.PasswordHash));
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ThrowsPasswordIncorrect()
    {
        var (db, user) = await SeedAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() => Service(db).ChangePasswordAsync(user.Id,
            new ChangePasswordRequest { CurrentPassword = "WrongPassword1", NewPassword = "NewPassword123" }));

        Assert.Equal(ErrorCodes.PasswordIncorrect, ex.Code);
        Assert.True(BCrypt.Net.BCrypt.Verify("CurrentPassword1", (await db.Users.FindAsync(user.Id))!.PasswordHash));
    }

    [Fact]
    public async Task ChangePassword_MissingUser_ThrowsNotFound()
    {
        var (db, _) = await SeedAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service(db).ChangePasswordAsync(Guid.NewGuid(),
            new ChangePasswordRequest { CurrentPassword = "CurrentPassword1", NewPassword = "NewPassword123" }));
    }

    private sealed class StubJwtService : IJwtService
    {
        public int ExpiryMinutes => 15;
        public string GenerateToken(User user, IEnumerable<string> roles) => "test-token";
    }
}
