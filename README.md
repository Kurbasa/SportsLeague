## SportsLeague (Task 29)

ASP.NET Core Web API for managing a sports league: teams, players, matches, and standings (PostgreSQL).

### Tech
- .NET 9
- EF Core + PostgreSQL (Npgsql)
- Tests: xUnit + FluentAssertions
- Integration/DB tests: Testcontainers for PostgreSQL
- Performance tests: k6 scripts in `k6/`

### Run locally

1) Start PostgreSQL:

```bash
docker compose up -d
```

2) Run API:

```bash
dotnet run --project SportsLeague/SportsLeague.Api
```

3) Optional seed (>=10k rows)

Set in `SportsLeague.Api/appsettings.json`:
- `Seed:Enabled=true`

### Endpoints
- `GET /api/teams`
- `POST /api/teams`
- `GET /api/teams/{id}/players`
- `POST /api/players`
- `POST /api/matches`
- `PATCH /api/matches/{id}/score`
- `PATCH /api/matches/{id}/complete`
- `GET /api/standings`
- `GET /api/matches?team={id}`

Extra helper endpoint:
- `PATCH /api/matches/{id}/start` (to switch `Scheduled` -> `InProgress`)

### Business rules implemented
- `JerseyNumber` is unique within a team (DB unique index + service validation)
- Team cannot play against itself (service + DB trigger)
- One team cannot have 2 matches on the same day (service + DB trigger)
- Standings: win=3, draw=1, loss=0
- Score update allowed only in `InProgress`

### Tests

```bash
dotnet test SportsLeague/SportsLeague.sln
```

CI is configured in `.github/workflows/ci.yml` (runs on push/PR).

