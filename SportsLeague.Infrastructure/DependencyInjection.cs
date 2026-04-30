using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportsLeague.Infrastructure.Persistence;
using SportsLeague.Infrastructure.Services;

namespace SportsLeague.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LeagueDbContext>(options =>
        {
            var cs = configuration.GetConnectionString("LeagueDb");
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("Missing connection string 'ConnectionStrings:LeagueDb'.");

            options.UseNpgsql(cs);
        });

        services.AddScoped<MatchService>();
        services.AddScoped<PlayerService>();
        services.AddScoped<StandingsService>();
        services.AddScoped<LeagueSeeder>();

        return services;
    }
}

