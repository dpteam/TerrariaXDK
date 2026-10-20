using Microsoft.Xna.Framework.Net;

namespace Terraria.Net;

/// <summary>
/// Transport contract between <see cref="NetworkSession"/> and the wire. Two implementations
/// exist: the original MonoGame/Lidgren peer and <see cref="TcpPeer"/>, which runs the host
/// inside the game process (no separate server executable).
/// </summary>
internal interface IPeerTransport
{
	/// <summary>Sends to every remote gamer.</summary>
	void SendData(byte[] data, SendDataOptions options);

	/// <summary>Sends to a single gamer; local gamers are looped back by the session.</summary>
	void SendData(byte[] data, SendDataOptions options, NetworkGamer gamer);

	/// <summary>Applies inbound traffic on the game thread (called from NetworkSession.Update).</summary>
	void Drain();

	/// <summary>
	/// Sends an already-framed transport message (body without the length prefix), used for
	/// opcodes the game session does not know about, such as mod traffic.
	/// </summary>
	void SendFrame(byte[] frame);

	/// <summary>Propagates a host-driven session state change to remote peers.</summary>
	void BroadcastState(NetworkSessionState state);

	void Shutdown();
}
