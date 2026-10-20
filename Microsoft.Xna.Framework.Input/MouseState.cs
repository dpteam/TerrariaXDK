namespace Microsoft.Xna.Framework.Input;

public struct MouseState
{
	public int X { get; internal set; }

	public int Y { get; internal set; }

	public ButtonState LeftButton { get; internal set; }

	public ButtonState RightButton { get; internal set; }

	public ButtonState MiddleButton { get; internal set; }

	public ButtonState XButton1 { get; internal set; }

	public ButtonState XButton2 { get; internal set; }

	public int ScrollWheelValue { get; internal set; }

	public MouseState(int x, int y, int scrollWheel, ButtonState leftButton, ButtonState middleButton, ButtonState rightButton, ButtonState xButton1, ButtonState xButton2)
	{
		this = default;
		X = x;
		Y = y;
		ScrollWheelValue = scrollWheel;
		LeftButton = leftButton;
		MiddleButton = middleButton;
		RightButton = rightButton;
		XButton1 = xButton1;
		XButton2 = xButton2;
	}

	public static bool operator ==(MouseState left, MouseState right)
	{
		return left.X == right.X && left.Y == right.Y && left.LeftButton == right.LeftButton && left.MiddleButton == right.MiddleButton && left.RightButton == right.RightButton && left.ScrollWheelValue == right.ScrollWheelValue;
	}

	public static bool operator !=(MouseState left, MouseState right)
	{
		return !(left == right);
	}

	public override bool Equals(object obj)
	{
		return obj is MouseState && this == (MouseState)obj;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override string ToString()
	{
		string text = string.Empty;
		if (LeftButton == ButtonState.Pressed)
		{
			text = "Left";
		}
		if (RightButton == ButtonState.Pressed)
		{
			if (text.Length > 0)
			{
				text += " ";
			}
			text += "Right";
		}
		if (MiddleButton == ButtonState.Pressed)
		{
			if (text.Length > 0)
			{
				text += " ";
			}
			text += "Middle";
		}
		if (XButton1 == ButtonState.Pressed)
		{
			if (text.Length > 0)
			{
				text += " ";
			}
			text += "XButton1";
		}
		if (XButton2 == ButtonState.Pressed)
		{
			if (text.Length > 0)
			{
				text += " ";
			}
			text += "XButton2";
		}
		if (string.IsNullOrEmpty(text))
		{
			text = "None";
		}
		return $"[MouseState X={X}, Y={Y}, Buttons={text}, Wheel={ScrollWheelValue}]";
	}
}
