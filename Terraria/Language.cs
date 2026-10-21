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

	/// <summary>Highest value <see cref="Lang.SetLang"/> understands.</summary>
	public const int Max = 5;

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
	/// Applies the choice read at startup. Called from <c>LoadContent</c>, which runs after the
	/// constructor has parsed the settings, so the culture-based default is already installed
	/// (the rating-logo lookup depends on that order).
	/// </summary>
	public static void ApplySaved()
	{
		// Auto means "the system already decided" - applying 0 here would call SetLang(0), which
		// Lang treats as English, overriding a German system.
		if (_saved <= Auto || _saved == Lang.LangOption)
		{
			return;
		}
		Lang.SetLang(_saved);
		_saved = Lang.LangOption;
	}

	public static void Save(Settings settings)
	{
		settings.Update("Display", "Language", _choice.ToString(), "0 = follow the system, 1 = English, 2 = Deutsch, 3 = Italiano, 4 = Français, 5 = Español.");
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
		Lang.SetLang(effective);
		_choice = choice;
		_saved = choice;
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
