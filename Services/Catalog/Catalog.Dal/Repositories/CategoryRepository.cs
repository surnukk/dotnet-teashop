using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class CategoryRepository(AppDbContext context)
    : GenericRepository<Category>(context), ICategoryRepository
{
    // Explicit Loading: товари категорії довантажуються окремим запитом
    public async Task<Category?> GetWithProductsAsync(int id, CancellationToken ct = default)
    {
        var category = await Set.FirstOrDefaultAsync(c => c.CategoryId == id, ct);
        if (category is null) return null;

        await Context.Entry(category)
            .Collection(c => c.Products)
            .LoadAsync(ct); 
        return category;
    }

    public Task<bool> ExistsByNameAsync(string categoryName, int? excludeId = null, CancellationToken ct = default)
    => Set.AnyAsync(c => c.CategoryName == categoryName && c.CategoryId != excludeId, ct);
}