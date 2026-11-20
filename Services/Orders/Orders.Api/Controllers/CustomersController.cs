using Microsoft.AspNetCore.Mvc;
using Orders.Bll.DTO;
using Orders.Bll.Services;
using Microsoft.AspNetCore.Authorization;
using Platform.ServiceDefaults.Security;

namespace Orders.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet("{id:long}")]
    [RequirePermission("orders:read")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerDto>> GetById(long id, CancellationToken ct)
    {
        var customer = await customerService.GetByIdAsync(id, ct);
        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpGet("by-email")]
    [RequirePermission("orders:read")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerDto>> GetByEmail([FromQuery] string email, CancellationToken ct)
    {
        var customer = await customerService.GetByEmailAsync(email, ct);
        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpGet]
    [RequirePermission("orders:read")]
    [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll(CancellationToken ct)
    {
        var customers = await customerService.GetAllAsync(ct);
        return Ok(customers);
    }

    [HttpPost]
    [RequirePermission("orders:create")]
    [ProducesResponseType(typeof(long), StatusCodes.Status201Created)]
    public async Task<ActionResult<long>> Create(UpsertCustomerRequest request, CancellationToken ct)
    {
        var customerId = await customerService.CreateCustomerAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = customerId }, customerId);
    }

    [HttpPut("{id:long}")]
    [RequirePermission("orders:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(long id, UpsertCustomerRequest request, CancellationToken ct)
    {
        await customerService.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission("orders:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await customerService.DeleteAsync(id, ct);
        return NoContent();
    }
}