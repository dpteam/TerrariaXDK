using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using SDL2;

namespace Microsoft.Xna.Framework;

internal static class FNAPlatform
{
	public delegate IntPtr MallocFunc(int size);

	public delegate void FreeFunc(IntPtr ptr);

	public delegate GameWindow CreateWindowFunc();

	public delegate void DisposeWindowFunc(GameWindow window);

	public delegate void ApplyWindowChangesFunc(IntPtr window, int clientWidth, int clientHeight, bool wantsFullscreen, string screenDeviceName, ref string resultDeviceName);

	public delegate void ScaleForWindowFunc(IntPtr window, bool invert, ref int w, ref int h);

	public delegate Rectangle GetWindowBoundsFunc(IntPtr window);

	public delegate bool GetWindowResizableFunc(IntPtr window);

	public delegate void SetWindowResizableFunc(IntPtr window, bool resizable);

	public delegate bool GetWindowBorderlessFunc(IntPtr window);

	public delegate void SetWindowBorderlessFunc(IntPtr window, bool borderless);

	public delegate void SetWindowTitleFunc(IntPtr window, string title);

	public delegate GraphicsAdapter RegisterGameFunc(Game game);

	public delegate void UnregisterGameFunc(Game game);

	public delegate void PollEventsFunc(Game game, ref GraphicsAdapter currentAdapter, bool[] textInputControlDown, int[] textInputControlRepeat, ref bool textInputSuppress);

	public delegate GraphicsAdapter[] GetGraphicsAdaptersFunc();

	public delegate DisplayMode GetCurrentDisplayModeFunc(int adapterIndex);

	public delegate Keys GetKeyFromScancodeFunc(Keys scancode);

	public delegate void StartTextInputFunc();

	public delegate void StopTextInputFunc();

	public delegate void SetTextInputRectangleFunc(Rectangle rectangle);

	public delegate void GetMouseStateFunc(IntPtr window, out int x, out int y, out ButtonState left, out ButtonState middle, out ButtonState right, out ButtonState x1, out ButtonState x2);

	public delegate void SetMousePositionFunc(IntPtr window, int x, int y);

	public delegate void OnIsMouseVisibleChangedFunc(bool visible);

	public delegate bool GetRelativeMouseModeFunc();

	public delegate void SetRelativeMouseModeFunc(bool enable);

	public delegate GamePadCapabilities GetGamePadCapabilitiesFunc(int index);

	public delegate GamePadState GetGamePadStateFunc(int index, GamePadDeadZone deadZoneMode);

	public delegate bool SetGamePadVibrationFunc(int index, float leftMotor, float rightMotor);

	public delegate bool SetGamePadTriggerVibrationFunc(int index, float leftTrigger, float rightTrigger);

	public delegate string GetGamePadGUIDFunc(int index);

	public delegate void SetGamePadLightBarFunc(int index, Color color);

	public delegate bool GetGamePadGyroFunc(int index, out Vector3 gyro);

	public delegate bool GetGamePadAccelerometerFunc(int index, out Vector3 accel);

	public delegate string GetStorageRootFunc();

	public delegate DriveInfo GetDriveInfoFunc(string storageRoot);

	public delegate IntPtr ReadFileToPointerFunc(string path, out IntPtr size);

	public delegate void FreeFilePointerFunc(IntPtr file);

	public delegate void ShowRuntimeErrorFunc(string title, string message);

	public delegate Microphone[] GetMicrophonesFunc();

	public delegate int GetMicrophoneSamplesFunc(uint handle, byte[] buffer, int offset, int count);

	public delegate int GetMicrophoneQueuedBytesFunc(uint handle);

	public delegate void StartMicrophoneFunc(uint handle);

	public delegate void StopMicrophoneFunc(uint handle);

	public delegate TouchPanelCapabilities GetTouchCapabilitiesFunc();

	public delegate void UpdateTouchPanelStateFunc();

	public delegate int GetNumTouchFingersFunc();

	public delegate bool SupportsOrientationChangesFunc();

	public delegate bool NeedsPlatformMainLoopFunc();

	public delegate void RunPlatformMainLoopFunc(Game game);

	public static readonly MallocFunc Malloc;

	public static readonly FreeFunc Free;

	public static readonly string TitleLocation;

	public static readonly char[] TextInputCharacters;

	public static readonly Dictionary<Keys, int> TextInputBindings;

	public static readonly CreateWindowFunc CreateWindow;

	public static readonly DisposeWindowFunc DisposeWindow;

	public static readonly ApplyWindowChangesFunc ApplyWindowChanges;

	public static readonly ScaleForWindowFunc ScaleForWindow;

	public static readonly GetWindowBoundsFunc GetWindowBounds;

	public static readonly GetWindowResizableFunc GetWindowResizable;

	public static readonly SetWindowResizableFunc SetWindowResizable;

	public static readonly GetWindowBorderlessFunc GetWindowBorderless;

	public static readonly SetWindowBorderlessFunc SetWindowBorderless;

	public static readonly SetWindowTitleFunc SetWindowTitle;

	public static readonly RegisterGameFunc RegisterGame;

	public static readonly UnregisterGameFunc UnregisterGame;

	public static readonly PollEventsFunc PollEvents;

	public static readonly GetGraphicsAdaptersFunc GetGraphicsAdapters;

	public static readonly GetCurrentDisplayModeFunc GetCurrentDisplayMode;

	public static readonly GetKeyFromScancodeFunc GetKeyFromScancode;

	public static readonly StartTextInputFunc StartTextInput;

	public static readonly StopTextInputFunc StopTextInput;

	public static readonly SetTextInputRectangleFunc SetTextInputRectangle;

	public static readonly GetMouseStateFunc GetMouseState;

	public static readonly SetMousePositionFunc SetMousePosition;

	public static readonly OnIsMouseVisibleChangedFunc OnIsMouseVisibleChanged;

	public static readonly GetRelativeMouseModeFunc GetRelativeMouseMode;

	public static readonly SetRelativeMouseModeFunc SetRelativeMouseMode;

	public static readonly GetGamePadCapabilitiesFunc GetGamePadCapabilities;

	public static readonly GetGamePadStateFunc GetGamePadState;

	public static readonly SetGamePadVibrationFunc SetGamePadVibration;

	public static readonly SetGamePadTriggerVibrationFunc SetGamePadTriggerVibration;

	public static readonly GetGamePadGUIDFunc GetGamePadGUID;

	public static readonly SetGamePadLightBarFunc SetGamePadLightBar;

	public static readonly GetGamePadGyroFunc GetGamePadGyro;

	public static readonly GetGamePadAccelerometerFunc GetGamePadAccelerometer;

	public static readonly GetStorageRootFunc GetStorageRoot;

	public static readonly GetDriveInfoFunc GetDriveInfo;

	public static readonly ReadFileToPointerFunc ReadFileToPointer;

	public static readonly FreeFilePointerFunc FreeFilePointer;

	public static readonly ShowRuntimeErrorFunc ShowRuntimeError;

	public static readonly GetMicrophonesFunc GetMicrophones;

	public static readonly GetMicrophoneSamplesFunc GetMicrophoneSamples;

	public static readonly GetMicrophoneQueuedBytesFunc GetMicrophoneQueuedBytes;

	public static readonly StartMicrophoneFunc StartMicrophone;

	public static readonly StopMicrophoneFunc StopMicrophone;

	public static readonly GetTouchCapabilitiesFunc GetTouchCapabilities;

	public static readonly UpdateTouchPanelStateFunc UpdateTouchPanelState;

	public static readonly GetNumTouchFingersFunc GetNumTouchFingers;

	public static readonly SupportsOrientationChangesFunc SupportsOrientationChanges;

	public static readonly NeedsPlatformMainLoopFunc NeedsPlatformMainLoop;

	public static readonly RunPlatformMainLoopFunc RunPlatformMainLoop;

	static FNAPlatform()
	{
		TextInputCharacters = new char[7] { '\u0002', '\u0003', '\b', '\t', '\r', '\u007f', '\u0016' };
		TextInputBindings = new Dictionary<Keys, int>
		{
			{
				Keys.Home,
				0
			},
			{
				Keys.End,
				1
			},
			{
				Keys.Back,
				2
			},
			{
				Keys.Tab,
				3
			},
			{
				Keys.Enter,
				4
			},
			{
				Keys.Delete,
				5
			}
		};
		LaunchParameters launchParameters = new LaunchParameters();
		if (launchParameters.TryGetValue("enablehighdpi", out var value) && value == "1")
		{
			Environment.SetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI", "1");
		}
		if (launchParameters.TryGetValue("gldevice", out value))
		{
			Environment.SetEnvironmentVariable("FNA3D_FORCE_DRIVER", value);
		}
		if (launchParameters.TryGetValue("enablelateswaptear", out value) && value == "1")
		{
			Environment.SetEnvironmentVariable("FNA3D_ENABLE_LATESWAPTEAR", "1");
		}
		if (launchParameters.TryGetValue("mojoshaderprofile", out value))
		{
			Environment.SetEnvironmentVariable("FNA3D_MOJOSHADER_PROFILE", value);
		}
		if (launchParameters.TryGetValue("backbufferscalenearest", out value) && value == "1")
		{
			Environment.SetEnvironmentVariable("FNA3D_BACKBUFFER_SCALE_NEAREST", "1");
		}
		if (launchParameters.TryGetValue("usescancodes", out value) && value == "1")
		{
			Environment.SetEnvironmentVariable("FNA_KEYBOARD_USE_SCANCODES", "1");
		}
		if (launchParameters.TryGetValue("nukesteaminput", out value) && value == "1")
		{
			Environment.SetEnvironmentVariable("FNA_NUKE_STEAM_INPUT", "1");
		}
		Malloc = SDL2_FNAPlatform.Malloc;
		Free = SDL.SDL_free;
		CreateWindow = SDL2_FNAPlatform.CreateWindow;
		DisposeWindow = SDL2_FNAPlatform.DisposeWindow;
		ApplyWindowChanges = SDL2_FNAPlatform.ApplyWindowChanges;
		ScaleForWindow = SDL2_FNAPlatform.ScaleForWindow;
		GetWindowBounds = SDL2_FNAPlatform.GetWindowBounds;
		GetWindowResizable = SDL2_FNAPlatform.GetWindowResizable;
		SetWindowResizable = SDL2_FNAPlatform.SetWindowResizable;
		GetWindowBorderless = SDL2_FNAPlatform.GetWindowBorderless;
		SetWindowBorderless = SDL2_FNAPlatform.SetWindowBorderless;
		SetWindowTitle = SDL2_FNAPlatform.SetWindowTitle;
		RegisterGame = SDL2_FNAPlatform.RegisterGame;
		UnregisterGame = SDL2_FNAPlatform.UnregisterGame;
		PollEvents = SDL2_FNAPlatform.PollEvents;
		GetGraphicsAdapters = SDL2_FNAPlatform.GetGraphicsAdapters;
		GetCurrentDisplayMode = SDL2_FNAPlatform.GetCurrentDisplayMode;
		GetKeyFromScancode = SDL2_FNAPlatform.GetKeyFromScancode;
		StartTextInput = SDL.SDL_StartTextInput;
		StopTextInput = SDL.SDL_StopTextInput;
		SetTextInputRectangle = SDL2_FNAPlatform.SetTextInputRectangle;
		GetMouseState = SDL2_FNAPlatform.GetMouseState;
		SetMousePosition = SDL.SDL_WarpMouseInWindow;
		OnIsMouseVisibleChanged = SDL2_FNAPlatform.OnIsMouseVisibleChanged;
		GetRelativeMouseMode = SDL2_FNAPlatform.GetRelativeMouseMode;
		SetRelativeMouseMode = SDL2_FNAPlatform.SetRelativeMouseMode;
		GetGamePadCapabilities = SDL2_FNAPlatform.GetGamePadCapabilities;
		GetGamePadState = SDL2_FNAPlatform.GetGamePadState;
		SetGamePadVibration = SDL2_FNAPlatform.SetGamePadVibration;
		SetGamePadTriggerVibration = SDL2_FNAPlatform.SetGamePadTriggerVibration;
		GetGamePadGUID = SDL2_FNAPlatform.GetGamePadGUID;
		SetGamePadLightBar = SDL2_FNAPlatform.SetGamePadLightBar;
		GetGamePadGyro = SDL2_FNAPlatform.GetGamePadGyro;
		GetGamePadAccelerometer = SDL2_FNAPlatform.GetGamePadAccelerometer;
		GetStorageRoot = SDL2_FNAPlatform.GetStorageRoot;
		GetDriveInfo = SDL2_FNAPlatform.GetDriveInfo;
		ReadFileToPointer = SDL2_FNAPlatform.ReadToPointer;
		FreeFilePointer = SDL2_FNAPlatform.FreeFilePointer;
		ShowRuntimeError = SDL2_FNAPlatform.ShowRuntimeError;
		GetMicrophones = SDL2_FNAPlatform.GetMicrophones;
		GetMicrophoneSamples = SDL2_FNAPlatform.GetMicrophoneSamples;
		GetMicrophoneQueuedBytes = SDL2_FNAPlatform.GetMicrophoneQueuedBytes;
		StartMicrophone = SDL2_FNAPlatform.StartMicrophone;
		StopMicrophone = SDL2_FNAPlatform.StopMicrophone;
		GetTouchCapabilities = SDL2_FNAPlatform.GetTouchCapabilities;
		UpdateTouchPanelState = SDL2_FNAPlatform.UpdateTouchPanelState;
		GetNumTouchFingers = SDL2_FNAPlatform.GetNumTouchFingers;
		SupportsOrientationChanges = SDL2_FNAPlatform.SupportsOrientationChanges;
		NeedsPlatformMainLoop = SDL2_FNAPlatform.NeedsPlatformMainLoop;
		RunPlatformMainLoop = SDL2_FNAPlatform.RunPlatformMainLoop;
		FNALoggerEXT.Initialize();
		AppDomain.CurrentDomain.ProcessExit += SDL2_FNAPlatform.ProgramExit;
		TitleLocation = SDL2_FNAPlatform.ProgramInit(launchParameters);
		FNALoggerEXT.HookFNA3D();
	}
}
