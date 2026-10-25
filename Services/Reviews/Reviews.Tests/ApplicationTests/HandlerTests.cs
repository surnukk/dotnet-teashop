using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using MongoDB.Bson;
using NSubstitute;
using Reviews.Application;
using Reviews.Application.Commands.AddComment;
using Reviews.Application.Commands.CreateReview;
using Reviews.Application.Commands.DeleteReview;
using Reviews.Application.Commands.UpdateReview;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Application.Queries.GetReviewById;
using Reviews.Application.Queries.GetReviewsList;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Tests;

public class HandlerTests
{
    private static Review NewReview()
        => new(4, "Da Hong Pao Oolong", new Author("1", "Olena"), new Rating(5), "Great tea");

    private static IReviewRepository RepoReturning(Review? review)
    {
        var repo = Substitute.For<IReviewRepository>();
        repo.GetByIdAsync(Arg.Any<ObjectId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(review));
        return repo;
    }

    [Fact]
    public async Task Create_returns_object_id_and_saves_review()
    {
        var repo = Substitute.For<IReviewRepository>();
        var handler = new CreateReviewCommandHandler(repo);

        var id = await handler.Handle(
            new CreateReviewCommand(4, "Da Hong Pao Oolong", "1", "Olena", 5, "Great tea"), default);

        Assert.True(ObjectId.TryParse(id, out _));
        await repo.Received(1).AddAsync(Arg.Any<Review>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_unknown_review_throws_not_found()
    {
        var handler = new UpdateReviewCommandHandler(RepoReturning(null));

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new UpdateReviewCommand(ObjectId.GenerateNewId().ToString(), 4, "New text", 0), default));
    }

    [Fact]
    public async Task Update_with_wrong_version_throws_conflict_and_does_not_save()
    {
        var repo = RepoReturning(NewReview());
        var handler = new UpdateReviewCommandHandler(repo);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new UpdateReviewCommand(ObjectId.GenerateNewId().ToString(), 4, "New text", 5), default));

        await repo.DidNotReceive().UpdateAsync(Arg.Any<Review>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_with_current_version_changes_review_and_saves()
    {
        var review = NewReview();
        var repo = RepoReturning(review);
        var handler = new UpdateReviewCommandHandler(repo);

        await handler.Handle(
            new UpdateReviewCommand(review.Id.ToString(), 3, "Changed my mind", review.Version), default);

        Assert.Equal(3, review.Rating.Value);
        Assert.Equal("Changed my mind", review.Text);
        await repo.Received(1).UpdateAsync(review, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_missing_review_throws_not_found()
    {
        var repo = Substitute.For<IReviewRepository>();
        repo.DeleteAsync(Arg.Any<ObjectId>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteReviewCommandHandler(repo);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteReviewCommand(ObjectId.GenerateNewId().ToString()), default));
    }

    [Fact]
    public async Task AddComment_adds_comment_to_review_and_saves()
    {
        var review = NewReview();
        var repo = RepoReturning(review);
        var handler = new AddCommentCommandHandler(repo);

        var commentId = await handler.Handle(
            new AddCommentCommand(review.Id.ToString(), "2", "Ivan", "Agreed"), default);

        Assert.Single(review.Comments);
        Assert.Equal(review.Comments.First().Id.ToString(), commentId);
        await repo.Received(1).UpdateAsync(review, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_missing_review_throws_not_found()
    {
        var queries = Substitute.For<IReviewQueries>();
        queries.GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ReviewDto?>(null));
        var handler = new GetReviewByIdQueryHandler(queries);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetReviewByIdQuery(ObjectId.GenerateNewId().ToString()), default));
    }

    [Fact]
    public async Task GetList_clamps_page_size_to_100()
    {
        var queries = Substitute.For<IReviewQueries>();
        queries.GetListAsync(default, default, default, default)
            .ReturnsForAnyArgs(new PagedResult<ReviewDto>([], 1, 100, 0));
        var handler = new GetReviewsListQueryHandler(queries);

        await handler.Handle(new GetReviewsListQuery(null, 0, 1000), default);

        await queries.Received(1).GetListAsync(null, 1, 100, Arg.Any<CancellationToken>());
    }

    /*[Fact]
    public async Task Mediator_resolves_handler_and_creates_review()
    {
        var services = new ServiceCollection();

        services.AddSingleton(Substitute.For<IReviewRepository>());

        services.AddApplication(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();

        var sender = provider.GetRequiredService<ISender>();
        var id = await sender.Send(
            new CreateReviewCommand(4, "Da Hong Pao Oolong", "1", "Olena", 5, "Great tea"));

        Assert.True(ObjectId.TryParse(id, out _));
    }*/
}