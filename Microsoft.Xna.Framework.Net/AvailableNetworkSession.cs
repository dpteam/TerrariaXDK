using System.Net;

namespace Microsoft.Xna.Framework.Net;

/// <summary>
/// A joinable session as advertised by discovery (or listed by the master server).
///
/// The decompiled stub had every property read and write its own name
/// (<c>get { return CurrentGamerCount; }</c>), which is unbounded recursion: touching any
/// of them killed the process with a StackOverflowException. So the LAN session list and
/// the manual "join by address" path crashed as soon as they actually saw a server.
/// Auto-properties keep the same API without the recursion.
/// </summary>
public sealed class AvailableNetworkSession
{
	public int CurrentGamerCount { get; set; }

	public string HostGamertag { get; set; }

	public int OpenPrivateGamerSlots { get; set; }

	public int OpenPublicGamerSlots { get; set; }

	public QualityOfService QualityOfService { get; set; }

	public NetworkSessionProperties SessionProperties { get; set; }

	internal IPEndPoint EndPoint { get; set; }

	/// <summary>
	/// Relay room to join, or empty for a direct connection. When set, <see cref="EndPoint"/> is
	/// the relay rather than the host: the socket is paired with the host by the relay, and the
	/// game's handshake still runs end to end.
	/// </summary>
	internal string RelayRoom { get; set; }

	internal IPEndPoint InternalEndpont { get; set; }

	internal NetworkSessionType SessionType { get; set; }

	/// <summary>Host requires a password. Advertised by discovery and by the master server.</summary>
	internal bool Passworded { get; set; }

	/// <summary>
	/// The host runs a different mod set. The list still shows it (so the reason is
	/// visible), but the handshake will refuse the join.
	/// </summary>
	internal bool ModMismatch { get; set; }

	/// <summary>Wire protocol the host speaks; 0 when the listing did not say.</summary>
	internal int ProtocolVersion { get; set; }

	internal AvailableNetworkSession(int numGamers, string host, int privateSlots, int publicSlots, NetworkSessionProperties properties, QualityOfService qos)
	{
		CurrentGamerCount = numGamers;
		HostGamertag = host;
		OpenPrivateGamerSlots = privateSlots;
		OpenPublicGamerSlots = publicSlots;
		SessionProperties = properties;
		QualityOfService = qos;
	}

	public AvailableNetworkSession()
	{
		QualityOfService = new QualityOfService();
	}
}
