using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria.Input;

namespace Terraria.Cheats;

/// <summary>
/// Single-player cheat menu, modelled after the command console from the old PC builds
/// (refs/T1061C ModClass) but driven through the console-style UI instead of chat commands.
///
/// The screen draws itself, like Display / HUD / Network / Mods do. It used to be published
/// through the generic menu renderer, which draws a column of centred words with fixed
/// spacing; the cheat list is eighteen sentences and it needs a panel, a highlight and a
/// layout that fits the screen it is on. That is why the two complaints about it - "wrong
/// font" and "the list runs off the edge" - could not be fixed one row at a time.
///
/// Row height is computed, not constant: <see cref="PreferredRowHeight"/> when there is room
/// for the whole list, smaller when there is not, and scrolling once even that would make the
/// text touch itself.
/// </summary>
internal static class CheatMenu
{
	public const int ActionCount = 18;

	/// <summary>Row height with room to spare, in unscaled pixels (the settings screens use 44).</summary>
	private const int PreferredRowHeight = 40;

	/// <summary>Below this the rows would touch, so the list scrolls instead of shrinking.</summary>
	private const int MinRowHeight = 26;

	/// <summary>Panel width, in unscaled pixels.</summary>
	private const int PanelWidth = 560;

	private static int _selected;

	private static int _scroll;

	private static string _status;

	private static int _statusTime;

	/// <summary>Frames to wait before the held stick repeats. Zero means "not held".</summary>
	private static int _stickDelay;

	public static void Reset()
	{
		_selected = 0;
		_scroll = 0;
		_status = null;
		_statusTime = 0;
		_stickDelay = 0;
	}

	/// <summary>Returns true when the screen should close.</summary>
	public static bool Update(UI ui)
	{
		if (_statusTime > 0 && --_statusTime == 0)
		{
			_status = null;
		}
		int num = VisibleRows(out var _, out var _);
		// Both devices move the same cursor. A menu that answers only to the keyboard would
		// leave a controller player with no way to use a screen they can still open with F1.
		bool flag = false;
		if (KeyboardMouseInput.KeyPressed(Keys.Down) || ui.IsButtonTriggered(Buttons.DPadDown))
		{
			Move(1, num);
			flag = true;
			_stickDelay = 12;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Up) || ui.IsButtonTriggered(Buttons.DPadUp))
		{
			Move(-1, num);
			flag = true;
			_stickDelay = 12;
		}
		// The left stick is analog, so it needs a repeat: one step, then one every eight frames
		// while it is held. Without this a controller player had only the d-pad on a list of
		// eighteen.
		float y = ui.PadState.ThumbSticks.Left.Y;
		if (y < -0.5f || y > 0.5f)
		{
			if (_stickDelay <= 0)
			{
				Move((y < -0.5f) ? 1 : (-1), num);
				flag = true;
				_stickDelay = 8;
			}
			else
			{
				_stickDelay--;
			}
		}
		else
		{
			_stickDelay = 0;
		}
		// The wheel is drained here even when the list fits, so the steps do not pile up and
		// move the hotbar the moment the screen closes.
		int num3 = KeyboardMouseInput.ConsumeWheelSteps();
		if (num3 != 0)
		{
			Move((num3 > 0) ? (-1) : 1, num);
			flag = true;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Escape) || ui.IsButtonTriggered(Buttons.B) || ui.IsButtonTriggered(Buttons.Back))
		{
			return true;
		}
		int num2 = RowUnderPointer(out var _);
		// The pointer follows the highlight only when it is what moved. Otherwise a wheel notch
		// under a resting pointer was undone on the same frame: Move picked the next row, then
		// the pointer put the selection back on the row it was sitting on.
		if (num2 >= 0 && !flag)
		{
			_selected = num2;
		}
		// A click is the pointer's own action; Buttons.A is the controller's. The input layer
		// is told this screen handles the mouse (see KeyboardMouseInput.RefreshContext), so a
		// click cannot also arrive as A and fire twice.
		if (num2 >= 0 && KeyboardMouseInput.LeftClicked)
		{
			Activate(num2);
		}
		else if (ui.IsButtonTriggered(Buttons.A) && _selected >= 0 && _selected < ActionCount)
		{
			Activate(_selected);
		}
		return false;
	}

	private static void Move(int direction, int visibleRows)
	{
		_selected += direction;
		if (_selected < 0)
		{
			_selected = ActionCount - 1;
		}
		else if (_selected >= ActionCount)
		{
			_selected = 0;
		}
		ClampScroll(visibleRows);
		Main.PlaySound(12);
	}

	private static void ClampScroll(int visibleRows)
	{
		if (visibleRows < 1)
		{
			visibleRows = 1;
		}
		if (_selected < _scroll)
		{
			_scroll = _selected;
		}
		else if (_selected >= _scroll + visibleRows)
		{
			_scroll = _selected - visibleRows + 1;
		}
		int num = Math.Max(0, ActionCount - visibleRows);
		if (_scroll > num)
		{
			_scroll = num;
		}
		if (_scroll < 0)
		{
			_scroll = 0;
		}
	}

	/// <summary>Every action, in order. There is no page row: the whole list fits.</summary>
	public static string[] BuildRows()
	{
		string[] array = new string[ActionCount];
		for (int i = 0; i < ActionCount; i++)
		{
			array[i] = Label(i);
		}
		return array;
	}

	private static string Label(int action)
	{
		switch (action)
		{
		case 0:
			return Texts.Format("ui.cheats.god", OnOff(CheatState.GodMode));
		case 1:
			return Texts.Format("ui.cheats.noclip", OnOff(CheatState.NoClip));
		case 2:
			return Texts.Format("ui.cheats.mana", OnOff(CheatState.InfiniteMana));
		case 3:
			return Texts.Format("ui.cheats.buffs", OnOff(CheatState.InfiniteBuffs));
		case 4:
			return Texts.Format("ui.cheats.fall", OnOff(CheatState.NoFallDamage));
		case 5:
			return Texts.Get("ui.cheats.heal");
		case 6:
			return Texts.Get("ui.cheats.caps");
		case 7:
			return Texts.Get("ui.cheats.dawn");
		case 8:
			return Texts.Get("ui.cheats.noon");
		case 9:
			return Texts.Get("ui.cheats.dusk");
		case 10:
			return Texts.Get("ui.cheats.midnight");
		case 11:
			return Texts.Format("ui.cheats.bloodmoon", OnOff(Main.GameTime.IsBloodMoon));
		case 12:
			return Texts.Get("ui.cheats.eye");
		case 13:
			return Texts.Get("ui.cheats.skeletron");
		case 14:
			return Texts.Get("ui.cheats.killall");
		case 15:
			return Texts.Get("ui.cheats.meteor");
		case 16:
			return Texts.Format("ui.cheats.recipes", OnOff(Main.UnlockAllRecipes));
		case 17:
			return Texts.Get("ui.cheats.save");
		default:
			return "?";
		}
	}

	private static string OnOff(bool value)
	{
		return Texts.Get(value ? "ui.common.on.upper" : "ui.common.off.upper");
	}

	/// <summary>Runs the action behind a row.</summary>
	public static void Activate(int row)
	{
		if (row < 0 || row >= ActionCount)
		{
			return;
		}
		if (!CheatState.IsAvailable)
		{
			Main.NewText(Texts.Get("ui.cheats.single.only"), 255, 120, 120);
			return;
		}
		Player activePlayer = UI.MainUI?.ActivePlayer;
		if (activePlayer == null)
		{
			return;
		}
		switch (row)
		{
		case 0:
			CheatState.GodMode = !CheatState.GodMode;
			break;
		case 1:
			CheatState.NoClip = !CheatState.NoClip;
			break;
		case 2:
			CheatState.InfiniteMana = !CheatState.InfiniteMana;
			break;
		case 3:
			CheatState.InfiniteBuffs = !CheatState.InfiniteBuffs;
			break;
		case 4:
			CheatState.NoFallDamage = !CheatState.NoFallDamage;
			break;
		case 5:
			activePlayer.statLife = activePlayer.StatLifeMax;
			activePlayer.statMana = activePlayer.statManaMax2;
			break;
		case 6:
			activePlayer.StatLifeMax = 500;
			activePlayer.statManaMax = 200;
			activePlayer.statManaMax2 = 200;
			activePlayer.statLife = 500;
			activePlayer.statMana = 200;
			break;
		case 7:
			SetTime(day: true, 0f);
			break;
		case 8:
			SetTime(day: true, 27000f);
			break;
		case 9:
			SetTime(day: true, 45900f);
			break;
		case 10:
			SetTime(day: false, 0f);
			break;
		case 11:
			Main.GameTime.IsBloodMoon = !Main.GameTime.IsBloodMoon;
			break;
		case 12:
			NPC.SpawnOnPlayer(activePlayer, 4);
			break;
		case 13:
			NPC.SpawnOnPlayer(activePlayer, 35);
			break;
		case 14:
			KillAllEnemies();
			break;
		case 15:
			WorldGen.ToSpawnMeteor = true;
			break;
		case 16:
			Main.UnlockAllRecipes = !Main.UnlockAllRecipes;
			break;
		case 17:
			WorldGen.saveAllWhilePlaying();
			break;
		default:
			return;
		}
		Main.PlaySound(12);
		string text = Label(row);
		Main.NewText(Texts.Format("ui.cheats.status", text), 50, 255, 130);
		// Same sentence in the panel's own line: the chat line scrolls away, and a toggle that
		// is already ON gives no feedback otherwise.
		_status = text;
		_statusTime = 240;
	}

	private static void SetTime(bool day, float worldTime)
	{
		Main.GameTime.DayTime = day;
		Main.GameTime.WorldTime = worldTime;
	}

	private static void KillAllEnemies()
	{
		int num = 0;
		for (int i = Main.NPCSet.Length - 1; i >= 0; i--)
		{
			NPC nPC = Main.NPCSet[i];
			if (nPC != null && nPC.Active != 0 && !nPC.IsTownNPC && nPC.Life > 0)
			{
				nPC.Life = 0;
				nPC.Active = 0;
				num++;
			}
		}
		Main.NewText(Texts.Format("ui.cheats.removed", num), 255, 120, 120);
	}

	/// <summary>
	/// Row height and how many rows fit in <paramref name="available"/> pixels.
	///
	/// The screen used to place fourteen rows at a constant 38 px from a constant top, which
	/// put the bottom of the panel 42 px below the last line of a 720-pixel frame: the list
	/// ran off the edge no matter what the resolution was. The height is now an output.
	///
	/// Pure, so the self-test can drive it without a window: the invariant is
	/// <c>Visible * RowHeight &lt;= available</c>, and the whole list is visible whenever it
	/// fits at <paramref name="minimum"/> or more.
	/// </summary>
	internal static void FitRows(int available, int preferred, int minimum, int count, out int RowHeight, out int Visible)
	{
		RowHeight = Math.Max(1, minimum);
		Visible = 0;
		if (count <= 0 || available <= 0)
		{
			return;
		}
		int num = Math.Min(Math.Max(1, preferred), available / count);
		if (num >= RowHeight)
		{
			// The whole list fits at a readable size, so nothing scrolls.
			RowHeight = num;
			Visible = count;
			return;
		}
		// It does not: keep the text legible and scroll instead of shrinking the rows until
		// they touch. This is the case the old constant spacing could never express.
		Visible = Math.Max(1, available / RowHeight);
		if (Visible > count)
		{
			Visible = count;
		}
	}

	/// <summary>
	/// Panel, rows and row height for the current screen. Pure geometry from the resolution,
	/// so the pointer hit-test, the scroll clamp and the drawing all read the same numbers.
	/// </summary>
	private static bool TryGetLayout(out Rectangle Panel, out Rectangle Rows, out int RowHeight, out int Visible)
	{
		Panel = default;
		Rows = default;
		RowHeight = 0;
		Visible = 0;
		UI ui = UI.MainUI;
		if (ui == null || ui.CurrentView == null)
		{
			return false;
		}
		float num = (Main.ScreenMultiplier <= 0f) ? 1f : Main.ScreenMultiplier;
		int num2 = ui.CurrentView.ViewWidth;
		int num3 = (int)((float)PanelWidth * num);
		if (num3 > num2 - (int)(20f * num))
		{
			num3 = num2 - (int)(20f * num);
		}
		if (num3 < (int)(120f * num))
		{
			return false;
		}
		int num4 = (int)(30f * num);
		int num5 = (int)(38f * num);
		// Two lines of footer: the hint and the single-player note. They used to be drawn one
		// above the panel and one below it, and the lower one was clipped by the frame.
		int num6 = (int)(46f * num);
		int num7 = Main.ResolutionHeight - (int)(12f * num);
		int num8 = num7 - num4 - num5 - num6;
		if (num8 < 1)
		{
			// Not even one row: better to draw nothing than a panel with the text outside it.
			return false;
		}
		int num11;
		int num12;
		FitRows(num8, (int)((float)PreferredRowHeight * num), (int)((float)MinRowHeight * num), ActionCount, out num11, out num12);
		if (num11 < 1 || num12 < 1)
		{
			return false;
		}
		int num13 = num5 + num12 * num11 + num6;
		Panel = new Rectangle((num2 - num3) / 2, num4, num3, num13);
		Rows = new Rectangle(Panel.X, Panel.Y + num5, Panel.Width, num12 * num11);
		RowHeight = num11;
		Visible = num12;
		return true;
	}

	private static int VisibleRows(out Rectangle Panel, out int RowHeight)
	{
		if (!TryGetLayout(out Panel, out var _, out RowHeight, out var visible))
		{
			return 0;
		}
		return visible;
	}

	/// <summary>Which action the pointer is over, or -1.</summary>
	private static int RowUnderPointer(out Rectangle Panel)
	{
		Panel = default;
		if (!TryGetLayout(out Panel, out var rows, out var rowHeight, out var visible))
		{
			return -1;
		}
		if (!KeyboardMouseInput.Enabled || !KeyboardMouseInput.CursorOverride)
		{
			return -1;
		}
		int num = KeyboardMouseInput.MouseX;
		int num2 = KeyboardMouseInput.MouseY;
		if (num < rows.X || num > rows.Right)
		{
			return -1;
		}
		int num3 = num2 - rows.Y;
		if (num3 < 0)
		{
			return -1;
		}
		int num4 = num3 / rowHeight;
		if (num4 < 0 || num4 >= visible)
		{
			return -1;
		}
		int num5 = _scroll + num4;
		if (num5 < 0 || num5 >= ActionCount)
		{
			return -1;
		}
		return num5;
	}

	public static void Draw()
	{
		if (!TryGetLayout(out var panel, out var rows, out var rowHeight, out var visible))
		{
			return;
		}
		float num = (Main.ScreenMultiplier <= 0f) ? 1f : Main.ScreenMultiplier;
		string[] array = BuildRows();
		Main.DrawSolidRect(new Rectangle(0, 0, Main.ResolutionWidth, Main.ResolutionHeight), new Color(0, 0, 0, 170));
		Main.DrawSolidRect(new Rectangle(panel.X - (int)(10f * num), panel.Y - (int)(8f * num), panel.Width + (int)(20f * num), panel.Height + (int)(16f * num)), new Color(14, 18, 26, 240));
		Main.DrawSolidRect(new Rectangle(panel.X - (int)(10f * num), panel.Y - (int)(8f * num), panel.Width + (int)(20f * num), 2), new Color(70, 110, 170));
		UI.DrawStringCC(UI.BigFont, Texts.Get("ui.cheats.title"), panel.Center.X, panel.Y + (int)(14f * num), Color.White);
		for (int i = 0; i < visible; i++)
		{
			int num2 = _scroll + i;
			if (num2 >= array.Length)
			{
				break;
			}
			int y = rows.Y + i * rowHeight;
			if (num2 == _selected)
			{
				Main.DrawSolidRect(new Rectangle(rows.X, y, rows.Width, rowHeight - 2), new Color(40, 70, 120, 220));
			}
			Color color = ((num2 == _selected) ? Color.White : new Color(230, 230, 230));
			UI.DrawStringLC(UI.BoldSmallFont, array[num2], rows.X + (int)(10f * num), y + rowHeight / 2, color);
		}
		// A scroll hint, not decoration: with the rows clipped there is no other way to know
		// that more of them exist.
		string text = ((_status != null) ? _status : ((visible < array.Length) ? Texts.Get("ui.cheats.more") : Texts.Get("ui.cheats.hint")));
		UI.DrawStringCC(UI.SmallFont, text, panel.Center.X, panel.Bottom - (int)(30f * num), new Color(150, 160, 180));
		// Say out loud that this screen is single-player only: it is unreachable in multiplayer,
		// and the note stops the "why is the cheat menu missing" question. It lives inside the
		// panel: drawn below it, the last line of a 720-pixel frame cut it in half.
		UI.DrawStringCC(UI.SmallFont, Texts.Get("ui.cheats.single.only"), panel.Center.X, panel.Bottom - (int)(13f * num), new Color(120, 130, 150));
	}
}
