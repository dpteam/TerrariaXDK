using System;
using System.Runtime.Serialization;

namespace Microsoft.Xna.Framework.GamerServices;

public class GameUpdateRequiredException : Exception
{
	public GameUpdateRequiredException()
	{
	}

	public GameUpdateRequiredException(string message)
		: base(message)
	{
	}

	public GameUpdateRequiredException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	protected GameUpdateRequiredException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
