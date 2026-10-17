using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", t =>
            t.HasCheckConstraint("CK_Categories_Name",
                "len(ltrim(rtrim([CategoryName]))) > 0"));

        builder.HasKey(c => c.CategoryId);

        builder.Property(c => c.CategoryName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Descrip).HasMaxLength(500);

        builder.HasIndex(c => c.CategoryName).IsUnique();
    }
}