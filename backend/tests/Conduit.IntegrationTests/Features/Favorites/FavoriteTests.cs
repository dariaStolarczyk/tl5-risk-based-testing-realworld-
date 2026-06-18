using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Features.Favorites;
using Conduit.Infrastructure.Errors;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Conduit.IntegrationTests.Features.Favorites;

public class FavoriteTests : SliceFixture
{
    [Fact]
    public async Task Expect_Add_Favorite_To_Mark_Article_As_Favorited()
    {
        var testData = await CreateArticleWithCurrentUser("favorite-add-test-article");

        var handler = new Add.QueryHandler(
            GetDbContext(),
            new StubCurrentUserAccessor(testData.CurrentUserName)
        );

        var result = await handler.Handle(
            new Add.Command(testData.Slug),
            CancellationToken.None
        );

        Assert.True(result.Article.Favorited);
        Assert.Equal(1, result.Article.FavoritesCount);

        var favoriteCount = await ExecuteDbContextAsync(db =>
            db.ArticleFavorites.CountAsync(favorite =>
                favorite.ArticleId == testData.Article.ArticleId &&
                favorite.PersonId == testData.CurrentUser.PersonId
            )
        );

        Assert.Equal(1, favoriteCount);
    }

    [Fact]
    public async Task Expect_Add_Favorite_Twice_To_Be_Idempotent()
    {
        var testData = await CreateArticleWithCurrentUser("favorite-idempotent-test-article");

        var handler = new Add.QueryHandler(
            GetDbContext(),
            new StubCurrentUserAccessor(testData.CurrentUserName)
        );

        await handler.Handle(new Add.Command(testData.Slug), CancellationToken.None);

        var secondResult = await handler.Handle(
            new Add.Command(testData.Slug),
            CancellationToken.None
        );

        Assert.True(secondResult.Article.Favorited);
        Assert.Equal(1, secondResult.Article.FavoritesCount);

        var favoriteCount = await ExecuteDbContextAsync(db =>
            db.ArticleFavorites.CountAsync(favorite =>
                favorite.ArticleId == testData.Article.ArticleId &&
                favorite.PersonId == testData.CurrentUser.PersonId
            )
        );

        Assert.Equal(1, favoriteCount);
    }

    [Fact]
    public async Task Expect_Delete_Favorite_To_Remove_Favorite()
    {
        var testData = await CreateArticleWithCurrentUser("favorite-delete-test-article");

        var addHandler = new Add.QueryHandler(
            GetDbContext(),
            new StubCurrentUserAccessor(testData.CurrentUserName)
        );

        await addHandler.Handle(new Add.Command(testData.Slug), CancellationToken.None);

        var deleteHandler = new Delete.QueryHandler(
            GetDbContext(),
            new StubCurrentUserAccessor(testData.CurrentUserName)
        );

        var result = await deleteHandler.Handle(
            new Delete.Command(testData.Slug),
            CancellationToken.None
        );

        Assert.False(result.Article.Favorited);
        Assert.Equal(0, result.Article.FavoritesCount);

        var favoriteCount = await ExecuteDbContextAsync(db =>
            db.ArticleFavorites.CountAsync(favorite =>
                favorite.ArticleId == testData.Article.ArticleId &&
                favorite.PersonId == testData.CurrentUser.PersonId
            )
        );

        Assert.Equal(0, favoriteCount);
    }

    [Fact]
    public async Task Expect_Add_Favorite_With_Unknown_Slug_To_Be_NotFound()
    {
        const string currentUserName = "favorite-unknown-slug-user";

        var currentUser = new Person
        {
            Username = currentUserName,
            Email = "favorite-unknown-slug-user@example.com",
        };

        await InsertAsync(currentUser);

        var handler = new Add.QueryHandler(
            GetDbContext(),
            new StubCurrentUserAccessor(currentUserName)
        );

        var exception = await Assert.ThrowsAsync<RestException>(() =>
            handler.Handle(new Add.Command("unknown-favorite-slug"), CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.NotFound, exception.Code);
        Assert.NotNull(exception.Errors);
    }

    private async Task<FavoriteTestData> CreateArticleWithCurrentUser(string slug)
    {
        var authorName = $"{slug}-author";
        var currentUserName = $"{slug}-current-user";

        var author = new Person
        {
            Username = authorName,
            Email = $"{authorName}@example.com",
        };

        var currentUser = new Person
        {
            Username = currentUserName,
            Email = $"{currentUserName}@example.com",
        };

        var article = new Article
        {
            Slug = slug,
            Title = $"Title for {slug}",
            Description = $"Description for {slug}",
            Body = $"Body for {slug}",
            Author = author,
            CreatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        };

        await InsertAsync(author, currentUser, article);

        return new FavoriteTestData(article, currentUser, currentUserName, slug);
    }

    private sealed record FavoriteTestData(
        Article Article,
        Person CurrentUser,
        string CurrentUserName,
        string Slug
    );
}
