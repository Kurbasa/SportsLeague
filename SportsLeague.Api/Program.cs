using Microsoft.EntityFrameworkCore;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Enums;
using SportsLeague.Domain.Errors;
using SportsLeague.Infrastructure;
using SportsLeague.Infrastructure.Persistence;
using SportsLeague.Infrastructure.Services;
using SportsLeague.Api.Requests;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapGet("/", () => Results.Text("SportsLeague API is running. Try /swagger or /api/teams"));

app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    catch (DomainException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// DB migrate + optional seed for local/perf runs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LeagueDbContext>();
    await db.Database.MigrateAsync();

    if (builder.Configuration.GetValue<bool>("Seed:Enabled"))
    {
        var seeder = scope.ServiceProvider.GetRequiredService<LeagueSeeder>();
        await seeder.SeedIfEmptyAsync(builder.Configuration.GetValue<int>("Seed:MinTotalRows", 10_000), CancellationToken.None);
    }
}

var api = app.MapGroup("/api");

api.MapGet("/teams", async (LeagueDbContext db, CancellationToken ct) =>
{
    var teams = await db.Teams.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
    return Results.Ok(teams);
});

api.MapPost("/teams", async (CreateTeamRequest req, LeagueDbContext db, CancellationToken ct) =>
{
    var team = new Team
    {
        Name = req.Name,
        City = req.City,
        Founded = req.Founded,
        LogoUrl = req.LogoUrl,
        CoachName = req.CoachName
    };

    db.Teams.Add(team);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/teams/{team.Id}", team);
});

api.MapGet("/teams/{id:int}/players", async (int id, LeagueDbContext db, CancellationToken ct) =>
{
    var players = await db.Players.AsNoTracking()
        .Where(p => p.TeamId == id)
        .OrderBy(p => p.JerseyNumber)
        .ToListAsync(ct);
    return Results.Ok(players);
});

api.MapPost("/players", async (RegisterPlayerRequest req, PlayerService playerService, CancellationToken ct) =>
{
    var player = new Player
    {
        TeamId = req.TeamId,
        FirstName = req.FirstName,
        LastName = req.LastName,
        Position = req.Position,
        JerseyNumber = req.JerseyNumber,
        DateOfBirth = req.DateOfBirth
    };

    var created = await playerService.RegisterAsync(player, ct);
    return Results.Created($"/api/players/{created.Id}", created);
});

api.MapPost("/matches", async (ScheduleMatchRequest req, MatchService matchService, CancellationToken ct) =>
{
    var match = new Match
    {
        HomeTeamId = req.HomeTeamId,
        AwayTeamId = req.AwayTeamId,
        MatchDate = req.MatchDate,
        HomeScore = 0,
        AwayScore = 0,
        Status = MatchStatus.Scheduled,
        Venue = req.Venue
    };

    var created = await matchService.ScheduleAsync(match, ct);
    return Results.Created($"/api/matches/{created.Id}", created);
});

api.MapPatch("/matches/{id:int}/start", async (int id, MatchService matchService, CancellationToken ct) =>
{
    var updated = await matchService.StartAsync(id, ct);
    return Results.Ok(updated);
});

api.MapPatch("/matches/{id:int}/score", async (int id, UpdateScoreRequest req, MatchService matchService, CancellationToken ct) =>
{
    var updated = await matchService.UpdateScoreAsync(id, req.HomeScore, req.AwayScore, ct);
    return Results.Ok(updated);
});

api.MapPatch("/matches/{id:int}/complete", async (int id, MatchService matchService, CancellationToken ct) =>
{
    var updated = await matchService.CompleteAsync(id, ct);
    return Results.Ok(updated);
});

api.MapGet("/standings", async (StandingsService standings, CancellationToken ct) =>
{
    var rows = await standings.GetStandingsAsync(ct);
    return Results.Ok(rows);
});

api.MapGet("/matches", async (int team, LeagueDbContext db, CancellationToken ct) =>
{
    var matches = await db.Matches.AsNoTracking()
        .Where(m => m.HomeTeamId == team || m.AwayTeamId == team)
        .OrderByDescending(m => m.MatchDate)
        .ToListAsync(ct);
    return Results.Ok(matches);
});

app.Run();

public partial class Program { }

