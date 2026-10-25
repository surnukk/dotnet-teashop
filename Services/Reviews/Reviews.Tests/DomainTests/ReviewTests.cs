using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Tests.DomainTests;

public class ReviewTests
{
    private static Author Olena => new("1", "Olena");

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(10)]
    public void Rating_out_of_range_throws(int value)
        => Assert.Throws<DomainException>(() => new Rating(value));

    [Fact]
    public void Review_with_rating_10_throws_before_any_db_call()
        => Assert.Throws<DomainException>(
            () => new Review(4, "Da Hong Pao Oolong", Olena, new Rating(10), "Perfectly"));

    [Fact]
    public void Review_with_blank_text_throws()
        => Assert.Throws<DomainException>(
            () => new Review(4, "Da Hong Pao Oolong", Olena, new Rating(5), "  "));

    [Fact]
    public void Review_with_zero_product_id_throws()
        => Assert.Throws<DomainException>(
            () => new Review(0, "Tea", Olena, new Rating(5), "Text"));

    [Fact]
    public void Authors_with_same_values_are_equal()
        => Assert.Equal(new Author("1", "Olena"), new Author("1", "Olena"));

    [Fact]
    public void Author_without_display_name_throws()
        => Assert.Throws<DomainException>(() => new Author("1", ""));

    [Fact]
    public void AddComment_adds_comment_and_touches_review()
    {
        var review = new Review(4, "Da Hong Pao Oolong", Olena, new Rating(5), "Perfectly");

        var comment = review.AddComment(new Author("2", "Ivan"), "Agree");

        Assert.Single(review.Comments);
        Assert.NotNull(review.UpdatedAt);
        Assert.NotEqual(default, comment.Id);
    }

    [Fact]
    public void AddComment_with_empty_text_throws()
    {
        var review = new Review(4, "Da Hong Pao Oolong", Olena, new Rating(5), "Perfectly");
        Assert.Throws<DomainException>(() => review.AddComment(new Author("2", "Ivan"), ""));
    }

    [Fact]
    public void RemoveComment_unknown_id_throws_not_found()
    {
        var review = new Review(4, "Da Hong Pao Oolong", Olena, new Rating(5), "Perfectly");
        Assert.Throws<NotFoundException>(() => review.RemoveComment(MongoDB.Bson.ObjectId.GenerateNewId()));
    }
}