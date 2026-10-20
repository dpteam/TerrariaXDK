using System;
using System.IO;
using System.Text;

namespace XnbToPng;

/// <summary>
/// Minimal XNB reader for textures (Windows platform, version 5).
///
/// Only what this project needs: the header, the LZ4-compressed payload, and the
/// <c>Texture2DReader</c> object. SpriteFont/Effect/Song assets are recognised and skipped
/// rather than half-parsed.
///
/// Layout (XNA 4 / FNA):
/// <code>
/// 'X','N','B', platform, version, flags, fileSize
///   flags &amp; 0x80 -> int32 decompressedSize, then LZ4 blocks
///   content: 7bit typeReaderCount, [string]*, int32 sharedResources, 7bit objectTypeId
///     Texture2D: int32 format, int32 width, int32 height, int32 mipCount,
///                mipCount x (int32 size, bytes)
/// </code>
/// </summary>
internal sealed class XnbFile
{
	public int Format;

	public int Width;

	public int Height;

	public int MipCount;

	/// <summary>Raw surface data of the largest mip.</summary>
	public byte[] Data;

	public string TypeName;

	/// <summary>'w' (Windows/desktop) or 'x' (Xbox 360). Xbox textures are byte-swapped.</summary>
	public char Platform;

	/// <summary>True when the payload really is a Texture2D we can decode.</summary>
	public bool IsTexture => Data != null && Width > 0 && Height > 0;

	public static XnbFile Read(string path)
	{
		byte[] array = File.ReadAllBytes(path);
		if (array.Length < 10 || array[0] != 'X' || array[1] != 'N' || array[2] != 'B')
		{
			throw new InvalidDataException("not an XNB file");
		}
		char c = (char)array[3];
		if (c != 'w' && c != 'x')
		{
			throw new InvalidDataException("platform '" + c + "' is not supported (only 'w' and 'x')");
		}
		int num = array[4];
		if (num != 5)
		{
			throw new InvalidDataException("XNB version " + num + " is not supported");
		}
		byte flags = array[5];
		byte[] buffer;
		if ((flags & 0x80) != 0)
		{
			// Compressed: int32 file size, int32 decompressed size, then LZX frames.
			int num2 = BitConverter.ToInt32(array, 10);
			buffer = DecompressLzx(array, 14, num2);
		}
		else
		{
			buffer = new byte[array.Length - 10];
			Buffer.BlockCopy(array, 10, buffer, 0, buffer.Length);
		}
		XnbFile xnbFile = Parse(buffer);
		xnbFile.Platform = c;
		return xnbFile;
	}

	/// <summary>
	/// LZX frames, the format FNA's own <c>ContentManager</c> reads: a big-endian 16-bit
	/// block size followed by that many bytes, producing a full 32 KiB frame. A leading
	/// <c>0xFF</c> means the sizes are 16 bits each:
	/// <c>0xFF, outHi, outLo, blkHi, blkLo</c>. This mirrors the runtime loader exactly -
	/// if it drifts, compressed assets would extract differently from how the game shows them.
	/// </summary>
	private static byte[] DecompressLzx(byte[] source, int offset, int decompressedSize)
	{
		if (decompressedSize <= 0 || decompressedSize > 512 * 1024 * 1024)
		{
			throw new InvalidDataException("implausible decompressed size " + decompressedSize);
		}
		byte[] array = new byte[decompressedSize];
		using MemoryStream memoryStream = new MemoryStream(array, 0, array.Length, writable: true, publiclyVisible: true);
		Microsoft.Xna.Framework.Content.LzxDecoder lzxDecoder = new Microsoft.Xna.Framework.Content.LzxDecoder(16);
		int num = offset;
		int num2 = source.Length;
		while (num + 2 <= num2)
		{
			int num3 = source[num];
			int num4 = source[num + 1];
			num += 2;
			int blockSize;
			int outputSize;
			if (num3 == 255)
			{
				// 0xFF, outHi, outLo, blkHi, blkLo: five bytes total.
				if (num + 3 > num2)
				{
					throw new InvalidDataException("truncated LZX frame header");
				}
				outputSize = (num4 << 8) | source[num];
				num++;
				blockSize = (source[num] << 8) | source[num + 1];
				num += 2;
			}
			else
			{
				// The common case: 16-bit block size, a full 32 KiB frame.
				blockSize = (num3 << 8) | num4;
				outputSize = 32768;
			}
			if (blockSize == 0 || outputSize == 0)
			{
				break;
			}
			if (num + blockSize > num2)
			{
				throw new InvalidDataException("LZX frame overruns the file (" + blockSize + " bytes at " + num + " of " + num2 + ")");
			}
			using (MemoryStream input = new MemoryStream(source, num, blockSize))
			{
				lzxDecoder.Decompress(input, blockSize, memoryStream, outputSize);
			}
			num += blockSize;
		}
		if (memoryStream.Position != decompressedSize)
		{
			throw new InvalidDataException("LZX decompressed " + memoryStream.Position + " of " + decompressedSize + " bytes");
		}
		return array;
	}

	private static XnbFile Parse(byte[] data)
	{
		int num = 0;
		int num2 = Read7BitInt(data, ref num);
		string[] array = new string[num2];
		for (int i = 0; i < num2; i++)
		{
			array[i] = ReadString(data, ref num);
			// Each type reader entry also carries an int32 version. Skipping it shifted the
			// whole object by four bytes, which looked like "type id 0".
			ReadInt32(data, ref num);
		}
		Read7BitInt(data, ref num);
		int num3 = Read7BitInt(data, ref num);
		if (num3 <= 0 || num3 > num2)
		{
			throw new InvalidDataException("bad primary object type id " + num3);
		}
		XnbFile xnbFile = new XnbFile();
		xnbFile.TypeName = array[num3 - 1] ?? string.Empty;
		if (xnbFile.TypeName.IndexOf("Texture2DReader", StringComparison.Ordinal) < 0)
		{
			// Not a texture: report it and let the caller skip the file.
			return xnbFile;
		}
		xnbFile.Format = ReadInt32(data, ref num);
		xnbFile.Width = ReadInt32(data, ref num);
		xnbFile.Height = ReadInt32(data, ref num);
		xnbFile.MipCount = ReadInt32(data, ref num);
		if (xnbFile.MipCount <= 0)
		{
			throw new InvalidDataException("texture has no mips");
		}
		for (int j = 0; j < xnbFile.MipCount; j++)
		{
			int num4 = ReadInt32(data, ref num);
			if (num4 < 0 || num + num4 > data.Length)
			{
				throw new InvalidDataException("mip " + j + " is out of bounds");
			}
			if (j == 0)
			{
				xnbFile.Data = new byte[num4];
				Buffer.BlockCopy(data, num, xnbFile.Data, 0, num4);
			}
			num += num4;
		}
		return xnbFile;
	}

	private static int Read7BitInt(byte[] data, ref int offset)
	{
		int num = 0;
		int num2 = 0;
		while (num2 < 5)
		{
			if (offset >= data.Length)
			{
				throw new InvalidDataException("truncated 7-bit integer");
			}
			byte b = data[offset++];
			num |= (b & 0x7F) << (num2 * 7);
			if ((b & 0x80) == 0)
			{
				return num;
			}
			num2++;
		}
		throw new InvalidDataException("bad 7-bit integer");
	}

	private static int ReadInt32(byte[] data, ref int offset)
	{
		if (offset + 4 > data.Length)
		{
			throw new InvalidDataException("truncated int32");
		}
		int result = BitConverter.ToInt32(data, offset);
		offset += 4;
		return result;
	}

	private static string ReadString(byte[] data, ref int offset)
	{
		int num = Read7BitInt(data, ref offset);
		if (num < 0 || offset + num > data.Length)
		{
			throw new InvalidDataException("truncated string");
		}
		string result = Encoding.UTF8.GetString(data, offset, num);
		offset += num;
		return result;
	}
}
