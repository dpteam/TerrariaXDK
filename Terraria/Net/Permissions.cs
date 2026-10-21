using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terraria.Net;

/// <summary>
/// Groups and the players in them, stored in <c>Storage/Config/permissions.ini</c>.
///
/// <code>
/// [groups]
/// mod   = inspect, kick, mute
/// admin = *
///
/// [users]
/// Alice = admin
/// Bob   = mod
/// </code>
///
/// Rules that matter, and why:
/// <list type="bullet">
/// <item>The host is always the owner, from code, never from the file. A typo in the file
/// must not be able to lock the host out of their own server.</item>
/// <item>Unknown permission names are reported and ignored, not treated as "deny
/// everything": a typo in one entry should not silently strip an operator.</item>
/// <item>A remote player is never granted host powers, even if they joined using the host's
/// name. There is no account system here, so identity is self-asserted; trusting a name for
/// ownership would make every server trivially takeable.</item>
/// </list>
/// </summary>
internal static class Permissions
{
	public const string FileName = "permissions.ini";

	/// <summary>Players by name (case-insensitive) and the group they belong to.</summary>
	private static readonly Dictionary<string, string> Users = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	/// <summary>Groups and their permissions.</summary>
	private static readonly Dictionary<string, Permission> Groups = new Dictionary<string, Permission>(StringComparer.OrdinalIgnoreCase);

	private static readonly List<string> Problems = new List<string>();

	private static readonly object Sync = new object();

	private static bool _loaded;

	public static string FilePath => Path.Combine(Terraria.Storage.AppPaths.Config, FileName);

	/// <summary>Lines the file could not understand. Shown once in the log and by !groups.</summary>
	public static IList<string> LoadProblems
	{
		get
		{
			Load();
			lock (Sync)
			{
				return new List<string>(Problems);
			}
		}
	}

	/// <summary>
	/// What a player with no group may do. Building is normal play, so it is included -
	/// otherwise every fresh server would refuse to let anyone place a block. Servers that
	/// want to restrict it use <see cref="WorldProtection.DisableBuild"/> or put the player in
	/// a group that grants <see cref="Permission.Play"/> only.
	/// </summary>
	internal const Permission ImplicitPlayerMask = Permission.Play | Permission.Build;

	/// <summary>
	/// True when this player may do the thing. <paramref name="isLocalHost"/> is the only way
	/// to be the owner: the transport knows whether a command arrived from the host's own
	/// keyboard or from a socket, and that distinction cannot be faked by picking a name.
	/// </summary>
	internal static bool Allows(string playerName, Permission permission, bool isLocalHost)
	{
		if (isLocalHost)
		{
			return true;
		}
		if (string.IsNullOrWhiteSpace(playerName))
		{
			return false;
		}
		Load();
		lock (Sync)
		{
			string text;
			if (!Users.TryGetValue(playerName.Trim(), out text))
			{
				// Nobody has granted this player anything: they play, they build, they moderate
				// nothing.
				return (ImplicitPlayerMask & permission) == permission;
			}
			Permission permission2;
			if (!Groups.TryGetValue(text, out permission2))
			{
				return false;
			}
			return (permission2 & permission) == permission;
		}
	}

	/// <summary>The group this player is in, or null.</summary>
	public static string GroupOf(string playerName)
	{
		if (string.IsNullOrWhiteSpace(playerName))
		{
			return null;
		}
		Load();
		lock (Sync)
		{
			string result;
			return Users.TryGetValue(playerName.Trim(), out result) ? result : null;
		}
	}

	/// <summary>Permissions granted to a group, or None when the group does not exist.</summary>
	public static Permission PermissionsOf(string group)
	{
		if (string.IsNullOrWhiteSpace(group))
		{
			return Permission.None;
		}
		Load();
		lock (Sync)
		{
			Permission result;
			return Groups.TryGetValue(group.Trim(), out result) ? result : Permission.None;
		}
	}

	/// <summary>Group names, sorted. Host-only groups included, since only a manager sees this.</summary>
	public static List<string> GroupNames()
	{
		Load();
		lock (Sync)
		{
			List<string> list = new List<string>(Groups.Keys);
			list.Sort(StringComparer.OrdinalIgnoreCase);
			return list;
		}
	}

	/// <summary>Players and their groups, sorted. Empty when nobody was granted anything.</summary>
	public static List<KeyValuePair<string, string>> UserList()
	{
		Load();
		lock (Sync)
		{
			List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>(Users.Count);
			foreach (KeyValuePair<string, string> user in Users)
			{
				list.Add(user);
			}
			list.Sort(delegate(KeyValuePair<string, string> a, KeyValuePair<string, string> b)
			{
				return string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase);
			});
			return list;
		}
	}

	/// <summary>
	/// Puts a player in a group. Returns false when the group does not exist, so <c>!op</c>
	/// cannot create a typo-shaped group that grants nothing.
	/// </summary>
	public static bool Assign(string playerName, string group, out string error)
	{
		error = null;
		if (string.IsNullOrWhiteSpace(playerName))
		{
			error = "A player name is required.";
			return false;
		}
		Load();
		lock (Sync)
		{
			if (!Groups.ContainsKey(group ?? string.Empty))
			{
				error = "No group called '" + group + "'. Known: " + string.Join(", ", GroupNames().ToArray());
				return false;
			}
			Users[playerName.Trim()] = group;
		}
		Save();
		return true;
	}

	/// <summary>Removes a player from every group. False when they were not in one.</summary>
	public static bool Revoke(string playerName)
	{
		if (string.IsNullOrWhiteSpace(playerName))
		{
			return false;
		}
		Load();
		bool flag;
		lock (Sync)
		{
			flag = Users.Remove(playerName.Trim());
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
			Users.Clear();
			Groups.Clear();
			Problems.Clear();
		}
	}

	/// <summary>
	/// Parses a permission list: <c>*</c>, or comma/space separated names. Unknown names are
	/// collected into <paramref name="unknown"/> instead of silently denying everything.
	/// </summary>
	internal static Permission ParsePermissions(string text, List<string> unknown)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return Permission.None;
		}
		string text2 = text.Trim();
		if (text2 == "*" || text2.Equals("all", StringComparison.OrdinalIgnoreCase))
		{
			return Permission.All;
		}
		Permission permission = Permission.None;
		string[] array = text2.Split(new char[3] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string text3 = array[i].Trim();
			if (text3.Length == 0)
			{
				continue;
			}
			Permission result;
			if (TryParsePermission(text3, out result))
			{
				permission |= result;
			}
			else if (unknown != null)
			{
				unknown.Add(text3);
			}
		}
		return permission;
	}

	internal static bool TryParsePermission(string name, out Permission permission)
	{
		permission = Permission.None;
		if (string.IsNullOrWhiteSpace(name))
		{
			return false;
		}
		// Enum.TryParse would accept "1" and "Kick, Ban"; match names explicitly instead so a
		// number in the file is a typo that gets reported, not a silent permission.
		switch (name.Trim().ToLowerInvariant())
		{
		case "inspect":
		case "see":
			permission = Permission.Inspect;
			return true;
		case "kick":
			permission = Permission.Kick;
			return true;
		case "ban":
			permission = Permission.Ban;
			return true;
		case "mute":
			permission = Permission.Mute;
			return true;
		case "whisper":
			permission = Permission.Whisper;
			return true;
		case "servertext":
		case "say":
		case "motd":
			permission = Permission.ServerText;
			return true;
		case "manage":
		case "op":
			permission = Permission.Manage;
			return true;
		case "build":
			permission = Permission.Build;
			return true;
		case "play":
			permission = Permission.Play;
			return true;
		default:
			return false;
		}
	}

	/// <summary>Human-readable permission list, for <c>!groups</c>.</summary>
	internal static string Describe(Permission permission)
	{
		if (permission == Permission.None)
		{
			return "nothing";
		}
		if (permission == Permission.All)
		{
			return "*";
		}
		List<string> list = new List<string>();
		Permission[] all = Defaults.AllPermissions;
		for (int i = 0; i < all.Length; i++)
		{
			if ((permission & all[i]) == all[i])
			{
				list.Add(all[i].ToString().ToLowerInvariant());
			}
		}
		return string.Join(", ", list.ToArray());
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
			Users.Clear();
			Groups.Clear();
			Problems.Clear();
			// Built-in groups: usable right after install, before anyone edits a file.
			Groups["admin"] = Permission.All;
			// A moderator moderates; they still play and build like anyone else.
			Groups["mod"] = Permission.Play | Permission.Build | Permission.Inspect | Permission.Kick | Permission.Mute;
			Groups["player"] = Permission.Play | Permission.Build;
			try
			{
				if (!File.Exists(FilePath))
				{
					return;
				}
				string section = string.Empty;
				string[] array = File.ReadAllLines(FilePath);
				for (int i = 0; i < array.Length; i++)
				{
					string text = array[i].Trim();
					if (text.Length == 0 || text.StartsWith("#") || text.StartsWith(";"))
					{
						continue;
					}
					if (text.StartsWith("[") && text.EndsWith("]"))
					{
						section = text.Substring(1, text.Length - 2).Trim().ToLowerInvariant();
						continue;
					}
					int num = text.IndexOf('=');
					if (num <= 0)
					{
						Problems.Add("line " + (i + 1) + ": expected 'name = value'");
						continue;
					}
					string text2 = text.Substring(0, num).Trim();
					string text3 = text.Substring(num + 1).Trim();
					if (text2.Length == 0)
					{
						Problems.Add("line " + (i + 1) + ": empty name");
						continue;
					}
					if (section == "groups")
					{
						List<string> unknown = new List<string>();
						Groups[text2] = ParsePermissions(text3, unknown);
						for (int j = 0; j < unknown.Count; j++)
						{
							Problems.Add("group '" + text2 + "': unknown permission '" + unknown[j] + "'");
						}
					}
					else if (section == "users")
					{
						// Keep the assignment even when the group does not exist yet: the host may
						// declare players before groups, and a missing group grants nothing anyway.
						Users[text2] = text3;
						if (!Groups.ContainsKey(text3))
						{
							Problems.Add("user '" + text2 + "': unknown group '" + text3 + "'");
						}
					}
					else
					{
						Problems.Add("line " + (i + 1) + ": outside of [groups] or [users]");
					}
				}
			}
			catch (Exception ex)
			{
				Problems.Add("could not read the file: " + ex.Message);
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
			stringBuilder.AppendLine("# Server permissions. The host always has every permission.");
			stringBuilder.AppendLine("# [groups] section: group = comma separated permissions, or *");
			stringBuilder.AppendLine("# [users]  section: player = group");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("[groups]");
			lock (Sync)
			{
				List<string> list = new List<string>(Groups.Keys);
				list.Sort(StringComparer.OrdinalIgnoreCase);
				for (int i = 0; i < list.Count; i++)
				{
					stringBuilder.AppendLine(list[i] + " = " + DescribePermissions(Groups[list[i]]));
				}
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("[users]");
				List<string> list2 = new List<string>(Users.Keys);
				list2.Sort(StringComparer.OrdinalIgnoreCase);
				for (int j = 0; j < list2.Count; j++)
				{
					stringBuilder.AppendLine(list2[j] + " = " + Users[list2[j]]);
				}
			}
			File.WriteAllText(FilePath, stringBuilder.ToString(), Encoding.UTF8);
		}
		catch (Exception ex)
		{
			Program.LogWarn("Permissions", "could not save the permissions: " + ex.Message);
		}
	}

	/// <summary>Machine-readable permission list, the inverse of <see cref="ParsePermissions"/>.</summary>
	private static string DescribePermissions(Permission permission)
	{
		if (permission == Permission.All)
		{
			return "*";
		}
		List<string> list = new List<string>();
		Permission[] all = Defaults.AllPermissions;
		for (int i = 0; i < all.Length; i++)
		{
			if ((permission & all[i]) == all[i])
			{
				list.Add(all[i].ToString().ToLowerInvariant());
			}
		}
		return (list.Count == 0) ? "none" : string.Join(", ", list.ToArray());
	}

	/// <summary>Single-value permissions, in the order they are printed.</summary>
	private static class Defaults
	{
		public static readonly Permission[] AllPermissions = new Permission[9]
		{
			Permission.Inspect,
			Permission.Kick,
			Permission.Ban,
			Permission.Mute,
			Permission.Whisper,
			Permission.ServerText,
			Permission.Manage,
			Permission.Build,
			Permission.Play
		};
	}
}
