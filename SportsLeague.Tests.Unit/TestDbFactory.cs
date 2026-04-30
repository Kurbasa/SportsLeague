using Microsoft.EntityFrameworkCore;
using SportsLeague.Infrastructure.Persistence;

namespace SportsLeague.Tests.Unit;

internal static class TestDbFactory
{
    public static LeagueDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LeagueDbContext>()
            .UseInMemoryDatabase(databaseName: $"sports-league-tests-{Guid.NewGuid():N}")
            .EnableSensitiveDataLogging()
            .Options;

        return new LeagueDbContext(options);
    }
}

