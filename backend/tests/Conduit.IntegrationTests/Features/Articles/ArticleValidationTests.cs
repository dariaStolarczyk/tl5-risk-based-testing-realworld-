using System.Threading.Tasks;
using FluentValidation;
using Xunit;
using ArticleCreate = Conduit.Features.Articles.Create;

namespace Conduit.IntegrationTests.Features.Articles;

public class ArticleValidationTests : SliceFixture
{
    [Theory]
    [InlineData(null, "description", "body")]
    [InlineData("", "description", "body")]
    [InlineData("title", null, "body")]
    [InlineData("title", "", "body")]
    [InlineData("title", "description", null)]
    [InlineData("title", "description", "")]
    public async Task Expect_Create_Article_With_Missing_Required_Field_To_Fail_Validation(
        string? title,
        string? description,
        string? body
    )
    {
        var command = new ArticleCreate.Command(
            new ArticleCreate.ArticleData
            {
                Title = title,
                Description = description,
                Body = body,
                TagList = ["testing"],
            }
        );

        await Assert.ThrowsAsync<ValidationException>(() => SendAsync(command));
    }

    [Fact]
    public async Task Expect_Create_Article_With_Null_Article_To_Fail_Validation()
    {
        var command = new ArticleCreate.Command(null!);

        await Assert.ThrowsAsync<ValidationException>(() => SendAsync(command));
    }
}
