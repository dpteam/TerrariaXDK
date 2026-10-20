using System;

namespace Microsoft.Xna.Framework.Input.Touch;

public struct TouchLocation : IEquatable<TouchLocation>
{
	private Vector2 prevPosition;

	private TouchLocationState prevState;

	public int Id { get; private set; }

	public Vector2 Position { get; private set; }

	public TouchLocationState State { get; private set; }

	public TouchLocation(int id, TouchLocationState state, Vector2 position)
	{
		this = default;
		Id = id;
		State = state;
		Position = position;
		prevState = TouchLocationState.Invalid;
		prevPosition = Vector2.Zero;
	}

	public TouchLocation(int id, TouchLocationState state, Vector2 position, TouchLocationState previousState, Vector2 previousPosition)
	{
		this = default;
		Id = id;
		State = state;
		Position = position;
		prevState = previousState;
		prevPosition = previousPosition;
	}

	public bool Equals(TouchLocation other)
	{
		return Id == other.Id && Position == other.Position && State == other.State && prevPosition == other.prevPosition && prevState == other.prevState;
	}

	public override bool Equals(object obj)
	{
		return obj is TouchLocation && Equals((TouchLocation)obj);
	}

	public override int GetHashCode()
	{
		return Id.GetHashCode() + Position.GetHashCode();
	}

	public override string ToString()
	{
		return "{Position:" + Position.ToString() + "}";
	}

	public bool TryGetPreviousLocation(out TouchLocation previousLocation)
	{
		previousLocation = new TouchLocation(Id, prevState, prevPosition);
		return previousLocation.State != TouchLocationState.Invalid;
	}

	public static bool operator ==(TouchLocation value1, TouchLocation value2)
	{
		return value1.Equals(value2);
	}

	public static bool operator !=(TouchLocation value1, TouchLocation value2)
	{
		return !value1.Equals(value2);
	}
}
