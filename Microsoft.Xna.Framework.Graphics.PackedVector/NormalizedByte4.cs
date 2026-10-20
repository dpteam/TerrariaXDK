using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct NormalizedByte4 : IPackedVector<uint>, IPackedVector, IEquatable<NormalizedByte4>
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

	public NormalizedByte4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public NormalizedByte4(float x, float y, float z, float w)
	{
		packedValue = Pack(x, y, z, w);
	}

	public Vector4 ToVector4()
	{
		return new Vector4((float)(sbyte)(packedValue & 0xFF) / 127f, (float)(sbyte)((packedValue >> 8) & 0xFF) / 127f, (float)(sbyte)((packedValue >> 16) & 0xFF) / 127f, (float)(sbyte)((packedValue >> 24) & 0xFF) / 127f);
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public static bool operator !=(NormalizedByte4 a, NormalizedByte4 b)
	{
		return a.packedValue != b.packedValue;
	}

	public static bool operator ==(NormalizedByte4 a, NormalizedByte4 b)
	{
		return a.packedValue == b.packedValue;
	}

	public override bool Equals(object obj)
	{
		return obj is NormalizedByte4 && Equals((NormalizedByte4)obj);
	}

	public bool Equals(NormalizedByte4 other)
	{
		return packedValue == other.packedValue;
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
		uint num = (uint)Math.Round(MathHelper.Clamp(x, -1f, 1f) * 127f) & 0xFF;
		uint num2 = ((uint)Math.Round(MathHelper.Clamp(y, -1f, 1f) * 127f) << 8) & 0xFF00;
		uint num3 = ((uint)Math.Round(MathHelper.Clamp(z, -1f, 1f) * 127f) << 16) & 0xFF0000;
		uint num4 = ((uint)Math.Round(MathHelper.Clamp(w, -1f, 1f) * 127f) << 24) & 0xFF000000u;
		return num | num2 | num3 | num4;
	}
}
