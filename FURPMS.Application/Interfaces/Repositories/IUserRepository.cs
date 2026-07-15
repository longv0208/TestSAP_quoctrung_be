using FURPMS.Domain.Entities.Users;

namespace FURPMS.Application.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    IQueryable<Role> Roles { get; }
    IQueryable<UserRole> UserRoles { get; }
    IQueryable<AcademicProfile> AcademicProfiles { get; }
    void AddUserRole(UserRole userRole);
    void RemoveUserRoles(IEnumerable<UserRole> userRoles);
}
