using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework.Net;

namespace Terraria.Net;

/// <summary>
/// TCP transport for the built-in multiplayer. The host listens inside the game process, so
/// no separate server executable is needed (the old PC builds shipped TerrariaServer.exe).
///
/// Wire format: every frame is a 4-byte little-endian payload length followed by an opcode
/// byte and its payload. Game packets are forwarded verbatim, which keeps the existing
/// <see cref="NetMessage"/> serialisation untouched.
///
/// All mutations of <see cref="NetworkSession"/> collections happen on the game thread via
/// <see cref="Drain"/>, so the rest of the engine stays single-threaded.
/// </summary>
internal sealed class TcpPeer : IPeerTransport
{
	// --- Opcodes ------------------------------------------------------------
	private const byte OpHello = 1;

	private const byte OpWelcome = 2;

	private const byte OpRoster = 3;

	private const byte OpData = 4;

	private const byte OpJoin = 5;

	private const byte OpLeave = 6;

	private const byte OpState = 7;

	private const byte OpRosterRequest = 8;

	/// <summary>Bumped whenever the wire format or handshake changes.</summary>
	public const int ProtocolVersion = 1;

	/// <summary>Same value, exposed to the headless self-test.</summary>
	public const int ProtocolVersionPublic = ProtocolVersion;

	private static readonly byte[] DiscoveryMagic = Encoding.ASCII.GetBytes("TXDK");

	private const int MaxFrameSize = 4 * 1024 * 1024;

	private readonly NetworkSession _session;

	private readonly bool _isHost;

	private readonly IPEndPoint _remoteEndPoint;

	private readonly ConcurrentQueue<Action> _inbox = new ConcurrentQueue<Action>();

	private readonly List<Connection> _connections = new List<Connection>();

	private readonly object _writeLock = new object();

	private TcpListener _listener;

	private Thread _acceptThread;

	private TcpClient _client;

	private NetworkStream _stream;

	private long _localRemoteId;

	private volatile bool _stopped;

	private int _discoveryPort;

	internal TcpPeer(NetworkSession session, AvailableNetworkSession availableSession)
	{
		_session = session;
		_isHost = availableSession == null;
		_remoteEndPoint = availableSession?.EndPoint;
		_discoveryPort = NetworkConfig.Port;
		if (_isHost)
		{
			StartHost();
		}
		else
		{
			StartClient();
		}
	}

	// --- Host ---------------------------------------------------------------

	private void StartHost()
	{
		int port = NetworkConfig.Port;
		_listener = new TcpListener(IPAddress.Any, port);
		_listener.Start();
		NetworkConfig.ActivePort = port;
		ModLoaderLog("Hosting on TCP port " + port);
		_acceptThread = new Thread(AcceptLoop)
		{
			IsBackground = true,
			Name = "TerrariaXDK.Accept"
		};
		_acceptThread.Start();
		Thread thread = new Thread(DiscoveryLoop)
		{
			IsBackground = true,
			Name = "TerrariaXDK.Discovery"
		};
		thread.Start();
	}

	private void AcceptLoop()
	{
		while (!_stopped)
		{
			TcpClient tcpClient;
			try
			{
				tcpClient = _listener.AcceptTcpClient();
			}
			catch
			{
				return;
			}
			try
			{
				tcpClient.NoDelay = true;
				Connection connection = new Connection(tcpClient);
				Thread thread = new Thread(() => ServeConnection(connection))
				{
					IsBackground = true,
					Name = "TerrariaXDK.Peer"
				};
				thread.Start();
			}
			catch
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
	}

	private void ServeConnection(Connection connection)
	{
		try
		{
			byte[] frame = WireFormat.ReadFrame(connection.Stream);
			if (frame == null || frame[0] != OpHello)
			{
				connection.Close();
				return;
			}
			string gamertag;
			string signature;
			int protocol;
			using (MemoryStream memoryStream = new MemoryStream(frame, 1, frame.Length - 1))
			{
				BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);
				protocol = binaryReader.ReadInt32();
				gamertag = binaryReader.ReadString();
				signature = binaryReader.ReadString();
			}
			if (protocol != ProtocolVersion)
			{
				ModLoaderLog("Rejected peer " + gamertag + ": protocol " + protocol);
				connection.Close();
				return;
			}
			if (!Terraria.Mods.ModLoader.IsCompatible(signature))
			{
				ModLoaderLog("Rejected peer " + gamertag + ": mod signature " + signature + " != " + Terraria.Mods.ModLoader.Signature);
				connection.Close();
				_inbox.Enqueue(() => RejectForMods(gamertag));
				return;
			}
			if (RemoteCount() >= NetworkConfig.MaxPlayers)
			{
				connection.Close();
				return;
			}
			connection.Gamertag = string.IsNullOrEmpty(gamertag) ? "Anonymous" : gamertag;
			lock (_connections)
			{
				_connections.Add(connection);
			}
			// Welcome carries the id the client must use for itself.
			connection.Send(WireFormat.Frame(OpWelcome, BitConverter.GetBytes(connection.Id)));
			// Late joiners need the current session state (host may already be playing).
			connection.Send(WireFormat.Frame(OpState, BitConverter.GetBytes((int)_session.SessionState)));
			// Roster: everyone already connected, so the newcomer can render them.
			SendRoster(connection);
			// Announce the newcomer to everybody else and to the host session.
			Broadcast(WireFormat.Frame(OpJoin, JoinPayload(connection)), connection);
			_inbox.Enqueue(() => _session.AddRemoteGamer(connection.Gamertag, connection.Id));
			ReceiveLoop(connection);
		}
		catch (Exception ex)
		{
			ModLoaderLog("Peer connection error: " + ex.Message);
			Disconnect(connection);
		}
	}

	private void ReceiveLoop(Connection connection)
	{
		while (!_stopped)
		{
			byte[] frame = WireFormat.ReadFrame(connection.Stream);
			if (frame == null)
			{
				break;
			}
			if (frame[0] != OpData || frame.Length < 2)
			{
				continue;
			}
			byte[] packet = new byte[frame.Length - 1];
			Buffer.BlockCopy(frame, 1, packet, 0, packet.Length);
			Connection captured = connection;
			_inbox.Enqueue(() =>
			{
				NetworkGamer gamer = _session.FindGamerByRemoteId(captured.Id);
				if (gamer != null)
				{
					_session.EnqueueIncomingPacket(gamer, packet);
				}
			});
			// Fan out to other clients (host is authoritative relay).
			Broadcast(WireFormat.Frame(OpData, packet), connection);
		}
		Disconnect(connection);
	}

	private void Disconnect(Connection connection)
	{
		bool flag;
		lock (_connections)
		{
			flag = _connections.Remove(connection);
		}
		connection.Close();
		if (!flag)
		{
			return;
		}
		Broadcast(WireFormat.Frame(OpLeave, BitConverter.GetBytes(connection.Id)), null);
		_inbox.Enqueue(() => _session.RemoveRemoteGamer(connection.Id));
		ModLoaderLog("Peer left: " + connection.Gamertag);
	}

	private int RemoteCount()
	{
		lock (_connections)
		{
			return _connections.Count;
		}
	}

	private void Broadcast(byte[] frame, Connection except)
	{
		Connection[] array;
		lock (_connections)
		{
			array = _connections.ToArray();
		}
		foreach (Connection connection in array)
		{
			if (connection == except)
			{
				continue;
			}
			connection.Send(frame);
		}
	}

	private void SendRoster(Connection connection)
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
		binaryWriter.Write(OpRoster);
		Connection[] array;
		lock (_connections)
		{
			array = _connections.ToArray();
		}
		binaryWriter.Write(array.Length);
		foreach (Connection item in array)
		{
			binaryWriter.Write(item.Id);
			binaryWriter.Write(item.Gamertag);
		}
		binaryWriter.Flush();
		connection.Send(WireFormat.LengthPrefix(memoryStream.ToArray()));
	}

	private void DiscoveryLoop()
	{
		UdpClient udpClient = null;
		try
		{
			udpClient = new UdpClient();
			udpClient.EnableBroadcast = true;
			udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
			while (!_stopped)
			{
				IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
				byte[] array;
				try
				{
					array = udpClient.Receive(ref remoteEP);
				}
				catch
				{
					return;
				}
				if (array.Length < DiscoveryMagic.Length || !StartsWithMagic(array, DiscoveryMagic))
				{
					continue;
				}
				byte[] bytes = Encoding.UTF8.GetBytes(BuildDiscoveryResponse());
				try
				{
					udpClient.Send(bytes, bytes.Length, remoteEP);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		finally
		{
			try
			{
				udpClient?.Close();
			}
			catch
			{
			}
		}
	}

	private string BuildDiscoveryResponse()
	{
		string gamertag = "Host";
		if (_session.LocalGamers.Count > 0)
		{
			gamertag = _session.LocalGamers[0].Gamertag;
		}
		return "TXDK|" + gamertag + "|" + (RemoteCount() + 1) + "|" + (NetworkConfig.MaxPlayers - RemoteCount()) + "|" + Terraria.Mods.ModLoader.Signature + "|" + _discoveryPort;
	}

	// --- Client -------------------------------------------------------------

	private void StartClient()
	{
		if (_remoteEndPoint == null)
		{
			throw new InvalidOperationException("No host endpoint to connect to.");
		}
		_client = new TcpClient();
		_client.NoDelay = true;
		// A dead or firewalled host must fail fast: the game blocks in Join() before it can
		// show any UI, so the default (tens of seconds) looks like a freeze.
		if (!ConnectWithTimeout(_client, _remoteEndPoint, NetworkConfig.ConnectTimeoutMs))
		{
			try
			{
				_client.Close();
			}
			catch
			{
			}
			throw new TimeoutException("Could not reach " + _remoteEndPoint + " within " + NetworkConfig.ConnectTimeoutMs + " ms.");
		}
		_stream = _client.GetStream();
		string gamertag = (Microsoft.Xna.Framework.GamerServices.Gamer.SignedInGamers.Count > 0) ? Microsoft.Xna.Framework.GamerServices.Gamer.SignedInGamers[0].Gamertag : "Terrarian";
		SendHandshake(gamertag);
		Thread thread = new Thread(ClientLoop)
		{
			IsBackground = true,
			Name = "TerrariaXDK.Client"
		};
		thread.Start();
	}

	/// <summary>Connects with a bounded wait, so an unreachable host cannot hang the game.</summary>
	private static bool ConnectWithTimeout(TcpClient client, IPEndPoint endPoint, int timeoutMs)
	{
		IAsyncResult asyncResult = client.BeginConnect(endPoint.Address, endPoint.Port, null, null);
		if (!asyncResult.AsyncWaitHandle.WaitOne(timeoutMs))
		{
			return false;
		}
		try
		{
			client.EndConnect(asyncResult);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void SendHandshake(string gamertag)
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
		binaryWriter.Write(OpHello);
		binaryWriter.Write(ProtocolVersion);
		binaryWriter.Write(gamertag ?? "Terrarian");
		binaryWriter.Write(Terraria.Mods.ModLoader.Signature);
		binaryWriter.Flush();
		WireFormat.WriteAll(_stream, WireFormat.LengthPrefix(memoryStream.ToArray()));
	}

	private void ClientLoop()
	{
		try
		{
			while (!_stopped)
			{
				byte[] frame = WireFormat.ReadFrame(_stream);
				if (frame == null)
				{
					break;
				}
				HandleClientFrame(frame);
			}
		}
		catch (Exception ex)
		{
			if (!_stopped)
			{
				ModLoaderLog("Disconnected from host: " + ex.Message);
			}
		}
		if (!_stopped)
		{
			_inbox.Enqueue(() => _session.ApplyRemoteSessionState(NetworkSessionState.Ended));
		}
	}

	private void HandleClientFrame(byte[] frame)
	{
		switch (frame[0])
		{
		case OpWelcome:
		{
			long num = BitConverter.ToInt64(frame, 1);
			_localRemoteId = num;
			_inbox.Enqueue(() =>
			{
				// The host is represented by a remote gamer carrying the host id.
				NetworkGamer networkGamer = _session.FindGamerByRemoteId(0L);
				if (networkGamer == null)
				{
					networkGamer = _session.MirrorRemoteGamer("Host", 0L);
				}
				_session.SetHostGamer(networkGamer);
			});
			break;
		}
		case OpRoster:
		{
			using MemoryStream memoryStream = new MemoryStream(frame, 1, frame.Length - 1);
			BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);
			int num2 = binaryReader.ReadInt32();
			for (int i = 0; i < num2; i++)
			{
				long remoteId = binaryReader.ReadInt64();
				string gamertag = binaryReader.ReadString();
				long captured = remoteId;
				string capturedTag = gamertag;
				if (captured != _localRemoteId)
				{
					_inbox.Enqueue(() => _session.MirrorRemoteGamer(capturedTag, captured));
				}
			}
			break;
		}
		case OpJoin:
		{
			long remoteId = BitConverter.ToInt64(frame, 1);
			string gamertag = Encoding.UTF8.GetString(frame, 9, frame.Length - 9);
			if (remoteId != _localRemoteId)
			{
				long captured2 = remoteId;
				string capturedTag2 = gamertag;
				_inbox.Enqueue(() => _session.MirrorRemoteGamer(capturedTag2, captured2));
			}
			break;
		}
		case OpLeave:
		{
			long remoteId = BitConverter.ToInt64(frame, 1);
			_inbox.Enqueue(() => _session.RemoveRemoteGamer(remoteId));
			break;
		}
		case OpState:
		{
			NetworkSessionState state = (NetworkSessionState)BitConverter.ToInt32(frame, 1);
			_inbox.Enqueue(() => _session.ApplyRemoteSessionState(state));
			break;
		}
		case OpData:
		{
			byte[] packet = new byte[frame.Length - 1];
			Buffer.BlockCopy(frame, 1, packet, 0, packet.Length);
			_inbox.Enqueue(() =>
			{
				NetworkGamer gamer = _session.FindGamerByRemoteId(0L);
				if (gamer != null)
				{
					_session.EnqueueIncomingPacket(gamer, packet);
				}
			});
			break;
		}
		}
	}

	// --- IPeerTransport -----------------------------------------------------

	public void SendData(byte[] data, SendDataOptions options)
	{
		if (_stopped || data == null)
		{
			return;
		}
		if (_isHost)
		{
			Broadcast(WireFormat.Frame(OpData, data), null);
		}
		else if (_stream != null)
		{
			try
			{
				WireFormat.WriteAll(_stream, WireFormat.Frame(OpData, data));
			}
			catch
			{
			}
		}
	}

	public void SendData(byte[] data, SendDataOptions options, NetworkGamer gamer)
	{
		if (gamer == null)
		{
			SendData(data, options);
			return;
		}
		if (gamer.IsLocal)
		{
			// Loopback: split-screen players talk to each other inside the process.
			_session.EnqueueIncomingPacket(gamer, data);
			return;
		}
		if (_isHost)
		{
			Connection[] array;
			lock (_connections)
			{
				array = _connections.ToArray();
			}
			foreach (Connection connection in array)
			{
				if (connection.Id == gamer.RemoteUniqueIdentifier)
				{
					connection.Send(WireFormat.Frame(OpData, data));
					return;
				}
			}
		}
		else if (_stream != null)
		{
			try
			{
				WireFormat.WriteAll(_stream, WireFormat.Frame(OpData, data));
			}
			catch
			{
			}
		}
	}

	public void Drain()
	{
		Action result;
		while (_inbox.TryDequeue(out result))
		{
			try
			{
				result();
			}
			catch (Exception ex)
			{
				ModLoaderLog("Transport drain error: " + ex.Message);
			}
		}
	}

	public void BroadcastState(NetworkSessionState state)
	{
		if (_stopped)
		{
			return;
		}
		byte[] array = WireFormat.Frame(OpState, BitConverter.GetBytes((int)state));
		if (_isHost)
		{
			Broadcast(array, null);
		}
		else if (_stream != null)
		{
			try
			{
				WireFormat.WriteAll(_stream, array);
			}
			catch
			{
			}
		}
	}

	public void Shutdown()
	{
		_stopped = true;
		try
		{
			_listener?.Stop();
		}
		catch
		{
		}
		Connection[] array;
		lock (_connections)
		{
			array = _connections.ToArray();
			_connections.Clear();
		}
		foreach (Connection connection in array)
		{
			connection.Close();
		}
		try
		{
			_stream?.Close();
		}
		catch
		{
		}
		try
		{
			_client?.Close();
		}
		catch
		{
		}
	}

	// --- Framing (see WireFormat) -------------------------------------------

	private static byte[] JoinPayload(Connection connection)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(connection.Gamertag);
		byte[] array = new byte[8 + bytes.Length];
		Buffer.BlockCopy(BitConverter.GetBytes(connection.Id), 0, array, 0, 8);
		Buffer.BlockCopy(bytes, 0, array, 8, bytes.Length);
		return array;
	}

	private static bool StartsWithMagic(byte[] data, byte[] magic)
	{
		for (int i = 0; i < magic.Length; i++)
		{
			if (data[i] != magic[i])
			{
				return false;
			}
		}
		return true;
	}

	private static void ModLoaderLog(string message)
	{
		try
		{
			Program.Log("[Net] " + message);
		}
		catch
		{
		}
	}

	private void RejectForMods(string gamertag)
	{
		try
		{
			UI.Error("Mod mismatch", "Player '" + gamertag + "' uses a different mod set and cannot join.");
		}
		catch
		{
		}
	}

	// --- LAN discovery ------------------------------------------------------

	/// <summary>Broadcasts a discovery probe and collects the replies.</summary>
	internal static void Discover(List<AvailableNetworkSession> results)
	{
		UdpClient udpClient = new UdpClient();
		try
		{
			udpClient.EnableBroadcast = true;
			IPEndPoint remoteEP = new IPEndPoint(IPAddress.Broadcast, NetworkConfig.DiscoveryPort);
			byte[] array = new byte[4];
			Buffer.BlockCopy(DiscoveryMagic, 0, array, 0, DiscoveryMagic.Length);
			udpClient.Send(array, array.Length, remoteEP);
			udpClient.Client.ReceiveTimeout = 1200;
			DateTime utcNow = DateTime.UtcNow;
			while ((DateTime.UtcNow - utcNow).TotalMilliseconds < 1200.0)
			{
				IPEndPoint remoteEP2 = new IPEndPoint(IPAddress.Any, 0);
				byte[] array2;
				try
				{
					array2 = udpClient.Receive(ref remoteEP2);
				}
				catch
				{
					break;
				}
				string[] array3 = Encoding.UTF8.GetString(array2).Split('|');
				if (array3.Length < 6 || array3[0] != "TXDK")
				{
					continue;
				}
				if (!Terraria.Mods.ModLoader.IsCompatible(array3[4]))
				{
					continue;
				}
				int num = 0;
				int.TryParse(array3[5], out num);
				AvailableNetworkSession availableNetworkSession = new AvailableNetworkSession(num > 0 ? num : NetworkConfig.Port, array3[1], 0, 0, new NetworkSessionProperties(), new QualityOfService());
				availableNetworkSession.SessionType = NetworkSessionType.SystemLink;
				availableNetworkSession.EndPoint = new IPEndPoint(remoteEP2.Address, num > 0 ? num : NetworkConfig.Port);
				results.Add(availableNetworkSession);
			}
		}
		finally
		{
			try
			{
				udpClient.Close();
			}
			catch
			{
			}
		}
	}

	private sealed class Connection
	{
		private static long _nextId = 1L;

		public readonly long Id;

		public readonly TcpClient Client;

		public readonly NetworkStream Stream;

		public string Gamertag = "Anonymous";

		public Connection(TcpClient client)
		{
			Client = client;
			Stream = client.GetStream();
			Id = Interlocked.Increment(ref _nextId);
		}

		public void Send(byte[] data)
		{
			try
			{
				WireFormat.WriteAll(Stream, data);
			}
			catch
			{
			}
		}

		public void Close()
		{
			try
			{
				Stream.Close();
			}
			catch
			{
			}
			try
			{
				Client.Close();
			}
			catch
			{
			}
		}
	}
}
