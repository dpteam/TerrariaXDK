using System;

namespace Microsoft.Xna.Framework.Audio;

public sealed class Cue : IDisposable
{
	private IntPtr handle;

	private SoundBank bank;

	private WeakReference selfReference;

	public bool IsCreated
	{
		get
		{
			FAudio.FACTCue_GetState(handle, out var pdwState);
			return (pdwState & 1) != 0;
		}
	}

	public bool IsDisposed { get; private set; }

	public bool IsPaused
	{
		get
		{
			FAudio.FACTCue_GetState(handle, out var pdwState);
			return (pdwState & 0x40) != 0;
		}
	}

	public bool IsPlaying
	{
		get
		{
			FAudio.FACTCue_GetState(handle, out var pdwState);
			return (pdwState & 8) != 0;
		}
	}

	public bool IsPrepared
	{
		get
		{
			FAudio.FACTCue_GetState(handle, out var pdwState);
			return (pdwState & 4) != 0;
		}
	}

	public bool IsPreparing
	{
		get
		{
			FAudio.FACTCue_GetState(handle, out var pdwState);
			return (pdwState & 2) != 0;
		}
	}

	public bool IsStopped
	{
		get
		{
			FAudio.FACTCue_GetState(handle, out var pdwState);
			return (pdwState & 0x20) != 0;
		}
	}

	public bool IsStopping
	{
		get
		{
			FAudio.FACTCue_GetState(handle, out var pdwState);
			return (pdwState & 0x10) != 0;
		}
	}

	public string Name { get; private set; }

	public event EventHandler<EventArgs> Disposing;

	internal Cue(IntPtr cue, string name, SoundBank soundBank)
	{
		handle = cue;
		Name = name;
		bank = soundBank;
		selfReference = new WeakReference(this, trackResurrection: true);
		bank.engine.RegisterCue(handle, selfReference);
	}

	~Cue()
	{
		if (!AudioEngine.ProgramExiting)
		{
			if (!IsDisposed && IsPlaying)
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
		emitter.emitterData.ChannelCount = bank.dspSettings.SrcChannelCount;
		emitter.emitterData.CurveDistanceScaler = float.MaxValue;
		FAudio.FACT3DCalculate(bank.engine.handle3D, ref listener.listenerData, ref emitter.emitterData, ref bank.dspSettings);
		FAudio.FACT3DApply(ref bank.dspSettings, handle);
	}

	public float GetVariable(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		ushort num = FAudio.FACTCue_GetVariableIndex(handle, name);
		if (num == ushort.MaxValue)
		{
			throw new InvalidOperationException("Invalid variable name!");
		}
		FAudio.FACTCue_GetVariable(handle, num, out var nValue);
		return nValue;
	}

	public void Pause()
	{
		FAudio.FACTCue_Pause(handle, 1);
	}

	public void Play()
	{
		FAudio.FACTCue_Play(handle);
	}

	public void Resume()
	{
		FAudio.FACTCue_Pause(handle, 0);
	}

	public void SetVariable(string name, float value)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		ushort num = FAudio.FACTCue_GetVariableIndex(handle, name);
		if (num == ushort.MaxValue)
		{
			throw new InvalidOperationException("Invalid variable name!");
		}
		FAudio.FACTCue_SetVariable(handle, num, value);
	}

	public void Stop(AudioStopOptions options)
	{
		FAudio.FACTCue_Stop(handle, (options == AudioStopOptions.Immediate) ? 1u : 0u);
	}

	internal void OnCueDestroyed()
	{
		IsDisposed = true;
		handle = IntPtr.Zero;
		selfReference = null;
	}

	private void Dispose(bool disposing)
	{
		lock (bank.engine.gcSync)
		{
			if (!IsDisposed)
			{
				if (Disposing != null)
				{
					Disposing(this, null);
				}
				if (!bank.engine.IsDisposed)
				{
					bank.engine.UnregisterCue(handle);
					FAudio.FACTCue_Destroy(handle);
				}
				OnCueDestroyed();
			}
		}
	}
}
