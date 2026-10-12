using Microsoft.AspNetCore.Mvc;
using Orders.Bll.DTO;
using Orders.Bll.Services;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProductDto>> GetById(long id, CancellationToken ct)
    {
        var product = await productService.GetByIdAsync(id, ct);
        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll(CancellationToken ct)
    {
        var products = await productService.GetAllAsync(ct);
        return Ok(products);
    }

    [HttpGet("{id:long}/check-stock")]
    public async Task<ActionResult<bool>> CheckStock(long id, [FromQuery] int units, CancellationToken ct)
    {
        var inStock = await productService.CheckStockAsync(id, units, ct);
        return Ok(inStock);
    }
}