import React, {useEffect, useMemo, useState} from 'react';
import {
  Alert,
  AppState,
  AppStateStatus,
  SafeAreaView,
  ScrollView,
  StyleSheet,
  Switch,
  Text,
  View
} from 'react-native';
import {
  API_BASE_URL,
  MANUAL_DEVICE_CHANNELS,
  STATUS_SYNC_INTERVAL_MS,
  TEST_USER_ID
} from './src/config/deviceProfile';
import {getCurrentNetworkType, getOrCreateDeviceId} from './src/services/deviceContext';
import {registerDevice, updateDeviceStatus} from './src/services/api';
import {
  getPushToken,
  requestPushPermission,
  subscribeToForegroundMessages
} from './src/services/pushNotifications';
import {DeviceContext} from './src/types';

export default function App(): React.JSX.Element {
  const [deviceContext, setDeviceContext] = useState<DeviceContext | null>(null);
  const [manualOnline, setManualOnline] = useState(true);
  const [lastMessage, setLastMessage] = useState('Sem notificacoes recebidas');
  const [statusText, setStatusText] = useState('A inicializar...');

  const effectiveChannels = useMemo(() => MANUAL_DEVICE_CHANNELS, []);

  useEffect(() => {
    const bootstrap = async () => {
      try {
        await requestPushPermission();
        const [deviceId, pushToken, networkType] = await Promise.all([
          getOrCreateDeviceId(),
          getPushToken(),
          getCurrentNetworkType()
        ]);

        const initialContext: DeviceContext = {
          deviceId,
          pushToken,
          isOnline: manualOnline,
          networkType
        };

        await registerDevice({
          deviceId,
          userId: TEST_USER_ID,
          channels: effectiveChannels,
          pushToken,
          isOnline: manualOnline,
          networkType
        });

        setDeviceContext(initialContext);
        setStatusText('Device registado no backend');
      } catch (error) {
        const message = error instanceof Error ? error.message : 'Erro desconhecido';
        setStatusText(`Falha no bootstrap: ${message}`);
        Alert.alert('Erro', message);
      }
    };

    bootstrap();
  }, [effectiveChannels]);

  useEffect(() => {
    if (!deviceContext) {
      return;
    }

    const statusInterval = setInterval(async () => {
      const currentNetworkType = await getCurrentNetworkType();
      await syncStatus(deviceContext.deviceId, manualOnline, currentNetworkType);
    }, STATUS_SYNC_INTERVAL_MS);

    const unsubscribeMessaging = subscribeToForegroundMessages(remoteMessage => {
      setLastMessage(
        remoteMessage.notification?.body ??
          JSON.stringify(remoteMessage.data ?? {})
      );
    });

    const appStateSubscription = AppState.addEventListener(
      'change',
      async (nextState: AppStateStatus) => {
        const networkType = await getCurrentNetworkType();
        const isOnline = nextState === 'active' && manualOnline;
        await syncStatus(deviceContext.deviceId, isOnline, networkType);
      }
    );

    return () => {
      appStateSubscription.remove();
      clearInterval(statusInterval);
      unsubscribeMessaging();
    };
  }, [deviceContext, manualOnline]);

  async function syncStatus(deviceId: string, isOnline: boolean, networkType: string) {
    await updateDeviceStatus(deviceId, {isOnline, networkType});
    setDeviceContext(current =>
      current
        ? {
            ...current,
            isOnline,
            networkType
          }
        : current
    );
    setStatusText(`Contexto sincronizado: ${isOnline ? 'online' : 'offline'} em ${networkType}`);
  }

  return (
    <SafeAreaView style={styles.safeArea}>
      <ScrollView contentContainerStyle={styles.content}>
        <Text style={styles.title}>Alerting Mobile Device</Text>
        <Text style={styles.subtitle}>React Native + FCM + device context</Text>

        <View style={styles.panel}>
          <Text style={styles.label}>API</Text>
          <Text style={styles.value}>{API_BASE_URL}</Text>
          <Text style={styles.label}>User</Text>
          <Text style={styles.value}>{TEST_USER_ID}</Text>
          <Text style={styles.label}>Device ID</Text>
          <Text style={styles.value}>{deviceContext?.deviceId ?? 'a gerar...'}</Text>
          <Text style={styles.label}>Push token</Text>
          <Text style={styles.value}>{deviceContext?.pushToken ?? 'sem token FCM'}</Text>
          <Text style={styles.label}>Network</Text>
          <Text style={styles.value}>{deviceContext?.networkType ?? 'desconhecida'}</Text>
        </View>

        <View style={styles.panel}>
          <View style={styles.row}>
            <Text style={styles.label}>Forcar online</Text>
            <Switch value={manualOnline} onValueChange={setManualOnline} />
          </View>
          <Text style={styles.helper}>
            Os canais disponiveis sao definidos manualmente em `src/config/deviceProfile.ts`.
          </Text>
          <Text style={styles.label}>Channels</Text>
          <Text style={styles.value}>{effectiveChannels.join(', ')}</Text>
          <Text style={styles.label}>Estado</Text>
          <Text style={styles.value}>{statusText}</Text>
        </View>

        <View style={styles.panel}>
          <Text style={styles.label}>Ultima notificacao</Text>
          <Text style={styles.value}>{lastMessage}</Text>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#0d1117'
  },
  content: {
    padding: 24,
    gap: 16
  },
  title: {
    color: '#f0f6fc',
    fontSize: 28,
    fontWeight: '700'
  },
  subtitle: {
    color: '#8b949e',
    fontSize: 15
  },
  panel: {
    backgroundColor: '#161b22',
    borderRadius: 8,
    padding: 16,
    gap: 10,
    borderWidth: 1,
    borderColor: '#30363d'
  },
  label: {
    color: '#8b949e',
    fontSize: 13,
    textTransform: 'uppercase'
  },
  value: {
    color: '#f0f6fc',
    fontSize: 15
  },
  helper: {
    color: '#9fb3c8',
    fontSize: 13
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between'
  }
});
