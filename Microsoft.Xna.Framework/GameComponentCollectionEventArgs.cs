using System;

namespace Microsoft.Xna.Framework;

public class GameComponentCollectionEventArgs : EventArgs
{
	public IGameComponent GameComponent { get; private set; }

	public GameComponentCollectionEventArgs(IGameComponent gameComponent)
	{
		GameComponent = gameComponent;
	}
}
