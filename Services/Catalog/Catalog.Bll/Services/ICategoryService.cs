using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Catalog.Bll.DTO;

namespace Catalog.Bll.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct);
    Task<CategoryWithProductsDto> GetWithProductsAsync(int id, CancellationToken ct);
    Task<CategoryDto> CreateAsync(CreateCategoryDto input, CancellationToken ct);
    Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto input, CancellationToken ct);
}