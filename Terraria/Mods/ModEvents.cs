namespace Terraria.Mods;

/// <summary>
/// World events that mods can subscribe to.
///
/// This is the single place where the engine tells mods "something changed". Every call site
/// in the game is one line, and <see cref="IsActive"/> short-circuits immediately when no
/// mods are loaded, so the cost with vanilla content is a few static reads.
///
/// Events are suppressed during world generation and outside a running world. Generation
/// calls the same tile functions millions of times; firing a mod hook for each one would turn
/// a two-second generation into a minute.
/// </summary>
internal static class ModEvents
{
	/// <summary>Events actually handed to mods. Diagnostics and tests only.</summary>
	internal static int DispatchCount;

	/// <summary>
	/// Cheap gate: no mods, world generation in progress, or a menu open means no work.
	///
	/// The menu test is <c>CurMenuType</c> rather than a lifecycle flag on purpose:
	/// <c>IsGameStarted</c> is only ever set to true in this port and never cleared, so a flag
	/// that nobody resets would leak events into the menus.
	/// </summary>
	internal static bool IsActive => ShouldDispatch(WorldGen.Gen, ModLoader.ModCount, MenuOpen);

	/// <summary>
	/// The gate as a pure function, so every combination is testable without a running world.
	/// A missing UI counts as "in game": before the UI exists no world is running either, so
	/// there is nothing to suppress and nothing that could fire.
	/// </summary>
	internal static bool ShouldDispatch(bool generating, int modCount, bool menuOpen)
	{
		return !generating && modCount > 0 && !menuOpen;
	}

	private static bool MenuOpen
	{
		get
		{
			try
			{
				UI ui = UI.MainUI;
				return ui != null && ui.CurMenuType != MenuType.NONE;
			}
			catch
			{
				return true;
			}
		}
	}

	public static void TilePlaced(int x, int y)
	{
		if (!IsActive)
		{
			return;
		}
		DispatchCount++;
		ModLoader.DispatchTilePlaced(x, y);
	}

	public static void TileKilled(int x, int y)
	{
		if (!IsActive)
		{
			return;
		}
		DispatchCount++;
		ModLoader.DispatchTileKilled(x, y);
	}

	/// <summary>
	/// An NPC took damage. <c>npc.Life</c> already reflects the hit, so a mod can detect a
	/// kill by checking <c>npc.Life &lt;= 0</c>. The separate "died" event
	/// (<see cref="NpcDeath"/>) fires once per death; this one fires once per hit.
	/// </summary>
	public static void NpcHurt(NPC npc, int damage)
	{
		if (!IsActive || npc == null)
		{
			return;
		}
		DispatchCount++;
		ModLoader.DispatchNpcHurt(npc, damage);
	}

	/// <summary>An NPC appeared. Fires once, after it is fully initialised.</summary>
	public static void NpcSpawn(NPC npc)
	{
		if (!IsActive || npc == null)
		{
			return;
		}
		DispatchCount++;
		ModLoader.DispatchNpcSpawn(npc);
	}

	/// <summary>
	/// An NPC died. Called from <c>CheckDead</c>, which already guarantees once-per-death: it
	/// returns early for an inactive NPC and clears the slot at the end, so a piercing projectile
	/// that lands three hits on the same frame cannot report three deaths.
	/// </summary>
	public static void NpcDeath(NPC npc)
	{
		if (!IsActive || npc == null)
		{
			return;
		}
		DispatchCount++;
		ModLoader.DispatchNpcDeath(npc);
	}

	/// <summary>A player lost health; <paramref name="damage"/> is what was actually taken.</summary>
	public static void PlayerHurt(Player player, int damage)
	{
		if (!IsActive || player == null)
		{
			return;
		}
		DispatchCount++;
		ModLoader.DispatchPlayerHurt(player, damage);
	}

	/// <summary>
	/// A player changed the liquid at these coordinates (bucket use). Deliberately not the
	/// simulation: natural flow touches thousands of cells per frame.
	/// </summary>
	public static void LiquidChanged(int x, int y)
	{
		if (!IsActive)
		{
			return;
		}
		DispatchCount++;
		ModLoader.DispatchLiquidChanged(x, y);
	}

	internal static void Reset()
	{
		DispatchCount = 0;
	}
}
