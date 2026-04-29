# Global Technical Architecture

## Visão geral

Este prototipo implementa uma arquitetura orientada a eventos com Kafka como backbone central.

A diferenca principal para uma arquitetura REST classica e esta:

- em REST, um servico chama outro servico diretamente
- aqui, um servico publica um evento e outros servicos reagem a esse evento

Ou seja, a comunicacao interna nao e feita por HTTP entre servicos.

## Mapa de responsabilidades

- `alerts-api`: traduz HTTP para eventos Kafka
- `device-service`: constroi a visao materializada de devices
- `decision-engine`: aplica regras e resolve canal por device
- `push-service`: trata notificacoes push
- `sms-service`: trata SMS, Email e fallback
- `whatsapp-service`: simula WhatsApp
- `telegram-service`: simula Telegram
- `mobile-app`: fornece contexto de dispositivo ao sistema

## Fluxo tecnico completo

### Fase 1: entrada de contexto

1. A app faz `POST /devices`
2. A API publica `devices.registered`
3. `device-service` e `decision-engine` consomem o evento
4. ambos constroem ou atualizam a sua projeção local

Depois:

1. a app faz `PUT /devices/{id}/status`
2. a API publica `devices.status.updated`
3. `device-service` e `decision-engine` atualizam o estado local

### Fase 2: entrada do alerta

1. Um cliente envia `POST /alerts`
2. A API converte CAP XML em `AlertCreatedEvent`
3. publica em `alerts.created`

### Fase 3: decisao

1. `decision-engine` consome `alerts.created`
2. usa a projeção local de devices
3. resolve os devices alvo
4. para cada device, escolhe canal inicial
5. publica um `AlertProcessedEvent` por device em `alerts.processed`

### Fase 4: dispatch

Cada dispatcher consome `alerts.processed`:

- `push-service` trata `Push`
- `sms-service` trata `Sms` e `Email`
- `whatsapp-service` trata `Whatsapp`
- `telegram-service` trata `Telegram`

Depois de tentar enviar, publica `alerts.dispatched`.

### Fase 5: fallback

1. `push-service` publica falha em `alerts.dispatched`
2. `sms-service` consome a falha
3. calcula o proximo canal
4. tenta `Sms` ou `Email`
5. se esgotar, publica `alerts.failed`

## Kafka explicado do ponto de vista de quem conhece REST

### Producer

Um producer e quem escreve num topic.

No teu sistema:

- a API e producer de `alerts.created`
- a API e producer de `devices.registered`
- a API e producer de `devices.status.updated`
- o `decision-engine` e producer de `alerts.processed`
- os dispatchers sao producers de `alerts.dispatched`

### Consumer

Um consumer e quem le de um topic.

No teu sistema:

- `decision-engine` consome alertas e eventos de devices
- `device-service` consome eventos de devices
- dispatchers consomem trabalho e resultados

### Topic

Um topic e um log de mensagens ordenado por particao.

Nao e uma chamada de funcao.
Nao e um endpoint.
Nao e uma fila “consome e desaparece” no sentido mais simples.

E um stream persistente onde varios servicos podem ler.

### Consumer group

Um consumer group permite escalar horizontalmente.

Se tiveres:

- 3 particoes
- 3 replicas de um servico no mesmo group

o Kafka distribui o trabalho entre essas replicas.

Isto e equivalente, em termos de capacidade, a ter varios workers a consumir da mesma fila.

## Eventual consistency

Uma diferenca critica para REST e a consistencia eventual.

Quando a API responde `202`, o trabalho ainda esta em curso.

Isso significa:

- a resposta HTTP nao prova que a notificacao ja foi entregue
- apenas prova que o pedido entrou no pipeline

Por isso o sistema precisa de eventos intermediarios:

- `created`
- `processed`
- `dispatched`
- `failed`

## Porque isto escala melhor

Escala bem porque separa etapas:

- entrada HTTP
- decisao
- dispatch
- fallback

Cada etapa pode escalar independentemente.

Exemplos:

- se houver muitos alertas, aumentas replicas do `decision-engine`
- se houver muito push, aumentas replicas do `push-service`
- se houver muito SMS, aumentas replicas do `sms-service`

## Padrões que ja aparecem no prototipo

- event-driven choreography
- materialized view / projection
- asynchronous command handling
- retry with exponential backoff
- channel-specialized workers
- eventual consistency

## Limites tecnicos atuais

- projeções ainda em memoria
- sem snapshot persistente
- warm-up necessario apos restart
- sem idempotencia distribuida forte
- FCM em modo mock por defeito

## O que melhorar numa fase seguinte

- persistir devices numa base de dados
- adicionar snapshots para aquecer o `decision-engine`
- usar OpenTelemetry
- separar topicos por dominio e throughput
- aumentar brokers e particoes
- introduzir dead-letter topics

## Regra mental para entender o sistema

Se vieres de REST, pensa assim:

- a API nao “faz tudo”
- a API “abre o processo”
- o Kafka transporta esse processo entre servicos
- cada servico faz uma etapa pequena e publica o resultado

Esse e o modelo mental mais util para navegar este prototipo.
