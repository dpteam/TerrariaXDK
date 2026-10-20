# ExampleMod

Reference mod for TerrariaXDK. Small, harmless, and it exercises every hook the API
exposes — copy it as the starting point for your own mod.

## What it does

| Hook | Demonstrated behaviour |
|---|---|
| `Load()` | reads `greeting.txt` from the mod folder via `Context.ReadAllText` |
| `OnWorldLoaded()` | prints the greeting into the in-game chat (`Main.NewText`) |
| `OnGameUpdate()` | counts world time, throttled to once per second |
| `OnPlayerUpdate(Player)` | logs the local player's tile position and health every 10 s |
| `OnWorldSaving()` | logs before the world is written |
| `Unload()` | logs total world time when the mod goes away |

It changes no gameplay on purpose: the goal is to show *where* to hook, not what to do.

## Build

The game must be built first, because the mod references its assembly.

```powershell
# from the repository root
dotnet build TerrariaXDK.csproj
dotnet build Examples/ExampleMod/ExampleMod.csproj
```

Output: `Examples/ExampleMod/bin/Debug/net40/ExampleMod.dll`.

If you build the game into another configuration, fix `HintPath` in
`ExampleMod.csproj` (it points at `bin/Debug/net40/TerrariaXDK.exe`).

## Install

```
<game folder>/
  Storage/
    Mods/
      ExampleMod/
        ExampleMod.dll
        greeting.txt
```

Copy `ExampleMod.dll` and `greeting.txt` into `Storage/Mods/ExampleMod/`, then start the
game. Nothing else is needed — the loader scans `Storage/Mods/*/` at startup.

Verify it was picked up:

```powershell
TerrariaXDK.exe --selftest
# -> mods loaded: 1, signature 3dd2a148ec9af596
```

A non-`vanilla` signature means the mod set is active. In multiplayer that same signature
must match on the host, otherwise the join is rejected (see `docs/MODDING.md`).

## Edit the greeting

`greeting.txt` is plain text, read at `Load()`. Change it, restart, and the new line appears
in chat when a world loads. If the file is missing the mod falls back to a built-in string.

## Where to go next

- `docs/MODDING.md` — full API reference and multiplayer rules.
- `IDEAS.md` — planned API additions (mod commands, world events, mod networking).

## Notes

- The mod targets `net40`/`x64`, matching the game. Other targets will not load.
- `ModInfo` (assembly attribute) carries name and version; the version is part of the
  multiplayer signature, so bump it when behaviour changes.
- Do not touch `player` objects other than `Context.LocalPlayer` in
  `OnPlayerUpdate`: remote players are driven by the network.
