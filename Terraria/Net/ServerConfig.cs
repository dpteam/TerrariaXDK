using System;
using System.IO;

namespace Terraria.Net;

/// <summary>
/// The optional hosting profile: <c>Storage/Config/ServerConfig.ini</c>.
///
/// Motivation: hosting a world and playing it are different jobs with different settings. A
/// shared world needs a password, a MOTD, protection and a published address; the player's own
/// <c>Settings.ini</c> is about their window size, zoom and keybinds. Keeping both in one file
/// means handing someone "the server config" hands them your keybinds too, and editing one
/// thing risks the other.
///
/// Precedence, stated plainly because guessing here would be a support nightmare:
/// <list type="bullet">
/// <item>No <c>ServerConfig.ini</c>: everything comes from <c>Settings.ini</c>, exactly as
/// before. Nothing changes for anyone who does not want this.</item>
/// <item><c>ServerConfig.ini</c> exists: every key <b>it</b> defines wins. Keys it omits still
/// come from <c>Settings.ini</c>, so a minimal file with just a password works.</item>
/// </list>
/// The file is never created behind the player's back: creating it would silently switch the
/// source of truth for a setting they just changed in the menu and saved elsewhere.
/// </summary>
internal static class ServerConfig
{
	public const string FileName = "ServerConfig.ini";

	private static bool _active;

	private static readonly object Sync = new object();

	/// <summary>True when <see cref="AppPaths.ServerConfigFile"/> exists and is in use.</summary>
	public static bool IsActive
	{
		get
		{
			lock (Sync)
			{
				return _active;
			}
		}
	}

	public static string FilePath => Terraria.Storage.AppPaths.ServerConfigFile;

	/// <summary>
	/// Applies the file over the values already loaded from Settings.ini. Invalid values are
	/// reported and ignored - a broken hosting file must not stop the game or silently change
	/// something else.
	/// </summary>
	public static void Apply()
	{
		lock (Sync)
		{
			_active = false;
		}
		if (!File.Exists(FilePath))
		{
			return;
		}
		Settings settings;
		try
		{
			settings = new Settings(FilePath);
		}
		catch (Exception ex)
		{
			Program.LogWarn("ServerConfig", "could not read " + FileName + ": " + ex.Message);
			return;
		}
		try
		{
			int num = ReadInt(settings, "Port", NetworkConfig.Port, 1, 65535);
			if (num != NetworkConfig.Port)
			{
				NetworkConfig.Port = num;
			}
			// An explicitly empty password means "no password" and must win over a password in
			// Settings.ini, otherwise the file could never turn the password off.
			if (Has(settings, "Password"))
			{
				NetworkConfig.Password = settings.Get("Network", "Password");
			}
			if (Has(settings, "MaxPlayers"))
			{
				NetworkConfig.MaxPlayers = ReadInt(settings, "MaxPlayers", NetworkConfig.MaxPlayers, 1, 255);
			}
			if (Has(settings, "PublicAddress"))
			{
				NetworkConfig.PublicAddress = settings.Get("Network", "PublicAddress");
			}
			if (Has(settings, "PublishToMaster"))
			{
				MasterServerClient.Publish = settings.Get("Network", "PublishToMaster").ToLower().Equals("true");
			}
			if (Has(settings, "Motd"))
			{
				HostModeration.Motd = (settings.Get("Network", "Motd") ?? string.Empty).Trim();
			}
			if (Has(settings, "Rules"))
			{
				HostModeration.Rules = (settings.Get("Network", "Rules") ?? string.Empty).Trim();
			}
			if (Has(settings, "DisableBuild"))
			{
				WorldProtection.DisableBuild = settings.Get("Network", "DisableBuild").ToLower().Equals("true");
			}
			if (Has(settings, "ProtectSpawnRadius"))
			{
				WorldProtection.SpawnRadius = ReadInt(settings, "ProtectSpawnRadius", WorldProtection.SpawnRadius, 0, 200);
			}
			if (Has(settings, "AntiCheat"))
			{
				AntiCheat.Mode = AntiCheat.Parse(settings.Get("Network", "AntiCheat"));
			}
			lock (Sync)
			{
				_active = true;
			}
			Program.LogInfo("ServerConfig", "hosting profile applied from " + FileName + " (port " + NetworkConfig.Port + ")");
		}
		catch (Exception ex)
		{
			Program.LogWarn("ServerConfig", "could not apply " + FileName + ": " + ex.Message);
		}
		finally
		{
			try
			{
				settings.Close();
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// Writes the current hosting values to the profile file and switches to it. Called when the
	/// host asks for it, never automatically.
	/// </summary>
	public static bool Create(out string error)
	{
		error = null;
		try
		{
			string text = Path.GetDirectoryName(FilePath);
			if (!string.IsNullOrEmpty(text) && !Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			Settings settings = new Settings(FilePath);
			settings.Update("Network", "Port", NetworkConfig.Port.ToString(), "TCP port the host listens on.");
			settings.Update("Network", "Password", NetworkConfig.Password ?? string.Empty, "Password clients must send. Empty means anyone may join.");
			settings.Update("Network", "MaxPlayers", NetworkConfig.MaxPlayers.ToString(), "How many remote players the host accepts (1-255).");
			settings.Update("Network", "PublicAddress", NetworkConfig.PublicAddress ?? string.Empty, "Address advertised to the master server. Empty lets the master use the address it sees.");
			settings.Update("Network", "PublishToMaster", MasterServerClient.Publish ? "True" : "False", "List this world publicly while hosting.");
			settings.Update("Network", "Motd", HostModeration.Motd ?? string.Empty, "Message shown to every player who joins.");
			settings.Update("Network", "Rules", HostModeration.Rules ?? string.Empty, "Rules shown to every player who joins.");
			settings.Update("Network", "DisableBuild", WorldProtection.DisableBuild ? "True" : "False", "While set, only the host can change the world.");
			settings.Update("Network", "ProtectSpawnRadius", WorldProtection.SpawnRadius.ToString(), "Tiles around spawn that remote players cannot modify (0 = off).");
			settings.Close();
			lock (Sync)
			{
				_active = true;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	/// <summary>
	/// Saves one hosting value: to the profile when it is in use, otherwise to Settings.ini.
	/// Hiding this choice here is what stops the menu from writing to a file that is being
	/// overridden - which would look exactly like "my change did not save".
	/// </summary>
	public static void Persist(string key, string value, string comment)
	{
		try
		{
			string text = IsActive ? FilePath : Terraria.Storage.AppPaths.SettingsFile;
			Settings settings = new Settings(text);
			settings.Update("Network", key, value, comment);
			settings.Close();
		}
		catch (Exception ex)
		{
			Program.LogWarn("ServerConfig", "could not save '" + key + "': " + ex.Message);
		}
	}

	/// <summary>One-line status for <c>!serverconfig</c> and the log.</summary>
	public static string Describe()
	{
		return IsActive
			? Texts.Format("chat.host.serverconfig.using.profile", FileName)
			: Texts.Format("chat.host.serverconfig.using.settings", FileName);
	}

	private static bool Has(Settings settings, string key)
	{
		// Settings.Get returns "" for a missing key, and an empty value is meaningful here
		// (empty password), so presence is checked by the comment/key scan instead.
		try
		{
			return settings.HasKey("Network", key);
		}
		catch
		{
			return !string.IsNullOrEmpty(settings.Get("Network", key));
		}
	}

	private static int ReadInt(Settings settings, string key, int fallback, int min, int max)
	{
		int result;
		if (!int.TryParse(settings.Get("Network", key), out result) || result < min || result > max)
		{
			if (Has(settings, key))
			{
				Program.LogWarn("ServerConfig", "'" + key + "' must be " + min + "-" + max + ", keeping " + fallback);
			}
			return fallback;
		}
		return result;
	}
}
