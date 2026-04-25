import AsyncStorage from '@react-native-async-storage/async-storage';
import NetInfo from '@react-native-community/netinfo';
import 'react-native-get-random-values';
import {v4 as uuidv4} from 'uuid';

const DEVICE_ID_KEY = 'alerting-device-id';

export async function getOrCreateDeviceId(): Promise<string> {
  const stored = await AsyncStorage.getItem(DEVICE_ID_KEY);
  if (stored) {
    return stored;
  }

  const newDeviceId = uuidv4();
  await AsyncStorage.setItem(DEVICE_ID_KEY, newDeviceId);
  return newDeviceId;
}

export async function getCurrentNetworkType(): Promise<string> {
  const network = await NetInfo.fetch();
  return network.type ?? 'unknown';
}
