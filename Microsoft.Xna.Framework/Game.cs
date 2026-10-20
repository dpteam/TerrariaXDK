using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Microsoft.Xna.Framework;

public class Game : IDisposable
{
	private ContentManager INTERNAL_content;

	private TimeSpan INTERNAL_inactiveSleepTime;

	private bool INTERNAL_isActive;

	private bool INTERNAL_isMouseVisible;

	private TimeSpan INTERNAL_targetElapsedTime;

	internal bool RunApplication;

	private List<IUpdateable> updateableComponents;

	private List<IUpdateable> currentlyUpdatingComponents;

	private List<IDrawable> drawableComponents;

	private List<IDrawable> currentlyDrawingComponents;

	private IGraphicsDeviceService graphicsDeviceService;

	private IGraphicsDeviceManager graphicsDeviceManager;

	private GraphicsAdapter currentAdapter;

	private bool hasInitialized;

	private bool suppressDraw;

	private bool isDisposed;

	private readonly GameTime gameTime;

	private Stopwatch gameTimer;

	private TimeSpan accumulatedElapsedTime;

	private long previousTicks = 0L;

	private int updateFrameLag;

	private bool forceElapsedTimeToZero = false;

	private const int PREVIOUS_SLEEP_TIME_COUNT = 128;

	private const int SLEEP_TIME_MASK = 127;

	private TimeSpan[] previousSleepTimes = new TimeSpan[128];

	private int sleepTimeIndex = 0;

	private TimeSpan worstCaseSleepPrecision = TimeSpan.FromMilliseconds(1.0);

	private static readonly TimeSpan MaxElapsedTime = TimeSpan.FromMilliseconds(500.0);

	private bool[] textInputControlDown;

	private int[] textInputControlRepeat;

	private bool textInputSuppress;

	public GameComponentCollection Components { get; private set; }

	public ContentManager Content
	{
		get
		{
			return INTERNAL_content;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException();
			}
			INTERNAL_content = value;
		}
	}

	public GraphicsDevice GraphicsDevice
	{
		get
		{
			if (graphicsDeviceService == null)
			{
				graphicsDeviceService = (IGraphicsDeviceService)Services.GetService(typeof(IGraphicsDeviceService));
				if (graphicsDeviceService == null)
				{
					throw new InvalidOperationException("No Graphics Device Service");
				}
			}
			return graphicsDeviceService.GraphicsDevice;
		}
	}

	public TimeSpan InactiveSleepTime
	{
		get
		{
			return INTERNAL_inactiveSleepTime;
		}
		set
		{
			if (value < TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException("The time must be positive.", (Exception)null);
			}
			if (INTERNAL_inactiveSleepTime != value)
			{
				INTERNAL_inactiveSleepTime = value;
			}
		}
	}

	public bool IsActive
	{
		get
		{
			return INTERNAL_isActive;
		}
		internal set
		{
			if (INTERNAL_isActive != value)
			{
				INTERNAL_isActive = value;
				if (INTERNAL_isActive)
				{
					OnActivated(this, EventArgs.Empty);
				}
				else
				{
					OnDeactivated(this, EventArgs.Empty);
				}
			}
		}
	}

	public bool IsFixedTimeStep { get; set; }

	public bool IsMouseVisible
	{
		get
		{
			return INTERNAL_isMouseVisible;
		}
		set
		{
			if (INTERNAL_isMouseVisible != value)
			{
				INTERNAL_isMouseVisible = value;
				FNAPlatform.OnIsMouseVisibleChanged(value);
			}
		}
	}

	public LaunchParameters LaunchParameters { get; private set; }

	public TimeSpan TargetElapsedTime
	{
		get
		{
			return INTERNAL_targetElapsedTime;
		}
		set
		{
			if (value <= TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException("The time must be positive and non-zero.", (Exception)null);
			}
			INTERNAL_targetElapsedTime = value;
		}
	}

	public GameServiceContainer Services { get; private set; }

	public GameWindow Window { get; private set; }

	public event EventHandler<EventArgs> Activated;

	public event EventHandler<EventArgs> Deactivated;

	public event EventHandler<EventArgs> Disposed;

	public event EventHandler<EventArgs> Exiting;

	public Game()
	{
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		LaunchParameters = new LaunchParameters();
		Components = new GameComponentCollection();
		Services = new GameServiceContainer();
		Content = new ContentManager(Services);
		updateableComponents = new List<IUpdateable>();
		currentlyUpdatingComponents = new List<IUpdateable>();
		drawableComponents = new List<IDrawable>();
		currentlyDrawingComponents = new List<IDrawable>();
		IsMouseVisible = false;
		IsFixedTimeStep = true;
		TargetElapsedTime = TimeSpan.FromTicks(166667L);
		InactiveSleepTime = TimeSpan.FromSeconds(0.02);
		for (int i = 0; i < previousSleepTimes.Length; i++)
		{
			previousSleepTimes[i] = TimeSpan.FromMilliseconds(1.0);
		}
		textInputControlDown = new bool[FNAPlatform.TextInputCharacters.Length];
		textInputControlRepeat = new int[FNAPlatform.TextInputCharacters.Length];
		hasInitialized = false;
		suppressDraw = false;
		isDisposed = false;
		gameTime = new GameTime();
		Window = FNAPlatform.CreateWindow();
		Mouse.WindowHandle = Window.Handle;
		TouchPanel.WindowHandle = Window.Handle;
		FrameworkDispatcher.Update();
		RunApplication = true;
	}

	~Game()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
		if (Disposed != null)
		{
			Disposed(this, EventArgs.Empty);
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (isDisposed)
		{
			return;
		}
		if (disposing)
		{
			for (int i = 0; i < Components.Count; i++)
			{
				if (Components[i] is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
			if (Content != null)
			{
				Content.Dispose();
			}
			if (graphicsDeviceService != null)
			{
				(graphicsDeviceService as IDisposable).Dispose();
			}
			if (Window != null)
			{
				FNAPlatform.DisposeWindow(Window);
			}
			ContentTypeReaderManager.ClearTypeCreators();
		}
		AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
		isDisposed = true;
	}

	[DebuggerNonUserCode]
	private void AssertNotDisposed()
	{
		if (isDisposed)
		{
			string name = GetType().Name;
			throw new ObjectDisposedException(name, "The " + name + " object was used after being Disposed.");
		}
	}

	public void Exit()
	{
		RunApplication = false;
		suppressDraw = true;
	}

	public void ResetElapsedTime()
	{
		if (!IsFixedTimeStep)
		{
			forceElapsedTimeToZero = true;
		}
	}

	public void SuppressDraw()
	{
		suppressDraw = true;
	}

	public void RunOneFrame()
	{
		if (!hasInitialized)
		{
			DoInitialize();
			gameTimer = Stopwatch.StartNew();
			hasInitialized = true;
		}
		Tick();
	}

	public void Run()
	{
		AssertNotDisposed();
		if (!hasInitialized)
		{
			DoInitialize();
			hasInitialized = true;
		}
		BeginRun();
		BeforeLoop();
		gameTimer = Stopwatch.StartNew();
		RunLoop();
		EndRun();
		AfterLoop();
	}

	public void Tick()
	{
		AdvanceElapsedTime();
		if (IsFixedTimeStep)
		{
			while (accumulatedElapsedTime + worstCaseSleepPrecision < TargetElapsedTime)
			{
				Thread.Sleep(1);
				TimeSpan timeSpentSleeping = AdvanceElapsedTime();
				UpdateEstimatedSleepPrecision(timeSpentSleeping);
			}
			while (accumulatedElapsedTime < TargetElapsedTime)
			{
				Thread.SpinWait(1);
				AdvanceElapsedTime();
			}
		}
		FNAPlatform.PollEvents(this, ref currentAdapter, textInputControlDown, textInputControlRepeat, ref textInputSuppress);
		if (accumulatedElapsedTime > MaxElapsedTime)
		{
			accumulatedElapsedTime = MaxElapsedTime;
		}
		if (IsFixedTimeStep)
		{
			gameTime.ElapsedGameTime = TargetElapsedTime;
			int num = 0;
			while (accumulatedElapsedTime >= TargetElapsedTime)
			{
				gameTime.TotalGameTime += TargetElapsedTime;
				accumulatedElapsedTime -= TargetElapsedTime;
				num++;
				AssertNotDisposed();
				Update(gameTime);
			}
			updateFrameLag += Math.Max(0, num - 1);
			if (gameTime.IsRunningSlowly)
			{
				if (updateFrameLag == 0)
				{
					gameTime.IsRunningSlowly = false;
				}
			}
			else if (updateFrameLag >= 5)
			{
				gameTime.IsRunningSlowly = true;
			}
			if (num == 1 && updateFrameLag > 0)
			{
				updateFrameLag--;
			}
			gameTime.ElapsedGameTime = TimeSpan.FromTicks(TargetElapsedTime.Ticks * num);
		}
		else
		{
			if (forceElapsedTimeToZero)
			{
				gameTime.ElapsedGameTime = TimeSpan.Zero;
				forceElapsedTimeToZero = false;
			}
			else
			{
				gameTime.ElapsedGameTime = accumulatedElapsedTime;
				gameTime.TotalGameTime += gameTime.ElapsedGameTime;
			}
			accumulatedElapsedTime = TimeSpan.Zero;
			AssertNotDisposed();
			Update(gameTime);
		}
		if (suppressDraw)
		{
			suppressDraw = false;
		}
		else if (BeginDraw())
		{
			Draw(gameTime);
			EndDraw();
		}
	}

	internal void RedrawWindow()
	{
		if (gameTime.TotalGameTime != TimeSpan.Zero && BeginDraw())
		{
			Draw(new GameTime(gameTime.TotalGameTime, TimeSpan.Zero));
			EndDraw();
		}
	}

	protected virtual bool BeginDraw()
	{
		if (graphicsDeviceManager != null)
		{
			return graphicsDeviceManager.BeginDraw();
		}
		return true;
	}

	protected virtual void EndDraw()
	{
		if (graphicsDeviceManager != null)
		{
			graphicsDeviceManager.EndDraw();
		}
	}

	protected virtual void BeginRun()
	{
	}

	protected virtual void EndRun()
	{
	}

	protected virtual void LoadContent()
	{
	}

	protected virtual void UnloadContent()
	{
	}

	protected virtual void Initialize()
	{
		for (int i = 0; i < Components.Count; i++)
		{
			Components[i].Initialize();
		}
		graphicsDeviceService = (IGraphicsDeviceService)Services.GetService(typeof(IGraphicsDeviceService));
		if (graphicsDeviceService == null)
		{
			return;
		}
		graphicsDeviceService.DeviceDisposing += delegate
		{
			UnloadContent();
		};
		if (graphicsDeviceService.GraphicsDevice != null)
		{
			LoadContent();
			return;
		}
		graphicsDeviceService.DeviceCreated += delegate
		{
			LoadContent();
		};
	}

	protected virtual void Draw(GameTime gameTime)
	{
		lock (drawableComponents)
		{
			for (int i = 0; i < drawableComponents.Count; i++)
			{
				currentlyDrawingComponents.Add(drawableComponents[i]);
			}
		}
		foreach (IDrawable currentlyDrawingComponent in currentlyDrawingComponents)
		{
			if (currentlyDrawingComponent.Visible)
			{
				currentlyDrawingComponent.Draw(gameTime);
			}
		}
		currentlyDrawingComponents.Clear();
	}

	protected virtual void Update(GameTime gameTime)
	{
		lock (updateableComponents)
		{
			for (int i = 0; i < updateableComponents.Count; i++)
			{
				currentlyUpdatingComponents.Add(updateableComponents[i]);
			}
		}
		foreach (IUpdateable currentlyUpdatingComponent in currentlyUpdatingComponents)
		{
			if (currentlyUpdatingComponent.Enabled)
			{
				currentlyUpdatingComponent.Update(gameTime);
			}
		}
		currentlyUpdatingComponents.Clear();
		FrameworkDispatcher.Update();
	}

	protected virtual void OnExiting(object sender, EventArgs args)
	{
		if (Exiting != null)
		{
			Exiting(this, args);
		}
	}

	protected virtual void OnActivated(object sender, EventArgs args)
	{
		AssertNotDisposed();
		if (Activated != null)
		{
			Activated(this, args);
		}
	}

	protected virtual void OnDeactivated(object sender, EventArgs args)
	{
		AssertNotDisposed();
		if (Deactivated != null)
		{
			Deactivated(this, args);
		}
	}

	protected virtual bool ShowMissingRequirementMessage(Exception exception)
	{
		if (exception is NoAudioHardwareException)
		{
			FNAPlatform.ShowRuntimeError(Window.Title, "Could not find a suitable audio device.  Verify that a sound card is\ninstalled, and check the driver properties to make sure it is not disabled.");
			return true;
		}
		if (exception is NoSuitableGraphicsDeviceException)
		{
			FNAPlatform.ShowRuntimeError(Window.Title, "Could not find a suitable graphics device. More information:\n\n" + exception.Message);
			return true;
		}
		return false;
	}

	private void DoInitialize()
	{
		AssertNotDisposed();
		graphicsDeviceManager = (IGraphicsDeviceManager)Services.GetService(typeof(IGraphicsDeviceManager));
		if (graphicsDeviceManager != null)
		{
			graphicsDeviceManager.CreateDevice();
		}
		Initialize();
		updateableComponents.Clear();
		drawableComponents.Clear();
		for (int i = 0; i < Components.Count; i++)
		{
			CategorizeComponent(Components[i]);
		}
		Components.ComponentAdded += OnComponentAdded;
		Components.ComponentRemoved += OnComponentRemoved;
	}

	private void CategorizeComponent(IGameComponent component)
	{
		if (component is IUpdateable updateable)
		{
			lock (updateableComponents)
			{
				SortUpdateable(updateable);
			}
			updateable.UpdateOrderChanged += OnUpdateOrderChanged;
		}
		if (component is IDrawable drawable)
		{
			lock (drawableComponents)
			{
				SortDrawable(drawable);
			}
			drawable.DrawOrderChanged += OnDrawOrderChanged;
		}
	}

	private void SortUpdateable(IUpdateable updateable)
	{
		for (int i = 0; i < updateableComponents.Count; i++)
		{
			if (updateable.UpdateOrder < updateableComponents[i].UpdateOrder)
			{
				updateableComponents.Insert(i, updateable);
				return;
			}
		}
		updateableComponents.Add(updateable);
	}

	private void SortDrawable(IDrawable drawable)
	{
		for (int i = 0; i < drawableComponents.Count; i++)
		{
			if (drawable.DrawOrder < drawableComponents[i].DrawOrder)
			{
				drawableComponents.Insert(i, drawable);
				return;
			}
		}
		drawableComponents.Add(drawable);
	}

	private void BeforeLoop()
	{
		currentAdapter = FNAPlatform.RegisterGame(this);
		IsActive = true;
		TouchPanel.TouchDeviceExists = FNAPlatform.GetTouchCapabilities().IsConnected;
	}

	private void AfterLoop()
	{
		FNAPlatform.UnregisterGame(this);
	}

	private void RunLoop()
	{
		if (FNAPlatform.NeedsPlatformMainLoop())
		{
			FNAPlatform.RunPlatformMainLoop(this);
		}
		while (RunApplication)
		{
			Tick();
		}
		OnExiting(this, EventArgs.Empty);
	}

	private TimeSpan AdvanceElapsedTime()
	{
		long ticks = gameTimer.Elapsed.Ticks;
		TimeSpan timeSpan = TimeSpan.FromTicks(ticks - previousTicks);
		accumulatedElapsedTime += timeSpan;
		previousTicks = ticks;
		return timeSpan;
	}

	private void UpdateEstimatedSleepPrecision(TimeSpan timeSpentSleeping)
	{
		TimeSpan timeSpan = TimeSpan.FromMilliseconds(4.0);
		if (timeSpentSleeping > timeSpan)
		{
			timeSpentSleeping = timeSpan;
		}
		if (timeSpentSleeping >= worstCaseSleepPrecision)
		{
			worstCaseSleepPrecision = timeSpentSleeping;
		}
		else if (previousSleepTimes[sleepTimeIndex] == worstCaseSleepPrecision)
		{
			TimeSpan timeSpan2 = TimeSpan.MinValue;
			for (int i = 0; i < previousSleepTimes.Length; i++)
			{
				if (previousSleepTimes[i] > timeSpan2)
				{
					timeSpan2 = previousSleepTimes[i];
				}
			}
			worstCaseSleepPrecision = timeSpan2;
		}
		previousSleepTimes[sleepTimeIndex] = timeSpentSleeping;
		sleepTimeIndex = (sleepTimeIndex + 1) & 0x7F;
	}

	private void OnComponentAdded(object sender, GameComponentCollectionEventArgs e)
	{
		e.GameComponent.Initialize();
		CategorizeComponent(e.GameComponent);
	}

	private void OnComponentRemoved(object sender, GameComponentCollectionEventArgs e)
	{
		if (e.GameComponent is IUpdateable updateable)
		{
			lock (updateableComponents)
			{
				updateableComponents.Remove(updateable);
			}
			updateable.UpdateOrderChanged -= OnUpdateOrderChanged;
		}
		if (e.GameComponent is IDrawable drawable)
		{
			lock (drawableComponents)
			{
				drawableComponents.Remove(drawable);
			}
			drawable.DrawOrderChanged -= OnDrawOrderChanged;
		}
	}

	private void OnUpdateOrderChanged(object sender, EventArgs e)
	{
		IUpdateable updateable = sender as IUpdateable;
		lock (updateableComponents)
		{
			updateableComponents.Remove(updateable);
			SortUpdateable(updateable);
		}
	}

	private void OnDrawOrderChanged(object sender, EventArgs e)
	{
		IDrawable drawable = sender as IDrawable;
		lock (drawableComponents)
		{
			drawableComponents.Remove(drawable);
			SortDrawable(drawable);
		}
	}

	private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
	{
		ShowMissingRequirementMessage(args.ExceptionObject as Exception);
	}
}
