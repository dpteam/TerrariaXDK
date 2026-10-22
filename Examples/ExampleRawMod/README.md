# ExampleRawMod

A mod that is **not compiled**. Drop this folder into `Storage/Mods/` and start the game: the
item and its recipe are there.

```
Storage/Mods/ExampleRawMod/
  mod.ini                 name, version, author (optional)
  Item/Example Blade.ini  the item and its recipe
  Item/Example Blade.png  the sprite, next to its INI
```

## Why this exists

A compiled mod can do things a raw one cannot: run code (`OnUse`, a custom AI, custom drawing).
But most of what a mod says is data - damage, use time, a sprite, a recipe - and requiring a
.NET SDK to change a number is a bad trade. A raw mod is the same mod without the assembly.

The format is deliberately the same as an old tConfig pack, so a pack can be dropped in as it
is. The full rules are in `docs/MODDING.md`, section 10a.

## What this example shows

| Thing | Where |
|---|---|
| a new item | `type=-1` in `Item/Example Blade.ini` |
| its stats | `damage`, `useTime`, `knockBack`, ... (same names as the C# API) |
| its tooltip | `toolTip` |
| a recipe | `[Items]` for ingredients, `[Tiles]` for the station, `[Amount]` for the output |
| changing an existing item | a file named after it, e.g. `Item/Copper Bar.ini` with `value=1234` |

## Things worth knowing

- **A sprite is required** for a new item. Without a PNG next to the INI the file is skipped and
  the reason goes to `Storage/Logs/terraria-debug.log` with the `[Raw]` prefix.
- **One bad file does not cancel the pack.** Everything else still loads.
- **It takes part in multiplayer.** The signature is `name|version|raw:<content hash>`, so two
  players whose folders differ in any byte are told they differ instead of quietly disagreeing
  about item numbers. `disabled.txt` is not part of the hash.
- **The menu shows it as `[Raw]`**, with how many items it provides.
- **No code.** If you need behaviour, write a compiled mod (`Examples/ExampleMod`) - or start
  from this data and convert it into a project.
