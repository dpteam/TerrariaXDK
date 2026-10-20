using System;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class ResourceCreatedEventArgs : EventArgs
{
	public object Resource { get; private set; }

	internal ResourceCreatedEventArgs(object resource)
	{
		Resource = resource;
	}
}
