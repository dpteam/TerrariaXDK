using System;

namespace Microsoft.Xna.Framework;

[Serializable]
public class CurveKey : IEquatable<CurveKey>, IComparable<CurveKey>
{
	public CurveContinuity Continuity { get; set; }

	public float Position { get; private set; }

	public float TangentIn { get; set; }

	public float TangentOut { get; set; }

	public float Value { get; set; }

	public CurveKey(float position, float value)
		: this(position, value, 0f, 0f, CurveContinuity.Smooth)
	{
	}

	public CurveKey(float position, float value, float tangentIn, float tangentOut)
		: this(position, value, tangentIn, tangentOut, CurveContinuity.Smooth)
	{
	}

	public CurveKey(float position, float value, float tangentIn, float tangentOut, CurveContinuity continuity)
	{
		Position = position;
		Value = value;
		TangentIn = tangentIn;
		TangentOut = tangentOut;
		Continuity = continuity;
	}

	public CurveKey Clone()
	{
		return new CurveKey(Position, Value, TangentIn, TangentOut, Continuity);
	}

	public int CompareTo(CurveKey other)
	{
		return Position.CompareTo(other.Position);
	}

	public bool Equals(CurveKey other)
	{
		return this == other;
	}

	public static bool operator !=(CurveKey a, CurveKey b)
	{
		return !(a == b);
	}

	public static bool operator ==(CurveKey a, CurveKey b)
	{
		if (object.Equals(a, null))
		{
			return object.Equals(b, null);
		}
		if (object.Equals(b, null))
		{
			return object.Equals(a, null);
		}
		return a.Position == b.Position && a.Value == b.Value && a.TangentIn == b.TangentIn && a.TangentOut == b.TangentOut && a.Continuity == b.Continuity;
	}

	public override bool Equals(object obj)
	{
		return obj as CurveKey == this;
	}

	public override int GetHashCode()
	{
		return Position.GetHashCode() ^ Value.GetHashCode() ^ TangentIn.GetHashCode() ^ TangentOut.GetHashCode() ^ Continuity.GetHashCode();
	}
}
