using System;

namespace Microsoft.Xna.Framework.Audio;

[Serializable]
public sealed class NoMicrophoneConnectedException : Exception
{
	public NoMicrophoneConnectedException()
	{
	}

	public NoMicrophoneConnectedException(string message)
		: base(message)
	{
	}

	public NoMicrophoneConnectedException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
