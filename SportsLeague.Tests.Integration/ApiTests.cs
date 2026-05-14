using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SportsLeague.Api.Requests;
using SportsLeague.Domain.Entities;
using SportsLeague.Infrastructure.Services;

namespace SportsLeague.Tests.Integration;

public sealed class ApiTests : IClassFixture<LeagueApiFactory>
{
    private readonly LeagueApiFactory _factory;

    public ApiTests(LeagueApiFactory factory) => _factory = factory;

    [SkippableFact]
    public async Task Match_flow_start_score_complete()
    {
        Skip.IfNot(LeagueApiFactory.DockerAvailable, "Docker");
        var c = _factory.CreateClient();
        var a = await PostTeam(c, "A");
        var b = await PostTeam(c, "B");
        var m = await PostMatch(c, a.Id, b.Id);

        (await c.PatchAsync($"/api/matches/{m.Id}/start", null)).EnsureSuccessStatusCode();
        (await c.PatchAsJsonAsync($"/api/matches/{m.Id}/score", new UpdateScoreRequest(1, 0))).EnsureSuccessStatusCode();
        (await c.PatchAsync($"/api/matches/{m.Id}/complete", null)).EnsureSuccessStatusCode();

        (await c.PatchAsJsonAsync($"/api/matches/{m.Id}/score", new UpdateScoreRequest(9, 9))).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task Standings_list_not_empty()
    {
        Skip.IfNot(LeagueApiFactory.DockerAvailable, "Docker");
        var c = _factory.CreateClient();
        await PostTeam(c, "StandingsX");
        var rows = await c.GetFromJsonAsync<List<StandingRow>>("/api/standings");
        rows.Should().NotBeNull().And.NotBeEmpty();
    }

    [SkippableFact]
    public async Task Duplicate_jersey_returns_400()
    {
        Skip.IfNot(LeagueApiFactory.DockerAvailable, "Docker");
        var c = _factory.CreateClient();
        var t = await PostTeam(c, "Dup");
        await c.PostAsJsonAsync("/api/players", new RegisterPlayerRequest(t.Id, "x", "y", "MF", 5, new DateOnly(2000, 1, 1)));
        var bad = await c.PostAsJsonAsync("/api/players", new RegisterPlayerRequest(t.Id, "z", "w", "DF", 5, new DateOnly(2001, 1, 1)));

        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        JsonDocument.Parse(await bad.Content.ReadAsStringAsync()).RootElement.GetProperty("error").GetString()
            .Should().Contain("Jersey");
    }

    private static async Task<Team> PostTeam(HttpClient c, string name) =>
        (await (await c.PostAsJsonAsync("/api/teams", new CreateTeamRequest(name, "c", 2000, "https://x/1.png", "coach")))
            .Content.ReadFromJsonAsync<Team>())!;

    private static async Task<Match> PostMatch(HttpClient c, int homeId, int awayId) =>
        (await (await c.PostAsJsonAsync("/api/matches",
                new ScheduleMatchRequest(homeId, awayId, DateTimeOffset.UtcNow.AddDays(30), "S")))
            .Content.ReadFromJsonAsync<Match>())!;
}
