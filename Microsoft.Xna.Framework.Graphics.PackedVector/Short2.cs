using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct Short2 : IPackedVector<uint>, IPackedVector, IEquatable<Short2>
{
	private uint packedValue;
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

	public Short2(Vector2 vector)
	{
		packedValue = Pack(vector.X, vector.Y);
	}

	public Short2(float x, float y)
	{
		packedValue = Pack(x, y);
	}

	public Vector2 ToVector2()
	{
		return new Vector2((short)(packedValue & 0xFFFF), (short)(packedValue >> 16));
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y);
	}

	Vector4 IPackedVector.ToVector4()
	{
		return new Vector4(ToVector2(), 0f, 1f);
	}

	public static bool operator !=(Short2 a, Short2 b)
	{
		return a.packedValue != b.packedValue;
	}

	public static bool operator ==(Short2 a, Short2 b)
	{
		return a.packedValue == b.packedValue;
	}

	public override bool Equals(object obj)
	{
		return obj is Short2 && Equals((Short2)obj);
	}

	public bool Equals(Short2 other)
	{
		return this == other;
	}

	public override int GetHashCode()
	{
		return packedValue.GetHashCode();
	}

	public override string ToString()
	{
		return packedValue.ToString("X");
	}

	private static uint Pack(float x, float y)
	{
		return (uint)(((int)Math.Round(MathHelper.Clamp(x, -32768f, 32767f)) & 0xFFFF) | ((int)Math.Round(MathHelper.Clamp(y, -32768f, 32767f)) << 16));
	}
}
