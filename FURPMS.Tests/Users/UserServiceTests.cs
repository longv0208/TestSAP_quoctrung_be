using FURPMS.Application.DTOs.Users;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.Users;

public class UserServiceTests
{
    private static async Task<(FURPMSDbContext db, Role staff, OrganizationalUnit unit)> SeedAsync()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var staff = new Role { Id = 1, Name = "Staff" };
        var admin = new Role { Id = 2, Name = "Admin" };
        var unit = new OrganizationalUnit { Id = 1, Code = "CNTT", Name = "Khoa CNTT", UnitType = "FACULTY" };
        db.Roles.AddRange(staff, admin);
        db.OrganizationalUnits.Add(unit);
        await db.SaveChangesAsync();
        return (db, staff, unit);
    }

    private static UserService Service(FURPMSDbContext db) => new(
        new UserRepository(db), TestNotifier.Create(db), new ProposalRepository(db), new ReviewRepository(db));

    [Fact]
    public async Task Create_ValidUser_TrimsEmailHashesPasswordAssignsRoleAndNotifies()
    {
        var (db, staff, _) = await SeedAsync();

        var result = await Service(db).CreateUserAsync(new CreateUserRequest
        {
            Email = "  new.user@example.com  ", FullName = "Người mới", TemporaryPassword = "TempPass123",
            Roles = new() { staff.Id }
        }, Guid.NewGuid());

        Assert.Equal("new.user@example.com", result.Email);
        Assert.Contains("Staff", result.Roles);
        var saved = await db.Users.SingleAsync(u => u.Id == result.Id);
        Assert.NotEqual("TempPass123", saved.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("TempPass123", saved.PasswordHash));
        Assert.True(await db.Notifications.AnyAsync(n => n.UserId == result.Id && n.NotificationType == "ACCOUNT_CREATED"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("person@localhost")]
    [InlineData("person@example")]
    public async Task Create_InvalidEmail_ThrowsArgumentException(string email)
    {
        var (db, staff, _) = await SeedAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).CreateUserAsync(new CreateUserRequest
        {
            Email = email, FullName = "Người dùng", TemporaryPassword = "TempPass123", Roles = new() { staff.Id }
        }, Guid.NewGuid()));
    }

    [Fact]
    public async Task Create_DuplicateActiveEmail_ThrowsConflict()
    {
        var (db, staff, _) = await SeedAsync();
        db.Users.Add(new User { Email = "same@example.com", FullName = "Cũ", Status = UserStatus.Active });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(db).CreateUserAsync(new CreateUserRequest
        {
            Email = "same@example.com", FullName = "Mới", TemporaryPassword = "TempPass123", Roles = new() { staff.Id }
        }, Guid.NewGuid()));
    }

    [Fact]
    public async Task Create_UnknownRole_ThrowsNotFoundBeforePersistingUser()
    {
        var (db, _, _) = await SeedAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service(db).CreateUserAsync(new CreateUserRequest
        {
            Email = "new@example.com", FullName = "Người dùng", TemporaryPassword = "TempPass123", Roles = new() { 999 }
        }, Guid.NewGuid()));

        Assert.Empty(await db.Users.ToListAsync());
    }

    [Fact]
    public async Task Update_ValidUser_ChangesProfileUnitDegreeAndRoles()
    {
        var (db, staff, unit) = await SeedAsync();
        var user = new User { Email = "old@example.com", FullName = "Tên cũ", Status = UserStatus.Active };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await Service(db).UpdateUserAsync(user.Id, new UpdateUserRequest
        {
            FullName = "Tên mới", PhoneNumber = "0900000000", Department = unit.Code,
            AcademicDegree = 2, Roles = new() { staff.Id }
        });

        Assert.Equal("Tên mới", result.FullName);
        Assert.Equal("0900000000", result.PhoneNumber);
        Assert.Equal(unit.Name, result.Department);
        Assert.Equal("Tiến sĩ", result.AcademicDegree);
        Assert.Contains("Staff", result.Roles);
        Assert.Equal(unit.Id, (await db.Users.FindAsync(user.Id))!.UnitId);
        Assert.Equal("Tiến sĩ", (await db.AcademicProfiles.SingleAsync(a => a.UserId == user.Id)).DegreeLevel);
    }

    [Fact]
    public async Task Update_MissingNameOrUnknownRole_IsRejected()
    {
        var (db, staff, _) = await SeedAsync();
        var user = new User { Email = "user@example.com", FullName = "Tên", Status = UserStatus.Active };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).UpdateUserAsync(user.Id,
            new UpdateUserRequest { FullName = "  ", Roles = new() { staff.Id } }));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service(db).UpdateUserAsync(user.Id,
            new UpdateUserRequest { FullName = "Tên mới", Roles = new() { 999 } }));
    }

    [Fact]
    public async Task Update_UnknownUser_ThrowsNotFound()
    {
        var (db, staff, _) = await SeedAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service(db).UpdateUserAsync(Guid.NewGuid(),
            new UpdateUserRequest { FullName = "Tên mới", Roles = new() { staff.Id } }));
    }
}
