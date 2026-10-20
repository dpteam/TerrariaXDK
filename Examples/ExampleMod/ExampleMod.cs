using System;
using Terraria;
using Terraria.Mods;

// Assembly level metadata. The loader shows this name and version in the mod list and uses
// both for the multiplayer signature, so bump the version whenever behaviour changes.
[assembly: ModInfo("ExampleMod", "1.0.0", "Terraria Agent", "Reference implementation for the TerrariaXDK mod API")]

namespace ExampleMod;

/// <summary>
/// Small, harmless mod that demonstrates every hook the API exposes.
///
/// What it shows:
/// <list type="bullet">
/// <item><see cref="Load"/> / <see cref="Unload"/> - lifecycle and per-mod logging;</item>
/// <item><see cref="ModContext.ReadAllText"/> - reading a file from the mod's own folder;</item>
/// <item><see cref="OnWorldLoaded"/> - reacting to a world being ready (greeting in chat);</item>
/// <item><see cref="OnPlayerUpdate"/> - inspecting the local player each frame, throttled
/// so it does not spam the log;</item>
/// <item><see cref="OnWorldSaving"/> - writing your own data next to the world save;</item>
/// <item><see cref="OnGameUpdate"/> - per-frame game tick, independent of players;</item>
/// <item><see cref="ModContext.SendPacket"/> plus <see cref="OnPacket"/> - mod-to-mod
/// traffic without touching the game's own packet ids.</item>
/// </list>
///
/// It deliberately changes no gameplay: copy it, rename it, and build from there.
/// </summary>
public class ExampleMod : Mod
{
	private const int LogEveryFrames = 600;

	private int _frames;

	private int _worldSeconds;

	/// <summary>Greeting text, read once from the mod folder (falls back to a default).</summary>
	private string _greeting = "ExampleMod loaded.";

	public override void Load()
	{
		string text = Context.ReadAllText("greeting.txt");
		if (!string.IsNullOrEmpty(text))
		{
			_greeting = text.Trim();
		}
		Context.Log("loaded; hooking into a world will print '" + _greeting + "'");
	}

	public override void Unload()
	{
		Context.Log("unloaded after " + _worldSeconds + " s of world time");
	}

	public override void OnWorldLoaded()
	{
		// Main.NewText is the same path the game uses for chat lines, so this shows up in the
		// normal chat feed instead of only in the log.
		Main.NewText(_greeting, 120, 200, 255);
		Context.Log("world loaded: " + Main.WorldName);
		_worldSeconds = 0;
	}

	public override void OnPlayerUpdate(Player player)
	{
		if (_frames % LogEveryFrames != 0)
		{
			return;
		}
		// Only the local player is interesting for per-machine logic; remote players are
		// driven by the network and must not be touched.
		if (player != Context.LocalPlayer)
		{
			return;
		}
		Context.Log($"local player at ({player.XYWH.X >> 4}, {player.XYWH.Y >> 4}), hp {player.statLife}/{player.StatLifeMax}");
	}

	public override void OnWorldSaving()
	{
		// Mods own their persistence: the game never serialises custom state for you.
		// A real mod would write to Context.Directory, not into the game's save files.
		Context.Log("world saving; elapsed " + _worldSeconds + " s");
	}

	/// <summary>
	/// Mod traffic: the payload is yours alone, the mod id travels with it, so there is
	/// nothing to register and no way to collide with another mod or with the game.
	/// </summary>
	public override void OnPacket(byte[] payload)
	{
		// This runs on the network thread. Never touch Main.*, the world or players here -
		// stash what you need and act on it in OnGameUpdate.
		_pendingPackets++;
	}

	public override void OnGameUpdate()
	{
		// Runs before players update. Cheap work belongs here; anything heavy should be
		// throttled like the counter below.
		_frames++;
		if (_frames % 60 == 0)
		{
			_worldSeconds++;
		}
		if (_pendingPackets == 0)
		{
			return;
		}
		int num = _pendingPackets;
		_pendingPackets = 0;
		// Applied on the game thread, which is where world state is allowed to change.
		Context.Log("received " + num + " mod packet(s)");
	}

	/// <summary>Counter filled on the network thread, drained on the game thread.</summary>
	private volatile int _pendingPackets;
}
