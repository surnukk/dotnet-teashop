using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Payments.Api.Controllers;

[ApiController]
[Route("api/billing/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private static readonly PaymentStub[] Samples =
    {
        new(1, 1001, 25.50m, "Paid"),
        new(2, 1002, 48.00m, "Pending"),
        new(3, 1003, 12.90m, "Refunded")
    };

    [HttpGet]
    [Authorize(Policy = "payments.read")]
    public ActionResult<IEnumerable<PaymentStub>> GetAll() => Ok(Samples);

    [HttpGet("{id:int}")]
    [Authorize(Policy = "payments.read")]
    public ActionResult<PaymentStub> GetById(int id)
    {
        var payment = Samples.FirstOrDefault(p => p.Id == id);
        return payment is null ? NotFound() : Ok(payment);
    }

    [HttpPost]
    [Authorize(Policy = "payments.write")]
    public ActionResult<PaymentStub> Create([FromBody] CreatePaymentRequest request)
    {
        var created = new PaymentStub(Samples.Length + 1, request.OrderId, request.Amount, "Pending");
        return Created($"/api/billing/payments/{created.Id}", created);
    }

    [HttpPost("{id:int}/refund")]
    [Authorize(Policy = "payments.write")]
    public IActionResult Refund(int id) => Accepted(new { paymentId = id, status = "RefundRequested" });

    // Діагностика: що бачить сервіс у токені та в заголовку від Gateway
    [HttpGet("whoami")]
    [Authorize(Policy = "payments.read")]
    public IActionResult WhoAmI() => Ok(new
    {
        sub = User.FindFirst("sub")?.Value,
        clientId = User.FindFirst("client_id")?.Value,
        xUserId = Request.Headers["X-User-Id"].ToString(),
        scopes = User.FindAll("scope").Select(c => c.Value)
    });
}

public record PaymentStub(int Id, int OrderId, decimal Amount, string Status);
public record CreatePaymentRequest(int OrderId, decimal Amount);