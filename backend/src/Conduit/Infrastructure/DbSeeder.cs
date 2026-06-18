using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Conduit.Infrastructure;

public static class DbSeeder
{
    private static readonly JsonSerializerOptions SEED_JSON_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true
    };
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ConduitContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var environment = services.GetRequiredService<IHostEnvironment>();

        await context.Database.EnsureCreatedAsync();

        if (await context.Persons.AnyAsync(x => x.Email == "demo@conduit.local"))
        {
            return;
        }

        var seedPath = Path.Combine(
            environment.ContentRootPath,
            "Infrastructure",
            "Seed",
            "seedData.json"
        );

        if (!File.Exists(seedPath))
        {
            throw new FileNotFoundException($"Seed data file not found: {seedPath}");
        }

        var json = await File.ReadAllTextAsync(seedPath);

        var seedData = JsonSerializer.Deserialize<SeedData>(
            json, SEED_JSON_OPTIONS
        );

        if (seedData is null)
        {
            throw new InvalidOperationException("Seed data could not be loaded.");
        }

        var persons = new Dictionary<string, Person>();

        foreach (var seedUser in seedData.Users)
        {
            var salt = Guid.NewGuid().ToByteArray();

            var person = new Person
            {
                Username = seedUser.Username,
                Email = seedUser.Email,
                Bio = seedUser.Bio,
                Image = seedUser.Image,
                Salt = salt,
                Hash = await passwordHasher.Hash(seedUser.Password, salt)
            };

            persons[seedUser.Username] = person;
            await context.Persons.AddAsync(person);
        }

        var tags = new Dictionary<string, Tag>();

        foreach (var seedTag in seedData.Tags)
        {
            var tag = new Tag { TagId = seedTag };
            tags[seedTag] = tag;
            await context.Tags.AddAsync(tag);
        }

        foreach (var seedArticle in seedData.Articles)
        {
            var article = new Article
            {
                Title = seedArticle.Title,
                Description = seedArticle.Description,
                Body = seedArticle.Body,
                Slug = seedArticle.Slug,
                Author = persons[seedArticle.Author],
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.Articles.AddAsync(article);

            foreach (var tagName in seedArticle.Tags)
            {
                await context.ArticleTags.AddAsync(new ArticleTag
                {
                    Article = article,
                    Tag = tags[tagName]
                });
            }

            foreach (var seedComment in seedArticle.Comments ?? [])
            {
                await context.Comments.AddAsync(new Comment
                {
                    Body = seedComment.Body,
                    Author = persons[seedComment.Author],
                    Article = article,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            foreach (var username in seedArticle.FavoritedBy ?? [])
            {
                await context.ArticleFavorites.AddAsync(new ArticleFavorite
                {
                    Article = article,
                    Person = persons[username]
                });
            }
        }

        foreach (var follow in seedData.Follows)
        {
            await context.FollowedPeople.AddAsync(new FollowedPeople
            {
                Observer = persons[follow.Observer],
                Target = persons[follow.Target]
            });
        }

        await context.SaveChangesAsync();
    }
}

public sealed class SeedData
{
    public List<SeedUser> Users { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public List<SeedArticle> Articles { get; set; } = [];
    public List<SeedFollow> Follows { get; set; } = [];
}

public sealed class SeedUser
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Bio { get; set; } = "";
    public string Image { get; set; } = "";
}

public sealed class SeedArticle
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Body { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Author { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public List<SeedComment>? Comments { get; set; }
    public List<string>? FavoritedBy { get; set; }
}

public sealed class SeedComment
{
    public string Author { get; set; } = "";
    public string Body { get; set; } = "";
}

public sealed class SeedFollow
{
    public string Observer { get; set; } = "";
    public string Target { get; set; } = "";
}
