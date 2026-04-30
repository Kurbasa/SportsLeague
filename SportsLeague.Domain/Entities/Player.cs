namespace SportsLeague.Domain.Entities;

public sealed class Player
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Position { get; set; }
    public int JerseyNumber { get; set; }
    public DateOnly DateOfBirth { get; set; }

    public Team? Team { get; set; }
}

