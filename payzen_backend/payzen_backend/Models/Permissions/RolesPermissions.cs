namespace payzen_backend.Models.Permissions
{
    public class RolesPermissions
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public Roles Role { get; set; } = null!;
        public Permissions Permission { get; set; } = null!;
    }
}
