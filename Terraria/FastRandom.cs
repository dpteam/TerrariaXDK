using System;

namespace Terraria;

public sealed class FastRandom
{
	private const double IntRealUnit = 4.656612873077393E-10;

	private const uint YSetting = 842502087u;

	private const uint ZSetting = 3579807591u;

	private const uint WSetting = 273326509u;

	private uint X;

	private uint Y;

	private uint Z;

	private uint W;

	private uint BitBuffer;

	private uint BitMask = 1u;

	public FastRandom()
	{
		Reinitialise((uint)Environment.TickCount);
	}

	public FastRandom(uint Seed)
	{
		Reinitialise(Seed);
	}

	public void Reinitialise(uint Seed)
	{
		X = Seed;
		Y = 842502087u;
		Z = 3579807591u;
		W = 273326509u;
	}

	public int Next()
	{
		uint num2;
		do
		{
			uint num = X ^ (X << 11);
			X = Y;
			Y = Z;
			Z = W;
			W = W ^ (W >> 19) ^ (num ^ (num >> 8));
			num2 = W & 0x7FFFFFFF;
		}
		while (num2 == int.MaxValue);
		return (int)num2;
	}

	public int Next(int UpperBound)
	{
		uint num = X ^ (X << 11);
		X = Y;
		Y = Z;
		Z = W;
		return (int)(4.656612873077393E-10 * (double)(int)(0x7FFFFFFF & (W = W ^ (W >> 19) ^ (num ^ (num >> 8)))) * (double)UpperBound);
	}

	public int Next(int LowerBound, int UpperBound)
	{
		uint num = X ^ (X << 11);
		X = Y;
		Y = Z;
		Z = W;
		return LowerBound + (int)(4.656612873077393E-10 * (double)(int)(0x7FFFFFFF & (W = W ^ (W >> 19) ^ (num ^ (num >> 8)))) * (double)(UpperBound - LowerBound));
	}

	public double NextDouble()
	{
		uint num = X ^ (X << 11);
		X = Y;
		Y = Z;
		Z = W;
		return 4.656612873077393E-10 * (double)(int)(0x7FFFFFFF & (W = W ^ (W >> 19) ^ (num ^ (num >> 8))));
	}

	public void NextBytes(byte[] Buffer)
	{
		uint num = X;
		uint num2 = Y;
		uint num3 = Z;
		uint num4 = W;
		int num5 = 0;
		int num6 = Buffer.Length - 3;
		while (num5 < num6)
		{
			uint num7 = num ^ (num << 11);
			num = num2;
			num2 = num3;
			num3 = num4;
			num4 = num4 ^ (num4 >> 19) ^ (num7 ^ (num7 >> 8));
			Buffer[num5++] = (byte)num4;
			Buffer[num5++] = (byte)(num4 >> 8);
			Buffer[num5++] = (byte)(num4 >> 16);
			Buffer[num5++] = (byte)(num4 >> 24);
		}
		if (num5 < Buffer.Length)
		{
			uint num8 = num ^ (num << 11);
			num = num2;
			num2 = num3;
			num3 = num4;
			num4 = num4 ^ (num4 >> 19) ^ (num8 ^ (num8 >> 8));
			Buffer[num5++] = (byte)num4;
			if (num5 < Buffer.Length)
			{
				Buffer[num5++] = (byte)(num4 >> 8);
				if (num5 < Buffer.Length)
				{
					Buffer[num5++] = (byte)(num4 >> 16);
					if (num5 < Buffer.Length)
					{
						Buffer[num5] = (byte)(num4 >> 24);
					}
				}
			}
		}
		X = num;
		Y = num2;
		Z = num3;
		W = num4;
	}

	public uint NextUInt()
	{
		uint num = X ^ (X << 11);
		X = Y;
		Y = Z;
		Z = W;
		return W = W ^ (W >> 19) ^ (num ^ (num >> 8));
	}

	public int NextInt()
	{
		uint num = X ^ (X << 11);
		X = Y;
		Y = Z;
		Z = W;
		return (int)(0x7FFFFFFF & (W = W ^ (W >> 19) ^ (num ^ (num >> 8))));
	}

	public bool NextBool()
	{
		if (BitMask == 1)
		{
			uint num = X ^ (X << 11);
			X = Y;
			Y = Z;
			Z = W;
			BitBuffer = (W = W ^ (W >> 19) ^ (num ^ (num >> 8)));
			BitMask = 2147483648u;
			return (BitBuffer & BitMask) == 0;
		}
		return (BitBuffer & (BitMask >>= 1)) == 0;
	}
}
