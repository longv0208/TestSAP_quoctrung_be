using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;

namespace FURPMS.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(FURPMSDbContext db) : base(db) { }

    public IQueryable<Role> Roles => _db.Roles;
    public IQueryable<UserRole> UserRoles => _db.UserRoles;
    public IQueryable<AcademicProfile> AcademicProfiles => _db.AcademicProfiles;

    public IQueryable<OrganizationalUnit> OrganizationalUnits => _db.OrganizationalUnits.AsQueryable();

    public void AddAcademicProfile(AcademicProfile profile) => _db.AcademicProfiles.Add(profile);

    public void AddUserRole(UserRole userRole) => _db.UserRoles.Add(userRole);
    public void RemoveUserRoles(IEnumerable<UserRole> userRoles) => _db.UserRoles.RemoveRange(userRoles);
}
