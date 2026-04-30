using Microsoft.EntityFrameworkCore;
using SportsLeague.Domain.Entities;

namespace SportsLeague.Infrastructure.Persistence;

public sealed class LeagueDbContext(DbContextOptions<LeagueDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Match> Matches => Set<Match>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Team>(b =>
        {
            b.Property(x => x.Name).HasMaxLength(200);
            b.Property(x => x.City).HasMaxLength(200);
            b.Property(x => x.LogoUrl).HasMaxLength(2000);
            b.Property(x => x.CoachName).HasMaxLength(200);

            b.HasMany(x => x.Players)
                .WithOne(x => x.Team)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.HomeMatches)
                .WithOne(x => x.HomeTeam)
                .HasForeignKey(x => x.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(x => x.AwayMatches)
                .WithOne(x => x.AwayTeam)
                .HasForeignKey(x => x.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Player>(b =>
        {
            b.Property(x => x.FirstName).HasMaxLength(200);
            b.Property(x => x.LastName).HasMaxLength(200);
            b.Property(x => x.Position).HasMaxLength(100);

            b.HasIndex(x => new { x.TeamId, x.JerseyNumber })
                .IsUnique();
        });

        modelBuilder.Entity<Match>(b =>
        {
            b.Property(x => x.Venue).HasMaxLength(400);

            b.HasIndex(x => new { x.HomeTeamId, x.MatchDate });
            b.HasIndex(x => new { x.AwayTeamId, x.MatchDate });
        });
    }
}

