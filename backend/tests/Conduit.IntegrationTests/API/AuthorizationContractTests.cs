using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Conduit.IntegrationTests.Api;

public sealed class AuthorizationContractTests : ApiTestBase
{
    [Fact]
    public async Task GetFeed_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/articles/feed");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostArticle_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(
            "/articles",
            NewArticlePayload(
                title: Unique("unauthorized-article"),
                description: "Article should not be created without token.",
                body: "This request is intentionally unauthenticated.",
                tags: ["tl5-auth"]
            )
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutArticle_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.PutAsJsonAsync(
            "/articles/some-existing-or-non-existing-slug",
            NewArticlePayload(
                title: "Unauthorized update",
                description: "This update must not be accepted without token.",
                body: "This request is intentionally unauthenticated.",
                tags: ["tl5-auth"]
            )
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteArticle_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.DeleteAsync("/articles/some-existing-or-non-existing-slug");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostComment_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(
            "/articles/some-existing-or-non-existing-slug/comments",
            NewCommentPayload("This comment must not be created without token.")
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.DeleteAsync(
            "/articles/some-existing-or-non-existing-slug/comments/1"
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostFavorite_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(
            "/articles/some-existing-or-non-existing-slug/favorite",
            new { }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteFavorite_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.DeleteAsync(
            "/articles/some-existing-or-non-existing-slug/favorite"
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostFollow_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(
            "/profiles/some-existing-or-non-existing-user/follow",
            new { }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteFollow_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.DeleteAsync(
            "/profiles/some-existing-or-non-existing-user/follow"
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/user");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.PutAsJsonAsync(
            "/user",
            NewUserUpdatePayload(
                bio: "This update must not be accepted without token."
            )
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
