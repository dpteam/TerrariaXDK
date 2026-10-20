using System;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Xna.Framework.Design;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(PlaneConverter))]
[DebuggerDisplay("{DebugDisplayString,nq}")]
public struct Plane : IEquatable<Plane>
{
	public Vector3 Normal;

	public float D;

	internal string DebugDisplayString => Normal.DebugDisplayString + " " + D;

	public Plane(Vector4 value)
		: this(new Vector3(value.X, value.Y, value.Z), value.W)
	{
	}

	public Plane(Vector3 normal, float d)
	{
		Normal = normal;
		D = d;
	}

	public Plane(Vector3 a, Vector3 b, Vector3 c)
	{
		Vector3 vector = b - a;
		Vector3 vector2 = c - a;
		Vector3 value = Vector3.Cross(vector, vector2);
		Vector3.Normalize(ref value, out Normal);
		D = 0f - Vector3.Dot(Normal, a);
	}

	public Plane(float a, float b, float c, float d)
		: this(new Vector3(a, b, c), d)
	{
	}

	public float Dot(Vector4 value)
	{
		return Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z + D * value.W;
	}

	public void Dot(ref Vector4 value, out float result)
	{
		result = Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z + D * value.W;
	}

	public float DotCoordinate(Vector3 value)
	{
		return Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z + D;
	}

	public void DotCoordinate(ref Vector3 value, out float result)
	{
		result = Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z + D;
	}

	public float DotNormal(Vector3 value)
	{
		return Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z;
	}

	public void DotNormal(ref Vector3 value, out float result)
	{
		result = Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z;
	}

	public void Normalize()
	{
		float num = Normal.Length();
		float num2 = 1f / num;
		Vector3.Multiply(ref Normal, num2, out Normal);
		D *= num2;
	}

	public PlaneIntersectionType Intersects(BoundingBox box)
	{
		return box.Intersects(this);
	}

	public void Intersects(ref BoundingBox box, out PlaneIntersectionType result)
	{
		box.Intersects(ref this, out result);
	}

	public PlaneIntersectionType Intersects(BoundingSphere sphere)
	{
		return sphere.Intersects(this);
	}

	public void Intersects(ref BoundingSphere sphere, out PlaneIntersectionType result)
	{
		sphere.Intersects(ref this, out result);
	}

	public PlaneIntersectionType Intersects(BoundingFrustum frustum)
	{
		return frustum.Intersects(this);
	}

	internal PlaneIntersectionType Intersects(ref Vector3 point)
	{
		DotCoordinate(ref point, out var result);
		if (result > 0f)
		{
			return PlaneIntersectionType.Front;
		}
		if (result < 0f)
		{
			return PlaneIntersectionType.Back;
		}
		return PlaneIntersectionType.Intersecting;
	}

	public static Plane Normalize(Plane value)
	{
		Normalize(ref value, out var result);
		return result;
	}

	public static void Normalize(ref Plane value, out Plane result)
	{
		float num = value.Normal.Length();
		float num2 = 1f / num;
		Vector3.Multiply(ref value.Normal, num2, out result.Normal);
		result.D = value.D * num2;
	}

	public static Plane Transform(Plane plane, Matrix matrix)
	{
		Transform(ref plane, ref matrix, out var result);
		return result;
	}

	public static void Transform(ref Plane plane, ref Matrix matrix, out Plane result)
	{
		Matrix.Invert(ref matrix, out var result2);
		Matrix.Transpose(ref result2, out result2);
		Vector4 vector = new Vector4(plane.Normal, plane.D);
		Vector4.Transform(ref vector, ref result2, out var result3);
		result = new Plane(result3);
	}

	public static Plane Transform(Plane plane, Quaternion rotation)
	{
		Transform(ref plane, ref rotation, out var result);
		return result;
	}

	public static void Transform(ref Plane plane, ref Quaternion rotation, out Plane result)
	{
		Vector3.Transform(ref plane.Normal, ref rotation, out result.Normal);
		result.D = plane.D;
	}

	public static bool operator !=(Plane plane1, Plane plane2)
	{
		return !plane1.Equals(plane2);
	}

	public static bool operator ==(Plane plane1, Plane plane2)
	{
		return plane1.Equals(plane2);
	}

	public override bool Equals(object obj)
	{
		return obj is Plane && Equals((Plane)obj);
	}

	public bool Equals(Plane other)
	{
		return Normal == other.Normal && D == other.D;
	}

	public override int GetHashCode()
	{
		return Normal.GetHashCode() ^ D.GetHashCode();
	}

	public override string ToString()
	{
		return "{Normal:" + Normal.ToString() + " D:" + D + "}";
	}
}
