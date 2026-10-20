using System;

namespace Microsoft.Xna.Framework.Audio;

public struct AudioCategory : IEquatable<AudioCategory>
{
	private string INTERNAL_name;

	private AudioEngine parent;

	private ushort index;

	public string Name => INTERNAL_name;

	internal AudioCategory(AudioEngine engine, ushort category, string name)
	{
		parent = engine;
		index = category;
		INTERNAL_name = name;
	}

	public void Pause()
	{
		lock (parent.gcSync)
		{
			if (!parent.IsDisposed)
			{
				FAudio.FACTAudioEngine_Pause(parent.handle, index, 1);
			}
		}
	}

	public void Resume()
	{
		lock (parent.gcSync)
		{
			if (!parent.IsDisposed)
			{
				FAudio.FACTAudioEngine_Pause(parent.handle, index, 0);
			}
		}
	}

	public void SetVolume(float volume)
	{
		lock (parent.gcSync)
		{
			if (!parent.IsDisposed)
			{
				FAudio.FACTAudioEngine_SetVolume(parent.handle, index, volume);
			}
		}
	}

	public void Stop(AudioStopOptions options)
	{
		lock (parent.gcSync)
		{
			if (!parent.IsDisposed)
			{
				FAudio.FACTAudioEngine_Stop(parent.handle, index, (options == AudioStopOptions.Immediate) ? 1u : 0u);
			}
		}
	}

	public override int GetHashCode()
	{
		return Name.GetHashCode();
	}

	public bool Equals(AudioCategory other)
	{
		return GetHashCode() == other.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj is AudioCategory)
		{
			return Equals((AudioCategory)obj);
		}
		return false;
	}

	public static bool operator ==(AudioCategory value1, AudioCategory value2)
	{
		return value1.Equals(value2);
	}

	public static bool operator !=(AudioCategory value1, AudioCategory value2)
	{
		return !value1.Equals(value2);
	}
}
