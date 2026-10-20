using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Audio;

public class SoundEffectInstance : IDisposable
{
	private bool INTERNAL_looped = false;

	private float INTERNAL_pan = 0f;

	private float INTERNAL_pitch = 0f;

	private SoundState INTERNAL_state = SoundState.Stopped;

	private float INTERNAL_volume = 1f;

	internal IntPtr handle;

	internal bool isDynamic;

	private SoundEffect parentEffect;

	private WeakReference selfReference;

	private bool hasStarted;

	private bool is3D;

	private bool usingReverb;

	private FAudio.F3DAUDIO_DSP_SETTINGS dspSettings;

	public bool IsDisposed { get; protected set; }

	public virtual bool IsLooped
	{
		get
		{
			return INTERNAL_looped;
		}
		set
		{
			if (hasStarted)
			{
				throw new InvalidOperationException();
			}
			INTERNAL_looped = value;
		}
	}

	public float Pan
	{
		get
		{
			return INTERNAL_pan;
		}
		set
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException("SoundEffectInstance");
			}
			if (value > 1f || value < -1f)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			INTERNAL_pan = value;
			if (!is3D)
			{
				SetPanMatrixCoefficients();
				if (handle != IntPtr.Zero)
				{
					FAudio.FAudioVoice_SetOutputMatrix(handle, SoundEffect.Device().MasterVoice, dspSettings.SrcChannelCount, dspSettings.DstChannelCount, dspSettings.pMatrixCoefficients, 0u);
				}
			}
		}
	}

	public float Pitch
	{
		get
		{
			return INTERNAL_pitch;
		}
		set
		{
			INTERNAL_pitch = MathHelper.Clamp(value, -1f, 1f);
			if (handle != IntPtr.Zero)
			{
				UpdatePitch();
			}
		}
	}

	public SoundState State
	{
		get
		{
			if (!isDynamic && handle != IntPtr.Zero && INTERNAL_state == SoundState.Playing)
			{
				FAudio.FAudioSourceVoice_GetState(handle, out var pVoiceState, 256u);
				if (pVoiceState.BuffersQueued == 0)
				{
					Stop(immediate: true);
				}
			}
			return INTERNAL_state;
		}
	}

	public float Volume
	{
		get
		{
			return INTERNAL_volume;
		}
		set
		{
			INTERNAL_volume = value;
			if (handle != IntPtr.Zero)
			{
				FAudio.FAudioVoice_SetVolume(handle, INTERNAL_volume, 0u);
			}
		}
	}

	internal SoundEffectInstance(SoundEffect parent = null)
	{
		SoundEffect.Device();
		selfReference = new WeakReference(this, trackResurrection: true);
		parentEffect = parent;
		isDynamic = this is DynamicSoundEffectInstance;
		hasStarted = false;
		is3D = false;
		usingReverb = false;
		INTERNAL_state = SoundState.Stopped;
		if (!isDynamic)
		{
			InitDSPSettings(parentEffect.channels);
		}
		if (parentEffect != null)
		{
			parentEffect.Instances.Add(selfReference);
		}
	}

	~SoundEffectInstance()
	{
		if (!IsDisposed && State == SoundState.Playing)
		{
			GC.ReRegisterForFinalize(this);
		}
		else
		{
			Dispose();
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public void Apply3D(AudioListener listener, AudioEmitter emitter)
	{
		if (listener == null)
		{
			throw new ArgumentNullException("listener");
		}
		if (emitter == null)
		{
			throw new ArgumentNullException("emitter");
		}
		if (IsDisposed)
		{
			throw new ObjectDisposedException("SoundEffectInstance");
		}
		is3D = true;
		SoundEffect.FAudioContext fAudioContext = SoundEffect.Device();
		emitter.emitterData.CurveDistanceScaler = fAudioContext.CurveDistanceScaler;
		emitter.emitterData.ChannelCount = dspSettings.SrcChannelCount;
		FAudio.F3DAudioCalculate(fAudioContext.Handle3D, ref listener.listenerData, ref emitter.emitterData, 33u, ref dspSettings);
		if (handle != IntPtr.Zero)
		{
			UpdatePitch();
			FAudio.FAudioVoice_SetOutputMatrix(handle, SoundEffect.Device().MasterVoice, dspSettings.SrcChannelCount, dspSettings.DstChannelCount, dspSettings.pMatrixCoefficients, 0u);
		}
	}

	public void Apply3D(AudioListener[] listeners, AudioEmitter emitter)
	{
		if (listeners == null)
		{
			throw new ArgumentNullException("listeners");
		}
		if (listeners.Length == 1)
		{
			Apply3D(listeners[0], emitter);
			return;
		}
		throw new NotSupportedException("Only one listener is supported.");
	}

	public virtual void Play()
	{
		if (State == SoundState.Playing)
		{
			return;
		}
		if (State == SoundState.Paused)
		{
			FAudio.FAudioSourceVoice_Start(handle, 0u, 0u);
			INTERNAL_state = SoundState.Playing;
			return;
		}
		SoundEffect.FAudioContext fAudioContext = SoundEffect.Device();
		if (isDynamic)
		{
			FAudio.FAudio_CreateSourceVoice(fAudioContext.Handle, out handle, ref (this as DynamicSoundEffectInstance).format, 8u, 2f, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
		}
		else
		{
			FAudio.FAudio_CreateSourceVoice(fAudioContext.Handle, out handle, parentEffect.formatPtr, 8u, 2f, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
		}
		if (handle == IntPtr.Zero)
		{
			return;
		}
		FAudio.FAudioVoice_SetVolume(handle, INTERNAL_volume, 0u);
		UpdatePitch();
		if (is3D || Pan != 0f)
		{
			FAudio.FAudioVoice_SetOutputMatrix(handle, SoundEffect.Device().MasterVoice, dspSettings.SrcChannelCount, dspSettings.DstChannelCount, dspSettings.pMatrixCoefficients, 0u);
		}
		if (isDynamic)
		{
			(this as DynamicSoundEffectInstance).QueueInitialBuffers();
		}
		else
		{
			if (IsLooped)
			{
				parentEffect.handle.LoopCount = 255u;
				parentEffect.handle.LoopBegin = parentEffect.loopStart;
				parentEffect.handle.LoopLength = parentEffect.loopLength;
			}
			else
			{
				parentEffect.handle.LoopCount = 0u;
				parentEffect.handle.LoopBegin = 0u;
				parentEffect.handle.LoopLength = 0u;
			}
			FAudio.FAudioSourceVoice_SubmitSourceBuffer(handle, ref parentEffect.handle, IntPtr.Zero);
		}
		FAudio.FAudioSourceVoice_Start(handle, 0u, 0u);
		INTERNAL_state = SoundState.Playing;
		hasStarted = true;
	}

	public void Pause()
	{
		if (handle != IntPtr.Zero && State == SoundState.Playing)
		{
			FAudio.FAudioSourceVoice_Stop(handle, 0u, 0u);
			INTERNAL_state = SoundState.Paused;
		}
	}

	public void Resume()
	{
		SoundState state = State;
		if (handle == IntPtr.Zero)
		{
			Play();
		}
		else if (state == SoundState.Paused)
		{
			FAudio.FAudioSourceVoice_Start(handle, 0u, 0u);
			INTERNAL_state = SoundState.Playing;
		}
	}

	public void Stop()
	{
		Stop(immediate: true);
	}

	public void Stop(bool immediate)
	{
		if (handle == IntPtr.Zero)
		{
			return;
		}
		if (immediate)
		{
			FAudio.FAudioSourceVoice_Stop(handle, 0u, 0u);
			FAudio.FAudioSourceVoice_FlushSourceBuffers(handle);
			FAudio.FAudioVoice_DestroyVoice(handle);
			handle = IntPtr.Zero;
			usingReverb = false;
			INTERNAL_state = SoundState.Stopped;
			if (isDynamic)
			{
				lock (FrameworkDispatcher.Streams)
				{
					FrameworkDispatcher.Streams.Remove(this as DynamicSoundEffectInstance);
				}
				(this as DynamicSoundEffectInstance).ClearBuffers();
			}
		}
		else
		{
			if (isDynamic)
			{
				throw new InvalidOperationException();
			}
			FAudio.FAudioSourceVoice_ExitLoop(handle, 0u);
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			Stop(immediate: true);
			if (parentEffect != null)
			{
				parentEffect.Instances.Remove(selfReference);
			}
			selfReference = null;
			Marshal.FreeHGlobal(dspSettings.pMatrixCoefficients);
			IsDisposed = true;
		}
	}

	internal unsafe void InitDSPSettings(uint srcChannels)
	{
		dspSettings = default;
		dspSettings.DopplerFactor = 1f;
		dspSettings.SrcChannelCount = srcChannels;
		dspSettings.DstChannelCount = SoundEffect.Device().DeviceDetails.OutputFormat.Format.nChannels;
		int num = (int)(4 * dspSettings.SrcChannelCount * dspSettings.DstChannelCount);
		dspSettings.pMatrixCoefficients = Marshal.AllocHGlobal(num);
		byte* ptr = (byte*)(void*)dspSettings.pMatrixCoefficients;
		for (int i = 0; i < num; i++)
		{
			ptr[i] = 0;
		}
		SetPanMatrixCoefficients();
	}

	internal unsafe void INTERNAL_applyReverb(float rvGain)
	{
		if (!(handle == IntPtr.Zero))
		{
			if (!usingReverb)
			{
				SoundEffect.Device().AttachReverb(handle);
				usingReverb = true;
			}
			float* ptr = (float*)(void*)dspSettings.pMatrixCoefficients;
			*ptr = rvGain;
			if (dspSettings.SrcChannelCount == 2)
			{
				ptr[1] = rvGain;
			}
			FAudio.FAudioVoice_SetOutputMatrix(handle, SoundEffect.Device().ReverbVoice, dspSettings.SrcChannelCount, 1u, dspSettings.pMatrixCoefficients, 0u);
		}
	}

	internal void INTERNAL_applyLowPassFilter(float cutoff)
	{
		if (!(handle == IntPtr.Zero))
		{
			FAudio.FAudioFilterParameters pParameters = new FAudio.FAudioFilterParameters
			{
				Type = FAudio.FAudioFilterType.FAudioLowPassFilter,
				Frequency = cutoff,
				OneOverQ = 1f
			};
			FAudio.FAudioVoice_SetFilterParameters(handle, ref pParameters, 0u);
		}
	}

	internal void INTERNAL_applyHighPassFilter(float cutoff)
	{
		if (!(handle == IntPtr.Zero))
		{
			FAudio.FAudioFilterParameters pParameters = new FAudio.FAudioFilterParameters
			{
				Type = FAudio.FAudioFilterType.FAudioHighPassFilter,
				Frequency = cutoff,
				OneOverQ = 1f
			};
			FAudio.FAudioVoice_SetFilterParameters(handle, ref pParameters, 0u);
		}
	}

	internal void INTERNAL_applyBandPassFilter(float center)
	{
		if (!(handle == IntPtr.Zero))
		{
			FAudio.FAudioFilterParameters pParameters = new FAudio.FAudioFilterParameters
			{
				Type = FAudio.FAudioFilterType.FAudioBandPassFilter,
				Frequency = center,
				OneOverQ = 1f
			};
			FAudio.FAudioVoice_SetFilterParameters(handle, ref pParameters, 0u);
		}
	}

	private void UpdatePitch()
	{
		float dopplerScale = SoundEffect.Device().DopplerScale;
		float num = ((is3D && dopplerScale != 0f) ? (dspSettings.DopplerFactor * dopplerScale) : 1f);
		FAudio.FAudioSourceVoice_SetFrequencyRatio(handle, (float)Math.Pow(2.0, INTERNAL_pitch) * num, 0u);
	}

	private unsafe void SetPanMatrixCoefficients()
	{
		float* ptr = (float*)(void*)dspSettings.pMatrixCoefficients;
		if (dspSettings.SrcChannelCount == 1)
		{
			if (dspSettings.DstChannelCount == 1)
			{
				*ptr = 1f;
				return;
			}
			*ptr = ((INTERNAL_pan > 0f) ? (1f - INTERNAL_pan) : 1f);
			ptr[1] = ((INTERNAL_pan < 0f) ? (1f + INTERNAL_pan) : 1f);
		}
		else if (dspSettings.DstChannelCount == 1)
		{
			*ptr = 1f;
			ptr[1] = 1f;
		}
		else if (INTERNAL_pan <= 0f)
		{
			*ptr = 0.5f * INTERNAL_pan + 1f;
			ptr[1] = 0.5f * (0f - INTERNAL_pan);
			ptr[2] = 0f;
			ptr[3] = INTERNAL_pan + 1f;
		}
		else
		{
			*ptr = 0f - INTERNAL_pan + 1f;
			ptr[1] = 0f;
			ptr[2] = 0.5f * INTERNAL_pan;
			ptr[3] = 0.5f * (0f - INTERNAL_pan) + 1f;
		}
	}
}
