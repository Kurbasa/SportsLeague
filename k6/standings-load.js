import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    standings: {
      executor: 'constant-vus',
      vus: __ENV.VUS ? parseInt(__ENV.VUS, 10) : 25,
      duration: __ENV.DURATION || '30s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<800'],
  },
};

const baseUrl = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
  const res = http.get(`${baseUrl}/api/standings`);
  check(res, {
    'status 200': (r) => r.status === 200,
    'has body': (r) => (r.body || '').length > 0,
  });
  sleep(0.2);
}

