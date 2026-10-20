#define DEBUG
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework.Design;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(Vector3Converter))]
[DebuggerDisplay("{DebugDisplayString,nq}")]
public struct Vector3 : IEquatable<Vector3>
{
	private static Vector3 zero;

	private static Vector3 one;

	private static Vector3 unitX;

	private static Vector3 unitY;

	private static Vector3 unitZ;

	private static Vector3 up;

	private static Vector3 down;

	private static Vector3 right;

	private static Vector3 left;

	private static Vector3 forward;

	private static Vector3 backward;

	public float X;

	public float Y;

	public float Z;

	public static Vector3 Zero => zero;

	public static Vector3 One => one;

	public static Vector3 UnitX => unitX;

	public static Vector3 UnitY => unitY;

	public static Vector3 UnitZ => unitZ;

	public static Vector3 Up => up;

	public static Vector3 Down => down;

	public static Vector3 Right => right;

	public static Vector3 Left => left;

	public static Vector3 Forward => forward;

	public static Vector3 Backward => backward;

	internal string DebugDisplayString => X + " " + Y + " " + Z;

	public Vector3(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public Vector3(float value)
	{
		X = value;
		Y = value;
		Z = value;
	}

	public Vector3(Vector2 value, float z)
	{
		X = value.X;
		Y = value.Y;
		Z = z;
	}

	public override bool Equals(object obj)
	{
		return obj is Vector3 && Equals((Vector3)obj);
	}

	public bool Equals(Vector3 other)
	{
		return X == other.X && Y == other.Y && Z == other.Z;
	}

	public override int GetHashCode()
	{
		return X.GetHashCode() + Y.GetHashCode() + Z.GetHashCode();
	}

	public float Length()
	{
		return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
	}

	public float LengthSquared()
	{
		return X * X + Y * Y + Z * Z;
	}

	public void Normalize()
	{
		float num = 1f / (float)Math.Sqrt(X * X + Y * Y + Z * Z);
		X *= num;
		Y *= num;
		Z *= num;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder(32);
		stringBuilder.Append("{X:");
		stringBuilder.Append(X);
		stringBuilder.Append(" Y:");
		stringBuilder.Append(Y);
		stringBuilder.Append(" Z:");
		stringBuilder.Append(Z);
		stringBuilder.Append("}");
		return stringBuilder.ToString();
	}

	public static Vector3 Add(Vector3 value1, Vector3 value2)
	{
		value1.X += value2.X;
		value1.Y += value2.Y;
		value1.Z += value2.Z;
		return value1;
	}

	public static void Add(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
	{
		result.X = value1.X + value2.X;
		result.Y = value1.Y + value2.Y;
		result.Z = value1.Z + value2.Z;
	}

	public static Vector3 Barycentric(Vector3 value1, Vector3 value2, Vector3 value3, float amount1, float amount2)
	{
		return new Vector3(MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2), MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2), MathHelper.Barycentric(value1.Z, value2.Z, value3.Z, amount1, amount2));
	}

	public static void Barycentric(ref Vector3 value1, ref Vector3 value2, ref Vector3 value3, float amount1, float amount2, out Vector3 result)
	{
		result.X = MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2);
		result.Y = MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2);
		result.Z = MathHelper.Barycentric(value1.Z, value2.Z, value3.Z, amount1, amount2);
	}

	public static Vector3 CatmullRom(Vector3 value1, Vector3 value2, Vector3 value3, Vector3 value4, float amount)
	{
		return new Vector3(MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount), MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount), MathHelper.CatmullRom(value1.Z, value2.Z, value3.Z, value4.Z, amount));
	}

	public static void CatmullRom(ref Vector3 value1, ref Vector3 value2, ref Vector3 value3, ref Vector3 value4, float amount, out Vector3 result)
	{
		result.X = MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount);
		result.Y = MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount);
		result.Z = MathHelper.CatmullRom(value1.Z, value2.Z, value3.Z, value4.Z, amount);
	}

	public static Vector3 Clamp(Vector3 value1, Vector3 min, Vector3 max)
	{
		return new Vector3(MathHelper.Clamp(value1.X, min.X, max.X), MathHelper.Clamp(value1.Y, min.Y, max.Y), MathHelper.Clamp(value1.Z, min.Z, max.Z));
	}

	public static void Clamp(ref Vector3 value1, ref Vector3 min, ref Vector3 max, out Vector3 result)
	{
		result.X = MathHelper.Clamp(value1.X, min.X, max.X);
		result.Y = MathHelper.Clamp(value1.Y, min.Y, max.Y);
		result.Z = MathHelper.Clamp(value1.Z, min.Z, max.Z);
	}

	public static Vector3 Cross(Vector3 vector1, Vector3 vector2)
	{
		Cross(ref vector1, ref vector2, out vector1);
		return vector1;
	}

	public static void Cross(ref Vector3 vector1, ref Vector3 vector2, out Vector3 result)
	{
		float x = vector1.Y * vector2.Z - vector2.Y * vector1.Z;
		float y = 0f - (vector1.X * vector2.Z - vector2.X * vector1.Z);
		float z = vector1.X * vector2.Y - vector2.X * vector1.Y;
		result.X = x;
		result.Y = y;
		result.Z = z;
	}

	public static float Distance(Vector3 vector1, Vector3 vector2)
	{
		DistanceSquared(ref vector1, ref vector2, out var result);
		return (float)Math.Sqrt(result);
	}

	public static void Distance(ref Vector3 value1, ref Vector3 value2, out float result)
	{
		DistanceSquared(ref value1, ref value2, out result);
		result = (float)Math.Sqrt(result);
	}

	public static float DistanceSquared(Vector3 value1, Vector3 value2)
	{
		return (value1.X - value2.X) * (value1.X - value2.X) + (value1.Y - value2.Y) * (value1.Y - value2.Y) + (value1.Z - value2.Z) * (value1.Z - value2.Z);
	}

	public static void DistanceSquared(ref Vector3 value1, ref Vector3 value2, out float result)
	{
		result = (value1.X - value2.X) * (value1.X - value2.X) + (value1.Y - value2.Y) * (value1.Y - value2.Y) + (value1.Z - value2.Z) * (value1.Z - value2.Z);
	}

	public static Vector3 Divide(Vector3 value1, Vector3 value2)
	{
		value1.X /= value2.X;
		value1.Y /= value2.Y;
		value1.Z /= value2.Z;
		return value1;
	}

	public static void Divide(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
	{
		result.X = value1.X / value2.X;
		result.Y = value1.Y / value2.Y;
		result.Z = value1.Z / value2.Z;
	}

	public static Vector3 Divide(Vector3 value1, float value2)
	{
		float num = 1f / value2;
		value1.X *= num;
		value1.Y *= num;
		value1.Z *= num;
		return value1;
	}

	public static void Divide(ref Vector3 value1, float value2, out Vector3 result)
	{
		float num = 1f / value2;
		result.X = value1.X * num;
		result.Y = value1.Y * num;
		result.Z = value1.Z * num;
	}

	public static float Dot(Vector3 vector1, Vector3 vector2)
	{
		return vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z;
	}

	public static void Dot(ref Vector3 vector1, ref Vector3 vector2, out float result)
	{
		result = vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z;
	}

	public static Vector3 Hermite(Vector3 value1, Vector3 tangent1, Vector3 value2, Vector3 tangent2, float amount)
	{
		Vector3 result = default;
		Hermite(ref value1, ref tangent1, ref value2, ref tangent2, amount, out result);
		return result;
	}

	public static void Hermite(ref Vector3 value1, ref Vector3 tangent1, ref Vector3 value2, ref Vector3 tangent2, float amount, out Vector3 result)
	{
		result.X = MathHelper.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount);
		result.Y = MathHelper.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount);
		result.Z = MathHelper.Hermite(value1.Z, tangent1.Z, value2.Z, tangent2.Z, amount);
	}

	public static Vector3 Lerp(Vector3 value1, Vector3 value2, float amount)
	{
		return new Vector3(MathHelper.Lerp(value1.X, value2.X, amount), MathHelper.Lerp(value1.Y, value2.Y, amount), MathHelper.Lerp(value1.Z, value2.Z, amount));
	}

	public static void Lerp(ref Vector3 value1, ref Vector3 value2, float amount, out Vector3 result)
	{
		result.X = MathHelper.Lerp(value1.X, value2.X, amount);
		result.Y = MathHelper.Lerp(value1.Y, value2.Y, amount);
		result.Z = MathHelper.Lerp(value1.Z, value2.Z, amount);
	}

	public static Vector3 Max(Vector3 value1, Vector3 value2)
	{
		return new Vector3(MathHelper.Max(value1.X, value2.X), MathHelper.Max(value1.Y, value2.Y), MathHelper.Max(value1.Z, value2.Z));
	}

	public static void Max(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
	{
		result.X = MathHelper.Max(value1.X, value2.X);
		result.Y = MathHelper.Max(value1.Y, value2.Y);
		result.Z = MathHelper.Max(value1.Z, value2.Z);
	}

	public static Vector3 Min(Vector3 value1, Vector3 value2)
	{
		return new Vector3(MathHelper.Min(value1.X, value2.X), MathHelper.Min(value1.Y, value2.Y), MathHelper.Min(value1.Z, value2.Z));
	}

	public static void Min(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
	{
		result.X = MathHelper.Min(value1.X, value2.X);
		result.Y = MathHelper.Min(value1.Y, value2.Y);
		result.Z = MathHelper.Min(value1.Z, value2.Z);
	}

	public static Vector3 Multiply(Vector3 value1, Vector3 value2)
	{
		value1.X *= value2.X;
		value1.Y *= value2.Y;
		value1.Z *= value2.Z;
		return value1;
	}

	public static Vector3 Multiply(Vector3 value1, float scaleFactor)
	{
		value1.X *= scaleFactor;
		value1.Y *= scaleFactor;
		value1.Z *= scaleFactor;
		return value1;
	}

	public static void Multiply(ref Vector3 value1, float scaleFactor, out Vector3 result)
	{
		result.X = value1.X * scaleFactor;
		result.Y = value1.Y * scaleFactor;
		result.Z = value1.Z * scaleFactor;
	}

	public static void Multiply(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
	{
		result.X = value1.X * value2.X;
		result.Y = value1.Y * value2.Y;
		result.Z = value1.Z * value2.Z;
	}

	public static Vector3 Negate(Vector3 value)
	{
		value = new Vector3(0f - value.X, 0f - value.Y, 0f - value.Z);
		return value;
	}

	public static void Negate(ref Vector3 value, out Vector3 result)
	{
		result.X = 0f - value.X;
		result.Y = 0f - value.Y;
		result.Z = 0f - value.Z;
	}

	public static Vector3 Normalize(Vector3 value)
	{
		float num = 1f / (float)Math.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z);
		return new Vector3(value.X * num, value.Y * num, value.Z * num);
	}

	public static void Normalize(ref Vector3 value, out Vector3 result)
	{
		float num = 1f / (float)Math.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z);
		result.X = value.X * num;
		result.Y = value.Y * num;
		result.Z = value.Z * num;
	}

	public static Vector3 Reflect(Vector3 vector, Vector3 normal)
	{
		float num = vector.X * normal.X + vector.Y * normal.Y + vector.Z * normal.Z;
		return new Vector3
		{
			X = vector.X - 2f * normal.X * num,
			Y = vector.Y - 2f * normal.Y * num,
			Z = vector.Z - 2f * normal.Z * num
		};
	}

	public static void Reflect(ref Vector3 vector, ref Vector3 normal, out Vector3 result)
	{
		float num = vector.X * normal.X + vector.Y * normal.Y + vector.Z * normal.Z;
		result.X = vector.X - 2f * normal.X * num;
		result.Y = vector.Y - 2f * normal.Y * num;
		result.Z = vector.Z - 2f * normal.Z * num;
	}

	public static Vector3 SmoothStep(Vector3 value1, Vector3 value2, float amount)
	{
		return new Vector3(MathHelper.SmoothStep(value1.X, value2.X, amount), MathHelper.SmoothStep(value1.Y, value2.Y, amount), MathHelper.SmoothStep(value1.Z, value2.Z, amount));
	}

	public static void SmoothStep(ref Vector3 value1, ref Vector3 value2, float amount, out Vector3 result)
	{
		result.X = MathHelper.SmoothStep(value1.X, value2.X, amount);
		result.Y = MathHelper.SmoothStep(value1.Y, value2.Y, amount);
		result.Z = MathHelper.SmoothStep(value1.Z, value2.Z, amount);
	}

	public static Vector3 Subtract(Vector3 value1, Vector3 value2)
	{
		value1.X -= value2.X;
		value1.Y -= value2.Y;
		value1.Z -= value2.Z;
		return value1;
	}

	public static void Subtract(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
	{
		result.X = value1.X - value2.X;
		result.Y = value1.Y - value2.Y;
		result.Z = value1.Z - value2.Z;
	}

	public static Vector3 Transform(Vector3 position, Matrix matrix)
	{
		Transform(ref position, ref matrix, out position);
		return position;
	}

	public static void Transform(ref Vector3 position, ref Matrix matrix, out Vector3 result)
	{
		float x = position.X * matrix.M11 + position.Y * matrix.M21 + position.Z * matrix.M31 + matrix.M41;
		float y = position.X * matrix.M12 + position.Y * matrix.M22 + position.Z * matrix.M32 + matrix.M42;
		float z = position.X * matrix.M13 + position.Y * matrix.M23 + position.Z * matrix.M33 + matrix.M43;
		result.X = x;
		result.Y = y;
		result.Z = z;
	}

	public static void Transform(Vector3[] sourceArray, ref Matrix matrix, Vector3[] destinationArray)
	{
		Debug.Assert(destinationArray.Length >= sourceArray.Length, "The destination array is smaller than the source array.");
		for (int i = 0; i < sourceArray.Length; i++)
		{
			Vector3 vector = sourceArray[i];
			destinationArray[i] = new Vector3(vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31 + matrix.M41, vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32 + matrix.M42, vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33 + matrix.M43);
		}
	}

	public static void Transform(Vector3[] sourceArray, int sourceIndex, ref Matrix matrix, Vector3[] destinationArray, int destinationIndex, int length)
	{
		Debug.Assert(sourceArray.Length - sourceIndex >= length, "The source array is too small for the given sourceIndex and length.");
		Debug.Assert(destinationArray.Length - destinationIndex >= length, "The destination array is too small for the given destinationIndex and length.");
		for (int i = 0; i < length; i++)
		{
			Vector3 vector = sourceArray[sourceIndex + i];
			destinationArray[destinationIndex + i] = new Vector3(vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31 + matrix.M41, vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32 + matrix.M42, vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33 + matrix.M43);
		}
	}

	public static Vector3 Transform(Vector3 value, Quaternion rotation)
	{
		Transform(ref value, ref rotation, out var result);
		return result;
	}

	public static void Transform(ref Vector3 value, ref Quaternion rotation, out Vector3 result)
	{
		float num = 2f * (rotation.Y * value.Z - rotation.Z * value.Y);
		float num2 = 2f * (rotation.Z * value.X - rotation.X * value.Z);
		float num3 = 2f * (rotation.X * value.Y - rotation.Y * value.X);
		result.X = value.X + num * rotation.W + (rotation.Y * num3 - rotation.Z * num2);
		result.Y = value.Y + num2 * rotation.W + (rotation.Z * num - rotation.X * num3);
		result.Z = value.Z + num3 * rotation.W + (rotation.X * num2 - rotation.Y * num);
	}

	public static void Transform(Vector3[] sourceArray, ref Quaternion rotation, Vector3[] destinationArray)
	{
		Debug.Assert(destinationArray.Length >= sourceArray.Length, "The destination array is smaller than the source array.");
		for (int i = 0; i < sourceArray.Length; i++)
		{
			Vector3 vector = sourceArray[i];
			float num = 2f * (rotation.Y * vector.Z - rotation.Z * vector.Y);
			float num2 = 2f * (rotation.Z * vector.X - rotation.X * vector.Z);
			float num3 = 2f * (rotation.X * vector.Y - rotation.Y * vector.X);
			destinationArray[i] = new Vector3(vector.X + num * rotation.W + (rotation.Y * num3 - rotation.Z * num2), vector.Y + num2 * rotation.W + (rotation.Z * num - rotation.X * num3), vector.Z + num3 * rotation.W + (rotation.X * num2 - rotation.Y * num));
		}
	}

	public static void Transform(Vector3[] sourceArray, int sourceIndex, ref Quaternion rotation, Vector3[] destinationArray, int destinationIndex, int length)
	{
		Debug.Assert(sourceArray.Length - sourceIndex >= length, "The source array is too small for the given sourceIndex and length.");
		Debug.Assert(destinationArray.Length - destinationIndex >= length, "The destination array is too small for the given destinationIndex and length.");
		for (int i = 0; i < length; i++)
		{
			Vector3 vector = sourceArray[sourceIndex + i];
			float num = 2f * (rotation.Y * vector.Z - rotation.Z * vector.Y);
			float num2 = 2f * (rotation.Z * vector.X - rotation.X * vector.Z);
			float num3 = 2f * (rotation.X * vector.Y - rotation.Y * vector.X);
			destinationArray[destinationIndex + i] = new Vector3(vector.X + num * rotation.W + (rotation.Y * num3 - rotation.Z * num2), vector.Y + num2 * rotation.W + (rotation.Z * num - rotation.X * num3), vector.Z + num3 * rotation.W + (rotation.X * num2 - rotation.Y * num));
		}
	}

	public static Vector3 TransformNormal(Vector3 normal, Matrix matrix)
	{
		TransformNormal(ref normal, ref matrix, out normal);
		return normal;
	}

	public static void TransformNormal(ref Vector3 normal, ref Matrix matrix, out Vector3 result)
	{
		float x = normal.X * matrix.M11 + normal.Y * matrix.M21 + normal.Z * matrix.M31;
		float y = normal.X * matrix.M12 + normal.Y * matrix.M22 + normal.Z * matrix.M32;
		float z = normal.X * matrix.M13 + normal.Y * matrix.M23 + normal.Z * matrix.M33;
		result.X = x;
		result.Y = y;
		result.Z = z;
	}

	public static void TransformNormal(Vector3[] sourceArray, ref Matrix matrix, Vector3[] destinationArray)
	{
		Debug.Assert(destinationArray.Length >= sourceArray.Length, "The destination array is smaller than the source array.");
		for (int i = 0; i < sourceArray.Length; i++)
		{
			Vector3 vector = sourceArray[i];
			destinationArray[i].X = vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31;
			destinationArray[i].Y = vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32;
			destinationArray[i].Z = vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33;
		}
	}

	public static void TransformNormal(Vector3[] sourceArray, int sourceIndex, ref Matrix matrix, Vector3[] destinationArray, int destinationIndex, int length)
	{
		if (sourceArray == null)
		{
			throw new ArgumentNullException("sourceArray");
		}
		if (destinationArray == null)
		{
			throw new ArgumentNullException("destinationArray");
		}
		if (sourceIndex + length > sourceArray.Length)
		{
			throw new ArgumentException("the combination of sourceIndex and length was greater than sourceArray.Length");
		}
		if (destinationIndex + length > destinationArray.Length)
		{
			throw new ArgumentException("destinationArray is too small to contain the result");
		}
		for (int i = 0; i < length; i++)
		{
			Vector3 vector = sourceArray[i + sourceIndex];
			destinationArray[i + destinationIndex].X = vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31;
			destinationArray[i + destinationIndex].Y = vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32;
			destinationArray[i + destinationIndex].Z = vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33;
		}
	}

	public static bool operator ==(Vector3 value1, Vector3 value2)
	{
		return value1.X == value2.X && value1.Y == value2.Y && value1.Z == value2.Z;
	}

	public static bool operator !=(Vector3 value1, Vector3 value2)
	{
		return !(value1 == value2);
	}

	public static Vector3 operator +(Vector3 value1, Vector3 value2)
	{
		value1.X += value2.X;
		value1.Y += value2.Y;
		value1.Z += value2.Z;
		return value1;
	}

	public static Vector3 operator -(Vector3 value)
	{
		value = new Vector3(0f - value.X, 0f - value.Y, 0f - value.Z);
		return value;
	}

	public static Vector3 operator -(Vector3 value1, Vector3 value2)
	{
		value1.X -= value2.X;
		value1.Y -= value2.Y;
		value1.Z -= value2.Z;
		return value1;
	}

	public static Vector3 operator *(Vector3 value1, Vector3 value2)
	{
		value1.X *= value2.X;
		value1.Y *= value2.Y;
		value1.Z *= value2.Z;
		return value1;
	}

	public static Vector3 operator *(Vector3 value, float scaleFactor)
	{
		value.X *= scaleFactor;
		value.Y *= scaleFactor;
		value.Z *= scaleFactor;
		return value;
	}

	public static Vector3 operator *(float scaleFactor, Vector3 value)
	{
		value.X *= scaleFactor;
		value.Y *= scaleFactor;
		value.Z *= scaleFactor;
		return value;
	}

	public static Vector3 operator /(Vector3 value1, Vector3 value2)
	{
		value1.X /= value2.X;
		value1.Y /= value2.Y;
		value1.Z /= value2.Z;
		return value1;
	}

	public static Vector3 operator /(Vector3 value, float divider)
	{
		float num = 1f / divider;
		value.X *= num;
		value.Y *= num;
		value.Z *= num;
		return value;
	}

	static Vector3()
	{
		zero = new Vector3(0f, 0f, 0f);
		one = new Vector3(1f, 1f, 1f);
		unitX = new Vector3(1f, 0f, 0f);
		unitY = new Vector3(0f, 1f, 0f);
		unitZ = new Vector3(0f, 0f, 1f);
		up = new Vector3(0f, 1f, 0f);
		down = new Vector3(0f, -1f, 0f);
		right = new Vector3(1f, 0f, 0f);
		left = new Vector3(-1f, 0f, 0f);
		forward = new Vector3(0f, 0f, -1f);
		backward = new Vector3(0f, 0f, 1f);
	}
}
