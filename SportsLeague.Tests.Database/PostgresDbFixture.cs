using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsLeague.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SportsLeague.Tests.Database;

public sealed class PostgresDbFixture : IAsyncLifetime
{
    public PostgreSqlContainer? Container { get; private set; }
    public static bool DockerAvailable { get; private set; } = true;

    public string ConnectionString => Container!.GetConnectionString();

    public async Task InitializeAsync()
    {
        try
        {
            Container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("sports_league_dbtests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await Container.StartAsync();
        }
        catch
        {
            DockerAvailable = false;
            return;
        }

        var services = new ServiceCollection();
        services.AddDbContext<LeagueDbContext>(o => o.UseNpgsql(ConnectionString));
        await using var sp = services.BuildServiceProvider();

        await using var db = sp.GetRequiredService<LeagueDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (Container is not null)
            await Container.DisposeAsync();
    }
}

