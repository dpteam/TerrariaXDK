using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct Short4 : IPackedVector<ulong>, IPackedVector, IEquatable<Short4>
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

	public Short4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public Short4(float x, float y, float z, float w)
	{
		packedValue = Pack(x, y, z, w);
	}

	public Vector4 ToVector4()
	{
		return new Vector4((short)(packedValue & 0xFFFF), (short)((packedValue >> 16) & 0xFFFF), (short)((packedValue >> 32) & 0xFFFF), (short)(packedValue >> 48));
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public static bool operator !=(Short4 a, Short4 b)
	{
		return a.PackedValue != b.PackedValue;
	}

	public static bool operator ==(Short4 a, Short4 b)
	{
		return a.PackedValue == b.PackedValue;
	}

	public override bool Equals(object obj)
	{
		return obj is Short4 && Equals((Short4)obj);
	}

	public bool Equals(Short4 other)
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

	private static ulong Pack(float x, float y, float z, float w)
	{
		return (ulong)(((long)Math.Round(MathHelper.Clamp(x, -32768f, 32767f)) & 0xFFFF) | (((long)Math.Round(MathHelper.Clamp(y, -32768f, 32767f)) << 16) & 0xFFFF0000u) | (((long)Math.Round(MathHelper.Clamp(z, -32768f, 32767f)) << 32) & 0xFFFF00000000L) | ((long)Math.Round(MathHelper.Clamp(w, -32768f, 32767f)) << 48));
	}
}
