namespace FURPMS.Domain.Entities.Users;

public class OrganizationalUnit
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string UnitType { get; set; } = null!;
    public int? ParentId { get; set; }
    public Guid? HeadUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public int? SortOrder { get; set; }

    public OrganizationalUnit? Parent { get; set; }
    public User? HeadUser { get; set; }
    public ICollection<OrganizationalUnit> Children { get; set; } = new List<OrganizationalUnit>();
    public ICollection<User> Members { get; set; } = new List<User>();
}
