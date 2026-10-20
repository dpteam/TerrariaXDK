using System;

namespace Microsoft.Xna.Framework.GamerServices;

public class SignedInEventArgs : EventArgs
{
	public SignedInGamer Gamer { get; private set; }

	public SignedInEventArgs(SignedInGamer gamer)
	{
		Gamer = gamer;
	}
}
