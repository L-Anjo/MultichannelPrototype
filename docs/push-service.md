# Push Service

## Papel no sistema

O `push-service` processa notificacoes cujo canal inicial decidido e `Push`.

No prototipo, ele esta preparado para trabalhar com FCM, mas usa modo mock por omissao quando nao existem credenciais reais.

## Ficheiros principais

- [Program.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/push-service/Push.Service/Program.cs)
- [Worker.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/push-service/Push.Service/Worker.cs)
- [FcmPushSender.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/push-service/Push.Service/FcmPushSender.cs)
- [FcmOptions.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/push-service/Push.Service/FcmOptions.cs)

## Topics consumidos

- `alerts.processed`

## Topics publicados

- `alerts.dispatched`

## Como funciona

### Filtro por canal

O worker consome `alerts.processed`, mas ignora qualquer evento cujo:

- `processedEvent.Channel != Push`

Isto e um padrao importante no projeto:

- todos os dispatchers podem ler do mesmo topic logico
- cada um trata apenas o canal que lhe pertence

### Envio

Quando recebe um `AlertProcessedEvent` de push:

1. le `deviceId`, `userId`, `pushToken`
2. chama `FcmPushSender`
3. simula ou tenta envio
4. aplica retries
5. publica `AlertDispatchResultEvent`

### `FcmPushSender`

Esta classe abstrai a integracao FCM.

Se existir:

- `CredentialsPath`

o servico fica pronto para integrar envio real.

Se nao existir e `UseMockWhenCredentialsMissing=true`:

- faz log
- devolve sucesso logico

## Retry e backoff

O servico usa:

- `MaxRetries`
- `BaseDelayMilliseconds`

O delay entre tentativas cresce exponencialmente.

Na pratica:

- tentativa 1
- espera `base`
- tentativa 2
- espera `base * 2`
- tentativa 3

## O que publica

Publica um `AlertDispatchResultEvent` com:

- `Status = Succeeded` ou `Failed`
- `Attempts`
- `Channel = Push`
- `DeviceId`
- `UserId`
- `PushToken`

Se falhar, o evento vai para `alerts.dispatched` e depois outro servico pode decidir o fallback.

## O que ele nao faz

- nao decide fallback diretamente
- nao envia SMS
- nao mantem estado dos devices

Ele e um worker especializado apenas em push.
