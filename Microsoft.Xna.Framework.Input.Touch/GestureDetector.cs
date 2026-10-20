using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Input.Touch;

internal static class GestureDetector
{
	private enum GestureState
	{
		NONE,
		HOLDING,
		HELD,
		JUST_TAPPED,
		DRAGGING_FREE,
		DRAGGING_H,
		DRAGGING_V,
		PINCHING
	}

	private static int activeFingerId = -1;

	private static Vector2 activeFingerPosition;

	private static bool callBelatedPinchComplete = false;

	private static DateTime eventTimestamp;

	private static List<int> fingerIds = new List<int>();

	private static bool justDoubleTapped = false;

	private static Vector2 lastUpdatePosition;

	private static Vector2 pressPosition;

	private static int secondFingerId = -1;

	private static Vector2 secondFingerPosition;

	private static GestureState state = GestureState.NONE;

	private static DateTime updateTimestamp;

	private static Vector2 velocity;

	private const int MOVE_THRESHOLD = 35;

	private const int MIN_FLICK_VELOCITY = 100;

	internal static void OnPressed(int fingerId, Vector2 touchPosition)
	{
		fingerIds.Add(fingerId);
		if (state == GestureState.PINCHING)
		{
			return;
		}
		if (activeFingerId == -1)
		{
			activeFingerId = fingerId;
			activeFingerPosition = touchPosition;
			if (state == GestureState.JUST_TAPPED && IsGestureEnabled(GestureType.DoubleTap))
			{
				TimeSpan timeSpan = DateTime.Now - eventTimestamp;
				if (timeSpan <= TimeSpan.FromMilliseconds(300.0))
				{
					float num = (touchPosition - pressPosition).Length();
					if (num <= 35f)
					{
						TouchPanel.EnqueueGesture(new GestureSample(GestureType.DoubleTap, GetGestureTimestamp(), touchPosition, Vector2.Zero, Vector2.Zero, Vector2.Zero, fingerId, -1));
						justDoubleTapped = true;
					}
				}
			}
			state = GestureState.HOLDING;
			pressPosition = touchPosition;
			eventTimestamp = DateTime.Now;
		}
		else if (IsGestureEnabled(GestureType.Pinch))
		{
			secondFingerId = fingerId;
			secondFingerPosition = touchPosition;
			state = GestureState.PINCHING;
		}
	}

	internal static void OnReleased(int fingerId, Vector2 touchPosition)
	{
		fingerIds.Remove(fingerId);
		if (state == GestureState.PINCHING)
		{
			OnReleased_Pinch(fingerId, touchPosition);
			return;
		}
		if (fingerId == activeFingerId)
		{
			activeFingerId = -1;
		}
		if (FNAPlatform.GetNumTouchFingers() > 0)
		{
			return;
		}
		if (state == GestureState.HOLDING)
		{
			bool flag = IsGestureEnabled(GestureType.Tap);
			bool flag2 = IsGestureEnabled(GestureType.DoubleTap);
			if (flag | flag2)
			{
				TimeSpan timeSpan = DateTime.Now - eventTimestamp;
				if (timeSpan < TimeSpan.FromSeconds(1.0) && !justDoubleTapped)
				{
					if (flag)
					{
						TouchPanel.EnqueueGesture(new GestureSample(GestureType.Tap, GetGestureTimestamp(), touchPosition, Vector2.Zero, Vector2.Zero, Vector2.Zero, fingerId, -1));
					}
					state = GestureState.JUST_TAPPED;
				}
			}
		}
		justDoubleTapped = false;
		if (IsGestureEnabled(GestureType.Flick))
		{
			float num = (touchPosition - pressPosition).Length();
			if (num > 35f && velocity.Length() >= 100f)
			{
				TouchPanel.EnqueueGesture(new GestureSample(GestureType.Flick, GetGestureTimestamp(), Vector2.Zero, Vector2.Zero, velocity, Vector2.Zero, fingerId, -1));
			}
			velocity = Vector2.Zero;
			lastUpdatePosition = Vector2.Zero;
			updateTimestamp = DateTime.MinValue;
		}
		if (IsGestureEnabled(GestureType.DragComplete) && (state == GestureState.DRAGGING_H || state == GestureState.DRAGGING_V || state == GestureState.DRAGGING_FREE))
		{
			TouchPanel.EnqueueGesture(new GestureSample(GestureType.DragComplete, GetGestureTimestamp(), Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, fingerId, -1));
		}
		if (callBelatedPinchComplete && IsGestureEnabled(GestureType.PinchComplete))
		{
			TouchPanel.EnqueueGesture(new GestureSample(GestureType.PinchComplete, GetGestureTimestamp(), Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, -1, -1));
		}
		callBelatedPinchComplete = false;
		if (state != GestureState.JUST_TAPPED)
		{
			state = GestureState.NONE;
		}
		eventTimestamp = DateTime.Now;
	}

	internal static void OnMoved(int fingerId, Vector2 touchPosition, Vector2 delta)
	{
		if (state == GestureState.PINCHING)
		{
			OnMoved_Pinch(fingerId, touchPosition, delta);
			return;
		}
		if (activeFingerId == -1)
		{
			activeFingerId = fingerId;
		}
		if (fingerId != activeFingerId)
		{
			return;
		}
		activeFingerPosition = touchPosition;
		bool flag = IsGestureEnabled(GestureType.HorizontalDrag);
		bool flag2 = IsGestureEnabled(GestureType.VerticalDrag);
		bool flag3 = IsGestureEnabled(GestureType.FreeDrag);
		if (state == GestureState.HOLDING || state == GestureState.HELD)
		{
			float num = (touchPosition - pressPosition).Length();
			if (num > 35f)
			{
				if (flag && Math.Abs(delta.X) > Math.Abs(delta.Y))
				{
					state = GestureState.DRAGGING_H;
				}
				else if (flag2 && Math.Abs(delta.Y) > Math.Abs(delta.X))
				{
					state = GestureState.DRAGGING_V;
				}
				else if (flag3)
				{
					state = GestureState.DRAGGING_FREE;
				}
				else
				{
					state = GestureState.NONE;
				}
			}
		}
		if ((state == GestureState.DRAGGING_H) & flag)
		{
			TouchPanel.EnqueueGesture(new GestureSample(GestureType.HorizontalDrag, GetGestureTimestamp(), touchPosition, Vector2.Zero, new Vector2(delta.X, 0f), Vector2.Zero, fingerId, -1));
		}
		else if ((state == GestureState.DRAGGING_V) & flag2)
		{
			TouchPanel.EnqueueGesture(new GestureSample(GestureType.VerticalDrag, GetGestureTimestamp(), touchPosition, Vector2.Zero, new Vector2(0f, delta.Y), Vector2.Zero, fingerId, -1));
		}
		else if ((state == GestureState.DRAGGING_FREE) & flag3)
		{
			TouchPanel.EnqueueGesture(new GestureSample(GestureType.FreeDrag, GetGestureTimestamp(), touchPosition, Vector2.Zero, delta, Vector2.Zero, fingerId, -1));
		}
		if ((state == GestureState.DRAGGING_H && !flag) || (state == GestureState.DRAGGING_V && !flag2) || (state == GestureState.DRAGGING_FREE && !flag3))
		{
			state = GestureState.HELD;
		}
	}

	internal static void OnUpdate()
	{
		if (state == GestureState.PINCHING)
		{
			if (!IsGestureEnabled(GestureType.Pinch))
			{
				state = GestureState.HELD;
				secondFingerId = -1;
				callBelatedPinchComplete = true;
			}
		}
		else
		{
			if (activeFingerId == -1)
			{
				return;
			}
			if (IsGestureEnabled(GestureType.Flick))
			{
				if (updateTimestamp != DateTime.MinValue)
				{
					float num = (float)(DateTime.Now - updateTimestamp).TotalSeconds;
					Vector2 vector = activeFingerPosition - lastUpdatePosition;
					Vector2 vector2 = vector / (0.001f + num);
					velocity += (vector2 - velocity) * 0.45f;
				}
				lastUpdatePosition = activeFingerPosition;
				updateTimestamp = DateTime.Now;
			}
			if (IsGestureEnabled(GestureType.Hold) && state == GestureState.HOLDING)
			{
				TimeSpan timeSpan = DateTime.Now - eventTimestamp;
				if (timeSpan >= TimeSpan.FromSeconds(1.0))
				{
					TouchPanel.EnqueueGesture(new GestureSample(GestureType.Hold, GetGestureTimestamp(), activeFingerPosition, Vector2.Zero, Vector2.Zero, Vector2.Zero, activeFingerId, -1));
					state = GestureState.HELD;
				}
			}
		}
	}

	private static TimeSpan GetGestureTimestamp()
	{
		return TimeSpan.FromTicks(Environment.TickCount);
	}

	private static bool IsGestureEnabled(GestureType gestureType)
	{
		return (TouchPanel.EnabledGestures & gestureType) != 0;
	}

	private static void OnReleased_Pinch(int fingerId, Vector2 touchPosition)
	{
		if (fingerId != activeFingerId && fingerId != secondFingerId)
		{
			return;
		}
		if (IsGestureEnabled(GestureType.PinchComplete))
		{
			TouchPanel.EnqueueGesture(new GestureSample(GestureType.PinchComplete, GetGestureTimestamp(), Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, activeFingerId, secondFingerId));
		}
		if (fingerId == activeFingerId)
		{
			activeFingerId = secondFingerId;
			activeFingerPosition = secondFingerPosition;
		}
		secondFingerId = -1;
		bool flag = false;
		foreach (int fingerId2 in fingerIds)
		{
			if (fingerId2 != activeFingerId)
			{
				secondFingerId = fingerId2;
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			state = GestureState.HELD;
		}
	}

	private static void OnMoved_Pinch(int fingerId, Vector2 touchPosition, Vector2 delta)
	{
		if (fingerId == activeFingerId || fingerId == secondFingerId)
		{
			if (fingerId == activeFingerId)
			{
				activeFingerPosition = touchPosition;
				TouchPanel.EnqueueGesture(new GestureSample(GestureType.Pinch, GetGestureTimestamp(), activeFingerPosition, secondFingerPosition, delta, Vector2.Zero, activeFingerId, secondFingerId));
			}
			else
			{
				secondFingerPosition = touchPosition;
				TouchPanel.EnqueueGesture(new GestureSample(GestureType.Pinch, GetGestureTimestamp(), activeFingerPosition, secondFingerPosition, Vector2.Zero, delta, activeFingerId, secondFingerId));
			}
		}
	}
}
