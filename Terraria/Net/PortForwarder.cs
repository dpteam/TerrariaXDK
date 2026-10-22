using System;
using System.Threading;

namespace Terraria.Net;

/// <summary>
/// Asks the router to forward the game's port, in the background.
///
/// This is the first step of the hosting order in <c>IDEAS.md</c>: a direct connection is better
/// than the relay in every way - lower latency, and no traffic through somebody else's machine -
/// so it is tried first, and the relay is the fallback for when it does not work.
///
/// Never blocks. A router can take seconds to answer, or never answer at all, and a host waiting
/// on a router before the world appears would be a worse bug than the one this fixes. Everything
/// happens on its own thread and the result only lands in the log and in
/// <see cref="ExternalAddress"/>.
/// </summary>
internal static class PortForwarder
{
	private static Thread _thread;

	private static UPnP.Gateway _gateway;

	private static int _port;

	private static volatile bool _stopping;

	/// <summary>
	/// True when this run created the router rule, so <see cref="Stop"/> may remove it. A rule
	/// that was already there belongs to the player, not to the game.
	/// </summary>
	private static bool _ownsMapping;

	/// <summary>True while the mapping is in place.</summary>
	public static bool IsMapped { get; private set; }

	/// <summary>The address the router reports for this connection, or empty.</summary>
	public static string ExternalAddress { get; private set; } = string.Empty;

	/// <summary>Why forwarding did not happen, or empty when it did.</summary>
	public static string LastError { get; private set; } = string.Empty;

	/// <summary>
	/// Starts forwarding <paramref name="port"/>. Does nothing when the feature is off, when the
	/// host is not an online host, or when a run is already in flight.
	/// </summary>
	public static void Start(int port)
	{
		Stop();
		if (!NetworkConfig.UseUPnP || port <= 0)
		{
			return;
		}
		// A singleplayer world also runs a loopback host; there is nothing to forward.
		if (Main.NetMode != 2)
		{
			return;
		}
		_port = port;
		_stopping = false;
		_thread = new Thread(Run)
		{
			IsBackground = true,
			Name = "TerrariaXDK.UPnP"
		};
		_thread.Start();
	}

	/// <summary>
	/// Stops forwarding and removes the mapping. Returns immediately: the router round trip
	/// happens on a short-lived background thread, so leaving a world never waits on the network.
	/// </summary>
	public static void Stop()
	{
		_stopping = true;
		Thread thread = _thread;
		_thread = null;
		UPnP.Gateway gateway = _gateway;
		int port = _port;
		bool flag = _ownsMapping;
		_gateway = null;
		_ownsMapping = false;
		IsMapped = false;
		ExternalAddress = string.Empty;
		if (gateway == null || port <= 0)
		{
			return;
		}
		if (!flag)
		{
			// The rule was already there when hosting started - most likely the player forwarded
			// the port by hand. Deleting it would break the very setup that made hosting work.
			Program.LogInfo("UPnP", "leaving the existing port " + port + " forward in place");
			return;
		}
		Thread thread2 = new Thread(delegate()
		{
			string error;
			if (!UPnP.Unmap(gateway, port, out error))
			{
				Program.LogWarn("UPnP", "could not remove the mapping for port " + port + ": " + error);
			}
		})
		{
			IsBackground = true,
			Name = "TerrariaXDK.UPnP.Cleanup"
		};
		thread2.Start();
	}

	private static void Run()
	{
		try
		{
			string error;
			UPnP.Gateway gateway = UPnP.Discover(2000, out error);
			if (gateway == null)
			{
				LastError = error;
				Program.LogInfo("UPnP", "no port forwarding: " + error);
				return;
			}
			if (_stopping)
			{
				return;
			}
			string text = UPnP.LocalAddressFor(gateway.Host);
			if (text == null)
			{
				LastError = "cannot determine this machine's address on the gateway's network";
				Program.LogWarn("UPnP", LastError);
				return;
			}
			if (!UPnP.Map(gateway, _port, text, out bool flag2, out error))
			{
				LastError = error;
				Program.LogWarn("UPnP", error);
				return;
			}
			_gateway = gateway;
			_ownsMapping = flag2;
			IsMapped = true;
			LastError = string.Empty;
			string text2 = UPnP.ExternalAddress(gateway, out error);
			ExternalAddress = (text2 == null) ? string.Empty : text2;
			Program.LogInfo("UPnP", "port " + _port + " forwarded to " + text
				+ (ExternalAddress.Length == 0 ? "" : "; reachable at " + ExternalAddress + ":" + _port));
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
			Program.LogWarn("UPnP", "forwarding failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}
