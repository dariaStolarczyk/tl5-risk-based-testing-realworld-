using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Conduit.IntegrationTests.Api;

public sealed class CommentApiContractTests : ApiTestBase
{
    [Fact]
    public async Task PostComment_WithValidToken_ReturnsCommentEnvelope()
    {
        var username = Unique("comment-user");
        var email = $"{username}@example.test";
        var token = await RegisterUserAndGetTokenAsync(username, email, "password123");
        var slug = await CreateCommentArticleAndGetSlugAsync(token);

        var commentBody = "This comment was created through the real HTTP API.";

        var response = await SendWithTokenAsync(
            HttpMethod.Post,
            $"/articles/{slug}/comments",
            token,
            NewCommentPayload(commentBody)
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var comment = json.RootElement.GetProperty("comment");

        Assert.True(comment.GetProperty("id").GetInt32() > 0);
        Assert.Equal(commentBody, comment.GetProperty("body").GetString());

        var author = comment.GetProperty("author");
        Assert.Equal(username, author.GetProperty("username").GetString());
    }

    [Fact]
    public async Task GetComments_ForArticle_ReturnsCommentsEnvelope()
    {
        var username = Unique("comment-list-user");
        var email = $"{username}@example.test";
        var token = await RegisterUserAndGetTokenAsync(username, email, "password123");
        var slug = await CreateCommentArticleAndGetSlugAsync(token);

        var commentBody = "This comment should be returned by GET comments.";
        var createdCommentId = await CreateCommentAndGetIdAsync(token, slug, commentBody);

        var response = await Client.GetAsync($"/articles/{slug}/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("comments", out var commentsElement));

        var comments = commentsElement.EnumerateArray().ToList();

        Assert.Contains(
            comments,
            comment =>
                comment.GetProperty("id").GetInt32() == createdCommentId
                && comment.GetProperty("body").GetString() == commentBody
        );

        Assert.Contains(
            comments,
            comment =>
                comment.GetProperty("author").GetProperty("username").GetString() == username
        );
    }

    [Fact]
    public async Task DeleteComment_WithValidToken_ReturnsSuccess()
    {
        var username = Unique("comment-delete-user");
        var email = $"{username}@example.test";
        var token = await RegisterUserAndGetTokenAsync(username, email, "password123");
        var slug = await CreateCommentArticleAndGetSlugAsync(token);

        var commentBody = "This comment should be deleted through the HTTP API.";
        var commentId = await CreateCommentAndGetIdAsync(token, slug, commentBody);

        var response = await SendWithTokenAsync(
            HttpMethod.Delete,
            $"/articles/{slug}/comments/{commentId}",
            token
        );

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent,
            $"Expected 200 OK or 204 NoContent, but got {(int)response.StatusCode} {response.StatusCode}."
        );

        var getResponse = await Client.GetAsync($"/articles/{slug}/comments");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var json = await ReadJsonAsync(getResponse);
        var comments = json
            .RootElement
            .GetProperty("comments")
            .EnumerateArray()
            .ToList();

        Assert.DoesNotContain(
            comments,
            comment => comment.GetProperty("id").GetInt32() == commentId
        );
    }

    [Fact]
    public async Task PostComment_WithEmptyBody_ReturnsValidationError()
    {
        var username = Unique("comment-empty-body-user");
        var email = $"{username}@example.test";
        var token = await RegisterUserAndGetTokenAsync(username, email, "password123");
        var slug = await CreateCommentArticleAndGetSlugAsync(token);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/articles/{slug}/comments"
        )
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Token", token);

        var response = await Client.SendAsync(request);

        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Expected 400 BadRequest or 422 UnprocessableEntity, but got {(int)response.StatusCode} {response.StatusCode}."
        );

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("errors", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostComment_ForUnknownSlug_ReturnsNotFound()
    {
        var username = Unique("comment-unknown-slug-user");
        var email = $"{username}@example.test";
        var token = await RegisterUserAndGetTokenAsync(username, email, "password123");

        var unknownSlug = Unique("unknown-article-slug");

        var response = await SendWithTokenAsync(
            HttpMethod.Post,
            $"/articles/{unknownSlug}/comments",
            token,
            NewCommentPayload("This comment should not be created because the article does not exist.")
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("errors", body, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> CreateCommentArticleAndGetSlugAsync(string token)
    {
        return await CreateArticleAndGetSlugAsync(
            token,
            title: Unique("Comment API Contract Article"),
            description: "Article used for comment API contract tests.",
            body: "This article exists only as fixture data for comment HTTP tests.",
            tags: ["tl5-comments"]
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
}
