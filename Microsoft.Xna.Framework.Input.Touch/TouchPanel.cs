using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Input.Touch;

public static class TouchPanel
{
	internal const int MAX_TOUCHES = 8;

	internal const int NO_FINGER = -1;

	internal static bool TouchDeviceExists;

	private static Queue<GestureSample> gestures = new Queue<GestureSample>();

	private static TouchLocation[] touches = new TouchLocation[8];

	private static TouchLocation[] prevTouches = new TouchLocation[8];

	private static List<TouchLocation> validTouches = new List<TouchLocation>();

	public static int DisplayWidth { get; set; }

	public static int DisplayHeight { get; set; }

	public static DisplayOrientation DisplayOrientation { get; set; }

	public static GestureType EnabledGestures { get; set; }

	public static bool IsGestureAvailable => gestures.Count > 0;

	public static IntPtr WindowHandle { get; set; }

	public static TouchPanelCapabilities GetCapabilities()
	{
		return FNAPlatform.GetTouchCapabilities();
	}

	public static TouchCollection GetState()
	{
		validTouches.Clear();
		for (int i = 0; i < 8; i++)
		{
			if (touches[i].State != TouchLocationState.Invalid)
			{
				validTouches.Add(touches[i]);
			}
		}
		return new TouchCollection(validTouches.ToArray());
	}

	public static GestureSample ReadGesture()
	{
		if (gestures.Count == 0)
		{
			throw new InvalidOperationException();
		}
		return gestures.Dequeue();
	}

	internal static void EnqueueGesture(GestureSample gesture)
	{
		gestures.Enqueue(gesture);
	}

	internal static void INTERNAL_onTouchEvent(int fingerId, TouchLocationState state, float x, float y, float dx, float dy)
	{
		Vector2 touchPosition = new Vector2((float)Math.Round(x * (float)DisplayWidth), (float)Math.Round(y * (float)DisplayHeight));
		switch (state)
		{
		case TouchLocationState.Pressed:
			GestureDetector.OnPressed(fingerId, touchPosition);
			break;
		case TouchLocationState.Moved:
		{
			Vector2 delta = new Vector2((float)Math.Round(dx * (float)DisplayWidth), (float)Math.Round(dy * (float)DisplayHeight));
			GestureDetector.OnMoved(fingerId, touchPosition, delta);
			break;
		}
		case TouchLocationState.Released:
			GestureDetector.OnReleased(fingerId, touchPosition);
			break;
		}
	}

	internal static void SetFinger(int index, int fingerId, Vector2 fingerPos)
	{
		if (fingerId == -1)
		{
			if (prevTouches[index].State != TouchLocationState.Invalid && prevTouches[index].State != TouchLocationState.Released)
			{
				touches[index] = new TouchLocation(prevTouches[index].Id, TouchLocationState.Released, prevTouches[index].Position, prevTouches[index].State, prevTouches[index].Position);
			}
			else
			{
				touches[index] = new TouchLocation(-1, TouchLocationState.Invalid, Vector2.Zero);
			}
		}
		else if (prevTouches[index].State == TouchLocationState.Invalid)
		{
			touches[index] = new TouchLocation(fingerId, TouchLocationState.Pressed, fingerPos);
		}
		else
		{
			touches[index] = new TouchLocation(fingerId, TouchLocationState.Moved, fingerPos, prevTouches[index].State, prevTouches[index].Position);
		}
	}

	internal static void Update()
	{
		GestureDetector.OnUpdate();
		touches.CopyTo(prevTouches, 0);
		FNAPlatform.UpdateTouchPanelState();
	}
}
