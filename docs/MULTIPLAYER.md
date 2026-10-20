# MULTIPLAYER

Как устроен встроенный мультиплеер TerrariaXDK: без отдельного серверного EXE,
хост живёт внутри игрового процесса.

---

## 1. Почему пришлось переделывать

Консольная версия ходила через `Microsoft.Xna.Framework.Net.NetworkSession` — обёртку над
Xbox Live. В порте эта обёртка осталась, но `SignedInGamer`/`GamerServices` — заглушки:
`UI.HasOnline()` всегда возвращал `false`, `CanPlayOnline()` тоже, поэтому сетевые пункты
меню были недоступны.

ПК-версия (`refs/T111M`) устроена иначе: `Netplay` c `TcpListener`/`TcpClient`, плюс
**отдельный** `TerrariaServer.exe` (`ProgramServer.cs`). Нам нужен был третий путь:
логика ПК-версии, но сервер — внутри игры.

---

## 2. Слои

```
игровой код (NetMessage, Netplay, Main.UpdateClient/UpdateServer)
        │  умеет только это API
        ▼
NetworkSession            (Microsoft.Xna.Framework.Net)
        │  выбор транспорта
        ▼
IPeerTransport
     ├── TcpPeer       ← новый, по умолчанию
     └── MonoGamerPeer ← старый Lidgren, fallback
```

Ключевая идея: **`NetworkSession` остаётся нетронутым API**, меняется только транспорт.
Игровой код по-прежнему пишет `Netplay.gamer.SendData(packet, options, gamer)`,
а куда это уйдёт — TCP или Lidgren — решает `NetworkConfig.UseTcpTransport`.

### Почему интерфейс, а не переписывание

`NetMessage.cs` (75 КБ) и `Netplay.cs` содержат всю игровую сериализацию (packet id,
компактные целые, секции тайлов). Переписывать их — значит сломать совместимость
с уже работающими сценариями. Транспорт — самая изолированная часть, поэтому менялся
только он.

---

## 3. Протокол

Один TCP-порт (`[Network] Port`, по умолчанию 7777). Каждый кадр:

```
[int32 length][byte opcode][payload ...]
```

`length` — длина тела (opcode + payload), little-endian.
Верхняя граница — 4 МБ (`WireFormat.MaxFrameSize`), чтобы битый заголовок
не приводил к гигантскому выделению памяти.

| Opcode | Имя | Payload | Кто шлёт |
|---|---|---|---|
| 1 | `Hello` | `int32 protocol`, `string gamertag`, `string modSignature`, `string password` | клиент → хост |
| 2 | `Welcome` | `int64 assignedId` | хост → клиент |
| 3 | `Roster` | `int32 count`, затем `(int64 id, string tag)*` | хост → клиент |
| 4 | `Data` | сырой пакет игры (`NetMessage`) | обе стороны |
| 5 | `Join` | `int64 id`, `string tag` | хост → остальные |
| 6 | `Leave` | `int64 id` | хост → остальные |
| 7 | `State` | `int32 NetworkSessionState` | хост → клиент |
| 8 | `RosterRequest` | — | зарезервировано |
| 9 | `Reject` | `string reason` | хост → клиент |

Строки — `BinaryWriter.Write(string)` (7-bit length prefix + UTF-8).

### Handshake

1. Клиент подключается, шлёт `Hello` с `ProtocolVersion`, подписью модов и паролем.
2. Хост проверяет версию протокола, подпись (`ModLoader.IsCompatible`) и пароль.
   Несовпадение → кадр `Reject` с причиной, соединение закрывается; клиент показывает
   «Connection refused: …» и завершает сессию.
3. Хост выдаёт `Welcome` с id клиента.
4. Хост шлёт `State` — чтобы поздний подключившийся сразу знал, идёт ли игра.
5. Хост шлёт `Roster` — кто уже в игре.
6. Хост рассылает остальным `Join` и добавляет геймера в сессию.

### Relay

Хост — звезда: клиентский трафик идёт на хост, хост раздаёт остальным.
`TcpPeer.ReceiveLoop` после обработки кадра вызывает `Broadcast(frame, except: sender)`.
Это упрощает NAT (нужен проброс только у хоста) и повторяет модель ПК-версии.

---

## 4. Потоки

Правило из `AGENTS.md`: игровая логика — один поток.

- Фоновые потоки (`AcceptLoop`, `ReceiveLoop`, `ClientLoop`, `DiscoveryLoop`)
  **не трогают** `Main`, `UI`, мир и игроков. Они только:
  - читают байты и разбирают кадры;
  - кладут `Action` в `ConcurrentQueue<Action> _inbox`.
- `TcpPeer.Drain()` вызывается из `NetworkSession.Update()` на игровом потоке и
  применяет все накопленные действия: `AddRemoteGamer`, `RemoveRemoteGamer`,
  `EnqueueIncomingPacket`, `ApplyRemoteSessionState`.

Отправка синхронная и защищена `lock (stream)` в `WireFormat.WriteAll` — очередей
на отправку нет намеренно: `NetMessage.SendMessage()` и так вызывается из игрового
потока, а блокирующая запись упрощает порядок доставки.

### Локальные игроки (split-screen)

`LocalNetworkGamer.SendData` изменился: пакет больше не кладётся в
`NetworkEvent.PacketSend` для всех подряд (это было для Lidgren-модели).
Теперь:

- локальные геймеры → `Session.SendNetworkEvent(PacketSend)` (замыкание в процессе);
- удалённые → `Session.SendPacketToAll` / `SendPacket` → `IPeerTransport`.

Так split-screen продолжает работать, а удалённые игроки получают данные по сети.

---

## 5. Отличие от `refs/T111M`

| | `refs/T111M` (ПК) | TerrariaXDK |
|---|---|---|
| Хост | отдельный `TerrariaServer.exe` | внутри игрового процесса |
| Транспорт | TCP + собственный `messageBuffer` | TCP + `IPeerTransport` поверх `NetworkSession` |
| Формат | собственный `NetMessage` ПК-версии (другие id) | консольный `NetMessage` (несовместим с ПК) |
| Секции мира | 200×150 тайлов | 40×30 тайлов (`NetClient.TileSectionsTaken`) |
| Игроков | 255 | 8 (`Main.PlayerSet`) |

**Совместимости с ПК-серверами нет и не планируется** — разные packet id и разные
размеры секций. Это отдельная задача из `ROADMAP.md` (этап 6), если понадобится.

---

## 6. LAN-дискавери

UDP на `[Network] DiscoveryPort` (по умолчанию 7778).

- Хост слушает широковещательные запросы длиной ≥4 байта с префиксом `TXDK`.
- Отвечает строкой: `TXDK|gamertag|players|freeSlots|modSignature|port`.
- Клиент (`TcpPeer.Discover`) шлёт broadcast, собирает ответы 1.2 с, отбрасывает
  хосты с несовпадающей подписью модов и добавляет остальные в
  `Netplay.availableSessions`.

Дополнительно `Netplay.DiscoverLanSessions()` подмешивает адрес из
`[Network] JoinAddress` (формат `host` или `host:port`) — ручное подключение,
когда broadcast не проходит (разные подсети, VPN).

---

## 7. Что ещё не сделано

- **Пароль хоста.** `NetworkConfig.Password` объявлен, но `Hello` его не передаёт
  и хост не проверяет. См. `ROADMAP.md` этап 3.
- **Пинг/потери в UI.** Данные есть только в логе.
- **Тест «хост + 2 клиента».** `--selftest` проверяет framing и loopback, но не
  полноценную сессию: для неё нужен `NetworkSession`, а он тянет `GamerServices`.
  Кандидат на отдельный headless-режим (см. `IDEAS.md`).
- **Dedicated-хост.** Тот же `TcpPeer` без `Main`/графики.
