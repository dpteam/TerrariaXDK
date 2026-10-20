using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct Rg32 : IPackedVector<uint>, IPackedVector, IEquatable<Rg32>
{
	private uint packedValue;

	[CLSCompliant(false)]
	public uint PackedValue
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

	public Rg32(float x, float y)
	{
		packedValue = Pack(x, y);
	}

	public Rg32(Vector2 vector)
	{
		packedValue = Pack(vector.X, vector.Y);
	}

	public Vector2 ToVector2()
	{
		return new Vector2((float)(packedValue & 0xFFFF) / 65535f, (float)(packedValue >> 16) / 65535f);
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y);
	}

	Vector4 IPackedVector.ToVector4()
	{
		return new Vector4(ToVector2(), 0f, 1f);
	}

	public override bool Equals(object obj)
	{
		return obj is Rg32 && Equals((Rg32)obj);
	}

	public bool Equals(Rg32 other)
	{
		return packedValue == other.packedValue;
	}

	public override string ToString()
	{
		return packedValue.ToString("X");
	}

	public override int GetHashCode()
	{
		return packedValue.GetHashCode();
	}

	public static bool operator ==(Rg32 lhs, Rg32 rhs)
	{
		return lhs.packedValue == rhs.packedValue;
	}

	public static bool operator !=(Rg32 lhs, Rg32 rhs)
	{
		return lhs.packedValue != rhs.packedValue;
	}

	private static uint Pack(float x, float y)
	{
		return (uint)Math.Round(MathHelper.Clamp(x, 0f, 1f) * 65535f) | ((uint)Math.Round(MathHelper.Clamp(y, 0f, 1f) * 65535f) << 16);
	}
}
