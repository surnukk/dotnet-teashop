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
    }
}