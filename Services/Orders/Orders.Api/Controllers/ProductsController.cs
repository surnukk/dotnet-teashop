using Microsoft.AspNetCore.Mvc;
using Orders.Bll.DTO;
using Orders.Bll.Services;
using Microsoft.AspNetCore.Authorization;
using Platform.ServiceDefaults.Security;

namespace Orders.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders/products")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet("{id:long}")]
    [RequirePermission("orders:read")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductDto>> GetById(long id, CancellationToken ct)
    {
        var product = await productService.GetByIdAsync(id, ct);
        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [HttpGet]
    [RequirePermission("orders:read")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll(CancellationToken ct)
    {
        var products = await productService.GetAllAsync(ct);
        return Ok(products);
    }
}