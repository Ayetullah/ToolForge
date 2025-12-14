using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UtilityTools.Domain.Entities;

namespace UtilityTools.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        // SubscriptionTier stored as integer (enum default)
        // builder.Property(u => u.SubscriptionTier).HasConversion<string>(); // Removed - use integer
        builder.Property(u => u.PasswordResetToken).HasMaxLength(500);
        builder.HasMany(u => u.UsageRecords).WithOne(ur => ur.User).HasForeignKey(ur => ur.UserId);
        // Job relationship is configured in JobConfiguration (optional for anonymous users)
        builder.HasMany(u => u.Roles).WithMany(r => r.Users);
    }
}

