using System;
using System.Net;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Features.Users;
using Conduit.Infrastructure.Errors;
using Conduit.Infrastructure.Security;
using Xunit;

namespace Conduit.IntegrationTests.Features.Users;

public class LoginUsernameTests : SliceFixture
{
    [Fact]
    public async Task Expect_Login_With_Username()
    {
        var salt = Guid.NewGuid().ToByteArray();

        var person = new Person
        {
            Username = "username-login-test",
            Email = "username-login-test@example.com",
            Hash = await new PasswordHasher().Hash("password", salt),
            Salt = salt,
        };

        await InsertAsync(person);

        var command = new Login.Command(
            new Login.UserData
            {
                Username = person.Username,
                Password = "password",
            }
        );

        var result = await SendAsync(command);

        Assert.NotNull(result.User);
        Assert.Equal(person.Username, result.User.Username);
        Assert.Equal(person.Email, result.User.Email);
        Assert.NotNull(result.User.Token);
    }

    [Fact]
    public async Task Expect_Login_With_Wrong_Password_To_Be_Unauthorized()
    {
        var salt = Guid.NewGuid().ToByteArray();

        var person = new Person
        {
            Username = "wrong-password-user",
            Email = "wrong-password-user@example.com",
            Hash = await new PasswordHasher().Hash("correct-password", salt),
            Salt = salt,
        };

        await InsertAsync(person);

        var command = new Login.Command(
            new Login.UserData
            {
                Username = person.Username,
                Password = "wrong-password",
            }
        );

        var exception = await Assert.ThrowsAsync<RestException>(() => SendAsync(command));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.Code);
    }
}
