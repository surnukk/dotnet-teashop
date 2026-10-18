using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class TagRepository(AppDbContext context)
    : GenericRepository<Tag>(context), ITagRepository
{
    public Task<bool> ExistsByNameAsync(string tagName, CancellationToken ct = default)
        => Set.AnyAsync(t => t.TagName == tagName, ct);
}