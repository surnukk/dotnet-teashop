using Microsoft.AspNetCore.Mvc;
using Orders.Bll.DTO;
using Orders.Bll.Services;
using Microsoft.AspNetCore.Authorization;
using Platform.ServiceDefaults.Security;

namespace Orders.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/promotions")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public class PromotionsController(IPromotionService promotionService) : ControllerBase
{
    [HttpGet("{id:long}")]
    [RequirePermission("orders:read")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PromotionDto>> GetById(long id, CancellationToken ct)
    {
        var promo = await promotionService.GetByIdAsync(id, ct);
        if (promo == null)
            return NotFound();

        return Ok(promo);
    }

    [HttpGet("by-code/{code}")]
    [RequirePermission("orders:read")]
    [ProducesResponseType(typeof(PromotionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PromotionDto>> GetByCode(string code, CancellationToken ct)
    {
        var promo = await promotionService.GetByCodeAsync(code, ct);
        if (promo == null)
            return NotFound();

        return Ok(promo);
    }

    [HttpGet("active")]
    [RequirePermission("orders:read")]
    [ProducesResponseType(typeof(IEnumerable<PromotionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PromotionDto>>> GetActive(CancellationToken ct)
    {
        var promos = await promotionService.GetActivePromotionsAsync(ct);
        return Ok(promos);
    }

    [HttpPost]
    [RequirePermission("orders:create")]
    [ProducesResponseType(typeof(long), StatusCodes.Status201Created)]
    public async Task<ActionResult<long>> Create(UpsertPromotionRequest request, CancellationToken ct)
    {
        var id = await promotionService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:long}")]
    [RequirePermission("orders:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(long id, UpsertPromotionRequest request, CancellationToken ct)
    {
        await promotionService.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission("orders:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await promotionService.DeleteAsync(id, ct);
        return NoContent();
    }
}