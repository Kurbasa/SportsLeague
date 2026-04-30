using FluentAssertions;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Enums;
using SportsLeague.Infrastructure.Services;

namespace SportsLeague.Tests.Unit;

public sealed class StandingsServiceTests
{
    [Fact]
    public async Task Standings_are_calculated_correctly()
    {
        await using var db = TestDbFactory.CreateContext();

        var a = new Team { Name = "A", City = "X", Founded = 2000, LogoUrl = "https://x/a.png", CoachName = "C1" };
        var b = new Team { Name = "B", City = "Y", Founded = 2001, LogoUrl = "https://x/b.png", CoachName = "C2" };
        db.Teams.AddRange(a, b);
        await db.SaveChangesAsync();

        db.Matches.AddRange(
            new Match
            {
                HomeTeamId = a.Id,
                AwayTeamId = b.Id,
                MatchDate = DateTimeOffset.UtcNow.AddDays(-2),
                HomeScore = 2,
                AwayScore = 0,
                Status = MatchStatus.Completed,
                Venue = "V"
            },
            new Match
            {
                HomeTeamId = b.Id,
                AwayTeamId = a.Id,
                MatchDate = DateTimeOffset.UtcNow.AddDays(-1),
                HomeScore = 1,
                AwayScore = 1,
                Status = MatchStatus.Completed,
                Venue = "V"
            });

        await db.SaveChangesAsync();

        var service = new StandingsService(db);
        var standings = await service.GetStandingsAsync(CancellationToken.None);

        standings.Should().HaveCount(2);
        standings[0].TeamId.Should().Be(a.Id);
        standings[0].Points.Should().Be(4);
        standings[0].Wins.Should().Be(1);
        standings[0].Draws.Should().Be(1);
        standings[0].Losses.Should().Be(0);
        standings[0].GoalsFor.Should().Be(3);
        standings[0].GoalsAgainst.Should().Be(1);
        standings[0].GoalDifference.Should().Be(2);

        standings[1].TeamId.Should().Be(b.Id);
        standings[1].Points.Should().Be(1);
    }
}

