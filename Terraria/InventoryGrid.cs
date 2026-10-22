namespace Terraria;

/// <summary>
/// The inventory grid's arithmetic, in one place.
///
/// The bug this exists to prevent: drawing used <c>(int)(i * cell)</c> for each column's left
/// edge, while the hit-test used <c>(int)(offset / cell)</c>. Those are not inverses. At 1080p
/// the cell is 104.3 pixels, so column 4 starts at pixel 417 - and 417 / 104.3 is 3.998, which
/// truncates to 3. Every column boundary was off by one pixel, so a click on the first pixel of
/// a slot selected the slot to its left. It is the kind of bug that reads as "the inventory picks
/// the wrong item" rather than "the arithmetic is wrong", which is why it is fixed here, once,
/// instead of at both call sites.
/// </summary>
internal static class InventoryGrid
{
	/// <summary>Left edge of column <paramref name="index"/>, in pixels from the grid's origin.</summary>
	internal static int CellStart(int index, float cell)
	{
		return (int)((float)index * cell);
	}

	/// <summary>
	/// Which cell contains <paramref name="offset"/> pixels from the grid's origin, or -1 when it
	/// is outside the grid. Uses the same expression as <see cref="CellStart"/>, so the two cannot
	/// disagree - that is the whole point.
	///
	/// The division is only a starting guess; the two loops correct it by at most one step, so
	/// this stays constant-time while remaining exact for any cell size, including the fractional
	/// ones the resolution multipliers produce.
	/// </summary>
	internal static int CellIndex(int offset, float cell, int count)
	{
		if (cell <= 0f || count <= 0 || offset < 0)
		{
			return -1;
		}
		// The far edge is exclusive. Without this, the first pixel past the grid resolved to the
		// last cell - the same off-by-one at the other end.
		if (offset >= CellStart(count, cell))
		{
			return -1;
		}
		int num = (int)((float)offset / cell);
		if (num >= count)
		{
			num = count - 1;
		}
		while (num > 0 && CellStart(num, cell) > offset)
		{
			num--;
		}
		while (num + 1 < count && CellStart(num + 1, cell) <= offset)
		{
			num++;
		}
		return num;
	}

	/// <summary>
	/// Converts a pointer position to a cell inside a grid that starts at
	/// (<paramref name="originX"/>, <paramref name="originY"/>). Returns false when the point is
	/// outside, including the negative side: a negative offset divided by a positive cell would
	/// otherwise truncate towards zero and land on cell 0.
	/// </summary>
	internal static bool TryCell(int pointerX, int pointerY, int originX, int originY, float cell, int columns, int rows, out int column, out int row)
	{
		column = -1;
		row = -1;
		if (pointerX < originX || pointerY < originY)
		{
			return false;
		}
		column = CellIndex(pointerX - originX, cell, columns);
		row = CellIndex(pointerY - originY, cell, rows);
		return column >= 0 && row >= 0;
	}
}
