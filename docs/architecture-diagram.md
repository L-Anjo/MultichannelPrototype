# Architecture Diagram

```mermaid
flowchart LR
    Client[External Client / ANEPC]
    Mobile[Mobile App]
    API[alerts-api]
    Kafka[(Kafka)]
    DeviceSvc[device-service]
    Decision[decision-engine]
    Postgres[(PostgreSQL devices)]
    Push[push-service]
    Sms[sms-service]
    Whats[whatsapp-service]
    Telegram[telegram-service]
    Failed[alerts.failed]

    Mobile -->|POST /devices| API
    Mobile -->|PUT /devices/{id}/status| API
    Client -->|POST /alerts CAP XML| API

    API -->|devices.registered| Kafka
    API -->|devices.status.updated| Kafka
    API -->|alerts.created| Kafka

    Kafka -->|devices.registered / status.updated| DeviceSvc
    Kafka -->|devices.registered / status.updated| Decision
    DeviceSvc -->|upsert current catalog| Postgres
    Postgres -->|warm-up snapshot| DeviceSvc
    Postgres -->|warm-up snapshot| Decision

    Decision -->|alerts.processed 1 event per device| Kafka

    Kafka -->|Push events| Push
    Kafka -->|Sms / Email events| Sms
    Kafka -->|Whatsapp events| Whats
    Kafka -->|Telegram events| Telegram

    Push -->|alerts.dispatched| Kafka
    Sms -->|alerts.dispatched| Kafka
    Whats -->|alerts.dispatched| Kafka
    Telegram -->|alerts.dispatched| Kafka

    Kafka -->|failed push => fallback| Sms
    Sms -->|terminal failure| Failed
```

## Reading guide

- `alerts.created`: one alert request enters the async pipeline.
- `alerts.processed`: the decision engine expands one alert into many device-specific work items.
- `alerts.dispatched`: channel workers publish delivery results for each attempted dispatch.
- `PostgreSQL`: stores the current device catalog; Kafka stores the event history and work queue.
