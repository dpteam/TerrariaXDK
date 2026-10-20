namespace Microsoft.Xna.Framework.Input;

public struct GamePadDPad
{
	public ButtonState Down { get; internal set; }

	public ButtonState Left { get; internal set; }

	public ButtonState Right { get; internal set; }

	public ButtonState Up { get; internal set; }

	public GamePadDPad(ButtonState upValue, ButtonState downValue, ButtonState leftValue, ButtonState rightValue)
	{
		this = default;
		Up = upValue;
		Down = downValue;
		Left = leftValue;
		Right = rightValue;
	}

	internal static GamePadDPad FromButtonArray(params Buttons[] buttons)
	{
		ButtonState upValue = ButtonState.Released;
		ButtonState downValue = ButtonState.Released;
		ButtonState leftValue = ButtonState.Released;
		ButtonState rightValue = ButtonState.Released;
		foreach (Buttons buttons2 in buttons)
		{
			if ((buttons2 & Buttons.DPadUp) == Buttons.DPadUp)
			{
				upValue = ButtonState.Pressed;
			}
			if ((buttons2 & Buttons.DPadDown) == Buttons.DPadDown)
			{
				downValue = ButtonState.Pressed;
			}
			if ((buttons2 & Buttons.DPadLeft) == Buttons.DPadLeft)
			{
				leftValue = ButtonState.Pressed;
			}
			if ((buttons2 & Buttons.DPadRight) == Buttons.DPadRight)
			{
				rightValue = ButtonState.Pressed;
			}
		}
		return new GamePadDPad(upValue, downValue, leftValue, rightValue);
	}

	public static bool operator ==(GamePadDPad left, GamePadDPad right)
	{
		return left.Down == right.Down && left.Left == right.Left && left.Right == right.Right && left.Up == right.Up;
	}

	public static bool operator !=(GamePadDPad left, GamePadDPad right)
	{
		return !(left == right);
	}

	public override bool Equals(object obj)
	{
		return obj is GamePadDPad && this == (GamePadDPad)obj;
	}

	public override int GetHashCode()
	{
		return ((Down == ButtonState.Pressed) ? 1 : 0) + ((Left == ButtonState.Pressed) ? 2 : 0) + ((Right == ButtonState.Pressed) ? 4 : 0) + ((Up == ButtonState.Pressed) ? 8 : 0);
	}
}
