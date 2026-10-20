using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct HalfVector4 : IPackedVector<ulong>, IPackedVector, IEquatable<HalfVector4>
{
	private ulong packedValue;
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

	public HalfVector4(float x, float y, float z, float w)
	{
		packedValue = Pack(x, y, z, w);
	}

	public HalfVector4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public Vector4 ToVector4()
	{
		return new Vector4(HalfTypeHelper.Convert((ushort)packedValue), HalfTypeHelper.Convert((ushort)(packedValue >> 16)), HalfTypeHelper.Convert((ushort)(packedValue >> 32)), HalfTypeHelper.Convert((ushort)(packedValue >> 48)));
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = Pack(vector.X, vector.Y, vector.Z, vector.W);
	}

	public override string ToString()
	{
		return packedValue.ToString("X");
	}

	public override int GetHashCode()
	{
		return packedValue.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		return obj is HalfVector4 && Equals((HalfVector4)obj);
	}

	public bool Equals(HalfVector4 other)
	{
		return packedValue.Equals(other.packedValue);
	}

	public static bool operator ==(HalfVector4 a, HalfVector4 b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(HalfVector4 a, HalfVector4 b)
	{
		return !a.Equals(b);
	}

	private static ulong Pack(float x, float y, float z, float w)
	{
		return HalfTypeHelper.Convert(x) | ((ulong)HalfTypeHelper.Convert(y) << 16) | ((ulong)HalfTypeHelper.Convert(z) << 32) | ((ulong)HalfTypeHelper.Convert(w) << 48);
	}
}
