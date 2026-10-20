using System;

namespace Microsoft.Xna.Framework.GamerServices;

public class SignedOutEventArgs : EventArgs
{
	public SignedInGamer Gamer { get; private set; }

	public SignedOutEventArgs(SignedInGamer gamer)
	{
		Gamer = gamer;
	}
}
