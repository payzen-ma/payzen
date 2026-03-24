using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Auth;

public class UsersRoles : BaseEntity
{
    public int UserId { get; set; }
    public int RoleId { get; set; }

    // Navigation properties
    public Users User { get; set; } = null!;
    public Roles Role { get; set; } = null!;
}