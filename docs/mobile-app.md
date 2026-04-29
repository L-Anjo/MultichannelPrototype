# Mobile App

## Papel no sistema

A app mobile representa um utilizador real e um dispositivo real dentro do prototipo.

Ela nao faz dispatch de notificacoes. O papel dela e fornecer contexto ao backend.

## Ficheiros principais

- [App.tsx](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/mobile-app/App.tsx)
- [deviceProfile.ts](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/mobile-app/src/config/deviceProfile.ts)
- [api.ts](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/mobile-app/src/services/api.ts)
- [deviceContext.ts](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/mobile-app/src/services/deviceContext.ts)
- [pushNotifications.ts](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/mobile-app/src/services/pushNotifications.ts)

## O que faz

### 1. Gera e persiste `deviceId`

Usa `AsyncStorage` para garantir que o mesmo device mantem um identificador estavel.

### 2. Pede permissao FCM e obtem `pushToken`

Usa `@react-native-firebase/messaging`.

Isto permite ao backend saber se o device suporta push real.

### 3. Regista o device

Chama:

- `POST /devices`

com:

- `deviceId`
- `userId`
- `channels`
- `pushToken`
- `isOnline`
- `networkType`

### 4. Sincroniza estado

Chama:

- `PUT /devices/{deviceId}/status`

periodicamente e tambem quando o estado da app muda.

### 5. Recebe notificacoes em foreground

Usa o callback de `messaging().onMessage(...)` para:

- mostrar alerta no ecra
- registar o conteudo recebido

## Porque e importante para a arquitetura

Sem a app, o sistema trabalha apenas com regras estaticas.

Com a app, o backend passa a ter contexto de execucao:

- o device existe
- o device esta online ou offline
- o device tem ou nao push token
- o device declara que canais pode receber

Isto permite decisao contextual e nao apenas regras fixas.
