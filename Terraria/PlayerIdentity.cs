using System;

namespace Terraria;

/// <summary>
/// The name other players see.
///
/// The port has no Xbox Live, so the console's gamertag has to come from somewhere. It used
/// to default to the literal string "Terrarian", which meant every player who never edited
/// Settings.ini appeared under the same name in LAN lists, the roster and the master server.
///
/// The default is now derived from the operating system account (and the machine name as a
/// fallback), which is both unique enough to tell players apart and requires no setup. The
/// player can still type their own in the network settings screen.
/// </summary>
internal static class PlayerIdentity
{
	/// <summary>Longest name; the roster and the LAN list use fixed-width columns.</summary>
	public const int MaxLength = 16;

	public const string Fallback = "Terrarian";

	/// <summary>Name this machine announces. Never empty.</summary>
	public static string Current
	{
		get
		{
			string text = NameRules.Sanitize(Main.Gamertag);
			return (text.Length == 0) ? Fallback : text;
		}
	}

	/// <summary>
	/// Picks a default name for a fresh install: the OS user name, then the machine name,
	/// then the generic fallback. Values that would be useless (empty, only punctuation,
	/// too long) are rejected so the roster never shows a blank entry.
	/// </summary>
	public static string Default()
	{
		string text = SanitizeCandidate(SafeUserName());
		if (text.Length == 0)
		{
			text = SanitizeCandidate(SafeMachineName());
		}
		return (text.Length == 0) ? Fallback : text;
	}

	/// <summary>
	/// Applies a new name to the running game: the setting, the static, and the signed-in
	/// gamer that the transport reads when it introduces itself. Renaming only the setting
	/// would take effect after a restart, which looks like the change was ignored.
	/// </summary>
	public static bool Rename(string name)
	{
		return Rename(name, persist: true);
	}

	/// <summary>
	/// As <see cref="Rename(string)"/>, but the caller can skip writing Settings.ini - the
	/// self-test uses that so it never renames the player on the machine running it.
	/// </summary>
	internal static bool Rename(string name, bool persist)
	{
		string text;
		string error;
		if (!NameRules.TryNormalize(name, MaxLength, out text, out error))
		{
			return false;
		}
		Main.Gamertag = text;
		try
		{
			UI ui = UI.MainUI;
			if (ui != null && ui.SignedInGamer != null)
			{
				ui.SignedInGamer.Gamertag = text;
			}
		}
		catch
		{
			// No UI yet: the setting is still saved and applies on the next start.
		}
		if (!persist)
		{
			return true;
		}
		try
		{
			Settings settings = new Settings(Terraria.Storage.AppPaths.SettingsFile);
			settings.Update("Game", "Gamertag", text, "Name other players see in multiplayer and in LAN lists.");
			settings.Close();
		}
		catch (Exception ex)
		{
			Program.LogWarn("Identity", "could not save the player name: " + ex.Message);
		}
		return true;
	}

	internal static string SanitizeCandidate(string candidate)
	{
		string text = NameRules.Sanitize(candidate);
		if (text.Length > MaxLength)
		{
			text = text.Substring(0, MaxLength).TrimEnd();
		}
		return text;
	}

	private static string SafeUserName()
	{
		try
		{
			return Environment.UserName;
		}
		catch
		{
			return null;
		}
	}

	private static string SafeMachineName()
	{
		try
		{
			return Environment.MachineName;
		}
		catch
		{
			return null;
		}
	}
}
