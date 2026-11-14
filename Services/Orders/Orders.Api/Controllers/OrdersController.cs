using Microsoft.AspNetCore.Mvc;
using Orders.Api.Metrics;
using Orders.Bll.DTO;
using Orders.Bll.Services;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class OrdersController(IOrderService orderService, OrderMetrics metrics) : ControllerBase
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderDto>> GetById(long id, CancellationToken ct)
    {
        var order = await metrics.MeasureAsync("get_by_id", () => orderService.GetByIdAsync(id, ct));
        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpPost("{id:long}/ship")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Ship(long id, CancellationToken ct)
    {
        await metrics.MeasureCommandAsync("ship", () => orderService.ShipOrderAsync(id, ct));
        metrics.OrderShipped();
        return NoContent();
    }

    [HttpGet("customer/{customerId:long}")]
    [ProducesResponseType(typeof(long), StatusCodes.Status201Created)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetByCustomer(long customerId, CancellationToken ct)
    {
        var orders = await metrics.MeasureAsync("get_by_customer", () => orderService.GetByCustomerIdAsync(customerId, ct));
        return Ok(orders);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<long>> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var orderId = await metrics.MeasureAsync("create", () => orderService.CreateOrderAsync(request, ct));
        metrics.OrderCreated("Submitted", request.PromotionId is not null);
        return CreatedAtAction(nameof(GetById), new { id = orderId }, orderId);
    }

    [HttpPost("{id:long}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Confirm(long id, CancellationToken ct)
    {
        await metrics.MeasureCommandAsync("confirm", () => orderService.ConfirmOrderAsync(id, ct));
        metrics.OrderConfirmed();
        return NoContent();
    }

    [HttpPost("{id:long}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        await metrics.MeasureCommandAsync("cancel", () => orderService.CancelOrderAsync(id, ct));
        metrics.OrderCancelled();
        return NoContent();
    }
}