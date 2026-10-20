using System;

namespace Microsoft.Xna.Framework.Input;

[Flags]
public enum Buttons
{
	DPadUp = 1,
	DPadDown = 2,
	DPadLeft = 4,
	DPadRight = 8,
	Start = 0x10,
	Back = 0x20,
	LeftStick = 0x40,
	RightStick = 0x80,
	LeftShoulder = 0x100,
	RightShoulder = 0x200,
	BigButton = 0x800,
	A = 0x1000,
	B = 0x2000,
	X = 0x4000,
	Y = 0x8000,
	LeftThumbstickLeft = 0x200000,
	RightTrigger = 0x400000,
	LeftTrigger = 0x800000,
	RightThumbstickUp = 0x1000000,
	RightThumbstickDown = 0x2000000,
	RightThumbstickRight = 0x4000000,
	RightThumbstickLeft = 0x8000000,
	LeftThumbstickUp = 0x10000000,
	LeftThumbstickDown = 0x20000000,
	LeftThumbstickRight = 0x40000000,
	Misc1EXT = 0x400,
	Paddle1EXT = 0x10000,
	Paddle2EXT = 0x20000,
	Paddle3EXT = 0x40000,
	Paddle4EXT = 0x80000,
	TouchPadEXT = 0x100000
}
