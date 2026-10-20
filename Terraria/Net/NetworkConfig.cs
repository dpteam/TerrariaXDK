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

	/// <summary>Optional join password; empty disables the password request.</summary>
	public static string Password = string.Empty;

	/// <summary>Parses "host", "host:port" or "[ipv6]:port".</summary>
	public static bool TryParseAddress(string text, out string host, out int port)
	{
		host = null;
		port = Port;
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
			return host.Length > 0;
		}
		if (text.StartsWith("[") && text.EndsWith("]"))
		{
			host = text.Substring(1, text.Length - 2);
			return host.Length > 0;
		}
		host = text;
		return true;
	}
}
