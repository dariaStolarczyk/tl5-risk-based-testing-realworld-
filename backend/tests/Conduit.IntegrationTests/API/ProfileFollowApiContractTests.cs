using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Conduit.IntegrationTests.Api;

public sealed class ProfileFollowApiContractTests : ApiTestBase
{
    [Fact]
    public async Task GetProfile_ForExistingUser_ReturnsProfileEnvelope()
    {
        var username = Unique("profile-user");
        var email = $"{username}@example.test";

        await RegisterUserAndGetTokenAsync(username, email, "password123");

        var response = await Client.GetAsync($"/profiles/{username}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var profile = json.RootElement.GetProperty("profile");

        Assert.Equal(username, profile.GetProperty("username").GetString());
        Assert.True(profile.TryGetProperty("following", out var followingElement));
        Assert.False(followingElement.GetBoolean());
    }

    [Fact]
    public async Task PostFollow_WithValidToken_ReturnsProfileEnvelopeWithFollowingTrue()
    {
        var targetUsername = Unique("follow-target");
        var targetEmail = $"{targetUsername}@example.test";

        await RegisterUserAndGetTokenAsync(targetUsername, targetEmail, "password123");

        var currentUsername = Unique("follow-current");
        var currentEmail = $"{currentUsername}@example.test";
        var token = await RegisterUserAndGetTokenAsync(
            currentUsername,
            currentEmail,
            "password123"
        );

        var response = await SendWithTokenAsync(
            HttpMethod.Post,
            $"/profiles/{targetUsername}/follow",
            token
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var profile = json.RootElement.GetProperty("profile");

        Assert.Equal(targetUsername, profile.GetProperty("username").GetString());
        Assert.True(profile.GetProperty("following").GetBoolean());
    }

    [Fact]
    public async Task DeleteFollow_WithValidToken_ReturnsProfileEnvelopeWithFollowingFalse()
    {
        var targetUsername = Unique("unfollow-target");
        var targetEmail = $"{targetUsername}@example.test";

        await RegisterUserAndGetTokenAsync(targetUsername, targetEmail, "password123");

        var currentUsername = Unique("unfollow-current");
        var currentEmail = $"{currentUsername}@example.test";
        var token = await RegisterUserAndGetTokenAsync(
            currentUsername,
            currentEmail,
            "password123"
        );

        var followResponse = await SendWithTokenAsync(
            HttpMethod.Post,
            $"/profiles/{targetUsername}/follow",
            token
        );

        Assert.Equal(HttpStatusCode.OK, followResponse.StatusCode);

        var response = await SendWithTokenAsync(
            HttpMethod.Delete,
            $"/profiles/{targetUsername}/follow",
            token
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = await ReadJsonAsync(response);
        var profile = json.RootElement.GetProperty("profile");

        Assert.Equal(targetUsername, profile.GetProperty("username").GetString());
        Assert.False(profile.GetProperty("following").GetBoolean());
    }

    [Fact]
    public async Task PostFollow_ForUnknownUser_ReturnsNotFound()
    {
        var currentUsername = Unique("follow-unknown-current");
        var currentEmail = $"{currentUsername}@example.test";
        var token = await RegisterUserAndGetTokenAsync(
            currentUsername,
            currentEmail,
            "password123"
        );

        var unknownUsername = Unique("unknown-profile-user");

        var response = await SendWithTokenAsync(
            HttpMethod.Post,
            $"/profiles/{unknownUsername}/follow",
            token
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("errors", body, StringComparison.OrdinalIgnoreCase);
    }
}
