# Mobile App

Aplicacao React Native para representar um utilizador/dispositivo real dentro do prototipo.

## O que faz

- gera e persiste `deviceId`
- pede permissao FCM
- obtem `pushToken`
- regista o dispositivo em `POST /devices`
- sincroniza estado em `PUT /devices/{deviceId}/status`
- mostra no ecra a ultima notificacao recebida em foreground

## Configuracao de teste

Editar [deviceProfile.ts](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/mobile-app/src/config/deviceProfile.ts) para:

- mudar `API_BASE_URL`
- trocar `TEST_USER_ID`
- simular canais disponiveis em `MANUAL_DEVICE_CHANNELS`

## Cenarios uteis

- remover `PUSH` da lista para simular um device sem push
- deixar apenas `SMS` para testar fallback e roteamento alternativo
- desligar o toggle `Forcar online` para simular `offline`

## Nota FCM

O codigo usa `@react-native-firebase/messaging`, mas ainda precisa da configuracao nativa normal do Firebase no Android/iOS:

- `google-services.json` para Android
- `GoogleService-Info.plist` para iOS
- plugins Gradle/Xcode adequados
