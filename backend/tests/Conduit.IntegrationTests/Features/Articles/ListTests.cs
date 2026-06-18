using System;
using System.Linq;
using System.Threading.Tasks;
using Conduit.Domain;
using Xunit;
using ArticleList = Conduit.Features.Articles.List;

namespace Conduit.IntegrationTests.Features.Articles;

public class ListTests : SliceFixture
{
    [Fact]
    public async Task Expect_List_Articles_Without_Filters_Returns_All_Articles()
    {
        var author = new Person
        {
            Username = "list-author",
            Email = "list-author@example.com",
        };

        var firstArticle = BuildArticle(
            author,
            "first-list-article",
            "First list article",
            new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc)
        );

        var secondArticle = BuildArticle(
            author,
            "second-list-article",
            "Second list article",
            new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc)
        );

        var thirdArticle = BuildArticle(
            author,
            "third-list-article",
            "Third list article",
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        );

        await InsertAsync(author, firstArticle, secondArticle, thirdArticle);

        var result = await SendAsync(
            new ArticleList.Query(
                Tag: null,
                Author: null,
                FavoritedUsername: null,
                Limit: null,
                Offset: null
            )
        );

        var slugs = result.Articles.Select(article => article.Slug).ToArray();

        Assert.Equal(3, result.ArticlesCount);
        Assert.Equal(3, result.Articles.Count);
        Assert.Contains("first-list-article", slugs);
        Assert.Contains("second-list-article", slugs);
        Assert.Contains("third-list-article", slugs);
    }

    [Fact]
    public async Task Expect_List_Articles_With_Limit_And_Offset_Returns_Expected_Page()
    {
        var author = new Person
        {
            Username = "pagination-author",
            Email = "pagination-author@example.com",
        };

        var oldestArticle = BuildArticle(
            author,
            "oldest-pagination-article",
            "Oldest pagination article",
            new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc)
        );

        var middleArticle = BuildArticle(
            author,
            "middle-pagination-article",
            "Middle pagination article",
            new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc)
        );

        var newestArticle = BuildArticle(
            author,
            "newest-pagination-article",
            "Newest pagination article",
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        );

        await InsertAsync(author, oldestArticle, middleArticle, newestArticle);

        var result = await SendAsync(
            new ArticleList.Query(
                Tag: null,
                Author: null,
                FavoritedUsername: null,
                Limit: 2,
                Offset: 1
            )
        );

        var slugs = result.Articles.Select(article => article.Slug).ToArray();

        Assert.Equal(3, result.ArticlesCount);
        Assert.Equal(2, result.Articles.Count);
        Assert.Equal("middle-pagination-article", slugs[0]);
        Assert.Equal("oldest-pagination-article", slugs[1]);
    }

    private static Article BuildArticle(
        Person author,
        string slug,
        string title,
        DateTime createdAt
    )
    {
        return new Article
        {
            Slug = slug,
            Title = title,
            Description = $"Description for {title}",
            Body = $"Body for {title}",
            Author = author,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };
    }
}
