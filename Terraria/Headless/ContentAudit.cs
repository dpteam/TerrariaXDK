using System;
using System.Collections.Generic;
using System.IO;

namespace Terraria.Headless;

/// <summary>
/// Compares what is in <c>Content/</c> against what the game actually loads.
///
/// The comparison lives here rather than inside the self-test because it has a bug that only
/// shows up off Windows: the manifest speaks <c>/</c> and a directory listing speaks the
/// platform separator, so converting only one side made every file "unexpected" on Linux while
/// the check passed on the machine it was written on. Splitting the scan out means the check can
/// be pointed at a scratch directory and exercised on any platform.
/// </summary>
internal static class ContentAudit
{
	/// <summary>Files allowed in <c>Content/</c> that are not loaded through the pipeline.</summary>
	public static readonly string[] AllowedRaw = new string[3]
	{
		"Worlds/tutorial.wld",
		"ExtMusic/Music_1.wav",
		"ExtMusic/Snow.wav"
	};

	/// <summary>
	/// Names that may live under a directory prefix without being listed one by one: the music
	/// pack is data, not code, so enumerating it in a manifest would only add noise.
	/// </summary>
	public static readonly string[] AllowedPrefixes = new string[1] { "ExtMusic/" };

	/// <summary>
	/// Scans <paramref name="root"/> and returns the files the game does not use, in portable
	/// form. <paramref name="required"/> holds pipeline names without an extension
	/// (<c>Images/_sheetTiles</c>), as written in the manifest.
	/// </summary>
	public static List<string> FindUnexpected(string root, IEnumerable<string> required)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (required != null)
		{
			foreach (string item in required)
			{
				hashSet.Add(Portability.Portable(item) + ".xnb");
			}
		}
		List<string> list = new List<string>();
		if (!Directory.Exists(root))
		{
			return list;
		}
		string[] files = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
		for (int i = 0; i < files.Length; i++)
		{
			// Normalised on both sides, so this works with '/' or '\' from the listing.
			string text = Portability.RelativeTo(root, files[i]);
			string extension = Path.GetExtension(text);
			if (extension.Equals(".xnb", StringComparison.OrdinalIgnoreCase))
			{
				if (!hashSet.Contains(text))
				{
					list.Add(text);
				}
				continue;
			}
			if (Allowed(text))
			{
				continue;
			}
			// Anything else in Content is unexpected, including a stray .dds or .png: the game
			// loads .xnb only, so a raw image there is either a mistake or dead weight.
			list.Add(text);
		}
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	private static bool Allowed(string portablePath)
	{
		for (int i = 0; i < AllowedRaw.Length; i++)
		{
			if (string.Equals(portablePath, AllowedRaw[i], StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		for (int j = 0; j < AllowedPrefixes.Length; j++)
		{
			if (portablePath.StartsWith(AllowedPrefixes[j], StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}
}
