using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Lidgren.Network;

namespace Microsoft.Xna.Framework.Net;

internal class MonoGamerPeer
{
	private BackgroundWorker MGServerWorker = new BackgroundWorker();

	private bool done;

	private NetServer peer;

	private NetworkSession session;

	private AvailableNetworkSession availableSession;

	private string myLocalAddress = string.Empty;

	private IPEndPoint myLocalEndPoint;

	private Dictionary<long, NetConnection> pendingGamers = new Dictionary<long, NetConnection>();

	private bool online;

	private static int port;

	private static IPEndPoint m_masterServer;

	private static int masterserverport;

	private static string masterServer;

	internal static string applicationIdentifier;

	private static NetPeer netPeer;

	private static List<NetIncomingMessage> discoveryMsgs;

	public TimeSpan SimulatedLatency
	{
		get
		{
			return new TimeSpan(0L);
		}
		set
		{
		}
	}

	public float SimulatedPacketLoss
	{
		get
		{
			return 0f;
		}
		set
		{
		}
	}

	internal bool IsReady => peer != null;

	static MonoGamerPeer()
	{
		port = 3074;
		masterserverport = 6000;
		masterServer = "monolive.servegame.com";
		applicationIdentifier = "monogame";
		Assembly assembly = Assembly.GetAssembly(typeof(Game));
		if (assembly != null)
		{
			object[] customAttributes = assembly.GetCustomAttributes(typeof(GuidAttribute), inherit: false);
			if (customAttributes.Length != 0)
			{
				applicationIdentifier = ((GuidAttribute)customAttributes[0]).Value;
			}
		}
	}

	public MonoGamerPeer(NetworkSession session, AvailableNetworkSession availableSession)
	{
		this.session = session;
		online = this.session.SessionType == NetworkSessionType.PlayerMatch;
		this.availableSession = availableSession;
		MGServerWorker.WorkerSupportsCancellation = true;
		MGServerWorker.DoWork += MGServer_DoWork;
		MGServerWorker.RunWorkerCompleted += MGServer_RunWorkerCompleted;
		MGServerWorker.RunWorkerAsync();
		HookEvents();
	}

	private void HookEvents()
	{
		session.GameEnded += HandleSessionStateChanged;
		session.SessionEnded += HandleSessionStateChanged;
		session.GameStarted += HandleSessionStateChanged;
	}

	private void HandleSessionStateChanged(object sender, EventArgs e)
	{
		SendSessionStateChange();
		if (session.SessionState == NetworkSessionState.Ended)
		{
			MGServerWorker.CancelAsync();
		}
	}

	internal void ShutDown()
	{
		MGServerWorker.CancelAsync();
	}

	private void MGServer_DoWork(object sender, DoWorkEventArgs e)
	{
		BackgroundWorker backgroundWorker = sender as BackgroundWorker;
		NetPeerConfiguration netPeerConfiguration = new NetPeerConfiguration(applicationIdentifier);
		netPeerConfiguration.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
		netPeerConfiguration.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
		netPeerConfiguration.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
		if (availableSession == null)
		{
			netPeerConfiguration.Port = port;
		}
		peer = new NetServer(netPeerConfiguration);
		peer.Start();
		myLocalAddress = GetMyLocalIpAddress();
		IPAddress address = IPAddress.Parse(myLocalAddress);
		myLocalEndPoint = new IPEndPoint(address, port);
		try
		{
			while (session?.LocalGamers == null || session.LocalGamers.Count <= 0)
			{
				Thread.Sleep(10);
			}
		}
		catch
		{
		}
		if (availableSession != null)
		{
			if (!online)
			{
				peer.Connect(availableSession.EndPoint);
			}
			else
			{
				RequestNATIntroduction(availableSession.EndPoint, peer);
			}
		}
		else if (online)
		{
			IPAddress iPAddress = NetUtility.Resolve(masterServer);
			if (iPAddress == null)
			{
				throw new Exception("Could not resolve live host");
			}
			m_masterServer = new IPEndPoint(iPAddress, masterserverport);
			LocalNetworkGamer localNetworkGamer = session.LocalGamers[0];
			NetOutgoingMessage netOutgoingMessage = peer.CreateMessage();
			netOutgoingMessage.Write((byte)0);
			netOutgoingMessage.Write(session.AllGamers.Count);
			netOutgoingMessage.Write(localNetworkGamer.Gamertag);
			netOutgoingMessage.Write(session.PrivateGamerSlots);
			netOutgoingMessage.Write(session.MaxGamers);
			netOutgoingMessage.Write(localNetworkGamer.IsHost);
			netOutgoingMessage.Write(myLocalEndPoint);
			netOutgoingMessage.Write(peer.Configuration.AppIdentifier);
			int[] array = new int[session.SessionProperties.Count * 2];
			NetworkSessionProperties.WriteProperties(session.SessionProperties, array);
			for (int i = 0; i < array.Length; i++)
			{
				netOutgoingMessage.Write(array[i]);
			}
			peer.SendUnconnectedMessage(netOutgoingMessage, m_masterServer);
		}
		while (true)
		{
			NetIncomingMessage netIncomingMessage;
			if ((netIncomingMessage = peer.ReadMessage()) != null)
			{
				switch (netIncomingMessage.MessageType)
				{
				case NetIncomingMessageType.NatIntroductionSuccess:
					peer.Connect(netIncomingMessage.SenderEndPoint);
					break;
				case NetIncomingMessageType.DiscoveryRequest:
				{
					LocalNetworkGamer localNetworkGamer2 = session.LocalGamers[0];
					NetOutgoingMessage netOutgoingMessage2 = peer.CreateMessage();
					netOutgoingMessage2.Write(session.RemoteGamers.Count);
					netOutgoingMessage2.Write(localNetworkGamer2.Gamertag);
					netOutgoingMessage2.Write(session.PrivateGamerSlots);
					netOutgoingMessage2.Write(session.MaxGamers);
					netOutgoingMessage2.Write(localNetworkGamer2.IsHost);
					int[] array3 = new int[session.SessionProperties.Count * 2];
					NetworkSessionProperties.WriteProperties(session.SessionProperties, array3);
					for (int j = 0; j < array3.Length; j++)
					{
						netOutgoingMessage2.Write(array3[j]);
					}
					peer.SendDiscoveryResponse(netOutgoingMessage2, netIncomingMessage.SenderEndPoint);
					break;
				}
				case NetIncomingMessageType.StatusChanged:
				{
					byte b = netIncomingMessage.ReadByte();
					if (b == 7)
					{
						CommandEvent item3 = new CommandEvent(new CommandGamerLeft(netIncomingMessage.SenderConnection.RemoteUniqueIdentifier));
						session.commandQueue.Enqueue(item3);
					}
					if (b == 5 && !pendingGamers.ContainsKey(netIncomingMessage.SenderConnection.RemoteUniqueIdentifier))
					{
						pendingGamers.Add(netIncomingMessage.SenderConnection.RemoteUniqueIdentifier, netIncomingMessage.SenderConnection);
						SendProfileRequest(netIncomingMessage.SenderConnection);
					}
					break;
				}
				case NetIncomingMessageType.Data:
					switch ((NetworkMessageType)netIncomingMessage.ReadByte())
					{
					case NetworkMessageType.Data:
					{
						byte[] array2 = new byte[netIncomingMessage.LengthBytes - 1];
						netIncomingMessage.ReadBytes(array2, 0, array2.Length);
						CommandEvent item2 = new CommandEvent(new CommandReceiveData(netIncomingMessage.SenderConnection.RemoteUniqueIdentifier, array2));
						session.commandQueue.Enqueue(item2);
						break;
					}
					case NetworkMessageType.Introduction:
					{
						string endPoint = netIncomingMessage.ReadString();
						try
						{
							IPEndPoint iPEndPoint = ParseIPEndPoint(endPoint);
							if (myLocalEndPoint.ToString() != iPEndPoint.ToString() && !AlreadyConnected(iPEndPoint))
							{
								peer.Connect(iPEndPoint);
							}
						}
						catch (Exception)
						{
						}
						break;
					}
					case NetworkMessageType.GamerProfile:
						if (pendingGamers.ContainsKey(netIncomingMessage.SenderConnection.RemoteUniqueIdentifier))
						{
							pendingGamers.Remove(netIncomingMessage.SenderConnection.RemoteUniqueIdentifier);
							netIncomingMessage.ReadInt32();
							string gamerTag = netIncomingMessage.ReadString();
							netIncomingMessage.ReadInt32();
							netIncomingMessage.ReadInt32();
							GamerStates gamerStates2 = (GamerStates)netIncomingMessage.ReadInt32();
							gamerStates2 &= ~GamerStates.Local;
							CommandEvent item = new CommandEvent(new CommandGamerJoined(netIncomingMessage.SenderConnection.RemoteUniqueIdentifier)
							{
								GamerTag = gamerTag,
								State = gamerStates2
							});
							session.commandQueue.Enqueue(item);
						}
						break;
					case NetworkMessageType.RequestGamerProfile:
						SendProfile(netIncomingMessage.SenderConnection);
						break;
					case NetworkMessageType.GamerStateChange:
					{
						GamerStates gamerStates = (GamerStates)netIncomingMessage.ReadInt32();
						gamerStates &= ~GamerStates.Local;
						foreach (NetworkGamer remoteGamer in session.RemoteGamers)
						{
							if (remoteGamer.RemoteUniqueIdentifier == netIncomingMessage.SenderConnection.RemoteUniqueIdentifier)
							{
								remoteGamer.State = (int)gamerStates;
							}
						}
						break;
					}
					case NetworkMessageType.SessionStateChange:
					{
						NetworkSessionState networkSessionState = (NetworkSessionState)netIncomingMessage.ReadInt32();
						foreach (NetworkGamer remoteGamer2 in session.RemoteGamers)
						{
							if (remoteGamer2.RemoteUniqueIdentifier == netIncomingMessage.SenderConnection.RemoteUniqueIdentifier && remoteGamer2.IsHost && networkSessionState == NetworkSessionState.Playing)
							{
								session.StartGame();
							}
						}
						break;
					}
					}
					break;
				}
			}
			else
			{
				Thread.Sleep(1);
				if (backgroundWorker.CancellationPending)
				{
					e.Cancel = true;
					done = true;
				}
				if (done)
				{
					break;
				}
			}
		}
	}

	private bool AlreadyConnected(IPEndPoint endPoint)
	{
		foreach (NetConnection connection in peer.Connections)
		{
			if (connection.RemoteEndPoint == endPoint)
			{
				return true;
			}
		}
		return false;
	}

	private void MGServer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		if (online && availableSession == null)
		{
			NetOutgoingMessage netOutgoingMessage = peer.CreateMessage();
			netOutgoingMessage.Write((byte)3);
			netOutgoingMessage.Write(session.Host.Gamertag);
			netOutgoingMessage.Write(peer.Configuration.AppIdentifier);
			peer.SendUnconnectedMessage(netOutgoingMessage, m_masterServer);
		}
		peer.Shutdown("app exiting");
	}

	internal void SendProfile(NetConnection player)
	{
		NetOutgoingMessage netOutgoingMessage = peer.CreateMessage();
		netOutgoingMessage.Write((byte)4);
		netOutgoingMessage.Write(session.AllGamers.Count);
		netOutgoingMessage.Write(session.LocalGamers[0].Gamertag);
		netOutgoingMessage.Write(session.PrivateGamerSlots);
		netOutgoingMessage.Write(session.MaxGamers);
		netOutgoingMessage.Write(session.LocalGamers[0].State);
		peer.SendMessage(netOutgoingMessage, player, NetDeliveryMethod.ReliableOrdered);
	}

	internal void SendProfileRequest(NetConnection player)
	{
		NetOutgoingMessage netOutgoingMessage = peer.CreateMessage();
		netOutgoingMessage.Write((byte)5);
		peer.SendMessage(netOutgoingMessage, player, NetDeliveryMethod.ReliableOrdered);
	}

	internal void SendPeerIntroductions(NetworkGamer gamer)
	{
		NetConnection netConnection = null;
		foreach (NetConnection connection in peer.Connections)
		{
			if (connection.RemoteUniqueIdentifier == gamer.RemoteUniqueIdentifier)
			{
				netConnection = connection;
			}
		}
		if (netConnection == null)
		{
			return;
		}
		foreach (NetConnection connection2 in peer.Connections)
		{
			NetOutgoingMessage netOutgoingMessage = peer.CreateMessage();
			netOutgoingMessage.Write((byte)3);
			netOutgoingMessage.Write(netConnection.RemoteEndPoint.ToString());
			peer.SendMessage(netOutgoingMessage, connection2, NetDeliveryMethod.ReliableOrdered);
		}
	}

	internal void SendGamerStateChange(NetworkGamer gamer)
	{
		NetOutgoingMessage netOutgoingMessage = peer.CreateMessage();
		netOutgoingMessage.Write((byte)6);
		netOutgoingMessage.Write(gamer.State);
		SendMessage(netOutgoingMessage, SendDataOptions.Reliable, gamer);
	}

	internal void SendSessionStateChange()
	{
		NetOutgoingMessage netOutgoingMessage = peer.CreateMessage();
		netOutgoingMessage.Write((byte)7);
		netOutgoingMessage.Write((int)session.SessionState);
		SendMessage(netOutgoingMessage, SendDataOptions.Reliable, null);
	}

	public static IPEndPoint ParseIPEndPoint(string endPoint)
	{
		string[] array = endPoint.Split(':');
		if (array.Length != 2)
		{
			throw new FormatException("Invalid endpoint format");
		}
		if (!IPAddress.TryParse(array[0], out var address))
		{
			throw new FormatException("Invalid ip-adress");
		}
		if (!int.TryParse(array[1], out var result))
		{
			throw new FormatException("Invalid port");
		}
		return new IPEndPoint(address, result);
	}

	internal static string GetMyLocalIpAddress()
	{
		string result = "?";
		IPAddress[] addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
		IPAddress[] array = addressList;
		foreach (IPAddress iPAddress in array)
		{
			if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
			{
				result = iPAddress.ToString();
				break;
			}
		}
		return result;
	}

	internal void DiscoverPeers()
	{
		peer.DiscoverLocalPeers(port);
	}

	internal void SendData(byte[] data, SendDataOptions options)
	{
		SendMessage(NetworkMessageType.Data, data, options, null);
	}

	internal void SendData(byte[] data, SendDataOptions options, NetworkGamer gamer)
	{
		SendMessage(NetworkMessageType.Data, data, options, gamer);
	}

	private void SendMessage(NetworkMessageType messageType, byte[] data, SendDataOptions options, NetworkGamer gamer)
	{
		NetOutgoingMessage netOutgoingMessage = peer.CreateMessage();
		netOutgoingMessage.Write((byte)messageType);
		netOutgoingMessage.Write(data);
		SendMessage(netOutgoingMessage, options, gamer);
	}

	private void SendMessage(NetOutgoingMessage om, SendDataOptions options, NetworkGamer gamer)
	{
		NetDeliveryMethod method = NetDeliveryMethod.Unreliable;
		switch (options)
		{
		case SendDataOptions.Reliable:
			method = NetDeliveryMethod.ReliableSequenced;
			break;
		case SendDataOptions.ReliableInOrder:
			method = NetDeliveryMethod.ReliableOrdered;
			break;
		case SendDataOptions.InOrder:
			method = NetDeliveryMethod.UnreliableSequenced;
			break;
		case SendDataOptions.None:
			method = NetDeliveryMethod.Unknown;
			break;
		}
		peer.SendToAll(om, method);
	}

	internal static void Find(NetworkSessionType sessionType)
	{
		NetPeerConfiguration netPeerConfiguration = new NetPeerConfiguration(applicationIdentifier);
		if (sessionType == NetworkSessionType.PlayerMatch)
		{
			netPeerConfiguration.EnableMessageType(NetIncomingMessageType.UnconnectedData);
			netPeerConfiguration.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
		}
		else
		{
			netPeerConfiguration.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
		}
		if (MonoGameNetworkConfiguration.Broadcast != IPAddress.None)
		{
			netPeerConfiguration.BroadcastAddress = MonoGameNetworkConfiguration.Broadcast;
		}
		netPeer = new NetPeer(netPeerConfiguration);
		netPeer.Start();
		if (sessionType == NetworkSessionType.PlayerMatch)
		{
			GetServerList(netPeer);
		}
		else
		{
			netPeer.DiscoverLocalPeers(port);
		}
		DateTime utcNow = DateTime.UtcNow;
		discoveryMsgs = new List<NetIncomingMessage>();
		while (true)
		{
			NetIncomingMessage netIncomingMessage;
			if ((netIncomingMessage = netPeer.ReadMessage()) != null)
			{
				switch (netIncomingMessage.MessageType)
				{
				case NetIncomingMessageType.DiscoveryResponse:
					discoveryMsgs.Add(netIncomingMessage);
					break;
				case NetIncomingMessageType.UnconnectedData:
					if (netIncomingMessage.SenderEndPoint.Equals(m_masterServer))
					{
						discoveryMsgs.Add(netIncomingMessage);
					}
					break;
				}
			}
			else if ((DateTime.UtcNow - utcNow).Seconds > 2)
			{
				break;
			}
		}
		netPeer.Shutdown("Find shutting down");
	}

	private static void GetServerList(NetPeer netPeer)
	{
		m_masterServer = new IPEndPoint(NetUtility.Resolve(masterServer), masterserverport);
		NetOutgoingMessage netOutgoingMessage = netPeer.CreateMessage();
		netOutgoingMessage.Write((byte)1);
		netOutgoingMessage.Write(netPeer.Configuration.AppIdentifier);
		netPeer.SendUnconnectedMessage(netOutgoingMessage, m_masterServer);
	}

	public static void RequestNATIntroduction(IPEndPoint host, NetPeer peer)
	{
		if (host != null)
		{
			if (m_masterServer == null)
			{
				throw new Exception("Must connect to master server first!");
			}
			NetOutgoingMessage netOutgoingMessage = peer.CreateMessage();
			netOutgoingMessage.Write((byte)2);
			IPAddress address = IPAddress.Parse(GetMyLocalIpAddress());
			netOutgoingMessage.Write(new IPEndPoint(address, peer.Port));
			IPEndPoint endPoint = new IPEndPoint(host.Address, port);
			netOutgoingMessage.Write(endPoint);
			netOutgoingMessage.Write(peer.Configuration.AppIdentifier);
			peer.SendUnconnectedMessage(netOutgoingMessage, m_masterServer);
		}
	}

	internal static void FindResults(List<AvailableNetworkSession> networkSessions)
	{
		foreach (NetIncomingMessage discoveryMsg in discoveryMsgs)
		{
			AvailableNetworkSession availableNetworkSession = new AvailableNetworkSession();
			switch (discoveryMsg.MessageType)
			{
			case NetIncomingMessageType.DiscoveryResponse:
			{
				int currentGamerCount2 = discoveryMsg.ReadInt32();
				string hostGamertag2 = discoveryMsg.ReadString();
				int openPrivateGamerSlots2 = discoveryMsg.ReadInt32();
				int openPublicGamerSlots2 = discoveryMsg.ReadInt32();
				discoveryMsg.ReadBoolean();
				NetworkSessionProperties networkSessionProperties = new NetworkSessionProperties();
				int[] array = new int[networkSessionProperties.Count * 2];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = discoveryMsg.ReadInt32();
				}
				NetworkSessionProperties.ReadProperties(networkSessionProperties, array);
				availableNetworkSession.SessionProperties = networkSessionProperties;
				availableNetworkSession.SessionType = NetworkSessionType.SystemLink;
				availableNetworkSession.CurrentGamerCount = currentGamerCount2;
				availableNetworkSession.HostGamertag = hostGamertag2;
				availableNetworkSession.OpenPrivateGamerSlots = openPrivateGamerSlots2;
				availableNetworkSession.OpenPublicGamerSlots = openPublicGamerSlots2;
				availableNetworkSession.EndPoint = discoveryMsg.SenderEndPoint;
				availableNetworkSession.InternalEndpont = null;
				break;
			}
			case NetIncomingMessageType.UnconnectedData:
				if (discoveryMsg.SenderEndPoint.Equals(m_masterServer))
				{
					int currentGamerCount = discoveryMsg.ReadInt32();
					string hostGamertag = discoveryMsg.ReadString();
					int openPrivateGamerSlots = discoveryMsg.ReadInt32();
					int openPublicGamerSlots = discoveryMsg.ReadInt32();
					discoveryMsg.ReadBoolean();
					IPEndPoint internalEndpont = discoveryMsg.ReadIPEndPoint();
					IPEndPoint endPoint = discoveryMsg.ReadIPEndPoint();
					availableNetworkSession.SessionType = NetworkSessionType.PlayerMatch;
					availableNetworkSession.CurrentGamerCount = currentGamerCount;
					availableNetworkSession.HostGamertag = hostGamertag;
					availableNetworkSession.OpenPrivateGamerSlots = openPrivateGamerSlots;
					availableNetworkSession.OpenPublicGamerSlots = openPublicGamerSlots;
					availableNetworkSession.EndPoint = endPoint;
					availableNetworkSession.InternalEndpont = internalEndpont;
				}
				break;
			}
			networkSessions.Add(availableNetworkSession);
		}
	}

	internal void UpdateLiveSession(NetworkSession networkSession)
	{
		if (peer != null && m_masterServer != null && networkSession.IsHost)
		{
			NetOutgoingMessage netOutgoingMessage = peer.CreateMessage();
			netOutgoingMessage.Write((byte)0);
			netOutgoingMessage.Write(session.AllGamers.Count);
			netOutgoingMessage.Write(session.LocalGamers[0].Gamertag);
			netOutgoingMessage.Write(session.PrivateGamerSlots);
			netOutgoingMessage.Write(session.MaxGamers);
			netOutgoingMessage.Write(session.LocalGamers[0].IsHost);
			IPAddress address = IPAddress.Parse(GetMyLocalIpAddress());
			netOutgoingMessage.Write(new IPEndPoint(address, port));
			netOutgoingMessage.Write(peer.Configuration.AppIdentifier);
			peer.SendUnconnectedMessage(netOutgoingMessage, m_masterServer);
		}
	}
}
