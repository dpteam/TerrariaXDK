using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.GamerServices;

public sealed class FriendCollection : GamerCollection<FriendGamer>, IDisposable
{
	public bool IsDisposed { get; private set; }

	internal FriendCollection(List<FriendGamer> friends)
		: base(friends)
	{
		IsDisposed = false;
	}

	public void Dispose()
	{
		if (!IsDisposed)
		{
			collection.Clear();
			IsDisposed = true;
		}
	}
}
