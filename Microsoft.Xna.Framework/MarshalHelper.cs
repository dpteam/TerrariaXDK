using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework;

internal static class MarshalHelper
{
	internal static int SizeOf<T>()
	{
		return Marshal.SizeOf(typeof(T));
	}

	internal static string PtrToInternedStringAnsi(IntPtr ptr)
	{
		string text = Marshal.PtrToStringAnsi(ptr);
		if (text != null)
		{
			text = string.Intern(text);
		}
		return text;
	}
}
