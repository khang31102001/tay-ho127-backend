using AdminPlatform.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminPlatform.Modules.Catalog.Infrastructure.Configurations;

internal sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions", table =>
        {
            table.HasCheckConstraint("ck_promotions_value_positive", "value > 0");
            table.HasCheckConstraint("ck_promotions_max_discount_non_negative", "max_discount_amount IS NULL OR max_discount_amount >= 0");
            table.HasCheckConstraint("ck_promotions_minimum_order_non_negative", "minimum_order_amount IS NULL OR minimum_order_amount >= 0");
            table.HasCheckConstraint("ck_promotions_usage_limit_positive", "usage_limit IS NULL OR usage_limit >= 1");
            table.HasCheckConstraint("ck_promotions_usage_count_non_negative", "usage_count >= 0");
        });
        builder.HasKey(p => p.Id);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.Property(p => p.Code).HasMaxLength(Promotion.MaxCodeLength).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();
        builder.Property(p => p.Name).HasMaxLength(Promotion.MaxNameLength).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(Promotion.MaxDescriptionLength);
        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(p => p.Value).HasPrecision(12, 2);
        builder.Property(p => p.MaxDiscountAmount).HasPrecision(12, 2);
        builder.Property(p => p.MinimumOrderAmount).HasPrecision(12, 2);

        builder.HasMany(p => p.Products)
            .WithOne()
            .HasForeignKey(link => link.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Products).HasField("_products");

        builder.HasMany(p => p.Categories)
            .WithOne()
            .HasForeignKey(link => link.PromotionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Categories).HasField("_categories");
    }
}

internal sealed class PromotionProductConfiguration : IEntityTypeConfiguration<PromotionProduct>
{
    public void Configure(EntityTypeBuilder<PromotionProduct> builder)
    {
        builder.ToTable("promotion_products");
        builder.HasKey(link => new { link.PromotionId, link.ProductId });

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(link => link.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(link => link.ProductId);
    }
}

internal sealed class PromotionCategoryConfiguration : IEntityTypeConfiguration<PromotionCategory>
{
    public void Configure(EntityTypeBuilder<PromotionCategory> builder)
    {
        builder.ToTable("promotion_categories");
        builder.HasKey(link => new { link.PromotionId, link.CategoryId });

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(link => link.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(link => link.CategoryId);
    }
}
