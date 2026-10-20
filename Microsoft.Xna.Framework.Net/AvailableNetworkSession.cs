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

	internal IPEndPoint InternalEndpont { get; set; }

	internal NetworkSessionType SessionType { get; set; }

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
