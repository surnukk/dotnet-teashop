using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Api.Metrics;
using Orders.Bll.DTO;
using Orders.Bll.Services;
using Platform.ServiceDefaults.Security;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class OrdersController(
    IOrderService orderService,
    ICustomerService customerService,
    OrderMetrics metrics) : ControllerBase
{
    private async Task<bool> OwnsCustomerAsync(long customerId, CancellationToken ct)
    {
        if (User.IsAdmin()) return true;

        var email = User.GetEmail();
        if (string.IsNullOrEmpty(email)) return false;

        var me = await customerService.GetByEmailAsync(email, ct);
        return me is not null && me.CustomerID == customerId;
    }

    [HttpGet("{id:long}")]
    [RequirePermission("orders:read")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderDto>> GetById(long id, CancellationToken ct)
    {
        var order = await metrics.MeasureAsync("get_by_id", () => orderService.GetByIdAsync(id, ct));
        if (order == null)
            return NotFound();

        if (!await OwnsCustomerAsync(order.CustomerID, ct))
            return Forbid();

        return Ok(order);
    }

    [HttpPost("{id:long}/ship")]
    [RequirePermission("orders:approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Ship(long id, CancellationToken ct)
    {
        await metrics.MeasureCommandAsync("ship", () => orderService.ShipOrderAsync(id, ct));
        metrics.OrderShipped();
        return NoContent();
    }

    [HttpGet("customer/{customerId:long}")]
    [RequirePermission("orders:read")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetByCustomer(long customerId, CancellationToken ct)
    {
        if (!await OwnsCustomerAsync(customerId, ct))
            return Forbid();

        var orders = await metrics.MeasureAsync("get_by_customer", () => orderService.GetByCustomerIdAsync(customerId, ct));
        return Ok(orders);
    }

    [HttpPost]
    [RequirePermission("orders:create")]
    [ProducesResponseType(typeof(long), StatusCodes.Status201Created)]
    public async Task<ActionResult<long>> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        if (!await OwnsCustomerAsync(request.CustomerId, ct))
            return Forbid();

        var orderId = await metrics.MeasureAsync("create", () => orderService.CreateOrderAsync(request, ct));
        metrics.OrderCreated("Submitted", request.PromotionId is not null);
        return CreatedAtAction(nameof(GetById), new { id = orderId }, orderId);
    }

    [HttpPost("{id:long}/confirm")]
    [RequirePermission("orders:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Confirm(long id, CancellationToken ct)
    {
        var order = await orderService.GetByIdAsync(id, ct);
        if (order == null) return NotFound();
        if (!await OwnsCustomerAsync(order.CustomerID, ct)) return Forbid();

        await metrics.MeasureCommandAsync("confirm", () => orderService.ConfirmOrderAsync(id, ct));
        metrics.OrderConfirmed();
        return NoContent();
    }

    [HttpPost("{id:long}/cancel")]
    [RequirePermission("orders:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var order = await orderService.GetByIdAsync(id, ct);
        if (order == null) return NotFound();
        if (!await OwnsCustomerAsync(order.CustomerID, ct)) return Forbid();

        await metrics.MeasureCommandAsync("cancel", () => orderService.CancelOrderAsync(id, ct));
        metrics.OrderCancelled();
        return NoContent();
    }

    [HttpGet]
    [RequirePermission("orders:read")]
    public ActionResult<IEnumerable<OrderDto>> GetAll() => Ok(Array.Empty<OrderDto>());

    [HttpPut("{id:long}")]
    [RequirePermission("orders:update")]
    public async Task<IActionResult> Update(long id, CancellationToken ct)
    {
        var order = await orderService.GetByIdAsync(id, ct);
        return order is null ? NotFound() : Ok();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission("orders:delete")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var order = await orderService.GetByIdAsync(id, ct);
        if (order is null) return NotFound();
        await orderService.CancelOrderAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/approve")]
    [RequirePermission("orders:approve")]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
    {
        var order = await orderService.GetByIdAsync(id, ct);
        if (order is null) return NotFound();
        await orderService.ConfirmOrderAsync(id, ct);
        return NoContent();
    }
}