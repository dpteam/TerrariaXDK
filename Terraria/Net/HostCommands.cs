using System;
using System.Collections.Generic;

namespace Terraria.Net;

/// <summary>
/// Host-side console commands, typed into chat with a leading <c>!</c>.
///
/// The cheat console is single-player only by design (<see cref="Terraria.Cheats.CheatState"/>),
/// so a host had no way to remove a griefing player short of restarting the world. These are
/// deliberately a separate, small set that only a host can run, and none of them changes the
/// world: they act on who is connected and who may connect.
///
/// <code>
/// !kick &lt;player&gt; [reason]   disconnect someone, on the record
/// !ban  &lt;player|ip&gt;         disconnect and refuse future connections
/// !unban &lt;player|ip&gt;        lift a ban
/// !bans                   list the ban list
/// !who                    who is connected
/// !say &lt;text&gt;              say something as the server
/// !help                   this list
/// </code>
/// </summary>
internal static class HostCommands
{
	/// <summary>Every command name, for chat completion.</summary>
	public static readonly string[] Names = new string[16]
	{
		"!kick",
		"!ban",
		"!unban",
		"!bans",
		"!who",
		"!say",
		"!mute",
		"!unmute",
		"!whisper",
		"!motd",
		"!rules",
		"!op",
		"!deop",
		"!groups",
		"!region",
		"!help"
	};

	private const string HelpText = "!kick <player> [reason], !ban/!unban <player|ip>, !bans, !who, !mute/!unmute <player>, !whisper <player> <text>, !motd [text], !rules [text], !say <text>, !op <player> [group], !deop <player>, !groups, !region list|add <name> [radius]|remove <name>|group <name> [group], !help";

	/// <summary>
	/// Permission each command needs. Anything not listed here (only <c>!help</c>) is open to
	/// everyone - a help text is not a privilege.
	/// </summary>
	private static Permission RequiredFor(string word)
	{
		switch (word)
		{
		case "!who":
		case "!bans":
		case "!groups":
			return Permission.Inspect;
		case "!region":
			// Listing is harmless; changing the map's rules is not. The subcommand decides,
			// and it is checked below.
			return Permission.Inspect;
		case "!kick":
			return Permission.Kick;
		case "!ban":
		case "!unban":
			return Permission.Ban;
		case "!mute":
		case "!unmute":
			return Permission.Mute;
		case "!whisper":
			return Permission.Whisper;
		case "!motd":
		case "!rules":
		case "!say":
			return Permission.ServerText;
		case "!op":
		case "!deop":
			return Permission.Manage;
		default:
			return Permission.None;
		}
	}

	/// <summary>True when this process is hosting a multiplayer world.</summary>
	public static bool IsAvailable => Main.NetMode == 2 && TcpPeer.CurrentHost != null;

	/// <summary>True when the text starts with a host command we know.</summary>
	public static bool IsCommand(string text)
	{
		if (string.IsNullOrWhiteSpace(text) || text[0] != '!')
		{
			return false;
		}
		string word = FirstWord(text).ToLowerInvariant();
		for (int i = 0; i < Names.Length; i++)
		{
			if (Names[i] == word)
			{
				return true;
			}
		}
		return false;
	}

	private static string FirstWord(string text)
	{
		int num = text.IndexOf(' ');
		return (num < 0) ? text : text.Substring(0, num);
	}

	/// <summary>
	/// Runs a command as a specific player. <paramref name="actor"/> is null for the local
	/// host (who is always the owner); a remote player's name comes from the connection, never
	/// from the message text, so it cannot be spoofed by typing a different name.
	///
	/// Returns false when the text is not a command at all, so the caller can fall back to
	/// normal chat.
	/// </summary>
	public static bool TryExecute(string text, string actor, out string error)
	{
		error = null;
		if (!IsCommand(text))
		{
			return false;
		}
		if (!IsAvailable)
		{
			// Not hosting: the caller's chat should still be delivered normally, so this is
			// reported as an error but not as "handled".
			error = "Commands only work while hosting a multiplayer world.";
			return true;
		}
		string word = FirstWord(text).ToLowerInvariant();
		// Only "no actor" means the host's own keyboard: TcpPeer always passes the connection's
		// gamertag, so a remote player cannot become the owner by choosing the host's name.
		bool isLocalHost = actor == null;
		Permission permission = RequiredFor(word);
		if (permission != Permission.None && !Permissions.Allows(actor, permission, isLocalHost))
		{
			error = (actor ?? "You") + " may not use " + word + ".";
			return true;
		}
		string rest = text.Substring(word.Length).Trim();
		switch (word)
		{
		case "!help":
			Say(HelpText, 255, 220, 120);
			return true;
		case "!who":
			DoWho();
			return true;
		case "!bans":
			DoBans();
			return true;
		case "!kick":
			DoKick(rest, out error);
			return true;
		case "!ban":
			DoBan(rest, out error);
			return true;
		case "!unban":
			DoUnban(rest, out error);
			return true;
		case "!say":
			if (rest.Length == 0)
			{
				error = "Usage: !say <text>";
				return true;
			}
			Say("[Server] " + rest, 255, 255, 160);
			return true;
		case "!mute":
			DoMute(rest, out error);
			return true;
		case "!unmute":
			DoUnmute(rest, out error);
			return true;
		case "!whisper":
			DoWhisper(rest, out error);
			return true;
		case "!motd":
			DoMotd(rest);
			return true;
		case "!rules":
			DoRules(rest);
			return true;
		case "!op":
			DoOp(rest, out error);
			return true;
		case "!deop":
			DoDeop(rest, out error);
			return true;
		case "!groups":
			DoGroups();
			return true;
		case "!region":
			DoRegion(rest, actor, isLocalHost, out error);
			return true;
		}
		return false;
	}

	/// <summary>
	/// <c>!region list</c> is readable by anyone allowed to inspect; the mutating subcommands
	/// need <see cref="Permission.Manage"/>, because they change who may build where for the
	/// whole map.
	/// </summary>
	private static void DoRegion(string rest, string actor, bool isLocalHost, out string error)
	{
		error = null;
		string text;
		string text2;
		SplitTarget(rest, out text, out text2);
		text = text.ToLowerInvariant();
		if (text.Length == 0 || text == "list")
		{
			DoRegionList();
			return;
		}
		if (!Permissions.Allows(actor, Permission.Manage, isLocalHost))
		{
			error = "Changing regions needs the 'manage' permission.";
			return;
		}
		switch (text)
		{
		case "add":
			DoRegionAdd(text2, out error);
			return;
		case "remove":
			DoRegionRemove(text2, out error);
			return;
		case "group":
			DoRegionGroup(text2, out error);
			return;
		default:
			error = "Usage: !region list | add <name> [radius] | remove <name> | group <name> [group]";
			break;
		}
	}

	private static void DoRegionList()
	{
		List<Region> list = Regions.All;
		if (list.Count == 0)
		{
			Say("No regions. !region add <name> [radius] protects the area around you.", 200, 200, 210);
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Say(list[i].Describe(), 200, 220, 255);
		}
		IList<string> loadProblems = Regions.LoadProblems;
		for (int j = 0; j < loadProblems.Count; j++)
		{
			Say("regions.txt: " + loadProblems[j], 255, 170, 140);
		}
	}

	/// <summary>
	/// Creates a region around the host's current position. The host is the only one who can
	/// do this, so "around me" is well defined - a remote player would have to send
	/// coordinates, and a misplaced region is hard to notice from inside it.
	/// </summary>
	private static void DoRegionAdd(string rest, out string error)
	{
		error = null;
		string text;
		string text2;
		SplitTarget(rest, out text, out text2);
		if (text.Length == 0)
		{
			error = "Usage: !region add <name> [radius]";
			return;
		}
		int result = 24;
		if (text2.Length != 0 && (!int.TryParse(text2, out result) || result < 1 || result > 500))
		{
			error = "Radius must be 1-500 tiles.";
			return;
		}
		int x = 0;
		int y = 0;
		if (!TryGetHostTile(out x, out y))
		{
			error = "Stand in the world first: the region is created around you.";
			return;
		}
		Region region = new Region();
		region.Name = text;
		region.X1 = x - result;
		region.Y1 = y - result;
		region.X2 = x + result;
		region.Y2 = y + result;
		region.Normalize();
		if (!Regions.Add(region, out error))
		{
			return;
		}
		Say("Created region '" + text + "' around you (" + region.Width + "x" + region.Height + "), host only. "
			+ "Use !region group " + text + " <group> to let a group build.", 180, 230, 180);
	}

	private static void DoRegionRemove(string name, out string error)
	{
		error = null;
		if (name.Length == 0)
		{
			error = "Usage: !region remove <name>";
			return;
		}
		if (!Regions.Remove(name))
		{
			error = "No region called '" + name + "'.";
			return;
		}
		Say("Removed region '" + name + "'.", 255, 190, 160);
	}

	private static void DoRegionGroup(string rest, out string error)
	{
		error = null;
		string text;
		string text2;
		SplitTarget(rest, out text, out text2);
		if (text.Length == 0)
		{
			error = "Usage: !region group <name> [group]";
			return;
		}
		if (!Regions.SetGroup(text, text2, out error))
		{
			return;
		}
		Say(string.IsNullOrEmpty(text2) ? ("Region '" + text + "' is now host only.") : ("Region '" + text + "' is now buildable by group '" + text2 + "'."), 180, 230, 180);
	}

	/// <summary>The host's own tile position, or false when there is no world running.</summary>
	private static bool TryGetHostTile(out int x, out int y)
	{
		x = 0;
		y = 0;
		try
		{
			UI ui = UI.MainUI;
			if (ui == null || ui.ActivePlayer == null)
			{
				return false;
			}
			Microsoft.Xna.Framework.Rectangle rectangle = ui.ActivePlayer.XYWH;
			// Player coordinates are in pixels; a tile is 16.
			x = (rectangle.X + 10) / 16;
			y = (rectangle.Y + 21) / 16;
			return x >= 0 && y >= 0 && x < Main.MaxTilesX && y < Main.MaxTilesY;
		}
		catch
		{
			return false;
		}
	}

	private static void DoOp(string rest, out string error)
	{
		error = null;
		string target;
		string group;
		SplitTarget(rest, out target, out group);
		if (target.Length == 0)
		{
			error = "Usage: !op <player> [group]";
			return;
		}
		// Default to "mod": promoting someone should not silently hand out !op as well.
		if (group.Length == 0)
		{
			group = "mod";
		}
		if (!Permissions.Assign(target, group, out error))
		{
			return;
		}
		Say(target + " is now in group '" + group + "' (" + Permissions.Describe(Permissions.PermissionsOf(group)) + ").", 180, 230, 180);
	}

	private static void DoDeop(string rest, out string error)
	{
		error = null;
		if (rest.Length == 0)
		{
			error = "Usage: !deop <player>";
			return;
		}
		if (!Permissions.Revoke(rest))
		{
			error = "'" + rest + "' had no group.";
			return;
		}
		Say(rest + " lost their group.", 255, 190, 160);
	}

	private static void DoGroups()
	{
		List<string> list = Permissions.GroupNames();
		if (list.Count == 0)
		{
			Say("No groups defined.", 200, 200, 210);
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Say(list[i] + ": " + Permissions.Describe(Permissions.PermissionsOf(list[i])), 200, 220, 255);
		}
		List<KeyValuePair<string, string>> list2 = Permissions.UserList();
		if (list2.Count == 0)
		{
			Say("Nobody has a group yet (the host is always the owner).", 200, 200, 210);
			return;
		}
		string text = string.Empty;
		for (int j = 0; j < list2.Count; j++)
		{
			if (j > 0)
			{
				text += ", ";
			}
			text += list2[j].Key + "=" + list2[j].Value;
		}
		Say(text, 200, 200, 210);
		// File problems are only useful to whoever maintains the file, i.e. the host.
		IList<string> loadProblems = Permissions.LoadProblems;
		for (int k = 0; k < loadProblems.Count; k++)
		{
			Say("permissions.ini: " + loadProblems[k], 255, 170, 140);
		}
	}

	private static void DoMute(string rest, out string error)
	{
		error = null;
		string target;
		string reason;
		SplitTarget(rest, out target, out reason);
		if (target.Length == 0)
		{
			error = "Usage: !mute <player>";
			return;
		}
		// Resolve to the name the host actually has, so the mute is not keyed to a prefix.
		string text = target;
		TcpPeer host = TcpPeer.CurrentHost;
		if (host != null)
		{
			string resolved;
			if (host.ResolveName(target, out resolved))
			{
				text = resolved;
			}
		}
		if (!HostModeration.Mute(text))
		{
			error = "'" + text + "' is already muted.";
			return;
		}
		Say("Muted " + text + ".", 255, 190, 160);
	}

	private static void DoUnmute(string rest, out string error)
	{
		error = null;
		if (rest.Length == 0)
		{
			error = "Usage: !unmute <player>";
			return;
		}
		if (!HostModeration.Unmute(rest))
		{
			error = "'" + rest + "' is not muted.";
			return;
		}
		Say("Unmuted " + rest + ".", 180, 230, 180);
	}

	private static void DoWhisper(string rest, out string error)
	{
		error = null;
		string target;
		string message;
		SplitTarget(rest, out target, out message);
		if (target.Length == 0 || message.Length == 0)
		{
			error = "Usage: !whisper <player> <text>";
			return;
		}
		string sent;
		if (!TcpPeer.CurrentHost.SendTextTo(target, "-> " + message, out sent))
		{
			error = "No single player matching '" + target + "'.";
			return;
		}
		// The host sees what it sent, otherwise a whisper looks like nothing happened.
		Say("-> " + sent + ": " + message, 170, 190, 220);
	}

	private static void DoMotd(string rest)
	{
		if (rest.Length == 0)
		{
			Say(string.IsNullOrEmpty(HostModeration.Motd) ? "No MOTD set." : ("MOTD: " + HostModeration.Motd), 255, 235, 150);
			return;
		}
		HostModeration.Motd = rest;
		Persist();
		// New joiners get it from the handshake; the current players get it now.
		Broadcast("[Server] " + rest, 255, 235, 150);
	}

	private static void DoRules(string rest)
	{
		if (rest.Length == 0)
		{
			Say(string.IsNullOrEmpty(HostModeration.Rules) ? "No rules set." : ("Rules: " + HostModeration.Rules), 255, 235, 150);
			return;
		}
		HostModeration.Rules = rest;
		Persist();
		Broadcast("[Server] Rules: " + rest, 255, 235, 150);
	}

	/// <summary>Writes the MOTD and rules to Settings.ini so they outlive the session.</summary>
	private static void Persist()
	{
		try
		{
			Settings settings = new Settings(Terraria.Storage.AppPaths.SettingsFile);
			HostModeration.Save(settings);
			settings.Close();
		}
		catch (Exception ex)
		{
			Program.LogWarn("HostCommands", "could not save the server text: " + ex.Message);
		}
	}

	/// <summary>
	/// Says something to every connected player and to the host's own feed. Used for MOTD and
	/// rules, which are meant for everyone, unlike <c>!whisper</c>.
	/// </summary>
	private static void Broadcast(string text, int r, int g, int b)
	{
		Say(text, r, g, b);
		TcpPeer host = TcpPeer.CurrentHost;
		if (host == null)
		{
			return;
		}
		string[] names = host.ConnectedNames().ToArray();
		for (int i = 0; i < names.Length; i++)
		{
			string sent;
			host.SendTextTo(names[i], text, out sent);
		}
	}

	private static void DoWho()
	{
		TcpPeer host = TcpPeer.CurrentHost;
		if (host == null)
		{
			Say("Nobody is connected.", 200, 200, 210);
			return;
		}
		List<string> list = host.ConnectedNames();
		if (list.Count == 0)
		{
			Say("Nobody is connected.", 200, 200, 210);
			return;
		}
		Say(list.Count + " connected: " + string.Join(", ", list.ToArray()), 200, 220, 255);
	}

	private static void DoBans()
	{
		List<string> list = BanList.All;
		if (list.Count == 0)
		{
			Say("The ban list is empty.", 200, 200, 210);
			return;
		}
		Say(list.Count + " banned: " + string.Join(", ", list.ToArray()), 255, 190, 160);
	}

	private static void DoKick(string rest, out string error)
	{
		error = null;
		string target;
		string reason;
		SplitTarget(rest, out target, out reason);
		if (target.Length == 0)
		{
			error = "Usage: !kick <player> [reason]";
			return;
		}
		if (reason.Length == 0)
		{
			reason = "Kicked by the host.";
		}
		string kicked;
		if (!TcpPeer.CurrentHost.Kick(target, reason, out kicked))
		{
			error = "No player matching '" + target + "'.";
			return;
		}
		Say("Kicked " + kicked + ".", 255, 190, 160);
	}

	private static void DoBan(string rest, out string error)
	{
		error = null;
		string target;
		string reason;
		SplitTarget(rest, out target, out reason);
		if (target.Length == 0)
		{
			error = "Usage: !ban <player|ip>";
			return;
		}
		// Ban first, then kick: the other order would let the player reconnect in between.
		bool added = BanList.Add(target);
		string kicked = null;
		if (TcpPeer.CurrentHost != null)
		{
			TcpPeer.CurrentHost.Kick(target, (reason.Length == 0) ? "Banned by the host." : reason, out kicked);
		}
		if (!added && kicked == null)
		{
			error = "Nothing to do: '" + target + "' is not connected and not on the ban list.";
			return;
		}
		Say(added ? ("Banned " + target + (kicked != null ? (" and kicked " + kicked) : "") + ".") : ("'" + target + "' was already banned; kicked " + kicked + "."), 255, 160, 140);
	}

	private static void DoUnban(string rest, out string error)
	{
		error = null;
		if (rest.Length == 0)
		{
			error = "Usage: !unban <player|ip>";
			return;
		}
		if (!BanList.Remove(rest))
		{
			error = "'" + rest + "' is not on the ban list.";
			return;
		}
		Say("Unbanned " + rest + ".", 180, 230, 180);
	}

	/// <summary>Splits "name reason words" into the target and the optional reason.</summary>
	internal static void SplitTarget(string rest, out string target, out string reason)
	{
		target = string.Empty;
		reason = string.Empty;
		if (string.IsNullOrWhiteSpace(rest))
		{
			return;
		}
		string text = rest.Trim();
		int num = text.IndexOf(' ');
		if (num < 0)
		{
			target = text;
			return;
		}
		target = text.Substring(0, num);
		reason = text.Substring(num + 1).Trim();
	}

	private static void Say(string text, int r, int g, int b)
	{
		try
		{
			Main.NewText(text, r, g, b);
		}
		catch
		{
		}
	}
}
