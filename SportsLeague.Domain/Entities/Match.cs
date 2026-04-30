using SportsLeague.Domain.Enums;

namespace SportsLeague.Domain.Entities;

public sealed class Match
{
    public int Id { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public DateTimeOffset MatchDate { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
    public required string Venue { get; set; }

    public Team? HomeTeam { get; set; }
    public Team? AwayTeam { get; set; }
}

