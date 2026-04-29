# Whatsapp Service

## Papel no sistema

O `whatsapp-service` simula um canal OTT.

No prototipo ele existe para demonstrar que a arquitetura consegue suportar novos canais sem alterar a forma base de comunicacao entre servicos.

## Ficheiros principais

- [Program.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/whatsapp-service/Whatsapp.Service/Program.cs)
- [Worker.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/whatsapp-service/Whatsapp.Service/Worker.cs)

## Topics consumidos

- `alerts.processed`

## Topics publicados

- `alerts.dispatched`

## Como funciona

1. consome `alerts.processed`
2. filtra apenas `Channel = Whatsapp`
3. escreve log estruturado
4. cria `AlertDispatchResultEvent` com sucesso
5. publica em `alerts.dispatched`

## Porque isto e util

Mesmo sendo mock, demonstra duas coisas:

- adicionar um canal nao exige criar chamadas REST entre servicos
- basta um novo consumer especializado no topic de trabalho

Num sistema real, aqui ficaria a integracao com a API de um provider WhatsApp Business.
