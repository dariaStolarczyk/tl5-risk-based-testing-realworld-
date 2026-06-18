using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Conduit.IntegrationTests.Api;

public sealed class ApiContractTests : ApiTestBase
{
    [Fact]
    public async Task PostLogin_WithValidCredentials_ReturnsUserEnvelopeWithToken()
    {
        var username = Unique("login-user");
        var email = $"{username}@example.test";
        const string password = "password123";

        await RegisterUserAndGetTokenAsync(username, email, password);

        var response = await Client.PostAsJsonAsync(
            "/users/login",
            new
            {
                user = new
                {
                    email,
                    password
                }
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var user = json.RootElement.GetProperty("user");

        Assert.Equal(username, user.GetProperty("username").GetString());
        Assert.Equal(email, user.GetProperty("email").GetString());
        Assert.False(string.IsNullOrWhiteSpace(user.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task PostLogin_WithInvalidCredentials_ReturnsUnauthorizedOrValidationError()
    {
        var response = await Client.PostAsJsonAsync(
            "/users/login",
            new
            {
                user = new
                {
                    email = $"missing-{Guid.NewGuid():N}@example.test",
                    password = "wrong-password"
                }
            }
        );

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized
                or HttpStatusCode.BadRequest
                or HttpStatusCode.UnprocessableEntity,
            $"Expected 401, 400 or 422, but got {(int)response.StatusCode} {response.StatusCode}."
        );

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("errors", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostArticle_WithValidToken_CreatesArticleAndReturnsArticleEnvelope()
    {
        var username = Unique("article-user");
        var email = $"{username}@example.test";
        var token = await RegisterUserAndGetTokenAsync(username, email, "password123");

        var title = Unique("HTTP API Contract Article");
        var description = "Created through a real HTTP API request.";
        var body = "This test verifies routing, authentication, serialization and the article envelope.";
        var tag = Unique("tl5-api");

        var response = await SendWithTokenAsync(
            HttpMethod.Post,
            "/articles",
            token,
            NewArticlePayload(title, description, body, [tag])
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var article = json.RootElement.GetProperty("article");

        Assert.Equal(title, article.GetProperty("title").GetString());
        Assert.Equal(description, article.GetProperty("description").GetString());
        Assert.Equal(body, article.GetProperty("body").GetString());
        Assert.False(string.IsNullOrWhiteSpace(article.GetProperty("slug").GetString()));

        var tagList = article
            .GetProperty("tagList")
            .EnumerateArray()
            .Select(x => x.GetString())
            .ToList();

        Assert.Contains(tag, tagList);
    }

    [Fact]
    public async Task GetArticles_WithLimitOffsetAndTag_ReturnsFilteredArticleEnvelope()
    {
        var username = Unique("list-user");
        var email = $"{username}@example.test";
        var token = await RegisterUserAndGetTokenAsync(username, email, "password123");

        var targetTag = Unique("tl5-api-filter");
        var otherTag = Unique("tl5-other-filter");

        var matchingTitle = Unique("Matching Article");
        var nonMatchingTitle = Unique("Other Article");

        using var matchingArticle = await CreateArticleAsync(
            token,
            matchingTitle,
            "Matching description",
            "Matching body",
            [targetTag]
        );

        using var nonMatchingArticle = await CreateArticleAsync(
            token,
            nonMatchingTitle,
            "Other description",
            "Other body",
            [otherTag]
        );

        var response = await Client.GetAsync(
            $"/articles?tag={Uri.EscapeDataString(targetTag)}&limit=20&offset=0"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("articles", out var articlesElement));
        Assert.True(root.TryGetProperty("articlesCount", out var articlesCountElement));
        Assert.True(articlesCountElement.GetInt32() >= 1);

        var articles = articlesElement.EnumerateArray().ToList();

        Assert.Contains(
            articles,
            article => article.GetProperty("title").GetString() == matchingTitle
        );

        Assert.DoesNotContain(
            articles,
            article => article.GetProperty("title").GetString() == nonMatchingTitle
        );

        Assert.All(
            articles,
            article =>
            {
                var tags = article
                    .GetProperty("tagList")
                    .EnumerateArray()
                    .Select(x => x.GetString())
                    .ToList();

                Assert.Contains(targetTag, tags);
            }
        );
    }
}
