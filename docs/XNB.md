# xnb2png — извлечение текстур из Content

`Content/` в этом порте — набор `.xnb` (скомпилированный XNA-контент). Чтобы править
графику, нужны исходные PNG. `tools/XnbToPng` читает `.xnb` и раскладывает PNG с той же
структурой папок.

## Сборка и запуск

```powershell
dotnet build tools\XnbToPng\XnbToPng.csproj

tools\XnbToPng\bin\Debug\net40\xnb2png.exe ..\Content ..\..\_extracted
tools\XnbToPng\bin\Debug\net40\xnb2png.exe --info ..\Content      # только список
tools\XnbToPng\bin\Debug\net40\xnb2png.exe --selftest             # проверки декодеров
```

Код возврата: `0` — всё разобрано, `1` — были ошибки (файлы перечислены в конце),
`2` — неверные аргументы.

## Что поддерживается

| | |
|---|---|
| Платформа | `w` (Windows/десктоп) и `x` (Xbox 360) |
| Версия XNB | 5 (XNA 4 / FNA) |
| Сжатие | LZX — тот же декодер, что использует сама игра |
| Форматы поверхности | `Color`, `Bgr565`, `Bgra5551`, `Bgra4444`, `Dxt1`, `Dxt3`, `Dxt5`, `Alpha8` |
| Mip-уровни | берётся самый большой, остальные пропускаются |

Из Xbox-контента сохраняются байты в порядке, который ожидают декодеры
(`SurfaceDecoder.SwapForXbox`) — иначе `Color` выглядел бы как синий шум, а DXT-блоки
рассыпались бы.

## Что сознательно не делается

- **Звук и шрифты.** `SoundEffectReader`, `SongReader`, `SpriteFontReader` распознаются
  и пропускаются (`skipped`), а не парсятся наполовину.
- **LZ4.** Некоторые сборщики контента (MonoGame) сжимают XNB в LZ4. Игра такое читать
  не умеет, поэтому и конвертер не умеет: было бы странно извлекать то, что не грузится.
- **Обратная упаковка.** `.xnb` для этого порта не нужны: игра грузит только их, но
  сборка контента идёт через XNA-пайплайн, а не через этот инструмент.

## Фактический результат на текущем Content

```
converted : 194
skipped   : 102   (все — SoundEffectReader)
failed    : 0
formats   : Color=99  Dxt1=88  Dxt5=7
```

Проверено глазами: `Images/Background_*.png` (Dxt1 с Xbox, тайлы деревьев),
`Images/Cursor.png` и `Images/_sheetTiles.png` (Dxt5 с Xbox, альфа корректная),
`Images/logo_1.png` (Color, LZX-сжатый Windows-файл), `Fonts/big.png` (атлас шрифта).

## Заметки по формату

Внутри XNB v5 всё лежит так:

```
'X','N','B', platform, version, flags, fileSize
flags & 0x80: int32 fileSize, int32 decompressedSize, LZX-фреймы
7bit readerCount, readerCount x (string name, int32 version)
int32 sharedResourceCount
7bit rootTypeId
Texture2D: int32 format, int32 width, int32 height, int32 mipCount,
           mipCount x (int32 size, bytes)
```

Две вещи, на которых легко споткнуться (обе были багами, пока не поправлены):

1. **`int32 version` после имени каждого type reader.** Без него весь разбор съезжает на
   4 байта, и корневой объект выглядит как «тип 0».
2. **Заголовок LZX-фрейма — `blkSize` (2 байта big-endian) и ничего больше.** Размер
   выхода неявно равен 32 КиБ, кроме случая `0xFF`. Если читать наоборот (сначала размер
   выхода), все сжатые файлы падают с «frame overruns the file».
