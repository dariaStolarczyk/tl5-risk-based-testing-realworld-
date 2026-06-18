using Conduit.Infrastructure;
using Xunit;

namespace Conduit.IntegrationTests.Infrastructure;

public class SlugTests
{
    [Fact]
    public void Expect_GenerateSlug_With_Null_To_Return_Null()
    {
        string? phrase = null;

        var slug = phrase.GenerateSlug();

        Assert.Null(slug);
    }

    [Fact]
    public void Expect_GenerateSlug_To_Lowercase_And_Replace_Spaces_With_Hyphens()
    {
        var slug = "Hello World Test".GenerateSlug();

        Assert.Equal("hello-world-test", slug);
    }

    [Fact]
    public void Expect_GenerateSlug_To_Remove_Invalid_Characters()
    {
        var slug = "Hello, World! This is a Test.".GenerateSlug();

        Assert.Equal("hello-world-this-is-a-test", slug);
    }

    [Fact]
    public void Expect_GenerateSlug_To_Collapse_Multiple_Spaces()
    {
        var slug = "  Hello     World     Test  ".GenerateSlug();

        Assert.Equal("hello-world-test", slug);
    }

    [Fact]
    public void Expect_GenerateSlug_To_Cut_Result_To_45_Characters()
    {
        var phrase = new string('A', 60);

        var slug = phrase.GenerateSlug();

        Assert.NotNull(slug);
        Assert.Equal(45, slug.Length);
        Assert.Equal(new string('a', 45), slug);
    }
}
