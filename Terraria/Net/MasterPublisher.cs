using System;
using System.Threading;

namespace Terraria.Net;

/// <summary>
/// Keeps this host listed on the master server while it is running.
///
/// Off unless <c>[Network] PublishToMaster</c> is set: publishing your address is a
/// deliberate choice, and the host is the only one who can make it.
///
/// All network work happens on its own thread. <see cref="Start"/> and <see cref="Stop"/>
/// only flip a flag and wake the thread, so leaving a world never waits on the network.
/// </summary>
internal static class MasterPublisher
{
	/// <summary>The master drops a listing that has not been refreshed for a couple of minutes.</summary>
	private const int IntervalMs = 45000;

	private static Thread thread;

	private static ManualResetEvent wake;

	private static volatile bool stopping = true;

	private static int port;

	public static bool IsRunning => thread != null;

	/// <summary>Starts announcing. Does nothing when publishing is disabled.</summary>
	public static void Start(int activePort)
	{
		Stop();
		if (!MasterServerClient.Publish || activePort <= 0)
		{
			return;
		}
		// Singleplayer also runs a loopback host; only a real online host (NetMode 2) may
		// be advertised. Publishing a singleplayer world would expose it for nothing.
		if (Main.NetMode != 2)
		{
			return;
		}
		port = activePort;
		stopping = false;
		wake = new ManualResetEvent(initialState: false);
		thread = new Thread(Loop)
		{
			IsBackground = true,
			Name = "TerrariaXDK.Master"
		};
		thread.Start();
		Program.LogInfo("Master", "publishing this world on the master server");
	}

	/// <summary>Stops announcing and asks the master to drop the entry. Never blocks.</summary>
	public static void Stop()
	{
		stopping = true;
		ManualResetEvent manualResetEvent = wake;
		wake = null;
		thread = null;
		if (manualResetEvent != null)
		{
			try
			{
				manualResetEvent.Set();
			}
			catch
			{
			}
		}
	}

	private static void Loop()
	{
		ManualResetEvent manualResetEvent = wake;
		while (!stopping)
		{
			try
			{
				MasterServerClient.Announce(BuildEntry(port));
			}
			catch (Exception ex)
			{
				Program.LogWarn("Master", "announce failed: " + ex.Message);
			}
			if (manualResetEvent == null)
			{
				break;
			}
			// Woken by Stop(); a timeout means "announce again".
			if (manualResetEvent.WaitOne(IntervalMs))
			{
				break;
			}
		}
		try
		{
			// Best effort: even if this fails, the entry expires on its own.
			MasterServerClient.Remove(ResolvePublicHost(), port);
		}
		catch
		{
		}
	}

	/// <summary>
	/// Builds the record sent to the master. Only plain statics are read, so this is safe
	/// to call from the publisher thread.
	/// </summary>
	internal static MasterServerEntry BuildEntry(int activePort)
	{
		MasterServerEntry masterServerEntry = new MasterServerEntry();
		masterServerEntry.Name = string.IsNullOrEmpty(Main.WorldName) ? "Terraria world" : Main.WorldName;
		masterServerEntry.Host = ResolvePublicHost();
		masterServerEntry.Port = activePort <= 0 ? NetworkConfig.Port : activePort;
		masterServerEntry.Players = 1 + Netplay.ClientList.Count;
		masterServerEntry.MaxPlayers = NetworkConfig.MaxPlayers + 1;
		masterServerEntry.ProtocolVersion = TcpPeer.ProtocolVersion;
		masterServerEntry.Passworded = !string.IsNullOrEmpty(NetworkConfig.Password);
		masterServerEntry.WorldName = Main.WorldName ?? string.Empty;
		masterServerEntry.ModSignature = Terraria.Mods.ModLoader.Signature;
		return masterServerEntry;
	}

	/// <summary>
	/// The address to advertise. Empty means "let the master use the address it sees",
	/// which is the only correct answer behind NAT; set <c>[Network] PublicAddress</c>
	/// when the host is reached through a forwarded port or a hostname.
	/// </summary>
	private static string ResolvePublicHost()
	{
		return NetworkConfig.PublicAddress ?? string.Empty;
	}
}
