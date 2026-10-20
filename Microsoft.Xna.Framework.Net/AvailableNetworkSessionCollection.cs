using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Net;

public sealed class AvailableNetworkSessionCollection : ReadOnlyCollection<AvailableNetworkSession>, IDisposable
{
	private List<AvailableNetworkSession> collection;

	public bool IsDisposed { get; private set; }

	internal AvailableNetworkSessionCollection(List<AvailableNetworkSession> collection)
		: base((IList<AvailableNetworkSession>)collection)
	{
		this.collection = collection;
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
