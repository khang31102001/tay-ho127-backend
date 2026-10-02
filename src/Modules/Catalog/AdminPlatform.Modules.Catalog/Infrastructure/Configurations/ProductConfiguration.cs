using AdminPlatform.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Catalog.Infrastructure.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", table =>
        {
            table.HasCheckConstraint("ck_products_price_non_negative", "price >= 0");
            table.HasCheckConstraint("ck_products_old_price_non_negative", "old_price IS NULL OR old_price >= 0");
            table.HasCheckConstraint("ck_products_rating_range", "rating IS NULL OR (rating >= 0 AND rating <= 5)");
        });
        builder.HasKey(p => p.Id);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(Slug.MaxLength).IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.Price).HasPrecision(12, 2);
        builder.Property(p => p.OldPrice).HasPrecision(12, 2);
        builder.Property(p => p.Description).HasMaxLength(4000);
        builder.Property(p => p.Badge).HasMaxLength(64);
        builder.Property(p => p.Rating).HasPrecision(3, 1);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.CategoryId);

        builder.HasMany(p => p.Media)
            .WithOne()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Media).HasField("_media");

        builder.HasMany(p => p.ModifierGroups)
            .WithOne()
            .HasForeignKey(g => g.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.ModifierGroups).HasField("_modifierGroups");
    }
}

internal sealed class ProductMediaConfiguration : IEntityTypeConfiguration<ProductMedia>
{
    public void Configure(EntityTypeBuilder<ProductMedia> builder)
    {
        builder.ToTable("product_media");
        builder.HasKey(m => new { m.ProductId, m.MediaId });
        builder.Property(m => m.MediaId).HasMaxLength(ProductMedia.MaxMediaIdLength);
    }
}

internal sealed class ProductModifierGroupConfiguration : IEntityTypeConfiguration<ProductModifierGroup>
{
    public void Configure(EntityTypeBuilder<ProductModifierGroup> builder)
    {
        builder.ToTable("product_modifier_groups");
        builder.HasKey(g => new { g.ProductId, g.ModifierGroupId });

        builder.HasOne<ModifierGroup>()
            .WithMany()
            .HasForeignKey(g => g.ModifierGroupId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(g => g.ModifierGroupId);
    }
}
