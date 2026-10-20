using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct Rgba1010102 : IPackedVector<uint>, IPackedVector, IEquatable<Rgba1010102>
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

	public Rgba1010102(float x, float y, float z, float w)
	{
		packedValue = Pack(x, y, z, w);
	}

	public Rgba1010102(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public Vector4 ToVector4()
	{
		return new Vector4((float)(packedValue & 0x3FF) / 1023f, (float)((packedValue >> 10) & 0x3FF) / 1023f, (float)((packedValue >> 20) & 0x3FF) / 1023f, (float)(packedValue >> 30) / 3f);
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public override bool Equals(object obj)
	{
		return obj is Rgba1010102 && Equals((Rgba1010102)obj);
	}

	public bool Equals(Rgba1010102 other)
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

	public static bool operator ==(Rgba1010102 lhs, Rgba1010102 rhs)
	{
		return lhs.packedValue == rhs.packedValue;
	}

	public static bool operator !=(Rgba1010102 lhs, Rgba1010102 rhs)
	{
		return lhs.packedValue != rhs.packedValue;
	}

	private static uint Pack(float x, float y, float z, float w)
	{
		return (uint)Math.Round(MathHelper.Clamp(x, 0f, 1f) * 1023f) | ((uint)Math.Round(MathHelper.Clamp(y, 0f, 1f) * 1023f) << 10) | ((uint)Math.Round(MathHelper.Clamp(z, 0f, 1f) * 1023f) << 20) | ((uint)Math.Round(MathHelper.Clamp(w, 0f, 1f) * 3f) << 30);
	}
}
