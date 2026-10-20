using System;

namespace Microsoft.Xna.Framework.Input.Touch;

public struct GestureSample
{
	public Vector2 Delta { get; private set; }

	public Vector2 Delta2 { get; private set; }

	public GestureType GestureType { get; private set; }

	public Vector2 Position { get; private set; }

	public Vector2 Position2 { get; private set; }

	public TimeSpan Timestamp { get; private set; }

	public int FingerIdEXT { get; private set; }

	public int FingerId2EXT { get; private set; }

	public GestureSample(GestureType gestureType, TimeSpan timestamp, Vector2 position, Vector2 position2, Vector2 delta, Vector2 delta2)
	{
		this = default;
		GestureType = gestureType;
		Timestamp = timestamp;
		Position = position;
		Position2 = position2;
		Delta = delta;
		Delta2 = delta2;
		FingerIdEXT = -1;
		FingerId2EXT = -1;
	}

	internal GestureSample(GestureType gestureType, TimeSpan timestamp, Vector2 position, Vector2 position2, Vector2 delta, Vector2 delta2, int fingerId, int fingerId2)
	{
		this = default;
		GestureType = gestureType;
		Timestamp = timestamp;
		Position = position;
		Position2 = position2;
		Delta = delta;
		Delta2 = delta2;
		FingerIdEXT = fingerId;
		FingerId2EXT = fingerId2;
	}
}
