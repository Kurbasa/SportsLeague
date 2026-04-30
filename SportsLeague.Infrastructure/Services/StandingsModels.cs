namespace SportsLeague.Infrastructure.Services;

public sealed record StandingRow(
    int TeamId,
    string TeamName,
    int Played,
    int Wins,
    int Draws,
    int Losses,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points
);

