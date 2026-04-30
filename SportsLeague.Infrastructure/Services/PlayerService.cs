using Microsoft.EntityFrameworkCore;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Errors;
using SportsLeague.Infrastructure.Persistence;

namespace SportsLeague.Infrastructure.Services;

public sealed class PlayerService(LeagueDbContext db)
{
    public async Task<Player> RegisterAsync(Player player, CancellationToken ct)
    {
        var exists = await db.Players
            .AsNoTracking()
            .AnyAsync(p => p.TeamId == player.TeamId && p.JerseyNumber == player.JerseyNumber, ct);

        if (exists)
            throw new DomainException("JerseyNumber must be unique within a team.");

        db.Players.Add(player);
        await db.SaveChangesAsync(ct);
        return player;
    }
}

