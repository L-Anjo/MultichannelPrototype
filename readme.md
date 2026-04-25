# MultichannelPrototype

Prototipo de disseminacao de alertas criticos com Event-Driven Architecture, CAP XML, Kafka, contexto de dispositivo e app mobile React Native.

## O que existe agora

- `alerts-api` para receber alertas CAP e registo de devices
- `decision-engine` com projeção de devices em memoria, alimentada por eventos Kafka
- `device-service` para materializar o catalogo de dispositivos
- `push-service` com FCM mock-ready
- `sms-service` com SMS e Email mock
- `whatsapp-service` e `telegram-service` como canais OTT simulados
- `mobile-app` em React Native com registo de device, heartbeat de estado e rececao FCM preparada

## Fluxo principal

1. A app mobile chama `POST /devices` e publica um `devices.registered`.
2. A app envia contexto periodico para `PUT /devices/{deviceId}/status` e publica `devices.status.updated`.
3. O `decision-engine` consome esses eventos e mantem uma projecao local de devices.
4. A API recebe um CAP XML em `POST /alerts` e publica `alerts.created`.
5. O `decision-engine` resolve, por device, o melhor canal inicial e publica `alerts.processed`.
6. Cada dispatcher consome apenas os eventos do seu canal e publica `alerts.dispatched`.
7. Falhas seguem fallback assíncrono: `Push -> SMS -> Email -> alerts.failed`.

## Regras de decisao

Prioridade CAP:

- `Immediate` + `Severe` -> `HIGH`
- `Expected` + `Moderate` -> `MEDIUM`
- `Future` + `Minor` -> `LOW`

Selecao inicial por device:

- `HIGH` -> `PUSH` se o device estiver online e tiver `pushToken`, senao `SMS`, senao `EMAIL`
- `MEDIUM` -> `PUSH`; se o device nao suportar push, pode cair para `WHATSAPP` ou `TELEGRAM`
- `LOW` -> `EMAIL`; se nao existir, tenta `TELEGRAM` e depois `WHATSAPP`

Fallback de entrega:

- `PUSH` falha -> tenta `SMS`
- `SMS` falha -> tenta `EMAIL`
- `EMAIL` falha -> publica em `alerts.failed`

## Topicos Kafka

- `alerts.created`
- `alerts.processed`
- `alerts.dispatched`
- `alerts.failed`
- `devices.registered`
- `devices.status.updated`

## Estrutura

```text
.
├── docker-compose.yml
├── services
│   ├── api
│   ├── decision-engine
│   ├── device-service
│   ├── push-service
│   ├── sms-service
│   ├── telegram-service
│   └── whatsapp-service
├── shared
│   └── Alerting.Shared
├── mobile-app
├── k6
├── samples
├── README.md
└── DEVICE_CONTEXT_ARCHITECTURE.md
```

## Arranque

```bash
docker compose up --build
```

UIs:

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Kafka UI: `http://localhost:8085`
- Portainer: `http://localhost:9000`

## Testes rapidos

Registar um device:

```bash
curl -X POST http://localhost:8080/devices \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "device-user123",
    "userId": "user-123",
    "channels": ["Push", "Sms", "Email"],
    "pushToken": "fcm-token-user123",
    "isOnline": true,
    "networkType": "wifi"
  }'
```

Atualizar contexto:

```bash
curl -X PUT http://localhost:8080/devices/device-user123/status \
  -H "Content-Type: application/json" \
  -d '{
    "isOnline": false,
    "networkType": "wifi"
  }'
```

Enviar alerta CAP para um utilizador:

```bash
curl -X POST http://localhost:8080/alerts \
  -H "Content-Type: application/xml" \
  -H "X-User-Id: user-123" \
  --data-binary @samples/cap-alert.xml
```

Enviar broadcast:

```bash
curl -X POST http://localhost:8080/alerts \
  -H "Content-Type: application/xml" \
  -H "X-User-Id: broadcast" \
  -H "X-Broadcast: true" \
  --data-binary @samples/cap-alert.xml
```

## Load testing

- `k6/alerts-load.js`: carga sobre `/alerts`
- `k6/register-devices.js`: registo massivo de devices
- `k6/broadcast-alert.js`: envio de alertas broadcast

Exemplos:

```bash
k6 run k6/register-devices.js
k6 run k6/alerts-load.js
k6 run k6/broadcast-alert.js
```

## Mobile app

Ver [mobile-app/README.md](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/mobile-app/README.md).

## Nota sobre arranque

Como o `decision-engine` constroi a sua projeção de devices a partir dos eventos Kafka, o primeiro alerta logo apos um restart pode chegar antes dessa projeção estar quente. Em steady-state o comportamento fica correto, e isso e o que foi validado no prototipo.

## Stack atual

- `.NET 8` no runtime local
- Kafka em modo `KRaft`
- React Native para a app mobile
- FCM preparado com modo mock no backend

O pedido original referia `.NET 10 LTS`, mas o ambiente desta workspace tem apenas o SDK `8.0.403`. O prototipo foi mantido executavel ja, e a migracao para `.NET 10` fica essencialmente limitada a atualizar `TargetFramework` e imagens Docker.
