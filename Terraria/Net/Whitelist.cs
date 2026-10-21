using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terraria.Net;

/// <summary>
/// Host allow-list, stored as plain lines in <c>Storage/Config/whitelist.txt</c>.
///
/// One entry per line, either a player name or an IP address; <c>#</c> starts a comment.
/// Matching is whole-string and case-insensitive, the same rule the ban list uses: a substring
/// match would let "Bob" whitelist "Bobby" and, worse, let a name that merely contains an entry
/// in through a check nobody expects to be fuzzy.
///
/// Why it exists: a shared host password answers "should strangers get in", but not "which
/// strangers". Once a password has been handed to a group, it cannot be taken back from one
/// person without changing it for everybody, and a server that is meant for four friends ends up
/// open to whoever the password reaches. A list of names is revocable per person.
///
/// Deliberately different from <see cref="BanList"/> in one way: when the whitelist is on and
/// empty, <b>nobody</b> gets in. That is the honest reading of "only these players", and the
/// alternative - an empty list meaning "everyone" - is the failure mode that looks like the
/// feature is broken. The host is not affected: the list is checked on incoming connections only,
/// and the host's own session never goes through it.
/// </summary>
internal static class Whitelist
{
	private static readonly List<string> Entries = new List<string>();

	private static bool _loaded;

	private static readonly object Sync = new object();

	/// <summary>Logged once when an enabled-but-empty list turns players away.</summary>
	private static bool _warnedEmpty;

	public static string FilePath => Path.Combine(Terraria.Storage.AppPaths.Config, "whitelist.txt");

	/// <summary>
	/// True when only listed players may join (<c>[Network] Whitelist</c>). Off by default: an
	/// allow-list is a server policy, and a server that suddenly rejects everybody because a file
	/// was edited is worse than one that accepts a stranger.
	/// </summary>
	public static bool Enabled;

	/// <summary>Number of entries. Used by the command output and the self-test.</summary>
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
	/// True when this player may join. An empty name and address is not a match; a disabled list
	/// lets everyone in, because that is what "off" means.
	/// </summary>
	public static bool IsAllowed(string gamertag, string address)
	{
		if (!Enabled)
		{
			return true;
		}
		if (string.IsNullOrWhiteSpace(gamertag) && string.IsNullOrWhiteSpace(address))
		{
			return false;
		}
		Load();
		lock (Sync)
		{
			if (Entries.Count == 0)
			{
				if (!_warnedEmpty)
				{
					_warnedEmpty = true;
					Program.LogWarn("Whitelist", "the whitelist is enabled but " + FilePath
						+ " has no entries, so every player is refused");
				}
				return false;
			}
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
			_warnedEmpty = false;
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
				// A file that cannot be read must not silently become "nobody may join" without a
				// word: the list is empty, and the warning above explains the consequence.
				Program.LogWarn("Whitelist", "could not read " + FilePath + ": " + ex.Message);
			}
		}
	}

	private static void Save()
	{
		try
		{
			List<string> list;
			lock (Sync)
			{
				list = new List<string>(Entries);
			}
			StringBuilder stringBuilder = new StringBuilder(128 + list.Count * 24);
			stringBuilder.AppendLine("# Players allowed to join while [Network] Whitelist=True.");
			stringBuilder.AppendLine("# One entry per line: a player name or an IP address. '#' starts a comment.");
			stringBuilder.AppendLine("# An empty list means nobody can join - that is what a whitelist is.");
			for (int i = 0; i < list.Count; i++)
			{
				stringBuilder.AppendLine(list[i]);
			}
			Directory.CreateDirectory(Terraria.Storage.AppPaths.Config);
			File.WriteAllText(FilePath, stringBuilder.ToString(), Encoding.UTF8);
		}
		catch (Exception ex)
		{
			Program.LogWarn("Whitelist", "could not write " + FilePath + ": " + ex.Message);
		}
	}
}
