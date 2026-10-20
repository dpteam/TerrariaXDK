using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct Rgba64 : IPackedVector<ulong>, IPackedVector, IEquatable<Rgba64>
{
	private ulong packedValue;

	[CLSCompliant(false)]
	public ulong PackedValue
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

	public Rgba64(float x, float y, float z, float w)
	{
		packedValue = Pack(x, y, z, w);
	}

	public Rgba64(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public Vector4 ToVector4()
	{
		return new Vector4((float)(packedValue & 0xFFFF) / 65535f, (float)((packedValue >> 16) & 0xFFFF) / 65535f, (float)((packedValue >> 32) & 0xFFFF) / 65535f, (float)(packedValue >> 48) / 65535f);
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public override bool Equals(object obj)
	{
		return obj is Rgba64 && Equals((Rgba64)obj);
	}

	public bool Equals(Rgba64 other)
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

	public static bool operator ==(Rgba64 lhs, Rgba64 rhs)
	{
		return lhs.packedValue == rhs.packedValue;
	}

	public static bool operator !=(Rgba64 lhs, Rgba64 rhs)
	{
		return lhs.packedValue != rhs.packedValue;
	}

	private static ulong Pack(float x, float y, float z, float w)
	{
		return (ulong)Math.Round(MathHelper.Clamp(x, 0f, 1f) * 65535f) | ((ulong)Math.Round(MathHelper.Clamp(y, 0f, 1f) * 65535f) << 16) | ((ulong)Math.Round(MathHelper.Clamp(z, 0f, 1f) * 65535f) << 32) | ((ulong)Math.Round(MathHelper.Clamp(w, 0f, 1f) * 65535f) << 48);
	}
}
