using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace Terraria.Textures;

/// <summary>One region of an atlas replaced by a PNG.</summary>
internal struct AtlasPatch
{
	/// <summary>Name of the sprite in the atlas, as written in the file name.</summary>
	public string Name;

	/// <summary>Where the sprite lives inside the atlas.</summary>
	public Rectangle Rect;

	public ImageData Image;
}

/// <summary>
/// What should be pasted into an atlas, and what could not be understood.
///
/// A plan is a plain value - no graphics device, no file handles - so the geometry can be
/// checked without a GPU. See <see cref="AtlasComposer"/> for the part that touches pixels.
/// </summary>
internal sealed class AtlasPlan
{
	public readonly List<AtlasPatch> Patches = new List<AtlasPatch>();

	/// <summary>Files that could not be used at all. Logged as warnings, because the sprite stays as it was.</summary>
	public readonly List<string> Problems = new List<string>();

	/// <summary>Files that were applied with a remark (a scaled image). Logged as info.</summary>
	public readonly List<string> Notes = new List<string>();

	public bool IsEmpty => Patches.Count == 0;
}

/// <summary>
/// Per-sprite replacement inside a baked atlas.
///
/// The whole-sheet override (<see cref="TextureOverrides"/>) already works, but it asks the
/// author for a 2048x2048 PNG with every other sprite in the right place - which means
/// extracting the sheet first, and one misplaced pixel shifts everything after it. This lets a
/// mod ship <c>Atlas/TILES_0.png</c> and have exactly that region replaced.
///
/// Naming: the file is the sprite's name in the atlas enum, so
/// <c>_sheetTiles.ID.TILES_0</c> becomes <c>Storage/Textures/Atlases/_sheetTiles/TILES_0.png</c>.
/// The names are already public API in the generated enum, so there is no second list to keep in
/// sync - and a typo is reported by name instead of being silently skipped.
/// </summary>
internal static class AtlasOverrides
{
	/// <summary>Folder under <c>Storage/Textures/</c> holding one subfolder per atlas.</summary>
	public static string Root => Path.Combine(Terraria.Storage.AppPaths.Textures, "Atlases");

	/// <summary>
	/// The folder for one atlas. The leaf name is used (<c>_sheetSprites</c>, not
	/// <c>Images/_sheetSprites</c>): atlas names are unique, and a shorter path is easier to
	/// type and to explain.
	/// </summary>
	public static string FolderFor(string atlasName)
	{
		string text = Portability.Portable(atlasName);
		int num = text.LastIndexOf('/');
		if (num >= 0)
		{
			text = text.Substring(num + 1);
		}
		return Path.Combine(Root, text);
	}

	/// <summary>
	/// Builds a plan from the files present on disk.
	///
	/// <paramref name="names"/> is the atlas enum's names, in the same order as
	/// <paramref name="source"/>: the index of a sprite is its rectangle's index. The texture
	/// size is needed to reject a rectangle that does not fit - that would mean the code and the
	/// atlas file disagree, which is worth saying out loud rather than clipping silently.
	/// </summary>
	public static AtlasPlan Build(string atlasName, string[] names, Rectangle[] source, int textureWidth, int textureHeight)
	{
		AtlasPlan plan = new AtlasPlan();
		string folder;
		try
		{
			folder = FolderFor(atlasName);
		}
		catch (Exception ex)
		{
			plan.Problems.Add("could not resolve the folder for " + atlasName + ": " + ex.Message);
			return plan;
		}
		if (!Directory.Exists(folder) || names == null || source == null)
		{
			return plan;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < names.Length && i < source.Length; i++)
		{
			dictionary[names[i]] = i;
		}
		string[] files;
		try
		{
			files = Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly);
		}
		catch (Exception ex)
		{
			plan.Problems.Add("could not read " + Portability.Portable(folder) + ": " + ex.Message);
			return plan;
		}
		Array.Sort(files, StringComparer.OrdinalIgnoreCase);
		HashSet<int> hashSet = new HashSet<int>();
		for (int j = 0; j < files.Length; j++)
		{
			string text = Path.GetFileName(files[j]);
			string extension = Path.GetExtension(text);
			if (!extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
			{
				// A readme or a stray .dds: the readme is genuinely expected, so only report the
				// ones that look like a failed attempt at a texture.
				if (extension.Length != 0 && !text.Equals("readme.txt", StringComparison.OrdinalIgnoreCase))
				{
					plan.Problems.Add(text + ": only .png is supported");
				}
				continue;
			}
			string text2 = Path.GetFileNameWithoutExtension(text);
			int num;
			if (!dictionary.TryGetValue(text2, out num))
			{
				plan.Problems.Add(text + ": no sprite with that name in " + atlasName);
				continue;
			}
			if (!hashSet.Add(num))
			{
				plan.Problems.Add(text + ": another file already replaced " + text2);
				continue;
			}
			Rectangle rectangle = source[num];
			if (rectangle.Width <= 0 || rectangle.Height <= 0)
			{
				plan.Problems.Add(text + ": the atlas rectangle for " + text2 + " is empty");
				continue;
			}
			if (rectangle.X < 0 || rectangle.Y < 0 || rectangle.Right > textureWidth || rectangle.Bottom > textureHeight)
			{
				plan.Problems.Add(text + ": " + text2 + " is at " + rectangle.X + "," + rectangle.Y + " " + rectangle.Width + "x" + rectangle.Height
					+ ", outside the " + textureWidth + "x" + textureHeight + " atlas");
				continue;
			}
			ImageData imageData;
			string error;
			try
			{
				imageData = null;
				if (!PngDecoder.TryDecode(File.ReadAllBytes(files[j]), out imageData, out error))
				{
					plan.Problems.Add(text + ": " + error);
					continue;
				}
			}
			catch (Exception ex2)
			{
				plan.Problems.Add(text + ": could not be read (" + ex2.Message + ")");
				continue;
			}
			if (imageData.Width != rectangle.Width || imageData.Height != rectangle.Height)
			{
				// Not fatal: scaling to fit is what an author usually wants when they export at a
				// different resolution. Said out loud because nearest-neighbour scaling of a
				// sprite at a non-integer ratio looks like corruption rather than a resize.
				plan.Notes.Add(text + ": " + imageData.Width + "x" + imageData.Height + " scaled to "
					+ rectangle.Width + "x" + rectangle.Height + " (the atlas slot size)");
			}
			AtlasPatch item = default(AtlasPatch);
			item.Name = text2;
			item.Rect = rectangle;
			item.Image = imageData;
			plan.Patches.Add(item);
		}
		return plan;
	}

	/// <summary>Creates the atlas folders so they are discoverable, with a readme in the root.</summary>
	public static void EnsureFolders()
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
					"Replace single sprites inside an atlas, leaving the rest of the sheet alone.\r\n"
					+ "\r\n"
					+ "One subfolder per atlas, one PNG per sprite, named as in the game's atlas enum:\r\n"
					+ "  Storage/Textures/Atlases/_sheetTiles/TILES_0.png\r\n"
					+ "  Storage/Textures/Atlases/_sheetSprites/ARM_BONE.png\r\n"
					+ "\r\n"
					+ "The file is pasted into that sprite's slot. A different size is scaled to fit\r\n"
					+ "and mentioned in the log. An unknown name is reported, not ignored.\r\n"
					+ "\r\n"
					+ "To replace the whole sheet, use Storage/Textures/Images/_sheetTiles.png instead.\r\n");
			}
		}
		catch (Exception ex)
		{
			Program.LogWarn("Textures", "could not prepare " + Root + ": " + ex.Message);
		}
	}
}
