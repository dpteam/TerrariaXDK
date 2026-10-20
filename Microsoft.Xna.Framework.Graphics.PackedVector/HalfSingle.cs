using System;

namespace Microsoft.Xna.Framework.Graphics.PackedVector;

public struct HalfSingle : IPackedVector<ushort>, IPackedVector, IEquatable<HalfSingle>
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

	public HalfSingle(float single)
	{
		packedValue = HalfTypeHelper.Convert(single);
	}

	public float ToSingle()
	{
		return HalfTypeHelper.Convert(packedValue);
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = HalfTypeHelper.Convert(vector.X);
	}

	Vector4 IPackedVector.ToVector4()
	{
		return new Vector4(ToSingle(), 0f, 0f, 1f);
	}

	public override bool Equals(object obj)
	{
		return obj is HalfSingle && Equals((HalfSingle)obj);
	}

	public bool Equals(HalfSingle other)
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

	public static bool operator ==(HalfSingle lhs, HalfSingle rhs)
	{
		return lhs.packedValue == rhs.packedValue;
	}

	public static bool operator !=(HalfSingle lhs, HalfSingle rhs)
	{
		return lhs.packedValue != rhs.packedValue;
	}
}
