using System;
using System.IO;

namespace XnbToPng;

/// <summary>
/// Converts XNB texture payloads into top-down RGBA byte arrays.
///
/// Only the surface formats that actually appear in game content are implemented; anything
/// else is reported instead of being silently mangled.
/// </summary>
internal static class SurfaceDecoder
{
	public const int FormatColor = 0;

	public const int FormatBgr565 = 1;

	public const int FormatBgra5551 = 2;

	public const int FormatBgra4444 = 3;

	public const int FormatDxt1 = 4;

	public const int FormatDxt3 = 5;

	public const int FormatDxt5 = 6;

	public const int FormatAlpha8 = 12;

	public static string Name(int format)
	{
		switch (format)
		{
		case FormatColor:
			return "Color";
		case FormatBgr565:
			return "Bgr565";
		case FormatBgra5551:
			return "Bgra5551";
		case FormatBgra4444:
			return "Bgra4444";
		case FormatDxt1:
			return "Dxt1";
		case FormatDxt3:
			return "Dxt3";
		case FormatDxt5:
			return "Dxt5";
		case FormatAlpha8:
			return "Alpha8";
		default:
			return "format#" + format;
		}
	}

	public static bool IsSupported(int format)
	{
		return format == FormatColor || format == FormatBgr565 || format == FormatBgra5551 || format == FormatBgra4444 || format == FormatDxt1 || format == FormatDxt3 || format == FormatDxt5 || format == FormatAlpha8;
	}

	/// <summary>Decodes the largest mip into RGBA, top-down.</summary>
	public static byte[] Decode(int format, int width, int height, byte[] data)
	{
		return Decode(format, width, height, data, platform: 'w');
	}

	/// <summary>
	/// Decodes the largest mip into RGBA. Xbox 360 textures store 16-bit words
	/// big-endian, which is why the runtime runs them through <c>X360TexUtil</c> before
	/// uploading; the same swap is applied here so the PNG looks like the game does.
	/// </summary>
	public static byte[] Decode(int format, int width, int height, byte[] data, char platform)
	{
		byte[] array = ((platform == 'x') ? SwapForXbox(format, width, height, data) : data);
		switch (format)
		{
		case FormatColor:
			return DecodeColor(width, height, array);
		case FormatBgr565:
			return Decode16(width, height, array, DecodeBgr565);
		case FormatBgra5551:
			return Decode16(width, height, array, DecodeBgra5551);
		case FormatBgra4444:
			return Decode16(width, height, array, DecodeBgra4444);
		case FormatDxt1:
			return DecodeDxt(width, height, array, 8, DecodeDxt1Block);
		case FormatDxt3:
			return DecodeDxt(width, height, array, 16, DecodeDxt3Block);
		case FormatDxt5:
			return DecodeDxt(width, height, array, 16, DecodeDxt5Block);
		case FormatAlpha8:
			return DecodeAlpha8(width, height, array);
		default:
			throw new NotSupportedException("surface format " + Name(format) + " is not supported");
		}
	}

	/// <summary>
	/// Byte-swaps Xbox 360 texture data into the little-endian layout the decoders expect.
	/// Color data is swapped per 32-bit pixel; DXT and 16-bit formats per 16-bit word.
	/// </summary>
	private static byte[] SwapForXbox(int format, int width, int height, byte[] data)
	{
		byte[] array = new byte[data.Length];
		switch (format)
		{
		case FormatColor:
			// A pixel is one 32-bit word: reversing the word is the whole swap.
			for (int i = 0; i + 4 <= data.Length; i += 4)
			{
				array[i] = data[i + 3];
				array[i + 1] = data[i + 2];
				array[i + 2] = data[i + 1];
				array[i + 3] = data[i];
			}
			return array;
		case FormatBgr565:
		case FormatBgra5551:
		case FormatBgra4444:
		case FormatDxt1:
		case FormatDxt3:
		case FormatDxt5:
			// A sequence of 16-bit words: 565 pixels or DXT block words.
			for (int j = 0; j + 2 <= data.Length; j += 2)
			{
				array[j] = data[j + 1];
				array[j + 1] = data[j];
			}
			return array;
		default:
			return data;
		}
	}

	private static byte[] DecodeColor(int width, int height, byte[] data)
	{
		int num = width * height * 4;
		if (data.Length < num)
		{
			throw new InvalidDataException("texture data is " + data.Length + " bytes, expected " + num);
		}
		byte[] array = new byte[num];
		Buffer.BlockCopy(data, 0, array, 0, num);
		return array;
	}

	private static byte[] DecodeAlpha8(int width, int height, byte[] data)
	{
		int num = width * height;
		if (data.Length < num)
		{
			throw new InvalidDataException("texture data is too short");
		}
		byte[] array = new byte[num * 4];
		for (int i = 0; i < num; i++)
		{
			byte b = data[i];
			array[i * 4] = b;
			array[i * 4 + 1] = b;
			array[i * 4 + 2] = b;
			array[i * 4 + 3] = 255;
		}
		return array;
	}

	private delegate void Pixel16(ushort value, byte[] destination, int index);

	private static byte[] Decode16(int width, int height, byte[] data, Pixel16 convert)
	{
		int num = width * height;
		if (data.Length < num * 2)
		{
			throw new InvalidDataException("texture data is too short");
		}
		byte[] array = new byte[num * 4];
		for (int i = 0; i < num; i++)
		{
			convert(BitConverter.ToUInt16(data, i * 2), array, i * 4);
		}
		return array;
	}

	private static void DecodeBgr565(ushort value, byte[] destination, int index)
	{
		int num = (value >> 11) & 0x1F;
		int num2 = (value >> 5) & 0x3F;
		int num3 = value & 0x1F;
		destination[index] = (byte)((num << 3) | (num >> 2));
		destination[index + 1] = (byte)((num2 << 2) | (num2 >> 4));
		destination[index + 2] = (byte)((num3 << 3) | (num3 >> 2));
		destination[index + 3] = 255;
	}

	private static void DecodeBgra5551(ushort value, byte[] destination, int index)
	{
		int num = (value >> 10) & 0x1F;
		int num2 = (value >> 5) & 0x1F;
		int num3 = value & 0x1F;
		destination[index] = (byte)((num3 << 3) | (num3 >> 2));
		destination[index + 1] = (byte)((num2 << 3) | (num2 >> 2));
		destination[index + 2] = (byte)((num << 3) | (num >> 2));
		destination[index + 3] = (byte)(((value >> 15) & 1) * 255);
	}

	private static void DecodeBgra4444(ushort value, byte[] destination, int index)
	{
		int num = value & 0xF;
		int num2 = (value >> 4) & 0xF;
		int num3 = (value >> 8) & 0xF;
		int num4 = (value >> 12) & 0xF;
		destination[index] = (byte)((num << 4) | num);
		destination[index + 1] = (byte)((num2 << 4) | num2);
		destination[index + 2] = (byte)((num3 << 4) | num3);
		destination[index + 3] = (byte)((num4 << 4) | num4);
	}

	private delegate void BlockDecoder(byte[] source, int offset, byte[] destination);

	/// <summary>Decodes a 4x4-block texture (DXT) into a full RGBA image.</summary>
	private static byte[] DecodeDxt(int width, int height, byte[] data, int blockSize, BlockDecoder decode)
	{
		int num = (width + 3) / 4;
		int num2 = (height + 3) / 4;
		if (data.Length < num * num2 * blockSize)
		{
			throw new InvalidDataException("DXT data is too short: " + data.Length + " bytes for " + width + "x" + height);
		}
		byte[] array = new byte[width * height * 4];
		byte[] array2 = new byte[64];
		int num3 = 0;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				decode(data, num3, array2);
				num3 += blockSize;
				// A texture whose size is not a multiple of 4 keeps only the pixels that
				// fall inside the image.
				for (int k = 0; k < 4; k++)
				{
					int num4 = i * 4 + k;
					if (num4 >= height)
					{
						break;
					}
					for (int l = 0; l < 4; l++)
					{
						int num5 = j * 4 + l;
						if (num5 >= width)
						{
							break;
						}
						int num6 = (num4 * width + num5) * 4;
						int num7 = (k * 4 + l) * 4;
						array[num6] = array2[num7];
						array[num6 + 1] = array2[num7 + 1];
						array[num6 + 2] = array2[num7 + 2];
						array[num6 + 3] = array2[num7 + 3];
					}
				}
			}
		}
		return array;
	}

	private static void DecodeDxt1Block(byte[] source, int offset, byte[] destination)
	{
		DecodeColorBlock(source, offset, destination, allowTransparent: true);
	}

	private static void DecodeDxt3Block(byte[] source, int offset, byte[] destination)
	{
		// 8 bytes of explicit 4-bit alpha, then a colour block. DXT3 always has alpha, so
		// the colour block never falls back to the 3-colour/transparent mode.
		byte[] array = new byte[64];
		DecodeColorBlock(source, offset + 8, array, allowTransparent: false);
		for (int i = 0; i < 16; i++)
		{
			int num = (source[offset + i / 2] >> ((i & 1) * 4)) & 0xF;
			destination[i * 4] = array[i * 4];
			destination[i * 4 + 1] = array[i * 4 + 1];
			destination[i * 4 + 2] = array[i * 4 + 2];
			destination[i * 4 + 3] = (byte)((num << 4) | num);
		}
	}

	private static void DecodeDxt5Block(byte[] source, int offset, byte[] destination)
	{
		// 8 bytes of interpolated alpha, then a colour block; alpha comes from the first
		// part and must survive the colour decode.
		byte[] array = new byte[64];
		DecodeColorBlock(source, offset + 8, array, allowTransparent: false);
		int num = source[offset];
		int num2 = source[offset + 1];
		byte[] array2 = new byte[8];
		array2[0] = (byte)num;
		array2[1] = (byte)num2;
		if (num > num2)
		{
			for (int i = 0; i < 6; i++)
			{
				array2[i + 2] = (byte)(((6 - i) * num + (1 + i) * num2) / 7);
			}
		}
		else
		{
			for (int j = 0; j < 4; j++)
			{
				array2[j + 2] = (byte)(((4 - j) * num + (1 + j) * num2) / 5);
			}
			array2[6] = 0;
			array2[7] = 255;
		}
		long num3 = 0L;
		for (int k = 0; k < 6; k++)
		{
			num3 |= (long)source[offset + 2 + k] << (8 * k);
		}
		for (int l = 0; l < 16; l++)
		{
			int num4 = (int)((num3 >> (l * 3)) & 7L);
			destination[l * 4] = array[l * 4];
			destination[l * 4 + 1] = array[l * 4 + 1];
			destination[l * 4 + 2] = array[l * 4 + 2];
			destination[l * 4 + 3] = array2[num4];
		}
	}

	/// <summary>
	/// Decodes the 8-byte colour part shared by DXT1/3/5 into 16 RGBA pixels.
	/// <paramref name="allowTransparent"/> enables the DXT1 3-colour mode used when
	/// <c>c0 &lt;= c1</c>; DXT3/5 must pass false.
	/// </summary>
	private static void DecodeColorBlock(byte[] source, int offset, byte[] destination, bool allowTransparent)
	{
		int num = source[offset] | (source[offset + 1] << 8);
		int num2 = source[offset + 2] | (source[offset + 3] << 8);
		byte[][] array = new byte[4][];
		for (int i = 0; i < 4; i++)
		{
			array[i] = new byte[4];
		}
		Expand565(num, array[0]);
		Expand565(num2, array[1]);
		if (allowTransparent && num <= num2)
		{
			for (int j = 0; j < 3; j++)
			{
				array[2][j] = (byte)((array[0][j] + array[1][j]) / 2);
				array[3][j] = 0;
			}
			array[2][3] = 255;
			array[3][3] = 0;
		}
		else
		{
			for (int k = 0; k < 3; k++)
			{
				array[2][k] = (byte)((2 * array[0][k] + array[1][k]) / 3);
				array[3][k] = (byte)((array[0][k] + 2 * array[1][k]) / 3);
			}
			array[2][3] = 255;
			array[3][3] = 255;
		}
		int num3 = source[offset + 4] | (source[offset + 5] << 8) | (source[offset + 6] << 16) | (source[offset + 7] << 24);
		for (int l = 0; l < 16; l++)
		{
			int num4 = (num3 >> (l * 2)) & 3;
			byte[] array2 = array[num4];
			destination[l * 4] = array2[0];
			destination[l * 4 + 1] = array2[1];
			destination[l * 4 + 2] = array2[2];
			destination[l * 4 + 3] = array2[3];
		}
	}

	/// <summary>Expands RGB565 into RGBA at the start of <paramref name="rgba"/>.</summary>
	private static void Expand565(int value, byte[] rgba)
	{
		int num = (value >> 11) & 0x1F;
		int num2 = (value >> 5) & 0x3F;
		int num3 = value & 0x1F;
		rgba[0] = (byte)((num << 3) | (num >> 2));
		rgba[1] = (byte)((num2 << 2) | (num2 >> 4));
		rgba[2] = (byte)((num3 << 3) | (num3 >> 2));
		rgba[3] = 255;
	}
}
