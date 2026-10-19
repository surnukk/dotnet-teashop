using Catalog.Bll.DTO;
using Catalog.Bll.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(IProductService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetList(
        [FromQuery] ProductQuery query,
        [FromServices] IValidator<ProductQuery> validator,
        CancellationToken ct)
    {
        if (await ValidateAsync(validator, query, ct) is { } invalid) return invalid;

        var result = await service.GetListAsync(query, ct);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();
        return Ok(result);
    }

    /// <summary>Картка товару з деталями, картинками й тегами (Eager Loading).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailsDto>> GetById(int id, CancellationToken ct)
        => Ok(await service.GetByIdAsync(id, ct));

    /// <summary>Створити товар. Назва має бути унікальною.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Create(
        CreateProductDto input,
        [FromServices] IValidator<CreateProductDto> validator,
        CancellationToken ct)
    {
        if (await ValidateAsync(validator, input, ct) is { } invalid) return invalid;

        var created = await service.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, created);
    }

    /// <summary>Змінити ціну товару.</summary>
    [HttpPut("{id:int}/price")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> ChangePrice(
        int id, ChangePriceDto input, CancellationToken ct)
        => Ok(await service.ChangePriceAsync(id, input, ct));

    /// <summary>Поповнити залишок.</summary>
    [HttpPost("{id:int}/stock/add")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> AddStock(
        int id, StockChangeDto input, CancellationToken ct)
        => Ok(await service.AddStockAsync(id, input, ct));

    /// <summary>Списати залишок. Не можна списати більше, ніж є.</summary>
    [HttpPost("{id:int}/stock/remove")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> RemoveStock(
        int id, StockChangeDto input, CancellationToken ct)
        => Ok(await service.RemoveStockAsync(id, input, ct));

    /// <summary>Видалити товар.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Додати тег товару (запис у ProductTag, дату ставить база).</summary>
    [HttpPost("{id:int}/tags/{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddTag(int id, int tagId, CancellationToken ct)
    {
        await service.AddTagAsync(id, tagId, ct);
        return NoContent();
    }

    /// <summary>Прибрати тег із товару.</summary>
    [HttpDelete("{id:int}/tags/{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTag(int id, int tagId, CancellationToken ct)
    {
        await service.RemoveTagAsync(id, tagId, ct);
        return NoContent();
    }

    // явний виклик FluentValidation: помилки у форматі ValidationProblemDetails
    private async Task<ActionResult?> ValidateAsync<T>(
        IValidator<T> validator, T instance, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(instance, ct);
        return result.IsValid
            ? null
            : ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));
    }
}