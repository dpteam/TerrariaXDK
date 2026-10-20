using System;

namespace Microsoft.Xna.Framework.Net;

public class GamerJoinedEventArgs : EventArgs
{
	public NetworkGamer Gamer { get; private set; }

	public GamerJoinedEventArgs(NetworkGamer gamer)
	{
		Gamer = gamer;
	}
}
