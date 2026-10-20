using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct Bgr565 : IPackedVector<ushort>, IPackedVector, IEquatable<Bgr565>
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

	public Bgr565(float x, float y, float z)
	{
		packedValue = Pack(x, y, z);
	}

	public Bgr565(Vector3 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z);
	}

	public Vector3 ToVector3()
	{
		return new Vector3((float)(packedValue >> 11) / 31f, (float)((packedValue >> 5) & 0x3F) / 63f, (float)(packedValue & 0x1F) / 31f);
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		Pack(vector.X, vector.Y, vector.Z);
	}

	Vector4 IPackedVector.ToVector4()
	{
		return new Vector4(ToVector3(), 1f);
	}

	public override bool Equals(object obj)
	{
		return obj is Bgr565 && Equals((Bgr565)obj);
	}

	public bool Equals(Bgr565 other)
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

	public static bool operator ==(Bgr565 lhs, Bgr565 rhs)
	{
		return lhs.packedValue == rhs.packedValue;
	}

	public static bool operator !=(Bgr565 lhs, Bgr565 rhs)
	{
		return lhs.packedValue != rhs.packedValue;
	}

	private static ushort Pack(float x, float y, float z)
	{
		return (ushort)(((ushort)Math.Round(MathHelper.Clamp(x, 0f, 1f) * 31f) << 11) | ((ushort)Math.Round(MathHelper.Clamp(y, 0f, 1f) * 63f) << 5) | (ushort)Math.Round(MathHelper.Clamp(z, 0f, 1f) * 31f));
	}
}
