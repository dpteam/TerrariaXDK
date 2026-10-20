public static class CRC32Helper
{
	private static readonly uint[] crcTable;

	static CRC32Helper()
	{
		crcTable = new uint[256];
		for (uint num = 0u; num < 256; num++)
		{
			uint num2 = num;
			for (int i = 0; i < 8; i++)
			{
				num2 = (((num2 & 1) != 1) ? (num2 >> 1) : ((num2 >> 1) ^ 0xEDB88320u));
			}
			crcTable[num] = num2;
		}
	}

	public static uint CalculateCRC32(byte[] data)
	{
		uint num = uint.MaxValue;
		foreach (byte b in data)
		{
			num = (num >> 8) ^ crcTable[(num ^ b) & 0xFF];
		}
		return num ^ 0xFFFFFFFFu;
	}
}
