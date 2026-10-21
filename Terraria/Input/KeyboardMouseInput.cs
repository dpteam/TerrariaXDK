using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Terraria.Input;

/// <summary>
/// Quick actions available on the keyboard, mirroring the PC bindings.
/// </summary>
internal enum QuickAction
{
	None = 0,
	Heal,
	Mana,
	Buff
}

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

	/// <summary>One wheel notch in raw XNA units. Devices report multiples of this.</summary>
	public const int WheelNotch = 120;

	/// <summary>How many notches a single frame may hand out. Bounds stale accumulation.</summary>
	private const int MaxPendingWheelSteps = 10;

	private static int _pendingWheelSteps;

	/// <summary>
	/// True once the mouse has been used since the gamepad last moved. Menus keep the cursor
	/// visible off this flag instead of off "moved this frame", so the pointer does not
	/// disappear while the player reads an entry and does not get hijacked by stick logic.
	/// </summary>
	private static bool _mouseEngaged;

	private static int _hotbarRequest = -1;
	private static QuickAction _quickAction = QuickAction.None;

	/// <summary>Set while the inventory (not a menu screen) is open.</summary>
	private static bool _inventoryContext;

	/// <summary>
	/// True on the character-creation screen. Enter means "confirm" there, and that screen
	/// listens for <c>Buttons.Start</c> rather than <c>A</c> (A cycles categories), so the
	/// mapping has to be context aware.
	/// </summary>
	private static bool _enterConfirms;

	/// <summary>
	/// Escape fires once per press. Without this, the same held key opened the pause menu and
	/// then immediately activated "Resume" on the next frame, because the mapping changes from
	/// Start to Back as soon as the menu opens.
	/// </summary>
	private static bool _escapeLatched;

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
	/// select, game mode). Without it a click would do the job twice: once through the
	/// screen's own hit-test and again through the "LMB acts as A" shortcut.
	/// </summary>
	public static bool MouseHandledByScreen { get; set; }

	/// <summary>
	/// Set while a text field (chat) has focus. Game input is suppressed so typing does not
	/// move the player or swing the held item.
	/// </summary>
	public static bool TextInputActive { get; set; }

	/// <summary>
	/// Suppresses game input for the rest of the current frame. Set when a text field closes
	/// on Escape/Enter, so the same keystroke cannot also open the pause menu or swing an item.
	/// Cleared at the start of each <see cref="Update"/>.
	/// </summary>
	public static bool SuppressGameInput { get; set; }

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
		SuppressGameInput = false;
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
		// Wheel movement is exposed as discrete steps, not as a shoulder-button press: the
		// wheel means "scroll the thing under my focus" (hotbar in game, recipes in the
		// crafting menu), while the shoulder buttons mean "switch section". Mapping the wheel
		// onto shoulders made it impossible to tell the two intentions apart.
		int num = WheelSteps(ScrollDelta, ref _wheelAccumulator);
		if (num != 0)
		{
			_pendingWheelSteps += num;
			// Bounded so a menu that ignores the wheel cannot build up a burst that fires the
			// moment the player closes it.
			if (_pendingWheelSteps > MaxPendingWheelSteps)
			{
				_pendingWheelSteps = MaxPendingWheelSteps;
			}
			else if (_pendingWheelSteps < -MaxPendingWheelSteps)
			{
				_pendingWheelSteps = -MaxPendingWheelSteps;
			}
		}
		// Refresh the context flags from the live UI state here rather than in
		// UI.UpdateGamePad: that runs later in the frame, so the mapping would otherwise use
		// last frame's answer (one frame of wrong context on every menu transition).
		RefreshContext();

		bool moved = _mouse.X != _prevMouse.X || _mouse.Y != _prevMouse.Y;
		bool clicked = LeftPressed || RightPressed ||
			_mouse.MiddleButton == ButtonState.Pressed ||
			_mouse.XButton1 == ButtonState.Pressed ||
			_mouse.XButton2 == ButtonState.Pressed;
		if (moved || clicked || ScrollDelta != 0)
		{
			_idleFrames = 0;
			_mouseEngaged = true;
			CursorOverride = true;
		}
		else if (CursorOverride && ++_idleFrames > CursorIdleFrames)
		{
			// Player went back to the controller (or is idle): restore stick behaviour.
			CursorOverride = false;
		}
		// Menus are pointed at, not steered: as long as the mouse has been used, keep the
		// pointer in charge. Re-arming here is what fixes "cursor snaps to the buttons".
		if (MenuContext && _mouseEngaged)
		{
			_idleFrames = 0;
			CursorOverride = true;
		}
		_hotbarRequest = -1;
		if (!TextInputActive)
		{
			// Hotbar slots 1..9 and 0 map to the ten hotbar entries, like the PC version.
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
		// PC bindings: H heal, J mana, B buff.
		_quickAction = QuickAction.None;
		if (!TextInputActive && !MenuContext)
		{
			if (KeyPressed(KeyBindings.Get(KeyBindings.Action.Heal)))
			{
				_quickAction = QuickAction.Heal;
			}
			else if (KeyPressed(KeyBindings.Get(KeyBindings.Action.Mana)))
			{
				_quickAction = QuickAction.Mana;
			}
			else if (KeyPressed(KeyBindings.Get(KeyBindings.Action.Buff)))
			{
				_quickAction = QuickAction.Buff;
			}
		}
	}

	/// <summary>
	/// Derives the input context from the current UI state.
	///
	/// Only <see cref="UI.CurMenuType"/> is a reliable indicator: <c>CurMenuMode</c> is never
	/// reset to <c>NONE</c> when gameplay starts, so testing it made every frame look like a
	/// menu - which routed the left click to "confirm" and made attacking impossible.
	/// </summary>
	private static void RefreshContext()
	{
		UI ui = UI.MainUI;
		if (ui == null)
		{
			return;
		}
		MenuContext = ui.CurMenuType != MenuType.NONE || ui.InventoryMode > 0 || MessageBox.IsVisible();
		_inventoryContext = ui.InventoryMode > 0;
		_enterConfirms = ui.CurMenuMode == MenuMode.CREATE_CHARACTER;
		// Only screens that consume the click themselves opt out of "left click = A".
		MouseHandledByScreen = ui.CurMenuType != MenuType.NONE && (ui.CurMenuMode == MenuMode.CREATE_CHARACTER || ui.CurMenuMode == MenuMode.WORLD_SELECT);
		// A click that lands nowhere on a menu list must do nothing, not activate the
		// previously highlighted entry.
		if (!MouseHandledByScreen && ui.ClickOutsideMenuEntries)
		{
			MouseHandledByScreen = true;
		}
	}

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

	/// <summary>
	/// Wheel movement since the last call, in notches: positive is "up" (or "previous").
	/// Consumed so exactly one part of the UI reacts to it - the hotbar in game, the recipe
	/// list while crafting.
	/// </summary>
	public static int ConsumeWheelSteps()
	{
		int result = _pendingWheelSteps;
		_pendingWheelSteps = 0;
		return result;
	}

	/// <summary>
	/// Converts a raw wheel delta into whole notches, keeping the remainder so a device with
	/// finer resolution (a trackpad scrolling 40 units at a time) still moves the list after a
	/// few events instead of never reaching 120. Pure, so the self-test can drive it.
	/// </summary>
	internal static int WheelSteps(float delta, ref float accumulator)
	{
		accumulator += delta;
		int num = (int)(accumulator / WheelNotch);
		if (num != 0)
		{
			accumulator -= num * WheelNotch;
		}
		return num;
	}

	/// <summary>Quick action requested this frame (H/J/B), or <see cref="QuickAction.None"/>.</summary>
	public static QuickAction ConsumeQuickAction()
	{
		QuickAction result = _quickAction;
		_quickAction = QuickAction.None;
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
		bool flag = _keys.IsKeyDown(Keys.Escape);
		if (!flag)
		{
			_escapeLatched = false;
		}
		bool flag2 = !_escapeLatched;
		if (flag && flag2)
		{
			_escapeLatched = true;
		}
		return BuildSyntheticState(_mouse, _prevMouse, _keys, MenuContext && !TextInputActive, TextInputActive || SuppressGameInput, flag2, _enterConfirms, _inventoryContext);
	}

	/// <summary>
	/// Pure mapping function: given a device snapshot, produce the equivalent pad state.
	/// Kept free of instance state so it can be exercised by the headless self-test.
	/// </summary>
	internal static GamePadState BuildSyntheticState(MouseState mouse, MouseState prevMouse, KeyboardState keys, bool menuContext, bool textInputActive, bool escapeAllowed = true, bool enterConfirms = false, bool inventoryContext = false)
	{
		Vector2 leftStick = Vector2.Zero;
		float leftTrigger = 0f;
		float rightTrigger = 0f;
		Buttons buttons = 0;
		if (textInputActive)
		{
			// Typing: the keyboard belongs to the text field, not to the player.
			return new GamePadState(leftStick, Vector2.Zero, 0f, 0f, buttons);
		}
		bool left = mouse.LeftButton == ButtonState.Pressed;
		bool right = mouse.RightButton == ButtonState.Pressed;

		// Movement: bound keys drive the left stick.
		if (keys.IsKeyDown(KeyBindings.Get(KeyBindings.Action.MoveLeft)))
		{
			leftStick.X -= 1f;
		}
		if (keys.IsKeyDown(KeyBindings.Get(KeyBindings.Action.MoveRight)))
		{
			leftStick.X += 1f;
		}
		if (keys.IsKeyDown(KeyBindings.Get(KeyBindings.Action.MoveUp)))
		{
			leftStick.Y += 1f;
		}
		if (keys.IsKeyDown(KeyBindings.Get(KeyBindings.Action.MoveDown)))
		{
			leftStick.Y -= 1f;
		}
		// Menus / cursor nudging: arrow keys act as the D-Pad.
		if (keys.IsKeyDown(Keys.Left))
		{
			buttons |= Buttons.DPadLeft;
			leftStick.X = -1f;
		}
		if (keys.IsKeyDown(Keys.Right))
		{
			buttons |= Buttons.DPadRight;
			leftStick.X = 1f;
		}
		if (keys.IsKeyDown(Keys.Up))
		{
			buttons |= Buttons.DPadUp;
			leftStick.Y = 1f;
		}
		if (keys.IsKeyDown(Keys.Down))
		{
			buttons |= Buttons.DPadDown;
			leftStick.Y = -1f;
		}
		// Buttons.
		if (keys.IsKeyDown(KeyBindings.Get(KeyBindings.Action.Jump)))
		{
			buttons |= Buttons.A; // jump / confirm
		}
		// Enter confirms inside menus, but must NOT act as "jump" during play: there it opens
		// the chat instead (see ChatInput). Space stays the universal confirm/jump.
		// Screens that confirm with Start (character creation) get Start instead of A.
		if (keys.IsKeyDown(Keys.Enter) && menuContext)
		{
			buttons |= enterConfirms ? Buttons.Start : Buttons.A;
		}
		if (keys.IsKeyDown(Keys.LeftShift) || keys.IsKeyDown(Keys.RightShift))
		{
			buttons |= Buttons.LeftStick; // BTN_JUMP2
		}
		// Inventory: the bound key plus I as a second, familiar binding.
		if (keys.IsKeyDown(KeyBindings.Get(KeyBindings.Action.Inventory)) || keys.IsKeyDown(Keys.I))
		{
			buttons |= Buttons.Y; // inventory (character creation reuses Y for randomize)
		}
		if (enterConfirms)
		{
			// Character creation: Q/R cycle the category (the screen listens to the shoulder
			// buttons), which is otherwise unreachable from a keyboard.
			if (keys.IsKeyDown(Keys.Q))
			{
				buttons |= Buttons.LeftShoulder;
			}
			if (keys.IsKeyDown(Keys.R))
			{
				buttons |= Buttons.RightShoulder;
			}
		}
		else if (keys.IsKeyDown(KeyBindings.Get(KeyBindings.Action.Throw)) || keys.IsKeyDown(Keys.R))
		{
			buttons |= Buttons.X; // throw item
		}
		if (keys.IsKeyDown(KeyBindings.Get(KeyBindings.Action.SmartCursor)))
		{
			buttons |= Buttons.RightStick; // toggle smart cursor
		}
		// Pause is context sensitive: "cancel" inside a menu, "pause" during play. Mapping it
		// to Start everywhere made it activate the highlighted entry instead of leaving.
		if (keys.IsKeyDown(KeyBindings.Get(KeyBindings.Action.Pause)) && escapeAllowed)
		{
			buttons |= menuContext ? Buttons.Back : Buttons.Start;
		}
		// Map / cancel. The bound key plus M as a second binding.
		if (keys.IsKeyDown(Keys.M) || keys.IsKeyDown(Keys.Back) || keys.IsKeyDown(KeyBindings.Get(KeyBindings.Action.Map)))
		{
			buttons |= Buttons.Back;
		}
		// Mouse buttons.
		if (menuContext)
		{
			// Edge-triggered only: using the held state meant that the first frame after a
			// screen change still saw the button down from the previous screen and activated
			// a menu entry immediately (world select jumped straight into the game, skipping
			// the host options).
			bool flag3 = left && prevMouse.LeftButton == ButtonState.Released;
			if (flag3 && !MouseHandledByScreen)
			{
				buttons |= Buttons.A; // confirm menu entry
			}
			// PC parity: with the inventory open the right button splits a stack instead of
			// cancelling, which is how the console build's RightTrigger behaves.
			if (inventoryContext && right && prevMouse.RightButton == ButtonState.Released)
			{
				rightTrigger = 1f;
				buttons |= Buttons.RightTrigger;
			}
			else if (right)
			{
				buttons |= Buttons.B; // cancel
			}
		}
		else
		{
			if (left)
			{
				rightTrigger = 1f; // use item / attack
				buttons |= Buttons.RightTrigger;
			}
			// PC parity: the right button places blocks and interacts with tiles
			// (controlUseTile); the grapple lives on the middle button.
			if (right)
			{
				buttons |= Buttons.B;
			}
		}
		// Grapple: middle mouse button or the bound key.
		if (mouse.MiddleButton == ButtonState.Pressed || keys.IsKeyDown(KeyBindings.Get(KeyBindings.Action.Grapple)))
		{
			leftTrigger = 1f;
			buttons |= Buttons.LeftTrigger;
		}
		// Wheel movement is no longer mapped to the shoulder buttons: it is exposed as
		// discrete steps instead (see ConsumeWheelSteps), because the wheel scrolls what has
		// focus rather than switching sections.
		if (leftStick.LengthSquared() > 1f)
		{
			leftStick.Normalize();
		}
		return new GamePadState(leftStick, Vector2.Zero, leftTrigger, rightTrigger, buttons);
	}

	/// <summary>Merges physical gamepad input with the synthetic keyboard/mouse state.</summary>
	public static GamePadState Merge(GamePadState physical)
	{
		if (!Enabled)
		{
			return physical;
		}
		// A gamepad in use means the player switched devices: hand control back to it so a
		// controller user never sees a mouse pointer sitting in the middle of the screen.
		if (PadIsActive(physical))
		{
			_mouseEngaged = false;
			CursorOverride = false;
			_idleFrames = 0;
		}
		GamePadState synthetic = BuildSyntheticState();
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

	/// <summary>True when the physical pad shows real activity (stick, trigger or button).</summary>
	private static bool PadIsActive(GamePadState pad)
	{
		if (!pad.IsConnected)
		{
			return false;
		}
		if (pad.Buttons.buttons != 0)
		{
			return true;
		}
		// Generous thresholds on purpose: a resting controller with stick drift or a
		// half-pressed trigger must not steal control from the mouse every few frames.
		if (pad.Triggers.Left > 0.35f || pad.Triggers.Right > 0.35f)
		{
			return true;
		}
		return pad.ThumbSticks.Left.LengthSquared() > 0.36f || pad.ThumbSticks.Right.LengthSquared() > 0.36f;
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
