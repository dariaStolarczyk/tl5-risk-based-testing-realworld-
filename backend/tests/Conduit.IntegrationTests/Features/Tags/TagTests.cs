using System;
using System.Linq;
using System.Threading.Tasks;
using Conduit.Domain;
using Xunit;
using ArticleList = Conduit.Features.Articles.List;
using TagList = Conduit.Features.Tags.List;

namespace Conduit.IntegrationTests.Features.Tags;

public class TagTests : SliceFixture
{
    [Fact]
    public async Task Expect_List_Tags_Returns_All_Available_Tags()
    {
        await InsertAsync(
            new Tag { TagId = "testing" },
            new Tag { TagId = "dotnet" },
            new Tag { TagId = "angular" }
        );

        var result = await SendAsync(new TagList.Query());

        Assert.Equal(3, result.Tags.Count);
        Assert.Equal(["angular", "dotnet", "testing"], result.Tags);
    }

    [Fact]
    public async Task Expect_List_Articles_With_Tag_Filter_Returns_Only_Matching_Articles()
    {
        var author = BuildPerson("tag-filter-author");

        var matchingArticle = BuildArticle(
            author,
            slug: "matching-tag-filter-article",
            title: "Matching tag filter article",
            createdAt: new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        );

        var nonMatchingArticle = BuildArticle(
            author,
            slug: "non-matching-tag-filter-article",
            title: "Non matching tag filter article",
            createdAt: new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc)
        );

        await InsertAsync(
            author,
            matchingArticle,
            nonMatchingArticle,
            new Tag { TagId = "tl5-risk" },
            new Tag { TagId = "other-risk" }
        );

        await AddTagToArticleAsync(matchingArticle, "tl5-risk");
        await AddTagToArticleAsync(nonMatchingArticle, "other-risk");

        var result = await SendAsync(
            new ArticleList.Query(
                Tag: "tl5-risk",
                Author: null,
                FavoritedUsername: null,
                Limit: null,
                Offset: null
            )
        );

        var slugs = result.Articles.Select(article => article.Slug).ToArray();

        Assert.Equal(1, result.ArticlesCount);
        Assert.Single(result.Articles);
        Assert.Contains("matching-tag-filter-article", slugs);
        Assert.DoesNotContain("non-matching-tag-filter-article", slugs);
        Assert.All(result.Articles, article => Assert.Contains("tl5-risk", article.TagList));
    }

    [Fact]
    public async Task Expect_List_Articles_With_Author_Filter_Returns_Only_Author_Articles()
    {
        var targetAuthor = BuildPerson("target-author");
        var otherAuthor = BuildPerson("other-author");

        var targetAuthorArticle = BuildArticle(
            targetAuthor,
            slug: "target-author-article",
            title: "Target author article",
            createdAt: new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        );

        var otherAuthorArticle = BuildArticle(
            otherAuthor,
            slug: "other-author-article",
            title: "Other author article",
            createdAt: new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc)
        );

        await InsertAsync(
            targetAuthor,
            otherAuthor,
            targetAuthorArticle,
            otherAuthorArticle
        );

        var result = await SendAsync(
            new ArticleList.Query(
                Tag: null,
                Author: "target-author",
                FavoritedUsername: null,
                Limit: null,
                Offset: null
            )
        );

        var slugs = result.Articles.Select(article => article.Slug).ToArray();

        Assert.Equal(1, result.ArticlesCount);
        Assert.Single(result.Articles);
        Assert.Contains("target-author-article", slugs);
        Assert.DoesNotContain("other-author-article", slugs);
        Assert.All(result.Articles, article =>
            Assert.Equal("target-author", article.Author?.Username)
        );
    }

    [Fact]
    public async Task Expect_List_Articles_With_Favorited_Filter_Returns_Only_Favorited_Articles()
    {
        var author = BuildPerson("favorite-filter-author");
        var favoriteUser = BuildPerson("favorite-filter-user");

        var favoritedArticle = BuildArticle(
            author,
            slug: "favorited-filter-article",
            title: "Favorited filter article",
            createdAt: new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        );

        var nonFavoritedArticle = BuildArticle(
            author,
            slug: "non-favorited-filter-article",
            title: "Non favorited filter article",
            createdAt: new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc)
        );

        await InsertAsync(
            author,
            favoriteUser,
            favoritedArticle,
            nonFavoritedArticle
        );

        await AddFavoriteToArticleAsync(favoritedArticle, favoriteUser);

        var result = await SendAsync(
            new ArticleList.Query(
                Tag: null,
                Author: null,
                FavoritedUsername: "favorite-filter-user",
                Limit: null,
                Offset: null
            )
        );

        var slugs = result.Articles.Select(article => article.Slug).ToArray();

        Assert.Equal(1, result.ArticlesCount);
        Assert.Single(result.Articles);
        Assert.Contains("favorited-filter-article", slugs);
        Assert.DoesNotContain("non-favorited-filter-article", slugs);
        Assert.All(result.Articles, article =>
        {
            Assert.True(article.Favorited);
            Assert.Equal(1, article.FavoritesCount);
        });
    }

    private async Task AddTagToArticleAsync(Article article, string tagId)
    {
        await ExecuteDbContextAsync(async db =>
        {
            db.ArticleTags.Add(
                new ArticleTag
                {
                    ArticleId = article.ArticleId,
                    TagId = tagId
                }
            );

            await db.SaveChangesAsync();
        });
    }

    private async Task AddFavoriteToArticleAsync(Article article, Person person)
    {
        await ExecuteDbContextAsync(async db =>
        {
            db.ArticleFavorites.Add(
                new ArticleFavorite
                {
                    ArticleId = article.ArticleId,
                    PersonId = person.PersonId
                }
            );

            await db.SaveChangesAsync();
        });
    }

    private static Person BuildPerson(string username)
    {
        return new Person
        {
            Username = username,
            Email = $"{username}@example.com"
        };
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
            UpdatedAt = createdAt
        };
    }
}
