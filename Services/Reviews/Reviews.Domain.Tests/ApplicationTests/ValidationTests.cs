using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using FluentValidation.TestHelper;
using MediatR;
using Microsoft.Extensions.Logging;
using Reviews.Application.Commands.CreateReview;
using Reviews.Application.Commands.UpdateReview;
using Reviews.Application.Common.Behaviors;
using Reviews.Application.Queries.GetReviewById;
using Reviews.Application.Queries.GetReviewsList;

namespace Reviews.Domain.Tests;

public class ValidationTests
{
    private static readonly CreateReviewCommand ValidCreate =
        new(4, "Da Hong Pao Oolong", "1", "Olena", 5, "Great tea");

    [Fact]
    public void Create_valid_command_has_no_errors()
        => new CreateReviewCommandValidator().TestValidate(ValidCreate)
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Create_with_rating_10_fails_on_rating()
        => new CreateReviewCommandValidator().TestValidate(ValidCreate with { Rating = 10 })
            .ShouldHaveValidationErrorFor(x => x.Rating);

    [Fact]
    public void Create_with_empty_text_fails_on_text()
        => new CreateReviewCommandValidator().TestValidate(ValidCreate with { Text = "" })
            .ShouldHaveValidationErrorFor(x => x.Text);

    [Fact]
    public void Create_with_zero_product_id_fails_on_product_id()
        => new CreateReviewCommandValidator().TestValidate(ValidCreate with { ProductId = 0 })
            .ShouldHaveValidationErrorFor(x => x.ProductId);

    [Fact]
    public void Update_with_invalid_id_fails_on_id()
        => new UpdateReviewCommandValidator()
            .TestValidate(new UpdateReviewCommand("not-an-id", 5, "Text", 0))
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void Update_with_negative_version_fails()
        => new UpdateReviewCommandValidator()
            .TestValidate(new UpdateReviewCommand("66f0c2000000000000000001", 5, "Text", -1))
            .ShouldHaveValidationErrorFor(x => x.ExpectedVersion);

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("")]
    public void GetById_with_invalid_object_id_fails(string id)
        => new GetReviewByIdQueryValidator().TestValidate(new GetReviewByIdQuery(id))
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void GetById_with_valid_object_id_passes()
        => new GetReviewByIdQueryValidator()
            .TestValidate(new GetReviewByIdQuery("66f0c2000000000000000001"))
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void GetList_with_page_zero_fails()
        => new GetReviewsListQueryValidator().TestValidate(new GetReviewsListQuery(null, 0, 20))
            .ShouldHaveValidationErrorFor(x => x.Page);

    [Fact]
    public void GetList_with_page_size_101_fails()
        => new GetReviewsListQueryValidator().TestValidate(new GetReviewsListQuery(null, 1, 101))
            .ShouldHaveValidationErrorFor(x => x.PageSize);

    [Fact]
    public void GetList_with_defaults_passes()
        => new GetReviewsListQueryValidator().TestValidate(new GetReviewsListQuery(null))
            .ShouldNotHaveAnyValidationErrors();

    private static ValidationBehavior<CreateReviewCommand, string> NewValidationBehavior()
        => new(new[] { new CreateReviewCommandValidator() });

    [Fact]
    public async Task ValidationBehavior_invalid_command_throws_and_skips_handler()
    {
        var handlerCalled = false;
        RequestHandlerDelegate<string> next = _ => { handlerCalled = true; return Task.FromResult("id"); };

        await Assert.ThrowsAsync<ValidationException>(() =>
            NewValidationBehavior().Handle(ValidCreate with { Rating = 10 }, next, default));

        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task ValidationBehavior_collects_all_errors_together()
    {
        RequestHandlerDelegate<string> next = _ => Task.FromResult("id");
        var bad = ValidCreate with { Rating = 10, Text = "", ProductId = 0 };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            NewValidationBehavior().Handle(bad, next, default));

        Assert.True(ex.Errors.Count() >= 3);
    }

    [Fact]
    public async Task ValidationBehavior_valid_command_calls_handler()
    {
        RequestHandlerDelegate<string> next = _ => Task.FromResult("created-id");

        var result = await NewValidationBehavior().Handle(ValidCreate, next, default);

        Assert.Equal("created-id", result);
    }


    [Fact]
    public async Task Pipeline_rating_10_is_rejected_before_handler_and_log_shows_it()
    {
        var logger = new ListLogger<LoggingBehavior<CreateReviewCommand, string>>();
        var logging = new LoggingBehavior<CreateReviewCommand, string>(logger);
        var validation = NewValidationBehavior();

        var handlerCalled = false;
        RequestHandlerDelegate<string> handler = _ => { handlerCalled = true; return Task.FromResult("id"); };
        var command = ValidCreate with { Rating = 10 };
        RequestHandlerDelegate<string> afterLogging = ct => validation.Handle(command, handler, ct);

        await Assert.ThrowsAsync<ValidationException>(() => logging.Handle(command, afterLogging, default));

        Assert.False(handlerCalled);
        Assert.Contains(logger.Messages, m => m.StartsWith("Handling CreateReviewCommand"));
        Assert.DoesNotContain(logger.Messages, m => m.StartsWith("Handled "));
        Assert.Contains(logger.Messages, m => m.Contains("ValidationException"));
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}