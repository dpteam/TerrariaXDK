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
		settings.Update("Backups", "Enabled", Enabled ? "True" : "False", "Keep dated copies of the world in Storage/Backups.");
		settings.Update("Backups", "Keep", Keep.ToString(), "How many backups to keep per world (0-100).\n; Oldest are removed first; 0 disables automatic backups.");
		settings.Update("Backups", "MinMinutes", MinMinutes.ToString(), "Minutes between automatic backups of the same world (0 = every save).");
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
			List<string> list = ListFor(worldName);
			if (list.Count == 0)
			{
				return true;
			}
			DateTime dateTime = DateTime.MinValue;
			for (int i = 0; i < list.Count; i++)
			{
				DateTime lastWriteTime = File.GetLastWriteTime(list[i]);
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

	/// <summary>
	/// Deletes the oldest backups for a world until only <see cref="Keep"/> remain.
	/// Uses <see cref="ListFor"/> so the matching rules are identical to the ones the UI and
	/// the restore path use - including names that contain wildcard characters.
	/// </summary>
	private static void Prune(string worldName)
	{
		List<string> list = ListFor(worldName);
		if (list.Count <= Keep)
		{
			return;
		}
		// Newest first, so everything past Keep is the oldest.
		for (int i = Keep; i < list.Count; i++)
		{
			try
			{
				File.Delete(list[i]);
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

	/// <summary>
	/// Backups belonging to one world, newest first. Backups are named
	/// <c>&lt;world&gt;_&lt;timestamp&gt;.wld</c>, so a prefix match is enough - and it must
	/// be the sanitised name, the same transformation the writer applied.
	///
	/// The match is done in code rather than with a wildcard pattern: <c>Directory.GetFiles</c>
	/// treats <c>[</c> and <c>]</c> as pattern syntax, so a world called "My[World]" would
	/// silently find nothing, and its backups could never be restored.
	/// </summary>
	public static List<string> ListFor(string worldName)
	{
		List<string> list = new List<string>();
		if (string.IsNullOrEmpty(worldName))
		{
			return list;
		}
		try
		{
			if (!Directory.Exists(Folder))
			{
				return list;
			}
			string prefix = Sanitize(worldName) + "_";
			string[] files = Directory.GetFiles(Folder, "*.wld");
			List<string> list2 = new List<string>();
			for (int i = 0; i < files.Length; i++)
			{
				if (Path.GetFileName(files[i]).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				{
					list2.Add(files[i]);
				}
			}
			list2.Sort(StringComparer.OrdinalIgnoreCase);
			for (int j = list2.Count - 1; j >= 0; j--)
			{
				list.Add(list2[j]);
			}
		}
		catch (Exception ex)
		{
			Program.LogWarn("Backups", "could not list backups: " + ex.Message);
		}
		return list;
	}

	/// <summary>
	/// Copies a backup over the live save file. The save being replaced is first kept as
	/// <c>&lt;file&gt;.prerestore</c>, so restoring the wrong backup is itself undoable.
	///
	/// Callers must make sure no world is loaded: writing over a save that the game will later
	/// overwrite again simply loses the restore.
	/// </summary>
	public static bool Restore(string backupFile, string worldFile, out string error)
	{
		error = null;
		try
		{
			if (string.IsNullOrEmpty(backupFile) || !File.Exists(backupFile))
			{
				error = "That backup no longer exists.";
				return false;
			}
			if (string.IsNullOrEmpty(worldFile))
			{
				error = "No world file to restore into.";
				return false;
			}
			// A backup that is not a world file would silently destroy the save; the header is
			// the cheapest check that it is at least shaped like one.
			if (!LooksLikeWorld(backupFile))
			{
				error = "That file does not look like a world save.";
				return false;
			}
			if (File.Exists(worldFile))
			{
				File.Copy(worldFile, worldFile + ".prerestore", overwrite: true);
			}
			File.Copy(backupFile, worldFile, overwrite: true);
			Program.LogInfo("Backups", "restored " + Path.GetFileName(backupFile) + " over " + Path.GetFileName(worldFile));
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			Program.LogWarn("Backups", "could not restore a backup: " + ex.Message);
			return false;
		}
	}

	/// <summary>
	/// The first bytes of a world save are a header (version number then metadata). An empty
	/// or tiny file is rejected rather than copied over a real world.
	/// </summary>
	internal static bool LooksLikeWorld(string path)
	{
		try
		{
			FileInfo fileInfo = new FileInfo(path);
			// Smallest plausible save is a few hundred bytes of header; 64 is generous.
			return fileInfo.Exists && fileInfo.Length >= 64;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Turns a backup file name into something a list row can show:
	/// <c>MyWorld_20260101_120000.wld</c> becomes <c>2026-01-01 12:00</c>.
	/// </summary>
	public static string Describe(string backupFile)
	{
		string fileName = Path.GetFileNameWithoutExtension(backupFile);
		int num = fileName.LastIndexOf('_');
		if (num <= 0)
		{
			return fileName;
		}
		// The name is <world>_<yyyyMMdd>_<HHmmss>; the last two underscores separate the date.
		int num2 = fileName.LastIndexOf('_', num - 1);
		if (num2 <= 0 || num - num2 != 9 || fileName.Length - num != 7)
		{
			return fileName;
		}
		string text = fileName.Substring(num2 + 1, 8);
		string text2 = fileName.Substring(num + 1, 6);
		if (text.Length != 8 || text2.Length != 6)
		{
			return fileName;
		}
		return text.Substring(0, 4) + "-" + text.Substring(4, 2) + "-" + text.Substring(6, 2) + " " + text2.Substring(0, 2) + ":" + text2.Substring(2, 2);
	}

	/// <summary>Strips characters that are illegal in file names.</summary>
	internal static string Sanitize(string name)
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
