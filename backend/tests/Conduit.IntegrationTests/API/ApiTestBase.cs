using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Conduit.IntegrationTests.Api;

public abstract class ApiTestBase : IDisposable
{
    private readonly ApiFactory _factory;

    protected HttpClient Client { get; }

    protected ApiTestBase()
    {
        _factory = new ApiFactory();
        Client = _factory.CreateClient();
    }

    protected async Task<string> RegisterUserAndGetTokenAsync(
        string username,
        string email,
        string password
    )
    {
        var response = await Client.PostAsJsonAsync(
            "/users",
            new
            {
                user = new
                {
                    username,
                    email,
                    password
                }
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);

        var token = json
            .RootElement
            .GetProperty("user")
            .GetProperty("token")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        return token;
    }

    protected async Task<JsonDocument> CreateArticleAsync(
        string token,
        string title,
        string description,
        string body,
        string[] tags
    )
    {
        var response = await SendWithTokenAsync(
            HttpMethod.Post,
            "/articles",
            token,
            NewArticlePayload(title, description, body, tags)
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await ReadJsonAsync(response);
    }

    protected async Task<string> CreateArticleAndGetSlugAsync(
        string token,
        string? title = null,
        string? description = null,
        string? body = null,
        string[]? tags = null
    )
    {
        using var json = await CreateArticleAsync(
            token,
            title ?? Unique("api-contract-article"),
            description ?? "Article used as API contract test fixture.",
            body ?? "This article exists only as fixture data for HTTP API tests.",
            tags ?? ["tl5-api"]
        );

        var slug = json
            .RootElement
            .GetProperty("article")
            .GetProperty("slug")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(slug));

        return slug;
    }

    protected async Task<HttpResponseMessage> SendWithTokenAsync(
        HttpMethod method,
        string url,
        string token,
        object? body = null
    )
    {
        using var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Token", token);

        return await Client.SendAsync(request);
    }

    protected static object NewArticlePayload(
        string title,
        string description,
        string body,
        string[] tags
    ) =>
        new
        {
            article = new
            {
                title,
                description,
                body,
                tagList = tags
            }
        };

    protected static object NewCommentPayload(string body) =>
        new
        {
            comment = new
            {
                body
            }
        };

    protected static object NewUserUpdatePayload(
        string? email = null,
        string? bio = null,
        string? image = null
    ) =>
        new
        {
            user = new
            {
                email,
                bio,
                image
            }
        };

    protected static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        Assert.False(
            string.IsNullOrWhiteSpace(content),
            $"Response body was empty. Status code: {(int)response.StatusCode} {response.StatusCode}."
        );

        return JsonDocument.Parse(content);
    }

    protected static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    public void Dispose()
    {
        Client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
