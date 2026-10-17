using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", t =>
        {
            t.HasCheckConstraint("CK_Products_Name", "len(ltrim(rtrim([ProductName]))) > 0");
            t.HasCheckConstraint("CK_Products_Price", "[Price] > 0");
            t.HasCheckConstraint("CK_Products_Stock", "[StockQuantity] >= 0");
        });

        builder.HasKey(p => p.ProductId);

        builder.Property(p => p.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");

        builder.HasIndex(p => p.ProductName);
        builder.HasIndex(p => new { p.CategoryId, p.IsAvailable })
               .IncludeProperties(p => new { p.ProductName, p.Price })
               .HasDatabaseName("IX_Products_CategoryId_IsAvailable");

        builder.HasOne(p => p.Category) // N:1
               .WithMany(c => c.Products)
               .HasForeignKey(p => p.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Detail) // 1:1
               .WithOne()
               .HasForeignKey<ProductDetail>(d => d.ProductId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Images) // 1:N
               .WithOne()
               .HasForeignKey(i => i.ProductId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}