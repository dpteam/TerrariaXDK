using System;
using System.IO;
using System.IO.Compression;

namespace Terraria.Headless;

/// <summary>
/// Builds real PNG files from explicit bytes, so the decoder can be tested against the format
/// instead of against itself.
///
/// The important detail is that the caller supplies the **already filtered** rows, including
/// the per-row filter byte. A helper that filtered and unfiltered with the same code would
/// round-trip happily even if both halves were wrong; this way the expected bytes are written
/// out in the test, derived from the PNG specification by hand.
/// </summary>
internal static class PngTestData
{
	/// <summary>Signature + IHDR + IDAT + IEND around the given filtered rows.</summary>
	public static byte[] Build(int width, int height, int bitDepth, int colorType, byte[] filteredRows, byte[] palette, byte[] transparency)
	{
		using MemoryStream memoryStream = new MemoryStream();
		memoryStream.Write(new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
		using (MemoryStream memoryStream2 = new MemoryStream())
		{
			WriteInt32(memoryStream2, width);
			WriteInt32(memoryStream2, height);
			memoryStream2.WriteByte((byte)bitDepth);
			memoryStream2.WriteByte((byte)colorType);
			memoryStream2.WriteByte(0);
			memoryStream2.WriteByte(0);
			memoryStream2.WriteByte(0);
			WriteChunk(memoryStream, "IHDR", memoryStream2.ToArray());
		}
		if (palette != null)
		{
			WriteChunk(memoryStream, "PLTE", palette);
		}
		if (transparency != null)
		{
			WriteChunk(memoryStream, "tRNS", transparency);
		}
		WriteChunk(memoryStream, "IDAT", Deflate(filteredRows));
		WriteChunk(memoryStream, "IEND", new byte[0]);
		return memoryStream.ToArray();
	}

	/// <summary>A PNG whose IDAT holds bytes that are not valid zlib, for the corruption test.</summary>
	public static byte[] BuildWithCorruptData(int width, int height, int bitDepth, int colorType, byte[] junk)
	{
		using MemoryStream memoryStream = new MemoryStream();
		memoryStream.Write(new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
		using (MemoryStream memoryStream2 = new MemoryStream())
		{
			WriteInt32(memoryStream2, width);
			WriteInt32(memoryStream2, height);
			memoryStream2.WriteByte((byte)bitDepth);
			memoryStream2.WriteByte((byte)colorType);
			memoryStream2.WriteByte(0);
			memoryStream2.WriteByte(0);
			memoryStream2.WriteByte(0);
			WriteChunk(memoryStream, "IHDR", memoryStream2.ToArray());
		}
		WriteChunk(memoryStream, "IDAT", junk);
		WriteChunk(memoryStream, "IEND", new byte[0]);
		return memoryStream.ToArray();
	}

	/// <summary>
	/// zlib-wrapped deflate (RFC 1950 around RFC 1951) with a correct Adler-32, so the files are
	/// valid PNGs that any reader would accept.
	/// </summary>
	private static byte[] Deflate(byte[] raw)
	{
		using MemoryStream memoryStream = new MemoryStream();
		memoryStream.WriteByte(0x78);
		memoryStream.WriteByte(0x9C);
		using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress, leaveOpen: true))
		{
			deflateStream.Write(raw, 0, raw.Length);
		}
		uint num = Adler32(raw);
		memoryStream.WriteByte((byte)(num >> 24));
		memoryStream.WriteByte((byte)(num >> 16));
		memoryStream.WriteByte((byte)(num >> 8));
		memoryStream.WriteByte((byte)num);
		return memoryStream.ToArray();
	}

	private static uint Adler32(byte[] data)
	{
		uint num = 1u;
		uint num2 = 0u;
		for (int i = 0; i < data.Length; i++)
		{
			num = (num + data[i]) % 65521;
			num2 = (num2 + num) % 65521;
		}
		return (num2 << 16) | num;
	}

	private static void WriteChunk(Stream stream, string type, byte[] payload)
	{
		WriteInt32(stream, payload.Length);
		byte[] array = new byte[4];
		for (int i = 0; i < 4; i++)
		{
			array[i] = (byte)type[i];
		}
		stream.Write(array, 0, 4);
		stream.Write(payload, 0, payload.Length);
		uint num = Crc32(array, payload);
		WriteInt32(stream, (int)num);
	}

	private static uint Crc32(byte[] type, byte[] payload)
	{
		uint num = 4294967295u;
		num = Update(num, type);
		num = Update(num, payload);
		return num ^ 0xFFFFFFFFu;
	}

	private static uint Update(uint crc, byte[] data)
	{
		for (int i = 0; i < data.Length; i++)
		{
			crc ^= data[i];
			for (int j = 0; j < 8; j++)
			{
				crc = (((crc & 1) != 0) ? ((crc >> 1) ^ 0xEDB88320u) : (crc >> 1));
			}
		}
		return crc;
	}

	private static void WriteInt32(Stream stream, int value)
	{
		stream.WriteByte((byte)(value >> 24));
		stream.WriteByte((byte)(value >> 16));
		stream.WriteByte((byte)(value >> 8));
		stream.WriteByte((byte)value);
	}
}
