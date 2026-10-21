using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terraria.Net;

/// <summary>
/// Host ban list, stored as plain lines in <c>Storage/Config/banlist.txt</c>.
///
/// One entry per line, either a player name or an IP address; <c>#</c> starts a comment.
/// Matching is case-insensitive and compares whole entries only: a substring match would
/// let "Bob" ban "Bobby", and nobody expects that.
///
/// The list is checked during the handshake, before the peer joins, so a banned player never
/// sees the roster or world data - rejecting after the join would leak both.
/// </summary>
internal static class BanList
{
	private static readonly List<string> Entries = new List<string>();

	private static bool _loaded;

	private static readonly object Sync = new object();

	public static string FilePath => Path.Combine(Terraria.Storage.AppPaths.Config, "banlist.txt");

	/// <summary>Number of entries. Used by the UI and the self-test.</summary>
	public static int Count
	{
		get
		{
			Load();
			lock (Sync)
			{
				return Entries.Count;
			}
		}
	}

	/// <summary>A copy of the entries, for display. Never a live view: callers are on other threads.</summary>
	public static List<string> All
	{
		get
		{
			Load();
			lock (Sync)
			{
				return new List<string>(Entries);
			}
		}
	}

	/// <summary>
	/// True when this player or address is banned. A null name or address is simply not a
	/// match; an empty one never bans everyone.
	/// </summary>
	public static bool IsBanned(string gamertag, string address)
	{
		if (string.IsNullOrWhiteSpace(gamertag) && string.IsNullOrWhiteSpace(address))
		{
			return false;
		}
		Load();
		lock (Sync)
		{
			for (int i = 0; i < Entries.Count; i++)
			{
				if (Matches(Entries[i], gamertag) || Matches(Entries[i], address))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool Matches(string entry, string candidate)
	{
		if (string.IsNullOrEmpty(entry) || string.IsNullOrEmpty(candidate))
		{
			return false;
		}
		return string.Equals(entry, candidate.Trim(), StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>Adds an entry. Returns false when it is blank or already there.</summary>
	public static bool Add(string entry)
	{
		string text = (entry ?? string.Empty).Trim();
		if (text.Length == 0)
		{
			return false;
		}
		Load();
		lock (Sync)
		{
			for (int i = 0; i < Entries.Count; i++)
			{
				if (string.Equals(Entries[i], text, StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}
			Entries.Add(text);
		}
		Save();
		return true;
	}

	public static bool Remove(string entry)
	{
		string text = (entry ?? string.Empty).Trim();
		if (text.Length == 0)
		{
			return false;
		}
		Load();
		bool flag = false;
		lock (Sync)
		{
			for (int i = Entries.Count - 1; i >= 0; i--)
			{
				if (string.Equals(Entries[i], text, StringComparison.OrdinalIgnoreCase))
				{
					Entries.RemoveAt(i);
					flag = true;
				}
			}
		}
		if (flag)
		{
			Save();
		}
		return flag;
	}

	/// <summary>Drops the cache so the next query re-reads the file (hand edits, tests).</summary>
	public static void Reload()
	{
		lock (Sync)
		{
			_loaded = false;
			Entries.Clear();
		}
	}

	private static void Load()
	{
		lock (Sync)
		{
			if (_loaded)
			{
				return;
			}
			_loaded = true;
			Entries.Clear();
			try
			{
				if (!File.Exists(FilePath))
				{
					return;
				}
				string[] array = File.ReadAllLines(FilePath);
				for (int i = 0; i < array.Length; i++)
				{
					string text = array[i].Trim();
					if (text.Length != 0 && !text.StartsWith("#") && !Entries.Contains(text))
					{
						Entries.Add(text);
					}
				}
			}
			catch (Exception ex)
			{
				Program.LogWarn("Bans", "could not read the ban list: " + ex.Message);
			}
		}
	}

	private static void Save()
	{
		try
		{
			string text = Path.GetDirectoryName(FilePath);
			if (!string.IsNullOrEmpty(text) && !Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("# Banned players and addresses, one per line. Use !unban in the host console to remove.");
			lock (Sync)
			{
				for (int i = 0; i < Entries.Count; i++)
				{
					stringBuilder.AppendLine(Entries[i]);
				}
			}
			File.WriteAllText(FilePath, stringBuilder.ToString(), Encoding.UTF8);
		}
		catch (Exception ex)
		{
			Program.LogWarn("Bans", "could not save the ban list: " + ex.Message);
		}
	}
}
