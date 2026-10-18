using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class ProductTagRepository(AppDbContext context)
    : GenericRepository<ProductTag>(context), IProductTagRepository
{
    // M:N через проміжну сутність і її власне поле AddedDate
    public async Task<IReadOnlyList<Product>> GetRecentlyAddedAsync(
        string tagName, DateTime since, CancellationToken ct = default)
        => await Set
            .Where(pt => pt.Tag.TagName == tagName && pt.AddedDate >= since)
            .OrderByDescending(pt => pt.AddedDate)
            .Select(pt => pt.Product)
            .AsNoTracking()
            .ToListAsync(ct);

    public Task<ProductTag?> GetAsync(int productId, int tagId, CancellationToken ct = default)
    => Set.FirstOrDefaultAsync(pt => pt.ProductId == productId && pt.TagId == tagId, ct);
}