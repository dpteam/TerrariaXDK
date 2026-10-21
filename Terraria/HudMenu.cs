using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria.Input;

namespace Terraria;

/// <summary>
/// HUD settings screen: camera zoom and the always-on information widgets.
///
/// All four values existed in <c>Settings.ini</c> and none of them could be changed in game.
/// Zoom now applies immediately, so the player can see the result while adjusting instead of
/// discovering it after a restart.
/// </summary>
internal static class HudMenu
{
	private const int Rows = 6;

	private static int _selected;

	private static string _status;

	private static int _statusTime;

	public static void Reset()
	{
		_selected = 0;
		_status = null;
		_statusTime = 0;
	}

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
			if (!HudSettings.AdjustZoom(direction))
			{
				SetStatus(Texts.Format((direction > 0) ? "ui.hud.zoom.max" : "ui.hud.zoom.min", HudSettings.ZoomLabel));
				return;
			}
			SetStatus(Texts.Format("ui.hud.zoom", HudSettings.ZoomLabel));
			break;
		case 1:
			HudSettings.ResetZoom();
			SetStatus(Texts.Format("ui.hud.zoom.reset.done", HudSettings.ZoomLabel));
			break;
		case 2:
			HudSettings.AlwaysWatch = !HudSettings.AlwaysWatch;
			SetStatus(Texts.Format("ui.hud.clock", WidgetState(HudSettings.AlwaysWatch, "ui.hud.with.watch")));
			break;
		case 3:
			HudSettings.AlwaysDepth = !HudSettings.AlwaysDepth;
			SetStatus(Texts.Format("ui.hud.depth", WidgetState(HudSettings.AlwaysDepth, "ui.hud.with.depth")));
			break;
		case 4:
			HudSettings.AlwaysCompass = !HudSettings.AlwaysCompass;
			SetStatus(Texts.Format("ui.hud.compass", WidgetState(HudSettings.AlwaysCompass, "ui.hud.with.compass")));
			break;
		case 5:
			// Same spirit as the widgets above: an option that used to exist only in the file.
			Main.HardmodeAlert = !Main.HardmodeAlert;
			SetStatus(Texts.Format("ui.hud.hardmode", Texts.Get(Main.HardmodeAlert ? "ui.common.on" : "ui.common.off")));
			break;
		}
		Main.PlaySound(12);
		Persist();
	}

	/// <summary>The widget state: "always on", or what the player needs for it.</summary>
	private static string WidgetState(bool always, string otherwise)
	{
		return Texts.Get(always ? "ui.hud.always" : otherwise);
	}

	private static void SetStatus(string text)
	{
		_status = text;
		_statusTime = 240;
	}

	private static void Persist()
	{
		try
		{
			Settings settings = new Settings(Terraria.Storage.AppPaths.SettingsFile);
			HudSettings.Save(settings);
			settings.Update("Additions", "HardmodeAlert", Main.HardmodeAlert ? "True" : "False", "Shows an introduction the first time a world enters hardmode.");
			settings.Close();
		}
		catch (Exception ex)
		{
			Program.LogWarn("HudMenu", "could not save the HUD settings: " + ex.Message);
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
		int num = (int)(470f * Main.ScreenMultiplier);
		int num2 = (int)(150f * Main.ScreenMultiplier);
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
		UI.DrawStringCC(UI.BigFont, Texts.Get("ui.hud.title"), rectangle.Center.X, rectangle.Y - (int)(34f * num), Color.White);
		string[] array = new string[Rows]
		{
			Texts.Format("ui.hud.zoom", HudSettings.ZoomLabel),
			Texts.Get("ui.hud.zoom.reset"),
			Texts.Format("ui.hud.clock", WidgetState(HudSettings.AlwaysWatch, "ui.hud.with.watch")),
			Texts.Format("ui.hud.depth", WidgetState(HudSettings.AlwaysDepth, "ui.hud.with.depth")),
			Texts.Format("ui.hud.compass", WidgetState(HudSettings.AlwaysCompass, "ui.hud.with.compass")),
			Texts.Format("ui.hud.hardmode", Texts.Get(Main.HardmodeAlert ? "ui.common.on" : "ui.common.off"))
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
		string text = _status ?? Texts.Get("ui.hud.change.back");
		UI.DrawStringCC(UI.SmallFont, text, rectangle.Center.X, num2, new Color(150, 160, 180));
		UI.DrawStringCC(UI.SmallFont, Texts.Get("ui.hud.hint.zoom"), rectangle.Center.X, num2 + (int)(18f * num), new Color(120, 130, 150));
	}
}
