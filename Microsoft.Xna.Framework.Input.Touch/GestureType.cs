using System;

namespace Microsoft.Xna.Framework.Input.Touch;

[Flags]
public enum GestureType
{
	None = 0,
	Tap = 1,
	DoubleTap = 2,
	Hold = 4,
	HorizontalDrag = 8,
	VerticalDrag = 0x10,
	FreeDrag = 0x20,
	Pinch = 0x40,
	Flick = 0x80,
	DragComplete = 0x100,
	PinchComplete = 0x200
}
