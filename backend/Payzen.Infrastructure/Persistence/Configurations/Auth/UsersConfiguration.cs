using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payzen.Domain.Entities.Auth;

namespace Payzen.Infrastructure.Persistence.Configurations.Auth;

public class UsersConfiguration : IEntityTypeConfiguration<Users>
{
    public void Configure(EntityTypeBuilder<Users> entity)
    {
        entity.ToTable("Users");
        entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
        entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
        entity.Property(u => u.EmailPersonal).HasMaxLength(100);
        entity.Property(u => u.ExternalId).HasMaxLength(200);
        entity.Property(u => u.Source).HasMaxLength(50);
        entity.Property(u => u.IsActive).HasDefaultValue(true);
        entity.HasIndex(u => u.Email).IsUnique().HasFilter("[DeletedAt] IS NULL");
        entity.HasIndex(u => u.Username).IsUnique().HasFilter("[DeletedAt] IS NULL");
        entity.HasIndex(u => u.ExternalId).HasFilter("[ExternalId] IS NOT NULL AND [DeletedAt] IS NULL");
        entity.HasOne(u => u.Employee).WithMany().HasForeignKey(u => u.EmployeeId).OnDelete(DeleteBehavior.SetNull);
    }
}
