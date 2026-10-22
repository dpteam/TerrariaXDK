using System;
using System.Collections.Generic;
using System.Globalization;

namespace Terraria.Mods;

/// <summary>
/// A crafting recipe declared by a mod.
///
/// The shape is the game's own <see cref="Recipe"/>: one result, up to twelve ingredients, up to
/// three stations. It is a separate type rather than a public <c>Recipe</c> setter because a mod
/// must not be able to reach into the live array - recipes are registered in a batch after all
/// mods have loaded, so ids and the "discovered" bit array are sized correctly.
/// </summary>
public sealed class ModRecipe
{
	internal readonly List<KeyValuePair<string, int>> IngredientNames = new List<KeyValuePair<string, int>>();

	internal readonly List<string> StationNames = new List<string>();

	/// <summary>The item this makes. Set through <see cref="Context.AddRecipe"/>.</summary>
	public string Result { get; internal set; }

	/// <summary>How many are produced per craft.</summary>
	public int Amount { get; set; } = 1;

	public Recipe.Category Category { get; set; } = Recipe.Category.MISC;

	public Recipe.SubCategory SubCategory { get; set; } = Recipe.SubCategory.NONE;

	/// <summary>True when the recipe needs to stand next to water.</summary>
	public bool NeedsWater { get; set; }

	/// <summary>The mod that declared it.</summary>
	public string ModName { get; internal set; }

	/// <summary>Adds an ingredient by the name the game shows. Returns false when it is unknown.</summary>
	public bool AddIngredient(string itemName, int count)
	{
		if (string.IsNullOrEmpty(itemName) || count <= 0)
		{
			return false;
		}
		// Resolved when the recipe is built, not now: the ingredient may be an item another mod
		// defines, and those ids do not exist until every mod has loaded.
		IngredientNames.Add(new KeyValuePair<string, int>(itemName.Trim(), count));
		return true;
	}

	/// <summary>Adds a crafting station by name: "Work Bench", "Anvil", "Furnace", ...</summary>
	public bool AddStation(string stationName)
	{
		if (string.IsNullOrEmpty(stationName))
		{
			return false;
		}
		StationNames.Add(stationName);
		return true;
	}

	/// <summary>One line for the log: "Copper Bulgeon = 6x Copper Bar + 1x Wood @ Anvil".</summary>
	public string Describe()
	{
		System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
		stringBuilder.Append(Result).Append(" = ");
		for (int i = 0; i < IngredientNames.Count; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(" + ");
			}
			stringBuilder.Append(IngredientNames[i].Value.ToString(CultureInfo.InvariantCulture))
				.Append("x ").Append(IngredientNames[i].Key);
		}
		if (StationNames.Count > 0)
		{
			stringBuilder.Append(" @ ");
			for (int j = 0; j < StationNames.Count; j++)
			{
				if (j > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(StationNames[j]);
			}
		}
		return stringBuilder.ToString();
	}
}

/// <summary>
/// The registry of mod recipes, and the conversion of a name-based recipe into the game's
/// id-based <see cref="Recipe"/>.
///
/// Why a separate pass: a recipe names its result and its ingredients, and a mod item has no id
/// until every mod has loaded. So recipes are collected during <c>Mod.Load</c> and turned into
/// <see cref="Recipe"/> objects afterwards, in the same deterministic order as the ids.
/// </summary>
internal static class ModRecipes
{
	/// <summary>
	/// How many recipes mods may add in total. The vanilla array is a fixed 342 and the "discovered"
	/// bit array is sized from it, so this is the one number that has to be chosen rather than
	/// derived.
	/// </summary>
	internal const int Capacity = 2048;

	/// <summary>
	/// Crafting stations, by the English name tConfig packs use, with the id this build gives the
	/// tile.
	///
	/// A fixed table rather than <c>Main.TileNames</c>: that array is refilled by
	/// <see cref="Lang.SetLang"/> in the player's language, so a German client would look up
	/// "Anvil" and find "Amboss" at that index - or worse, a name that means something else.
	/// Recipes are data files, and data files are written in English.
	/// </summary>
	/// <summary>
	/// Name → tile id, for the stations this build actually has.
	///
	/// Every entry must be below <see cref="Player.StationTableBits"/>: a station the build cannot
	/// recognise is worse than a missing station, because the recipe looks craftable and is not.
	/// <see cref="StationId"/> refuses anything above the table anyway; the list is kept honest so
	/// the refusal is not the only thing standing between a pack and an uncraftable recipe.
	/// </summary>
	private static readonly Dictionary<string, int> Stations = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
	{
		{ "Work Bench", 18 },
		{ "Workbench", 18 },
		{ "Anvil", 16 },
		{ "Mythril Anvil", 134 },
		{ "Furnace", 17 },
		{ "Hellforge", 77 },
		{ "Bottle", 13 },
		{ "Alchemy Station", 13 },
		{ "Alchemy Table", 13 },
		{ "Table", 14 },
		{ "Chair", 15 },
		{ "Demon Altar", 26 },
		{ "Altar", 26 },
		{ "Loom", 86 },
		{ "Cooking Pot", 96 },
		{ "Sawmill", 106 },
		{ "Tinkerer's Workshop", 114 },
		{ "Tinkerers Workshop", 114 },
		{ "Bookcase", 101 },
		{ "Dye Vat", 93 },
		{ "Imbuing Station", 103 },
		{ "Keg", 94 },
		// Blend-O-Matic is tile 217 in later PC builds; this build's tile table stops at 134, so
		// the station cannot exist here and is not listed. A pack that names it gets the standard
		// "station is a mod tile this build does not have" note and a craftable recipe, which is
		// better than a recipe that is offered and can never be made.
	};

	private static readonly List<ModRecipe> Pending = new List<ModRecipe>();

	private static readonly object Sync = new object();

	internal static int Count
	{
		get
		{
			lock (Sync)
			{
				return Pending.Count;
			}
		}
	}

	/// <summary>Declares a recipe. Returns null with a reason for anything unusable.</summary>
	internal static ModRecipe Register(string modName, string result, out string error)
	{
		error = null;
		string text = (result ?? string.Empty).Trim();
		if (text.Length == 0)
		{
			error = "a recipe needs a result";
			return null;
		}
		ModRecipe modRecipe = new ModRecipe();
		modRecipe.ModName = modName;
		modRecipe.Result = text;
		lock (Sync)
		{
			Pending.Add(modRecipe);
		}
		return modRecipe;
	}

	/// <summary>
	/// The tile id for a station name, or -1 when this build has no such station.
	///
	/// The range check is not decoration: this build's tile table ends at
	/// <see cref="Player.StationTableBits"/> (135), and a station id above it can never be stood
	/// next to - <c>AdjTiles</c> skips those tiles entirely. Returning the id anyway produced a
	/// recipe that is offered and can never be crafted, and reading the "seen this station" flag
	/// for it threw. Reporting "no such station" makes the caller drop the station and keep the
	/// recipe craftable, which is the documented behaviour for mod-tile stations.
	/// </summary>
	internal static int StationId(string name)
	{
		int result;
		if (name == null || !Stations.TryGetValue(name.Trim(), out result))
		{
			return -1;
		}
		return (result > 0 && result < Terraria.Player.StationTableBits) ? result : -1;
	}

	/// <summary>Every station name this build knows, for the tests and the log.</summary>
	internal static List<string> StationNames()
	{
		List<string> list = new List<string>(Stations.Keys);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	/// <summary>
	/// Turns the collected recipes into the game's array, and returns how many were added.
	///
	/// Sorted by (mod, result) for the same reason item ids are: the recipe index is stored in the
	/// player's "discovered recipes" bit array, so two players with the same mods must agree on
	/// which index is which.
	///
	/// A recipe whose result or ingredient does not exist is reported and skipped - an item that
	/// cannot be made is worse than a recipe that is missing, because the crafting screen would
	/// offer something that produces nothing.
	/// </summary>
	internal static int Apply(out List<string> problems)
	{
		problems = new List<string>();
		lock (Sync)
		{
			Pending.Sort(delegate(ModRecipe a, ModRecipe b)
			{
				int num = string.Compare(a.ModName, b.ModName, StringComparison.OrdinalIgnoreCase);
				return (num != 0) ? num : string.Compare(a.Result, b.Result, StringComparison.OrdinalIgnoreCase);
			});
			int num2 = 0;
			for (int i = 0; i < Pending.Count; i++)
			{
				ModRecipe modRecipe = Pending[i];
				if (Recipe.NumRecipes >= Recipe.MaxNumRecipes + Capacity)
				{
					problems.Add("'" + modRecipe.Result + "': no room for more recipes (limit "
						+ Capacity.ToString(CultureInfo.InvariantCulture) + ")");
					continue;
				}
				Item item = default(Item);
				int type = ItemLookup.Find(modRecipe.Result);
				if (type <= 0)
				{
					problems.Add("'" + modRecipe.Result + "': no such item in this build");
					continue;
				}
				item.SetDefaults(type);
				Recipe recipe = new Recipe();
				recipe.CraftedItem.SetDefaults(item.Type, Math.Max(1, modRecipe.Amount));
				recipe.RecipeCategory = modRecipe.Category;
				recipe.RecipeSubCategory = modRecipe.SubCategory;
				recipe.NeedsWater = modRecipe.NeedsWater;
				int num3 = 0;
				bool flag = false;
				for (int j = 0; j < modRecipe.IngredientNames.Count && num3 < 12; j++)
				{
					// By name through the real table, and after ids are assigned - so an ingredient
					// another mod defines resolves too.
					int num6 = ItemLookup.Find(modRecipe.IngredientNames[j].Key);
					Item item2 = default(Item);
					if (num6 > 0)
					{
						item2.SetDefaults(num6);
					}
					if (item2.Type <= 0)
					{
						problems.Add("'" + modRecipe.Result + "': ingredient '"
							+ modRecipe.IngredientNames[j].Key + "' does not exist");
						flag = true;
						break;
					}
					recipe.RequiredItem[num3].SetDefaults(item2.Type, modRecipe.IngredientNames[j].Value);
					num3++;
				}
				if (flag)
				{
					continue;
				}
				if (num3 == 0)
				{
					problems.Add("'" + modRecipe.Result + "': no ingredients");
					continue;
				}
				int num4 = 0;
				for (int k = 0; k < modRecipe.StationNames.Count && num4 < 3; k++)
				{
					int num5 = StationId(modRecipe.StationNames[k]);
					if (num5 < 0)
					{
						// The station is usually a tile the pack itself adds, and this build has no
						// mod tiles. Refusing the whole recipe would make the item unobtainable,
						// which is worse than making it craftable without its station - so the
						// station is dropped and the caveat is reported.
						problems.Add("'" + modRecipe.Result + "': station '"
							+ modRecipe.StationNames[k] + "' is a mod tile this build does not have - the recipe is craftable without it");
						continue;
					}
					recipe.RequiredTile[num4] = (short)num5;
					num4++;
				}
				if (flag)
				{
					continue;
				}
				recipe.NumRequiredItems = (byte)num3;
				recipe.NumRequiredTiles = (byte)num4;
				Main.ActiveRecipe[Recipe.NumRecipes] = recipe;
				Recipe.NumRecipes++;
				num2++;
			}
			Pending.Clear();
			return num2;
		}
	}

	/// <summary>Drops everything collected. For a reload and for the tests.</summary>
	internal static void Clear()
	{
		lock (Sync)
		{
			Pending.Clear();
		}
	}
}
