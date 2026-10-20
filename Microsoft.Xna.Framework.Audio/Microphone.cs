using System;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Audio;

public class Microphone
{
	public readonly string Name;

	private TimeSpan bufferDuration;

	private uint handle;

	internal static ReadOnlyCollection<Microphone> micList;

	internal const int SAMPLERATE = 44100;

	public static ReadOnlyCollection<Microphone> All
	{
		get
		{
			if (micList == null)
			{
				micList = new ReadOnlyCollection<Microphone>(FNAPlatform.GetMicrophones());
			}
			return micList;
		}
	}

	public static Microphone Default
	{
		get
		{
			if (All.Count == 0)
			{
				return null;
			}
			return All[0];
		}
	}

	public TimeSpan BufferDuration
	{
		get
		{
			return bufferDuration;
		}
		set
		{
			if (value.Milliseconds < 100 || value.Milliseconds > 1000 || value.Milliseconds % 10 != 0)
			{
				throw new ArgumentOutOfRangeException();
			}
			bufferDuration = value;
		}
	}

	public bool IsHeadset => false;

	public int SampleRate => 44100;

	public MicrophoneState State { get; private set; }

	public event EventHandler<EventArgs> BufferReady;

	internal Microphone(uint id, string name)
	{
		handle = id;
		Name = name;
		bufferDuration = TimeSpan.FromSeconds(1.0);
		State = MicrophoneState.Stopped;
	}

	public int GetData(byte[] buffer)
	{
		return GetData(buffer, 0, buffer.Length);
	}

	public int GetData(byte[] buffer, int offset, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentException("buffer is null!");
		}
		if (offset < 0 || offset > buffer.Length)
		{
			throw new ArgumentException("offset");
		}
		if (count <= 0 || offset + count > buffer.Length)
		{
			throw new ArgumentException("count");
		}
		return FNAPlatform.GetMicrophoneSamples(handle, buffer, offset, count);
	}

	public TimeSpan GetSampleDuration(int sizeInBytes)
	{
		return SoundEffect.GetSampleDuration(sizeInBytes, SampleRate, AudioChannels.Mono);
	}

	public int GetSampleSizeInBytes(TimeSpan duration)
	{
		return SoundEffect.GetSampleSizeInBytes(duration, SampleRate, AudioChannels.Mono);
	}

	public void Start()
	{
		FNAPlatform.StartMicrophone(handle);
		State = MicrophoneState.Started;
	}

	public void Stop()
	{
		FNAPlatform.StopMicrophone(handle);
		State = MicrophoneState.Stopped;
	}

	internal void CheckBuffer()
	{
		if (BufferReady != null && GetSampleDuration(FNAPlatform.GetMicrophoneQueuedBytes(handle)) > bufferDuration)
		{
			BufferReady(this, EventArgs.Empty);
		}
	}
}
