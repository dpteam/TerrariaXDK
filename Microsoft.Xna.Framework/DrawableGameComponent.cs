using System;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework;

public class DrawableGameComponent : GameComponent, IDrawable
{
	private bool _initialized;

	private int _drawOrder;

	private bool _visible = true;

	public GraphicsDevice GraphicsDevice => Game.GraphicsDevice;

	public int DrawOrder
	{
		get
		{
			return _drawOrder;
		}
		set
		{
			if (_drawOrder != value)
			{
				_drawOrder = value;
				if (DrawOrderChanged != null)
				{
					DrawOrderChanged(this, null);
				}
				OnDrawOrderChanged(this, null);
			}
		}
	}

	public bool Visible
	{
		get
		{
			return _visible;
		}
		set
		{
			if (_visible != value)
			{
				_visible = value;
				if (VisibleChanged != null)
				{
					VisibleChanged(this, EventArgs.Empty);
				}
				OnVisibleChanged(this, EventArgs.Empty);
			}
		}
	}

	public event EventHandler<EventArgs> DrawOrderChanged;

	public event EventHandler<EventArgs> VisibleChanged;

	public DrawableGameComponent(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		if (_initialized)
		{
			return;
		}
		_initialized = true;
		IGraphicsDeviceService graphicsDeviceService = (IGraphicsDeviceService)Game.Services.GetService(typeof(IGraphicsDeviceService));
		if (graphicsDeviceService != null)
		{
			if (graphicsDeviceService.GraphicsDevice != null)
			{
				LoadContent();
			}
			else
			{
				graphicsDeviceService.DeviceCreated += OnDeviceCreated;
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (_initialized)
		{
			UnloadContent();
		}
		base.Dispose(disposing);
	}

	private void OnDeviceCreated(object sender, EventArgs e)
	{
		LoadContent();
	}

	public virtual void Draw(GameTime gameTime)
	{
	}

	protected virtual void LoadContent()
	{
	}

	protected virtual void UnloadContent()
	{
	}

	protected virtual void OnVisibleChanged(object sender, EventArgs args)
	{
	}

	protected virtual void OnDrawOrderChanged(object sender, EventArgs args)
	{
	}
}
