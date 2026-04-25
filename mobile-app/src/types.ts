export type DispatchChannel = 'PUSH' | 'SMS' | 'EMAIL' | 'WHATSAPP' | 'TELEGRAM';

export type DeviceRegistrationPayload = {
  deviceId: string;
  userId: string;
  channels: DispatchChannel[];
  pushToken: string | null;
  isOnline: boolean;
  networkType: string;
};

export type DeviceStatusPayload = {
  isOnline: boolean;
  networkType: string;
};

export type DeviceContext = {
  deviceId: string;
  pushToken: string | null;
  isOnline: boolean;
  networkType: string;
};
