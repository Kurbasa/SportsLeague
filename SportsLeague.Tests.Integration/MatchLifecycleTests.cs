using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SportsLeague.Api.Requests;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Enums;
using SportsLeague.Infrastructure.Services;

namespace SportsLeague.Tests.Integration;

public sealed class MatchLifecycleTests : IClassFixture<LeagueApiFactory>
{
    private readonly LeagueApiFactory _factory;

    public MatchLifecycleTests(LeagueApiFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task Match_lifecycle_and_score_updates_work()
    {
        Skip.IfNot(LeagueApiFactory.DockerAvailable, "Docker is required for Testcontainers-based integration tests.");
        using var client = _factory.CreateClient();

        var t1 = (await (await client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest("Team One", "City", 2000, "https://x/1.png", "Coach 1"))).Content.ReadFromJsonAsync<Team>())!;

        var t2 = (await (await client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest("Team Two", "City", 2001, "https://x/2.png", "Coach 2"))).Content.ReadFromJsonAsync<Team>())!;

        var match = (await (await client.PostAsJsonAsync("/api/matches",
            new ScheduleMatchRequest(t1.Id, t2.Id, DateTimeOffset.UtcNow.AddDays(10), "Stadium"))).Content.ReadFromJsonAsync<Match>())!;

        match.Status.Should().Be(MatchStatus.Scheduled);

        var started = (await (await client.PatchAsync($"/api/matches/{match.Id}/start", null)).Content.ReadFromJsonAsync<Match>())!;
        started.Status.Should().Be(MatchStatus.InProgress);

        var updated = (await (await client.PatchAsJsonAsync($"/api/matches/{match.Id}/score", new UpdateScoreRequest(2, 1)))
            .Content.ReadFromJsonAsync<Match>())!;
        updated.HomeScore.Should().Be(2);
        updated.AwayScore.Should().Be(1);

        var completed = (await (await client.PatchAsync($"/api/matches/{match.Id}/complete", null)).Content.ReadFromJsonAsync<Match>())!;
        completed.Status.Should().Be(MatchStatus.Completed);

        var bad = await client.PatchAsJsonAsync($"/api/matches/{match.Id}/score", new UpdateScoreRequest(3, 3));
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task Standings_endpoint_returns_rows()
    {
        Skip.IfNot(LeagueApiFactory.DockerAvailable, "Docker is required for Testcontainers-based integration tests.");
        using var client = _factory.CreateClient();

        var rows = await client.GetFromJsonAsync<List<StandingRow>>("/api/standings");
        rows.Should().NotBeNull();
        rows!.Count.Should().BeGreaterThan(0);
    }
}

