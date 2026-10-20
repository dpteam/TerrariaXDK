using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Terraria.Input;

/// <summary>
/// Player chat. The console build only ever displayed chat lines (<c>Main.ChatLineSet</c>);
/// there was no way to type anything. This adds the missing input side:
///
/// * Enter (or T) opens the prompt, Escape closes it.
/// * Characters come from <see cref="TextInputEXT"/>, so the OS keyboard layout and dead
///   keys work properly instead of being guessed from key codes.
/// * Enter sends: to the server in multiplayer, and to the console in single player,
///   where a leading '!' runs a <see cref="Terraria.Cheats.CheatConsole"/> command.
/// </summary>
internal static class ChatInput
{
	private const int MaxLength = 200;

	/// <summary>Number of previous messages shown above the input.</summary>
	private const int HistoryLines = 4;

	private static readonly System.Collections.Generic.List<string> History = new System.Collections.Generic.List<string>();

	/// <summary>Previously sent lines, newest last. Navigated with Up/Down like a terminal.</summary>
	private static readonly System.Collections.Generic.List<string> Sent = new System.Collections.Generic.List<string>();

	private static int _sentCursor = -1;

	private static readonly StringBuilder Buffer = new StringBuilder(MaxLength);

	private static bool _subscribed;

	/// <summary>True while the player is typing.</summary>
	public static bool IsActive { get; private set; }

	public static string Text => Buffer.ToString();

	/// <summary>Wire the character source once. Safe to call repeatedly.</summary>
	public static void Initialize()
	{
		if (_subscribed)
		{
			return;
		}
		_subscribed = true;
		TextInputEXT.TextInput += OnTextInput;
	}

	/// <summary>
	/// Single source of truth for typed characters. FNA raises this for printable characters
	/// (from SDL_TEXTINPUT) and for editing keys (backspace, delete, tab, enter, home/end),
	/// so nothing else may also append or delete - that was the source of the doubled input.
	/// </summary>
	private static void OnTextInput(char c)
	{
		if (!IsActive)
		{
			return;
		}
		switch (c)
		{
		case '\b':
			// Backspace.
			if (Buffer.Length > 0)
			{
				Buffer.Length--;
			}
			return;
		case '\u007f':
			// Delete: no caret movement model, so treat it like backspace.
			if (Buffer.Length > 0)
			{
				Buffer.Length--;
			}
			return;
		case '\r':
			// Enter is handled by the key loop, which also knows whether we are opening or
			// submitting; ignore the duplicate from the text input path.
			return;
		}
		if (c < ' ')
		{
			return;
		}
		if (Buffer.Length < MaxLength)
		{
			Buffer.Append(c);
		}
	}

	/// <summary>Opens the prompt if it is closed.</summary>
	public static void Open(bool commandMode = false)
	{
		if (IsActive)
		{
			return;
		}
		IsActive = true;
		Buffer.Length = 0;
		_sentCursor = -1;
		if (commandMode)
		{
			Buffer.Append('!');
		}
		KeyboardMouseInput.TextInputActive = true;
		try
		{
			TextInputEXT.StartTextInput();
		}
		catch
		{
			// Backend without IME support: key codes still work for ASCII.
		}
	}

	/// <summary>Closes the prompt without sending.</summary>
	public static void Close()
	{
		if (!IsActive)
		{
			return;
		}
		IsActive = false;
		Buffer.Length = 0;
		KeyboardMouseInput.TextInputActive = false;
		// The keystroke that closed the prompt must not also reach the game this frame.
		KeyboardMouseInput.SuppressGameInput = true;
		try
		{
			TextInputEXT.StopTextInput();
		}
		catch
		{
		}
	}

	/// <summary>
	/// Per-frame handling of the open prompt: Enter sends, Escape cancels, Backspace deletes.
	/// Must run before the player reads input, otherwise typing would move the character.
	/// </summary>
	public static void Update()
	{
		if (KeyboardMouseInput.KeyPressed(KeyBindings.Get(KeyBindings.Action.Chat)))
		{
			if (IsActive)
			{
				Submit();
			}
			else if (!KeyboardMouseInput.MenuContext)
			{
				// Chat is always available during play. In single player it is the console:
				// commands run when cheats are enabled, and plain text simply echoes locally.
				Open();
			}
			return;
		}
		if (!IsActive)
		{
			return;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Escape))
		{
			Close();
			return;
		}
		// Gamepad users have no keyboard: A brings up the on-screen keyboard while the
		// prompt is open. On desktop this is harmless (A is the pad's confirm button).
		if (UI.MainUI != null && UI.MainUI.IsButtonTriggered(Buttons.A) && !VirtualKeyboard.IsOpen)
		{
			string captured = Text;
			VirtualKeyboard.Open("Say", captured, delegate(string result)
			{
				SetBuffer(result);
			});
			return;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Up))
		{
			RecallSent(-1);
			return;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Down))
		{
			RecallSent(1);
			return;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Tab))
		{
			CompleteCommand();
			return;
		}
		// Backspace is NOT handled here: SDL delivers it through TextInputEXT as '\b', and
		// handling it in both places deleted two characters per press.
	}

	/// <summary>Replaces the buffer contents (used by the on-screen keyboard).</summary>
	internal static void SetBuffer(string text)
	{
		Buffer.Length = 0;
		if (!string.IsNullOrEmpty(text))
		{
			Buffer.Append(text);
		}
	}

	/// <summary>Steps through previously sent lines. Direction -1 is older, +1 is newer.</summary>
	private static void RecallSent(int direction)
	{
		if (Sent.Count == 0)
		{
			return;
		}
		if (_sentCursor < 0)
		{
			_sentCursor = Sent.Count;
		}
		_sentCursor += direction;
		if (_sentCursor < 0)
		{
			_sentCursor = 0;
		}
		if (_sentCursor >= Sent.Count)
		{
			// Past the newest entry: back to an empty prompt.
			_sentCursor = -1;
			Buffer.Length = 0;
			return;
		}
		Buffer.Length = 0;
		Buffer.Append(Sent[_sentCursor]);
	}

	/// <summary>Tab-completes a cheat command name when the line starts with '!' or '/'.</summary>
	private static void CompleteCommand()
	{
		string text = Buffer.ToString();
		if (text.Length < 1 || (text[0] != '!' && text[0] != '/'))
		{
			return;
		}
		string text2 = text.Substring(1).ToLowerInvariant();
		string[] names = Cheats.CheatConsole.CommandNames;
		for (int i = 0; i < names.Length; i++)
		{
			if (!names[i].StartsWith(text2, StringComparison.Ordinal))
			{
				continue;
			}
			Buffer.Length = 0;
			Buffer.Append(text[0]);
			Buffer.Append(names[i]);
			Buffer.Append(' ');
			return;
		}
	}

	private static void Submit()
	{
		string text = Buffer.ToString().Trim();
		Close();
		if (text.Length == 0)
		{
			return;
		}
		// Remember it for Up/Down recall.
		if (Sent.Count == 0 || Sent[Sent.Count - 1] != text)
		{
			Sent.Add(text);
			while (Sent.Count > 50)
			{
				Sent.RemoveAt(0);
			}
		}
		// Cheat commands stay local and never reach the network.
		if (Cheats.CheatConsole.IsCommand(text))
		{
			Program.LogInfo("Chat", "command: " + text);
			string error;
			Cheats.CheatConsole.TryExecute(text, out error);
			if (!string.IsNullOrEmpty(error))
			{
				Main.NewText(error, 255, 200, 120);
			}
			return;
		}
		Main.NewText("<Player> " + text, 255, 255, 255);
		AddHistory("<Player> " + text);
		if (Main.NetMode != 0)
		{
			try
			{
				NetMessage.SendText(text, 255, 255, 255, 255);
				NetMessage.SendMessage();
			}
			catch (Exception ex)
			{
				Program.LogWarn("Chat", "send failed: " + ex.Message);
			}
		}
	}

	/// <summary>
	/// Draws the chat prompt: a panel with a border, a caption, the typed text and a blinking
	/// caret. Sized from the font so it scales with the resolution like the rest of the HUD.
	/// </summary>
	public static void Draw()
	{
		if (!IsActive)
		{
			return;
		}
		UI ui = UI.MainUI;
		int num = ((ui != null && ui.CurrentView != null) ? ui.CurrentView.ViewWidth : Main.ResolutionWidth);
		float num2 = Main.ScreenMultiplier;
		int num3 = (int)(48f * num2);
		// Panel sits above the hotbar and is tall enough for the input plus a short history,
		// so the prompt no longer feels squeezed into a single line.
		int num4 = (int)(36f * num2) * (HistoryLines + 1) + (int)(30f * num2);
		int num5 = Main.ResolutionHeight - (int)(90f * num2) - num4;
		int num6 = num - (int)(64f * num2);
		int num7 = (int)(34f * num2);
		// Shadow, body, border.
		Main.DrawSolidRect(new Rectangle(num3 + 2, num5 + 2, num6, num4), new Color(0, 0, 0, 120));
		Main.DrawSolidRect(new Rectangle(num3, num5, num6, num4), new Color(12, 16, 24, 232));
		Main.DrawSolidRect(new Rectangle(num3, num5, num6, 1), new Color(70, 110, 170, 255));
		Main.DrawSolidRect(new Rectangle(num3, num5 + num4 - 1, num6, 1), new Color(70, 110, 170, 255));
		Main.DrawSolidRect(new Rectangle(num3, num5, 1, num4), new Color(70, 110, 170, 255));
		Main.DrawSolidRect(new Rectangle(num3 + num6 - 1, num5, 1, num4), new Color(70, 110, 170, 255));
		int num8 = (int)(12f * num2);
		int num9 = (int)(36f * num2);
		// History above the input, dimmed.
		for (int i = 0; i < HistoryLines; i++)
		{
			if (i >= History.Count)
			{
				break;
			}
			string s = History[History.Count - 1 - i];
			UI.DrawStringLC(UI.BoldSmallFont, s, num3 + num8, num5 + (int)(8f * num2) + i * num9 + (int)(8f * num2), new Color(140, 150, 165));
		}
		int num10 = num5 + num4 - num7 - (int)(6f * num2);
		Main.DrawSolidRect(new Rectangle(num3, num10 - (int)(4f * num2), num6, 1), new Color(40, 60, 90, 255));
		bool flag = IsCommand(Buffer.ToString());
		string text = flag ? "Command" : "Say";
		int num11 = num10 + (num7 - (int)UI.BoldSmallFont.MeasureString("A").Y) / 2;
		UI.DrawStringLC(UI.BoldSmallFont, text, num3 + num8, num11, flag ? new Color(120, 230, 140) : new Color(130, 170, 220));
		int num12 = num3 + num8 + (int)UI.BoldSmallFont.MeasureString(text + "  ").X;
		string text2 = Buffer.ToString();
		UI.DrawStringLC(UI.BoldSmallFont, text2, num12, num11, Color.White);
		if ((Main.FrameCounter / 20) % 2 == 0)
		{
			int num13 = num12 + (int)UI.BoldSmallFont.MeasureString(text2).X;
			Main.DrawSolidRect(new Rectangle(num13 + 1, num11, 2, (int)UI.BoldSmallFont.MeasureString("A").Y), Color.White);
		}
		string text3 = flag ? "Enter = run   Tab = complete   Up/Down = history   Esc = cancel" : "Enter = send   Up/Down = history   Esc = cancel";
		UI.DrawStringLC(UI.BoldSmallFont, text3, num3 + num8, num10 + num7 + (int)(2f * num2), new Color(140, 140, 150));
	}

	/// <summary>Adds a line to the small history shown above the input.</summary>
	public static void AddHistory(string line)
	{
		if (string.IsNullOrEmpty(line))
		{
			return;
		}
		History.Add(line);
		while (History.Count > HistoryLines)
		{
			History.RemoveAt(0);
		}
	}

	/// <summary>True when the buffer starts with a command marker.</summary>
	private static bool IsCommand(string text)
	{
		return text.Length > 0 && (text[0] == '!' || text[0] == '/');
	}
}
