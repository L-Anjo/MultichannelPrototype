# Telegram Service

## Papel no sistema

O `telegram-service` simula notificacoes via Telegram.

Arquiteturalmente, e um dispatcher OTT especializado, igual ao `whatsapp-service`, mas para outro canal.

## Ficheiros principais

- [Program.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/telegram-service/Telegram.Service/Program.cs)
- [Worker.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/telegram-service/Telegram.Service/Worker.cs)

## Topics consumidos

- `alerts.processed`

## Topics publicados

- `alerts.dispatched`

## Como funciona

1. le `alerts.processed`
2. processa apenas eventos com `Channel = Telegram`
3. regista o dispatch em log
4. publica um `AlertDispatchResultEvent` com sucesso

## Valor arquitetural

Este servico mostra que o sistema esta preparado para extensao horizontal por canal.

Para adicionar outro canal no futuro, o padrao seria o mesmo:

- consumer dedicado
- filtro por canal
- publish de `alerts.dispatched`
