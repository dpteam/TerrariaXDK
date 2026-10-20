using System;
using System.IO;

namespace MonoGame.Utilities;

internal static class FileHelpers
{
	public static readonly char ForwardSlash = '/';

	public static readonly string ForwardSlashString = new string(ForwardSlash, 1);

	public static readonly char BackwardSlash = '\\';

	public static readonly char NotSeparator = ((Path.DirectorySeparatorChar == BackwardSlash) ? ForwardSlash : BackwardSlash);

	public static readonly char Separator = Path.DirectorySeparatorChar;

	public static string NormalizeFilePathSeparators(string name)
	{
		return name.Replace(NotSeparator, Separator);
	}

	public static string ResolveRelativePath(string filePath, string relativeFile)
	{
		filePath = filePath.Replace(BackwardSlash, ForwardSlash);
		while (filePath.Contains("//"))
		{
			filePath = filePath.Replace("//", "/");
		}
		bool flag = filePath.StartsWith(ForwardSlashString);
		if (!flag)
		{
			filePath = ForwardSlashString + filePath;
		}
		Uri baseUri = new Uri("file://" + filePath);
		Uri uri = new Uri(baseUri, relativeFile);
		string text = uri.LocalPath;
		if (!flag && text.StartsWith("/"))
		{
			text = text.Substring(1);
		}
		return NormalizeFilePathSeparators(text);
	}
}
