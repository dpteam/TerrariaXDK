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

	/// <summary>Inventory toggle requested this frame (E / I), see <see cref="ConsumeInventoryToggle"/>.</summary>
	private static bool _inventoryToggle;

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
	/// Cleared at the start of each <see cref="Update"/> - unless <see cref="HoldUntilRelease"/>
	/// was called, which keeps it set while the closing key is still physically held.
	/// </summary>
	public static bool SuppressGameInput { get; set; }

	/// <summary>
	/// Keeps input swallowed until every key and button that could have closed a modal is
	/// released, instead of for a single frame.
	///
	/// Why it is needed: the on-screen keyboard closes on Enter/A, and the very next frame the
	/// screen underneath sees a fresh edge on a key that is still down, because its previous
	/// state was the zeroed pad from while the keyboard was open. In character creation Enter
	/// means "Start", so accepting a name reopened the name prompt - and it looked like the
	/// keyboard itself was broken. One frame of suppression is not enough; the key has to come
	/// up first.
	/// </summary>
	public static void HoldUntilRelease()
	{
		SuppressGameInput = true;
		_holdUntilRelease = true;
	}

	private static bool _holdUntilRelease;

	/// <summary>
	/// True while the window has lost focus and has not regained it since. Used to detect the
	/// refocus click, which must not become "use item".
	/// </summary>
	private static bool _lostFocus;

	/// <summary>
	/// True while every mouse button is ignored, until the player releases them all. Set on
	/// regaining focus and on closing a menu with a button held; both are cases where a click had
	/// a purpose that has nothing to do with the game world.
	/// </summary>
	private static bool _swallowMouseUntilRelease;

	/// <summary>Menu context as of the previous frame, to notice a menu closing.</summary>
	private static bool _prevMenuContext;

	/// <summary>
	/// True when any mouse button is down. Pure, so the swallow logic is testable without a
	/// window: the rule is "ignore until nothing is held", and getting it wrong either swallows a
	/// real click or lets the refocus click fire a weapon.
	/// </summary>
	internal static bool AnyMouseButtonDown(MouseState mouse)
	{
		return mouse.LeftButton == ButtonState.Pressed
			|| mouse.RightButton == ButtonState.Pressed
			|| mouse.MiddleButton == ButtonState.Pressed
			|| mouse.XButton1 == ButtonState.Pressed
			|| mouse.XButton2 == ButtonState.Pressed;
	}

	/// <summary>
	/// The buttons that reach the game this frame. Zeroed while a swallow is in effect, which is
	/// why this is one function rather than three call sites each remembering to check.
	/// </summary>
	internal static void EffectiveMouseButtons(MouseState mouse, bool swallow, out bool left, out bool right, out bool middle)
	{
		bool flag = !swallow;
		left = flag && mouse.LeftButton == ButtonState.Pressed;
		right = flag && mouse.RightButton == ButtonState.Pressed;
		middle = flag && mouse.MiddleButton == ButtonState.Pressed;
	}

	/// <summary>Absolute mouse position in internal-resolution pixels (see <see cref="SetViewport"/>).</summary>
	public static int MouseX { get; private set; }

	public static int MouseY { get; private set; }

	// The mapping from window pixels to internal resolution. Set once per frame by Main.
	private static int _bufferWidth;

	private static int _bufferHeight;

	private static int _viewWidth;

	private static int _viewHeight;

	/// <summary>
	/// Tells the input layer how window pixels map onto the game's internal resolution.
	///
	/// The game renders into a RenderTarget of <c>ResolutionWidth x ResolutionHeight</c> and then
	/// stretches it to the backbuffer, so a pointer reported by the OS at window coordinates is
	/// only correct when the two happen to be the same size. At 1080p in a 720p window the pointer
	/// covered two thirds of the screen; at 540p the right third was unreachable. Every hit-test in
	/// the game compares against resolution-space rectangles, so the conversion belongs here, once,
	/// instead of in each of them.
	///
	/// Zeroes mean "no mapping yet": coordinates are then used as they come, which keeps the
	/// headless tests and the first frames working.
	/// </summary>
	public static void SetViewport(int bufferWidth, int bufferHeight, int viewWidth, int viewHeight)
	{
		_bufferWidth = bufferWidth;
		_bufferHeight = bufferHeight;
		_viewWidth = viewWidth;
		_viewHeight = viewHeight;
	}

	/// <summary>
	/// Maps one axis from buffer pixels to view pixels, clamped into the view. Pure, so the
	/// arithmetic is testable without a window.
	/// </summary>
	internal static int MapAxis(int value, int from, int to)
	{
		if (from <= 0 || to <= 0)
		{
			return value;
		}
		if (from == to)
		{
			return (value < 0) ? 0 : ((value >= to) ? (to - 1) : value);
		}
		// 64-bit on purpose: a 4K buffer times a large value overflows int.
		int num = (int)((long)value * to / from);
		if (num < 0)
		{
			return 0;
		}
		return (num >= to) ? (to - 1) : num;
	}

	public static int ScrollDelta { get; private set; }

	// These four are what UI code reads directly, so they obey the swallow too. Reporting a click
	// here while the game ignores it in BuildSyntheticState would make the two disagree: a click
	// that reopened a menu would also press a button in it.
	public static bool LeftPressed => !_swallowMouseUntilRelease && _mouse.LeftButton == ButtonState.Pressed;

	public static bool RightPressed => !_swallowMouseUntilRelease && _mouse.RightButton == ButtonState.Pressed;

	public static bool LeftClicked =>
		!_swallowMouseUntilRelease
		&& _mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;

	public static bool RightClicked =>
		!_swallowMouseUntilRelease
		&& _mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Released;

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
		// The "swallow this frame" flag is cleared here, after the new state is known, because
		// whether input may resume depends on whether the key that closed the modal is still down.
		SuppressGameInput = _holdUntilRelease && ModalKeysDown();
		if (!SuppressGameInput)
		{
			_holdUntilRelease = false;
		}
		MouseX = MapAxis(_mouse.X, _bufferWidth, _viewWidth);
		MouseY = MapAxis(_mouse.Y, _bufferHeight, _viewHeight);
		ScrollDelta = _mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
		// Two ways a held button must not act the moment it becomes relevant again:
		//
		//  * the window lost focus and got it back - clicking to refocus would otherwise fire the
		//    item, which is the case the PC build handles with `delayUseItem`;
		//  * a menu closed while the button was down - the click that chose "Resume" would keep
		//    attacking until the player let go.
		//
		// In both cases the honest answer is "wait for the release": the player pressed the button
		// for a reason that has nothing to do with swinging a sword. The menu half is decided
		// after RefreshContext below, because that is where the context is current.
		if (!Main.HasFocus)
		{
			_lostFocus = true;
		}
		else if (_lostFocus)
		{
			_lostFocus = false;
			_swallowMouseUntilRelease = true;
		}
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
		// A menu that just closed with a button still down must not hand that button to the
		// world. Opening a menu needs no such care: it consumes the click itself.
		if (MenuContext != _prevMenuContext)
		{
			if (_prevMenuContext && AnyMouseButtonDown(_mouse))
			{
				_swallowMouseUntilRelease = true;
			}
			_prevMenuContext = MenuContext;
		}
		// The swallow ends on the release, not after a frame: "one frame of grace" would still
		// fire on the second frame of a slow click.
		if (_swallowMouseUntilRelease && !AnyMouseButtonDown(_mouse))
		{
			_swallowMouseUntilRelease = false;
		}

		bool moved = _mouse.X != _prevMouse.X || _mouse.Y != _prevMouse.Y;
		// Cursor visibility, not game input: a swallowed click still means the player reached for
		// the mouse, so the pointer should appear even though the click itself is ignored.
		bool clicked = AnyMouseButtonDown(_mouse);
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
		// One edge-triggered request for the inventory, handled by the UI as a toggle. This is
		// not routed through Buttons.Y: Y means "open on release", "switch crafting section"
		// and "show NPCs" in different places, so one press did two things.
		_inventoryToggle = false;
		if (!TextInputActive && !SuppressGameInput
			&& (KeyPressed(KeyBindings.Get(KeyBindings.Action.Inventory)) || KeyPressed(Keys.I)))
		{
			_inventoryToggle = true;
		}
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
		// The NPC / sign dialog is a screen that lives in gameplay, so CurMenuType cannot see it.
		// Left as gameplay, a click on "Shop" became "use item": the pickaxe swung, and the
		// dialog's own option rectangles were the only way in - with the swing happening on top.
		bool dialog = ui.ChatOpen;
		MenuContext = dialog || ui.CurMenuType != MenuType.NONE || ui.InventoryMode > 0 || MessageBox.IsVisible();
		_inventoryContext = ui.InventoryMode > 0;
		_enterConfirms = ui.CurMenuMode == MenuMode.CREATE_CHARACTER;
		// Only screens that consume the click themselves opt out of "left click = A". The cheat
		// menu is one of them: it has its own row rectangles, and letting the click arrive a
		// second time as A would run the highlighted action as well as the one that was clicked.
		MouseHandledByScreen = dialog || (ui.CurMenuType != MenuType.NONE && (ui.CurMenuMode == MenuMode.CREATE_CHARACTER || ui.CurMenuMode == MenuMode.WORLD_SELECT || ui.CurMenuMode == MenuMode.MAP || ui.CurMenuMode == MenuMode.CHEATS));
		// A click that lands nowhere on a menu list must do nothing, not activate the
		// previously highlighted entry.
		if (!MouseHandledByScreen && ui.ClickOutsideMenuEntries)
		{
			MouseHandledByScreen = true;
		}
	}

	/// <summary>
	/// The inventory key (and I) was pressed this frame. Consumed by the UI, which toggles the
	/// inventory itself.
	///
	/// Why this is not just "press Y": the console build uses Y for three different things
	/// depending on where you are - open the inventory (on release), switch the crafting
	/// section, toggle the NPC overlay. Mapping E onto Y meant a single press did two of them:
	/// the inventory opened, and the section under it changed on the way. That is the "one
	/// keypress, two actions" report.
	/// </summary>
	public static bool ConsumeInventoryToggle()
	{
		bool result = _inventoryToggle;
		_inventoryToggle = false;
		return result;
	}

	/// <summary>True while either Shift key is held. Shift-click means "quick transfer".</summary>
	public static bool ShiftDown => _keys.IsKeyDown(Keys.LeftShift) || _keys.IsKeyDown(Keys.RightShift);

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
		return BuildSyntheticState(_mouse, _prevMouse, _keys, MenuContext && !TextInputActive, TextInputActive || SuppressGameInput, flag2, _enterConfirms, _inventoryContext, _swallowMouseUntilRelease);
	}

	/// <summary>
	/// Pure mapping function: given a device snapshot, produce the equivalent pad state.
	/// Kept free of instance state so it can be exercised by the headless self-test.
	/// </summary>
	internal static GamePadState BuildSyntheticState(MouseState mouse, MouseState prevMouse, KeyboardState keys, bool menuContext, bool textInputActive, bool escapeAllowed = true, bool enterConfirms = false, bool inventoryContext = false, bool swallowMouse = false)
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
		bool left;
		bool right;
		bool middle;
		EffectiveMouseButtons(mouse, swallowMouse, out left, out right, out middle);

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
		// Inventory: in character creation Y means "randomize", and the screen listens for it.
		// Everywhere else the inventory key is an explicit toggle request (see
		// ConsumeInventoryToggle) because Y also switches the crafting section and toggles the
		// NPC overlay - one keypress used to do two of those at once.
		if (enterConfirms && (keys.IsKeyDown(KeyBindings.Get(KeyBindings.Action.Inventory)) || keys.IsKeyDown(Keys.I)))
		{
			buttons |= Buttons.Y; // randomize (character creation only)
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
		if (middle || keys.IsKeyDown(KeyBindings.Get(KeyBindings.Action.Grapple)))
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

	/// <summary>
	/// True while any key or button that could have closed a modal is still down. Confirm keys
	/// and cancel keys together: the latch exists to wait for the release, not to judge intent.
	/// </summary>
	private static bool ModalKeysDown()
	{
		bool anyPad = false;
		try
		{
			for (int i = 0; i < 4; i++)
			{
				GamePadState state = GamePad.GetState((PlayerIndex)i);
				if (state.IsConnected && (state.IsButtonDown(Buttons.A) || state.IsButtonDown(Buttons.B) || state.IsButtonDown(Buttons.Start) || state.IsButtonDown(Buttons.Back)))
				{
					anyPad = true;
					break;
				}
			}
		}
		catch
		{
			// No pad backend (headless); the keyboard half still answers.
		}
		return ModalInputDown(_keys, _mouse, anyPad);
	}

	/// <summary>
	/// The device-independent half of <see cref="ModalKeysDown"/>: is any confirm or cancel
	/// control still held? Pure so the headless suite can check the latch without a window.
	/// </summary>
	internal static bool ModalInputDown(KeyboardState keys, MouseState mouse, bool anyPadButtonDown)
	{
		if (keys.IsKeyDown(Keys.Enter) || keys.IsKeyDown(Keys.Escape) || keys.IsKeyDown(Keys.Space)
			|| keys.IsKeyDown(Keys.E) || keys.IsKeyDown(Keys.B))
		{
			return true;
		}
		if (mouse.LeftButton == ButtonState.Pressed || mouse.RightButton == ButtonState.Pressed)
		{
			return true;
		}
		return anyPadButtonDown;
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
