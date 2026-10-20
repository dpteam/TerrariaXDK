using System;

namespace Microsoft.Xna.Framework;

public static class MathHelper
{
	public const float E = (float)Math.E;

	public const float Log10E = 0.4342945f;

	public const float Log2E = 1.442695f;

	public const float Pi = (float)Math.PI;

	public const float PiOver2 = (float)Math.PI / 2f;

	public const float PiOver4 = (float)Math.PI / 4f;

	public const float TwoPi = (float)Math.PI * 2f;

	internal static readonly float MachineEpsilonFloat = GetMachineEpsilonFloat();

	public static float Barycentric(float value1, float value2, float value3, float amount1, float amount2)
	{
		return value1 + (value2 - value1) * amount1 + (value3 - value1) * amount2;
	}

	public static float CatmullRom(float value1, float value2, float value3, float value4, float amount)
	{
		double num = amount * amount;
		double num2 = num * (double)amount;
		return (float)(0.5 * (2.0 * (double)value2 + (double)((value3 - value1) * amount) + (2.0 * (double)value1 - 5.0 * (double)value2 + 4.0 * (double)value3 - (double)value4) * num + (3.0 * (double)value2 - (double)value1 - 3.0 * (double)value3 + (double)value4) * num2));
	}

	public static float Clamp(float value, float min, float max)
	{
		value = ((value > max) ? max : value);
		value = ((value < min) ? min : value);
		return value;
	}

	public static float Distance(float value1, float value2)
	{
		return Math.Abs(value1 - value2);
	}

	public static float Hermite(float value1, float tangent1, float value2, float tangent2, float amount)
	{
		double num = value1;
		double num2 = value2;
		double num3 = tangent1;
		double num4 = tangent2;
		double num5 = amount;
		double num6 = num5 * num5 * num5;
		double num7 = num5 * num5;
		double num8;
		if (WithinEpsilon(amount, 0f))
		{
			num8 = value1;
		}
		else
		{
			num8 = ((!WithinEpsilon(amount, 1f)) ? ((2.0 * num - 2.0 * num2 + num4 + num3) * num6 + (3.0 * num2 - 3.0 * num - 2.0 * num3 - num4) * num7 + num3 * num5 + num) : ((double)value2));
		}
		return (float)num8;
	}

	public static float Lerp(float value1, float value2, float amount)
	{
		return value1 + (value2 - value1) * amount;
	}

	public static float Max(float value1, float value2)
	{
		return (value1 > value2) ? value1 : value2;
	}

	public static float Min(float value1, float value2)
	{
		return (value1 < value2) ? value1 : value2;
	}

	public static float SmoothStep(float value1, float value2, float amount)
	{
		float amount2 = Clamp(amount, 0f, 1f);
		return Hermite(value1, 0f, value2, 0f, amount2);
	}

	public static float ToDegrees(float radians)
	{
		return (float)((double)radians * (180.0 / Math.PI));
	}

	public static float ToRadians(float degrees)
	{
		return (float)((double)degrees * (Math.PI / 180.0));
	}

	public static float WrapAngle(float angle)
	{
		if (angle > -(float)Math.PI && angle <= (float)Math.PI)
		{
			return angle;
		}
		angle %= (float)Math.PI * 2f;
		if (angle <= -(float)Math.PI)
		{
			return angle + (float)Math.PI * 2f;
		}
		if (angle > (float)Math.PI)
		{
			return angle - (float)Math.PI * 2f;
		}
		return angle;
	}

	internal static int Clamp(int value, int min, int max)
	{
		value = ((value > max) ? max : value);
		value = ((value < min) ? min : value);
		return value;
	}

	internal static bool WithinEpsilon(float floatA, float floatB)
	{
		return Math.Abs(floatA - floatB) < MachineEpsilonFloat;
	}

	internal static int ClosestMSAAPower(int value)
	{
		if (value == 1)
		{
			return 0;
		}
		int num = value - 1;
		num |= num >> 1;
		num |= num >> 2;
		num |= num >> 4;
		num |= num >> 8;
		num |= num >> 16;
		num++;
		if (num == value)
		{
			return num;
		}
		return num >> 1;
	}

	private static float GetMachineEpsilonFloat()
	{
		float num = 1f;
		float num2;
		do
		{
			num *= 0.5f;
			num2 = 1f + num;
		}
		while (num2 > 1f);
		return num;
	}
}
