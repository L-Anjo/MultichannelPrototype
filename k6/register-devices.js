import http from 'k6/http';
import {check, sleep} from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';
const vus = Number(__ENV.VUS || 100);
const iterations = Number(__ENV.ITERATIONS || 1000);
const pauseSeconds = Number(__ENV.SLEEP_SECONDS || 0.05);

export const options = {
  scenarios: {
    register_devices: {
      executor: 'per-vu-iterations',
      vus,
      iterations,
      maxDuration: '10m'
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<1000']
  }
};

function buildPayload(index) {
  return JSON.stringify({
    deviceId: `device-${index}`,
    userId: `user-${index}`,
    channels: ['PUSH', 'SMS', 'EMAIL', 'WHATSAPP', 'TELEGRAM'],
    pushToken: `fcm-token-${index}`,
    isOnline: index % 10 !== 0,
    networkType: index % 2 === 0 ? 'wifi' : 'cellular'
  });
}

export default function () {
  const deviceNumber = (__VU - 1) * iterations + __ITER;
  const response = http.post(`${baseUrl}/devices`, buildPayload(deviceNumber), {
    headers: {
      'Content-Type': 'application/json'
    }
  });

  check(response, {
    'device accepted': r => r.status === 202
  });

  sleep(pauseSeconds);
}
