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
	internal const byte OpHello = 1;

	internal const byte OpWelcome = 2;

	private const byte OpRoster = 3;

	/// <summary>
	/// Game data. Internal rather than private so the batching check can name it instead of
	/// hardcoding 4: a test that repeats a protocol number passes when the number changes and
	/// batching silently stops working.
	/// </summary>
	internal const byte OpData = 4;

	private const byte OpJoin = 5;

	private const byte OpLeave = 6;

	private const byte OpState = 7;

	private const byte OpRosterRequest = 8;

	/// <summary>Host rejects the connection and explains why (wrong password, mods, version).</summary>
	internal const byte OpReject = 9;

	/// <summary>Latency probe; the peer echoes it back as <see cref="OpPong"/>.</summary>
	private const byte OpPing = 10;

	private const byte OpPong = 11;

	/// <summary>
	/// Mod-to-mod payload: <c>[modIdLength][modId][payload]</c>. Relayed like game data, so
	/// mods never need a NetMessage id (see <see cref="ModNet"/>).
	/// </summary>
	internal const byte OpMod = 12;

	/// <summary>
	/// Mod files moving from a host to a client that does not have them
	/// (<c>[Mods] AllowDownload</c>). The payload is a sub-protocol, see <see cref="ModTransfer"/>.
	///
	/// A separate opcode rather than a <c>OpMod</c> packet: mod payloads are relayed to every
	/// other player and are limited to 64 KiB, while this is a private stream between one client
	/// and the host, carries whole files, and must never be relayed - a download from one client
	/// is not something the others asked for.
	/// </summary>
	internal const byte OpModFile = 13;

	/// <summary>
	/// Bumped whenever the wire format or handshake changes.
	///
	/// 3: the <c>OpReject</c> payload became a reason code plus fields instead of an English
	/// sentence. A version bump rather than a silent change, because a 2 client would print the
	/// raw bytes as text - and because the refusal it gets ("versions differ") is now shown in
	/// its own language, which is exactly the case this change was for.
	///
	/// 4: <c>MSG_SYNC_ITEM</c> can carry a mod item's id. The type used to share a short with the
	/// ignoring player (11 bits, 0..1023), so a dropped mod item arrived as a different item in
	/// the wrong slot. Ids above 1023 now follow as a separate <c>short</c> after the packed word;
	/// a 3 client would read those two bytes as the item's prefix. Vanilla items are byte-for-byte
	/// unchanged, but the frame is not, so the version moves.
	/// </summary>
	public const int ProtocolVersion = 4;

	/// <summary>Same value, exposed to the headless self-test.</summary>
	public const int ProtocolVersionPublic = ProtocolVersion;

	private static readonly byte[] DiscoveryMagic = Encoding.ASCII.GetBytes("TXDK");

	private const int MaxFrameSize = 4 * 1024 * 1024;

	private readonly NetworkSession _session;

	private readonly bool _isHost;

	private readonly IPEndPoint _remoteEndPoint;

	/// <summary>
	/// Relay room to join, or empty for a direct connection. Set from the session the client is
	/// joining, which got it from the master's listing.
	/// </summary>
	private readonly string _relayRoom;

	private readonly ConcurrentQueue<Action> _inbox = new ConcurrentQueue<Action>();

	private readonly List<Connection> _connections = new List<Connection>();

	private readonly object _writeLock = new object();

	private TcpListener _listener;

	private Thread _acceptThread;

	private TcpClient _client;

	private NetworkStream _stream;

	/// <summary>Game data held until the end of the tick; see <see cref="PacketBufferer"/>.</summary>
	private readonly PacketBufferer _clientOut = new PacketBufferer();

	private long _localRemoteId;

	private volatile bool _stopped;

	private int _advertisedPort;

	/// <summary>
	/// The peer that owns the listener, if this process is hosting. Host console commands
	/// (!kick, !ban) need to reach the connections, and the session keeps its transport
	/// private, so the host registers itself here.
	/// </summary>
	internal static TcpPeer CurrentHost;

	/// <summary>How many remote players are connected. Host console and diagnostics.</summary>
	internal int ConnectedCount => RemoteCount();

	/// <summary>Names of the connected players, in connection order.</summary>
	internal List<string> ConnectedNames()
	{
		List<string> list = new List<string>();
		lock (_connections)
		{
			for (int i = 0; i < _connections.Count; i++)
			{
				list.Add(_connections[i].Gamertag);
			}
		}
		return list;
	}

	/// <summary>
	/// Disconnects one player and tells them why. Returns false when nobody matches, so the
	/// host console can say "no such player" instead of silently doing nothing.
	///
	/// Matching is by gamertag (case-insensitive), which is what the host sees; an exact match
	/// is preferred over a prefix so "Bob" does not kick "Bobby" when both are online.
	/// </summary>
	internal bool Kick(string gamertag, string reason, out string kicked)
	{
		kicked = null;
		if (string.IsNullOrWhiteSpace(gamertag))
		{
			return false;
		}
		string text = gamertag.Trim();
		Connection connection = null;
		lock (_connections)
		{
			for (int i = 0; i < _connections.Count; i++)
			{
				if (string.Equals(_connections[i].Gamertag, text, StringComparison.OrdinalIgnoreCase))
				{
					connection = _connections[i];
					break;
				}
			}
			// No exact match: allow a unique prefix, which is how people type names in chat.
			if (connection == null)
			{
				Connection connection2 = null;
				int num = 0;
				for (int j = 0; j < _connections.Count; j++)
				{
					if (_connections[j].Gamertag.StartsWith(text, StringComparison.OrdinalIgnoreCase))
					{
						connection2 = _connections[j];
						num++;
					}
				}
				if (num == 1)
				{
					connection = connection2;
				}
			}
		}
		if (connection == null)
		{
			return false;
		}
		kicked = connection.Gamertag;
		ModLoaderLog("Kicked " + connection.Gamertag + ": " + reason);
		// Tell the client why before the socket goes away; the disconnect path does the close.
		// The reason is the host's own words, so it travels as a field of a Kicked refusal.
		SendReject(connection, RejectReason.Kicked, reason);
		Disconnect(connection);
		return true;
	}

	private long _lastPingTick;

	private long _pendingPingTick;

	internal TcpPeer(NetworkSession session, AvailableNetworkSession availableSession)
	{
		_session = session;
		_isHost = availableSession == null;
		_remoteEndPoint = availableSession?.EndPoint;
		_relayRoom = availableSession?.RelayRoom ?? string.Empty;
		_advertisedPort = NetworkConfig.Port;
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
		NetworkStats.Reset();
		int port = NetworkConfig.Port;
		_listener = new TcpListener(IPAddress.Any, port);
		_listener.Start();
		CurrentHost = this;
		// Port 0 means "let the OS pick": report the port actually bound, not the request.
		// Without this a caller asking for 0 would advertise 0 in discovery and to clients.
		port = ((IPEndPoint)_listener.LocalEndpoint).Port;
		NetworkConfig.ActivePort = port;
		_advertisedPort = port;
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
		// Ask the router to forward this port, so the world is reachable directly. Runs in the
		// background: a router that never answers must not delay the host. If it fails, the
		// relay is the fallback and the reason is in the log.
		PortForwarder.Start(port);
		// Optional: also make this world reachable through the relay, for hosts that cannot be
		// reached directly at all. Opt-in ([Network] AllowRelay), because a relay costs a hop of
		// latency and somebody else's bandwidth.
		//
		// Registered before the publisher starts: the announcement has to carry the room id, or
		// the first listing would advertise a world nobody outside can join until it refreshes.
		if (NetworkConfig.AllowRelay)
		{
			string error;
			string text = RelayLink.Register(out error);
			if (text != null)
			{
				NetworkConfig.RelayRoom = text;
				RelayLink.StartPump(this);
			}
			else
			{
				Program.LogWarn("Relay", "relay is enabled but the room could not be opened: " + error);
				NetworkConfig.RelayRoom = string.Empty;
			}
		}
		// Optional: list this world publicly. No-op unless [Network] PublishToMaster is on.
		MasterPublisher.Start(port);
	}

	/// <summary>
	/// Takes an already-connected socket into the host's connection list.
	///
	/// Used by the relay: the host dials out and the relay pairs that socket with a waiting
	/// client, so from here on it is an ordinary TCP stream and the normal accept path applies.
	/// Doing it this way means the relay adds no second code path through the handshake, the
	/// roster or the world sync.
	/// </summary>
	internal void AdoptRelaySocket(TcpClient Client)
	{
		if (_stopped || Client == null)
		{
			try
			{
				Client?.Close();
			}
			catch
			{
			}
			return;
		}
		try
		{
			Client.NoDelay = true;
			Connection connection = new Connection(Client);
			Thread thread = new Thread(() => ServeConnection(connection))
			{
				IsBackground = true,
				Name = "TerrariaXDK.Peer"
			};
			thread.Start();
		}
		catch (Exception ex)
		{
			ModLoaderLog("Could not adopt a relayed connection: " + ex.Message);
			try
			{
				Client.Close();
			}
			catch
			{
			}
		}
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
			string password;
			string mods;
			bool download;
			int protocol;
			using (MemoryStream memoryStream = new MemoryStream(frame, 1, frame.Length - 1))
			{
				BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);
				protocol = binaryReader.ReadInt32();
				gamertag = binaryReader.ReadString();
				signature = binaryReader.ReadString();
				password = (memoryStream.Position < memoryStream.Length) ? binaryReader.ReadString() : "";
				// Optional trailing fields: the mod list, then the download request. A client that
				// predates them simply does not send them, and the refusal then names no mods -
				// still correct, just less helpful.
				mods = (memoryStream.Position < memoryStream.Length) ? binaryReader.ReadString() : string.Empty;
				download = (memoryStream.Position < memoryStream.Length) && binaryReader.ReadBoolean();
			}
			if (protocol != ProtocolVersion)
			{
				ModLoaderLog("Rejected peer " + gamertag + ": protocol " + protocol);
				Reject(connection, RejectReason.ProtocolMismatch, protocol.ToString(), ProtocolVersion.ToString());
				return;
			}
			// Bans are checked before the peer joins, so a banned player never sees the roster
			// or any world data. Checking after the join would leak both.
			//
			// Before the mod check as well: the mod check can now end in serving files, and a
			// banned or unauthenticated connection must not receive any. It also means the
			// refusal names the real reason - a banned player with the wrong mods is banned.
			string remoteAddress = RemoteAddressOf(connection);
			if (Terraria.Net.BanList.IsBanned(gamertag, remoteAddress))
			{
				ModLoaderLog("Rejected banned peer " + gamertag + " from " + remoteAddress);
				Reject(connection, RejectReason.Banned);
				return;
			}
			// Password is checked before the peer is registered, so an unauthenticated
			// connection can never see the roster or receive world data.
			if (!string.IsNullOrEmpty(NetworkConfig.Password) && !CheckPassword(password))
			{
				ModLoaderLog("Rejected peer " + gamertag + ": wrong password");
				Reject(connection, RejectReason.WrongPassword);
				return;
			}
			// Whitelist after the password and before registration: a player who is not on the
			// list must not learn the roster or the world either. Order matters for the player:
			// "wrong password" is actionable, "not whitelisted" is not, so the password is asked
			// first.
			if (!Terraria.Net.Whitelist.IsAllowed(gamertag, remoteAddress))
			{
				ModLoaderLog("Rejected peer " + gamertag + " from " + remoteAddress + ": not on the whitelist");
				Reject(connection, RejectReason.NotWhitelisted);
				return;
			}
			if (RemoteCount() >= NetworkConfig.MaxPlayers)
			{
				Reject(connection, RejectReason.ServerFull);
				return;
			}
			System.Collections.Generic.List<Terraria.Mods.ModEntry> hostMods =
				(HostModListForTests != null) ? Terraria.Mods.ModSet.Parse(HostModListForTests) : Terraria.Mods.ModSet.Loaded();
			// A download connection is not a join attempt: it carries files and nothing else, so
			// it is answered before the compatibility check. Otherwise a host with
			// RequireMatchingMods turned off would accept it as a player and the client would wait
			// for files that never come.
			if (download && NetworkConfig.AllowDownload)
			{
				ServeModDownloads(connection, gamertag, hostMods);
				return;
			}
			if (!Terraria.Mods.ModLoader.IsCompatible(HostSignatureForTests ?? Terraria.Mods.ModLoader.Signature, signature))
			{
				// Say *which* mods: "your mods differ" leaves the player comparing folders by
				// hand, and the whole point of requiring a matching set is that the fix should be
				// obvious. The lists travel as separate fields because a sentence cannot be
				// assembled from translated fragments.
				System.Collections.Generic.List<Terraria.Mods.ModEntry> clientMods = Terraria.Mods.ModSet.Parse(mods);
				Terraria.Mods.ModDifference difference = Terraria.Mods.ModSet.Compare(hostMods, clientMods);
				ModLoaderLog("Rejected peer " + gamertag + ": mod signature " + signature + " != "
					+ (HostSignatureForTests ?? Terraria.Mods.ModLoader.Signature) + " (" + difference.Describe(20) + ")");
				// A fourth field says whether this host will send the files, a fifth names them.
				// Neither is part of the sentence, which takes three arguments - see
				// RejectReasons.ArgumentsFor.
				Reject(connection, RejectReason.ModMismatch,
					JoinList(difference.Missing), JoinList(difference.Extra), JoinList(difference.VersionMismatch),
					(NetworkConfig.AllowDownload ? "1" : ""),
					(NetworkConfig.AllowDownload ? JoinList(difference.DownloadableNames()) : ""));
				_inbox.Enqueue(() => RejectForMods(gamertag, difference));
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
			// MOTD and rules: without this a joining player had no idea what this server
			// expected, and the host had no way to say it.
			SendWelcomeLines(connection);
			ReceiveLoop(connection);
		}
		catch (Exception ex)
		{
			ModLoaderLog("Peer connection error: " + ex.Message);
			Disconnect(connection);
		}
	}

	/// <summary>
	/// True when the supplied password matches the configured one. An empty configured
	/// password means "no password required".
	/// </summary>
	internal static bool CheckPassword(string supplied)
	{
		if (string.IsNullOrEmpty(NetworkConfig.Password))
		{
			return true;
		}
		return string.Equals(supplied ?? "", NetworkConfig.Password, StringComparison.Ordinal);
	}

	/// <summary>
	/// The peer's IP address, or null when the socket is already gone. Used by the ban check:
	/// a name can be changed, an address is what the connection actually came from.
	/// </summary>
	private static string RemoteAddressOf(Connection connection)
	{
		try
		{
			if (connection == null || connection.Client == null || connection.Client.Client == null)
			{
				return null;
			}
			IPEndPoint iPEndPoint = connection.Client.Client.RemoteEndPoint as IPEndPoint;
			return (iPEndPoint == null) ? null : iPEndPoint.Address.ToString();
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Serves mod files to a client that was refused for a mod mismatch.
	///
	/// Runs on the connection's own thread, after the ban, password and whitelist checks and
	/// before the peer is registered: the client never joins, never sees the roster, and the
	/// socket carries nothing but files. Only names from <paramref name="hostMods"/> are
	/// answered, and the folder comes from the host's own mod list - a client cannot ask for a
	/// path.
	/// </summary>
	private static void ServeModDownloads(Connection connection, string gamertag,
		System.Collections.Generic.List<Terraria.Mods.ModEntry> hostMods)
	{
		try
		{
			ModLoaderLog("Serving mods to " + gamertag + " (" + NetworkConfig.AllowDownload + ")");
			Terraria.Mods.ModTransfer.ServeResult result = Terraria.Mods.ModTransfer.Serve(
				connection.Stream, connection.Stream, delegate(string name)
				{
					// Resolved through the host's own list, so "which folder" is never the
					// client's answer. A name it did not announce is not served at all.
					for (int i = 0; i < hostMods.Count; i++)
					{
						if (string.Equals(hostMods[i].Name, name, StringComparison.OrdinalIgnoreCase))
						{
							return FindModFolder(hostMods[i].Name);
						}
					}
					return null;
				});
			ModLoaderLog("Sent " + result.ModsSent + " mod(s), " + result.BytesSent + " byte(s) to "
				+ gamertag
				+ ((result.Refused.Count == 0) ? "" : (", refused " + result.Refused.Count + " request(s)"))
				+ (string.IsNullOrEmpty(result.Error) ? "" : (" (" + result.Error + ")")));
		}
		catch (Exception ex)
		{
			ModLoaderLog("Mod download for " + gamertag + " failed: " + ex.Message);
		}
		finally
		{
			connection.Close();
		}
	}

	/// <summary>
	/// The folder of a loaded mod, by name. Null when no loaded mod has that name, which is what
	/// makes "the host has it" and "the host can send it" the same question.
	/// </summary>
	internal static string FindModFolder(string name)
	{
		System.Collections.Generic.IList<Terraria.Mods.RawMod> rawMods = Terraria.Mods.ModLoader.RawMods;
		for (int i = 0; i < rawMods.Count; i++)
		{
			if (string.Equals(rawMods[i].Name, name, StringComparison.OrdinalIgnoreCase))
			{
				return rawMods[i].Directory;
			}
		}
		System.Collections.Generic.IList<Terraria.Mods.Mod> mods = Terraria.Mods.ModLoader.Mods;
		for (int j = 0; j < mods.Count; j++)
		{
			string text = Terraria.Mods.ModLoader.DirectoryOf(mods[j]);
			string text2 = (text == null) ? null : System.IO.Path.GetFileName(text);
			if (string.Equals(mods[j].Name, name, StringComparison.OrdinalIgnoreCase)
				|| (text2 != null && string.Equals(text2, name, StringComparison.OrdinalIgnoreCase)))
			{
				return text;
			}
		}
		return null;
	}

	/// <summary>
	/// The mod names a refusal offers to send, or an empty array.
	///
	/// Only for a mod mismatch with the "this host will send" flag set: a refusal that names mods
	/// but cannot send them must not start a download that will be refused.
	/// </summary>
	internal static string[] DownloadNamesFrom(RejectReason reason, string[] fields)
	{
		if (reason != RejectReason.ModMismatch || fields == null || fields.Length < 5 || fields[3] != "1")
		{
			return new string[0];
		}
		if (string.IsNullOrEmpty(fields[4]))
		{
			return new string[0];
		}
		string[] array = fields[4].Split('\n');
		System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>(array.Length);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.Length > 0 && Terraria.Mods.ModFiles.SafeFolderName(text).Length > 0)
			{
				list.Add(text);
			}
		}
		return list.ToArray();
	}

	/// <summary>
	/// Fetches the offered mods on a second connection, verifies them and puts them in place.
	///
	/// A second connection rather than the one that was refused: the host closes that socket on
	/// purpose, and a refused connection must not be able to stay open and keep asking for files.
	///
	/// Runs on the network thread and touches no game state: everything it needs is a socket and
	/// the filesystem, and the result is posted to the inbox like any other network event.
	/// </summary>
	private void DownloadMods(string[] wanted)
	{
		string error = null;
		int num = 0;
		System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
		try
		{
			if (_remoteEndPoint == null)
			{
				error = "no host address";
			}
			else
			{
				using TcpClient tcpClient = new TcpClient();
				tcpClient.NoDelay = true;
				if (!ConnectWithTimeout(tcpClient, _remoteEndPoint, NetworkConfig.ConnectTimeoutMs))
				{
					error = "could not reach the host";
				}
				else
				{
					using NetworkStream stream = tcpClient.GetStream();
					SendDownloadHandshake(stream);
					Terraria.Mods.ModTransfer.ReceiveResult receiveResult =
						Terraria.Mods.ModTransfer.Receive(stream, stream, wanted);
					num = receiveResult.Received.Count;
					for (int i = 0; i < receiveResult.Received.Count; i++)
					{
						string text = receiveResult.Received[i];
						string text2;
						if (Terraria.Mods.ModFiles.Install(text, out text2))
						{
							list.Add(text);
						}
						else
						{
							error = text + ": " + text2;
						}
					}
					for (int j = 0; j < receiveResult.Failed.Count; j++)
					{
						if (!list.Contains(receiveResult.Failed[j]))
						{
							list.Add(receiveResult.Failed[j]);
						}
					}
					if (receiveResult.Received.Count == 0)
					{
						error = receiveResult.Error ?? "nothing arrived";
					}
				}
			}
		}
		catch (Exception ex)
		{
			error = ex.Message;
		}
		ModLoaderLog("Mod download: " + num + " installed of " + wanted.Length
			+ (string.IsNullOrEmpty(error) ? "" : (" (" + error + ")")));
		int installed = num;
		string capturedError = error;
		_inbox.Enqueue(delegate
		{
			if (installed > 0)
			{
				// A restart, not a reload: assemblies and item ids are fixed at load time, and
				// pretending otherwise would be the "mod is enabled but does nothing" bug again.
				Main.NewText(Texts.Format("ui.modmismatch.installed", installed), 120, 230, 140);
				Main.NewText(Texts.Get("ui.modmismatch.restart"), 200, 200, 120);
			}
			else
			{
				Main.NewText(Texts.Format("ui.modmismatch.download.failed", capturedError ?? ""), 255, 120, 120);
			}
		});
	}

	/// <summary>
	/// The hello for a download connection: same fields as a join, with the download flag set.
	/// It never joins, so the host never registers it.
	/// </summary>
	private void SendDownloadHandshake(Stream stream)
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
		binaryWriter.Write(OpHello);
		binaryWriter.Write(ProtocolVersion);
		binaryWriter.Write("Downloader");
		binaryWriter.Write(Terraria.Mods.ModLoader.Signature);
		binaryWriter.Write(NetworkConfig.Password ?? "");
		// The mod list is deliberately empty: this connection is not asking to join, and claiming
		// a set it does not have would only make the host compare the wrong thing.
		binaryWriter.Write(string.Empty);
		binaryWriter.Write(true);
		binaryWriter.Flush();
		WireFormat.WriteAll(stream, WireFormat.LengthPrefix(memoryStream.ToArray()));
	}

	/// <summary>Tells the client why it was refused, then closes. Best effort.</summary>
	private static void Reject(Connection connection, RejectReason reason, params string[] fields)
	{
		SendReject(connection, reason, fields);
		connection.Close();
	}

	/// <summary>
	/// Sends the refusal without closing. Used by <see cref="Kick"/>, which lets the normal
	/// disconnect path close the socket: closing here as well would race with it.
	///
	/// The host's own words travel as a field rather than as the message: the reason is chosen
	/// by the client, in its language, and the free text is appended where it belongs.
	/// </summary>
	private static void SendReject(Connection connection, RejectReason reason, params string[] fields)
	{
		try
		{
			byte[] payload = RejectReasons.Build(reason, fields);
			connection.Send(WireFormat.Frame(OpReject, payload));
		}
		catch
		{
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
			NetworkStats.OnReceived(frame.Length);
			if (frame[0] == OpPing)
			{
				// Echo immediately from the transport thread: latency should measure the
				// network, not the game's frame rate.
				byte[] array = new byte[frame.Length];
				Buffer.BlockCopy(frame, 0, array, 0, frame.Length);
				array[0] = OpPong;
				connection.Send(WireFormat.LengthPrefix(array));
				continue;
			}
			if (frame[0] == OpMod)
			{
				// Mod traffic is relayed verbatim to the other players and handed to the local
				// handlers; it never touches the game's own packet stream.
				Broadcast(WireFormat.LengthPrefix(frame), connection);
				if (ModNet.TryDecode(frame, out var modId, out var modPayload))
				{
					ModNet.Dispatch(modId, modPayload);
				}
				continue;
			}
			if (frame[0] != OpData || frame.Length < 2)
			{
				continue;
			}
			// Chat from a client: muted lines are dropped, commands run on the host instead of
			// being chat, everything else is relayed. See HandleIncomingChat.
			if (HandleIncomingChat(connection, frame))
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
		// A player who leaves gets a clean budget: a reconnect must not inherit the credit (or
		// the exhaustion) of the session before it.
		Terraria.Net.EditBudget.Reset(connection.Gamertag);
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

	/// <summary>
	/// Sends the MOTD and rules to one connection as ordinary chat lines, so they appear in
	/// the same feed as everything else and the client needs no new packet type.
	/// </summary>
	private void SendWelcomeLines(Connection connection)
	{
		List<string> list = HostModeration.WelcomeLines();
		for (int i = 0; i < list.Count; i++)
		{
			connection.Send(WireFormat.Frame(OpData, HostModeration.BuildChatPayload(list[i], 255, 235, 150)));
		}
	}

	/// <summary>
	/// Sends a chat line to exactly one player. Used by <c>!whisper</c>: the game's own send
	/// path broadcasts or targets a gamer, which is not the same as "this connection".
	/// </summary>
	internal bool SendTextTo(string gamertag, string text, out string sent)
	{
		sent = null;
		if (string.IsNullOrWhiteSpace(gamertag) || string.IsNullOrEmpty(text))
		{
			return false;
		}
		Connection connection = FindConnection(gamertag);
		if (connection == null)
		{
			return false;
		}
		sent = connection.Gamertag;
		connection.Send(WireFormat.Frame(OpData, HostModeration.BuildChatPayload(text, 200, 220, 255)));
		return true;
	}

	/// <summary>
	/// Resolves a typed name to the connection's real gamertag, using the same exact-then-unique
	/// prefix rule as <see cref="Kick"/>. Exposed so commands can report the resolved name.
	/// </summary>
	internal bool ResolveName(string gamertag, out string resolved)
	{
		resolved = null;
		Connection connection = FindConnection(gamertag);
		if (connection == null)
		{
			return false;
		}
		resolved = connection.Gamertag;
		return true;
	}

	/// <summary>Exact name match first, then a unique prefix. Null when ambiguous or absent.</summary>
	private Connection FindConnection(string gamertag)
	{
		if (string.IsNullOrWhiteSpace(gamertag))
		{
			return null;
		}
		string text = gamertag.Trim();
		lock (_connections)
		{
			for (int i = 0; i < _connections.Count; i++)
			{
				if (string.Equals(_connections[i].Gamertag, text, StringComparison.OrdinalIgnoreCase))
				{
					return _connections[i];
				}
			}
			Connection result = null;
			int num = 0;
			for (int j = 0; j < _connections.Count; j++)
			{
				if (_connections[j].Gamertag.StartsWith(text, StringComparison.OrdinalIgnoreCase))
				{
					result = _connections[j];
					num++;
				}
			}
			return (num == 1) ? result : null;
		}
	}

	/// <summary>
	/// Decides what to do with an incoming chat line from a client, and does the host-side part.
	/// Returns true when the frame must NOT be relayed to the other players.
	///
	/// Three cases, in order:
	/// <list type="number">
	/// <item>The sender is muted: the line is dropped and they are reminded why.</item>
	/// <item>The line is a command: it runs on the host, as that player, with that player's
	/// permissions. It is never relayed - a command is addressed to the server, not to the
	/// other players.</item>
	/// <item>Ordinary chat: relayed as before.</item>
	/// </list>
	///
	/// The identity comes from <paramref name="connection"/>, never from the message text:
	/// otherwise "&lt;Host&gt; !ban everyone" typed by a client would be executed as the host.
	/// </summary>
	private bool HandleIncomingChat(Connection connection, byte[] frame)
	{
		if (frame.Length < 2 || frame[0] != OpData || frame[1] != ChatPacket.MessageId)
		{
			return false;
		}
		string text;
		if (!ChatPacket.TryReadText(frame, 1, out text))
		{
			return false;
		}
		if (HostModeration.IsMuted(connection.Gamertag))
		{
			connection.MaybeMuteNotice();
			return true;
		}
		// Clients wrap every line as "<Name> text", so unwrap before asking whether this is a
		// command. The name in that prefix is never trusted: the actor below comes from the
		// connection, otherwise a client could type "<Host> !ban everyone".
		string text2 = ChatPacket.StripSpeaker(text);
		if (!HostCommands.IsCommand(text2))
		{
			return false;
		}
		// Commands touch chat text and the player lists, which are game-thread state, so they
		// are queued rather than run on this transport thread.
		string actor = connection.Gamertag;
		string command = text2;
		_inbox.Enqueue(delegate
		{
			string error;
			if (!HostCommands.TryExecute(command, actor, out error) && !string.IsNullOrEmpty(error))
			{
				Main.NewText(error, 255, 200, 120);
			}
		});
		return true;
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
			// Must bind the discovery port, not an ephemeral one: clients broadcast their
			// probe to NetworkConfig.DiscoveryPort, so a random port could never receive it
			// and no host was ever visible on a LAN.
			udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, NetworkConfig.DiscoveryPort));
		}
		catch (Exception ex)
		{
			// Port taken (a second instance, or another program): LAN discovery is off, but
			// hosting still works. Joining by address is unaffected.
			ModLoaderLog("LAN discovery disabled, could not bind port " + NetworkConfig.DiscoveryPort + ": " + ex.Message);
			try
			{
				udpClient?.Close();
			}
			catch
			{
			}
			return;
		}
		try
		{
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
		// Trailing flags keep the reply backward compatible: older clients stop reading
		// after the port, newer ones also learn whether a password is required.
		int num = string.IsNullOrEmpty(NetworkConfig.Password) ? 0 : 1;
		return "TXDK|" + gamertag + "|" + (RemoteCount() + 1) + "|" + (NetworkConfig.MaxPlayers - RemoteCount()) + "|" + Terraria.Mods.ModLoader.Signature + "|" + _advertisedPort + "|" + num;
	}

	// --- Client -------------------------------------------------------------

	private void StartClient()
	{
		if (_remoteEndPoint == null)
		{
			throw new InvalidOperationException("No host endpoint to connect to.");
		}
		NetworkStats.Reset();
		string relayRoom = _relayRoom;
		if (!string.IsNullOrEmpty(relayRoom))
		{
			// Relayed session: the endpoint is the relay, and the room id says which host to be
			// paired with. The socket that comes back is already connected to the host, so the
			// handshake below is exactly the same as for a direct connection - the relay never
			// sees or rewrites it.
			string error;
			_client = RelayLink.Join(_remoteEndPoint.Address.ToString(), _remoteEndPoint.Port, relayRoom, out error);
			if (_client == null)
			{
				throw new IOException("Could not join the relayed world: " + error);
			}
			_client.NoDelay = true;
			_stream = _client.GetStream();
		}
		else
		{
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
		}
		// Announce this session's own player, not "the first signed-in gamer": with several
		// sessions in one process (or split-screen) every client would send the same tag.
		string gamertag = (_session.LocalGamers.Count > 0 && !string.IsNullOrEmpty(_session.LocalGamers[0].Gamertag)) ? _session.LocalGamers[0].Gamertag : ((Microsoft.Xna.Framework.GamerServices.Gamer.SignedInGamers.Count > 0) ? Microsoft.Xna.Framework.GamerServices.Gamer.SignedInGamers[0].Gamertag : "Terrarian");
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

	/// <summary>
	/// Test hooks: what the host believes its own set is, and what the client claims.
	///
	/// The requirement being tested is "the two sets must agree", which has nothing to do with
	/// which mods happen to be on disk - and a test that installed mods to check it would be
	/// writing into the player's folders. Overriding both sides lets the real handshake, the real
	/// comparison and the real refusal run unchanged.
	/// </summary>
	internal static string HostSignatureForTests;

	internal static string HostModListForTests;

	internal static string ClientSignatureForTests;

	internal static string ClientModListForTests;

	private void SendHandshake(string gamertag)
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
		binaryWriter.Write(OpHello);
		binaryWriter.Write(ProtocolVersion);
		binaryWriter.Write(gamertag ?? "Terrarian");
		binaryWriter.Write(ClientSignatureForTests ?? Terraria.Mods.ModLoader.Signature);
		binaryWriter.Write(NetworkConfig.Password ?? "");
		// The mod list, so a refusal can name the mods instead of saying "they differ". A host
		// that predates this field stops reading after the password and ignores it.
		binaryWriter.Write(ClientModListForTests ?? Terraria.Mods.ModSet.Describe());
		// Optional trailing field: "I will accept files if your set does not match mine". The host
		// only acts on it when the sets actually differ and it is willing to send.
		binaryWriter.Write(NetworkConfig.DownloadFromHost);
		binaryWriter.Flush();
		SendToHost(WireFormat.LengthPrefix(memoryStream.ToArray()));
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
				NetworkStats.OnReceived(frame.Length);
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
		case OpReject:
		{
			// The host sends a code plus its fields, not a sentence: the wording is chosen here,
			// in the player's language. An old host's plain text still parses, as Unknown.
			byte[] payload = new byte[frame.Length - 1];
			Buffer.BlockCopy(frame, 1, payload, 0, payload.Length);
			RejectReason reason;
			string[] fields;
			RejectReasons.TryParse(payload, out reason, out fields);
			string spoken = RejectReasons.Describe(reason, fields);
			ModLoaderLog("Host refused the connection: " + RejectReasons.DescribeEnglish(reason, fields));
			// A mod mismatch the host is willing to fix: the offer is in the fourth field, the
			// names in the fifth. Taking it here means the player sees "downloading" instead of
			// an error they cannot act on.
			string[] wanted = DownloadNamesFrom(reason, fields);
			if (wanted.Length > 0 && NetworkConfig.DownloadFromHost)
			{
				string captured2 = spoken;
				_inbox.Enqueue(delegate
				{
					Main.NewText(Texts.Format("ui.modmismatch.downloading", wanted.Length), 200, 200, 255);
					_session.ApplyRemoteSessionState(NetworkSessionState.Ended);
				});
				DownloadMods(wanted);
				break;
			}
			string captured = spoken;
			_inbox.Enqueue(delegate
			{
				Main.NewText(Texts.Format("ui.reject.title", captured), 255, 120, 120);
				// Only mention the option when the host actually offers it, so the hint never
				// sends the player looking for a setting that would change nothing.
				if (reason == RejectReason.ModMismatch && fields != null && fields.Length > 3 && fields[3] == "1")
				{
					Main.NewText(Texts.Get("ui.modmismatch.offer"), 200, 200, 120);
				}
				_session.ApplyRemoteSessionState(NetworkSessionState.Ended);
			});
			break;
		}
		case OpPong:
		{
			if (frame.Length >= 9)
			{
				long num = BitConverter.ToInt64(frame, 1);
				if (num == _pendingPingTick)
				{
					int milliseconds = (int)Math.Max(0L, Environment.TickCount - num);
					NetworkStats.SetRtt(milliseconds);
				}
			}
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
		case OpMod:
		{
			if (ModNet.TryDecode(frame, out var modId, out var modPayload))
			{
				ModNet.Dispatch(modId, modPayload);
			}
			break;
		}
		}
	}

	// --- IPeerTransport -----------------------------------------------------

	/// <summary>
	/// Sends a pre-framed body (first byte is the opcode) to every other player, or to the
	/// host when this machine is a client. Used by <see cref="ModNet"/>.
	/// </summary>
	public void SendFrame(byte[] frame)
	{
		if (_stopped || frame == null || frame.Length == 0)
		{
			return;
		}
		byte[] array = WireFormat.LengthPrefix(frame);
		if (_isHost)
		{
			Broadcast(array, null);
			NetworkStats.OnSent(array.Length);
			return;
		}
		SendToHost(array);
	}

	/// <summary>
	/// One frame towards the host, batched when it is game data. Kept in one place so the client
	/// path cannot forget the buffer that the host path uses.
	/// </summary>
	private void SendToHost(byte[] frame)
	{
		if (_stream == null)
		{
			return;
		}
		if (PacketBufferer.IsBatchable(frame, OpData) && !_clientOut.Add(frame))
		{
			NetworkStats.OnSent(frame.Length);
			return;
		}
		FlushClientOutgoing();
		try
		{
			WireFormat.WriteAll(_stream, frame);
			NetworkStats.OnSent(frame.Length);
			NetworkStats.OnWrite();
		}
		catch
		{
		}
	}

	private void FlushClientOutgoing()
	{
		if (_stream == null)
		{
			_clientOut.Clear();
			return;
		}
		int frames;
		if (_clientOut.Flush(_stream, out frames) > 0)
		{
			NetworkStats.OnWrite();
		}
	}

	/// <summary>Writes everything held for every connection, and for the client's own stream.</summary>
	private void FlushOutgoing()
	{
		FlushClientOutgoing();
		Connection[] array;
		lock (_connections)
		{
			array = _connections.ToArray();
		}
		for (int i = 0; i < array.Length; i++)
		{
			array[i].FlushOutgoing();
		}
	}

	public void SendData(byte[] data, SendDataOptions options)
	{
		if (_stopped || data == null)
		{
			return;
		}
		if (_isHost)
		{
			Broadcast(WireFormat.Frame(OpData, data), null);
			return;
		}
		SendToHost(WireFormat.Frame(OpData, data));
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
			SendToHost(WireFormat.Frame(OpData, data));
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
		// Everything the game produced this tick leaves in one write per connection. Doing it
		// here - the game thread, once per frame - is what makes the batching free: no timer, no
		// second thread, and the latency is the same tick the data was created in.
		FlushOutgoing();
		// Client keeps a latency probe running once per second. Doing it here (game thread)
		// keeps the transport thread free of timers and reuses the locked write path.
		if (!_isHost && !_stopped && _stream != null)
		{
			long num = Environment.TickCount;
			if (num - _lastPingTick >= 1000L)
			{
				_lastPingTick = num;
				_pendingPingTick = num;
				try
				{
					WireFormat.WriteAll(_stream, WireFormat.Frame(OpPing, BitConverter.GetBytes(num)));
				}
				catch
				{
				}
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
			SendToHost(array);
		}
	}

	public void Shutdown()
	{
		_stopped = true;
		if (CurrentHost == this)
		{
			CurrentHost = null;
			// Mutes are per hosting session: the next world starts with nobody silenced.
			HostModeration.ResetMutes();
		}
		MasterPublisher.Stop();
		if (_isHost)
		{
			// Closing the presence socket is what tells the relay the room is gone, so clients
			// stop waiting for a host that has left.
			RelayLink.Stop();
			NetworkConfig.RelayRoom = string.Empty;
			// Take the router rule back out: a mapping left behind outlives the game and is a
			// hole nobody remembers opening.
			PortForwarder.Stop();
			// Every edit budget belongs to this hosting session.
			Terraria.Net.EditBudget.ResetAll();
		}
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

	private void RejectForMods(string gamertag, Terraria.Mods.ModDifference difference)
	{
		try
		{
			UI.Error(Texts.Get("ui.modmismatch.title"), Texts.Format("ui.modmismatch.text", gamertag));
			if (difference != null && difference.Any)
			{
				Program.LogInfo("Mods", "refused " + gamertag + ": " + difference.Describe(20));
			}
		}
		catch
		{
		}
	}

	/// <summary>
	/// Joins a list of mod labels for a reject field. Newline-separated, because a mod name can
	/// contain a comma and the client has to be able to split it back.
	/// </summary>
	private static string JoinList(System.Collections.Generic.List<string> items)
	{
		if (items == null || items.Count == 0)
		{
			return string.Empty;
		}
		return string.Join("\n", items.ToArray());
	}

	// --- LAN discovery ------------------------------------------------------

	/// <summary>
	/// Turns one discovery reply into a session record, or null when the reply is not ours.
	/// Format: <c>TXDK|gamertag|players|freeSlots|modSignature|port[|flags]</c>; the flags
	/// field is optional so older hosts stay listable.
	/// </summary>
	internal static AvailableNetworkSession ParseDiscoveryReply(string[] fields, IPAddress address)
	{
		if (fields == null || fields.Length < 6 || fields[0] != "TXDK")
		{
			return null;
		}
		int port;
		int.TryParse(fields[5], out port);
		if (port <= 0 || port > 65535)
		{
			port = NetworkConfig.Port;
		}
		int players;
		int.TryParse(fields[2], out players);
		int freeSlots;
		int.TryParse(fields[3], out freeSlots);
		int flags = 0;
		if (fields.Length >= 7)
		{
			int.TryParse(fields[6], out flags);
		}
		// The first constructor argument is the player count. It used to receive the port,
		// which made every LAN world show "7777/8" players.
		AvailableNetworkSession session = new AvailableNetworkSession(players, fields[1], 0, freeSlots, new NetworkSessionProperties(), new QualityOfService());
		session.SessionType = NetworkSessionType.SystemLink;
		session.EndPoint = new IPEndPoint(address, port);
		session.Passworded = (flags & 1) != 0;
		// An incompatible mod set is marked, not hidden: the handshake will refuse it and
		// say why, but the player can at least see the world exists.
		session.ModMismatch = !Terraria.Mods.ModLoader.IsCompatible(fields[4]);
		session.ProtocolVersion = ProtocolVersion;
		return session;
	}

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
				AvailableNetworkSession parsed = ParseDiscoveryReply(array3, remoteEP2.Address);
				if (parsed != null)
				{
					results.Add(parsed);
				}
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

		/// <summary>Game data held until the end of the tick; see <see cref="PacketBufferer"/>.</summary>
		private readonly PacketBufferer _out = new PacketBufferer();

		public void Send(byte[] data)
		{
			if (data == null || data.Length == 0)
			{
				return;
			}
			if (PacketBufferer.IsBatchable(data, OpData) && !_out.Add(data))
			{
				// Held until the next flush: one write per connection per tick.
				return;
			}
			// Anything that is not game data, and a buffer that just filled up, goes out now -
			// after whatever was already held, so the order on the wire is the order of calls.
			FlushOutgoing();
			try
			{
				WireFormat.WriteAll(Stream, data);
				NetworkStats.OnWrite();
			}
			catch
			{
			}
		}

		/// <summary>
		/// Writes everything held for this connection. Called once per tick by
		/// <see cref="TcpPeer.Drain"/>, and before anything that must not overtake it.
		/// </summary>
		internal void FlushOutgoing()
		{
			int frames;
			if (_out.Flush(Stream, out frames) > 0)
			{
				NetworkStats.OnWrite();
			}
		}

		public void Close()
		{
			// A reject or a kick reason can be buffered in the same tick as the close; without
			// this the client is disconnected and never learns why.
			FlushOutgoing();
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

		/// <summary>Last time this player was told they are muted, in Environment.TickCount ms.</summary>
		private int _lastMuteNotice;

		/// <summary>
		/// Reminds a muted player why their message vanished, at most once per
		/// <see cref="HostModeration.MuteNoticeSeconds"/>. Silently swallowing chat looks like
		/// a bug on the client side, but replying to every line would be worse than the silence.
		/// </summary>
		public void MaybeMuteNotice()
		{
			int tickCount = Environment.TickCount;
			// TickCount wraps every ~49 days; an unsigned subtraction keeps the comparison sane.
			if (unchecked((uint)(tickCount - _lastMuteNotice)) < (uint)(HostModeration.MuteNoticeSeconds * 1000) && _lastMuteNotice != 0)
			{
				return;
			}
			_lastMuteNotice = tickCount;
			try
			{
				Send(WireFormat.Frame(OpData, HostModeration.BuildChatPayload(HostModeration.MuteNotice, 255, 190, 160)));
			}
			catch
			{
			}
		}
	}
}
