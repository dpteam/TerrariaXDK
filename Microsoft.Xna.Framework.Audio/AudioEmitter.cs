using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Audio;

public class AudioEmitter
{
	internal FAudio.F3DAUDIO_EMITTER emitterData;

	private static readonly float[] stereoAzimuth = new float[2];

	private static readonly GCHandle stereoAzimuthHandle = GCHandle.Alloc(stereoAzimuth, GCHandleType.Pinned);

	public float DopplerScale
	{
		get
		{
			return emitterData.DopplerScaler;
		}
		set
		{
			if (value < 0f)
			{
				throw new ArgumentOutOfRangeException("AudioEmitter.DopplerScale must be greater than or equal to 0.0f");
			}
			emitterData.DopplerScaler = value;
		}
	}

	public Vector3 Forward
	{
		get
		{
			return new Vector3(emitterData.OrientFront.x, emitterData.OrientFront.y, 0f - emitterData.OrientFront.z);
		}
		set
		{
			emitterData.OrientFront.x = value.X;
			emitterData.OrientFront.y = value.Y;
			emitterData.OrientFront.z = 0f - value.Z;
		}
	}

	public Vector3 Position
	{
		get
		{
			return new Vector3(emitterData.Position.x, emitterData.Position.y, 0f - emitterData.Position.z);
		}
		set
		{
			emitterData.Position.x = value.X;
			emitterData.Position.y = value.Y;
			emitterData.Position.z = 0f - value.Z;
		}
	}

	public Vector3 Up
	{
		get
		{
			return new Vector3(emitterData.OrientTop.x, emitterData.OrientTop.y, 0f - emitterData.OrientTop.z);
		}
		set
		{
			emitterData.OrientTop.x = value.X;
			emitterData.OrientTop.y = value.Y;
			emitterData.OrientTop.z = 0f - value.Z;
		}
	}

	public Vector3 Velocity
	{
		get
		{
			return new Vector3(emitterData.Velocity.x, emitterData.Velocity.y, 0f - emitterData.Velocity.z);
		}
		set
		{
			emitterData.Velocity.x = value.X;
			emitterData.Velocity.y = value.Y;
			emitterData.Velocity.z = 0f - value.Z;
		}
	}

	public AudioEmitter()
	{
		emitterData = default;
		DopplerScale = 1f;
		Forward = Vector3.Forward;
		Position = Vector3.Zero;
		Up = Vector3.Up;
		Velocity = Vector3.Zero;
		emitterData.pCone = IntPtr.Zero;
		emitterData.ChannelCount = 1u;
		emitterData.ChannelRadius = 1f;
		emitterData.pChannelAzimuths = stereoAzimuthHandle.AddrOfPinnedObject();
		emitterData.pVolumeCurve = IntPtr.Zero;
		emitterData.pLFECurve = IntPtr.Zero;
		emitterData.pLPFDirectCurve = IntPtr.Zero;
		emitterData.pLPFReverbCurve = IntPtr.Zero;
		emitterData.pReverbCurve = IntPtr.Zero;
		emitterData.CurveDistanceScaler = 1f;
	}
}
