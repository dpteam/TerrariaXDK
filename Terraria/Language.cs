using System;

namespace Terraria;

/// <summary>
/// The game's language, chosen by the player.
///
/// <see cref="Lang"/> has carried five translations all along, but nothing could select one:
/// <see cref="Lang.SetSystemLang"/> picked it from the OS culture and that was final. A player
/// whose Windows is in English but who wants German - or the other way round - had no way to say
/// so, and "язык" sat in the settings roadmap for exactly that reason.
///
/// Two values are kept apart on purpose:
/// <list type="bullet">
/// <item><b>the choice</b>, 0..5, where 0 means "follow the system" - this is what the setting
/// stores and what the menu cycles;</item>
/// <item><b>the effective language</b>, <see cref="Lang.LangOption"/>, which is never 0 after
/// startup because <see cref="Lang.SetSystemLang"/> resolves Auto into a concrete language.</item>
/// </list>
/// Keeping them separate is what lets the menu still say "Auto" after you pick it. Reading
/// <see cref="Lang.LangOption"/> directly would make Auto indistinguishable from a language.
/// </summary>
internal static class Language
{
	/// <summary>Follow the operating system's culture.</summary>
	public const int Auto = 0;

	/// <summary>
	/// A language the build has no compiled tables for, described entirely by
	/// <c>Storage/Lang/&lt;code&gt;.txt</c> and drawn with <c>Storage/Fonts/&lt;code&gt;.*</c>.
	///
	/// This is what makes "add a language without recompiling" real. The five shipped
	/// translations are compiled into <see cref="Lang"/> with their glyphs baked into the content
	/// atlases, so a sixth could only be added by editing a 17 000-line file and shipping new
	/// artwork. A custom language instead starts from English (the fallback for any string its
	/// file does not cover) and replaces what the file does cover.
	/// </summary>
	public const int Custom = 6;

	/// <summary>Highest value <see cref="Lang.SetLang"/> understands.</summary>
	public const int Max = 6;

	/// <summary>
	/// Two-letter code of the custom language, from <c>[Display] CustomLanguage</c>. Empty means
	/// the feature is not configured, and <see cref="Custom"/> then behaves like English.
	/// </summary>
	public static string CustomCode { get; private set; } = string.Empty;

	private static int _choice;

	private static int _saved = -1;

	/// <summary>The stored choice: 0 = Auto, 1..5 = a concrete language.</summary>
	public static int Choice => _choice;

	/// <summary>The language actually in use, as <see cref="Lang"/> sees it.</summary>
	public static int Effective => Lang.LangOption;

	/// <summary>Name to show in the menu, in its own language.</summary>
	public static string Label(int option)
	{
		switch (option)
		{
		case 1:
			return "English";
		case 2:
			return "Deutsch";
		case 3:
			return "Italiano";
		case 4:
			return "Français";
		case 5:
			return "Español";
		case Custom:
			// The code, not a translated name: the code is what the player typed into the
			// settings file, so it is the one thing they can recognise as theirs.
			return string.IsNullOrEmpty(CustomCode) ? "Custom" : ("Custom (" + CustomCode + ")");
		default:
			return "Auto";
		}
	}

	/// <summary>The label for the current choice.</summary>
	public static string CurrentLabel => Label(_choice);

	/// <summary>
	/// Parses the stored value. Anything unreadable or out of range falls back to
	/// <see cref="Auto"/> - an invalid language must not leave the game with empty strings, and
	/// silently picking English would hide the mistake.
	/// </summary>
	internal static int Parse(string text)
	{
		int result;
		if (!int.TryParse((text ?? string.Empty).Trim(), out result))
		{
			return Auto;
		}
		return (result < 0 || result > Max) ? Auto : result;
	}

	/// <summary>
	/// Reads the setting and installs the strings. Called from the constructor, where the
	/// culture-based default is established; <see cref="ApplySaved"/> finishes the job in
	/// LoadContent.
	/// </summary>
	internal static void Load(Settings settings)
	{
		CustomCode = SanitizeCode(settings.Get("Display", "CustomLanguage"));
		int num = Parse(settings.Get("Display", "Language"));
		_choice = num;
		_saved = num;
		if (num == Auto)
		{
			// Same path as before: culture decides.
			Lang.SetSystemLang();
			return;
		}
		Lang.SetLang(num);
	}

	/// <summary>
	/// A language code is used as a file name, so it is filtered to letters, digits and dashes.
	/// Accepting a path separator here would let the setting point at any file on the disk.
	/// </summary>
	internal static string SanitizeCode(string text)
	{
		string text2 = (text ?? string.Empty).Trim().ToLowerInvariant();
		System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
		for (int i = 0; i < text2.Length && stringBuilder.Length < 12; i++)
		{
			char c = text2[i];
			if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_')
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}

	/// <summary>
	/// Applies the choice read at startup. Called from <c>LoadContent</c>, which runs after the
	/// constructor has parsed the settings, so the culture-based default is already installed
	/// (the rating-logo lookup depends on that order).
	/// </summary>
	public static void ApplySaved()
	{
		// Auto means "the system already decided" - applying 0 here would call SetLang(0), which
		// Lang treats as English, overriding a German system.
		if (_saved > Auto && _saved != Lang.LangOption)
		{
			Lang.SetLang(_saved);
			_saved = Lang.LangOption;
		}
		// Overrides go on top of whatever SetLang just filled, including the Auto case: a
		// translator's file should apply whether the language came from the system or the menu.
		LangOverrides.Apply();
	}

	public static void Save(Settings settings)
	{
		settings.Update("Display", "Language", _choice.ToString(), "0 = follow the system, 1 = English, 2 = Deutsch, 3 = Italiano, 4 = Français, 5 = Español, 6 = custom (see CustomLanguage).");
		settings.Update("Display", "CustomLanguage", CustomCode, "Language code for option 6: Storage/Lang/<code>.txt supplies the text,\n; Storage/Fonts/<code>.* supplies the glyphs. Made by tools/FontAtlas.");
	}

	/// <summary>
	/// Applies a choice and remembers it. Returns false for an out-of-range option, so the menu
	/// cannot put the game into a state the loader would reject.
	/// </summary>
	public static bool Apply(int option, out string error)
	{
		error = null;
		if (option < Auto || option > Max)
		{
			error = "Unknown language " + option + ".";
			return false;
		}
		_choice = option;
		if (option == Auto)
		{
			Lang.SetSystemLang();
			_saved = Lang.LangOption;
		}
		else
		{
			Lang.SetLang(option);
			_saved = option;
		}
		// SetLang refilled the built-in text; the override file for this language goes on top.
		LangOverrides.Apply();
		Persist();
		return true;
	}

	/// <summary>Next/previous option, wrapping. Covers Auto..Max inclusive.</summary>
	public static int Next(int option, int direction)
	{
		int num = option + direction;
		if (num < Auto)
		{
			num = Max;
		}
		else if (num > Max)
		{
			num = Auto;
		}
		return num;
	}

	private static void Persist()
	{
		try
		{
			Settings settings = new Settings(Terraria.Storage.AppPaths.SettingsFile);
			Save(settings);
			settings.Close();
		}
		catch (Exception ex)
		{
			Program.LogWarn("Language", "could not save the choice: " + ex.Message);
		}
	}

	/// <summary>Restores the tables after the language check. Test hook, not used by the game.</summary>
	internal static void Restore(int choice, int effective)
	{
		// SetLang only refills the tables when the option changes, so a test that tainted one
		// string would otherwise leave that text behind for the running session. Dropping the
		// option to 0 first forces the refill whatever the effective value was.
		Lang.LangOption = 0;
		if (effective <= Auto)
		{
			Lang.SetSystemLang();
		}
		else
		{
			Lang.SetLang(effective);
		}
		_choice = choice;
		_saved = choice;
		// A player who really has a translation file must still have it applied afterwards.
		LangOverrides.Apply();
	}

	/// <summary>
	/// True when the string tables actually hold text. A table that was never filled would leave
	/// the whole UI blank, and <see cref="Lang.SetLang"/> only refills when the option changes.
	/// </summary>
	internal static bool TablesFilled()
	{
		try
		{
			for (int i = 0; i < Lang.MenuText.Length; i++)
			{
				if (!string.IsNullOrEmpty(Lang.MenuText[i]))
				{
					return !string.IsNullOrEmpty(Lang.MiscText[0]);
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}
}
