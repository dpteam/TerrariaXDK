using System;

namespace Microsoft.Xna.Framework.Audio;

public class AudioListener
{
	internal FAudio.F3DAUDIO_LISTENER listenerData;

	public Vector3 Forward
	{
		get
		{
			return new Vector3(listenerData.OrientFront.x, listenerData.OrientFront.y, 0f - listenerData.OrientFront.z);
		}
		set
		{
			listenerData.OrientFront.x = value.X;
			listenerData.OrientFront.y = value.Y;
			listenerData.OrientFront.z = 0f - value.Z;
		}
	}

	public Vector3 Position
	{
		get
		{
			return new Vector3(listenerData.Position.x, listenerData.Position.y, 0f - listenerData.Position.z);
		}
		set
		{
			listenerData.Position.x = value.X;
			listenerData.Position.y = value.Y;
			listenerData.Position.z = 0f - value.Z;
		}
	}

	public Vector3 Up
	{
		get
		{
			return new Vector3(listenerData.OrientTop.x, listenerData.OrientTop.y, 0f - listenerData.OrientTop.z);
		}
		set
		{
			listenerData.OrientTop.x = value.X;
			listenerData.OrientTop.y = value.Y;
			listenerData.OrientTop.z = 0f - value.Z;
		}
	}

	public Vector3 Velocity
	{
		get
		{
			return new Vector3(listenerData.Velocity.x, listenerData.Velocity.y, 0f - listenerData.Velocity.z);
		}
		set
		{
			listenerData.Velocity.x = value.X;
			listenerData.Velocity.y = value.Y;
			listenerData.Velocity.z = 0f - value.Z;
		}
	}

	public AudioListener()
	{
		listenerData = default;
		Forward = Vector3.Forward;
		Position = Vector3.Zero;
		Up = Vector3.Up;
		Velocity = Vector3.Zero;
		listenerData.pCone = IntPtr.Zero;
	}
}
