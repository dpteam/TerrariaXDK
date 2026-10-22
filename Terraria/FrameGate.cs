namespace Terraria;

/// <summary>
/// Makes "one click, one action" true for the inventory, which has two input paths that both run
/// every frame: the pointer and the stick.
///
/// The bug this exists to prevent is not obvious from the code, which is why it is its own type
/// with its own test: <c>IsButtonTriggered</c> is a pure edge test against the previous frame's
/// state and is <b>not consumed</b>. So when both paths called the same slot handler, the action
/// ran twice - an item was picked up and immediately put back. On screen that looks like the
/// mouse does nothing at all, and a slot whose second call happened to be a no-op looked like the
/// only working one.
///
/// Reset once per frame by <c>UI.UpdateMouse</c>; the first caller in a frame wins.
/// </summary>
internal sealed class FrameGate
{
	private bool _taken;

	/// <summary>True the first time it is called after a reset, false afterwards.</summary>
	internal bool TryBegin()
	{
		if (_taken)
		{
			return false;
		}
		_taken = true;
		return true;
	}

	/// <summary>True when something has already acted this frame.</summary>
	internal bool Taken => _taken;

	/// <summary>Starts a new frame.</summary>
	internal void Reset()
	{
		_taken = false;
	}
}
