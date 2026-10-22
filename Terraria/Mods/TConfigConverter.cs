using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Terraria.Mods;

/// <summary>
/// Turns a tConfig pack into a mod project.
///
/// A tConfig pack is a folder of INI files read at startup by <see cref="TConfigImporter"/>.
/// That is enough to change numbers, but it is not a mod: it has no name in the mod list, it
/// takes no part in the multiplayer signature, and a host cannot require it. Converting the pack
/// into a project gives exactly those three things, and gives the author a place to add code.
///
/// The output is a normal project, not a one-way export: build it, edit it, keep editing it.
/// </summary>
internal static class TConfigConverter
{
	internal sealed class Result
	{
		public string OutputDirectory;

		public string ProjectFile;

		public string SourceFile;

		public int Items;

		public int Fields;

		public int UnknownItems;

		public int UnsupportedFields;

		/// <summary>Files that could not be read or produced no usable output. Not fatal.</summary>
		public int FailedFiles;

		/// <summary>
		/// Items the pack defines from scratch (<c>type=-1</c>). These are converted through
		/// <c>Context.DefineItem</c>, not <c>Context.Item</c>: there is no existing item to tweak.
		/// </summary>
		public int NewItems;

		/// <summary>
		/// New items that could not be converted, almost always because the pack has no sprite for
		/// them. Reported separately from <see cref="NewItems"/> so "converted" means converted.
		/// </summary>
		public int NewItemsSkipped;

		/// <summary>Sprite files copied into the project, one per new item.</summary>
		public int SpritesCopied;

		/// <summary>
		/// Things the player should look at: dropped fields, unknown names, and files that threw.
		/// Kept apart from <see cref="Errors"/> by severity, not by source.
		/// </summary>
		public readonly List<string> Warnings = new List<string>();

		/// <summary>
		/// Failures that were survived. A pack with one broken file still converts: refusing the
		/// whole pack because of one typo is worse than converting the rest and saying what was
		/// skipped - the player would have no way to find the typo from "it did not work".
		/// </summary>
		public readonly List<string> Errors = new List<string>();

		/// <summary>Path of the log written for this run, or empty when logging failed too.</summary>
		public string LogFile = string.Empty;

		public bool HasProblems => Errors.Count > 0 || Warnings.Count > 0;
	}

	/// <summary>
	/// Where a conversion writes its log. One file per run is not enough to debug an old pack, so
	/// the file is appended to and rotated at a size limit - the same rule the game log follows.
	/// </summary>
	internal static string LogPath => LogPathOverride ?? Path.Combine(Terraria.Storage.AppPaths.Logs, "tconfig-convert.log");

	/// <summary>Test hook: keeps a self-test from writing into the player's own log.</summary>
	internal static string LogPathOverride;

	private const long MaxLogBytes = 1024 * 1024;

	/// <summary>
	/// Reads <paramref name="packDirectory"/> and writes a project into
	/// <paramref name="outputDirectory"/>. Returns null when the pack has nothing to convert;
	/// the reason is in <paramref name="error"/>.
	///
	/// Never throws. A pack is somebody else's data: a truncated INI, a name that is not an item
	/// in this build, a file that is locked by an editor - each of those is a thing to report,
	/// not a reason to bring the menu down. Everything that goes wrong lands in
	/// <see cref="Result.Errors"/>, in the generated README, and in a log file.
	/// </summary>
	internal static Result Convert(string packDirectory, string outputDirectory, string modName, out string error)
	{
		error = null;
		Result result = new Result();
		try
		{
			return ConvertCore(packDirectory, outputDirectory, modName, result, out error);
		}
		catch (Exception ex)
		{
			// The last line of defence. Everything below is wrapped per file, so reaching here
			// means something unexpected - and it is still better to say so than to crash.
			error = "unexpected failure: " + ex.GetType().Name + ": " + ex.Message;
			result.Errors.Add(error);
			WriteLog(packDirectory, result, error);
			return null;
		}
	}

	private static Result ConvertCore(string packDirectory, string outputDirectory, string modName, Result result, out string error)
	{
		error = null;
		if (string.IsNullOrEmpty(packDirectory) || !Directory.Exists(packDirectory))
		{
			error = "no such pack folder: " + packDirectory;
			WriteLog(packDirectory, result, error);
			return null;
		}
		if (string.IsNullOrEmpty(modName))
		{
			// The folder name is the pack's name everywhere else, so it is the honest default.
			modName = new DirectoryInfo(Path.GetFullPath(packDirectory)).Name;
		}
		modName = Identifier(modName, upperFirst: true);
		if (modName.Length == 0)
		{
			error = "the pack folder name has no usable characters for a mod name";
			WriteLog(packDirectory, result, error);
			return null;
		}
		string text = Path.Combine(packDirectory, "Item");
		List<string> list = new List<string>();
		if (Directory.Exists(text))
		{
			try
			{
				string[] files = Directory.GetFiles(text, "*.ini", SearchOption.TopDirectoryOnly);
				Array.Sort(files, StringComparer.OrdinalIgnoreCase);
				list.AddRange(files);
			}
			catch (Exception ex2)
			{
				error = "cannot list " + text + ": " + ex2.Message;
				result.Errors.Add(error);
				WriteLog(packDirectory, result, error);
				return null;
			}
		}
		if (list.Count == 0)
		{
			error = "no Item/*.ini files in " + packDirectory;
			WriteLog(packDirectory, result, error);
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);
		foreach (string item in list)
		{
			try
			{
				AppendItem(stringBuilder, item, packDirectory, outputDirectory, used, result);
			}
			catch (Exception ex3)
			{
				// One file, one failure, and the rest of the pack still converts.
				result.FailedFiles++;
				string text2 = Path.GetFileName(item) + ": " + ex3.GetType().Name + ": " + ex3.Message;
				result.Errors.Add(text2);
			}
		}
		if (result.Items == 0)
		{
			if (result.NewItemsSkipped > 0)
			{
				error = "this pack defines " + result.NewItemsSkipped.ToString(CultureInfo.InvariantCulture)
					+ " new item(s) but ships no sprite for them, and an item that cannot be drawn is not converted";
			}
			else
			{
				error = "the pack names no item this build knows ("
					+ result.UnknownItems.ToString(CultureInfo.InvariantCulture) + " unknown"
					+ (result.FailedFiles == 0 ? "" : ", " + result.FailedFiles.ToString(CultureInfo.InvariantCulture) + " failed")
					+ ")";
			}
			WriteLog(packDirectory, result, error);
			return null;
		}
		string text3 = Path.Combine(outputDirectory, modName + ".cs");
		string text4 = Path.Combine(outputDirectory, modName + ".csproj");
		string text5 = Path.Combine(outputDirectory, "README.md");
		try
		{
			Directory.CreateDirectory(outputDirectory);
			File.WriteAllText(text3, Source(modName, packDirectory, stringBuilder.ToString()), new UTF8Encoding(false));
			File.WriteAllText(text4, Project(modName, GameAssemblyPath()), new UTF8Encoding(false));
			File.WriteAllText(text5, Readme(modName, packDirectory, result), new UTF8Encoding(false));
		}
		catch (Exception ex4)
		{
			error = "cannot write to " + outputDirectory + ": " + ex4.Message;
			result.Errors.Add(error);
			WriteLog(packDirectory, result, error);
			return null;
		}
		result.OutputDirectory = outputDirectory;
		result.ProjectFile = text4;
		result.SourceFile = text3;
		result.LogFile = WriteLog(packDirectory, result, null);
		return result;
	}

	/// <summary>
	/// Appends one run to the conversion log and returns its path (empty when even that failed).
	///
	/// The log is the answer to "the converter said something was wrong, what exactly": the status
	/// line in the menu has room for one sentence, and a pack with thirty dropped fields needs
	/// thirty lines. Failing to write the log must not fail the conversion - the project is
	/// already written and more useful than a log.
	/// </summary>
	private static string WriteLog(string packDirectory, Result result, string fatal)
	{
		try
		{
			string text = LogPath;
			string directoryName = Path.GetDirectoryName(text);
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			// Rotate rather than grow without bound: a player converting repeatedly would
			// otherwise accumulate a file nobody reads.
			try
			{
				FileInfo fileInfo = new FileInfo(text);
				if (fileInfo.Exists && fileInfo.Length > MaxLogBytes)
				{
					string text2 = text + ".1";
					if (File.Exists(text2))
					{
						File.Delete(text2);
					}
					File.Move(text, text2);
				}
			}
			catch
			{
				// Rotation is housekeeping; if it fails, keep appending.
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("=== ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
				.Append(" ===\n");
			stringBuilder.Append("pack: ").Append(packDirectory ?? "(none)").Append('\n');
			if (fatal != null)
			{
				stringBuilder.Append("failed: ").Append(fatal).Append('\n');
			}
			else
			{
				stringBuilder.Append("result: ").Append(result.Items.ToString(CultureInfo.InvariantCulture)).Append(" item(s), ")
					.Append(result.Fields.ToString(CultureInfo.InvariantCulture)).Append(" field(s), ")
					.Append(result.UnknownItems.ToString(CultureInfo.InvariantCulture)).Append(" unknown, ")
					.Append(result.UnsupportedFields.ToString(CultureInfo.InvariantCulture)).Append(" dropped, ")
					.Append(result.FailedFiles.ToString(CultureInfo.InvariantCulture)).Append(" failed file(s)\n");
				stringBuilder.Append("output: ").Append(result.OutputDirectory ?? "(not written)").Append('\n');
			}
			if (result.Errors.Count > 0)
			{
				stringBuilder.Append("errors:\n");
				for (int i = 0; i < result.Errors.Count; i++)
				{
					stringBuilder.Append("  ! ").Append(result.Errors[i]).Append('\n');
				}
			}
			if (result.Warnings.Count > 0)
			{
				stringBuilder.Append("warnings:\n");
				for (int j = 0; j < result.Warnings.Count; j++)
				{
					stringBuilder.Append("  - ").Append(result.Warnings[j]).Append('\n');
				}
			}
			stringBuilder.Append('\n');
			File.AppendAllText(text, stringBuilder.ToString(), new UTF8Encoding(false));
			return text;
		}
		catch
		{
			return string.Empty;
		}
	}

	private static void AppendItem(StringBuilder body, string iniPath, string packDirectory, string outputDirectory, HashSet<string> used, Result result)
	{
		string text = Path.GetFileNameWithoutExtension(iniPath);
		Item item = default(Item);
		// SetDefaults is not a lookup table: for a few items it reads the active player (the
		// familiar clothes take their colour from it), and it can throw when there is no UI yet.
		// That is a bug in those items, but it must not take a whole pack down with it.
		try
		{
			item.SetDefaults(text);
		}
		catch (Exception ex)
		{
			result.FailedFiles++;
			result.Errors.Add("'" + text + "': cannot create the item (" + ex.GetType().Name + ": " + ex.Message + ")");
			return;
		}
		if (item.Type <= 0)
		{
			// "Unknown item" and "this pack adds a new item" are different facts, and now they
			// lead to different code: one is a mistake in the pack, the other is a feature.
			if (DefinesNewItem(iniPath))
			{
				AppendNewItem(body, iniPath, packDirectory, outputDirectory, used, result);
				return;
			}
			result.UnknownItems++;
			result.Warnings.Add("'" + text + "': no such item in this build, skipped");
			return;
		}
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		string error;
		List<KeyValuePair<string, string>> lines = TConfigImporter.ReadIni(iniPath, out error);
		if (lines == null)
		{
			// A file the player asked to convert that could not be read is a failure to report,
			// not a file with nothing in it.
			result.FailedFiles++;
			result.Errors.Add("'" + text + "': " + Path.GetFileName(iniPath) + ": " + error);
			return;
		}
		try
		{
			foreach (KeyValuePair<string, string> line in lines)
			{
				// PropertyName is the second half of the check: ItemOverrides can accept a field
				// that the mod API has no property for (consumable is recorded as an error there),
				// and emitting a property that does not exist would produce a project that cannot
				// build.
				if (!ItemOverrides.IsSupportedField(line.Key) || PropertyName(line.Key) == null)
				{
					result.UnsupportedFields++;
					result.Warnings.Add("'" + text + "': field '" + line.Key + "' has no equivalent, dropped");
					continue;
				}
				// A value that cannot become a C# literal is quoted by Literal() and would make the
				// generated project fail to compile at that line. Catching it here is better: the
				// project builds, and the player is told which field was nonsense.
				if (!IsUsableValue(line.Key, line.Value))
				{
					result.UnsupportedFields++;
					result.Warnings.Add("'" + text + "': field '" + line.Key + "' has the value '"
						+ line.Value + "', which is not a number - dropped");
					continue;
				}
				list.Add(line);
			}
		}
		catch (Exception ex2)
		{
			result.FailedFiles++;
			result.Errors.Add("'" + text + "': cannot read " + Path.GetFileName(iniPath)
				+ " (" + ex2.GetType().Name + ": " + ex2.Message + ")");
			return;
		}
		if (list.Count == 0)
		{
			result.Warnings.Add("'" + text + "': nothing to convert");
			return;
		}
		string text2 = UniqueIdentifier(Identifier(text, upperFirst: false), used);
		body.Append("\t\t// ").Append(text).Append('\n');
		body.Append("\t\tItemOverride ").Append(text2).Append(" = Context.Item(").Append(Quote(text)).Append(");\n");
		body.Append("\t\tif (").Append(text2).Append(" != null)\n");
		body.Append("\t\t{\n");
		foreach (KeyValuePair<string, string> item2 in list)
		{
			body.Append("\t\t\t").Append(text2).Append('.').Append(PropertyName(item2.Key))
				.Append(" = ").Append(Literal(item2.Key, item2.Value)).Append(";\n");
			result.Fields++;
		}
		body.Append("\t\t}\n\n");
		result.Items++;
	}

	/// <summary>
	/// Emits a <c>Context.DefineItem</c> block for a new item and copies its sprite into the
	/// project.
	///
	/// The sprite is required: the item cannot be drawn without one, and an invisible item in the
	/// inventory is indistinguishable from a bug. A pack that defines an item but ships no PNG is
	/// reported rather than converted into something broken.
	/// </summary>
	private static void AppendNewItem(StringBuilder body, string iniPath, string packDirectory,
		string outputDirectory, HashSet<string> used, Result result)
	{
		string text = Path.GetFileNameWithoutExtension(iniPath);
		string spriteSource = Path.ChangeExtension(iniPath, ".png");
		if (!File.Exists(spriteSource))
		{
			result.NewItemsSkipped++;
			result.Warnings.Add("'" + text + "': new item with no sprite (" + Path.GetFileName(spriteSource)
				+ ") - an item that cannot be drawn would look like a bug, so it is not converted");
			return;
		}
		string error;
		List<KeyValuePair<string, string>> list = TConfigImporter.ReadIni(iniPath, out error);
		if (list == null)
		{
			result.FailedFiles++;
			result.Errors.Add("'" + text + "': " + Path.GetFileName(iniPath) + ": " + error);
			return;
		}
		// Copy the sprite next to the generated source, so the project is self-contained and does
		// not depend on the tConfig folder staying where it is.
		string text2 = Path.Combine("items", text + ".png");
		string text3 = Path.Combine(outputDirectory, text2);
		try
		{
			string directoryName = Path.GetDirectoryName(text3);
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.Copy(spriteSource, text3, overwrite: true);
			result.SpritesCopied++;
		}
		catch (Exception ex)
		{
			result.FailedFiles++;
			result.Errors.Add("'" + text + "': cannot copy the sprite (" + ex.GetType().Name + ": " + ex.Message + ")");
			return;
		}
		string text4 = UniqueIdentifier(Identifier(text, upperFirst: false), used);
		body.Append("\t\t// ").Append(text).Append(" (new item)\n");
		body.Append("\t\tModItem ").Append(text4).Append(" = Context.DefineItem(")
			.Append(Quote(text)).Append(", ").Append(Quote(text2.Replace('\\', '/'))).Append(");\n");
		body.Append("\t\tif (").Append(text4).Append(" != null)\n");
		body.Append("\t\t{\n");
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			KeyValuePair<string, string> keyValuePair = list[i];
			// "type" is the tConfig marker for "this is a new item"; it is not a field of the item.
			if (string.Equals(keyValuePair.Key, "type", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			string text5 = PropertyName(keyValuePair.Key);
			if (text5 == null || !IsUsableValue(keyValuePair.Key, keyValuePair.Value))
			{
				result.UnsupportedFields++;
				result.Warnings.Add("'" + text + "': field '" + keyValuePair.Key + "' with value '"
					+ keyValuePair.Value + "' has no equivalent - dropped");
				continue;
			}
			body.Append("\t\t\t").Append(text4).Append('.').Append(text5)
				.Append(" = ").Append(Literal(keyValuePair.Key, keyValuePair.Value)).Append(";\n");
			num++;
			result.Fields++;
		}
		body.Append("\t\t}\n\n");
		result.NewItems++;
		result.Items++;
		if (num == 0)
		{
			result.Warnings.Add("'" + text + "': no fields converted, the item will exist with default stats");
		}
	}

	/// <summary>
	/// True when the INI defines a new item rather than tweaking one: tConfig writes
	/// <c>type=-1</c> (or any negative id) for that. Reading the file is the only way to know,
	/// because the item's name does not exist in this build either way.
	/// </summary>
	private static bool DefinesNewItem(string iniPath)
	{
		string error;
		List<KeyValuePair<string, string>> list = TConfigImporter.ReadIni(iniPath, out error);
		if (list == null)
		{
			return false;
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (!string.Equals(list[i].Key, "type", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			int result;
			if (int.TryParse(list[i].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
			{
				return result < 0;
			}
		}
		return false;
	}

	/// <summary>
	/// True when a field's value can become a C# literal of the right type. Boolean fields take
	/// true/false; everything else has to parse as a number in the invariant culture.
	///
	/// This exists because <see cref="Literal"/> deliberately quotes a value it cannot read, so
	/// that a bad value fails to compile instead of silently becoming zero. Failing to compile is
	/// the right answer for a generator; for a converter run by a player it is not, because the
	/// project they asked for is then unusable and the reason is buried in a compiler message.
	/// So the bad field is dropped here and named in the report instead.
	/// </summary>
	private static bool IsUsableValue(string field, string value)
	{
		string text = (value ?? string.Empty).Trim();
		if (text.Length == 0)
		{
			return false;
		}
		if (IsBoolean(field))
		{
			bool result;
			return bool.TryParse(text, out result);
		}
		double result2;
		return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result2);
	}

	/// <summary>
	/// INI field name to <see cref="ItemOverride"/> property. The two lists have to agree, and a
	/// mismatch here would produce a project that does not compile - which is at least loud, but
	/// the self-test checks the mapping so it never ships that way.
	/// </summary>
	internal static string PropertyName(string field)
	{
		switch (field.ToLowerInvariant())
		{
		case "damage":
			return "Damage";
		case "defense":
			return "Defense";
		case "pick":
			return "Pick";
		case "axe":
			return "Axe";
		case "hammer":
			return "Hammer";
		case "useanimation":
			return "UseAnimation";
		case "usetime":
			return "UseTime";
		case "usesound":
			return "UseSound";
		case "shoot":
			return "Shoot";
		case "shootspeed":
			return "ShootSpeed";
		case "knockback":
			return "Knockback";
		case "mana":
			return "Mana";
		case "heallife":
			return "HealLife";
		case "healmana":
			return "HealMana";
		case "bufftype":
			return "BuffType";
		case "bufftime":
			return "BuffTime";
		case "rare":
			return "Rare";
		case "value":
			return "Value";
		case "maxstack":
			return "MaxStack";
		case "autoreuse":
			return "AutoReuse";
		case "melee":
			return "Melee";
		case "magic":
			return "Magic";
		case "ranged":
			return "Ranged";
		case "potion":
			return "Potion";
		case "accessory":
			return "Accessory";
		case "nomelee":
			return "NoMelee";
		case "crit":
			return "Crit";
		case "width":
			return "Width";
		case "height":
			return "Height";
		case "usestyle":
			return "UseStyle";
		case "holdstyle":
			return "HoldStyle";
		case "scale":
			return "Scale";
		case "ismaterial":
			return "Material";
		case "ammo":
			return "Ammo";
		case "useammo":
			return "UseAmmo";
		default:
			return null;
		}
	}

	/// <summary>
	/// The C# literal for an INI value. Numeric fields keep their text as written (a float with
	/// a comma would not compile), and booleans become C# <c>true</c>/<c>false</c>. A value that
	/// is not the right shape is quoted, so the generated project fails to compile at that line
	/// instead of silently assigning zero.
	/// </summary>
	private static string Literal(string field, string value)
	{
		string text = value.Trim();
		if (IsBoolean(field))
		{
			bool result;
			if (bool.TryParse(text, out result))
			{
				return result ? "true" : "false";
			}
			return Quote(text);
		}
		double result2;
		if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result2))
		{
			// A float field needs the suffix: `4.5` on its own is a double literal and does not
			// convert implicitly. The first version of this emitted the raw text, and every pack
			// with a fractional knockback or scale produced a project that would not compile -
			// 114 items, all of them, which is how it was found.
			return IsFloat(field) ? (text + "f") : text;
		}
		return Quote(text);
	}

	/// <summary>Fields whose property is a <c>float</c> rather than an <c>int</c>.</summary>
	private static bool IsFloat(string field)
	{
		string text = PropertyName(field);
		return text == "ShootSpeed" || text == "Knockback" || text == "Scale";
	}

	private static bool IsBoolean(string field)
	{
		string text = PropertyName(field);
		return text == "AutoReuse" || text == "Melee" || text == "Magic" || text == "Ranged"
			|| text == "Potion" || text == "Accessory" || text == "NoMelee" || text == "Material";
	}

	internal static string Identifier(string name, bool upperFirst)
	{
		if (string.IsNullOrEmpty(name))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		for (int i = 0; i < name.Length; i++)
		{
			char c = name[i];
			if (char.IsLetterOrDigit(c))
			{
				if (stringBuilder.Length == 0 && char.IsDigit(c))
				{
					// An identifier cannot start with a digit; the item name is still in the comment.
					stringBuilder.Append('n');
				}
				stringBuilder.Append(flag ? char.ToUpperInvariant(c) : c);
				flag = false;
			}
			else
			{
				flag = true;
			}
		}
		if (stringBuilder.Length == 0)
		{
			return "";
		}
		if (upperFirst)
		{
			stringBuilder[0] = char.ToUpperInvariant(stringBuilder[0]);
		}
		else
		{
			stringBuilder[0] = char.ToLowerInvariant(stringBuilder[0]);
		}
		string text = stringBuilder.ToString();
		return IsKeyword(text) ? text + "Item" : text;
	}

	private static bool IsKeyword(string text)
	{
		string[] array = new string[16]
		{
			"abstract", "base", "class", "event", "fixed", "internal", "lock", "namespace",
			"object", "operator", "params", "string", "struct", "this", "value", "void"
		};
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == text)
			{
				return true;
			}
		}
		return false;
	}

	private static string UniqueIdentifier(string name, HashSet<string> used)
	{
		string text = name;
		int num = 2;
		while (!used.Add(text))
		{
			text = name + num.ToString(CultureInfo.InvariantCulture);
			num++;
		}
		return text;
	}

	private static string Quote(string text)
	{
		StringBuilder stringBuilder = new StringBuilder("\"");
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (c == '"' || c == '\\')
			{
				stringBuilder.Append('\\');
			}
			stringBuilder.Append(c);
		}
		stringBuilder.Append('"');
		return stringBuilder.ToString();
	}

	private static string Source(string modName, string packDirectory, string body)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("using Terraria.Mods;\n\n");
		stringBuilder.Append("// Generated by TerrariaXDK from the tConfig pack at\n");
		stringBuilder.Append("//   ").Append(packDirectory.Replace('\\', '/')).Append("\n");
		stringBuilder.Append("//\n");
		stringBuilder.Append("// This is a normal mod, not a one-way export: edit it, extend it, keep it under version\n");
		stringBuilder.Append("// control. The numbers below were the pack's Item/*.ini files; anything the pack said that\n");
		stringBuilder.Append("// this build has no field for was dropped and is listed in README.md.\n");
		stringBuilder.Append("[assembly: ModInfo(").Append(Quote(modName)).Append(", \"1.0.0\", \"\", ")
			.Append(Quote("Converted from the tConfig pack '" + modName + "'")).Append(")]\n\n");
		stringBuilder.Append("namespace ").Append(modName).Append(";\n\n");
		stringBuilder.Append("/// <summary>Item stat changes from a tConfig pack.</summary>\n");
		stringBuilder.Append("public class ").Append(modName).Append("Mod : Mod\n");
		stringBuilder.Append("{\n");
		stringBuilder.Append("\tpublic override void Load()\n");
		stringBuilder.Append("\t{\n");
		stringBuilder.Append(body.TrimEnd('\n')).Append('\n');
		stringBuilder.Append("\t}\n");
		stringBuilder.Append("}\n");
		return stringBuilder.ToString();
	}

	private static string Project(string modName, string gameAssembly)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<Project Sdk=\"Microsoft.NET.Sdk\">\n");
		stringBuilder.Append("  <!--\n");
		stringBuilder.Append("    Generated by TerrariaXDK. Build it, then copy the DLL:\n");
		stringBuilder.Append("\n");
		stringBuilder.Append("        dotnet build ").Append(modName).Append(".csproj\n");
		stringBuilder.Append("        copy bin/Debug/net40/").Append(modName).Append(".dll ../../Mods/").Append(modName).Append("/\n");
		stringBuilder.Append("\n");
		stringBuilder.Append("    The reference below points at the executable that generated this project. If you move\n");
		stringBuilder.Append("    the project to another machine, fix the HintPath.\n");
		stringBuilder.Append("  -->\n");
		stringBuilder.Append("  <PropertyGroup>\n");
		stringBuilder.Append("    <AssemblyName>").Append(modName).Append("</AssemblyName>\n");
		stringBuilder.Append("    <TargetFramework>net40</TargetFramework>\n");
		stringBuilder.Append("    <PlatformTarget>x64</PlatformTarget>\n");
		stringBuilder.Append("    <LangVersion>latest</LangVersion>\n");
		stringBuilder.Append("    <GenerateAssemblyInfo>False</GenerateAssemblyInfo>\n");
		stringBuilder.Append("    <RootNamespace>").Append(modName).Append("</RootNamespace>\n");
		stringBuilder.Append("    <NoWarn>MSB3277</NoWarn>\n");
		stringBuilder.Append("  </PropertyGroup>\n");
		stringBuilder.Append("  <ItemGroup>\n");
		stringBuilder.Append("    <Reference Include=\"TerrariaXDK\">\n");
		stringBuilder.Append("      <HintPath>").Append(gameAssembly).Append("</HintPath>\n");
		stringBuilder.Append("      <Private>false</Private>\n");
		stringBuilder.Append("    </Reference>\n");
		stringBuilder.Append("    <Reference Include=\"System\" />\n");
		stringBuilder.Append("    <Reference Include=\"System.Core\" />\n");
		stringBuilder.Append("  </ItemGroup>\n");
		stringBuilder.Append("</Project>\n");
		return stringBuilder.ToString();
	}

	private static string Readme(string modName, string packDirectory, Result result)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("# ").Append(modName).Append("\n\n");
		stringBuilder.Append("Generated from the tConfig pack `")
			.Append(packDirectory.Replace('\\', '/')).Append("`.\n\n");
		stringBuilder.Append("| | |\n|---|---|\n");
		stringBuilder.Append("| Items | ").Append(result.Items.ToString(CultureInfo.InvariantCulture)).Append(" |\n");
		stringBuilder.Append("| Fields | ").Append(result.Fields.ToString(CultureInfo.InvariantCulture)).Append(" |\n");
		stringBuilder.Append("| Unknown items (skipped) | ").Append(result.UnknownItems.ToString(CultureInfo.InvariantCulture)).Append(" |\n");
		stringBuilder.Append("| Fields with no equivalent (dropped) | ").Append(result.UnsupportedFields.ToString(CultureInfo.InvariantCulture)).Append(" |\n");
		stringBuilder.Append("| Files that failed (skipped) | ").Append(result.FailedFiles.ToString(CultureInfo.InvariantCulture)).Append(" |\n");
		stringBuilder.Append("| New items converted | ").Append(result.NewItems.ToString(CultureInfo.InvariantCulture)).Append(" |\n");
		stringBuilder.Append("| New items skipped (no sprite) | ").Append(result.NewItemsSkipped.ToString(CultureInfo.InvariantCulture)).Append(" |\n\n");
		stringBuilder.Append("## Build and install\n\n");
		stringBuilder.Append("```\ndotnet build ").Append(modName).Append(".csproj\n");
		stringBuilder.Append("```\n\n");
		stringBuilder.Append("Copy `bin/Debug/net40/").Append(modName)
			.Append(".dll` into `Storage/Mods/").Append(modName).Append("/`");
		if (result.SpritesCopied > 0)
		{
			// The sprites are separate files, not embedded: a mod that is only a DLL would look
			// for its pictures and find nothing, and its items would be invisible.
			stringBuilder.Append(", **and the `items` folder next to it** - the sprites are files, not embedded");
		}
		stringBuilder.Append(". The mod list in the game\n");
		stringBuilder.Append("then shows it by name, and multiplayer uses it in the mod signature - which is the\n");
		stringBuilder.Append("reason to convert a pack at all: a loose `Storage/tConfig/` folder changes the numbers\n");
		stringBuilder.Append("on one machine and takes no part in the signature, so a host cannot require it.\n\n");
		if (result.Errors.Count > 0)
		{
			stringBuilder.Append("## What failed\n\n");
			stringBuilder.Append("These files or items could not be converted and are **not** in the mod. The\n");
			stringBuilder.Append("rest of the pack was converted anyway, so the project still builds.\n\n");
			for (int i = 0; i < result.Errors.Count; i++)
			{
				stringBuilder.Append("- ").Append(result.Errors[i]).Append('\n');
			}
			stringBuilder.Append('\n');
		}
		if (result.NewItemsSkipped > 0)
		{
			stringBuilder.Append("## New items that were skipped\n\n");
			stringBuilder.Append("A new item needs a sprite: an item that cannot be drawn is indistinguishable\n");
			stringBuilder.Append("from a bug in the inventory, so it is not converted. The items below had no\n");
			stringBuilder.Append("PNG next to their `.ini` in the pack.\n\n");
			for (int num = 0; num < result.Warnings.Count; num++)
			{
				if (result.Warnings[num].IndexOf("new item with no sprite", StringComparison.Ordinal) >= 0)
				{
					stringBuilder.Append("- ").Append(result.Warnings[num]).Append('\n');
				}
			}
			stringBuilder.Append('\n');
		}
		stringBuilder.Append("## Why a field was dropped\n\n");
		stringBuilder.Append("A tConfig pack can name fields this build does not have, or give a value that is not\n");
		stringBuilder.Append("a number. They are listed here rather than guessed at, because a silently ignored\n");
		stringBuilder.Append("field looks like a conversion that worked.\n\n");
		if (result.Warnings.Count == 0)
		{
			stringBuilder.Append("Nothing was dropped.\n");
		}
		else
		{
			for (int j = 0; j < result.Warnings.Count; j++)
			{
				stringBuilder.Append("- ").Append(result.Warnings[j]).Append('\n');
			}
		}
		return stringBuilder.ToString();
	}

	/// <summary>Path of the running executable, used as the project's assembly reference.</summary>
	private static string GameAssemblyPath()
	{
		string text = Path.Combine(Terraria.Storage.AppPaths.BaseDirectory, "TerrariaXDK.exe");
		if (File.Exists(text))
		{
			return text;
		}
		// A Debug build has the same name; if neither exists the project still opens and the
		// author can point the reference somewhere real.
		return text;
	}
}
