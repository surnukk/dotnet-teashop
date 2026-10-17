using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class ProductTagConfiguration : IEntityTypeConfiguration<ProductTag>
{
    public void Configure(EntityTypeBuilder<ProductTag> builder)
    {
        builder.ToTable("ProductTags");

        builder.HasKey(pt => new { pt.ProductId, pt.TagId }); // складений ключ

        builder.Property(pt => pt.AddedDate).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(pt => pt.Product)
               .WithMany(p => p.ProductTags)
               .HasForeignKey(pt => pt.ProductId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pt => pt.Tag)
               .WithMany(t => t.ProductTags)
               .HasForeignKey(pt => pt.TagId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pt => pt.TagId).HasDatabaseName("IX_ProductTags_TagId");
    }
}