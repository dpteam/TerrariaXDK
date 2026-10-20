using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct Byte4 : IPackedVector<uint>, IPackedVector, IEquatable<Byte4>
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

	public Byte4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public Byte4(float x, float y, float z, float w)
	{
		packedValue = Pack(x, y, z, w);
	}

	public Vector4 ToVector4()
	{
		return new Vector4(packedValue & 0xFF, (packedValue >> 8) & 0xFF, (packedValue >> 16) & 0xFF, packedValue >> 24);
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public static bool operator !=(Byte4 a, Byte4 b)
	{
		return a.packedValue != b.packedValue;
	}

	public static bool operator ==(Byte4 a, Byte4 b)
	{
		return a.packedValue == b.packedValue;
	}

	public override bool Equals(object obj)
	{
		return obj is Byte4 && Equals((Byte4)obj);
	}

	public bool Equals(Byte4 other)
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

	private static uint Pack(float x, float y, float z, float w)
	{
		return (uint)Math.Round(MathHelper.Clamp(x, 0f, 255f)) | ((uint)Math.Round(MathHelper.Clamp(y, 0f, 255f)) << 8) | ((uint)Math.Round(MathHelper.Clamp(z, 0f, 255f)) << 16) | ((uint)Math.Round(MathHelper.Clamp(w, 0f, 255f)) << 24);
	}
}
