#if DEBUG
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Terraria.Cheats;

/// <summary>
/// A debug-only room with everything a test needs: crafting stations, light, and a wall of chests
/// sorted by category so an item can be found without paging through six hundred ids.
///
/// Why it is built in code rather than shipped as a world file: a template binary would have to
/// be regenerated every time the item table changes, and it would be one more opaque blob in the
/// repository. The layout here is a dozen numbers; the contents come from the game's own item
/// table at the moment it runs, so they are never stale.
///
/// Compiled only in Debug builds (<c>#if DEBUG</c>), like <see cref="DebugKit"/>: a Release build
/// should not contain a way to conjure a warehouse of every item.
/// </summary>
internal static class DebugWorld
{
	/// <summary>Chest categories, in the order they are placed from left to right.</summary>
	internal enum Category
	{
		Weapons,
		Armor,
		Accessories,
		Tools,
		Blocks,
		Walls,
		Potions,
		Materials,
		Furniture
	}

	/// <summary>Every category, in placement order.</summary>
	internal static readonly Category[] Categories = new Category[9]
	{
		Category.Weapons,
		Category.Armor,
		Category.Accessories,
		Category.Tools,
		Category.Blocks,
		Category.Walls,
		Category.Potions,
		Category.Materials,
		Category.Furniture
	};

	/// <summary>Slots in a chest; <see cref="Chest"/> has twenty.</summary>
	public const int ChestSlots = 20;

	/// <summary>Labels for the chat message, English because they are names, not prose.</summary>
	internal static string Label(Category category)
	{
		return category.ToString().ToLowerInvariant();
	}

	/// <summary>
	/// Puts one item id into a category. Order matters: a block that also heals is a block, and a
	/// tool that also damages is a tool, because that is what the player would search for.
	/// </summary>
	internal static Category Classify(Item item)
	{
		if (item.CreateTile >= 0)
		{
			// Zero is a real tile here: TileID.DIRT is 0, and -1 is "not a block". Testing
			// "> 0" filed every dirt and stone block under materials, which made the blocks
			// chest empty and the room useless for building.
			int num = item.CreateTile;
			if (num == (int)EntityID.TileID.WORK_BENCH || num == (int)EntityID.TileID.FURNACE || num == (int)EntityID.TileID.ANVIL
				|| num == (int)EntityID.TileID.TABLE || num == (int)EntityID.TileID.CHAIR || num == (int)EntityID.TileID.CHEST
				|| num == (int)EntityID.TileID.PLATFORM || num == (int)EntityID.TileID.DOOR_CLOSED || num == (int)EntityID.TileID.BOTTLE)
			{
				return Category.Furniture;
			}
			return Category.Blocks;
		}
		if (item.CreateWall > 0)
		{
			// Zero is "no wall" (WallID.NONE), so unlike tiles the lower bound here is 1.
			return Category.Walls;
		}
		if (item.IsPotion || item.HealLife > 0 || item.HealMana > 0 || item.BuffType > 0)
		{
			return Category.Potions;
		}
		if (item.HeadSlot >= 0 || item.BodySlot >= 0 || item.LegSlot >= 0)
		{
			return Category.Armor;
		}
		if (item.IsAccessory)
		{
			return Category.Accessories;
		}
		if (item.PickPower > 0 || item.AxePower > 0 || item.HammerPower > 0)
		{
			return Category.Tools;
		}
		if (item.Damage > 0 || item.IsMelee || item.IsMagic || item.IsRanged)
		{
			return Category.Weapons;
		}
		return Category.Materials;
	}

	/// <summary>
	/// Item ids per category, from the live item table. Ids that do not resolve to an item are
	/// skipped: <c>SetDefaults</c> leaves them at type 0, and a chest full of nothing looks like
	/// the command failed.
	/// </summary>
	/// <summary>
	/// Item ids per category, from the live item table - <b>including the items mods define</b>.
	///
	/// The loop used to stop at <c>EntityID.ItemID.NUM_TYPES</c>, so with mods installed the room
	/// held the vanilla 631 items and none of the mods' - which is the opposite of what a debug
	/// room is for. Mod items start at <c>ModItems.IdBase</c> (20000) and are only reachable
	/// through the registry, so they have to be appended rather than found by counting.
	/// </summary>
	internal static Dictionary<Category, List<int>> Collect()
	{
		Dictionary<Category, List<int>> dictionary = new Dictionary<Category, List<int>>();
		for (int i = 0; i < Categories.Length; i++)
		{
			dictionary[Categories[i]] = new List<int>();
		}
		int num = (int)EntityID.ItemID.NUM_TYPES;
		for (int j = 1; j < num; j++)
		{
			Item item;
			if (!TryCreate(j, out item))
			{
				continue;
			}
			dictionary[Classify(item)].Add(j);
		}
		List<Terraria.Mods.ModItem> all = Terraria.Mods.ModItems.All();
		for (int k = 0; k < all.Count; k++)
		{
			int id = all[k].Type;
			Item item;
			if (id <= 0 || !TryCreate(id, out item))
			{
				continue;
			}
			dictionary[Classify(item)].Add(id);
		}
		return dictionary;
	}

	/// <summary>
	/// How many chests the collected items need, at <see cref="ChestSlots"/> per chest.
	///
	/// The room used to place exactly one chest per category, so with 631 vanilla items it held
	/// 180 of them and silently dropped the other 451 - "all the items" that was not all the
	/// items. The count comes from the table, so it stays right as the table grows.
	/// </summary>
	internal static int ChestsNeeded(Dictionary<Category, List<int>> collected)
	{
		int num = 0;
		for (int i = 0; i < Categories.Length; i++)
		{
			List<int> list;
			if (collected.TryGetValue(Categories[i], out list) && list.Count > 0)
			{
				num += (list.Count + ChestSlots - 1) / ChestSlots;
			}
		}
		return num;
	}

	/// <summary>
	/// One item id turned into an item, or false when the table has no such item.
	///
	/// <c>SetDefaults</c> throws for a few ids outside a running game (they read tables that only
	/// exist once content is loaded), and one bad id must not cost the player the other six
	/// hundred: the room is a debugging tool, not a validation pass.
	/// </summary>
	internal static bool TryCreate(int Id, out Item Item)
	{
		Item = default(Item);
		try
		{
			Item = DebugKit.Create(Id);
		}
		catch (Exception)
		{
			return false;
		}
		return Item.Type > 0;
	}

	/// <summary>
	/// Builds the room next to the player: floor, walls, light, the three crafting stations and a
	/// row of chests, each filled with one category.
	///
	/// Called from a command rather than from world generation: decorating every new world in a
	/// Debug build would make an ordinary test world and this one impossible to tell apart.
	/// </summary>
	public static bool Build(Player player, out string error)
	{
		error = null;
		if (player == null || Main.TileSet == null || Main.ChestSet == null)
		{
			error = "no world loaded";
			return false;
		}
		int num = player.XYWH.X >> 4;
		int num2 = (player.XYWH.Y >> 4) + 2;
		if (num < 60 || num2 < 40 || num >= Main.MaxTilesX - 60 || num2 >= Main.MaxTilesY - 40)
		{
			error = "too close to the edge of the world";
			return false;
		}
		// A room sized by what it has to hold: three stations, then one chest per 20 items with a
		// tile between them. The old fixed 44x14 room held nine chests, which is 180 items - a
		// third of the vanilla table and none of the mods'.
		Dictionary<Category, List<int>> collected = Collect();
		int chests = ChestsNeeded(collected);
		int width = 12 + chests * 3 + 2;
		if (width < 44)
		{
			width = 44;
		}
		int num3 = num - (width >> 1);
		int num5 = num3 + width - 1;
		int num4 = num2 - 9;
		int num6 = num2 + 4;
		if (num3 < 60 || num5 >= Main.MaxTilesX - 60)
		{
			error = "not enough room here for " + chests + " chests (" + width
				+ " tiles wide) - move away from the edge of the world";
			return false;
		}
		for (int i = num3; i <= num5; i++)
		{
			for (int j = num4; j <= num6; j++)
			{
				ref Tile reference = ref Main.TileSet[i, j];
				reference.IsActive = 0;
				reference.Type = 0;
				reference.FrameX = 0;
				reference.FrameY = 0;
				reference.Liquid = 0;
				reference.Lava = 0;
				// Background wall, so the room is a room and not a clearing: without it the
				// lighting looks like an outdoor platform and the station checks still work but
				// the picture is misleading.
				reference.WallType = (byte)((i == num3 || i == num5 || j == num4 || j == num6)
					? EntityID.WallID.GREY_BRICK
					: EntityID.WallID.WOOD);
			}
		}
		// Floor and ceiling of stone, so nothing falls out of the room.
		for (int k = num3; k <= num5; k++)
		{
			SetTile(k, num6 + 1, EntityID.TileID.STONE);
			SetTile(k, num4 - 1, EntityID.TileID.STONE);
		}
		for (int l = num4; l <= num6; l++)
		{
			SetTile(num3 - 1, l, EntityID.TileID.STONE);
			SetTile(num5 + 1, l, EntityID.TileID.STONE);
		}
		// Stations along the back wall, then chests in a row with one empty tile between them.
		SetTile(num3 + 2, num6, EntityID.TileID.WORK_BENCH);
		SetTile(num3 + 5, num6, EntityID.TileID.FURNACE);
		SetTile(num3 + 8, num6, EntityID.TileID.ANVIL);
		int num7 = num3 + 12;
		int placed = 0;
		for (int m = 0; m < Categories.Length; m++)
		{
			List<int> list = collected[Categories[m]];
			// As many chests as the category needs, not one: 194 blocks do not fit in 20 slots,
			// and the ones that did not fit were dropped without a word.
			for (int n = 0; n < list.Count; n += ChestSlots)
			{
				if (num7 + 1 > num5 - 1)
				{
					break;
				}
				placed += FillOne(num7, num6, list, n);
				num7 += 3;
			}
		}
		// Two torches near the middle so the room is visible at night.
		SetTile(num3 + 3, num4 + 1, EntityID.TileID.TORCH);
		SetTile(num5 - 3, num4 + 1, EntityID.TileID.TORCH);
		// Logged rather than silent: if a chest could not be placed (an obstructed spot, a world
		// with a hole in the floor), the room is smaller than the table and the player should be
		// able to tell without counting.
		int wanted = 0;
		for (int i = 0; i < Categories.Length; i++)
		{
			wanted += collected[Categories[i]].Count;
		}
		LastPlacedItems = placed;
		LastWantedItems = wanted;
		if (placed < wanted)
		{
			Terraria.Program.LogWarn("Cheats", "debug room: " + placed + " of " + wanted
				+ " items placed (" + chests + " chests planned)");
		}
		return true;
	}

	/// <summary>Items actually put into chests by the last <see cref="Build"/>, and how many the table has.</summary>
	internal static int LastPlacedItems;

	/// <summary>See <see cref="LastPlacedItems"/>.</summary>
	internal static int LastWantedItems;

	/// <summary>
	/// Places one chest on the floor row and fills it with 20 items from <paramref name="ids"/>,
	/// starting at <paramref name="From"/>.
	///
	/// Returns the number of items actually placed, so the caller can report the total instead of
	/// guessing. A chest that could not be placed returns 0 rather than throwing: one bad spot in
	/// a debugging tool must not cost the player the rest of the room.
	/// </summary>
	private static int FillOne(int X, int FloorY, List<int> Ids, int From)
	{
		int num = WorldGen.PlaceChest(X, FloorY);
		if (num < 0 || num >= Main.ChestSet.Length || Main.ChestSet[num] == null)
		{
			return 0;
		}
		Chest chest = Main.ChestSet[num];
		int num2 = 0;
		for (int i = From; i < Ids.Count && num2 < ChestSlots; i++)
		{
			Item item;
			if (!TryCreate(Ids[i], out item))
			{
				continue;
			}
			item.Stack = item.MaxStack;
			chest.ItemSet[num2] = item;
			num2++;
		}
		return num2;
	}

	/// <summary>Sets one tile, ignoring coordinates outside the world.</summary>
	private static void SetTile(int X, int Y, EntityID.TileID Type)
	{
		if (X < 0 || Y < 0 || X >= Main.MaxTilesX || Y >= Main.MaxTilesY)
		{
			return;
		}
		ref Tile reference = ref Main.TileSet[X, Y];
		reference.IsActive = 1;
		reference.Type = (byte)Type;
		reference.FrameX = 0;
		reference.FrameY = 0;
	}

	/// <summary>One line for the chat, including what the room will hold.</summary>
	public static string Describe()
	{
		// The chest count is read from the table rather than stated: it is 70 with the mods on
		// this machine and 32 on a vanilla install, and a hardcoded number would be wrong in both.
		Dictionary<Category, List<int>> collected = Collect();
		int num = 0;
		for (int i = 0; i < Categories.Length; i++)
		{
			num += collected[Categories[i]].Count;
		}
		return "!debugworld builds a room next to you: workbench, furnace, anvil, and "
			+ ChestsNeeded(collected) + " chests (" + num + " items) sorted by category ("
			+ Label(Categories[0]) + " .. " + Label(Categories[Categories.Length - 1]) + ").";
	}
}
#endif
