using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XnbToPng;

/// <summary>
/// Extracts textures from a Content folder.
///
/// <code>
/// xnb2png &lt;contentDir&gt; [outDir]
/// xnb2png --info &lt;contentDir&gt;
/// </code>
///
/// The output mirrors the input tree, so <c>Content/Images/Tiles_0.xnb</c> becomes
/// <c>out/Images/Tiles_0.png</c>. Non-texture assets and the Xbox 360 variant are reported
/// and skipped: this is a texture extractor, not a decompiler.
/// </summary>
internal static class Program
{
	private static int Main(string[] args)
	{
		if (args.Length == 0)
		{
			Console.WriteLine("usage: xnb2png <contentDir> [outDir]");
			Console.WriteLine("       xnb2png --info <contentDir>");
			Console.WriteLine("       xnb2png --selftest");
			return 2;
		}
		if (args[0] == "--selftest")
		{
			return XnbToPng.SelfTest.Run();
		}
		bool infoOnly = args[0] == "--info";
		int num = infoOnly ? 1 : 0;
		if (args.Length <= num)
		{
			Console.WriteLine("error: missing the content directory");
			return 2;
		}
		string text = args[num];
		if (!Directory.Exists(text))
		{
			Console.WriteLine("error: '" + text + "' is not a directory");
			return 2;
		}
		string text2 = (args.Length > num + 1) ? args[num + 1] : Path.Combine(Directory.GetCurrentDirectory(), "xnb-extracted");
		string[] array = Directory.GetFiles(text, "*.xnb", SearchOption.AllDirectories);
		Array.Sort(array, StringComparer.OrdinalIgnoreCase);
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		List<string> list = new List<string>();
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		for (int i = 0; i < array.Length; i++)
		{
			string text3 = array[i];
			string text4 = Path.Combine(text2, Path.ChangeExtension(Relative(text, text3), ".png"));
			try
			{
				XnbFile xnbFile = XnbFile.Read(text3);
				if (!xnbFile.IsTexture)
				{
					num3++;
					list.Add("skip (not a texture: " + Trim(xnbFile.TypeName) + "): " + Relative(text, text3));
					continue;
				}
				int num5;
				dictionary.TryGetValue(xnbFile.Format, out num5);
				dictionary[xnbFile.Format] = num5 + 1;
				if (infoOnly)
				{
					Console.WriteLine(Relative(text, text3) + ": " + xnbFile.Width + "x" + xnbFile.Height + " " + SurfaceDecoder.Name(xnbFile.Format) + " [" + xnbFile.Platform + "] mips=" + xnbFile.MipCount);
					num2++;
					continue;
				}
				if (!SurfaceDecoder.IsSupported(xnbFile.Format))
				{
					num4++;
					list.Add("unsupported " + SurfaceDecoder.Name(xnbFile.Format) + ": " + Relative(text, text3));
					continue;
				}
				byte[] array2 = SurfaceDecoder.Decode(xnbFile.Format, xnbFile.Width, xnbFile.Height, xnbFile.Data, xnbFile.Platform);
				PngWriter.Write(text4, array2, xnbFile.Width, xnbFile.Height);
				num2++;
			}
			catch (Exception ex)
			{
				num4++;
				list.Add("failed (" + ex.GetType().Name + ": " + ex.Message + "): " + Relative(text, text3));
			}
		}
		Console.WriteLine();
		Console.WriteLine("converted : " + num2);
		Console.WriteLine("skipped   : " + num3);
		Console.WriteLine("failed    : " + num4);
		if (dictionary.Count > 0)
		{
			StringBuilder stringBuilder = new StringBuilder("formats   : ");
			foreach (KeyValuePair<int, int> item in dictionary)
			{
				stringBuilder.Append(SurfaceDecoder.Name(item.Key)).Append('=').Append(item.Value).Append(' ');
			}
			Console.WriteLine(stringBuilder.ToString().TrimEnd());
		}
		if (!infoOnly)
		{
			Console.WriteLine("output    : " + text2);
		}
		if (list.Count > 0)
		{
			Console.WriteLine();
			// Keep the tail of a long list: the summary above already gives the counts.
			int num6 = Math.Max(0, list.Count - 20);
			if (num6 > 0)
			{
				Console.WriteLine("... " + num6 + " more ...");
			}
			for (int j = num6; j < list.Count; j++)
			{
				Console.WriteLine("  " + list[j]);
			}
		}
		return (num4 > 0) ? 1 : 0;
	}

	private static string Relative(string root, string path)
	{
		string text = Path.GetFullPath(root);
		if (!text.EndsWith(Path.DirectorySeparatorChar.ToString()))
		{
			text += Path.DirectorySeparatorChar;
		}
		string text2 = Path.GetFullPath(path);
		if (text2.StartsWith(text, StringComparison.OrdinalIgnoreCase))
		{
			return text2.Substring(text.Length);
		}
		return Path.GetFileName(path);
	}

	private static string Trim(string typeName)
	{
		if (string.IsNullOrEmpty(typeName))
		{
			return "unknown";
		}
		int num = typeName.LastIndexOf('.');
		return (num >= 0) ? typeName.Substring(num + 1) : typeName;
	}
}
