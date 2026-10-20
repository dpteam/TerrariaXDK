using System;
using System.IO;
using MonoGame.Utilities;

namespace Microsoft.Xna.Framework.Audio;

public class WaveBank : IDisposable
{
	private IntPtr handle;

	private AudioEngine engine;

	private WeakReference selfReference;

	private IntPtr bankData;

	private IntPtr bankDataLen;

	public bool IsDisposed { get; private set; }

	public bool IsPrepared
	{
		get
		{
			FAudio.FACTWaveBank_GetState(handle, out var pdwState);
			return (pdwState & 4) != 0;
		}
	}

	public bool IsInUse
	{
		get
		{
			FAudio.FACTWaveBank_GetState(handle, out var pdwState);
			return (pdwState & 0x80) != 0;
		}
	}

	public event EventHandler<EventArgs> Disposing;

	public WaveBank(AudioEngine audioEngine, string nonStreamingWaveBankFilename)
	{
		if (audioEngine == null)
		{
			throw new ArgumentNullException("audioEngine");
		}
		if (string.IsNullOrEmpty(nonStreamingWaveBankFilename))
		{
			throw new ArgumentNullException("nonStreamingWaveBankFilename");
		}
		bankData = TitleContainer.ReadToPointer(nonStreamingWaveBankFilename, out bankDataLen);
		FAudio.FACTAudioEngine_CreateInMemoryWaveBank(audioEngine.handle, bankData, (uint)(int)bankDataLen, 0u, 0u, out handle);
		engine = audioEngine;
		selfReference = new WeakReference(this, trackResurrection: true);
		engine.RegisterWaveBank(handle, selfReference);
		IsDisposed = false;
	}

	public WaveBank(AudioEngine audioEngine, string streamingWaveBankFilename, int offset, short packetsize)
	{
		if (audioEngine == null)
		{
			throw new ArgumentNullException("audioEngine");
		}
		if (string.IsNullOrEmpty(streamingWaveBankFilename))
		{
			throw new ArgumentNullException("streamingWaveBankFilename");
		}
		string text = FileHelpers.NormalizeFilePathSeparators(streamingWaveBankFilename);
		if (!Path.IsPathRooted(text))
		{
			text = Path.Combine(TitleLocation.Path, text);
		}
		bankData = FAudio.FAudio_fopen(text);
		FAudio.FACTStreamingParameters pParms = new FAudio.FACTStreamingParameters
		{
			file = bankData
		};
		FAudio.FACTAudioEngine_CreateStreamingWaveBank(audioEngine.handle, ref pParms, out handle);
		engine = audioEngine;
		selfReference = new WeakReference(this, trackResurrection: true);
		engine.RegisterWaveBank(handle, selfReference);
		IsDisposed = false;
	}

	~WaveBank()
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

	protected virtual void Dispose(bool disposing)
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
					engine.UnregisterWaveBank(handle);
					FAudio.FACTWaveBank_Destroy(handle);
				}
				OnWaveBankDestroyed();
			}
		}
	}

	internal void OnWaveBankDestroyed()
	{
		IsDisposed = true;
		if (bankData != IntPtr.Zero)
		{
			if (bankDataLen != IntPtr.Zero)
			{
				FNAPlatform.FreeFilePointer(bankData);
				bankDataLen = IntPtr.Zero;
			}
			else
			{
				FAudio.FAudio_close(bankData);
			}
			bankData = IntPtr.Zero;
		}
		handle = IntPtr.Zero;
		selfReference = null;
	}
}
