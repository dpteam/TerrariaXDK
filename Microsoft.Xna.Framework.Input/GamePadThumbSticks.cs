namespace Microsoft.Xna.Framework.Input;

public struct GamePadThumbSticks
{
	private Vector2 left;

	private Vector2 right;

	public Vector2 Left => left;

	public Vector2 Right => right;

	public GamePadThumbSticks(Vector2 leftPosition, Vector2 rightPosition)
	{
		left = leftPosition;
		right = rightPosition;
		ApplySquareClamp();
	}

	internal GamePadThumbSticks(Vector2 leftPosition, Vector2 rightPosition, GamePadDeadZone deadZoneMode)
	{
		left = leftPosition;
		right = rightPosition;
		ApplyDeadZone(deadZoneMode);
		if (deadZoneMode == GamePadDeadZone.Circular)
		{
			ApplyCircularClamp();
		}
		else
		{
			ApplySquareClamp();
		}
	}

	private void ApplyDeadZone(GamePadDeadZone dz)
	{
		switch (dz)
		{
		case GamePadDeadZone.None:
			break;
		case GamePadDeadZone.IndependentAxes:
			left.X = GamePad.ExcludeAxisDeadZone(left.X, 0.23953247f);
			left.Y = GamePad.ExcludeAxisDeadZone(left.Y, 0.23953247f);
			right.X = GamePad.ExcludeAxisDeadZone(right.X, 0.26516724f);
			right.Y = GamePad.ExcludeAxisDeadZone(right.Y, 0.26516724f);
			break;
		case GamePadDeadZone.Circular:
			left = ExcludeCircularDeadZone(left, 0.23953247f);
			right = ExcludeCircularDeadZone(right, 0.26516724f);
			break;
		}
	}

	private void ApplySquareClamp()
	{
		left.X = MathHelper.Clamp(left.X, -1f, 1f);
		left.Y = MathHelper.Clamp(left.Y, -1f, 1f);
		right.X = MathHelper.Clamp(right.X, -1f, 1f);
		right.Y = MathHelper.Clamp(right.Y, -1f, 1f);
	}

	private void ApplyCircularClamp()
	{
		if (left.LengthSquared() > 1f)
		{
			left.Normalize();
		}
		if (right.LengthSquared() > 1f)
		{
			right.Normalize();
		}
	}

	private static Vector2 ExcludeCircularDeadZone(Vector2 value, float deadZone)
	{
		float num = value.Length();
		if (num <= deadZone)
		{
			return Vector2.Zero;
		}
		float num2 = (num - deadZone) / (1f - deadZone);
		return value * (num2 / num);
	}

	public static bool operator ==(GamePadThumbSticks left, GamePadThumbSticks right)
	{
		return left.left == right.left && left.right == right.right;
	}

	public static bool operator !=(GamePadThumbSticks left, GamePadThumbSticks right)
	{
		return !(left == right);
	}

	public override bool Equals(object obj)
	{
		return obj is GamePadThumbSticks && this == (GamePadThumbSticks)obj;
	}

	public override int GetHashCode()
	{
		return Left.GetHashCode() + 37 * Right.GetHashCode();
	}
}
