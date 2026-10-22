using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Textures;

namespace Terraria.Mods;

/// <summary>
/// Packs the sprites of one mod into a single texture, and caches the result on disk.
///
/// Why one texture per mod rather than one texture per item: <c>SpriteBatch</c> flushes its batch
/// whenever the texture changes, and every sprite in this game is drawn through a single static
/// texture for exactly that reason. A chest holding two hundred mod items would cost two hundred
/// flushes per frame if each item had its own file.
///
/// Why the cache: packing means decoding every PNG and uploading a texture. Doing that on every
/// launch is visible on a mod with a few hundred sprites, and the result only changes when the
/// sprite files do. The cache is keyed on names, sizes and write times, so editing one sprite
/// rebuilds that mod's sheet and nothing else.
///
/// Why per mod rather than one global sheet: a mod that fails to pack cannot corrupt the others,
/// and one mod changing does not rebuild everybody.
/// </summary>
internal static class ModSheets
{
	internal const string Magic = "TerrariaXDK-mod-sheet";

	/// <summary>Largest sheet side. Bigger than any single mod's sprite set needs, small enough for old GPUs.</summary>
	private const int MaxSide = 4096;

	/// <summary>One packed sheet: the texture and where each named sprite landed.</summary>
	internal sealed class Sheet
	{
		public string ModName;

		public Texture2D Texture;

		public readonly Dictionary<string, Rectangle> ByName = new Dictionary<string, Rectangle>(StringComparer.OrdinalIgnoreCase);

		/// <summary>Why the sheet is empty, or empty when it packed fine.</summary>
		public string Problem = string.Empty;

		public bool IsUsable => Texture != null && ByName.Count > 0;

		public bool TryGet(string sprite, out Rectangle rect)
		{
			rect = default;
			return Texture != null && ByName.TryGetValue(sprite, out rect);
		}
	}

	private static readonly Dictionary<string, Sheet> Sheets = new Dictionary<string, Sheet>(StringComparer.OrdinalIgnoreCase);

	private static readonly object Sync = new object();

	internal static string CacheDirectory => Path.Combine(Terraria.Storage.AppPaths.Cache, "ModSheets");

	/// <summary>
	/// Builds or loads the sheet for one mod. <paramref name="sprites"/> maps a sprite key (the
	/// item name) to a file path relative to <paramref name="modDirectory"/>.
	///
	/// Never throws: a broken sprite becomes a logged problem and that item simply has no picture,
	/// which is better than a mod that cannot load at all.
	/// </summary>
	internal static Sheet Get(GraphicsDevice device, string modName, string modDirectory, IDictionary<string, string> sprites)
	{
		lock (Sync)
		{
			Sheet sheet;
			if (Sheets.TryGetValue(modName, out sheet))
			{
				return sheet;
			}
			sheet = new Sheet();
			sheet.ModName = modName;
			Sheets[modName] = sheet;
			if (sprites == null || sprites.Count == 0)
			{
				sheet.Problem = "the mod declares no sprites";
				return sheet;
			}
			if (device == null)
			{
				sheet.Problem = "no graphics device";
				return sheet;
			}
			try
			{
				Build(device, sheet, modDirectory, sprites);
			}
			catch (Exception ex)
			{
				sheet.Problem = ex.GetType().Name + ": " + ex.Message;
				ModLoader.Log("[ModSheets] " + modName + ": " + sheet.Problem);
			}
			return sheet;
		}
	}

	/// <summary>Drops every sheet. Called when mods are reloaded, and by tests.</summary>
	internal static void Clear()
	{
		lock (Sync)
		{
			foreach (KeyValuePair<string, Sheet> sheet in Sheets)
			{
				try
				{
					sheet.Value.Texture?.Dispose();
				}
				catch
				{
				}
			}
			Sheets.Clear();
		}
	}

	private static void Build(GraphicsDevice device, Sheet sheet, string modDirectory, IDictionary<string, string> sprites)
	{
		// Sorted so the layout is deterministic: the same mod always packs the same way, which is
		// what makes the cache key stable.
		List<string> keys = new List<string>(sprites.Keys);
		keys.Sort(StringComparer.OrdinalIgnoreCase);
		string key = CacheKey(modDirectory, keys, sprites);
		string cacheBase = Path.Combine(CacheDirectory, SafeFileName(sheet.ModName));
		string metricsPath = cacheBase + ".metrics";
		string texturePath = cacheBase + ".png";
		if (TryLoadCache(device, sheet, metricsPath, texturePath, key))
		{
			return;
		}
		// Cache miss: decode everything, pack, upload, and write the cache for next time.
		List<Entry> entries = new List<Entry>();
		for (int i = 0; i < keys.Count; i++)
		{
			string path = sprites[keys[i]];
			ImageData image;
			if (!TryDecode(modDirectory, path, out image, out string error))
			{
				ModLoader.Log("[ModSheets] " + sheet.ModName + ": sprite '" + keys[i] + "' skipped - " + error);
				continue;
			}
			Entry entry = new Entry();
			entry.Name = keys[i];
			entry.Image = image;
			entries.Add(entry);
		}
		if (entries.Count == 0)
		{
			sheet.Problem = "no sprite could be decoded";
			return;
		}
		int width;
		int height;
		Pack(entries, out width, out height);
		Color[] pixels = new Color[width * height];
		for (int j = 0; j < entries.Count; j++)
		{
			Entry entry2 = entries[j];
			for (int k = 0; k < entry2.Image.Height; k++)
			{
				for (int l = 0; l < entry2.Image.Width; l++)
				{
					pixels[(entry2.Y + k) * width + entry2.X + l] = AtlasComposer.ToPremultiplied(entry2.Image, l, k);
				}
			}
			sheet.ByName[entry2.Name] = new Rectangle(entry2.X, entry2.Y, entry2.Image.Width, entry2.Image.Height);
		}
		Texture2D texture = new Texture2D(device, width, height);
		texture.SetData(pixels);
		sheet.Texture = texture;
		WriteCache(sheet, metricsPath, texturePath, key, width, height);
	}

	private sealed class Entry
	{
		public string Name;

		public ImageData Image;

		public int X;

		public int Y;
	}

	/// <summary>
	/// Shelf packing: sort by height, fill a row until it is full, start the next. Not optimal,
	/// but the sheet is built once and the waste is a few percent on sprite art, which is what
	/// this has to pack.
	/// </summary>
	private static void Pack(List<Entry> entries, out int width, out int height)
	{
		entries.Sort((a, b) => b.Image.Height.CompareTo(a.Image.Height));
		long area = 0L;
		int widest = 1;
		for (int i = 0; i < entries.Count; i++)
		{
			area += (long)entries[i].Image.Width * entries[i].Image.Height;
			if (entries[i].Image.Width > widest)
			{
				widest = entries[i].Image.Width;
			}
		}
		// Aim for a square, rounded up to a power of two: friendlier to old GPUs than an odd size.
		int num = 256;
		while (num < MaxSide && (long)num * num < area)
		{
			num <<= 1;
		}
		if (num < widest)
		{
			num = widest;
		}
		width = num;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		for (int j = 0; j < entries.Count; j++)
		{
			Entry entry = entries[j];
			if (num3 + entry.Image.Width > width)
			{
				num4 += num2;
				num2 = 0;
				num3 = 0;
			}
			entry.X = num3;
			entry.Y = num4;
			num3 += entry.Image.Width;
			if (entry.Image.Height > num2)
			{
				num2 = entry.Image.Height;
			}
		}
		height = num4 + num2;
		// Round the height up as well; a texture of an arbitrary height is legal but wasteful.
		int num5 = 1;
		while (num5 < height)
		{
			num5 <<= 1;
		}
		height = Math.Min(num5, MaxSide);
	}

	private static bool TryDecode(string modDirectory, string relative, out ImageData image, out string error)
	{
		image = null;
		error = null;
		string text = Path.Combine(modDirectory, (relative ?? string.Empty).Replace('/', Path.DirectorySeparatorChar));
		if (!File.Exists(text))
		{
			error = "no such file: " + relative;
			return false;
		}
		byte[] data;
		try
		{
			data = File.ReadAllBytes(text);
		}
		catch (Exception ex)
		{
			error = "cannot read " + relative + ": " + ex.Message;
			return false;
		}
		return PngDecoder.TryDecode(data, out image, out error);
	}

	/// <summary>
	/// Hash of everything that would change the packed result: the sorted sprite names, their
	/// sizes and their write times. Editing one PNG changes its time and invalidates the cache.
	/// </summary>
	private static string CacheKey(string modDirectory, List<string> keys, IDictionary<string, string> sprites)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < keys.Count; i++)
		{
			stringBuilder.Append(keys[i]).Append('|');
			string text = Path.Combine(modDirectory, (sprites[keys[i]] ?? string.Empty).Replace('/', Path.DirectorySeparatorChar));
			try
			{
				FileInfo fileInfo = new FileInfo(text);
				if (fileInfo.Exists)
				{
					stringBuilder.Append(fileInfo.Length).Append('|').Append(fileInfo.LastWriteTimeUtc.Ticks);
				}
				else
				{
					stringBuilder.Append("missing");
				}
			}
			catch
			{
				stringBuilder.Append("unknown");
			}
			stringBuilder.Append('\n');
		}
		uint num = 2166136261u;
		string text2 = stringBuilder.ToString();
		for (int j = 0; j < text2.Length; j++)
		{
			num = (num ^ text2[j]) * 16777619;
		}
		return num.ToString("x8");
	}

	/// <summary>
	/// Parses a cache metrics file. Pure, so the format that decides whether a cached sheet is
	/// still valid can be tested without a graphics device - and that format is the part that
	/// silently rots: a stale key would serve sprites that no longer match their files.
	/// </summary>
	internal static Dictionary<string, Rectangle> ParseMetrics(string text, string expectedKey, out string error)
	{
		error = null;
		Dictionary<string, Rectangle> dictionary = new Dictionary<string, Rectangle>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrEmpty(text))
		{
			error = "empty metrics";
			return null;
		}
		string[] array = text.Replace("\r\n", "\n").Split('\n');
		if (array.Length < 2 || !array[0].StartsWith("# " + Magic, StringComparison.Ordinal))
		{
			error = "not a " + Magic + " file";
			return null;
		}
		if (array[1] != "key=" + expectedKey)
		{
			error = "the cache was written for different sprites";
			return null;
		}
		for (int i = 2; i < array.Length; i++)
		{
			string text2 = array[i];
			if (text2.Length == 0 || text2[0] == '#')
			{
				continue;
			}
			// "x y w h name" - the name is last so it may contain spaces, which real item names do.
			string[] array2 = text2.Split(new char[1] { ' ' }, 5);
			if (array2.Length != 5)
			{
				error = "line " + (i + 1) + " does not have 5 fields";
				return null;
			}
			int num;
			int num2;
			int num3;
			int num4;
			if (!int.TryParse(array2[0], out num) || !int.TryParse(array2[1], out num2)
				|| !int.TryParse(array2[2], out num3) || !int.TryParse(array2[3], out num4))
			{
				error = "line " + (i + 1) + " has a non-numeric rectangle";
				return null;
			}
			if (num3 <= 0 || num4 <= 0 || num < 0 || num2 < 0)
			{
				error = "line " + (i + 1) + " has an impossible rectangle";
				return null;
			}
			dictionary[array2[4]] = new Rectangle(num, num2, num3, num4);
		}
		if (dictionary.Count == 0)
		{
			error = "the file lists no sprites";
			return null;
		}
		return dictionary;
	}

	/// <summary>Writes the metrics in the form <see cref="ParseMetrics"/> reads. Pure.</summary>
	internal static string FormatMetrics(string key, IDictionary<string, Rectangle> byName)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("# ").Append(Magic).Append(" 1\n");
		stringBuilder.Append("key=").Append(key).Append('\n');
		List<string> list = new List<string>(byName.Keys);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < list.Count; i++)
		{
			Rectangle rectangle = byName[list[i]];
			stringBuilder.Append(rectangle.X.ToString(CultureInfo.InvariantCulture)).Append(' ')
				.Append(rectangle.Y.ToString(CultureInfo.InvariantCulture)).Append(' ')
				.Append(rectangle.Width.ToString(CultureInfo.InvariantCulture)).Append(' ')
				.Append(rectangle.Height.ToString(CultureInfo.InvariantCulture)).Append(' ')
				.Append(list[i]).Append('\n');
		}
		return stringBuilder.ToString();
	}

	private static bool TryLoadCache(GraphicsDevice device, Sheet sheet, string metricsPath, string texturePath, string key)
	{
		try
		{
			if (!File.Exists(metricsPath) || !File.Exists(texturePath))
			{
				return false;
			}
			string error;
			Dictionary<string, Rectangle> dictionary = ParseMetrics(File.ReadAllText(metricsPath), key, out error);
			if (dictionary == null)
			{
				ModLoader.Log("[ModSheets] " + sheet.ModName + ": cache ignored - " + error);
				return false;
			}
			Texture2D texture2D;
			using (FileStream stream = File.OpenRead(texturePath))
			{
				texture2D = Texture2D.FromStream(device, stream);
			}
			if (texture2D == null)
			{
				return false;
			}
			foreach (KeyValuePair<string, Rectangle> item in dictionary)
			{
				Rectangle rectangle = item.Value;
				if (rectangle.X < 0 || rectangle.Y < 0 || rectangle.Right > texture2D.Width || rectangle.Bottom > texture2D.Height)
				{
					texture2D.Dispose();
					return false;
				}
			}
			sheet.Texture = texture2D;
			foreach (KeyValuePair<string, Rectangle> item2 in dictionary)
			{
				sheet.ByName[item2.Key] = item2.Value;
			}
			return true;
		}
		catch (Exception ex)
		{
			ModLoader.Log("[ModSheets] " + sheet.ModName + ": cache ignored - " + ex.Message);
			return false;
		}
	}

	private static void WriteCache(Sheet sheet, string metricsPath, string texturePath, string key, int width, int height)
	{
		try
		{
			Directory.CreateDirectory(CacheDirectory);
			using (FileStream stream = File.Create(texturePath))
			{
				sheet.Texture.SaveAsPng(stream, width, height);
			}
			File.WriteAllText(metricsPath, FormatMetrics(key, sheet.ByName), new UTF8Encoding(false));
		}
		catch (Exception ex)
		{
			// A cache that cannot be written is not a failure: the sheet is already in memory and
			// the game works. Only the next launch is slower.
			ModLoader.Log("[ModSheets] " + sheet.ModName + ": could not write the cache - " + ex.Message);
		}
	}

	/// <summary>A mod name as a file name: no separators, no characters Windows rejects.</summary>
	internal static string SafeFileName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return "mod";
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < name.Length && stringBuilder.Length < 48; i++)
		{
			char c = name[i];
			if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
			{
				stringBuilder.Append(c);
			}
			else
			{
				stringBuilder.Append('_');
			}
		}
		return (stringBuilder.Length == 0) ? "mod" : stringBuilder.ToString();
	}
}
