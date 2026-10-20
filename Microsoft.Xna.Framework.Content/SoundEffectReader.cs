using System.IO;
using Microsoft.Xna.Framework.Audio;

namespace Microsoft.Xna.Framework.Content;

internal class SoundEffectReader : ContentTypeReader<SoundEffect>
{
	private static string[] supportedExtensions = new string[1] { ".wav" };

	internal static string Normalize(string fileName)
	{
		return ContentTypeReader.Normalize(fileName, supportedExtensions);
	}

	protected internal override SoundEffect Read(ContentReader input, SoundEffect existingInstance)
	{
		bool swap = input.platform == 'x';
		uint num = input.ReadUInt32();
		ushort num2 = Swap(swap, input.ReadUInt16());
		ushort nChannels = Swap(swap, input.ReadUInt16());
		uint nSamplesPerSec = Swap(swap, input.ReadUInt32());
		uint nAvgBytesPerSec = Swap(swap, input.ReadUInt32());
		ushort nBlockAlign = Swap(swap, input.ReadUInt16());
		ushort wBitsPerSample = Swap(swap, input.ReadUInt16());
		byte[] array = null;
		if (num > 16)
		{
			ushort num3 = Swap(swap, input.ReadUInt16());
			if (num2 == 358 && num3 == 34)
			{
				array = new byte[34];
				using (MemoryStream output = new MemoryStream(array))
				{
					using BinaryWriter binaryWriter = new BinaryWriter(output);
					binaryWriter.Write(Swap(swap, input.ReadUInt16()));
					binaryWriter.Write(Swap(swap, input.ReadUInt32()));
					binaryWriter.Write(Swap(swap, input.ReadUInt32()));
					binaryWriter.Write(Swap(swap, input.ReadUInt32()));
					binaryWriter.Write(Swap(swap, input.ReadUInt32()));
					binaryWriter.Write(Swap(swap, input.ReadUInt32()));
					binaryWriter.Write(Swap(swap, input.ReadUInt32()));
					binaryWriter.Write(Swap(swap, input.ReadUInt32()));
					binaryWriter.Write(input.ReadByte());
					binaryWriter.Write(input.ReadByte());
					binaryWriter.Write(Swap(swap, input.ReadUInt16()));
				}
				input.ReadBytes((int)(num - 18 - 34));
			}
			else
			{
				input.ReadBytes((int)(num - 18));
			}
		}
		byte[] array2 = input.ReadBytes(input.ReadInt32());
		int loopStart = input.ReadInt32();
		int loopLength = input.ReadInt32();
		input.ReadUInt32();
		return new SoundEffect(input.AssetName, array2, 0, array2.Length, array, num2, nChannels, nSamplesPerSec, nAvgBytesPerSec, nBlockAlign, wBitsPerSample, loopStart, loopLength);
	}

	internal static ushort Swap(bool swap, ushort x)
	{
		return (!swap) ? x : ((ushort)(((x >> 8) & 0xFF) | ((x << 8) & 0xFF00)));
	}

	internal static uint Swap(bool swap, uint x)
	{
		return (!swap) ? x : (((x >> 24) & 0xFF) | ((x >> 8) & 0xFF00) | ((x << 8) & 0xFF0000) | ((x << 24) & 0xFF000000u));
	}
}
