using Catalog.Bll.DTO;
using Catalog.Bll.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Platform.ServiceDefaults.Security;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController(ICategoryService service) : ControllerBase
{
    /// <summary>Список категорій.</summary>
    [HttpGet]
    [RequirePermission("products:read")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken ct)
        => Ok(await service.GetAllAsync(ct));

    /// <summary>Категорія разом із товарами (Explicit Loading).</summary>
    [HttpGet("{id:int}")]
    [RequirePermission("products:read")]
    [ProducesResponseType(typeof(CategoryWithProductsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryWithProductsDto>> GetById(int id, CancellationToken ct)
        => Ok(await service.GetWithProductsAsync(id, ct));

    /// <summary>Створити категорію. Назва має бути унікальною.</summary>
    [HttpPost]
    [RequirePermission("products:create")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryDto input, CancellationToken ct)
    {
        var created = await service.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.CategoryId }, created);
    }

    /// <summary>Змінити назву й опис категорії.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("products:update")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Update(int id, UpdateCategoryDto input, CancellationToken ct)
        => Ok(await service.UpdateAsync(id, input, ct));
}