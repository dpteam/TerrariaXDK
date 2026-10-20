using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Terraria.Input;

/// <summary>
/// Bridges desktop keyboard + mouse input onto the gamepad-centric input model used by
/// the Xbox 360 code base. Nothing in the game logic is rewritten: a synthetic
/// <see cref="GamePadState"/> is merged with the physical one, plus an absolute mouse
/// cursor that takes over from the stick-driven virtual cursor while the mouse is in use.
/// </summary>
internal static class KeyboardMouseInput
{
	// --- Tunables -----------------------------------------------------------
	private const float StickThreshold = 0.125f;
	private const float TriggerThreshold = 0.1176f;
	private const int CursorIdleFrames = 180;

	// --- State --------------------------------------------------------------
	private static MouseState _mouse;
	private static MouseState _prevMouse;
	private static KeyboardState _keys;
	private static KeyboardState _prevKeys;

	private static int _idleFrames;
	private static float _wheelAccumulator;

	/// <summary>Master switch (Settings.ini: [Input] KeyboardMouse).</summary>
	public static bool Enabled = true;

	/// <summary>
	/// True while the player is actually using the mouse. While false the game behaves
	/// exactly like the original gamepad-only build.
	/// </summary>
	public static bool CursorOverride { get; private set; }

	/// <summary>True when the current UI is a menu / inventory, where LMB means "confirm".</summary>
	public static bool MenuContext { get; set; }

	/// <summary>
	/// Set by screens that perform their own mouse hit-testing (character creation, world
	/// select). Without it a click would do the job twice: once through the screen's own
	/// hit-test and again through the "LMB acts as A" shortcut, which in character creation
	/// advanced the category on every click.
	/// </summary>
	public static bool MouseHandledByScreen { get; set; }

	/// <summary>Absolute mouse position in backbuffer pixels.</summary>
	public static int MouseX { get; private set; }

	public static int MouseY { get; private set; }

	public static int ScrollDelta { get; private set; }

	public static bool LeftPressed => _mouse.LeftButton == ButtonState.Pressed;

	public static bool RightPressed => _mouse.RightButton == ButtonState.Pressed;

	public static bool LeftClicked =>
		_mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;

	public static bool RightClicked =>
		_mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Released;

	/// <summary>Polls devices once per frame. Must run before any UI is updated.</summary>
	public static void Update()
	{
		if (!Enabled)
		{
			CursorOverride = false;
			return;
		}
		_prevMouse = _mouse;
		_prevKeys = _keys;
		try
		{
			_mouse = Mouse.GetState();
			_keys = Keyboard.GetState();
		}
		catch
		{
			// Windowing backend not ready yet (splash screens, shutdown).
			return;
		}
		MouseX = _mouse.X;
		MouseY = _mouse.Y;
		ScrollDelta = _mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
		_wheelAccumulator += ScrollDelta;

		bool moved = _mouse.X != _prevMouse.X || _mouse.Y != _prevMouse.Y;
		bool clicked = LeftPressed || RightPressed ||
			_mouse.MiddleButton == ButtonState.Pressed ||
			_mouse.XButton1 == ButtonState.Pressed ||
			_mouse.XButton2 == ButtonState.Pressed;
		if (moved || clicked || ScrollDelta != 0)
		{
			_idleFrames = 0;
			CursorOverride = true;
		}
		else if (MenuContext)
		{
			// Menus are pointed at, not steered with a stick: keep the pointer available even
			// when the mouse sits still, otherwise it vanishes while the player reads an entry.
			_idleFrames = 0;
		}
		else if (CursorOverride && ++_idleFrames > CursorIdleFrames)
		{
			// Player went back to the controller (or is idle): restore stick behaviour.
			CursorOverride = false;
		}
		// Hotbar slots 1..9 and 0 map to the ten hotbar entries, like the PC version.
		_hotbarRequest = -1;
		for (int i = 0; i < 9; i++)
		{
			if (KeyPressed((Keys)((int)Keys.D1 + i)))
			{
				_hotbarRequest = i;
				break;
			}
		}
		if (_hotbarRequest < 0 && KeyPressed(Keys.D0))
		{
			_hotbarRequest = 9;
		}
	}

	private static int _hotbarRequest = -1;

	/// <summary>
	/// Hotbar slot requested by a number key this frame, or -1. The caller applies it so this
	/// class stays independent of the player and UI types.
	/// </summary>
	public static int ConsumeHotbarRequest()
	{
		int result = _hotbarRequest;
		_hotbarRequest = -1;
		return result;
	}

	public static bool KeyDown(Keys key)
	{
		return _keys.IsKeyDown(key);
	}

	public static bool KeyPressed(Keys key)
	{
		return _keys.IsKeyDown(key) && !_prevKeys.IsKeyDown(key);
	}

	public static bool KeyReleased(Keys key)
	{
		return _keys.IsKeyUp(key) && !_prevKeys.IsKeyUp(key);
	}

	/// <summary>Builds the synthetic pad state for the current keyboard/mouse state.</summary>
	public static GamePadState BuildSyntheticState()
	{
		Vector2 leftStick = Vector2.Zero;
		Vector2 rightStick = Vector2.Zero;
		float leftTrigger = 0f;
		float rightTrigger = 0f;
		Buttons buttons = 0;

		// Movement: WASD drives the left stick.
		if (_keys.IsKeyDown(Keys.A))
		{
			leftStick.X -= 1f;
		}
		if (_keys.IsKeyDown(Keys.D))
		{
			leftStick.X += 1f;
		}
		if (_keys.IsKeyDown(Keys.W))
		{
			leftStick.Y += 1f;
		}
		if (_keys.IsKeyDown(Keys.S))
		{
			leftStick.Y -= 1f;
		}

		// Menus / cursor nudging: arrow keys act as the D-Pad.
		if (_keys.IsKeyDown(Keys.Left))
		{
			buttons |= Buttons.DPadLeft;
			leftStick.X = -1f;
		}
		if (_keys.IsKeyDown(Keys.Right))
		{
			buttons |= Buttons.DPadRight;
			leftStick.X = 1f;
		}
		if (_keys.IsKeyDown(Keys.Up))
		{
			buttons |= Buttons.DPadUp;
			leftStick.Y = 1f;
		}
		if (_keys.IsKeyDown(Keys.Down))
		{
			buttons |= Buttons.DPadDown;
			leftStick.Y = -1f;
		}

		// Buttons.
		if (_keys.IsKeyDown(Keys.Space))
		{
			buttons |= Buttons.A; // jump / confirm
		}
		if (_keys.IsKeyDown(Keys.Enter))
		{
			buttons |= Buttons.A;
		}
		if (_keys.IsKeyDown(Keys.LeftShift) || _keys.IsKeyDown(Keys.RightShift))
		{
			buttons |= Buttons.LeftStick; // BTN_JUMP2
		}
		if (_keys.IsKeyDown(Keys.E) || _keys.IsKeyDown(Keys.Tab))
		{
			buttons |= Buttons.Y; // inventory (character creation reuses Y for randomize)
		}
		// PC uses Q for "throw item"; R is kept as a second binding for convenience.
		if (_keys.IsKeyDown(Keys.R) || _keys.IsKeyDown(Keys.Q))
		{
			buttons |= Buttons.X;
		}
		if (_keys.IsKeyDown(Keys.C))
		{
			buttons |= Buttons.RightStick; // toggle smart cursor
		}
		// Escape is context sensitive. On the console "Start" means confirm, so mapping Escape
		// to Start everywhere made it activate the highlighted entry instead of leaving the
		// menu. In a menu Escape must be Cancel (Back); in game it opens the pause menu.
		if (_keys.IsKeyDown(Keys.Escape))
		{
			buttons |= MenuContext ? Buttons.Back : Buttons.Start;
		}
		// Map / cancel. Q is taken by "throw item" (PC parity), so the map is M only.
		if (_keys.IsKeyDown(Keys.M) || _keys.IsKeyDown(Keys.Back))
		{
			buttons |= Buttons.Back;
		}

		// Mouse buttons.
		if (MenuContext)
		{
			if (LeftPressed && !MouseHandledByScreen)
			{
				buttons |= Buttons.A; // confirm menu entry
			}
			if (RightPressed)
			{
				buttons |= Buttons.B; // cancel
			}
		}
		else
		{
			if (LeftPressed)
			{
				rightTrigger = 1f; // use item / attack
				buttons |= Buttons.RightTrigger;
			}
			// PC parity: right button places blocks and interacts with tiles
			// (controlUseTile), it is NOT the grapple. Grapple lives on the middle button.
			if (RightPressed)
			{
				buttons |= Buttons.B;
			}
		}
		// Grapple: middle mouse button or V, matching the "hook" action on PC.
		if (_mouse.MiddleButton == ButtonState.Pressed || _keys.IsKeyDown(Keys.V))
		{
			leftTrigger = 1f;
			buttons |= Buttons.LeftTrigger;
		}

		// Mouse wheel cycles the hotbar.
		if (ScrollDelta > 0)
		{
			buttons |= Buttons.LeftShoulder;
			_wheelAccumulator = 0f;
		}
		else if (ScrollDelta < 0)
		{
			buttons |= Buttons.RightShoulder;
			_wheelAccumulator = 0f;
		}

		if (leftStick.LengthSquared() > 1f)
		{
			leftStick.Normalize();
		}
		GamePadState state = new GamePadState(leftStick, rightStick, leftTrigger, rightTrigger, buttons);
		return state;
	}

	/// <summary>Merges physical gamepad input with the synthetic keyboard/mouse state.</summary>
	public static GamePadState Merge(GamePadState physical)
	{
		if (!Enabled)
		{
			return physical;
		}
		GamePadState synthetic = BuildSyntheticState();

		// Union of buttons/dpad; sticks prefer whichever is being moved.
		Buttons buttons = physical.Buttons.buttons | synthetic.Buttons.buttons;
		GamePadDPad dpad = physical.DPad;
		if (synthetic.DPad.Up == ButtonState.Pressed)
		{
			dpad.Up = ButtonState.Pressed;
		}
		if (synthetic.DPad.Down == ButtonState.Pressed)
		{
			dpad.Down = ButtonState.Pressed;
		}
		if (synthetic.DPad.Left == ButtonState.Pressed)
		{
			dpad.Left = ButtonState.Pressed;
		}
		if (synthetic.DPad.Right == ButtonState.Pressed)
		{
			dpad.Right = ButtonState.Pressed;
		}
		Vector2 left = physical.ThumbSticks.Left;
		if (synthetic.ThumbSticks.Left.LengthSquared() > StickThreshold * StickThreshold)
		{
			left = synthetic.ThumbSticks.Left;
		}
		Vector2 right = physical.ThumbSticks.Right;
		if (synthetic.ThumbSticks.Right.LengthSquared() > StickThreshold * StickThreshold)
		{
			right = synthetic.ThumbSticks.Right;
		}
		float lt = Math.Max(physical.Triggers.Left, synthetic.Triggers.Left > TriggerThreshold ? synthetic.Triggers.Left : 0f);
		float rt = Math.Max(physical.Triggers.Right, synthetic.Triggers.Right > TriggerThreshold ? synthetic.Triggers.Right : 0f);
		return new GamePadState(new GamePadThumbSticks(left, right), new GamePadTriggers(lt, rt), new GamePadButtons(buttons), dpad);
	}

	/// <summary>
	/// Absolute cursor position clamped into the given view. Returns false when the
	/// mouse is not currently driving the cursor (so stick logic keeps working).
	/// </summary>
	public static bool TryGetViewCursor(int viewWidth, int viewHeight, out int x, out int y)
	{
		x = 0;
		y = 0;
		if (!Enabled || !CursorOverride || viewWidth <= 0 || viewHeight <= 0)
		{
			return false;
		}
		x = MouseX;
		y = MouseY;
		if (x < 0)
		{
			x = 0;
		}
		else if (x > viewWidth - 1)
		{
			x = viewWidth - 1;
		}
		if (y < 0)
		{
			y = 0;
		}
		else if (y > viewHeight - 1)
		{
			y = viewHeight - 1;
		}
		return true;
	}
}
