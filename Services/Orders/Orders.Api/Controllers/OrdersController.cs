using Microsoft.AspNetCore.Mvc;
using Orders.Bll.DTO;
using Orders.Bll.Services;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<ActionResult<OrderDto>> GetById(long id, CancellationToken ct)
    {
        var order = await orderService.GetByIdAsync(id, ct);
        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpPost("{id:long}/ship")]
    public async Task<IActionResult> Ship(long id, CancellationToken ct)
    {
        await orderService.ShipOrderAsync(id, ct);
        return NoContent();
    }

    [HttpGet("customer/{customerId:long}")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetByCustomer(long customerId, CancellationToken ct)
    {
        var orders = await orderService.GetByCustomerIdAsync(customerId, ct);
        return Ok(orders);
    }

    [HttpPost]
    public async Task<ActionResult<long>> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var orderId = await orderService.CreateOrderAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = orderId }, orderId);
    }

    [HttpPost("{id:long}/confirm")]
    public async Task<IActionResult> Confirm(long id, CancellationToken ct)
    {
        await orderService.ConfirmOrderAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        await orderService.CancelOrderAsync(id, ct);
        return NoContent();
    }
}