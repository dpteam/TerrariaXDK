using System;
using System.ComponentModel;

namespace Microsoft.Xna.Framework;

internal class FNAWindow : GameWindow
{
	private IntPtr window;

	private string deviceName;

	private bool wantsFullscreen;

	[DefaultValue(false)]
	public override bool AllowUserResizing
	{
		get
		{
			return FNAPlatform.GetWindowResizable(window);
		}
		set
		{
			FNAPlatform.SetWindowResizable(window, value);
		}
	}

	public override Rectangle ClientBounds => FNAPlatform.GetWindowBounds(window);

	public override DisplayOrientation CurrentOrientation { get; internal set; }

	public override IntPtr Handle => window;

	public override bool IsBorderlessEXT
	{
		get
		{
			return FNAPlatform.GetWindowBorderless(window);
		}
		set
		{
			FNAPlatform.SetWindowBorderless(window, value);
		}
	}

	public override string ScreenDeviceName => deviceName;

	internal FNAWindow(IntPtr nativeWindow, string display)
	{
		window = nativeWindow;
		deviceName = display;
		wantsFullscreen = false;
	}

	public override void BeginScreenDeviceChange(bool willBeFullScreen)
	{
		wantsFullscreen = willBeFullScreen;
	}

	public override void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight)
	{
		string text = deviceName;
		FNAPlatform.ApplyWindowChanges(window, clientWidth, clientHeight, wantsFullscreen, screenDeviceName, ref deviceName);
		if (deviceName != text)
		{
			OnScreenDeviceNameChanged();
		}
	}

	internal void INTERNAL_ClientSizeChanged()
	{
		OnClientSizeChanged();
	}

	internal void INTERNAL_ScreenDeviceNameChanged()
	{
		OnScreenDeviceNameChanged();
	}

	internal void INTERNAL_OnOrientationChanged()
	{
		OnOrientationChanged();
	}

	protected internal override void SetSupportedOrientations(DisplayOrientation orientations)
	{
		FNALoggerEXT.LogWarn("Setting SupportedOrientations has no effect!");
	}

	protected override void SetTitle(string title)
	{
		FNAPlatform.SetWindowTitle(window, title);
	}
}
