using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct HalfVector2 : IPackedVector<uint>, IPackedVector, IEquatable<HalfVector2>
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

	public HalfVector2(float x, float y)
	{
		packedValue = PackHelper(x, y);
	}

	public HalfVector2(Vector2 vector)
	{
		packedValue = PackHelper(vector.X, vector.Y);
	}

	public Vector2 ToVector2()
	{
		return new Vector2
		{
			X = HalfTypeHelper.Convert((ushort)packedValue),
			Y = HalfTypeHelper.Convert((ushort)(packedValue >> 16))
		};
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = PackHelper(vector.X, vector.Y);
	}

	Vector4 IPackedVector.ToVector4()
	{
		return new Vector4(ToVector2(), 0f, 1f);
	}

	public override string ToString()
	{
		return packedValue.ToString("X");
	}

	public override int GetHashCode()
	{
		return packedValue.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		return obj is HalfVector2 && Equals((HalfVector2)obj);
	}

	public bool Equals(HalfVector2 other)
	{
		return packedValue.Equals(other.packedValue);
	}

	public static bool operator ==(HalfVector2 a, HalfVector2 b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(HalfVector2 a, HalfVector2 b)
	{
		return !a.Equals(b);
	}

	private static uint PackHelper(float vectorX, float vectorY)
	{
		return (uint)(HalfTypeHelper.Convert(vectorX) | (HalfTypeHelper.Convert(vectorY) << 16));
	}
}
