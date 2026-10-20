using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct Bgra4444 : IPackedVector<ushort>, IPackedVector, IEquatable<Bgra4444>
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

	public Bgra4444(float x, float y, float z, float w)
	{
		packedValue = Pack(x, y, z, w);
	}

	public Bgra4444(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public Vector4 ToVector4()
	{
		return new Vector4((float)((packedValue >> 8) & 0xF) / 15f, (float)((packedValue >> 4) & 0xF) / 15f, (float)(packedValue & 0xF) / 15f, (float)(packedValue >> 12) / 15f);
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public override bool Equals(object obj)
	{
		return obj is Bgra4444 && Equals((Bgra4444)obj);
	}

	public bool Equals(Bgra4444 other)
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

	public static bool operator ==(Bgra4444 lhs, Bgra4444 rhs)
	{
		return lhs.packedValue == rhs.packedValue;
	}

	public static bool operator !=(Bgra4444 lhs, Bgra4444 rhs)
	{
		return lhs.packedValue != rhs.packedValue;
	}

	private static ushort Pack(float x, float y, float z, float w)
	{
		return (ushort)(((ushort)Math.Round(MathHelper.Clamp(x, 0f, 1f) * 15f) << 8) | ((ushort)Math.Round(MathHelper.Clamp(y, 0f, 1f) * 15f) << 4) | (ushort)Math.Round(MathHelper.Clamp(z, 0f, 1f) * 15f) | ((ushort)Math.Round(MathHelper.Clamp(w, 0f, 1f) * 15f) << 12));
	}
}
