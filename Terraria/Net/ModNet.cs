using System;
using System.Text;

namespace Terraria.Net;

/// <summary>
/// Mod-to-mod traffic over the built-in multiplayer stack.
///
/// Mods cannot use <see cref="Terraria.NetMessage"/>: its message ids are fixed by the
/// console protocol and adding one would break compatibility with anything that is not
/// this build. Instead a dedicated transport frame (<c>OpMod</c>) carries
/// <c>[modId][payload]</c>, the host relays it like a game packet, and each side raises
/// <see cref="Received"/> with the mod id attached. No id registration is needed at all,
/// so two mods can never collide on a number.
///
/// This is deliberately transport-level: nothing here touches world state, so it is safe
/// to receive on the transport thread - the callback is invoked there and must not touch
/// <c>Main.*</c>. Queue your work and apply it from <c>OnGameUpdate</c>.
/// </summary>
public static class ModNet
{
	/// <summary>Upper bound for one mod payload. Larger writes are rejected, not chunked.</summary>
	public const int MaxPayload = 64 * 1024;

	/// <summary>Set while a connection is live; null in singleplayer and in menus.</summary>
	private static IPeerTransport active;

	private static int dropped;

	/// <summary>Fired for every inbound mod packet. Runs on the network thread.</summary>
	public static event ModPacketHandler Received;

	public delegate void ModPacketHandler(string modId, byte[] payload);

	/// <summary>True when there is a link to send on (host or joined client).</summary>
	public static bool IsConnected => active != null;

	/// <summary>Packets discarded because no link was up. Used by diagnostics and tests.</summary>
	public static int DroppedCount => dropped;

	internal static void Attach(IPeerTransport transport)
	{
		active = transport;
	}

	internal static void Detach()
	{
		active = null;
	}

	/// <summary>
	/// Sends a payload to every other player (client to host, host to everyone).
	/// Returns false when there is no connection or the payload is too large.
	/// </summary>
	public static bool Send(string modId, byte[] payload)
	{
		IPeerTransport peerTransport = active;
		if (peerTransport == null)
		{
			dropped++;
			return false;
		}
		byte[] array = Encode(modId, payload);
		if (array == null)
		{
			dropped++;
			return false;
		}
		try
		{
			peerTransport.SendFrame(array);
			return true;
		}
		catch (Exception ex)
		{
			ModLoaderLog("send failed: " + ex.Message);
			return false;
		}
	}

	/// <summary>Encodes <c>[opcode][modId][payload]</c> into a transport frame.</summary>
	internal static byte[] Encode(string modId, byte[] payload)
	{
		if (string.IsNullOrEmpty(modId))
		{
			return null;
		}
		byte[] bytes = Encoding.UTF8.GetBytes(modId);
		if (bytes.Length == 0 || bytes.Length > 255)
		{
			return null;
		}
		int num = (payload == null) ? 0 : payload.Length;
		if (num > MaxPayload)
		{
			ModLoaderLog("payload of " + num + " bytes rejected (limit " + MaxPayload + ")");
			return null;
		}
		byte[] array = new byte[2 + bytes.Length + num];
		array[0] = TcpPeer.OpMod;
		array[1] = (byte)bytes.Length;
		Buffer.BlockCopy(bytes, 0, array, 2, bytes.Length);
		if (num > 0)
		{
			Buffer.BlockCopy(payload, 0, array, 2 + bytes.Length, num);
		}
		return array;
	}

	/// <summary>
	/// Decodes a transport frame back into a mod id and payload. Returns false for a frame
	/// that is not a mod packet or is truncated - a hostile client must not be able to make
	/// this throw.
	/// </summary>
	internal static bool TryDecode(byte[] frame, out string modId, out byte[] payload)
	{
		modId = null;
		payload = null;
		if (frame == null || frame.Length < 3 || frame[0] != TcpPeer.OpMod)
		{
			return false;
		}
		int num = frame[1];
		if (num == 0 || 2 + num > frame.Length)
		{
			return false;
		}
		int num2 = frame.Length - 2 - num;
		if (num2 > MaxPayload)
		{
			return false;
		}
		modId = Encoding.UTF8.GetString(frame, 2, num);
		payload = new byte[num2];
		if (num2 > 0)
		{
			Buffer.BlockCopy(frame, 2 + num, payload, 0, num2);
		}
		return true;
	}

	/// <summary>
	/// Hands an inbound packet to the subscribers. Called from the transport thread once the
	/// frame has been relayed, so host and clients see the same stream.
	/// </summary>
	internal static void Dispatch(string modId, byte[] payload)
	{
		ModPacketHandler modPacketHandler = Received;
		if (modPacketHandler == null)
		{
			return;
		}
		try
		{
			modPacketHandler(modId, payload);
		}
		catch (Exception ex)
		{
			// A broken handler must not take the network thread with it.
			ModLoaderLog("handler threw: " + ex);
		}
	}

	/// <summary>Reads a little-endian int from a payload, for mods that need framing.</summary>
	public static int ReadInt(byte[] payload, int offset, int fallback = 0)
	{
		if (payload == null || offset < 0 || offset + 4 > payload.Length)
		{
			return fallback;
		}
		return BitConverter.ToInt32(payload, offset);
	}

	/// <summary>Writes a little-endian int into a fresh payload of the given size.</summary>
	public static byte[] Create(int size)
	{
		return new byte[Math.Max(0, size)];
	}

	private static void ModLoaderLog(string message)
	{
		try
		{
			Terraria.Mods.ModLoader.Log("[ModNet] " + message);
		}
		catch
		{
		}
	}
}
