using Microsoft.EntityFrameworkCore;
using SportsLeague.Domain.Enums;
using SportsLeague.Infrastructure.Persistence;

namespace SportsLeague.Infrastructure.Services;

public sealed class StandingsService(LeagueDbContext db)
{
    public async Task<IReadOnlyList<StandingRow>> GetStandingsAsync(CancellationToken ct)
    {
        var completed = await db.Matches
            .AsNoTracking()
            .Where(m => m.Status == MatchStatus.Completed)
            .Select(m => new
            {
                m.HomeTeamId,
                m.AwayTeamId,
                m.HomeScore,
                m.AwayScore
            })
            .ToListAsync(ct);

        var teams = await db.Teams
            .AsNoTracking()
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(ct);

        var byTeam = teams.ToDictionary(
            t => t.Id,
            t => new MutableStanding(t.Id, t.Name)
        );

        foreach (var m in completed)
        {
            ApplyMatch(byTeam[m.HomeTeamId], m.HomeScore, m.AwayScore);
            ApplyMatch(byTeam[m.AwayTeamId], m.AwayScore, m.HomeScore);
        }

        return byTeam.Values
            .Select(s => s.ToRow())
            .OrderByDescending(r => r.Points)
            .ThenByDescending(r => r.GoalDifference)
            .ThenByDescending(r => r.GoalsFor)
            .ThenBy(r => r.TeamName)
            .ToList();
    }

    private static void ApplyMatch(MutableStanding s, int goalsFor, int goalsAgainst)
    {
        s.Played++;
        s.GoalsFor += goalsFor;
        s.GoalsAgainst += goalsAgainst;
        if (goalsFor > goalsAgainst) s.Wins++;
        else if (goalsFor == goalsAgainst) s.Draws++;
        else s.Losses++;
    }

    private sealed class MutableStanding(int teamId, string teamName)
    {
        public int TeamId { get; } = teamId;
        public string TeamName { get; } = teamName;

        public int Played { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }

        public StandingRow ToRow()
        {
            var gd = GoalsFor - GoalsAgainst;
            var points = (Wins * 3) + Draws;
            return new StandingRow(TeamId, TeamName, Played, Wins, Draws, Losses, GoalsFor, GoalsAgainst, gd, points);
        }
    }
}

