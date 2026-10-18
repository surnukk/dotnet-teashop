using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Catalog.Bll.DTO;
using Catalog.Dal;
using Catalog.Domain;

namespace Catalog.Bll.Services;

public class CategoryService(IUnitOfWork uow, IMapper mapper) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct)
    {
        var categories = await uow.Categories.GetAllAsync(ct);
        return mapper.Map<List<CategoryDto>>(categories);
    }

    // Explicit Loading: категорія окремим запитом, товари другим
    public async Task<CategoryWithProductsDto> GetWithProductsAsync(int id, CancellationToken ct)
    {
        var category = await uow.Categories.GetWithProductsAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Category), id);
        return mapper.Map<CategoryWithProductsDto>(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto input, CancellationToken ct)
    {
        var name = input.CategoryName?.Trim() ?? "";
        if (await uow.Categories.ExistsByNameAsync(name, null, ct))
            throw new ConflictException($"Category '{name}' already exists");

        var category = new Category(name, input.Descrip);   // порожню назву відхилить сутність
        await uow.Categories.AddAsync(category, ct);
        await uow.SaveChangesAsync(ct);
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
        return mapper.Map<CategoryDto>(category);
    }
}