using System;
using System.IO;

namespace XnbToPng;

/// <summary>
/// Offline checks for the decoder. The game content happens to contain only uncompressed
/// Color textures, so the DXT/16-bit paths would otherwise ship completely untested.
/// Run with <c>xnb2png --selftest</c>.
/// </summary>
internal static class SelfTest
{
	public static int Run()
	{
		int num = 0;
		int num2 = 0;
		Check("bgr565 red", ref num, ref num2, delegate
		{
			byte[] array = SurfaceDecoder.Decode(SurfaceDecoder.FormatBgr565, 1, 1, new byte[2] { 0, 248 });
			// 0xF800 = 11111 000000 00000: pure red.
			if (array[0] != 255 || array[1] != 0 || array[2] != 0 || array[3] != 255)
			{
				return "Bgr565 red decoded as " + Describe(array);
			}
			return null;
		});
		Check("bgra4444 half alpha", ref num, ref num2, delegate
		{
			// B=0, G=0, R=0, A=8 -> alpha 0x88, RGB 0.
			byte[] array = SurfaceDecoder.Decode(SurfaceDecoder.FormatBgra4444, 1, 1, new byte[2] { 0, 128 });
			if (array[3] != 136 || array[0] != 0)
			{
				return "Bgra4444 decoded as " + Describe(array);
			}
			return null;
		});
		Check("alpha8 expands to greyscale", ref num, ref num2, delegate
		{
			byte[] array = SurfaceDecoder.Decode(SurfaceDecoder.FormatAlpha8, 2, 1, new byte[2] { 16, 240 });
			if (array[0] != 16 || array[3] != 255 || array[4] != 240)
			{
				return "Alpha8 decoded as " + Describe(array);
			}
			return null;
		});
		Check("dxt1 solid red", ref num, ref num2, delegate
		{
			// c0 = 0xF800 (red), c1 = 0x0000, indices all 0 -> every pixel red.
			byte[] array = SurfaceDecoder.Decode(SurfaceDecoder.FormatDxt1, 4, 4, Dxt1Block(0xF800, 0, 0u));
			if (array[0] != 255 || array[1] != 0 || array[2] != 0 || array[3] != 255)
			{
				return "DXT1 red decoded as " + Describe(array);
			}
			for (int i = 0; i < 16; i++)
			{
				if (array[i * 4] != 255 || array[i * 4 + 3] != 255)
				{
					return "DXT1 block is not uniform at pixel " + i;
				}
			}
			return null;
		});
		Check("dxt1 interpolation", ref num, ref num2, delegate
		{
			// Two-colour block, index pattern 0,1,2,3 repeated.
			byte[] array = SurfaceDecoder.Decode(SurfaceDecoder.FormatDxt1, 4, 4, Dxt1Block(0xF800, 0x001F, 0xE4u));
			// c0=red, c1=blue: c2 = 2/3 red, c3 = 1/3 red.
			if (array[0] != 255 || array[2] != 0)
			{
				return "DXT1 first pixel is not pure red: " + Describe(array, 0);
			}
			if (array[4] != 0 || array[6] != 255)
			{
				return "DXT1 second pixel is not pure blue: " + Describe(array, 4);
			}
			if (array[8] != 170)
			{
				return "DXT1 2/3 blend is " + array[8] + ", expected 170";
			}
			if (array[12] != 85)
			{
				return "DXT1 1/3 blend is " + array[12] + ", expected 85";
			}
			return null;
		});
		Check("dxt1 transparent mode", ref num, ref num2, delegate
		{
			// c0 < c1 selects the three-colour + transparent mode; index 3 is transparent.
			byte[] array = SurfaceDecoder.Decode(SurfaceDecoder.FormatDxt1, 4, 4, Dxt1Block(0x0000, 0xF800, 0xE4u));
			if (array[3] != 255 || array[7] != 255)
			{
				return "DXT1 transparent mode lost the opaque pixels";
			}
			if (array[12 + 3] != 0)
			{
				return "DXT1 transparent mode produced alpha " + array[15];
			}
			return null;
		});
		Check("dxt3 explicit alpha", ref num, ref num2, delegate
		{
			byte[] data = new byte[16];
			// First pixel alpha nibble = 0xF, second = 0x0.
			data[0] = 0x0F;
			// Colour block: solid green (0x07E0), both endpoints equal.
			data[8] = 0xE0;
			data[9] = 0x07;
			data[10] = 0xE0;
			data[11] = 0x07;
			byte[] array = SurfaceDecoder.Decode(SurfaceDecoder.FormatDxt3, 4, 4, data);
			if (array[3] != 255 || array[7] != 0)
			{
				return "DXT3 alpha decoded as " + array[3] + "/" + array[7];
			}
			if (array[1] != 255 || array[0] != 0)
			{
				return "DXT3 colour decoded as " + Describe(array, 0);
			}
			return null;
		});
		Check("dxt5 gradient alpha", ref num, ref num2, delegate
		{
			byte[] data = new byte[16];
			// a0 = 255, a1 = 0 -> interpolated alpha 218,182,146,109,73,36 (integer division).
			data[0] = 255;
			data[1] = 0;
			// First pixel index 0 -> 255, second pixel index 1 -> 0, third index 2 -> 218.
			// Three 3-bit indices packed little-endian: 0 | 1&lt;&lt;3 | 2&lt;&lt;6 = 0x88.
			data[2] = 0x88;
			// Colour: solid white.
			data[8] = 0xFF;
			data[9] = 0xFF;
			data[10] = 0xFF;
			data[11] = 0xFF;
			byte[] array = SurfaceDecoder.Decode(SurfaceDecoder.FormatDxt5, 4, 4, data);
			if (array[3] != 255 || array[7] != 0 || array[11] != 218)
			{
				return "DXT5 alpha decoded as " + array[3] + "/" + array[7] + "/" + array[11];
			}
			if (array[0] != 255 || array[1] != 255 || array[2] != 255)
			{
				return "DXT5 colour decoded as " + Describe(array, 0);
			}
			return null;
		});
		Check("dxt rejects short buffers", ref num, ref num2, delegate
		{
			try
			{
				SurfaceDecoder.Decode(SurfaceDecoder.FormatDxt1, 8, 8, new byte[8]);
				return "a 8x8 DXT1 texture was decoded from one block";
			}
			catch (InvalidDataException)
			{
				return null;
			}
		});
		Console.WriteLine();
		Console.WriteLine("Passed: " + num + "  Failed: " + num2);
		return (num2 == 0) ? 0 : 1;
	}

	/// <summary>Builds an 8-byte DXT1 colour block whose 2-bit indices repeat 0,1,2,3.</summary>
	private static byte[] Dxt1Block(ushort c0, ushort c1, uint indices)
	{
		byte[] array = new byte[8];
		BitConverter.GetBytes(c0).CopyTo(array, 0);
		BitConverter.GetBytes(c1).CopyTo(array, 2);
		BitConverter.GetBytes(indices).CopyTo(array, 4);
		return array;
	}

	private static string Describe(byte[] rgba)
	{
		return Describe(rgba, 0);
	}

	private static string Describe(byte[] rgba, int index)
	{
		return "(" + rgba[index] + "," + rgba[index + 1] + "," + rgba[index + 2] + "," + rgba[index + 3] + ")";
	}

	private static void Check(string name, ref int passed, ref int failed, Func<string> body)
	{
		string text;
		try
		{
			text = body();
		}
		catch (Exception ex)
		{
			text = ex.GetType().Name + ": " + ex.Message;
		}
		if (text == null)
		{
			passed++;
			Console.WriteLine("  ok   " + name);
		}
		else
		{
			failed++;
			Console.WriteLine("  FAIL " + name + " - " + text);
		}
	}
}
