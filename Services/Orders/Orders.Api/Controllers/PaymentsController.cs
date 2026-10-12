using Microsoft.AspNetCore.Mvc;
using Orders.Bll.DTO;
using Orders.Bll.Services;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet("order/{orderId:long}")]
    public async Task<ActionResult<PaymentRecordDto>> GetByOrderId(long orderId, CancellationToken ct)
    {
        var payment = await paymentService.GetByOrderIdAsync(orderId, ct);
        if (payment == null)
            return NotFound();

        return Ok(payment);
    }

    [HttpPost]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request, CancellationToken ct)
    {
        await paymentService.ProcessPaymentAsync(request.OrderId, request.Amount, request.PaymentMethod, ct);
        return Ok();
    }

    [HttpPost("order/{orderId:long}/refund")]
    public async Task<IActionResult> Refund(long orderId, CancellationToken ct)
    {
        await paymentService.RefundPaymentAsync(orderId, ct);
        return NoContent();
    }
}