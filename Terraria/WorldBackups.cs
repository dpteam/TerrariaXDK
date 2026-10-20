using System;
using System.Collections.Generic;
using System.IO;

namespace Terraria;

/// <summary>
/// Rolling world backups in <c>Storage/Backups</c>.
///
/// The game already keeps a single <c>.wld.bak</c> next to the world, which is overwritten on
/// every save - one bad save destroys the only copy. This keeps a dated history instead, with
/// a configurable limit so the folder cannot grow without bound.
/// </summary>
internal static class WorldBackups
{
	/// <summary>Settings.ini: [Backups] Keep. Zero disables automatic backups.</summary>
	public static int Keep = 10;

	/// <summary>Settings.ini: [Backups] Enabled.</summary>
	public static bool Enabled = true;

	/// <summary>
	/// Minimum gap between two backups of the same world, in minutes. Autosave runs every
	/// few minutes; without this the backup folder would mirror every autosave. Zero means
	/// "back up on every save".
	/// </summary>
	public static int MinMinutes = 15;

	public static string Folder => Terraria.Storage.AppPaths.Backups;

	public static void Load(Settings settings)
	{
		Enabled = settings.Get("Backups", "Enabled").ToLower().Equals("true");
		int result;
		if (int.TryParse(settings.Get("Backups", "Keep"), out result) && result >= 0 && result <= 100)
		{
			Keep = result;
		}
		if (int.TryParse(settings.Get("Backups", "MinMinutes"), out result) && result >= 0 && result <= 1440)
		{
			MinMinutes = result;
		}
	}

	public static void Save(Settings settings)
	{
		settings.Set("Backups", "Enabled", Enabled ? "True" : "False", "Keep dated copies of the world in Storage/Backups.");
		settings.Set("Backups", "Keep", Keep.ToString(), "How many backups to keep per world (0-100).\n; Oldest are removed first; 0 disables automatic backups.");
		settings.Set("Backups", "MinMinutes", MinMinutes.ToString(), "Minutes between automatic backups of the same world (0 = every save).");
	}

	/// <summary>
	/// Copies a saved world into the backup folder and prunes old copies. Best effort: a
	/// failure here must never break saving.
	/// </summary>
	public static void Backup(string worldFile, string worldName)
	{
		if (!Enabled || Keep <= 0 || string.IsNullOrEmpty(worldFile))
		{
			return;
		}
		if (!ShouldBackup(worldName))
		{
			return;
		}
		BackupNow(worldFile, worldName);
	}

	/// <summary>
	/// True when the newest backup of this world is older than <see cref="MinMinutes"/>.
	/// Works off file timestamps rather than names so a hand-renamed file still counts.
	/// </summary>
	private static bool ShouldBackup(string worldName)
	{
		if (MinMinutes <= 0 || string.IsNullOrEmpty(worldName))
		{
			return true;
		}
		try
		{
			string text = Sanitize(worldName) + "_*.wld";
			string[] files = Directory.GetFiles(Folder, text);
			if (files.Length == 0)
			{
				return true;
			}
			DateTime dateTime = DateTime.MinValue;
			for (int i = 0; i < files.Length; i++)
			{
				DateTime lastWriteTime = File.GetLastWriteTime(files[i]);
				if (lastWriteTime > dateTime)
				{
					dateTime = lastWriteTime;
				}
			}
			return (DateTime.Now - dateTime).TotalMinutes >= MinMinutes;
		}
		catch
		{
			return true;
		}
	}

	/// <summary>Copies the world right now, ignoring the interval. Used by tests and by "back up now".</summary>
	public static void BackupNow(string worldFile, string worldName)
	{
		try
		{
			if (!File.Exists(worldFile))
			{
				return;
			}
			string text = Folder;
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string text2 = Sanitize(string.IsNullOrEmpty(worldName) ? Path.GetFileNameWithoutExtension(worldFile) : worldName);
			string text3 = Path.Combine(text, text2 + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wld");
			// Two saves in the same second must not overwrite each other.
			int num = 1;
			while (File.Exists(text3))
			{
				text3 = Path.Combine(text, text2 + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + num + ".wld");
				num++;
			}
			File.Copy(worldFile, text3);
			Program.LogInfo("Backups", "wrote " + Path.GetFileName(text3));
			Prune(text2);
		}
		catch (Exception ex)
		{
			Program.LogWarn("Backups", "could not back up the world: " + ex.Message);
		}
	}

	/// <summary>Deletes the oldest backups for a world until only <see cref="Keep"/> remain.</summary>
	private static void Prune(string worldName)
	{
		string[] files;
		try
		{
			files = Directory.GetFiles(Folder, worldName + "_*.wld");
		}
		catch
		{
			return;
		}
		if (files.Length <= Keep)
		{
			return;
		}
		// Names carry a timestamp, so a plain name sort is chronological.
		Array.Sort(files, StringComparer.OrdinalIgnoreCase);
		int num = files.Length - Keep;
		for (int i = 0; i < num; i++)
		{
			try
			{
				File.Delete(files[i]);
			}
			catch
			{
			}
		}
	}

	/// <summary>Existing backups, newest first. Used by the UI and by the self-test.</summary>
	public static List<string> List()
	{
		List<string> list = new List<string>();
		try
		{
			if (!Directory.Exists(Folder))
			{
				return list;
			}
			string[] files = Directory.GetFiles(Folder, "*.wld");
			Array.Sort(files, StringComparer.OrdinalIgnoreCase);
			for (int i = files.Length - 1; i >= 0; i--)
			{
				list.Add(files[i]);
			}
		}
		catch
		{
		}
		return list;
	}

	/// <summary>Strips characters that are illegal in file names.</summary>
	private static string Sanitize(string name)
	{
		char[] array = name.ToCharArray();
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		for (int i = 0; i < array.Length; i++)
		{
			for (int j = 0; j < invalidFileNameChars.Length; j++)
			{
				if (array[i] == invalidFileNameChars[j])
				{
					array[i] = '_';
					break;
				}
			}
		}
		string text = new string(array).Trim();
		if (text.Length == 0)
		{
			return "world";
		}
		return text;
	}
}
