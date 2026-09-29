using AdminPlatform.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Catalog.Infrastructure.Configurations;

internal sealed class SalesMenuConfiguration : IEntityTypeConfiguration<SalesMenu>
{
    public void Configure(EntityTypeBuilder<SalesMenu> builder)
    {
        builder.ToTable("sales_menus");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.RowVersion).IsRowVersion();

        builder.Property(m => m.Code).HasMaxLength(SalesMenu.MaxCodeLength).IsRequired();
        builder.HasIndex(m => m.Code).IsUnique();
        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
    }
}

internal sealed class SalesMenuProductConfiguration : IEntityTypeConfiguration<SalesMenuProduct>
{
    public void Configure(EntityTypeBuilder<SalesMenuProduct> builder)
    {
        builder.ToTable("sales_menu_products", table =>
            table.HasCheckConstraint("ck_sales_menu_products_price_override_non_negative", "price_override IS NULL OR price_override >= 0"));
        builder.HasKey(mp => mp.Id);
        builder.Property(mp => mp.RowVersion).IsRowVersion();

        builder.Property(mp => mp.PriceOverride).HasPrecision(12, 2);

        builder.HasOne<SalesMenu>()
            .WithMany()
            .HasForeignKey(mp => mp.SalesMenuId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(mp => mp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(mp => new { mp.SalesMenuId, mp.ProductId }).IsUnique();
        builder.HasIndex(mp => mp.ProductId);
    }
}
