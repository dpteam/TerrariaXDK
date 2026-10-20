namespace Microsoft.Xna.Framework.Net;

internal class CommandReceiveData : ICommand
{
	internal byte[] data;

	internal long remoteUniqueIdentifier = -1L;

	public CommandEventType Command => CommandEventType.ReceiveData;

	public CommandReceiveData(long remoteUniqueIdentifier, byte[] data)
	{
		this.remoteUniqueIdentifier = remoteUniqueIdentifier;
		this.data = data;
	}
}
