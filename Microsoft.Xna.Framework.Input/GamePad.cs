using System;

namespace Microsoft.Xna.Framework.Input;

public static class GamePad
{
	internal const float LeftDeadZone = 0.23953247f;

	internal const float RightDeadZone = 0.26516724f;

	internal const float TriggerThreshold = 30f / 255f;

	internal static readonly int GAMEPAD_COUNT = DetermineNumGamepads();

	private static int DetermineNumGamepads()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("FNA_GAMEPAD_NUM_GAMEPADS");
		if (!string.IsNullOrEmpty(environmentVariable) && int.TryParse(environmentVariable, out var result) && result >= 0)
		{
			return result;
		}
		return Enum.GetNames(typeof(PlayerIndex)).Length;
	}

	public static GamePadCapabilities GetCapabilities(PlayerIndex playerIndex)
	{
		return FNAPlatform.GetGamePadCapabilities((int)playerIndex);
	}

	public static GamePadState GetState(PlayerIndex playerIndex)
	{
		return FNAPlatform.GetGamePadState((int)playerIndex, GamePadDeadZone.IndependentAxes);
	}

	public static GamePadState GetState(PlayerIndex playerIndex, GamePadDeadZone deadZoneMode)
	{
		return FNAPlatform.GetGamePadState((int)playerIndex, deadZoneMode);
	}

	public static bool SetVibration(PlayerIndex playerIndex, float leftMotor, float rightMotor)
	{
		return FNAPlatform.SetGamePadVibration((int)playerIndex, leftMotor, rightMotor);
	}

	public static string GetGUIDEXT(PlayerIndex playerIndex)
	{
		return FNAPlatform.GetGamePadGUID((int)playerIndex);
	}

	public static void SetLightBarEXT(PlayerIndex playerIndex, Color color)
	{
		FNAPlatform.SetGamePadLightBar((int)playerIndex, color);
	}

	public static bool SetTriggerVibrationEXT(PlayerIndex playerIndex, float leftTrigger, float rightTrigger)
	{
		return FNAPlatform.SetGamePadTriggerVibration((int)playerIndex, leftTrigger, rightTrigger);
	}

	public static bool GetGyroEXT(PlayerIndex playerIndex, out Vector3 gyro)
	{
		return FNAPlatform.GetGamePadGyro((int)playerIndex, out gyro);
	}

	public static bool GetAccelerometerEXT(PlayerIndex playerIndex, out Vector3 accel)
	{
		return FNAPlatform.GetGamePadAccelerometer((int)playerIndex, out accel);
	}

	internal static float ExcludeAxisDeadZone(float value, float deadZone)
	{
		if (value < 0f - deadZone)
		{
			value += deadZone;
		}
		else
		{
			if (!(value > deadZone))
			{
				return 0f;
			}
			value -= deadZone;
		}
		return value / (1f - deadZone);
	}
}
