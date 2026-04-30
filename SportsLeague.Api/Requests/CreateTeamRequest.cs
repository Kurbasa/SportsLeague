namespace SportsLeague.Api.Requests;

public sealed record CreateTeamRequest(
    string Name,
    string City,
    int Founded,
    string LogoUrl,
    string CoachName
);

