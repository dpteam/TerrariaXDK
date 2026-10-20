namespace Microsoft.Xna.Framework.Net;

internal class CommandGamerLeft : ICommand
{
	private int gamerInternalIndex = -1;

	internal long remoteUniqueIdentifier = -1L;

	public int InternalIndex => gamerInternalIndex;

	public CommandEventType Command => CommandEventType.GamerLeft;

	public CommandGamerLeft(int internalIndex)
	{
		gamerInternalIndex = internalIndex;
	}

	public CommandGamerLeft(long uniqueIndentifier)
	{
		remoteUniqueIdentifier = uniqueIndentifier;
	}
}
