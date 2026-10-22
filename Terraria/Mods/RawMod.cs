using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Terraria.Mods;

/// <summary>
/// A "Raw Mod": a folder of INI files and PNGs that the game reads directly, with nothing
/// compiled.
///
/// The compiled form is not going away - it is the only way to add behaviour (<c>OnUse</c>, AI,
/// drawing). But most of what a pack actually says is data: damage, use time, a sprite, a
/// recipe. That data was already readable: <see cref="TConfigImporter"/> parses it and
/// <see cref="TConfigConverter"/> turns it into C#. The converter needed a compiler on the
/// player's machine, which is the one thing a mod should not need.
///
/// So the layout is deliberately the same as a tConfig pack, which means a pack can be dropped
/// into <c>Storage/Mods</c> as it is:
/// <code>
/// Storage/Mods/My Pack/
///   mod.ini                  [Mod] Name=, Version=, Author=
///   Item/Copper Bulgeon.ini  type=-1, damage=12, useTime=20, [Items] Copper Bar=6
///   Item/Copper Bulgeon.png  the sprite, next to its INI
/// </code>
/// A file whose item exists in this build changes that item (an override); a file with
/// <c>type=-1</c> or an unknown name that has a sprite declares a new one. Both go through the
/// same registries a compiled mod uses, so ids, sprite sheets, the network signature and the
/// crafting screen behave identically.
///
/// What it cannot do, and does not pretend to: run code. A pack that used tConfig's
/// <c>OnUse</c> or a custom AI becomes data only, and says so in <see cref="Problems"/>.
/// </summary>
internal sealed class RawMod
{
	/// <summary>Optional metadata file. Without it the folder name is the mod name.</summary>
	internal const string ManifestFile = "mod.ini";

	/// <summary>A recipe holds twelve ingredients; the game's own array is that long.</summary>
	private const int MaxIngredients = 12;

	private readonly List<string> _problems = new List<string>();

	public string Name { get; private set; } = "";

	public string Version { get; private set; } = "0.0.0";

	public string Author { get; private set; } = "";

	/// <summary>Absolute path of the mod folder.</summary>
	public string Directory { get; private set; } = "";

	/// <summary>How many new items it declared.</summary>
	public int NewItems { get; private set; }

	/// <summary>How many existing items it changed.</summary>
	public int Overrides { get; private set; }

	/// <summary>How many recipes it added.</summary>
	public int Recipes { get; private set; }

	/// <summary>
	/// Hash of everything in the folder. Two players with a folder of the same name but
	/// different contents must not be able to join each other, and the name is not the content.
	/// </summary>
	public string Signature { get; private set; } = "";

	public IList<string> Problems => _problems;

	/// <summary>
	/// True for a folder the loader should treat as a raw mod: no assembly, but item INI files
	/// (or a manifest) in it.
	///
	/// The "no assembly" half is what keeps a compiled mod's own data files out of this path: a
	/// mod may well ship <c>Item/*.ini</c> for its own code to read, and loading those as
	/// definitions as well would apply everything twice.
	/// </summary>
	internal static bool LooksLikeRawMod(string directory)
	{
		if (string.IsNullOrEmpty(directory) || !System.IO.Directory.Exists(directory))
		{
			return false;
		}
		if (System.IO.Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly).Length > 0)
		{
			return false;
		}
		if (ItemDirectory(directory) != null)
		{
			return true;
		}
		return File.Exists(Path.Combine(directory, ManifestFile));
	}

	/// <summary>The folder holding item INIs, or null. "Item" is what tConfig writes.</summary>
	internal static string ItemDirectory(string directory)
	{
		string[] array = new string[3] { "Item", "Items", "items" };
		for (int i = 0; i < array.Length; i++)
		{
			string text = Path.Combine(directory, array[i]);
			if (!System.IO.Directory.Exists(text))
			{
				continue;
			}
			if (System.IO.Directory.GetFiles(text, "*.ini", SearchOption.TopDirectoryOnly).Length > 0)
			{
				return text;
			}
		}
		return null;
	}

	/// <summary>
	/// Reads the folder and registers everything it describes. Never throws for bad input: a
	/// pack is somebody else's data, and one broken file must not take the game down with it.
	/// </summary>
	internal static RawMod Load(string directory)
	{
		RawMod mod = new RawMod();
		mod.Directory = directory;
		mod.Name = Path.GetFileName(directory);
		mod.ReadManifest();
		string text = ItemDirectory(directory);
		if (text != null)
		{
			string[] files = System.IO.Directory.GetFiles(text, "*.ini", SearchOption.TopDirectoryOnly);
			Array.Sort(files, StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < files.Length; i++)
			{
				mod.LoadItemFile(files[i]);
			}
		}
		mod.Signature = ContentHash(directory);
		return mod;
	}

	private void ReadManifest()
	{
		string path = Path.Combine(Directory, ManifestFile);
		if (!File.Exists(path))
		{
			return;
		}
		string error;
		List<TConfigImporter.SectionLine> list = TConfigImporter.ReadIniSections(path, out error);
		if (list == null)
		{
			_problems.Add(ManifestFile + ": " + error);
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			string key = list[i].Key;
			string value = list[i].Value;
			if (key.Equals("name", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
			{
				Name = value;
			}
			else if (key.Equals("version", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
			{
				Version = value;
			}
			else if (key.Equals("author", StringComparison.OrdinalIgnoreCase))
			{
				Author = value;
			}
		}
	}

	private void LoadItemFile(string iniPath)
	{
		string text = Path.GetFileNameWithoutExtension(iniPath);
		string error;
		List<TConfigImporter.SectionLine> list = TConfigImporter.ReadIniSections(iniPath, out error);
		if (list == null)
		{
			_problems.Add("'" + text + "': " + Path.GetFileName(iniPath) + ": " + error);
			return;
		}
		string text2 = FindSprite(iniPath);
		bool flag = DeclaresNewItem(list);
		int num = 0;
		if (!flag)
		{
			num = Terraria.ItemLookup.Find(text);
			if (num <= 0)
			{
				// Unknown name without type=-1: a hand-written pack may simply have forgotten the
				// marker. A sprite is the evidence that it means to define something, and without
				// one the file is a mistake worth naming rather than a new item worth inventing.
				if (text2 == null)
				{
					_problems.Add("'" + text + "': no such item in this build and no sprite, skipped");
					return;
				}
				flag = true;
			}
		}
		if (flag)
		{
			if (text2 == null)
			{
				// An item that cannot be drawn is not a feature: it would show as an empty slot and
				// read as a bug in the game.
				_problems.Add("'" + text + "': new item with no sprite (" + text + ".png), skipped");
				return;
			}
			ModItem modItem = ModItems.Register(Name, text, text2, Directory, out error);
			if (modItem == null)
			{
				_problems.Add("'" + text + "': " + error);
				return;
			}
			ApplyFields(list, text, modItem);
			NewItems++;
			AppendRecipe(list, text, modItem.Fields);
			return;
		}
		int num2 = 0;
		for (int i = 0; i < list.Count; i++)
		{
			TConfigImporter.SectionLine sectionLine = list[i];
			if (TConfigConverter.IsRecipeSection(sectionLine.Section))
			{
				continue;
			}
			if (string.Equals(sectionLine.Key, "type", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (!ItemOverrides.IsSupportedField(sectionLine.Key))
			{
				_problems.Add("'" + text + "': field '" + sectionLine.Key + "' has no equivalent, dropped");
				continue;
			}
			ItemOverrides.Set(num, sectionLine.Key, sectionLine.Value);
			num2++;
		}
		if (num2 > 0)
		{
			Overrides++;
		}
		else
		{
			_problems.Add("'" + text + "': nothing to change");
		}
		// An existing item can carry a recipe too: a pack that makes a vanilla item craftable is
		// as legitimate as one that adds a new item.
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		for (int j = 0; j < list.Count; j++)
		{
			if (!TConfigConverter.IsRecipeSection(list[j].Section))
			{
				dictionary[list[j].Key] = list[j].Value;
			}
		}
		AppendRecipe(list, text, dictionary);
	}

	/// <summary>Copies the item's fields onto a new definition, reporting what has no equivalent.</summary>
	private void ApplyFields(List<TConfigImporter.SectionLine> lines, string itemName, ModItem definition)
	{
		for (int i = 0; i < lines.Count; i++)
		{
			TConfigImporter.SectionLine sectionLine = lines[i];
			if (TConfigConverter.IsRecipeSection(sectionLine.Section))
			{
				continue;
			}
			if (string.Equals(sectionLine.Key, "type", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (!ItemOverrides.IsSupportedField(sectionLine.Key))
			{
				_problems.Add("'" + itemName + "': field '" + sectionLine.Key + "' has no equivalent, dropped");
				continue;
			}
			definition.Set(sectionLine.Key, sectionLine.Value);
		}
	}

	/// <summary>
	/// Registers the file's recipe, if it has one. The sections are the ones tConfig uses:
	/// <c>[Items]</c> for ingredients, <c>[Tiles]</c> for stations, <c>[Amount]</c> for the output
	/// count, <c>[needWater]</c> for the water requirement.
	/// </summary>
	private void AppendRecipe(List<TConfigImporter.SectionLine> lines, string itemName, Dictionary<string, string> fields)
	{
		List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
		List<string> list2 = new List<string>();
		int num = 1;
		bool flag = false;
		for (int i = 0; i < lines.Count; i++)
		{
			TConfigImporter.SectionLine sectionLine = lines[i];
			if (sectionLine.Section.Equals("Items", StringComparison.OrdinalIgnoreCase))
			{
				int result;
				if (!int.TryParse(sectionLine.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) || result <= 0)
				{
					_problems.Add("'" + itemName + "': ingredient '" + sectionLine.Key + "' has the count '"
						+ sectionLine.Value + "', which is not a number");
					continue;
				}
				if (list.Count >= MaxIngredients)
				{
					_problems.Add("'" + itemName + "': more than " + MaxIngredients
						+ " ingredients - the recipe is not added");
					return;
				}
				list.Add(new KeyValuePair<string, int>(sectionLine.Key, result));
			}
			else if (sectionLine.Section.Equals("Tiles", StringComparison.OrdinalIgnoreCase))
			{
				if (!IsTrue(sectionLine.Value))
				{
					continue;
				}
				if (ModRecipes.StationId(sectionLine.Key) < 0)
				{
					// The station is usually a tile the pack itself would add, and this build has no
					// mod tiles. Dropping the station keeps the item obtainable; dropping the recipe
					// would make it exist and be impossible to make.
					_problems.Add("'" + itemName + "': station '" + sectionLine.Key
						+ "' is a mod tile this build does not have - the recipe is added without it");
					continue;
				}
				list2.Add(sectionLine.Key);
			}
			else if (sectionLine.Section.Equals("Amount", StringComparison.OrdinalIgnoreCase))
			{
				int result2;
				if (int.TryParse(sectionLine.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result2) && result2 > 0)
				{
					num = result2;
				}
			}
			else if (sectionLine.Section.Equals("needWater", StringComparison.OrdinalIgnoreCase))
			{
				flag = IsTrue(sectionLine.Value);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		string error;
		ModRecipe modRecipe = ModRecipes.Register(Name, itemName, out error);
		if (modRecipe == null)
		{
			_problems.Add("'" + itemName + "': " + error);
			return;
		}
		modRecipe.Amount = num;
		modRecipe.NeedsWater = flag;
		modRecipe.Category = CategoryFor(fields);
		for (int j = 0; j < list.Count; j++)
		{
			modRecipe.AddIngredient(list[j].Key, list[j].Value);
		}
		for (int k = 0; k < list2.Count; k++)
		{
			modRecipe.AddStation(list2[k]);
		}
		Recipes++;
	}

	/// <summary>
	/// Which crafting tab the recipe belongs in. The same honest guess the converter makes:
	/// tConfig does not say, and a wrong tab is a nuisance rather than a bug.
	/// </summary>
	private static Recipe.Category CategoryFor(Dictionary<string, string> fields)
	{
		if (fields == null)
		{
			return Recipe.Category.MISC;
		}
		if (IsPositive(fields, "damage") || IsTrue(Field(fields, "melee"))
			|| IsTrue(Field(fields, "magic")) || IsTrue(Field(fields, "ranged")))
		{
			return Recipe.Category.WEAPONS;
		}
		if (IsPositive(fields, "defense"))
		{
			return Recipe.Category.ARMOR;
		}
		if (IsTrue(Field(fields, "potion")))
		{
			return Recipe.Category.POTIONS;
		}
		if (IsPositive(fields, "pick") || IsPositive(fields, "axe") || IsPositive(fields, "hammer"))
		{
			return Recipe.Category.TOOLS;
		}
		return Recipe.Category.MISC;
	}

	private static string Field(Dictionary<string, string> fields, string name)
	{
		string result;
		return fields.TryGetValue(name, out result) ? result : null;
	}

	private static bool IsPositive(Dictionary<string, string> fields, string name)
	{
		int result;
		string text = Field(fields, name);
		return text != null && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result > 0;
	}

	private static bool IsTrue(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		string text = value.Trim();
		return text.Equals("true", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("yes", StringComparison.OrdinalIgnoreCase)
			|| text.Equals("1", StringComparison.Ordinal);
	}

	/// <summary>True when the file says <c>type</c> is negative: tConfig's "this is a new item".</summary>
	private static bool DeclaresNewItem(List<TConfigImporter.SectionLine> lines)
	{
		for (int i = 0; i < lines.Count; i++)
		{
			if (!string.Equals(lines[i].Key, "type", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			int result;
			if (int.TryParse(lines[i].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
			{
				return result < 0;
			}
		}
		return false;
	}

	/// <summary>
	/// The sprite for an item file: <c>X.png</c> beside <c>X.ini</c>, or in the pack's
	/// <c>Sprites</c> folder. Returned relative to the mod folder with '/' separators, which is
	/// what <see cref="ModItems.Register"/> expects.
	/// </summary>
	private string FindSprite(string iniPath)
	{
		string text = Path.GetFileNameWithoutExtension(iniPath);
		string text2 = Path.ChangeExtension(iniPath, ".png");
		if (File.Exists(text2))
		{
			return Relative(text2);
		}
		string[] array = new string[2] { "Sprites", "Images" };
		for (int i = 0; i < array.Length; i++)
		{
			string text3 = Path.Combine(Directory, array[i], text + ".png");
			if (File.Exists(text3))
			{
				return Relative(text3);
			}
		}
		return null;
	}

	private string Relative(string path)
	{
		string text = Path.GetFullPath(path);
		string text2 = Path.GetFullPath(Directory);
		if (text.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
		{
			string text3 = text.Substring(text2.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			return text3.Replace(Path.DirectorySeparatorChar, '/');
		}
		return Path.GetFileName(text);
	}

	/// <summary>
	/// FNV-1a over every file's relative path and bytes, in sorted order.
	///
	/// Content, not the folder name: two players with "Omnirs Weapons" where one edited a damage
	/// number must be told they differ, and the version string in the manifest is optional.
	/// </summary>
	internal static string ContentHash(string directory)
	{
		ulong num = 14695981039346656037uL;
		string[] files;
		try
		{
			files = System.IO.Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
		}
		catch
		{
			return "unreadable";
		}
		Array.Sort(files, StringComparer.OrdinalIgnoreCase);
		string text = Path.GetFullPath(directory);
		byte[] array = new byte[8192];
		for (int i = 0; i < files.Length; i++)
		{
			string text2 = files[i].Substring(text.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
				.Replace(Path.DirectorySeparatorChar, '/');
			// disabled.txt is not content: enabling or disabling a mod must not change its
			// signature, or the host and the client would disagree after a toggle.
			if (text2.Equals("disabled.txt", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			num = Hash(num, Encoding.UTF8.GetBytes(text2));
			try
			{
				using FileStream stream = File.OpenRead(files[i]);
				int num2;
				while ((num2 = stream.Read(array, 0, array.Length)) > 0)
				{
					for (int j = 0; j < num2; j++)
					{
						num ^= array[j];
						num *= 1099511628211;
					}
				}
			}
			catch
			{
				// A file that cannot be read still has to change the hash: "could not read" and
				// "empty" are different contents.
				num = Hash(num, Encoding.UTF8.GetBytes("<unreadable>"));
			}
			// Separate files, so that moving a byte from the end of one into the start of the
			// next is not invisible.
			num = Hash(num, Encoding.UTF8.GetBytes("\n"));
		}
		return num.ToString("x16");
	}

	private static ulong Hash(ulong hash, byte[] bytes)
	{
		for (int i = 0; i < bytes.Length; i++)
		{
			hash ^= bytes[i];
			hash *= 1099511628211;
		}
		return hash;
	}
}
