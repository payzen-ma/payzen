using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Auth;

public class RolesPermissions : BaseEntity
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }

    // Navigation properties
    public Roles Role { get; set; } = null!;
    public Permissions Permission { get; set; } = null!;
}
