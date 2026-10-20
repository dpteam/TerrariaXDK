# CONTENT_AUDIT

Аудит папки `Content/` — что оставлено, что вычищено и почему.
Правило проверки: игра грузит **только** `.xnb` (через `ContentManager.OpenStream`,
который добавляет расширение сам), плюс `Content/Worlds/tutorial.wld` и
`Content/ExtMusic/*.wav` напрямую.

Все удалённые файлы **не стёрты**, а перемещены в `_RemovedContent/`
с сохранением структуры. Вернуть — скопировать обратно.

---

## Как проверялось

1. Собран полный список всех `Content.Load<...>("...")` по коду, включая пути,
   собранные конкатенацией (шрифты `Fonts/HD/` + имя + `_PS3`, ачивки
   `"Achievement" + k.ToString("00")`, фоны `"Images/Background_" + i`,
   лидерборды `"UI/Leaderboards/" + column.ToString().ToLower()`).
2. Для каждого файла в `Content/` проверено, встречается ли его путь **без расширения**
   в исходниках (регистронезависимо).
3. Отдельно проверено, что файл не грузится «сырым» через `TitleContainer`/`File`
   (так грузится только `tutorial.wld`).
4. Результат закреплён тестом `--selftest` (проверки `content manifest` и
   `content orphans`), чтобы список не разъехался с кодом.

---

## Удалено

### 1. Сырые текстуры (`.dds`, `.png`) — 96 файлов

Игра читает только `.xnb`. Рядом с каждым `.xnb` лежал исходник `.dds` (и `.png`
у ачивок), который не грузится ниоткуда.

- `Achievements/*.dds`, `Achievements/*.png` (39 + 39)
- `Fonts/**.dds` (17)
- `Fonts/HD/**.dds` (18)
- `UI/*.dds` (8: PSController, PSControllerHD, PSDPad, PSDPadHD, XBController,
  XBControllerHD, XBDPad, XBDPadHD)
- `UI/CharacterCreation/Palette.dds`
- `Images/UpsellHM.dds`
- `ACB.tga` в корне

### 2. `Content/FontsOld/` — 12 файлов

Старый формат шрифтов. В коде есть только упоминание в диагностическом списке
`Program.CheckCriticalPaths`; ни один `Content.Load` туда не ходит.
Строка из `CheckCriticalPaths` тоже убрана.

### 3. XACT-банки — 3 файла

`Sound Bank.xsb`, `Wave Bank.xwb`, `TerrariaMusic.xgs`.

Это остатки XACT-аудио консольной версии. Порт использует FNA + SDL2:
музыка идёт из `Content/ExtMusic/*.wav` через `SoundManager`/`MusicEntry`
(`SoundEffect.FromStream`), звуки — из `Content/Sounds/*.xnb` через
`SfxInstancePool`. Ни `SoundBank`, ни `WaveBank`, ни `TerrariaMusic` в коде нет.

`Wave Bank.xwb` — 9.5 МБ, самый крупный удалённый файл.

### 4. `SoundsOld.zip`

Архив старых звуков, нигде не распаковывается и не читается.

### 5. `UI/Controller_Layout01.xnb`

Строка `Controller_Layout01` отсутствует в коде. Раскладка контроллера рисуется
из `UI/XBController(.HD)` / `UI/PSController(.HD)` + текстовых подписей
(`Lang.Controls()`), а не из этой картинки.

### 6. Кнопки создания персонажа — 4 файла

`UI/CharacterCreation/Button_Abort.xnb`, `Button_Confirm.xnb`, `Button_Random.xnb`,
`Gender_Selector.xnb`.

`Terraria.CreateCharacter.Assets.LoadContent` грузит `Button_Difficulty_Soft/Med/Hard`
и иконки категорий, но **не** эти четыре. Подтверждение/отмена в UI создания
персонажа рисуются текстом и через `SpriteSheet<_sheetSprites>`
(`DrawCentered(136, ...)`), пол выбирается `Gender_Male`/`Gender_Female`.

### 7. `Sounds/Jump.xnb`

Найдено self-тестом. Звук прыжка в этой сборке не используется: `Player` вызывает
`Main.PlaySound(…, Style)` для двойного прыжка (`Sounds/Double_Jump`) и бега
(`Sounds/Run`). Строки `"Sounds/Jump"` в коде нет — есть только текстовая подпись
`"Jump"` в таблице управления.

### 8. `Fonts/HD/small_sc_PS3.xnb`

Тоже найдено self-тестом. `UI.LoadSplitscreenFonts` добавляет `_PS3` только к
`big_sc` и `small2_sc` (строки 1323–1324), `small_sc_PS3` не запрашивается никогда.
В `Fonts/` (без HD) этого файла и не было — асимметрия и была признаком опечатки.

---

## Оставлено и почему

| Что | Почему |
|---|---|
| `Content/Fonts/**` (35 файлов) | грузятся `UI.LoadFonts` / `LoadSplitscreenFonts`; PS3-варианты есть только у `big`, `small`, `small2` |
| `Content/Fonts/HD/**` (17) | выбираются при `ScreenHeight == FHD` |
| `Content/Images/Background_3..31.xnb` | `WorldView.LoadContent` грузит все 3–31 |
| `Content/Images/logo_1..4.xnb`, `Logo`, `Logo2` | сплэш-экран |
| `Content/ACB.xnb`, `ESRB.xnb`, `PEGI.xnb`, `USK.xnb` | `Lang.SetSystemLang()` возвращает имя по региону, `Main.LoadContent` грузит его напрямую |
| `Content/UI/Upsell/01_*.xnb` | по одному на язык (`Lang.LanguageId`) |
| `Content/UI/HowToPlay/*`, `Leaderboards/*`, `CharacterCreation/*` | `Assets.LoadContent` соответствующих модулей |
| `Content/Sounds/*.xnb` (102) | `Main.LoadAllSfxPools` |
| `Content/ExtMusic/*.wav` (19) | `SoundManager` по списку `MusicCueNames` |
| `Content/Worlds/tutorial.wld` | `WorldGen` — обучающий мир через `TitleContainer.OpenStream` |
| `Content/Achievements/*.xnb` (39) | `Terraria.Achievements.Assets` — ровно 39 |

---

## Итог

- Было: 463 файла.
- Удалено: 147 файлов, ≈ 11.2 МБ.
- Осталось: 316 файлов.

Проверка после чистки:

```
TerrariaXDK.exe --selftest
  ok   content manifest
  ok   content orphans
```
