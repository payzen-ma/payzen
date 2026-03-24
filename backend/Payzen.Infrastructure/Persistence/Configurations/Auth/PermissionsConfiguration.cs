using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payzen.Domain.Entities.Auth;

namespace Payzen.Infrastructure.Persistence.Configurations.Auth;

public class PermissionsConfiguration : IEntityTypeConfiguration<Permissions>
{
    public void Configure(EntityTypeBuilder<Permissions> entity)
    {
        entity.ToTable("Permissions");
        entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
        entity.Property(p => p.Description).IsRequired().HasMaxLength(500);
        entity.Property(p => p.Resource).HasMaxLength(100);
        entity.Property(p => p.Action).HasMaxLength(100);
        entity.HasIndex(p => p.Name).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}
