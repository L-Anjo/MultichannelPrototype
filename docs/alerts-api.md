# Alerts API

## Papel no sistema

`alerts-api` e a porta de entrada HTTP do prototipo.

E o unico servico que recebe pedidos REST diretamente de clientes externos para:

- criar alertas
- registar devices
- atualizar estado de devices

Tecnicamente, a funcao principal desta API e transformar pedidos HTTP em eventos Kafka.

Ela nao faz orchestration direta nem chama os outros servicos por HTTP.

## Ficheiros principais

- [Program.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/api/Alerts.Api/Program.cs)
- [AlertsController.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/api/Alerts.Api/Controllers/AlertsController.cs)
- [DevicesController.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/api/Alerts.Api/Controllers/DevicesController.cs)
- [appsettings.json](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/api/Alerts.Api/appsettings.json)

## Stack tecnica

- ASP.NET Core Web API
- serializacao JSON com enums como string
- logging estruturado em JSON
- publisher Kafka atraves de `IKafkaEventPublisher`

## O que acontece no arranque

No `Program.cs`, a API:

1. configura logging em JSON
2. regista controllers MVC
3. ativa Swagger
4. liga a camada de publicacao Kafka com `AddKafkaMessaging`

Isto significa que qualquer controller pode publicar diretamente para topics Kafka usando a abstracao comum da biblioteca partilhada.

## Endpoints

### `POST /alerts`

Responsabilidade:

- receber um alerta em XML CAP
- validar estrutura minima
- calcular prioridade
- publicar em `alerts.created`

Fluxo interno:

1. le o body bruto como XML
2. faz parse com `XDocument`
3. extrai campos obrigatorios
4. converte `urgency + severity` em `AlertPriority`
5. cria um `AlertCreatedEvent`
6. publica o evento no Kafka
7. responde `202 Accepted`

### Porque responde `202`

Isto e importante.

A API nao garante que o alerta foi enviado por push, sms ou email no momento da resposta.

Ela apenas garante que o alerta foi aceite e colocado no pipeline assíncrono.

Em REST tradicional, muitas vezes a API tentaria terminar o trabalho antes de responder.

Aqui ela responde cedo porque o processamento continua noutros servicos.

### `POST /devices`

Responsabilidade:

- receber registo de um dispositivo mobile
- publicar em `devices.registered`

Fluxo interno:

1. recebe JSON com `deviceId`, `userId`, `channels`, `pushToken`, `isOnline`, `networkType`
2. valida campos minimos
3. cria `DeviceRegisteredEvent`
4. publica em Kafka
5. responde `202 Accepted`

Esta API nao grava o device numa base de dados propria.

O armazenamento e materializado depois por consumers Kafka.

### `PUT /devices/{deviceId}/status`

Responsabilidade:

- receber contexto dinamico do device
- publicar em `devices.status.updated`

Fluxo interno:

1. recebe o `deviceId` por route
2. recebe `isOnline` e `networkType` no body
3. cria `DeviceStatusUpdatedEvent`
4. publica no Kafka
5. responde `202 Accepted`

## Topics usados

Publica em:

- `alerts.created`
- `devices.registered`
- `devices.status.updated`

Nao consome nenhum topic.

## Porque esta API existe

No desenho do sistema, este servico separa duas camadas:

- interface sincrona com o mundo exterior
- pipeline interno assíncrono por eventos

Isto torna o sistema mais escalavel porque a API nao fica bloqueada a espera de toda a cadeia de notificacao terminar.

## O que ela nao faz

- nao decide o canal final
- nao sabe se o user esta online
- nao envia push
- nao envia sms
- nao consulta outros servicos

Tudo isso fica delegado ao ecossistema de consumers Kafka.
