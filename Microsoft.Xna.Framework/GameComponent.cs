using System;

namespace Microsoft.Xna.Framework;

public class GameComponent : IGameComponent, IUpdateable, IComparable<GameComponent>, IDisposable
{
	private bool _enabled = true;

	private int _updateOrder;

	public Game Game { get; private set; }

	public bool Enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			if (_enabled != value)
			{
				_enabled = value;
				if (EnabledChanged != null)
				{
					EnabledChanged(this, EventArgs.Empty);
				}
				OnEnabledChanged(this, null);
			}
		}
	}

	public int UpdateOrder
	{
		get
		{
			return _updateOrder;
		}
		set
		{
			if (_updateOrder != value)
			{
				_updateOrder = value;
				if (UpdateOrderChanged != null)
				{
					UpdateOrderChanged(this, EventArgs.Empty);
				}
				OnUpdateOrderChanged(this, null);
			}
		}
	}

	public event EventHandler<EventArgs> Disposed;

	public event EventHandler<EventArgs> EnabledChanged;

	public event EventHandler<EventArgs> UpdateOrderChanged;

	public GameComponent(Game game)
	{
		Game = game;
	}

	~GameComponent()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public virtual void Initialize()
	{
	}

	public virtual void Update(GameTime gameTime)
	{
	}

	protected virtual void OnUpdateOrderChanged(object sender, EventArgs args)
	{
	}

	protected virtual void OnEnabledChanged(object sender, EventArgs args)
	{
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing && Disposed != null)
		{
			Disposed(this, EventArgs.Empty);
		}
	}

	int IComparable<GameComponent>.CompareTo(GameComponent other)
	{
		return other.UpdateOrder - UpdateOrder;
	}
}
