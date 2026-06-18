using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Conduit.IntegrationTests.Api;

public sealed class CurrentUserApiContractTests : ApiTestBase
{
    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsUserEnvelope()
    {
        var username = Unique("current-user");
        var email = $"{username}@example.test";
        var token = await RegisterUserAndGetTokenAsync(username, email, "password123");

        var response = await SendWithTokenAsync(HttpMethod.Get, "/user", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var user = json.RootElement.GetProperty("user");

        Assert.Equal(username, user.GetProperty("username").GetString());
        Assert.Equal(email, user.GetProperty("email").GetString());
        Assert.False(string.IsNullOrWhiteSpace(user.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task PutCurrentUser_WithValidToken_UpdatesUserAndReturnsUserEnvelope()
    {
        var username = Unique("current-user-update");
        var email = $"{username}@example.test";
        var token = await RegisterUserAndGetTokenAsync(username, email, "password123");

        var updatedEmail = $"updated-{username}@example.test";
        var updatedBio = "Updated through PUT /user in an API contract test.";
        var updatedImage = "https://example.test/avatar.png";

        var response = await SendWithTokenAsync(
            HttpMethod.Put,
            "/user",
            token,
            NewUserUpdatePayload(
                email: updatedEmail,
                bio: updatedBio,
                image: updatedImage
            )
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var user = json.RootElement.GetProperty("user");

        Assert.Equal(username, user.GetProperty("username").GetString());
        Assert.Equal(updatedEmail, user.GetProperty("email").GetString());
        Assert.Equal(updatedBio, user.GetProperty("bio").GetString());
        Assert.Equal(updatedImage, user.GetProperty("image").GetString());
        Assert.False(string.IsNullOrWhiteSpace(user.GetProperty("token").GetString()));
    }
}
