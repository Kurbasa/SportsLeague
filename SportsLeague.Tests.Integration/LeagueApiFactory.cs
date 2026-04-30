using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportsLeague.Infrastructure.Persistence;
using SportsLeague.Infrastructure.Services;
using Testcontainers.PostgreSql;

namespace SportsLeague.Tests.Integration;

public sealed class LeagueApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _db;
    public static bool DockerAvailable { get; private set; } = true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LeagueDb"] = _db?.GetConnectionString(),
                ["Seed:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Ensure DbContext uses container connection string
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<LeagueDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<LeagueDbContext>(o => o.UseNpgsql(_db!.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        try
        {
            _db = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("sports_league_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _db.StartAsync();
        }
        catch
        {
            DockerAvailable = false;
            return;
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LeagueDbContext>();
        await db.Database.MigrateAsync();

        // Pre-fill for integration tests (>= 10k rows) per requirements.
        var seeder = scope.ServiceProvider.GetRequiredService<LeagueSeeder>();
        await seeder.SeedIfEmptyAsync(10_000, CancellationToken.None);
    }

    public new async Task DisposeAsync()
    {
        if (_db is not null)
            await _db.DisposeAsync();
    }
}

