namespace SportsLeague.Api.Requests;

public sealed record RegisterPlayerRequest(
    int TeamId,
    string FirstName,
    string LastName,
    string Position,
    int JerseyNumber,
    DateOnly DateOfBirth
);

