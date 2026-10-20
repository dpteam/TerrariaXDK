using System;

namespace Microsoft.Xna.Framework.Graphics;

[Serializable]
public sealed class DeviceLostException : Exception
{
	public DeviceLostException()
	{
	}

	public DeviceLostException(string message)
		: base(message)
	{
	}

	public DeviceLostException(string message, Exception inner)
		: base(message, inner)
	{
	}
}
