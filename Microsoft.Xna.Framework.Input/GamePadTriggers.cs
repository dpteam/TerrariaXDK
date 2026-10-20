namespace Microsoft.Xna.Framework.Input;

public struct GamePadTriggers
{
	private float left;

	private float right;

	public float Left => left;

	public float Right => right;

	public GamePadTriggers(float leftTrigger, float rightTrigger)
	{
		left = MathHelper.Clamp(leftTrigger, 0f, 1f);
		right = MathHelper.Clamp(rightTrigger, 0f, 1f);
	}

	internal GamePadTriggers(float leftTrigger, float rightTrigger, GamePadDeadZone deadZoneMode)
	{
		if (deadZoneMode == GamePadDeadZone.None)
		{
			left = MathHelper.Clamp(leftTrigger, 0f, 1f);
			right = MathHelper.Clamp(rightTrigger, 0f, 1f);
		}
		else
		{
			left = MathHelper.Clamp(GamePad.ExcludeAxisDeadZone(leftTrigger, 30f / 255f), 0f, 1f);
			right = MathHelper.Clamp(GamePad.ExcludeAxisDeadZone(rightTrigger, 30f / 255f), 0f, 1f);
		}
	}

	public static bool operator ==(GamePadTriggers left, GamePadTriggers right)
	{
		return MathHelper.WithinEpsilon(left.left, right.left) && MathHelper.WithinEpsilon(left.right, right.right);
	}

	public static bool operator !=(GamePadTriggers left, GamePadTriggers right)
	{
		return !(left == right);
	}

	public override bool Equals(object obj)
	{
		return obj is GamePadTriggers && this == (GamePadTriggers)obj;
	}

	public override int GetHashCode()
	{
		return Left.GetHashCode() + Right.GetHashCode();
	}
}
