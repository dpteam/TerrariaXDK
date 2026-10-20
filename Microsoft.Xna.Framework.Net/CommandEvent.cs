namespace Microsoft.Xna.Framework.Net;

internal class CommandEvent
{
	private CommandEventType command;

	private object commandObject;

	public CommandEventType Command => command;

	public object CommandObject => commandObject;

	public CommandEvent(CommandEventType command, object commandObject)
	{
		this.command = command;
		this.commandObject = commandObject;
	}

	public CommandEvent(ICommand command)
	{
		this.command = command.Command;
		commandObject = command;
	}
}
