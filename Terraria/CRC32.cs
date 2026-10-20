namespace Terraria;

public sealed class CRC32
{
	private const uint Polynomial = 3988292384u;

	private const int TableCount = 8;

	private static uint[] Tables;

	private uint CRCValue;

	public static void Initialize()
	{
		Tables = new uint[2048];
		int i;
		for (i = 0; i < 256; i++)
		{
			uint num = (uint)i;
			for (int j = 0; j < 8; j++)
			{
				num = (num >> 1) ^ (0xEDB88320u & ~((num & 1) - 1));
			}
			Tables[i] = num;
		}
		for (; i < 2048; i++)
		{
			uint num2 = Tables[i - 256];
			Tables[i] = Tables[num2 & 0x100] ^ (num2 >> 8);
		}
	}

	public CRC32()
	{
		CRCValue = uint.MaxValue;
	}

	public void Init()
	{
		CRCValue = uint.MaxValue;
	}

	public uint GetValue()
	{
		return CRCValue ^ 0xFFFFFFFFu;
	}

	public void UpdateByte(byte Byte)
	{
		CRCValue = (CRCValue >> 8) ^ Tables[(byte)CRCValue ^ Byte];
	}

	public void Update(byte[] Data, int Offset, int Size)
	{
		if (Size == 0)
		{
			return;
		}
		uint[] tables = Tables;
		uint num = CRCValue;
		while ((Offset & 7) != 0 && Size != 0)
		{
			num = (num >> 8) ^ tables[(byte)num ^ Data[Offset++]];
			Size--;
		}
		if (Size >= 8)
		{
			int num2 = (Size - 8) & -8;
			Size -= num2;
			num2 += Offset;
			while (Offset != num2)
			{
				num ^= (uint)(Data[Offset] + (Data[Offset + 1] << 8) + (Data[Offset + 2] << 16) + (Data[Offset + 3] << 24));
				uint num3 = (uint)(Data[Offset + 4] + (Data[Offset + 5] << 8) + (Data[Offset + 6] << 16) + (Data[Offset + 7] << 24));
				Offset += 8;
				num = tables[(byte)num + 1792] ^ tables[(byte)(num >>= 8) + 1536] ^ tables[(byte)(num >>= 8) + 1280] ^ tables[(num >> 8) + 1024] ^ tables[(byte)num3 + 768] ^ tables[(byte)(num3 >>= 8) + 512] ^ tables[(byte)(num3 >>= 8) + 256] ^ tables[num3 >> 8];
			}
		}
		while (Size-- != 0)
		{
			num = (num >> 8) ^ tables[(byte)num ^ Data[Offset++]];
		}
		CRCValue = num;
	}
}
