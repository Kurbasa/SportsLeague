using Bogus;
using Microsoft.EntityFrameworkCore;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Enums;
using SportsLeague.Infrastructure.Persistence;

namespace SportsLeague.Infrastructure.Services;

public sealed class LeagueSeeder(LeagueDbContext db)
{
    public async Task SeedIfEmptyAsync(int minTotalRows, CancellationToken ct)
    {
        var any = await db.Teams.AsNoTracking().AnyAsync(ct);
        if (any) return;

        await SeedAsync(minTotalRows, ct);
    }

    public async Task SeedAsync(int minTotalRows, CancellationToken ct)
    {
        // Realistic-ish distribution: many players, fewer teams, fewer matches.
        var teamCount = 250;
        var playersPerTeam = 30; // 7,500 players
        var matchCount = 3_000;  // Total rows: 250 + 7500 + 3000 = 10,750

        if (teamCount + (teamCount * playersPerTeam) + matchCount < minTotalRows)
            matchCount = Math.Max(matchCount, minTotalRows - (teamCount + (teamCount * playersPerTeam)));

        var faker = new Faker("en");

        var teamFaker = new Faker<Team>("en")
            .RuleFor(t => t.Name, f => $"{f.Company.CompanyName()} FC")
            .RuleFor(t => t.City, f => f.Address.City())
            .RuleFor(t => t.Founded, f => f.Random.Int(1880, 2020))
            .RuleFor(t => t.LogoUrl, f => f.Internet.UrlWithPath("https", "example.com", "logo.png"))
            .RuleFor(t => t.CoachName, f => f.Name.FullName());

        var teams = teamFaker.Generate(teamCount);
        db.Teams.AddRange(teams);
        await db.SaveChangesAsync(ct);

        var teamIds = await db.Teams.AsNoTracking().Select(t => t.Id).ToListAsync(ct);

        var playerFaker = new Faker<Player>("en")
            .RuleFor(p => p.FirstName, f => f.Name.FirstName())
            .RuleFor(p => p.LastName, f => f.Name.LastName());

        var positions = new[] { "GK", "DF", "MF", "FW" };
        var players = new List<Player>(teamCount * playersPerTeam);

        foreach (var teamId in teamIds)
        {
            var jerseyNumbers = Enumerable.Range(1, 99).OrderBy(_ => faker.Random.Int()).Take(playersPerTeam).ToArray();
            for (var i = 0; i < playersPerTeam; i++)
            {
                var p = playerFaker.Generate();
                p.TeamId = teamId;
                p.Position = positions[faker.Random.Int(0, positions.Length - 1)];
                p.JerseyNumber = jerseyNumbers[i];
                p.DateOfBirth = DateOnly.FromDateTime(faker.Date.Past(35, DateTime.UtcNow.AddYears(-17)));
                players.Add(p);
            }
        }

        db.Players.AddRange(players);
        await db.SaveChangesAsync(ct);

        // Matches with no per-team same-day conflicts.
        var usedDays = teamIds.ToDictionary(id => id, _ => new HashSet<DateOnly>());
        var venues = new Faker("en").Company.CompanyName();

        var start = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-180));
        var end = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(180));
        var totalDays = (end.ToDateTime(TimeOnly.MinValue) - start.ToDateTime(TimeOnly.MinValue)).Days;

        var matches = new List<Match>(matchCount);
        for (var i = 0; i < matchCount; i++)
        {
            // try a few times to find a conflict-free pair/day
            for (var attempt = 0; attempt < 50; attempt++)
            {
                var home = teamIds[faker.Random.Int(0, teamIds.Count - 1)];
                var away = teamIds[faker.Random.Int(0, teamIds.Count - 1)];
                if (home == away) continue;

                var day = start.AddDays(faker.Random.Int(0, Math.Max(1, totalDays)));
                if (usedDays[home].Contains(day) || usedDays[away].Contains(day)) continue;

                usedDays[home].Add(day);
                usedDays[away].Add(day);

                var statusRoll = faker.Random.Int(1, 100);
                var status = statusRoll switch
                {
                    <= 10 => MatchStatus.Scheduled,
                    <= 20 => MatchStatus.InProgress,
                    _ => MatchStatus.Completed
                };

                var (hs, @as) = status == MatchStatus.Scheduled
                    ? (0, 0)
                    : (faker.Random.Int(0, 5), faker.Random.Int(0, 5));

                matches.Add(new Match
                {
                    HomeTeamId = home,
                    AwayTeamId = away,
                    MatchDate = new DateTimeOffset(day.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(faker.Random.Int(12, 21)))), TimeSpan.Zero),
                    HomeScore = hs,
                    AwayScore = @as,
                    Status = status,
                    Venue = venues
                });
                break;
            }
        }

        db.Matches.AddRange(matches);
        await db.SaveChangesAsync(ct);
    }
}

