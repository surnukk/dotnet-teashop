using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages", t =>
            t.HasCheckConstraint("CK_ProductImages_Url",
                "len(ltrim(rtrim([ImageUrl]))) > 0"));

        builder.HasKey(i => i.ImageId);

        builder.Property(i => i.ImageUrl).IsRequired().HasMaxLength(500);

        builder.HasIndex(i => i.ProductId, "UX_ProductImages_Main")
               .IsUnique()
               .HasFilter("[IsMain] = 1");
    }
}