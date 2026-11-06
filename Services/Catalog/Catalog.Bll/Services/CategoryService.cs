using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Catalog.Bll.Caching;
using Catalog.Bll.DTO;
using Catalog.Dal;
using Catalog.Domain;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Catalog.Bll.Services;

public class CategoryService(IUnitOfWork uow, IMapper mapper, ILogger<CategoryService> logger, ICacheService cache) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct)
    {
        var result = await cache.GetOrCreateAsync<IReadOnlyList<CategoryDto>>(CatalogCache.Categories, async token =>
        {
            var categories = await uow.Categories.GetAllAsync(token);
            return mapper.Map<List<CategoryDto>>(categories);
        }, CatalogCachePolicies.Reference, ct);

        return result!;
    }

    public async Task<CategoryWithProductsDto> GetWithProductsAsync(int id, CancellationToken ct)
    {
        var dto = await cache.GetOrCreateAsync<CategoryWithProductsDto>(CatalogCache.CategoryWithProducts(id), async token =>
        {
            var category = await uow.Categories.GetWithProductsAsync(id, token);
            return category is null ? null : mapper.Map<CategoryWithProductsDto>(category);
        }, CatalogCachePolicies.List, ct);

        return dto ?? throw new EntityNotFoundException(nameof(Category), id);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto input, CancellationToken ct)
    {
        var name = input.CategoryName?.Trim() ?? "";
        if (await uow.Categories.ExistsByNameAsync(name, null, ct))
            throw new ConflictException($"Category '{name}' already exists");

        var category = new Category(name, input.Descrip);  
        await uow.Categories.AddAsync(category, ct);
        await uow.SaveChangesAsync(ct);

        cache.Remove(CatalogCache.Categories, "CategoryCreated");

        logger.LogInformation("Category {CategoryId} created", category.CategoryId);

        return mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto input, CancellationToken ct)
    {
        var category = await uow.Categories.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Category), id);

        var name = input.CategoryName?.Trim() ?? "";
        if (await uow.Categories.ExistsByNameAsync(name, id, ct))
            throw new ConflictException($"Category '{name}' already exists");

        category.Update(name, input.Descrip);
        await uow.SaveChangesAsync(ct);

        cache.Remove(CatalogCache.Categories, "CategoryUpdated");
        cache.RemoveGroup(CatalogCache.ListsGroup, "CategoryUpdated");
        cache.RemoveGroup(CatalogCache.ProductCardsGroup, "CategoryUpdated");

        logger.LogInformation("Category {CategoryId} updated", id);

        return mapper.Map<CategoryDto>(category);
    }
}
