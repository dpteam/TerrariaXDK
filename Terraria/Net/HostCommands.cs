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
	public static readonly string[] Names = new string[12]
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
		"!help"
	};

	private const string HelpText = "!kick <player> [reason], !ban/!unban <player|ip>, !bans, !who, !mute/!unmute <player>, !whisper <player> <text>, !motd [text], !rules [text], !say <text>";

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
	/// Runs a command. Returns false when it is not a host command at all, so the caller can
	/// fall back to normal chat.
	/// </summary>
	public static bool TryExecute(string text, out string error)
	{
		error = null;
		if (!IsCommand(text))
		{
			return false;
		}
		if (!IsAvailable)
		{
			error = "Only the host can use " + FirstWord(text) + ".";
			return true;
		}
		string word = FirstWord(text).ToLowerInvariant();
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
		}
		return false;
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
