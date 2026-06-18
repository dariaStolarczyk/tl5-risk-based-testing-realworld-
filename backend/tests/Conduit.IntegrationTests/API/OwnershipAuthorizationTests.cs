#pragma warning disable xUnit1004
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Conduit.IntegrationTests.Api;

public sealed class OwnershipAuthorizationTests : ApiTestBase
{
    [Fact(Skip = "Documents missing ownership authorization in current implementation.")]
    public async Task PutArticle_AsDifferentUser_ReturnsForbiddenOrNotFound()
    {
        var ownerUsername = Unique("article-owner");
        var ownerEmail = $"{ownerUsername}@example.test";
        var ownerToken = await RegisterUserAndGetTokenAsync(
            ownerUsername,
            ownerEmail,
            "password123"
        );

        var otherUsername = Unique("article-non-owner");
        var otherEmail = $"{otherUsername}@example.test";
        var otherToken = await RegisterUserAndGetTokenAsync(
            otherUsername,
            otherEmail,
            "password123"
        );

        var originalTitle = Unique("Ownership Article");
        var slug = await CreateArticleAndGetSlugAsync(
            ownerToken,
            title: originalTitle,
            description: "Article owned by another user.",
            body: "This article must not be editable by a different user.",
            tags: ["tl5-ownership"]
        );

        var updatedTitle = Unique("Illegitimate Update");

        var response = await SendWithTokenAsync(
            HttpMethod.Put,
            $"/articles/{slug}",
            otherToken,
            NewArticlePayload(
                updatedTitle,
                "This update should be rejected because the caller is not the owner.",
                "A different authenticated user must not be able to edit this article.",
                ["tl5-ownership", "unauthorized-update"]
            )
        );

        AssertForbiddenOrNotFound(response.StatusCode);

        var getResponse = await Client.GetAsync($"/articles/{slug}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var json = await ReadJsonAsync(getResponse);
        var article = json.RootElement.GetProperty("article");

        Assert.Equal(originalTitle, article.GetProperty("title").GetString());
    }

    [Fact(Skip = "Documents missing ownership authorization in current implementation.")]
    public async Task DeleteArticle_AsDifferentUser_ReturnsForbiddenOrNotFound()
    {
        var ownerUsername = Unique("delete-article-owner");
        var ownerEmail = $"{ownerUsername}@example.test";
        var ownerToken = await RegisterUserAndGetTokenAsync(
            ownerUsername,
            ownerEmail,
            "password123"
        );

        var otherUsername = Unique("delete-article-non-owner");
        var otherEmail = $"{otherUsername}@example.test";
        var otherToken = await RegisterUserAndGetTokenAsync(
            otherUsername,
            otherEmail,
            "password123"
        );

        var slug = await CreateArticleAndGetSlugAsync(
            ownerToken,
            title: Unique("Ownership Delete Article"),
            description: "Article owned by another user.",
            body: "This article must not be deletable by a different user.",
            tags: ["tl5-ownership"]
        );

        var response = await SendWithTokenAsync(
            HttpMethod.Delete,
            $"/articles/{slug}",
            otherToken
        );

        AssertForbiddenOrNotFound(response.StatusCode);

        var getResponse = await Client.GetAsync($"/articles/{slug}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact(Skip = "Documents missing ownership authorization in current implementation.")]
    public async Task DeleteComment_AsDifferentUser_ReturnsForbiddenOrNotFound()
    {
        var ownerUsername = Unique("comment-owner");
        var ownerEmail = $"{ownerUsername}@example.test";
        var ownerToken = await RegisterUserAndGetTokenAsync(
            ownerUsername,
            ownerEmail,
            "password123"
        );

        var otherUsername = Unique("comment-non-owner");
        var otherEmail = $"{otherUsername}@example.test";
        var otherToken = await RegisterUserAndGetTokenAsync(
            otherUsername,
            otherEmail,
            "password123"
        );

        var slug = await CreateArticleAndGetSlugAsync(
            ownerToken,
            title: Unique("Ownership Comment Article"),
            description: "Article used for comment ownership authorization tests.",
            body: "Comments on this article must not be deletable by a different user.",
            tags: ["tl5-ownership", "comments"]
        );

        var commentBody = "This comment belongs to the original user and must not be deleted by another user.";
        var commentId = await CreateCommentAndGetIdAsync(ownerToken, slug, commentBody);

        var response = await SendWithTokenAsync(
            HttpMethod.Delete,
            $"/articles/{slug}/comments/{commentId}",
            otherToken
        );

        AssertForbiddenOrNotFound(response.StatusCode);

        var getResponse = await Client.GetAsync($"/articles/{slug}/comments");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var json = await ReadJsonAsync(getResponse);
        var comments = json
            .RootElement
            .GetProperty("comments")
            .EnumerateArray()
            .ToList();

        Assert.Contains(
            comments,
            comment =>
                comment.GetProperty("id").GetInt32() == commentId
                && comment.GetProperty("body").GetString() == commentBody
        );
    }

    private async Task<int> CreateCommentAndGetIdAsync(
        string token,
        string slug,
        string body
    )
    {
        var response = await SendWithTokenAsync(
            HttpMethod.Post,
            $"/articles/{slug}/comments",
            token,
            NewCommentPayload(body)
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var comment = json.RootElement.GetProperty("comment");
        var id = comment.GetProperty("id").GetInt32();

        Assert.True(id > 0);
        Assert.Equal(body, comment.GetProperty("body").GetString());

        return id;
    }

    private static void AssertForbiddenOrNotFound(HttpStatusCode statusCode)
    {
        Assert.True(
            statusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            $"Expected 403 Forbidden or 404 NotFound, but got {(int)statusCode} {statusCode}."
        );
    }
}
