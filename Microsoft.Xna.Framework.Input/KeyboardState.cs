using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Input;

public struct KeyboardState
{
	private uint keys0 = 0u;

	private uint keys1 = 0u;

	private uint keys2 = 0u;

	private uint keys3 = 0u;

	private uint keys4 = 0u;

	private uint keys5 = 0u;

	private uint keys6 = 0u;

	private uint keys7 = 0u;

	private static Keys[] empty = new Keys[0];

	public KeyState this[Keys key] => InternalGetKey(key) ? KeyState.Down : KeyState.Up;

	public KeyboardState(params Keys[] keys)
	{
		if (keys != null)
		{
			foreach (Keys key in keys)
			{
				InternalSetKey(key);
			}
		}
	}

	internal KeyboardState(List<Keys> keys)
	{
		if (keys == null)
		{
			return;
		}
		foreach (Keys key in keys)
		{
			InternalSetKey(key);
		}
	}

	public bool IsKeyDown(Keys key)
	{
		return InternalGetKey(key);
	}

	public bool IsKeyUp(Keys key)
	{
		return !InternalGetKey(key);
	}

	public Keys[] GetPressedKeys()
	{
		uint num = CountBits(keys0) + CountBits(keys1) + CountBits(keys2) + CountBits(keys3) + CountBits(keys4) + CountBits(keys5) + CountBits(keys6) + CountBits(keys7);
		if (num == 0)
		{
			return empty;
		}
		Keys[] array = new Keys[num];
		int index = 0;
		if (keys0 != 0)
		{
			index = AddKeysToArray(keys0, 0, array, index);
		}
		if (keys1 != 0)
		{
			index = AddKeysToArray(keys1, 32, array, index);
		}
		if (keys2 != 0)
		{
			index = AddKeysToArray(keys2, 64, array, index);
		}
		if (keys3 != 0)
		{
			index = AddKeysToArray(keys3, 96, array, index);
		}
		if (keys4 != 0)
		{
			index = AddKeysToArray(keys4, 128, array, index);
		}
		if (keys5 != 0)
		{
			index = AddKeysToArray(keys5, 160, array, index);
		}
		if (keys6 != 0)
		{
			index = AddKeysToArray(keys6, 192, array, index);
		}
		if (keys7 != 0)
		{
			index = AddKeysToArray(keys7, 224, array, index);
		}
		return array;
	}

	private bool InternalGetKey(Keys key)
	{
		uint num = (uint)(1 << (int)(key & (Keys)0x1F));
		int num2 = (int)key >> 5;
		if (1 == 0)
		{
		}
		int num3 = num2 switch
		{
			0 => (int)keys0, 
			1 => (int)keys1, 
			2 => (int)keys2, 
			3 => (int)keys3, 
			4 => (int)keys4, 
			5 => (int)keys5, 
			6 => (int)keys6, 
			7 => (int)keys7, 
			_ => 0, 
		};
		if (1 == 0)
		{
		}
		return ((uint)num3 & num) != 0;
	}

	private void InternalSetKey(Keys key)
	{
		uint num = (uint)(1 << (int)(key & (Keys)0x1F));
		switch ((int)key >> 5)
		{
		case 0:
			keys0 |= num;
			break;
		case 1:
			keys1 |= num;
			break;
		case 2:
			keys2 |= num;
			break;
		case 3:
			keys3 |= num;
			break;
		case 4:
			keys4 |= num;
			break;
		case 5:
			keys5 |= num;
			break;
		case 6:
			keys6 |= num;
			break;
		case 7:
			keys7 |= num;
			break;
		}
	}

	private void InternalClearKey(Keys key)
	{
		uint num = (uint)(1 << (int)(key & (Keys)0x1F));
		switch ((int)key >> 5)
		{
		case 0:
			keys0 &= ~num;
			break;
		case 1:
			keys1 &= ~num;
			break;
		case 2:
			keys2 &= ~num;
			break;
		case 3:
			keys3 &= ~num;
			break;
		case 4:
			keys4 &= ~num;
			break;
		case 5:
			keys5 &= ~num;
			break;
		case 6:
			keys6 &= ~num;
			break;
		case 7:
			keys7 &= ~num;
			break;
		}
	}

	private void InternalClearAllKeys()
	{
		keys0 = 0u;
		keys1 = 0u;
		keys2 = 0u;
		keys3 = 0u;
		keys4 = 0u;
		keys5 = 0u;
		keys6 = 0u;
		keys7 = 0u;
	}

	public override int GetHashCode()
	{
		return (int)(keys0 ^ keys1 ^ keys2 ^ keys3 ^ keys4 ^ keys5 ^ keys6 ^ keys7);
	}

	public static bool operator ==(KeyboardState a, KeyboardState b)
	{
		return a.keys0 == b.keys0 && a.keys1 == b.keys1 && a.keys2 == b.keys2 && a.keys3 == b.keys3 && a.keys4 == b.keys4 && a.keys5 == b.keys5 && a.keys6 == b.keys6 && a.keys7 == b.keys7;
	}

	public static bool operator !=(KeyboardState a, KeyboardState b)
	{
		return !(a == b);
	}

	public override bool Equals(object obj)
	{
		return obj is KeyboardState && this == (KeyboardState)obj;
	}

	private static uint CountBits(uint v)
	{
		v -= (v >> 1) & 0x55555555;
		v = (v & 0x33333333) + ((v >> 2) & 0x33333333);
		return ((v + (v >> 4)) & 0xF0F0F0F) * 16843009 >> 24;
	}

	private static int AddKeysToArray(uint keys, int offset, Keys[] pressedKeys, int index)
	{
		for (int i = 0; i < 32; i++)
		{
			if ((keys & (1 << i)) != 0)
			{
				pressedKeys[index++] = (Keys)(offset + i);
			}
		}
		return index;
	}
}
