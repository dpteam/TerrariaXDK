using System;
using System.Collections.Generic;
using System.IO;

namespace Terraria.Mods;

/// <summary>
/// Reads tConfig mod packs (refs/T1061M, refs/T111M) and turns them into runtime overrides.
///
/// A tConfig mod is a folder of INI files describing game objects:
/// <code>
/// tConfig/
///   MyPack/
///     Item/Copper Shortsword.ini     damage=12
///     Buff/...
///     NPC/...
/// </code>
/// Each INI line is a field of the corresponding object. We support the fields that map
/// onto <see cref="Item"/> today; anything else is reported instead of silently dropped.
///
/// New item ids cannot be invented without breaking saves and the network protocol, so an
/// imported pack adjusts the item it names. That covers the common "stat tweak" packs.
/// </summary>
internal static class TConfigImporter
{
	/// <summary>Folder scanned automatically at startup, next to the other Storage folders.</summary>
	internal static string ImportFolder => Path.Combine(Terraria.Storage.AppPaths.Root, "tConfig");

	public static int ImportedItems { get; private set; }

	public static int SkippedItems { get; private set; }

	public static int UnsupportedFields { get; private set; }

	/// <summary>Scans <see cref="ImportFolder"/> if it exists. Safe to call once at startup.</summary>
	public static void ImportIfPresent()
	{
		string text = ImportFolder;
		if (!Directory.Exists(text))
		{
			return;
		}
		Import(text);
	}

	/// <summary>Imports every pack under the given directory. Returns the number of items applied.</summary>
	public static int Import(string root)
	{
		ImportedItems = 0;
		SkippedItems = 0;
		UnsupportedFields = 0;
		if (!Directory.Exists(root))
		{
			ModLoader.Log("tConfig: no packs found at " + root);
			return 0;
		}
		string[] directories = Directory.GetDirectories(root);
		Array.Sort(directories, StringComparer.OrdinalIgnoreCase);
		int num = 0;
		string[] array = directories;
		foreach (string text in array)
		{
			num += ImportPack(text);
		}
		// Also tolerate packs dropped directly into the root.
		num += ImportPack(root);
		ModLoader.Log($"tConfig: {ImportedItems} item(s) applied, {SkippedItems} skipped, {UnsupportedFields} unsupported field(s) from {num} pack(s)");
		return ImportedItems;
	}

	private static int ImportPack(string packDir)
	{
		string text = Path.Combine(packDir, "Item");
		if (!Directory.Exists(text))
		{
			return 0;
		}
		string[] files = Directory.GetFiles(text, "*.ini", SearchOption.TopDirectoryOnly);
		int num = 0;
		string[] array = files;
		foreach (string text2 in array)
		{
			if (ApplyItemFile(text2))
			{
				num++;
			}
		}
		return num;
	}

	private static bool ApplyItemFile(string path)
	{
		string text = Path.GetFileNameWithoutExtension(path);
		// Resolve the name to an existing item the same way the game does.
		Item item = default(Item);
		item.SetDefaults(text);
		if (item.Type <= 0)
		{
			SkippedItems++;
			ModLoader.Log("tConfig: unknown item '" + text + "' in " + Path.GetFileName(path));
			return false;
		}
		int num = 0;
		foreach (KeyValuePair<string, string> line in ParseIni(path))
		{
			if (!ItemOverrides.IsSupportedField(line.Key))
			{
				UnsupportedFields++;
				ModLoader.Log("tConfig: '" + text + "' field '" + line.Key + "' is not supported");
				continue;
			}
			ItemOverrides.Set(item.Type, line.Key, line.Value);
			num++;
		}
		if (num == 0)
		{
			SkippedItems++;
			return false;
		}
		ImportedItems++;
		return true;
	}

	/// <summary>
	/// Minimal INI reader: `key = value` lines, `;` and `#` comments, `[section]` headers
	/// flattened into `section.key`. tConfig writes flat files, but tolerating sections costs
	/// nothing and avoids mis-parsing hand-edited packs.
	/// </summary>
	private static IEnumerable<KeyValuePair<string, string>> ParseIni(string path)
	{
		string[] lines;
		try
		{
			lines = File.ReadAllLines(path);
		}
		catch (Exception ex)
		{
			ModLoader.Log("tConfig: cannot read " + path + ": " + ex.Message);
			yield break;
		}
		string text = "";
		string[] array = lines;
		foreach (string text2 in array)
		{
			string text3 = text2.Trim();
			if (text3.Length == 0 || text3[0] == ';' || text3[0] == '#')
			{
				continue;
			}
			if (text3[0] == '[' && text3[text3.Length - 1] == ']')
			{
				text = text3.Substring(1, text3.Length - 2).Trim();
				continue;
			}
			int num = text3.IndexOf('=');
			if (num <= 0)
			{
				continue;
			}
			string key = text3.Substring(0, num).Trim();
			string value = text3.Substring(num + 1).Trim();
			// Sections are organisational only; the field name is what matters.
			int num2 = key.LastIndexOf('.');
			if (num2 >= 0 && num2 < key.Length - 1)
			{
				key = key.Substring(num2 + 1);
			}
			yield return new KeyValuePair<string, string>(key, value);
		}
	}
}
