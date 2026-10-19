using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags", t =>
            t.HasCheckConstraint("CK_Tags_Name", "len(ltrim(rtrim([TagName]))) > 0"));

        builder.HasKey(t => t.TagId);

        builder.Property(t => t.TagName).IsRequired().HasMaxLength(50);

        builder.HasIndex(t => t.TagName).IsUnique();

        builder.HasData(
            new Tag { TagId = 1, TagName = "Organic" },
            new Tag { TagId = 2, TagName = "Bestseller" },
            new Tag { TagId = 3, TagName = "New" },
            new Tag { TagId = 4, TagName = "Premium" },
            new Tag { TagId = 5, TagName = "Limited Edition" });
    }
}