using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class ProductRepository(AppDbContext context)
    : GenericRepository<Product>(context), IProductRepository
{
    // Eager Loading: картка товару з усім пов'язаним одним запитом
    public Task<Product?> GetWithDetailsAsync(int id, CancellationToken ct = default)
        => Set.Include(p => p.Category)
              .Include(p => p.Detail)
              .Include(p => p.Images)
              .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
              .FirstOrDefaultAsync(p => p.ProductId == id, ct);

    public Task<bool> ExistsByNameAsync(string productName, CancellationToken ct = default)
        => Set.AnyAsync(p => p.ProductName == productName, ct);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
    ProductFilter f, CancellationToken ct = default)
    {
        IQueryable<Product> query = Set.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var search = f.Search.Trim();
            query = query.Where(p => p.ProductName.Contains(search));
        }
        if (f.CategoryId is not null)
            query = query.Where(p => p.CategoryId == f.CategoryId);
        if (f.TagId is not null)
            query = query.Where(p => p.ProductTags.Any(pt => pt.TagId == f.TagId));
        if (f.MinPrice is not null)
            query = query.Where(p => p.Price >= f.MinPrice);
        if (f.MaxPrice is not null)
            query = query.Where(p => p.Price <= f.MaxPrice);
        if (f.IsAvailable is not null)
            query = query.Where(p => p.IsAvailable == f.IsAvailable);

        var total = await query.CountAsync(ct);

        query = (f.SortBy, f.Descending) switch
        {
            ("price", true) => query.OrderByDescending(p => p.Price).ThenBy(p => p.ProductId),
            ("price", false) => query.OrderBy(p => p.Price).ThenBy(p => p.ProductId),
            ("stock", true) => query.OrderByDescending(p => p.StockQuantity).ThenBy(p => p.ProductId),
            ("stock", false) => query.OrderBy(p => p.StockQuantity).ThenBy(p => p.ProductId),
            (_, true) => query.OrderByDescending(p => p.ProductName).ThenBy(p => p.ProductId),
            _ => query.OrderBy(p => p.ProductName).ThenBy(p => p.ProductId)
        };

        var items = await query
            .Skip((f.Page - 1) * f.PageSize)      
            .Take(f.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}