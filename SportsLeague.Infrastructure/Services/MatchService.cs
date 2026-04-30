using Microsoft.EntityFrameworkCore;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Enums;
using SportsLeague.Domain.Errors;
using SportsLeague.Infrastructure.Persistence;

namespace SportsLeague.Infrastructure.Services;

public sealed class MatchService(LeagueDbContext db)
{
    public async Task<Match> ScheduleAsync(Match match, CancellationToken ct)
    {
        if (match.HomeTeamId == match.AwayTeamId)
            throw new DomainException("Team cannot play against itself.");

        var date = match.MatchDate.Date;

        var conflict = await db.Matches
            .AsNoTracking()
            .AnyAsync(m =>
                m.MatchDate.Date == date &&
                (m.HomeTeamId == match.HomeTeamId ||
                 m.AwayTeamId == match.HomeTeamId ||
                 m.HomeTeamId == match.AwayTeamId ||
                 m.AwayTeamId == match.AwayTeamId),
                ct);

        if (conflict)
            throw new DomainException("Schedule conflict: a team already has a match on this day.");

        match.Status = MatchStatus.Scheduled;
        db.Matches.Add(match);
        await db.SaveChangesAsync(ct);
        return match;
    }

    public async Task<Match> StartAsync(int matchId, CancellationToken ct)
    {
        var match = await db.Matches.FirstOrDefaultAsync(x => x.Id == matchId, ct)
            ?? throw new DomainException("Match not found.");

        if (match.Status != MatchStatus.Scheduled)
            throw new DomainException("Only scheduled matches can be started.");

        match.Status = MatchStatus.InProgress;
        await db.SaveChangesAsync(ct);
        return match;
    }

    public async Task<Match> UpdateScoreAsync(int matchId, int homeScore, int awayScore, CancellationToken ct)
    {
        var match = await db.Matches.FirstOrDefaultAsync(x => x.Id == matchId, ct)
            ?? throw new DomainException("Match not found.");

        if (match.Status != MatchStatus.InProgress)
            throw new DomainException("Score can be updated only when match status is InProgress.");

        if (homeScore < 0 || awayScore < 0)
            throw new DomainException("Scores cannot be negative.");

        match.HomeScore = homeScore;
        match.AwayScore = awayScore;
        await db.SaveChangesAsync(ct);
        return match;
    }

    public async Task<Match> CompleteAsync(int matchId, CancellationToken ct)
    {
        var match = await db.Matches.FirstOrDefaultAsync(x => x.Id == matchId, ct)
            ?? throw new DomainException("Match not found.");

        if (match.Status != MatchStatus.InProgress)
            throw new DomainException("Only in-progress matches can be completed.");

        match.Status = MatchStatus.Completed;
        await db.SaveChangesAsync(ct);
        return match;
    }
}

