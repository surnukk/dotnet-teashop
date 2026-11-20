using Catalog.Bll.DTO;
using Catalog.Bll.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Platform.ServiceDefaults.Security;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize]
public class TagsController(ITagService service) : ControllerBase
{
    [HttpGet("{tagName}/products")]
    [RequirePermission("products:read")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetRecentProducts(
        string tagName, [FromQuery] DateTime? since, CancellationToken ct)
        => Ok(await service.GetRecentlyAddedProductsAsync(
            tagName, since ?? DateTime.UtcNow.AddDays(-30), ct));

    /// <summary>Список усіх тегів.</summary>
    [HttpGet]
    [RequirePermission("products:read")]
    [ProducesResponseType(typeof(IReadOnlyList<TagDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> GetAll(CancellationToken ct)
        => Ok(await service.GetAllAsync(ct));

    /// <summary>Тег за Id.</summary>
    [HttpGet("{id:int}")]
    [RequirePermission("products:read")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagDto>> GetById(int id, CancellationToken ct)
        => Ok(await service.GetByIdAsync(id, ct));

    /// <summary>Створити тег. Назва має бути унікальною.</summary>
    [HttpPost]
    [RequirePermission("products:create")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagDto>> Create(CreateTagDto input, CancellationToken ct)
    {
        var created = await service.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.TagId }, created);
    }
}