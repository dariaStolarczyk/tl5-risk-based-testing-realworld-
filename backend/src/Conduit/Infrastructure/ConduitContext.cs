using System.Data;
using Conduit.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Conduit.Infrastructure;

public class ConduitContext(DbContextOptions options) : DbContext(options)
{
    private IDbContextTransaction? _currentTransaction;

    public DbSet<Article> Articles { get; init; } = null!;
    public DbSet<Comment> Comments { get; init; } = null!;
    public DbSet<Person> Persons { get; init; } = null!;
    public DbSet<Tag> Tags { get; init; } = null!;
    public DbSet<ArticleTag> ArticleTags { get; init; } = null!;
    public DbSet<ArticleFavorite> ArticleFavorites { get; init; } = null!;
    public DbSet<FollowedPeople> FollowedPeople { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>(b =>
        {
            b.Property(p => p.Username).IsRequired();
            b.Property(p => p.Email).IsRequired();

            b.HasIndex(p => p.Username).IsUnique();
            b.HasIndex(p => p.Email).IsUnique();
        });

        modelBuilder.Entity<ArticleTag>(b =>
        {
            b.HasKey(t => new { t.ArticleId, t.TagId });

            b.HasOne(pt => pt.Article)
                .WithMany(p => p.ArticleTags)
                .HasForeignKey(pt => pt.ArticleId);

            b.HasOne(pt => pt.Tag)
                .WithMany(t => t.ArticleTags)
                .HasForeignKey(pt => pt.TagId);
        });

        modelBuilder.Entity<ArticleFavorite>(b =>
        {
            b.HasKey(t => new { t.ArticleId, t.PersonId });

            b.HasOne(pt => pt.Article)
                .WithMany(p => p.ArticleFavorites)
                .HasForeignKey(pt => pt.ArticleId);

            b.HasOne(pt => pt.Person)
                .WithMany(t => t.ArticleFavorites)
                .HasForeignKey(pt => pt.PersonId);
        });

        modelBuilder.Entity<FollowedPeople>(b =>
        {
            b.HasKey(t => new { t.ObserverId, t.TargetId });

            b.HasOne(pt => pt.Observer)
                .WithMany(p => p.Following)
                .HasForeignKey(pt => pt.ObserverId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(pt => pt.Target)
                .WithMany(p => p.Followers)
                .HasForeignKey(pt => pt.TargetId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    #region Transaction Handling

    public void BeginTransaction()
    {
        if (_currentTransaction != null)
        {
            return;
        }

        if (!Database.IsInMemory())
        {
            _currentTransaction = Database.BeginTransaction(IsolationLevel.ReadCommitted);
        }
    }

    public void CommitTransaction()
    {
        try
        {
            _currentTransaction?.Commit();
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    public void RollbackTransaction()
    {
        try
        {
            _currentTransaction?.Rollback();
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    #endregion
}
