import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    cap_alert_burst: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '15s', target: 10 },
        { duration: '30s', target: 50 },
        { duration: '15s', target: 0 }
      ]
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<1000']
  }
};

const xmlTemplate = open('../samples/cap-alert.xml');

export default function () {
  const correlation = `${__VU}-${__ITER}-${Date.now()}`;
  const xmlPayload = xmlTemplate
    .replace('<identifier>123</identifier>', `<identifier>${correlation}</identifier>`)
    .replace('Incendio em Braga', `Incendio em Braga ${correlation}`);

  const response = http.post('http://localhost:8080/alerts', xmlPayload, {
    headers: {
      'Content-Type': 'application/xml',
      'X-User-Id': `user-${__VU}`,
      'X-Broadcast': 'false'
    }
  });

  check(response, {
    'accepted': (r) => r.status === 202
  });

  sleep(0.2);
}
