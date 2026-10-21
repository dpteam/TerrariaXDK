namespace Terraria.Mods;

/// <summary>
/// Base class for every TerrariaXDK mod. A mod is a normal .NET class library placed in
/// <c>Mods/&lt;Folder&gt;/&lt;Assembly&gt;.dll</c>; the loader instantiates every type that
/// derives from <see cref="Mod"/> and calls <see cref="Load"/>.
/// </summary>
public abstract class Mod
{
	/// <summary>Assembly level metadata, filled in by the loader.</summary>
	public ModInfoAttribute Info { get; internal set; }

	/// <summary>Access to the mod's own files and to the running game.</summary>
	public ModContext Context { get; internal set; }

	public string Name => Info?.Name ?? GetType().Name;

	public string Version => Info?.Version ?? "0.0.0";

	/// <summary>Called once, on the main thread, after the game finished loading content.</summary>
	public virtual void Load()
	{
	}

	/// <summary>Called when the mod is unloaded (world exit / shutdown).</summary>
	public virtual void Unload()
	{
	}

	/// <summary>Called once per frame while a world is running.</summary>
	public virtual void OnGameUpdate()
	{
	}

	/// <summary>Called for every active player, every frame.</summary>
	public virtual void OnPlayerUpdate(Player player)
	{
	}

	/// <summary>Called after a world has been generated or loaded.</summary>
	public virtual void OnWorldLoaded()
	{
	}

	/// <summary>Called before the world is written to disk.</summary>
	public virtual void OnWorldSaving()
	{
	}

	/// <summary>A tile appeared at these coordinates (player placement, not world gen).</summary>
	public virtual void OnTilePlaced(int x, int y)
	{
	}

	/// <summary>A tile was destroyed at these coordinates (player mining, not world gen).</summary>
	public virtual void OnTileKilled(int x, int y)
	{
	}

	/// <summary>
	/// An NPC took damage. <c>npc.Life</c> is already updated, so <c>npc.Life &lt;= 0</c> means
	/// this hit killed it. In multiplayer this fires on whichever side applied the damage -
	/// check <c>Context.IsHost</c> before changing anything authoritative.
	/// </summary>
	public virtual void OnNpcHurt(NPC npc, int damage)
	{
	}

	/// <summary>
	/// An NPC appeared: spawned by the world, a boss summon, a statue or a mod.
	///
	/// Fires once per spawn, after the NPC is fully initialised (type, position, life), so a mod
	/// can read and change it. It does <b>not</b> fire for the NPC slots being recycled when an
	/// NPC despawns - that is a disappearance, not a spawn.
	/// </summary>
	public virtual void OnNpcSpawn(NPC npc)
	{
	}

	/// <summary>
	/// An NPC died: <c>Life</c> reached zero and its slot was cleared.
	///
	/// Exactly once per death, which is why it exists next to <see cref="OnNpcHurt"/>: a
	/// multi-hit weapon or a piercing projectile would otherwise look like several kills. Loot has
	/// already been dropped and the slot is inactive when this runs, so a mod that wants to change
	/// the loot must do it in <see cref="OnNpcHurt"/> while <c>Life &lt;= 0</c> and the NPC is
	/// still active.
	/// </summary>
	public virtual void OnNpcDeath(NPC npc)
	{
	}

	/// <summary>
	/// A player lost health. <paramref name="damage"/> is the damage actually taken after
	/// defence, so it is what the player saw in the combat text.
	///
	/// Fires on the machine that applied the damage. In multiplayer a client also receives the
	/// host's version of the same hit, so check <c>Context.IsHost</c> before acting on it if the
	/// action has to happen once.
	/// </summary>
	public virtual void OnPlayerHurt(Player player, int damage)
	{
	}

	/// <summary>
	/// A player changed the liquid at these coordinates - a bucket emptied or filled.
	///
	/// Only player actions, not the simulation: natural flow moves thousands of cells per frame,
	/// and a hook on that would be a hook on the water itself. If a mod needs "the lake is
	/// draining", it should sample the tiles it cares about in <see cref="OnGameUpdate"/> instead.
	/// </summary>
	public virtual void OnLiquidChanged(int x, int y)
	{
	}

	/// <summary>
	/// Called for every mod packet that arrives from another player. The payload is already
	/// yours (the mod id was stripped), so this is not called for packets you sent.
	///
	/// Runs on the network thread: do not touch <c>Main.*</c>, the world or players here.
	/// Queue the data and apply it from <see cref="OnGameUpdate"/>.
	/// </summary>
	public virtual void OnPacket(byte[] payload)
	{
	}
}
