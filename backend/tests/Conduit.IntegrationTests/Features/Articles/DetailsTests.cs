using System;
using System.Net;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure.Errors;
using FluentValidation;
using Xunit;
using ArticleDetails = Conduit.Features.Articles.Details;

namespace Conduit.IntegrationTests.Features.Articles;

public class DetailsTests : SliceFixture
{
    [Fact]
    public async Task Expect_Details_Returns_Article_By_Slug()
    {
        var author = new Person
        {
            Username = "details-author",
            Email = "details-author@example.com",
        };

        var article = new Article
        {
            Slug = "details-test-article",
            Title = "Details test article",
            Description = "Description for details test article",
            Body = "Body for details test article",
            Author = author,
            CreatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        };

        await InsertAsync(author, article);

        var result = await SendAsync(new ArticleDetails.Query("details-test-article"));

        Assert.NotNull(result.Article);
        Assert.Equal("details-test-article", result.Article.Slug);
        Assert.Equal("Details test article", result.Article.Title);
        Assert.Equal("Description for details test article", result.Article.Description);
        Assert.Equal("Body for details test article", result.Article.Body);
        Assert.NotNull(result.Article.Author);
        Assert.Equal("details-author", result.Article.Author.Username);
    }

    [Fact]
    public async Task Expect_Details_With_Unknown_Slug_To_Be_NotFound()
    {
        var exception = await Assert.ThrowsAsync<RestException>(() =>
            SendAsync(new ArticleDetails.Query("unknown-article-slug"))
        );

        Assert.Equal(HttpStatusCode.NotFound, exception.Code);
        Assert.NotNull(exception.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Expect_Details_With_Missing_Slug_To_Fail_Validation(string? slug)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            SendAsync(new ArticleDetails.Query(slug!))
        );
    }
}
