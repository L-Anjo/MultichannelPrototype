import {DispatchChannel} from '../types';

export const API_BASE_URL = 'http://10.0.2.2:8080';

export const TEST_USER_ID = 'user-123';

export const MANUAL_DEVICE_CHANNELS: DispatchChannel[] = [
  'PUSH',
  'SMS',
  'EMAIL',
  'WHATSAPP',
  'TELEGRAM'
];

export const STATUS_SYNC_INTERVAL_MS = 15000;
