using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payzen.Domain.Entities.Auth;

namespace Payzen.Infrastructure.Persistence.Configurations.Auth;

public class RolesConfiguration : IEntityTypeConfiguration<Roles>
{
    public void Configure(EntityTypeBuilder<Roles> entity)
    {
        entity.ToTable("Roles");
        entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
        entity.Property(r => r.Description).IsRequired().HasMaxLength(500);
        entity.HasIndex(r => r.Name).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}
