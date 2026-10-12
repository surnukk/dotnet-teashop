using Microsoft.AspNetCore.Mvc;
using Orders.Bll.DTO;
using Orders.Bll.Services;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<ActionResult<CustomerDto>> GetById(long id, CancellationToken ct)
    {
        var customer = await customerService.GetByIdAsync(id, ct);
        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpGet("by-email")]
    public async Task<ActionResult<CustomerDto>> GetByEmail([FromQuery] string email, CancellationToken ct)
    {
        var customer = await customerService.GetByEmailAsync(email, ct);
        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll(CancellationToken ct)
    {
        var customers = await customerService.GetAllAsync(ct);
        return Ok(customers);
    }

    [HttpPost]
    public async Task<ActionResult<long>> Create(UpsertCustomerRequest request, CancellationToken ct)
    {
        var customerId = await customerService.CreateCustomerAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = customerId }, customerId);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpsertCustomerRequest request, CancellationToken ct)
    {
        await customerService.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await customerService.DeleteAsync(id, ct);
        return NoContent();
    }
}