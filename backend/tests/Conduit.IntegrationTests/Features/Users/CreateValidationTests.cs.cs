using System.Net;
using System.Threading.Tasks;
using Conduit.Features.Users;
using Conduit.Infrastructure.Errors;
using FluentValidation;
using Xunit;

namespace Conduit.IntegrationTests.Features.Users;

public class CreateValidationTests : SliceFixture
{
    [Fact]
    public async Task Expect_Create_User_With_Duplicate_Username_To_Be_BadRequest()
    {
        await SendAsync(
            new Create.Command(
                new Create.UserData(
                    "duplicate-username",
                    "first@example.com",
                    "password"
                )
            )
        );

        var command = new Create.Command(
            new Create.UserData(
                "duplicate-username",
                "second@example.com",
                "password"
            )
        );

        var exception = await Assert.ThrowsAsync<RestException>(() => SendAsync(command));

        Assert.Equal(HttpStatusCode.BadRequest, exception.Code);
        Assert.NotNull(exception.Errors);
    }

    [Fact]
    public async Task Expect_Create_User_With_Duplicate_Email_To_Be_BadRequest()
    {
        await SendAsync(
            new Create.Command(
                new Create.UserData(
                    "first-username",
                    "duplicate@example.com",
                    "password"
                )
            )
        );

        var command = new Create.Command(
            new Create.UserData(
                "second-username",
                "duplicate@example.com",
                "password"
            )
        );

        var exception = await Assert.ThrowsAsync<RestException>(() => SendAsync(command));

        Assert.Equal(HttpStatusCode.BadRequest, exception.Code);
        Assert.NotNull(exception.Errors);
    }

    [Theory]
    [InlineData(null, "email@example.com", "password")]
    [InlineData("", "email@example.com", "password")]
    [InlineData("username", null, "password")]
    [InlineData("username", "", "password")]
    [InlineData("username", "email@example.com", null)]
    [InlineData("username", "email@example.com", "")]
    public async Task Expect_Create_User_With_Missing_Required_Field_To_Fail_Validation(
        string? username,
        string? email,
        string? password
    )
    {
        var command = new Create.Command(
            new Create.UserData(username, email, password)
        );

        await Assert.ThrowsAsync<ValidationException>(() => SendAsync(command));
    }
}
