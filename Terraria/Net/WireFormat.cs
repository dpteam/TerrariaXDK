using System;
using System.IO;

namespace Terraria.Net;

/// <summary>
/// Length-prefixed framing shared by every transport channel. Kept separate from
/// <see cref="TcpPeer"/> so it can be exercised without a socket (see Headless/SelfTest).
///
/// Layout: [int32 payloadLength][byte opcode][payload...], little endian.
/// </summary>
internal static class WireFormat
{
	public const int HeaderSize = 4;

	public const int MaxFrameSize = 4 * 1024 * 1024;

	public static byte[] Frame(byte opcode, byte[] payload)
	{
		int num = 1 + ((payload == null) ? 0 : payload.Length);
		byte[] array = new byte[HeaderSize + num];
		array[0] = (byte)num;
		array[1] = (byte)(num >> 8);
		array[2] = (byte)(num >> 16);
		array[3] = (byte)(num >> 24);
		array[4] = opcode;
		if (payload != null && payload.Length != 0)
		{
			Buffer.BlockCopy(payload, 0, array, 5, payload.Length);
		}
		return array;
	}

	/// <summary>Prefixes an already framed body with its length (used for the handshake).</summary>
	public static byte[] LengthPrefix(byte[] body)
	{
		byte[] array = new byte[HeaderSize + body.Length];
		int num = body.Length;
		array[0] = (byte)num;
		array[1] = (byte)(num >> 8);
		array[2] = (byte)(num >> 16);
		array[3] = (byte)(num >> 24);
		Buffer.BlockCopy(body, 0, array, HeaderSize, body.Length);
		return array;
	}

	public static void WriteAll(Stream stream, byte[] data)
	{
		lock (stream)
		{
			stream.Write(data, 0, data.Length);
			stream.Flush();
		}
	}

	/// <summary>Reads one frame, or null on clean EOF / corruption.</summary>
	public static byte[] ReadFrame(Stream stream)
	{
		byte[] array = new byte[HeaderSize];
		if (!ReadExactly(stream, array, HeaderSize))
		{
			return null;
		}
		int num = array[0] | (array[1] << 8) | (array[2] << 16) | (array[3] << 24);
		if (num <= 0 || num > MaxFrameSize)
		{
			return null;
		}
		byte[] array2 = new byte[num];
		if (!ReadExactly(stream, array2, num))
		{
			return null;
		}
		return array2;
	}

	public static bool ReadExactly(Stream stream, byte[] buffer, int count)
	{
		int num = 0;
		while (num < count)
		{
			int num2;
			try
			{
				num2 = stream.Read(buffer, num, count - num);
			}
			catch
			{
				return false;
			}
			if (num2 <= 0)
			{
				return false;
			}
			num += num2;
		}
		return true;
	}
}
