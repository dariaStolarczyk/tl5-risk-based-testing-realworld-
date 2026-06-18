using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure.Errors;
using Conduit.IntegrationTests.Features.Articles;
using Conduit.IntegrationTests.Features.Users;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ArticleCreate = Conduit.Features.Articles.Create;
using CommentCreate = Conduit.Features.Comments.Create;
using CommentDelete = Conduit.Features.Comments.Delete;
using CommentList = Conduit.Features.Comments.List;

namespace Conduit.IntegrationTests.Features.Comments;

public class CommentTests : SliceFixture
{
    [Fact]
    public async Task Expect_Create_Comment_For_Existing_Article()
    {
        var article = await CreateDefaultArticleAsync();
        var slug = article.Slug ?? throw new InvalidOperationException("Article slug was null.");

        var command = NewCreateCommentCommand(slug, "This is a TL5 integration test comment.");

        var comment = await CommentHelpers.CreateComment(
            this,
            command,
            UserHelpers.DefaultUserName
        );

        Assert.NotNull(comment);
        Assert.True(comment.CommentId > 0);
        Assert.Equal("This is a TL5 integration test comment.", comment.Body);
        Assert.Equal(article.ArticleId, comment.ArticleId);
        Assert.Equal(UserHelpers.DefaultUserName, comment.Author?.Username);
    }

    [Fact]
    public async Task Expect_List_Comments_For_Article()
    {
        var article = await CreateDefaultArticleAsync();
        var slug = article.Slug ?? throw new InvalidOperationException("Article slug was null.");

        await CommentHelpers.CreateComment(
            this,
            NewCreateCommentCommand(slug, "First comment"),
            UserHelpers.DefaultUserName
        );

        await CommentHelpers.CreateComment(
            this,
            NewCreateCommentCommand(slug, "Second comment"),
            UserHelpers.DefaultUserName
        );

        var dbContext = GetDbContext();
        var handler = new CommentList.QueryHandler(dbContext);

        var result = await handler.Handle(
            new CommentList.Query(slug),
            CancellationToken.None
        );

        Assert.NotNull(result);
        Assert.NotNull(result.Comments);
        Assert.Equal(2, result.Comments.Count);
        Assert.Contains(result.Comments, comment => comment.Body == "First comment");
        Assert.Contains(result.Comments, comment => comment.Body == "Second comment");
        Assert.All(result.Comments, comment =>
        {
            Assert.Equal(article.ArticleId, comment.ArticleId);
            Assert.Equal(UserHelpers.DefaultUserName, comment.Author?.Username);
        });
    }

    [Fact]
    public async Task Expect_Delete_Own_Comment()
    {
        var article = await CreateDefaultArticleAsync();
        var slug = article.Slug ?? throw new InvalidOperationException("Article slug was null.");

        var comment = await CommentHelpers.CreateComment(
            this,
            NewCreateCommentCommand(slug, "Comment to delete"),
            UserHelpers.DefaultUserName
        );

        var dbContext = GetDbContext();
        var handler = new CommentDelete.QueryHandler(dbContext);

        await handler.Handle(
            new CommentDelete.Command(slug, comment.CommentId),
            CancellationToken.None
        );

        var deletedComment = await ExecuteDbContextAsync(db =>
            db.Comments.SingleOrDefaultAsync(x => x.CommentId == comment.CommentId)
        );

        var articleWithComments = await ExecuteDbContextAsync(db =>
            db.Articles
                .Include(x => x.Comments)
                .SingleAsync(x => x.ArticleId == article.ArticleId)
        );

        Assert.Null(deletedComment);
        Assert.DoesNotContain(
            articleWithComments.Comments,
            existingComment => existingComment.CommentId == comment.CommentId
        );
    }

    [Fact]
    public async Task Expect_Create_Comment_With_Unknown_Slug_To_Be_NotFound()
    {
        var command = NewCreateCommentCommand(
            slug: "unknown-article-slug",
            body: "This comment should not be created."
        );

        var dbContext = GetDbContext();
        var currentUserAccessor = new StubCurrentUserAccessor(UserHelpers.DefaultUserName);
        var handler = new CommentCreate.Handler(dbContext, currentUserAccessor);

        var exception = await Assert.ThrowsAsync<RestException>(() =>
            handler.Handle(command, CancellationToken.None)
        );

        Assert.Equal(HttpStatusCode.NotFound, exception.Code);
        Assert.NotNull(exception.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Expect_Create_Comment_With_Empty_Body_To_Fail_Validation(string? body)
    {
        var command = new CommentCreate.Command(
            new CommentCreate.Model(
                new CommentCreate.CommentData(body)
            ),
            "some-existing-or-non-existing-slug"
        );

        await Assert.ThrowsAsync<ValidationException>(() => SendAsync(command));
    }

    private async Task<Article> CreateDefaultArticleAsync()
    {
        var command = new ArticleCreate.Command(
            new ArticleCreate.ArticleData
            {
                Title = $"TL5 Comment Test Article {Guid.NewGuid():N}",
                Description = "Article used for comment integration tests.",
                Body = "This article is created as fixture data for comment tests.",
                TagList = ["tl5", "comments"],
            }
        );

        return await ArticleHelpers.CreateArticle(this, command);
    }

    private static CommentCreate.Command NewCreateCommentCommand(string slug, string? body)
    {
        return new CommentCreate.Command(
            new CommentCreate.Model(
                new CommentCreate.CommentData(body)
            ),
            slug
        );
    }
}
