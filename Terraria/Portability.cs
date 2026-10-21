using System;
using System.IO;

namespace Terraria;

/// <summary>
/// The few platform facts the game needs, in one place.
///
/// This port was written on Windows and inherited three assumptions: that a path separator is
/// <c>\</c>, that the registry exists, and that the native libraries are called <c>*.dll</c>.
/// Only the first two are code problems; the third is a packaging one (documented, not fixed
/// here - see <c>docs/PORTABILITY.md</c>).
///
/// The rule this class exists to enforce: <b>a path that is compared or displayed is portable,
/// a path that is opened is native</b>. Getting that backwards is what makes a content audit
/// report every file as unexpected on Linux while working perfectly on the machine it was
/// written on.
/// </summary>
internal static class Portability
{
	/// <summary>
	/// True on Windows. Used to decide whether a Windows-only API may be called at all, rather
	/// than to catch the exception it throws when it is not.
	/// </summary>
	public static bool IsWindows
	{
		get
		{
			PlatformID platform = Environment.OSVersion.Platform;
			// Mono reports Unix for macOS as well; either way it is "not Windows", which is all
			// the callers need to know.
			return platform == PlatformID.Win32NT || platform == PlatformID.Win32S || platform == PlatformID.Win32Windows;
		}
	}

	/// <summary>The separator this OS uses for real paths.</summary>
	public static char Separator => Path.DirectorySeparatorChar;

	/// <summary>
	/// A relative path in the portable form: forward slashes, no leading separator.
	///
	/// Forward slashes because they are what the content pipeline, the manifest and every log
	/// line already speak (<c>Images/_sheetTiles</c>), and because Windows accepts them too. A
	/// half-converted comparison - manifest with <c>/</c>, directory listing with <c>\</c> - is
	/// the classic way to make a check that silently passes everywhere except where it matters.
	///
	/// A drive root ("C:\Games") keeps its prefix; it is an absolute path, and stripping the
	/// separator there would produce "C:GAMES", which is not the same place.
	/// </summary>
	public static string Portable(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return string.Empty;
		}
		string text = path.Replace('\\', '/');
		bool flag = text.Length >= 2 && text[1] == ':';
		if (!flag)
		{
			text = text.TrimStart('/');
		}
		return text;
	}

	/// <summary>
	/// The path of <paramref name="fullPath"/> relative to <paramref name="root"/>, in the
	/// portable form. Used for anything that is listed, compared or logged.
	/// </summary>
	public static string RelativeTo(string root, string fullPath)
	{
		if (string.IsNullOrEmpty(fullPath))
		{
			return string.Empty;
		}
		string text = root ?? string.Empty;
		if (text.Length != 0)
		{
			// Compare in one form, so a root written with '/' and a path reported with '\' still
			// match. The trailing separator is added so "Content" does not strip from "Contents".
			string text2 = Portable(text).TrimEnd('/') + "/";
			string text3 = Portable(fullPath);
			if (text3.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
			{
				return text3.Substring(text2.Length);
			}
			if (string.Equals(text3, Portable(text).TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
			{
				return string.Empty;
			}
		}
		return Portable(fullPath);
	}

	/// <summary>
	/// Turns a portable relative path into one this OS can open. Anything that touches the file
	/// system goes through here, so the forward slashes never reach an API that cares.
	/// </summary>
	public static string Native(string portablePath)
	{
		if (string.IsNullOrEmpty(portablePath))
		{
			return string.Empty;
		}
		return (Separator == '/') ? portablePath.Replace('\\', '/') : portablePath.Replace('/', '\\');
	}

	/// <summary>
	/// Combines a root with a portable relative path. <see cref="Path.Combine"/> alone is enough
	/// when the relative part already uses the native separator, which is why the two steps are
	/// kept together here instead of being done ad hoc.
	/// </summary>
	public static string Combine(string root, string portablePath)
	{
		return Path.Combine(root ?? string.Empty, Native(portablePath));
	}

	/// <summary>One line for the log: what the game thinks it is running on.</summary>
	public static string Describe()
	{
		try
		{
			return (IsWindows ? "Windows" : "Unix") + " (" + Environment.OSVersion + "), separator '" + Separator + "'";
		}
		catch
		{
			return "unknown platform";
		}
	}
}
