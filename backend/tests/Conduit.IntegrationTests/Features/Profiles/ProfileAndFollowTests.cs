using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Conduit.Domain;
using Conduit.Features.Profiles;
using Conduit.Infrastructure.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ArticleList = Conduit.Features.Articles.List;
using FollowerAdd = Conduit.Features.Followers.Add;
using FollowerDelete = Conduit.Features.Followers.Delete;
using ProfileDetails = Conduit.Features.Profiles.Details;

namespace Conduit.IntegrationTests.Features.Profiles;

public class ProfileAndFollowTests : SliceFixture
{
    [Fact]
    public async Task Expect_Profile_Details_Returns_Following_False_By_Default()
    {
        var currentUser = BuildPerson("profile-current-user");
        var targetUser = BuildPerson("profile-target-user");

        await InsertAsync(currentUser, targetUser);

        var handler = new ProfileDetails.QueryHandler(
            await CreateProfileReaderAsync(currentUser.Username!)
        );

        var result = await handler.Handle(
            new ProfileDetails.Query(targetUser.Username!),
            CancellationToken.None
        );

        Assert.Equal(targetUser.Username, result.Profile.Username);
        Assert.False(result.Profile.IsFollowed);
    }

    [Fact]
    public async Task Expect_Follow_User_Sets_Following_True()
    {
        var currentUser = BuildPerson("follow-current-user");
        var targetUser = BuildPerson("follow-target-user");

        await InsertAsync(currentUser, targetUser);

        var handler = new FollowerAdd.QueryHandler(
            GetDbContext(),
            new StubCurrentUserAccessor(currentUser.Username!),
            await CreateProfileReaderAsync(currentUser.Username!)
        );

        var result = await handler.Handle(
            new FollowerAdd.Command(targetUser.Username!),
            CancellationToken.None
        );

        Assert.Equal(targetUser.Username, result.Profile.Username);
        Assert.True(result.Profile.IsFollowed);

        var followCount = await ExecuteDbContextAsync(db =>
            db.FollowedPeople.CountAsync(follow =>
                follow.ObserverId == currentUser.PersonId &&
                follow.TargetId == targetUser.PersonId
            )
        );

        Assert.Equal(1, followCount);
    }

    [Fact]
    public async Task Expect_Unfollow_User_Sets_Following_False()
    {
        var currentUser = BuildPerson("unfollow-current-user");
        var targetUser = BuildPerson("unfollow-target-user");

        await InsertAsync(currentUser, targetUser);

        var addHandler = new FollowerAdd.QueryHandler(
            GetDbContext(),
            new StubCurrentUserAccessor(currentUser.Username!),
            await CreateProfileReaderAsync(currentUser.Username!)
        );

        await addHandler.Handle(
            new FollowerAdd.Command(targetUser.Username!),
            CancellationToken.None
        );

        var deleteHandler = new FollowerDelete.QueryHandler(
            GetDbContext(),
            new StubCurrentUserAccessor(currentUser.Username!),
            await CreateProfileReaderAsync(currentUser.Username!)
        );

        var result = await deleteHandler.Handle(
            new FollowerDelete.Command(targetUser.Username!),
            CancellationToken.None
        );

        Assert.Equal(targetUser.Username, result.Profile.Username);
        Assert.False(result.Profile.IsFollowed);

        var followCount = await ExecuteDbContextAsync(db =>
            db.FollowedPeople.CountAsync(follow =>
                follow.ObserverId == currentUser.PersonId &&
                follow.TargetId == targetUser.PersonId
            )
        );

        Assert.Equal(0, followCount);
    }

    [Fact]
    public async Task Expect_Follow_Unknown_User_To_Be_NotFound()
    {
        var currentUser = BuildPerson("follow-unknown-current-user");

        await InsertAsync(currentUser);

        var handler = new FollowerAdd.QueryHandler(
            GetDbContext(),
            new StubCurrentUserAccessor(currentUser.Username!),
            await CreateProfileReaderAsync(currentUser.Username!)
        );

        var exception = await Assert.ThrowsAsync<RestException>(() =>
            handler.Handle(
                new FollowerAdd.Command("unknown-profile-user"),
                CancellationToken.None
            )
        );

        Assert.Equal(HttpStatusCode.NotFound, exception.Code);
        Assert.NotNull(exception.Errors);
    }

    [Fact]
    public async Task Expect_Feed_Returns_Articles_From_Followed_Users()
    {
        var currentUser = BuildPerson("feed-current-user");
        var followedAuthor = BuildPerson("feed-followed-author");
        var otherAuthor = BuildPerson("feed-other-author");

        var followedArticle = BuildArticle(
            followedAuthor,
            slug: "followed-author-feed-article",
            title: "Followed author feed article",
            createdAt: new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        );

        var otherArticle = BuildArticle(
            otherAuthor,
            slug: "other-author-feed-article",
            title: "Other author feed article",
            createdAt: new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc)
        );

        await InsertAsync(
            currentUser,
            followedAuthor,
            otherAuthor,
            followedArticle,
            otherArticle
        );

        var followHandler = new FollowerAdd.QueryHandler(
            GetDbContext(),
            new StubCurrentUserAccessor(currentUser.Username!),
            await CreateProfileReaderAsync(currentUser.Username!)
        );

        await followHandler.Handle(
            new FollowerAdd.Command(followedAuthor.Username!),
            CancellationToken.None
        );

        var listHandler = new ArticleList.QueryHandler(
            GetDbContext(),
            new StubCurrentUserAccessor(currentUser.Username!)
        );

        var result = await listHandler.Handle(
            new ArticleList.Query(
                Tag: null,
                Author: null,
                FavoritedUsername: null,
                Limit: null,
                Offset: null,
                IsFeed: true
            ),
            CancellationToken.None
        );

        var slugs = result.Articles.Select(article => article.Slug).ToArray();

        Assert.Equal(1, result.ArticlesCount);
        Assert.Single(result.Articles);
        Assert.Contains("followed-author-feed-article", slugs);
        Assert.DoesNotContain("other-author-feed-article", slugs);

        Assert.All(result.Articles, article =>
            Assert.Equal(followedAuthor.Username, article.Author?.Username)
        );
    }

    private async Task<ProfileReader> CreateProfileReaderAsync(string currentUsername)
    {
        var mapper = await ExecuteScopeAsync(sp =>
            Task.FromResult(sp.GetRequiredService<IMapper>())
        );

        return new ProfileReader(
            GetDbContext(),
            new StubCurrentUserAccessor(currentUsername),
            mapper
        );
    }

    private static Person BuildPerson(string username)
    {
        return new Person
        {
            Username = username,
            Email = $"{username}@example.com"
        };
    }

    private static Article BuildArticle(
        Person author,
        string slug,
        string title,
        DateTime createdAt
    )
    {
        return new Article
        {
            Slug = slug,
            Title = title,
            Description = $"Description for {title}",
            Body = $"Body for {title}",
            Author = author,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }
}
