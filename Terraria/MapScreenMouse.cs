using System;
using Microsoft.Xna.Framework;
using Terraria.Input;

namespace Terraria;

/// <summary>
/// Mouse support for the fullscreen map (the screen Tab opens).
///
/// The screen was written for a gamepad: a D-pad cursor walks the PvP box, the team dots and
/// the player list, and the map itself pans with the right stick or the arrow buttons. A mouse
/// could move the drawn pointer over it and nothing else happened, which is why "nothing on the
/// map screen is clickable".
///
/// Two separate jobs, and they must not fight each other:
/// <list type="bullet">
/// <item>widgets (PvP, team, player rows) - a click acts on the one under the pointer, using the
/// rectangles recorded while drawing;</item>
/// <item>the map - dragging pans it and the wheel zooms, which is what the stick and the
/// triggers do for a controller.</item>
/// </list>
///
/// Dragging is deliberately restricted to a press that started on the map: otherwise the drag
/// that selects a team dot would also scroll the map underneath it.
/// </summary>
internal static class MapScreenMouse
{
	/// <summary>Team dot index to the console cursor position, in the order the screen draws them.</summary>
	private static readonly byte[] DotCursorX = new byte[5] { 1, 2, 3, 2, 3 };

	private static readonly byte[] DotCursorY = new byte[5] { 0, 0, 0, 1, 1 };

	/// <summary>
	/// Console cursor position for a team dot. Pure, because the drawing loop, the cursor
	/// movement and this file all have to agree on the order of the five dots - and they did
	/// not: the dot the screen draws first is not the team the cursor computes first.
	/// </summary>
	internal static void CursorForDot(int Dot, out int X, out int Y)
	{
		if (Dot < 0 || Dot >= DotCursorX.Length)
		{
			Dot = 0;
		}
		X = DotCursorX[Dot];
		Y = DotCursorY[Dot];
	}

	/// <summary>
	/// The team a console cursor selects. This mirrors <c>UI.PositionMapScreenCursor</c>, so a
	/// test can prove the round trip: cursor of dot i selects team i.
	/// </summary>
	internal static int TeamForCursor(int X, int Y)
	{
		if (Y == 0)
		{
			return X - 1;
		}
		return (X != 1) ? (X + 1) : 0;
	}

	private static readonly Rectangle[] TeamRects = new Rectangle[5];

	private static readonly bool[] TeamValid = new bool[5];

	private static Rectangle PlayerRowArea;

	private static bool PlayerRowsValid;

	/// <summary>The PvP checkbox, recorded while drawing.</summary>
	private static Rectangle PvPRect;

	private static bool PvPValid;

	/// <summary>True while a drag that began on the map is still held.</summary>
	private static bool _dragging;

	private static int _dragX;

	private static int _dragY;

	/// <summary>
	/// Records the widget rectangles for this frame. Called from the draw code, so the click
	/// targets are the pictures the player sees rather than numbers guessed a second time.
	/// </summary>
	internal static void RecordWidgets(Rectangle PvP, Rectangle[] Teams, Rectangle PlayerRows)
	{
		PvPRect = PvP;
		PvPValid = PvP.Width > 0 && PvP.Height > 0;
		for (int i = 0; i < TeamRects.Length; i++)
		{
			if (Teams != null && i < Teams.Length && Teams[i].Width > 0 && Teams[i].Height > 0)
			{
				TeamRects[i] = Teams[i];
				TeamValid[i] = true;
			}
			else
			{
				TeamValid[i] = false;
			}
		}
		PlayerRowArea = PlayerRows;
		PlayerRowsValid = PlayerRows.Width > 0 && PlayerRows.Height > 0;
	}

	/// <summary>
	/// Applies one frame of pointer input to the map screen. Returns true when the pointer was
	/// over the map or one of its widgets, so the caller can skip stick-driven movement for that
	/// frame (a mouse and a resting stick must not both move the same cursor).
	/// </summary>
	internal static bool Update(UI Ui)
	{
		if (!KeyboardMouseInput.Enabled || !KeyboardMouseInput.CursorOverride || Ui == null || Ui.CurrentView == null)
		{
			_dragging = false;
			return false;
		}
		MiniMap miniMap = Ui.miniMap;
		Rectangle rectangle = miniMap.DestinationRect(Ui.CurrentView);
		int num = KeyboardMouseInput.MouseX;
		int num2 = KeyboardMouseInput.MouseY;
		// Widgets first: they sit above the map, and a click on one must not pan the map.
		if (PvPValid && PvPRect.Contains(num, num2))
		{
			Ui.SetMapCursor(0, 0);
			if (KeyboardMouseInput.LeftClicked)
			{
				Main.PlaySound(10);
				Ui.pvpCooldown = 180;
				Ui.pvpSelected = !Ui.pvpSelected;
			}
			return true;
		}
		int num3;
		if (TryTeamUnderPointer(num, num2, out num3))
		{
			int x;
			int y;
			CursorForDot(num3, out x, out y);
			Ui.SetMapCursor(x, y);
			if (KeyboardMouseInput.LeftClicked)
			{
				Main.PlaySound(10);
				Ui.teamCooldown = 180;
				// The dot index is the team number, which is also what the console path ends up
				// computing from the cursor: one mapping, not two.
				Ui.teamSelected = (byte)num3;
			}
			return true;
		}
		if (PlayerRowsValid && PlayerRowArea.Contains(num, num2))
		{
			int num4 = (num2 - PlayerRowArea.Y) / Math.Max(1, PlayerRowArea.Height / 8);
			if (num4 < 0)
			{
				num4 = 0;
			}
			else if (num4 > 7)
			{
				num4 = 7;
			}
			Ui.SetMapCursor(0, num4 + 2);
			if (KeyboardMouseInput.LeftClicked)
			{
				// A row is a player, not a toggle: opening their card is the only action the
				// console build offers here.
				Ui.OpenMapGamerCard(num4);
			}
			return true;
		}
		if (!rectangle.Contains(num, num2))
		{
			_dragging = false;
			return false;
		}
		// Wheel zooms, like the triggers do. Consumed here so the hotbar (which reads the same
		// counter) cannot act on it while the map is open. Two steps per notch: one notch is a
		// noticeable zoom, and 5% (one step) is not.
		int num5 = KeyboardMouseInput.ConsumeWheelSteps();
		if (num5 != 0)
		{
			miniMap.Scale(num5 * 2, Ui);
		}
		// Drag to pan. The anchor is taken on the press and kept until release, so a drag that
		// leaves the map rectangle keeps working instead of stopping at the edge.
		if (KeyboardMouseInput.LeftPressed)
		{
			if (!_dragging)
			{
				_dragging = true;
				_dragX = num;
				_dragY = num2;
			}
			int num6 = num - _dragX;
			int num7 = num2 - _dragY;
			if (num6 != 0 || num7 != 0)
			{
				// The map moves with the hand: dragging right shows what is to the left.
				miniMap.Pan(-num6, -num7, Ui);
				_dragX = num;
				_dragY = num2;
			}
		}
		else
		{
			_dragging = false;
		}
		return true;
	}

	/// <summary>Team dot under the pointer, or false. The index is the team number.</summary>
	private static bool TryTeamUnderPointer(int X, int Y, out int Dot)
	{
		for (int i = 0; i < TeamRects.Length; i++)
		{
			if (TeamValid[i] && TeamRects[i].Contains(X, Y))
			{
				Dot = i;
				return true;
			}
		}
		Dot = 0;
		return false;
	}

	/// <summary>Test hook: forgets a drag in progress.</summary>
	internal static void ResetForTests()
	{
		_dragging = false;
		_dragX = 0;
		_dragY = 0;
	}
}
