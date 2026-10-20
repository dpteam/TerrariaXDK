using System;

namespace Microsoft.Xna.Framework.Net;

public class HostChangedEventArgs : EventArgs
{
	public NetworkGamer OldHost { get; private set; }

	public NetworkGamer NewHost { get; private set; }

	public HostChangedEventArgs(NetworkGamer oldHost, NetworkGamer newHost)
	{
		OldHost = oldHost;
		NewHost = newHost;
	}
}
