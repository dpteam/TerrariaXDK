using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework.GamerServices;
using Terraria.Net;

namespace Microsoft.Xna.Framework.Net;

public sealed class NetworkSession : IDisposable
{
	internal enum NetworkEventType
	{
		PacketSend,
		GamerJoin,
		GamerLeave,
		HostChange,
		StateChange
	}

	internal struct NetworkEvent
	{
		public NetworkEventType Type;

		public NetworkGamer Gamer;

		public byte[] Packet;

		public SendDataOptions Reliable;

		public NetworkSessionState State;

		// Carried so a future reconnect path can report why a session ended; the current
		// transport reports disconnects through SessionEnded directly.
#pragma warning disable 0649
		public NetworkSessionEndReason Reason;
#pragma warning restore 0649
	}

	public delegate NetworkSession NetworkSessionAsynchronousCreate(NetworkSessionType sessionType, int maxLocalGamers, int maxGamers, int privateGamerSlots, NetworkSessionProperties sessionProperties, int hostGamer, bool isHost);

	internal class NetworkSessionAction : IAsyncResult
	{
		public readonly AsyncCallback Callback;

		public readonly int MaxLocalGamers;

		public readonly IEnumerable<SignedInGamer> LocalGamers;

		public readonly int MaxPrivateSlots;

		public readonly NetworkSessionProperties SessionProperties;

		public readonly NetworkSessionType SessionType;

		public object AsyncState { get; private set; }

		public bool CompletedSynchronously => false;

		public bool IsCompleted { get; internal set; }

		public WaitHandle AsyncWaitHandle { get; private set; }

		public NetworkSessionAction(object state, AsyncCallback callback, int maxLocal, IEnumerable<SignedInGamer> localGamers, int maxPrivateSlots, NetworkSessionProperties properties, NetworkSessionType type)
		{
			AsyncState = state;
			Callback = callback;
			IsCompleted = false;
			AsyncWaitHandle = new ManualResetEvent(initialState: true);
			MaxLocalGamers = maxLocal;
			LocalGamers = localGamers;
			MaxPrivateSlots = maxPrivateSlots;
			SessionProperties = properties;
			SessionType = type;
		}
	}

	public const int MaxSupportedGamers = 31;

	public const int MaxPreviousGamers = 100;

	private int maxLocalGamers;

	private Queue<NetworkEvent> networkEvents;

	internal static List<NetworkSession> activeSessions = new List<NetworkSession>();

	private GamerCollection<NetworkGamer> _allGamers;

	private GamerCollection<LocalNetworkGamer> _localGamers;

	private GamerCollection<NetworkGamer> _remoteGamers;

	private GamerCollection<NetworkGamer> _previousGamers;

	internal Queue<CommandEvent> commandQueue;

	internal IPeerTransport networkPeer;

	internal static AsyncCallback actCallback;

	internal static int actMaxLocal;

	internal static IEnumerable<SignedInGamer> actGamers;

	internal static int actMaxPriv;

	internal static NetworkSessionProperties actProp;

	internal static NetworkSessionType actType;

	private static NetworkSessionAction activeAction = null;

	private static NetworkSession activeSession = null;

	/// <summary>Endpoint a pending Join() is targeting; consumed by <see cref="EndJoin"/>.</summary>
	private static AvailableNetworkSession pendingJoinSession = null;

	public bool IsDisposed { get; private set; }

	public GamerCollection<NetworkGamer> AllGamers { get; private set; }

	public GamerCollection<LocalNetworkGamer> LocalGamers { get; private set; }

	public GamerCollection<NetworkGamer> RemoteGamers { get; private set; }

	public GamerCollection<NetworkGamer> PreviousGamers { get; private set; }

	public bool AllowHostMigration { get; set; }

	public bool AllowJoinInProgress { get; set; }

	public int BytesPerSecondReceived { get; private set; }

	public int BytesPerSecondSent { get; private set; }

	public NetworkGamer Host { get; private set; }

	public bool IsEveryoneReady
	{
		get
		{
			foreach (LocalNetworkGamer localGamer in LocalGamers)
			{
				if (!localGamer.IsReady)
				{
					return false;
				}
			}
			return true;
		}
	}

	public bool IsHost
	{
		get
		{
			foreach (LocalNetworkGamer localGamer in LocalGamers)
			{
				if (localGamer.IsHost)
				{
					return true;
				}
			}
			return false;
		}
	}

	public int MaxGamers { get; set; }

	public int PrivateGamerSlots { get; set; }

	public NetworkSessionProperties SessionProperties { get; private set; }

	public NetworkSessionState SessionState { get; private set; }

	public NetworkSessionType SessionType { get; private set; }

	public TimeSpan SimulatedLatency { get; set; }

	public float SimulatedPacketLoss { get; set; }

	public event EventHandler<GameStartedEventArgs> GameStarted;

	public event EventHandler<GameEndedEventArgs> GameEnded;

	public event EventHandler<GamerJoinedEventArgs> GamerJoined;

	public event EventHandler<GamerLeftEventArgs> GamerLeft;

	public event EventHandler<HostChangedEventArgs> HostChanged;

	public event EventHandler<NetworkSessionEndedEventArgs> SessionEnded;

	// Declared for API compatibility with XNA. The Xbox Live leaderboard backend is gone,
	// so these are never raised; keeping them lets mod code subscribe without breaking.
#pragma warning disable 0067
	public event EventHandler<WriteLeaderboardsEventArgs> WriteArbitratedLeaderboard;

	public event EventHandler<WriteLeaderboardsEventArgs> WriteUnarbitratedLeaderboard;

	public event EventHandler<WriteLeaderboardsEventArgs> WriteTrueSkill;

	public static event EventHandler<InviteAcceptedEventArgs> InviteAccepted;
#pragma warning restore 0067

	internal NetworkSession(NetworkSessionProperties properties, NetworkSessionType type, int maxGamers, int privateGamerSlots, int maxLocal, IEnumerable<SignedInGamer> localGamers)
	{
		SessionProperties = properties;
		SessionType = type;
		MaxGamers = maxGamers;
		PrivateGamerSlots = privateGamerSlots;
		List<LocalNetworkGamer> list = new List<LocalNetworkGamer>();
		if (localGamers == null)
		{
			maxLocalGamers = maxLocal;
			for (int i = 0; i < Gamer.SignedInGamers.Count && i < maxLocalGamers; i++)
			{
				if (!Gamer.SignedInGamers[i].IsGuest)
				{
					list.Add(new LocalNetworkGamer(Gamer.SignedInGamers[i], this));
				}
			}
		}
		else
		{
			maxLocalGamers = 0;
			foreach (SignedInGamer localGamer in localGamers)
			{
				list.Add(new LocalNetworkGamer(localGamer, this));
				maxLocalGamers++;
			}
		}
		LocalGamers = new GamerCollection<LocalNetworkGamer>(list);
		List<NetworkGamer> collection = new List<NetworkGamer>();
		RemoteGamers = new GamerCollection<NetworkGamer>(collection);
		List<NetworkGamer> list2 = new List<NetworkGamer>();
		list2.AddRange(collection);
		list2.AddRange(list);
		AllGamers = new GamerCollection<NetworkGamer>(list2);
		PreviousGamers = new GamerCollection<NetworkGamer>(new List<NetworkGamer>());
		Host = LocalGamers[0];
		if (IsHost)
		{
			AllowHostMigration = false;
			AllowJoinInProgress = false;
			SessionState = NetworkSessionState.Lobby;
		}
		networkEvents = new Queue<NetworkEvent>();
		foreach (NetworkGamer allGamer in AllGamers)
		{
			NetworkEvent evt = new NetworkEvent
			{
				Type = NetworkEventType.GamerJoin,
				Gamer = allGamer
			};
			SendNetworkEvent(evt);
		}
		SimulatedLatency = TimeSpan.Zero;
		SimulatedPacketLoss = 0f;
		IsDisposed = false;
		BytesPerSecondReceived = 0;
		BytesPerSecondSent = 0;
	}

	private NetworkSession()
	{
		activeSessions.Add(this);
	}

	/// <summary>
	/// Chooses the transport. TCP is the default: it hosts inside the game process and needs
	/// no external server executable. The legacy Lidgren peer stays available as a fallback.
	/// </summary>
	private IPeerTransport CreatePeer(AvailableNetworkSession availableSession)
	{
		if (Terraria.Net.NetworkConfig.UseTcpTransport)
		{
			return new Terraria.Net.TcpPeer(this, availableSession);
		}
		return new MonoGamerPeer(this, availableSession);
	}

	private NetworkSession(NetworkSessionType sessionType, int maxGamers, int privateGamerSlots, NetworkSessionProperties sessionProperties, bool isHost, int hostGamer, AvailableNetworkSession availableSession)
		: this()
	{
		if (sessionProperties == null)
		{
			throw new ArgumentNullException("sessionProperties");
		}
		_allGamers = new GamerCollection<NetworkGamer>();
		_localGamers = new GamerCollection<LocalNetworkGamer>();
		_remoteGamers = new GamerCollection<NetworkGamer>();
		_previousGamers = new GamerCollection<NetworkGamer>();
		commandQueue = new Queue<CommandEvent>();
		if (isHost)
		{
			// With the TCP transport the listener is bound by EndCreate() on the session that
			// is actually returned to the game; binding here would leak a second listener.
			if (!Terraria.Net.NetworkConfig.UseTcpTransport)
			{
				networkPeer = CreatePeer(null);
			}
		}
		else if (networkPeer == null)
		{
			networkPeer = CreatePeer(availableSession);
		}
		CommandGamerJoined command = new CommandGamerJoined(hostGamer, isHost, isLocal: true);
		commandQueue.Enqueue(new CommandEvent(command));
	}

	private NetworkSession(NetworkSessionType sessionType, int maxGamers, int privateGamerSlots, NetworkSessionProperties sessionProperties, bool isHost, int hostGamer)
		: this(sessionType, maxGamers, privateGamerSlots, sessionProperties, isHost, hostGamer, null)
	{
	}

	public void Dispose()
	{
		foreach (LocalNetworkGamer localGamer in LocalGamers)
		{
			localGamer.packetQueue.Clear();
		}
		networkPeer?.Shutdown();
		networkPeer = null;
		activeSession = null;
		IsDisposed = true;
	}

	// --- Transport-facing helpers (used by IPeerTransport implementations) ----

	/// <summary>Host side: adds a remote gamer and raises the join event on the next Update.</summary>
	internal NetworkGamer AddRemoteGamer(string gamertag, long remoteId)
	{
		NetworkGamer networkGamer = new NetworkGamer(this);
		networkGamer.Gamertag = gamertag;
		networkGamer.RemoteUniqueIdentifier = remoteId;
		RemoteGamers.collection.Add(networkGamer);
		AllGamers.collection.Add(networkGamer);
		SendNetworkEvent(new NetworkEvent
		{
			Type = NetworkEventType.GamerJoin,
			Gamer = networkGamer
		});
		return networkGamer;
	}

	/// <summary>Client side: mirrors a host-side gamer into this session.</summary>
	internal NetworkGamer MirrorRemoteGamer(string gamertag, long remoteId)
	{
		NetworkGamer networkGamer = FindGamerByRemoteId(remoteId);
		if (networkGamer != null)
		{
			return networkGamer;
		}
		return AddRemoteGamer(gamertag, remoteId);
	}

	internal NetworkGamer FindGamerByRemoteId(long remoteId)
	{
		foreach (NetworkGamer allGamer in AllGamers)
		{
			if (allGamer.RemoteUniqueIdentifier == remoteId)
			{
				return allGamer;
			}
		}
		return null;
	}

	internal void RemoveRemoteGamer(long remoteId)
	{
		NetworkGamer networkGamer = FindGamerByRemoteId(remoteId);
		if (networkGamer == null)
		{
			return;
		}
		networkGamer.HasLeftSession = true;
		RemoteGamers.collection.Remove(networkGamer);
		AllGamers.collection.Remove(networkGamer);
		SendNetworkEvent(new NetworkEvent
		{
			Type = NetworkEventType.GamerLeave,
			Gamer = networkGamer
		});
	}

	/// <summary>Queues an inbound packet for the game's own message loop.</summary>
	internal void EnqueueIncomingPacket(NetworkGamer sender, byte[] data)
	{
		if (LocalGamers.Count == 0)
		{
			return;
		}
		LocalGamers[0].packetQueue.Enqueue(new NetworkEvent
		{
			Type = NetworkEventType.PacketSend,
			Gamer = sender,
			Packet = data,
			Reliable = SendDataOptions.Reliable
		});
	}

	/// <summary>Host side: binds the listener for a freshly created session.</summary>
	internal void StartHostPeer()
	{
		if (networkPeer == null)
		{
			networkPeer = CreatePeer(null);
		}
	}

	/// <summary>
	/// Client side: opens the transport to the host. Without this the session existed but had
	/// no connection at all, so joining a game hung on "connecting" forever.
	/// </summary>
	internal void StartClientPeer(AvailableNetworkSession availableSession)
	{
		if (availableSession == null)
		{
			throw new ArgumentNullException("availableSession");
		}
		if (networkPeer == null)
		{
			networkPeer = CreatePeer(availableSession);
		}
	}

	/// <summary>Applies a session-state change coming from the host.</summary>
	internal void ApplyRemoteSessionState(NetworkSessionState state)
	{
		SendNetworkEvent(new NetworkEvent
		{
			Type = NetworkEventType.StateChange,
			State = state
		});
	}

	internal void SetHostGamer(NetworkGamer gamer)
	{
		Host = gamer;
	}

	public void Update()
	{
		if (IsDisposed)
		{
			throw new ObjectDisposedException("this");
		}
		// Inbound traffic is applied on the game thread, before events are dispatched.
		networkPeer?.Drain();
		while (networkEvents.Count > 0)
		{
			NetworkEvent networkEvent = networkEvents.Dequeue();
			if (networkEvent.Type == NetworkEventType.PacketSend)
			{
				continue;
			}
			if (networkEvent.Type == NetworkEventType.GamerJoin)
			{
				if (GamerJoined != null)
				{
					GamerJoined(this, new GamerJoinedEventArgs(networkEvent.Gamer));
				}
			}
			else if (networkEvent.Type == NetworkEventType.GamerLeave)
			{
				if (GamerLeft != null)
				{
					GamerLeft(this, new GamerLeftEventArgs(networkEvent.Gamer));
				}
			}
			else if (networkEvent.Type == NetworkEventType.HostChange)
			{
				if (HostChanged != null)
				{
					HostChanged(this, new HostChangedEventArgs(Host, networkEvent.Gamer));
				}
				Host = networkEvent.Gamer;
			}
			else
			{
				if (networkEvent.Type != NetworkEventType.StateChange)
				{
					continue;
				}
				if (networkEvent.State == NetworkSessionState.Playing)
				{
					if (GameStarted != null)
					{
						GameStarted(this, new GameStartedEventArgs());
					}
				}
				else if (networkEvent.State == NetworkSessionState.Lobby)
				{
					if (GameEnded != null)
					{
						GameEnded(this, new GameEndedEventArgs());
					}
				}
				else
				{
					SessionEnded(this, new NetworkSessionEndedEventArgs(NetworkSessionEndReason.Disconnected));
				}
				SessionState = networkEvent.State;
			}
		}
	}

	public void AddLocalGamer(SignedInGamer gamer)
	{
		if (LocalGamers.Count == maxLocalGamers)
		{
			throw new InvalidOperationException("LocalGamer max limit!");
		}
		LocalNetworkGamer item = new LocalNetworkGamer(gamer, this);
		LocalGamers.collection.Add(item);
		AllGamers.collection.Add(item);
	}

	public NetworkGamer FindGamerById(byte gameId)
	{
		foreach (NetworkGamer allGamer in AllGamers)
		{
			if (allGamer.Id == gameId)
			{
				return allGamer;
			}
		}
		return null;
	}

	public void ResetReady()
	{
		if (IsDisposed)
		{
			throw new ObjectDisposedException("this");
		}
		if (!IsHost)
		{
			throw new InvalidOperationException("This NetworkSession is not the host");
		}
		foreach (NetworkGamer allGamer in AllGamers)
		{
			allGamer.IsReady = false;
		}
	}

	public void StartGame()
	{
		if (IsDisposed)
		{
			throw new ObjectDisposedException("this");
		}
		if (!IsHost)
		{
			throw new InvalidOperationException("This NetworkSession is not the host");
		}
		if (SessionState != NetworkSessionState.Lobby)
		{
			throw new InvalidOperationException("NetworkSession is not Lobby");
		}
		NetworkEvent evt = new NetworkEvent
		{
			Type = NetworkEventType.StateChange,
			State = NetworkSessionState.Playing
		};
		SendNetworkEvent(evt);
		networkPeer?.BroadcastState(NetworkSessionState.Playing);
	}

	public void EndGame()
	{
		if (IsDisposed)
		{
			throw new ObjectDisposedException("this");
		}
		if (!IsHost)
		{
			throw new InvalidOperationException("This NetworkSession is not the host");
		}
		if (SessionState != NetworkSessionState.Playing)
		{
			throw new InvalidOperationException("NetworkSession is not Playing");
		}
		NetworkEvent evt = new NetworkEvent
		{
			Type = NetworkEventType.StateChange,
			State = NetworkSessionState.Lobby
		};
		SendNetworkEvent(evt);
		networkPeer?.BroadcastState(NetworkSessionState.Lobby);
	}

	internal void SendNetworkEvent(NetworkEvent evt)
	{
		networkEvents.Enqueue(evt);
	}

	/// <summary>Broadcasts a game packet over the transport (remote gamers only).</summary>
	internal void SendPacketToAll(byte[] data, SendDataOptions options)
	{
		networkPeer?.SendData(data, options);
	}

	/// <summary>Sends a game packet to one gamer over the transport.</summary>
	internal void SendPacket(byte[] data, SendDataOptions options, NetworkGamer recipient)
	{
		networkPeer?.SendData(data, options, recipient);
	}

	public static NetworkSession Create(NetworkSessionType sessionType, int maxLocalGamers, int maxGamers)
	{
		IAsyncResult asyncResult = BeginCreate(sessionType, maxLocalGamers, maxGamers, null, null);
		while (!asyncResult.IsCompleted)
		{
			if (!GamerServicesDispatcher.UpdateAsync())
			{
				activeAction.IsCompleted = true;
			}
		}
		return EndCreate(asyncResult);
	}

	public static NetworkSession Create(NetworkSessionType sessionType, int maxLocalGamers, int maxGamers, int privateGamerSlots, NetworkSessionProperties sessionProperties)
	{
		IAsyncResult asyncResult = BeginCreate(sessionType, maxLocalGamers, maxGamers, privateGamerSlots, sessionProperties, null, null);
		while (!asyncResult.IsCompleted)
		{
			if (!GamerServicesDispatcher.UpdateAsync())
			{
				activeAction.IsCompleted = true;
			}
		}
		return EndCreate(asyncResult);
	}

	public static NetworkSession Create(NetworkSessionType sessionType, IEnumerable<SignedInGamer> localGamers, int maxGamers, int privateGamerSlots, NetworkSessionProperties sessionProperties)
	{
		return EndCreate(BeginCreate(sessionType, localGamers, maxGamers, privateGamerSlots, sessionProperties, null, null));
	}

	private static NetworkSession Create(NetworkSessionType sessionType, int maxLocalGamers, int maxGamers, int privateGamerSlots, NetworkSessionProperties sessionProperties, int hostGamer, bool isHost)
	{
		if (sessionProperties == null)
		{
			sessionProperties = new NetworkSessionProperties();
		}
		return new NetworkSession(sessionType, maxGamers, privateGamerSlots, sessionProperties, isHost, hostGamer);
	}

	public static IAsyncResult BeginCreate(NetworkSessionType sessionType, int maxLocalGamers, int maxGamers, AsyncCallback callback, object asyncState)
	{
		if (maxLocalGamers < 1 || maxLocalGamers > 4)
		{
			throw new ArgumentOutOfRangeException("maxLocalGamers");
		}
		if (activeAction != null || activeSession != null)
		{
			throw new InvalidOperationException();
		}
		activeAction = new NetworkSessionAction(asyncState, callback, maxLocalGamers, null, 0, new NetworkSessionProperties(), sessionType);
		return activeAction;
	}

	public static IAsyncResult BeginCreate(NetworkSessionType sessionType, int maxLocalGamers, int maxGamers, int privateGamerSlots, NetworkSessionProperties sessionProperties, AsyncCallback callback, object asyncState)
	{
		if (maxLocalGamers < 1 || maxLocalGamers > 4)
		{
			throw new ArgumentOutOfRangeException("maxLocalGamers");
		}
		if (privateGamerSlots < 0 || privateGamerSlots > maxGamers)
		{
			throw new ArgumentOutOfRangeException("privateGamerSlots");
		}
		if (activeAction != null || activeSession != null)
		{
			throw new InvalidOperationException();
		}
		activeAction = new NetworkSessionAction(asyncState, callback, maxLocalGamers, null, privateGamerSlots, sessionProperties, sessionType);
		return activeAction;
	}

	public static IAsyncResult BeginCreate(NetworkSessionType sessionType, IEnumerable<SignedInGamer> localGamers, int maxGamers, int privateGamerSlots, NetworkSessionProperties sessionProperties, AsyncCallback callback, object asyncState)
	{
		int num = -1;
		SignedInGamer signedInGamer = null;
		if (localGamers == null)
		{
			throw new ArgumentNullException("localGamers");
		}
		foreach (SignedInGamer localGamer in localGamers)
		{
			if (localGamer == null)
			{
				throw new ArgumentException("gamer can not be null in list of localGamers.");
			}
			if (localGamer.IsDisposed)
			{
				throw new ObjectDisposedException("localGamers", "A gamer is disposed in the list of localGamers");
			}
			if (signedInGamer == null)
			{
				signedInGamer = localGamer;
			}
		}
		if (signedInGamer == null)
		{
			throw new ArgumentException("Invalid gamer in localGamers.");
		}
		num = (int)signedInGamer.PlayerIndex;
		actGamers = localGamers;
		return BeginCreate(sessionType, num, 4, maxGamers, privateGamerSlots, sessionProperties, callback, asyncState);
	}

	public static IAsyncResult BeginCreate(NetworkSessionType sessionType, int hostGamer, int maxLocalGamers, int maxGamers, int privateGamerSlots, NetworkSessionProperties sessionProperties, AsyncCallback callback, object asyncState)
	{
		if (maxLocalGamers < 1 || maxLocalGamers > 4)
		{
			throw new ArgumentOutOfRangeException("Maximum local players must be between 1 and 4.");
		}
		if (maxGamers < 2 || maxGamers > 32)
		{
			throw new ArgumentOutOfRangeException("Maximum number of gamers must be between 2 and 32.");
		}
		try
		{
			return new NetworkSessionAsynchronousCreate(Create).BeginInvoke(sessionType, maxLocalGamers, maxGamers, privateGamerSlots, sessionProperties, hostGamer, isHost: true, callback, asyncState);
		}
		finally
		{
			actMaxLocal = maxLocalGamers;
			actMaxPriv = privateGamerSlots;
			actProp = sessionProperties;
			actCallback = callback;
			actType = sessionType;
		}
	}

	public static NetworkSession EndCreate(IAsyncResult result)
	{
		if (result != activeAction)
		{
			activeAction = new NetworkSessionAction(result.AsyncState, actCallback, actMaxLocal, actGamers, actMaxPriv, actProp, actType);
		}
		activeSession = new NetworkSession(activeAction.SessionProperties, activeAction.SessionType, 69, activeAction.MaxPrivateSlots, activeAction.MaxLocalGamers, activeAction.LocalGamers);
		activeSession.StartHostPeer();
		activeAction = null;
		return activeSession;
	}

	public static AvailableNetworkSessionCollection Find(NetworkSessionType sessionType, int maxLocalGamers, NetworkSessionProperties searchProperties)
	{
		IAsyncResult asyncResult = BeginFind(sessionType, maxLocalGamers, searchProperties, null, null);
		while (!asyncResult.IsCompleted)
		{
			if (!GamerServicesDispatcher.UpdateAsync())
			{
				activeAction.IsCompleted = true;
			}
		}
		return EndFind(asyncResult);
	}

	public static AvailableNetworkSessionCollection Find(NetworkSessionType sessionType, IEnumerable<SignedInGamer> localGamers, NetworkSessionProperties searchProperties)
	{
		IAsyncResult asyncResult = BeginFind(sessionType, localGamers, searchProperties, null, null);
		while (!asyncResult.IsCompleted)
		{
			if (!GamerServicesDispatcher.UpdateAsync())
			{
				activeAction.IsCompleted = true;
			}
		}
		return EndFind(asyncResult);
	}

	public static IAsyncResult BeginFind(NetworkSessionType sessionType, int maxLocalGamers, NetworkSessionProperties searchProperties, AsyncCallback callback, object asyncState)
	{
		if (sessionType == NetworkSessionType.Local)
		{
			throw new ArgumentException("sessionType");
		}
		if (maxLocalGamers < 1 || maxLocalGamers > 4)
		{
			throw new ArgumentOutOfRangeException("maxLocalGamers");
		}
		if (activeAction != null || activeSession != null)
		{
			throw new InvalidOperationException();
		}
		activeAction = new NetworkSessionAction(asyncState, callback, maxLocalGamers, null, 0, searchProperties, sessionType);
		return activeAction;
	}

	public static IAsyncResult BeginFind(NetworkSessionType sessionType, IEnumerable<SignedInGamer> localGamers, NetworkSessionProperties searchProperties, AsyncCallback callback, object asyncState)
	{
		if (sessionType == NetworkSessionType.Local)
		{
			throw new ArgumentException("sessionType");
		}
		if (activeAction != null || activeSession != null)
		{
			throw new InvalidOperationException();
		}
		int num = 0;
		foreach (SignedInGamer localGamer in localGamers)
		{
			num++;
		}
		activeAction = new NetworkSessionAction(asyncState, callback, num, localGamers, 0, searchProperties, sessionType);
		return activeAction;
	}

	public static AvailableNetworkSessionCollection EndFind(IAsyncResult result)
	{
		if (result != activeAction)
		{
			throw new ArgumentException("result");
		}
		List<AvailableNetworkSession> collection = new List<AvailableNetworkSession>();
		activeAction = null;
		return new AvailableNetworkSessionCollection(collection);
	}

	public static NetworkSession Join(AvailableNetworkSession availableSession)
	{
		IAsyncResult asyncResult = BeginJoin(availableSession, null, null);
		while (!asyncResult.IsCompleted)
		{
			if (!GamerServicesDispatcher.UpdateAsync())
			{
				activeAction.IsCompleted = true;
			}
		}
		return EndJoin(asyncResult);
	}

	public static IAsyncResult BeginJoin(AvailableNetworkSession availableSession, AsyncCallback callback, object asyncState)
	{
		if (availableSession == null)
		{
			throw new ArgumentNullException("availableSession");
		}
		if (activeAction != null || activeSession != null)
		{
			throw new InvalidOperationException();
		}
		activeAction = new NetworkSessionAction(asyncState, callback, 4, null, 0, null, NetworkSessionType.PlayerMatch);
		pendingJoinSession = availableSession;
		return activeAction;
	}

	public static NetworkSession EndJoin(IAsyncResult result)
	{
		if (result != activeAction)
		{
			throw new ArgumentException("result");
		}
		int maxLocal = activeAction.MaxLocalGamers;
		IEnumerable<SignedInGamer> localGamers = activeAction.LocalGamers;
		activeAction = null;
		AvailableNetworkSession availableNetworkSession = pendingJoinSession;
		pendingJoinSession = null;
		activeSession = new NetworkSession(null, NetworkSessionType.PlayerMatch, 31, 4, maxLocal, localGamers);
		try
		{
			activeSession.StartClientPeer(availableNetworkSession);
		}
		catch
		{
			// The caller reports the failure (Netplay.ClientLoop shows UI.Error); do not leave
			// a half-built session registered as active, or the next attempt is rejected.
			activeSession = null;
			throw;
		}
		return activeSession;
	}

	public static NetworkSession JoinInvited(int maxLocalGamers)
	{
		IAsyncResult asyncResult = BeginJoinInvited(maxLocalGamers, null, null);
		while (!asyncResult.IsCompleted)
		{
			if (!GamerServicesDispatcher.UpdateAsync())
			{
				activeAction.IsCompleted = true;
			}
		}
		return EndJoinInvited(asyncResult);
	}

	public static NetworkSession JoinInvited(IEnumerable<SignedInGamer> localGamers)
	{
		IAsyncResult asyncResult = BeginJoinInvited(localGamers, null, null);
		while (!asyncResult.IsCompleted)
		{
			if (!GamerServicesDispatcher.UpdateAsync())
			{
				activeAction.IsCompleted = true;
			}
		}
		return EndJoinInvited(asyncResult);
	}

	public static IAsyncResult BeginJoinInvited(int maxLocalGamers, AsyncCallback callback, object asyncState)
	{
		if (maxLocalGamers < 1 || maxLocalGamers > 4)
		{
			throw new ArgumentOutOfRangeException("maxLocalGamers");
		}
		if (activeAction != null || activeSession != null)
		{
			throw new InvalidOperationException();
		}
		activeAction = new NetworkSessionAction(asyncState, callback, maxLocalGamers, null, 0, null, NetworkSessionType.PlayerMatch);
		return activeAction;
	}

	public static IAsyncResult BeginJoinInvited(IEnumerable<SignedInGamer> localGamers, AsyncCallback callback, object asyncState)
	{
		if (activeAction != null || activeSession != null)
		{
			throw new InvalidOperationException();
		}
		activeAction = new NetworkSessionAction(asyncState, callback, 0, localGamers, 0, null, NetworkSessionType.PlayerMatch);
		return activeAction;
	}

	public static NetworkSession EndJoinInvited(IAsyncResult result)
	{
		if (result != activeAction)
		{
			throw new ArgumentException("result");
		}
		int maxLocal = activeAction.MaxLocalGamers;
		IEnumerable<SignedInGamer> localGamers = activeAction.LocalGamers;
		activeAction = null;
		activeSession = new NetworkSession(null, NetworkSessionType.PlayerMatch, 31, 4, maxLocal, localGamers);
		return activeSession;
	}
}
