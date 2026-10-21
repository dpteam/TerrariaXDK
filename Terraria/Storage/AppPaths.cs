using System;
using System.IO;

namespace Terraria.Storage;

/// <summary>
/// Single source of truth for every mutable path. Everything the game writes lives under
/// <c>Storage/</c> next to the executable, so an install is fully portable: copy the folder,
/// keep your worlds, characters, mods, logs and config.
///
/// Layout:
/// <code>
/// TerrariaXDK.exe
/// Content/                  read-only game assets
/// Storage/
///   Saves/Characters/PlayerN/playerN.plr
///   Saves/Worlds/PlayerN/worldN.wld
///   Saves/Settings/PlayerN/config.dat
///   Mods/&lt;Mod&gt;/&lt;Mod&gt;.dll
///   Logs/terraria-debug.log
///   Logs/client-crashlog.txt
///   Backups/
///   Config/Settings.ini
/// </code>
/// </summary>
internal static class AppPaths
{
	public const string RootFolderName = "Storage";

	/// <summary>Directory the executable lives in.</summary>
	public static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;

	public static string Root => Path.Combine(BaseDirectory, RootFolderName);

	/// <summary>Passed to the XNA storage device as its root, so containers nest under here.</summary>
	public static string Saves => Path.Combine(Root, "Saves");

	public static string Logs => Path.Combine(Root, "Logs");

	public static string Mods => Path.Combine(Root, "Mods");

	public static string Backups => Path.Combine(Root, "Backups");

	public static string Config => Path.Combine(Root, "Config");

	public static string Cache => Path.Combine(Root, "Cache");

	/// <summary>
	/// Translation overrides. Files here are read on top of the built-in strings, so a
	/// translation can be corrected or a new language added without rebuilding the game.
	/// </summary>
	public static string Lang => Path.Combine(Root, "Lang");

	/// <summary>
	/// Loose PNG files that stand in for compiled textures, mirroring the content tree:
	/// <c>Textures/Images/_sheetTiles.png</c> replaces <c>Images/_sheetTiles.xnb</c>. Kept out of
	/// <c>Content/</c> because Content is read-only for the game and this folder is meant to be
	/// edited by hand.
	/// </summary>
	public static string Textures => Path.Combine(Root, "Textures");

	public static string LogFile => Path.Combine(Logs, "terraria-debug.log");

	public static string CrashLogFile => Path.Combine(Logs, "client-crashlog.txt");

	/// <summary>
	/// Settings always live in Storage/Config. A Settings.ini shipped next to the
	/// executable acts as a seed template: it is copied into Storage/Config on first run,
	/// so defaults travel with the install without making the file read-only.
	/// </summary>
	public static string SettingsFile => Path.Combine(Config, "Settings.ini");

	/// <summary>
	/// Set by the headless self-test before <see cref="EnsureDirectories"/>. It stops this class
	/// from touching any XNA type: <c>StorageDevice.RootOverride</c> initialises FNAPlatform,
	/// which loads SDL2, and the self-test needs neither. Without this a machine without the FNA
	/// native libraries - CI, or a bare checkout - could not run the suite at all.
	/// </summary>
	internal static bool Headless;

	/// <summary>
	/// Optional hosting profile. When this file exists it wins over <see cref="SettingsFile"/>
	/// for every key it defines, so a world can be hosted with its own policy without
	/// touching the player's client settings.
	/// </summary>
	public static string ServerConfigFile => Path.Combine(Config, "ServerConfig.ini");

	/// <summary>Creates the directory tree. Safe to call more than once.</summary>
	public static void EnsureDirectories()
	{
		string[] array = new string[9]
		{
			Root, Saves, Logs, Mods, Backups, Config, Cache, Lang, Textures
		};
		foreach (string path in array)
		{
			try
			{
				if (!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}
			}
			catch
			{
				// A read-only install should not prevent the game from starting;
				// the failing component reports the real error when it writes.
			}
		}
		SeedSettings();
		// Redirect the XNA storage device so containers resolve under Storage/Saves.
		//
		// Skipped in headless mode: the assignment initialises FNAPlatform, which loads SDL2,
		// and the self-test has no use for either. The game itself still reports a missing DLL
		// loudly when it tries to create a window.
		if (Headless)
		{
			return;
		}
		try
		{
			Microsoft.Xna.Framework.Storage.StorageDevice.RootOverride = Saves;
		}
		catch (Exception ex)
		{
			Program.LogWarn("Storage", "could not redirect the XNA storage device (" + ex.GetType().Name + "); "
				+ "saves will not resolve unless the FNA native libraries are present");
		}
	}

	/// <summary>Copies the shipped Settings.ini template into Storage/Config once.</summary>
	private static void SeedSettings()
	{
		try
		{
			string text = Path.Combine(BaseDirectory, "Settings.ini");
			if (!File.Exists(SettingsFile) && File.Exists(text))
			{
				File.Copy(text, SettingsFile);
			}
		}
		catch
		{
		}
	}
}
