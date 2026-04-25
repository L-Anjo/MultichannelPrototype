import http from 'k6/http';
import {check} from 'k6';

const xmlTemplate = open('../samples/cap-alert.xml');

export const options = {
  scenarios: {
    broadcast_alert: {
      executor: 'shared-iterations',
      vus: 10,
      iterations: 200,
      maxDuration: '5m'
    }
  }
};

export default function () {
  const correlation = `${__VU}-${__ITER}-${Date.now()}`;
  const xmlPayload = xmlTemplate
    .replace('<identifier>123</identifier>', `<identifier>broadcast-${correlation}</identifier>`)
    .replace('Incendio em Braga', `Broadcast alert ${correlation}`);

  const response = http.post('http://localhost:8080/alerts', xmlPayload, {
    headers: {
      'Content-Type': 'application/xml',
      'X-User-Id': 'broadcast',
      'X-Broadcast': 'true'
    }
  });

  check(response, {
    'broadcast accepted': r => r.status === 202
  });
}
