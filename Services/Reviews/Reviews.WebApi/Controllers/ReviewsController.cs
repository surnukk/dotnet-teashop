using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reviews.Application.Commands.AddComment;
using Reviews.Application.Commands.CreateReview;
using Reviews.Application.Commands.DeleteReview;
using Reviews.Application.Commands.RemoveComment;
using Reviews.Application.Commands.UpdateReview;
using Reviews.Application.Common.Models;
using Reviews.Application.Queries.GetReviewById;
using Reviews.Application.Queries.GetReviewsList;
using Reviews.WebApi.Contracts;

namespace Reviews.WebApi.Controllers;

[Route("api/reviews")]
public class ReviewsController(ISender mediator, IMapper mapper) : ApiControllerBase(mediator)
{
    /// <summary>Список відгуків (фільтр за товаром, пагінація).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ReviewDto>>> GetList(
        [FromQuery] long? productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetReviewsListQuery(productId, page, pageSize), ct));

    /// <summary>Відгук за Id. Версія документа повертається в заголовку ETag.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> GetById(string id, CancellationToken ct)
    {
        var review = await Mediator.Send(new GetReviewByIdQuery(id), ct);
        Response.Headers.ETag = $"\"{review.Version}\"";
        return Ok(review);
    }

    /// <summary>Створити відгук. Повертає 201 і Location із Id.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<string>> Create(CreateReviewRequest request, CancellationToken ct)
    {
        var id = await Mediator.Send(mapper.Map<CreateReviewCommand>(request), ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Оновити відгук. Очікувана версія передається в заголовку If-Match.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        string id,
        UpdateReviewRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (!int.TryParse(ifMatch?.Trim('"'), out var expectedVersion))
            return Problem(
                detail: "Header If-Match with the review version is required, e.g. If-Match: \"0\"",
                statusCode: StatusCodes.Status400BadRequest);

        await Mediator.Send(new UpdateReviewCommand(id, request.Rating, request.Text, expectedVersion), ct);
        return NoContent();
    }

    /// <summary>Видалити відгук.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteReviewCommand(id), ct);
        return NoContent();
    }

    /// <summary>Додати коментар (вкладений масив comments).</summary>
    [HttpPost("{id}/comments")]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> AddComment(
        string id, AddCommentRequest request, CancellationToken ct)
    {
        var commentId = await Mediator.Send(
            new AddCommentCommand(id, request.AuthorId, request.AuthorName, request.Text), ct);
        return CreatedAtAction(nameof(GetById), new { id }, commentId);
    }

    /// <summary>Видалити один коментар.</summary>
    [HttpDelete("{id}/comments/{commentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveComment(string id, string commentId, CancellationToken ct)
    {
        await Mediator.Send(new RemoveCommentCommand(id, commentId), ct);
        return NoContent();
    }
}