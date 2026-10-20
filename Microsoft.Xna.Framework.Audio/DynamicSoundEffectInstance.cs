using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Audio;

public sealed class DynamicSoundEffectInstance : SoundEffectInstance
{
	internal FAudio.FAudioWaveFormatEx format;

	private int sampleRate;

	private AudioChannels channels;

	private List<IntPtr> queuedBuffers;

	private List<uint> queuedSizes;

	private const int MINIMUM_BUFFER_CHECK = 3;

	public int PendingBufferCount => queuedBuffers.Count;

	public override bool IsLooped
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	public event EventHandler<EventArgs> BufferNeeded;

	public DynamicSoundEffectInstance(int sampleRate, AudioChannels channels)
	{
		this.sampleRate = sampleRate;
		this.channels = channels;
		isDynamic = true;
		format = default;
		format.wFormatTag = 1;
		format.nChannels = (ushort)channels;
		format.nSamplesPerSec = (uint)sampleRate;
		format.wBitsPerSample = 16;
		format.nBlockAlign = (ushort)(2 * format.nChannels);
		format.nAvgBytesPerSec = format.nBlockAlign * format.nSamplesPerSec;
		format.cbSize = 0;
		queuedBuffers = new List<IntPtr>();
		queuedSizes = new List<uint>();
		InitDSPSettings(format.nChannels);
	}

	~DynamicSoundEffectInstance()
	{
		Dispose();
	}

	public TimeSpan GetSampleDuration(int sizeInBytes)
	{
		return SoundEffect.GetSampleDuration(sizeInBytes, sampleRate, channels);
	}

	public int GetSampleSizeInBytes(TimeSpan duration)
	{
		return SoundEffect.GetSampleSizeInBytes(duration, sampleRate, channels);
	}

	public override void Play()
	{
		Update();
		base.Play();
		lock (FrameworkDispatcher.Streams)
		{
			FrameworkDispatcher.Streams.Add(this);
		}
	}

	public void SubmitBuffer(byte[] buffer)
	{
		SubmitBuffer(buffer, 0, buffer.Length);
	}

	public void SubmitBuffer(byte[] buffer, int offset, int count)
	{
		IntPtr intPtr = Marshal.AllocHGlobal(count);
		Marshal.Copy(buffer, offset, intPtr, count);
		lock (queuedBuffers)
		{
			queuedBuffers.Add(intPtr);
			if (State != SoundState.Stopped)
			{
				FAudio.FAudioBuffer pBuffer = default;
				pBuffer.AudioBytes = (uint)count;
				pBuffer.pAudioData = intPtr;
				pBuffer.PlayLength = pBuffer.AudioBytes / (uint)channels / (uint)(format.wBitsPerSample / 8);
				FAudio.FAudioSourceVoice_SubmitSourceBuffer(handle, ref pBuffer, IntPtr.Zero);
			}
			else
			{
				queuedSizes.Add((uint)count);
			}
		}
	}

	public void SubmitFloatBufferEXT(float[] buffer)
	{
		SubmitFloatBufferEXT(buffer, 0, buffer.Length);
	}

	public void SubmitFloatBufferEXT(float[] buffer, int offset, int count)
	{
		if (State != SoundState.Stopped && format.wFormatTag == 1)
		{
			throw new InvalidOperationException("Submit a float buffer before Playing!");
		}
		format.wFormatTag = 3;
		format.wBitsPerSample = 32;
		format.nBlockAlign = (ushort)(4 * format.nChannels);
		format.nAvgBytesPerSec = format.nBlockAlign * format.nSamplesPerSec;
		IntPtr intPtr = Marshal.AllocHGlobal(count * 4);
		Marshal.Copy(buffer, offset, intPtr, count);
		lock (queuedBuffers)
		{
			queuedBuffers.Add(intPtr);
			if (State != SoundState.Stopped)
			{
				FAudio.FAudioBuffer pBuffer = default;
				pBuffer.AudioBytes = (uint)(count * 4);
				pBuffer.pAudioData = intPtr;
				pBuffer.PlayLength = pBuffer.AudioBytes / (uint)channels / (uint)(format.wBitsPerSample / 8);
				FAudio.FAudioSourceVoice_SubmitSourceBuffer(handle, ref pBuffer, IntPtr.Zero);
			}
			else
			{
				queuedSizes.Add((uint)(count * 4));
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
	}

	internal void QueueInitialBuffers()
	{
		FAudio.FAudioBuffer pBuffer = default;
		lock (queuedBuffers)
		{
			for (int i = 0; i < queuedBuffers.Count; i++)
			{
				pBuffer.AudioBytes = queuedSizes[i];
				pBuffer.pAudioData = queuedBuffers[i];
				pBuffer.PlayLength = pBuffer.AudioBytes / (uint)channels / (uint)(format.wBitsPerSample / 8);
				FAudio.FAudioSourceVoice_SubmitSourceBuffer(handle, ref pBuffer, IntPtr.Zero);
			}
			queuedSizes.Clear();
		}
	}

	internal void ClearBuffers()
	{
		lock (queuedBuffers)
		{
			foreach (IntPtr queuedBuffer in queuedBuffers)
			{
				Marshal.FreeHGlobal(queuedBuffer);
			}
			queuedBuffers.Clear();
			queuedSizes.Clear();
		}
	}

	internal void Update()
	{
		if (State != SoundState.Playing)
		{
			return;
		}
		if (handle != IntPtr.Zero)
		{
			FAudio.FAudioSourceVoice_GetState(handle, out var pVoiceState, 256u);
			while (PendingBufferCount > pVoiceState.BuffersQueued)
			{
				lock (queuedBuffers)
				{
					Marshal.FreeHGlobal(queuedBuffers[0]);
					queuedBuffers.RemoveAt(0);
				}
			}
		}
		int num = 3 - PendingBufferCount;
		while (num > 0 && BufferNeeded != null)
		{
			BufferNeeded(this, null);
			num--;
		}
	}
}
