using CrmMida.Domain.Common;

namespace CrmMida.Domain.Security;

public sealed class Role : BaseEntity
{
    private Role() { }

    public Role(string name, string description)
    {
        Name = name.Trim();
        Description = description.Trim();
    }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();
}
