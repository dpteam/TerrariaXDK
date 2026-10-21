# AGENTS.md

Правила для всех, кто меняет код TerrariaXDK — людей и агентов.

Автор коммитов: **Terraria Agent**.

---

## 1. Базовая картина

TerrariaXDK — это **консольная** кодовая база Terraria (Xbox 360 v0.7.8), посаженная на
FNA/SDL2. Она принципиально отличается от ПК-версии:

| | TerrariaXDK (этот репозиторий) | ПК-версия (`refs/`) |
|---|---|---|
| Ввод | `GamePadState`, виртуальный курсор | `KeyboardState`/`MouseState` напрямую |
| Экран | 540p/720p/1080p, `ScreenMultiplier`, safe area | произвольное окно, зум 1.0–2.0 |
| Игроки | до 4 локальных (split-screen), `UIInstance[4]` | один игрок |
| Сеть | `NetworkSession` (Xbox Live → теперь TCP) | `Netplay` на `TcpListener`/`TcpClient` |
| Контент | атласы `_sheetSprites`/`_sheetTiles`, `Fonts/HD` | россыпь PNG + `Content/Images` |
| UI | полностью свой (`UI.cs`), без ImGui/меню ПК | свои экраны |

**Нельзя** копировать код из `refs/` один-в-один — он не соберётся. Из `refs/` берутся
**идеи и алгоритмы**, реализация пишется под консольную модель.

---

## 2. Архитектурные правила

1. **Игровую логику не переписываем.** Основной поток (Update/Draw, `WorldGen`, `NPC`,
   `Projectile`) трогаем только точечно, с сохранением поведения.
2. **Новые возможности — отдельными файлами и папками.**
   `Terraria/Input/`, `Terraria/Cheats/`, `Terraria/Net/`, `Terraria/Mods/`,
   `Terraria/Storage/`, `Terraria/Headless/`. Ничего общего не сваливаем в `Main.cs`.
3. **Только аддитивные правки в существующих файлах.** По возможности — одна-две строки
   вызова в уже существующую точку (`Main.Update`, `Player.Hurt`, `UI.UpdateGamePad`).
   Если правка большая — выносим в helper и вызываем из одной строки.
4. **Всё новое — под фича-флагом**, если оно меняет поведение. Флаги живут в `Settings.ini`
   (`Main` парсит их в конструкторе) и в статическом классе фичи (например
   `KeyboardMouseInput.Enabled`).
5. **Сингл vs мультиплеер.** Читы (и вообще любые локальные читерские вещи) обязаны
   проверяться через `CheatState.IsAvailable` → `Main.NetMode == 0`. Никаких «протащу
   проверку по месту» — гейт один.
6. **Сеть — только через `IPeerTransport`.** Не вызывайте `Lidgren` из игрового кода.
   Всё, что нужно игре, уже умеет `NetworkSession`. Модам — `ModNet` (отдельный opcode
   транспорта), а не `NetMessage`: его id зафиксированы консольным протоколом.
7. **Потоки.** Игровая логика — один поток. Всё, что приходит из сети, кладём в очередь
   и применяем на игровом потоке (`TcpPeer.Drain` вызывается из `NetworkSession.Update`).
   Из фонового потока **нельзя** трогать `Main.*`, `UI.*`, мир и игроков. Исключение —
   `Mod.OnPacket`: он вызывается на сетевом потоке и обязан только складывать данные.

---

## 3. Стиль кода

- Язык — C#, `net40`, `LangVersion=latest`, `PlatformTarget=x64`, `AllowUnsafeBlocks`.
- Скобки — новая строка (как в существующих файлах).
- Отступ — **табы**.
- Приватные поля — `_camelCase`, статические — `PascalCase`.
- `var` не используем (в декомпилированном коде его нет; единообразие важнее).
- Комментарии — по-английски, по делу: объясняют **почему**, а не «что».
- Публичные типы фич — `public`; внутренности — `internal`.
- Файл на тип/фичу, не «God class».

---

## 4. Обязательные проверки перед коммитом

```powershell
tools\build.ps1                                   # build + self-test одним шагом
# или вручную:
dotnet build TerrariaXDK.csproj -v q --nologo     # 0 ошибок
# staging: Content + нативные DLL рядом с exe
.\bin\Debug\net40\TerrariaXDK.exe --selftest       # 0 failed
```

`--selftest` **не** требует GPU, поэтому гоняется везде. Если вы добавили загрузку
нового ассета — добавьте его в манифест `Headless/SelfTest.cs`, иначе тест
«content orphans» начнёт ругаться (и это правильно).

Полноценный запуск игры возможен только там, где есть OpenGL 2.1+. Если графики нет,
это фиксируется строкой `NoSuitableGraphicsDeviceException` в `terraria-debug.log`.

---

## 5. Content

- Игра грузит **только** `.xnb` (плюс `Content/Worlds/tutorial.wld` и `Content/ExtMusic/*.wav`).
- `Content/` — только чтение. Ничего не пишите туда, и не возвращайте `.dds`/`.png`.
- `.dds`/`.png` в `Content/` — сырьё, игрой не читаются.
- Перед удалением любого файла: искать в коде **без расширения** (`_sheetSprites`,
  `Achievement07`, `Background_12`), учитывая, что имена могут собираться конкатенацией
  (`"Achievement" + k.ToString("00")`, `"Fonts/HD/" + name + "_PS3"`).
- Удалённое — в `_RemovedContent/` с сохранением структуры, и фиксируется в
  `docs/CONTENT_AUDIT.md`.
- После любой правки `Content/` — прогнать `--selftest`.

---

## 6. Storage (что и куда пишется)

**Правило: игра не пишет ничего рядом с exe и ничего в профиль пользователя.**
Всё изменяемое идёт в `Storage/` рядом с exe — это делает установку портативной.

| Что | Куда | Кто пишет |
|---|---|---|
| Персонажи | `Storage/Saves/Characters/PlayerN/*.plr` | `StorageContainer` |
| Миры | `Storage/Saves/Worlds/PlayerN/*.wld` | `StorageContainer` |
| Настройки игрока | `Storage/Saves/Settings/PlayerN/config.dat` | `StorageContainer` |
| Настройки игры | `Storage/Config/Settings.ini` | `Settings` |
| Бан-лист | `Storage/Config/banlist.txt` | `BanList` |
| Моды | `Storage/Mods/<Mod>/<Mod>.dll` | `ModLoader` |
| Отладочный лог | `Storage/Logs/terraria-debug.log` (прошлый запуск — `terraria-debug.prev.log`) | `Program` |
| Крашлог | `Storage/Logs/client-crashlog.txt` | `Program` |
| Резервные копии | `Storage/Backups/<мир>_<дата>.wld` | `WorldBackups` |
| Кэш сети | `Storage/Cache/servers.json`, `favorites.txt` | `MasterServerClient`, `ServerFavorites` |
| Кэш | `Storage/Cache/` (атласы — задел) | `AppPaths` |

- **Никогда не хардкодьте пути.** Единственный источник — `Terraria/Storage/AppPaths.cs`.
  Нужна новая папка — добавьте свойство туда и в `EnsureDirectories()`.
- `StorageDevice.RootOverride` выставляется в `AppPaths.EnsureDirectories()`; не
  переопределяйте его в другом месте.
- `Storage/` не коммитится. `Content/` — тоже (он рядом, вне git).
- `Settings.ini` рядом с exe — это **шаблон**: при первом запуске копируется в
  `Storage/Config/`. Не читайте его напрямую.

---

## 7. Git

- Репозиторий — этот корень; код и вся документация в нём.
  `Content/`, `refs/`, `_RemovedContent/` лежат рядом с корнем, но в него не входят.
- Автор: `Terraria Agent <terraria-agent@localhost>`.
- Сообщения — Conventional Commits: `feat:`, `fix:`, `docs:`, `chore:`, `refactor:`.
  Английский, императив, до 72 символов в заголовке.
- Один коммит = одна законченная мысль. Сборка должна быть зелёной на каждом коммите.

---

## 8. Что делать при сомнении

1. Прочитать `IDEAS.md` — возможно, вопрос уже решён или отклонён с причиной.
2. Посмотреть `CHANGELOG.md` — что уже работает и какие баги были (чтобы не «чинить»
   заново то, что чинили, и не возвращать старое поведение).
3. Посмотреть `docs/REFS_DIFF.md` — как это было устроено в ПК-версии.
4. Если решение меняет архитектуру — сначала записать идею в `IDEAS.md`, потом код.
5. Не «улучшать» попутно декомпилированный код: переименования ради красоты ломают
   соответствие с `refs/` и усложняют диффы.
