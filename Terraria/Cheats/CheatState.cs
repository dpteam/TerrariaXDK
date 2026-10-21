namespace Terraria.Cheats;

/// <summary>
/// Persistent state for the TerrariaXDK cheat menu. Every flag is a no-op in multiplayer:
/// <see cref="IsAvailable"/> is the single gate the game code checks, which keeps the
/// server-authoritative path untouched.
/// </summary>
internal static class CheatState
{
	/// <summary>Settings.ini: [Cheats] Enabled.</summary>
	public static bool Enabled = true;

	public static bool GodMode;

	public static bool NoClip;

	public static bool InfiniteMana;

	public static bool InfiniteBuffs;

	public static bool NoFallDamage;

	/// <summary>Cheats are strictly a single-player feature.</summary>
	public static bool IsAvailable => Enabled && Main.NetMode == 0;

	public static bool GodModeActive => IsAvailable && GodMode;

	public static bool NoClipActive => IsAvailable && NoClip;

	public static bool InfiniteManaActive => IsAvailable && InfiniteMana;

	public static bool InfiniteBuffsActive => IsAvailable && InfiniteBuffs;

	public static bool NoFallDamageActive => IsAvailable && NoFallDamage;

	/// <summary>True when at least one cheat toggle is currently on.</summary>
	public static bool AnyActive => GodMode || NoClip || InfiniteMana || InfiniteBuffs || NoFallDamage;

	/// <summary>Short label for the HUD badge, or null when nothing is active.</summary>
	public static string ActiveLabel
	{
		get
		{
			if (!AnyActive)
			{
				return null;
			}
			if (!IsAvailable)
			{
				// Toggles are ignored in multiplayer; saying so avoids "why is god mode off".
				return "cheats inactive (multiplayer)";
			}
			return "CHEATS ACTIVE";
		}
	}

	/// <summary>
	/// Writes <see cref="Enabled"/> back to Settings.ini so the choice survives a restart.
	/// The file may not exist yet; <see cref="Settings"/> creates it on demand.
	/// </summary>
	public static void PersistEnabled()
	{
		try
		{
			Settings settings = new Settings(Terraria.Storage.AppPaths.SettingsFile);
			settings.Update("Cheats", "Enabled", Enabled ? "True" : "False", "Enables the single-player cheat menu (TerrariaXDK addition). Has no effect in multiplayer.");
			settings.Close();
		}
		catch
		{
			// A read-only install must not break the menu; the toggle still applies for the
			// current session.
		}
	}

	/// <summary>Called once per frame before the players update.</summary>
	public static void Update()
	{
		if (!InfiniteBuffsActive)
		{
			return;
		}
		Player activePlayer = UI.MainUI?.ActivePlayer;
		if (activePlayer == null)
		{
			return;
		}
		for (int i = 0; i < activePlayer.buff.Length; i++)
		{
			if (activePlayer.buff[i].Type > 0 && !activePlayer.buff[i].IsDebuff())
			{
				activePlayer.buff[i].Time = 3600;
			}
		}
	}

	/// <summary>Clears every toggle. Used when leaving a single-player world.</summary>
	public static void Reset()
	{
		GodMode = false;
		NoClip = false;
		InfiniteMana = false;
		InfiniteBuffs = false;
		NoFallDamage = false;
	}
}
