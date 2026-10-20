using System;
using System.IO;
using MonoGame.Utilities;

namespace Microsoft.Xna.Framework;

public static class TitleContainer
{
	public static Stream OpenStream(string name)
	{
		string text = FileHelpers.NormalizeFilePathSeparators(name);
		if (Path.IsPathRooted(text))
		{
			return File.OpenRead(text);
		}
		return File.OpenRead(Path.Combine(TitleLocation.Path, text));
	}

	internal static IntPtr ReadToPointer(string name, out IntPtr size)
	{
		string text = FileHelpers.NormalizeFilePathSeparators(name);
		string text2 = ((!Path.IsPathRooted(text)) ? Path.Combine(TitleLocation.Path, text) : text);
		if (!File.Exists(text2))
		{
			throw new FileNotFoundException(text2);
		}
		return FNAPlatform.ReadFileToPointer(text2, out size);
	}
}
