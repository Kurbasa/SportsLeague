## k6 performance tests

### Prerequisites
- `k6` installed locally
- API is running and database is migrated/seeded (see `SportsLeague.Api` settings)

### Standings load test

```bash
k6 run --env BASE_URL=http://localhost:5000 --env VUS=25 --env DURATION=30s k6/standings-load.js
```

### Concurrent score updates stress test

1) Create a match and start it (`/api/matches/{id}/start`).
2) Run:

```bash
k6 run --env BASE_URL=http://localhost:5000 --env MATCH_ID=123 --env VUS=50 --env DURATION=30s k6/score-update-stress.js
```

