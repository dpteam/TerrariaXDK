using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.GamerServices;

namespace Microsoft.Xna.Framework.Net;

public sealed class NetworkMachine
{
	public GamerCollection<NetworkGamer> Gamers { get; private set; }

	internal NetworkMachine()
	{
		Gamers = new GamerCollection<NetworkGamer>(new List<NetworkGamer>());
	}

	public void RemoveFromSession()
	{
		throw new NotImplementedException();
	}
}
