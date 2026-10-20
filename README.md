# TerrariaXDK

Порт **Terraria (Xbox 360, v0.7.8)** на десктоп через [FNA](https://fna-xna.github.io/) /
SDL2, с добавленными клавиатурой-мышью, встроенным мультиплеером, читами для сингла и
поддержкой модов.

Проект вырос из **TerrariaOGC v1.03** (переименован в TerrariaXDK) и использует исходники
консольной версии как основу, а сборки ПК-версии из `refs/` — как источник идей.

---

## Что умеет

| Возможность | Где | Статус |
|---|---|---|
| Клавиатура + мышь | `Terraria/Input/KeyboardMouseInput.cs` | работает |
| Чит-меню (сингл) | `Terraria/Cheats/` | работает |
| Чат и консольные команды (`!god`, `!give`…) | `Terraria/Input/ChatInput.cs` | работает (сингл) |
| Мультиплеер без серверного EXE | `Terraria/Net/` | работает (LAN + IP) |
| Моды (сингл и онлайн) | `Terraria/Mods/` | API + загрузчик |
| Headless self-test | `TerrariaXDK.exe --selftest` | работает |
| Виртуальная клавиатура для геймпада | — | в планах, см. `IDEAS.md` |

---

## Сборка

Нужен .NET SDK (проверено на 9.0) и Windows для запуска (FNA-нативные DLL — Win64).

```powershell
tools\build.ps1                 # сборка + --selftest
tools\build.ps1 -Pack           # + dist/TerrariaXDK/ — готовая портативная папка
tools\build.ps1 -Pack -Zip      # + dist/TerrariaXDK-<дата>.zip
```

Скрипт сам подкладывает к exe нативные DLL (из `bin\Debug\net40`, `native\win64` или
переменной `TXDK_NATIVE_DIR`) и делает junction на `Content`, поэтому `--selftest`
работает в любой конфигурации.

Вручную то же самое:

```powershell
dotnet build TerrariaXDK.csproj -c Release
```

Результат: `bin/Release/net40/TerrariaXDK.exe` (net40, x64).

### Что положить рядом с exe

Игра ищет контент и нативные библиотеки рядом с исполняемым файлом:

```
TerrariaXDK.exe
TerrariaXDK.exe.config
Content/            <- копия Content из корня проекта (рядом с этой папкой)
FAudio.dll
FNA3D.dll
libtheorafile.dll
SDL2.dll
Settings.ini        <- опционально: шаблон настроек
```

Всё остальное игра создаёт сама в `Storage/`:

```
Storage/
  Saves/Characters/Player1/player1.plr    персонажи
  Saves/Worlds/Player1/world1.wld         миры
  Saves/Settings/Player1/config.dat       настройки игрока
  Config/Settings.ini                     настройки игры
  Mods/<Mod>/<Mod>.dll                    моды
  Logs/terraria-debug.log                 отладочный лог
  Logs/client-crashlog.txt                крашлог
  Backups/world1_20260101_120000.wld    резервные копии миров
  Cache/                                  кэш (задел)
```

Установка **портативна**: чтобы перенести игру на другую машину или флешку,
достаточно скопировать всю папку целиком — сохранения, моды и настройки поедут с ней.

Быстрый способ собрать рабочий каталог:

```powershell
dotnet build TerrariaXDK.csproj
$dst = "bin\Debug\net40"
Copy-Item -Recurse -Force ..\Content $dst
Copy-Item -Force ..\Settings.ini,..\FAudio.dll,..\FNA3D.dll,..\libtheorafile.dll,..\SDL2.dll $dst
```

Требуется **OpenGL 2.1+**. На машинах без GPU (или в RDP/VM с `GDI Generic`)
игра не запустится — для таких случаев есть `--selftest` (см. ниже).

---

## Запуск

```powershell
.\TerrariaXDK.exe              # игра
.\TerrariaXDK.exe --selftest   # проверки без графики
```

### Управление (клавиатура)

| Клавиша | Действие |
|---|---|
| `W A S D` | движение |
| `Space` / `Enter` | прыжок / подтвердить |
| `Shift` | двойной прыжок |
| `E` / `I` | инвентарь |
| `R` | бросок предмета |
| `C` | переключить smart cursor |
| `Esc` | пауза |
| `Tab` / `M` | карта |
| `F1` | чит-меню (только сингл) |
| `F` | полный экран |
| `+` / `-` | зум |
| ЛКМ / ПКМ | использовать предмет / граппл |
| ПКМ | поставить блок / взаимодействовать (как на ПК) |
| Средняя кнопка / `V` | граппл |
| `Q` / `R` | бросить предмет |
| `H` / `J` / `B` | быстрое лечение / мана / баффы |
| `1`…`9`, `0` | выбрать слот хотбара |
| `Enter` | открыть чат (в сингле — консоль команд) |

Все клавиши переназначаются: **Настройки → Keyboard bindings**. Значения пишутся
в `Storage/Config/Settings.ini`, секция `[Keys]`; там же их можно править вручную.

Размер окна, полноэкранный режим и внутреннее разрешение (540p / 720p / 1080p) —
**Настройки → Display**. Окно применяется сразу, внутреннее разрешение — после перезапуска.
| Колесо | смена предмета в хотбаре |
| Стрелки | навигация по меню |

Мышь и геймпад работают одновременно: пока вы двигаете мышь, курсор абсолютный,
через ~3 секунды простоя управление возвращается стику.

---

## Настройки

`Settings.ini` создаётся при первом запуске в `Storage/Config/` (рядом с exe можно
положить шаблон — он будет скопирован туда) и дополняется новыми ключами:

```ini
[Game]
Gamertag=Terrarian
TrialActive=False
PSMode=False
UnlockAllRecipes=False

[Display]
ScreenHeight=0          ; внутреннее разрешение: 0 = 540p, 1 = 720p, 2 = 1080p
Width=1280              ; размер окна
Height=720
ShowFPS=False
StartFullscreen=True
ZoomLevel=0
AlwaysOnWatch=False
AlwaysOnDepth=False
AlwaysOnCompass=False

[Additions]
HardmodeAlert=False

[Input]
KeyboardMouse=True      ; выключить, чтобы получить оригинальное поведение

[Cheats]
Enabled=True            ; чит-меню доступно только в сингле

[Network]
Port=7777
Password=               ; пусто = пускать всех
JoinAddress=            ; "host:port" для быстрого подключения

[Mods]
Enabled=True
RequireMatchingMods=True

[Backups]
Enabled=True            ; датированные копии мира в Storage/Backups
Keep=10                 ; сколько копий хранить на мир (0 = выключить)
MinMinutes=15           ; интервал между копиями (0 = при каждом сохранении)
```

---

## Headless self-test

```powershell
.\TerrariaXDK.exe --selftest
```

Проверяет без графики:

1. **пути** — что всё пишется в `Storage/` рядом с exe, а не в профиль пользователя;
2. **манифест контента** — что каждый `Content.Load(...)` путь реально существует;
3. **осиротевшие файлы** — что в `Content/` нет ничего, что игра не грузит;
4. **framing** — сериализация кадров протокола;
5. **tcp loopback** — реальное TCP-соединение с handshake и обменом данными;
6. **конфиг сети** — парсинг `host:port`, `[::1]`, отказ на некорректных портах;
7. **моды** — загрузка, детерминированность подписи, отказ чужой подписи;
8. **читы** — что они включены в сингле и недоступны в мультиплеере.

Код возврата 0 — всё хорошо. Это же запускается в CI.

---

## Структура

```
.                           корень репозитория (эта папка)
  TerrariaXDK.csproj
  Terraria/                 игровая логика консольной версии
    Input/                  клавиатура + мышь
    Cheats/                 чит-меню (сингл)
    Net/                    встроенный TCP-транспорт
    Mods/                   API и загрузчик модов
    Storage/                единая точка путей (AppPaths)
    Headless/               self-test
  Microsoft.Xna.Framework*/ FNA-совместимый слой XNA
  Lidgren.Network/          сетевой стек (legacy-транспорт)
  SDL2/, FAudio.cs, ...     нативные обёртки
  README.md, AGENTS.md      документация проекта
  IDEAS.md, ROADMAP.md
  docs/                     подробные гайды

Рядом с корнем репозитория, но вне git:
  Content/                  ассеты (.xnb + музыка)
  refs/                     исходники ПК-версии и старых сборок
  _RemovedContent/          вычищенное из Content (архив)

Рядом с exe во время работы (тоже вне git):
  Storage/                  сохранения, логи, моды, конфиг
```

---

## Документация

Вся документация лежит рядом с кодом, в корне репозитория:

- `AGENTS.md` — правила работы над кодом (для людей и агентов).
- `IDEAS.md` — идеи, планы, отклонённые решения.
- `ROADMAP.md` — порядок работ и критерии готовности.
- `PROMPT.md` — исходное задание по проекту.
- `docs/MULTIPLAYER.md` — как устроен встроенный сетевой стек.
- `docs/MODDING.md` — как писать моды.
- `docs/CONTENT_AUDIT.md` — что и почему вычищено из `Content/`.
- `docs/REFS_DIFF.md` — сравнение архитектур TerrariaXDK и сборок из `refs/`.
  Там же — какой функционал стоит перенести из `TShock` (`refs/TShock`).
- `docs/PERFORMANCE.md` — аудит загрузки и ОЗУ: почему старт бывал нестабильным,
  куда уходит память и что осталось оптимизировать.
- `docs/INPUT.md` — построчное сравнение управления с ПК-версией (`refs/`):
  что расходится и что уже исправлено.

---

## Лицензия и правовой статус

Код происходит из декомпилированной клиентской сборки Terraria (Xbox 360) и
переиспользует открытые части FNA. Terraria и связанные ассеты — собственность
Re-Logic. Репозиторий существует для исследования совместимости и сохранения
архивных сборок; коммерческое использование ассетов не подразумевается.
