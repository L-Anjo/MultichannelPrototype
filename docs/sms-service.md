# SMS Service

## Papel no sistema

O `sms-service` tem uma responsabilidade maior do que o nome sugere.

No prototipo ele trata:

- `SMS`
- `Email` mock
- fallback de falhas vindas de `Push`

## Ficheiros principais

- [Program.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/sms-service/Sms.Service/Program.cs)
- [Worker.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/sms-service/Sms.Service/Worker.cs)
- [ChannelDispatchSimulator.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/sms-service/Sms.Service/ChannelDispatchSimulator.cs)

## Topics consumidos

- `alerts.processed`
- `alerts.dispatched`

## Topics publicados

- `alerts.dispatched`
- `alerts.failed`

## Como funciona

### Caminho 1: envio inicial

Quando consome `alerts.processed`, o servico:

1. desserializa o evento
2. verifica se `Channel` e `Sms` ou `Email`
3. executa retries
4. publica o resultado em `alerts.dispatched`

### Caminho 2: fallback

Quando consome `alerts.dispatched`, o servico:

1. procura eventos com `Status = Failed`
2. calcula o proximo canal com `NextFallbackChannel`
3. valida se o device suporta esse canal
4. tenta de novo

Exemplo:

- `Push` falhou -> tenta `Sms`
- `Sms` falhou -> tenta `Email`
- `Email` falhou -> publica `alerts.failed`

## Porque e este servico que trata o fallback

Porque o fallback nao e uma chamada interna entre servicos.

Em vez disso:

- um servico publica falha
- outro servico consome essa falha
- o pipeline continua por eventos

Isto e mais distribuido e menos acoplado do que um metodo direto do tipo `trySmsAfterPushFail()`.

## Simulacao

`ChannelDispatchSimulator` injeta falhas aleatorias consoante o canal:

- `SmsFailureRate`
- `EmailFailureRate`

Isto e util para demonstrar resiliência e fallback.

## O que publica em falha terminal

Quando nao existe mais fallback possivel, cria `AlertFailedEvent` e publica em `alerts.failed`.

Isto e o equivalente, no mundo assíncrono, a marcar o trabalho como definitivamente falhado.
