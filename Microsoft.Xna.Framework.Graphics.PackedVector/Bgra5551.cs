using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct Bgra5551 : IPackedVector<ushort>, IPackedVector, IEquatable<Bgra5551>
{
	private ushort packedValue;

	[CLSCompliant(false)]
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

	public Bgra5551(float x, float y, float z, float w)
	{
		packedValue = Pack(x, y, z, w);
	}

	public Bgra5551(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public Vector4 ToVector4()
	{
		return new Vector4((float)((packedValue >> 10) & 0x1F) / 31f, (float)((packedValue >> 5) & 0x1F) / 31f, (float)(packedValue & 0x1F) / 31f, packedValue >> 15);
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public override bool Equals(object obj)
	{
		return obj is Bgra5551 && Equals((Bgra5551)obj);
	}

	public bool Equals(Bgra5551 other)
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

	public static bool operator ==(Bgra5551 lhs, Bgra5551 rhs)
	{
		return lhs.packedValue == rhs.packedValue;
	}

	public static bool operator !=(Bgra5551 lhs, Bgra5551 rhs)
	{
		return lhs.packedValue != rhs.packedValue;
	}

	private static ushort Pack(float x, float y, float z, float w)
	{
		return (ushort)(((ushort)Math.Round(MathHelper.Clamp(x, 0f, 1f) * 31f) << 10) | ((ushort)Math.Round(MathHelper.Clamp(y, 0f, 1f) * 31f) << 5) | (ushort)Math.Round(MathHelper.Clamp(z, 0f, 1f) * 31f) | ((ushort)Math.Round(MathHelper.Clamp(w, 0f, 1f)) << 15));
	}
}
