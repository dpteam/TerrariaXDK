using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Terraria;

public static class GameMode
{
	private static int CursorY = 2;

	// Row geometry captured while drawing, so the pointer maps straight onto the entries.
	private static Rectangle MouseRowsArea;

	private static int MouseRowTop;

	private static int MouseRowHeight = 1;

	public static void Update()
	{
		bool flag = HandleMouse();
		if (UI.MainUI.IsBackButtonTriggered())
		{
			UI.MainUI.PrevMenu();
			return;
		}
		if (flag || UI.MainUI.IsButtonTriggered(Buttons.A))
		{
			Activate(CursorY);
		}
	}

	/// <summary>Runs the action behind a row. Shared by gamepad confirm and mouse click.</summary>
	private static void Activate(int row)
	{
		switch (row)
		{
		case 0:
			if (!UI.MainUI.CanPlayOnline())
			{
				Main.PlaySound(11);
				return;
			}
			UI.MainUI.IsOnline = !UI.MainUI.IsOnline;
			UI.MainUI.SettingsDirty = true;
			Main.PlaySound(12);
			break;
		case 1:
			if (!UI.MainUI.IsOnline)
			{
				Main.PlaySound(11);
				return;
			}
			UI.MainUI.IsInviteOnly = !UI.MainUI.IsInviteOnly;
			UI.MainUI.SettingsDirty = true;
			Main.PlaySound(12);
			break;
		case 2:
			StartGame();
			break;
		}
	}

	/// <summary>
	/// Hover-only: moving the pointer over a row selects it. Clicking is left to the shared
	/// "left mouse = A" path so there is exactly one activation per click; handling the click
	/// here as well used to toggle a checkbox twice (on and immediately back off).
	/// </summary>
	private static bool HandleMouse()
	{
		if (!Terraria.Input.KeyboardMouseInput.Enabled || !Terraria.Input.KeyboardMouseInput.CursorOverride)
		{
			return false;
		}
		Rectangle mouseRowsArea = MouseRowsArea;
		if (mouseRowsArea.Width <= 0)
		{
			return false;
		}
		int num = Terraria.Input.KeyboardMouseInput.MouseY;
		if (!mouseRowsArea.Contains(Terraria.Input.KeyboardMouseInput.MouseX, num))
		{
			return false;
		}
		int num2 = (num - MouseRowTop) / MouseRowHeight;
		if (num2 < 0 || num2 > 2)
		{
			return false;
		}
		// Row 1 only exists while online mode is on.
		if (num2 == 1 && !UI.MainUI.IsOnline)
		{
			return false;
		}
		CursorY = (byte)num2;
		return false;
	}

	public static void UpdateCursor(int dx, int dy)
	{
		if (!UI.MainUI.CanPlayOnline())
		{
			UI.MainUI.IsOnline = false;
			CursorY = 2;
		}
		else
		{
			if (dy == 0)
			{
				return;
			}
			Main.PlaySound(12);
			int num = CursorY;
			do
			{
				num += dy;
				if (num < 0)
				{
					num = 2;
				}
				else if (num > 2)
				{
					num = 0;
				}
			}
			while (num == 1 && !UI.MainUI.IsOnline);
			CursorY = (byte)num;
		}
	}

	public static void Draw(WorldView view)
	{
		Main.DrawRect(451, new Rectangle
		{
			X = (view.ViewWidth >> 1) - (int)(190f * Main.ScreenMultiplier),
			Y = (int)(210f * Main.ScreenMultiplier),
			Width = (int)(380f * Main.ScreenMultiplier),
			Height = (int)(168f * Main.ScreenMultiplier)
		}, 64);
		Color c = Color.White;
		int num = (view.ViewWidth >> 1) - (int)(100f * Main.ScreenMultiplier);
		int num2 = (int)(230f * Main.ScreenMultiplier);
		MouseRowTop = num2;
		MouseRowHeight = (int)(64f * Main.ScreenMultiplier);
		MouseRowsArea = new Rectangle(num - (int)(8f * Main.ScreenMultiplier), num2 - (int)(8f * Main.ScreenMultiplier), (int)(320f * Main.ScreenMultiplier), MouseRowHeight * 3);
		bool flag = UI.MainUI.CanPlayOnline();
		// The selection marker is drawn even with the mouse active: it is the highlight, not a
		// pointer (the pointer is drawn separately and the marker no longer moves it).
		if (CursorY == 0)
		{
			view.Ui.DrawInventoryCursor(num, num2, Main.ScreenMultiplier);
		}
		else
		{
			c = (flag ? Color.White : new Color(128, 128, 128, 255));
			SpriteSheet<_sheetSprites>.DrawScaledTL(451, num, num2, c, Main.ScreenMultiplier);
		}
		if (UI.MainUI.IsOnline)
		{
			SpriteSheet<_sheetSprites>.DrawScaledTL(202, num + (int)(10f * Main.ScreenMultiplier), num2 + (int)(10f * Main.ScreenMultiplier), c, Main.ScreenMultiplier);
		}
		UI.DrawStringLC(UI.SmallFont, Lang.MenuText[6], num + (int)(60f * Main.ScreenMultiplier), num2 + (int)(26f * Main.ScreenMultiplier), c);
		num2 += (int)(64f * Main.ScreenMultiplier);
		if (CursorY == 1)
		{
			view.Ui.DrawInventoryCursor(num, num2, Main.ScreenMultiplier);
		}
		else
		{
			c = ((flag && UI.MainUI.IsOnline) ? Color.White : new Color(128, 128, 128, 255));
			SpriteSheet<_sheetSprites>.DrawScaledTL(451, num, num2, c, Main.ScreenMultiplier);
		}
		if (UI.MainUI.IsInviteOnly)
		{
			SpriteSheet<_sheetSprites>.DrawScaledTL(202, num + (int)(10f * Main.ScreenMultiplier), num2 + (int)(10f * Main.ScreenMultiplier), c, Main.ScreenMultiplier);
		}
		UI.DrawStringLC(UI.SmallFont, Lang.MenuText[7], num + (int)(60f * Main.ScreenMultiplier), num2 + (int)(26f * Main.ScreenMultiplier), c);
		string text = ((WorldSelect.WorldName() != null) ? Lang.MenuText[10] : Lang.MenuText[11]);
		float num3 = 1f;
		if (CursorY != 2)
		{
			c = new Color(240, 240, 240, 240);
		}
		else
		{
			num3 *= 1f + UI.cursorAlpha * 0.1f;
			c = new Color(UI.CursorColour.A, UI.CursorColour.A, 100, 255);
		}
		Vector2 pivot = UI.MeasureString(UI.BigFont, text);
		pivot.X *= 0.5f;
		pivot.Y *= 0.5f;
		float y = 454f;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			y = 620f;
			break;
		case ScreenHeights.FHD:
			y = 940f;
			break;
		}
		UI.DrawStringScaled(pos: new Vector2(view.ViewWidth >> 1, y), font: UI.BigFont, s: text, c: c, pivot: pivot, scale: num3);
	}

	public static void ControlDescription(StringBuilder StringBuilder)
	{
		StringBuilder.Append(Lang.Controls(Lang.CONTROLS.SELECT));
		StringBuilder.Append(' ');
		StringBuilder.Append(Lang.Controls(Lang.CONTROLS.BACK));
	}

	private static void StartGame()
	{
		Main.PlaySound(10);
		string text = WorldSelect.WorldName();
		if (text == null)
		{
			UI.MainUI.SetMenu(MenuMode.WORLD_SIZE);
			return;
		}
		UI.MainUI.SetMenu(MenuMode.STATUS_SCREEN);
		Main.WorldName = text;
		WorldGen.PlayWorld();
	}
}
