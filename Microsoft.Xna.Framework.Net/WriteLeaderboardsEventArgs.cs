using System;

namespace Microsoft.Xna.Framework.Net;

public sealed class WriteLeaderboardsEventArgs : EventArgs
{
	public NetworkGamer Gamer { get; private set; }

	public bool IsLeaving { get; private set; }

	internal WriteLeaderboardsEventArgs(NetworkGamer gamer, bool isLeaving)
	{
		Gamer = gamer;
		IsLeaving = isLeaving;
	}
}
