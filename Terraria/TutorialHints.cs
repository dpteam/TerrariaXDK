using System;
using System.Collections.Generic;

namespace Terraria;

/// <summary>
/// Rewrites the tutorial's gamepad button glyphs into the keys a keyboard-and-mouse player
/// actually presses.
///
/// The tutorial is a port of the console build's, and its text is full of glyphs from
/// <c>Content/Fonts</c>: <c>\u0081</c> is a trigger, <c>\u008c</c> is A, and so on. They render as
/// button icons, which is exactly right on a pad and meaningless on a keyboard. That is the whole
/// bug: the tutorial taught buttons the player does not have.
///
/// Why phrases and not glyphs: a glyph is one button, and one button does several things. A is
/// "jump" in one step and "create this" in another; the left stick is "move" in one step and
/// "between slots" in the next. A per-glyph substitution would be right about half the time, and a
/// tutorial that is wrong half the time is worse than one that is merely console-shaped. So each
/// rule names the sentence, which is unambiguous.
///
/// The rules are English because the tutorial's other four languages phrase the same steps
/// differently, and a rule that half-matches a German sentence would corrupt it. A language with
/// no rules keeps the original text - no regression, and the work left is written down in
/// <c>IDEAS.md</c>. <see cref="HasGlyph"/> exists so a test can prove the English tutorial has no
/// glyph left: a missing rule shows up as a failing check, not as a button icon on screen.
/// </summary>
internal static class TutorialHints
{
	/// <summary>
	/// Sentence → desktop sentence. Ordered longest first, so a specific rule always wins over a
	/// general one that happens to be a substring of it.
	/// </summary>
	private static readonly string[][] Rules = new string[][]
	{
		new string[2]
		{
			"Use \u008c to pick up and place stacks of items. Use \u0081 to move one item at a time.",
			"Use LMB to pick up and place stacks of items. Use RMB to move one item at a time."
		},
		new string[2]
		{
			"Press (or hold) \u0081 to perform actions with the current item. Aim with \u0085.",
			"Press (or hold) LMB to perform actions with the current item. Aim with the mouse."
		},
		new string[2]
		{
			"Press \u0082 or \u0083 until you switch to the Crafting section.",
			"Click the Crafting tab."
		},
		new string[2]
		{
			"Switch to Crafting with \u0082 and \u0083.",
			"Click the Crafting tab."
		},
		new string[2]
		{
			"Press \u0083 to switch to the Equip section.",
			"Click the Equip tab."
		},
		new string[2]
		{
			"In Manual Cursor Mode \u0085 acts like a mouse.",
			"In Manual Cursor Mode the cursor follows the mouse."
		},
		new string[2]
		{
			"Press \u008f to switch your Cursor Mode.",
			"Press C to switch your Cursor Mode."
		},
		new string[2]
		{
			"Press \u008d to exit Crafting.",
			"Press RMB to exit Crafting."
		},
		new string[2]
		{
			"<c>Press \u008d to continue.",
			"<c>Press RMB to continue."
		},
		new string[2]
		{
			"Press \u008b to open the Inventory Menu.",
			"Press I to open the Inventory Menu."
		},
		new string[2]
		{
			"Use \u0084 to move between slots.",
			"Use WASD to move between slots."
		},
		new string[2]
		{
			"Now jump out by pressing \u008c.",
			"Now jump out by pressing Space."
		},
		new string[2]
		{
			"and press \u008c to create it.",
			"and click it to create it."
		},
		new string[2]
		{
			"Aim at it and press \u008d.",
			"Aim at it and press RMB."
		},
		new string[2]
		{
			"press \u008b to enter or exit the Ingredients area",
			"press Q to enter or exit the Ingredients area"
		},
		new string[2]
		{
			"pressing \u0080 and \u0081.",
			"clicking the category tabs."
		},
		new string[2]
		{
			"Cursor Mode, \u008f.",
			"Cursor Mode, C."
		},
		new string[2]
		{
			"Use \u0084 to move around.",
			"Use WASD to move around."
		},
		new string[2]
		{
			"Press \u008c to jump.",
			"Press Space to jump."
		},
		new string[2]
		{
			"pressing down \u0084",
			"pressing S"
		},
		new string[2]
		{
			"with \u0082 & \u0083",
			"with the mouse wheel"
		},
		new string[2]
		{
			"by pressing \u008f.",
			"by pressing C."
		},
		new string[2]
		{
			"pressing \u0081",
			"pressing LMB"
		},
		new string[2]
		{
			"holding \u0081",
			"holding LMB"
		},
		new string[2]
		{
			"press \u0081",
			"press LMB"
		},
		new string[2]
		{
			"or press \u008a.",
			"or press R."
		}
	};

	/// <summary>
	/// True when the player is on keyboard and mouse, which is the setting the text follows.
	///
	/// <see cref="Terraria.Input.KeyboardMouseInput.Enabled"/> rather than the live cursor state:
	/// the text is built once per tutorial step, and a player who moves the mouse after reading a
	/// step would keep the old text anyway. The setting is the player's own answer to "which device
	/// am I using", so following it is both stable and predictable.
	/// </summary>
	internal static bool Desktop => Terraria.Input.KeyboardMouseInput.Enabled;

	/// <summary>
	/// Rewrites the glyphs in a tutorial string, or returns it unchanged.
	///
	/// Not every language has rules; the ones that do not keep the console text. Nothing here
	/// throws or guesses: an unknown glyph is left alone rather than replaced by a word that might
	/// be wrong.
	/// </summary>
	internal static string Translate(string text)
	{
		if (string.IsNullOrEmpty(text) || !Desktop)
		{
			return text;
		}
		string result = text;
		for (int i = 0; i < Rules.Length; i++)
		{
			if (result.IndexOf(Rules[i][0], StringComparison.Ordinal) >= 0)
			{
				result = result.Replace(Rules[i][0], Rules[i][1]);
			}
		}
		return result;
	}

	/// <summary>True when a string still contains a console button glyph. For the test.</summary>
	internal static bool HasGlyph(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] >= '\u0080' && text[i] <= '\u008f')
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>How many rules there are. For the test and the log.</summary>
	internal static int RuleCount => Rules.Length;
}
