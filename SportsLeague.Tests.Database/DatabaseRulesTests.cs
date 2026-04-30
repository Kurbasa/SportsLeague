using Dapper;
using FluentAssertions;
using Npgsql;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Enums;
using SportsLeague.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SportsLeague.Tests.Database;

public sealed class DatabaseRulesTests : IClassFixture<PostgresDbFixture>
{
    private readonly string _cs;

    public DatabaseRulesTests(PostgresDbFixture fx) =>
        _cs = PostgresDbFixture.DockerAvailable ? fx.ConnectionString : "";

    [SkippableFact]
    public async Task Unique_jersey_number_is_enforced_in_database()
    {
        Skip.IfNot(PostgresDbFixture.DockerAvailable, "Docker is required for Testcontainers-based database tests.");

        var options = new DbContextOptionsBuilder<LeagueDbContext>()
            .UseNpgsql(_cs)
            .Options;

        await using var db = new LeagueDbContext(options);
        var team = new Team { Name = "T", City = "C", Founded = 2000, LogoUrl = "https://x/t.png", CoachName = "Coach" };
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        db.Players.Add(new Player
        {
            TeamId = team.Id,
            FirstName = "A",
            LastName = "B",
            Position = "MF",
            JerseyNumber = 7,
            DateOfBirth = new DateOnly(2000, 1, 1)
        });
        await db.SaveChangesAsync();

        db.Players.Add(new Player
        {
            TeamId = team.Id,
            FirstName = "C",
            LastName = "D",
            Position = "DF",
            JerseyNumber = 7,
            DateOfBirth = new DateOnly(2001, 1, 1)
        });

        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [SkippableFact]
    public async Task Schedule_conflicts_are_enforced_in_database()
    {
        Skip.IfNot(PostgresDbFixture.DockerAvailable, "Docker is required for Testcontainers-based database tests.");

        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();

        var team1 = await conn.ExecuteScalarAsync<int>(
            """INSERT INTO "Teams" ("Name","City","Founded","LogoUrl","CoachName") VALUES ('A','X',2000,'https://x/a.png','C1') RETURNING "Id";""");
        var team2 = await conn.ExecuteScalarAsync<int>(
            """INSERT INTO "Teams" ("Name","City","Founded","LogoUrl","CoachName") VALUES ('B','Y',2001,'https://x/b.png','C2') RETURNING "Id";""");
        var team3 = await conn.ExecuteScalarAsync<int>(
            """INSERT INTO "Teams" ("Name","City","Founded","LogoUrl","CoachName") VALUES ('C','Z',2002,'https://x/c.png','C3') RETURNING "Id";""");

        var day = new DateTimeOffset(new DateTime(2030, 1, 10, 18, 0, 0, DateTimeKind.Utc));

        await conn.ExecuteAsync(
            """INSERT INTO "Matches" ("HomeTeamId","AwayTeamId","MatchDate","HomeScore","AwayScore","Status","Venue") VALUES (@h,@a,@d,0,0,@s,'V');""",
            new { h = team1, a = team2, d = day, s = (int)MatchStatus.Scheduled });

        var act = async () => await conn.ExecuteAsync(
            """INSERT INTO "Matches" ("HomeTeamId","AwayTeamId","MatchDate","HomeScore","AwayScore","Status","Venue") VALUES (@h,@a,@d,0,0,@s,'V2');""",
            new { h = team1, a = team3, d = day.AddHours(1), s = (int)MatchStatus.Scheduled });

        await act.Should().ThrowAsync<PostgresException>()
            .WithMessage("*Schedule conflict*");
    }

    [SkippableFact]
    public async Task Standings_can_be_aggregated_in_sql()
    {
        Skip.IfNot(PostgresDbFixture.DockerAvailable, "Docker is required for Testcontainers-based database tests.");

        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();

        var a = await conn.ExecuteScalarAsync<int>(
            """INSERT INTO "Teams" ("Name","City","Founded","LogoUrl","CoachName") VALUES ('A','X',2000,'https://x/a.png','C1') RETURNING "Id";""");
        var b = await conn.ExecuteScalarAsync<int>(
            """INSERT INTO "Teams" ("Name","City","Founded","LogoUrl","CoachName") VALUES ('B','Y',2001,'https://x/b.png','C2') RETURNING "Id";""");

        await conn.ExecuteAsync(
            """
            INSERT INTO "Matches" ("HomeTeamId","AwayTeamId","MatchDate","HomeScore","AwayScore","Status","Venue")
            VALUES (@h,@a,@d,2,0,@s,'V');
            """,
            new { h = a, a = b, d = DateTimeOffset.UtcNow.AddDays(-2), s = (int)MatchStatus.Completed });

        await conn.ExecuteAsync(
            """
            INSERT INTO "Matches" ("HomeTeamId","AwayTeamId","MatchDate","HomeScore","AwayScore","Status","Venue")
            VALUES (@h,@a,@d,1,1,@s,'V');
            """,
            new { h = b, a = a, d = DateTimeOffset.UtcNow.AddDays(-1), s = (int)MatchStatus.Completed });

        var rows = (await conn.QueryAsync<SqlStandingRow>(
            """
            WITH completed AS (
                SELECT "HomeTeamId" AS team_id, "HomeScore" AS gf, "AwayScore" AS ga
                FROM "Matches" WHERE "Status" = @completed
                UNION ALL
                SELECT "AwayTeamId" AS team_id, "AwayScore" AS gf, "HomeScore" AS ga
                FROM "Matches" WHERE "Status" = @completed
            )
            SELECT
                t."Id" AS TeamId,
                SUM(CASE WHEN c.gf > c.ga THEN 1 ELSE 0 END) AS Wins,
                SUM(CASE WHEN c.gf = c.ga THEN 1 ELSE 0 END) AS Draws,
                SUM(CASE WHEN c.gf < c.ga THEN 1 ELSE 0 END) AS Losses,
                SUM(c.gf) AS GoalsFor,
                SUM(c.ga) AS GoalsAgainst,
                (SUM(CASE WHEN c.gf > c.ga THEN 3 WHEN c.gf = c.ga THEN 1 ELSE 0 END)) AS Points
            FROM "Teams" t
            LEFT JOIN completed c ON c.team_id = t."Id"
            GROUP BY t."Id"
            ORDER BY Points DESC, (SUM(c.gf) - SUM(c.ga)) DESC, SUM(c.gf) DESC, t."Id" ASC;
            """,
            new { completed = (int)MatchStatus.Completed })).ToList();

        rows.Should().NotBeEmpty();

        var first = rows.First();
        first.TeamId.Should().Be(a);
        first.Points.Should().Be(4);
    }

    private sealed class SqlStandingRow
    {
        public int TeamId { get; init; }
        public long Wins { get; init; }
        public long Draws { get; init; }
        public long Losses { get; init; }
        public long GoalsFor { get; init; }
        public long GoalsAgainst { get; init; }
        public long Points { get; init; }
    }
}

