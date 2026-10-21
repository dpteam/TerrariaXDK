using System;

namespace Terraria;

/// <summary>
/// The Collectors Edition bunny pet: the one thing this port ever read from the Windows
/// registry.
///
/// Why it moved: the check was <c>HKCU\Software\Terraria\Bunny == "1"</c>, which is set by the
/// PC Collectors Edition installer. On any other OS the call throws, the old code caught the
/// exception and the flag silently stayed false - so the feature was Windows-only by accident,
/// and <c>Main.cs</c> carried a Windows-only <c>using</c> just for it.
///
/// Now the flag has two sources, in order:
/// <list type="number">
/// <item><c>[Game] CollectorsEdition</c> in the settings file, set by <see cref="Load"/> during
/// startup - works everywhere, and lets a player who owns the PC edition say so on a machine
/// without the registry (Wine, a copied game folder, a fresh install);</item>
/// <item>the registry, on Windows only, guarded by <see cref="Portability.IsWindows"/> - so
/// someone who does have the PC edition installed gets the pet without configuring anything.</item>
/// </list>
/// The setting wins, because an explicit choice in the game's own config should beat a guess
/// about the machine.
/// </summary>
internal static class CollectorsEdition
{
	/// <summary>Settings section and key holding the manual override.</summary>
	public const string Section = "Game";

	public const string Key = "CollectorsEdition";

	/// <summary>Not decided by the settings file: fall through to the platform check.</summary>
	private const int Auto = 0;

	private const int On = 1;

	private const int Off = 2;

	private static int _choice = Auto;

	/// <summary>
	/// Reads the setting once, while the settings file is still open during startup. Kept as a
	/// parsed value rather than a <see cref="Settings"/> reference because the flag is resolved
	/// later, after the splash, when that file is long closed.
	/// </summary>
	public static void Load(Settings settings)
	{
		_choice = Auto;
		bool flag;
		if (!TryReadSetting(settings, out flag))
		{
			return;
		}
		_choice = flag ? On : Off;
	}

	/// <summary>True when the bunny pet should be granted.</summary>
	public static bool IsActive()
	{
		if (_choice == On)
		{
			return true;
		}
		if (_choice == Off)
		{
			return false;
		}
		return IsActiveInRegistry();
	}

	/// <summary>
	/// Reads the manual override. Returns false for "not set", which is different from "set to
	/// False": an absent key must fall through to the registry, an explicit False must not.
	/// </summary>
	internal static bool TryReadSetting(Settings settings, out bool value)
	{
		value = false;
		if (settings == null)
		{
			return false;
		}
		string text = settings.Get(Section, Key);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		text = text.Trim();
		if (text.Equals("True", StringComparison.OrdinalIgnoreCase) || text == "1")
		{
			value = true;
			return true;
		}
		if (text.Equals("False", StringComparison.OrdinalIgnoreCase) || text == "0")
		{
			value = false;
			return true;
		}
		// Unreadable value: treat it as absent rather than as False, so a typo does not silently
		// take away a pet the player does own.
		return false;
	}

	/// <summary>
	/// The original check, restricted to the OS where the registry exists. Never throws: an
	/// absent key, a permissions problem and a missing registry are all just "no".
	/// </summary>
	internal static bool IsActiveInRegistry()
	{
		if (!Portability.IsWindows)
		{
			return false;
		}
		try
		{
			Microsoft.Win32.RegistryKey currentUser = Microsoft.Win32.Registry.CurrentUser;
			currentUser = currentUser.CreateSubKey("Software\\Terraria");
			if (currentUser == null)
			{
				return false;
			}
			object value = currentUser.GetValue("Bunny");
			return value != null && value.ToString() == "1";
		}
		catch
		{
			return false;
		}
	}

	/// <summary>Test hook: forgets the stored choice, as if the setting were absent.</summary>
	internal static void ResetForTests()
	{
		_choice = Auto;
	}
}
