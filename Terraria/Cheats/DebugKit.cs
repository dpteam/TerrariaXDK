#if DEBUG
using System;
using Microsoft.Xna.Framework;

namespace Terraria.Cheats;

/// <summary>
/// Debug-build conveniences: a ready-made test character and a way to reach every item without
/// typing ids one at a time.
///
/// Why this exists: checking a tile, a recipe or a dye means having the item, and the item is
/// usually thousands of ids away from whatever the last session ended with. `!give` is fine when
/// you know the name of one thing; it is useless when you want to page through all of them.
///
/// Compiled only in Debug builds (<c>#if DEBUG</c> at the top of the file), so a Release build
/// does not contain any of it - not even the strings. That is done with the preprocessor rather
/// than a runtime flag on purpose: a cheat that is present but disabled is still a cheat that can
/// be enabled, and this must never reach a player's copy.
/// </summary>
internal static class DebugKit
{
	/// <summary>How many inventory slots one page fills. Leaves the last few free for drops.</summary>
	public const int PageSize = 44;

	/// <summary>Highest item id the game knows. <c>NUM_TYPES</c> is the sentinel, not an item.</summary>
	public static int ItemCount => (int)EntityID.ItemID.NUM_TYPES;

	/// <summary>Pages needed to walk every item id once, from 0.</summary>
	public static int PageCount => (ItemCount + PageSize - 1) / PageSize;

	/// <summary>Clamps a page number into range. Returns -1 when there is nothing to give.</summary>
	public static int ClampPage(int page)
	{
		if (ItemCount <= 0)
		{
			return -1;
		}
		if (page < 0)
		{
			return 0;
		}
		return (page >= PageCount) ? (PageCount - 1) : page;
	}

	/// <summary>
	/// The id range one page covers, half-open: <c>[first, last)</c>. Empty when the page is out
	/// of range, which is what makes <see cref="GivePage"/> safe to call with anything.
	/// </summary>
	public static void RangeForPage(int page, out int first, out int last)
	{
		first = 0;
		last = 0;
		int num = ClampPage(page);
		if (num < 0)
		{
			return;
		}
		first = num * PageSize + 1;
		last = Math.Min(first + PageSize, ItemCount);
		if (first >= last)
		{
			first = 0;
			last = 0;
		}
	}

	/// <summary>
	/// Fills the inventory with every item id on one page. Ids the game does not define are
	/// skipped: <see cref="Item.SetDefaults(int)"/> leaves such an item at type 0, and filling
	/// slots with nothing would look like the command half-worked.
	/// </summary>
	/// <returns>How many items were actually placed.</returns>
	public static int GivePage(Player player, int page)
	{
		if (player == null || player.Inventory == null)
		{
			return 0;
		}
		int num;
		int num2;
		RangeForPage(page, out num, out num2);
		if (num == 0 && num2 == 0)
		{
			return 0;
		}
		int num3 = 0;
		int num4 = 0;
		for (int j = num; j < num2 && num4 < player.Inventory.Length && num3 < PageSize; j++)
		{
			Item item = default(Item);
			item.SetDefaults(j);
			if (item.Type <= 0)
			{
				continue;
			}
			// A full stack, so testing a stack-size bug is possible from the same page.
			item.Stack = item.MaxStack;
			while (num4 < player.Inventory.Length && player.Inventory[num4].Type > 0)
			{
				num4++;
			}
			if (num4 >= player.Inventory.Length)
			{
				break;
			}
			player.Inventory[num4] = item;
			num4++;
			num3++;
		}
		return num3;
	}

	/// <summary>The kit variants. Chosen by name from the console; see <see cref="Parse"/>.</summary>
	internal enum Kit
	{
		Default,
		Pvp,
		Pve,
		Full
	}

	/// <summary>
	/// Parses a kit name. Unknown text returns false so the caller can list what is available
	/// instead of silently handing out the default set.
	/// </summary>
	internal static bool Parse(string text, out Kit kit)
	{
		kit = Kit.Default;
		switch ((text ?? string.Empty).Trim().ToLowerInvariant())
		{
		case "":
		case "0":
		case "default":
		case "tools":
			kit = Kit.Default;
			return true;
		case "1":
		case "pvp":
			kit = Kit.Pvp;
			return true;
		case "2":
		case "pve":
			kit = Kit.Pve;
			return true;
		case "3":
		case "full":
			kit = Kit.Full;
			return true;
		default:
			return false;
		}
	}

	/// <summary>Names for the help line, in the same order as the numbers.</summary>
	internal static string List => "default (0), pvp (1), pve (2), full (3)";

	/// <summary>
	/// Fills the inventory with one kit.
	///
	/// The point of separate kits is that "everything" is not always what you want: a PvP check
	/// needs a weapon and nothing else in the way, while a full play-through kit needs a pickaxe
	/// before it can start. Every id comes from the enum, never from a name - `SetDefaults(string)`
	/// is a switch over the port's own additions and returns type 0 for the game's items.
	/// </summary>
	public static int Give(Player player, Kit kit)
	{
		if (player == null || player.Inventory == null)
		{
			return 0;
		}
		EntityID.ItemID[][] sets = new EntityID.ItemID[4][]
		{
			// Default: reach any place and look at it.
			new EntityID.ItemID[12]
			{
				EntityID.ItemID.COPPER_PICKAXE,
				EntityID.ItemID.COPPER_AXE,
				EntityID.ItemID.COPPER_HAMMER,
				EntityID.ItemID.TORCH,
				EntityID.ItemID.DIRT_BLOCK,
				EntityID.ItemID.MAGIC_MIRROR,
				EntityID.ItemID.GRAPPLING_HOOK,
				EntityID.ItemID.HEALING_POTION,
				EntityID.ItemID.MANA_POTION,
				EntityID.ItemID.WOOD,
				EntityID.ItemID.GLOWSTICK,
				EntityID.ItemID.STONE_BLOCK
			},
			// PvP: mobility, a ranged option and healing, nothing else to fumble past.
			new EntityID.ItemID[8]
			{
				EntityID.ItemID.GRAPPLING_HOOK,
				EntityID.ItemID.MAGIC_MIRROR,
				EntityID.ItemID.HEALING_POTION,
				EntityID.ItemID.MANA_POTION,
				EntityID.ItemID.TORCH,
				EntityID.ItemID.GLOWSTICK,
				EntityID.ItemID.DIRT_BLOCK,
				EntityID.ItemID.WOOD
			},
			// PvE: survivability and digging.
			new EntityID.ItemID[10]
			{
				EntityID.ItemID.COPPER_PICKAXE,
				EntityID.ItemID.COPPER_AXE,
				EntityID.ItemID.COPPER_HAMMER,
				EntityID.ItemID.TORCH,
				EntityID.ItemID.HEALING_POTION,
				EntityID.ItemID.MANA_POTION,
				EntityID.ItemID.GRAPPLING_HOOK,
				EntityID.ItemID.MAGIC_MIRROR,
				EntityID.ItemID.DIRT_BLOCK,
				EntityID.ItemID.WOOD
			},
			// Full: one of everything the game needs you to own, page by page, so a play-through
			// can start from the first page instead of hunting for an id.
			new EntityID.ItemID[0]
		};
		int num = 0;
		if (kit == Kit.Full)
		{
			// "Full" is every page at once, in id order, so it is also the way to check that
			// paging and direct granting agree.
			for (int i = 0; i < PageCount; i++)
			{
				num += GivePage(player, i);
			}
			return num;
		}
		EntityID.ItemID[] array = sets[(int)kit];
		for (int j = 0; j < array.Length && num < player.Inventory.Length; j++)
		{
			int num2 = (int)array[j];
			if (num2 <= 0)
			{
				continue;
			}
			Item item = default(Item);
			item.SetDefaults(num2);
			if (item.Type <= 0)
			{
				continue;
			}
			// Skip slots already in use: replacing a working setup is the most annoying thing a
			// debug command can do.
			while (num < player.Inventory.Length && player.Inventory[num].Type > 0)
			{
				num++;
			}
			if (num >= player.Inventory.Length)
			{
				break;
			}
			item.Stack = item.MaxStack;
			player.Inventory[num] = item;
			num++;
		}
		return array.Length;
	}

	/// <summary>
	/// A test character: full life and mana, and the tools needed to look at anything (pickaxe,
	/// axe, hammer, a light source, blocks, a mirror to get out of a cave). Deliberately not a
	/// best-in-slot set: the point is reaching a place and trying a thing, not playing.
	///
	/// Ids come from the enum, not from names. <c>Item.SetDefaults(string)</c> is a switch over
	/// the port's own additions (Gold Pickaxe and friends), not a name table for the game's
	/// items - asking it for "Copper Pickaxe" silently yields type 0, which looked like the
	/// command doing nothing.
	/// </summary>
	public static void ApplyEssentials(Player player)
	{
		if (player == null)
		{
			return;
		}
		player.StatLifeMax = 500;
		player.statLife = 500;
		player.statManaMax = 200;
		player.statManaMax2 = 200;
		player.statMana = 200;
	}

	/// <summary>One line describing what the commands do, for <c>!debugkit help</c>.</summary>
	public static string Describe()
	{
		return "Debug build: !debugkit gives a test character, !allitems <page> fills the inventory with 44 item ids (page 0-"
			+ (PageCount - 1) + ").";
	}
}
#endif
