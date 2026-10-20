using System;
using Microsoft.Xna.Framework.GamerServices;

namespace Microsoft.Xna.Framework.Net;

public class NetworkGamer : Gamer
{
	private long remoteUniqueIdentifier = -1L;

	public bool HasLeftSession { get; private set; }

	public bool HasVoice { get; private set; }

	public byte Id => 0;

	public bool IsGuest { get; private set; }

	public bool IsHost => true;

	public bool IsLocal => this is LocalNetworkGamer;

	public bool IsMutedByLocalUser { get; private set; }

	public bool IsPrivateSlot { get; private set; }

	public bool IsReady { get; set; }

	public bool IsTalking { get; private set; }

	internal int State
	{
		get
		{
			int num = 0;
			if (IsLocal)
			{
				num++;
			}
			if (IsHost)
			{
				num += 16;
			}
			if (HasVoice)
			{
				num += 256;
			}
			if (IsGuest)
			{
				num += 4096;
			}
			if (IsMutedByLocalUser)
			{
				num += 65536;
			}
			if (IsPrivateSlot)
			{
				num += 1048576;
			}
			if (IsReady)
			{
				num += 16777216;
			}
			return num;
		}
		set
		{
			int num = value;
			if (num >= 16777216)
			{
				IsReady = true;
				num -= 16777216;
			}
			else
			{
				IsReady = false;
			}
			if (num >= 1048576)
			{
				IsPrivateSlot = true;
				num -= 1048576;
			}
			else
			{
				IsPrivateSlot = false;
			}
			if (num >= 65536)
			{
				IsMutedByLocalUser = true;
				num -= 65536;
			}
			else
			{
				IsMutedByLocalUser = false;
			}
			if (num >= 4096)
			{
				IsGuest = true;
				num -= 4096;
			}
			else
			{
				IsGuest = false;
			}
			if (num >= 256)
			{
				HasVoice = true;
				num -= 256;
			}
			else
			{
				HasVoice = false;
			}
		}
	}

	public NetworkMachine Machine { get; set; }

	public TimeSpan RoundtripTime { get; private set; }

	public NetworkSession Session { get; private set; }

	internal long RemoteUniqueIdentifier
	{
		get
		{
			return remoteUniqueIdentifier;
		}
		set
		{
			remoteUniqueIdentifier = value;
		}
	}

	internal NetworkGamer(NetworkSession session)
		: base("Stub Gamer", "Stub Gamer")
	{
		Session = session;
		HasLeftSession = false;
		HasVoice = false;
		IsGuest = false;
		IsMutedByLocalUser = false;
		IsPrivateSlot = false;
		IsReady = false;
		IsTalking = false;
		Machine = new NetworkMachine();
		RoundtripTime = TimeSpan.Zero;
	}
}
