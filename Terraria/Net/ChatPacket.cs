using System;
using System.IO;
using System.Text;

namespace Terraria.Net;

/// <summary>
/// Reads and writes the game's chat packet (message 25).
///
/// The host needs both halves for two reasons: it sends MOTD/whisper lines itself, and it has
/// to look inside an incoming chat line to recognise a command. Doing that with hand-written
/// byte offsets would silently break the first time the packet layout is touched, so the
/// layout lives in one place here - and it is checked against a real kit round-trip in the
/// self-test.
///
/// Layout: <c>[25][r][g][b][string]</c>, the string written exactly as
/// <see cref="BinaryWriter.Write(string)"/> does it (7-bit length, then UTF-8 bytes), which is
/// what the game's own reader expects.
/// </summary>
internal static class ChatPacket
{
	/// <summary>Message id of a plain chat line in the console protocol.</summary>
	public const byte MessageId = 25;

	/// <summary>Builds a full game packet, without the transport framing.</summary>
	internal static byte[] Build(string text, int r, int g, int b)
	{
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8))
		{
			binaryWriter.Write(MessageId);
			binaryWriter.Write((byte)r);
			binaryWriter.Write((byte)g);
			binaryWriter.Write((byte)b);
			binaryWriter.Write(text ?? string.Empty);
			binaryWriter.Flush();
		}
		// ToArray() works after the writer closed the stream and returns exactly what was
		// written, whatever the stream position is.
		return memoryStream.ToArray();
	}

	/// <summary>
	/// Removes the leading <c>&lt;Speaker&gt; </c> that the sending client puts in front of a
	/// chat line, so the host can see whether what follows is a command.
	///
	/// The client wraps every line, including one that starts with <c>!</c>, because the game
	/// packet carries only a string - so the host has to unwrap it. The speaker itself is
	/// ignored: permissions are always taken from the connection, never from the text.
	/// </summary>
	internal static string StripSpeaker(string text)
	{
		if (string.IsNullOrEmpty(text) || text[0] != '<')
		{
			return text;
		}
		// A name is short; a message that merely starts with '<' must not be mis-split.
		int num = text.IndexOf('>', 1);
		if (num <= 1 || num > 40)
		{
			return text;
		}
		if (num + 1 >= text.Length)
		{
			return text;
		}
		return (text[num + 1] == ' ') ? text.Substring(num + 2) : text;
	}

	/// <summary>
	/// Extracts the text of a chat packet starting at <paramref name="offset"/> (the message id).
	/// Returns false for anything that is not a well-formed chat line; a hostile client must not
	/// be able to make this throw or read out of bounds.
	/// </summary>
	internal static bool TryReadText(byte[] packet, int offset, out string text)
	{
		text = null;
		if (packet == null || offset < 0 || offset + 5 > packet.Length)
		{
			return false;
		}
		if (packet[offset] != MessageId)
		{
			return false;
		}
		try
		{
			using MemoryStream memoryStream = new MemoryStream(packet, offset + 4, packet.Length - offset - 4);
			using BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);
			string text2 = binaryReader.ReadString();
			// Anything the game would keep as a chat line is useful to us; strip control
			// characters so a command cannot be smuggled in as "!\u0000kick".
			text = NameRules.Sanitize(text2);
			return text.Length != 0;
		}
		catch
		{
			// Truncated or malformed length prefix, invalid UTF-8, or a string longer than the
			// frame. Not ours to fix: report "not a chat line" and let the caller ignore it.
			return false;
		}
	}
}
