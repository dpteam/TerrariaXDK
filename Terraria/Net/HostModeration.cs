using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terraria.Net;

/// <summary>
/// Host-side moderation state: who is muted, and the MOTD/rules shown on join.
///
/// Mutes are deliberately **not** persisted: they exist for the current session, and a player
/// who is quiet today should not silently stay muted a week later because nobody remembered
/// to unmute them. Bans, which are meant to outlive the session, live in
/// <see cref="BanList"/> instead.
///
/// The MOTD and rules do persist, in <c>[Network] Motd</c> / <c>[Network] Rules</c>, because
/// they are server policy and usually outlive one evening.
/// </summary>
internal static class HostModeration
{
	private static readonly HashSet<string> Muted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static readonly object Sync = new object();

	/// <summary>Message sent to a muted player instead of their message being delivered.</summary>
	public static string MuteNotice = "You are muted by the host.";

	/// <summary>Message of the day, shown to every joining player. Empty disables it.</summary>
	public static string Motd = string.Empty;

	/// <summary>Rules text, shown right after the MOTD. Empty disables it.</summary>
	public static string Rules = string.Empty;

	/// <summary>How long a muted player may be reminded, in seconds, so it is not every line.</summary>
	public const int MuteNoticeSeconds = 10;

	public static void Load(Settings settings)
	{
		Motd = (settings.Get("Network", "Motd") ?? string.Empty).Trim();
		Rules = (settings.Get("Network", "Rules") ?? string.Empty).Trim();
	}

	public static void Save(Settings settings)
	{
		settings.Update("Network", "Motd", Motd, "Message shown to every player who joins this host.");
		settings.Update("Network", "Rules", Rules, "Rules shown to every player who joins this host.");
	}

	/// <summary>Muted players for this session, in the order they were muted.</summary>
	public static List<string> All
	{
		get
		{
			lock (Sync)
			{
				return new List<string>(Muted);
			}
		}
	}

	public static int Count
	{
		get
		{
			lock (Sync)
			{
				return Muted.Count;
			}
		}
	}

	public static bool IsMuted(string gamertag)
	{
		if (string.IsNullOrWhiteSpace(gamertag))
		{
			return false;
		}
		string text = gamertag.Trim();
		lock (Sync)
		{
			return Muted.Contains(text);
		}
	}

	/// <summary>Mutes a player. Returns false when the name is blank or already muted.</summary>
	public static bool Mute(string gamertag)
	{
		if (string.IsNullOrWhiteSpace(gamertag))
		{
			return false;
		}
		string text = gamertag.Trim();
		lock (Sync)
		{
			return Muted.Add(text);
		}
	}

	/// <summary>Unmutes a player. Returns false when they were not muted.</summary>
	public static bool Unmute(string gamertag)
	{
		if (string.IsNullOrWhiteSpace(gamertag))
		{
			return false;
		}
		string text = gamertag.Trim();
		lock (Sync)
		{
			return Muted.Remove(text);
		}
	}

	/// <summary>Forgets every mute. Called when hosting stops; mutes are per session.</summary>
	public static void ResetMutes()
	{
		lock (Sync)
		{
			Muted.Clear();
		}
	}

	/// <summary>
	/// Builds a chat packet (message 25) so the host can talk to one player without going
	/// through the game's own send path, which has no "send to this connection" option.
	///
	/// The game reads this with a <see cref="BinaryReader"/>, so the string must be written the
	/// same way: 7-bit-encoded length, then UTF-8 bytes.
	/// </summary>
	internal static byte[] BuildChatPayload(string text, int r, int g, int b)
	{
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8))
		{
			binaryWriter.Write((byte)25);
			binaryWriter.Write((byte)r);
			binaryWriter.Write((byte)g);
			binaryWriter.Write((byte)b);
			binaryWriter.Write(text ?? string.Empty);
			binaryWriter.Flush();
		}
		// MemoryStream.ToArray() still works after the writer closed it, and returns exactly
		// the bytes written regardless of the stream's position.
		return memoryStream.ToArray();
	}

	/// <summary>The lines a joining player should see, in order. Empty strings are skipped.</summary>
	public static List<string> WelcomeLines()
	{
		List<string> list = new List<string>(2);
		if (!string.IsNullOrEmpty(Motd))
		{
			list.Add(Motd);
		}
		if (!string.IsNullOrEmpty(Rules))
		{
			list.Add("Rules: " + Rules);
		}
		return list;
	}
}
