using System;
using System.Collections.Generic;

namespace Terraria.Storage;

/// <summary>
/// The slot arithmetic behind the character and world lists.
///
/// Both lists used to be fixed-size arrays: five characters (plus a sixth "new" row) and sixteen
/// worlds, with the world list split into pages by resolution. The limits were in the arrays, not
/// in anything the player could see, so a full list simply stopped accepting new entries, and the
/// number of worlds you could see depended on the window size.
///
/// This file holds only the pure part - how many slots exist, which window of them is on screen
/// and where the cursor goes - so the scrolling can be tested without a GPU. The callers
/// (<c>UI</c> for characters, <c>WorldSelect</c> for worlds) still own the drawing and the files.
/// </summary>
internal static class SaveSlots
{
	/// <summary>
	/// Index from a save file name, or false when the name is not one of ours.
	///
	/// Parsed rather than taken from position: after a deletion the files are no longer
	/// contiguous, and treating "the third name in the directory listing" as slot 3 would load the
	/// wrong save into the wrong row.
	/// </summary>
	internal static bool TryParseIndex(string fileName, string prefix, string extension, out int index)
	{
		index = -1;
		if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(extension))
		{
			return false;
		}
		if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		int num = prefix.Length;
		int num2 = fileName.Length - extension.Length;
		if (num2 <= num)
		{
			// "player.plr": a name with no digits is not a numbered slot.
			return false;
		}
		string text = fileName.Substring(num, num2 - num);
		for (int i = 0; i < text.Length; i++)
		{
			if (!char.IsDigit(text[i]))
			{
				return false;
			}
		}
		return int.TryParse(text, out index) && index >= 0;
	}

	/// <summary>
	/// How many slots the list needs for these files: the highest index plus one, so a gap left by
	/// a deletion still has a row (and can be reused by a new character).
	/// </summary>
	internal static int SlotCount(IEnumerable<string> fileNames, string prefix, string extension)
	{
		int num = 0;
		if (fileNames == null)
		{
			return 0;
		}
		foreach (string fileName in fileNames)
		{
			int index;
			if (TryParseIndex(fileName, prefix, extension, out index) && index + 1 > num)
			{
				num = index + 1;
			}
		}
		return num;
	}

	/// <summary>
	/// Lowest unused index, or <paramref name="SlotCount"/> when every existing slot is taken -
	/// which grows the list by one, so there is always a slot to create into.
	/// </summary>
	internal static int NextFreeSlot(IList<bool> Used)
	{
		for (int i = 0; i < Used.Count; i++)
		{
			if (!Used[i])
			{
				return i;
			}
		}
		return Used.Count;
	}

	/// <summary>
	/// Scroll position that keeps <paramref name="Selected"/> on screen with the fewest rows
	/// moved. Pure so the boundary cases are testable: selection above the window, below it, and a
	/// list shorter than the window (which scrolls to the top and stays there).
	/// </summary>
	internal static int ClampScroll(int ScrollTop, int Selected, int VisibleRows, int Count)
	{
		if (VisibleRows <= 0 || Count <= 0)
		{
			return 0;
		}
		if (VisibleRows >= Count)
		{
			return 0;
		}
		int num = ScrollTop;
		if (num > Count - VisibleRows)
		{
			num = Count - VisibleRows;
		}
		if (num < 0)
		{
			num = 0;
		}
		if (Selected < num)
		{
			num = Selected;
		}
		else if (Selected >= num + VisibleRows)
		{
			num = Selected - VisibleRows + 1;
		}
		if (num > Count - VisibleRows)
		{
			num = Count - VisibleRows;
		}
		if (num < 0)
		{
			num = 0;
		}
		return num;
	}

	/// <summary>Cursor movement that wraps at both ends, like the console lists always did.</summary>
	internal static int Move(int Selected, int Delta, int Count)
	{
		if (Count <= 0)
		{
			return 0;
		}
		int num = (Selected + Delta) % Count;
		if (num < 0)
		{
			num += Count;
		}
		return num;
	}

	/// <summary>
	/// How many rows fit in <paramref name="Available"/> pixels at <paramref name="RowHeight"/>,
	/// with at least one row so an absurd window size still draws something.
	/// </summary>
	internal static int VisibleRows(int Available, int RowHeight)
	{
		if (RowHeight <= 0)
		{
			return 1;
		}
		int num = Available / RowHeight;
		return (num < 1) ? 1 : num;
	}

	/// <summary>
	/// "Worlds 12/40"-style label. Returns null rather than a pointless "5/5" while everything
	/// fits, because a counter that never changes is noise.
	/// </summary>
	internal static bool NeedsScroll(int Count, int VisibleRows)
	{
		return VisibleRows > 0 && Count > VisibleRows;
	}
}
