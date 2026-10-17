using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class ProductDetailConfiguration : IEntityTypeConfiguration<ProductDetail>
{
    public void Configure(EntityTypeBuilder<ProductDetail> builder)
    {
        builder.ToTable("ProductDetails", t =>
            t.HasCheckConstraint("CK_ProductDetails_Weight", "[WeightGrams] > 0"));

        builder.HasKey(d => d.ProductId); // PK є водночас FK

        builder.Property(d => d.OriginCountry).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Region).HasMaxLength(150);
        builder.Property(d => d.TeaForm).IsRequired().HasMaxLength(50);
        builder.Property(d => d.TeaVariety).IsRequired().HasMaxLength(100);
        builder.Property(d => d.FlavorNotes).HasMaxLength(300);
    }
}
