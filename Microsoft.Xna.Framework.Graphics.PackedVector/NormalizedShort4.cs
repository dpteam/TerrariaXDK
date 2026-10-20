using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct NormalizedShort4 : IPackedVector<ulong>, IPackedVector, IEquatable<NormalizedShort4>
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

	public NormalizedShort4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public NormalizedShort4(float x, float y, float z, float w)
	{
		packedValue = Pack(x, y, z, w);
	}

	public Vector4 ToVector4()
	{
		return new Vector4((float)(short)(packedValue & 0xFFFF) / 32767f, (float)(short)((packedValue >> 16) & 0xFFFF) / 32767f, (float)(short)((packedValue >> 32) & 0xFFFF) / 32767f, (float)(short)((packedValue >> 48) & 0xFFFF) / 32767f);
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public static bool operator !=(NormalizedShort4 a, NormalizedShort4 b)
	{
		return !a.Equals(b);
	}

	public static bool operator ==(NormalizedShort4 a, NormalizedShort4 b)
	{
		return a.Equals(b);
	}

	public override bool Equals(object obj)
	{
		return obj is NormalizedShort4 && Equals((NormalizedShort4)obj);
	}

	public bool Equals(NormalizedShort4 other)
	{
		return packedValue.Equals(other.packedValue);
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
		ulong num = (ulong)MathHelper.Clamp((float)Math.Round(x * 32767f), -32767f, 32767f) & 0xFFFF;
		ulong num2 = ((ulong)MathHelper.Clamp((float)Math.Round(y * 32767f), -32767f, 32767f) & 0xFFFF) << 16;
		ulong num3 = ((ulong)MathHelper.Clamp((float)Math.Round(z * 32767f), -32767f, 32767f) & 0xFFFF) << 32;
		ulong num4 = ((ulong)MathHelper.Clamp((float)Math.Round(w * 32767f), -32767f, 32767f) & 0xFFFF) << 48;
		return num | num2 | num3 | num4;
	}
}
