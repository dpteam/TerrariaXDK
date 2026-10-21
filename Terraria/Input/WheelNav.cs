namespace Terraria.Input;

/// <summary>
/// Index arithmetic for scrolling a list with the wheel.
///
/// Kept as pure functions because the two places that use it (hotbar slots and crafting
/// recipes) sit deep inside the console UI code, where a mistake means the highlight lands
/// on a slot that does not exist. The self-test covers the wrapping and the multi-step case
/// that a mouse with a fast wheel produces.
/// </summary>
internal static class WheelNav
{
	/// <summary>
	/// Moves <paramref name="index"/> by <paramref name="delta"/> places in a list of
	/// <paramref name="count"/> items, wrapping in both directions. Returns 0 for an empty or
	/// single-item list, and for any delta when there is nowhere to go.
	/// </summary>
	public static int Wrap(int index, int delta, int count)
	{
		if (count <= 1)
		{
			return 0;
		}
		if (index < 0)
		{
			index = 0;
		}
		else if (index >= count)
		{
			index = count - 1;
		}
		// Modulo twice: C# gives a negative remainder for a negative dividend, and the hotbar
		// has to wrap from slot 0 back to slot 9 in both directions.
		int num = (index + delta) % count;
		if (num < 0)
		{
			num += count;
		}
		return num;
	}

	/// <summary>Formats a wrap as a direction the UI animation code understands (-1/0/1).</summary>
	public static int Direction(int from, int to, int count)
	{
		if (from == to || count <= 1)
		{
			return 0;
		}
		// Shortest way round, so scrolling from 9 to 0 slides forward, not the whole list back.
		int num = to - from;
		if (num > count / 2)
		{
			num -= count;
		}
		else if (num < -(count / 2))
		{
			num += count;
		}
		return (num > 0) ? 1 : ((num < 0) ? (-1) : 0);
	}
}
