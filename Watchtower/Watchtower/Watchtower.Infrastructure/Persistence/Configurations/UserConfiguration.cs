using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Watchtower.Domain.Entities;

namespace Watchtower.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.EmailVerificationToken).HasMaxLength(64);
        builder.Property(u => u.PasswordResetToken).HasMaxLength(64);

        builder.HasMany(u => u.Endpoints)
               .WithOne(e => e.Owner)
               .HasForeignKey(e => e.OwnerId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
