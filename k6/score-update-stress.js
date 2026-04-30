import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    score_updates: {
      executor: 'constant-vus',
      vus: __ENV.VUS ? parseInt(__ENV.VUS, 10) : 50,
      duration: __ENV.DURATION || '30s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<1200'],
  },
};

const baseUrl = __ENV.BASE_URL || 'http://localhost:5000';
const matchId = __ENV.MATCH_ID ? parseInt(__ENV.MATCH_ID, 10) : null;

export default function () {
  if (!matchId) {
    throw new Error('Set MATCH_ID env var to an InProgress match id.');
  }

  const home = Math.floor(Math.random() * 6);
  const away = Math.floor(Math.random() * 6);

  const res = http.patch(
    `${baseUrl}/api/matches/${matchId}/score`,
    JSON.stringify({ homeScore: home, awayScore: away }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  check(res, { 'status 200 or 400': (r) => r.status === 200 || r.status === 400 });
  sleep(0.1);
}

