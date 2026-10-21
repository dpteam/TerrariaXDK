using System;
using Microsoft.Xna.Framework;

namespace Terraria;

/// <summary>
/// Display configuration: window size, fullscreen, and the internal render resolution.
///
/// Two independent knobs, which is what the console build always had:
/// <list type="bullet">
/// <item><b>backbuffer</b> - the window / output size (<c>Width</c>, <c>Height</c>);</item>
/// <item><b>render scale</b> - the internal resolution the game draws into, then stretched
/// to the backbuffer (<see cref="ScreenHeights"/> + <see cref="Main.ScreenMultiplier"/>).</item>
/// </list>
/// The console shipped three presets (540p / 720p / 1080p) and that is what
/// <c>[Display] ScreenHeight</c> always meant - it just was never read.
/// </summary>
internal static class DisplaySettings
{
	/// <summary>800x450 .. 3840x2160, in 16:9 steps; the window size menu cycles this list.</summary>
	public static readonly int[][] WindowSizes = new int[6][]
	{
		new int[2] { 1280, 720 },
		new int[2] { 1600, 900 },
		new int[2] { 1920, 1080 },
		new int[2] { 2560, 1440 },
		new int[2] { 3840, 2160 },
		new int[2] { 960, 540 }
	};

	public static int Width = 1280;

	public static int Height = 720;

	public static bool Fullscreen = true;

	/// <summary>Internal render resolution preset (0 = base, 1 = HD, 2 = full HD).</summary>
	public static ScreenHeights RenderScale = ScreenHeights.BASE;

	/// <summary>FPS / memory readout in the corner.</summary>
	public static bool ShowFPS;

	/// <summary>Present with vsync. Ignored when <see cref="FpsLimit"/> is set.</summary>
	public static bool VSync = true;

	/// <summary>
	/// Frame rate cap. 0 means "no cap" (vsync or the driver decides). Any other value drives
	/// the game with a fixed time step, which is what actually holds the rate steady - a
	/// counter that skips drawing would still burn a whole core.
	/// </summary>
	public static int FpsLimit;

	/// <summary>Limits offered by the settings screen.</summary>
	public static readonly int[] FpsLimits = new int[6]
	{
		0, 30, 60, 120, 144, 240
	};

	public static string Label
	{
		get
		{
			string text = (RenderScale == ScreenHeights.BASE) ? "540p" : ((RenderScale == ScreenHeights.HD) ? "720p" : "1080p");
			return Width + "x" + Height + "  (" + text + (Fullscreen ? ", fullscreen" : ", windowed") + ")";
		}
	}

	public static void Load(Settings settings)
	{
		Width = ParseInt(settings.Get("Display", "Width"), 1280);
		Height = ParseInt(settings.Get("Display", "Height"), 720);
		Fullscreen = settings.Get("Display", "StartFullscreen").ToLower().Equals("true");
		ShowFPS = settings.Get("Display", "ShowFPS").ToLower().Equals("true");
		VSync = !settings.Get("Display", "VSync").ToLower().Equals("false");
		int num2 = ParseInt(settings.Get("Display", "FpsLimit"), 0);
		FpsLimit = IsSupportedFpsLimit(num2) ? num2 : 0;
		int num = ParseInt(settings.Get("Display", "ScreenHeight"), 0);
		if (num < 0 || num > 2)
		{
			num = 0;
		}
		RenderScale = (ScreenHeights)num;
		// Guard against nonsense values in a hand-edited file.
		if (Width < 320 || Width > 7680 || Height < 240 || Height > 4320)
		{
			Width = 1280;
			Height = 720;
		}
	}

	public static void Save(Settings settings)
	{
		settings.Update("Display", "Width", Width.ToString(), "Window width in pixels.");
		settings.Update("Display", "Height", Height.ToString(), "Window height in pixels.");
		settings.Update("Display", "ScreenHeight", ((int)RenderScale).ToString(), "Internal render resolution: 0 = 540p, 1 = 720p, 2 = 1080p.\n; Higher values look sharper and cost more.");
		settings.Update("Display", "StartFullscreen", Fullscreen ? "True" : "False", "Start in fullscreen mode.");
		settings.Update("Display", "ShowFPS", ShowFPS ? "True" : "False", "Shows the frame rate in the corner.");
		settings.Update("Display", "VSync", VSync ? "True" : "False", "Vertical sync. Prevents tearing; ignored when FpsLimit is set.");
		settings.Update("Display", "FpsLimit", FpsLimit.ToString(), "Frame rate cap: 0 = unlimited, or 30/60/120/144/240.\n; A cap lowers GPU load and heat, which matters on handhelds.");
	}

	/// <summary>Cycles the window presets.</summary>
	public static void NextWindowSize(int direction)
	{
		int num = 0;
		for (int i = 0; i < WindowSizes.Length; i++)
		{
			if (WindowSizes[i][0] == Width && WindowSizes[i][1] == Height)
			{
				num = i;
				break;
			}
		}
		num = (num + direction + WindowSizes.Length) % WindowSizes.Length;
		Width = WindowSizes[num][0];
		Height = WindowSizes[num][1];
	}

	/// <summary>Cycles the internal resolution preset.</summary>
	public static void NextRenderScale(int direction)
	{
		int num = (int)RenderScale + direction;
		if (num < 0)
		{
			num = 2;
		}
		else if (num > 2)
		{
			num = 0;
		}
		RenderScale = (ScreenHeights)num;
	}

	public static bool IsSupportedFpsLimit(int limit)
	{
		for (int i = 0; i < FpsLimits.Length; i++)
		{
			if (FpsLimits[i] == limit)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>Cycles the frame cap through the offered values.</summary>
	public static void NextFpsLimit(int direction)
	{
		int num = 0;
		for (int i = 0; i < FpsLimits.Length; i++)
		{
			if (FpsLimits[i] == FpsLimit)
			{
				num = i;
				break;
			}
		}
		num = (num + direction + FpsLimits.Length) % FpsLimits.Length;
		FpsLimit = FpsLimits[num];
	}

	/// <summary>
	/// Applies vsync and the frame cap. Called at startup and whenever either changes.
	///
	/// An explicit cap wins over vsync: mixing a fixed time step with a vsync'd present adds
	/// judder, because the two limits are never exactly the same period.
	/// </summary>
	public static void ApplyFrameLimit(Game game, GraphicsDeviceManager graphics)
	{
		if (game == null || graphics == null)
		{
			return;
		}
		if (FpsLimit > 1)
		{
			game.IsFixedTimeStep = true;
			game.TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / FpsLimit);
			graphics.SynchronizeWithVerticalRetrace = false;
			return;
		}
		// Unlimited: let vsync (or the driver) pace the frame.
		game.IsFixedTimeStep = true;
		game.TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
		graphics.SynchronizeWithVerticalRetrace = VSync;
	}

	/// <summary>
	/// Applies the render scale to the game's statics. Called before the render target and the
	/// UI are created, so everything derives from the same numbers.
	/// </summary>
	public static void ApplyRenderScale()
	{
		switch (RenderScale)
		{
		case ScreenHeights.HD:
			// 1.3333x of the base keeps the 4:3 internal frame the UI was laid out for.
			Main.ScreenMultiplier = 4f / 3f;
			Main.ResolutionWidth = 1280;
			Main.ResolutionHeight = 960;
			break;
		case ScreenHeights.FHD:
			Main.ScreenMultiplier = 2f;
			Main.ResolutionWidth = 1920;
			Main.ResolutionHeight = 1440;
			break;
		default:
			Main.ScreenMultiplier = 1f;
			Main.ResolutionWidth = 960;
			Main.ResolutionHeight = 720;
			break;
		}
		Main.ScreenHeightPtr = RenderScale;
	}

	private static int ParseInt(string text, int fallback)
	{
		int result;
		if (int.TryParse(text, out result))
		{
			return result;
		}
		return fallback;
	}
}
