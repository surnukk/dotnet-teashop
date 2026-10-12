using Microsoft.AspNetCore.Mvc;
using Orders.Bll.DTO;
using Orders.Bll.Services;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/promotions")]
public class PromotionsController(IPromotionService promotionService) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<ActionResult<PromotionDto>> GetById(long id, CancellationToken ct)
    {
        var promo = await promotionService.GetByIdAsync(id, ct);
        if (promo == null)
            return NotFound();

        return Ok(promo);
    }

    [HttpGet("by-code/{code}")]
    public async Task<ActionResult<PromotionDto>> GetByCode(string code, CancellationToken ct)
    {
        var promo = await promotionService.GetByCodeAsync(code, ct);
        if (promo == null)
            return NotFound();

        return Ok(promo);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<PromotionDto>>> GetActive(CancellationToken ct)
    {
        var promos = await promotionService.GetActivePromotionsAsync(ct);
        return Ok(promos);
    }
}
