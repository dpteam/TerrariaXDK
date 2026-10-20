using System;

namespace Microsoft.Xna.Framework;

[Serializable]
public class Curve
{
	public bool IsConstant => Keys.Count <= 1;

	public CurveKeyCollection Keys { get; private set; }

	public CurveLoopType PostLoop { get; set; }

	public CurveLoopType PreLoop { get; set; }

	public Curve()
	{
		Keys = new CurveKeyCollection();
	}

	private Curve(CurveKeyCollection keys)
	{
		Keys = keys;
	}

	public Curve Clone()
	{
		Curve curve = new Curve(Keys.Clone());
		curve.PreLoop = PreLoop;
		curve.PostLoop = PostLoop;
		return curve;
	}

	public float Evaluate(float position)
	{
		if (Keys.Count == 0)
		{
			return 0f;
		}
		if (Keys.Count == 1)
		{
			return Keys[0].Value;
		}
		CurveKey curveKey = Keys[0];
		CurveKey curveKey2 = Keys[Keys.Count - 1];
		if (position < curveKey.Position)
		{
			switch (PreLoop)
			{
			case CurveLoopType.Constant:
				return curveKey.Value;
			case CurveLoopType.Linear:
				return curveKey.Value - curveKey.TangentIn * (curveKey.Position - position);
			case CurveLoopType.Cycle:
			{
				int numberOfCycle2 = GetNumberOfCycle(position);
				float position3 = position - (float)numberOfCycle2 * (curveKey2.Position - curveKey.Position);
				return GetCurvePosition(position3);
			}
			case CurveLoopType.CycleOffset:
			{
				int numberOfCycle3 = GetNumberOfCycle(position);
				float position4 = position - (float)numberOfCycle3 * (curveKey2.Position - curveKey.Position);
				return GetCurvePosition(position4) + (float)numberOfCycle3 * (curveKey2.Value - curveKey.Value);
			}
			case CurveLoopType.Oscillate:
			{
				int numberOfCycle = GetNumberOfCycle(position);
				float position2 = ((0f != (float)numberOfCycle % 2f) ? (curveKey2.Position - position + curveKey.Position + (float)numberOfCycle * (curveKey2.Position - curveKey.Position)) : (position - (float)numberOfCycle * (curveKey2.Position - curveKey.Position)));
				return GetCurvePosition(position2);
			}
			}
		}
		else if (position > curveKey2.Position)
		{
			switch (PostLoop)
			{
			case CurveLoopType.Constant:
				return curveKey2.Value;
			case CurveLoopType.Linear:
				return curveKey2.Value + curveKey.TangentOut * (position - curveKey2.Position);
			case CurveLoopType.Cycle:
			{
				int numberOfCycle5 = GetNumberOfCycle(position);
				float position5 = position - (float)numberOfCycle5 * (curveKey2.Position - curveKey.Position);
				return GetCurvePosition(position5);
			}
			case CurveLoopType.CycleOffset:
			{
				int numberOfCycle6 = GetNumberOfCycle(position);
				float position6 = position - (float)numberOfCycle6 * (curveKey2.Position - curveKey.Position);
				return GetCurvePosition(position6) + (float)numberOfCycle6 * (curveKey2.Value - curveKey.Value);
			}
			case CurveLoopType.Oscillate:
			{
				int numberOfCycle4 = GetNumberOfCycle(position);
				float num = position - (float)numberOfCycle4 * (curveKey2.Position - curveKey.Position);
				num = ((0f != (float)numberOfCycle4 % 2f) ? (curveKey2.Position - position + curveKey.Position + (float)numberOfCycle4 * (curveKey2.Position - curveKey.Position)) : (position - (float)numberOfCycle4 * (curveKey2.Position - curveKey.Position)));
				return GetCurvePosition(num);
			}
			}
		}
		return GetCurvePosition(position);
	}

	public void ComputeTangents(CurveTangent tangentType)
	{
		ComputeTangents(tangentType, tangentType);
	}

	public void ComputeTangents(CurveTangent tangentInType, CurveTangent tangentOutType)
	{
		for (int i = 0; i < Keys.Count; i++)
		{
			ComputeTangent(i, tangentInType, tangentOutType);
		}
	}

	public void ComputeTangent(int keyIndex, CurveTangent tangentType)
	{
		ComputeTangent(keyIndex, tangentType, tangentType);
	}

	public void ComputeTangent(int keyIndex, CurveTangent tangentInType, CurveTangent tangentOutType)
	{
		CurveKey curveKey = Keys[keyIndex];
		float num2;
		float position;
		float num = (num2 = (position = curveKey.Position));
		float num4;
		float value;
		float num3 = (num4 = (value = curveKey.Value));
		if (keyIndex > 0)
		{
			num = Keys[keyIndex - 1].Position;
			num3 = Keys[keyIndex - 1].Value;
		}
		if (keyIndex < Keys.Count - 1)
		{
			position = Keys[keyIndex + 1].Position;
			value = Keys[keyIndex + 1].Value;
		}
		switch (tangentInType)
		{
		case CurveTangent.Flat:
			curveKey.TangentIn = 0f;
			break;
		case CurveTangent.Linear:
			curveKey.TangentIn = num4 - num3;
			break;
		case CurveTangent.Smooth:
		{
			float num5 = position - num;
			if (MathHelper.WithinEpsilon(num5, 0f))
			{
				curveKey.TangentIn = 0f;
			}
			else
			{
				curveKey.TangentIn = (value - num3) * ((num2 - num) / num5);
			}
			break;
		}
		}
		switch (tangentOutType)
		{
		case CurveTangent.Flat:
			curveKey.TangentOut = 0f;
			break;
		case CurveTangent.Linear:
			curveKey.TangentOut = value - num4;
			break;
		case CurveTangent.Smooth:
		{
			float num6 = position - num;
			if (Math.Abs(num6) < float.Epsilon)
			{
				curveKey.TangentOut = 0f;
			}
			else
			{
				curveKey.TangentOut = (value - num3) * ((position - num2) / num6);
			}
			break;
		}
		}
	}

	private int GetNumberOfCycle(float position)
	{
		float num = (position - Keys[0].Position) / (Keys[Keys.Count - 1].Position - Keys[0].Position);
		if (num < 0f)
		{
			num--;
		}
		return (int)num;
	}

	private float GetCurvePosition(float position)
	{
		CurveKey curveKey = Keys[0];
		for (int i = 1; i < Keys.Count; i++)
		{
			CurveKey curveKey2 = Keys[i];
			if (curveKey2.Position >= position)
			{
				if (curveKey.Continuity == CurveContinuity.Step)
				{
					if (position >= 1f)
					{
						return curveKey2.Value;
					}
					return curveKey.Value;
				}
				float num = (position - curveKey.Position) / (curveKey2.Position - curveKey.Position);
				float num2 = num * num;
				float num3 = num2 * num;
				return (2f * num3 - 3f * num2 + 1f) * curveKey.Value + (num3 - 2f * num2 + num) * curveKey.TangentOut + (3f * num2 - 2f * num3) * curveKey2.Value + (num3 - num2) * curveKey2.TangentIn;
			}
			curveKey = curveKey2;
		}
		return 0f;
	}
}
