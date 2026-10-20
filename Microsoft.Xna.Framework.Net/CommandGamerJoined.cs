namespace Microsoft.Xna.Framework.Net;

internal class CommandGamerJoined : ICommand
{
	private int gamerInternalIndex = -1;

	internal long remoteUniqueIdentifier = -1L;

	private GamerStates states;

	private string gamerTag = string.Empty;

	private string displayName = string.Empty;

	public string DisplayName
	{
		get
		{
			return displayName;
		}
		set
		{
			displayName = value;
		}
	}

	public string GamerTag
	{
		get
		{
			return gamerTag;
		}
		set
		{
			gamerTag = value;
		}
	}

	public GamerStates State
	{
		get
		{
			return states;
		}
		set
		{
			states = value;
		}
	}

	public int InternalIndex => gamerInternalIndex;

	public CommandEventType Command => CommandEventType.GamerJoined;

	public CommandGamerJoined(int internalIndex, bool isHost, bool isLocal)
	{
		gamerInternalIndex = internalIndex;
		if (isHost)
		{
			states |= GamerStates.Host;
		}
		if (isLocal)
		{
			states |= GamerStates.Local;
		}
	}

	public CommandGamerJoined(long uniqueIndentifier)
	{
		remoteUniqueIdentifier = uniqueIndentifier;
	}
}
