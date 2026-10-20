using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Audio;

[Serializable]
public sealed class InstancePlayLimitException : ExternalException
{
	public InstancePlayLimitException()
	{
	}

	public InstancePlayLimitException(string message)
		: base(message)
	{
	}

	public InstancePlayLimitException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
