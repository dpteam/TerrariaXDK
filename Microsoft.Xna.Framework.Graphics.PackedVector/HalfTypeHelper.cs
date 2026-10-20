using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

internal static class HalfTypeHelper
{
	[StructLayout(LayoutKind.Explicit)]
	private struct uif
	{
		[FieldOffset(0)]
		public float f;

		[FieldOffset(0)]
		public int i;

		[FieldOffset(0)]
		public uint u;
	}

	internal static ushort Convert(float f)
	{
		uif uif2 = new uif
		{
			f = f
		};
		return Convert(uif2.i);
	}

	internal static ushort Convert(int i)
	{
		int num = (i >> 16) & 0x8000;
		int num2 = ((i >> 23) & 0xFF) - 112;
		int num3 = i & 0x7FFFFF;
		if (num2 <= 0)
		{
			if (num2 < -10)
			{
				return (ushort)num;
			}
			num3 |= 0x800000;
			int num4 = 14 - num2;
			int num5 = (1 << num4 - 1) - 1;
			int num6 = (num3 >> num4) & 1;
			num3 = num3 + num5 + num6 >> num4;
			return (ushort)(num | num3);
		}
		if (num2 == 143)
		{
			if (num3 == 0)
			{
				return (ushort)(num | 0x7C00);
			}
			num3 >>= 13;
			return (ushort)((uint)(num | 0x7C00 | num3) | ((num3 == 0) ? 1u : 0u));
		}
		num3 = num3 + 4095 + ((num3 >> 13) & 1);
		if ((num3 & 0x800000) != 0)
		{
			num3 = 0;
			num2++;
		}
		if (num2 > 30)
		{
			return (ushort)(num | 0x7C00);
		}
		return (ushort)(num | (num2 << 10) | (num3 >> 13));
	}

	internal static float Convert(ushort value)
	{
		uint num = (uint)(value & 0x3FF);
		uint num2 = 4294967282u;
		uint u;
		if ((value & -33792) == 0)
		{
			if (num != 0)
			{
				while ((num & 0x400) == 0)
				{
					num2--;
					num <<= 1;
				}
				num &= 0xFFFFFBFFu;
				u = (uint)((value & 0x8000) << 16) | (num2 + 127 << 23) | (num << 13);
			}
			else
			{
				u = (uint)((value & 0x8000) << 16);
			}
		}
		else
		{
			u = (uint)(((value & 0x8000) << 16) | (((value >>> 10) & 0x1F) - 15 + 127 << 23)) | (num << 13);
		}
		return new uif
		{
			u = u
		}.f;
	}
}
