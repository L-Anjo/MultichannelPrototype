# Decision Engine

## Papel no sistema

O `decision-engine` e o cerebro de negocio do prototipo.

E o servico responsavel por:

- manter uma projeção local do estado dos devices
- decidir o melhor canal inicial por device
- transformar um alerta bruto num conjunto de ordens de dispatch

## Ficheiros principais

- [Program.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/decision-engine/Decision.Engine/Program.cs)
- [Worker.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/decision-engine/Decision.Engine/Worker.cs)
- [DeviceProjectionStore.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/decision-engine/Decision.Engine/DeviceProjectionStore.cs)
- [KafkaConsumerFactory.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/decision-engine/Decision.Engine/KafkaConsumerFactory.cs)

## Stack tecnica

- .NET Worker Service
- consumer Kafka com commit manual
- projeção em memoria com `ConcurrentDictionary`
- publisher Kafka para eventos derivados

## Topics consumidos

- `alerts.created`
- `devices.registered`
- `devices.status.updated`

## Topics publicados

- `alerts.processed`
- `alerts.failed`

## O que faz tecnicamente

### 1. Constroi uma projeção de devices

O `decision-engine` nao faz `GET /devices`.

Em vez disso, ele aprende o estado dos dispositivos consumindo eventos.

Quando recebe:

- `DeviceRegisteredEvent`
- `DeviceStatusUpdatedEvent`

atualiza o `DeviceProjectionStore`.

Esta projeção guarda:

- `deviceId`
- `userId`
- `channels`
- `pushToken`
- `isOnline`
- `networkType`
- `lastUpdatedUtc`

### Porque isto e importante

Isto elimina acoplamento HTTP entre servicos.

O `decision-engine` consegue decidir sozinho porque tem uma copia local do estado relevante.

### 2. Decide o canal inicial

Quando chega um `AlertCreatedEvent`, o worker:

1. descobre se o alerta e dirigido ou broadcast
2. resolve os devices alvo
3. para cada device, calcula o melhor canal inicial

As regras estao centralizadas nas extensoes partilhadas:

- `HIGH`
- `MEDIUM`
- `LOW`

e usam:

- prioridade
- canais suportados pelo device
- `isOnline`
- `pushToken`

### 3. Publica um evento por device

Em vez de publicar um evento com uma lista de canais, o `decision-engine` publica um `AlertProcessedEvent` por `deviceId`.

Isto e importante porque:

- o dispatcher passa a receber trabalho ja resolvido
- o paralelismo aumenta
- o fallback fica mais controlavel

## Estrutura de processamento

No `Worker.cs`, a funcao `HandleMessageAsync` faz um dispatch por topic.

Isso significa que o mesmo processo trata:

- eventos de device
- eventos de alerta

Este padrao e comum em event-driven systems quando um servico precisa de manter contexto local e tambem reagir a comandos/eventos de negocio.

## Casos importantes

### Sem devices conhecidos

Se nao encontrar devices para um user ou broadcast, publica `alerts.failed`.

Isto impede que o alerta desapareca silenciosamente.

### Warm-up apos restart

Como a projeção e reconstruida a partir de Kafka, um alerta pode entrar antes de todos os eventos de device terem sido processados.

Nesse periodo inicial:

- o motor ainda nao conhece todos os devices
- pode emitir `alerts.failed` por ausencia de contexto

Isto e normal nesta abordagem quando nao existe snapshot persistente.

## O que ele nao faz

- nao envia notificacoes diretamente
- nao fala com push/sms por HTTP
- nao persiste estado em base de dados

Ele apenas decide e publica trabalho para o resto do ecossistema.
