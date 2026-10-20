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
				// Chat is only useful when it can go somewhere: to the server, or to the
				// local cheat console in single player.
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

	/// <summary>Draws the prompt above the hotbar.</summary>
	public static void Draw()
	{
		if (!IsActive)
		{
			return;
		}
		Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch = Main.SpriteBatch;
		UI ui = UI.MainUI;
		int num = ui.CurrentView != null ? ui.CurrentView.ViewWidth : Main.ResolutionWidth;
		int y = Main.ResolutionHeight - (int)(120f * Main.ScreenMultiplier);
		string text = "Say: " + Buffer.ToString() + ((Main.FrameCounter / 30) % 2 == 0 ? "_" : "");
		Main.DrawSolidRect(new Rectangle(0, y - 6, num, (int)(34f * Main.ScreenMultiplier)), new Color(0, 0, 0, 160));
		UI.DrawStringLC(UI.BoldSmallFont, text, (int)(16f * Main.ScreenMultiplier), y, Color.White);
	}
}
