namespace SportsLeague.Domain.Errors;

public sealed class DomainException(string message) : Exception(message);

