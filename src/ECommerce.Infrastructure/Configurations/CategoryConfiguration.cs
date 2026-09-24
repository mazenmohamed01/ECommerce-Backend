using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.ImageUrl)
            .HasMaxLength(2048);

        builder.Property(c => c.ImagePublicId)
            .HasMaxLength(512);

        builder.Property(c => c.SortOrder)
            .HasDefaultValue(0);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        // PostgreSQL optimistic concurrency via xmin (using EF Core 7+ standard approach)
        builder.Property<uint>("Version")
            .IsRowVersion();

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired(false);

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(c => c.Slug)
            .IsUnique()
            .HasDatabaseName("uix_category_slug");

        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("ix_category_is_active");

        // ── Relationships ─────────────────────────────────────────────────────
        builder.HasMany(c => c.Products)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Categories");
    }
}
