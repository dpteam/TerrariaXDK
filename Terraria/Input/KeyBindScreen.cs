using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Terraria.Input;

/// <summary>
/// Rebind screen. Shows every action with its current key; clicking (or pressing Space)
/// starts capture, and the next key press becomes the new binding.
///
/// Drawn manually instead of through the generic menu renderer because the generic list is
/// capped at 14 rows and needs a second column for the key names.
/// </summary>
internal static class KeyBindScreen
{
	private const int RowHeight = 30;

	private const int VisibleRows = 11;

	private static int _selected;

	private static int _scroll;

	private static bool _capturing;

	private static string _status;

	private static int _statusTime;

	private static KeyBindings.Action[] _actions = new KeyBindings.Action[0];

	/// <summary>Recomputes the action list once per frame it is displayed.</summary>
	private static KeyBindings.Action[] Actions
	{
		get
		{
			if (_actions.Length == 0)
			{
				_actions = KeyBindings.All;
			}
			return _actions;
		}
	}

	public static void Reset()
	{
		_selected = 0;
		_scroll = 0;
		_capturing = false;
		_status = null;
		_statusTime = 0;
		_actions = new KeyBindings.Action[0];
	}

	/// <summary>Runs input for the screen. Returns true when the screen should close.</summary>
	public static bool Update(Terraria.UI ui)
	{
		if (_statusTime > 0 && --_statusTime == 0)
		{
			_status = null;
		}
		KeyBindings.Action[] actions = Actions;
		if (_capturing)
		{
			return UpdateCapture(ui, actions);
		}
		// Keyboard navigation.
		if (KeyboardMouseInput.KeyPressed(Keys.Down))
		{
			_selected = Math.Min(actions.Length - 1, _selected + 1);
			ClampScroll();
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Up))
		{
			_selected = Math.Max(0, _selected - 1);
			ClampScroll();
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Escape) || ui.IsBackButtonTriggered())
		{
			return true;
		}
		// Mouse: hover selects, left click starts capture on that row.
		if (UI.PointerActive)
		{
			int num = RowUnderPointer(ui);
			if (num >= 0)
			{
				_selected = num;
				if (KeyboardMouseInput.LeftClicked)
				{
					_capturing = true;
					_status = "Press a key for " + KeyBindings.Label(actions[_selected]) + " (Esc cancels)";
					_statusTime = 999999;
				}
			}
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Space) || ui.IsButtonTriggered(Buttons.A))
		{
			_capturing = true;
			_status = "Press a key for " + KeyBindings.Label(actions[_selected]) + " (Esc cancels)";
			_statusTime = 999999;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.R))
		{
			KeyBindings.ResetToDefaults();
			_status = "Bindings reset to defaults";
			_statusTime = 240;
		}
		return false;
	}

	private static bool UpdateCapture(Terraria.UI ui, KeyBindings.Action[] actions)
	{
		if (KeyboardMouseInput.KeyPressed(Keys.Escape))
		{
			_capturing = false;
			_status = "Cancelled";
			_statusTime = 120;
			return false;
		}
		// Scan for the first newly pressed key. Enumerating Keys is cheap enough here because
		// this only runs while the player is deliberately binding something.
		foreach (Keys value in Enum.GetValues(typeof(Keys)))
		{
			if (value == Keys.None || !KeyboardMouseInput.KeyPressed(value))
			{
				continue;
			}
			if (KeyBindings.TrySet(actions[_selected], value))
			{
				_status = KeyBindings.Label(actions[_selected]) + " = " + value;
				Persist();
			}
			else
			{
				_status = value + " is already used by another action";
			}
			_statusTime = 180;
			_capturing = false;
			return false;
		}
		return false;
	}

	/// <summary>Writes the new binding to disk immediately; a crash should not lose it.</summary>
	private static void Persist()
	{
		try
		{
			Settings settings = new Settings(Terraria.Storage.AppPaths.SettingsFile);
			KeyBindings.Save(settings);
			settings.Close();
		}
		catch
		{
		}
	}

	private static void ClampScroll()
	{
		if (_selected < _scroll)
		{
			_scroll = _selected;
		}
		else if (_selected >= _scroll + VisibleRows)
		{
			_scroll = _selected - VisibleRows + 1;
		}
	}

	private static int RowUnderPointer(Terraria.UI ui)
	{
		Rectangle area = new Rectangle(0, 0, 1, 1);
		if (!TryGetLayout(ui, out area))
		{
			return -1;
		}
		int num = KeyboardMouseInput.MouseY - area.Y;
		if (num < 0)
		{
			return -1;
		}
		int num2 = num / (int)(RowHeight * Main.ScreenMultiplier);
		if (num2 < 0 || num2 >= Math.Min(VisibleRows, Actions.Length - _scroll))
		{
			return -1;
		}
		if (KeyboardMouseInput.MouseX < area.X || KeyboardMouseInput.MouseX > area.Right)
		{
			return -1;
		}
		return _scroll + num2;
	}

	private static bool TryGetLayout(Terraria.UI ui, out Rectangle rows)
	{
		rows = default;
		if (ui == null || ui.CurrentView == null)
		{
			return false;
		}
		int num = (int)(380f * Main.ScreenMultiplier);
		int num2 = (int)(120f * Main.ScreenMultiplier);
		int num3 = (ui.CurrentView.ViewWidth - num) / 2;
		rows = new Rectangle(num3, num2, num, VisibleRows * (int)(RowHeight * Main.ScreenMultiplier));
		return true;
	}

	/// <summary>Draws the screen. Called from <c>UI.DrawMenu</c>.</summary>
	public static void Draw(Terraria.UI ui)
	{
		KeyBindings.Action[] actions = Actions;
		Rectangle rectangle;
		if (!TryGetLayout(ui, out rectangle))
		{
			return;
		}
		float num = Main.ScreenMultiplier;
		int num2 = rectangle.X;
		int num3 = rectangle.Y;
		int width = rectangle.Width;
		Main.DrawSolidRect(new Rectangle(num2 - (int)(8f * num), num3 - (int)(40f * num), width + (int)(16f * num), rectangle.Height + (int)(86f * num)), new Color(0, 0, 0, 190));
		UI.DrawStringCC(UI.BigFont, "Keyboard bindings", rectangle.Center.X, num3 - (int)(26f * num), Color.White);
		int num4 = Math.Min(VisibleRows, actions.Length - _scroll);
		for (int i = 0; i < num4; i++)
		{
			int num5 = _scroll + i;
			int y = num3 + i * (int)(RowHeight * num);
			bool flag = num5 == _selected;
			if (flag)
			{
				Main.DrawSolidRect(new Rectangle(num2, y, width, (int)(RowHeight * num) - 2), new Color(40, 70, 120, 220));
			}
			string s = KeyBindings.Label(actions[num5]);
			UI.DrawStringLC(UI.BoldSmallFont, s, num2 + (int)(8f * num), y + (int)(8f * num), Color.White);
			string text = KeyBindings.Get(actions[num5]).ToString();
			Color color = KeyBindings.IsDefault(actions[num5]) ? new Color(200, 200, 200) : new Color(120, 230, 140);
			if (flag && _capturing)
			{
				text = "press a key…";
				color = new Color(255, 210, 120);
			}
			Vector2 vector = UI.BoldSmallFont.MeasureString(text);
			UI.DrawStringLC(UI.BoldSmallFont, text, rectangle.Right - (int)vector.X - (int)(8f * num), y + (int)(8f * num), color);
		}
		int num6 = num3 + VisibleRows * (int)(RowHeight * num) + (int)(6f * num);
		string text2 = _status ?? "LMB / Space - rebind   R - reset all   Esc - back";
		UI.DrawStringCC(UI.SmallFont, text2, rectangle.Center.X, num6, new Color(150, 160, 180));
		if (_scroll > 0)
		{
			UI.DrawStringCC(UI.SmallFont, "^ more", rectangle.Center.X, num3 - (int)(6f * num), new Color(150, 160, 180));
		}
		if (_scroll + VisibleRows < actions.Length)
		{
			UI.DrawStringCC(UI.SmallFont, "v more", rectangle.Center.X, num6 + (int)(16f * num), new Color(150, 160, 180));
		}
	}
}
