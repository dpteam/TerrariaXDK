using System;
using System.IO;
using System.IO.Compression;

namespace Terraria.Textures;

/// <summary>
/// A PNG reader, because the game can only load <c>.xnb</c>.
///
/// Why hand-rolled: the obvious alternative is <c>System.Drawing</c>, which is GDI+ - Windows
/// only, and this project is trying to stop depending on that (see <c>docs/PORTABILITY.md</c>).
/// It also cannot be called from a headless test without a desktop session.
///
/// What this supports: every colour type (grey, RGB, palette, grey+alpha, RGBA), bit depths
/// 1/2/4/8/16, all five filter types, multiple <c>IDAT</c> chunks, and <c>tRNS</c>
/// transparency. What it refuses, loudly, is Adam7 interlacing: a quarter of the code for a
/// feature no Terraria texture uses, and silently mis-decoding an interlaced file would look
/// like corrupted art rather than an unsupported format.
///
/// The result is always 8-bit RGBA (<see cref="ImageData"/>), so nothing upstream has to care
/// what the file actually was.
/// </summary>
internal static class PngDecoder
{
	private static readonly byte[] Signature = new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 };

	/// <summary>Largest image accepted, so a corrupt header cannot ask for a 4 GB allocation.</summary>
	public const int MaxDimension = 16384;

	/// <summary>
	/// Largest total pixel count accepted. A texture bigger than this is not something the game
	/// would draw, and the bound keeps every size computation below in <c>int</c> range - which
	/// is what turns "corrupt header" into a sentence instead of an arithmetic overflow.
	/// </summary>
	public const long MaxPixels = 16L * 1024L * 1024L;

	/// <summary>True when the bytes start with the PNG signature. Cheap, for routing.</summary>
	public static bool IsPng(byte[] data)
	{
		if (data == null || data.Length < Signature.Length)
		{
			return false;
		}
		for (int i = 0; i < Signature.Length; i++)
		{
			if (data[i] != Signature[i])
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Decodes a PNG. Returns false with a reason that names the chunk or the field, because
	/// "unsupported PNG" is useless when someone's modded texture does not show up.
	/// </summary>
	public static bool TryDecode(byte[] data, out ImageData image, out string error)
	{
		image = null;
		error = null;
		if (!IsPng(data))
		{
			error = "not a PNG file";
			return false;
		}
		int width = 0;
		int height = 0;
		int bitDepth = 0;
		int colorType = -1;
		int interlace = 0;
		byte[] palette = null;
		byte[] transparency = null;
		byte[] compressed = null;
		int num = Signature.Length;
		bool flag = false;
		try
		{
			while (num + 8 <= data.Length)
			{
				int num2 = ReadInt32(data, num);
				string text = Ascii(data, num + 4, 4);
				int num3 = num + 8;
				if (num2 < 0 || num3 + num2 + 4 > data.Length)
				{
					error = "chunk '" + text + "' runs past the end of the file";
					return false;
				}
				if (text == "IHDR")
				{
					if (num2 < 13)
					{
						error = "IHDR is only " + num2 + " bytes";
						return false;
					}
					width = ReadInt32(data, num3);
					height = ReadInt32(data, num3 + 4);
					bitDepth = data[num3 + 8];
					colorType = data[num3 + 9];
					int num4 = data[num3 + 10];
					int num5 = data[num3 + 11];
					interlace = data[num3 + 12];
					if (num4 != 0 || num5 != 0)
					{
						error = "unknown compression/filter method (" + num4 + "/" + num5 + ")";
						return false;
					}
					if (interlace != 0)
					{
						error = "Adam7 interlacing is not supported";
						return false;
					}
					if (width <= 0 || height <= 0 || width > MaxDimension || height > MaxDimension)
					{
						error = "image size " + width + "x" + height + " is out of range";
						return false;
					}
					if ((long)width * (long)height > MaxPixels)
					{
						error = "image size " + width + "x" + height + " is larger than the " + (MaxPixels / 1024 / 1024) + " megapixel limit";
						return false;
					}
				}
				else if (text == "PLTE")
				{
					if (num2 % 3 != 0)
					{
						error = "PLTE is not a multiple of 3 bytes";
						return false;
					}
					palette = Slice(data, num3, num2);
				}
				else if (text == "tRNS")
				{
					transparency = Slice(data, num3, num2);
				}
				else if (text == "IDAT")
				{
					// IDAT may be split arbitrarily; the zlib stream is the concatenation.
					if (compressed == null)
					{
						compressed = Slice(data, num3, num2);
					}
					else
					{
						byte[] array = new byte[compressed.Length + num2];
						Buffer.BlockCopy(compressed, 0, array, 0, compressed.Length);
						Buffer.BlockCopy(data, num3, array, compressed.Length, num2);
						compressed = array;
					}
				}
				else if (text == "IEND")
				{
					flag = true;
					break;
				}
				num = num3 + num2 + 4;
			}
			if (!flag)
			{
				error = "the file ends without IEND (truncated)";
				return false;
			}
			if (colorType < 0)
			{
				error = "no IHDR chunk";
				return false;
			}
			if (compressed == null || compressed.Length == 0)
			{
				error = "no image data";
				return false;
			}
			int num6 = Channels(colorType);
			if (num6 == 0 || !ValidDepth(colorType, bitDepth))
			{
				error = "unsupported colour type " + colorType + " at bit depth " + bitDepth;
				return false;
			}
			if (colorType == 3 && palette == null)
			{
				error = "palette image without a PLTE chunk";
				return false;
			}
			int num7 = num6 * bitDepth;
			int num8 = (num7 + 7) / 8;
			// Row length in bytes: sub-byte depths pack several pixels into one byte, so this
			// is ceil(width * bitsPerPixel / 8) and not width * bytesPerPixel - with a 1-bit
			// palette image the latter asks for four bytes per row where the file has one.
			int num9 = (width * num7 + 7) / 8;
			int num10 = num9 + 1;
			byte[] array2 = Inflate(compressed, height * num10);
			if (array2 == null)
			{
				error = "the compressed image data is corrupt or truncated";
				return false;
			}
			if (array2.Length < height * num10)
			{
				error = "compressed image data is corrupt or truncated: expected " + (height * num10) + " bytes, got " + array2.Length;
				return false;
			}
			byte[] array3 = Unfilter(array2, height, num9, num8);
			// For a palette image the tRNS chunk is the alpha table, one byte per index; for the
			// other colour types it means something else and is handled inside ToRgba.
			byte[] array4 = (colorType == 3) ? transparency : null;
			image = ToRgba(array3, width, height, bitDepth, colorType, palette, array4, transparency);
			return true;
		}
		catch (Exception ex)
		{
			error = ex.GetType().Name + ": " + ex.Message;
			return false;
		}
	}

	/// <summary>Channels per pixel for a colour type, or 0 when the type is unknown.</summary>
	private static int Channels(int colorType)
	{
		switch (colorType)
		{
		case 0:
			return 1;
		case 2:
			return 3;
		case 3:
			return 1;
		case 4:
			return 2;
		case 6:
			return 4;
		default:
			return 0;
		}
	}

	private static bool ValidDepth(int colorType, int bitDepth)
	{
		switch (colorType)
		{
		case 0:
			return bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8 || bitDepth == 16;
		case 2:
		case 4:
		case 6:
			return bitDepth == 8 || bitDepth == 16;
		case 3:
			return bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8;
		default:
			return false;
		}
	}

	/// <summary>
	/// Reverses the per-row filters. Each row is prefixed by the filter type it used, and
	/// filters 2-4 refer to the row above and/or the pixel to the left - so this must run top to
	/// bottom, left to right, in place.
	/// </summary>
	private static byte[] Unfilter(byte[] raw, int height, int rowBytes, int bpp)
	{
		byte[] array = new byte[height * rowBytes];
		int num = rowBytes + 1;
		for (int i = 0; i < height; i++)
		{
			int num2 = i * num;
			int num3 = raw[num2];
			int num4 = i * rowBytes;
			int num5 = (i - 1) * rowBytes;
			for (int j = 0; j < rowBytes; j++)
			{
				int num6 = raw[num2 + 1 + j];
				int num7 = (j >= bpp) ? array[num4 + j - bpp] : 0;
				int num8 = (i > 0) ? array[num5 + j] : 0;
				int num9 = (i > 0 && j >= bpp) ? array[num5 + j - bpp] : 0;
				int num10;
				switch (num3)
				{
				case 0:
					num10 = 0;
					break;
				case 1:
					num10 = num7;
					break;
				case 2:
					num10 = num8;
					break;
				case 3:
					num10 = (num7 + num8) >> 1;
					break;
				case 4:
					num10 = Paeth(num7, num8, num9);
					break;
				default:
					// An unknown filter cannot be reconstructed. Treating it as 0 would produce
					// plausible-looking garbage, which is worse than a named error.
					throw new InvalidDataException("row " + i + " uses filter type " + num3);
				}
				array[num4 + j] = (byte)((num6 + num10) & 0xFF);
			}
		}
		return array;
	}

	/// <summary>The PNG Paeth predictor: whichever neighbour is closest to a + b - c.</summary>
	private static int Paeth(int a, int b, int c)
	{
		int num = a + b - c;
		int num2 = Math.Abs(num - a);
		int num3 = Math.Abs(num - b);
		int num4 = Math.Abs(num - c);
		if (num2 <= num3 && num2 <= num4)
		{
			return a;
		}
		if (num3 <= num4)
		{
			return b;
		}
		return c;
	}

	private static ImageData ToRgba(byte[] rows, int width, int height, int bitDepth, int colorType, byte[] palette, byte[] paletteAlpha, byte[] transparency)
	{
		ImageData imageData = new ImageData(width, height);
		int channels = Channels(colorType);
		int num = channels * bitDepth;
		int num2 = (num + 7) / 8;
		for (int i = 0; i < height; i++)
		{
			int num3 = i * width * num2;
			for (int j = 0; j < width; j++)
			{
				// num3 is already the start of this row. Adding j * num2 here as well would
				// double-count the pixel offset, because ReadSample/ReadBits take x separately.
				int num4 = num3;
				if (colorType == 3)
				{
					int num5 = ReadBits(rows, num4, bitDepth, j);
					if (num5 * 3 + 2 >= palette.Length)
					{
						// An index past the palette: magenta is the traditional "missing texture"
						// colour, and it cannot be mistaken for real art.
						imageData.Set(j, i, 255, 0, 255, 255);
						continue;
					}
					byte a = (paletteAlpha != null && num5 < paletteAlpha.Length) ? paletteAlpha[num5] : (byte)255;
					imageData.Set(j, i, palette[num5 * 3], palette[num5 * 3 + 1], palette[num5 * 3 + 2], a);
					continue;
				}
				if (colorType == 0)
				{
					int num6 = ReadSample(rows, num4, bitDepth, j, 0, 1);
					byte b = ToByte(num6, bitDepth);
					byte a2 = (byte)255;
					if (transparency != null && transparency.Length >= 2)
					{
						// tRNS for grey is one 16-bit sample meaning "this value is transparent".
						if (num6 == ((transparency[0] << 8) | transparency[1]))
						{
							a2 = 0;
						}
					}
					imageData.Set(j, i, b, b, b, a2);
					continue;
				}
				if (colorType == 4)
				{
					int num6 = ReadSample(rows, num4, bitDepth, j, 0, 2);
					byte b = ToByte(num6, bitDepth);
					imageData.Set(j, i, b, b, b, ToByte(ReadSample(rows, num4, bitDepth, j, 1, 2), bitDepth));
					continue;
				}
				byte r = ToByte(ReadSample(rows, num4, bitDepth, j, 0, channels), bitDepth);
				byte g = ToByte(ReadSample(rows, num4, bitDepth, j, 1, channels), bitDepth);
				byte blue = ToByte(ReadSample(rows, num4, bitDepth, j, 2, channels), bitDepth);
				byte alpha = (byte)255;
				if (colorType == 6)
				{
					alpha = ToByte(ReadSample(rows, num4, bitDepth, j, 3, channels), bitDepth);
				}
				else if (colorType == 2 && transparency != null && transparency.Length >= 6)
				{
					// tRNS for RGB is one 16-bit sample per channel: "this colour is transparent".
					int num8 = (transparency[0] << 8) | transparency[1];
					int num9 = (transparency[2] << 8) | transparency[3];
					int num10 = (transparency[4] << 8) | transparency[5];
					if (ReadSample(rows, num4, bitDepth, j, 0, channels) == num8
						&& ReadSample(rows, num4, bitDepth, j, 1, channels) == num9
						&& ReadSample(rows, num4, bitDepth, j, 2, channels) == num10)
					{
						alpha = 0;
					}
				}
				imageData.Set(j, i, r, g, blue, alpha);
			}
		}
		return imageData;
	}

	/// <summary>
	/// One sample of one pixel. Handles sub-byte depths, where several pixels share a byte and
	/// the first pixel is in the high bits (not the low ones - getting this backwards shifts
	/// every pixel in a 1/2/4-bit image).
	/// </summary>
	private static int ReadSample(byte[] rows, int rowStart, int bitDepth, int x, int channel, int channels)
	{
		if (bitDepth == 8)
		{
			return rows[rowStart + x * channels + channel];
		}
		if (bitDepth == 16)
		{
			int num = rowStart + (x * channels + channel) * 2;
			return (rows[num] << 8) | rows[num + 1];
		}
		return ReadBits(rows, rowStart, bitDepth, x);
	}

	/// <summary>A packed sample for depths below 8 bits per pixel.</summary>
	private static int ReadBits(byte[] rows, int rowStart, int bitDepth, int x)
	{
		int num = x * bitDepth;
		int num2 = rowStart + (num >> 3);
		int num3 = num & 7;
		int num4 = 8 - bitDepth;
		return (rows[num2] >> (num4 - num3)) & ((1 << bitDepth) - 1);
	}

	/// <summary>Scales a sample to 8 bits: 16-bit keeps the high byte, 1/2/4-bit stretch.</summary>
	private static byte ToByte(int value, int bitDepth)
	{
		switch (bitDepth)
		{
		case 8:
			return (byte)value;
		case 16:
			return (byte)(value >> 8);
		case 4:
			return (byte)(value * 17);
		case 2:
			return (byte)(value * 85);
		default:
			return (byte)(value * 255);
		}
	}

	/// <summary>
	/// zlib-wrapped deflate to raw bytes. The two-byte header is checked (one that fails the
	/// checksum usually means the data is not zlib at all, so the reader falls back to bare
	/// deflate), and the four-byte Adler-32 trailer is skipped: a bad checksum on a texture is
	/// not worth refusing the image over, and the expected-length check catches real truncation.
	/// </summary>
	private static byte[] Inflate(byte[] compressed, int expectedAtLeast)
	{
		int num = 0;
		bool flag = compressed.Length >= 2 && ((compressed[0] << 8) | compressed[1]) % 31 == 0 && (compressed[0] & 0x0F) == 8;
		if (flag)
		{
			num = 2;
		}
		int num2 = compressed.Length - num;
		if (flag)
		{
			num2 -= 4;
		}
		if (num2 <= 0)
		{
			return null;
		}
		try
		{
			using (MemoryStream memoryStream = new MemoryStream(compressed, num, num2))
			using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
			using (MemoryStream memoryStream2 = new MemoryStream(expectedAtLeast))
			{
				byte[] array = new byte[8192];
				int num3;
				while ((num3 = deflateStream.Read(array, 0, array.Length)) > 0)
				{
					memoryStream2.Write(array, 0, num3);
					// A hostile or broken file must not grow without limit.
					if (memoryStream2.Length > (long)expectedAtLeast + 1024L * 1024L)
					{
						return null;
					}
				}
				return memoryStream2.ToArray();
			}
		}
		catch (Exception)
		{
			// A raw exception from the inflater reaches the player as a localised .NET message
			// ("invalid block type"), which explains nothing. Null here becomes "the compressed
			// image data is corrupt or truncated" - a sentence about the file, not about the
			// runtime.
			return null;
		}
	}

	private static int ReadInt32(byte[] data, int offset)
	{
		return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
	}

	private static byte[] Slice(byte[] data, int offset, int count)
	{
		byte[] array = new byte[count];
		Buffer.BlockCopy(data, offset, array, 0, count);
		return array;
	}

	private static string Ascii(byte[] data, int offset, int count)
	{
		char[] array = new char[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = (char)data[offset + i];
		}
		return new string(array);
	}
}
