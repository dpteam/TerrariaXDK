using System;
using System.IO;

namespace Terraria.Net;

/// <summary>
/// Collects outgoing game data so a frame of gameplay leaves in one write instead of dozens.
///
/// Why this exists: every packet is length-prefixed and written with <c>NoDelay</c> set, so one
/// <see cref="System.Net.Sockets.TcpClient"/> write becomes its own TCP segment. A host sending
/// player states, tile edits, item drops and NPC updates to four clients does that many small
/// writes per tick - and each one carries a 40-byte header and its own acknowledgement
/// behaviour. Coalescing them into a single write per connection per tick costs nothing in
/// latency (the flush happens in the same tick the data was produced) and removes the overhead.
///
/// Only <c>OpData</c> is batched. Handshake, roster, reject and ping stay immediate, because
/// they are the frames something waits on, and a reject that is still sitting in a buffer when
/// the connection closes is a bug with no upside.
///
/// A buffered frame keeps its own length prefix: batching is concatenation, so the reader needs
/// no change at all - it still sees a stream of length-prefixed frames.
/// </summary>
internal sealed class PacketBufferer
{
	/// <summary>
	/// Beyond this the buffer is written immediately. A burst larger than this is not "one
	/// tick of gameplay" any more, and holding it would only add memory and latency.
	/// </summary>
	internal const int MaxPendingBytes = 32 * 1024;

	private readonly MemoryStream _pending = new MemoryStream();

	private readonly object _sync = new object();

	/// <summary>Bytes waiting to be written.</summary>
	public int PendingBytes
	{
		get
		{
			lock (_sync)
			{
				return (int)_pending.Length;
			}
		}
	}

	/// <summary>Frames taken in but not yet written.</summary>
	public int PendingFrames { get; private set; }

	/// <summary>
	/// Adds one already-framed packet. Returns true when the caller should write immediately
	/// because the buffer is full - the data is kept either way.
	/// </summary>
	public bool Add(byte[] frame)
	{
		lock (_sync)
		{
			_pending.Write(frame, 0, frame.Length);
			PendingFrames++;
			return _pending.Length >= MaxPendingBytes;
		}
	}

	/// <summary>
	/// Writes everything buffered as one call, and returns the number of bytes written (0 when
	/// there was nothing). On failure the buffer is dropped: a stream that throws once is dead,
	/// and retrying would just throw again on every frame.
	/// </summary>
	public int Flush(Stream stream, out int frames)
	{
		frames = 0;
		byte[] array;
		lock (_sync)
		{
			if (_pending.Length == 0)
			{
				return 0;
			}
			array = _pending.ToArray();
			frames = PendingFrames;
			_pending.SetLength(0L);
			PendingFrames = 0;
		}
		if (stream == null)
		{
			return 0;
		}
		try
		{
			WireFormat.WriteAll(stream, array);
		}
		catch
		{
			return 0;
		}
		return array.Length;
	}

	/// <summary>
	/// True for frames that may be held until the end of the tick. Everything else - handshake,
	/// roster, reject, ping - is a frame something is waiting on, so it goes out at once.
	///
	/// The opcode is passed in rather than duplicated here: the protocol's numbers are defined
	/// once in <see cref="TcpPeer"/>, and a second copy is a second thing to forget to change.
	/// </summary>
	internal static bool IsBatchable(byte[] frame, byte opcode)
	{
		// [int32 length][byte opcode][payload]
		return frame != null && frame.Length > WireFormat.HeaderSize
			&& frame[WireFormat.HeaderSize] == opcode;
	}

	/// <summary>Drops everything buffered, for a connection that is going away.</summary>
	public void Clear()
	{
		lock (_sync)
		{
			_pending.SetLength(0L);
			PendingFrames = 0;
		}
	}
}
