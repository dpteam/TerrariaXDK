using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct NormalizedShort2 : IPackedVector<uint>, IPackedVector, IEquatable<NormalizedShort2>
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

	public NormalizedShort2(Vector2 vector)
	{
		packedValue = Pack(vector.X, vector.Y);
	}

	public NormalizedShort2(float x, float y)
	{
		packedValue = Pack(x, y);
	}

	public Vector2 ToVector2()
	{
		return new Vector2((float)(short)(packedValue & 0xFFFF) / 32767f, (float)(short)(packedValue >> 16) / 32767f);
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y);
	}

	Vector4 IPackedVector.ToVector4()
	{
		return new Vector4(ToVector2(), 0f, 1f);
	}

	public static bool operator !=(NormalizedShort2 a, NormalizedShort2 b)
	{
		return !a.Equals(b);
	}

	public static bool operator ==(NormalizedShort2 a, NormalizedShort2 b)
	{
		return a.Equals(b);
	}

	public override bool Equals(object obj)
	{
		return obj is NormalizedShort2 && Equals((NormalizedShort2)obj);
	}

	public bool Equals(NormalizedShort2 other)
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

	private static uint Pack(float x, float y)
	{
		uint num = (uint)((int)MathHelper.Clamp((float)Math.Round(x * 32767f), -32767f, 32767f) & 0xFFFF);
		uint num2 = (uint)(((int)MathHelper.Clamp((float)Math.Round(y * 32767f), -32767f, 32767f) & 0xFFFF) << 16);
		return num | num2;
	}
}
