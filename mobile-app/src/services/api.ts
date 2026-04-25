import {API_BASE_URL} from '../config/deviceProfile';
import {DeviceRegistrationPayload, DeviceStatusPayload} from '../types';

export async function registerDevice(payload: DeviceRegistrationPayload): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/devices`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    throw new Error(`Device registration failed with status ${response.status}`);
  }
}

export async function updateDeviceStatus(
  deviceId: string,
  payload: DeviceStatusPayload
): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/devices/${deviceId}/status`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    throw new Error(`Status update failed with status ${response.status}`);
  }
}
