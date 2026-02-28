using AmieLife.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmieLife.Infrastructure.Data.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");

        builder.HasKey(p => p.ResetId);
        builder.Property(p => p.ResetId)
            .HasColumnName("reset_id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(p => p.TokenHash).HasColumnName("token_hash").IsRequired();
        builder.Property(p => p.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(p => p.IsUsed).HasColumnName("is_used").HasDefaultValue(false);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(p => p.TokenHash).HasDatabaseName("ix_password_reset_token_hash");
        builder.HasIndex(p => p.UserId).HasDatabaseName("ix_password_reset_user_id");
    }
}
