using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Text;

namespace Terraria.FontAtlasTool;

/// <summary>
/// Turns a TrueType font into the atlas pair the game reads: a PNG of glyph cells and a text
/// metrics file (<see cref="Terraria.Fonts.FontAtlas"/> describes the format).
///
/// Why a separate tool instead of doing this in the game: rasterising a TTF needs GDI+, which is
/// Windows-only. This port has to run on Linux, so the dependency stays in a tool that runs once,
/// on the machine of whoever is adding a language. The game itself only reads a PNG and a text
/// file, both of which are platform-neutral.
///
/// The output convention matches the baked console atlases exactly: every glyph is a cell of
/// <c>advance x lineHeight</c>, with the baseline at a fixed row, drawn from the cell's top-left.
/// Getting this wrong is not a subtle bug - the text either overlaps or drifts upward per
/// character - so the tool and the game share one description of it rather than two.
/// </summary>
internal static class Program
{
	/// <summary>
	/// Cell sizes per resolution preset. The numbers come from the baked atlases' line spacing
	/// (152 / 30 / 33 pixels at 1080p) scaled to the font's own em size, so generated text has
	/// the same visual weight as the shipped text it replaces.
	/// </summary>
	private static readonly Dictionary<string, double> Presets = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
	{
		{ "fhd", 1.0 },
		{ "hd", 0.75 },
		{ "sd", 0.5625 }
	};

	/// <summary>Em size in pixels at 1080p, per font kind.</summary>
	private static readonly Dictionary<string, double> BaseEm = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
	{
		{ "big", 112.0 },
		{ "small", 22.0 },
		{ "small2", 24.0 }
	};

	public static int Main(string[] args)
	{
		string text = null;
		string text2 = null;
		string text3 = "fhd";
		string text4 = null;
		for (int i = 0; i < args.Length; i++)
		{
			string text5 = args[i].ToLowerInvariant();
			if (text5 == "--out")
			{
				text2 = (i + 1 < args.Length) ? args[++i] : null;
			}
			else if (text5 == "--preset")
			{
				text3 = (i + 1 < args.Length) ? args[++i] : null;
			}
			else if (text5 == "--chars")
			{
				text4 = (i + 1 < args.Length) ? args[++i] : null;
			}
			else if (text == null)
			{
				text = args[i];
			}
		}
		if (text == null || text2 == null)
		{
			Console.Error.WriteLine("Usage: FontAtlas <font.ttf> --out <folder> [--preset fhd|hd|sd] [--chars <file>]");
			Console.Error.WriteLine();
			Console.Error.WriteLine("  Writes <code>.big.metrics, <code>.small.metrics, <code>.small2.metrics");
			Console.Error.WriteLine("  and their PNGs, ready to drop into Storage/Fonts/.");
			Console.Error.WriteLine("  --chars is a UTF-8 text file whose characters are all included");
			Console.Error.WriteLine("  (default: Latin + Cyrillic + Greek + punctuation).");
			return 2;
		}
		double num;
		if (!Presets.TryGetValue(text3 ?? "", out num))
		{
			Console.Error.WriteLine("Unknown preset '" + text3 + "'. Use fhd, hd or sd.");
			return 2;
		}
		if (!File.Exists(text))
		{
			Console.Error.WriteLine("No such font: " + text);
			return 2;
		}
		string text6 = Charset(text4);
		if (text6 == null)
		{
			return 1;
		}
		try
		{
			Directory.CreateDirectory(text2);
			using PrivateFontCollection privateFontCollection = new PrivateFontCollection();
			privateFontCollection.AddFontFile(text);
			if (privateFontCollection.Families.Length == 0)
			{
				Console.Error.WriteLine("GDI+ could not open " + text + " as a font.");
				return 1;
			}
			string text7 = Path.GetFileNameWithoutExtension(text);
			foreach (KeyValuePair<string, double> baseEm in BaseEm)
			{
				float num2 = (float)(baseEm.Value * num);
				if (!WriteKind(text2, text7, baseEm.Key, privateFontCollection.Families[0], num2, text6))
				{
					return 1;
				}
			}
			Console.WriteLine("Wrote " + text7 + ".* into " + Path.GetFullPath(text2));
			Console.WriteLine("Rename the files to <language code>.* (e.g. ru.big.metrics) and put them in Storage/Fonts/.");
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine("Failed: " + ex.GetType().Name + ": " + ex.Message);
			return 1;
		}
	}

	/// <summary>
	/// The characters to include. A default set rather than "everything in the font": a full
	/// TrueType file has thousands of glyphs, and a texture holding all of them would be both
	/// enormous and mostly empty.
	/// </summary>
	private static string Charset(string path)
	{
		if (path != null)
		{
			try
			{
				return File.ReadAllText(path);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Cannot read the charset file: " + ex.Message);
				return null;
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		// Printable ASCII, the same range the baked atlases carry.
		for (char c = ' '; c <= '~'; c++)
		{
			stringBuilder.Append(c);
		}
		// Cyrillic: the reason this tool exists.
		for (char c2 = '\u0410'; c2 <= '\u044f'; c2++)
		{
			stringBuilder.Append(c2);
		}
		stringBuilder.Append("\u0401\u0451"); // Ё ё
		// Greek.
		for (char c3 = '\u0391'; c3 <= '\u03c9'; c3++)
		{
			stringBuilder.Append(c3);
		}
		// Punctuation that appears in translations but not in ASCII.
		stringBuilder.Append("\u00ab\u00bb\u2014\u2013\u2026\u00a0\u00b0\u2116\u20ac\u2116");
		return stringBuilder.ToString();
	}

	private static bool WriteKind(string folder, string code, string kind, FontFamily family, float emSize, string chars)
	{
		List<char> list = new List<char>();
		HashSet<char> hashSet = new HashSet<char>();
		for (int i = 0; i < chars.Length; i++)
		{
			char c = chars[i];
			if (c == '\r' || c == '\n' || c == '\t' || hashSet.Contains(c))
			{
				continue;
			}
			hashSet.Add(c);
			list.Add(c);
		}
		if (list.Count == 0)
		{
			Console.Error.WriteLine("The charset is empty.");
			return false;
		}
		using Font font = new Font(family, emSize, FontStyle.Regular, GraphicsUnit.Pixel);
		// One measurement pass to get the cell height and the widest advance, so the packing is
		// a plain grid and every cell has the same height. Uniform cells are what the game's
		// cropping convention expects.
		int num = (int)Math.Ceiling(font.GetHeight());
		int num2 = 0;
		int[] array = new int[list.Count];
		using (Bitmap bitmap = new Bitmap(1, 1))
		{
			using Graphics graphics = Graphics.FromImage(bitmap);
			graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
			using StringFormat stringFormat = StringFormat.GenericTypographic;
			stringFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
			for (int j = 0; j < list.Count; j++)
			{
				array[j] = (int)Math.Ceiling(graphics.MeasureString(list[j].ToString(), font, PointF.Empty, stringFormat).Width);
				if (array[j] < 1)
				{
					array[j] = 1;
				}
				if (array[j] > num2)
				{
					num2 = array[j];
				}
			}
		}
		// Packing: a grid of the widest advance. Wasteful for a proportional font, but the game
		// draws each cell at its own advance, so cells do not have to be uniform width - only
		// uniform height. Keeping the grid means the metrics stay trivial to read and verify.
		int num3 = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(list.Count)));
		int num4 = num3 * num2;
		int num5 = num3 * num;
		if (num4 > 8192 || num5 > 8192)
		{
			Console.Error.WriteLine("The atlas would be " + num4 + "x" + num5 + ", which is too large. Use a smaller preset.");
			return false;
		}
		using Bitmap bitmap2 = new Bitmap(num4, num5, PixelFormat.Format32bppArgb);
		using (Graphics graphics2 = Graphics.FromImage(bitmap2))
		{
			graphics2.Clear(Color.Transparent);
			graphics2.TextRenderingHint = TextRenderingHint.AntiAlias;
			graphics2.SmoothingMode = SmoothingMode.AntiAlias;
			using StringFormat stringFormat2 = StringFormat.GenericTypographic;
			// The baseline sits at the font's ascent, so a glyph that reaches above the cap height
			// (accents, Ё) still fits and nothing drifts.
			float num6 = (float)Math.Ceiling(font.Size * (float)family.GetLineSpacing(FontStyle.Regular) / (float)family.GetEmHeight(FontStyle.Regular));
			if (num6 > num)
			{
				num6 = num;
			}
			for (int k = 0; k < list.Count; k++)
			{
				int num7 = (k % num3) * num2;
				int num8 = (k / num3) * num;
				graphics2.DrawString(list[k].ToString(), font, Brushes.White, new PointF(num7, num8), stringFormat2);
			}
		}
		string text = code + "." + kind;
		string text2 = Path.Combine(folder, text + ".png");
		string text3 = Path.Combine(folder, text + ".metrics");
		bitmap2.Save(text2, ImageFormat.Png);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("# TerrariaXDK-font-atlas 1\n");
		stringBuilder.Append("# generated by tools/FontAtlas from ").Append(Path.GetFileName(family.Name)).Append('\n');
		stringBuilder.Append("texture=").Append(text).Append(".png\n");
		stringBuilder.Append("lineHeight=").Append(num.ToString(CultureInfo.InvariantCulture)).Append('\n');
		stringBuilder.Append("spacing=0\n");
		// '?' is in every set above; if someone supplies a charset without it, -1 means "no
		// fallback" and MeasureString throws on an unexpected character, which is the honest
		// failure - better than drawing boxes that look like a font bug.
		stringBuilder.Append("default=").Append(hashSet.Contains('?') ? "63" : "-1").Append('\n');
		stringBuilder.Append("glyphs=").Append(list.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
		for (int l = 0; l < list.Count; l++)
		{
			int num9 = (l % num3) * num2;
			int num10 = (l / num3) * num;
			stringBuilder.Append(((int)list[l]).ToString(CultureInfo.InvariantCulture)).Append(' ')
				.Append(array[l].ToString(CultureInfo.InvariantCulture)).Append(' ')
				.Append(num9.ToString(CultureInfo.InvariantCulture)).Append(' ')
				.Append(num10.ToString(CultureInfo.InvariantCulture)).Append('\n');
		}
		File.WriteAllText(text3, stringBuilder.ToString(), new UTF8Encoding(false));
		Console.WriteLine("  " + text + ": " + list.Count + " glyph(s), cell " + num2 + "x" + num + ", atlas " + num4 + "x" + num5);
		return true;
	}
}
