using System.Collections.Generic;
using Terraria.Achievements;

namespace Terraria;

public sealed class Recipe
{
	public enum Category : byte
	{
		STRUCTURES,
		TOOLS,
		WEAPONS,
		ARMOR,
		POTIONS,
		MISC,
		NUM_CATEGORIES
	}

	public enum SubCategory : byte
	{
		NONE = 0,
		TABLEWARE = NONE,
		TORCHES = 1,
		WALLS = 2,
		BRICKS = 3,
		LANTERNS = 4,
		CHANDELIERS = 5,
		CHESTS = 6,
		CRAFTING_STATIONS = 7,
		ANVILS = 8,
		STATUES = 9,
		MECHANISM = 10,
		TIMERS = 11,
		PUMPS = 12,
		PICKAXES = TORCHES,
		DRILLS = WALLS,
		AXES = BRICKS,
		CHAINSAWS = LANTERNS,
		HAMMERS = CHANDELIERS,
		HAMAXES = CHESTS,
		BARS = CRAFTING_STATIONS,
		GRAPPLING = ANVILS,
		SWORDS = TORCHES,
		BROADSWORDS = WALLS,
		SHORTSWORDS = BRICKS,
		FLAILS = LANTERNS,
		BOOMERANGS = CHANDELIERS,
		BOWS = CHESTS,
		ARROWS = CRAFTING_STATIONS,
		GUNS = ANVILS,
		MAGIC_GUNS = STATUES,
		SPELLS = MECHANISM,
		REPEATERS = TIMERS,
		BULLETS = PUMPS,
		PHASEBLADES = 13,
		PHASESABERS = 14,
		SPEARS = 15,
		EXPLOSIVES = 16,
		BOOTS = NONE,
		HEADGEAR = TORCHES,
		HELMETS = WALLS,
		MASKS = BRICKS,
		HOODS = LANTERNS,
		HATS = CHANDELIERS,
		GOGGLES = CHESTS,
		BODY = CRAFTING_STATIONS,
		SHIRTS = ANVILS,
		LEGS = STATUES,
		PANTS = MECHANISM,
		WINGS = TIMERS,
		HEALING = TORCHES,
		MANA = WALLS,
		RESTORATION = BRICKS,
		BANNERS = TORCHES,
		WATCHES = WALLS,
		MUSICBOXES = BRICKS,
		EYES = LANTERNS,
		SKULLS = CHANDELIERS
	}

	public sealed class SubCategoryList
	{
		public bool CanCraft;

		public List<short> RecipeList;

		public SubCategoryList(int ReserveIdx)
		{
			CanCraft = false;
			RecipeList = new List<short>(ReserveIdx);
		}

		public void Add(Player CurPlayer, int RecipeIdx)
		{
			RecipeList.Add((short)RecipeIdx);
			if (!CanCraft)
			{
				CanCraft = CurPlayer.CanCraftRecipe(Main.ActiveRecipe[RecipeIdx]);
			}
		}
	}

	public const int MaxItemRequirements = 12;

	public const int MaxTileRequirements = 3;

	public const int MaxNumRecipes = 342;

	/// <summary>
	/// Total slots in <c>Main.ActiveRecipe</c>: the vanilla recipes plus room for mods.
	///
	/// The vanilla array was exactly 342 and every loop said so, which is why a mod could not add
	/// a recipe at all. Loops now run to <see cref="NumRecipes"/>, so the size only has to be big
	/// enough - and the "discovered recipes" bit array is sized from the same constant.
	/// </summary>
	public static int Capacity => MaxNumRecipes + Terraria.Mods.ModRecipes.Capacity;

	public static Recipe NewRecipe = new Recipe();

	public Item CraftedItem;

	public Item[] RequiredItem = new Item[12];

	public short[] RequiredTile = new short[3];

	public byte NumRequiredItems;

	public byte NumRequiredTiles;

	public bool NeedsWater;

	public Category RecipeCategory;

	public SubCategory RecipeSubCategory;

	public static int NumRecipes = 0;

	public static Dictionary<int, SubCategoryList> CurrentRecipes = new Dictionary<int, SubCategoryList>();

	public Recipe()
	{
		for (int i = 0; i < 12; i++)
		{
			RequiredItem[i].Init();
		}
		for (int j = 0; j < 3; j++)
		{
			RequiredTile[j] = -1;
		}
	}

	public void Create(UI ActiveUI)
	{
		for (int i = 0; i < NumRequiredItems; i++)
		{
			int num = RequiredItem[i].Stack;
			for (int j = 0; j < 48; j++)
			{
				if (ActiveUI.ActivePlayer.Inventory[j].NetID == RequiredItem[i].NetID)
				{
					if (ActiveUI.ActivePlayer.Inventory[j].Stack > num)
					{
						ActiveUI.ActivePlayer.Inventory[j].Stack -= (short)num;
						num = 0;
					}
					else
					{
						num -= ActiveUI.ActivePlayer.Inventory[j].Stack;
						ActiveUI.ActivePlayer.Inventory[j].Init();
					}
				}
				if (num <= 0)
				{
					break;
				}
			}
		}
		if ((RequiredTile[0] == 16 || RequiredTile[0] == 134) && ++ActiveUI.TotalAnvilCrafting == 300)
		{
			ActiveUI.AchievementTriggers.SetState(Trigger.UsedLotsOfAnvils, State: true);
		}
		switch (RecipeCategory)
		{
		case Category.STRUCTURES:
			ActiveUI.Statistics.IncreaseStat(StatisticEntry.FurnitureCrafted);
			break;
		case Category.TOOLS:
			ActiveUI.Statistics.IncreaseStat(StatisticEntry.ToolsCrafted);
			break;
		case Category.WEAPONS:
			ActiveUI.Statistics.IncreaseStat(StatisticEntry.WeaponsCrafted);
			break;
		case Category.ARMOR:
			ActiveUI.Statistics.IncreaseStat(StatisticEntry.ArmorCrafted);
			break;
		case Category.POTIONS:
			ActiveUI.Statistics.IncreaseStat(StatisticEntry.ConsumablesCrafted);
			break;
		default:
			ActiveUI.Statistics.IncreaseStat(StatisticEntry.MiscCrafted);
			break;
		}
		switch ((EntityID.ItemID)CraftedItem.Type)
		{
		case EntityID.ItemID.TORCH:
		case EntityID.ItemID.BLUE_TORCH:
		case EntityID.ItemID.RED_TORCH:
		case EntityID.ItemID.GREEN_TORCH:
		case EntityID.ItemID.PURPLE_TORCH:
		case EntityID.ItemID.WHITE_TORCH:
		case EntityID.ItemID.YELLOW_TORCH:
		case EntityID.ItemID.DEMON_TORCH:
			ActiveUI.TotalTorchesCrafted += (uint)CraftedItem.Stack;
			break;
		case EntityID.ItemID.WOOD_PLATFORM:
			ActiveUI.TotalWoodPlatformsCrafted += (uint)CraftedItem.Stack;
			break;
		case EntityID.ItemID.STONE_WALL:
		case EntityID.ItemID.DIRT_WALL:
		case EntityID.ItemID.WOOD_WALL:
		case EntityID.ItemID.PLANKED_WALL:
			ActiveUI.TotalWallsCrafted += (uint)CraftedItem.Stack;
			break;
		case EntityID.ItemID.GOLD_BAR:
		case EntityID.ItemID.COPPER_BAR:
		case EntityID.ItemID.SILVER_BAR:
		case EntityID.ItemID.IRON_BAR:
		case EntityID.ItemID.DEMONITE_BAR:
		case EntityID.ItemID.METEORITE_BAR:
		case EntityID.ItemID.HELLSTONE_BAR:
		case EntityID.ItemID.COBALT_BAR:
		case EntityID.ItemID.MYTHRIL_BAR:
		case EntityID.ItemID.ADAMANTITE_BAR:
			ActiveUI.TotalBarsCrafted += (uint)CraftedItem.Stack;
			if (ActiveUI.TotalBarsCrafted >= 10000)
			{
				ActiveUI.ActivePlayer.AchievementTrigger(Trigger.CreatedLotsOfBars);
			}
			break;
		}
	}

	public static void FindRecipes(UI ActiveUI, Category RecipeCategory, bool IsCraftable)
	{
		int num = 1024;
		ActiveUI.ActivePlayer.UpdateRecipes();
		// Up to NumRecipes, not the vanilla 342: a mod recipe past that index would simply never
		// appear in the crafting screen, and nothing would say why.
		for (int i = 0; i < NumRecipes; i++)
		{
			Recipe recipe = Main.ActiveRecipe[i];
			if (RecipeCategory != recipe.RecipeCategory)
			{
				continue;
			}
			if (IsCraftable)
			{
				if (!ActiveUI.ActivePlayer.CanCraftRecipe(recipe))
				{
					continue;
				}
			}
			else if (ActiveUI.CraftGuide)
			{
				bool flag = false;
				for (int num2 = recipe.NumRequiredItems - 1; num2 >= 0; num2--)
				{
					if (recipe.RequiredItem[num2].NetID == ActiveUI.GuideItem.NetID)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					continue;
				}
			}
			else if (!Main.UnlockAllRecipes && !ActiveUI.ActivePlayer.RecipesFound[i])
			{
				continue;
			}
			int num3 = (int)recipe.RecipeSubCategory;
			if (num3 == 0)
			{
				num3 = num++;
				CurrentRecipes.Add(num3, new SubCategoryList(1));
			}
			else if (!CurrentRecipes.ContainsKey(num3))
			{
				CurrentRecipes.Add(num3, new SubCategoryList(32));
			}
			CurrentRecipes[num3].Add(ActiveUI.ActivePlayer, i);
		}
		Dictionary<int, SubCategoryList>.ValueCollection values = CurrentRecipes.Values;
		ActiveUI.CurrentRecipeCategory.Clear();
		foreach (SubCategoryList item in values)
		{
			if (item.CanCraft)
			{
				ActiveUI.CurrentRecipeCategory.Add(item);
			}
		}
		foreach (SubCategoryList item2 in values)
		{
			if (!item2.CanCraft)
			{
				ActiveUI.CurrentRecipeCategory.Add(item2);
			}
		}
		int count = CurrentRecipes.Count;
		if (count == 0)
		{
			ActiveUI.CraftingRecipe = NewRecipe;
			ActiveUI.CraftingRecipeX = 0;
			ActiveUI.CraftingRecipeWindowX = 0;
			ActiveUI.CraftingRecipeY = 0;
		}
		else
		{
			if (ActiveUI.CraftingRecipeY >= count)
			{
				ActiveUI.CraftingRecipeY = (sbyte)(count - 1);
			}
			int count2 = ActiveUI.CurrentRecipeCategory[ActiveUI.CraftingRecipeY].RecipeList.Count;
			if (ActiveUI.CraftingRecipeX >= count2)
			{
				ActiveUI.CraftingRecipeX = (sbyte)(count2 - 1);
			}
			// The window is the row's leftmost cell and is not the cursor, so it needs its own
			// bound: the rebuilt list can be shorter than the offset the row was scrolled to.
			if (count2 <= 0)
			{
				ActiveUI.CraftingRecipeWindowX = 0;
			}
			else if (ActiveUI.CraftingRecipeWindowX >= count2 || ActiveUI.CraftingRecipeWindowX < 0)
			{
				ActiveUI.CraftingRecipeWindowX = 0;
			}
		}
		CurrentRecipes.Clear();
	}

	public static void SetupRecipes()
	{
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(36);
		NewRecipe.RequiredItem[0].SetDefaults(9, 10);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(8, 3);
		NewRecipe.RequiredItem[0].SetDefaults(23);
		NewRecipe.RequiredItem[1].SetDefaults(9);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(351);
		NewRecipe.RequiredItem[0].SetDefaults(170);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(357);
		NewRecipe.RequiredItem[0].SetDefaults(356);
		NewRecipe.RequiredItem[1].SetDefaults(5);
		NewRecipe.RequiredItem[2].SetDefaults(261);
		NewRecipe.RequiredTile[0] = 96;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(31, 2);
		NewRecipe.RequiredItem[0].SetDefaults(170);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(126);
		NewRecipe.RequiredItem[0].SetDefaults(31);
		NewRecipe.NeedsWater = true;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(422, 5);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(501, 5);
		NewRecipe.RequiredItem[2].SetDefaults(369);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(423, 5);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(370);
		NewRecipe.RequiredItem[2].SetDefaults(59);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(353);
		NewRecipe.RequiredItem[0].SetDefaults(351);
		NewRecipe.RequiredTile[0] = 94;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(28, 2);
		NewRecipe.RequiredItem[0].SetDefaults(5);
		NewRecipe.RequiredItem[1].SetDefaults(23, 2);
		NewRecipe.RequiredItem[2].SetDefaults(31, 2);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(188);
		NewRecipe.RequiredItem[0].SetDefaults(28, 2);
		NewRecipe.RequiredItem[1].SetDefaults(183);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(499);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(501, 3);
		NewRecipe.RequiredItem[2].SetDefaults(502);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(110, 2);
		NewRecipe.RequiredItem[0].SetDefaults(75);
		NewRecipe.RequiredItem[1].SetDefaults(23, 2);
		NewRecipe.RequiredItem[2].SetDefaults(31, 2);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(189);
		NewRecipe.RequiredItem[0].SetDefaults(110, 2);
		NewRecipe.RequiredItem[1].SetDefaults(183);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(226);
		NewRecipe.RequiredItem[0].SetDefaults(28);
		NewRecipe.RequiredItem[1].SetDefaults(110);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(227);
		NewRecipe.RequiredItem[0].SetDefaults(188);
		NewRecipe.RequiredItem[1].SetDefaults(189);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(288);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(318);
		NewRecipe.RequiredItem[2].SetDefaults(317);
		NewRecipe.RequiredItem[3].SetDefaults(173);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(289);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(313);
		NewRecipe.RequiredItem[2].SetDefaults(5);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(290);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(315);
		NewRecipe.RequiredItem[2].SetDefaults(276);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(291);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(317);
		NewRecipe.RequiredItem[2].SetDefaults(275);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(292);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(313);
		NewRecipe.RequiredItem[2].SetDefaults(11);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(293);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(314);
		NewRecipe.RequiredItem[2].SetDefaults(313);
		NewRecipe.RequiredItem[3].SetDefaults(75);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(294);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(314);
		NewRecipe.RequiredItem[2].SetDefaults(316);
		NewRecipe.RequiredItem[3].SetDefaults(75);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(295);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(313);
		NewRecipe.RequiredItem[2].SetDefaults(315);
		NewRecipe.RequiredItem[3].SetDefaults(320);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(296);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(315);
		NewRecipe.RequiredItem[2].SetDefaults(314);
		NewRecipe.RequiredItem[3].SetDefaults(13);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(297);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(315);
		NewRecipe.RequiredItem[2].SetDefaults(314);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(298);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(313);
		NewRecipe.RequiredItem[2].SetDefaults(183);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(299);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(313);
		NewRecipe.RequiredItem[2].SetDefaults(315);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(300);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(316);
		NewRecipe.RequiredItem[2].SetDefaults(68);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(301);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(316);
		NewRecipe.RequiredItem[2].SetDefaults(276);
		NewRecipe.RequiredItem[3].SetDefaults(69);
		NewRecipe.RequiredItem[4].SetDefaults(209);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(302);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(317);
		NewRecipe.RequiredItem[2].SetDefaults(319);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(303);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(313);
		NewRecipe.RequiredItem[2].SetDefaults(38);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(304);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(313);
		NewRecipe.RequiredItem[2].SetDefaults(315);
		NewRecipe.RequiredItem[3].SetDefaults(319);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(305);
		NewRecipe.RequiredItem[0].SetDefaults(126);
		NewRecipe.RequiredItem[1].SetDefaults(318);
		NewRecipe.RequiredItem[2].SetDefaults(316);
		NewRecipe.RequiredItem[3].SetDefaults(315);
		NewRecipe.RequiredItem[4].SetDefaults(320);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(40, 5);
		NewRecipe.RequiredItem[0].SetDefaults(9);
		NewRecipe.RequiredItem[1].SetDefaults(3);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(41, 3);
		NewRecipe.RequiredItem[0].SetDefaults(40, 3);
		NewRecipe.RequiredItem[1].SetDefaults(8);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(47, 3);
		NewRecipe.RequiredItem[0].SetDefaults(40, 3);
		NewRecipe.RequiredItem[1].SetDefaults(69);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(265, 10);
		NewRecipe.RequiredItem[0].SetDefaults(40, 10);
		NewRecipe.RequiredItem[1].SetDefaults(175);
		NewRecipe.RequiredItem[2].SetDefaults(8, 5);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(545, 15);
		NewRecipe.RequiredItem[0].SetDefaults(40, 15);
		NewRecipe.RequiredItem[1].SetDefaults(522);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(516, 35);
		NewRecipe.RequiredItem[0].SetDefaults(40, 35);
		NewRecipe.RequiredItem[1].SetDefaults(501, 6);
		NewRecipe.RequiredItem[2].SetDefaults(526);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.PUMPS;
		NewRecipe.CraftedItem.SetDefaults(234, 25);
		NewRecipe.RequiredItem[0].SetDefaults(97, 25);
		NewRecipe.RequiredItem[1].SetDefaults(117);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.PUMPS;
		NewRecipe.CraftedItem.SetDefaults(515, 25);
		NewRecipe.RequiredItem[0].SetDefaults(97, 25);
		NewRecipe.RequiredItem[1].SetDefaults(502);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.PUMPS;
		NewRecipe.CraftedItem.SetDefaults(546, 25);
		NewRecipe.RequiredItem[0].SetDefaults(97, 25);
		NewRecipe.RequiredItem[1].SetDefaults(522);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(255);
		NewRecipe.RequiredItem[0].SetDefaults(31);
		NewRecipe.RequiredItem[1].SetDefaults(194, 3);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(67, 5);
		NewRecipe.RequiredItem[0].SetDefaults(60);
		NewRecipe.RequiredTile[0] = 13;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(287, 20);
		NewRecipe.RequiredItem[0].SetDefaults(279, 20);
		NewRecipe.RequiredItem[1].SetDefaults(67);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(427, 3);
		NewRecipe.RequiredItem[0].SetDefaults(8, 3);
		NewRecipe.RequiredItem[1].SetDefaults(177);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(428, 3);
		NewRecipe.RequiredItem[0].SetDefaults(8, 3);
		NewRecipe.RequiredItem[1].SetDefaults(178);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(429, 3);
		NewRecipe.RequiredItem[0].SetDefaults(8, 3);
		NewRecipe.RequiredItem[1].SetDefaults(179);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(432, 3);
		NewRecipe.RequiredItem[0].SetDefaults(8, 3);
		NewRecipe.RequiredItem[1].SetDefaults(180);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(430, 3);
		NewRecipe.RequiredItem[0].SetDefaults(8, 3);
		NewRecipe.RequiredItem[1].SetDefaults(181);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(431, 3);
		NewRecipe.RequiredItem[0].SetDefaults(8, 3);
		NewRecipe.RequiredItem[1].SetDefaults(182);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(523, 33);
		NewRecipe.RequiredItem[0].SetDefaults(8, 33);
		NewRecipe.RequiredItem[1].SetDefaults(522);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.LANTERNS;
		NewRecipe.CraftedItem.SetDefaults(344);
		NewRecipe.RequiredItem[0].SetDefaults(225, 5);
		NewRecipe.RequiredItem[1].SetDefaults(8);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.LANTERNS;
		NewRecipe.CraftedItem.SetDefaults(342);
		NewRecipe.RequiredItem[0].SetDefaults(8);
		NewRecipe.RequiredItem[1].SetDefaults(9, 3);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.LANTERNS;
		NewRecipe.CraftedItem.SetDefaults(341);
		NewRecipe.RequiredItem[0].SetDefaults(22, 3);
		NewRecipe.RequiredItem[1].SetDefaults(170, 2);
		NewRecipe.RequiredItem[2].SetDefaults(8);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.LANTERNS;
		NewRecipe.CraftedItem.SetDefaults(347);
		NewRecipe.RequiredItem[0].SetDefaults(154, 10);
		NewRecipe.RequiredItem[1].SetDefaults(8);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.EXPLOSIVES;
		NewRecipe.CraftedItem.SetDefaults(235);
		NewRecipe.RequiredItem[0].SetDefaults(166);
		NewRecipe.RequiredItem[1].SetDefaults(23, 5);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(286);
		NewRecipe.RequiredItem[0].SetDefaults(282);
		NewRecipe.RequiredItem[1].SetDefaults(23);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(170);
		NewRecipe.RequiredItem[0].SetDefaults(169, 2);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(392, 4);
		NewRecipe.RequiredItem[0].SetDefaults(170);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(222);
		NewRecipe.RequiredItem[0].SetDefaults(133, 6);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(350);
		NewRecipe.RequiredItem[0].SetDefaults(133, 4);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(356);
		NewRecipe.RequiredItem[0].SetDefaults(133, 2);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(129);
		NewRecipe.RequiredItem[0].SetDefaults(3, 2);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(130, 4);
		NewRecipe.RequiredItem[0].SetDefaults(129);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(131);
		NewRecipe.RequiredItem[0].SetDefaults(133, 2);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(132, 4);
		NewRecipe.RequiredItem[0].SetDefaults(131);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(145);
		NewRecipe.RequiredItem[0].SetDefaults(3);
		NewRecipe.RequiredItem[1].SetDefaults(12);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(146, 4);
		NewRecipe.RequiredItem[0].SetDefaults(145);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(143);
		NewRecipe.RequiredItem[0].SetDefaults(3);
		NewRecipe.RequiredItem[1].SetDefaults(14);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(144, 4);
		NewRecipe.RequiredItem[0].SetDefaults(143);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(141);
		NewRecipe.RequiredItem[0].SetDefaults(3);
		NewRecipe.RequiredItem[1].SetDefaults(13);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(142, 4);
		NewRecipe.RequiredItem[0].SetDefaults(141);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(214);
		NewRecipe.RequiredItem[0].SetDefaults(174);
		NewRecipe.RequiredItem[1].SetDefaults(3);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(192);
		NewRecipe.RequiredItem[0].SetDefaults(173, 2);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(330, 4);
		NewRecipe.RequiredItem[0].SetDefaults(192);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(594);
		NewRecipe.RequiredItem[0].SetDefaults(593, 2);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(595, 4);
		NewRecipe.RequiredItem[0].SetDefaults(594);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(587, 4);
		NewRecipe.RequiredItem[0].SetDefaults(586);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(592, 4);
		NewRecipe.RequiredItem[0].SetDefaults(591);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(412);
		NewRecipe.RequiredItem[0].SetDefaults(408);
		NewRecipe.RequiredItem[1].SetDefaults(409);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(417, 4);
		NewRecipe.RequiredItem[0].SetDefaults(412);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(413);
		NewRecipe.RequiredItem[0].SetDefaults(3);
		NewRecipe.RequiredItem[1].SetDefaults(172);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(418, 4);
		NewRecipe.RequiredItem[0].SetDefaults(413);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(414);
		NewRecipe.RequiredItem[0].SetDefaults(3);
		NewRecipe.RequiredItem[1].SetDefaults(176);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(419, 4);
		NewRecipe.RequiredItem[0].SetDefaults(414);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(415);
		NewRecipe.RequiredItem[0].SetDefaults(3);
		NewRecipe.RequiredItem[1].SetDefaults(364);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(420, 4);
		NewRecipe.RequiredItem[0].SetDefaults(415);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(416);
		NewRecipe.RequiredItem[0].SetDefaults(3);
		NewRecipe.RequiredItem[1].SetDefaults(365);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(421, 4);
		NewRecipe.RequiredItem[0].SetDefaults(416);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(577, 5);
		NewRecipe.RequiredItem[0].SetDefaults(56);
		NewRecipe.RequiredItem[1].SetDefaults(61, 5);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(176);
		NewRecipe.RequiredItem[0].SetDefaults(2);
		NewRecipe.NeedsWater = true;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(30, 4);
		NewRecipe.RequiredItem[0].SetDefaults(2);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(26, 4);
		NewRecipe.RequiredItem[0].SetDefaults(3);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(93, 4);
		NewRecipe.RequiredItem[0].SetDefaults(9);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(94);
		NewRecipe.RequiredItem[0].SetDefaults(9);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(25);
		NewRecipe.RequiredItem[0].SetDefaults(9, 6);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(34);
		NewRecipe.RequiredItem[0].SetDefaults(9, 4);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(171);
		NewRecipe.RequiredItem[0].SetDefaults(9, 6);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.CHESTS;
		NewRecipe.CraftedItem.SetDefaults(48);
		NewRecipe.RequiredItem[0].SetDefaults(9, 8);
		NewRecipe.RequiredItem[1].SetDefaults(22, 2);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(32);
		NewRecipe.RequiredItem[0].SetDefaults(9, 8);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(479, 4);
		NewRecipe.RequiredItem[0].SetDefaults(3);
		NewRecipe.RequiredItem[1].SetDefaults(9);
		NewRecipe.RequiredTile[0] = 106;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(480, 2);
		NewRecipe.RequiredItem[0].SetDefaults(9);
		NewRecipe.RequiredTile[0] = 106;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(498);
		NewRecipe.RequiredItem[0].SetDefaults(9, 20);
		NewRecipe.RequiredTile[0] = 106;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(224);
		NewRecipe.RequiredItem[0].SetDefaults(9, 15);
		NewRecipe.RequiredItem[1].SetDefaults(225, 5);
		NewRecipe.RequiredTile[0] = 106;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(354);
		NewRecipe.RequiredItem[0].SetDefaults(9, 20);
		NewRecipe.RequiredItem[1].SetDefaults(149, 10);
		NewRecipe.RequiredTile[0] = 106;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(343);
		NewRecipe.RequiredItem[0].SetDefaults(9, 9);
		NewRecipe.RequiredItem[1].SetDefaults(22);
		NewRecipe.RequiredTile[0] = 106;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(359);
		NewRecipe.RequiredItem[0].SetDefaults(19, 3);
		NewRecipe.RequiredItem[1].SetDefaults(170, 6);
		NewRecipe.RequiredItem[2].SetDefaults(9, 10);
		NewRecipe.RequiredTile[0] = 106;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(352);
		NewRecipe.RequiredItem[0].SetDefaults(9, 14);
		NewRecipe.RequiredTile[0] = 106;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(333);
		NewRecipe.RequiredItem[0].SetDefaults(154, 4);
		NewRecipe.RequiredItem[1].SetDefaults(9, 15);
		NewRecipe.RequiredItem[2].SetDefaults(149);
		NewRecipe.RequiredTile[0] = 106;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(332);
		NewRecipe.RequiredItem[0].SetDefaults(9, 12);
		NewRecipe.RequiredTile[0] = 106;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(334);
		NewRecipe.RequiredItem[0].SetDefaults(9, 16);
		NewRecipe.RequiredTile[0] = 106;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(335);
		NewRecipe.RequiredItem[0].SetDefaults(9, 8);
		NewRecipe.RequiredTile[0] = 106;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(363);
		NewRecipe.RequiredItem[0].SetDefaults(9, 10);
		NewRecipe.RequiredItem[1].SetDefaults(22, 2);
		NewRecipe.RequiredItem[2].SetDefaults(85);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(24);
		NewRecipe.RequiredItem[0].SetDefaults(9, 7);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.SetDefaults(196);
		NewRecipe.RequiredItem[0].SetDefaults(9, 8);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CHESTS;
		NewRecipe.CraftedItem.SetDefaults(39);
		NewRecipe.RequiredItem[0].SetDefaults(9, 10);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(361);
		NewRecipe.RequiredItem[0].SetDefaults(362, 10);
		NewRecipe.RequiredItem[1].SetDefaults(9, 5);
		NewRecipe.RequiredTile[0] = 86;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(225);
		NewRecipe.RequiredItem[0].SetDefaults(150, 10);
		NewRecipe.RequiredTile[0] = 86;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(337);
		NewRecipe.RequiredItem[0].SetDefaults(225, 3);
		NewRecipe.RequiredTile[0] = 86;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(338);
		NewRecipe.RequiredItem[0].SetDefaults(225, 3);
		NewRecipe.RequiredTile[0] = 86;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(339);
		NewRecipe.RequiredItem[0].SetDefaults(225, 3);
		NewRecipe.RequiredTile[0] = 86;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(340);
		NewRecipe.RequiredItem[0].SetDefaults(225, 3);
		NewRecipe.RequiredTile[0] = 86;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.SetDefaults(247);
		NewRecipe.RequiredItem[0].SetDefaults(225, 20);
		NewRecipe.RequiredItem[1].SetDefaults(255, 3);
		NewRecipe.RequiredTile[0] = 86;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.ANVILS;
		NewRecipe.CraftedItem.SetDefaults(248);
		NewRecipe.RequiredItem[0].SetDefaults(225, 20);
		NewRecipe.RequiredItem[1].SetDefaults(255, 3);
		NewRecipe.RequiredTile[0] = 86;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.MECHANISM;
		NewRecipe.CraftedItem.SetDefaults(249);
		NewRecipe.RequiredItem[0].SetDefaults(225, 20);
		NewRecipe.RequiredItem[1].SetDefaults(255, 3);
		NewRecipe.RequiredTile[0] = 86;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.ANVILS;
		NewRecipe.CraftedItem.SetDefaults(240);
		NewRecipe.RequiredItem[0].SetDefaults(225, 20);
		NewRecipe.RequiredItem[1].SetDefaults(254, 3);
		NewRecipe.RequiredTile[0] = 86;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.MECHANISM;
		NewRecipe.CraftedItem.SetDefaults(241);
		NewRecipe.RequiredItem[0].SetDefaults(225, 20);
		NewRecipe.RequiredItem[1].SetDefaults(254, 3);
		NewRecipe.RequiredTile[0] = 86;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(262);
		NewRecipe.RequiredItem[0].SetDefaults(225, 30);
		NewRecipe.RequiredItem[1].SetDefaults(178, 3);
		NewRecipe.RequiredTile[0] = 86;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(259);
		NewRecipe.RequiredItem[0].SetDefaults(68, 5);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.ANVILS;
		NewRecipe.CraftedItem.SetDefaults(252);
		NewRecipe.RequiredItem[0].SetDefaults(259, 15);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.MECHANISM;
		NewRecipe.CraftedItem.SetDefaults(253);
		NewRecipe.RequiredItem[0].SetDefaults(259, 15);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(250);
		NewRecipe.RequiredItem[0].SetDefaults(261, 2);
		NewRecipe.RequiredItem[1].SetDefaults(126);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(33);
		NewRecipe.RequiredItem[0].SetDefaults(3, 20);
		NewRecipe.RequiredItem[1].SetDefaults(9, 4);
		NewRecipe.RequiredItem[2].SetDefaults(8, 3);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(360);
		NewRecipe.RequiredItem[0].SetDefaults(3, 100);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(20);
		NewRecipe.RequiredItem[0].SetDefaults(12, 3);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.NetDefaults(-13);
		NewRecipe.RequiredItem[0].SetDefaults(20, 12);
		NewRecipe.RequiredItem[1].SetDefaults(9, 4);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.NetDefaults(-16);
		NewRecipe.RequiredItem[0].SetDefaults(20, 9);
		NewRecipe.RequiredItem[1].SetDefaults(9, 3);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.NetDefaults(-17);
		NewRecipe.RequiredItem[0].SetDefaults(20, 10);
		NewRecipe.RequiredItem[1].SetDefaults(9, 3);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.NetDefaults(-14);
		NewRecipe.RequiredItem[0].SetDefaults(20, 8);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.NetDefaults(-15);
		NewRecipe.RequiredItem[0].SetDefaults(20, 7);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CHESTS;
		NewRecipe.CraftedItem.NetDefaults(-18);
		NewRecipe.RequiredItem[0].SetDefaults(20, 7);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(89);
		NewRecipe.RequiredItem[0].SetDefaults(20, 15);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(80);
		NewRecipe.RequiredItem[0].SetDefaults(20, 25);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(76);
		NewRecipe.RequiredItem[0].SetDefaults(20, 20);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(15);
		NewRecipe.RequiredItem[0].SetDefaults(20, 10);
		NewRecipe.RequiredItem[1].SetDefaults(85);
		NewRecipe.RequiredTile[0] = 14;
		NewRecipe.RequiredTile[1] = 15;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.SetDefaults(106);
		NewRecipe.RequiredItem[0].SetDefaults(20, 4);
		NewRecipe.RequiredItem[1].SetDefaults(8, 4);
		NewRecipe.RequiredItem[2].SetDefaults(85);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(22);
		NewRecipe.RequiredItem[0].SetDefaults(11, 3);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.ANVILS;
		NewRecipe.CraftedItem.SetDefaults(35);
		NewRecipe.RequiredItem[0].SetDefaults(22, 5);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(205);
		NewRecipe.RequiredItem[0].SetDefaults(22, 3);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(348);
		NewRecipe.RequiredItem[0].SetDefaults(22, 8);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(336);
		NewRecipe.RequiredItem[0].SetDefaults(22, 14);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(358);
		NewRecipe.RequiredItem[0].SetDefaults(22, 6);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(345);
		NewRecipe.RequiredItem[0].SetDefaults(22, 10);
		NewRecipe.RequiredItem[1].SetDefaults(9, 2);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(1);
		NewRecipe.RequiredItem[0].SetDefaults(22, 12);
		NewRecipe.RequiredItem[1].SetDefaults(9, 3);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(10);
		NewRecipe.RequiredItem[0].SetDefaults(22, 9);
		NewRecipe.RequiredItem[1].SetDefaults(9, 3);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.SetDefaults(7);
		NewRecipe.RequiredItem[0].SetDefaults(22, 10);
		NewRecipe.RequiredItem[1].SetDefaults(9, 3);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(4);
		NewRecipe.RequiredItem[0].SetDefaults(22, 8);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(6);
		NewRecipe.RequiredItem[0].SetDefaults(22, 7);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CHESTS;
		NewRecipe.CraftedItem.SetDefaults(99);
		NewRecipe.RequiredItem[0].SetDefaults(22, 7);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(90);
		NewRecipe.RequiredItem[0].SetDefaults(22, 20);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(81);
		NewRecipe.RequiredItem[0].SetDefaults(22, 30);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(77);
		NewRecipe.RequiredItem[0].SetDefaults(22, 25);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(85);
		NewRecipe.RequiredItem[0].SetDefaults(22, 3);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(21);
		NewRecipe.RequiredItem[0].SetDefaults(14, 4);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.NetDefaults(-7);
		NewRecipe.RequiredItem[0].SetDefaults(21, 12);
		NewRecipe.RequiredItem[1].SetDefaults(9, 4);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.NetDefaults(-10);
		NewRecipe.RequiredItem[0].SetDefaults(21, 9);
		NewRecipe.RequiredItem[1].SetDefaults(9, 3);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.NetDefaults(-11);
		NewRecipe.RequiredItem[0].SetDefaults(21, 10);
		NewRecipe.RequiredItem[1].SetDefaults(9, 3);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.NetDefaults(-8);
		NewRecipe.RequiredItem[0].SetDefaults(21, 8);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.NetDefaults(-9);
		NewRecipe.RequiredItem[0].SetDefaults(21, 6);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CHESTS;
		NewRecipe.CraftedItem.NetDefaults(-12);
		NewRecipe.RequiredItem[0].SetDefaults(21, 7);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(91);
		NewRecipe.RequiredItem[0].SetDefaults(21, 20);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(82);
		NewRecipe.RequiredItem[0].SetDefaults(21, 30);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(78);
		NewRecipe.RequiredItem[0].SetDefaults(21, 25);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(16);
		NewRecipe.RequiredItem[0].SetDefaults(21, 10);
		NewRecipe.RequiredItem[1].SetDefaults(85);
		NewRecipe.RequiredTile[0] = 14;
		NewRecipe.RequiredTile[1] = 15;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.SetDefaults(107);
		NewRecipe.RequiredItem[0].SetDefaults(21, 4);
		NewRecipe.RequiredItem[1].SetDefaults(8, 4);
		NewRecipe.RequiredItem[2].SetDefaults(85);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(19);
		NewRecipe.RequiredItem[0].SetDefaults(13, 4);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.NetDefaults(-1);
		NewRecipe.RequiredItem[0].SetDefaults(19, 12);
		NewRecipe.RequiredItem[1].SetDefaults(9, 4);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.NetDefaults(-4);
		NewRecipe.RequiredItem[0].SetDefaults(19, 9);
		NewRecipe.RequiredItem[1].SetDefaults(9, 3);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.NetDefaults(-5);
		NewRecipe.RequiredItem[0].SetDefaults(19, 10);
		NewRecipe.RequiredItem[1].SetDefaults(9, 3);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.NetDefaults(-2);
		NewRecipe.RequiredItem[0].SetDefaults(19, 8);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.NetDefaults(-3);
		NewRecipe.RequiredItem[0].SetDefaults(19, 7);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CHESTS;
		NewRecipe.CraftedItem.NetDefaults(-6);
		NewRecipe.RequiredItem[0].SetDefaults(19, 7);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(92);
		NewRecipe.RequiredItem[0].SetDefaults(19, 25);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(83);
		NewRecipe.RequiredItem[0].SetDefaults(19, 35);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(79);
		NewRecipe.RequiredItem[0].SetDefaults(19, 30);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(17);
		NewRecipe.RequiredItem[0].SetDefaults(19, 10);
		NewRecipe.RequiredItem[1].SetDefaults(85);
		NewRecipe.RequiredTile[0] = 14;
		NewRecipe.RequiredTile[1] = 15;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(264);
		NewRecipe.RequiredItem[0].SetDefaults(19, 30);
		NewRecipe.RequiredItem[1].SetDefaults(178);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.SetDefaults(108);
		NewRecipe.RequiredItem[0].SetDefaults(19, 4);
		NewRecipe.RequiredItem[1].SetDefaults(8, 4);
		NewRecipe.RequiredItem[2].SetDefaults(85);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(355);
		NewRecipe.RequiredItem[0].SetDefaults(225, 20);
		NewRecipe.RequiredItem[1].SetDefaults(19, 30);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.LANTERNS;
		NewRecipe.CraftedItem.SetDefaults(105);
		NewRecipe.RequiredItem[0].SetDefaults(19);
		NewRecipe.RequiredItem[1].SetDefaults(8);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.LANTERNS;
		NewRecipe.CraftedItem.SetDefaults(349);
		NewRecipe.RequiredItem[0].SetDefaults(19, 5);
		NewRecipe.RequiredItem[1].SetDefaults(8, 3);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(57);
		NewRecipe.RequiredItem[0].SetDefaults(56, 4);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CHESTS;
		NewRecipe.CraftedItem.SetDefaults(44);
		NewRecipe.RequiredItem[0].SetDefaults(57, 8);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(45);
		NewRecipe.RequiredItem[0].SetDefaults(57, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(46);
		NewRecipe.RequiredItem[0].SetDefaults(57, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(102);
		NewRecipe.RequiredItem[0].SetDefaults(57, 15);
		NewRecipe.RequiredItem[1].SetDefaults(86, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(101);
		NewRecipe.RequiredItem[0].SetDefaults(57, 25);
		NewRecipe.RequiredItem[1].SetDefaults(86, 20);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(100);
		NewRecipe.RequiredItem[0].SetDefaults(57, 20);
		NewRecipe.RequiredItem[1].SetDefaults(86, 15);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(103);
		NewRecipe.RequiredItem[0].SetDefaults(57, 12);
		NewRecipe.RequiredItem[1].SetDefaults(86, 6);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.SetDefaults(104);
		NewRecipe.RequiredItem[0].SetDefaults(57, 10);
		NewRecipe.RequiredItem[1].SetDefaults(86, 5);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.ANVILS;
		NewRecipe.CraftedItem.SetDefaults(84);
		NewRecipe.RequiredItem[0].SetDefaults(85, 3);
		NewRecipe.RequiredItem[1].SetDefaults(118);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(117);
		NewRecipe.RequiredItem[0].SetDefaults(116, 4);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.PHASEBLADES;
		NewRecipe.CraftedItem.SetDefaults(198);
		NewRecipe.RequiredItem[0].SetDefaults(117, 20);
		NewRecipe.RequiredItem[1].SetDefaults(177, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.PHASEBLADES;
		NewRecipe.CraftedItem.SetDefaults(199);
		NewRecipe.RequiredItem[0].SetDefaults(117, 20);
		NewRecipe.RequiredItem[1].SetDefaults(178, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.PHASEBLADES;
		NewRecipe.CraftedItem.SetDefaults(200);
		NewRecipe.RequiredItem[0].SetDefaults(117, 20);
		NewRecipe.RequiredItem[1].SetDefaults(179, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.PHASEBLADES;
		NewRecipe.CraftedItem.SetDefaults(201);
		NewRecipe.RequiredItem[0].SetDefaults(117, 20);
		NewRecipe.RequiredItem[1].SetDefaults(181, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.PHASEBLADES;
		NewRecipe.CraftedItem.SetDefaults(202);
		NewRecipe.RequiredItem[0].SetDefaults(117, 20);
		NewRecipe.RequiredItem[1].SetDefaults(182, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.PHASEBLADES;
		NewRecipe.CraftedItem.SetDefaults(203);
		NewRecipe.RequiredItem[0].SetDefaults(117, 20);
		NewRecipe.RequiredItem[1].SetDefaults(180, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.PHASESABERS;
		NewRecipe.CraftedItem.NetDefaults(-19);
		NewRecipe.RequiredItem[0].SetDefaults(198);
		NewRecipe.RequiredItem[1].SetDefaults(502, 50);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.PHASESABERS;
		NewRecipe.CraftedItem.NetDefaults(-20);
		NewRecipe.RequiredItem[0].SetDefaults(199);
		NewRecipe.RequiredItem[1].SetDefaults(502, 50);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.PHASESABERS;
		NewRecipe.CraftedItem.NetDefaults(-21);
		NewRecipe.RequiredItem[0].SetDefaults(200);
		NewRecipe.RequiredItem[1].SetDefaults(502, 50);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.PHASESABERS;
		NewRecipe.CraftedItem.NetDefaults(-22);
		NewRecipe.RequiredItem[0].SetDefaults(201);
		NewRecipe.RequiredItem[1].SetDefaults(502, 50);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.PHASESABERS;
		NewRecipe.CraftedItem.NetDefaults(-23);
		NewRecipe.RequiredItem[0].SetDefaults(202);
		NewRecipe.RequiredItem[1].SetDefaults(502, 50);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.PHASESABERS;
		NewRecipe.CraftedItem.NetDefaults(-24);
		NewRecipe.RequiredItem[0].SetDefaults(203);
		NewRecipe.RequiredItem[1].SetDefaults(502, 50);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CHESTS;
		NewRecipe.CraftedItem.SetDefaults(204);
		NewRecipe.RequiredItem[0].SetDefaults(117, 35);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(127);
		NewRecipe.RequiredItem[0].SetDefaults(95);
		NewRecipe.RequiredItem[1].SetDefaults(117, 30);
		NewRecipe.RequiredItem[2].SetDefaults(75, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.TIMERS;
		NewRecipe.CraftedItem.SetDefaults(197);
		NewRecipe.RequiredItem[0].SetDefaults(98);
		NewRecipe.RequiredItem[1].SetDefaults(117, 20);
		NewRecipe.RequiredItem[2].SetDefaults(75, 5);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(123);
		NewRecipe.RequiredItem[0].SetDefaults(117, 20);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(124);
		NewRecipe.RequiredItem[0].SetDefaults(117, 30);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(125);
		NewRecipe.RequiredItem[0].SetDefaults(117, 25);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(151);
		NewRecipe.RequiredItem[0].SetDefaults(154, 40);
		NewRecipe.RequiredItem[1].SetDefaults(150, 40);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(152);
		NewRecipe.RequiredItem[0].SetDefaults(154, 60);
		NewRecipe.RequiredItem[1].SetDefaults(150, 50);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(153);
		NewRecipe.RequiredItem[0].SetDefaults(154, 50);
		NewRecipe.RequiredItem[1].SetDefaults(150, 45);
		NewRecipe.RequiredTile[0] = 18;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(190);
		NewRecipe.RequiredItem[0].SetDefaults(331, 12);
		NewRecipe.RequiredItem[1].SetDefaults(209, 15);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.SetDefaults(191);
		NewRecipe.RequiredItem[0].SetDefaults(331, 6);
		NewRecipe.RequiredItem[1].SetDefaults(209, 15);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.ANVILS;
		NewRecipe.CraftedItem.SetDefaults(185);
		NewRecipe.RequiredItem[0].SetDefaults(84);
		NewRecipe.RequiredItem[1].SetDefaults(331, 12);
		NewRecipe.RequiredItem[2].SetDefaults(210, 3);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(228);
		NewRecipe.RequiredItem[0].SetDefaults(179);
		NewRecipe.RequiredItem[1].SetDefaults(177);
		NewRecipe.RequiredItem[2].SetDefaults(331, 8);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(229);
		NewRecipe.RequiredItem[0].SetDefaults(178);
		NewRecipe.RequiredItem[1].SetDefaults(182);
		NewRecipe.RequiredItem[2].SetDefaults(331, 16);
		NewRecipe.RequiredItem[3].SetDefaults(209, 12);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(230);
		NewRecipe.RequiredItem[0].SetDefaults(181);
		NewRecipe.RequiredItem[1].SetDefaults(180);
		NewRecipe.RequiredItem[2].SetDefaults(331, 8);
		NewRecipe.RequiredItem[3].SetDefaults(210, 2);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(175);
		NewRecipe.RequiredItem[0].SetDefaults(174, 4);
		NewRecipe.RequiredItem[1].SetDefaults(173);
		NewRecipe.RequiredTile[0] = 77;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.SetDefaults(119);
		NewRecipe.RequiredItem[0].SetDefaults(175, 15);
		NewRecipe.RequiredItem[1].SetDefaults(55);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CHESTS;
		NewRecipe.CraftedItem.SetDefaults(120);
		NewRecipe.RequiredItem[0].SetDefaults(175, 25);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(121);
		NewRecipe.RequiredItem[0].SetDefaults(175, 35);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(122);
		NewRecipe.RequiredItem[0].SetDefaults(175, 35);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CHESTS;
		NewRecipe.CraftedItem.SetDefaults(217);
		NewRecipe.RequiredItem[0].SetDefaults(175, 35);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.ANVILS;
		NewRecipe.CraftedItem.SetDefaults(219);
		NewRecipe.RequiredItem[0].SetDefaults(175, 20);
		NewRecipe.RequiredItem[1].SetDefaults(164);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(231);
		NewRecipe.RequiredItem[0].SetDefaults(175, 25);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(232);
		NewRecipe.RequiredItem[0].SetDefaults(175, 35);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(233);
		NewRecipe.RequiredItem[0].SetDefaults(175, 30);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(273);
		NewRecipe.RequiredItem[0].SetDefaults(46);
		NewRecipe.RequiredItem[1].SetDefaults(155);
		NewRecipe.RequiredItem[2].SetDefaults(190);
		NewRecipe.RequiredItem[3].SetDefaults(121);
		NewRecipe.RequiredTile[0] = 26;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.LANTERNS;
		NewRecipe.CraftedItem.SetDefaults(389);
		NewRecipe.RequiredItem[0].SetDefaults(527);
		NewRecipe.RequiredItem[1].SetDefaults(528);
		NewRecipe.RequiredItem[2].SetDefaults(521, 10);
		NewRecipe.RequiredItem[3].SetDefaults(520, 10);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(381);
		NewRecipe.RequiredItem[0].SetDefaults(364, 3);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(372);
		NewRecipe.RequiredItem[0].SetDefaults(381, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(373);
		NewRecipe.RequiredItem[0].SetDefaults(381, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(371);
		NewRecipe.RequiredItem[0].SetDefaults(381, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(374);
		NewRecipe.RequiredItem[0].SetDefaults(381, 20);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(375);
		NewRecipe.RequiredItem[0].SetDefaults(381, 15);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(385);
		NewRecipe.RequiredItem[0].SetDefaults(381, 15);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.LANTERNS;
		NewRecipe.CraftedItem.SetDefaults(383);
		NewRecipe.RequiredItem[0].SetDefaults(381, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.TIMERS;
		NewRecipe.CraftedItem.SetDefaults(435);
		NewRecipe.RequiredItem[0].SetDefaults(381, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(483);
		NewRecipe.RequiredItem[0].SetDefaults(381, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.SPEARS;
		NewRecipe.CraftedItem.SetDefaults(537);
		NewRecipe.RequiredItem[0].SetDefaults(381, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(382);
		NewRecipe.RequiredItem[0].SetDefaults(365, 4);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(377);
		NewRecipe.RequiredItem[0].SetDefaults(382, 10);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(378);
		NewRecipe.RequiredItem[0].SetDefaults(382, 10);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(376);
		NewRecipe.RequiredItem[0].SetDefaults(382, 10);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(379);
		NewRecipe.RequiredItem[0].SetDefaults(382, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(380);
		NewRecipe.RequiredItem[0].SetDefaults(382, 15);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(386);
		NewRecipe.RequiredItem[0].SetDefaults(382, 15);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.LANTERNS;
		NewRecipe.CraftedItem.SetDefaults(384);
		NewRecipe.RequiredItem[0].SetDefaults(382, 10);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.TIMERS;
		NewRecipe.CraftedItem.SetDefaults(436);
		NewRecipe.RequiredItem[0].SetDefaults(382, 10);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(484);
		NewRecipe.RequiredItem[0].SetDefaults(382, 10);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.SPEARS;
		NewRecipe.CraftedItem.SetDefaults(390);
		NewRecipe.RequiredItem[0].SetDefaults(382, 10);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.ANVILS;
		NewRecipe.CraftedItem.SetDefaults(525);
		NewRecipe.RequiredItem[0].SetDefaults(382, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(391);
		NewRecipe.RequiredItem[0].SetDefaults(366, 5);
		NewRecipe.RequiredTile[0] = 133;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(401);
		NewRecipe.RequiredItem[0].SetDefaults(391, 12);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(402);
		NewRecipe.RequiredItem[0].SetDefaults(391, 12);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(400);
		NewRecipe.RequiredItem[0].SetDefaults(391, 12);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(403);
		NewRecipe.RequiredItem[0].SetDefaults(391, 24);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(404);
		NewRecipe.RequiredItem[0].SetDefaults(391, 18);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(388);
		NewRecipe.RequiredItem[0].SetDefaults(391, 18);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.LANTERNS;
		NewRecipe.CraftedItem.SetDefaults(387);
		NewRecipe.RequiredItem[0].SetDefaults(391, 12);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.TIMERS;
		NewRecipe.CraftedItem.SetDefaults(481);
		NewRecipe.RequiredItem[0].SetDefaults(391, 12);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(482);
		NewRecipe.RequiredItem[0].SetDefaults(391, 12);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.SPEARS;
		NewRecipe.CraftedItem.SetDefaults(406);
		NewRecipe.RequiredItem[0].SetDefaults(391, 12);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(524);
		NewRecipe.RequiredItem[0].SetDefaults(366, 30);
		NewRecipe.RequiredItem[1].SetDefaults(221);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(559);
		NewRecipe.RequiredItem[0].SetDefaults(372);
		NewRecipe.RequiredItem[1].SetDefaults(377);
		NewRecipe.RequiredItem[2].SetDefaults(401);
		NewRecipe.RequiredItem[3].SetDefaults(549, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(558);
		NewRecipe.RequiredItem[0].SetDefaults(371);
		NewRecipe.RequiredItem[1].SetDefaults(376);
		NewRecipe.RequiredItem[2].SetDefaults(400);
		NewRecipe.RequiredItem[3].SetDefaults(549, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(553);
		NewRecipe.RequiredItem[0].SetDefaults(373);
		NewRecipe.RequiredItem[1].SetDefaults(378);
		NewRecipe.RequiredItem[2].SetDefaults(402);
		NewRecipe.RequiredItem[3].SetDefaults(549, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(551);
		NewRecipe.RequiredItem[0].SetDefaults(374);
		NewRecipe.RequiredItem[1].SetDefaults(379);
		NewRecipe.RequiredItem[2].SetDefaults(403);
		NewRecipe.RequiredItem[3].SetDefaults(548, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(552);
		NewRecipe.RequiredItem[0].SetDefaults(375);
		NewRecipe.RequiredItem[1].SetDefaults(380);
		NewRecipe.RequiredItem[2].SetDefaults(404);
		NewRecipe.RequiredItem[3].SetDefaults(547, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.TOOLS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(579);
		NewRecipe.RequiredItem[0].SetDefaults(385);
		NewRecipe.RequiredItem[1].SetDefaults(386);
		NewRecipe.RequiredItem[2].SetDefaults(388);
		NewRecipe.RequiredItem[3].SetDefaults(383);
		NewRecipe.RequiredItem[4].SetDefaults(384);
		NewRecipe.RequiredItem[5].SetDefaults(387);
		NewRecipe.RequiredItem[6].SetDefaults(547, 5);
		NewRecipe.RequiredItem[7].SetDefaults(548, 5);
		NewRecipe.RequiredItem[8].SetDefaults(549, 5);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.TIMERS;
		NewRecipe.CraftedItem.SetDefaults(578);
		NewRecipe.RequiredItem[0].SetDefaults(435);
		NewRecipe.RequiredItem[1].SetDefaults(436);
		NewRecipe.RequiredItem[2].SetDefaults(481);
		NewRecipe.RequiredItem[3].SetDefaults(549, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(368);
		NewRecipe.RequiredItem[0].SetDefaults(483);
		NewRecipe.RequiredItem[1].SetDefaults(484);
		NewRecipe.RequiredItem[2].SetDefaults(482);
		NewRecipe.RequiredItem[3].SetDefaults(548, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.SPEARS;
		NewRecipe.CraftedItem.SetDefaults(550);
		NewRecipe.RequiredItem[0].SetDefaults(537);
		NewRecipe.RequiredItem[1].SetDefaults(390);
		NewRecipe.RequiredItem[2].SetDefaults(406);
		NewRecipe.RequiredItem[3].SetDefaults(547, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.TIMERS;
		NewRecipe.CraftedItem.SetDefaults(533);
		NewRecipe.RequiredItem[0].SetDefaults(98);
		NewRecipe.RequiredItem[1].SetDefaults(324);
		NewRecipe.RequiredItem[2].SetDefaults(319, 5);
		NewRecipe.RequiredItem[3].SetDefaults(548, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.SetDefaults(561);
		NewRecipe.RequiredItem[0].SetDefaults(381, 10);
		NewRecipe.RequiredItem[1].SetDefaults(382, 10);
		NewRecipe.RequiredItem[2].SetDefaults(520, 5);
		NewRecipe.RequiredItem[3].SetDefaults(548, 5);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.TIMERS;
		NewRecipe.CraftedItem.SetDefaults(506);
		NewRecipe.RequiredItem[0].SetDefaults(20, 20);
		NewRecipe.RequiredItem[1].SetDefaults(22, 20);
		NewRecipe.RequiredItem[2].SetDefaults(324);
		NewRecipe.RequiredItem[3].SetDefaults(522, 35);
		NewRecipe.RequiredItem[4].SetDefaults(547, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(494);
		NewRecipe.RequiredItem[0].SetDefaults(508);
		NewRecipe.RequiredItem[1].SetDefaults(502, 25);
		NewRecipe.RequiredItem[2].SetDefaults(521, 15);
		NewRecipe.RequiredItem[3].SetDefaults(19, 15);
		NewRecipe.RequiredItem[4].SetDefaults(549, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(425);
		NewRecipe.RequiredItem[0].SetDefaults(507);
		NewRecipe.RequiredItem[1].SetDefaults(501, 80);
		NewRecipe.RequiredItem[2].SetDefaults(520, 15);
		NewRecipe.RequiredItem[3].SetDefaults(19, 15);
		NewRecipe.RequiredItem[4].SetDefaults(549, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(497);
		NewRecipe.RequiredItem[0].SetDefaults(275, 15);
		NewRecipe.RequiredItem[1].SetDefaults(319, 5);
		NewRecipe.RequiredItem[2].SetDefaults(261, 15);
		NewRecipe.RequiredItem[3].SetDefaults(520, 5);
		NewRecipe.RequiredItem[4].SetDefaults(521, 5);
		NewRecipe.RequiredItem[5].SetDefaults(547, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(495);
		NewRecipe.RequiredItem[0].SetDefaults(502, 30);
		NewRecipe.RequiredItem[1].SetDefaults(526, 4);
		NewRecipe.RequiredItem[2].SetDefaults(501, 60);
		NewRecipe.RequiredItem[3].SetDefaults(520, 10);
		NewRecipe.RequiredItem[4].SetDefaults(549, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.TIMERS;
		NewRecipe.CraftedItem.SetDefaults(493);
		NewRecipe.RequiredItem[0].SetDefaults(320, 10);
		NewRecipe.RequiredItem[1].SetDefaults(575, 25);
		NewRecipe.RequiredItem[2].SetDefaults(520, 30);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.TIMERS;
		NewRecipe.CraftedItem.SetDefaults(492);
		NewRecipe.RequiredItem[0].SetDefaults(320, 10);
		NewRecipe.RequiredItem[1].SetDefaults(575, 25);
		NewRecipe.RequiredItem[2].SetDefaults(521, 30);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(394);
		NewRecipe.RequiredItem[0].SetDefaults(187);
		NewRecipe.RequiredItem[1].SetDefaults(268);
		NewRecipe.RequiredTile[0] = 114;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(395);
		NewRecipe.RequiredItem[0].SetDefaults(17);
		NewRecipe.RequiredItem[1].SetDefaults(18);
		NewRecipe.RequiredItem[2].SetDefaults(393);
		NewRecipe.RequiredTile[0] = 114;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(396);
		NewRecipe.RequiredItem[0].SetDefaults(158);
		NewRecipe.RequiredItem[1].SetDefaults(193);
		NewRecipe.RequiredTile[0] = 114;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(397);
		NewRecipe.RequiredItem[0].SetDefaults(156);
		NewRecipe.RequiredItem[1].SetDefaults(193);
		NewRecipe.RequiredTile[0] = 114;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(399);
		NewRecipe.RequiredItem[0].SetDefaults(53);
		NewRecipe.RequiredItem[1].SetDefaults(159);
		NewRecipe.RequiredTile[0] = 114;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(405);
		NewRecipe.RequiredItem[0].SetDefaults(54);
		NewRecipe.RequiredItem[1].SetDefaults(128);
		NewRecipe.RequiredTile[0] = 114;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(555);
		NewRecipe.RequiredItem[0].SetDefaults(223);
		NewRecipe.RequiredItem[1].SetDefaults(189);
		NewRecipe.RequiredTile[0] = 114;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.MECHANISM;
		NewRecipe.CraftedItem.SetDefaults(511);
		NewRecipe.RequiredItem[0].SetDefaults(3);
		NewRecipe.RequiredItem[1].SetDefaults(530);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.MECHANISM;
		NewRecipe.CraftedItem.SetDefaults(512);
		NewRecipe.RequiredItem[0].SetDefaults(26, 4);
		NewRecipe.RequiredItem[1].SetDefaults(530);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.MECHANISM;
		NewRecipe.CraftedItem.SetDefaults(580);
		NewRecipe.RequiredItem[0].SetDefaults(167, 3);
		NewRecipe.RequiredItem[1].SetDefaults(530);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.PUMPS;
		NewRecipe.CraftedItem.SetDefaults(581);
		NewRecipe.RequiredItem[0].SetDefaults(22, 10);
		NewRecipe.RequiredItem[1].SetDefaults(530, 2);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.PUMPS;
		NewRecipe.CraftedItem.SetDefaults(582);
		NewRecipe.RequiredItem[0].SetDefaults(22, 10);
		NewRecipe.RequiredItem[1].SetDefaults(530, 2);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.TIMERS;
		NewRecipe.CraftedItem.SetDefaults(583);
		NewRecipe.RequiredItem[0].SetDefaults(17);
		NewRecipe.RequiredItem[1].SetDefaults(22, 5);
		NewRecipe.RequiredItem[2].SetDefaults(530);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.TIMERS;
		NewRecipe.CraftedItem.SetDefaults(584);
		NewRecipe.RequiredItem[0].SetDefaults(16);
		NewRecipe.RequiredItem[1].SetDefaults(22, 5);
		NewRecipe.RequiredItem[2].SetDefaults(530);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.TIMERS;
		NewRecipe.CraftedItem.SetDefaults(585);
		NewRecipe.RequiredItem[0].SetDefaults(15);
		NewRecipe.RequiredItem[1].SetDefaults(22, 5);
		NewRecipe.RequiredItem[2].SetDefaults(530);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.STRUCTURES;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(540);
		NewRecipe.RequiredItem[0].SetDefaults(3, 6);
		NewRecipe.RequiredTile[0] = 114;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(565);
		NewRecipe.RequiredItem[0].SetDefaults(562);
		NewRecipe.RequiredItem[1].SetDefaults(563);
		NewRecipe.RequiredItem[2].SetDefaults(564);
		NewRecipe.RequiredItem[3].SetDefaults(566);
		NewRecipe.RequiredItem[4].SetDefaults(567);
		NewRecipe.RequiredItem[5].SetDefaults(568);
		NewRecipe.RequiredItem[6].SetDefaults(569);
		NewRecipe.RequiredItem[7].SetDefaults(570);
		NewRecipe.RequiredItem[8].SetDefaults(571);
		NewRecipe.RequiredItem[9].SetDefaults(572);
		NewRecipe.RequiredItem[10].SetDefaults(573);
		NewRecipe.RequiredItem[11].SetDefaults(574);
		NewRecipe.RequiredTile[0] = 114;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(628);
		NewRecipe.RequiredItem[0].SetDefaults(626);
		NewRecipe.RequiredItem[1].SetDefaults(627);
		NewRecipe.RequiredItem[2].SetDefaults(629);
		NewRecipe.RequiredItem[3].SetDefaults(630);
		NewRecipe.RequiredItem[4].SetDefaults(631);
		NewRecipe.RequiredTile[0] = 114;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(18);
		NewRecipe.RequiredItem[0].SetDefaults(20, 10);
		NewRecipe.RequiredItem[1].SetDefaults(21, 8);
		NewRecipe.RequiredItem[2].SetDefaults(19, 6);
		NewRecipe.RequiredTile[0] = 14;
		NewRecipe.RequiredTile[1] = 15;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.SetDefaults(193);
		NewRecipe.RequiredItem[0].SetDefaults(173, 20);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.MECHANISM;
		NewRecipe.CraftedItem.SetDefaults(518);
		NewRecipe.RequiredItem[0].SetDefaults(531);
		NewRecipe.RequiredItem[1].SetDefaults(502, 30);
		NewRecipe.RequiredItem[2].SetDefaults(520, 20);
		NewRecipe.RequiredTile[0] = 101;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.MECHANISM;
		NewRecipe.CraftedItem.SetDefaults(519);
		NewRecipe.RequiredItem[0].SetDefaults(531);
		NewRecipe.RequiredItem[1].SetDefaults(522, 30);
		NewRecipe.RequiredItem[2].SetDefaults(521, 20);
		NewRecipe.RequiredTile[0] = 101;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.ANVILS;
		NewRecipe.CraftedItem.SetDefaults(266);
		NewRecipe.RequiredItem[0].SetDefaults(324);
		NewRecipe.RequiredItem[1].SetDefaults(323, 10);
		NewRecipe.RequiredItem[2].SetDefaults(180, 5);
		NewRecipe.RequiredTile[0] = 17;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CHESTS;
		NewRecipe.CraftedItem.SetDefaults(37);
		NewRecipe.RequiredItem[0].SetDefaults(38, 2);
		NewRecipe.RequiredTile[0] = 18;
		NewRecipe.RequiredTile[1] = 15;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CHESTS;
		NewRecipe.CraftedItem.SetDefaults(237);
		NewRecipe.RequiredItem[0].SetDefaults(236, 2);
		NewRecipe.RequiredTile[0] = 18;
		NewRecipe.RequiredTile[1] = 15;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.POTIONS;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(109);
		NewRecipe.RequiredItem[0].SetDefaults(75, 10);
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.LANTERNS;
		NewRecipe.CraftedItem.SetDefaults(43);
		NewRecipe.RequiredItem[0].SetDefaults(38, 6);
		NewRecipe.RequiredTile[0] = 26;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(70);
		NewRecipe.RequiredItem[0].SetDefaults(67, 30);
		NewRecipe.RequiredItem[1].SetDefaults(68, 15);
		NewRecipe.RequiredTile[0] = 26;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(560);
		NewRecipe.RequiredItem[0].SetDefaults(23, 99);
		NewRecipe.RequiredItem[1].SetDefaults(264);
		NewRecipe.RequiredTile[0] = 26;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.LANTERNS;
		NewRecipe.CraftedItem.SetDefaults(544);
		NewRecipe.RequiredItem[0].SetDefaults(38, 3);
		NewRecipe.RequiredItem[1].SetDefaults(20, 5);
		NewRecipe.RequiredItem[2].SetDefaults(22, 5);
		NewRecipe.RequiredItem[3].SetDefaults(520, 7);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.NONE;
		NewRecipe.CraftedItem.SetDefaults(556);
		NewRecipe.RequiredItem[0].SetDefaults(68, 6);
		NewRecipe.RequiredItem[1].SetDefaults(20, 5);
		NewRecipe.RequiredItem[2].SetDefaults(22, 5);
		NewRecipe.RequiredItem[3].SetDefaults(521, 7);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.SetDefaults(557);
		NewRecipe.RequiredItem[0].SetDefaults(154, 30);
		NewRecipe.RequiredItem[1].SetDefaults(20, 5);
		NewRecipe.RequiredItem[2].SetDefaults(22, 5);
		NewRecipe.RequiredItem[3].SetDefaults(520, 5);
		NewRecipe.RequiredItem[4].SetDefaults(521, 5);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.MISC;
		NewRecipe.RecipeSubCategory = SubCategory.CHANDELIERS;
		NewRecipe.CraftedItem.SetDefaults(619);
		NewRecipe.RequiredItem[0].SetDefaults(544, 2);
		NewRecipe.RequiredItem[1].SetDefaults(557);
		NewRecipe.RequiredItem[2].SetDefaults(391, 20);
		NewRecipe.RequiredItem[3].SetDefaults(521, 10);
		NewRecipe.RequiredItem[4].SetDefaults(520, 10);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.BRICKS;
		NewRecipe.CraftedItem.SetDefaults(604);
		NewRecipe.RequiredItem[0].SetDefaults(559);
		NewRecipe.RequiredItem[1].SetDefaults(372);
		NewRecipe.RequiredItem[2].SetDefaults(377);
		NewRecipe.RequiredItem[3].SetDefaults(401);
		NewRecipe.RequiredItem[4].SetDefaults(620, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(607);
		NewRecipe.RequiredItem[0].SetDefaults(551);
		NewRecipe.RequiredItem[1].SetDefaults(374);
		NewRecipe.RequiredItem[2].SetDefaults(379);
		NewRecipe.RequiredItem[3].SetDefaults(403);
		NewRecipe.RequiredItem[4].SetDefaults(548, 20);
		NewRecipe.RequiredItem[5].SetDefaults(620, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(610);
		NewRecipe.RequiredItem[0].SetDefaults(552);
		NewRecipe.RequiredItem[1].SetDefaults(375);
		NewRecipe.RequiredItem[2].SetDefaults(380);
		NewRecipe.RequiredItem[3].SetDefaults(404);
		NewRecipe.RequiredItem[4].SetDefaults(548, 20);
		NewRecipe.RequiredItem[5].SetDefaults(620, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.WALLS;
		NewRecipe.CraftedItem.SetDefaults(605);
		NewRecipe.RequiredItem[0].SetDefaults(553);
		NewRecipe.RequiredItem[1].SetDefaults(373);
		NewRecipe.RequiredItem[2].SetDefaults(378);
		NewRecipe.RequiredItem[3].SetDefaults(402);
		NewRecipe.RequiredItem[4].SetDefaults(620, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(608);
		NewRecipe.RequiredItem[0].SetDefaults(551);
		NewRecipe.RequiredItem[1].SetDefaults(374);
		NewRecipe.RequiredItem[2].SetDefaults(379);
		NewRecipe.RequiredItem[3].SetDefaults(403);
		NewRecipe.RequiredItem[4].SetDefaults(549, 20);
		NewRecipe.RequiredItem[5].SetDefaults(620, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(611);
		NewRecipe.RequiredItem[0].SetDefaults(552);
		NewRecipe.RequiredItem[1].SetDefaults(375);
		NewRecipe.RequiredItem[2].SetDefaults(380);
		NewRecipe.RequiredItem[3].SetDefaults(404);
		NewRecipe.RequiredItem[4].SetDefaults(549, 20);
		NewRecipe.RequiredItem[5].SetDefaults(620, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(606);
		NewRecipe.RequiredItem[0].SetDefaults(558);
		NewRecipe.RequiredItem[1].SetDefaults(371);
		NewRecipe.RequiredItem[2].SetDefaults(376);
		NewRecipe.RequiredItem[3].SetDefaults(400);
		NewRecipe.RequiredItem[4].SetDefaults(620, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.CRAFTING_STATIONS;
		NewRecipe.CraftedItem.SetDefaults(609);
		NewRecipe.RequiredItem[0].SetDefaults(551);
		NewRecipe.RequiredItem[1].SetDefaults(374);
		NewRecipe.RequiredItem[2].SetDefaults(379);
		NewRecipe.RequiredItem[3].SetDefaults(403);
		NewRecipe.RequiredItem[4].SetDefaults(547, 20);
		NewRecipe.RequiredItem[5].SetDefaults(620, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.ARMOR;
		NewRecipe.RecipeSubCategory = SubCategory.STATUES;
		NewRecipe.CraftedItem.SetDefaults(612);
		NewRecipe.RequiredItem[0].SetDefaults(552);
		NewRecipe.RequiredItem[1].SetDefaults(375);
		NewRecipe.RequiredItem[2].SetDefaults(380);
		NewRecipe.RequiredItem[3].SetDefaults(404);
		NewRecipe.RequiredItem[4].SetDefaults(547, 20);
		NewRecipe.RequiredItem[5].SetDefaults(620, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.TORCHES;
		NewRecipe.CraftedItem.SetDefaults(613);
		NewRecipe.RequiredItem[0].SetDefaults(368, 2);
		NewRecipe.RequiredItem[1].SetDefaults(620, 15);
		NewRecipe.RequiredItem[2].SetDefaults(391, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.SPEARS;
		NewRecipe.CraftedItem.SetDefaults(614);
		NewRecipe.RequiredItem[0].SetDefaults(550, 2);
		NewRecipe.RequiredItem[1].SetDefaults(620, 15);
		NewRecipe.RequiredItem[2].SetDefaults(391, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.CHESTS;
		NewRecipe.CraftedItem.SetDefaults(615);
		NewRecipe.RequiredItem[0].SetDefaults(120, 2);
		NewRecipe.RequiredItem[1].SetDefaults(175, 10);
		NewRecipe.RequiredTile[0] = 16;
		AddRecipe();
		NewRecipe.RecipeCategory = Category.WEAPONS;
		NewRecipe.RecipeSubCategory = SubCategory.TIMERS;
		NewRecipe.CraftedItem.SetDefaults(617);
		NewRecipe.RequiredItem[0].SetDefaults(578, 2);
		NewRecipe.RequiredItem[1].SetDefaults(620, 15);
		NewRecipe.RequiredItem[2].SetDefaults(391, 20);
		NewRecipe.RequiredTile[0] = 134;
		AddRecipe();
		for (int i = 0; i < NumRecipes; i++)
		{
			for (int j = 0; j < 12 && Main.ActiveRecipe[i].RequiredItem[j].Type > 0; j++)
			{
				Main.ActiveRecipe[i].RequiredItem[j].CheckMaterial();
			}
			Main.ActiveRecipe[i].CraftedItem.CheckMaterial();
		}
	}

	/// <summary>
	/// Recomputes which items count as crafting materials.
	///
	/// <see cref="SetupRecipes"/> runs before mods load, so an item used only by a mod recipe was
	/// never marked as a material - and the crafting guide then refuses to show it as an
	/// ingredient. Called once more after mod recipes are added.
	/// </summary>
	internal static void RecheckMaterials()
	{
		// CheckMaterial mutates the item it is called on, so it has to be called on the array
		// element itself - a copy would be marked and thrown away.
		for (int i = 0; i < NumRecipes; i++)
		{
			for (int j = 0; j < 12 && Main.ActiveRecipe[i].RequiredItem[j].Type > 0; j++)
			{
				Main.ActiveRecipe[i].RequiredItem[j].CheckMaterial();
			}
			Main.ActiveRecipe[i].CraftedItem.CheckMaterial();
		}
	}

	private static void AddRecipe()
	{
		for (int i = 0; i < 12 && NewRecipe.RequiredItem[i].Type > 0; i++)
		{
			NewRecipe.NumRequiredItems++;
		}
		for (int j = 0; j < 3 && NewRecipe.RequiredTile[j] >= 0; j++)
		{
			NewRecipe.NumRequiredTiles++;
		}
		Main.ActiveRecipe[NumRecipes++] = NewRecipe;
		NewRecipe = new Recipe();
	}
}
