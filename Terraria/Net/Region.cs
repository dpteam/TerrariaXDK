using System;
using System.Collections.Generic;

namespace Terraria.Net;

/// <summary>
/// A named rectangle of the world with its own rules.
///
/// Spawn protection answers "do not touch the tiles where players arrive". A region answers
/// the general question: "this is my house, only my group may build here". Regions are
/// rectangles for the same reason the spawn box is: the player can see a rectangle on the map,
/// so "why is this tile blocked" has an answer they can check.
/// </summary>
internal sealed class Region
{
	public string Name;

	/// <summary>Inclusive bounds, in tiles. Normalised on load (x1 &lt;= x2, y1 &lt;= y2).</summary>
	public int X1;

	public int Y1;

	public int X2;

	public int Y2;

	/// <summary>
	/// Group allowed to edit inside. Null or empty means "nobody except the host" - a region is
	/// created to protect something, so the safe default is closed.
	/// </summary>
	public string Group;

	public int Width => X2 - X1 + 1;

	public int Height => Y2 - Y1 + 1;

	public bool Contains(int x, int y)
	{
		return x >= X1 && x <= X2 && y >= Y1 && y <= Y2;
	}

	/// <summary>Makes the rectangle well-formed. A region given "backwards" must still work.</summary>
	public void Normalize()
	{
		if (X1 > X2)
		{
			int num = X1;
			X1 = X2;
			X2 = num;
		}
		if (Y1 > Y2)
		{
			int num2 = Y1;
			Y1 = Y2;
			Y2 = num2;
		}
	}

	/// <summary>
	/// True when this region overlaps another. Used to refuse duplicates that would be
	/// indistinguishable in the list: two regions with the same bounds but different groups
	/// means the answer to "may I build here" depends on iteration order.
	/// </summary>
	public bool Overlaps(Region other)
	{
		return other != null && X1 <= other.X2 && other.X1 <= X2 && Y1 <= other.Y2 && other.Y1 <= Y2;
	}

	public override string ToString()
	{
		return Texts.Format("chat.host.region.bounds", Name, X1, Y1, X2, Y2, string.IsNullOrEmpty(Group) ? Texts.Get("chat.host.region.group.host") : Texts.Format("chat.host.region.group.named", Group));
	}

	/// <summary>Short form for the region list.</summary>
	public string Describe()
	{
		return Texts.Format("chat.host.region.describe", Name, Width, Height, X1, Y1, string.IsNullOrEmpty(Group) ? Texts.Get("chat.host.region.group.host") : Texts.Format("chat.host.region.group.build", Group));
	}
}
