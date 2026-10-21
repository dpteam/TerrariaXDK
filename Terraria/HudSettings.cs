using System;

namespace Terraria;

/// <summary>
/// HUD and camera settings: zoom level and the permanent information widgets.
///
/// These live in <c>[Display]</c> and used to be read at startup and then ignored:
/// <c>ZoomLevel</c> was parsed into a private field that nothing applied, and every switch
/// back to the fullscreen view called <c>Zoom(1f)</c>, silently throwing the player's zoom
/// away. The three widget flags were half-wired: <see cref="Player"/> read them from
/// <see cref="Main"/>, but nothing in the game could change them.
/// </summary>
internal static class HudSettings
{
	/// <summary>Smallest allowed camera scale. 1 = the console default.</summary>
	public const float MinZoom = 1f;

	/// <summary>Largest allowed camera scale. Beyond ~1.6 the view stops being useful.</summary>
	public const float MaxZoom = 2f;

	/// <summary>Step for one key press / one menu click.</summary>
	public const float ZoomStep = 0.05f;

	/// <summary>Camera scale, 1..2. Applied to the active <see cref="WorldView"/>.</summary>
	public static float Zoom = MinZoom;

	/// <summary>Show the clock without wearing a watch.</summary>
	public static bool AlwaysWatch;

	/// <summary>Show the depth without wearing a depth meter.</summary>
	public static bool AlwaysDepth;

	/// <summary>Show the horizontal position without wearing a compass.</summary>
	public static bool AlwaysCompass;

	/// <summary>
	/// Reads the values. Bad input falls back to the default instead of throwing: the old
	/// code used <c>Convert.ToSingle</c>, so a hand-edited <c>ZoomLevel=abc</c> crashed the
	/// game before the main menu appeared.
	/// </summary>
	public static void Load(Settings settings)
	{
		Zoom = ParseZoom(settings.Get("Display", "ZoomLevel"));
		AlwaysWatch = settings.Get("Display", "AlwaysOnWatch").ToLower().Equals("true");
		AlwaysDepth = settings.Get("Display", "AlwaysOnDepth").ToLower().Equals("true");
		AlwaysCompass = settings.Get("Display", "AlwaysOnCompass").ToLower().Equals("true");
	}

	public static void Save(Settings settings)
	{
		// Stored as the offset from 1 so the file keeps the meaning it had before the
		// migration (0 = default zoom), and older builds can still read it.
		settings.Update("Display", "ZoomLevel", (Zoom - MinZoom).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), "Camera zoom: 0 = default, 1 = closest. Adjust in game with + and -.");
		settings.Update("Display", "AlwaysOnWatch", AlwaysWatch ? "True" : "False", "Always show the clock, without wearing a watch.");
		settings.Update("Display", "AlwaysOnDepth", AlwaysDepth ? "True" : "False", "Always show the depth, without wearing a depth meter.");
		settings.Update("Display", "AlwaysOnCompass", AlwaysCompass ? "True" : "False", "Always show the horizontal position, without wearing a compass.");
	}

	/// <summary>
	/// The file stores the offset from the default zoom: 0 means "no zoom", 1 means "closest".
	/// Anything outside 0..1 (a hand-edited file, a stale value) is clamped rather than
	/// trusted, and text that is not a number falls back to the default.
	/// </summary>
	internal static float ParseZoom(string text)
	{
		float result;
		if (!float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result))
		{
			return MinZoom;
		}
		if (result < 0f)
		{
			result = 0f;
		}
		else if (result > MaxZoom - MinZoom)
		{
			result = MaxZoom - MinZoom;
		}
		return MinZoom + result;
	}

	/// <summary>Zooms in (+1) or out (-1). Returns true when the value actually changed.</summary>
	public static bool AdjustZoom(int direction)
	{
		float num = Zoom + ZoomStep * direction;
		if (num < MinZoom)
		{
			num = MinZoom;
		}
		else if (num > MaxZoom)
		{
			num = MaxZoom;
		}
		// Rounded to the step grid, otherwise repeated float additions drift to 1.3000001.
		num = MinZoom + (float)Math.Round((num - MinZoom) / ZoomStep) * ZoomStep;
		if (num == Zoom)
		{
			return false;
		}
		Zoom = num;
		Apply();
		return true;
	}

	public static void ResetZoom()
	{
		Zoom = MinZoom;
		Apply();
	}

	/// <summary>Pushes the zoom to the player's view. Safe outside a world (no-op).</summary>
	public static void Apply()
	{
		try
		{
			UI ui = UI.MainUI;
			if (ui == null || ui.ActivePlayer == null || ui.ActivePlayer.CurrentView == null)
			{
				return;
			}
			ui.ActivePlayer.CurrentView.DirectZoom(Zoom);
		}
		catch
		{
			// No world yet: the value is applied when the view is first created.
		}
	}

	/// <summary>Human readable zoom, e.g. "1.25x".</summary>
	public static string ZoomLabel
	{
		get
		{
			return Zoom.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "x";
		}
	}
}
