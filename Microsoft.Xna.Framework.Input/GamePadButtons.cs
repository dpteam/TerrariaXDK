namespace Microsoft.Xna.Framework.Input;

public struct GamePadButtons
{
	internal Buttons buttons;

	public ButtonState A => ((buttons & Buttons.A) == Buttons.A) ? ButtonState.Pressed : ButtonState.Released;

	public ButtonState B => ((buttons & Buttons.B) == Buttons.B) ? ButtonState.Pressed : ButtonState.Released;

	public ButtonState Back => ((buttons & Buttons.Back) == Buttons.Back) ? ButtonState.Pressed : ButtonState.Released;

	public ButtonState X => ((buttons & Buttons.X) == Buttons.X) ? ButtonState.Pressed : ButtonState.Released;

	public ButtonState Y => ((buttons & Buttons.Y) == Buttons.Y) ? ButtonState.Pressed : ButtonState.Released;

	public ButtonState Start => ((buttons & Buttons.Start) == Buttons.Start) ? ButtonState.Pressed : ButtonState.Released;

	public ButtonState LeftShoulder => ((buttons & Buttons.LeftShoulder) == Buttons.LeftShoulder) ? ButtonState.Pressed : ButtonState.Released;

	public ButtonState LeftStick => ((buttons & Buttons.LeftStick) == Buttons.LeftStick) ? ButtonState.Pressed : ButtonState.Released;

	public ButtonState RightShoulder => ((buttons & Buttons.RightShoulder) == Buttons.RightShoulder) ? ButtonState.Pressed : ButtonState.Released;

	public ButtonState RightStick => ((buttons & Buttons.RightStick) == Buttons.RightStick) ? ButtonState.Pressed : ButtonState.Released;

	public ButtonState BigButton => ((buttons & Buttons.BigButton) == Buttons.BigButton) ? ButtonState.Pressed : ButtonState.Released;

	public GamePadButtons(Buttons buttons)
	{
		this.buttons = buttons;
	}

	internal static GamePadButtons FromButtonArray(params Buttons[] buttons)
	{
		Buttons buttons2 = (Buttons)0;
		foreach (Buttons buttons3 in buttons)
		{
			buttons2 |= buttons3;
		}
		return new GamePadButtons(buttons2);
	}

	public static bool operator ==(GamePadButtons left, GamePadButtons right)
	{
		return left.buttons == right.buttons;
	}

	public static bool operator !=(GamePadButtons left, GamePadButtons right)
	{
		return !(left == right);
	}

	public override bool Equals(object obj)
	{
		return obj is GamePadButtons && this == (GamePadButtons)obj;
	}

	public override int GetHashCode()
	{
		return (int)buttons;
	}
}
