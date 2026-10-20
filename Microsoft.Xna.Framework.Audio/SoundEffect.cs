using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Audio;

public sealed class SoundEffect : IDisposable
{
	internal class FAudioContext
	{
		public static FAudioContext Context;

		public readonly IntPtr Handle;

		public readonly byte[] Handle3D;

		public readonly IntPtr MasterVoice;

		public readonly FAudio.FAudioDeviceDetails DeviceDetails;

		public float CurveDistanceScaler;

		public float DopplerScale;

		public float SpeedOfSound;

		public IntPtr ReverbVoice;

		private FAudio.FAudioVoiceSends reverbSends;

		private FAudioContext(IntPtr ctx, uint devices)
		{
			Handle = ctx;
			uint num;
			for (num = 0u; num < devices; num++)
			{
				FAudio.FAudio_GetDeviceDetails(Handle, num, out DeviceDetails);
				if ((DeviceDetails.Role & FAudio.FAudioDeviceRole.FAudioDefaultGameDevice) == FAudio.FAudioDeviceRole.FAudioDefaultGameDevice)
				{
					break;
				}
			}
			if (num == devices)
			{
				num = 0u;
				FAudio.FAudio_GetDeviceDetails(Handle, num, out DeviceDetails);
			}
			if (FAudio.FAudio_CreateMasteringVoice(Handle, out MasterVoice, 0u, 0u, 0u, num, IntPtr.Zero) != 0)
			{
				FAudio.FAudio_Release(ctx);
				Handle = IntPtr.Zero;
				FNALoggerEXT.LogError("Failed to create mastering voice!");
				return;
			}
			CurveDistanceScaler = 1f;
			DopplerScale = 1f;
			SpeedOfSound = 343.5f;
			Handle3D = new byte[20];
			FAudio.F3DAudioInitialize(DeviceDetails.OutputFormat.dwChannelMask, SpeedOfSound, Handle3D);
			Context = this;
		}

		public void Dispose()
		{
			if (ReverbVoice != IntPtr.Zero)
			{
				FAudio.FAudioVoice_DestroyVoice(ReverbVoice);
				ReverbVoice = IntPtr.Zero;
				Marshal.FreeHGlobal(reverbSends.pSends);
			}
			if (MasterVoice != IntPtr.Zero)
			{
				FAudio.FAudioVoice_DestroyVoice(MasterVoice);
			}
			if (Handle != IntPtr.Zero)
			{
				FAudio.FAudio_Release(Handle);
			}
			Context = null;
		}

		public unsafe void AttachReverb(IntPtr voice)
		{
			if (ReverbVoice == IntPtr.Zero)
			{
				FAudio.FAudioCreateReverb(out var ppApo, 0u);
				IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FAudio.FAudioEffectChain)));
				FAudio.FAudioEffectChain* ptr = (FAudio.FAudioEffectChain*)(void*)intPtr;
				ptr->EffectCount = 1u;
				ptr->pEffectDescriptors = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FAudio.FAudioEffectDescriptor)));
				FAudio.FAudioEffectDescriptor* ptr2 = (FAudio.FAudioEffectDescriptor*)(void*)ptr->pEffectDescriptors;
				ptr2->InitialState = 1;
				ptr2->OutputChannels = ((DeviceDetails.OutputFormat.Format.nChannels != 6) ? 1u : 6u);
				ptr2->pEffect = ppApo;
				FAudio.FAudio_CreateSubmixVoice(Handle, out ReverbVoice, 1u, DeviceDetails.OutputFormat.Format.nSamplesPerSec, 0u, 0u, IntPtr.Zero, intPtr);
				FAudio.FAPOBase_Release(ppApo);
				Marshal.FreeHGlobal(ptr->pEffectDescriptors);
				Marshal.FreeHGlobal(intPtr);
				IntPtr intPtr2 = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FAudio.FAudioFXReverbParameters)));
				FAudio.FAudioFXReverbParameters* ptr3 = (FAudio.FAudioFXReverbParameters*)(void*)intPtr2;
				ptr3->WetDryMix = 100f;
				ptr3->ReflectionsDelay = 7u;
				ptr3->ReverbDelay = 11;
				ptr3->RearDelay = 5;
				ptr3->PositionLeft = 6;
				ptr3->PositionRight = 6;
				ptr3->PositionMatrixLeft = 27;
				ptr3->PositionMatrixRight = 27;
				ptr3->EarlyDiffusion = 15;
				ptr3->LateDiffusion = 15;
				ptr3->LowEQGain = 8;
				ptr3->LowEQCutoff = 4;
				ptr3->HighEQGain = 8;
				ptr3->HighEQCutoff = 6;
				ptr3->RoomFilterFreq = 5000f;
				ptr3->RoomFilterMain = -10f;
				ptr3->RoomFilterHF = -1f;
				ptr3->ReflectionsGain = -26.02f;
				ptr3->ReverbGain = 10f;
				ptr3->DecayTime = 1.49f;
				ptr3->Density = 100f;
				ptr3->RoomSize = 100f;
				FAudio.FAudioVoice_SetEffectParameters(ReverbVoice, 0u, intPtr2, (uint)Marshal.SizeOf(typeof(FAudio.FAudioFXReverbParameters)), 0u);
				Marshal.FreeHGlobal(intPtr2);
				reverbSends = default;
				reverbSends.SendCount = 2u;
				reverbSends.pSends = Marshal.AllocHGlobal(2 * Marshal.SizeOf(typeof(FAudio.FAudioSendDescriptor)));
				FAudio.FAudioSendDescriptor* ptr4 = (FAudio.FAudioSendDescriptor*)(void*)reverbSends.pSends;
				ptr4->Flags = 0u;
				ptr4->pOutputVoice = MasterVoice;
				ptr4[1].Flags = 0u;
				ptr4[1].pOutputVoice = ReverbVoice;
			}
			FAudio.FAudioVoice_SetOutputVoices(voice, ref reverbSends);
		}

		public static void Create()
		{
			IntPtr ppFAudio;
			try
			{
				FAudio.FAudioCreate(out ppFAudio, 0u, uint.MaxValue);
			}
			catch (Exception ex)
			{
				FNALoggerEXT.LogWarn("FAudio failed to load: " + ex.ToString());
				return;
			}
			FAudio.FAudio_GetDeviceCount(ppFAudio, out var pCount);
			if (pCount == 0)
			{
				FAudio.FAudio_Release(ppFAudio);
				return;
			}
			FAudioContext fAudioContext = new FAudioContext(ppFAudio, pCount);
			if (fAudioContext.Handle == IntPtr.Zero)
			{
				fAudioContext.Dispose();
			}
			else
			{
				Context = fAudioContext;
			}
		}
	}

	internal List<WeakReference> Instances = new List<WeakReference>();

	internal FAudio.FAudioBuffer handle;

	internal IntPtr formatPtr;

	internal ushort channels;

	internal uint sampleRate;

	internal uint loopStart;

	internal uint loopLength;

	public TimeSpan Duration => TimeSpan.FromSeconds((double)handle.PlayLength / (double)sampleRate);

	public bool IsDisposed { get; private set; }

	public string Name { get; set; }

	public static float MasterVolume
	{
		get
		{
			FAudio.FAudioVoice_GetVolume(Device().MasterVoice, out var pVolume);
			return pVolume;
		}
		set
		{
			FAudio.FAudioVoice_SetVolume(Device().MasterVoice, value, 0u);
		}
	}

	public static float DistanceScale
	{
		get
		{
			return Device().CurveDistanceScaler;
		}
		set
		{
			if (value <= 0f)
			{
				throw new ArgumentOutOfRangeException("value <= 0.0f");
			}
			Device().CurveDistanceScaler = value;
		}
	}

	public static float DopplerScale
	{
		get
		{
			return Device().DopplerScale;
		}
		set
		{
			if (value < 0f)
			{
				throw new ArgumentOutOfRangeException("value < 0.0f");
			}
			Device().DopplerScale = value;
		}
	}

	public static float SpeedOfSound
	{
		get
		{
			return Device().SpeedOfSound;
		}
		set
		{
			FAudioContext fAudioContext = Device();
			fAudioContext.SpeedOfSound = value;
			FAudio.F3DAudioInitialize(fAudioContext.DeviceDetails.OutputFormat.dwChannelMask, fAudioContext.SpeedOfSound, fAudioContext.Handle3D);
		}
	}

	public SoundEffect(byte[] buffer, int sampleRate, AudioChannels channels)
		: this(null, buffer, 0, buffer.Length, null, 1, (ushort)channels, (uint)sampleRate, (uint)(sampleRate * ((ushort)channels * 2)), (ushort)((ushort)channels * 2), 16, 0, 0)
	{
	}

	public SoundEffect(byte[] buffer, int offset, int count, int sampleRate, AudioChannels channels, int loopStart, int loopLength)
		: this(null, buffer, offset, count, null, 1, (ushort)channels, (uint)sampleRate, (uint)(sampleRate * ((ushort)channels * 2)), (ushort)((ushort)channels * 2), 16, loopStart, loopLength)
	{
	}

	internal unsafe SoundEffect(string name, byte[] buffer, int offset, int count, byte[] extraData, ushort wFormatTag, ushort nChannels, uint nSamplesPerSec, uint nAvgBytesPerSec, ushort nBlockAlign, ushort wBitsPerSample, int loopStart, int loopLength)
	{
		Device();
		Name = name;
		channels = nChannels;
		sampleRate = nSamplesPerSec;
		this.loopStart = (uint)loopStart;
		this.loopLength = (uint)loopLength;
		if (extraData == null)
		{
			formatPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FAudio.FAudioWaveFormatEx)));
		}
		else
		{
			formatPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FAudio.FAudioWaveFormatEx)) + extraData.Length);
			Marshal.Copy(extraData, 0, formatPtr + Marshal.SizeOf(typeof(FAudio.FAudioWaveFormatEx)), extraData.Length);
		}
		FAudio.FAudioWaveFormatEx* ptr = (FAudio.FAudioWaveFormatEx*)(void*)formatPtr;
		ptr->wFormatTag = wFormatTag;
		ptr->nChannels = nChannels;
		ptr->nSamplesPerSec = nSamplesPerSec;
		ptr->nAvgBytesPerSec = nAvgBytesPerSec;
		ptr->nBlockAlign = nBlockAlign;
		ptr->wBitsPerSample = wBitsPerSample;
		ptr->cbSize = (ushort)(extraData?.Length ?? 0);
		handle = default;
		handle.Flags = 64u;
		handle.pContext = IntPtr.Zero;
		handle.AudioBytes = (uint)count;
		handle.pAudioData = Marshal.AllocHGlobal(count);
		Marshal.Copy(buffer, offset, handle.pAudioData, count);
		handle.PlayBegin = 0u;
		switch (wFormatTag)
		{
		case 1:
			handle.PlayLength = (uint)(count / nChannels / (wBitsPerSample / 8));
			break;
		case 2:
			handle.PlayLength = (uint)(count / nBlockAlign * ((nBlockAlign / nChannels - 6) * 2));
			break;
		case 358:
		{
			FAudio.FAudioXMA2WaveFormatEx* ptr2 = (FAudio.FAudioXMA2WaveFormatEx*)(void*)formatPtr;
			handle.PlayLength = ptr2->dwPlayLength;
			break;
		}
		}
		handle.LoopBegin = 0u;
		handle.LoopLength = 0u;
		handle.LoopCount = 0u;
	}

	~SoundEffect()
	{
		if (Instances.Count > 0)
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
		if (IsDisposed)
		{
			return;
		}
		WeakReference[] array = Instances.ToArray();
		WeakReference[] array2 = array;
		foreach (WeakReference weakReference in array2)
		{
			object target = weakReference.Target;
			if (target != null)
			{
				(target as IDisposable).Dispose();
			}
		}
		Instances.Clear();
		Marshal.FreeHGlobal(formatPtr);
		Marshal.FreeHGlobal(handle.pAudioData);
		IsDisposed = true;
	}

	public bool Play()
	{
		return Play(1f, 0f, 0f);
	}

	public bool Play(float volume, float pitch, float pan)
	{
		SoundEffectInstance soundEffectInstance = new SoundEffectInstance(this);
		soundEffectInstance.Volume = volume;
		soundEffectInstance.Pitch = pitch;
		soundEffectInstance.Pan = pan;
		soundEffectInstance.Play();
		if (soundEffectInstance.State != SoundState.Playing)
		{
			soundEffectInstance.Dispose();
			return false;
		}
		return true;
	}

	public SoundEffectInstance CreateInstance()
	{
		return new SoundEffectInstance(this);
	}

	public static TimeSpan GetSampleDuration(int sizeInBytes, int sampleRate, AudioChannels channels)
	{
		sizeInBytes /= 2;
		int milliseconds = (int)((float)(sizeInBytes / (int)channels) / ((float)sampleRate / 1000f));
		return new TimeSpan(0, 0, 0, 0, milliseconds);
	}

	public static int GetSampleSizeInBytes(TimeSpan duration, int sampleRate, AudioChannels channels)
	{
		return (int)(duration.TotalSeconds * (double)sampleRate * (double)channels * 2.0);
	}

	public static SoundEffect FromStream(Stream stream)
	{
		int num = 0;
		int num2 = 0;
		ushort wFormatTag;
		ushort nChannels;
		uint nSamplesPerSec;
		uint nAvgBytesPerSec;
		ushort nBlockAlign;
		ushort wBitsPerSample;
		byte[] array;
		using (BinaryReader binaryReader = new BinaryReader(stream))
		{
			string text = new string(binaryReader.ReadChars(4));
			if (text != "RIFF")
			{
				throw new NotSupportedException("Specified stream is not a wave file.");
			}
			binaryReader.ReadUInt32();
			string text2 = new string(binaryReader.ReadChars(4));
			if (text2 != "WAVE")
			{
				throw new NotSupportedException("Specified stream is not a wave file.");
			}
			string text3 = new string(binaryReader.ReadChars(4));
			while (text3 != "fmt ")
			{
				binaryReader.ReadBytes(binaryReader.ReadInt32());
				text3 = new string(binaryReader.ReadChars(4));
			}
			int num3 = binaryReader.ReadInt32();
			wFormatTag = binaryReader.ReadUInt16();
			nChannels = binaryReader.ReadUInt16();
			nSamplesPerSec = binaryReader.ReadUInt32();
			nAvgBytesPerSec = binaryReader.ReadUInt32();
			nBlockAlign = binaryReader.ReadUInt16();
			wBitsPerSample = binaryReader.ReadUInt16();
			if (num3 > 16)
			{
				binaryReader.ReadBytes(num3 - 16);
			}
			string text4 = new string(binaryReader.ReadChars(4));
			while (text4.ToLowerInvariant() != "data")
			{
				binaryReader.ReadBytes(binaryReader.ReadInt32());
				text4 = new string(binaryReader.ReadChars(4));
			}
			if (text4 != "data")
			{
				throw new NotSupportedException("Specified wave file is not supported.");
			}
			int count = binaryReader.ReadInt32();
			array = binaryReader.ReadBytes(count);
			while (binaryReader.PeekChar() != -1)
			{
				char[] array2 = binaryReader.ReadChars(4);
				if (array2.Length < 4)
				{
					break;
				}
				byte[] array3 = binaryReader.ReadBytes(4);
				if (array3.Length < 4)
				{
					break;
				}
				string text5 = new string(array2);
				int count2 = BitConverter.ToInt32(array3, 0);
				if (text5 == "smpl")
				{
					binaryReader.ReadUInt32();
					binaryReader.ReadUInt32();
					binaryReader.ReadUInt32();
					binaryReader.ReadUInt32();
					binaryReader.ReadUInt32();
					binaryReader.ReadUInt32();
					binaryReader.ReadUInt32();
					uint num4 = binaryReader.ReadUInt32();
					int num5 = binaryReader.ReadInt32();
					for (int i = 0; i < num4; i++)
					{
						binaryReader.ReadUInt32();
						binaryReader.ReadUInt32();
						int num6 = binaryReader.ReadInt32();
						int num7 = binaryReader.ReadInt32();
						binaryReader.ReadUInt32();
						binaryReader.ReadUInt32();
						if (i == 0)
						{
							num = num6;
							num2 = num7;
						}
					}
					if (num5 != 0)
					{
						binaryReader.ReadBytes(num5);
					}
				}
				else
				{
					binaryReader.ReadBytes(count2);
				}
			}
		}
		return new SoundEffect(null, array, 0, array.Length, null, wFormatTag, nChannels, nSamplesPerSec, nAvgBytesPerSec, nBlockAlign, wBitsPerSample, num, num2 - num);
	}

	internal static FAudioContext Device()
	{
		if (FAudioContext.Context != null)
		{
			return FAudioContext.Context;
		}
		FAudioContext.Create();
		if (FAudioContext.Context == null)
		{
			throw new NoAudioHardwareException();
		}
		return FAudioContext.Context;
	}
}
