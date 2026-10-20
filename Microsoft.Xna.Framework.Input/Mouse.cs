using System;

namespace Microsoft.Xna.Framework.Input;

public static class Mouse
{
	internal static int INTERNAL_WindowWidth = GraphicsDeviceManager.DefaultBackBufferWidth;

	internal static int INTERNAL_WindowHeight = GraphicsDeviceManager.DefaultBackBufferHeight;

	internal static int INTERNAL_BackBufferWidth = GraphicsDeviceManager.DefaultBackBufferWidth;

	internal static int INTERNAL_BackBufferHeight = GraphicsDeviceManager.DefaultBackBufferHeight;

	internal static int INTERNAL_MouseWheel = 0;

	public static Action<int> ClickedEXT;

	public static IntPtr WindowHandle { get; set; }

	public static bool IsRelativeMouseModeEXT
	{
		get
		{
			return FNAPlatform.GetRelativeMouseMode();
		}
		set
		{
			FNAPlatform.SetRelativeMouseMode(value);
		}
	}

	public static MouseState GetState()
	{
		FNAPlatform.GetMouseState(WindowHandle, out var x, out var y, out var left, out var middle, out var right, out var x2, out var x3);
		x = (int)((double)x * (double)INTERNAL_BackBufferWidth / (double)INTERNAL_WindowWidth);
		y = (int)((double)y * (double)INTERNAL_BackBufferHeight / (double)INTERNAL_WindowHeight);
		return new MouseState(x, y, INTERNAL_MouseWheel, left, middle, right, x2, x3);
	}

	public static void SetPosition(int x, int y)
	{
		if (!IsRelativeMouseModeEXT)
		{
			x = (int)((double)x * (double)INTERNAL_WindowWidth / (double)INTERNAL_BackBufferWidth);
			y = (int)((double)y * (double)INTERNAL_WindowHeight / (double)INTERNAL_BackBufferHeight);
			FNAPlatform.SetMousePosition(WindowHandle, x, y);
		}
	}

	internal static void INTERNAL_onClicked(int button)
	{
		if (ClickedEXT != null)
		{
			ClickedEXT(button);
		}
	}
}
