using System;

namespace Terraria;

/// <summary>
/// Shift-click "quick transfer" between the inventory and whatever container is open.
///
/// Why a separate file: the move itself is a merge-then-place loop over two item arrays, and it is
/// the part that can silently duplicate or destroy items. Keeping it out of the UI means it can be
/// driven by the self-test with plain arrays - no graphics, no chest tile, no network.
///
/// The rule is the PC version's: fill existing stacks of the same item first, then take the first
/// empty slot, and never split a stack across more slots than one target can hold.
/// </summary>
internal static class InventoryTransfer
{
	/// <summary>
	/// Moves the stack at <paramref name="FromSlot"/> into <paramref name="To"/>, considering the
	/// first <paramref name="ToCount"/> slots. Returns true when anything moved, and reports the
	/// first target slot that changed so the caller can send it to the server.
	///
	/// A partial move (the target had a half-full stack and no free slot) leaves the remainder in
	/// the source: losing items would be worse than a transfer that only half worked.
	/// </summary>
	internal static bool Move(Item[] From, int FromSlot, Item[] To, int ToCount, out int ChangedSlot)
	{
		ChangedSlot = -1;
		if (From == null || To == null || FromSlot < 0 || FromSlot >= From.Length)
		{
			return false;
		}
		Item item = From[FromSlot];
		if (item.Type <= 0 || item.Stack <= 0)
		{
			return false;
		}
		int num = Math.Min(ToCount, To.Length);
		if (num <= 0)
		{
			return false;
		}
		// 1. Top up existing stacks of the same item.
		if (item.MaxStack > 1)
		{
			for (int i = 0; i < num && item.Type > 0 && item.Stack > 0; i++)
			{
				if (To[i].Type != item.Type || To[i].NetID != item.NetID || To[i].Stack >= To[i].MaxStack)
				{
					continue;
				}
				int num2 = Math.Min(To[i].MaxStack - To[i].Stack, item.Stack);
				if (num2 <= 0)
				{
					continue;
				}
				To[i].Stack += (short)num2;
				item.Stack -= (short)num2;
				if (ChangedSlot < 0)
				{
					ChangedSlot = i;
				}
				if (item.Stack <= 0)
				{
					break;
				}
			}
		}
		// 2. Put what is left into an empty slot.
		if (item.Stack > 0)
		{
			for (int j = 0; j < num; j++)
			{
				if (To[j].Type > 0)
				{
					continue;
				}
				To[j] = item;
				item.Init();
				if (ChangedSlot < 0)
				{
					ChangedSlot = j;
				}
				break;
			}
		}
		bool flag = ChangedSlot >= 0;
		if (item.Stack <= 0 || item.Type <= 0)
		{
			item.Init();
		}
		From[FromSlot] = item;
		return flag;
	}
}
