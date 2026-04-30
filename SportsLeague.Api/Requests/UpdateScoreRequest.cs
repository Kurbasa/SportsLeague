namespace SportsLeague.Api.Requests;

public sealed record UpdateScoreRequest(
    int HomeScore,
    int AwayScore
);

