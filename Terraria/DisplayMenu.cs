using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria.Input;

namespace Terraria;

/// <summary>
/// Display settings screen: window size, fullscreen and internal render resolution.
///
/// Window size and fullscreen apply immediately; the render scale needs the render target and
/// the font atlases to be rebuilt, so it is saved and applied on the next start (the screen
/// says so instead of pretending).
/// </summary>
internal static class DisplayMenu
{
	private const int Rows = 5;

	private static int _selected;

	private static string _status;

	private static int _statusTime;

	public static void Reset()
	{
		_selected = 0;
		_status = null;
		_statusTime = 0;
	}

	/// <summary>Returns true when the screen should close.</summary>
	public static bool Update()
	{
		if (_statusTime > 0 && --_statusTime == 0)
		{
			_status = null;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Down))
		{
			_selected = (_selected + 1) % Rows;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Up))
		{
			_selected = (_selected + Rows - 1) % Rows;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Escape))
		{
			return true;
		}
		int num = RowUnderPointer();
		if (num >= 0)
		{
			_selected = num;
		}
		bool flag = KeyboardMouseInput.KeyPressed(Keys.Space) || (num >= 0 && KeyboardMouseInput.LeftClicked);
		// Left/Right cycle the value on the selected row, like a slider.
		if (KeyboardMouseInput.KeyPressed(Keys.Right))
		{
			Change(_selected, 1);
			return false;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Left))
		{
			Change(_selected, -1);
			return false;
		}
		if (flag)
		{
			Change(_selected, 1);
		}
		return false;
	}

	private static void Change(int row, int direction)
	{
		switch (row)
		{
		case 0:
			ApplyProfile(SettingsProfiles.Next(SettingsProfiles.Current, direction));
			break;
		case 1:
			DisplaySettings.NextWindowSize(direction);
			ApplyWindow();
			_status = "Window: " + DisplaySettings.Width + "x" + DisplaySettings.Height;
			break;
		case 2:
			DisplaySettings.Fullscreen = !DisplaySettings.Fullscreen;
			ApplyWindow();
			_status = DisplaySettings.Fullscreen ? "Fullscreen" : "Windowed";
			break;
		case 3:
			DisplaySettings.NextRenderScale(direction);
			_status = "Render scale: " + (DisplaySettings.RenderScale == ScreenHeights.BASE ? "540p" : ((DisplaySettings.RenderScale == ScreenHeights.HD) ? "720p" : "1080p")) + " - restart required";
			break;
		case 4:
			// Lives here rather than only in Settings.ini: it is the first thing you want to
			// turn on when measuring the effect of a render scale change.
			DisplaySettings.ShowFPS = !DisplaySettings.ShowFPS;
			Main.ShowFPS = DisplaySettings.ShowFPS;
			_status = DisplaySettings.ShowFPS ? "FPS counter: On" : "FPS counter: Off";
			break;
		}
		_statusTime = 240;
		Main.PlaySound(12);
		Persist();
	}

	/// <summary>
	/// Applies a preset and pushes the window part immediately. The render scale inside a
	/// preset needs the same restart as changing it by hand, and the hint below says so.
	/// </summary>
	private static void ApplyProfile(string name)
	{
		if (!SettingsProfiles.Apply(name))
		{
			return;
		}
		ApplyWindow();
		_status = "Profile: " + name + " - render scale applies on the next start";
	}

	/// <summary>Pushes the window size / fullscreen to the graphics device right now.</summary>
	private static void ApplyWindow()
	{
		try
		{
			Main main = UI.Game;
			if (main == null)
			{
				return;
			}
			main.ApplyDisplayChange();
		}
		catch (Exception ex)
		{
			Program.LogWarn("Display", "could not apply window change: " + ex.Message);
			_status = "Could not change the window: " + ex.Message;
		}
	}

	private static void Persist()
	{
		try
		{
			Settings settings = new Settings(Terraria.Storage.AppPaths.SettingsFile);
			DisplaySettings.Save(settings);
			settings.Close();
		}
		catch
		{
		}
	}

	private static int RowUnderPointer()
	{
		Rectangle rectangle;
		if (!TryGetLayout(out rectangle))
		{
			return -1;
		}
		if (KeyboardMouseInput.MouseX < rectangle.X || KeyboardMouseInput.MouseX > rectangle.Right)
		{
			return -1;
		}
		int num = KeyboardMouseInput.MouseY - rectangle.Y;
		if (num < 0)
		{
			return -1;
		}
		int num2 = num / (int)(44f * Main.ScreenMultiplier);
		if (num2 < 0 || num2 >= Rows)
		{
			return -1;
		}
		return num2;
	}

	private static bool TryGetLayout(out Rectangle rows)
	{
		rows = default;
		UI ui = UI.MainUI;
		if (ui == null || ui.CurrentView == null)
		{
			return false;
		}
		int num = (int)(460f * Main.ScreenMultiplier);
		int num2 = (int)(140f * Main.ScreenMultiplier);
		int num3 = (ui.CurrentView.ViewWidth - num) / 2;
		rows = new Rectangle(num3, num2, num, Rows * (int)(44f * Main.ScreenMultiplier));
		return true;
	}

	public static void Draw()
	{
		Rectangle rectangle;
		if (!TryGetLayout(out rectangle))
		{
			return;
		}
		float num = Main.ScreenMultiplier;
		Main.DrawSolidRect(new Rectangle(0, 0, Main.ResolutionWidth, Main.ResolutionHeight), new Color(0, 0, 0, 170));
		Main.DrawSolidRect(new Rectangle(rectangle.X - (int)(10f * num), rectangle.Y - (int)(46f * num), rectangle.Width + (int)(20f * num), rectangle.Height + (int)(76f * num)), new Color(14, 18, 26, 240));
		UI.DrawStringCC(UI.BigFont, "Display", rectangle.Center.X, rectangle.Y - (int)(34f * num), Color.White);
		string[] array = new string[Rows]
		{
			"Profile: " + SettingsProfiles.Current,
			"Window size: " + DisplaySettings.Width + "x" + DisplaySettings.Height,
			"Fullscreen: " + (DisplaySettings.Fullscreen ? "On" : "Off"),
			"Render scale: " + ((DisplaySettings.RenderScale == ScreenHeights.BASE) ? "540p" : ((DisplaySettings.RenderScale == ScreenHeights.HD) ? "720p" : "1080p")),
			"FPS counter: " + (DisplaySettings.ShowFPS ? "On" : "Off")
		};
		for (int i = 0; i < Rows; i++)
		{
			int y = rectangle.Y + i * (int)(44f * num);
			if (i == _selected)
			{
				Main.DrawSolidRect(new Rectangle(rectangle.X, y, rectangle.Width, (int)(44f * num) - 2), new Color(40, 70, 120, 220));
			}
			UI.DrawStringLC(UI.BoldSmallFont, array[i], rectangle.X + (int)(10f * num), y + (int)(12f * num), Color.White);
		}
		int num2 = rectangle.Y + rectangle.Height + (int)(8f * num);
		string text = _status ?? "LMB / Left-Right - change    Esc - back";
		UI.DrawStringCC(UI.SmallFont, text, rectangle.Center.X, num2, new Color(150, 160, 180));
		UI.DrawStringCC(UI.SmallFont, "Render scale is applied on the next start", rectangle.Center.X, num2 + (int)(18f * num), new Color(120, 130, 150));
	}
}
