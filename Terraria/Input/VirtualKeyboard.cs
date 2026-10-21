using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Terraria.Input;

/// <summary>
/// On-screen keyboard for gamepad-only setups (handhelds, consoles). The console build relied
/// on the platform's system keyboard, which does not exist in this port, so every text field
/// was simply unreachable without a real keyboard.
///
/// Layout: letters, digits and the symbols used by commands and addresses, plus a caps toggle,
/// backspace, space and confirm. Navigation is D-Pad / left stick, A selects, B deletes,
/// X toggles case, Y switches to the symbol page, Start confirms and Back cancels.
/// </summary>
internal static class VirtualKeyboard
{
	private const int Columns = 10;

	private const int CellWidth = 44;

	private const int CellHeight = 34;

	private static readonly string[] LettersPage = new string[4]
	{
		"1234567890",
		"qwertyuiop",
		"asdfghjkl.",
		"zxcvbnm-_"
	};

	private static readonly string[] SymbolsPage = new string[4]
	{
		"!@#$%^&*()",
		"[]{}+=:;/'",
		"\"<>?\\|~`,",
		"0123456789"
	};

	private static readonly StringBuilder Buffer = new StringBuilder(200);

	private static bool _open;

	private static bool _symbols;

	private static bool _shift;

	private static int _row;

	private static int _col;

	private static Action<string> _onAccept;

	private static Action _onCancel;

	private static string _title = "";

	private static int _repeat;

	private static bool _prevA;

	private static bool _prevB;

	private static bool _prevX;

	private static bool _prevY;

	private static bool _prevStart;

	private static bool _prevBack;

	/// <summary>
	/// True while this keyboard has asked FNA to report typed characters. Tracked separately
	/// from <see cref="IsOpen"/> so the handler can be detached exactly once.
	/// </summary>
	private static bool _textInputHooked;

	public static bool IsOpen => _open;

	public static string Text => Buffer.ToString();

	/// <summary>Appends a character, honouring the 200 character limit.</summary>
	internal static void TypeChar(char c)
	{
		if (Buffer.Length < 200)
		{
			Buffer.Append(c);
		}
	}

	/// <summary>Removes the last character, if any.</summary>
	internal static void DeleteLast()
	{
		if (Buffer.Length > 0)
		{
			Buffer.Length--;
		}
	}

	/// <summary>Sets the buffer contents (used by Open and by the self-test).</summary>
	internal static void SetText(string text)
	{
		Buffer.Length = 0;
		if (!string.IsNullOrEmpty(text))
		{
			Buffer.Append(text);
		}
	}

	/// <summary>
	/// Opens the keyboard. <paramref name="onAccept"/> receives the typed text when the player
	/// confirms. <paramref name="onCancel"/> runs when the player backs out, so a caller that
	/// replaced a default value with "ask the player" can restore or drop it.
	/// </summary>
	public static void Open(string title, string initial, Action<string> onAccept, Action onCancel = null)
	{
		_open = true;
		_symbols = false;
		_shift = false;
		_row = 1;
		_col = 0;
		_repeat = 0;
		_title = title ?? "";
		SetText(initial);
		_onAccept = onAccept;
		_onCancel = onCancel;
		KeyboardMouseInput.TextInputActive = true;
		// The on-screen grid is for a gamepad. With a keyboard attached the player should be able
		// to just type: the field was unreachable ("the keyboard opens but I cannot type into
		// it") because TextInputActive suppresses the synthetic pad and nothing else read keys.
		// FNA reports real characters, so the OS layout and dead keys work.
		HookTextInput();
		// Take the current pad state as "already down" so the frame that opens the keyboard
		// does not immediately register a press on whatever button opened it.
		_prevA = true;
		_prevB = true;
		_prevX = true;
		_prevY = true;
		_prevStart = true;
		_prevBack = true;
	}

	public static void Close()
	{
		_open = false;
		_onAccept = null;
		_onCancel = null;
		KeyboardMouseInput.TextInputActive = false;
		UnhookTextInput();
		// The key that closed the keyboard must not also reach the game.
		KeyboardMouseInput.SuppressGameInput = true;
	}

	/// <summary>
	/// Starts receiving typed characters. Idempotent: opening twice must not double every letter,
	/// which is exactly the bug chat had before it centralised on <see cref="TextInputEXT"/>.
	/// </summary>
	private static void HookTextInput()
	{
		if (_textInputHooked)
		{
			return;
		}
		_textInputHooked = true;
		TextInputEXT.TextInput += OnTextInput;
		try
		{
			TextInputEXT.StartTextInput();
		}
		catch
		{
			// No window yet (headless test, shutdown): the grid still works with the pad.
		}
	}

	private static void UnhookTextInput()
	{
		if (!_textInputHooked)
		{
			return;
		}
		_textInputHooked = false;
		TextInputEXT.TextInput -= OnTextInput;
		try
		{
			TextInputEXT.StopTextInput();
		}
		catch
		{
		}
	}

	/// <summary>
	/// Printable characters, backspace and delete. Enter and Escape are deliberately ignored here:
	/// they are handled by the key loop in <see cref="Update"/>, which also knows whether the
	/// keyboard is confirming or cancelling, so handling them twice would submit twice.
	/// </summary>
	private static void OnTextInput(char c)
	{
		if (!_open)
		{
			return;
		}
		switch (c)
		{
		case '\b':
		case '\u007f':
			DeleteLast();
			return;
		case '\r':
		case '\n':
		case '\t':
			return;
		}
		if (c < ' ')
		{
			return;
		}
		TypeChar(c);
	}

	/// <summary>Closes the keyboard and reports the cancellation to whoever opened it.</summary>
	private static void Cancel()
	{
		Action onCancel = _onCancel;
		// Cleared before the callback so it cannot re-enter the keyboard from inside Close().
		_onCancel = null;
		_onAccept = null;
		_open = false;
		KeyboardMouseInput.TextInputActive = false;
		UnhookTextInput();
		KeyboardMouseInput.SuppressGameInput = true;
		if (onCancel != null)
		{
			try
			{
				onCancel();
			}
			catch (Exception ex)
			{
				Program.LogWarn("VirtualKeyboard", "cancel handler threw: " + ex.Message);
			}
		}
	}

	/// <summary>
	/// Selects the cell under the pointer and presses it, as A would. Returns false when the
	/// click missed the grid, so the caller can ignore it rather than acting on a stale cell.
	/// </summary>
	internal static bool ClickCell(int x, int y)
	{
		if (!_open)
		{
			return false;
		}
		string[] page = CurrentPage();
		for (int i = 0; i < page.Length; i++)
		{
			for (int j = 0; j < page[i].Length; j++)
			{
				if (CellRect(j, i).Contains(x, y))
				{
					_row = i;
					_col = j;
					ActivateCurrentCell();
					return true;
				}
			}
		}
		int num = page.Length;
		// Reverse order: later cells are drawn on top, so they win the hit-test.
		if (CellRect(5, num).Contains(x, y))
		{
			_row = num;
			_col = 5;
		}
		else if (CellRect(4, num).Contains(x, y))
		{
			_row = num;
			_col = 4;
		}
		else if (CellRect(3, num).Contains(x, y))
		{
			_row = num;
			_col = 3;
		}
		else if (CellRect(2, num).Contains(x, y))
		{
			_row = num;
			_col = 2;
		}
		else if (CellRect(0, num, wide: true).Contains(x, y))
		{
			_row = num;
			_col = 0;
		}
		else
		{
			return false;
		}
		ActivateCurrentCell();
		return true;
	}

	/// <summary>
	/// Presses the cell the cursor is on. Extracted so the pad, the mouse and the tests all run
	/// the same code: two copies of this switch is how the bottom row ended up with `del`
	/// overlapping `space`.
	/// </summary>
	private static void ActivateCurrentCell()
	{
		string[] page = CurrentPage();
		if (_row >= 0 && _row < page.Length)
		{
			if (_col < 0 || _col >= page[_row].Length)
			{
				return;
			}
			char c = page[_row][_col];
			if (_shift)
			{
				c = char.ToUpperInvariant(c);
			}
			if (Buffer.Length < 200)
			{
				Buffer.Append(c);
			}
			Main.PlaySound(12);
			return;
		}
		if (_row != page.Length)
		{
			return;
		}
		switch (_col)
		{
		case 0:
			if (Buffer.Length < 200)
			{
				Buffer.Append(' ');
			}
			Main.PlaySound(12);
			break;
		case 2:
			if (Buffer.Length > 0)
			{
				Buffer.Length--;
			}
			Main.PlaySound(12);
			break;
		case 3:
			_shift = !_shift;
			Main.PlaySound(12);
			break;
		case 4:
			Accept();
			break;
		case 5:
			Cancel();
			break;
		}
	}

	/// <summary>Runs one frame of the keyboard. Called from <c>Main.Update</c>.</summary>
	public static void Update()
	{
		if (!_open)
		{
			return;
		}
		UI ui = UI.MainUI;
		if (ui == null)
		{
			Close();
			return;
		}
		GamePadState padState = ui.PadState;
		bool flag = padState.IsButtonDown(Buttons.A);
		bool flag2 = padState.IsButtonDown(Buttons.B);
		bool flag3 = padState.IsButtonDown(Buttons.X);
		bool flag4 = padState.IsButtonDown(Buttons.Y);
		bool flag5 = padState.IsButtonDown(Buttons.Start);
		bool flag6 = padState.IsButtonDown(Buttons.Back);

		// Edge detection on the raw buttons: the grid must react once per press.
		bool flag7 = flag && !_prevA;
		bool flag8 = flag2 && !_prevB;
		bool flag9 = flag3 && !_prevX;
		bool flag10 = flag4 && !_prevY;
		bool flag11 = flag5 && !_prevStart;
		bool flag12 = flag6 && !_prevBack;
		_prevA = flag;
		_prevB = flag2;
		_prevX = flag3;
		_prevY = flag4;
		_prevStart = flag5;
		_prevBack = flag6;

		if (flag12)
		{
			// Back means "I changed my mind": the caller may need to undo whatever it did
			// before asking (for example, a name it was about to commit).
			Cancel();
			return;
		}
		// Keyboard: Enter confirms, Escape cancels, and a click on a key presses it. Handled
		// before the pad so a keyboard player is not stuck with a grid they cannot reach.
		if (KeyboardMouseInput.Enabled)
		{
			if (KeyboardMouseInput.KeyPressed(Keys.Enter))
			{
				Accept();
				return;
			}
			if (KeyboardMouseInput.KeyPressed(Keys.Escape))
			{
				Cancel();
				return;
			}
			if (KeyboardMouseInput.LeftClicked)
			{
				ClickCell(KeyboardMouseInput.MouseX, KeyboardMouseInput.MouseY);
				return;
			}
		}
		if (flag11)
		{
			Accept();
			return;
		}
		if (flag9)
		{
			// X deletes a character, matching the "backspace" affordance of console keyboards.
			if (Buffer.Length > 0)
			{
				Buffer.Length--;
			}
			Main.PlaySound(12);
		}
		if (flag10)
		{
			_symbols = !_symbols;
			Main.PlaySound(12);
		}

		// Navigation with repeat, like the rest of the UI.
		int num = 0;
		int num2 = 0;
		if (ui.IsDownButtonDown())
		{
			num2 = 1;
		}
		else if (ui.IsUpButtonDown())
		{
			num2 = -1;
		}
		if (ui.IsRightButtonDown())
		{
			num = 1;
		}
		else if (ui.IsLeftButtonDown())
		{
			num = -1;
		}
		if (num != 0 || num2 != 0)
		{
			if (_repeat == 0)
			{
				Move(num, num2);
				_repeat = 9;
			}
			else
			{
				_repeat--;
			}
		}
		else
		{
			_repeat = 0;
		}

		if (flag7)
		{
			// One implementation for pad and mouse; the accept/cancel cases may close the
			// keyboard, which is why this is a call rather than inline code.
			ActivateCurrentCell();
		}
		if (flag8 && Buffer.Length > 0)
		{
			Buffer.Length--;
			Main.PlaySound(12);
		}
	}

	private static void Accept()
	{
		string result = Buffer.ToString();
		Action<string> onAccept = _onAccept;
		Close();
		onAccept?.Invoke(result);
	}

	/// <summary>
	/// Confirms the current text, as Start or the on-screen "accept" cell would. Returns
	/// false when nothing was listening, which is what the headless tests assert on.
	/// </summary>
	internal static bool Commit()
	{
		if (!_open || _onAccept == null)
		{
			return false;
		}
		Accept();
		return true;
	}

	/// <summary>
	/// Cancels, as Back would, running the caller's cancel callback. Used by the headless
	/// tests: the button path is covered by the input mapping checks.
	/// </summary>
	internal static bool CancelEXT()
	{
		if (!_open)
		{
			return false;
		}
		Cancel();
		return true;
	}

	private static string[] CurrentPage()
	{
		return _symbols ? SymbolsPage : LettersPage;
	}

	private static void Move(int dx, int dy)
	{
		string[] page = CurrentPage();
		_row += dy;
		if (_row < 0)
		{
			_row = page.Length;
		}
		else if (_row > page.Length)
		{
			_row = 0;
		}
		if (_row < page.Length)
		{
			int length = page[_row].Length;
			_col += dx;
			if (_col < 0)
			{
				_col = length - 1;
			}
			else if (_col >= length)
			{
				_col = 0;
			}
		}
		else
		{
			// Bottom action row always has five cells.
			_col += dx;
			if (_col < 0)
			{
				_col = 4;
			}
			else if (_col > 4)
			{
				_col = 0;
			}
		}
	}

	/// <summary>Draws the keyboard. Called from <c>UI.Draw</c>.</summary>
	public static void Draw()
	{
		if (!_open)
		{
			return;
		}
		UI ui = UI.MainUI;
		float num = Main.ScreenMultiplier;
		string[] page = CurrentPage();
		int num2 = Columns * CellWidth;
		int num3 = (Main.ResolutionWidth - (int)((float)num2 * num)) / 2;
		int num4 = (int)(90f * num);
		int num5 = (int)(((float)(page.Length + 1) * CellHeight + 96f) * num);
		// Backdrop and frame.
		Main.DrawSolidRect(new Rectangle(0, 0, Main.ResolutionWidth, Main.ResolutionHeight), new Color(0, 0, 0, 170));
		Main.DrawSolidRect(new Rectangle(num3 - (int)(12f * num), num4 - (int)(44f * num), (int)((float)num2 * num) + (int)(24f * num), num5), new Color(14, 18, 26, 240));
		Main.DrawSolidRect(new Rectangle(num3 - (int)(12f * num), num4 - (int)(44f * num), (int)((float)num2 * num) + (int)(24f * num), 1), new Color(70, 110, 170));
		UI.DrawStringCC(UI.BoldSmallFont, _title, num3 + (int)((float)num2 * num / 2f), num4 - (int)(36f * num), Color.White);
		// Current value with a caret.
		string text = Buffer.ToString();
		if (text.Length == 0)
		{
			text = " ";
		}
		string text2 = "> " + text + ((Main.FrameCounter / 20) % 2 == 0 ? "_" : "");
		UI.DrawStringLC(UI.BoldSmallFont, text2, num3, num4 - (int)(16f * num), new Color(120, 200, 255));
		// Grid.
		for (int i = 0; i < page.Length; i++)
		{
			for (int j = 0; j < page[i].Length; j++)
			{
				DrawCell(num3, num4, j, i, page[i][j].ToString(), _row == i && _col == j);
			}
		}
		int num6 = page.Length;
		DrawCell(num3, num4, 0, num6, "space", _row == num6 && _col == 0, wide: true);
		DrawCell(num3, num4, 2, num6, "del", _row == num6 && _col == 2);
		DrawCell(num3, num4, 3, num6, _shift ? "CAPS" : "caps", _row == num6 && _col == 3);
		DrawCell(num3, num4, 4, num6, "OK", _row == num6 && _col == 4);
		DrawCell(num3, num4, 5, num6, "X", _row == num6 && _col == 5);
		string text3 = _symbols ? "Y - letters    X - delete    B - back    Start - confirm" : "Y - symbols    X - delete    B - back    Start - confirm";
		UI.DrawStringCC(UI.SmallFont, text3, num3 + (int)((float)num2 * num / 2f), num4 + (int)(((float)(num6 + 1) * CellHeight + 12f) * num), new Color(150, 160, 180));
		if (KeyboardMouseInput.Enabled)
		{
			// A keyboard-and-mouse player can type, but the hint should not claim the pad is the
			// only way in.
			UI.DrawStringCC(UI.SmallFont, "Type directly, Enter - confirm, Esc - cancel, or click a key",
				num3 + (int)((float)num2 * num / 2f), num4 + (int)(((float)(num6 + 1) * CellHeight + 30f) * num), new Color(120, 190, 140));
		}
	}

	/// <summary>
	/// The on-screen rectangle of one cell, in the same coordinates the grid is drawn in. Shared
	/// by the drawing code and by the mouse hit-test, so a click cannot land on a different key
	/// than the one it is over.
	/// </summary>
	internal static Rectangle CellRect(int col, int row, bool wide = false)
	{
		float num = Main.ScreenMultiplier;
		int num2 = Columns * CellWidth;
		int num3 = (Main.ResolutionWidth - (int)((float)num2 * num)) / 2;
		int num4 = (int)(90f * num);
		return new Rectangle(
			num3 + (int)((float)(col * CellWidth) * num),
			num4 + (int)((float)(row * CellHeight) * num),
			(int)((float)((wide ? CellWidth * 2 : CellWidth) - 3) * num),
			(int)((float)(CellHeight - 3) * num));
	}

	private static void DrawCell(int originX, int originY, int col, int row, string label, bool selected, bool wide = false)
	{
		float num = Main.ScreenMultiplier;
		int num2 = originX + (int)((float)(col * CellWidth) * num);
		int num3 = originY + (int)((float)(row * CellHeight) * num);
		int num4 = (int)((float)((wide ? CellWidth * 2 : CellWidth) - 3) * num);
		int num5 = (int)((float)(CellHeight - 3) * num);
		Color color = selected ? new Color(60, 110, 190) : new Color(34, 40, 52);
		Main.DrawSolidRect(new Rectangle(num2, num3, num4, num5), color);
		Vector2 vector = UI.SmallFont.MeasureString(label);
		UI.DrawStringLC(UI.SmallFont, label, num2 + (num4 - (int)vector.X) / 2, num3 + (num5 - (int)vector.Y) / 2, Color.White);
	}
}
