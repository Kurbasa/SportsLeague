namespace SportsLeague.Api.Requests;

public sealed record ScheduleMatchRequest(
    int HomeTeamId,
    int AwayTeamId,
    DateTimeOffset MatchDate,
    string Venue
);

