using AdminPlatform.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Organization.Infrastructure.Configurations;

internal sealed class UserDepartmentConfiguration : IEntityTypeConfiguration<UserDepartment>
{
    public void Configure(EntityTypeBuilder<UserDepartment> builder)
    {
        builder.ToTable("user_departments");
        builder.HasKey(ud => ud.Id);

        // UserId is a plain column, not an EF navigation to Identity's User (architecture assumption #6).
        builder.Property(ud => ud.UserId).IsRequired();
        builder.Property(ud => ud.DepartmentId).IsRequired();
        builder.HasIndex(ud => new { ud.UserId, ud.DepartmentId }).IsUnique();

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(ud => ud.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
