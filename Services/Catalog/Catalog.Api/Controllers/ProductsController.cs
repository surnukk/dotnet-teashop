using Catalog.Api.Metrics;
using Catalog.Bll.DTO;
using Catalog.Bll.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Policy = "products.read")]
public class ProductsController(IProductService service, CatalogMetrics metrics) : ControllerBase
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

        var result = await metrics.MeasureAsync("list", () => service.GetListAsync(query, ct));
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();
        return Ok(result);
    }

    /// <summary>Картка товару з деталями, картинками й тегами (Eager Loading).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailsDto>> GetById(int id, CancellationToken ct)
        => Ok(await metrics.MeasureAsync("get_by_id", () => service.GetByIdAsync(id, ct)));

    /// <summary>Створити товар. Назва має бути унікальною.</summary>
    [HttpPost]
    [Authorize(Policy = "products.write")]
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

        var created = await metrics.MeasureAsync("create", () => service.CreateAsync(input, ct));
        metrics.ProductCreated();
        return CreatedAtAction(nameof(GetById), new { id = created.ProductId }, created);
    }

    /// <summary>Змінити ціну товару.</summary>
    [HttpPut("{id:int}/price")]
    [Authorize(Policy = "products.write")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> ChangePrice(
        int id, ChangePriceDto input, CancellationToken ct)
    {
        var product = await metrics.MeasureAsync("change_price", () => service.ChangePriceAsync(id, input, ct));
        metrics.ProductUpdated("price");
        return Ok(product);
    }

    /// <summary>Перейменувати товар (публікує ProductRenamedEvent).</summary>
    [HttpPut("{id:int}/name")]
    [Authorize(Policy = "products.write")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Rename(
        int id, RenameProductDto input, CancellationToken ct)
    {
        var product = await metrics.MeasureAsync("rename", () => service.RenameAsync(id, input, ct));
        metrics.ProductUpdated("name");
        return Ok(product);
    }

    /// <summary>Поповнити залишок.</summary>
    [HttpPost("{id:int}/stock/add")]
    [Authorize(Policy = "products.write")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> AddStock(
        int id, StockChangeDto input, CancellationToken ct)
    {
        var product = await metrics.MeasureAsync("add_stock", () => service.AddStockAsync(id, input, ct));
        metrics.ProductUpdated("stock_added");
        return Ok(product);
    }

    /// <summary>Списати залишок. Не можна списати більше, ніж є.</summary>
    [HttpPost("{id:int}/stock/remove")]
    [Authorize(Policy = "products.write")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> RemoveStock(
        int id, StockChangeDto input, CancellationToken ct)
    {
        var product = await metrics.MeasureAsync("remove_stock", () => service.RemoveStockAsync(id, input, ct));
        metrics.ProductUpdated("stock_removed");
        return Ok(product);
    }

    /// <summary>Видалити товар.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "products.write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await metrics.MeasureCommandAsync("delete", () => service.DeleteAsync(id, ct));
        metrics.ProductDeleted();
        return NoContent();
    }

    /// <summary>Додати тег товару (запис у ProductTag, дату ставить база).</summary>
    [HttpPost("{id:int}/tags/{tagId:int}")]
    [Authorize(Policy = "products.write")]
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
    [Authorize(Policy = "products.write")]
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