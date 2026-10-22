using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using MonoGame.Utilities;
using ObjCRuntime;
using SDL2;

namespace Microsoft.Xna.Framework;

internal static class SDL2_FNAPlatform
{
	private delegate void em_callback_func();

	private static string OSVersion;

	private static readonly bool UseScancodes = Environment.GetEnvironmentVariable("FNA_KEYBOARD_USE_SCANCODES") == "1";

	private static bool SupportsGlobalMouse;

	private static List<Game> activeGames = new List<Game>();

	private static Game emscriptenGame;

	private static bool micInit = false;

	private static IntPtr[] INTERNAL_devices = new IntPtr[GamePad.GAMEPAD_COUNT];

	private static Dictionary<int, int> INTERNAL_instanceList = new Dictionary<int, int>();

	private static string[] INTERNAL_guids = GenStringArray();

	private static GamePadState[] INTERNAL_states = new GamePadState[GamePad.GAMEPAD_COUNT];

	private static GamePadCapabilities[] INTERNAL_capabilities = new GamePadCapabilities[GamePad.GAMEPAD_COUNT];

	private static readonly GamePadType[] INTERNAL_gamepadType = new GamePadType[9]
	{
		GamePadType.Unknown,
		GamePadType.GamePad,
		GamePadType.Wheel,
		GamePadType.ArcadeStick,
		GamePadType.FlightStick,
		GamePadType.DancePad,
		GamePadType.Guitar,
		GamePadType.DrumKit,
		GamePadType.BigButtonPad
	};

	private static Dictionary<int, Keys> INTERNAL_keyMap = new Dictionary<int, Keys>
	{
		{
			97,
			Keys.A
		},
		{
			98,
			Keys.B
		},
		{
			99,
			Keys.C
		},
		{
			100,
			Keys.D
		},
		{
			101,
			Keys.E
		},
		{
			102,
			Keys.F
		},
		{
			103,
			Keys.G
		},
		{
			104,
			Keys.H
		},
		{
			105,
			Keys.I
		},
		{
			106,
			Keys.J
		},
		{
			107,
			Keys.K
		},
		{
			108,
			Keys.L
		},
		{
			109,
			Keys.M
		},
		{
			110,
			Keys.N
		},
		{
			111,
			Keys.O
		},
		{
			112,
			Keys.P
		},
		{
			113,
			Keys.Q
		},
		{
			114,
			Keys.R
		},
		{
			115,
			Keys.S
		},
		{
			116,
			Keys.T
		},
		{
			117,
			Keys.U
		},
		{
			118,
			Keys.V
		},
		{
			119,
			Keys.W
		},
		{
			120,
			Keys.X
		},
		{
			121,
			Keys.Y
		},
		{
			122,
			Keys.Z
		},
		{
			48,
			Keys.D0
		},
		{
			49,
			Keys.D1
		},
		{
			50,
			Keys.D2
		},
		{
			51,
			Keys.D3
		},
		{
			52,
			Keys.D4
		},
		{
			53,
			Keys.D5
		},
		{
			54,
			Keys.D6
		},
		{
			55,
			Keys.D7
		},
		{
			56,
			Keys.D8
		},
		{
			57,
			Keys.D9
		},
		{
			1073741922,
			Keys.NumPad0
		},
		{
			1073741913,
			Keys.NumPad1
		},
		{
			1073741914,
			Keys.NumPad2
		},
		{
			1073741915,
			Keys.NumPad3
		},
		{
			1073741916,
			Keys.NumPad4
		},
		{
			1073741917,
			Keys.NumPad5
		},
		{
			1073741918,
			Keys.NumPad6
		},
		{
			1073741919,
			Keys.NumPad7
		},
		{
			1073741920,
			Keys.NumPad8
		},
		{
			1073741921,
			Keys.NumPad9
		},
		{
			1073742040,
			Keys.OemClear
		},
		{
			1073742044,
			Keys.Decimal
		},
		{
			1073741908,
			Keys.Divide
		},
		{
			1073741912,
			Keys.Enter
		},
		{
			1073741910,
			Keys.Subtract
		},
		{
			1073741909,
			Keys.Multiply
		},
		{
			1073741923,
			Keys.OemPeriod
		},
		{
			1073741911,
			Keys.Add
		},
		{
			1073741882,
			Keys.F1
		},
		{
			1073741883,
			Keys.F2
		},
		{
			1073741884,
			Keys.F3
		},
		{
			1073741885,
			Keys.F4
		},
		{
			1073741886,
			Keys.F5
		},
		{
			1073741887,
			Keys.F6
		},
		{
			1073741888,
			Keys.F7
		},
		{
			1073741889,
			Keys.F8
		},
		{
			1073741890,
			Keys.F9
		},
		{
			1073741891,
			Keys.F10
		},
		{
			1073741892,
			Keys.F11
		},
		{
			1073741893,
			Keys.F12
		},
		{
			1073741928,
			Keys.F13
		},
		{
			1073741929,
			Keys.F14
		},
		{
			1073741930,
			Keys.F15
		},
		{
			1073741931,
			Keys.F16
		},
		{
			1073741932,
			Keys.F17
		},
		{
			1073741933,
			Keys.F18
		},
		{
			1073741934,
			Keys.F19
		},
		{
			1073741935,
			Keys.F20
		},
		{
			1073741936,
			Keys.F21
		},
		{
			1073741937,
			Keys.F22
		},
		{
			1073741938,
			Keys.F23
		},
		{
			1073741939,
			Keys.F24
		},
		{
			32,
			Keys.Space
		},
		{
			1073741906,
			Keys.Up
		},
		{
			1073741905,
			Keys.Down
		},
		{
			1073741904,
			Keys.Left
		},
		{
			1073741903,
			Keys.Right
		},
		{
			1073742050,
			Keys.LeftAlt
		},
		{
			1073742054,
			Keys.RightAlt
		},
		{
			1073742048,
			Keys.LeftControl
		},
		{
			1073742052,
			Keys.RightControl
		},
		{
			1073742051,
			Keys.LeftWindows
		},
		{
			1073742055,
			Keys.RightWindows
		},
		{
			1073742049,
			Keys.LeftShift
		},
		{
			1073742053,
			Keys.RightShift
		},
		{
			1073741925,
			Keys.Apps
		},
		{
			1073741942,
			Keys.Apps
		},
		{
			47,
			Keys.OemQuestion
		},
		{
			92,
			Keys.OemPipe
		},
		{
			91,
			Keys.OemOpenBrackets
		},
		{
			93,
			Keys.OemCloseBrackets
		},
		{
			1073741881,
			Keys.CapsLock
		},
		{
			44,
			Keys.OemComma
		},
		{
			127,
			Keys.Delete
		},
		{
			1073741901,
			Keys.End
		},
		{
			8,
			Keys.Back
		},
		{
			13,
			Keys.Enter
		},
		{
			27,
			Keys.Escape
		},
		{
			1073741898,
			Keys.Home
		},
		{
			1073741897,
			Keys.Insert
		},
		{
			45,
			Keys.OemMinus
		},
		{
			1073741907,
			Keys.NumLock
		},
		{
			1073741899,
			Keys.PageUp
		},
		{
			1073741902,
			Keys.PageDown
		},
		{
			1073741896,
			Keys.Pause
		},
		{
			46,
			Keys.OemPeriod
		},
		{
			61,
			Keys.OemPlus
		},
		{
			1073741894,
			Keys.PrintScreen
		},
		{
			39,
			Keys.OemQuotes
		},
		{
			1073741895,
			Keys.Scroll
		},
		{
			59,
			Keys.OemSemicolon
		},
		{
			1073742106,
			Keys.Sleep
		},
		{
			9,
			Keys.Tab
		},
		{
			96,
			Keys.OemTilde
		},
		{
			1073741952,
			Keys.VolumeUp
		},
		{
			1073741953,
			Keys.VolumeDown
		},
		{
			178,
			Keys.OemTilde
		},
		{
			233,
			Keys.None
		},
		{
			124,
			Keys.OemPipe
		},
		{
			43,
			Keys.OemPlus
		},
		{
			248,
			Keys.OemSemicolon
		},
		{
			230,
			Keys.OemQuotes
		},
		{
			0,
			Keys.None
		}
	};

	private static Dictionary<int, Keys> INTERNAL_scanMap = new Dictionary<int, Keys>
	{
		{
			4,
			Keys.A
		},
		{
			5,
			Keys.B
		},
		{
			6,
			Keys.C
		},
		{
			7,
			Keys.D
		},
		{
			8,
			Keys.E
		},
		{
			9,
			Keys.F
		},
		{
			10,
			Keys.G
		},
		{
			11,
			Keys.H
		},
		{
			12,
			Keys.I
		},
		{
			13,
			Keys.J
		},
		{
			14,
			Keys.K
		},
		{
			15,
			Keys.L
		},
		{
			16,
			Keys.M
		},
		{
			17,
			Keys.N
		},
		{
			18,
			Keys.O
		},
		{
			19,
			Keys.P
		},
		{
			20,
			Keys.Q
		},
		{
			21,
			Keys.R
		},
		{
			22,
			Keys.S
		},
		{
			23,
			Keys.T
		},
		{
			24,
			Keys.U
		},
		{
			25,
			Keys.V
		},
		{
			26,
			Keys.W
		},
		{
			27,
			Keys.X
		},
		{
			28,
			Keys.Y
		},
		{
			29,
			Keys.Z
		},
		{
			39,
			Keys.D0
		},
		{
			30,
			Keys.D1
		},
		{
			31,
			Keys.D2
		},
		{
			32,
			Keys.D3
		},
		{
			33,
			Keys.D4
		},
		{
			34,
			Keys.D5
		},
		{
			35,
			Keys.D6
		},
		{
			36,
			Keys.D7
		},
		{
			37,
			Keys.D8
		},
		{
			38,
			Keys.D9
		},
		{
			98,
			Keys.NumPad0
		},
		{
			89,
			Keys.NumPad1
		},
		{
			90,
			Keys.NumPad2
		},
		{
			91,
			Keys.NumPad3
		},
		{
			92,
			Keys.NumPad4
		},
		{
			93,
			Keys.NumPad5
		},
		{
			94,
			Keys.NumPad6
		},
		{
			95,
			Keys.NumPad7
		},
		{
			96,
			Keys.NumPad8
		},
		{
			97,
			Keys.NumPad9
		},
		{
			216,
			Keys.OemClear
		},
		{
			220,
			Keys.Decimal
		},
		{
			84,
			Keys.Divide
		},
		{
			88,
			Keys.Enter
		},
		{
			86,
			Keys.Subtract
		},
		{
			85,
			Keys.Multiply
		},
		{
			99,
			Keys.OemPeriod
		},
		{
			87,
			Keys.Add
		},
		{
			58,
			Keys.F1
		},
		{
			59,
			Keys.F2
		},
		{
			60,
			Keys.F3
		},
		{
			61,
			Keys.F4
		},
		{
			62,
			Keys.F5
		},
		{
			63,
			Keys.F6
		},
		{
			64,
			Keys.F7
		},
		{
			65,
			Keys.F8
		},
		{
			66,
			Keys.F9
		},
		{
			67,
			Keys.F10
		},
		{
			68,
			Keys.F11
		},
		{
			69,
			Keys.F12
		},
		{
			104,
			Keys.F13
		},
		{
			105,
			Keys.F14
		},
		{
			106,
			Keys.F15
		},
		{
			107,
			Keys.F16
		},
		{
			108,
			Keys.F17
		},
		{
			109,
			Keys.F18
		},
		{
			110,
			Keys.F19
		},
		{
			111,
			Keys.F20
		},
		{
			112,
			Keys.F21
		},
		{
			113,
			Keys.F22
		},
		{
			114,
			Keys.F23
		},
		{
			115,
			Keys.F24
		},
		{
			44,
			Keys.Space
		},
		{
			82,
			Keys.Up
		},
		{
			81,
			Keys.Down
		},
		{
			80,
			Keys.Left
		},
		{
			79,
			Keys.Right
		},
		{
			226,
			Keys.LeftAlt
		},
		{
			230,
			Keys.RightAlt
		},
		{
			224,
			Keys.LeftControl
		},
		{
			228,
			Keys.RightControl
		},
		{
			227,
			Keys.LeftWindows
		},
		{
			231,
			Keys.RightWindows
		},
		{
			225,
			Keys.LeftShift
		},
		{
			229,
			Keys.RightShift
		},
		{
			101,
			Keys.Apps
		},
		{
			118,
			Keys.Apps
		},
		{
			56,
			Keys.OemQuestion
		},
		{
			49,
			Keys.OemPipe
		},
		{
			47,
			Keys.OemOpenBrackets
		},
		{
			48,
			Keys.OemCloseBrackets
		},
		{
			57,
			Keys.CapsLock
		},
		{
			54,
			Keys.OemComma
		},
		{
			76,
			Keys.Delete
		},
		{
			77,
			Keys.End
		},
		{
			42,
			Keys.Back
		},
		{
			40,
			Keys.Enter
		},
		{
			41,
			Keys.Escape
		},
		{
			74,
			Keys.Home
		},
		{
			73,
			Keys.Insert
		},
		{
			45,
			Keys.OemMinus
		},
		{
			83,
			Keys.NumLock
		},
		{
			75,
			Keys.PageUp
		},
		{
			78,
			Keys.PageDown
		},
		{
			72,
			Keys.Pause
		},
		{
			55,
			Keys.OemPeriod
		},
		{
			46,
			Keys.OemPlus
		},
		{
			70,
			Keys.PrintScreen
		},
		{
			52,
			Keys.OemQuotes
		},
		{
			71,
			Keys.Scroll
		},
		{
			51,
			Keys.OemSemicolon
		},
		{
			282,
			Keys.Sleep
		},
		{
			43,
			Keys.Tab
		},
		{
			53,
			Keys.OemTilde
		},
		{
			128,
			Keys.VolumeUp
		},
		{
			129,
			Keys.VolumeDown
		},
		{
			0,
			Keys.None
		},
		{
			50,
			Keys.None
		},
		{
			100,
			Keys.None
		}
	};

	private static Dictionary<int, SDL.SDL_Scancode> INTERNAL_xnaMap = new Dictionary<int, SDL.SDL_Scancode>
	{
		{
			65,
			SDL.SDL_Scancode.SDL_SCANCODE_A
		},
		{
			66,
			SDL.SDL_Scancode.SDL_SCANCODE_B
		},
		{
			67,
			SDL.SDL_Scancode.SDL_SCANCODE_C
		},
		{
			68,
			SDL.SDL_Scancode.SDL_SCANCODE_D
		},
		{
			69,
			SDL.SDL_Scancode.SDL_SCANCODE_E
		},
		{
			70,
			SDL.SDL_Scancode.SDL_SCANCODE_F
		},
		{
			71,
			SDL.SDL_Scancode.SDL_SCANCODE_G
		},
		{
			72,
			SDL.SDL_Scancode.SDL_SCANCODE_H
		},
		{
			73,
			SDL.SDL_Scancode.SDL_SCANCODE_I
		},
		{
			74,
			SDL.SDL_Scancode.SDL_SCANCODE_J
		},
		{
			75,
			SDL.SDL_Scancode.SDL_SCANCODE_K
		},
		{
			76,
			SDL.SDL_Scancode.SDL_SCANCODE_L
		},
		{
			77,
			SDL.SDL_Scancode.SDL_SCANCODE_M
		},
		{
			78,
			SDL.SDL_Scancode.SDL_SCANCODE_N
		},
		{
			79,
			SDL.SDL_Scancode.SDL_SCANCODE_O
		},
		{
			80,
			SDL.SDL_Scancode.SDL_SCANCODE_P
		},
		{
			81,
			SDL.SDL_Scancode.SDL_SCANCODE_Q
		},
		{
			82,
			SDL.SDL_Scancode.SDL_SCANCODE_R
		},
		{
			83,
			SDL.SDL_Scancode.SDL_SCANCODE_S
		},
		{
			84,
			SDL.SDL_Scancode.SDL_SCANCODE_T
		},
		{
			85,
			SDL.SDL_Scancode.SDL_SCANCODE_U
		},
		{
			86,
			SDL.SDL_Scancode.SDL_SCANCODE_V
		},
		{
			87,
			SDL.SDL_Scancode.SDL_SCANCODE_W
		},
		{
			88,
			SDL.SDL_Scancode.SDL_SCANCODE_X
		},
		{
			89,
			SDL.SDL_Scancode.SDL_SCANCODE_Y
		},
		{
			90,
			SDL.SDL_Scancode.SDL_SCANCODE_Z
		},
		{
			48,
			SDL.SDL_Scancode.SDL_SCANCODE_0
		},
		{
			49,
			SDL.SDL_Scancode.SDL_SCANCODE_1
		},
		{
			50,
			SDL.SDL_Scancode.SDL_SCANCODE_2
		},
		{
			51,
			SDL.SDL_Scancode.SDL_SCANCODE_3
		},
		{
			52,
			SDL.SDL_Scancode.SDL_SCANCODE_4
		},
		{
			53,
			SDL.SDL_Scancode.SDL_SCANCODE_5
		},
		{
			54,
			SDL.SDL_Scancode.SDL_SCANCODE_6
		},
		{
			55,
			SDL.SDL_Scancode.SDL_SCANCODE_7
		},
		{
			56,
			SDL.SDL_Scancode.SDL_SCANCODE_8
		},
		{
			57,
			SDL.SDL_Scancode.SDL_SCANCODE_9
		},
		{
			96,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_0
		},
		{
			97,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_1
		},
		{
			98,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_2
		},
		{
			99,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_3
		},
		{
			100,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_4
		},
		{
			101,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_5
		},
		{
			102,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_6
		},
		{
			103,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_7
		},
		{
			104,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_8
		},
		{
			105,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_9
		},
		{
			254,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_CLEAR
		},
		{
			110,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_DECIMAL
		},
		{
			111,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_DIVIDE
		},
		{
			106,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY
		},
		{
			109,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_MINUS
		},
		{
			107,
			SDL.SDL_Scancode.SDL_SCANCODE_KP_PLUS
		},
		{
			112,
			SDL.SDL_Scancode.SDL_SCANCODE_F1
		},
		{
			113,
			SDL.SDL_Scancode.SDL_SCANCODE_F2
		},
		{
			114,
			SDL.SDL_Scancode.SDL_SCANCODE_F3
		},
		{
			115,
			SDL.SDL_Scancode.SDL_SCANCODE_F4
		},
		{
			116,
			SDL.SDL_Scancode.SDL_SCANCODE_F5
		},
		{
			117,
			SDL.SDL_Scancode.SDL_SCANCODE_F6
		},
		{
			118,
			SDL.SDL_Scancode.SDL_SCANCODE_F7
		},
		{
			119,
			SDL.SDL_Scancode.SDL_SCANCODE_F8
		},
		{
			120,
			SDL.SDL_Scancode.SDL_SCANCODE_F9
		},
		{
			121,
			SDL.SDL_Scancode.SDL_SCANCODE_F10
		},
		{
			122,
			SDL.SDL_Scancode.SDL_SCANCODE_F11
		},
		{
			123,
			SDL.SDL_Scancode.SDL_SCANCODE_F12
		},
		{
			124,
			SDL.SDL_Scancode.SDL_SCANCODE_F13
		},
		{
			125,
			SDL.SDL_Scancode.SDL_SCANCODE_F14
		},
		{
			126,
			SDL.SDL_Scancode.SDL_SCANCODE_F15
		},
		{
			127,
			SDL.SDL_Scancode.SDL_SCANCODE_F16
		},
		{
			128,
			SDL.SDL_Scancode.SDL_SCANCODE_F17
		},
		{
			129,
			SDL.SDL_Scancode.SDL_SCANCODE_F18
		},
		{
			130,
			SDL.SDL_Scancode.SDL_SCANCODE_F19
		},
		{
			131,
			SDL.SDL_Scancode.SDL_SCANCODE_F20
		},
		{
			132,
			SDL.SDL_Scancode.SDL_SCANCODE_F21
		},
		{
			133,
			SDL.SDL_Scancode.SDL_SCANCODE_F22
		},
		{
			134,
			SDL.SDL_Scancode.SDL_SCANCODE_F23
		},
		{
			135,
			SDL.SDL_Scancode.SDL_SCANCODE_F24
		},
		{
			32,
			SDL.SDL_Scancode.SDL_SCANCODE_SPACE
		},
		{
			38,
			SDL.SDL_Scancode.SDL_SCANCODE_UP
		},
		{
			40,
			SDL.SDL_Scancode.SDL_SCANCODE_DOWN
		},
		{
			37,
			SDL.SDL_Scancode.SDL_SCANCODE_LEFT
		},
		{
			39,
			SDL.SDL_Scancode.SDL_SCANCODE_RIGHT
		},
		{
			164,
			SDL.SDL_Scancode.SDL_SCANCODE_LALT
		},
		{
			165,
			SDL.SDL_Scancode.SDL_SCANCODE_RALT
		},
		{
			162,
			SDL.SDL_Scancode.SDL_SCANCODE_LCTRL
		},
		{
			163,
			SDL.SDL_Scancode.SDL_SCANCODE_RCTRL
		},
		{
			91,
			SDL.SDL_Scancode.SDL_SCANCODE_LGUI
		},
		{
			92,
			SDL.SDL_Scancode.SDL_SCANCODE_RGUI
		},
		{
			160,
			SDL.SDL_Scancode.SDL_SCANCODE_LSHIFT
		},
		{
			161,
			SDL.SDL_Scancode.SDL_SCANCODE_RSHIFT
		},
		{
			93,
			SDL.SDL_Scancode.SDL_SCANCODE_APPLICATION
		},
		{
			191,
			SDL.SDL_Scancode.SDL_SCANCODE_SLASH
		},
		{
			220,
			SDL.SDL_Scancode.SDL_SCANCODE_BACKSLASH
		},
		{
			219,
			SDL.SDL_Scancode.SDL_SCANCODE_LEFTBRACKET
		},
		{
			221,
			SDL.SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET
		},
		{
			20,
			SDL.SDL_Scancode.SDL_SCANCODE_CAPSLOCK
		},
		{
			188,
			SDL.SDL_Scancode.SDL_SCANCODE_COMMA
		},
		{
			46,
			SDL.SDL_Scancode.SDL_SCANCODE_DELETE
		},
		{
			35,
			SDL.SDL_Scancode.SDL_SCANCODE_END
		},
		{
			8,
			SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE
		},
		{
			13,
			SDL.SDL_Scancode.SDL_SCANCODE_RETURN
		},
		{
			27,
			SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE
		},
		{
			36,
			SDL.SDL_Scancode.SDL_SCANCODE_HOME
		},
		{
			45,
			SDL.SDL_Scancode.SDL_SCANCODE_INSERT
		},
		{
			189,
			SDL.SDL_Scancode.SDL_SCANCODE_MINUS
		},
		{
			144,
			SDL.SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR
		},
		{
			33,
			SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP
		},
		{
			34,
			SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN
		},
		{
			19,
			SDL.SDL_Scancode.SDL_SCANCODE_PAUSE
		},
		{
			190,
			SDL.SDL_Scancode.SDL_SCANCODE_PERIOD
		},
		{
			187,
			SDL.SDL_Scancode.SDL_SCANCODE_EQUALS
		},
		{
			44,
			SDL.SDL_Scancode.SDL_SCANCODE_PRINTSCREEN
		},
		{
			222,
			SDL.SDL_Scancode.SDL_SCANCODE_APOSTROPHE
		},
		{
			145,
			SDL.SDL_Scancode.SDL_SCANCODE_SCROLLLOCK
		},
		{
			186,
			SDL.SDL_Scancode.SDL_SCANCODE_SEMICOLON
		},
		{
			95,
			SDL.SDL_Scancode.SDL_SCANCODE_SLEEP
		},
		{
			9,
			SDL.SDL_Scancode.SDL_SCANCODE_TAB
		},
		{
			192,
			SDL.SDL_Scancode.SDL_SCANCODE_GRAVE
		},
		{
			175,
			SDL.SDL_Scancode.SDL_SCANCODE_VOLUMEUP
		},
		{
			174,
			SDL.SDL_Scancode.SDL_SCANCODE_VOLUMEDOWN
		},
		{
			0,
			SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN
		}
	};

	private static SDL.SDL_EventFilter win32OnPaint = Win32OnPaint;

	private static SDL.SDL_EventFilter prevEventFilter;

	public static IntPtr Malloc(int size)
	{
		return SDL.SDL_malloc((IntPtr)size);
	}

	public static string ProgramInit(LaunchParameters args)
	{
		try
		{
			OSVersion = SDL.SDL_GetPlatform();
		}
		catch (DllNotFoundException)
		{
			FNALoggerEXT.LogError("SDL2 was not found! Do you have fnalibs?");
			throw;
		}
		catch (BadImageFormatException inner)
		{
			string text = string.Format("This process is {0}-bit, the DLL is {1}-bit!", (IntPtr.Size == 4) ? "32" : "64", (IntPtr.Size == 4) ? "64" : "32");
			FNALoggerEXT.LogError(text);
			throw new BadImageFormatException(text, inner);
		}
		SDL.SDL_SetMainReady();
		if (OSVersion.Equals("Windows") || OSVersion.Equals("WinRT"))
		{
			if (Debugger.IsAttached)
			{
				SDL.SDL_SetHint("SDL_WINDOWS_DISABLE_THREAD_NAMING", "1");
			}
			SDL.SDL_GetEventFilter(out prevEventFilter, out var userdata);
			SDL.SDL_SetEventFilter(win32OnPaint, userdata);
		}
		string baseDirectory = GetBaseDirectory();
		string text2 = Path.Combine(baseDirectory, "gamecontrollerdb.txt");
		if (File.Exists(text2))
		{
			SDL.SDL_SetHint("SDL_GAMECONTROLLERCONFIG_FILE", text2);
		}
		SDL.SDL_SetHintWithPriority("SDL_GAMECONTROLLER_USE_BUTTON_LABELS", "0", SDL.SDL_HintPriority.SDL_HINT_OVERRIDE);
		if (Environment.GetEnvironmentVariable("FNA_NUKE_STEAM_INPUT") == "1")
		{
			SDL.SDL_SetHintWithPriority("SDL_GAMECONTROLLER_IGNORE_DEVICES", "0x28DE/0x11FF", SDL.SDL_HintPriority.SDL_HINT_OVERRIDE);
			SDL.SDL_SetHintWithPriority("SDL_GAMECONTROLLER_IGNORE_DEVICES_EXCEPT", "", SDL.SDL_HintPriority.SDL_HINT_OVERRIDE);
			SDL.SDL_SetHintWithPriority("SDL_GAMECONTROLLER_ALLOW_STEAM_VIRTUAL_GAMEPAD", "0", SDL.SDL_HintPriority.SDL_HINT_OVERRIDE);
		}
		if (args.TryGetValue("glprofile", out var value))
		{
			switch (value)
			{
			case "es3":
				Environment.SetEnvironmentVariable("FNA3D_OPENGL_FORCE_ES3", "1");
				break;
			case "core":
				Environment.SetEnvironmentVariable("FNA3D_OPENGL_FORCE_CORE_PROFILE", "1");
				break;
			case "compatibility":
				Environment.SetEnvironmentVariable("FNA3D_OPENGL_FORCE_COMPATIBILITY_PROFILE", "1");
				break;
			}
		}
		if (args.TryGetValue("angle", out value) && value == "1")
		{
			Environment.SetEnvironmentVariable("FNA3D_OPENGL_FORCE_ES3", "1");
			Environment.SetEnvironmentVariable("SDL_OPENGL_ES_DRIVER", "1");
		}
		if (args.TryGetValue("forcemailboxvsync", out value) && value == "1")
		{
			Environment.SetEnvironmentVariable("FNA3D_VULKAN_FORCE_MAILBOX_VSYNC", "1");
		}
		SDL.SDL_Init(12832u);
		string text3 = SDL.SDL_GetCurrentVideoDriver();
		SupportsGlobalMouse = OSVersion.Equals("Windows") || OSVersion.Equals("Mac OS X") || text3.Equals("x11");
		if (!text3.Equals("wayland") && !text3.Equals("cocoa") && !text3.Equals("uikit"))
		{
			SDL.SDL_SetHintWithPriority("SDL_VIDEO_HIGHDPI_DISABLED", "1", SDL.SDL_HintPriority.SDL_HINT_NORMAL);
		}
		if (OSVersion.Equals("Windows") || OSVersion.Equals("WinRT"))
		{
			SDL.SDL_SetHint("SDL_VIDEO_MINIMIZE_ON_FOCUS_LOSS", "1");
		}
		string value2 = SDL.SDL_GetHint("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS");
		if (string.IsNullOrEmpty(value2))
		{
			SDL.SDL_SetHint("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS", "1");
		}
		SDL.SDL_SetHint("SDL_IOS_ORIENTATIONS", "LandscapeLeft LandscapeRight Portrait");
		SDL.SDL_Event[] array = new SDL.SDL_Event[1];
		SDL.SDL_PumpEvents();
		while (SDL.SDL_PeepEvents(array, 1, SDL.SDL_eventaction.SDL_GETEVENT, SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED, SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED) == 1)
		{
			INTERNAL_AddInstance(array[0].cdevice.which);
		}
		return baseDirectory;
	}

	public static void ProgramExit(object sender, EventArgs e)
	{
		AudioEngine.ProgramExiting = true;
		if (SoundEffect.FAudioContext.Context != null)
		{
			SoundEffect.FAudioContext.Context.Dispose();
		}
		MediaPlayer.DisposeIfNecessary();
		try
		{
			SDL.SDL_Quit();
		}
		catch (DllNotFoundException)
		{
			// Headless commands (--selftest, --tconfig-convert, --lang-export) never open a window
			// and may run on a machine with no SDL at all. SDL_Quit is then an unhandled exception
			// on the process-exit path, which prints a stack trace *after* the command already
			// succeeded - so a working conversion looked like a crash.
		}
		catch (EntryPointNotFoundException)
		{
			// Same story with an older SDL that has no SDL_Quit symbol.
		}
	}

	public static GameWindow CreateWindow()
	{
		SDL.SDL_WindowFlags sDL_WindowFlags = (SDL.SDL_WindowFlags)(0x608 | FNA3D.FNA3D_PrepareWindowAttributes());
		if ((sDL_WindowFlags & SDL.SDL_WindowFlags.SDL_WINDOW_VULKAN) == SDL.SDL_WindowFlags.SDL_WINDOW_VULKAN)
		{
			string text = SDL.SDL_GetHint("FNA3D_VULKAN_PIPELINE_CACHE_FILE_NAME");
			if (text == null)
			{
				text = ((!OSVersion.Equals("Windows") && !OSVersion.Equals("Mac OS X") && !OSVersion.Equals("Linux") && !OSVersion.Equals("FreeBSD") && !OSVersion.Equals("OpenBSD") && !OSVersion.Equals("NetBSD")) ? string.Empty : "FNA3D_Vulkan_PipelineCache.blob");
				SDL.SDL_SetHint("FNA3D_VULKAN_PIPELINE_CACHE_FILE_NAME", text);
			}
		}
		if (Environment.GetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI") == "1")
		{
			sDL_WindowFlags |= SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI;
		}
		string defaultWindowTitle = AssemblyHelper.GetDefaultWindowTitle();
		IntPtr intPtr = SDL.SDL_CreateWindow(defaultWindowTitle, 805240832, 805240832, GraphicsDeviceManager.DefaultBackBufferWidth, GraphicsDeviceManager.DefaultBackBufferHeight, sDL_WindowFlags);
		if (intPtr == IntPtr.Zero)
		{
			throw new NoSuitableGraphicsDeviceException(SDL.SDL_GetError());
		}
		INTERNAL_SetIcon(intPtr, defaultWindowTitle);
		SDL.SDL_DisableScreenSaver();
		OnIsMouseVisibleChanged(visible: false);
		sDL_WindowFlags = (SDL.SDL_WindowFlags)SDL.SDL_GetWindowFlags(intPtr);
		if ((sDL_WindowFlags & SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI) == 0)
		{
			Environment.SetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI", "0");
		}
		return new FNAWindow(intPtr, "\\\\.\\DISPLAY" + (SDL.SDL_GetWindowDisplayIndex(intPtr) + 1));
	}

	public static void DisposeWindow(GameWindow window)
	{
		SDL.SDL_SetHintWithPriority("SDL_VIDEO_MINIMIZE_ON_FOCUS_LOSS", "0", SDL.SDL_HintPriority.SDL_HINT_OVERRIDE);
		if (Mouse.WindowHandle == window.Handle)
		{
			Mouse.WindowHandle = IntPtr.Zero;
		}
		if (TouchPanel.WindowHandle == window.Handle)
		{
			TouchPanel.WindowHandle = IntPtr.Zero;
		}
		SDL.SDL_DestroyWindow(window.Handle);
	}

	public static void ApplyWindowChanges(IntPtr window, int clientWidth, int clientHeight, bool wantsFullscreen, string screenDeviceName, ref string resultDeviceName)
	{
		bool flag = false;
		ScaleForWindow(window, invert: false, ref clientWidth, ref clientHeight);
		if (!wantsFullscreen)
		{
			bool flag2 = false;
			if ((SDL.SDL_GetWindowFlags(window) & 1) != 0)
			{
				SDL.SDL_SetWindowFullscreen(window, 0u);
				flag2 = true;
			}
			else
			{
				SDL.SDL_GetWindowSize(window, out var w, out var h);
				flag2 = clientWidth != w || clientHeight != h;
			}
			if (flag2)
			{
				SDL.SDL_RestoreWindow(window);
				SDL.SDL_SetWindowSize(window, clientWidth, clientHeight);
				flag = true;
			}
		}
		int num = 0;
		for (int i = 0; i < GraphicsAdapter.Adapters.Count; i++)
		{
			if (screenDeviceName == GraphicsAdapter.Adapters[i].DeviceName)
			{
				num = i;
				break;
			}
		}
		if (resultDeviceName != screenDeviceName)
		{
			SDL.SDL_SetWindowFullscreen(window, 0u);
			resultDeviceName = screenDeviceName;
			flag = true;
		}
		if (flag)
		{
			int num2 = SDL.SDL_WINDOWPOS_CENTERED_DISPLAY(num);
			SDL.SDL_SetWindowPosition(window, num2, num2);
		}
		if (wantsFullscreen)
		{
			if ((SDL.SDL_GetWindowFlags(window) & 4) == 0)
			{
				SDL.SDL_GetCurrentDisplayMode(num, out var mode);
				SDL.SDL_SetWindowSize(window, mode.w, mode.h);
			}
			SDL.SDL_SetWindowFullscreen(window, 4097u);
		}
		if (Mouse.WindowHandle == window)
		{
			Rectangle windowBounds = GetWindowBounds(window);
			Mouse.INTERNAL_WindowWidth = windowBounds.Width;
			Mouse.INTERNAL_WindowHeight = windowBounds.Height;
		}
	}

	public static void ScaleForWindow(IntPtr window, bool invert, ref int w, ref int h)
	{
		SDL.SDL_GetWindowSize(window, out var w2, out var h2);
		FNA3D.FNA3D_GetDrawableSize(window, out var w3, out var h3);
		if (w2 != 0 && h2 != 0 && w3 != 0 && h3 != 0 && (w2 != w3 || h2 != h3))
		{
			if (invert)
			{
				w = (int)((float)w * ((float)w3 / (float)w2));
				h = (int)((float)h * ((float)h3 / (float)h2));
			}
			else
			{
				w = (int)((float)w / ((float)w3 / (float)w2));
				h = (int)((float)h / ((float)h3 / (float)h2));
			}
		}
	}

	public static Rectangle GetWindowBounds(IntPtr window)
	{
		Rectangle result = default;
		if ((SDL.SDL_GetWindowFlags(window) & 1) != 0)
		{
			SDL.SDL_GetCurrentDisplayMode(SDL.SDL_GetWindowDisplayIndex(window), out var mode);
			result.X = 0;
			result.Y = 0;
			result.Width = mode.w;
			result.Height = mode.h;
		}
		else
		{
			SDL.SDL_GetWindowPosition(window, out result.X, out result.Y);
			SDL.SDL_GetWindowSize(window, out result.Width, out result.Height);
		}
		return result;
	}

	public static bool GetWindowResizable(IntPtr window)
	{
		return (SDL.SDL_GetWindowFlags(window) & 0x20) != 0;
	}

	public static void SetWindowResizable(IntPtr window, bool resizable)
	{
		SDL.SDL_SetWindowResizable(window, resizable ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE);
	}

	public static bool GetWindowBorderless(IntPtr window)
	{
		return (SDL.SDL_GetWindowFlags(window) & 0x10) != 0;
	}

	public static void SetWindowBorderless(IntPtr window, bool borderless)
	{
		SDL.SDL_SetWindowBordered(window, (!borderless) ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE);
	}

	public static void SetWindowTitle(IntPtr window, string title)
	{
		SDL.SDL_SetWindowTitle(window, title);
	}

	private static void INTERNAL_SetIcon(IntPtr window, string title)
	{
		string empty = string.Empty;
		try
		{
			empty = INTERNAL_GetIconName(title + ".png");
			if (!string.IsNullOrEmpty(empty))
			{
				IntPtr intPtr;
				IntPtr intPtr2;
				using (Stream stream = TitleContainer.OpenStream(empty))
				{
					intPtr = FNA3D.ReadImageStream(stream, out var width, out var height, out var _);
					intPtr2 = SDL.SDL_CreateRGBSurfaceFrom(intPtr, width, height, 32, width * 4, 255u, 65280u, 16711680u, 4278190080u);
				}
				SDL.SDL_SetWindowIcon(window, intPtr2);
				SDL.SDL_FreeSurface(intPtr2);
				FNA3D.FNA3D_Image_Free(intPtr);
				return;
			}
		}
		catch (DllNotFoundException)
		{
		}
		empty = INTERNAL_GetIconName(title + ".bmp");
		if (!string.IsNullOrEmpty(empty))
		{
			IntPtr intPtr3 = SDL.SDL_LoadBMP(empty);
			SDL.SDL_SetWindowIcon(window, intPtr3);
			SDL.SDL_FreeSurface(intPtr3);
		}
	}

	private static string INTERNAL_GetIconName(string title)
	{
		string text = Path.Combine(TitleLocation.Path, title);
		if (File.Exists(text))
		{
			return text;
		}
		text = Path.Combine(TitleLocation.Path, INTERNAL_StripBadChars(title));
		if (File.Exists(text))
		{
			return text;
		}
		return string.Empty;
	}

	private static string INTERNAL_StripBadChars(string path)
	{
		char[] collection = new char[9] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
		List<char> list = new List<char>();
		list.AddRange(Path.GetInvalidFileNameChars());
		list.AddRange(collection);
		string text = path;
		foreach (char item in list)
		{
			text = text.Replace(item.ToString(), "");
		}
		return text;
	}

	public static void SetTextInputRectangle(Rectangle rectangle)
	{
		SDL.SDL_Rect rect = new SDL.SDL_Rect
		{
			x = rectangle.X,
			y = rectangle.Y,
			w = rectangle.Width,
			h = rectangle.Height
		};
		SDL.SDL_SetTextInputRect(ref rect);
	}

	private static DisplayOrientation INTERNAL_ConvertOrientation(SDL.SDL_DisplayOrientation orientation)
	{
		if (1 == 0)
		{
		}
		DisplayOrientation result = orientation switch
		{
			SDL.SDL_DisplayOrientation.SDL_ORIENTATION_LANDSCAPE => DisplayOrientation.LandscapeLeft, 
			SDL.SDL_DisplayOrientation.SDL_ORIENTATION_LANDSCAPE_FLIPPED => DisplayOrientation.LandscapeRight, 
			SDL.SDL_DisplayOrientation.SDL_ORIENTATION_PORTRAIT => DisplayOrientation.Portrait, 
			_ => throw new NotSupportedException("FNA does not support this device orientation."), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static void INTERNAL_HandleOrientationChange(DisplayOrientation orientation, GraphicsDevice graphicsDevice, FNAWindow window)
	{
		int backBufferWidth = graphicsDevice.PresentationParameters.BackBufferWidth;
		int backBufferHeight = graphicsDevice.PresentationParameters.BackBufferHeight;
		int num = Math.Min(backBufferWidth, backBufferHeight);
		int num2 = Math.Max(backBufferWidth, backBufferHeight);
		if (orientation == DisplayOrientation.Portrait)
		{
			graphicsDevice.PresentationParameters.BackBufferWidth = num;
			graphicsDevice.PresentationParameters.BackBufferHeight = num2;
		}
		else
		{
			graphicsDevice.PresentationParameters.BackBufferWidth = num2;
			graphicsDevice.PresentationParameters.BackBufferHeight = num;
		}
		graphicsDevice.PresentationParameters.DisplayOrientation = orientation;
		window.CurrentOrientation = orientation;
		graphicsDevice.Reset();
		window.INTERNAL_OnOrientationChanged();
	}

	public static bool SupportsOrientationChanges()
	{
		return OSVersion.Equals("iOS") || OSVersion.Equals("Android");
	}

	public static GraphicsAdapter RegisterGame(Game game)
	{
		SDL.SDL_ShowWindow(game.Window.Handle);
		activeGames.Add(game);
		int index = SDL.SDL_GetWindowDisplayIndex(game.Window.Handle);
		return GraphicsAdapter.Adapters[index];
	}

	public static void UnregisterGame(Game game)
	{
		activeGames.Remove(game);
	}

	public unsafe static void PollEvents(Game game, ref GraphicsAdapter currentAdapter, bool[] textInputControlDown, int[] textInputControlRepeat, ref bool textInputSuppress)
	{
		char* ptr = stackalloc char[32];
		SDL.SDL_Event _event;
		while (SDL.SDL_PollEvent(out _event) == 1)
		{
			if (_event.type == SDL.SDL_EventType.SDL_KEYDOWN)
			{
				Keys keys = ToXNAKey(ref _event.key.keysym);
				if (!Keyboard.keys.Contains(keys))
				{
					Keyboard.keys.Add(keys);
					if (FNAPlatform.TextInputBindings.TryGetValue(keys, out var value))
					{
						textInputControlDown[value] = true;
						textInputControlRepeat[value] = Environment.TickCount + 400;
						TextInputEXT.OnTextInput(FNAPlatform.TextInputCharacters[value]);
					}
					else if (Keyboard.keys.Contains(Keys.LeftControl) && keys == Keys.V)
					{
						textInputControlDown[6] = true;
						textInputControlRepeat[6] = Environment.TickCount + 400;
						TextInputEXT.OnTextInput(FNAPlatform.TextInputCharacters[6]);
						textInputSuppress = true;
					}
				}
			}
			else if (_event.type == SDL.SDL_EventType.SDL_KEYUP)
			{
				Keys keys2 = ToXNAKey(ref _event.key.keysym);
				if (Keyboard.keys.Remove(keys2))
				{
					if (FNAPlatform.TextInputBindings.TryGetValue(keys2, out var value2))
					{
						textInputControlDown[value2] = false;
					}
					else if ((!Keyboard.keys.Contains(Keys.LeftControl) && textInputControlDown[6]) || keys2 == Keys.V)
					{
						textInputControlDown[6] = false;
						textInputSuppress = false;
					}
				}
			}
			else if (_event.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN)
			{
				Mouse.INTERNAL_onClicked(_event.button.button - 1);
			}
			else if (_event.type == SDL.SDL_EventType.SDL_MOUSEWHEEL)
			{
				Mouse.INTERNAL_MouseWheel += _event.wheel.y * 120;
			}
			else if (_event.type == SDL.SDL_EventType.SDL_FINGERDOWN)
			{
				TouchPanel.TouchDeviceExists = true;
				TouchPanel.INTERNAL_onTouchEvent((int)_event.tfinger.fingerId, TouchLocationState.Pressed, _event.tfinger.x, _event.tfinger.y, 0f, 0f);
			}
			else if (_event.type == SDL.SDL_EventType.SDL_FINGERMOTION)
			{
				TouchPanel.INTERNAL_onTouchEvent((int)_event.tfinger.fingerId, TouchLocationState.Moved, _event.tfinger.x, _event.tfinger.y, _event.tfinger.dx, _event.tfinger.dy);
			}
			else if (_event.type == SDL.SDL_EventType.SDL_FINGERUP)
			{
				TouchPanel.INTERNAL_onTouchEvent((int)_event.tfinger.fingerId, TouchLocationState.Released, _event.tfinger.x, _event.tfinger.y, 0f, 0f);
			}
			else if (_event.type == SDL.SDL_EventType.SDL_WINDOWEVENT)
			{
				if (_event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED)
				{
					game.IsActive = true;
					if (SDL.SDL_GetCurrentVideoDriver() == "x11")
					{
						SDL.SDL_SetWindowFullscreen(game.Window.Handle, game.GraphicsDevice.PresentationParameters.IsFullScreen ? 4097u : 0u);
					}
					SDL.SDL_DisableScreenSaver();
				}
				else if (_event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST)
				{
					game.IsActive = false;
					if (SDL.SDL_GetCurrentVideoDriver() == "x11")
					{
						SDL.SDL_SetWindowFullscreen(game.Window.Handle, 0u);
					}
					SDL.SDL_EnableScreenSaver();
				}
				else if (_event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED)
				{
					Mouse.INTERNAL_WindowWidth = _event.window.data1;
					Mouse.INTERNAL_WindowHeight = _event.window.data2;
				}
				else if (_event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
				{
					uint num = SDL.SDL_GetWindowFlags(game.Window.Handle);
					if ((num & 0x20) != 0 && (num & 0x200) != 0)
					{
						((FNAWindow)game.Window).INTERNAL_ClientSizeChanged();
					}
				}
				else if (_event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_EXPOSED)
				{
					game.RedrawWindow();
				}
				else if (_event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MOVED)
				{
					int index = SDL.SDL_GetWindowDisplayIndex(game.Window.Handle);
					if (GraphicsAdapter.Adapters[index] != currentAdapter)
					{
						currentAdapter = GraphicsAdapter.Adapters[index];
						game.GraphicsDevice.Reset(game.GraphicsDevice.PresentationParameters, currentAdapter);
					}
				}
				else if (_event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER)
				{
					SDL.SDL_DisableScreenSaver();
				}
				else if (_event.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE)
				{
					SDL.SDL_EnableScreenSaver();
				}
			}
			else if (_event.type == SDL.SDL_EventType.SDL_DISPLAYEVENT)
			{
				GraphicsAdapter.AdaptersChanged();
				int index2 = SDL.SDL_GetWindowDisplayIndex(game.Window.Handle);
				currentAdapter = GraphicsAdapter.Adapters[index2];
				if (_event.display.displayEvent == SDL.SDL_DisplayEventID.SDL_DISPLAYEVENT_ORIENTATION)
				{
					DisplayOrientation orientation = INTERNAL_ConvertOrientation((SDL.SDL_DisplayOrientation)_event.display.data1);
					INTERNAL_HandleOrientationChange(orientation, game.GraphicsDevice, (FNAWindow)game.Window);
				}
			}
			else if (_event.type == SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED)
			{
				INTERNAL_AddInstance(_event.cdevice.which);
			}
			else if (_event.type == SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED)
			{
				INTERNAL_RemoveInstance(_event.cdevice.which);
			}
			else if (_event.type == SDL.SDL_EventType.SDL_TEXTINPUT && !textInputSuppress)
			{
				int num2 = MeasureStringLength(_event.text.text);
				if (num2 > 0)
				{
					int chars = Encoding.UTF8.GetChars(_event.text.text, num2, ptr, num2);
					for (int i = 0; i < chars; i++)
					{
						TextInputEXT.OnTextInput(ptr[i]);
					}
				}
			}
			else if (_event.type == SDL.SDL_EventType.SDL_TEXTEDITING)
			{
				int num3 = MeasureStringLength(_event.edit.text);
				if (num3 > 0)
				{
					int chars2 = Encoding.UTF8.GetChars(_event.edit.text, num3, ptr, num3);
					string text = new string(ptr, 0, chars2);
					TextInputEXT.OnTextEditing(text, _event.edit.start, _event.edit.length);
				}
				else
				{
					TextInputEXT.OnTextEditing(null, 0, 0);
				}
			}
			else if (_event.type == SDL.SDL_EventType.SDL_QUIT)
			{
				game.RunApplication = false;
				break;
			}
		}
		for (int j = 0; j < FNAPlatform.TextInputCharacters.Length; j++)
		{
			if (textInputControlDown[j] && textInputControlRepeat[j] <= Environment.TickCount)
			{
				TextInputEXT.OnTextInput(FNAPlatform.TextInputCharacters[j]);
			}
		}
	}

	private unsafe static int MeasureStringLength(byte* ptr)
	{
		int num = 0;
		while (*ptr != 0)
		{
			ptr++;
			num++;
		}
		return num;
	}

	public static bool NeedsPlatformMainLoop()
	{
		return SDL.SDL_GetPlatform().Equals("Emscripten");
	}

	public static void RunPlatformMainLoop(Game game)
	{
		if (SDL.SDL_GetPlatform().Equals("Emscripten"))
		{
			emscriptenGame = game;
			emscripten_set_main_loop(RunEmscriptenMainLoop, 0, 1);
			return;
		}
		throw new NotSupportedException("Cannot run the main loop of an unknown platform");
	}

	[DllImport("__Native", CallingConvention = CallingConvention.Cdecl)]
	private static extern void emscripten_set_main_loop(em_callback_func func, int fps, int simulate_infinite_loop);

	[DllImport("__Native", CallingConvention = CallingConvention.Cdecl)]
	private static extern void emscripten_cancel_main_loop();

	[MonoPInvokeCallback(typeof(em_callback_func))]
	private static void RunEmscriptenMainLoop()
	{
		emscriptenGame.RunOneFrame();
		if (!emscriptenGame.RunApplication)
		{
			emscriptenGame.Exit();
			emscripten_cancel_main_loop();
		}
	}

	public static GraphicsAdapter[] GetGraphicsAdapters()
	{
		SDL.SDL_DisplayMode mode = default;
		GraphicsAdapter[] array = new GraphicsAdapter[SDL.SDL_GetNumVideoDisplays()];
		for (int i = 0; i < array.Length; i++)
		{
			List<DisplayMode> list = new List<DisplayMode>();
			int num = SDL.SDL_GetNumDisplayModes(i);
			for (int num2 = num - 1; num2 >= 0; num2--)
			{
				SDL.SDL_GetDisplayMode(i, num2, out mode);
				bool flag = false;
				foreach (DisplayMode item in list)
				{
					if (mode.w == item.Width && mode.h == item.Height)
					{
						flag = true;
					}
				}
				if (!flag)
				{
					list.Add(new DisplayMode(mode.w, mode.h, SurfaceFormat.Color));
				}
			}
			array[i] = new GraphicsAdapter(new DisplayModeCollection(list), "\\\\.\\DISPLAY" + (i + 1), SDL.SDL_GetDisplayName(i));
		}
		return array;
	}

	public static DisplayMode GetCurrentDisplayMode(int adapterIndex)
	{
		SDL.SDL_DisplayMode mode = default;
		SDL.SDL_GetCurrentDisplayMode(adapterIndex, out mode);
		return new DisplayMode(mode.w, mode.h, SurfaceFormat.Color);
	}

	public static void GetMouseState(IntPtr window, out int x, out int y, out ButtonState left, out ButtonState middle, out ButtonState right, out ButtonState x1, out ButtonState x2)
	{
		uint num;
		if (GetRelativeMouseMode())
		{
			num = SDL.SDL_GetRelativeMouseState(out x, out y);
		}
		else if (SupportsGlobalMouse)
		{
			num = SDL.SDL_GetGlobalMouseState(out x, out y);
			int x3 = 0;
			int y2 = 0;
			SDL.SDL_GetWindowPosition(window, out x3, out y2);
			x -= x3;
			y -= y2;
		}
		else
		{
			num = SDL.SDL_GetMouseState(out x, out y);
		}
		left = (ButtonState)(num & SDL.SDL_BUTTON_LMASK);
		middle = (ButtonState)((num & SDL.SDL_BUTTON_MMASK) >> 1);
		right = (ButtonState)((num & SDL.SDL_BUTTON_RMASK) >> 2);
		x1 = (ButtonState)((num & SDL.SDL_BUTTON_X1MASK) >> 3);
		x2 = (ButtonState)((num & SDL.SDL_BUTTON_X2MASK) >> 4);
	}

	public static void OnIsMouseVisibleChanged(bool visible)
	{
		SDL.SDL_ShowCursor(visible ? 1 : 0);
	}

	public static bool GetRelativeMouseMode()
	{
		return SDL.SDL_GetRelativeMouseMode() == SDL.SDL_bool.SDL_TRUE;
	}

	public static void SetRelativeMouseMode(bool enable)
	{
		SDL.SDL_SetRelativeMouseMode(enable ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE);
	}

	private static string GetBaseDirectory()
	{
		if (Environment.GetEnvironmentVariable("FNA_SDL2_FORCE_BASE_PATH") != "1" && (OSVersion.Equals("Windows") || OSVersion.Equals("Mac OS X") || OSVersion.Equals("Linux") || OSVersion.Equals("FreeBSD") || OSVersion.Equals("OpenBSD") || OSVersion.Equals("NetBSD")))
		{
			return AppDomain.CurrentDomain.BaseDirectory;
		}
		string text = SDL.SDL_GetBasePath();
		if (string.IsNullOrEmpty(text))
		{
			text = AppDomain.CurrentDomain.BaseDirectory;
		}
		if (string.IsNullOrEmpty(text))
		{
			text = Environment.CurrentDirectory;
		}
		return text;
	}

	public static string GetStorageRoot()
	{
		string text = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName).Replace(".vshost", "");
		if (OSVersion.Equals("Windows"))
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SavedGames", text);
		}
		if (OSVersion.Equals("Mac OS X"))
		{
			string environmentVariable = Environment.GetEnvironmentVariable("HOME");
			if (string.IsNullOrEmpty(environmentVariable))
			{
				return ".";
			}
			return Path.Combine(environmentVariable, "Library/Application Support", text);
		}
		if (OSVersion.Equals("Linux") || OSVersion.Equals("FreeBSD") || OSVersion.Equals("OpenBSD") || OSVersion.Equals("NetBSD"))
		{
			string text2 = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
			if (string.IsNullOrEmpty(text2))
			{
				text2 = Environment.GetEnvironmentVariable("HOME");
				if (string.IsNullOrEmpty(text2))
				{
					return ".";
				}
				text2 += "/.local/share";
			}
			return Path.Combine(text2, text);
		}
		return SDL.SDL_GetPrefPath(null, text);
	}

	public static DriveInfo GetDriveInfo(string storageRoot)
	{
		if (OSVersion.Equals("WinRT"))
		{
			return null;
		}
		DriveInfo result;
		try
		{
			result = new DriveInfo(MonoPathRootWorkaround(storageRoot));
		}
		catch (Exception ex)
		{
			FNALoggerEXT.LogError("Failed to get DriveInfo: " + ex.ToString());
			result = null;
		}
		return result;
	}

	private static string MonoPathRootWorkaround(string storageRoot)
	{
		if (Environment.OSVersion.Platform == PlatformID.Win32NT)
		{
			return Path.GetPathRoot(storageRoot);
		}
		if (storageRoot == null)
		{
			return null;
		}
		if (storageRoot.Trim().Length == 0)
		{
			throw new ArgumentException("The specified path is not of a legal form.");
		}
		if (!Path.IsPathRooted(storageRoot) && !storageRoot.Contains(":"))
		{
			return string.Empty;
		}
		int num = -1;
		int num2 = 0;
		string[] logicalDrives = Environment.GetLogicalDrives();
		for (int i = 0; i < logicalDrives.Length; i++)
		{
			if (!string.IsNullOrEmpty(logicalDrives[i]))
			{
				string text = logicalDrives[i];
				if (text[text.Length - 1] != Path.DirectorySeparatorChar)
				{
					text += Path.DirectorySeparatorChar;
				}
				if (storageRoot.StartsWith(text) && text.Length > num2)
				{
					num = i;
					num2 = text.Length;
				}
			}
		}
		if (num >= 0)
		{
			return logicalDrives[num];
		}
		return Path.GetPathRoot(storageRoot);
	}

	public static IntPtr ReadToPointer(string path, out IntPtr size)
	{
		return SDL.SDL_LoadFile(path, out size);
	}

	public static void FreeFilePointer(IntPtr file)
	{
		SDL.SDL_free(file);
	}

	public static void ShowRuntimeError(string title, string message)
	{
		SDL.SDL_ShowSimpleMessageBox(SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, title ?? "", message ?? "", IntPtr.Zero);
	}

	public static Microphone[] GetMicrophones()
	{
		if (!micInit)
		{
			SDL.SDL_InitSubSystem(16u);
			micInit = true;
		}
		int num = SDL.SDL_GetNumAudioDevices(1);
		if (num < 1)
		{
			return new Microphone[0];
		}
		Microphone[] array = new Microphone[num + 1];
		SDL.SDL_AudioSpec desired = new SDL.SDL_AudioSpec
		{
			freq = 44100,
			format = 32784,
			channels = 1,
			samples = 4096
		};
		array[0] = new Microphone(SDL.SDL_OpenAudioDevice(null, 1, ref desired, out var obtained, 0), "Default Device");
		for (int i = 0; i < num; i++)
		{
			string text = SDL.SDL_GetAudioDeviceName(i, 1);
			array[i + 1] = new Microphone(SDL.SDL_OpenAudioDevice(text, 1, ref desired, out obtained, 0), text);
		}
		return array;
	}

	public unsafe static int GetMicrophoneSamples(uint handle, byte[] buffer, int offset, int count)
	{
		fixed (byte* ptr = &buffer[offset])
		{
			return (int)SDL.SDL_DequeueAudio(handle, (IntPtr)ptr, (uint)count);
		}
	}

	public static int GetMicrophoneQueuedBytes(uint handle)
	{
		return (int)SDL.SDL_GetQueuedAudioSize(handle);
	}

	public static void StartMicrophone(uint handle)
	{
		SDL.SDL_PauseAudioDevice(handle, 0);
	}

	public static void StopMicrophone(uint handle)
	{
		SDL.SDL_PauseAudioDevice(handle, 1);
	}

	public static GamePadCapabilities GetGamePadCapabilities(int index)
	{
		if (INTERNAL_devices[index] == IntPtr.Zero)
		{
			return default;
		}
		return INTERNAL_capabilities[index];
	}

	public static GamePadState GetGamePadState(int index, GamePadDeadZone deadZoneMode)
	{
		IntPtr intPtr = INTERNAL_devices[index];
		if (intPtr == IntPtr.Zero)
		{
			return default;
		}
		Vector2 leftPosition = new Vector2((float)SDL.SDL_GameControllerGetAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX) / 32767f, (float)SDL.SDL_GameControllerGetAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY) / -32767f);
		Vector2 rightPosition = new Vector2((float)SDL.SDL_GameControllerGetAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX) / 32767f, (float)SDL.SDL_GameControllerGetAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY) / -32767f);
		float leftTrigger = (float)SDL.SDL_GameControllerGetAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT) / 32767f;
		float rightTrigger = (float)SDL.SDL_GameControllerGetAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT) / 32767f;
		Buttons buttons = (Buttons)0;
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A) != 0)
		{
			buttons |= Buttons.A;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B) != 0)
		{
			buttons |= Buttons.B;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X) != 0)
		{
			buttons |= Buttons.X;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y) != 0)
		{
			buttons |= Buttons.Y;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK) != 0)
		{
			buttons |= Buttons.Back;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE) != 0)
		{
			buttons |= Buttons.BigButton;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START) != 0)
		{
			buttons |= Buttons.Start;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK) != 0)
		{
			buttons |= Buttons.LeftStick;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK) != 0)
		{
			buttons |= Buttons.RightStick;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER) != 0)
		{
			buttons |= Buttons.LeftShoulder;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER) != 0)
		{
			buttons |= Buttons.RightShoulder;
		}
		ButtonState upValue = ButtonState.Released;
		ButtonState downValue = ButtonState.Released;
		ButtonState leftValue = ButtonState.Released;
		ButtonState rightValue = ButtonState.Released;
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP) != 0)
		{
			buttons |= Buttons.DPadUp;
			upValue = ButtonState.Pressed;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN) != 0)
		{
			buttons |= Buttons.DPadDown;
			downValue = ButtonState.Pressed;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT) != 0)
		{
			buttons |= Buttons.DPadLeft;
			leftValue = ButtonState.Pressed;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT) != 0)
		{
			buttons |= Buttons.DPadRight;
			rightValue = ButtonState.Pressed;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_MISC1) != 0)
		{
			buttons |= Buttons.Misc1EXT;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE1) != 0)
		{
			buttons |= Buttons.Paddle1EXT;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE2) != 0)
		{
			buttons |= Buttons.Paddle2EXT;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE3) != 0)
		{
			buttons |= Buttons.Paddle3EXT;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE4) != 0)
		{
			buttons |= Buttons.Paddle4EXT;
		}
		if (SDL.SDL_GameControllerGetButton(intPtr, SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_TOUCHPAD) != 0)
		{
			buttons |= Buttons.TouchPadEXT;
		}
		GamePadState gamePadState = new GamePadState(new GamePadThumbSticks(leftPosition, rightPosition, deadZoneMode), new GamePadTriggers(leftTrigger, rightTrigger, deadZoneMode), new GamePadButtons(buttons), new GamePadDPad(upValue, downValue, leftValue, rightValue));
		gamePadState.IsConnected = true;
		gamePadState.PacketNumber = INTERNAL_states[index].PacketNumber;
		if (gamePadState != INTERNAL_states[index])
		{
			gamePadState.PacketNumber++;
			INTERNAL_states[index] = gamePadState;
		}
		return gamePadState;
	}

	public static bool SetGamePadVibration(int index, float leftMotor, float rightMotor)
	{
		IntPtr intPtr = INTERNAL_devices[index];
		if (intPtr == IntPtr.Zero)
		{
			return false;
		}
		return SDL.SDL_GameControllerRumble(intPtr, (ushort)(MathHelper.Clamp(leftMotor, 0f, 1f) * 65535f), (ushort)(MathHelper.Clamp(rightMotor, 0f, 1f) * 65535f), 0u) == 0;
	}

	public static bool SetGamePadTriggerVibration(int index, float leftTrigger, float rightTrigger)
	{
		IntPtr intPtr = INTERNAL_devices[index];
		if (intPtr == IntPtr.Zero)
		{
			return false;
		}
		return SDL.SDL_GameControllerRumbleTriggers(intPtr, (ushort)(MathHelper.Clamp(leftTrigger, 0f, 1f) * 65535f), (ushort)(MathHelper.Clamp(rightTrigger, 0f, 1f) * 65535f), 0u) == 0;
	}

	public static string GetGamePadGUID(int index)
	{
		return INTERNAL_guids[index];
	}

	public static void SetGamePadLightBar(int index, Color color)
	{
		IntPtr intPtr = INTERNAL_devices[index];
		if (!(intPtr == IntPtr.Zero))
		{
			SDL.SDL_GameControllerSetLED(intPtr, color.R, color.G, color.B);
		}
	}

	public unsafe static bool GetGamePadGyro(int index, out Vector3 gyro)
	{
		IntPtr intPtr = INTERNAL_devices[index];
		if (intPtr == IntPtr.Zero)
		{
			gyro = Vector3.Zero;
			return false;
		}
		if (SDL.SDL_GameControllerIsSensorEnabled(intPtr, SDL.SDL_SensorType.SDL_SENSOR_GYRO) == SDL.SDL_bool.SDL_FALSE)
		{
			SDL.SDL_GameControllerSetSensorEnabled(intPtr, SDL.SDL_SensorType.SDL_SENSOR_GYRO, SDL.SDL_bool.SDL_TRUE);
		}
		float* ptr = stackalloc float[3];
		if (SDL.SDL_GameControllerGetSensorData(intPtr, SDL.SDL_SensorType.SDL_SENSOR_GYRO, (IntPtr)ptr, 3) < 0)
		{
			gyro = Vector3.Zero;
			return false;
		}
		gyro.X = *ptr;
		gyro.Y = ptr[1];
		gyro.Z = ptr[2];
		return true;
	}

	public unsafe static bool GetGamePadAccelerometer(int index, out Vector3 accel)
	{
		IntPtr intPtr = INTERNAL_devices[index];
		if (intPtr == IntPtr.Zero)
		{
			accel = Vector3.Zero;
			return false;
		}
		if (SDL.SDL_GameControllerIsSensorEnabled(intPtr, SDL.SDL_SensorType.SDL_SENSOR_ACCEL) == SDL.SDL_bool.SDL_FALSE)
		{
			SDL.SDL_GameControllerSetSensorEnabled(intPtr, SDL.SDL_SensorType.SDL_SENSOR_ACCEL, SDL.SDL_bool.SDL_TRUE);
		}
		float* ptr = stackalloc float[3];
		if (SDL.SDL_GameControllerGetSensorData(intPtr, SDL.SDL_SensorType.SDL_SENSOR_ACCEL, (IntPtr)ptr, 3) < 0)
		{
			accel = Vector3.Zero;
			return false;
		}
		accel.X = *ptr;
		accel.Y = ptr[1];
		accel.Z = ptr[2];
		return true;
	}

	private static void INTERNAL_AddInstance(int dev)
	{
		int num = -1;
		for (int i = 0; i < INTERNAL_devices.Length; i++)
		{
			if (INTERNAL_devices[i] == IntPtr.Zero)
			{
				num = i;
				break;
			}
		}
		if (num == -1)
		{
			return;
		}
		SDL.SDL_ClearError();
		INTERNAL_devices[num] = SDL.SDL_GameControllerOpen(dev);
		IntPtr joystick = SDL.SDL_GameControllerGetJoystick(INTERNAL_devices[num]);
		int key = SDL.SDL_JoystickInstanceID(joystick);
		if (INTERNAL_instanceList.ContainsKey(key))
		{
			INTERNAL_devices[num] = IntPtr.Zero;
			return;
		}
		INTERNAL_instanceList.Add(key, num);
		INTERNAL_states[num] = default;
		INTERNAL_states[num].IsConnected = true;
		bool flag = SDL.SDL_GameControllerRumble(INTERNAL_devices[num], 0, 0, 0u) == 0;
		bool hasTriggerVibrationMotorsEXT = SDL.SDL_GameControllerRumbleTriggers(INTERNAL_devices[num], 0, 0, 0u) == 0;
		GamePadCapabilities gamePadCapabilities = new GamePadCapabilities
		{
			IsConnected = true,
			GamePadType = INTERNAL_gamepadType[(int)SDL.SDL_JoystickGetType(joystick)],
			HasAButton = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasBButton = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasXButton = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasYButton = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasBackButton = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasBigButton = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasStartButton = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasLeftStickButton = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasRightStickButton = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasLeftShoulderButton = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasRightShoulderButton = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasDPadUpButton = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasDPadDownButton = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasDPadLeftButton = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasDPadRightButton = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasLeftXThumbStick = (SDL.SDL_GameControllerGetBindForAxis(INTERNAL_devices[num], SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasLeftYThumbStick = (SDL.SDL_GameControllerGetBindForAxis(INTERNAL_devices[num], SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasRightXThumbStick = (SDL.SDL_GameControllerGetBindForAxis(INTERNAL_devices[num], SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasRightYThumbStick = (SDL.SDL_GameControllerGetBindForAxis(INTERNAL_devices[num], SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasLeftTrigger = (SDL.SDL_GameControllerGetBindForAxis(INTERNAL_devices[num], SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasRightTrigger = (SDL.SDL_GameControllerGetBindForAxis(INTERNAL_devices[num], SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasLeftVibrationMotor = flag,
			HasRightVibrationMotor = flag,
			HasVoiceSupport = false,
			HasLightBarEXT = (SDL.SDL_GameControllerHasLED(INTERNAL_devices[num]) == SDL.SDL_bool.SDL_TRUE),
			HasTriggerVibrationMotorsEXT = hasTriggerVibrationMotorsEXT,
			HasMisc1EXT = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_MISC1).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasPaddle1EXT = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE1).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasPaddle2EXT = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE2).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasPaddle3EXT = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE3).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasPaddle4EXT = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE4).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasTouchPadEXT = (SDL.SDL_GameControllerGetBindForButton(INTERNAL_devices[num], SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_TOUCHPAD).bindType != SDL.SDL_GameControllerBindType.SDL_CONTROLLER_BINDTYPE_NONE),
			HasGyroEXT = (SDL.SDL_GameControllerHasSensor(INTERNAL_devices[num], SDL.SDL_SensorType.SDL_SENSOR_GYRO) == SDL.SDL_bool.SDL_TRUE),
			HasAccelerometerEXT = (SDL.SDL_GameControllerHasSensor(INTERNAL_devices[num], SDL.SDL_SensorType.SDL_SENSOR_ACCEL) == SDL.SDL_bool.SDL_TRUE)
		};
		INTERNAL_capabilities[num] = gamePadCapabilities;
		ushort num2 = SDL.SDL_JoystickGetVendor(joystick);
		ushort num3 = SDL.SDL_JoystickGetProduct(joystick);
		if (num2 == 0 && num3 == 0)
		{
			INTERNAL_guids[num] = "xinput";
		}
		else
		{
			INTERNAL_guids[num] = $"{num2 & 0xFF:x2}{num2 >> 8:x2}{num3 & 0xFF:x2}{num3 >> 8:x2}";
		}
		string text = SDL.SDL_GameControllerMapping(INTERNAL_devices[num]);
		string text2 = ((!string.IsNullOrEmpty(text)) ? ("Mapping: " + text) : "Mapping not found");
		FNALoggerEXT.LogInfo("Controller " + num + ": " + SDL.SDL_GameControllerName(INTERNAL_devices[num]) + ", GUID: " + INTERNAL_guids[num] + ", " + text2);
	}

	private static void INTERNAL_RemoveInstance(int dev)
	{
		if (INTERNAL_instanceList.TryGetValue(dev, out var value))
		{
			INTERNAL_instanceList.Remove(dev);
			SDL.SDL_GameControllerClose(INTERNAL_devices[value]);
			INTERNAL_devices[value] = IntPtr.Zero;
			INTERNAL_states[value] = default;
			INTERNAL_guids[value] = string.Empty;
			SDL.SDL_ClearError();
			FNALoggerEXT.LogInfo("Removed device, player: " + value);
		}
	}

	private static string[] GenStringArray()
	{
		string[] array = new string[GamePad.GAMEPAD_COUNT];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = string.Empty;
		}
		return array;
	}

	public static TouchPanelCapabilities GetTouchCapabilities()
	{
		bool flag = SDL.SDL_GetNumTouchDevices() > 0;
		return new TouchPanelCapabilities(flag, flag ? 4 : 0);
	}

	public unsafe static void UpdateTouchPanelState()
	{
		long touchID = SDL.SDL_GetTouchDevice(0);
		for (int i = 0; i < 8; i++)
		{
			SDL.SDL_Finger* ptr = (SDL.SDL_Finger*)(void*)SDL.SDL_GetTouchFinger(touchID, i);
			if (ptr == null)
			{
				TouchPanel.SetFinger(i, -1, Vector2.Zero);
			}
			else
			{
				TouchPanel.SetFinger(i, (int)ptr->id, new Vector2((float)Math.Round(ptr->x * (float)TouchPanel.DisplayWidth), (float)Math.Round(ptr->y * (float)TouchPanel.DisplayHeight)));
			}
		}
	}

	public static int GetNumTouchFingers()
	{
		return SDL.SDL_GetNumTouchFingers(SDL.SDL_GetTouchDevice(0));
	}

	private static Keys ToXNAKey(ref SDL.SDL_Keysym key)
	{
		Keys value;
		if (UseScancodes)
		{
			if (INTERNAL_scanMap.TryGetValue((int)key.scancode, out value))
			{
				return value;
			}
		}
		else if (INTERNAL_keyMap.TryGetValue((int)key.sym, out value))
		{
			return value;
		}
		FNALoggerEXT.LogWarn("KEY/SCANCODE MISSING FROM SDL2->XNA DICTIONARY: " + key.sym.ToString() + " " + key.scancode);
		return Keys.None;
	}

	public static Keys GetKeyFromScancode(Keys scancode)
	{
		if (UseScancodes)
		{
			return scancode;
		}
		if (INTERNAL_xnaMap.TryGetValue((int)scancode, out var value))
		{
			SDL.SDL_Keycode key = SDL.SDL_GetKeyFromScancode(value);
			if (INTERNAL_keyMap.TryGetValue((int)key, out var value2))
			{
				return value2;
			}
			FNALoggerEXT.LogWarn("KEYCODE MISSING FROM SDL2->XNA DICTIONARY: " + key);
		}
		else
		{
			FNALoggerEXT.LogWarn("SCANCODE MISSING FROM XNA->SDL2 DICTIONARY: " + scancode);
		}
		return Keys.None;
	}

	private unsafe static int Win32OnPaint(IntPtr userdata, IntPtr evtPtr)
	{
		SDL.SDL_Event* ptr = (SDL.SDL_Event*)(void*)evtPtr;
		if (ptr->type == SDL.SDL_EventType.SDL_WINDOWEVENT && ptr->window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_EXPOSED)
		{
			foreach (Game activeGame in activeGames)
			{
				if (activeGame.Window != null && ptr->window.windowID == SDL.SDL_GetWindowID(activeGame.Window.Handle))
				{
					activeGame.RedrawWindow();
					return 0;
				}
			}
		}
		if (prevEventFilter != null)
		{
			return prevEventFilter(userdata, evtPtr);
		}
		return 1;
	}
}
