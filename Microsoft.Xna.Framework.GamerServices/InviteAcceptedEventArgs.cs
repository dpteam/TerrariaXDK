using System;

namespace Microsoft.Xna.Framework.GamerServices;

public class InviteAcceptedEventArgs : EventArgs
{
	public SignedInGamer Gamer { get; private set; }

	public bool IsCurrentSession { get; private set; }

	public InviteAcceptedEventArgs(SignedInGamer gamer, bool isCurrentSession)
	{
		Gamer = gamer;
		IsCurrentSession = isCurrentSession;
	}
}
