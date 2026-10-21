using System;

namespace Terraria;

/// <summary>
/// Named presets for the display settings.
///
/// The console build shipped a handful of options that only made sense together (window
/// size, fullscreen, internal resolution). Setting them one by one is easy to get wrong -
/// fullscreen 1080p with the 540p preset looks blurry, a handheld with the 1080p preset
/// runs at single-digit fps. A profile sets the whole group at once and the settings
/// screen shows which one is active.
///
/// Detection is by value, not by a stored name: if the player changes one field by hand the
/// screen honestly says "Custom" instead of claiming a preset that no longer matches.
/// </summary>
internal static class SettingsProfiles
{
	public const string Default = "Default";

	public const string Tv = "TV";

	public const string Handheld = "Handheld";

	public const string Custom = "Custom";

	/// <summary>Order the screen cycles through. "Custom" is not applied, so it is not here.</summary>
	private static readonly string[] Cycle = new string[3] { Default, Tv, Handheld };

	/// <summary>The preset that matches the current settings, or <see cref="Custom"/>.</summary>
	public static string Current
	{
		get
		{
			if (Matches(1280, 720, fullscreen: true, ScreenHeights.BASE))
			{
				return Default;
			}
			if (Matches(1920, 1080, fullscreen: true, ScreenHeights.HD))
			{
				return Tv;
			}
			if (Matches(1280, 720, fullscreen: false, ScreenHeights.BASE))
			{
				return Handheld;
			}
			return Custom;
		}
	}

	private static bool Matches(int width, int height, bool fullscreen, ScreenHeights scale)
	{
		return DisplaySettings.Width == width && DisplaySettings.Height == height && DisplaySettings.Fullscreen == fullscreen && DisplaySettings.RenderScale == scale;
	}

	/// <summary>
	/// Applies a preset. Returns false for an unknown name and for "Custom", which is not
	/// something you can apply - it is what you get by editing the fields.
	/// </summary>
	public static bool Apply(string name)
	{
		if (string.Equals(name, Default, StringComparison.OrdinalIgnoreCase))
		{
			DisplaySettings.Width = 1280;
			DisplaySettings.Height = 720;
			DisplaySettings.Fullscreen = true;
			DisplaySettings.RenderScale = ScreenHeights.BASE;
			return true;
		}
		if (string.Equals(name, Tv, StringComparison.OrdinalIgnoreCase))
		{
			// 1080p output with the 720p render preset: sharp on a TV, and the extra headroom
			// over 540p costs less than rendering at full 1080p.
			DisplaySettings.Width = 1920;
			DisplaySettings.Height = 1080;
			DisplaySettings.Fullscreen = true;
			DisplaySettings.RenderScale = ScreenHeights.HD;
			return true;
		}
		if (string.Equals(name, Handheld, StringComparison.OrdinalIgnoreCase))
		{
			// A small screen does not need 1080p, and the hardware cannot afford it. Windowed
			// so the taskbar/window manager still works on the small display.
			DisplaySettings.Width = 1280;
			DisplaySettings.Height = 720;
			DisplaySettings.Fullscreen = false;
			DisplaySettings.RenderScale = ScreenHeights.BASE;
			return true;
		}
		return false;
	}

	/// <summary>Cycles through the presets; "Custom" is skipped because it cannot be applied.</summary>
	public static string Next(string current, int direction)
	{
		int num = 0;
		for (int i = 0; i < Cycle.Length; i++)
		{
			if (string.Equals(Cycle[i], current, StringComparison.OrdinalIgnoreCase))
			{
				num = i;
				break;
			}
		}
		num = (num + direction + Cycle.Length) % Cycle.Length;
		if (num < 0)
		{
			num = 0;
		}
		return Cycle[num];
	}
}
