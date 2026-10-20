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

	private static void OnTextInput(char c)
	{
		if (!IsActive)
		{
			return;
		}
		// Control characters are handled by the key loop; newline would break the line.
		if (c < ' ' || c == '\u007F')
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
		if (KeyboardMouseInput.KeyPressed(Keys.Enter))
		{
			if (IsActive)
			{
				Submit();
			}
			else if (Main.NetMode != 0 || Cheats.CheatState.IsAvailable)
			{
				// Chat only opens during play. In menus Enter means "confirm", and it must not
				// be swallowed here or the menu would stop responding to it.
				if (!KeyboardMouseInput.MenuContext)
				{
					Open();
				}
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
		if (KeyboardMouseInput.KeyPressed(Keys.Back))
		{
			if (Buffer.Length > 0)
			{
				Buffer.Length--;
			}
			return;
		}
		// Fallback for platforms where TextInputEXT is not wired to SDL: accept plain ASCII
		// from A-Z, 0-9 and the space bar.
		AppendFallbackKeys();
	}

	private static void AppendFallbackKeys()
	{
		for (Keys key = Keys.A; key <= Keys.Z; key++)
		{
			if (KeyboardMouseInput.KeyPressed(key))
			{
				bool shift = KeyboardMouseInput.KeyDown(Keys.LeftShift) || KeyboardMouseInput.KeyDown(Keys.RightShift);
				char c = (char)('a' + (key - Keys.A));
				if (shift)
				{
					c = char.ToUpperInvariant(c);
				}
				if (Buffer.Length < MaxLength)
				{
					Buffer.Append(c);
				}
			}
		}
		for (Keys key2 = Keys.D0; key2 <= Keys.D9; key2++)
		{
			if (!KeyboardMouseInput.KeyPressed(key2))
			{
				continue;
			}
			// Skip the hotbar digits: while typing they are text, not slot selection, and
			// TextInputEXT already delivered them on platforms that support it.
			break;
		}
		if (KeyboardMouseInput.KeyPressed(Keys.Space) && Buffer.Length < MaxLength && Buffer.Length > 0)
		{
			Buffer.Append(' ');
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
		// Cheat commands stay local and never reach the network.
		if (Cheats.CheatConsole.IsCommand(text))
		{
			string error;
			if (Cheats.CheatConsole.TryExecute(text, out error) && error != null)
			{
				Main.NewText(error, 255, 200, 120);
			}
			return;
		}
		Main.NewText("<Player> " + text, 255, 255, 255);
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
		int num4 = Main.ResolutionHeight - num3 - (int)(90f * num2);
		int num5 = num - (int)(64f * num2);
		int num6 = (int)(34f * num2);
		// Shadowed frame with a slightly lighter interior reads better over the world.
		Main.DrawSolidRect(new Rectangle(num3 + 2, num4 + 2, num5, num6), new Color(0, 0, 0, 120));
		Main.DrawSolidRect(new Rectangle(num3, num4, num5, num6), new Color(12, 16, 24, 225));
		Main.DrawSolidRect(new Rectangle(num3, num4, num5, 1), new Color(70, 110, 170, 255));
		Main.DrawSolidRect(new Rectangle(num3, num4 + num6 - 1, num5, 1), new Color(70, 110, 170, 255));
		Main.DrawSolidRect(new Rectangle(num3, num4, 1, num6), new Color(70, 110, 170, 255));
		Main.DrawSolidRect(new Rectangle(num3 + num5 - 1, num4, 1, num6), new Color(70, 110, 170, 255));
		bool flag = IsCommand(Buffer.ToString());
		string text = flag ? "Command" : "Say";
		int num7 = (int)(12f * num2);
		int num8 = num4 + (num6 - (int)UI.BoldSmallFont.MeasureString("A").Y) / 2;
		UI.DrawStringLC(UI.BoldSmallFont, text, num3 + num7, num8, flag ? new Color(120, 230, 140) : new Color(130, 170, 220));
		int num9 = num3 + num7 + (int)UI.BoldSmallFont.MeasureString(text + "  ").X;
		string text2 = Buffer.ToString();
		UI.DrawStringLC(UI.BoldSmallFont, text2, num9, num8, Color.White);
		// Caret: only while it is "on", and always after the text.
		if ((Main.FrameCounter / 20) % 2 == 0)
		{
			int num10 = num9 + (int)UI.BoldSmallFont.MeasureString(text2).X;
			Main.DrawSolidRect(new Rectangle(num10 + 1, num8, 2, (int)UI.BoldSmallFont.MeasureString("A").Y), Color.White);
		}
		// Hint line under the field.
		string text3 = flag ? "Enter = run   Esc = cancel   !help" : "Enter = send   Esc = cancel";
		UI.DrawStringLC(UI.BoldSmallFont, text3, num3 + num7, num4 + num6 + (int)(4f * num2), new Color(140, 140, 150));
	}

	/// <summary>True when the buffer starts with a command marker.</summary>
	private static bool IsCommand(string text)
	{
		return text.Length > 0 && (text[0] == '!' || text[0] == '/');
	}
}
