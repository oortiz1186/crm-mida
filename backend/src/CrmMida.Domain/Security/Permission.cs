using CrmMida.Domain.Common;

namespace CrmMida.Domain.Security;

public sealed class Permission : BaseEntity
{
    private Permission() { }

    public Permission(string code, string description)
    {
        Code = code.Trim().ToLowerInvariant();
        Description = description.Trim();
    }

    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();
}
