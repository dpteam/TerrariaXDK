using System;
using System.Runtime.Serialization;

namespace Microsoft.Xna.Framework.GamerServices;

public class NetworkNotAvailableException : NetworkException
{
	public NetworkNotAvailableException()
	{
	}

	public NetworkNotAvailableException(string message)
		: base(message)
	{
	}

	public NetworkNotAvailableException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	protected NetworkNotAvailableException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
