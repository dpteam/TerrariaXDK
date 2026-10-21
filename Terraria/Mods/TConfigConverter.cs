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

		public readonly List<string> Warnings = new List<string>();
	}

	/// <summary>
	/// Reads <paramref name="packDirectory"/> and writes a project into
	/// <paramref name="outputDirectory"/>. Returns null when the pack has nothing to convert;
	/// the reason is in <paramref name="error"/>.
	/// </summary>
	internal static Result Convert(string packDirectory, string outputDirectory, string modName, out string error)
	{
		error = null;
		if (string.IsNullOrEmpty(packDirectory) || !Directory.Exists(packDirectory))
		{
			error = "no such pack folder: " + packDirectory;
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
			return null;
		}
		Result result = new Result();
		string text = Path.Combine(packDirectory, "Item");
		List<string> list = new List<string>();
		if (Directory.Exists(text))
		{
			string[] files = Directory.GetFiles(text, "*.ini", SearchOption.TopDirectoryOnly);
			Array.Sort(files, StringComparer.OrdinalIgnoreCase);
			list.AddRange(files);
		}
		if (list.Count == 0)
		{
			error = "no Item/*.ini files in " + packDirectory;
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		HashSet<string> used = new HashSet<string>(StringComparer.Ordinal);
		foreach (string item in list)
		{
			AppendItem(stringBuilder, item, used, result);
		}
		if (result.Items == 0)
		{
			error = "the pack names no item this build knows ("
				+ result.UnknownItems.ToString(CultureInfo.InvariantCulture) + " unknown)";
			return null;
		}
		string text2 = Path.Combine(outputDirectory, modName + ".cs");
		string text3 = Path.Combine(outputDirectory, modName + ".csproj");
		string text4 = Path.Combine(outputDirectory, "README.md");
		try
		{
			Directory.CreateDirectory(outputDirectory);
			File.WriteAllText(text2, Source(modName, packDirectory, stringBuilder.ToString()), new UTF8Encoding(false));
			File.WriteAllText(text3, Project(modName, GameAssemblyPath()), new UTF8Encoding(false));
			File.WriteAllText(text4, Readme(modName, packDirectory, result), new UTF8Encoding(false));
		}
		catch (Exception ex)
		{
			error = "cannot write to " + outputDirectory + ": " + ex.Message;
			return null;
		}
		result.OutputDirectory = outputDirectory;
		result.ProjectFile = text3;
		result.SourceFile = text2;
		return result;
	}

	private static void AppendItem(StringBuilder body, string iniPath, HashSet<string> used, Result result)
	{
		string text = Path.GetFileNameWithoutExtension(iniPath);
		Item item = default(Item);
		item.SetDefaults(text);
		if (item.Type <= 0)
		{
			result.UnknownItems++;
			result.Warnings.Add("'" + text + "': no such item in this build, skipped");
			return;
		}
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		foreach (KeyValuePair<string, string> line in TConfigImporter.ParseIni(iniPath))
		{
			// PropertyName is the second half of the check: ItemOverrides can accept a field that
			// the mod API has no property for (consumable is recorded as an error there), and
			// emitting a property that does not exist would produce a project that cannot build.
			if (!ItemOverrides.IsSupportedField(line.Key) || PropertyName(line.Key) == null)
			{
				result.UnsupportedFields++;
				result.Warnings.Add("'" + text + "': field '" + line.Key + "' has no equivalent, dropped");
				continue;
			}
			list.Add(line);
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
			return text;
		}
		return Quote(text);
	}

	private static bool IsBoolean(string field)
	{
		string text = PropertyName(field);
		return text == "AutoReuse" || text == "Melee" || text == "Magic" || text == "Ranged"
			|| text == "Potion" || text == "Accessory" || text == "NoMelee";
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
		stringBuilder.Append("| Fields with no equivalent (dropped) | ").Append(result.UnsupportedFields.ToString(CultureInfo.InvariantCulture)).Append(" |\n\n");
		stringBuilder.Append("## Build and install\n\n");
		stringBuilder.Append("```\ndotnet build ").Append(modName).Append(".csproj\n");
		stringBuilder.Append("```\n\n");
		stringBuilder.Append("Copy `bin/Debug/net40/").Append(modName)
			.Append(".dll` into `Storage/Mods/").Append(modName).Append("/`. The mod list in the game\n");
		stringBuilder.Append("then shows it by name, and multiplayer uses it in the mod signature - which is the\n");
		stringBuilder.Append("reason to convert a pack at all: a loose `Storage/tConfig/` folder changes the numbers\n");
		stringBuilder.Append("on one machine and takes no part in the signature, so a host cannot require it.\n\n");
		stringBuilder.Append("## Why a field was dropped\n\n");
		stringBuilder.Append("A tConfig pack can name fields this build does not have. They are listed here rather\n");
		stringBuilder.Append("than guessed at, because a silently ignored field looks like a conversion that worked.\n\n");
		if (result.Warnings.Count == 0)
		{
			stringBuilder.Append("Nothing was dropped.\n");
		}
		else
		{
			for (int i = 0; i < result.Warnings.Count; i++)
			{
				stringBuilder.Append("- ").Append(result.Warnings[i]).Append('\n');
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
