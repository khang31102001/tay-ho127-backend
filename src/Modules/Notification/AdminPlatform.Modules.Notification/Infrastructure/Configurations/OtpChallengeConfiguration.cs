using AdminPlatform.Modules.Notification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Notification.Infrastructure.Configurations;

internal sealed class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.ToTable("otp_challenges");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.RowVersion).IsRowVersion();

        builder.Property(c => c.Purpose).HasMaxLength(OtpChallenge.MaxPurposeLength).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(OtpChallenge.MaxPhoneLength).IsRequired();
        builder.Property(c => c.CodeHash).HasMaxLength(OtpChallenge.MaxCodeHashLength).IsRequired();
        builder.Property(c => c.CreatedAtUtc).IsRequired();
        builder.Property(c => c.ExpiresAtUtc).IsRequired();
        builder.Property(c => c.AttemptCount).IsRequired();
        builder.Property(c => c.RequestIp).HasMaxLength(OtpChallenge.MaxRequestIpLength);

        // The sending limits count recent rows per phone / per address / overall, so each lookup needs an index.
        builder.HasIndex(c => new { c.Phone, c.CreatedAtUtc });
        builder.HasIndex(c => new { c.RequestIp, c.CreatedAtUtc });
        builder.HasIndex(c => c.CreatedAtUtc);
    }
}
