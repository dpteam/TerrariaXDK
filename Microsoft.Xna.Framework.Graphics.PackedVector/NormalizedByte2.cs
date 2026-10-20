using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct NormalizedByte2 : IPackedVector<ushort>, IPackedVector, IEquatable<NormalizedByte2>
{
	private ushort packedValue;
	public ushort PackedValue
	{
		get
		{
			return packedValue;
		}
		set
		{
			packedValue = value;
		}
	}

	public NormalizedByte2(Vector2 vector)
	{
		packedValue = Pack(vector.X, vector.Y);
	}

	public NormalizedByte2(float x, float y)
	{
		packedValue = Pack(x, y);
	}

	public Vector2 ToVector2()
	{
		return new Vector2((float)(sbyte)(packedValue & 0xFF) / 127f, (float)(sbyte)((packedValue >> 8) & 0xFF) / 127f);
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y);
	}

	Vector4 IPackedVector.ToVector4()
	{
		return new Vector4(ToVector2(), 0f, 1f);
	}

	public static bool operator !=(NormalizedByte2 a, NormalizedByte2 b)
	{
		return a.packedValue != b.packedValue;
	}

	public static bool operator ==(NormalizedByte2 a, NormalizedByte2 b)
	{
		return a.packedValue == b.packedValue;
	}

	public override bool Equals(object obj)
	{
		return obj is NormalizedByte2 && Equals((NormalizedByte2)obj);
	}

	public bool Equals(NormalizedByte2 other)
	{
		return packedValue == other.packedValue;
	}

	public override int GetHashCode()
	{
		return packedValue.GetHashCode();
	}

	public override string ToString()
	{
		return packedValue.ToString("X");
	}

	private static ushort Pack(float x, float y)
	{
		int num = (ushort)Math.Round(MathHelper.Clamp(x, -1f, 1f) * 127f) & 0xFF;
		int num2 = ((ushort)Math.Round(MathHelper.Clamp(y, -1f, 1f) * 127f) << 8) & 0xFF00;
		return (ushort)(num | num2);
	}
}
