using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Terraria;

public static class GameMode
{
	private static int CursorY = 2;

	public static void Update()
	{
		if (UI.MainUI.IsBackButtonTriggered())
		{
			UI.MainUI.PrevMenu();
		}
		else if (UI.MainUI.IsButtonTriggered(Buttons.A))
		{
			switch (CursorY)
			{
			case 0:
				UI.MainUI.IsOnline = !UI.MainUI.IsOnline;
				UI.MainUI.SettingsDirty = true;
				break;
			case 1:
				UI.MainUI.IsInviteOnly = !UI.MainUI.IsInviteOnly;
				UI.MainUI.SettingsDirty = true;
				break;
			case 2:
				StartGame();
				break;
			}
		}
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
		bool flag = UI.MainUI.CanPlayOnline();
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
