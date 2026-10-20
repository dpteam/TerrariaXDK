using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.GamerServices;

namespace Microsoft.Xna.Framework.Net;

public sealed class LocalNetworkGamer : NetworkGamer
{
	internal Queue<NetworkSession.NetworkEvent> packetQueue;

	public bool IsDataAvailable => packetQueue.Count > 0;

	public SignedInGamer SignedInGamer { get; private set; }

	internal LocalNetworkGamer(SignedInGamer gamer, NetworkSession session)
		: base(session)
	{
		SignedInGamer = gamer;
		packetQueue = new Queue<NetworkSession.NetworkEvent>();
	}

	public void EnableSendVoice(NetworkGamer remoteGamer, bool enable)
	{
	}

	public void SendPartyInvites()
	{
	}

	public int ReceiveData(byte[] data, out NetworkGamer gamer)
	{
		return ReceiveData(data, 0, out gamer);
	}

	public int ReceiveData(byte[] data, int offset, out NetworkGamer sender)
	{
		sender = null;
		if (!IsDataAvailable)
		{
			return 0;
		}
		NetworkSession.NetworkEvent networkEvent = packetQueue.Dequeue();
		int num = Math.Min(networkEvent.Packet.Length, data.Length);
		Array.Copy(networkEvent.Packet, 0, data, offset, num);
		foreach (NetworkGamer allGamer in Session.AllGamers)
		{
			if (allGamer == networkEvent.Gamer)
			{
				sender = allGamer;
				return num;
			}
		}
		return num;
	}

	public int ReceiveData(PacketReader data, out NetworkGamer sender)
	{
		sender = null;
		if (!IsDataAvailable)
		{
			return 0;
		}
		uint result = 0u;
		NetworkSession.NetworkEvent networkEvent = packetQueue.Dequeue();
		data.BaseStream.Seek(0L, SeekOrigin.Begin);
		data.BaseStream.Write(networkEvent.Packet, 0, networkEvent.Packet.Length);
		data.BaseStream.Seek(0L, SeekOrigin.Begin);
		foreach (NetworkGamer allGamer in Session.AllGamers)
		{
			if (allGamer == networkEvent.Gamer)
			{
				sender = allGamer;
				return (int)result;
			}
		}
		return (int)result;
	}

	public void SendData(byte[] data, SendDataOptions options)
	{
		SendData(data, 0, data.Length, options);
	}

	public void SendData(byte[] data, int offset, int count, SendDataOptions options)
	{
		byte[] array = new byte[count];
		Array.Copy(data, offset, array, 0, array.Length);
		foreach (NetworkGamer allGamer in Session.AllGamers)
		{
			NetworkSession.NetworkEvent evt = new NetworkSession.NetworkEvent
			{
				Type = NetworkSession.NetworkEventType.PacketSend,
				Gamer = allGamer,
				Packet = array,
				Reliable = options
			};
			Session.SendNetworkEvent(evt);
		}
	}

	public void SendData(byte[] data, SendDataOptions options, NetworkGamer recipient)
	{
		SendData(data, 0, data.Length, options, recipient);
	}

	public void SendData(byte[] data, int offset, int count, SendDataOptions options, NetworkGamer recipient)
	{
		byte[] array = new byte[count];
		Array.Copy(data, offset, array, 0, array.Length);
		NetworkSession.NetworkEvent evt = new NetworkSession.NetworkEvent
		{
			Type = NetworkSession.NetworkEventType.PacketSend,
			Gamer = recipient,
			Packet = array,
			Reliable = options
		};
		Session.SendNetworkEvent(evt);
	}

	public void SendData(PacketWriter data, SendDataOptions options)
	{
		byte[] packet = (data.BaseStream as MemoryStream).ToArray();
		data.BaseStream.Seek(0L, SeekOrigin.Begin);
		foreach (NetworkGamer allGamer in Session.AllGamers)
		{
			NetworkSession.NetworkEvent evt = new NetworkSession.NetworkEvent
			{
				Type = NetworkSession.NetworkEventType.PacketSend,
				Gamer = allGamer,
				Packet = packet,
				Reliable = options
			};
			Session.SendNetworkEvent(evt);
		}
	}

	public void SendData(PacketWriter data, SendDataOptions options, NetworkGamer recipient)
	{
		byte[] packet = (data.BaseStream as MemoryStream).ToArray();
		data.BaseStream.Seek(0L, SeekOrigin.Begin);
		NetworkSession.NetworkEvent evt = new NetworkSession.NetworkEvent
		{
			Type = NetworkSession.NetworkEventType.PacketSend,
			Gamer = recipient,
			Packet = packet,
			Reliable = options
		};
		Session.SendNetworkEvent(evt);
	}
}
