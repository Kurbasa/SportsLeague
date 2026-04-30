using FluentAssertions;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Errors;
using SportsLeague.Infrastructure.Services;

namespace SportsLeague.Tests.Unit;

public sealed class MatchServiceTests
{
    [Fact]
    public async Task Team_cannot_play_against_itself()
    {
        await using var db = TestDbFactory.CreateContext();
        var team = new Team { Name = "A", City = "X", Founded = 2000, LogoUrl = "https://x/a.png", CoachName = "C1" };
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        var service = new MatchService(db);
        var act = async () => await service.ScheduleAsync(new Match
        {
            HomeTeamId = team.Id,
            AwayTeamId = team.Id,
            MatchDate = DateTimeOffset.UtcNow.AddDays(1),
            Venue = "V"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*against itself*");
    }

    [Fact]
    public async Task Team_cannot_have_two_matches_in_same_day()
    {
        await using var db = TestDbFactory.CreateContext();
        var a = new Team { Name = "A", City = "X", Founded = 2000, LogoUrl = "https://x/a.png", CoachName = "C1" };
        var b = new Team { Name = "B", City = "Y", Founded = 2001, LogoUrl = "https://x/b.png", CoachName = "C2" };
        var c = new Team { Name = "C", City = "Z", Founded = 2002, LogoUrl = "https://x/c.png", CoachName = "C3" };
        db.Teams.AddRange(a, b, c);
        await db.SaveChangesAsync();

        var day = new DateTimeOffset(new DateTime(2030, 1, 10, 18, 0, 0, DateTimeKind.Utc));
        var service = new MatchService(db);

        await service.ScheduleAsync(new Match
        {
            HomeTeamId = a.Id,
            AwayTeamId = b.Id,
            MatchDate = day,
            Venue = "V"
        }, CancellationToken.None);

        var act = async () => await service.ScheduleAsync(new Match
        {
            HomeTeamId = a.Id,
            AwayTeamId = c.Id,
            MatchDate = day.AddHours(2),
            Venue = "V2"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Schedule conflict*");
    }
}

