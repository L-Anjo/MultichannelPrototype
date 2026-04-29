# Device Context Architecture

## Objetivo desta extensao

Esta extensao acrescenta uma dimensao essencial ao prototipo: o backend deixa de decidir apenas com base na prioridade do alerta e passa a decidir com base no contexto real do dispositivo.

Isso permite demonstrar:

- registo de devices reais
- deteccao de canais disponiveis
- escolha adaptativa do canal
- comportamento diferente para `online`, `offline`, `sem push token` e `OTT only`
- escalabilidade para cargas muito grandes via Kafka

## Como foi configurado

### 1. API

Foram adicionados dois endpoints:

- `POST /devices`
- `PUT /devices/{deviceId}/status`

Estes endpoints nao escrevem diretamente para nenhum outro servico. Em vez disso:

- recebem JSON
- validam o payload
- publicam eventos Kafka

Topicos usados:

- `devices.registered`
- `devices.status.updated`

### 2. Device service

Foi criado o `device-service`, que consome os topicos de devices, mantem um catalogo em memoria e persiste o estado atual em PostgreSQL:

- `deviceId`
- `userId`
- `channels`
- `pushToken`
- `isOnline`
- `networkType`

Este servico serve como materializacao do registo de dispositivos dentro da arquitetura orientada a eventos e como writer do catalogo persistente.

### 3. Decision engine

O `decision-engine` foi alterado para consumir:

- `alerts.created`
- `devices.registered`
- `devices.status.updated`

Com isto ele constroi uma projeção local em memoria e, quando chega um alerta, resolve o melhor canal inicial por device.

Ao arrancar, o `decision-engine` aquece essa projeção a partir do catalogo persistido em PostgreSQL antes de continuar o consumo normal de Kafka.

Importante:

- nao existe HTTP entre `decision-engine` e `device-service`
- a sincronizacao acontece apenas pelo stream Kafka

### 4. Dispatchers

O fluxo de dispatch ficou mais granular. Em vez de um evento agregado com uma lista de canais, o `decision-engine` publica um `alerts.processed` por `deviceId` e por canal escolhido.

Cada dispatcher consome apenas o que lhe pertence:

- `push-service` -> `PUSH`
- `sms-service` -> `SMS` e `EMAIL`
- `whatsapp-service` -> `WHATSAPP`
- `telegram-service` -> `TELEGRAM`

### 5. Push e FCM

O `push-service` usa o `pushToken` vindo do device.

No estado atual do prototipo:

- se houver credenciais reais configuradas, o servico fica pronto para ser adaptado a FCM real
- se nao houver credenciais, entra em modo mock e regista um envio simulado

Isto permite validar fluxo e fallback sem bloquear a demonstracao por causa da configuracao nativa do Firebase.

### 6. App mobile

A app React Native:

- gera um `deviceId`
- obtem `pushToken`
- regista o device no backend
- envia heartbeat de estado
- permite simular canais no codigo
- mostra notificacoes recebidas em foreground

Os canais disponiveis ficam em:

[deviceProfile.ts](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/mobile-app/src/config/deviceProfile.ts)

## Como funciona em casos concretos

### Caso A: utilizador online com Push disponivel

Configuracao:

- `channels = [PUSH, SMS, EMAIL]`
- `isOnline = true`
- `pushToken` presente
- alerta `HIGH`

Fluxo:

1. `alerts.created` entra no Kafka
2. `decision-engine` resolve `PUSH`
3. `push-service` recebe o evento
4. faz envio FCM mock
5. publica sucesso em `alerts.dispatched`

Este caso foi validado com o alerta `Push final check`, em que o `decision-engine` resolveu `Push` e o `push-service` registou sucesso.

### Caso B: utilizador offline

Configuracao:

- `channels = [PUSH, SMS, EMAIL]`
- `isOnline = false`
- alerta `HIGH`

Fluxo:

1. `decision-engine` ve que o device esta offline
2. ignora `PUSH`
3. resolve `SMS`
4. `sms-service` faz o envio mock
5. publica sucesso em `alerts.dispatched`

Este caso foi validado com o alerta `High offline`.

### Caso C: Push falha e entra fallback

Configuracao:

- `channels = [PUSH, SMS, EMAIL]`
- `isOnline = true`
- `pushToken` presente
- o simulador do push falha 3 vezes

Fluxo:

1. `decision-engine` resolve `PUSH`
2. `push-service` tenta 3 vezes
3. publica falha em `alerts.dispatched`
4. `sms-service` consome essa falha
5. calcula `next fallback = SMS`
6. tenta `SMS`
7. se `SMS` falhar, tenta `EMAIL`
8. se `EMAIL` falhar, publica em `alerts.failed`

Este caso foi validado durante o broadcast steady-state, em que `device-broadcast` começou por `Push`, falhou e acabou em `SMS`.

### Caso D: utilizador sem Push, mas com canais OTT

Configuracao:

- `channels = [WHATSAPP, TELEGRAM]`
- `isOnline = true`
- alerta `MEDIUM`

Fluxo:

1. `decision-engine` tenta `PUSH`
2. como o device nao o suporta, escolhe `WHATSAPP`
3. `whatsapp-service` faz dispatch mock
4. publica sucesso em `alerts.dispatched`

Este caso foi validado com o alerta `OTT steady state`.

### Caso E: broadcast

Configuracao:

- multiplos devices registados
- `X-Broadcast: true`

Fluxo:

1. `decision-engine` percorre todos os devices conhecidos
2. resolve o melhor canal por cada um
3. publica um `alerts.processed` por device
4. os dispatchers processam em paralelo via consumer groups

No teste realizado:

- `user-broadcast` foi para `PUSH` e depois `SMS` por falha simulada
- `user-ott` foi para `WHATSAPP`
- `user-123` foi para `SMS` porque o contexto ainda estava marcado como offline quando o alerta foi resolvido

## Porque isto suporta cargas muito altas

O prototipo esta desenhado para crescer em volume porque:

- Kafka desacopla produtores e consumidores
- os eventos usam chaves (`eventId`, `deviceId`) para processamento distribuido
- os dispatchers funcionam em consumer groups
- os servicos sao horizontalmente escalaveis
- a decisao e a entrega estao separadas

Para simular volume muito grande:

1. registar muitos devices com `k6/register-devices.js`
2. enviar carga dirigida com `k6/alerts-load.js`
3. testar broadcast com `k6/broadcast-alert.js`

Chegar a um milhao de utilizadores e realisticamente uma questao de:

- aumentar particoes
- aumentar numero de brokers
- aumentar replicas de `decision-engine` e dispatchers
- mover o catalogo de devices para armazenamento persistente e particionado

## Limites atuais do prototipo

- o catalogo de devices fica persistido em PostgreSQL, mas a projeção local continua em memoria
- a projeção do `decision-engine` continua a ser eventual, embora arranque agora com warm-up persistente
- FCM esta em modo mock por omissao
- nao existe persistencia forte para replays longos
- `SMS` e `EMAIL` continuam mock
- a app mobile esta preparada, mas ainda sem os ficheiros nativos do Firebase dentro da repo

## O que pode ser melhorado

### Persistencia

- PostgreSQL ja foi integrado como catalogo persistente de devices
- o passo seguinte natural seria acrescentar snapshots dedicados ou cache para warm-up ainda mais rapido

### Arranque e warm-up

- criar um topico de snapshot de devices
- fazer o `decision-engine` esperar por sincronizacao minima antes de aceitar alertas

### Escalabilidade Kafka

- passar de 1 broker para 3 brokers
- aumentar particoes nos topicos mais quentes
- separar topicos de device e alertas por dominos

### Entrega real

- integrar FCM Admin SDK real
- ligar Twilio ou outro provider SMS
- ligar SMTP real ou provider transactional

### Observabilidade

- adicionar OpenTelemetry
- meter metricas de latencia por etapa
- contar sucessos/falhas por canal e prioridade

### Idempotencia forte

- guardar `eventId` processados num store distribuido
- impedir reenvio em retries apos restart

### Mobile

- adicionar UI para alternar canais e estado em runtime
- guardar historico local de alertas recebidos
- suportar background handling e notificacoes persistentes

## Resultado final desta extensao

Depois desta extensao, o sistema ja demonstra:

- app mobile real como fonte de contexto
- backend consciente de capacidades do device
- decisao orientada por contexto
- fallback assíncrono por eventos
- suporte logico a canais OTT
- base operacional para testar cenarios de grande escala
