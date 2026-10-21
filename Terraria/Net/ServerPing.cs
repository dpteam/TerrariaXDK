using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Terraria.Net;

/// <summary>
/// Measures how long a listed server takes to answer, so the list can show a ping before the
/// player commits to joining.
///
/// How it works: open a second socket, send the same <c>Hello</c> the real client sends, and
/// time the first reply. That reply is <c>Welcome</c> on an accepting host and <c>Reject</c> on
/// one that refuses (wrong password, different mods, other protocol version) - either way we
/// reached it, so the round trip is a valid measurement. Treating a refusal as "no ping" would
/// make every passworded server look dead.
///
/// The probe never touches the live session: its own socket, its own short timeout, and it
/// closes before anything else happens. Results are cached per address and expire, so the list
/// does not accumulate measurements from a server that has since changed.
/// </summary>
internal static class ServerPing
{
	/// <summary>Hard timeout for one probe. Short: a list with ten dead hosts must not hang.</summary>
	public const int TimeoutMs = 1500;

	/// <summary>
	/// Test-only override for <see cref="TimeoutMs"/>. Zero means "use the real timeout"; the
	/// self-test lowers it so a dead-host probe does not add a second and a half to every run.
	/// </summary>
	internal static int TimeoutMsOverrideForTests;

	private static int EffectiveTimeout => (TimeoutMsOverrideForTests > 0) ? TimeoutMsOverrideForTests : TimeoutMs;

	/// <summary>How many probes may run at once. Keeps a big list from spawning dozens of sockets.</summary>
	private const int MaxConcurrent = 6;

	/// <summary>A cached measurement older than this is refreshed on the next list update.</summary>
	private const int FreshMs = 60000;

	private static readonly Dictionary<string, Entry> Results = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

	private static readonly object Sync = new object();

	private static readonly SemaphoreSlim Gate = new SemaphoreSlim(MaxConcurrent, MaxConcurrent);

	private sealed class Entry
	{
		public int Milliseconds;

		/// <summary>False when the host did not answer at all.</summary>
		public bool Reachable;

		public int MeasuredTick;
	}

	/// <summary>
	/// Starts a probe unless a fresh result already exists. Never blocks: the caller is the UI or
	/// the discovery thread, and neither may wait on the network.
	/// </summary>
	public static void MeasureAsync(IPEndPoint endPoint)
	{
		if (endPoint == null || endPoint.Port <= 0)
		{
			return;
		}
		string text = Key(endPoint);
		lock (Sync)
		{
			Entry entry;
			if (Results.TryGetValue(text, out entry) && IsFresh(entry))
			{
				return;
			}
		}
		IPEndPoint captured = new IPEndPoint(endPoint.Address, endPoint.Port);
		Thread thread = new Thread(delegate()
		{
			try
			{
				Gate.Wait();
				try
				{
					Probe(captured);
				}
				finally
				{
					Gate.Release();
				}
			}
			catch
			{
				// A probe must never take the process with it.
			}
		})
		{
			IsBackground = true,
			Name = "TerrariaXDK.Ping"
		};
		thread.Start();
	}

	/// <summary>Starts probes for a list of sessions, skipping ones already measured recently.</summary>
	public static void MeasureAllAsync(IEnumerable<IPEndPoint> endPoints)
	{
		if (endPoints == null)
		{
			return;
		}
		foreach (IPEndPoint endPoint in endPoints)
		{
			MeasureAsync(endPoint);
		}
	}

	/// <summary>
	/// Cached result for this address. <paramref name="reachable"/> is false when the last probe
	/// timed out; <paramref name="milliseconds"/> is then negative. Returns false when nothing has
	/// been measured yet, which the UI draws as "unknown" rather than "dead".
	/// </summary>
	public static bool TryGet(IPEndPoint endPoint, out int milliseconds, out bool reachable)
	{
		milliseconds = -1;
		reachable = false;
		if (endPoint == null)
		{
			return false;
		}
		lock (Sync)
		{
			Entry entry;
			if (!Results.TryGetValue(Key(endPoint), out entry))
			{
				return false;
			}
			milliseconds = entry.Milliseconds;
			reachable = entry.Reachable;
			return true;
		}
	}

	/// <summary>Short label for a list row: "42 ms", "timeout", or an empty string if unmeasured.</summary>
	public static string Label(IPEndPoint endPoint)
	{
		int num;
		bool flag;
		if (!TryGet(endPoint, out num, out flag))
		{
			return string.Empty;
		}
		return flag ? (num + " ms") : "timeout";
	}

	/// <summary>Forgets everything, e.g. when the list is emptied on a fresh search.</summary>
	public static void Clear()
	{
		lock (Sync)
		{
			Results.Clear();
		}
	}

	/// <summary>
	/// One probe: connect, greet, read one frame, close. Any failure means "did not answer", which
	/// is a legitimate result and not an error to report.
	/// </summary>
	internal static void Probe(IPEndPoint endPoint)
	{
		int num = -1;
		bool flag = false;
		TcpClient tcpClient = null;
		try
		{
			tcpClient = new TcpClient();
			tcpClient.NoDelay = true;
			DateTime utcNow = DateTime.UtcNow;
			int timeout = EffectiveTimeout;
			if (!ConnectWithTimeout(tcpClient, endPoint, timeout))
			{
				Store(endPoint, -1, false);
				return;
			}
			NetworkStream stream = tcpClient.GetStream();
			stream.ReadTimeout = timeout;
			stream.WriteTimeout = timeout;
			byte[] array = BuildHello();
			WireFormat.WriteAll(stream, array);
			byte[] array2 = WireFormat.ReadFrame(stream);
			if (array2 != null && array2.Length > 0)
			{
				// Welcome or Reject: both prove the host is alive and answering.
				num = (int)(DateTime.UtcNow - utcNow).TotalMilliseconds;
				flag = true;
			}
		}
		catch
		{
			flag = false;
			num = -1;
		}
		finally
		{
			try
			{
				tcpClient?.Close();
			}
			catch
			{
			}
		}
		Store(endPoint, num, flag);
	}

	/// <summary>
	/// The same greeting the real client sends. The mod signature is this machine's, so a host
	/// with different mods answers with Reject - still a measurement, and it also means the list
	/// can show a ping for a server it would refuse to join.
	/// </summary>
	internal static byte[] BuildHello()
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8))
		{
			binaryWriter.Write(TcpPeer.OpHello);
			binaryWriter.Write(TcpPeer.ProtocolVersionPublic);
			binaryWriter.Write(Terraria.PlayerIdentity.Current);
			binaryWriter.Write(Terraria.Mods.ModLoader.Signature);
			binaryWriter.Write(NetworkConfig.Password ?? string.Empty);
		}
		// BinaryWriter closes the stream it was given; ToArray() is still valid afterwards.
		return WireFormat.LengthPrefix(memoryStream.ToArray());
	}

	private static bool ConnectWithTimeout(TcpClient client, IPEndPoint endPoint, int timeoutMs)
	{
		IAsyncResult asyncResult = client.BeginConnect(endPoint.Address, endPoint.Port, null, null);
		if (!asyncResult.AsyncWaitHandle.WaitOne(timeoutMs))
		{
			return false;
		}
		client.EndConnect(asyncResult);
		return client.Connected;
	}

	private static void Store(IPEndPoint endPoint, int milliseconds, bool reachable)
	{
		lock (Sync)
		{
			Entry entry = new Entry();
			entry.Milliseconds = milliseconds;
			entry.Reachable = reachable;
			entry.MeasuredTick = Environment.TickCount;
			Results[Key(endPoint)] = entry;
		}
	}

	private static bool IsFresh(Entry entry)
	{
		return entry != null && unchecked((uint)(Environment.TickCount - entry.MeasuredTick)) < (uint)FreshMs;
	}

	private static string Key(IPEndPoint endPoint)
	{
		return endPoint.Address + ":" + endPoint.Port;
	}
}
