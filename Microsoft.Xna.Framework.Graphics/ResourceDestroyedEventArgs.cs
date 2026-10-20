using System;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class ResourceDestroyedEventArgs : EventArgs
{
	public string Name { get; private set; }

	public object Tag { get; private set; }

	internal ResourceDestroyedEventArgs(string name, object tag)
	{
		Name = name;
		Tag = tag;
	}
}
