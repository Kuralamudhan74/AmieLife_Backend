using AmieLife.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmieLife.Infrastructure.Data.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");

        builder.HasKey(a => a.AddressId);
        builder.Property(a => a.AddressId)
            .HasColumnName("address_id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(a => a.FullName).HasColumnName("full_name").HasMaxLength(150).IsRequired();
        builder.Property(a => a.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20).IsRequired();
        builder.Property(a => a.AddressLine1).HasColumnName("address_line1").HasMaxLength(255).IsRequired();
        builder.Property(a => a.AddressLine2).HasColumnName("address_line2").HasMaxLength(255);
        builder.Property(a => a.City).HasColumnName("city").HasMaxLength(100).IsRequired();
        builder.Property(a => a.State).HasColumnName("state").HasMaxLength(100).IsRequired();
        builder.Property(a => a.PostalCode).HasColumnName("postal_code").HasMaxLength(20).IsRequired();
        builder.Property(a => a.Country).HasColumnName("country").HasMaxLength(100).IsRequired();
        builder.Property(a => a.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(a => a.UserId).HasDatabaseName("ix_addresses_user_id");
    }
}
