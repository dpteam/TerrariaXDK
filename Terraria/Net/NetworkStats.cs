using System;
using System.Diagnostics;

namespace Terraria.Net;

/// <summary>
/// Lightweight connection diagnostics: round-trip time and traffic counters. Updated by
/// the transport threads and read by the HUD, so every field is written under a lock.
/// </summary>
internal static class NetworkStats
{
	private static readonly object Sync = new object();

	private static int _rttMs = -1;

	private static long _bytesSent;

	private static long _bytesReceived;

	private static long _packetsSent;

	private static long _packetsReceived;

	private static readonly Stopwatch Since = Stopwatch.StartNew();

	/// <summary>Last round-trip time in milliseconds, or -1 when unknown (not connected).</summary>
	public static int RttMs
	{
		get
		{
			lock (Sync)
			{
				return _rttMs;
			}
		}
	}

	public static long BytesSent
	{
		get
		{
			lock (Sync)
			{
				return _bytesSent;
			}
		}
	}

	public static long BytesReceived
	{
		get
		{
			lock (Sync)
			{
				return _bytesReceived;
			}
		}
	}

	public static long PacketsSent
	{
		get
		{
			lock (Sync)
			{
				return _packetsSent;
			}
		}
	}

	public static long PacketsReceived
	{
		get
		{
			lock (Sync)
			{
				return _packetsReceived;
			}
		}
	}

	/// <summary>Average outgoing kilobits per second since startup.</summary>
	public static float SentKbps
	{
		get
		{
			lock (Sync)
			{
				return Rate(_bytesSent);
			}
		}
	}

	/// <summary>Average incoming kilobits per second since startup.</summary>
	public static float ReceivedKbps
	{
		get
		{
			lock (Sync)
			{
				return Rate(_bytesReceived);
			}
		}
	}

	internal static void OnSent(int bytes)
	{
		lock (Sync)
		{
			_bytesSent += bytes;
			_packetsSent++;
		}
	}

	internal static void OnReceived(int bytes)
	{
		lock (Sync)
		{
			_bytesReceived += bytes;
			_packetsReceived++;
		}
	}

	internal static void SetRtt(int milliseconds)
	{
		lock (Sync)
		{
			// Smooth a little: a single spike should not make the HUD jump around.
			_rttMs = (_rttMs < 0) ? milliseconds : (_rttMs * 3 + milliseconds) / 4;
		}
	}

	/// <summary>Clears everything when a session ends or a new one starts.</summary>
	public static void Reset()
	{
		lock (Sync)
		{
			_rttMs = -1;
			_bytesSent = 0L;
			_bytesReceived = 0L;
			_packetsSent = 0L;
			_packetsReceived = 0L;
			Since.Restart();
			StartedTick = Environment.TickCount;
			HasStart = true;
		}
	}

	/// <summary>
	/// Environment.TickCount at the moment the session started. The stopwatch in
	/// <see cref="Since"/> measures rates, not total uptime, and a wall-clock comparison would
	/// jump if the system time changed - which is exactly what <c>!stats</c> must not do.
	/// </summary>
	internal static int StartedTick;

	/// <summary>True once a session has been started (StartedTick is meaningful).</summary>
	internal static bool HasStart;

	/// <summary>One-line summary for the HUD / log.</summary>
	public static string Describe()
	{
		lock (Sync)
		{
			string text = (_rttMs < 0) ? "--" : _rttMs.ToString();
			return "ping " + text + " ms   up " + Rate(_bytesSent).ToString("0.0") + " kbps   down " + Rate(_bytesReceived).ToString("0.0") + " kbps";
		}
	}

	private static float Rate(long bytes)
	{
		double totalSeconds = Since.Elapsed.TotalSeconds;
		if (totalSeconds < 0.5)
		{
			return 0f;
		}
		return (float)(bytes * 8.0 / 1000.0 / totalSeconds);
	}
}
