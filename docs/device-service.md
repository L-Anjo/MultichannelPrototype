# Device Service

## Papel no sistema

O `device-service` e o servico que materializa o catalogo de dispositivos a partir dos eventos Kafka.

Ele funciona como uma projeção de leitura do dominio de devices.

## Ficheiros principais

- [Program.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/device-service/Device.Service/Program.cs)
- [Worker.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/device-service/Device.Service/Worker.cs)
- [DeviceStore.cs](/Users/anjo/Desktop/Mestrado/Tese/Code/MultichannelPrototype/services/device-service/Device.Service/DeviceStore.cs)

## Topics consumidos

- `devices.registered`
- `devices.status.updated`

## Topics publicados

- nenhum

## O que faz tecnicamente

### `DeviceStore`

O estado e mantido num `ConcurrentDictionary<string, RegisteredDevice>`.

Cada entrada representa o estado mais recente conhecido de um device.

### Quando chega `devices.registered`

O servico:

1. desserializa `DeviceRegisteredEvent`
2. faz upsert do device
3. grava:
   - user
   - canais
   - push token
   - estado online
   - tipo de rede

### Quando chega `devices.status.updated`

O servico:

1. encontra o device pelo `deviceId`
2. atualiza `isOnline`
3. atualiza `networkType`
4. atualiza `lastUpdatedUtc`

## Para que serve se o decision-engine ja tem uma projeção?

Porque sao duas responsabilidades diferentes:

- `decision-engine`: precisa de uma projeção local para decidir
- `device-service`: representa explicitamente o subdominio de devices

Num sistema evoluido, este servico poderia:

- expor query APIs
- persistir estado numa base de dados
- construir snapshots
- alimentar analytics

Neste prototipo ele serve como materializacao dedicada do estado dos devices.

## O que nao faz

- nao recebe HTTP
- nao decide canais
- nao envia notificacoes

E um consumer puro.
