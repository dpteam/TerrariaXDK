using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Input;

public static class Keyboard
{
	internal static List<Keys> keys = new List<Keys>();

	public static KeyboardState GetState()
	{
		return new KeyboardState(keys);
	}

	public static KeyboardState GetState(PlayerIndex playerIndex)
	{
		return new KeyboardState(keys);
	}

	public static Keys GetKeyFromScancodeEXT(Keys scancode)
	{
		return FNAPlatform.GetKeyFromScancode(scancode);
	}
}
