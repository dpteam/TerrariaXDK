namespace Terraria.Net;

/// <summary>
/// Runtime configuration for the built-in multiplayer stack (Settings.ini: [Network]).
/// </summary>
internal static class NetworkConfig
{
	/// <summary>Port the in-process host listens on.</summary>
	public static int Port = 7777;

	/// <summary>Optional "host:port" shortcut offered in the netplay menu.</summary>
	public static string JoinAddress = string.Empty;

	/// <summary>Maximum number of remote players accepted by the host.</summary>
	public static int MaxPlayers = 7;

	/// <summary>
	/// When true (default) multiplayer runs over the in-process TCP transport. Set to false
	/// to fall back to the legacy MonoGame/Lidgren peer.
	/// </summary>
	public static bool UseTcpTransport = true;

	/// <summary>Actual port the listener bound to (0 until hosting starts).</summary>
	public static int ActivePort;

	/// <summary>UDP port used for LAN discovery probes.</summary>
	public static int DiscoveryPort = 7778;

	/// <summary>
	/// How long a client waits for a host to accept the TCP connection. Kept short because
	/// the game blocks inside Join() while this runs, with a frozen screen.
	/// </summary>
	public static int ConnectTimeoutMs = 5000;

	/// <summary>
	/// Master server used for the public server list. Empty disables online browsing;
	/// LAN discovery keeps working regardless.
	/// </summary>
	public static string MasterServer = "play.dartpower.xyz";

	/// <summary>Port of the master server.</summary>
	public static int MasterServerPort = 17777;

	/// <summary>Optional join password; empty disables the password request.</summary>
	public static string Password = string.Empty;

	/// <summary>
	/// Address advertised to the master server. Empty means "use the address the master
	/// sees", which is what a home connection behind NAT needs.
	/// </summary>
	public static string PublicAddress = string.Empty;

	/// <summary>
	/// Host through the master server's relay (<c>[Network] AllowRelay</c>).
	///
	/// Off by default on purpose: a relay adds a hop of latency and spends somebody else's
	/// bandwidth, so it is a fallback for hosts that cannot be reached directly, not a mode.
	/// </summary>
	public static bool AllowRelay;

	/// <summary>
	/// "host:port" of the relay. Empty means "the master's host, <see cref="RelayPort"/>", which
	/// is the normal case: one machine publishes the list and relays the games.
	/// </summary>
	public static string RelayServer = string.Empty;

	/// <summary>Relay port used when <see cref="RelayServer"/> carries no port of its own.</summary>
	public static int RelayPort = 17778;

	/// <summary>
	/// Room this host is reachable under while relaying, or empty. Set when the room opens and
	/// carried in the master announcement, so a client can tell "this entry is relayed".
	/// </summary>
	public static string RelayRoom = string.Empty;

	/// <summary>Parses "host", "host:port" or "[ipv6]:port".</summary>
	public static bool TryParseAddress(string text, out string host, out int port)
	{
		return TryParseAddress(text, Port, out host, out port);
	}

	/// <summary>
	/// Parses "host" or "host:port", with <paramref name="defaultPort"/> used when no port is
	/// written.
	///
	/// The default matters: this method is used for both game servers and the master server, and
	/// they listen on different ports (7777 and 17777). Defaulting everything to the game port
	/// made "play.example.com" resolve to :7777, so a master server configured without an explicit
	/// port was silently never reached - the list looked empty rather than broken.
	/// </summary>
	public static bool TryParseAddress(string text, int defaultPort, out string host, out int port)
	{
		host = null;
		port = defaultPort;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		text = text.Trim();
		int num = text.LastIndexOf(':');
		if (num > 0 && num < text.Length - 1 && text.IndexOf(':') == num)
		{
			if (!int.TryParse(text.Substring(num + 1), out port) || port <= 0 || port > 65535)
			{
				return false;
			}
			host = text.Substring(0, num);
			return IsPlausibleHost(host);
		}
		if (text.StartsWith("[") && text.EndsWith("]"))
		{
			host = text.Substring(1, text.Length - 2);
			return IsPlausibleHost(host);
		}
		host = text;
		return IsPlausibleHost(host);
	}

	/// <summary>
	/// A hostname or address never contains whitespace. Without this, a stray line in a
	/// config file ("bogus line") became a "valid" server at the default port.
	/// </summary>
	private static bool IsPlausibleHost(string host)
	{
		if (string.IsNullOrEmpty(host))
		{
			return false;
		}
		for (int i = 0; i < host.Length; i++)
		{
			char c = host[i];
			if (char.IsWhiteSpace(c) || c == '/' || c == '\\')
			{
				return false;
			}
		}
		return true;
	}
}
