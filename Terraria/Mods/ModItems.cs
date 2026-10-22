using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Mods;

/// <summary>
/// The registry of items that mods create, and the place every "what is this id" question goes.
///
/// Vanilla ids are a compiled <c>switch</c> inside <c>Item.SetDefaults</c>, so a new item cannot be
/// added there. Instead mod items live in a range above the vanilla ones and are looked up here:
/// <c>Item.SetDefaults</c>, <c>Lang.ItemName</c> and the two draw paths each ask this class first
/// and fall through to the compiled tables when it says no.
///
/// Ids are numbers on the wire and in save files, which is why they are not hashes or strings: the
/// protocol already carries <c>short</c> item ids, and changing that would break every save. The
/// price is that both sides must have the mod - which is exactly what the matching mod set
/// requirement is for.
/// </summary>
internal static class ModItems
{
	/// <summary>
	/// First id a mod item can take. Above <c>Item.MaxNumItemTypes</c> (632) with a wide gap, so
	/// a future content update cannot collide with it, and far below <c>short.MaxValue</c> because
	/// <c>Item.Type</c> is a <c>short</c>.
	/// </summary>
	internal const int IdBase = 20000;

	/// <summary>Highest id that can be handed out. <c>Item.Type</c> is a short.</summary>
	internal const int IdLimit = 32000;

	private sealed class Pending
	{
		public string ModName;

		public string Name;

		public string SpritePath;

		public string ModDirectory;

		public ModItem Item;
	}

	/// <summary>Definitions keyed by id, once ids are assigned.</summary>
	private static readonly Dictionary<int, ModItem> ById = new Dictionary<int, ModItem>();

	/// <summary>Definitions keyed by "mod/name", before and after assignment.</summary>
	private static readonly Dictionary<string, ModItem> ByKey = new Dictionary<string, ModItem>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Which mod first claimed each item name, for the "two mods, one name" warning.
	///
	/// Names are the only way a recipe or <c>Context.Item</c> can refer to a mod's item, so a
	/// duplicate is not harmless even though the registry keys on (mod, name): the lookup has to
	/// return one of them, and the author of the other gets nothing.
	/// </summary>
	private static readonly Dictionary<string, string> NameOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Names refused because a vanilla item already has them.
	///
	/// Kept so a recipe that *makes* such an item can be refused too. Without it the pack's
	/// "Harp" recipe survives the refusal of its item and resolves to the vanilla Harp - a recipe
	/// the author never wrote, turning their ingredients into somebody else's item.
	/// </summary>
	private static readonly HashSet<string> RefusedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	/// <summary>True when a new item with this name was refused because the name was taken.</summary>
	internal static bool WasRefused(string name)
	{
		return name != null && RefusedNames.Contains(name.Trim());
	}

	private static readonly List<Pending> Unassigned = new List<Pending>();

	private static readonly object Sync = new object();

	/// <summary>Sheets, one per mod, keyed by mod name.</summary>
	private static readonly Dictionary<string, ModSheets.Sheet> SheetsByName = new Dictionary<string, ModSheets.Sheet>(StringComparer.OrdinalIgnoreCase);

	/// <summary>How many items are registered. For the log and the tests.</summary>
	internal static int Count
	{
		get
		{
			lock (Sync)
			{
				return ById.Count + Unassigned.Count;
			}
		}
	}

	/// <summary>True when this id belongs to a mod rather than to the compiled tables.</summary>
	internal static bool IsModItem(int id)
	{
		return id >= IdBase && id < IdLimit;
	}

	/// <summary>
	/// Declares a new item. Called from <c>ModContext.DefineItem</c> during <c>Mod.Load</c>.
	///
	/// Returns null with a reason for anything that cannot work - an empty name, a duplicate, or a
	/// sprite that does not exist. The sprite is required on purpose: an item with no picture is
	/// indistinguishable from a bug in the inventory, and the author finds out at load time
	/// instead of from a player.
	/// </summary>
	internal static ModItem Register(string modName, string name, string spritePath, string modDirectory, out string error)
	{
		error = null;
		string text = (name ?? string.Empty).Trim();
		if (text.Length == 0)
		{
			error = "an item needs a name";
			return null;
		}
		if (text.Length > 64)
		{
			error = "the name is longer than 64 characters";
			return null;
		}
		lock (Sync)
		{
			string key = modName + "/" + text;
			if (ByKey.ContainsKey(key))
			{
				error = "this mod already defines an item called '" + text + "'";
				return null;
			}
			// A new item may not take a vanilla item's name.
			//
			// Names are how recipes, `Context.Item` and the tConfig importer refer to items, and
			// `ItemLookup` asks the mod registry first - so a new "Harp" makes every name-based
			// reference to the vanilla Harp resolve to it instead. That is silent: the recipe
			// still works, it just needs a different item, and a mod that meant to adjust the
			// vanilla Harp adjusts a new one. Four items in the packs on this machine do this
			// (`Purple Dye`, `Harp`, `Shotgun`, `Excalibur`).
			//
			// Refused rather than renamed: the author's intent for a name is not ours to guess,
			// and a quietly renamed item is another surprise. Changing an existing item's stats is
			// what `Context.Item` is for.
			int vanilla = Terraria.ItemLookup.FindVanilla(text);
			if (vanilla > 0)
			{
				RefusedNames.Add(text);
				error = "an item called '" + text + "' already exists in this build (id " + vanilla
					+ "), and a new item cannot take its name - every recipe that names it would find"
					+ " this one instead. Rename the new item, or change the existing one with"
					+ " Context.Item";
				return null;
			}
			if (string.IsNullOrEmpty(spritePath))
			{
				error = "an item needs a sprite";
				return null;
			}
			string text2 = System.IO.Path.Combine(modDirectory,
				spritePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
			if (!System.IO.File.Exists(text2))
			{
				error = "no such sprite: " + spritePath;
				return null;
			}
			ModItem modItem = new ModItem();
			modItem.ModName = modName;
			modItem.Name = text;
			modItem.SpritePath = spritePath;
			// Two mods may use the same name - the key is (mod, name) - but then a name-based
			// lookup has to pick one, and the player cannot tell them apart. Allowed, and said out
			// loud, because refusing would be a policy the author cannot work around when the two
			// mods are not theirs.
			string owner;
			if (NameOwners.TryGetValue(text, out owner) && !string.Equals(owner, modName, StringComparison.OrdinalIgnoreCase))
			{
				ModLoader.Log("[Mods] '" + text + "' is defined by both '" + owner + "' and '" + modName
					+ "' - recipes and Context.Item that name it will find whichever loaded first");
			}
			else
			{
				NameOwners[text] = modName;
			}
			ByKey[key] = modItem;
			Pending pending = new Pending();
			pending.ModName = modName;
			pending.Name = text;
			pending.SpritePath = spritePath;
			pending.ModDirectory = modDirectory;
			pending.Item = modItem;
			Unassigned.Add(pending);
			return modItem;
		}
	}

	/// <summary>
	/// Assigns ids to everything registered, in a deterministic order, then packs each mod's
	/// sprites.
	///
	/// The order is (mod name, item name) rather than registration order: registration follows the
	/// mod load order, and two players can legitimately load the same mods in different orders.
	/// Ids travel on the wire, so "same mods, different ids" would be a desync that looks like an
	/// item turning into something else.
	///
	/// Called once, after every mod has loaded.
	/// </summary>
	internal static void Finalize(GraphicsDevice device)
	{
		lock (Sync)
		{
			Unassigned.Sort(delegate(Pending a, Pending b)
			{
				int num = string.Compare(a.ModName, b.ModName, StringComparison.OrdinalIgnoreCase);
				return (num != 0) ? num : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
			});
			// Ids are hashed from "mod/name", not handed out in sequence.
			//
			// Sequence looked simpler and cost the player items: the id is a number in the save
			// file, so a contiguous run means *inserting one item renumbers every item after it*.
			// Add one mod whose name sorts early and the sword a player saved becomes an axe -
			// silently, because the new number is still a defined item. Hashing makes an id depend
			// on the item's own name instead of on how many items happened to come before it, so
			// adding a mod leaves the existing ones where they were. The exception is the few
			// pairs whose hashes collide; the probe below moves the later one, which is a handful
			// of items rather than all of them.
			//
			// Still deterministic for the same mod set, and that is not negotiable: the sort above
			// fixes the order, so the probe walks the same slots on every machine and every run.
			// Ids travel on the wire, and "same mods, different ids" is a desync that looks like
			// an item turning into something else.
			bool[] taken = new bool[IdLimit - IdBase];
			for (int i = 0; i < Unassigned.Count; i++)
			{
				Pending pending0 = Unassigned[i];
				int num2 = PreferredId(pending0.ModName, pending0.Name) - IdBase;
				int num3 = 0;
				while (taken[num2] && num3 < taken.Length)
				{
					num2 = (num2 + 1) % taken.Length;
					num3++;
				}
				if (num3 >= taken.Length)
				{
					ModLoader.Log("[Mods] too many new items: '" + pending0.ModName + "/"
						+ pending0.Name + "' has no id and will not exist");
					continue;
				}
				taken[num2] = true;
				int num4 = IdBase + num2;
				pending0.Item.Type = num4;
				ById[num4] = pending0.Item;
			}
			// Group the sprites per mod so each mod gets one sheet.
			Dictionary<string, Dictionary<string, string>> dictionary =
				new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			for (int j = 0; j < Unassigned.Count; j++)
			{
				Pending pending = Unassigned[j];
				if (pending.Item.Type <= 0)
				{
					continue;
				}
				Dictionary<string, string> value;
				if (!dictionary.TryGetValue(pending.ModName, out value))
				{
					value = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					dictionary[pending.ModName] = value;
					dictionary2[pending.ModName] = pending.ModDirectory;
				}
				// The sprite key is the item name: it is what the sheet is looked up by, and it is
				// unique inside a mod by construction.
				value[pending.Name] = pending.SpritePath;
			}
			foreach (KeyValuePair<string, Dictionary<string, string>> item in dictionary)
			{
				string text = dictionary2[item.Key];
				ModSheets.Sheet sheet = ModSheets.Get(device, item.Key, text, item.Value);
				SheetsByName[item.Key] = sheet;
				if (!sheet.IsUsable)
				{
					ModLoader.Log("[Mods] " + item.Key + ": no sprite sheet (" + sheet.Problem
						+ ") - its items will not be drawable");
				}
			}
			// Point every item at its rectangle; an item whose sprite is missing stays undrawable
			// rather than borrowing somebody else's picture.
			for (int k = 0; k < Unassigned.Count; k++)
			{
				Pending pending2 = Unassigned[k];
				if (pending2.Item.Type <= 0)
				{
					continue;
				}
				ModSheets.Sheet sheet2;
				Rectangle rect;
				pending2.Item.HasSprite = SheetsByName.TryGetValue(pending2.ModName, out sheet2)
					&& sheet2.TryGet(pending2.Name, out rect);
				if (!pending2.Item.HasSprite)
				{
					ModLoader.Log("[Mods] " + pending2.ModName + "/" + pending2.Name
						+ ": sprite '" + pending2.SpritePath + "' is not in the sheet");
				}
				// Descriptions go into the shared table now that the id exists: Lang.ToolTip asks
				// there for both mod items and overridden vanilla ones.
				if (pending2.Item.ToolTipText.Length > 0 || pending2.Item.ToolTip2Text.Length > 0)
				{
					ItemToolTips.Set(pending2.Item.Type, pending2.Item.ToolTipText, pending2.Item.ToolTip2Text);
				}
			}
			int count = ById.Count;
			Unassigned.Clear();
			if (count > 0)
			{
				// Not a range any more: ids are hashed, so the highest one says nothing about the
				// count. Naming a range that is not contiguous would be a lie in the log.
				ModLoader.Log("[Mods] " + count.ToString(CultureInfo.InvariantCulture)
					+ " new item(s) registered, ids in " + IdBase + ".." + (IdLimit - 1));
			}
		}
	}

	/// <summary>
	/// The id an item asks for, derived from its own name and its mod's.
	///
	/// FNV-1a over "mod/name" in lower case, folded into the usable range. Small, dependency-free
	/// and, crucially, <b>stable across runtimes and platforms</b>: the same string has to give the
	/// same number on Linux, on Windows and in the self-test, or two players would disagree about
	/// what id 20043 means. Lower case because the registry treats names case-insensitively, so
	/// "Sword" and "sword" are the same item and must land on the same id.
	///
	/// The number is not an identity by itself - <see cref="Finalize"/> probes past collisions -
	/// but it is what makes an id depend on the item rather than on how many items precede it.
	/// </summary>
	private static int PreferredId(string modName, string name)
	{
		string key = (modName ?? string.Empty).ToLowerInvariant() + "/" + (name ?? string.Empty).ToLowerInvariant();
		uint hash = 2166136261u;
		for (int i = 0; i < key.Length; i++)
		{
			hash ^= key[i];
			hash *= 16777619u;
		}
		return IdBase + (int)(hash % (uint)(IdLimit - IdBase));
	}

	/// <summary>Every registered definition, for the tests and the mod list.</summary>
	internal static List<ModItem> All()
	{
		lock (Sync)
		{
			return new List<ModItem>(ById.Values);
		}
	}

	internal static ModItem Find(int id)
	{
		lock (Sync)
		{
			ModItem result;
			return ById.TryGetValue(id, out result) ? result : null;
		}
	}

	/// <summary>
	/// Fills an item from its definition. False when the id is unknown, in which case the item is
	/// left as an empty slot.
	///
	/// An unknown id is not hypothetical: a save written with a mod, opened without it. Empty is
	/// the only answer that neither crashes nor pretends - and it is logged, because a player who
	/// silently lost their sword deserves to know why.
	/// </summary>
	internal static bool Apply(ref Item item, int id)
	{
		ModItem modItem = Find(id);
		if (modItem == null)
		{
			// Reset to nothing: Type 0 draws as an empty slot, while a Type with no definition
			// would index past the end of every sprite table.
			item.SetDefaults(0);
			ModLoader.Log("[Mods] item id " + id.ToString(CultureInfo.InvariantCulture)
				+ " is not defined (the mod that made it is not loaded) - showing an empty slot");
			return false;
		}
		item.Type = (short)id;
		item.NetID = (short)id;
		item.Active = 1;
		// Sensible defaults before the definition's own fields: a new item that sets only
		// `damage` still has to be usable, and Item's defaults are for a blank item.
		item.MaxStack = 1;
		item.UseTime = 100;
		item.UseAnimation = 100;
		item.Scale = 1f;
		item.Width = 16;
		item.Height = 16;
		foreach (KeyValuePair<string, string> field in modItem.Fields)
		{
			ItemOverrides.ApplyField(ref item, field.Key, field.Value);
		}
		return true;
	}

	/// <summary>
	/// The id of a mod item by its display name, or 0.
	///
	/// Asked before the vanilla table because a mod item's name is not in <see cref="Lang.ItemName"/>'s
	/// compiled switch - and the name lookup caches the vanilla table, which is built before mods
	/// load. Without this, a recipe for a mod item could never be built.
	/// </summary>
	internal static int FindByName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return 0;
		}
		string text = name.Trim();
		lock (Sync)
		{
			foreach (KeyValuePair<int, ModItem> item in ById)
			{
				if (string.Equals(item.Value.Name, text, StringComparison.OrdinalIgnoreCase))
				{
					return item.Key;
				}
			}
		}
		return 0;
	}

	/// <summary>The display name, or null when the id is not a mod item this build knows.</summary>
	internal static string NameOf(int id)
	{
		ModItem modItem = Find(id);
		return (modItem == null) ? null : modItem.Name;
	}

	/// <summary>
	/// The sprite for an item id: the mod's sheet and where the sprite sits in it.
	///
	/// Returns false for an item whose sprite is missing, so the caller can draw a placeholder
	/// instead of indexing somebody else's rectangle.
	/// </summary>
	internal static bool TryGetSprite(int id, out Texture2D texture, out Rectangle rect)
	{
		texture = null;
		rect = default;
		ModItem modItem = Find(id);
		if (modItem == null || !modItem.HasSprite)
		{
			return false;
		}
		ModSheets.Sheet value;
		if (!SheetsByName.TryGetValue(modItem.ModName, out value))
		{
			return false;
		}
		texture = value.Texture;
		return value.TryGet(modItem.Name, out rect);
	}

	/// <summary>Drops every definition and sheet. For a reload and for the tests.</summary>
	internal static void Clear()
	{
		lock (Sync)
		{
			ById.Clear();
			ByKey.Clear();
			NameOwners.Clear();
			RefusedNames.Clear();
			Unassigned.Clear();
			SheetsByName.Clear();
		}
		ModSheets.Clear();
	}
}
