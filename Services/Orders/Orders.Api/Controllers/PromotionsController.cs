using Microsoft.AspNetCore.Mvc;
using Orders.Bll.DTO;
using Orders.Bll.Services;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/promotions")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public class PromotionsController(IPromotionService promotionService) : ControllerBase
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PromotionDto>> GetById(long id, CancellationToken ct)
    {
        var promo = await promotionService.GetByIdAsync(id, ct);
        if (promo == null)
            return NotFound();

        return Ok(promo);
    }

    [HttpGet("by-code/{code}")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PromotionDto>> GetByCode(string code, CancellationToken ct)
    {
        var promo = await promotionService.GetByCodeAsync(code, ct);
        if (promo == null)
            return NotFound();

        return Ok(promo);
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<PromotionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PromotionDto>>> GetActive(CancellationToken ct)
    {
        var promos = await promotionService.GetActivePromotionsAsync(ct);
        return Ok(promos);
    }
}
