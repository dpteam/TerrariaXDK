using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework;

public class GraphicsDeviceManager : IGraphicsDeviceService, IDisposable, IGraphicsDeviceManager
{
	private bool INTERNAL_isFullScreen;

	private bool INTERNAL_preferMultiSampling;

	private SurfaceFormat INTERNAL_preferredBackBufferFormat;

	private int INTERNAL_preferredBackBufferHeight;

	private int INTERNAL_preferredBackBufferWidth;

	private DepthFormat INTERNAL_preferredDepthStencilFormat;

	private bool INTERNAL_synchronizeWithVerticalRetrace;

	private DisplayOrientation INTERNAL_supportedOrientations;

	private Game game;

	private GraphicsDevice graphicsDevice;

	private bool drawBegun;

	private bool disposed;

	private bool prefsChanged;

	private bool supportsOrientations;

	private bool useResizedBackBuffer;

	private int resizedBackBufferWidth;

	private int resizedBackBufferHeight;

	public static readonly int DefaultBackBufferWidth = 800;

	public static readonly int DefaultBackBufferHeight = 480;

	public GraphicsProfile GraphicsProfile { get; set; }

	public GraphicsDevice GraphicsDevice => graphicsDevice;

	public bool IsFullScreen
	{
		get
		{
			return INTERNAL_isFullScreen;
		}
		set
		{
			INTERNAL_isFullScreen = value;
			prefsChanged = true;
		}
	}

	public bool PreferMultiSampling
	{
		get
		{
			return INTERNAL_preferMultiSampling;
		}
		set
		{
			INTERNAL_preferMultiSampling = value;
			prefsChanged = true;
		}
	}

	public SurfaceFormat PreferredBackBufferFormat
	{
		get
		{
			return INTERNAL_preferredBackBufferFormat;
		}
		set
		{
			INTERNAL_preferredBackBufferFormat = value;
			prefsChanged = true;
		}
	}

	public int PreferredBackBufferHeight
	{
		get
		{
			return INTERNAL_preferredBackBufferHeight;
		}
		set
		{
			INTERNAL_preferredBackBufferHeight = value;
			prefsChanged = true;
		}
	}

	public int PreferredBackBufferWidth
	{
		get
		{
			return INTERNAL_preferredBackBufferWidth;
		}
		set
		{
			INTERNAL_preferredBackBufferWidth = value;
			prefsChanged = true;
		}
	}

	public DepthFormat PreferredDepthStencilFormat
	{
		get
		{
			return INTERNAL_preferredDepthStencilFormat;
		}
		set
		{
			INTERNAL_preferredDepthStencilFormat = value;
			prefsChanged = true;
		}
	}

	public bool SynchronizeWithVerticalRetrace
	{
		get
		{
			return INTERNAL_synchronizeWithVerticalRetrace;
		}
		set
		{
			INTERNAL_synchronizeWithVerticalRetrace = value;
			prefsChanged = true;
		}
	}

	public DisplayOrientation SupportedOrientations
	{
		get
		{
			return INTERNAL_supportedOrientations;
		}
		set
		{
			INTERNAL_supportedOrientations = value;
			prefsChanged = true;
		}
	}

	public event EventHandler<EventArgs> Disposed;

	public event EventHandler<EventArgs> DeviceCreated;

	public event EventHandler<EventArgs> DeviceDisposing;

	public event EventHandler<EventArgs> DeviceReset;

	public event EventHandler<EventArgs> DeviceResetting;

	public event EventHandler<PreparingDeviceSettingsEventArgs> PreparingDeviceSettings;

	public GraphicsDeviceManager(Game game)
	{
		if (game == null)
		{
			throw new ArgumentNullException("The game cannot be null!");
		}
		this.game = game;
		INTERNAL_supportedOrientations = DisplayOrientation.Default;
		INTERNAL_preferredBackBufferHeight = DefaultBackBufferHeight;
		INTERNAL_preferredBackBufferWidth = DefaultBackBufferWidth;
		INTERNAL_preferredBackBufferFormat = SurfaceFormat.Color;
		INTERNAL_preferredDepthStencilFormat = DepthFormat.Depth24;
		INTERNAL_synchronizeWithVerticalRetrace = true;
		INTERNAL_preferMultiSampling = false;
		if (game.Services.GetService(typeof(IGraphicsDeviceManager)) != null)
		{
			throw new ArgumentException("Graphics Device Manager Already Present");
		}
		game.Services.AddService(typeof(IGraphicsDeviceManager), this);
		game.Services.AddService(typeof(IGraphicsDeviceService), this);
		prefsChanged = true;
		useResizedBackBuffer = false;
		supportsOrientations = FNAPlatform.SupportsOrientationChanges();
		game.Window.ClientSizeChanged += INTERNAL_OnClientSizeChanged;
	}

	~GraphicsDeviceManager()
	{
		Dispose(disposing: false);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			game.Services.RemoveService(typeof(IGraphicsDeviceManager));
			game.Services.RemoveService(typeof(IGraphicsDeviceService));
			if (disposing && graphicsDevice != null)
			{
				graphicsDevice.Dispose();
				graphicsDevice = null;
			}
			if (Disposed != null)
			{
				Disposed(this, EventArgs.Empty);
			}
			disposed = true;
		}
	}

	void IDisposable.Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public void ApplyChanges()
	{
		if (graphicsDevice == null)
		{
			((IGraphicsDeviceManager)this).CreateDevice();
		}
		else if (prefsChanged || useResizedBackBuffer)
		{
			GraphicsDeviceInformation graphicsDeviceInformation = new GraphicsDeviceInformation();
			graphicsDeviceInformation.Adapter = graphicsDevice.Adapter;
			graphicsDeviceInformation.PresentationParameters = graphicsDevice.PresentationParameters.Clone();
			INTERNAL_CreateGraphicsDeviceInformation(graphicsDeviceInformation);
			if (supportsOrientations)
			{
				game.Window.SetSupportedOrientations(INTERNAL_supportedOrientations);
			}
			game.Window.BeginScreenDeviceChange(graphicsDeviceInformation.PresentationParameters.IsFullScreen);
			game.Window.EndScreenDeviceChange(graphicsDeviceInformation.Adapter.DeviceName, graphicsDeviceInformation.PresentationParameters.BackBufferWidth, graphicsDeviceInformation.PresentationParameters.BackBufferHeight);
			graphicsDevice.Reset(graphicsDeviceInformation.PresentationParameters, graphicsDeviceInformation.Adapter);
			prefsChanged = false;
		}
	}

	public void ToggleFullScreen()
	{
		IsFullScreen = !IsFullScreen;
		ApplyChanges();
	}

	protected virtual void OnDeviceCreated(object sender, EventArgs args)
	{
		if (DeviceCreated != null)
		{
			DeviceCreated(sender, args);
		}
	}

	protected virtual void OnDeviceDisposing(object sender, EventArgs args)
	{
		if (DeviceDisposing != null)
		{
			DeviceDisposing(this, args);
		}
	}

	protected virtual void OnDeviceReset(object sender, EventArgs args)
	{
		if (DeviceReset != null)
		{
			DeviceReset(this, args);
		}
	}

	protected virtual void OnDeviceResetting(object sender, EventArgs args)
	{
		if (DeviceResetting != null)
		{
			DeviceResetting(this, args);
		}
	}

	protected virtual void OnPreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs args)
	{
		if (PreparingDeviceSettings != null)
		{
			PreparingDeviceSettings(sender, args);
		}
	}

	protected virtual bool CanResetDevice(GraphicsDeviceInformation newDeviceInfo)
	{
		throw new NotImplementedException();
	}

	protected virtual GraphicsDeviceInformation FindBestDevice(bool anySuitableDevice)
	{
		throw new NotImplementedException();
	}

	protected virtual void RankDevices(List<GraphicsDeviceInformation> foundDevices)
	{
		throw new NotImplementedException();
	}

	private void INTERNAL_OnClientSizeChanged(object sender, EventArgs e)
	{
		GameWindow gameWindow = sender as GameWindow;
		Rectangle clientBounds = gameWindow.ClientBounds;
		resizedBackBufferWidth = clientBounds.Width;
		resizedBackBufferHeight = clientBounds.Height;
		FNAPlatform.ScaleForWindow(gameWindow.Handle, invert: true, ref resizedBackBufferWidth, ref resizedBackBufferHeight);
		useResizedBackBuffer = true;
		ApplyChanges();
	}

	private void INTERNAL_CreateGraphicsDeviceInformation(GraphicsDeviceInformation gdi)
	{
		if (useResizedBackBuffer)
		{
			gdi.PresentationParameters.BackBufferWidth = resizedBackBufferWidth;
			gdi.PresentationParameters.BackBufferHeight = resizedBackBufferHeight;
			useResizedBackBuffer = false;
		}
		else if (!supportsOrientations)
		{
			gdi.PresentationParameters.BackBufferWidth = PreferredBackBufferWidth;
			gdi.PresentationParameters.BackBufferHeight = PreferredBackBufferHeight;
		}
		else
		{
			int num = Math.Min(PreferredBackBufferWidth, PreferredBackBufferHeight);
			int num2 = Math.Max(PreferredBackBufferWidth, PreferredBackBufferHeight);
			if (gdi.PresentationParameters.DisplayOrientation == DisplayOrientation.Portrait)
			{
				gdi.PresentationParameters.BackBufferWidth = num;
				gdi.PresentationParameters.BackBufferHeight = num2;
			}
			else
			{
				gdi.PresentationParameters.BackBufferWidth = num2;
				gdi.PresentationParameters.BackBufferHeight = num;
			}
		}
		gdi.PresentationParameters.BackBufferFormat = PreferredBackBufferFormat;
		gdi.PresentationParameters.DepthStencilFormat = PreferredDepthStencilFormat;
		gdi.PresentationParameters.IsFullScreen = IsFullScreen;
		gdi.PresentationParameters.PresentationInterval = (SynchronizeWithVerticalRetrace ? PresentInterval.One : PresentInterval.Immediate);
		if (!PreferMultiSampling)
		{
			gdi.PresentationParameters.MultiSampleCount = 0;
		}
		else if (gdi.PresentationParameters.MultiSampleCount == 0)
		{
			int val = 0;
			if (graphicsDevice != null)
			{
				val = FNA3D.FNA3D_GetMaxMultiSampleCount(graphicsDevice.GLDevice, gdi.PresentationParameters.BackBufferFormat, 8);
			}
			gdi.PresentationParameters.MultiSampleCount = Math.Min(val, 8);
		}
		gdi.GraphicsProfile = GraphicsProfile;
		OnPreparingDeviceSettings(this, new PreparingDeviceSettingsEventArgs(gdi));
	}

	void IGraphicsDeviceManager.CreateDevice()
	{
		if (graphicsDevice != null)
		{
			graphicsDevice.Dispose();
			graphicsDevice = null;
		}
		GraphicsDeviceInformation graphicsDeviceInformation = new GraphicsDeviceInformation();
		graphicsDeviceInformation.Adapter = GraphicsAdapter.DefaultAdapter;
		graphicsDeviceInformation.PresentationParameters = new PresentationParameters();
		graphicsDeviceInformation.PresentationParameters.DeviceWindowHandle = game.Window.Handle;
		INTERNAL_CreateGraphicsDeviceInformation(graphicsDeviceInformation);
		if (supportsOrientations)
		{
			game.Window.SetSupportedOrientations(INTERNAL_supportedOrientations);
		}
		game.Window.BeginScreenDeviceChange(graphicsDeviceInformation.PresentationParameters.IsFullScreen);
		game.Window.EndScreenDeviceChange(graphicsDeviceInformation.Adapter.DeviceName, graphicsDeviceInformation.PresentationParameters.BackBufferWidth, graphicsDeviceInformation.PresentationParameters.BackBufferHeight);
		graphicsDevice = new GraphicsDevice(graphicsDeviceInformation.Adapter, graphicsDeviceInformation.GraphicsProfile, graphicsDeviceInformation.PresentationParameters);
		graphicsDevice.Disposing += OnDeviceDisposing;
		graphicsDevice.DeviceResetting += OnDeviceResetting;
		graphicsDevice.DeviceReset += OnDeviceReset;
		OnDeviceCreated(this, EventArgs.Empty);
	}

	bool IGraphicsDeviceManager.BeginDraw()
	{
		if (graphicsDevice == null)
		{
			return false;
		}
		drawBegun = true;
		return true;
	}

	void IGraphicsDeviceManager.EndDraw()
	{
		if (graphicsDevice != null && drawBegun)
		{
			drawBegun = false;
			graphicsDevice.Present();
		}
	}
}
