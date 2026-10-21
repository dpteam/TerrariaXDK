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
	public static readonly string[] Names = new string[7]
	{
		"!kick",
		"!ban",
		"!unban",
		"!bans",
		"!who",
		"!say",
		"!help"
	};

	private const string HelpText = "!kick <player> [reason], !ban <player|ip>, !unban <player|ip>, !bans, !who, !say <text>";

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
		}
		return false;
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
