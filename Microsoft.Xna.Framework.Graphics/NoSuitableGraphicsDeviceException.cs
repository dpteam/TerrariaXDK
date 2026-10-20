using System;

namespace Microsoft.Xna.Framework.Graphics;

[Serializable]
public sealed class NoSuitableGraphicsDeviceException : Exception
{
	public NoSuitableGraphicsDeviceException()
	{
	}

	public NoSuitableGraphicsDeviceException(string message)
		: base(message)
	{
	}

	public NoSuitableGraphicsDeviceException(string message, Exception inner)
		: base(message, inner)
	{
	}
}
