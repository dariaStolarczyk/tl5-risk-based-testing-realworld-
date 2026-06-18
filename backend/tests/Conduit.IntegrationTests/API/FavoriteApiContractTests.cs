using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Conduit.IntegrationTests.Api;

public sealed class FavoriteApiContractTests : ApiTestBase
{
    [Fact]
    public async Task PostFavorite_WithValidToken_ReturnsArticleEnvelopeWithFavoritedTrue()
    {
        var username = Unique("favorite-user");
        var email = $"{username}@example.test";
        var token = await RegisterUserAndGetTokenAsync(username, email, "password123");
        var slug = await CreateFavoriteArticleAndGetSlugAsync(token);

        var response = await SendWithTokenAsync(
            HttpMethod.Post,
            $"/articles/{slug}/favorite",
            token
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var article = json.RootElement.GetProperty("article");

        Assert.Equal(slug, article.GetProperty("slug").GetString());
        Assert.True(article.GetProperty("favorited").GetBoolean());
        Assert.Equal(1, article.GetProperty("favoritesCount").GetInt32());
    }

    [Fact]
    public async Task DeleteFavorite_WithValidToken_ReturnsArticleEnvelopeWithFavoritedFalse()
    {
        var username = Unique("favorite-delete-user");
        var email = $"{username}@example.test";
        var token = await RegisterUserAndGetTokenAsync(username, email, "password123");
        var slug = await CreateFavoriteArticleAndGetSlugAsync(token);

        await FavoriteArticleAsync(token, slug);

        var response = await SendWithTokenAsync(
            HttpMethod.Delete,
            $"/articles/{slug}/favorite",
            token
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var article = json.RootElement.GetProperty("article");

        Assert.Equal(slug, article.GetProperty("slug").GetString());
        Assert.False(article.GetProperty("favorited").GetBoolean());
        Assert.Equal(0, article.GetProperty("favoritesCount").GetInt32());
    }

    [Fact]
    public async Task PostFavorite_ForUnknownSlug_ReturnsNotFound()
    {
        var username = Unique("favorite-unknown-slug-user");
        var email = $"{username}@example.test";
        var token = await RegisterUserAndGetTokenAsync(username, email, "password123");

        var unknownSlug = Unique("unknown-article-slug");

        var response = await SendWithTokenAsync(
            HttpMethod.Post,
            $"/articles/{unknownSlug}/favorite",
            token
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("errors", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostFavorite_Twice_RemainsIdempotentOrDoesNotDuplicate()
    {
        var username = Unique("favorite-idempotent-user");
        var email = $"{username}@example.test";
        var token = await RegisterUserAndGetTokenAsync(username, email, "password123");
        var slug = await CreateFavoriteArticleAndGetSlugAsync(token);

        var firstResponse = await SendWithTokenAsync(
            HttpMethod.Post,
            $"/articles/{slug}/favorite",
            token
        );

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        using (var firstJson = await ReadJsonAsync(firstResponse))
        {
            var firstArticle = firstJson.RootElement.GetProperty("article");

            Assert.Equal(slug, firstArticle.GetProperty("slug").GetString());
            Assert.True(firstArticle.GetProperty("favorited").GetBoolean());
            Assert.Equal(1, firstArticle.GetProperty("favoritesCount").GetInt32());
        }

        var secondResponse = await SendWithTokenAsync(
            HttpMethod.Post,
            $"/articles/{slug}/favorite",
            token
        );

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        using var secondJson = await ReadJsonAsync(secondResponse);
        var secondArticle = secondJson.RootElement.GetProperty("article");

        Assert.Equal(slug, secondArticle.GetProperty("slug").GetString());
        Assert.True(secondArticle.GetProperty("favorited").GetBoolean());
        Assert.Equal(1, secondArticle.GetProperty("favoritesCount").GetInt32());
    }

    private async Task<string> CreateFavoriteArticleAndGetSlugAsync(string token)
    {
        return await CreateArticleAndGetSlugAsync(
            token,
            title: Unique("Favorite API Contract Article"),
            description: "Article used for favorite API contract tests.",
            body: "This article exists only as fixture data for favorite HTTP tests.",
            tags: ["tl5-favorites"]
        );
    }

    private async Task FavoriteArticleAsync(string token, string slug)
    {
        var response = await SendWithTokenAsync(
            HttpMethod.Post,
            $"/articles/{slug}/favorite",
            token
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var article = json.RootElement.GetProperty("article");

        Assert.Equal(slug, article.GetProperty("slug").GetString());
        Assert.True(article.GetProperty("favorited").GetBoolean());
        Assert.Equal(1, article.GetProperty("favoritesCount").GetInt32());
    }
}
