using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Audio;

public class SoundBank : IDisposable
{
	internal AudioEngine engine;

	internal FAudio.F3DAUDIO_DSP_SETTINGS dspSettings;

	private IntPtr handle;

	private WeakReference selfReference;

	public bool IsDisposed { get; private set; }

	public bool IsInUse
	{
		get
		{
			FAudio.FACTSoundBank_GetState(handle, out var pdwState);
			return (pdwState & 0x80) != 0;
		}
	}

	public event EventHandler<EventArgs> Disposing;

	public SoundBank(AudioEngine audioEngine, string filename)
	{
		if (audioEngine == null)
		{
			throw new ArgumentNullException("audioEngine");
		}
		if (string.IsNullOrEmpty(filename))
		{
			throw new ArgumentNullException("filename");
		}
		IntPtr intPtr = TitleContainer.ReadToPointer(filename, out var size);
		FAudio.FACTAudioEngine_CreateSoundBank(audioEngine.handle, intPtr, (uint)(int)size, 0u, 0u, out handle);
		FNAPlatform.FreeFilePointer(intPtr);
		engine = audioEngine;
		selfReference = new WeakReference(this, trackResurrection: true);
		dspSettings = default;
		dspSettings.SrcChannelCount = 1u;
		dspSettings.DstChannelCount = engine.channels;
		dspSettings.pMatrixCoefficients = Marshal.AllocHGlobal((int)(4 * dspSettings.SrcChannelCount * dspSettings.DstChannelCount));
		engine.RegisterSoundBank(handle, selfReference);
		IsDisposed = false;
	}

	~SoundBank()
	{
		if (!AudioEngine.ProgramExiting)
		{
			if (!IsDisposed && IsInUse)
			{
				GC.ReRegisterForFinalize(this);
			}
			else
			{
				Dispose(disposing: false);
			}
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected void Dispose(bool disposing)
	{
		lock (engine.gcSync)
		{
			if (!IsDisposed)
			{
				if (Disposing != null)
				{
					Disposing(this, null);
				}
				if (!engine.IsDisposed)
				{
					engine.UnregisterSoundBank(handle);
					FAudio.FACTSoundBank_Destroy(handle);
					Marshal.FreeHGlobal(dspSettings.pMatrixCoefficients);
				}
				OnSoundBankDestroyed();
			}
		}
	}

	public Cue GetCue(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		ushort num = FAudio.FACTSoundBank_GetCueIndex(handle, name);
		if (num == ushort.MaxValue)
		{
			throw new InvalidOperationException("Invalid cue name!");
		}
		FAudio.FACTSoundBank_Prepare(handle, num, 0u, 0, out var ppCue);
		return new Cue(ppCue, name, this);
	}

	public void PlayCue(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		ushort num = FAudio.FACTSoundBank_GetCueIndex(handle, name);
		if (num == ushort.MaxValue)
		{
			throw new InvalidOperationException("Invalid cue name!");
		}
		FAudio.FACTSoundBank_Play(handle, num, 0u, 0, IntPtr.Zero);
	}

	public void PlayCue(string name, AudioListener listener, AudioEmitter emitter)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		if (listener == null)
		{
			throw new ArgumentNullException("listener");
		}
		if (emitter == null)
		{
			throw new ArgumentNullException("emitter");
		}
		ushort num = FAudio.FACTSoundBank_GetCueIndex(handle, name);
		if (num == ushort.MaxValue)
		{
			throw new InvalidOperationException("Invalid cue name!");
		}
		emitter.emitterData.ChannelCount = dspSettings.SrcChannelCount;
		emitter.emitterData.CurveDistanceScaler = float.MaxValue;
		FAudio.FACT3DCalculate(engine.handle3D, ref listener.listenerData, ref emitter.emitterData, ref dspSettings);
		FAudio.FACTSoundBank_Play3D(handle, num, 0u, 0, ref dspSettings, IntPtr.Zero);
	}

	internal void OnSoundBankDestroyed()
	{
		IsDisposed = true;
		handle = IntPtr.Zero;
		selfReference = null;
	}
}
