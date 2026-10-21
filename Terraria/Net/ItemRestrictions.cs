using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terraria.Net;

/// <summary>
/// Items a server refuses: the <c>additem</c>/<c>delitem</c> pair from the TShock feature list.
///
/// Enforcement point: the host rejects an inventory sync (message 5) that carries a banned
/// item, so the item never exists on the authoritative side and never reaches the other
/// players. That is the only place the server can act - item use happens on the client, and
/// trying to police it there would be both ineffective and unverifiable.
///
/// Entries are numeric <c>NetID</c>s. Names are accepted in the command for convenience but
/// are resolved through <see cref="TryParseEntry"/>, which only understands what the game can
/// actually look up: the game has no name-to-id table (item names live in a switch inside
/// <c>Item.SetDefaults</c>), so a typo in a name must fail loudly instead of silently banning
/// nothing. Numeric ids work for every item, so the feature is never blocked on that.
/// </summary>
internal static class ItemRestrictions
{
	private static readonly HashSet<int> Banned = new HashSet<int>();

	private static readonly object Sync = new object();

	private static bool _loaded;

	/// <summary>
	/// Message shown to a player whose inventory was refused. Resolved on read so it follows
	/// the chosen language instead of freezing the text at startup.
	/// </summary>
	public static string Notice => Texts.Get("chat.mod.item.blocked");

	public static string FilePath => Path.Combine(Terraria.Storage.AppPaths.Config, "items.txt");

	public static int Count
	{
		get
		{
			Load();
			lock (Sync)
			{
				return Banned.Count;
			}
		}
	}

	/// <summary>Banned ids, sorted, for display.</summary>
	public static List<int> All
	{
		get
		{
			Load();
			lock (Sync)
			{
				List<int> list = new List<int>(Banned);
				list.Sort();
				return list;
			}
		}
	}

	public static bool IsBanned(int netId)
	{
		if (netId <= 0)
		{
			return false;
		}
		Load();
		lock (Sync)
		{
			return Banned.Contains(netId);
		}
	}

	/// <summary>Bans an id. False when it is not a plausible item id or already banned.</summary>
	public static bool Add(int netId)
	{
		if (netId <= 0 || netId > 32767)
		{
			return false;
		}
		Load();
		lock (Sync)
		{
			if (!Banned.Add(netId))
			{
				return false;
			}
		}
		Save();
		return true;
	}

	public static bool Remove(int netId)
	{
		Load();
		bool flag;
		lock (Sync)
		{
			flag = Banned.Remove(netId);
		}
		if (flag)
		{
			Save();
		}
		return flag;
	}

	/// <summary>
	/// Parses a command argument: a number, or <c>name</c> when the name can be resolved.
	/// Names are not resolvable in this port, so the error explains that instead of pretending.
	/// </summary>
	internal static bool TryParseEntry(string text, out int netId, out string error)
	{
		netId = 0;
		error = null;
		if (string.IsNullOrWhiteSpace(text))
		{
			error = Texts.Get("chat.host.item.need.id");
			return false;
		}
		if (int.TryParse(text.Trim(), out netId))
		{
			if (netId <= 0 || netId > 32767)
			{
				error = Texts.Get("chat.host.item.id.range");
				netId = 0;
				return false;
			}
			return true;
		}
		error = Texts.Format("chat.host.item.id.nan", text.Trim());
		netId = 0;
		return false;
	}

	public static void Reload()
	{
		lock (Sync)
		{
			_loaded = false;
			Banned.Clear();
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
			Banned.Clear();
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
					if (text.Length == 0 || text.StartsWith("#") || text.StartsWith(";"))
					{
						continue;
					}
					int num;
					if (int.TryParse(text, out num) && num > 0 && num <= 32767)
					{
						Banned.Add(num);
					}
					else
					{
						Program.LogWarn("Items", "line " + (i + 1) + ": '" + text + "' is not an item id, ignored");
					}
				}
			}
			catch (Exception ex)
			{
				Program.LogWarn("Items", "could not read the banned item list: " + ex.Message);
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
			stringBuilder.AppendLine("# Item ids this server refuses, one per line. !additem <id> / !delitem <id>.");
			lock (Sync)
			{
				List<int> list = new List<int>(Banned);
				list.Sort();
				for (int i = 0; i < list.Count; i++)
				{
					stringBuilder.AppendLine(list[i].ToString());
				}
			}
			File.WriteAllText(FilePath, stringBuilder.ToString(), Encoding.UTF8);
		}
		catch (Exception ex)
		{
			Program.LogWarn("Items", "could not save the banned item list: " + ex.Message);
		}
	}
}
