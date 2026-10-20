using System;

namespace Microsoft.Xna.Framework.Graphics;

public abstract class GraphicsResource : IDisposable
{
	private WeakReference selfReference;

	private GraphicsDevice graphicsDevice;

	public GraphicsDevice GraphicsDevice
	{
		get
		{
			return graphicsDevice;
		}
		internal set
		{
			if (graphicsDevice != value)
			{
				if (graphicsDevice != null && selfReference != null)
				{
					graphicsDevice.RemoveResourceReference(selfReference);
					selfReference = null;
				}
				graphicsDevice = value;
				selfReference = new WeakReference(this);
				graphicsDevice.AddResourceReference(selfReference);
			}
		}
	}

	public bool IsDisposed { get; private set; }

	public string Name { get; set; }

	public object Tag { get; set; }

	public event EventHandler<EventArgs> Disposing;

	internal GraphicsResource()
	{
	}

	~GraphicsResource()
	{
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public override string ToString()
	{
		return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
	}

	protected internal virtual void GraphicsDeviceResetting()
	{
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing && Disposing != null)
			{
				Disposing(this, EventArgs.Empty);
			}
			if (graphicsDevice != null && selfReference != null)
			{
				graphicsDevice.RemoveResourceReference(selfReference);
				selfReference = null;
			}
			IsDisposed = true;
		}
	}
}
