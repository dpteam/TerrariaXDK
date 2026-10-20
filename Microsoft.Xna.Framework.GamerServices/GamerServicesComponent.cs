namespace Microsoft.Xna.Framework.GamerServices;

public class GamerServicesComponent : GameComponent
{
	public GamerServicesComponent(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		GamerServicesDispatcher.WindowHandle = Game.Window.Handle;
		GamerServicesDispatcher.Initialize(Game.Services);
	}

	public override void Update(GameTime gameTime)
	{
		GamerServicesDispatcher.Update();
	}
}
