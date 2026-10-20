using System;

namespace Microsoft.Xna.Framework.Graphics;

[Serializable]
public sealed class DeviceNotResetException : Exception
{
	public DeviceNotResetException()
	{
	}

	public DeviceNotResetException(string message)
		: base(message)
	{
	}

	public DeviceNotResetException(string message, Exception inner)
		: base(message, inner)
	{
	}
}
