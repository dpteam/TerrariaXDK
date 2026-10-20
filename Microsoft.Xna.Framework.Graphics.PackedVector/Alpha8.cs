using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct Alpha8 : IPackedVector<byte>, IPackedVector, IEquatable<Alpha8>
{
	private byte packedValue;
	public byte PackedValue
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

	public Alpha8(float alpha)
	{
		packedValue = Pack(alpha);
	}

	public float ToAlpha()
	{
		return (float)(int)packedValue / 255f;
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = Pack(vector.W);
	}

	Vector4 IPackedVector.ToVector4()
	{
		return new Vector4(0f, 0f, 0f, (float)(int)packedValue / 255f);
	}

	public override bool Equals(object obj)
	{
		return obj is Alpha8 && Equals((Alpha8)obj);
	}

	public bool Equals(Alpha8 other)
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

	public static bool operator ==(Alpha8 lhs, Alpha8 rhs)
	{
		return lhs.packedValue == rhs.packedValue;
	}

	public static bool operator !=(Alpha8 lhs, Alpha8 rhs)
	{
		return lhs.packedValue != rhs.packedValue;
	}

	private static byte Pack(float alpha)
	{
		return (byte)Math.Round(MathHelper.Clamp(alpha, 0f, 1f) * 255f);
	}
}
