using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terraria.Mods;

/// <summary>Sub-opcodes inside one <c>OpModFile</c> frame.</summary>
internal enum ModFileOp : byte
{
	/// <summary>Client: send me this mod.</summary>
	Request = 1,

	/// <summary>Host: here is what I am about to send - name, file count, size, content hash.</summary>
	Begin = 2,

	/// <summary>Host: this many bytes of this file, at this offset.</summary>
	Chunk = 3,

	/// <summary>Host: that is all, and the hash is still what <see cref="Begin"/> said.</summary>
	End = 4,

	/// <summary>Host: I will not send this one, and here is why.</summary>
	Refused = 5
}

/// <summary>One decoded <c>OpModFile</c> frame. Only the fields the opcode defines are set.</summary>
internal sealed class ModFileFrame
{
	public ModFileOp Op;

	public string ModName = "";

	public string Hash = "";

	public string Reason = "";

	public string Path = "";

	public long Offset;

	public int FileCount;

	/// <summary>The files the host announced in <see cref="ModFileOp.Begin"/>.</summary>
	public List<string> Files;

	public long TotalBytes;

	public byte[] Data;

	public int DataOffset;

	public int DataCount;
}

/// <summary>
/// Moving a mod folder from a host to a client.
///
/// Written as two functions over <see cref="Stream"/> rather than as socket code, for one
/// reason: this is the part that has to be right - path validation, size limits, hash checking -
/// and a stream pair in a test exercises exactly the same bytes as a socket does. The transport
/// only supplies the two streams.
///
/// The flow is request-driven. The host never pushes a file the client did not ask for by name,
/// and only serves names from its own loaded set, so a client cannot ask for a path.
///
/// A host must call this only after the client has passed the ban, password and whitelist checks:
/// these frames carry executable content, and "anonymous but connected" is not a reason to hand
/// it over.
/// </summary>
internal static class ModTransfer
{
	/// <summary>Most files one mod may consist of. Matches <see cref="ModFiles.MaxFileCount"/>.</summary>
	internal const int MaxFiles = ModFiles.MaxFileCount;

	/// <summary>Most bytes one mod may consist of. Matches <see cref="ModFiles.MaxBytes"/>.</summary>
	internal const int MaxBytes = ModFiles.MaxBytes;

	// --- framing ------------------------------------------------------------

	internal static byte[] Request(string modName)
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
		binaryWriter.Write(Terraria.Net.TcpPeer.OpModFile);
		binaryWriter.Write((byte)ModFileOp.Request);
		binaryWriter.Write(modName ?? "");
		binaryWriter.Flush();
		return memoryStream.ToArray();
	}

	internal static byte[] Begin(string modName, string[] files, long totalBytes, string hash)
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
		binaryWriter.Write(Terraria.Net.TcpPeer.OpModFile);
		binaryWriter.Write((byte)ModFileOp.Begin);
		binaryWriter.Write(modName ?? "");
		binaryWriter.Write(files.Length);
		for (int i = 0; i < files.Length; i++)
		{
			binaryWriter.Write(files[i] ?? "");
		}
		binaryWriter.Write(totalBytes);
		binaryWriter.Write(hash ?? "");
		binaryWriter.Flush();
		return memoryStream.ToArray();
	}

	internal static byte[] Chunk(string relativePath, long offset, byte[] data, int dataOffset, int count)
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
		binaryWriter.Write(Terraria.Net.TcpPeer.OpModFile);
		binaryWriter.Write((byte)ModFileOp.Chunk);
		binaryWriter.Write(relativePath ?? "");
		binaryWriter.Write(offset);
		binaryWriter.Write(count);
		binaryWriter.Write(data, dataOffset, count);
		binaryWriter.Flush();
		return memoryStream.ToArray();
	}

	internal static byte[] End(string modName, string hash)
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
		binaryWriter.Write(Terraria.Net.TcpPeer.OpModFile);
		binaryWriter.Write((byte)ModFileOp.End);
		binaryWriter.Write(modName ?? "");
		binaryWriter.Write(hash ?? "");
		binaryWriter.Flush();
		return memoryStream.ToArray();
	}

	internal static byte[] Refused(string modName, string reason)
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
		binaryWriter.Write(Terraria.Net.TcpPeer.OpModFile);
		binaryWriter.Write((byte)ModFileOp.Refused);
		binaryWriter.Write(modName ?? "");
		binaryWriter.Write(reason ?? "");
		binaryWriter.Flush();
		return memoryStream.ToArray();
	}

	/// <summary>
	/// Decodes a frame as it came off the wire: <c>[OpModFile][sub-op][fields]</c>.
	///
	/// Returns false for anything malformed - these frames come from another machine, and a
	/// truncated one must be a dropped frame, not an exception.
	/// </summary>
	internal static bool TryParse(byte[] payload, out ModFileFrame frame)
	{
		frame = null;
		if (payload == null || payload.Length < 3 || payload[0] != Terraria.Net.TcpPeer.OpModFile)
		{
			return false;
		}
		try
		{
			// The transport opcode and the sub-opcode are both consumed here, so every offset
			// computed below is shifted by two.
			using MemoryStream memoryStream = new MemoryStream(payload, 2, payload.Length - 2);
			BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);
			ModFileFrame modFileFrame = new ModFileFrame();
			modFileFrame.Op = (ModFileOp)payload[1];
			switch (modFileFrame.Op)
			{
			case ModFileOp.Request:
				modFileFrame.ModName = binaryReader.ReadString();
				break;
			case ModFileOp.Begin:
				modFileFrame.ModName = binaryReader.ReadString();
				modFileFrame.FileCount = binaryReader.ReadInt32();
				if (modFileFrame.FileCount < 0 || modFileFrame.FileCount > MaxFiles)
				{
					return false;
				}
				modFileFrame.Files = new List<string>(modFileFrame.FileCount);
				for (int i = 0; i < modFileFrame.FileCount; i++)
				{
					string text = binaryReader.ReadString();
					// The manifest is the contract: a path outside it is not written later, so
					// there is no reason to accept it now.
					if (!ModFiles.IsSafeRelativePath(text))
					{
						return false;
					}
					modFileFrame.Files.Add(text);
				}
				modFileFrame.TotalBytes = binaryReader.ReadInt64();
				modFileFrame.Hash = binaryReader.ReadString();
				if (modFileFrame.TotalBytes < 0L || modFileFrame.TotalBytes > MaxBytes)
				{
					return false;
				}
				break;
			case ModFileOp.Chunk:
				modFileFrame.Path = binaryReader.ReadString();
				modFileFrame.Offset = binaryReader.ReadInt64();
				modFileFrame.DataCount = binaryReader.ReadInt32();
				if (modFileFrame.Offset < 0L || modFileFrame.DataCount <= 0
					|| modFileFrame.DataCount > ModFiles.ChunkSize
					|| modFileFrame.DataCount > memoryStream.Length - memoryStream.Position)
				{
					return false;
				}
				// The payload the parser was handed starts two bytes in, so every offset into the
				// MemoryStream has to be shifted back by that much.
				modFileFrame.DataOffset = 2 + (int)memoryStream.Position;
				modFileFrame.Data = payload;
				// Consume the payload so a trailing-garbage check is possible below.
				memoryStream.Position += modFileFrame.DataCount;
				break;
			case ModFileOp.End:
				modFileFrame.ModName = binaryReader.ReadString();
				modFileFrame.Hash = binaryReader.ReadString();
				break;
			case ModFileOp.Refused:
				modFileFrame.ModName = binaryReader.ReadString();
				modFileFrame.Reason = binaryReader.ReadString();
				break;
			default:
				return false;
			}
			if (memoryStream.Position != memoryStream.Length)
			{
				// Trailing bytes mean the frame is not what it claims; refusing it is cheaper than
				// reasoning about which half to trust.
				return false;
			}
			frame = modFileFrame;
			return true;
		}
		catch (Exception)
		{
			frame = null;
			return false;
		}
	}

	// --- host side ----------------------------------------------------------

	/// <summary>What one serve pass did, for the log and the tests.</summary>
	internal sealed class ServeResult
	{
		public int ModsSent;

		public long BytesSent;

		public readonly List<string> Refused = new List<string>();

		public string Error;
	}

	/// <summary>
	/// Serves requested mods until the client stops asking or the stream ends.
	///
	/// <paramref name="folderOf"/> resolves a mod name to the folder the host would send, or null
	/// when the host does not have that mod. Nothing here trusts the client's name beyond looking
	/// it up: the files always come from the host's own set.
	/// </summary>
	internal static ServeResult Serve(Stream input, Stream output, Func<string, string> folderOf)
	{
		ServeResult serveResult = new ServeResult();
		byte[] array = new byte[8192];
		while (true)
		{
			byte[] frame;
			try
			{
				frame = Terraria.Net.WireFormat.ReadFrame(input);
			}
			catch (Exception ex)
			{
				serveResult.Error = ex.Message;
				return serveResult;
			}
			if (frame == null)
			{
				// The client closed, or the frame was corrupt. Either way there is nobody to send to.
				return serveResult;
			}
			ModFileFrame modFileFrame;
			if (!TryParse(frame, out modFileFrame) || modFileFrame.Op != ModFileOp.Request)
			{
				serveResult.Error = "unexpected frame";
				return serveResult;
			}
			string text = ModFiles.SafeFolderName(modFileFrame.ModName);
			string text2 = (text.Length == 0) ? null : folderOf(text);
			if (text2 == null)
			{
				Write(output, Refused(modFileFrame.ModName, "unknown"));
				serveResult.Refused.Add(modFileFrame.ModName);
				continue;
			}
			string error;
			if (!SendOne(output, text, text2, array, serveResult, out error))
			{
				serveResult.Error = error;
				return serveResult;
			}
		}
	}

	private static bool SendOne(Stream output, string modName, string folder, byte[] buffer,
		ServeResult result, out string error)
	{
		error = null;
		string[] array = ModFiles.List(folder, out error);
		if (error != null)
		{
			Write(output, Refused(modName, error));
			result.Refused.Add(modName);
			return true;
		}
		long num = ModFiles.TotalSize(folder);
		string text = ModFiles.ContentHash(folder);
		if (num < 0L || num > MaxBytes)
		{
			Write(output, Refused(modName, "too large"));
			result.Refused.Add(modName);
			return true;
		}
		if (!Write(output, Begin(modName, array, num, text)))
		{
			error = "connection lost";
			return false;
		}
		long num2 = 0L;
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = Path.Combine(folder, array[i].Replace('/', Path.DirectorySeparatorChar));
			try
			{
				using FileStream stream = File.OpenRead(text2);
				long num3 = 0L;
				int num4;
				while ((num4 = stream.Read(buffer, 0, buffer.Length)) > 0)
				{
					int num5 = 0;
					while (num5 < num4)
					{
						int count = Math.Min(ModFiles.ChunkSize, num4 - num5);
						if (!Write(output, Chunk(array[i], num3, buffer, num5, count)))
						{
							error = "connection lost";
							return false;
						}
						num3 += count;
						num5 += count;
						num2 += count;
					}
				}
			}
			catch (Exception ex)
			{
				// A file that vanished between the listing and the read: say so and stop this mod
				// rather than sending a short file the client would refuse anyway.
				Write(output, Refused(modName, "cannot read " + array[i] + ": " + ex.Message));
				result.Refused.Add(modName);
				return true;
			}
		}
		if (!Write(output, End(modName, text)))
		{
			error = "connection lost";
			return false;
		}
		result.ModsSent++;
		result.BytesSent += num2;
		return true;
	}

	// --- client side --------------------------------------------------------

	/// <summary>What one download pass did.</summary>
	internal sealed class ReceiveResult
	{
		/// <summary>Mods that arrived and matched the hash the host promised.</summary>
		public readonly List<string> Received = new List<string>();

		/// <summary>Mods that did not arrive: refused, mismatched, or cut off.</summary>
		public readonly List<string> Failed = new List<string>();

		public long BytesReceived;

		/// <summary>Why the last one failed, for the log.</summary>
		public string Error;
	}

	/// <summary>
	/// Asks for <paramref name="wanted"/> and writes what arrives into
	/// <c>Storage/Mods/.incoming/&lt;Name&gt;</c>, one mod at a time.
	///
	/// Nothing is installed here: a caller that wants the mods in place calls
	/// <see cref="ModFiles.Install"/> for each name in <see cref="ReceiveResult.Received"/>. That
	/// separation is what keeps "a half-written folder is not a mod" true even if the process dies
	/// mid-download.
	/// </summary>
	internal static ReceiveResult Receive(Stream input, Stream output, IEnumerable<string> wanted)
	{
		ReceiveResult receiveResult = new ReceiveResult();
		foreach (string item in wanted)
		{
			string text = ModFiles.SafeFolderName(item);
			if (text.Length == 0)
			{
				receiveResult.Failed.Add(item);
				continue;
			}
			// A previous attempt may have left a partial folder behind; starting from a clean one
			// is what makes the hash check meaningful.
			ModFiles.DiscardIncoming(text);
			if (!Write(output, Request(text)))
			{
				receiveResult.Error = "connection lost";
				receiveResult.Failed.Add(text);
				return receiveResult;
			}
			if (!ReceiveOne(input, text, receiveResult))
			{
				return receiveResult;
			}
		}
		return receiveResult;
	}

	private static bool ReceiveOne(Stream input, string modName, ReceiveResult result)
	{
		long num = 0L;
		string text = null;
		int num2 = 0;
		string text2 = null;
		HashSet<string> hashSet = null;
		while (true)
		{
			byte[] frame;
			try
			{
				frame = Terraria.Net.WireFormat.ReadFrame(input);
			}
			catch (Exception ex)
			{
				result.Error = ex.Message;
				result.Failed.Add(modName);
				return false;
			}
			if (frame == null)
			{
				result.Error = "connection closed";
				result.Failed.Add(modName);
				return false;
			}
			ModFileFrame modFileFrame;
			if (!TryParse(frame, out modFileFrame))
			{
				result.Error = "malformed frame";
				ModFiles.DiscardIncoming(modName);
				result.Failed.Add(modName);
				return false;
			}
			// Only the frames that carry a mod name are checked against the mod being received:
			// a chunk carries a file path instead, and checking its (empty) name would reject
			// every transfer.
			if (modFileFrame.Op != ModFileOp.Chunk
				&& !string.Equals(modFileFrame.ModName, modName, StringComparison.OrdinalIgnoreCase))
			{
				// One mod at a time, so a frame for another name means the stream is out of step.
				result.Error = "unexpected mod '" + modFileFrame.ModName + "' (op " + modFileFrame.Op + ")";
				ModFiles.DiscardIncoming(modName);
				result.Failed.Add(modName);
				return false;
			}
			switch (modFileFrame.Op)
			{
			case ModFileOp.Refused:
				result.Error = modFileFrame.Reason;
				result.Failed.Add(modName);
				return true;
			case ModFileOp.Begin:
			{
				text = modFileFrame.Hash;
				num2 = modFileFrame.FileCount;
				hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				string error;
				for (int i = 0; i < modFileFrame.Files.Count; i++)
				{
					hashSet.Add(modFileFrame.Files[i]);
					// Create every announced file now, so a zero-byte file exists and the content
					// hash the client computes afterwards matches the one the host computed.
					if (!ModFiles.TouchReceived(modName, modFileFrame.Files[i], out error))
					{
						result.Error = error;
						ModFiles.DiscardIncoming(modName);
						result.Failed.Add(modName);
						return true;
					}
				}
				break;
			}
			case ModFileOp.Chunk:
			{
				if (hashSet == null || !hashSet.Contains(modFileFrame.Path))
				{
					// A file the host did not announce. Writing it would make the downloaded folder
					// differ from the one whose hash was promised.
					result.Error = "unannounced file '" + modFileFrame.Path + "'";
					ModFiles.DiscardIncoming(modName);
					result.Failed.Add(modName);
					return true;
				}
				string error;
				if (!ModFiles.WriteReceived(modName, modFileFrame.Path, modFileFrame.Data,
					modFileFrame.DataOffset, modFileFrame.DataCount, out error))
				{
					result.Error = error;
					ModFiles.DiscardIncoming(modName);
					result.Failed.Add(modName);
					return true;
				}
				num += modFileFrame.DataCount;
				if (num > MaxBytes)
				{
					// The host is sending more than it announced a mod may be. Stop reading it
					// rather than filling the disk.
					result.Error = "mod exceeded the size limit";
					ModFiles.DiscardIncoming(modName);
					result.Failed.Add(modName);
					return false;
				}
				break;
			}
			case ModFileOp.End:
				text2 = modFileFrame.Hash;
				if (num2 <= 0)
				{
					result.Error = "the host never said what it was sending";
					ModFiles.DiscardIncoming(modName);
					result.Failed.Add(modName);
					return true;
				}
				if (!string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
				{
					result.Error = "the hash changed mid-transfer";
					ModFiles.DiscardIncoming(modName);
					result.Failed.Add(modName);
					return true;
				}
				// Verify what actually landed on disk against the hash the host promised *before*
				// the transfer started: a truncated or rewritten file must not become a mod.
				string text3 = ModFiles.ContentHash(ModFiles.IncomingDirectory(modName));
				if (!string.Equals(text3, text, StringComparison.OrdinalIgnoreCase))
				{
					result.Error = "hash mismatch (expected " + text + ", got " + text3 + ")";
					ModFiles.DiscardIncoming(modName);
					result.Failed.Add(modName);
					return true;
				}
				result.BytesReceived += num;
				result.Received.Add(modName);
				return true;
			default:
				result.Error = "unexpected frame " + modFileFrame.Op;
				ModFiles.DiscardIncoming(modName);
				result.Failed.Add(modName);
				return false;
			}
		}
	}

	// --- plumbing -----------------------------------------------------------

	private static bool Write(Stream output, byte[] payload)
	{
		try
		{
			byte[] array = Terraria.Net.WireFormat.LengthPrefix(payload);
			lock (output)
			{
				output.Write(array, 0, array.Length);
				output.Flush();
			}
			return true;
		}
		catch
		{
			return false;
		}
	}
}
