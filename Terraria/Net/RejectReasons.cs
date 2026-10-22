using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terraria.Net;

/// <summary>
/// Why the host refused a connection.
///
/// These used to travel as an English sentence (<c>"Wrong password."</c>) and the client printed
/// it verbatim, so a German client joining an English host was told, in English, what it had done
/// wrong - the one place in the language work that could not be fixed on the client alone,
/// because the text was chosen on the other machine.
///
/// The code is the payload; the human text is chosen by whoever reads it, in their own language.
/// Extra values (protocol numbers, a kick reason) travel as fields, because a sentence cannot be
/// safely assembled from translated fragments: German needs the verb last.
/// </summary>
internal enum RejectReason : byte
{
	/// <summary>Not a code this build knows - a newer host, or a legacy text payload.</summary>
	Unknown = 0,
	ProtocolMismatch = 1,
	ModMismatch = 2,
	Banned = 3,
	WrongPassword = 4,
	ServerFull = 5,
	Kicked = 6,
	/// <summary>The host allows only listed players, and this one is not on the list.</summary>
	NotWhitelisted = 7
}

/// <summary>Encoding and decoding of the <c>OpReject</c> payload.</summary>
internal static class RejectReasons
{
	/// <summary>Highest code this build understands; anything above it is <see cref="RejectReason.Unknown"/>.</summary>
	internal const byte MaxKnown = 7;

	/// <summary>
	/// Builds the payload: the code, then the fields as .NET binary strings.
	///
	/// Length-prefixed fields rather than a separator character, because one of the fields is a
	/// kick reason typed by the host - any separator would be something a player can type.
	/// </summary>
	public static byte[] Build(RejectReason reason, params string[] fields)
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
		binaryWriter.Write((byte)reason);
		int num = (fields == null) ? 0 : fields.Length;
		binaryWriter.Write(num);
		for (int i = 0; i < num; i++)
		{
			binaryWriter.Write(fields[i] ?? string.Empty);
		}
		binaryWriter.Flush();
		return memoryStream.ToArray();
	}

	/// <summary>
	/// Reads a payload. A payload that is not in the new format is treated as the old text form:
	/// the first byte of a sentence is a printable character, while every code this build knows
	/// is 1..6, so a legacy message can never be mistaken for a code.
	/// </summary>
	public static bool TryParse(byte[] payload, out RejectReason reason, out string[] fields)
	{
		reason = RejectReason.Unknown;
		fields = new string[0];
		if (payload == null || payload.Length == 0)
		{
			return false;
		}
		bool flag = payload[0] >= 1 && payload[0] <= MaxKnown && payload.Length >= 5;
		if (!flag)
		{
			fields = new string[1] { Encoding.UTF8.GetString(payload) };
			return true;
		}
		reason = (RejectReason)payload[0];
		try
		{
			using MemoryStream memoryStream = new MemoryStream(payload, 1, payload.Length - 1);
			BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);
			int num = binaryReader.ReadInt32();
			if (num < 0 || num > 16)
			{
				throw new InvalidDataException("field count " + num);
			}
			fields = new string[num];
			for (int i = 0; i < num; i++)
			{
				fields[i] = binaryReader.ReadString();
			}
			return true;
		}
		catch (Exception)
		{
			// Truncated or hostile payload: keep the code, drop the fields. Showing "the server
			// refused you" is better than throwing on a network thread.
			reason = RejectReason.Unknown;
			fields = new string[1] { Encoding.UTF8.GetString(payload) };
			return true;
		}
	}

	/// <summary>
	/// The text for this refusal, in the player's language. A kick carries the host's own words
	/// as the last field: that text cannot be translated by the client, so it is appended rather
	/// than pretended to be translatable.
	/// </summary>
	public static string Describe(RejectReason reason, string[] fields)
	{
		string key = KeyFor(reason, fields);
		return Texts.Format(key, ArgumentsFor(key, fields));
	}

	/// <summary>The same sentence in English, for the log. Logs stay English on purpose.</summary>
	public static string DescribeEnglish(RejectReason reason, string[] fields)
	{
		string key = KeyFor(reason, fields);
		return Texts.Fill(Texts.Default(key), ArgumentsFor(key, fields));
	}

	/// <summary>
	/// The placeholders for a sentence, chosen by the key rather than by the reason.
	///
	/// A mod mismatch has three possible sentences and each takes its own arguments: the single
	/// forms take one list, the combined one takes three. Passing all three to all of them puts the
	/// text in the wrong slot - the sentence then reads "you have mods this server does not run: "
	/// with nothing after the colon, which is exactly the kind of empty message this whole
	/// mechanism exists to avoid.
	/// </summary>
	private static object[] ArgumentsFor(string key, string[] fields)
	{
		if (key == "ui.reject.mods.missing")
		{
			return new object[1] { Clean(fields, 0) };
		}
		if (key == "ui.reject.mods.extra")
		{
			return new object[1] { Clean(fields, 1) };
		}
		if (key == "ui.reject.mods.version")
		{
			return new object[1] { Clean(fields, 2) };
		}
		if (key == "ui.reject.mods.mixed")
		{
			return new object[3] { Clean(fields, 0), Clean(fields, 1), Clean(fields, 2) };
		}
		return ObjectFields(fields);
	}

	/// <summary>The <c>Texts</c> key for a reason, so a test can check every code has one.</summary>
	internal static string KeyFor(RejectReason reason, string[] fields)
	{
		switch (reason)
		{
		case RejectReason.ProtocolMismatch:
			return "ui.reject.protocol";
		case RejectReason.ModMismatch:
			// Fields are: missing, extra, version mismatches - each a newline-separated list.
			// A host that does not send them (older build) gets the plain sentence.
			return ModKey(fields);
		case RejectReason.Banned:
			return "ui.reject.banned";
		case RejectReason.WrongPassword:
			return "ui.reject.password";
		case RejectReason.ServerFull:
			return "ui.reject.full";
		case RejectReason.NotWhitelisted:
			return "ui.reject.whitelist";
		case RejectReason.Kicked:
			return (fields != null && fields.Length != 0 && !string.IsNullOrEmpty(fields[0]))
				? "ui.reject.kicked.reason"
				: "ui.reject.kicked";
		default:
			// Unknown code or legacy text: the raw payload is the only thing there is to show.
			// An empty payload has nothing at all, which must still read as a sentence.
			return (fields != null && fields.Length != 0 && !string.IsNullOrEmpty(fields[0]))
				? "ui.reject.unknown"
				: "ui.reject.unknown.empty";
		}
	}

	/// <summary>
	/// Which mod-mismatch sentence to use: one reason is enough when only one kind of difference
	/// exists, and the combined sentence only when several do. "Missing: X; extra: ; versions: "
	/// with two empty slots is worse than plain speech.
	/// </summary>
	private static string ModKey(string[] fields)
	{
		if (fields == null || fields.Length == 0)
		{
			return "ui.reject.mods";
		}
		string missing = Clean(fields, 0);
		string extra = Clean(fields, 1);
		string version = Clean(fields, 2);
		int num = ((missing.Length != 0) ? 1 : 0) + ((extra.Length != 0) ? 1 : 0) + ((version.Length != 0) ? 1 : 0);
		if (num == 0)
		{
			return "ui.reject.mods";
		}
		if (num > 1)
		{
			return "ui.reject.mods.mixed";
		}
		if (missing.Length != 0)
		{
			return "ui.reject.mods.missing";
		}
		if (extra.Length != 0)
		{
			return "ui.reject.mods.extra";
		}
		return "ui.reject.mods.version";
	}

	/// <summary>
	/// Turns a newline-separated mod list into one readable phrase. Newline-separated because a
	/// mod name can contain a comma, and this is the only separator that cannot be typed into one.
	/// </summary>
	private static string Clean(string[] fields, int index)
	{
		if (fields == null || index >= fields.Length || string.IsNullOrEmpty(fields[index]))
		{
			return string.Empty;
		}
		string[] array = fields[index].Split('\n');
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < array.Length && i < 6; i++)
		{
			string text = array[i].Trim();
			if (text.Length == 0)
			{
				continue;
			}
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(", ");
			}
			stringBuilder.Append(text);
		}
		if (array.Length > 6)
		{
			stringBuilder.Append(", +").Append(array.Length - 6);
		}
		return stringBuilder.ToString();
	}

	private static object[] ObjectFields(string[] fields)
	{
		if (fields == null || fields.Length == 0)
		{
			return new object[0];
		}
		object[] array = new object[fields.Length];
		for (int i = 0; i < fields.Length; i++)
		{
			array[i] = fields[i];
		}
		return array;
	}

	/// <summary>Every code this build can send, for a test that checks the texts exist.</summary>
	internal static List<RejectReason> All
	{
		get
		{
			return new List<RejectReason>
			{
				RejectReason.ProtocolMismatch,
				RejectReason.ModMismatch,
				RejectReason.Banned,
				RejectReason.WrongPassword,
				RejectReason.ServerFull,
				RejectReason.NotWhitelisted,
				RejectReason.Kicked
			};
		}
	}
}
