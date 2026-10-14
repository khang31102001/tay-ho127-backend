using AdminPlatform.Modules.Customer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Customer.Infrastructure.Configurations;

internal sealed class PhoneVerificationConfiguration : IEntityTypeConfiguration<PhoneVerification>
{
    public void Configure(EntityTypeBuilder<PhoneVerification> builder)
    {
        builder.ToTable("phone_verifications");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Phone).HasMaxLength(32).IsRequired();
        builder.HasIndex(v => v.Phone).IsUnique();

        builder.Property(v => v.FullName).HasMaxLength(256).IsRequired();
        builder.Property(v => v.Email).HasMaxLength(256).IsRequired();
        builder.Property(v => v.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(v => v.CodeHash).HasMaxLength(128).IsRequired();
    }
}
