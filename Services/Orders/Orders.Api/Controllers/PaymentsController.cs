using Microsoft.AspNetCore.Mvc;
using Orders.Bll.DTO;
using Orders.Bll.Services;
using Microsoft.AspNetCore.Authorization;
using Platform.ServiceDefaults.Security;

namespace Orders.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet("order/{orderId:long}")]
    [RequirePermission("orders:read")]
    [ProducesResponseType(typeof(PaymentRecordDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentRecordDto>> GetByOrderId(long orderId, CancellationToken ct)
    {
        var payment = await paymentService.GetByOrderIdAsync(orderId, ct);
        if (payment == null)
            return NotFound();

        return Ok(payment);
    }

    [HttpPost]
    [RequirePermission("orders:create")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request, CancellationToken ct)
    {
        await paymentService.ProcessPaymentAsync(request.OrderId, request.Amount, request.PaymentMethod, ct);
        return NoContent();
    }

    [HttpPost("order/{orderId:long}/refund")]
    [RequirePermission("orders:approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Refund(long orderId, CancellationToken ct)
    {
        await paymentService.RefundPaymentAsync(orderId, ct);
        return NoContent();
    }
}