using System;
using System.Collections.Generic;

namespace Terraria;

/// <summary>What a button glyph means in the desktop build.</summary>
internal enum TutorialKey
{
	/// <summary>Advance the tutorial / close the dialogue: the right button.</summary>
	Continue,
	/// <summary>Walk: WASD.</summary>
	Move,
	/// <summary>Walk down: S.</summary>
	MoveDown,
	/// <summary>Jump: Space.</summary>
	Jump,
	/// <summary>Use / attack / place / pick up: the left button.</summary>
	Action,
	/// <summary>Aim: the mouse.</summary>
	Aim,
	/// <summary>The cursor itself, in the step that explains cursor modes.</summary>
	Cursor,
	/// <summary>Scroll through items or slots: the wheel.</summary>
	SwitchItem,
	/// <summary>Open the inventory: I.</summary>
	Inventory,
	/// <summary>Ingredients/recipes toggle: Q.</summary>
	Ingredients,
	/// <summary>Drop an item: R.</summary>
	Drop,
	/// <summary>Switch cursor mode: C.</summary>
	CursorMode,
	/// <summary>Move one item instead of the stack: the right button.</summary>
	OneItem
}

/// <summary>
/// Rewrites the tutorial's gamepad button glyphs into the keys a keyboard-and-mouse player
/// actually presses.
///
/// The tutorial is a port of the console build's, and its text is full of glyphs from
/// <c>Content/Fonts</c>: <c>\u0081</c> is a trigger, <c>\u008c</c> is A, and so on. They render as
/// button icons, which is exactly right on a pad and meaningless on a keyboard. That is the whole
/// bug: the tutorial taught buttons the player does not have.
///
/// <b>Keyed by step and glyph, not by sentence.</b> That is the design decision worth explaining,
/// because the obvious alternatives both fail:
/// <list type="bullet">
/// <item><b>By glyph alone</b> cannot work: one button does several things. A is "jump" in one
/// step and "create this" in another; the left stick is "move" in one step and "between slots" in
/// the next. A per-glyph substitution would be right about half the time.</item>
/// <item><b>By sentence</b> works but only for one language: the same step is phrased differently
/// in each of the five translations, so it would need 5 × 43 rules and a half-matched German
/// sentence would be corrupted rather than fixed.</item>
/// </list>
/// A step number and a glyph are the same in every language - the translations are line-for-line
/// equivalents - so one table covers all five. Verified: every language has its glyphs at exactly
/// the same steps.
///
/// Five steps need more than a glyph swap because the sentence itself talks about the button as a
/// button ("press A or B until you switch to the Crafting section"): those are replaced wholesale,
/// per language, in <see cref="Overrides"/>.
/// </summary>
internal static class TutorialHints
{
	/// <summary>Steps whose sentence is replaced as a whole rather than glyph by glyph.</summary>
	private static readonly int[] OverrideSteps = new int[5] { 7, 24, 29, 33, 40 };

	/// <summary>
	/// Replacement sentences: [step index in <see cref="OverrideSteps"/>][language]. Language
	/// order is the same as <see cref="Lang.LangOption"/>: EN, DE, IT, FR, ES.
	///
	/// Written to match the existing translations' style - plain ASCII, no accents - because the
	/// shipped atlases were built from those strings and a character they cannot draw would show as
	/// a box, which is a worse bug than the one being fixed.
	/// </summary>
	private static readonly string[][] Overrides = new string[][]
	{
		// 7: the inventory bar, "switch between items with LB & RB"
		new string[5]
		{
			"At the top of your screen is the Inventory Bar. You can switch between items with the mouse wheel. Now switch to your sword.",
			"Am oberen Ende des Bildschirms befindet sich deine Inventarleiste. Mit dem Mausrad kannst du zwischen Gegenstanden wechseln. Wechsle zu deinem Schwert.",
			"La barra dell'Inventario si trova nella parte superiore dello schermo. E possibile passare da un oggetto all'altro con la rotellina del mouse. Adesso passa alla spada.",
			"La barre d'inventaire figure en haut de votre ecran. Vous pouvez changer d'objet avec la molette de la souris. Maintenant, prenez votre epee.",
			"La barra de inventario esta en la parte superior de la pantalla. Puedes desplazarte entre los objetos con la rueda del raton. Ahora, cambia a la espada."
		},
		// 24: "press RB to switch to the Equip section"
		new string[5]
		{
			"Click the Equip tab to switch to the Equip section.",
			"Klicke auf die Registerkarte Ausrustung, um zur Ausrustungssektion zu wechseln.",
			"Fai clic sulla scheda Equipaggiamento per passare alla sezione Equipaggiamento.",
			"Cliquez sur l'onglet Equipement pour acceder a la section Equipement.",
			"Haz clic en la pestana Equipo para cambiar a la seccion Equipo."
		},
		// 29: "press LB or RB until you switch to the Crafting section"
		new string[5]
		{
			"Click the Crafting tab.",
			"Klicke auf die Registerkarte Herstellung.",
			"Fai clic sulla scheda Creazione Oggetti.",
			"Cliquez sur l'onglet Artisanat.",
			"Haz clic en la pestana Creacion."
		},
		// 33: "switch between them by pressing LT and RT"
		new string[5]
		{
			"There are several categories of crafting items. You can switch between them by clicking the category tabs.",
			"Es gibt verschiedene Kategorien von herstellbaren Gegenstanden. Du kannst zwischen ihnen wechseln, indem du auf die Registerkarten klickst.",
			"Ci sono diverse categorie di oggetti da creare. Puoi passare dall'una all'altra facendo clic sulle schede.",
			"Il existe plusieurs categories d'objets d'artisanat. Vous pouvez les consulter en cliquant sur les onglets.",
			"Hay varias categorias de objetos que puedes crear. Puedes cambiar entre ellas haciendo clic en las pestanas."
		},
		// 40: "press Y to open the inventory, switch to crafting with LB and RB"
		new string[5]
		{
			"Press I to open the Inventory Menu. Then click the Crafting tab.",
			"Drucke I, um das Inventarmenu zu offnen. Klicke dann auf die Registerkarte Herstellung.",
			"Premi I per aprire il menu dell'Inventario. Poi fai clic sulla scheda Creazione Oggetti.",
			"Appuyez sur I pour ouvrir le menu Inventaire. Cliquez ensuite sur l'onglet Artisanat.",
			"Pulsa I para abrir el menu Inventario. Luego haz clic en la pestana Creacion."
		}
	};

	/// <summary>
	/// Per-step meaning of each glyph: <c>[step, glyph, role]</c>.
	///
	/// Only the steps that contain a glyph are listed; a step without one is left alone. The step
	/// numbers are <see cref="Tutorial"/> values, so a reader can look the sentence up.
	/// </summary>
	private static readonly short[][] Steps = new short[][]
	{
		// INTRO_2, MONSTER_INFO_2, POTIONS_2, TORCH_2, INVENTORY_3, MOVEMENT_2, DROP_2,
		// EQUIPSCREEN_2, CHEST_2, CRAFTSCREEN_2, CRAFTING_EXIT, MINED_ORE_2, DAY_NIGHT_2,
		// BUILD_HOUSE_EXTRA_INFO, USE_BENCH_2, BACK_WALL_INFO_2, HOUSE_INFO_2, HOUSE_DONE_2,
		// THE_GUIDE_2, HAMMER_2, CONGRATS_2: "<c>Press B to continue."
		new short[3] { 1, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 2, 0x84, (short)TutorialKey.Move },
		new short[3] { 3, 0x8c, (short)TutorialKey.Jump },
		new short[3] { 4, 0x84, (short)TutorialKey.MoveDown },
		new short[3] { 5, 0x8c, (short)TutorialKey.Jump },
		new short[3] { 6, 0x81, (short)TutorialKey.Action },
		new short[3] { 6, 0x85, (short)TutorialKey.Aim },
		new short[3] { 8, 0x81, (short)TutorialKey.Action },
		new short[3] { 10, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 12, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 14, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 16, 0x81, (short)TutorialKey.Action },
		new short[3] { 17, 0x8b, (short)TutorialKey.Inventory },
		new short[3] { 19, 0x8d, (short)TutorialKey.Continue },
		// MOVEMENT_1: "move between slots / pick up a stack / move one item"
		new short[3] { 20, 0x84, (short)TutorialKey.Move },
		new short[3] { 20, 0x8c, (short)TutorialKey.Action },
		new short[3] { 20, 0x81, (short)TutorialKey.OneItem },
		new short[3] { 21, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 22, 0x8a, (short)TutorialKey.Drop },
		new short[3] { 23, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 26, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 28, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 30, 0x8c, (short)TutorialKey.Action },
		new short[3] { 31, 0x8b, (short)TutorialKey.Ingredients },
		new short[3] { 32, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 34, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 36, 0x81, (short)TutorialKey.Action },
		new short[3] { 38, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 42, 0x8f, (short)TutorialKey.CursorMode },
		// PLACING_1: the right stick becomes the cursor here, not the mouse: the sentence already
		// compares it to a mouse, so "the mouse acts like a mouse" would be nonsense.
		new short[3] { 43, 0x85, (short)TutorialKey.Cursor },
		new short[3] { 43, 0x81, (short)TutorialKey.Action },
		new short[3] { 45, 0x8f, (short)TutorialKey.CursorMode },
		new short[3] { 46, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 48, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 56, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 59, 0x8f, (short)TutorialKey.CursorMode },
		new short[3] { 60, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 62, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 65, 0x8f, (short)TutorialKey.CursorMode },
		new short[3] { 68, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 70, 0x8f, (short)TutorialKey.CursorMode },
		new short[3] { 72, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 74, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 76, 0x8d, (short)TutorialKey.Continue },
		new short[3] { 78, 0x8d, (short)TutorialKey.Continue }
	};

	/// <summary>
	/// Key labels. Deliberately the same in every language: "LMB", "WASD", "Space" are what the
	/// hint bar at the bottom of the screen already says, and a translated noun would need the right
	/// grammatical case in five languages ("mit der linken Maustaste" vs "mit die linke...") - which
	/// is a translation problem, not an input problem.
	/// </summary>
	private static string Label(TutorialKey key)
	{
		switch (key)
		{
		case TutorialKey.Continue:
		case TutorialKey.OneItem:
			return "RMB";
		case TutorialKey.Move:
			return "WASD";
		case TutorialKey.MoveDown:
			return "S";
		case TutorialKey.Jump:
			return "Space";
		case TutorialKey.Action:
			return "LMB";
		case TutorialKey.Inventory:
			return "I";
		case TutorialKey.Ingredients:
			return "Q";
		case TutorialKey.Drop:
			return "R";
		case TutorialKey.CursorMode:
			return "C";
		default:
			// The two that are words rather than key names; the caller supplies them.
			return null;
		}
	}

	/// <summary>
	/// "The mouse", "the cursor", "the wheel", per language, for the three roles that are not a key
	/// name. Indexed like <see cref="Lang.LangOption"/>: EN, DE, IT, FR, ES.
	/// </summary>
	private static string Word(TutorialKey key, int language)
	{
		string[][] table = Words;
		int num = (language >= 1 && language <= 5) ? (language - 1) : 0;
		switch (key)
		{
		case TutorialKey.Aim:
			return table[0][num];
		case TutorialKey.Cursor:
			return table[1][num];
		case TutorialKey.SwitchItem:
			return table[2][num];
		default:
			return null;
		}
	}

	private static readonly string[][] Words = new string[][]
	{
		// Aim
		new string[5] { "the mouse", "der Maus", "il mouse", "la souris", "el raton" },
		// Cursor
		new string[5] { "the cursor", "der Cursor", "il cursore", "le curseur", "el cursor" },
		// SwitchItem
		new string[5] { "the mouse wheel", "dem Mausrad", "la rotellina del mouse", "la molette de la souris", "la rueda del raton" }
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
	/// Rewrites the glyphs in one tutorial step, or returns the text unchanged.
	///
	/// <paramref name="step"/> is the <see cref="Tutorial"/> value the text belongs to. A step with
	/// no rules - or a build where keyboard support is off - keeps the console text, which is
	/// correct for a pad.
	/// </summary>
	internal static string Translate(string text, int step)
	{
		if (string.IsNullOrEmpty(text) || !Desktop)
		{
			return text;
		}
		int num = LanguageIndex();
		int num2 = IndexOfOverride(step);
		if (num2 >= 0)
		{
			return Overrides[num2][num];
		}
		string result = text;
		// A glyph that stands at the end of a sentence after a space is the "…, C." pattern:
		// "easiest in the manual cursor mode, C". Two of the translations omit the comma, which
		// reads as "mode C" - a key glued to the last word. Adding it here is language-neutral,
		// because a comma before a key name is correct in all five.
		// The already-comma'd form is normalised first, so the second replacement cannot double it.
		string text3 = ", " + (char)0x8f + ".";
		if (result.IndexOf(text3, StringComparison.Ordinal) >= 0)
		{
			result = result.Replace(text3, ", " + (Label(TutorialKey.CursorMode) ?? "C") + ".");
		}
		text3 = " " + (char)0x8f + ".";
		if (result.IndexOf(text3, StringComparison.Ordinal) >= 0)
		{
			result = result.Replace(text3, ", " + (Label(TutorialKey.CursorMode) ?? "C") + ".");
		}
		for (int i = 0; i < Steps.Length; i++)
		{
			if (Steps[i][0] != step)
			{
				continue;
			}
			char c = (char)Steps[i][1];
			if (result.IndexOf(c) < 0)
			{
				continue;
			}
			TutorialKey tutorialKey = (TutorialKey)Steps[i][2];
			string text2 = Label(tutorialKey) ?? Word(tutorialKey, num + 1);
			if (text2 != null)
			{
				result = result.Replace(c.ToString(), text2);
			}
		}
		return result;
	}

	/// <summary>Index into the per-language arrays, from the player's language.</summary>
	private static int LanguageIndex()
	{
		// Lang.LangOption is 1 = English, 2 = German, 3 = Italian, 4 = French, 5 = Spanish.
		int langOption = Lang.LangOption;
		return (langOption >= 1 && langOption <= 5) ? (langOption - 1) : 0;
	}

	private static int IndexOfOverride(int step)
	{
		for (int i = 0; i < OverrideSteps.Length; i++)
		{
			if (OverrideSteps[i] == step)
			{
				return i;
			}
		}
		return -1;
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

	/// <summary>How many step rules there are. For the test and the log.</summary>
	internal static int RuleCount => Steps.Length;
}
