import http from 'k6/http';
import {check, sleep} from 'k6';

export const options = {
  scenarios: {
    register_devices: {
      executor: 'per-vu-iterations',
      vus: 100,
      iterations: 1000,
      maxDuration: '10m'
    }
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
  const deviceNumber = (__VU - 1) * 1000 + __ITER;
  const response = http.post('http://localhost:8080/devices', buildPayload(deviceNumber), {
    headers: {
      'Content-Type': 'application/json'
    }
  });

  check(response, {
    'device accepted': r => r.status === 202
  });

  sleep(0.05);
}
