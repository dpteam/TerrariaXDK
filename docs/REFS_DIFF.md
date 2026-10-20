# REFS_DIFF

Сравнение архитектур TerrariaXDK и сборок из `refs/`.
Главная мысль: **кодовые базы очень разные**, несмотря на общий геймплей.
Копировать код нельзя, идеи — нужно.

---

## Что лежит в `refs/`

| Папка | Что это | Размер | Что оттуда брать |
|---|---|---|---|
| `T104C` | Terraria **1.0.4**, ранний ПК-клиент | ~28 .cs | серверная логика на сокетах, `-port/-join/-pass`, банлист |
| `T1061C` | Terraria **1.0.6.1** + чит-мод (`ModClass`) | ~35 .cs | консольные команды читов (`god`, `noclip`, `give`, `tppos`, …) |
| `T1061M` | Terraria 1.0.6.1 + **tConfig** | ~40 .cs | идеи мод-лоадера, `ProgramServer.cs` |
| `T111M` | Terraria **1.1.1** + tConfig | ~60 .cs | def-хендлеры, `Config.cs`, формат модов |
| `TShock` | Серверный мод с админкой | большой | права, регионы, баны, анти-чит, RCON/REST, UPnP |
| `NATUPNPLib` | Проброс портов через UPnP | 4 .cs | автопроброс порта для хоста |
| `XnaRessurector.cs` | Распаковщик XNB/XWB/XSB | 179 КБ, 1 файл | LZX, DXT1/3/5, `Texture2DReader`, `SoundEffectReader`, `XwbExtractor` — для конвертации контента в PNG/OGG |

Ни одна из этих сборок не компилируется вместе с TerrariaXDK: разные версии игры
и разные базовые типы.

---

## Ключевые различия

### Ввод

```
ПК (refs):        Keyboard.GetState() / Mouse.GetState()  → прямые проверки в Player
TerrariaXDK:      GamePad.GetState()  → GamePadState → BTN_* константы → Player
```

Консольная версия не знает про клавиатуру вообще: в `Player.UpdatePlayer` жёстко
проверяются `ui.PadState.ThumbSticks.Left.X`, `Buttons.A` и т.д.

**Как решено:** не переписывать `Player` (268 КБ!), а собирать синтетический
`GamePadState` из клавиатуры/мыши и подмешивать его к физическому в
`UI.UpdateGamePad`. Плюс отдельный путь для абсолютного курсора в
`Player.UpdateMouse`/`UpdateMouseSmart`.

### Экран и координаты

| | ПК | TerrariaXDK |
|---|---|---|
| Разрешение | произвольное окно | фиксированная база 540p, `ScreenMultiplier` |
| Зум | `Main.gameZoom`, 1.0–2.0 | `ZoomLevel`, 1.0–2.0, но через `DirectZoom` |
| Safe area | нет | `SafeAreaOffset*` на каждом `WorldView` |
| SpriteBatch | один общий | `CurrentView.ScreenProjection`, переключение `SetScreenView*` |
| Сплитскрин | нет | до 4 `UIInstance`, `NumActiveViews`, половина ширины экрана |

Поэтому любой UI-код из `refs/` требует пересчёта координат. В нашем коде
используйте `Main.ScreenMultiplier`, `CurrentView.ViewWidth` и
`CurrentView.SafeAreaOffset*`, а не абсолютные пиксели.

### Игроки и сущности

| | ПК | TerrariaXDK |
|---|---|---|
| Игроки | `Main.player[255]` | `Main.PlayerSet[9]` (8 игроков + резерв) |
| Локальные | 1 | до 4 (`UIInstance[4]`, split-screen) |
| NPC | `Main.npc[200]` | `Main.NPCSet[197]` |
| Снаряды | `Main.projectile[1000]` | `Main.ProjectileSet[...]` |
| Предметы | `Main.item[400]` | `Main.ItemSet` / пулы |

Индексация и границы другие — циклы из `refs/` нельзя переносить копипастом.

### Сеть

```
ПК (refs/T111M):
    TerrariaServer.exe  (отдельный процесс!)  →  Netplay.ServerLoop
    Terraria.exe        →  Netplay.ClientLoop
    Транспорт: TcpListener / TcpClient + messageBuffer (свой формат)

TerrariaXDK (было):
    NetworkSession (Xbox Live) → MonoGamerPeer (Lidgren) → 8 игроков
    Хост = один из игроков, отдельного сервера нет

TerrariaXDK (стало):
    NetworkSession → IPeerTransport
                       ├── TcpPeer       (внутри процесса, по умолчанию)
                       └── MonoGamerPeer (Lidgren, fallback)
```

Размеры секций мира тоже разные:

| | ПК | TerrariaXDK |
|---|---|---|
| Секция | 200×150 тайлов | 40×30 тайлов |
| Формат `NetMessage` | свой набор id | консольный набор id |
| Максимум игроков | 255 | 8 |

**Следствие:** совместимости с ПК-серверами нет. Подключиться к TShock-серверу
нельзя и наоборот. Это отдельная большая задача (см. `ROADMAP.md`, этап 6).

### Контент

```
ПК:           Content/Images/Tiles_0.png, Item_1.png, NPC_1.png, ...
              музыка .ogg/.mp3, шрифты .xnb

TerrariaXDK:  Content/Images/_sheetSprites.xnb   (атлас, 1489 кадров)
              Content/Images/_sheetTiles.xnb     (атлас, 219 кадров)
              Content/Fonts/HD/*.xnb
              Content/ExtMusic/*.wav
```

Атласы описаны в `_sheetSprites.cs` / `_sheetTiles.cs`: там перечислены
имена и прямоугольники. Игра обращается к спрайтам **по индексу**
(`SpriteSheet<_sheetSprites>.Draw(451, ...)`), а не по имени файла.

ПК-версия грузит отдельные файлы по имени. Именно из-за этого возможна идея
«динамической поддержки ПК-ресурсов» из `IDEAS.md`: нужен слой, собирающий атлас
из россыпи PNG по тем же прямоугольникам.

### UI

`UI.cs` — 284 КБ, полностью самописный: меню, инвентарь, крафт, миникарта,
достижения, лидерборды, чат. У ПК-версии всё иначе (другие экраны, другие
шрифты, `Main.menuMode` как `int` вместо enum `MenuMode`).

Следствие: экраны из `refs/` бесполезны как код, но полезны как **описание
поведения** (какие пункты, что делают).

---

## Что уже перенесено

| Возможность | Источник идеи | Где реализовано |
|---|---|---|
| Чит-меню | `refs/T1061C/ModClass.cs` (команды `god`, `noclip`, `give`, `tppos`, `bloodmoon`, `smashorb`, `dropmeteor`, `allbuffon`, …) | `Terraria/Cheats/` |
| Мультиплеер без серверного EXE | `refs/T111M/Netplay.cs` (модель «хост = игрок»), но сервер внутри процесса | `Terraria/Net/` |
| Мод-лоадер | `refs/T111M/Config.cs` + def-хендлеры | `Terraria/Mods/` |
| Клавиатура + мышь | `refs/T104C`, `T1061C` (прямой ввод) | `Terraria/Input/` |
| Паттерн «CLIENT_*» сообщений | `NetMessageId.cs`, `SendDataId.cs` (уже были) | используется как есть |

---

## Что стоит взять дальше

1. **`refs/T104C`** — самая старая и самая «серверная» сборка: `Netplay` на
   `TcpListener`, `ServerSock`/`ClientSock`, банлист, `spamCheck`, аргументы
   командной строки (`-port`, `-join`, `-pass`). Хороший источник для
   dedicated-хоста.
2. **`refs/TShock`** — единственный источник полноценной админки: права, группы,
   регионы, баны, анти-чит, RCON, REST, UPnP. Подробный разбор — в `IDEAS.md`,
   раздел «Из `refs/TShock`».
3. **`refs/T111M/Config.cs`** — def-хендлеры (`ItemDefHandler`, `npcDefHandler`,
   `ProjDefHandler`, `BuffDefHandler`) как модель описания контента модами.
4. **`refs/NATUPNPLib`** — автопроброс порта, чтобы «Host» работал без настройки
   роутера.
5. **`refs/XnaRessurector.cs`** — распаковка XNB в PNG/WAV (LZX, DXT) и XWB/XSB в
   отдельные треки. Нужен для перехода на открытые форматы, а он, в свою очередь,
   открывает путь к отказу от нативных `.dll` (см. `IDEAS.md`).

---

## Правило

> Из `refs/` берём **поведение и алгоритмы**, никогда — код.
> Любая новая фича сначала описывается в `IDEAS.md` (что и зачем), затем
> реализуется в своей папке, затем проверяется в `--selftest`.
