import messaging, {FirebaseMessagingTypes} from '@react-native-firebase/messaging';
import {Alert, Platform} from 'react-native';

export async function requestPushPermission(): Promise<void> {
  await messaging().requestPermission();

  if (Platform.OS === 'android') {
    await messaging().registerDeviceForRemoteMessages();
  }
}

export async function getPushToken(): Promise<string | null> {
  try {
    return await messaging().getToken();
  } catch {
    return null;
  }
}

export function subscribeToForegroundMessages(
  onMessageReceived: (message: FirebaseMessagingTypes.RemoteMessage) => void
): () => void {
  return messaging().onMessage(async remoteMessage => {
    Alert.alert(
      remoteMessage.notification?.title ?? 'Novo alerta',
      remoteMessage.notification?.body ?? JSON.stringify(remoteMessage.data ?? {})
    );
    onMessageReceived(remoteMessage);
  });
}
