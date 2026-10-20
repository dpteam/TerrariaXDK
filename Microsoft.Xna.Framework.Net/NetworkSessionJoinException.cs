using System;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework.GamerServices;

namespace Microsoft.Xna.Framework.Net;

public class NetworkSessionJoinException : NetworkException
{
	public NetworkSessionJoinError JoinError { get; set; }

	public NetworkSessionJoinException()
	{
	}

	public NetworkSessionJoinException(string message)
		: base(message)
	{
	}

	public NetworkSessionJoinException(string message, NetworkSessionJoinError joinError)
		: base(message)
	{
		JoinError = joinError;
	}

	public NetworkSessionJoinException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	protected NetworkSessionJoinException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
