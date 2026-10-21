using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terraria;

/// <summary>
/// Translation overrides from <c>Storage/Lang/&lt;code&gt;.txt</c>.
///
/// Why: every string lives in <see cref="Lang"/>, a 17 000-line C# file with five translations
/// baked in. Fixing a typo in the German text, or adding a language the console never shipped,
/// meant editing code and rebuilding. That is a barrier for exactly the people who would do the
/// work - translators - and it also means a player cannot fix a broken label themselves.
///
/// Layering: <see cref="Lang.SetLang"/> fills the tables as before; these files are applied on
/// top of that. A file only overrides what it lists, so a partial translation is valid and the
/// unchanged strings stay in the language the player picked. Nothing here can leave a table
/// empty: an entry is only applied when it exists and is not blank.
///
/// Format is INI-ish, on purpose - it reuses the settings parser the project already has:
/// <code>
/// [MenuText]
/// 6 = Play Online
/// [MiscText]
/// 8 = The Blood Moon is rising...
/// </code>
/// Sections are the table names: <c>MiscText</c>, <c>MenuText</c>, <c>WorldGenText</c>,
/// <c>InterfaceText</c>, <c>TipText</c>, <c>DeathText</c>. Keys are indices into that table.
/// Escapes: <c>\n</c>, <c>\t</c>, <c>\\</c> (some strings, like the save-icon hint, are
/// multi-line).
/// </summary>
internal static class LangOverrides
{
	/// <summary>Number of strings overridden by the last <see cref="Apply"/>. For tests and logs.</summary>
	public static int AppliedCount { get; private set; }

	/// <summary>
	/// Lines that could not be used: unknown section, index out of range, wrong file header.
	/// Reported rather than silently dropped - a translator whose file "does not work" needs to
	/// know which line is at fault.
	/// </summary>
	public static IList<string> Problems => new List<string>(_problems);

	/// <summary>
	/// When set, <see cref="Apply"/> does nothing but clear the translation state.
	///
	/// The self-test needs it: it switches the language to check the built-in tables, and a
	/// player who has their own override file would otherwise leak into the result - a file
	/// holding English text for German would make the "translations differ" check fail on a
	/// perfectly healthy install.
	/// </summary>
	internal static bool SuppressFiles;

	private static readonly List<string> _problems = new List<string>();

	/// <summary>
	/// Section holding this port's own text, keyed by name instead of by index. Same file as
	/// the game's strings: one file per language, everything a translator touches in one place.
	/// </summary>
	public const string TextsSection = "Texts";

	private static readonly string[] Sections = new string[6]
	{
		"MiscText",
		"MenuText",
		"WorldGenText",
		"InterfaceText",
		"TipText",
		"DeathText"
	};

	/// <summary>Path of the override file for one language option.</summary>
	public static string FileFor(int languageOption)
	{
		return Path.Combine(Terraria.Storage.AppPaths.Lang, Code(languageOption) + ".txt");
	}

	/// <summary>Two-letter code used in the file name and by <c>Lang.LanguageId</c>.</summary>
	public static string Code(int languageOption)
	{
		switch (languageOption)
		{
		case 2:
			return "de";
		case 3:
			return "it";
		case 4:
			return "fr";
		case 5:
			return "es";
		default:
			return "en";
		}
	}

	/// <summary>
	/// Inverse of <see cref="Code"/>: "de" or "2" both mean German, "auto" or "0" mean the
	/// system language. Used by the <c>--lang-export</c> command line.
	/// </summary>
	public static bool TryParse(string text, out int languageOption)
	{
		languageOption = 0;
		string text2 = (text ?? string.Empty).Trim().ToLowerInvariant();
		switch (text2)
		{
		case "":
		case "auto":
		case "0":
			languageOption = 0;
			return true;
		case "en":
		case "english":
		case "1":
			languageOption = 1;
			return true;
		case "de":
		case "german":
		case "2":
			languageOption = 2;
			return true;
		case "it":
		case "italian":
		case "3":
			languageOption = 3;
			return true;
		case "fr":
		case "french":
		case "4":
			languageOption = 4;
			return true;
		case "es":
		case "spanish":
		case "5":
			languageOption = 5;
			return true;
		default:
			return false;
		}
	}

	/// <summary>
	/// Applies the overrides for the language currently in use. Safe to call after every
	/// <see cref="Lang.SetLang"/>: it resets its counters and only touches listed entries.
	/// </summary>
	public static void Apply()
	{
		AppliedCount = 0;
		lock (_problems)
		{
			_problems.Clear();
		}
		string path = FileFor(Lang.LangOption);
		// Translations of this port's own text start from a clean slate on every language
		// change, exactly like the built-in tables SetLang refills. Without this, switching
		// from German to French would leave the German chat replies in place.
		Texts.ClearOverrides();
		if (SuppressFiles)
		{
			return;
		}
		try
		{
			if (!File.Exists(path))
			{
				return;
			}
			Settings settings = new Settings(path);
			try
			{
				// Unknown sections are almost always a typo ("MenuTexts"), and reporting them is
				// the difference between a translator fixing their file and giving up.
				List<string> list = settings.SectionNames();
				for (int k = 0; k < list.Count; k++)
				{
					string text = list[k];
					if (text.Length == 0 || Array.IndexOf(Sections, text) >= 0 || text == TextsSection)
					{
						continue;
					}
					Problem("unknown section [" + text + "]");
				}
				for (int i = 0; i < Sections.Length; i++)
				{
					ApplySection(settings, Sections[i]);
				}
				ApplyTexts(settings);
				if (AppliedCount > 0)
				{
					Program.LogInfo("Lang", "applied " + AppliedCount + " override(s) from " + Path.GetFileName(path));
				}
				if (_problems.Count > 0)
				{
					for (int j = 0; j < _problems.Count; j++)
					{
						Program.LogWarn("Lang", Path.GetFileName(path) + ": " + _problems[j]);
					}
				}
			}
			finally
			{
				// Not Close(): that rewrites the file, and this one belongs to the translator.
				settings.Discard();
			}
		}
		catch (Exception ex)
		{
			Program.LogWarn("Lang", "could not read " + Path.GetFileName(path) + ": " + ex.Message);
		}
	}

	private static void ApplySection(Settings settings, string section)
	{
		string[] table = Table(section);
		if (table == null)
		{
			return;
		}
		// Iterate the file, not the table: an entry with an index the table does not have must be
		// reported. Iterating the table would silently drop it, which looks like "my file does
		// nothing" and is impossible to debug from the player's side.
		List<string> list = settings.KeysIn(section);
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			int num;
			if (!int.TryParse(text.Trim(), out num))
			{
				Problem(section + ": '" + text + "' is not an index");
				continue;
			}
			if (num < 0 || num >= table.Length)
			{
				Problem(section + ": index " + num + " is outside 0.." + (table.Length - 1));
				continue;
			}
			string text2 = Value(settings, section, text);
			if (string.IsNullOrEmpty(text2))
			{
				// Leaving the built-in text is better than blanking a label.
				Problem(section + "[" + num + "]: empty value, ignored");
				continue;
			}
			table[num] = text2;
			AppliedCount++;
		}
	}

	private static void Problem(string message)
	{
		lock (_problems)
		{
			// A file with a systematic mistake (every key off by one) would otherwise produce
			// hundreds of identical lines.
			if (_problems.Count < 40)
			{
				_problems.Add(message);
			}
		}
	}

	/// <summary>
	/// One entry's text, with the single conventional space after '=' removed.
	///
	/// Exactly one, because some of these strings genuinely begin with a space - " has awoken!"
	/// is concatenated behind a boss name - and stripping all leading whitespace would quietly
	/// glue the words together. A translator who writes two spaces meant one.
	/// </summary>
	private static string Value(Settings settings, string section, string key)
	{
		string text = settings.Get(section, key);
		if (text.Length > 0 && (text[0] == ' ' || text[0] == '\t'))
		{
			return Unescape(text.Substring(1));
		}
		return Unescape(text);
	}

	private static string[] Table(string section)
	{
		switch (section)
		{
		case "MiscText":
			return Lang.MiscText;
		case "MenuText":
			return Lang.MenuText;
		case "WorldGenText":
			return Lang.WorldGenText;
		case "InterfaceText":
			return Lang.InterfaceText;
		case "TipText":
			return Lang.TipText;
		case "DeathText":
			return Lang.DeathText;
		default:
			return null;
		}
	}

	/// <summary>
	/// Looks a named entry up, ignoring the spaces around the key.
	///
	/// The settings parser keeps everything left of the first '=' verbatim, so a hand-written
	/// line <c>chat.host.help = ...</c> produces the key <c>"chat.host.help "</c> with a trailing
	/// space. Numeric keys survived that by being parsed as numbers; named ones did not, and
	/// every entry came back as "unknown key". Trimming here keeps the file readable.
	/// </summary>
	private static string NamedValue(Settings settings, string section, string key)
	{
		List<string> list = settings.KeysIn(section);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
			{
				return Value(settings, section, list[i]);
			}
		}
		return null;
	}

	/// <summary>
	/// Applies the <c>[Texts]</c> section: named keys instead of indices, because these are
	/// strings this port invented and their indices would mean nothing to a translator.
	/// </summary>
	private static void ApplyTexts(Settings settings)
	{
		List<string> list = settings.KeysIn(TextsSection);
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i].Trim();
			string text2 = Value(settings, TextsSection, list[i]);
			if (string.IsNullOrEmpty(text2))
			{
				Problem(TextsSection + ": '" + text + "' has an empty value, ignored");
				continue;
			}
			if (!Texts.SetOverride(text, text2))
			{
				// A typo here is invisible in the game: the English text simply stays. Naming the
				// key is the only way the translator finds it.
				Problem(TextsSection + ": unknown key '" + text + "'");
				continue;
			}
			// A translation that drops a placeholder silently loses the player's name or an item
			// id. Reported, but still applied: a human may have reworded the sentence, and
			// refusing the line would leave the English text with no explanation.
			List<int> list2 = Texts.Placeholders(Texts.Default(text));
			List<int> list3 = Texts.Placeholders(text2);
			for (int j = 0; j < list2.Count; j++)
			{
				if (!list3.Contains(list2[j]))
				{
					Problem(TextsSection + ": '" + text + "' is missing the placeholder {" + list2[j] + "}");
					break;
				}
			}
			AppliedCount++;
		}
	}

	/// <summary>
	/// Turns the two-character escapes into real characters. Unknown escapes are kept.
	/// </summary>
	internal static string Unescape(string text)
	{
		if (string.IsNullOrEmpty(text) || text.IndexOf('\\') < 0)
		{
			return text;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] != '\\' || i + 1 >= text.Length)
			{
				stringBuilder.Append(text[i]);
				continue;
			}
			char c = text[++i];
			switch (c)
			{
			case 'n':
				stringBuilder.Append('\n');
				break;
			case 't':
				stringBuilder.Append('\t');
				break;
			case 'r':
				stringBuilder.Append('\r');
				break;
			case '\\':
				stringBuilder.Append('\\');
				break;
			default:
				// Not an escape we know: keep it verbatim so a Windows path or a stray backslash
				// in a language file is not silently mangled.
				stringBuilder.Append('\\').Append(c);
				break;
			}
		}
		return stringBuilder.ToString();
	}

	/// <summary>Escapes text for the file format: the inverse of <see cref="Unescape"/>.</summary>
	internal static string Escape(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length + 8);
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			switch (c)
			{
			case '\\':
				stringBuilder.Append("\\\\");
				break;
			case '\n':
				stringBuilder.Append("\\n");
				break;
			case '\r':
				stringBuilder.Append("\\r");
				break;
			case '\t':
				stringBuilder.Append("\\t");
				break;
			default:
				// '=' needs no escaping: the loader keeps everything after the first '=', so
				// escaping it would only round-trip wrong. ';' is fine too - the parser only
				// treats it as a comment marker on a line that has no '=' at all.
				stringBuilder.Append(c);
				break;
			}
		}
		return stringBuilder.ToString();
	}

	/// <summary>Reads the file a refresh is about to overwrite, or null when there is none.</summary>
	private static Settings ReadExisting(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				return null;
			}
			return new Settings(path);
		}
		catch (Exception ex)
		{
			// A file we cannot read is a reason to fall back to built-in text, not to refuse
			// the export: that would leave the translator with nothing.
			Program.LogWarn("Lang", "could not read the existing " + Path.GetFileName(path) + ": " + ex.Message);
			return null;
		}
	}

	/// <summary>
	/// Writes the current strings of a language as an editable template, so a translator starts
	/// from real text instead of from an index list. Returns the path, or null with a reason.
	/// </summary>
	public static string WriteTemplate(int languageOption, out string error)
	{
		error = null;
		string path = FileFor(languageOption);
		try
		{
			if (!Directory.Exists(Terraria.Storage.AppPaths.Lang))
			{
				Directory.CreateDirectory(Terraria.Storage.AppPaths.Lang);
			}
			// Fill the tables for that language first, otherwise the template would contain the
			// strings of whatever language happened to be active. SetLang does nothing when the
			// option is already current - including the case where nothing was ever filled and
			// both values are 0 - so Auto needs an explicit resolve.
			if (languageOption == 0)
			{
				if (Lang.LangOption == 0)
				{
					Lang.SetSystemLang();
				}
			}
			else if (languageOption != Lang.LangOption)
			{
				Lang.SetLang(languageOption);
			}
			if (!Language.TablesFilled())
			{
				error = "the string tables are empty - the game did not initialise the language";
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("; Translation overrides for " + Code(languageOption) + ".");
			stringBuilder.AppendLine("; Only listed entries are replaced; delete a line to fall back to the built-in text.");
			stringBuilder.AppendLine("; Escapes: \\n newline, \\t tab, \\\\ backslash.");
			stringBuilder.AppendLine("; To keep a line you already translated, leave it as it is - exporting again");
			stringBuilder.AppendLine("; preserves whatever this file already had.");
			// Read the existing file first. Export is a refresh, not a reset: overwriting a
			// translator's work with built-in English because they wanted a new key added would
			// make the tool dangerous to use twice.
			Settings existing = ReadExisting(path);
			try
			{
				for (int i = 0; i < Sections.Length; i++)
				{
					string[] array = Table(Sections[i]);
					stringBuilder.AppendLine();
					stringBuilder.Append('[').Append(Sections[i]).AppendLine("]");
					for (int j = 0; j < array.Length; j++)
					{
						string text = array[j];
						string text2 = ((existing != null && existing.HasKey(Sections[i], j.ToString()))
							? Unescape(existing.Get(Sections[i], j.ToString()))
							: text);
						if (string.IsNullOrEmpty(text2) && string.IsNullOrEmpty(text))
						{
							continue;
						}
						stringBuilder.Append(j).Append(" = ").AppendLine(Escape(text2));
					}
				}
				stringBuilder.AppendLine();
				stringBuilder.Append('[').Append(TextsSection).AppendLine("]");
				stringBuilder.AppendLine("; Text this port added itself: chat replies and screen labels.");
				stringBuilder.AppendLine("; {0}, {1}... are filled by the game; keep them in your translation.");
				IList<string> keys = Texts.Keys;
				for (int k = 0; k < keys.Count; k++)
				{
					string text3 = keys[k];
					// The existing file wins, so a refresh keeps what was already translated;
					// the built-in English is only the starting point for a new entry.
					string text4 = (existing != null) ? NamedValue(existing, TextsSection, text3) : null;
					if (string.IsNullOrEmpty(text4))
					{
						text4 = Texts.Get(text3);
					}
					stringBuilder.Append(text3).Append(" = ").AppendLine(Escape(text4));
				}
			}
			finally
			{
				if (existing != null)
				{
					existing.Discard();
				}
			}
			Directory.CreateDirectory(Terraria.Storage.AppPaths.Lang);
			File.WriteAllText(path, stringBuilder.ToString(), Encoding.UTF8);
			return path;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return null;
		}
	}
}
