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
		return dictionary;
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
		// A 44x14 room: enough for nine chests in a row with a gap, the stations, and headroom
		// to walk in without hitting anything.
		int num3 = num - 22;
		int num4 = num2 - 9;
		int num5 = num + 21;
		int num6 = num2 + 4;
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
		for (int m = 0; m < Categories.Length; m++)
		{
			int num7 = num3 + 12 + m * 3;
			if (num7 + 1 > num5 - 1)
			{
				// Out of room: the chest count follows the room size rather than the other way
				// round, and the message says how many were placed.
				break;
			}
			FillOne(num7, num6, Categories[m]);
		}
		// Two torches near the middle so the room is visible at night.
		SetTile(num3 + 3, num4 + 1, EntityID.TileID.TORCH);
		SetTile(num5 - 3, num4 + 1, EntityID.TileID.TORCH);
		return true;
	}

	/// <summary>Places one chest on the floor row and fills it with one category.</summary>
	private static void FillOne(int X, int FloorY, Category Category)
	{
		int num = WorldGen.PlaceChest(X, FloorY);
		if (num < 0 || num >= Main.ChestSet.Length || Main.ChestSet[num] == null)
		{
			return;
		}
		List<int> list = Collect()[Category];
		Chest chest = Main.ChestSet[num];
		int num2 = 0;
		for (int i = 0; i < list.Count && num2 < ChestSlots; i++)
		{
			Item item;
			if (!TryCreate(list[i], out item))
			{
				continue;
			}
			item.Stack = item.MaxStack;
			chest.ItemSet[num2] = item;
			num2++;
		}
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

	/// <summary>One line for the chat, including how many chests were filled.</summary>
	public static string Describe()
	{
		return "!debugworld builds a room next to you: workbench, furnace, anvil, and "
			+ Categories.Length + " chests sorted by category (" + Label(Categories[0]) + " .. "
			+ Label(Categories[Categories.Length - 1]) + ").";
	}
}
#endif
