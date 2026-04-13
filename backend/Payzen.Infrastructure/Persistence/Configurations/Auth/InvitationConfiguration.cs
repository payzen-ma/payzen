using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payzen.Domain.Entities.Auth;

namespace Payzen.Infrastructure.Persistence.Configurations.Auth;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Token)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(i => i.Token)
            .IsUnique();

        builder.Property(i => i.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(i => i.Company)
            .WithMany()
            .HasForeignKey(i => i.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Role)
            .WithMany()
            .HasForeignKey(i => i.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
