namespace SportsLeague.Domain.Entities;

public sealed class Team
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string City { get; set; }
    public int Founded { get; set; }
    public required string LogoUrl { get; set; }
    public required string CoachName { get; set; }

    public List<Player> Players { get; set; } = new();
    public List<Match> HomeMatches { get; set; } = new();
    public List<Match> AwayMatches { get; set; } = new();
}

