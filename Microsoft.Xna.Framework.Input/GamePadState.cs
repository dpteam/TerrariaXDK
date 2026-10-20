namespace Microsoft.Xna.Framework.Input;

public struct GamePadState
{
	public bool IsConnected { get; internal set; }

	public int PacketNumber { get; internal set; }

	public GamePadButtons Buttons { get; internal set; }

	public GamePadDPad DPad { get; internal set; }

	public GamePadThumbSticks ThumbSticks { get; internal set; }

	public GamePadTriggers Triggers { get; internal set; }

	public GamePadState(GamePadThumbSticks thumbSticks, GamePadTriggers triggers, GamePadButtons buttons, GamePadDPad dPad)
	{
		this = default;
		if (triggers.Left > 30f / 255f)
		{
			buttons.buttons |= Microsoft.Xna.Framework.Input.Buttons.LeftTrigger;
		}
		if (triggers.Right > 30f / 255f)
		{
			buttons.buttons |= Microsoft.Xna.Framework.Input.Buttons.RightTrigger;
		}
		buttons.buttons |= StickToButtons(thumbSticks.Left, Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickLeft, Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickRight, Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickUp, Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickDown, 0.23953247f);
		buttons.buttons |= StickToButtons(thumbSticks.Right, Microsoft.Xna.Framework.Input.Buttons.RightThumbstickLeft, Microsoft.Xna.Framework.Input.Buttons.RightThumbstickRight, Microsoft.Xna.Framework.Input.Buttons.RightThumbstickUp, Microsoft.Xna.Framework.Input.Buttons.RightThumbstickDown, 0.26516724f);
		ThumbSticks = thumbSticks;
		Triggers = triggers;
		Buttons = buttons;
		DPad = dPad;
		IsConnected = true;
		PacketNumber = 0;
	}

	public GamePadState(Vector2 leftThumbStick, Vector2 rightThumbStick, float leftTrigger, float rightTrigger, params Buttons[] buttons)
		: this(new GamePadThumbSticks(leftThumbStick, rightThumbStick), new GamePadTriggers(leftTrigger, rightTrigger), GamePadButtons.FromButtonArray(buttons), GamePadDPad.FromButtonArray(buttons))
	{
	}

	public bool IsButtonDown(Buttons button)
	{
		return (Buttons.buttons & button) == button;
	}

	public bool IsButtonUp(Buttons button)
	{
		return (Buttons.buttons & button) != button;
	}

	private static Buttons StickToButtons(Vector2 stick, Buttons left, Buttons right, Buttons up, Buttons down, float DeadZoneSize)
	{
		Buttons buttons = (Buttons)0;
		if (stick.X > DeadZoneSize)
		{
			buttons |= right;
		}
		if (stick.X < 0f - DeadZoneSize)
		{
			buttons |= left;
		}
		if (stick.Y > DeadZoneSize)
		{
			buttons |= up;
		}
		if (stick.Y < 0f - DeadZoneSize)
		{
			buttons |= down;
		}
		return buttons;
	}

	public static bool operator ==(GamePadState left, GamePadState right)
	{
		return left.IsConnected == right.IsConnected && left.PacketNumber == right.PacketNumber && left.Buttons == right.Buttons && left.DPad == right.DPad && left.ThumbSticks == right.ThumbSticks && left.Triggers == right.Triggers;
	}

	public static bool operator !=(GamePadState left, GamePadState right)
	{
		return !(left == right);
	}

	public override bool Equals(object obj)
	{
		return obj is GamePadState && this == (GamePadState)obj;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override string ToString()
	{
		return base.ToString();
	}
}
