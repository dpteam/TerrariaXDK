using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

namespace Terraria;

public sealed class Netplay
{
	public enum ClientState
	{
		JOINING,
		WAITING_FOR_PLAYER_ID,
		WAITING_FOR_PLAYER_DATA_REQ,
		RECEIVED_PLAYER_DATA_REQ,
		WAITING_FOR_WORLD_INFO,
		ANNOUNCING_SPAWN_LOCATION,
		WAITING_FOR_TILE_DATA,
		PLAYING
	}

	public static bool PlayDisconnect;

	public static bool ToStopSession;

	public static bool HasHookEvents;

	public static bool isJoiningRemoteInvite;

	public static LocalNetworkGamer gamer;

	public static List<SignedInGamer> gamersWhoReceivedInvite = new List<SignedInGamer>(4);

	public static List<SignedInGamer> gamersWaitingToJoinInvite = new List<SignedInGamer>(4);

	public static List<UI> gamersWaitingForPlayerId = new List<UI>(4);

	public static List<UI> gamersWaitingToSendSpawn = new List<UI>(4);

	public static List<UI> gamersWaitingToSpawn = new List<UI>(4);

	public static List<NetClient> ClientList = new List<NetClient>(7);

	public static bool[] playerSlots = new bool[8];

	public static List<JoinableSession> availableSessions = new List<JoinableSession>(32);

	private static Thread sessionFinderThread = null;

	private static volatile bool stopSessionFinderThread = false;

	public static NetworkSession Session;

	public static InviteAcceptedEventArgs CurInvite;

	public static Thread sessionThread;

	public static AutoResetEvent SessionReadyEvent = new AutoResetEvent(initialState: false);

	public static ClientState clientState;

	public static int clientStatusCount;

	public static int clientStatusMax;

	public static void Init()
	{
		PlayDisconnect = false;
		HasHookEvents = false;
		ToStopSession = false;
		clientState = ClientState.JOINING;
		clientStatusCount = 0;
		clientStatusMax = 0;
	}

	public static void ResetSections()
	{
		for (int num = ClientList.Count - 1; num >= 0; num--)
		{
			ClientList[num].ResetSections();
		}
	}

	public static void ResetSections(ref Vector2i min, ref Vector2i max)
	{
		for (int num = ClientList.Count - 1; num >= 0; num--)
		{
			ClientList[num].ResetSections(ref min, ref max);
		}
	}

	public static void CreateSession()
	{
		NetworkSessionType sessionType = ((Main.NetMode != 0) ? NetworkSessionType.PlayerMatch : NetworkSessionType.Local);
		try
		{
			List<SignedInGamer> list = new List<SignedInGamer>(1);
			list.Add(UI.MainUI.SignedInGamer);
			NetworkSessionProperties networkSessionProperties = new NetworkSessionProperties();
			ulong num = 7934076125uL;
			if (UI.MainUI.IsInviteOnly)
			{
				networkSessionProperties[2] = -559038737;
			}
			else
			{
				networkSessionProperties[0] = (int)num;
				networkSessionProperties[1] = (int)(num >> 32);
				networkSessionProperties[2] = 2;
			}
			int maxGamers = 8;
			int privateGamerSlots = 0;
			if (!Main.IsTutorial() && UI.MainUI.IsInviteOnly)
			{
				privateGamerSlots = 7;
			}
			Session = NetworkSession.Create(sessionType, list, maxGamers, privateGamerSlots, networkSessionProperties);
			Session.AllowJoinInProgress = true;
			Session.AllowHostMigration = false;
			HasHookEvents = true;
			Session.StartGame();
		}
		catch (Exception)
		{
			UI.Error(Lang.MenuText[5], Lang.InterfaceText[20]);
			UI.MainUI.CurMenuType = MenuType.MAIN;
			Main.NetMode = 0;
			PlayDisconnect = true;
		}
	}

	public static void HookSessionEvents()
	{
		Session.GamerJoined += GamerJoinedEventHandler;
		Session.GamerLeft += GamerLeftEventHandler;
		Session.GameEnded += GameEndedEventHandler;
		Session.SessionEnded += SessionEndedEventHandler;
		HasHookEvents = false;
	}

	public static void DisposeSession()
	{
		Session.GamerJoined -= GamerJoinedEventHandler;
		Session.GamerLeft -= GamerLeftEventHandler;
		Session.GameEnded -= GameEndedEventHandler;
		Session.SessionEnded -= SessionEndedEventHandler;
		Session.Dispose();
		Session = null;
	}

	public static void Disconnect()
	{
		if (Session != null)
		{
			if (Main.NetMode != 1)
			{
				if (Session.SessionState == NetworkSessionState.Playing)
				{
					Session.EndGame();
					Session.Update();
				}
				ClientList.Clear();
				for (int num = 7; num >= 0; num--)
				{
					playerSlots[num] = false;
				}
			}
			DisposeSession();
			gamer = null;
			gamersWaitingForPlayerId.Clear();
			gamersWaitingToSendSpawn.Clear();
			gamersWaitingToSpawn.Clear();
			for (int i = 0; i < 4; i++)
			{
				Main.UIInstance[i].LeaveSession();
			}
		}
		PlayDisconnect = false;
		HasHookEvents = false;
		ToStopSession = false;
		Main.NetMode = 0;
	}

	public static void SetAsRemotePlayerSlot(int i)
	{
		Player player = Main.PlayerSet[i];
		UI ui = player.ui;
		if (ui != null)
		{
			if (player.CurrentView != null)
			{
				ui.setPlayer(-1);
			}
			else
			{
				ui.SetPlayer(null);
			}
		}
	}

	private static void GamerJoinedEventHandler(object sender, GamerJoinedEventArgs e)
	{
		NetworkGamer networkGamer = e.Gamer;
		if (Main.NetMode == 0)
		{
			for (int i = 0; i < 4; i++)
			{
				UI uI = Main.UIInstance[i];
				if (uI.wasRemovedFromSessionWithoutOurConsent)
				{
					SignedInGamer signedInGamer = uI.SignedInGamer;
					if (signedInGamer != null && signedInGamer.Gamertag == networkGamer.Gamertag)
					{
						networkGamer.Tag = uI.ActivePlayer;
						return;
					}
				}
			}
		}
		else if (!networkGamer.IsLocal)
		{
			Main.CheckUserGeneratedContent = true;
		}
		int j = 0;
		Player player = null;
		if (Main.NetMode != 1)
		{
			for (; playerSlots[j]; j++)
			{
			}
			playerSlots[j] = true;
		}
		if (networkGamer.IsLocal)
		{
			LocalNetworkGamer localNetworkGamer = (LocalNetworkGamer)networkGamer;
			SignedInGamer signedInGamer2 = localNetworkGamer.SignedInGamer;
			UI uI2 = Main.UIInstance[(int)signedInGamer2.PlayerIndex];
			uI2.localGamer = localNetworkGamer;
			if (Main.NetMode != 1)
			{
				uI2.JoinSession(j);
				object obj = (networkGamer.Tag = Main.PlayerSet[j]);
				object obj3 = obj;
				player = (Player)obj3;
				player.client = null;
				if (networkGamer.IsHost)
				{
					SessionReadyEvent.Set();
				}
				else
				{
					Main.JoinGame(uI2);
				}
			}
			else
			{
				gamersWaitingForPlayerId.Add(uI2);
			}
			if (gamer == null)
			{
				gamer = localNetworkGamer;
			}
		}
		else if (Main.NetMode == 2)
		{
			SetAsRemotePlayerSlot(j);
			NetClient netClient = null;
			for (int num = ClientList.Count - 1; num >= 0; num--)
			{
				if (ClientList[num].Machine == networkGamer.Machine)
				{
					netClient = ClientList[num];
					break;
				}
			}
			if (netClient == null)
			{
				netClient = new NetClient(networkGamer);
				ClientList.Add(netClient);
				NetMessage.syncPlayers();
			}
			player = Main.PlayerSet[j];
			netClient.GamerJoined(player);
			networkGamer.Tag = player;
			NetMessage.SendPlayerId(networkGamer, j);
		}
		else if (Main.NetMode == 1 && gamer != null)
		{
			NetMessage.CreateMessage0(11);
			NetMessage.SendMessage();
		}
	}

	private static void GamerLeftEventHandler(object sender, GamerLeftEventArgs e)
	{
		NetworkGamer networkGamer = e.Gamer;
		Player player = networkGamer.Tag as Player;
		if (networkGamer.IsLocal)
		{
			UI uI = Main.UIInstance[(int)((LocalNetworkGamer)networkGamer).SignedInGamer.PlayerIndex];
			if (Main.NetMode == 0 && uI.wasRemovedFromSessionWithoutOurConsent)
			{
				return;
			}
			uI.LeaveSession();
		}
		if (Main.NetMode == 1)
		{
			return;
		}
		int whoAmI = player.WhoAmI;
		player.Active = 0;
		playerSlots[whoAmI] = false;
		if (Main.NetMode != 2)
		{
			return;
		}
		if (networkGamer.IsLocal)
		{
			if (UI.MainUI.IsInviteOnly)
			{
				Session.PrivateGamerSlots++;
			}
		}
		else
		{
			NetClient client = player.client;
			if (client.GamerLeft(player))
			{
				ClientList.Remove(client);
			}
			else if (networkGamer == client.NetGamer)
			{
				client.NetGamer = client.Machine.Gamers[0];
			}
		}
		NetMessage.CreateMessage2(14, whoAmI, 0);
		NetMessage.SendMessage();
		if (player.IsAnnounced)
		{
			player.IsAnnounced = false;
			NetMessage.SendText(player.oldName, 33, 255, 240, 20, -1);
		}
	}

	private static void GameEndedEventHandler(object sender, GameEndedEventArgs e)
	{
		if (Main.NetMode == 1)
		{
			UI.Error(Lang.InterfaceText[66], Lang.InterfaceText[67]);
			Main.SaveOnExit = UI.MainUI.autoSave;
			UI.MainUI.ExitGame();
		}
	}

	private static void SessionEndedEventHandler(object sender, NetworkSessionEndedEventArgs e)
	{
		if (UI.MainUI.CurMenuMode != MenuMode.ERROR)
		{
			string desc;
			switch (e.EndReason)
			{
			case NetworkSessionEndReason.ClientSignedOut:
				GameEndedEventHandler(sender, new GameEndedEventArgs());
				return;
			case NetworkSessionEndReason.HostEndedSession:
			case NetworkSessionEndReason.RemovedByHost:
				desc = Lang.WorldGenText[46];
				break;
			default:
				desc = ((Main.NetMode != 1) ? Lang.InterfaceText[36] : Lang.WorldGenText[46]);
				break;
			}
			UI.Error(Lang.MenuText[5], desc);
			Main.SaveOnExit = UI.MainUI.autoSave;
			UI.MainUI.ExitGame();
		}
	}

	public static void StartServer()
	{
		SessionReadyEvent.Reset();
		Thread thread = new Thread(ServerLoop);
		thread.IsBackground = true;
		thread.Start();
		sessionThread = thread;
	}

	public static void ServerLoop()
	{
		Init();
		CreateSession();
		while (!PlayDisconnect && Session != null)
		{
			Thread.Sleep(16);
		}
		ToStopSession = true;
		sessionThread = null;
	}

	public static void StartClient()
	{
		Thread thread = new Thread(ClientLoop);
		thread.IsBackground = true;
		thread.Start();
		sessionThread = thread;
	}

	public static void ClientLoop()
	{
		Init();
		UI.MainUI.ActivePlayer.hostile = false;
		UI.MainUI.ActivePlayer.NetClone(UI.MainUI.netPlayer);
		for (int i = 0; i < 8; i++)
		{
			if (i != UI.MainUI.MyPlayer)
			{
				Main.PlayerSet[i].Active = 0;
			}
		}
		Main.MaxTilesX = 4200;
		Main.MaxTilesY = 1200;
		WorldGen.clearWorld();
		if (UI.MainUI.CurMenuMode == MenuMode.NETPLAY)
		{
			Main.NetMode = 1;
			WorldSelect.isLocalWorld = false;
			try
			{
				if (isJoiningRemoteInvite)
				{
					isJoiningRemoteInvite = false;
					Session = NetworkSession.JoinInvited(gamersWaitingToJoinInvite);
					gamersWaitingToJoinInvite.Clear();
					gamersWhoReceivedInvite.Clear();
				}
				else
				{
					Session = NetworkSession.Join(WorldSelect.Session());
				}
			}
			catch (Exception)
			{
				UI.Error(Lang.MenuText[5], Lang.InterfaceText[21], rememberPreviousMenu: true);
				UI.MainUI.CurMenuType = MenuType.MAIN;
				Main.NetMode = 0;
				PlayDisconnect = true;
				ToStopSession = true;
				goto IL_02fd;
			}
			HasHookEvents = true;
			while (!PlayDisconnect && Session != null)
			{
				switch (clientState)
				{
				case ClientState.JOINING:
					UI.MainUI.FirstProgressStep(3, Lang.MenuText[8]);
					clientState = ClientState.WAITING_FOR_PLAYER_ID;
					break;
				case ClientState.WAITING_FOR_PLAYER_ID:
					if (UI.MainUI.Progress <= 0.999f)
					{
						UI.MainUI.Progress = UI.MainUI.Progress + 0.001f;
					}
					break;
				case ClientState.WAITING_FOR_PLAYER_DATA_REQ:
					if (UI.MainUI.Progress <= 0.999f)
					{
						UI.MainUI.Progress = UI.MainUI.Progress + 0.001f;
					}
					break;
				case ClientState.RECEIVED_PLAYER_DATA_REQ:
					UI.MainUI.NextProgressStep(Lang.MenuText[73]);
					clientState = ClientState.WAITING_FOR_WORLD_INFO;
					break;
				case ClientState.WAITING_FOR_WORLD_INFO:
					if (UI.MainUI.Progress <= 0.999f)
					{
						UI.MainUI.Progress = UI.MainUI.Progress + 0.001f;
					}
					break;
				case ClientState.WAITING_FOR_TILE_DATA:
					if (clientStatusMax > 0)
					{
						if (clientStatusCount >= clientStatusMax)
						{
							clientStatusMax = 0;
							clientStatusCount = 0;
							UI.MainUI.Progress = 1f;
						}
						else
						{
							UI.MainUI.statusText = Lang.InterfaceText[44];
							UI.MainUI.Progress = (float)clientStatusCount / (float)clientStatusMax;
						}
					}
					break;
				}
				Thread.Sleep(0);
			}
			clientStatusCount = 0;
			clientStatusMax = 0;
			ToStopSession = true;
		}
		goto IL_02fd;
		IL_02fd:
		sessionThread = null;
	}

	public static void InviteAccepted()
	{
		PlayerIndex playerIndex = CurInvite.Gamer.PlayerIndex;
		if (Main.IsTrial)
		{
			MessageBox.Show(playerIndex, Lang.MenuText[5], Lang.InterfaceText[69], new string[1] { Lang.MenuText[90] });
		}
		else if (!UI.AllPlayersCanPlayOnline())
		{
			MessageBox.Show(playerIndex, Lang.MenuText[5], Lang.InterfaceText[68], new string[1] { Lang.MenuText[90] });
		}
		else
		{
			Main.UIInstance[(int)playerIndex].InviteAccepted(CurInvite);
		}
		CurInvite = null;
	}

	public static void NetworkSession_InviteAccepted(object sender, InviteAcceptedEventArgs e)
	{
		if (CurInvite == null)
		{
			CurInvite = e;
		}
	}

	public static bool IsFindingSessions()
	{
		return sessionFinderThread != null;
	}

	public static void StopFindingSessions()
	{
		availableSessions.Clear();
		if (sessionFinderThread != null)
		{
			stopSessionFinderThread = true;
		}
	}

	public static void FindSessions()
	{
		StopFindingSessions();
		if (UI.MainUI.CanPlayOnline())
		{
			if (sessionFinderThread != null)
			{
				sessionFinderThread.Join();
			}
			sessionFinderThread = new Thread(FindSessionsThread);
			sessionFinderThread.IsBackground = true;
			sessionFinderThread.Start();
		}
	}

	/// <summary>
	/// LAN discovery for the built-in transport: broadcasts a UDP probe and turns every reply
	/// into a joinable session. Runs before the legacy Xbox Live friend lookup.
	/// </summary>
	private static bool DiscoverLanSessions()
	{
		if (!Terraria.Net.NetworkConfig.UseTcpTransport)
		{
			return false;
		}
		List<AvailableNetworkSession> results = new List<AvailableNetworkSession>(16);
		try
		{
			Terraria.Net.TcpPeer.Discover(results);
		}
		catch (Exception ex)
		{
			Program.Log("[Netplay] LAN discovery failed: " + ex.Message);
		}
		bool added = false;
		if (Terraria.Net.NetworkConfig.TryParseAddress(Terraria.Net.NetworkConfig.JoinAddress, out var host, out var port))
		{
			try
			{
				System.Net.IPAddress[] addresses = System.Net.Dns.GetHostAddresses(host);
				if (addresses.Length != 0)
				{
					AvailableNetworkSession manual = new AvailableNetworkSession(0, host, 0, 7, new NetworkSessionProperties(), new QualityOfService());
					manual.SessionType = NetworkSessionType.SystemLink;
					manual.EndPoint = new System.Net.IPEndPoint(addresses[0], port);
					results.Add(manual);
				}
			}
			catch (Exception ex2)
			{
				Program.Log("[Netplay] Could not resolve JoinAddress: " + ex2.Message);
			}
		}
		lock (availableSessions)
		{
			for (int i = 0; i < results.Count; i++)
			{
				bool duplicate = false;
				for (int j = 0; j < availableSessions.Count; j++)
				{
					if (availableSessions[j].AvailableSession.EndPoint == results[i].EndPoint)
					{
						duplicate = true;
						break;
					}
				}
				if (!duplicate)
				{
					availableSessions.Add(new JoinableSession(results[i]));
					added = true;
				}
			}
		}
		return added;
	}

	/// <summary>
	/// Joins a server given as "host" or "host:port". Resolves the name, registers it as a
	/// joinable session and reports a readable error instead of hanging on a bad address.
	/// </summary>
	public static bool JoinDirect(string address, out string error)
	{
		error = null;
		string host;
		int port;
		if (!Terraria.Net.NetworkConfig.TryParseAddress(address, out host, out port))
		{
			error = "Use 'host' or 'host:port' (port 1-65535).";
			return false;
		}
		System.Net.IPAddress[] addresses;
		try
		{
			addresses = System.Net.Dns.GetHostAddresses(host);
		}
		catch (Exception ex)
		{
			error = "Could not resolve '" + host + "': " + ex.Message;
			return false;
		}
		if (addresses.Length == 0)
		{
			error = "Could not resolve '" + host + "'.";
			return false;
		}
		System.Net.IPAddress address2 = null;
		for (int i = 0; i < addresses.Length; i++)
		{
			// Prefer IPv4: the transport binds an IPv4 listener.
			if (addresses[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
			{
				address2 = addresses[i];
				break;
			}
		}
		if (address2 == null)
		{
			address2 = addresses[0];
		}
		AvailableNetworkSession manual = new AvailableNetworkSession(0, host, 0, Terraria.Net.NetworkConfig.MaxPlayers, new NetworkSessionProperties(), new QualityOfService());
		manual.SessionType = NetworkSessionType.SystemLink;
		manual.EndPoint = new System.Net.IPEndPoint(address2, port);
		lock (availableSessions)
		{
			for (int j = 0; j < availableSessions.Count; j++)
			{
				if (availableSessions[j].AvailableSession.EndPoint.Equals(manual.EndPoint))
				{
					return true;
				}
			}
			availableSessions.Add(new JoinableSession(manual));
		}
		Program.LogInfo("Netplay", "direct connect queued for " + address);
		return true;
	}

	/// <summary>
	/// Appends the public server list from the master server to the joinable sessions.
	/// Runs on the finder thread and never blocks the game; an unreachable master simply
	/// leaves the list as LAN-only. Cached entries are used when the master is down.
	/// </summary>
	private static void DiscoverMasterServers()
	{
		if (!Terraria.Net.NetworkConfig.UseTcpTransport)
		{
			return;
		}
		List<Terraria.Net.MasterServerEntry> entries;
		try
		{
			entries = Terraria.Net.MasterServerClient.Fetch();
		}
		catch (Exception ex)
		{
			Program.LogWarn("Netplay", "master server lookup failed: " + ex.Message);
			entries = new List<Terraria.Net.MasterServerEntry>();
		}
		if (entries.Count == 0)
		{
			entries = Terraria.Net.MasterServerClient.FetchCached();
		}
		if (entries.Count == 0)
		{
			return;
		}
		System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
		lock (availableSessions)
		{
			for (int i = 0; i < entries.Count && !stopSessionFinderThread; i++)
			{
				System.Net.IPEndPoint endPoint = ResolveMasterEntry(entries[i], stopwatch);
				if (endPoint == null)
				{
					continue;
				}
				bool duplicate = false;
				for (int j = 0; j < availableSessions.Count; j++)
				{
					if (availableSessions[j].AvailableSession.EndPoint != null && availableSessions[j].AvailableSession.EndPoint.Equals(endPoint))
					{
						duplicate = true;
						break;
					}
				}
				if (duplicate)
				{
					continue;
				}
				AvailableNetworkSession availableNetworkSession = new AvailableNetworkSession(entries[i].Players, entries[i].Name, 0, Math.Max(1, entries[i].MaxPlayers), new NetworkSessionProperties(), new QualityOfService());
				// SystemLink is the "plain TCP endpoint" type: it makes the join path skip
				// Xbox Live lookups and go straight to the address.
				availableNetworkSession.SessionType = NetworkSessionType.SystemLink;
				availableNetworkSession.EndPoint = endPoint;
				availableNetworkSession.Passworded = entries[i].Passworded;
				availableNetworkSession.ProtocolVersion = entries[i].ProtocolVersion;
				// Only trust the listing when it actually carries a signature; an older
				// master may omit the field, and "unknown" must not mean "incompatible".
				availableNetworkSession.ModMismatch = !string.IsNullOrEmpty(entries[i].ModSignature) && !Terraria.Mods.ModLoader.IsCompatible(entries[i].ModSignature);
				availableSessions.Add(new JoinableSession(availableNetworkSession));
			}
		}
	}

	/// <summary>
	/// Turns a listing into an endpoint. Literal addresses are free; names are resolved
	/// only while there is time budget left, so one slow DNS server cannot stall the list.
	/// </summary>
	private static System.Net.IPEndPoint ResolveMasterEntry(Terraria.Net.MasterServerEntry entry, System.Diagnostics.Stopwatch budget)
	{
		System.Net.IPAddress address;
		if (System.Net.IPAddress.TryParse(entry.Host, out address))
		{
			return new System.Net.IPEndPoint(address, entry.Port);
		}
		if (budget.ElapsedMilliseconds > 3000)
		{
			return null;
		}
		try
		{
			System.Net.IPAddress[] addresses = System.Net.Dns.GetHostAddresses(entry.Host);
			for (int i = 0; i < addresses.Length; i++)
			{
				if (addresses[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
				{
					return new System.Net.IPEndPoint(addresses[i], entry.Port);
				}
			}
		}
		catch (Exception ex)
		{
			Program.LogInfo("Netplay", "could not resolve listed server '" + entry.Host + "': " + ex.Message);
		}
		return null;
	}

	public static void FindSessionsThread()
	{
		DiscoverLanSessions();
		DiscoverMasterServers();
		NetworkSessionProperties networkSessionProperties = new NetworkSessionProperties();
		networkSessionProperties[2] = 2;
		UI mainUI = UI.MainUI;
		if (mainUI.HasOnline())
		{
			SignedInGamer signedInGamer = mainUI.SignedInGamer;
			if (signedInGamer != null)
			{
				try
				{
					List<SignedInGamer> list = new List<SignedInGamer>(1);
					list.Add(signedInGamer);
					FriendCollection friends = signedInGamer.GetFriends();
					int num = friends.Count - 1;
					while (num >= 0 && !stopSessionFinderThread)
					{
						if (friends[num].IsJoinable)
						{
							ulong num2 = (ulong)(7934076125L + num);
							networkSessionProperties[0] = (int)num2;
							networkSessionProperties[1] = (int)(num2 >> 32);
							AvailableNetworkSessionCollection availableNetworkSessionCollection = NetworkSession.Find(NetworkSessionType.PlayerMatch, list, networkSessionProperties);
							if (availableNetworkSessionCollection.Count > 0)
							{
								lock (availableSessions)
								{
									availableSessions.Add(new JoinableSession(availableNetworkSessionCollection[0]));
								}
							}
							if (stopSessionFinderThread)
							{
								break;
							}
							Thread.Sleep(5000);
						}
						num--;
					}
				}
				catch (Exception)
				{
				}
			}
		}
		stopSessionFinderThread = false;
		sessionFinderThread = null;
	}

	public static void CheckOfflineSession()
	{
		for (int i = 0; i < 4; i++)
		{
			UI uI = Main.UIInstance[i];
			if (uI.wasRemovedFromSessionWithoutOurConsent)
			{
				if (Session.SessionState == NetworkSessionState.Playing)
				{
					Session.AddLocalGamer(uI.SignedInGamer);
					Session.Update();
					uI.wasRemovedFromSessionWithoutOurConsent = false;
				}
				else if (uI == UI.MainUI)
				{
					DisposeSession();
					CreateSession();
					HookSessionEvents();
					uI.wasRemovedFromSessionWithoutOurConsent = false;
				}
			}
		}
	}
}
