using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Terraria.Net;

/// <summary>
/// Hosting and joining through the master server's relay, for players who cannot be reached from
/// the outside at all.
///
/// Port forwarding needs an address to forward to, and behind carrier-grade NAT, a mobile
/// connection or a guest network there is none. UPnP cannot help either: there is nothing to ask
/// the router for. An <b>outbound</b> connection always works, so both sides dial the relay and it
/// copies bytes between them.
///
/// The relay never parses a packet - <see cref="TcpPeer"/> already frames its stream - so the
/// handshake (protocol version, mod signature, password, ban list) stays end to end between host
/// and client. A relay cannot impersonate either side, and it needs no update when the game's
/// protocol changes.
///
/// Protocol (one UTF-8 line per command, '\n' terminated):
/// <code>
/// HOST &lt;token&gt;    -> "OK &lt;roomId&gt;"   the socket becomes the room's presence
/// JOIN &lt;roomId&gt;   -> "WAIT"               the socket is parked for the host
/// POLL &lt;token&gt;    -> "JOIN &lt;ticket&gt;" ... "END"
/// ACCEPT &lt;ticket&gt;  the relay pairs this socket with the waiting client
/// </code>
///
/// Opt-in: <c>[Network] AllowRelay=True</c>. Hosting directly keeps working and is always tried
/// first, because a relay costs a hop of latency and somebody else's bandwidth.
/// </summary>
internal static class RelayLink
{
	/// <summary>How often the host asks the relay for waiting players.</summary>
	private const int PollMs = 750;

	/// <summary>Connect timeout for the relay, matching the direct path's.</summary>
	private const int ConnectTimeoutMs = 5000;

	private static readonly object Sync = new object();

	/// <summary>The room this process is hosting through, or null when not relaying.</summary>
	private static string _hostRoom;

	/// <summary>The secret that proves we own the room. Never leaves this process.</summary>
	private static string _hostToken;

	/// <summary>The presence socket: closing it tells the relay the room is gone.</summary>
	private static TcpClient _presence;

	private static Thread _pump;

	private static volatile bool _stopping;

	/// <summary>Room id this host is reachable under, or null. Read by the publisher.</summary>
	public static string HostRoom
	{
		get
		{
			lock (Sync)
			{
				return _hostRoom;
			}
		}
	}

	/// <summary>
	/// The token of the room this process opened. Only used by the self-test, which has to poll its
	/// own room to prove the pairing works; nothing in the game reads it.
	/// </summary>
	internal static string CurrentToken
	{
		get
		{
			lock (Sync)
			{
				return _hostToken;
			}
		}
	}

	/// <summary>True while a relay room is open.</summary>
	public static bool IsHosting => HostRoom != null;

	/// <summary>
	/// The relay endpoint to use: <c>[Network] RelayServer</c> when set, otherwise the master's
	/// host with the conventional relay port. Empty when relaying is disabled.
	/// </summary>
	public static bool TryGetEndpoint(out string Host, out int Port)
	{
		Host = null;
		Port = 0;
		if (!NetworkConfig.AllowRelay)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(NetworkConfig.RelayServer))
		{
			return NetworkConfig.TryParseAddress(NetworkConfig.RelayServer, NetworkConfig.MasterServerPort + 1, out Host, out Port);
		}
		if (string.IsNullOrEmpty(NetworkConfig.MasterServer))
		{
			return false;
		}
		if (!NetworkConfig.TryParseAddress(NetworkConfig.MasterServer, NetworkConfig.MasterServerPort, out Host, out int num))
		{
			return false;
		}
		// The relay listens next to the list by convention: one machine, two ports, so an operator
		// does not have to publish two addresses.
		Port = NetworkConfig.RelayPort;
		return true;
	}

	/// <summary>
	/// Opens a room and keeps it open. Returns the room id, or null with a reason.
	///
	/// Called from the host's start path, before the world is announced, so the announcement can
	/// carry the room id and clients can find it.
	/// </summary>
	public static string Register(out string Error)
	{
		Error = null;
		string host;
		int port;
		if (!TryGetEndpoint(out host, out port))
		{
			Error = "relay is disabled";
			return null;
		}
		string text = NewToken();
		TcpClient tcpClient;
		try
		{
			tcpClient = Dial(host, port);
		}
		catch (Exception ex)
		{
			Error = "could not reach the relay at " + host + ":" + port + ": " + ex.Message;
			return null;
		}
		try
		{
			Send(tcpClient, HostLine(text));
			string text2 = ReadLine(tcpClient);
			string text3;
			if (!TryParseRoom(text2, out text3))
			{
				Error = "the relay refused the room: " + (text2 ?? "no answer");
				tcpClient.Close();
				return null;
			}
			lock (Sync)
			{
				_hostRoom = text3;
				_hostToken = text;
				_presence = tcpClient;
				_stopping = false;
			}
			Program.LogInfo("Relay", "room " + text3 + " opened at " + host + ":" + port);
			return text3;
		}
		catch (Exception ex2)
		{
			Error = "the relay did not answer: " + ex2.Message;
			try
			{
				tcpClient.Close();
			}
			catch
			{
			}
			return null;
		}
	}

	/// <summary>
	/// Starts the loop that collects waiting players. Each ticket becomes one more accepted
	/// connection, so a relayed session supports the same number of players as a direct one -
	/// the relay pairs sockets one to one instead of multiplexing.
	/// </summary>
	public static void StartPump(TcpPeer Peer)
	{
		if (Peer == null)
		{
			return;
		}
		string host;
		int port;
		if (!TryGetEndpoint(out host, out port))
		{
			return;
		}
		_pump = new Thread(delegate()
		{
			PumpLoop(Peer, host, port);
		});
		_pump.IsBackground = true;
		_pump.Name = "TerrariaXDK.RelayPump";
		_pump.Start();
	}

	private static void PumpLoop(TcpPeer Peer, string Host, int Port)
	{
		while (!_stopping)
		{
			try
			{
				Thread.Sleep(PollMs);
				if (_stopping)
				{
					return;
				}
				string token;
				lock (Sync)
				{
					token = _hostToken;
				}
				if (token == null)
				{
					return;
				}
				string error;
				List<string> list = Poll(Host, Port, token, out error);
				if (error != null)
				{
					Program.LogDebug("Relay", "poll failed: " + error);
					continue;
				}
				for (int i = 0; i < list.Count; i++)
				{
					AcceptTicket(Peer, Host, Port, list[i]);
				}
			}
			catch (Exception ex)
			{
				Program.LogDebug("Relay", "poll failed: " + ex.Message);
			}
		}
	}

	/// <summary>
	/// Asks the relay for waiting players and returns their tickets.
	///
	/// Split out of the pump so the protocol can be exercised without a <see cref="TcpPeer"/>:
	/// the interesting part is the conversation, not the loop around it.
	/// </summary>
	internal static List<string> Poll(string Host, int Port, string Token, out string Error)
	{
		Error = null;
		List<string> list = new List<string>();
		TcpClient tcpClient;
		try
		{
			tcpClient = Dial(Host, Port);
		}
		catch (Exception ex)
		{
			Error = ex.Message;
			return list;
		}
		try
		{
			Send(tcpClient, "POLL " + Token + "\n");
			string text;
			while ((text = ReadLine(tcpClient)) != null)
			{
				if (IsEnd(text))
				{
					break;
				}
				string text2;
				if (TryParseTicket(text, out text2))
				{
					list.Add(text2);
				}
			}
			return list;
		}
		catch (Exception ex2)
		{
			Error = ex2.Message;
			return list;
		}
		finally
		{
			try
			{
				tcpClient.Close();
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// Dials back with one ticket. The relay pairs this socket with the waiting client and from
	/// here on it is a plain TCP stream, which is exactly what the host's accept path expects.
	/// </summary>
	private static void AcceptTicket(TcpPeer Peer, string Host, int Port, string Ticket)
	{
		TcpClient tcpClient = AcceptRaw(Host, Port, Ticket);
		if (tcpClient == null)
		{
			return;
		}
		Program.LogInfo("Relay", "accepted a relayed player");
		Peer.AdoptRelaySocket(tcpClient);
	}

	/// <summary>
	/// Dials back with one ticket and returns the paired socket, or null.
	///
	/// The returned socket is already connected to the waiting player: the relay has joined the
	/// two, so from here it is an ordinary TCP stream. Split out of <see cref="AcceptTicket"/> so
	/// the pairing can be exercised without a session.
	/// </summary>
	internal static TcpClient AcceptRaw(string Host, int Port, string Ticket)
	{
		try
		{
			TcpClient tcpClient = Dial(Host, Port);
			Send(tcpClient, "ACCEPT " + Ticket + "\n");
			return tcpClient;
		}
		catch (Exception ex)
		{
			Program.LogDebug("Relay", "accept failed: " + ex.Message);
			return null;
		}
	}

	/// <summary>
	/// Joins a relayed room. Returns a connected socket positioned right after the relay's
	/// "WAIT", so the caller can speak the game's handshake on it as if it had dialled the host.
	/// </summary>
	public static TcpClient Join(string RelayHost, int RelayPort, string Room, out string Error)
	{
		Error = null;
		if (string.IsNullOrEmpty(Room))
		{
			Error = "no room";
			return null;
		}
		TcpClient tcpClient;
		try
		{
			tcpClient = Dial(RelayHost, RelayPort);
		}
		catch (Exception ex)
		{
			Error = "could not reach the relay: " + ex.Message;
			return null;
		}
		try
		{
			Send(tcpClient, JoinLine(Room));
			string text = ReadLine(tcpClient);
			string text2;
			if (!IsWait(text, out text2))
			{
				Error = "the relay refused the room: " + (text2 ?? "no answer");
				tcpClient.Close();
				return null;
			}
			// "WAIT" means the host has not accepted yet; the socket is live and will start
			// carrying game data the moment it does. The handshake below simply waits for it,
			// which is why the connect timeout covers this too.
			return tcpClient;
		}
		catch (Exception ex2)
		{
			Error = "the relay did not answer: " + ex2.Message;
			try
			{
				tcpClient.Close();
			}
			catch
			{
			}
			return null;
		}
	}

	/// <summary>Closes the room. Called from the host's shutdown.</summary>
	public static void Stop()
	{
		TcpClient tcpClient;
		lock (Sync)
		{
			_stopping = true;
			tcpClient = _presence;
			_presence = null;
			_hostRoom = null;
			_hostToken = null;
		}
		if (tcpClient != null)
		{
			try
			{
				tcpClient.Close();
			}
			catch
			{
			}
		}
	}

	// --- protocol helpers (pure, so the self-test can check them) -------------

	/// <summary>The HOST command line, including the newline the relay expects.</summary>
	internal static string HostLine(string Token)
	{
		return "HOST " + Token + "\n";
	}

	/// <summary>The JOIN command line.</summary>
	internal static string JoinLine(string Room)
	{
		return "JOIN " + Room + "\n";
	}

	/// <summary>Parses "OK &lt;roomId&gt;".</summary>
	internal static bool TryParseRoom(string Line, out string Room)
	{
		Room = null;
		if (Line == null || !Line.StartsWith("OK ", StringComparison.Ordinal))
		{
			return false;
		}
		string text = Line.Substring(3).Trim();
		if (text.Length == 0)
		{
			return false;
		}
		Room = text;
		return true;
	}

	/// <summary>Parses "JOIN &lt;ticket&gt;" from a poll answer.</summary>
	internal static bool TryParseTicket(string Line, out string Ticket)
	{
		Ticket = null;
		if (Line == null || !Line.StartsWith("JOIN ", StringComparison.Ordinal))
		{
			return false;
		}
		string text = Line.Substring(5).Trim();
		if (text.Length == 0)
		{
			return false;
		}
		Ticket = text;
		return true;
	}

	/// <summary>True for the "END" that closes a poll answer.</summary>
	internal static bool IsEnd(string Line)
	{
		return Line != null && Line.Trim().Equals("END", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// True for the "WAIT" that means "parked, the host has not accepted yet". A refusal comes
	/// back as "ERR ..." and is reported as the reason.
	/// </summary>
	internal static bool IsWait(string Line, out string Reason)
	{
		Reason = null;
		if (Line == null)
		{
			return false;
		}
		string text = Line.Trim();
		if (text.Equals("WAIT", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (text.StartsWith("ERR ", StringComparison.OrdinalIgnoreCase))
		{
			Reason = text.Substring(4).Trim();
		}
		else
		{
			Reason = text;
		}
		return false;
	}

	/// <summary>
	/// 128 bits of randomness, from the cryptographic generator: a guessable token would let
	/// somebody else take over the room, and a guessable ticket would let them attach to a
	/// stranger's session.
	/// </summary>
	internal static string NewToken()
	{
		byte[] array = new byte[16];
		using (System.Security.Cryptography.RandomNumberGenerator randomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator.Create())
		{
			randomNumberGenerator.GetBytes(array);
		}
		StringBuilder stringBuilder = new StringBuilder(32);
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.Append(array[i].ToString("x2", CultureInfo.InvariantCulture));
		}
		return stringBuilder.ToString();
	}

	// --- socket helpers -------------------------------------------------------

	private static TcpClient Dial(string Host, int Port)
	{
		TcpClient tcpClient = new TcpClient();
		tcpClient.NoDelay = true;
		tcpClient.ReceiveTimeout = ConnectTimeoutMs;
		IAsyncResult asyncResult = tcpClient.BeginConnect(Host, Port, null, null);
		if (!asyncResult.AsyncWaitHandle.WaitOne(ConnectTimeoutMs))
		{
			tcpClient.Close();
			throw new TimeoutException("timed out after " + ConnectTimeoutMs + " ms");
		}
		try
		{
			tcpClient.EndConnect(asyncResult);
		}
		catch
		{
			tcpClient.Close();
			throw;
		}
		return tcpClient;
	}

	private static void Send(TcpClient Client, string Text)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(Text);
		Client.GetStream().Write(bytes, 0, bytes.Length);
		Client.GetStream().Flush();
	}

	/// <summary>
	/// Reads one line from the relay. Null on close or timeout - a relay that stops answering must
	/// not hang the host, which is why the socket carries a receive timeout.
	/// </summary>
	private static string ReadLine(TcpClient Client)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder(64);
			NetworkStream stream = Client.GetStream();
			while (stringBuilder.Length < 512)
			{
				int num = stream.ReadByte();
				if (num < 0)
				{
					return (stringBuilder.Length == 0) ? null : stringBuilder.ToString();
				}
				if (num == 10)
				{
					return stringBuilder.ToString().TrimEnd('\r');
				}
				stringBuilder.Append((char)num);
			}
			return stringBuilder.ToString();
		}
		catch
		{
			return null;
		}
	}
}
