using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payzen.Domain.Entities.Auth;

namespace Payzen.Infrastructure.Persistence.Configurations.Auth;

public class RolesPermissionsConfiguration : IEntityTypeConfiguration<RolesPermissions>
{
    public void Configure(EntityTypeBuilder<RolesPermissions> entity)
    {
        entity.ToTable("RolesPermissions");
        entity.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique().HasFilter("[DeletedAt] IS NULL");
        entity.HasOne(rp => rp.Role).WithMany().HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne(rp => rp.Permission)
            .WithMany()
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class UsersRolesConfiguration : IEntityTypeConfiguration<UsersRoles>
{
    public void Configure(EntityTypeBuilder<UsersRoles> entity)
    {
        entity.ToTable("UsersRoles");
        entity.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique().HasFilter("[DeletedAt] IS NULL");
        entity
            .HasOne(ur => ur.User)
            .WithMany(u => u.UsersRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(ur => ur.Role).WithMany().HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}
