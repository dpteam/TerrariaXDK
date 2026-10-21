using System;
using System.Collections.Generic;
using System.IO;

namespace Terraria.Textures;

/// <summary>
/// Loose image files that stand in for a compiled <c>.xnb</c> texture.
///
/// Why: the game can only load <c>.xnb</c>, so replacing a texture - or shipping one with a mod -
/// means running the XNA content pipeline, which needs MonoGame's tooling and a Windows box.
/// A folder of PNGs named like the assets removes that step entirely: drop
/// <c>Storage/Textures/Images/_sheetTiles.png</c> in place and it is used instead.
///
/// Rules, in the order they matter:
/// <list type="number">
/// <item>the override wins over the embedded asset, because that is the only reason to have one;</item>
/// <item>if the override cannot be read or decoded, the game falls back to the compiled asset and
/// logs why - a broken texture must not become a broken game;</item>
/// <item>only PNG. A <c>.dds</c> in the folder is reported by name rather than ignored, because
/// "I put my texture there and nothing happened" is the worst possible answer.</item>
/// </list>
/// </summary>
internal static class TextureOverrides
{
	/// <summary>Folder under <c>Storage/</c> that mirrors the content tree.</summary>
	public static string Root => Terraria.Storage.AppPaths.Textures;

	/// <summary>Extensions the loader can actually read. Everything else is reported, not guessed at.</summary>
	public static readonly string[] Supported = new string[1] { ".png" };

	/// <summary>
	/// Extensions that are plausible but unsupported, so the message can say why instead of
	/// "no override found".
	/// </summary>
	private static readonly string[] KnownUnsupported = new string[3] { ".dds", ".jpg", ".jpeg" };

	private static readonly List<string> _problems = new List<string>();

	private static readonly object Sync = new object();

	/// <summary>Override files that failed to load, for the log and for tests.</summary>
	public static IList<string> Problems
	{
		get
		{
			lock (Sync)
			{
				return new List<string>(_problems);
			}
		}
	}

	/// <summary>Forgets the recorded problems. Called when a world starts, not per texture.</summary>
	public static void ResetProblems()
	{
		lock (Sync)
		{
			_problems.Clear();
		}
	}

	private static void Problem(string message)
	{
		lock (Sync)
		{
			// One line per distinct asset: loading a broken atlas would otherwise produce a
			// warning for every texture that shares it.
			if (_problems.Count < 64 && !_problems.Contains(message))
			{
				_problems.Add(message);
			}
		}
		Program.LogWarn("Textures", message);
	}

	/// <summary>
	/// The override file for a content asset name, or null when there is none.
	/// <c>Images/_sheetTiles</c> maps to <c>Storage/Textures/Images/_sheetTiles.png</c>: the same
	/// relative path the content pipeline uses, so there is nothing to look up in a table.
	/// </summary>
	public static string Resolve(string assetName)
	{
		if (string.IsNullOrEmpty(assetName))
		{
			return null;
		}
		string text = Portability.Portable(assetName);
		for (int i = 0; i < Supported.Length; i++)
		{
			string text2 = Portability.Combine(Root, text + Supported[i]);
			if (File.Exists(text2))
			{
				return text2;
			}
		}
		for (int j = 0; j < KnownUnsupported.Length; j++)
		{
			if (File.Exists(Portability.Combine(Root, text + KnownUnsupported[j])))
			{
				Problem("Storage/Textures/" + text + KnownUnsupported[j] + " is not supported: save it as .png");
				return null;
			}
		}
		return null;
	}

	/// <summary>
	/// Reads and decodes the override for an asset. Returns null when there is none, and also
	/// when there is one but it is broken - the caller decides whether to fall back, and both
	/// cases mean "use the compiled asset".
	/// </summary>
	public static ImageData Load(string assetName, out string path)
	{
		path = Resolve(assetName);
		if (path == null)
		{
			return null;
		}
		byte[] data;
		try
		{
			data = File.ReadAllBytes(path);
		}
		catch (Exception ex)
		{
			Problem(Path.GetFileName(path) + ": could not be read (" + ex.Message + "), using the built-in texture");
			return null;
		}
		ImageData imageData;
		string error;
		if (!PngDecoder.TryDecode(data, out imageData, out error))
		{
			Problem(Path.GetFileName(path) + ": " + error + " - using the built-in texture");
			return null;
		}
		Program.LogInfo("Textures", "override " + Portability.RelativeTo(Root, path) + " (" + imageData.Describe() + ")");
		return imageData;
	}

	/// <summary>
	/// Every override file currently installed, in portable form. Used by the mod screen and by
	/// the self-test to prove the folder is being read at all.
	/// </summary>
	public static List<string> Installed()
	{
		List<string> list = new List<string>();
		try
		{
			if (!Directory.Exists(Root))
			{
				return list;
			}
			string[] files = Directory.GetFiles(Root, "*", SearchOption.AllDirectories);
			for (int i = 0; i < files.Length; i++)
			{
				list.Add(Portability.RelativeTo(Root, files[i]));
			}
			list.Sort(StringComparer.OrdinalIgnoreCase);
		}
		catch (Exception ex)
		{
			Problem("could not list Storage/Textures: " + ex.Message);
		}
		return list;
	}

	/// <summary>
	/// Creates the folder so it is discoverable, and drops a readme explaining the naming rule.
	/// Written once; never overwritten, since the player may have edited it.
	/// </summary>
	public static void EnsureFolder()
	{
		try
		{
			if (!Directory.Exists(Root))
			{
				Directory.CreateDirectory(Root);
			}
			string text = Path.Combine(Root, "readme.txt");
			if (!File.Exists(text))
			{
				File.WriteAllText(text,
					"Drop PNG files here to replace the game's textures without rebuilding them.\r\n"
					+ "\r\n"
					+ "The path mirrors the content tree, and the folder name is dropped:\r\n"
					+ "  Images/_sheetTiles  ->  Storage/Textures/Images/_sheetTiles.png\r\n"
					+ "  UI/ProgressBar      ->  Storage/Textures/UI/ProgressBar.png\r\n"
					+ "\r\n"
					+ "Only PNG is supported. If a file is missing or broken the game uses the\r\n"
					+ "built-in texture and says so in Storage/Logs/terraria-debug.log.\r\n");
			}
		}
		catch (Exception ex)
		{
			Program.LogWarn("Textures", "could not prepare " + Root + ": " + ex.Message);
		}
	}
}
