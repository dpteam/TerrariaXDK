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
	public static readonly string[] Names = new string[22]
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
		"!additem",
		"!delitem",
		"!items",
		"!stats",
		"!world",
		"!serverconfig",
		"!help"
	};

	private const string HelpText = "chat.host.help";

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
		case "!additem":
		case "!delitem":
			return Permission.Manage;
		case "!items":
			return Permission.Inspect;
		case "!stats":
		case "!world":
			return Permission.Inspect;
		case "!serverconfig":
			return Permission.Manage;
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
			error = Texts.Get("chat.host.not.hosting");
			return true;
		}
		string word = FirstWord(text).ToLowerInvariant();
		// Only "no actor" means the host's own keyboard: TcpPeer always passes the connection's
		// gamertag, so a remote player cannot become the owner by choosing the host's name.
		bool isLocalHost = actor == null;
		Permission permission = RequiredFor(word);
		if (permission != Permission.None && !Permissions.Allows(actor, permission, isLocalHost))
		{
			error = Texts.Format("chat.host.forbidden", actor ?? Texts.Get("chat.host.you"), word);
			return true;
		}
		string rest = text.Substring(word.Length).Trim();
		switch (word)
		{
		case "!help":
			Say(Texts.Get(HelpText), 255, 220, 120);
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
				error = Texts.Get("chat.host.usage.say");
				return true;
			}
			Say(Texts.Format("chat.host.server.prefix", rest), 255, 255, 160);
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
		case "!additem":
			DoAddItem(rest, out error);
			return true;
		case "!delitem":
			DoDelItem(rest, out error);
			return true;
		case "!items":
			DoItems();
			return true;
		case "!stats":
			DoStats();
			return true;
		case "!world":
			DoWorld();
			return true;
		case "!serverconfig":
			DoServerConfig(rest, out error);
			return true;
		}
		return false;
	}

	/// <summary>
	/// <c>!serverconfig</c> shows which file is in charge; <c>!serverconfig create</c> writes the
	/// hosting profile from the current values. Creating it is explicit because the file wins
	/// over Settings.ini, and silently switching that over would lose the player's edits.
	/// </summary>
	private static void DoServerConfig(string rest, out string error)
	{
		error = null;
		if (rest.Length == 0 || rest.Equals("show", StringComparison.OrdinalIgnoreCase))
		{
			Say(ServerConfig.Describe(), 200, 220, 255);
			Say(Texts.Format("chat.host.serverconfig.summary", NetworkConfig.Port, NetworkConfig.MaxPlayers,
				Texts.Get(string.IsNullOrEmpty(NetworkConfig.Password) ? "chat.host.off" : "chat.host.on"),
				Texts.Get(MasterServerClient.Publish ? "chat.host.on" : "chat.host.off"),
				WorldProtection.SpawnRadius,
				Texts.Get(WorldProtection.DisableBuild ? "chat.host.disabled" : "chat.host.allowed")), 200, 220, 255);
			Say(Texts.Format("chat.host.serverconfig.hint", ServerConfig.FileName), 170, 180, 200);
			return;
		}
		if (!rest.Equals("create", StringComparison.OrdinalIgnoreCase))
		{
			error = Texts.Get("chat.host.usage.serverconfig");
			return;
		}
		if (!ServerConfig.Create(out error))
		{
			error = Texts.Format("chat.host.serverconfig.failed", ServerConfig.FileName, error);
			return;
		}
		Say(Texts.Format("chat.host.serverconfig.wrote", ServerConfig.FileName), 180, 230, 180);
	}

	private static void DoAddItem(string rest, out string error)
	{
		error = null;
		int netId;
		if (!ItemRestrictions.TryParseEntry(rest, out netId, out error))
		{
			return;
		}
		if (!ItemRestrictions.Add(netId))
		{
			error = Texts.Format("chat.host.item.already", netId);
			return;
		}
		Say(Texts.Format("chat.host.item.banned", netId), 255, 190, 160);
	}

	private static void DoDelItem(string rest, out string error)
	{
		error = null;
		int netId;
		if (!ItemRestrictions.TryParseEntry(rest, out netId, out error))
		{
			return;
		}
		if (!ItemRestrictions.Remove(netId))
		{
			error = Texts.Format("chat.host.item.not", netId);
			return;
		}
		Say(Texts.Format("chat.host.item.unbanned", netId), 180, 230, 180);
	}

	private static void DoItems()
	{
		List<int> list = ItemRestrictions.All;
		if (list.Count == 0)
		{
			Say(Texts.Get("chat.host.items.none"), 200, 200, 210);
			return;
		}
		Say(Texts.Format("chat.host.items.list", list.Count, string.Join(", ", list.ConvertAll(delegate(int id) { return id.ToString(); }).ToArray())), 255, 190, 160);
	}

	private static void DoStats()
	{
		Say(Texts.Format("chat.host.stats.players", (TcpPeer.CurrentHost == null) ? 0 : (TcpPeer.CurrentHost.ConnectedCount + 1), NetworkConfig.MaxPlayers + 1), 200, 220, 255);
		Say(Texts.Format("chat.host.stats.traffic", NetworkStats.Describe()), 200, 220, 255);
		Say(Texts.Format("chat.host.stats.uptime", FormatDuration(HostUptimeSeconds())), 200, 220, 255);
	}

	/// <summary>
	/// Seconds since hosting started. Uses the transport's start tick rather than a timer, so
	/// there is one source of truth for "how long has this server been up".
	/// </summary>
	internal static long HostUptimeSeconds()
	{
		if (!NetworkStats.HasStart)
		{
			return 0;
		}
		long num = unchecked(Environment.TickCount - NetworkStats.StartedTick);
		if (num < 0)
		{
			// TickCount wraps every ~49 days; anything negative is a wrap, not a time machine.
			num += int.MaxValue;
		}
		return num / 1000;
	}

	private static void DoWorld()
	{
		Say(Texts.Format("chat.host.world.name", string.IsNullOrEmpty(Main.WorldName) ? Texts.Get("chat.host.world.unnamed") : Main.WorldName, Main.MaxTilesX, Main.MaxTilesY), 200, 220, 255);
		Say(Texts.Format("chat.host.world.spawn", Main.SpawnTileX, Main.SpawnTileY, Main.WorldSurface, Main.RockLayer), 200, 220, 255);
		// Names joined with ", " rather than concatenated with trailing spaces: a translator
		// moving the list around should not have to preserve a space that ends an English word.
		List<string> list = new List<string>();
		if (NPC.HasDownedBoss1)
		{
			list.Add(Texts.Get("chat.host.boss.eye"));
		}
		if (NPC.HasDownedBoss2)
		{
			list.Add(Texts.Get("chat.host.boss.eater"));
		}
		if (NPC.HasDownedBoss3)
		{
			list.Add(Texts.Get("chat.host.boss.skeletron"));
		}
		Say(Texts.Format("chat.host.world.bosses", (list.Count == 0) ? Texts.Get("chat.host.boss.none") : string.Join(", ", list.ToArray())), 200, 220, 255);
	}

	/// <summary>"1h 02m 03s" - readable in one chat line.</summary>
	internal static string FormatDuration(long seconds)
	{
		if (seconds < 0)
		{
			seconds = 0;
		}
		long num = seconds / 3600;
		long num2 = seconds % 3600 / 60;
		long num3 = seconds % 60;
		if (num > 0)
		{
			return num + "h " + num2.ToString("00") + "m";
		}
		if (num2 > 0)
		{
			return num2 + "m " + num3.ToString("00") + "s";
		}
		return num3 + "s";
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
			error = Texts.Get("chat.host.region.need.manage");
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
			error = Texts.Get("chat.host.region.usage");
			break;
		}
	}

	private static void DoRegionList()
	{
		List<Region> list = Regions.All;
		if (list.Count == 0)
		{
			Say(Texts.Get("chat.host.region.none"), 200, 200, 210);
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Say(list[i].Describe(), 200, 220, 255);
		}
		IList<string> loadProblems = Regions.LoadProblems;
		for (int j = 0; j < loadProblems.Count; j++)
		{
			Say(Texts.Format("chat.host.region.file", loadProblems[j]), 255, 170, 140);
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
			error = Texts.Get("chat.host.region.usage.add");
			return;
		}
		int result = 24;
		if (text2.Length != 0 && (!int.TryParse(text2, out result) || result < 1 || result > 500))
		{
			error = Texts.Get("chat.host.region.radius");
			return;
		}
		int x = 0;
		int y = 0;
		if (!TryGetHostTile(out x, out y))
		{
			error = Texts.Get("chat.host.region.no.world");
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
		Say(Texts.Format("chat.host.region.created", text, region.Width, region.Height), 180, 230, 180);
	}

	private static void DoRegionRemove(string name, out string error)
	{
		error = null;
		if (name.Length == 0)
		{
			error = Texts.Get("chat.host.region.usage.remove");
			return;
		}
		if (!Regions.Remove(name))
		{
			error = Texts.Format("chat.host.region.missing", name);
			return;
		}
		Say(Texts.Format("chat.host.region.removed", name), 255, 190, 160);
	}

	private static void DoRegionGroup(string rest, out string error)
	{
		error = null;
		string text;
		string text2;
		SplitTarget(rest, out text, out text2);
		if (text.Length == 0)
		{
			error = Texts.Get("chat.host.region.usage.group");
			return;
		}
		if (!Regions.SetGroup(text, text2, out error))
		{
			return;
		}
		Say(string.IsNullOrEmpty(text2)
			? Texts.Format("chat.host.region.host.only", text)
			: Texts.Format("chat.host.region.group.set", text, text2), 180, 230, 180);
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
			error = Texts.Get("chat.host.usage.op");
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
		Say(Texts.Format("chat.host.op.done", target, group, Permissions.Describe(Permissions.PermissionsOf(group))), 180, 230, 180);
	}

	private static void DoDeop(string rest, out string error)
	{
		error = null;
		if (rest.Length == 0)
		{
			error = Texts.Get("chat.host.usage.deop");
			return;
		}
		if (!Permissions.Revoke(rest))
		{
			error = Texts.Format("chat.host.deop.none", rest);
			return;
		}
		Say(Texts.Format("chat.host.deop.done", rest), 255, 190, 160);
	}

	private static void DoGroups()
	{
		List<string> list = Permissions.GroupNames();
		if (list.Count == 0)
		{
			Say(Texts.Get("chat.host.groups.none"), 200, 200, 210);
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			Say(list[i] + ": " + Permissions.Describe(Permissions.PermissionsOf(list[i])), 200, 220, 255);
		}
		List<KeyValuePair<string, string>> list2 = Permissions.UserList();
		if (list2.Count == 0)
		{
			Say(Texts.Get("chat.host.groups.nobody"), 200, 200, 210);
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
			Say(Texts.Format("chat.host.groups.file", loadProblems[k]), 255, 170, 140);
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
			error = Texts.Get("chat.host.usage.mute");
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
			error = Texts.Format("chat.host.mute.already", text);
			return;
		}
		Say(Texts.Format("chat.host.mute.done", text), 255, 190, 160);
	}

	private static void DoUnmute(string rest, out string error)
	{
		error = null;
		if (rest.Length == 0)
		{
			error = Texts.Get("chat.host.usage.unmute");
			return;
		}
		if (!HostModeration.Unmute(rest))
		{
			error = Texts.Format("chat.host.unmute.not", rest);
			return;
		}
		Say(Texts.Format("chat.host.unmute.done", rest), 180, 230, 180);
	}

	private static void DoWhisper(string rest, out string error)
	{
		error = null;
		string target;
		string message;
		SplitTarget(rest, out target, out message);
		if (target.Length == 0 || message.Length == 0)
		{
			error = Texts.Get("chat.host.usage.whisper");
			return;
		}
		string sent;
		if (!TcpPeer.CurrentHost.SendTextTo(target, "-> " + message, out sent))
		{
			error = Texts.Format("chat.host.whisper.missing", target);
			return;
		}
		// The host sees what it sent, otherwise a whisper looks like nothing happened.
		Say(Texts.Format("chat.host.whisper.sent", sent, message), 170, 190, 220);
	}

	private static void DoMotd(string rest)
	{
		if (rest.Length == 0)
		{
			Say(string.IsNullOrEmpty(HostModeration.Motd) ? Texts.Get("chat.host.motd.none") : Texts.Format("chat.host.motd.show", HostModeration.Motd), 255, 235, 150);
			return;
		}
		HostModeration.Motd = rest;
		Persist();
		// New joiners get it from the handshake; the current players get it now.
		Broadcast(Texts.Format("chat.host.server.prefix", rest), 255, 235, 150);
	}

	private static void DoRules(string rest)
	{
		if (rest.Length == 0)
		{
			Say(string.IsNullOrEmpty(HostModeration.Rules) ? Texts.Get("chat.host.rules.none") : Texts.Format("chat.host.rules.show", HostModeration.Rules), 255, 235, 150);
			return;
		}
		HostModeration.Rules = rest;
		Persist();
		Broadcast(Texts.Format("chat.host.server.rules", rest), 255, 235, 150);
	}

	/// <summary>Writes the MOTD and rules to whichever config file is in charge.</summary>
	private static void Persist()
	{
		Terraria.Net.ServerConfig.Persist("Motd", HostModeration.Motd, "Message shown to every player who joins this host.");
		Terraria.Net.ServerConfig.Persist("Rules", HostModeration.Rules, "Rules shown to every player who joins this host.");
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
			Say(Texts.Get("chat.host.nobody"), 200, 200, 210);
			return;
		}
		List<string> list = host.ConnectedNames();
		if (list.Count == 0)
		{
			Say(Texts.Get("chat.host.nobody"), 200, 200, 210);
			return;
		}
		Say(Texts.Format("chat.host.who", list.Count, string.Join(", ", list.ToArray())), 200, 220, 255);
	}

	private static void DoBans()
	{
		List<string> list = BanList.All;
		if (list.Count == 0)
		{
			Say(Texts.Get("chat.host.bans.empty"), 200, 200, 210);
			return;
		}
		Say(Texts.Format("chat.host.bans.list", list.Count, string.Join(", ", list.ToArray())), 255, 190, 160);
	}

	private static void DoKick(string rest, out string error)
	{
		error = null;
		string target;
		string reason;
		SplitTarget(rest, out target, out reason);
		if (target.Length == 0)
		{
			error = Texts.Get("chat.host.usage.kick");
			return;
		}
		if (reason.Length == 0)
		{
			reason = Texts.Get("chat.host.kick.default.reason");
		}
		string kicked;
		if (!TcpPeer.CurrentHost.Kick(target, reason, out kicked))
		{
			error = Texts.Format("chat.host.kick.missing", target);
			return;
		}
		Say(Texts.Format("chat.host.kick.done", kicked), 255, 190, 160);
	}

	private static void DoBan(string rest, out string error)
	{
		error = null;
		string target;
		string reason;
		SplitTarget(rest, out target, out reason);
		if (target.Length == 0)
		{
			error = Texts.Get("chat.host.usage.ban");
			return;
		}
		// Ban first, then kick: the other order would let the player reconnect in between.
		bool added = BanList.Add(target);
		string kicked = null;
		if (TcpPeer.CurrentHost != null)
		{
			TcpPeer.CurrentHost.Kick(target, (reason.Length == 0) ? Texts.Get("chat.host.ban.default.reason") : reason, out kicked);
		}
		if (!added && kicked == null)
		{
			error = Texts.Format("chat.host.ban.nothing", target);
			return;
		}
		Say(added
			? ((kicked != null)
				? Texts.Format("chat.host.ban.done.kicked", target, kicked)
				: Texts.Format("chat.host.ban.done", target))
			: Texts.Format("chat.host.ban.already", target, kicked), 255, 160, 140);
	}

	private static void DoUnban(string rest, out string error)
	{
		error = null;
		if (rest.Length == 0)
		{
			error = Texts.Get("chat.host.usage.unban");
			return;
		}
		if (!BanList.Remove(rest))
		{
			error = Texts.Format("chat.host.unban.not", rest);
			return;
		}
		Say(Texts.Format("chat.host.unban.done", rest), 180, 230, 180);
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
