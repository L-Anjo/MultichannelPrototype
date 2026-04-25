# MultichannelPrototype

Prototipo de disseminacao de alertas criticos com Event-Driven Architecture, CAP XML e Kafka como backbone exclusivo entre servicos.

## Objetivo

Demonstrar um fluxo distribuido e assíncrono para:

- receber alertas em CAP XML
- converter para eventos internos
- processar regras de decisao
- distribuir por Push, SMS e Email mock
- aplicar fallback automatico
- suportar testes de carga com k6

## Arquitetura

Todos os servicos comunicam apenas via Kafka.

Fluxo principal:

1. `alerts-api` recebe `POST /alerts` com `application/xml`.
2. O CAP XML e validado e convertido num `AlertCreatedEvent`.
3. O evento e publicado em `alerts.created`.
4. O `decision-engine` consome e decide os canais, publicando em `alerts.processed`.
5. O `push-service` e o `sms-service` consomem `alerts.processed` e executam os dispatches.
6. Resultados de dispatch saem em `alerts.dispatched`.
7. Falhas terminais saem em `alerts.failed`.

Topicos Kafka criados automaticamente:

- `alerts.created`
- `alerts.processed`
- `alerts.dispatched`
- `alerts.failed`

## Regras

Prioridade CAP:

- `Immediate` + `Severe` -> `HIGH`
- `Expected` + `Moderate` -> `MEDIUM`
- `Future` + `Minor` -> `LOW`

Routing:

- `HIGH` -> `Push` + `SMS`
- `MEDIUM` -> `Push`
- `LOW` -> `Email`

Fallback:

- `Push` falha apos 3 tentativas -> tenta `SMS` se nao estiver ja no routing primario
- `SMS` falha apos 3 tentativas -> tenta `Email`
- `Email` falha apos 3 tentativas -> publica em `alerts.failed`

## Estrutura

```text
.
├── docker-compose.yml
├── services
│   ├── api
│   ├── decision-engine
│   ├── push-service
│   └── sms-service
├── shared
│   └── Alerting.Shared
├── k6
├── samples
└── README.md
```

## Stack

- `.NET 8` no codigo executavel local
- Kafka em modo `KRaft`
- Kafka UI
- Portainer
- k6

Nota: o pedido original refere `.NET 10 LTS`, mas o ambiente atual desta workspace tem apenas o SDK `8.0.403`. O prototipo fica pronto a executar ja e a migracao para `.NET 10` passa essencialmente por atualizar `TargetFramework` e as imagens Docker quando esse SDK estiver disponivel.

## Arranque

Subir infraestrutura e servicos:

```bash
docker compose up --build
```

Endpoints e UIs:

- API: `http://localhost:8080/alerts`
- Swagger: `http://localhost:8080/swagger`
- Kafka UI: `http://localhost:8085`
- Portainer: `http://localhost:9000`

## Teste manual

Exemplo com o sample CAP:

```bash
curl -X POST http://localhost:8080/alerts \
  -H "Content-Type: application/xml" \
  --data-binary @samples/cap-alert.xml
```

Resposta esperada: `202 Accepted` com `eventId`, prioridade e topico.

## Load testing com k6

Executar:

```bash
k6 run k6/alerts-load.js
```

O script envia bursts de CAP XML para `/alerts` e verifica a taxa de respostas `202`.

## Observabilidade

- Logging estruturado em JSON em todos os servicos
- `eventId` incluido em scopes de processamento
- Consumer groups para escalar horizontalmente
- Semantica `at-least-once` com commit manual apos processamento

## Idempotencia e stateless

- Cada evento usa `eventId` como chave Kafka
- Os servicos nao mantem estado local de negocio
- O processamento foi desenhado para ser reexecutavel sem depender de armazenamento local

## Notas de implementacao

- O `sms-service` trata `SMS` e tambem o `Email` mock para manter a topologia pedida com quatro servicos
- Nao existe comunicacao HTTP ou RPC entre servicos
- Os dispatchers usam retries com backoff exponencial e taxas de falha configuraveis em `appsettings.json`
