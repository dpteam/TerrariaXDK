using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using ObjCRuntime;

namespace Microsoft.Xna.Framework.Audio;

public class AudioEngine : IDisposable
{
	private class IntPtrComparer : IEqualityComparer<IntPtr>
	{
		public bool Equals(IntPtr x, IntPtr y)
		{
			return x == y;
		}

		public int GetHashCode(IntPtr obj)
		{
			return obj.GetHashCode();
		}
	}

	public const int ContentVersion = 46;

	internal readonly IntPtr handle;

	internal readonly byte[] handle3D;

	internal readonly ushort channels;

	internal readonly object gcSync = new object();

	private RendererDetail[] rendererDetails;

	private readonly FAudio.FACTNotificationCallback xactNotificationFunc;

	private FAudio.FACTNotificationDescription notificationDesc;

	private static readonly IntPtrComparer comparer = new IntPtrComparer();

	private static readonly Dictionary<IntPtr, WeakReference> xactPtrs = new Dictionary<IntPtr, WeakReference>(comparer);

	internal static bool ProgramExiting = false;

	public ReadOnlyCollection<RendererDetail> RendererDetails => new ReadOnlyCollection<RendererDetail>(rendererDetails);

	public bool IsDisposed { get; private set; }

	public event EventHandler<EventArgs> Disposing;

	public AudioEngine(string settingsFile)
		: this(settingsFile, new TimeSpan(0, 0, 0, 0, 250), null)
	{
	}

	public unsafe AudioEngine(string settingsFile, TimeSpan lookAheadTime, string rendererId)
	{
		if (string.IsNullOrEmpty(settingsFile))
		{
			throw new ArgumentNullException("settingsFile");
		}
		FAudio.FACTCreateEngine(0u, out handle);
		FAudio.FACTAudioEngine_GetRendererCount(handle, out var pnRendererCount);
		if (pnRendererCount == 0)
		{
			FAudio.FACTAudioEngine_Release(handle);
			throw new NoAudioHardwareException();
		}
		rendererDetails = new RendererDetail[pnRendererCount];
		byte[] array = new byte[510];
		for (ushort num = 0; num < pnRendererCount; num++)
		{
			FAudio.FACTAudioEngine_GetRendererDetails(handle, num, out var pRendererDetails);
			Marshal.Copy((IntPtr)pRendererDetails.displayName, array, 0, array.Length);
			string name = Encoding.Unicode.GetString(array).TrimEnd(default(char));
			Marshal.Copy((IntPtr)pRendererDetails.rendererID, array, 0, array.Length);
			string id = Encoding.Unicode.GetString(array).TrimEnd(default(char));
			rendererDetails[num] = new RendererDetail(name, id);
		}
		IntPtr pGlobalSettingsBuffer = TitleContainer.ReadToPointer(settingsFile, out var size);
		FAudio.FACTRuntimeParameters pParams = new FAudio.FACTRuntimeParameters
		{
			pGlobalSettingsBuffer = pGlobalSettingsBuffer,
			globalSettingsBufferSize = (uint)(int)size,
			globalSettingsFlags = 1u
		};
		xactNotificationFunc = OnXACTNotification;
		pParams.fnNotificationCallback = Marshal.GetFunctionPointerForDelegate((Delegate)xactNotificationFunc);
		pParams.lookAheadTime = (uint)lookAheadTime.Milliseconds;
		if (!string.IsNullOrEmpty(rendererId))
		{
			pParams.pRendererID = Marshal.StringToHGlobalAuto(rendererId);
		}
		if (FAudio.FACTAudioEngine_Initialize(handle, ref pParams) != 0)
		{
			throw new InvalidOperationException("Engine initialization failed!");
		}
		if (pParams.pRendererID != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(pParams.pRendererID);
		}
		handle3D = new byte[20];
		FAudio.FACT3DInitialize(handle, handle3D);
		FAudio.FACTAudioEngine_GetFinalMixFormat(handle, out var pFinalMixFormat);
		channels = pFinalMixFormat.Format.nChannels;
		notificationDesc = default;
	}

	~AudioEngine()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public AudioCategory GetCategory(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		ushort num = FAudio.FACTAudioEngine_GetCategory(handle, name);
		if (num == ushort.MaxValue)
		{
			throw new InvalidOperationException("Invalid category name!");
		}
		return new AudioCategory(this, num, name);
	}

	public float GetGlobalVariable(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		ushort num = FAudio.FACTAudioEngine_GetGlobalVariableIndex(handle, name);
		if (num == ushort.MaxValue)
		{
			throw new InvalidOperationException("Invalid variable name!");
		}
		FAudio.FACTAudioEngine_GetGlobalVariable(handle, num, out var pnValue);
		return pnValue;
	}

	public void SetGlobalVariable(string name, float value)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		ushort num = FAudio.FACTAudioEngine_GetGlobalVariableIndex(handle, name);
		if (num == ushort.MaxValue)
		{
			throw new InvalidOperationException("Invalid variable name!");
		}
		FAudio.FACTAudioEngine_SetGlobalVariable(handle, num, value);
	}

	public void Update()
	{
		FAudio.FACTAudioEngine_DoWork(handle);
	}

	protected virtual void Dispose(bool disposing)
	{
		lock (gcSync)
		{
			if (!IsDisposed)
			{
				if (Disposing != null)
				{
					Disposing(this, null);
				}
				FAudio.FACTAudioEngine_ShutDown(handle);
				FAudio.FACTAudioEngine_Release(handle);
				rendererDetails = null;
				IsDisposed = true;
			}
		}
	}

	internal void RegisterWaveBank(IntPtr ptr, WeakReference reference)
	{
		notificationDesc.type = 7;
		notificationDesc.pWaveBank = ptr;
		FAudio.FACTAudioEngine_RegisterNotification(handle, ref notificationDesc);
		lock (xactPtrs)
		{
			xactPtrs.Add(ptr, reference);
		}
	}

	internal void RegisterSoundBank(IntPtr ptr, WeakReference reference)
	{
		notificationDesc.type = 6;
		notificationDesc.pSoundBank = ptr;
		FAudio.FACTAudioEngine_RegisterNotification(handle, ref notificationDesc);
		lock (xactPtrs)
		{
			xactPtrs.Add(ptr, reference);
		}
	}

	internal void RegisterCue(IntPtr ptr, WeakReference reference)
	{
		notificationDesc.type = 4;
		notificationDesc.pCue = ptr;
		FAudio.FACTAudioEngine_RegisterNotification(handle, ref notificationDesc);
		lock (xactPtrs)
		{
			xactPtrs.Add(ptr, reference);
		}
	}

	internal void UnregisterWaveBank(IntPtr ptr)
	{
		lock (xactPtrs)
		{
			if (xactPtrs.ContainsKey(ptr))
			{
				notificationDesc.type = 7;
				notificationDesc.pWaveBank = ptr;
				FAudio.FACTAudioEngine_UnRegisterNotification(handle, ref notificationDesc);
				xactPtrs.Remove(ptr);
			}
		}
	}

	internal void UnregisterSoundBank(IntPtr ptr)
	{
		lock (xactPtrs)
		{
			if (xactPtrs.ContainsKey(ptr))
			{
				notificationDesc.type = 6;
				notificationDesc.pSoundBank = ptr;
				FAudio.FACTAudioEngine_UnRegisterNotification(handle, ref notificationDesc);
				xactPtrs.Remove(ptr);
			}
		}
	}

	internal void UnregisterCue(IntPtr ptr)
	{
		lock (xactPtrs)
		{
			if (xactPtrs.ContainsKey(ptr))
			{
				notificationDesc.type = 4;
				notificationDesc.pCue = ptr;
				FAudio.FACTAudioEngine_UnRegisterNotification(handle, ref notificationDesc);
				xactPtrs.Remove(ptr);
			}
		}
	}

	[MonoPInvokeCallback(typeof(FAudio.FACTNotificationCallback))]
	private unsafe static void OnXACTNotification(IntPtr notification)
	{
		FAudio.FACTNotification* ptr = (FAudio.FACTNotification*)(void*)notification;
		WeakReference value;
		if (ptr->type == 7)
		{
			IntPtr pWaveBank = ptr->anon.waveBank.pWaveBank;
			lock (xactPtrs)
			{
				if (xactPtrs.TryGetValue(pWaveBank, out value) && value.IsAlive)
				{
					(value.Target as WaveBank).OnWaveBankDestroyed();
				}
				xactPtrs.Remove(pWaveBank);
				return;
			}
		}
		if (ptr->type == 6)
		{
			IntPtr pSoundBank = ptr->anon.soundBank.pSoundBank;
			lock (xactPtrs)
			{
				if (xactPtrs.TryGetValue(pSoundBank, out value) && value.IsAlive)
				{
					(value.Target as SoundBank).OnSoundBankDestroyed();
				}
				xactPtrs.Remove(pSoundBank);
				return;
			}
		}
		if (ptr->type != 4)
		{
			return;
		}
		IntPtr pCue = ptr->anon.cue.pCue;
		lock (xactPtrs)
		{
			if (xactPtrs.TryGetValue(pCue, out value) && value.IsAlive)
			{
				(value.Target as Cue).OnCueDestroyed();
			}
			xactPtrs.Remove(pCue);
		}
	}
}
