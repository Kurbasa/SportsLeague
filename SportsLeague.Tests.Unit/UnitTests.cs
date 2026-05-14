using FluentAssertions;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Enums;
using SportsLeague.Domain.Errors;
using SportsLeague.Infrastructure.Services;

namespace SportsLeague.Tests.Unit;

public sealed class UnitTests
{
    [Fact]
    public async Task Match_cannot_be_home_and_away_same_team()
    {
        await using var db = TestDbFactory.CreateContext();
        var t = NewTeam("Solo");
        db.Teams.Add(t);
        await db.SaveChangesAsync();

        var act = () => new MatchService(db).ScheduleAsync(
            new Match { HomeTeamId = t.Id, AwayTeamId = t.Id, MatchDate = DateTimeOffset.UtcNow.AddDays(1), Venue = "V" },
            default);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*against itself*");
    }

    [Fact]
    public async Task Player_duplicate_jersey_on_team_fails()
    {
        await using var db = TestDbFactory.CreateContext();
        var t = NewTeam("Club");
        db.Teams.Add(t);
        await db.SaveChangesAsync();

        var svc = new PlayerService(db);
        await svc.RegisterAsync(NewPlayer(t.Id, 9), default);

        var act = () => svc.RegisterAsync(NewPlayer(t.Id, 9), default);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*JerseyNumber*");
    }

    [Fact]
    public async Task Standings_winner_has_more_points()
    {
        await using var db = TestDbFactory.CreateContext();
        var home = NewTeam("Home");
        var away = NewTeam("Away");
        db.Teams.AddRange(home, away);
        await db.SaveChangesAsync();
        db.Matches.Add(new Match
        {
            HomeTeamId = home.Id,
            AwayTeamId = away.Id,
            MatchDate = DateTimeOffset.UtcNow.AddDays(-1),
            HomeScore = 2,
            AwayScore = 0,
            Status = MatchStatus.Completed,
            Venue = "V"
        });
        await db.SaveChangesAsync();

        var rows = await new StandingsService(db).GetStandingsAsync(default);

        rows[0].TeamId.Should().Be(home.Id);
        rows[0].Points.Should().BeGreaterThan(rows[1].Points);
    }

    private static Team NewTeam(string name) => new()
    {
        Name = name,
        City = "C",
        Founded = 2000,
        LogoUrl = "https://x/t.png",
        CoachName = "Coach"
    };

    private static Player NewPlayer(int teamId, int jersey) => new()
    {
        TeamId = teamId,
        FirstName = "A",
        LastName = "B",
        Position = "MF",
        JerseyNumber = jersey,
        DateOfBirth = new DateOnly(2000, 1, 1)
    };
}
