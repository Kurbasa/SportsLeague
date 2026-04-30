using FluentAssertions;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Errors;
using SportsLeague.Infrastructure.Services;

namespace SportsLeague.Tests.Unit;

public sealed class PlayerServiceTests
{
    [Fact]
    public async Task JerseyNumber_must_be_unique_within_team()
    {
        await using var db = TestDbFactory.CreateContext();
        var team = new Team { Name = "A", City = "X", Founded = 2000, LogoUrl = "https://x/a.png", CoachName = "C1" };
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        var service = new PlayerService(db);
        await service.RegisterAsync(new Player
        {
            TeamId = team.Id,
            FirstName = "F1",
            LastName = "L1",
            Position = "MF",
            JerseyNumber = 10,
            DateOfBirth = new DateOnly(2000, 1, 1)
        }, CancellationToken.None);

        var act = async () => await service.RegisterAsync(new Player
        {
            TeamId = team.Id,
            FirstName = "F2",
            LastName = "L2",
            Position = "DF",
            JerseyNumber = 10,
            DateOfBirth = new DateOnly(2001, 1, 1)
        }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*JerseyNumber*unique*");
    }
}

