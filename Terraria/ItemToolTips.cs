using System;
using System.Collections.Generic;

namespace Terraria;

/// <summary>
/// Tooltip text for items that do not have one in the compiled tables.
///
/// Item descriptions live in <see cref="Lang.ToolTip"/> as a switch over vanilla ids, so there is
/// no field on <see cref="Item"/> to write. A mod item, and a tConfig override of a vanilla item,
/// therefore had no description at all - and a pack carries one for almost every item (62 in one
/// pack on this machine), so dropping them made every converted item a mystery.
///
/// One table for both paths: the importer writes into it for existing items, and the mod registry
/// copies its definitions in once ids are assigned. That keeps <see cref="Lang.ToolTip"/> with a
/// single place to ask.
/// </summary>
internal static class ItemToolTips
{
	private static readonly Dictionary<int, string> First = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> Second = new Dictionary<int, string>();

	private static readonly object Sync = new object();

	/// <summary>Sets the description for an item id. Empty text clears it.</summary>
	internal static void Set(int id, string first, string second)
	{
		if (id <= 0)
		{
			return;
		}
		lock (Sync)
		{
			SetOne(First, id, first);
			SetOne(Second, id, second);
		}
	}

	private static void SetOne(Dictionary<int, string> map, int id, string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			map.Remove(id);
			return;
		}
		map[id] = text;
	}

	/// <summary>The first description line, or null when the item has none.</summary>
	internal static string GetFirst(int id)
	{
		lock (Sync)
		{
			string result;
			return First.TryGetValue(id, out result) ? result : null;
		}
	}

	/// <summary>The second description line, or null.</summary>
	internal static string GetSecond(int id)
	{
		lock (Sync)
		{
			string result;
			return Second.TryGetValue(id, out result) ? result : null;
		}
	}

	/// <summary>How many items have a description. For the log and the tests.</summary>
	internal static int Count
	{
		get
		{
			lock (Sync)
			{
				return First.Count;
			}
		}
	}

	internal static void Clear()
	{
		lock (Sync)
		{
			First.Clear();
			Second.Clear();
		}
	}
}
