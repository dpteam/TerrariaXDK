using System;
using System.Runtime.InteropServices;
using System.Text;

public static class FAudio
{
	[Flags]
	public enum FAudioDeviceRole
	{
		FAudioNotDefaultDevice = 0,
		FAudioDefaultConsoleDevice = 1,
		FAudioDefaultMultimediaDevice = 2,
		FAudioDefaultCommunicationsDevice = 4,
		FAudioDefaultGameDevice = 8,
		FAudioGlobalDefaultDevice = FAudioDefaultConsoleDevice | FAudioDefaultMultimediaDevice | FAudioDefaultCommunicationsDevice | FAudioDefaultGameDevice,
		FAudioInvalidDeviceRole = ~FAudioGlobalDefaultDevice
	}

	public enum FAudioFilterType
	{
		FAudioLowPassFilter,
		FAudioBandPassFilter,
		FAudioHighPassFilter,
		FAudioNotchFilter
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioGUID
	{
		public uint Data1;

		public ushort Data2;

		public ushort Data3;

		public unsafe fixed byte Data4[8];
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioWaveFormatEx
	{
		public ushort wFormatTag;

		public ushort nChannels;

		public uint nSamplesPerSec;

		public uint nAvgBytesPerSec;

		public ushort nBlockAlign;

		public ushort wBitsPerSample;

		public ushort cbSize;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioWaveFormatExtensible
	{
		public FAudioWaveFormatEx Format;

		public ushort Samples;

		public uint dwChannelMask;

		public FAudioGUID SubFormat;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioADPCMCoefSet
	{
		public short iCoef1;

		public short iCoef2;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioADPCMWaveFormat
	{
		public FAudioWaveFormatEx wfx;

		public ushort wSamplesPerBlock;

		public ushort wNumCoef;

		public IntPtr aCoef;
	}

	public struct FAudioXMA2WaveFormatEx
	{
		public FAudioWaveFormatEx wfx;

		public ushort wNumStreams;

		public uint dwChannelMask;

		public uint dwSamplesEncoded;

		public uint dwBytesPerBlock;

		public uint dwPlayBegin;

		public uint dwPlayLength;

		public uint dwLoopBegin;

		public uint dwLoopLength;

		public byte bLoopCount;

		public byte bEncoderVersion;

		public ushort wBlockCount;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioDeviceDetails
	{
		public unsafe fixed short DeviceID[256];

		public unsafe fixed short DisplayName[256];

		public FAudioDeviceRole Role;

		public FAudioWaveFormatExtensible OutputFormat;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioVoiceDetails
	{
		public uint CreationFlags;

		public uint ActiveFlags;

		public uint InputChannels;

		public uint InputSampleRate;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioSendDescriptor
	{
		public uint Flags;

		public IntPtr pOutputVoice;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioVoiceSends
	{
		public uint SendCount;

		public IntPtr pSends;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioEffectDescriptor
	{
		public IntPtr pEffect;

		public int InitialState;

		public uint OutputChannels;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioEffectChain
	{
		public uint EffectCount;

		public IntPtr pEffectDescriptors;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioFilterParameters
	{
		public FAudioFilterType Type;

		public float Frequency;

		public float OneOverQ;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioBuffer
	{
		public uint Flags;

		public uint AudioBytes;

		public IntPtr pAudioData;

		public uint PlayBegin;

		public uint PlayLength;

		public uint LoopBegin;

		public uint LoopLength;

		public uint LoopCount;

		public IntPtr pContext;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioBufferWMA
	{
		public IntPtr pDecodedPacketCumulativeBytes;

		public uint PacketCount;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioVoiceState
	{
		public IntPtr pCurrentBufferContext;

		public uint BuffersQueued;

		public ulong SamplesPlayed;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioPerformanceData
	{
		public ulong AudioCyclesSinceLastQuery;

		public ulong TotalCyclesSinceLastQuery;

		public uint MinimumCyclesPerQuantum;

		public uint MaximumCyclesPerQuantum;

		public uint MemoryUsageInBytes;

		public uint CurrentLatencyInSamples;

		public uint GlitchesSinceEngineStarted;

		public uint ActiveSourceVoiceCount;

		public uint TotalSourceVoiceCount;

		public uint ActiveSubmixVoiceCount;

		public uint ActiveResamplerCount;

		public uint ActiveMatrixMixCount;

		public uint ActiveXmaSourceVoices;

		public uint ActiveXmaStreams;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FAudioDebugConfiguration
	{
		public uint TraceMask;

		public uint BreakMask;

		public int LogThreadID;

		public int LogFileline;

		public int LogFunctionName;

		public int LogTiming;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void OnCriticalErrorFunc(IntPtr engineCallback, uint Error);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void OnProcessingPassEndFunc(IntPtr engineCallback);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void OnProcessingPassStartFunc(IntPtr engineCallback);

	public struct FAudioEngineCallback
	{
		public IntPtr OnCriticalError;

		public IntPtr OnProcessingPassEnd;

		public IntPtr OnProcessingPassStart;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void OnBufferEndFunc(IntPtr voiceCallback, IntPtr pBufferContext);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void OnBufferStartFunc(IntPtr voiceCallback, IntPtr pBufferContext);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void OnLoopEndFunc(IntPtr voiceCallback, IntPtr pBufferContext);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void OnStreamEndFunc(IntPtr voiceCallback);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void OnVoiceErrorFunc(IntPtr voiceCallback, IntPtr pBufferContext, uint Error);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void OnVoiceProcessingPassEndFunc(IntPtr voiceCallback);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void OnVoiceProcessingPassStartFunc(IntPtr voiceCallback, uint BytesRequired);

	public struct FAudioVoiceCallback
	{
		public IntPtr OnBufferEnd;

		public IntPtr OnBufferStart;

		public IntPtr OnLoopEnd;

		public IntPtr OnStreamEnd;

		public IntPtr OnVoiceError;

		public IntPtr OnVoiceProcessingPassEnd;

		public IntPtr OnVoiceProcessingPassStart;
	}

	public struct FAudioFXReverbParameters
	{
		public float WetDryMix;

		public uint ReflectionsDelay;

		public byte ReverbDelay;

		public byte RearDelay;

		public byte PositionLeft;

		public byte PositionRight;

		public byte PositionMatrixLeft;

		public byte PositionMatrixRight;

		public byte EarlyDiffusion;

		public byte LateDiffusion;

		public byte LowEQGain;

		public byte LowEQCutoff;

		public byte HighEQGain;

		public byte HighEQCutoff;

		public float RoomFilterFreq;

		public float RoomFilterMain;

		public float RoomFilterHF;

		public float ReflectionsGain;

		public float ReverbGain;

		public float DecayTime;

		public float Density;

		public float RoomSize;
	}

	public struct FAudioFXReverbParameters9
	{
		public float WetDryMix;

		public uint ReflectionsDelay;

		public byte ReverbDelay;

		public byte RearDelay;

		public byte SideDelay;

		public byte PositionLeft;

		public byte PositionRight;

		public byte PositionMatrixLeft;

		public byte PositionMatrixRight;

		public byte EarlyDiffusion;

		public byte LateDiffusion;

		public byte LowEQGain;

		public byte LowEQCutoff;

		public byte HighEQGain;

		public byte HighEQCutoff;

		public float RoomFilterFreq;

		public float RoomFilterMain;

		public float RoomFilterHF;

		public float ReflectionsGain;

		public float ReverbGain;

		public float DecayTime;

		public float Density;

		public float RoomSize;
	}

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate int FACTReadFileCallback(IntPtr hFile, IntPtr buffer, uint nNumberOfBytesToRead, IntPtr lpOverlapped);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate int FACTGetOverlappedResultCallback(IntPtr hFile, IntPtr lpOverlapped, out uint lpNumberOfBytesTransferred, int bWait);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	public delegate void FACTNotificationCallback(IntPtr pNotification);

	public enum FACTWaveBankSegIdx
	{
		FACT_WAVEBANK_SEGIDX_BANKDATA,
		FACT_WAVEBANK_SEGIDX_ENTRYMETADATA,
		FACT_WAVEBANK_SEGIDX_SEEKTABLES,
		FACT_WAVEBANK_SEGIDX_ENTRYNAMES,
		FACT_WAVEBANK_SEGIDX_ENTRYWAVEDATA,
		FACT_WAVEBANK_SEGIDX_COUNT
	}

	public struct FACTRendererDetails
	{
		public unsafe fixed short rendererID[255];

		public unsafe fixed short displayName[255];

		public int defaultDevice;
	}

	public struct FACTOverlapped
	{
		public IntPtr Internal;

		public IntPtr InternalHigh;

		public uint Offset;

		public uint OffsetHigh;

		public IntPtr hEvent;
	}

	public struct FACTFileIOCallbacks
	{
		public IntPtr readFileCallback;

		public IntPtr getOverlappedResultCallback;
	}

	public struct FACTRuntimeParameters
	{
		public uint lookAheadTime;

		public IntPtr pGlobalSettingsBuffer;

		public uint globalSettingsBufferSize;

		public uint globalSettingsFlags;

		public uint globalSettingsAllocAttributes;

		public FACTFileIOCallbacks fileIOCallbacks;

		public IntPtr fnNotificationCallback;

		public IntPtr pRendererID;

		public IntPtr pXAudio2;

		public IntPtr pMasteringVoice;
	}

	public struct FACTStreamingParameters
	{
		public IntPtr file;

		public uint offset;

		public uint flags;

		public ushort packetSize;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FACTWaveBankRegion
	{
		public uint dwOffset;

		public uint dwLength;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FACTWaveBankSampleRegion
	{
		public uint dwStartSample;

		public uint dwTotalSamples;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FACTWaveBankMiniWaveFormat
	{
		public uint dwValue;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FACTWaveBankEntry
	{
		public uint dwFlagsAndDuration;

		public FACTWaveBankMiniWaveFormat Format;

		public FACTWaveBankRegion PlayRegion;

		public FACTWaveBankSampleRegion LoopRegion;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FACTWaveBankData
	{
		public uint dwFlags;

		public uint dwEntryCount;

		public unsafe fixed char szBankName[64];

		public uint dwEntryMetaDataElementSize;

		public uint dwEntryNameElementSize;

		public uint dwAlignment;

		public FACTWaveBankMiniWaveFormat CompactFormat;

		public ulong BuildTime;
	}

	public struct FACTWaveProperties
	{
		public unsafe fixed byte friendlyName[64];

		public FACTWaveBankMiniWaveFormat format;

		public uint durationInSamples;

		public FACTWaveBankSampleRegion loopRegion;

		public int streaming;
	}

	public struct FACTWaveInstanceProperties
	{
		public FACTWaveProperties properties;

		public int backgroundMusic;
	}

	public struct FACTCueProperties
	{
		public unsafe fixed char friendlyName[255];

		public int interactive;

		public ushort iaVariableIndex;

		public ushort numVariations;

		public byte maxInstances;

		public byte currentInstances;
	}

	public struct FACTTrackProperties
	{
		public uint duration;

		public ushort numVariations;

		public byte numChannels;

		public ushort waveVariation;

		public byte loopCount;
	}

	public struct FACTVariationProperties
	{
		public ushort index;

		public byte weight;

		public float iaVariableMin;

		public float iaVariableMax;

		public int linger;
	}

	public struct FACTSoundProperties
	{
		public ushort category;

		public byte priority;

		public short pitch;

		public float volume;

		public ushort numTracks;

		public FACTTrackProperties arrTrackProperties;
	}

	public struct FACTSoundVariationProperties
	{
		public FACTVariationProperties variationProperties;

		public FACTSoundProperties soundProperties;
	}

	public struct FACTCueInstanceProperties
	{
		public uint allocAttributes;

		public FACTCueProperties cueProperties;

		public FACTSoundVariationProperties activeVariationProperties;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FACTNotificationDescription
	{
		public byte type;

		public byte flags;

		public IntPtr pSoundBank;

		public IntPtr pWaveBank;

		public IntPtr pCue;

		public IntPtr pWave;

		public ushort cueIndex;

		public ushort waveIndex;

		public IntPtr pvContext;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FACTNotificationCue
	{
		public ushort cueIndex;

		public IntPtr pSoundBank;

		public IntPtr pCue;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FACTNotificationMarker
	{
		public ushort cueIndex;

		public IntPtr pSoundBank;

		public IntPtr pCue;

		public uint marker;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FACTNotificationSoundBank
	{
		public IntPtr pSoundBank;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FACTNotificationWaveBank
	{
		public IntPtr pWaveBank;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FACTNotificationVariable
	{
		public ushort cueIndex;

		public IntPtr pSoundBank;

		public IntPtr pCue;

		public ushort variableIndex;

		public float variableValue;

		public int local;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FACTNotificationGUI
	{
		public uint reserved;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FACTNotificationWave
	{
		public IntPtr pWaveBank;

		public ushort waveIndex;

		public ushort cueIndex;

		public IntPtr pSoundBank;

		public IntPtr pCue;

		public IntPtr pWave;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct FACTNotification_union
	{
		[FieldOffset(0)]
		public FACTNotificationCue cue;

		[FieldOffset(0)]
		public FACTNotificationMarker marker;

		[FieldOffset(0)]
		public FACTNotificationSoundBank soundBank;

		[FieldOffset(0)]
		public FACTNotificationWaveBank waveBank;

		[FieldOffset(0)]
		public FACTNotificationVariable variable;

		[FieldOffset(0)]
		public FACTNotificationGUI gui;

		[FieldOffset(0)]
		public FACTNotificationWave wave;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FACTNotification
	{
		public byte type;

		public int timeStamp;

		public IntPtr pvContext;

		public FACTNotification_union anon;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct F3DAUDIO_VECTOR
	{
		public float x;

		public float y;

		public float z;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct F3DAUDIO_DISTANCE_CURVE_POINT
	{
		public float Distance;

		public float DSPSetting;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct F3DAUDIO_DISTANCE_CURVE
	{
		private IntPtr pPoints;

		public uint PointCount;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct F3DAUDIO_CONE
	{
		public float InnerAngle;

		public float OuterAngle;

		public float InnerVolume;

		public float OuterVolume;

		public float InnerLPF;

		public float OuterLPF;

		public float InnerReverb;

		public float OuterReverb;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct F3DAUDIO_LISTENER
	{
		public F3DAUDIO_VECTOR OrientFront;

		public F3DAUDIO_VECTOR OrientTop;

		public F3DAUDIO_VECTOR Position;

		public F3DAUDIO_VECTOR Velocity;

		public IntPtr pCone;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct F3DAUDIO_EMITTER
	{
		public IntPtr pCone;

		public F3DAUDIO_VECTOR OrientFront;

		public F3DAUDIO_VECTOR OrientTop;

		public F3DAUDIO_VECTOR Position;

		public F3DAUDIO_VECTOR Velocity;

		public float InnerRadius;

		public float InnerRadiusAngle;

		public uint ChannelCount;

		public float ChannelRadius;

		public IntPtr pChannelAzimuths;

		public IntPtr pVolumeCurve;

		public IntPtr pLFECurve;

		public IntPtr pLPFDirectCurve;

		public IntPtr pLPFReverbCurve;

		public IntPtr pReverbCurve;

		public float CurveDistanceScaler;

		public float DopplerScaler;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct F3DAUDIO_DSP_SETTINGS
	{
		public IntPtr pMatrixCoefficients;

		public IntPtr pDelayTimes;

		public uint SrcChannelCount;

		public uint DstChannelCount;

		public float LPFDirectCoefficient;

		public float LPFReverbCoefficient;

		public float ReverbLevel;

		public float DopplerFactor;

		public float EmitterToListenerAngle;

		public float EmitterToListenerDistance;

		public float EmitterVelocityComponent;

		public float ListenerVelocityComponent;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate IntPtr FAudio_readfunc(IntPtr data, IntPtr dst, IntPtr size, IntPtr count);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate long FAudio_seekfunc(IntPtr data, long offset, int whence);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int FAudio_closefunc(IntPtr data);

	public struct FAudioIOStream
	{
		public IntPtr data;

		public IntPtr read;

		public IntPtr seek;

		public IntPtr close;

		public IntPtr ioLock;
	}

	public struct stb_vorbis_alloc
	{
		public IntPtr alloc_buffer;

		public int alloc_buffer_length_in_bytes;
	}

	public struct stb_vorbis_info
	{
		public uint sample_rate;

		public int channels;

		public uint setup_memory_required;

		public uint setup_temp_memory_required;

		public uint temp_memory_required;

		public int max_frame_size;
	}

	public struct stb_vorbis_comment
	{
		public IntPtr vendor;

		public int comment_list_length;

		public IntPtr comment_list;
	}

	private const string nativeLibName = "FAudio";

	public const uint FAUDIO_TARGET_VERSION = 8u;

	public const uint FAUDIO_ABI_VERSION = 0u;

	public const uint FAUDIO_MAJOR_VERSION = 22u;

	public const uint FAUDIO_MINOR_VERSION = 3u;

	public const uint FAUDIO_PATCH_VERSION = 0u;

	public const uint FAUDIO_COMPILED_VERSION = 220300u;

	public const uint FAUDIO_DEFAULT_PROCESSOR = uint.MaxValue;

	public const uint FAUDIO_MAX_BUFFER_BYTES = 2147483648u;

	public const uint FAUDIO_MAX_QUEUED_BUFFERS = 64u;

	public const uint FAUDIO_MAX_AUDIO_CHANNELS = 64u;

	public const uint FAUDIO_MIN_SAMPLE_RATE = 1000u;

	public const uint FAUDIO_MAX_SAMPLE_RATE = 200000u;

	public const float FAUDIO_MAX_VOLUME_LEVEL = 16777216f;

	public const float FAUDIO_MIN_FREQ_RATIO = 1f / 1024f;

	public const float FAUDIO_MAX_FREQ_RATIO = 1024f;

	public const float FAUDIO_DEFAULT_FREQ_RATIO = 2f;

	public const float FAUDIO_MAX_FILTER_ONEOVERQ = 1.5f;

	public const float FAUDIO_MAX_FILTER_FREQUENCY = 1f;

	public const uint FAUDIO_MAX_LOOP_COUNT = 254u;

	public const uint FAUDIO_COMMIT_NOW = 0u;

	public const uint FAUDIO_COMMIT_ALL = 0u;

	public const uint FAUDIO_INVALID_OPSET = uint.MaxValue;

	public const uint FAUDIO_NO_LOOP_REGION = 0u;

	public const uint FAUDIO_LOOP_INFINITE = 255u;

	public const uint FAUDIO_DEFAULT_CHANNELS = 0u;

	public const uint FAUDIO_DEFAULT_SAMPLERATE = 0u;

	public const uint FAUDIO_DEBUG_ENGINE = 1u;

	public const uint FAUDIO_VOICE_NOPITCH = 2u;

	public const uint FAUDIO_VOICE_NOSRC = 4u;

	public const uint FAUDIO_VOICE_USEFILTER = 8u;

	public const uint FAUDIO_VOICE_MUSIC = 16u;

	public const uint FAUDIO_PLAY_TAILS = 32u;

	public const uint FAUDIO_END_OF_STREAM = 64u;

	public const uint FAUDIO_SEND_USEFILTER = 128u;

	public const uint FAUDIO_VOICE_NOSAMPLESPLAYED = 256u;

	public const uint FAUDIO_1024_QUANTUM = 32768u;

	public const FAudioFilterType FAUDIO_DEFAULT_FILTER_TYPE = FAudioFilterType.FAudioLowPassFilter;

	public const float FAUDIO_DEFAULT_FILTER_FREQUENCY = 1f;

	public const float FAUDIO_DEFAULT_FILTER_ONEOVERQ = 1f;

	public const ushort FAUDIO_LOG_ERRORS = 1;

	public const ushort FAUDIO_LOG_WARNINGS = 2;

	public const ushort FAUDIO_LOG_INFO = 4;

	public const ushort FAUDIO_LOG_DETAIL = 8;

	public const ushort FAUDIO_LOG_API_CALLS = 16;

	public const ushort FAUDIO_LOG_FUNC_CALLS = 32;

	public const ushort FAUDIO_LOG_TIMING = 64;

	public const ushort FAUDIO_LOG_LOCKS = 128;

	public const ushort FAUDIO_LOG_MEMORY = 256;

	public const ushort FAUDIO_LOG_STREAMING = 4096;

	public const float FAUDIOFX_REVERB_DEFAULT_WET_DRY_MIX = 100f;

	public const uint FAUDIOFX_REVERB_DEFAULT_REFLECTIONS_DELAY = 5u;

	public const byte FAUDIOFX_REVERB_DEFAULT_REVERB_DELAY = 5;

	public const byte FAUDIOFX_REVERB_DEFAULT_REAR_DELAY = 5;

	public const byte FAUDIOFX_REVERB_DEFAULT_7POINT1_SIDE_DELAY = 5;

	public const byte FAUDIOFX_REVERB_DEFAULT_7POINT1_REAR_DELAY = 20;

	public const byte FAUDIOFX_REVERB_DEFAULT_POSITION = 6;

	public const byte FAUDIOFX_REVERB_DEFAULT_POSITION_MATRIX = 27;

	public const byte FAUDIOFX_REVERB_DEFAULT_EARLY_DIFFUSION = 8;

	public const byte FAUDIOFX_REVERB_DEFAULT_LATE_DIFFUSION = 8;

	public const byte FAUDIOFX_REVERB_DEFAULT_LOW_EQ_GAIN = 8;

	public const byte FAUDIOFX_REVERB_DEFAULT_LOW_EQ_CUTOFF = 4;

	public const byte FAUDIOFX_REVERB_DEFAULT_HIGH_EQ_GAIN = 8;

	public const byte FAUDIOFX_REVERB_DEFAULT_HIGH_EQ_CUTOFF = 4;

	public const float FAUDIOFX_REVERB_DEFAULT_ROOM_FILTER_FREQ = 5000f;

	public const float FAUDIOFX_REVERB_DEFAULT_ROOM_FILTER_MAIN = 0f;

	public const float FAUDIOFX_REVERB_DEFAULT_ROOM_FILTER_HF = 0f;

	public const float FAUDIOFX_REVERB_DEFAULT_REFLECTIONS_GAIN = 0f;

	public const float FAUDIOFX_REVERB_DEFAULT_REVERB_GAIN = 0f;

	public const float FAUDIOFX_REVERB_DEFAULT_DECAY_TIME = 1f;

	public const float FAUDIOFX_REVERB_DEFAULT_DENSITY = 100f;

	public const float FAUDIOFX_REVERB_DEFAULT_ROOM_SIZE = 100f;

	public const int FACT_CONTENT_VERSION = 46;

	public const uint FACT_FLAG_MANAGEDATA = 1u;

	public const uint FACT_FLAG_STOP_RELEASE = 0u;

	public const uint FACT_FLAG_STOP_IMMEDIATE = 1u;

	public const uint FACT_FLAG_BACKGROUND_MUSIC = 2u;

	public const uint FACT_FLAG_UNITS_MS = 4u;

	public const uint FACT_FLAG_UNITS_SAMPLES = 8u;

	public const uint FACT_STATE_CREATED = 1u;

	public const uint FACT_STATE_PREPARING = 2u;

	public const uint FACT_STATE_PREPARED = 4u;

	public const uint FACT_STATE_PLAYING = 8u;

	public const uint FACT_STATE_STOPPING = 16u;

	public const uint FACT_STATE_STOPPED = 32u;

	public const uint FACT_STATE_PAUSED = 64u;

	public const uint FACT_STATE_INUSE = 128u;

	public const uint FACT_STATE_PREPAREFAILED = 2147483648u;

	public const short FACTPITCH_MIN = -1200;

	public const short FACTPITCH_MAX = 1200;

	public const short FACTPITCH_MIN_TOTAL = -2400;

	public const short FACTPITCH_MAX_TOTAL = 2400;

	public const float FACTVOLUME_MIN = 0f;

	public const float FACTVOLUME_MAX = 16777216f;

	public const ushort FACTINDEX_INVALID = ushort.MaxValue;

	public const ushort FACTVARIABLEINDEX_INVALID = ushort.MaxValue;

	public const ushort FACTCATEGORY_INVALID = ushort.MaxValue;

	public const uint FACT_ENGINE_LOOKAHEAD_DEFAULT = 250u;

	public const byte FACTNOTIFICATIONTYPE_CUEPREPARED = 1;

	public const byte FACTNOTIFICATIONTYPE_CUEPLAY = 2;

	public const byte FACTNOTIFICATIONTYPE_CUESTOP = 3;

	public const byte FACTNOTIFICATIONTYPE_CUEDESTROYED = 4;

	public const byte FACTNOTIFICATIONTYPE_MARKER = 5;

	public const byte FACTNOTIFICATIONTYPE_SOUNDBANKDESTROYED = 6;

	public const byte FACTNOTIFICATIONTYPE_WAVEBANKDESTROYED = 7;

	public const byte FACTNOTIFICATIONTYPE_LOCALVARIABLECHANGED = 8;

	public const byte FACTNOTIFICATIONTYPE_GLOBALVARIABLECHANGED = 9;

	public const byte FACTNOTIFICATIONTYPE_GUICONNECTED = 10;

	public const byte FACTNOTIFICATIONTYPE_GUIDISCONNECTED = 11;

	public const byte FACTNOTIFICATIONTYPE_WAVEPREPARED = 12;

	public const byte FACTNOTIFICATIONTYPE_WAVEPLAY = 13;

	public const byte FACTNOTIFICATIONTYPE_WAVESTOP = 14;

	public const byte FACTNOTIFICATIONTYPE_WAVELOOPED = 15;

	public const byte FACTNOTIFICATIONTYPE_WAVEDESTROYED = 16;

	public const byte FACTNOTIFICATIONTYPE_WAVEBANKPREPARED = 17;

	public const byte FACTNOTIFICATIONTYPE_WAVEBANKSTREAMING_INVALIDCONTENT = 18;

	public const byte FACT_FLAG_NOTIFICATION_PERSIST = 1;

	public const uint FACT_WAVEBANK_TYPE_BUFFER = 0u;

	public const uint FACT_WAVEBANK_TYPE_STREAMING = 1u;

	public const uint FACT_WAVEBANK_TYPE_MASK = 1u;

	public const uint FACT_WAVEBANK_FLAGS_ENTRYNAMES = 65536u;

	public const uint FACT_WAVEBANK_FLAGS_COMPACT = 131072u;

	public const uint FACT_WAVEBANK_FLAGS_SYNC_DISABLED = 262144u;

	public const uint FACT_WAVEBANK_FLAGS_SEEKTABLES = 524288u;

	public const uint FACT_WAVEBANK_FLAGS_MASK = 983040u;

	public const uint SPEAKER_FRONT_LEFT = 1u;

	public const uint SPEAKER_FRONT_RIGHT = 2u;

	public const uint SPEAKER_FRONT_CENTER = 4u;

	public const uint SPEAKER_LOW_FREQUENCY = 8u;

	public const uint SPEAKER_BACK_LEFT = 16u;

	public const uint SPEAKER_BACK_RIGHT = 32u;

	public const uint SPEAKER_FRONT_LEFT_OF_CENTER = 64u;

	public const uint SPEAKER_FRONT_RIGHT_OF_CENTER = 128u;

	public const uint SPEAKER_BACK_CENTER = 256u;

	public const uint SPEAKER_SIDE_LEFT = 512u;

	public const uint SPEAKER_SIDE_RIGHT = 1024u;

	public const uint SPEAKER_TOP_CENTER = 2048u;

	public const uint SPEAKER_TOP_FRONT_LEFT = 4096u;

	public const uint SPEAKER_TOP_FRONT_CENTER = 8192u;

	public const uint SPEAKER_TOP_FRONT_RIGHT = 16384u;

	public const uint SPEAKER_TOP_BACK_LEFT = 32768u;

	public const uint SPEAKER_TOP_BACK_CENTER = 65536u;

	public const uint SPEAKER_TOP_BACK_RIGHT = 131072u;

	public const uint SPEAKER_MONO = 4u;

	public const uint SPEAKER_STEREO = 3u;

	public const uint SPEAKER_2POINT1 = 11u;

	public const uint SPEAKER_SURROUND = 263u;

	public const uint SPEAKER_QUAD = 51u;

	public const uint SPEAKER_4POINT1 = 59u;

	public const uint SPEAKER_5POINT1 = 63u;

	public const uint SPEAKER_7POINT1 = 255u;

	public const uint SPEAKER_5POINT1_SURROUND = 1551u;

	public const uint SPEAKER_7POINT1_SURROUND = 1599u;

	public const uint SPEAKER_XBOX = 63u;

	public const float F3DAUDIO_PI = (float)Math.PI;

	public const float F3DAUDIO_2PI = (float)Math.PI * 2f;

	public const uint F3DAUDIO_CALCULATE_MATRIX = 1u;

	public const uint F3DAUDIO_CALCULATE_DELAY = 2u;

	public const uint F3DAUDIO_CALCULATE_LPF_DIRECT = 4u;

	public const uint F3DAUDIO_CALCULATE_LPF_REVERB = 8u;

	public const uint F3DAUDIO_CALCULATE_REVERB = 16u;

	public const uint F3DAUDIO_CALCULATE_DOPPLER = 32u;

	public const uint F3DAUDIO_CALCULATE_EMITTER_ANGLE = 64u;

	public const uint F3DAUDIO_CALCULATE_ZEROCENTER = 65536u;

	public const uint F3DAUDIO_CALCULATE_REDIRECT_TO_LFE = 131072u;

	public const int F3DAUDIO_HANDLE_BYTESIZE = 20;

	public const float LEFT_AZIMUTH = 4.712389f;

	public const float RIGHT_AZIMUTH = (float)Math.PI / 2f;

	public const float FRONT_LEFT_AZIMUTH = 5.4977875f;

	public const float FRONT_RIGHT_AZIMUTH = (float)Math.PI / 4f;

	public const float FRONT_CENTER_AZIMUTH = 0f;

	public const float LOW_FREQUENCY_AZIMUTH = (float)Math.PI * 2f;

	public const float BACK_LEFT_AZIMUTH = 3.926991f;

	public const float BACK_RIGHT_AZIMUTH = (float)Math.PI * 3f / 4f;

	public const float BACK_CENTER_AZIMUTH = (float)Math.PI;

	public const float FRONT_LEFT_OF_CENTER_AZIMUTH = 5.8904862f;

	public const float FRONT_RIGHT_OF_CENTER_AZIMUTH = (float)Math.PI / 8f;

	public static readonly float[] aStereoLayout = new float[2]
	{
		4.712389f,
		(float)Math.PI / 2f
	};

	public static readonly float[] a2Point1Layout = new float[3]
	{
		4.712389f,
		(float)Math.PI / 2f,
		(float)Math.PI * 2f
	};

	public static readonly float[] aQuadLayout = new float[4]
	{
		5.4977875f,
		(float)Math.PI / 4f,
		3.926991f,
		(float)Math.PI * 3f / 4f
	};

	public static readonly float[] a4Point1Layout = new float[5]
	{
		5.4977875f,
		(float)Math.PI / 4f,
		(float)Math.PI * 2f,
		3.926991f,
		(float)Math.PI * 3f / 4f
	};

	public static readonly float[] a5Point1Layout = new float[6]
	{
		5.4977875f,
		(float)Math.PI / 4f,
		0f,
		(float)Math.PI * 2f,
		3.926991f,
		(float)Math.PI * 3f / 4f
	};

	public static readonly float[] a7Point1Layout = new float[8]
	{
		5.4977875f,
		(float)Math.PI / 4f,
		0f,
		(float)Math.PI * 2f,
		3.926991f,
		(float)Math.PI * 3f / 4f,
		4.712389f,
		(float)Math.PI / 2f
	};

	private static int Utf8Size(string str)
	{
		return str.Length * 4 + 1;
	}

	private unsafe static byte* Utf8Encode(string str, byte* buffer, int bufferSize)
	{
		fixed (char* chars = str)
		{
			Encoding.UTF8.GetBytes(chars, str.Length + 1, buffer, bufferSize);
		}
		return buffer;
	}

	private unsafe static byte* Utf8Encode(string str)
	{
		int num = str.Length * 4 + 1;
		byte* ptr = (byte*)(void*)Marshal.AllocHGlobal(num);
		fixed (char* chars = str)
		{
			Encoding.UTF8.GetBytes(chars, str.Length + 1, ptr, num);
		}
		return ptr;
	}

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioLinkedVersion();

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioCreate(out IntPtr ppFAudio, uint Flags, uint XAudio2Processor);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudio_AddRef(IntPtr audio);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudio_Release(IntPtr audio);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudio_GetDeviceCount(IntPtr audio, out uint pCount);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudio_GetDeviceDetails(IntPtr audio, uint Index, out FAudioDeviceDetails pDeviceDetails);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudio_Initialize(IntPtr audio, uint Flags, uint XAudio2Processor);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudio_RegisterForCallbacks(IntPtr audio, IntPtr pCallback);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudio_UnregisterForCallbacks(IntPtr audio, IntPtr pCallback);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudio_CreateSourceVoice(IntPtr audio, out IntPtr ppSourceVoice, ref FAudioWaveFormatEx pSourceFormat, uint Flags, float MaxFrequencyRatio, IntPtr pCallback, IntPtr pSendList, IntPtr pEffectChain);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudio_CreateSourceVoice(IntPtr audio, out IntPtr ppSourceVoice, IntPtr pSourceFormat, uint Flags, float MaxFrequencyRatio, IntPtr pCallback, IntPtr pSendList, IntPtr pEffectChain);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudio_CreateSubmixVoice(IntPtr audio, out IntPtr ppSubmixVoice, uint InputChannels, uint InputSampleRate, uint Flags, uint ProcessingStage, IntPtr pSendList, IntPtr pEffectChain);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudio_CreateMasteringVoice(IntPtr audio, out IntPtr ppMasteringVoice, uint InputChannels, uint InputSampleRate, uint Flags, uint DeviceIndex, IntPtr pEffectChain);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudio_StartEngine(IntPtr audio);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudio_StopEngine(IntPtr audio);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl, EntryPoint = "FAudio_CommitOperationSet")]
	public static extern uint FAudio_CommitChanges(IntPtr audio, uint OperationSet);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudio_GetPerformanceData(IntPtr audio, out FAudioPerformanceData pPerfData);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudio_SetDebugConfiguration(IntPtr audio, ref FAudioDebugConfiguration pDebugConfiguration, IntPtr pReserved);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudio_GetProcessingQuantum(IntPtr audio, out uint quantumNumerator, out uint quantumDenominator);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudioVoice_GetVoiceDetails(IntPtr voice, out FAudioVoiceDetails pVoiceDetails);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioVoice_SetOutputVoices(IntPtr voice, ref FAudioVoiceSends pSendList);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioVoice_SetEffectChain(IntPtr voice, ref FAudioEffectChain pEffectChain);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioVoice_EnableEffect(IntPtr voice, uint EffectIndex, uint OperationSet);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioVoice_DisableEffect(IntPtr voice, uint EffectIndex, uint OperationSet);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudioVoice_GetEffectState(IntPtr voice, uint EffectIndex, out int pEnabled);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioVoice_SetEffectParameters(IntPtr voice, uint EffectIndex, IntPtr pParameters, uint ParametersByteSize, uint OperationSet);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioVoice_GetEffectParameters(IntPtr voice, uint EffectIndex, IntPtr pParameters, uint ParametersByteSize);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioVoice_SetFilterParameters(IntPtr voice, ref FAudioFilterParameters pParameters, uint OperationSet);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudioVoice_GetFilterParameters(IntPtr voice, out FAudioFilterParameters pParameters);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioVoice_SetOutputFilterParameters(IntPtr voice, IntPtr pDestinationVoice, ref FAudioFilterParameters pParameters, uint OperationSet);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudioVoice_GetOutputFilterParameters(IntPtr voice, IntPtr pDestinationVoice, out FAudioFilterParameters pParameters);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioVoice_SetVolume(IntPtr voice, float Volume, uint OperationSet);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudioVoice_GetVolume(IntPtr voice, out float pVolume);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioVoice_SetChannelVolumes(IntPtr voice, uint Channels, float[] pVolumes, uint OperationSet);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudioVoice_GetChannelVolumes(IntPtr voice, uint Channels, float[] pVolumes);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioVoice_SetOutputMatrix(IntPtr voice, IntPtr pDestinationVoice, uint SourceChannels, uint DestinationChannels, IntPtr pLevelMatrix, uint OperationSet);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudioVoice_GetOutputMatrix(IntPtr voice, IntPtr pDestinationVoice, uint SourceChannels, uint DestinationChannels, float[] pLevelMatrix);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudioVoice_DestroyVoice(IntPtr voice);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioSourceVoice_Start(IntPtr voice, uint Flags, uint OperationSet);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioSourceVoice_Stop(IntPtr voice, uint Flags, uint OperationSet);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioSourceVoice_SubmitSourceBuffer(IntPtr voice, ref FAudioBuffer pBuffer, IntPtr pBufferWMA);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioSourceVoice_SubmitSourceBuffer(IntPtr voice, ref FAudioBuffer pBuffer, ref FAudioBufferWMA pBufferWMA);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioSourceVoice_FlushSourceBuffers(IntPtr voice);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioSourceVoice_Discontinuity(IntPtr voice);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioSourceVoice_ExitLoop(IntPtr voice, uint OperationSet);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudioSourceVoice_GetState(IntPtr voice, out FAudioVoiceState pVoiceState, uint Flags);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioSourceVoice_SetFrequencyRatio(IntPtr voice, float Ratio, uint OperationSet);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudioSourceVoice_GetFrequencyRatio(IntPtr voice, out float pRatio);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioSourceVoice_SetSourceSampleRate(IntPtr voice, uint NewSourceSampleRate);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioCreateReverb(out IntPtr ppApo, uint Flags);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAudioCreateReverb9(out IntPtr ppApo, uint Flags);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FAPOBase_Release(IntPtr fapo);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTCreateEngine(uint dwCreationFlags, out IntPtr ppEngine);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_AddRef(IntPtr pEngine);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_Release(IntPtr pEngine);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_GetRendererCount(IntPtr pEngine, out ushort pnRendererCount);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_GetRendererDetails(IntPtr pEngine, ushort nRendererIndex, out FACTRendererDetails pRendererDetails);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_GetFinalMixFormat(IntPtr pEngine, out FAudioWaveFormatExtensible pFinalMixFormat);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_Initialize(IntPtr pEngine, ref FACTRuntimeParameters pParams);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_ShutDown(IntPtr pEngine);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_DoWork(IntPtr pEngine);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_CreateSoundBank(IntPtr pEngine, IntPtr pvBuffer, uint dwSize, uint dwFlags, uint dwAllocAttributes, out IntPtr ppSoundBank);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_CreateInMemoryWaveBank(IntPtr pEngine, IntPtr pvBuffer, uint dwSize, uint dwFlags, uint dwAllocAttributes, out IntPtr ppWaveBank);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_CreateStreamingWaveBank(IntPtr pEngine, ref FACTStreamingParameters pParms, out IntPtr ppWaveBank);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern uint FACTAudioEngine_PrepareWave(IntPtr pEngine, uint dwFlags, byte* szWavePath, uint wStreamingPacketSize, uint dwAlignment, uint dwPlayOffset, byte nLoopCount, out IntPtr ppWave);

	public unsafe static uint FACTAudioEngine_PrepareWave(IntPtr pEngine, uint dwFlags, string szWavePath, uint wStreamingPacketSize, uint dwAlignment, uint dwPlayOffset, byte nLoopCount, out IntPtr ppWave)
	{
		byte* ptr = Utf8Encode(szWavePath);
		uint result = FACTAudioEngine_PrepareWave(pEngine, dwFlags, ptr, wStreamingPacketSize, dwAlignment, dwPlayOffset, nLoopCount, out ppWave);
		Marshal.FreeHGlobal((IntPtr)ptr);
		return result;
	}

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_PrepareInMemoryWave(IntPtr pEngine, uint dwFlags, FACTWaveBankEntry entry, uint[] pdwSeekTable, byte[] pbWaveData, uint dwPlayOffset, byte nLoopCount, out IntPtr ppWave);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_PrepareStreamingWave(IntPtr pEngine, uint dwFlags, FACTWaveBankEntry entry, FACTStreamingParameters streamingParams, uint dwAlignment, uint[] pdwSeekTable, byte[] pbWaveData, uint dwPlayOffset, byte nLoopCount, out IntPtr ppWave);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_RegisterNotification(IntPtr pEngine, ref FACTNotificationDescription pNotificationDescription);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_UnRegisterNotification(IntPtr pEngine, ref FACTNotificationDescription pNotificationDescription);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern ushort FACTAudioEngine_GetCategory(IntPtr pEngine, byte* szFriendlyName);

	public unsafe static ushort FACTAudioEngine_GetCategory(IntPtr pEngine, string szFriendlyName)
	{
		int num = Utf8Size(szFriendlyName);
		byte* buffer = stackalloc byte[(int)(uint)num];
		return FACTAudioEngine_GetCategory(pEngine, Utf8Encode(szFriendlyName, buffer, num));
	}

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_Stop(IntPtr pEngine, ushort nCategory, uint dwFlags);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_SetVolume(IntPtr pEngine, ushort nCategory, float volume);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_Pause(IntPtr pEngine, ushort nCategory, int fPause);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern ushort FACTAudioEngine_GetGlobalVariableIndex(IntPtr pEngine, byte* szFriendlyName);

	public unsafe static ushort FACTAudioEngine_GetGlobalVariableIndex(IntPtr pEngine, string szFriendlyName)
	{
		int num = Utf8Size(szFriendlyName);
		byte* buffer = stackalloc byte[(int)(uint)num];
		return FACTAudioEngine_GetGlobalVariableIndex(pEngine, Utf8Encode(szFriendlyName, buffer, num));
	}

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_SetGlobalVariable(IntPtr pEngine, ushort nIndex, float nValue);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTAudioEngine_GetGlobalVariable(IntPtr pEngine, ushort nIndex, out float pnValue);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern ushort FACTSoundBank_GetCueIndex(IntPtr pSoundBank, byte* szFriendlyName);

	public unsafe static ushort FACTSoundBank_GetCueIndex(IntPtr pSoundBank, string szFriendlyName)
	{
		int num = Utf8Size(szFriendlyName);
		byte* buffer = stackalloc byte[(int)(uint)num];
		return FACTSoundBank_GetCueIndex(pSoundBank, Utf8Encode(szFriendlyName, buffer, num));
	}

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTSoundBank_GetNumCues(IntPtr pSoundBank, out ushort pnNumCues);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTSoundBank_GetCueProperties(IntPtr pSoundBank, ushort nCueIndex, out FACTCueProperties pProperties);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTSoundBank_Prepare(IntPtr pSoundBank, ushort nCueIndex, uint dwFlags, int timeOffset, out IntPtr ppCue);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTSoundBank_Play(IntPtr pSoundBank, ushort nCueIndex, uint dwFlags, int timeOffset, out IntPtr ppCue);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTSoundBank_Play(IntPtr pSoundBank, ushort nCueIndex, uint dwFlags, int timeOffset, IntPtr ppCue);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTSoundBank_Play3D(IntPtr pSoundBank, ushort nCueIndex, uint dwFlags, int timeOffset, ref F3DAUDIO_DSP_SETTINGS pDSPSettings, IntPtr ppCue);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTSoundBank_Stop(IntPtr pSoundBank, ushort nCueIndex, uint dwFlags);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTSoundBank_Destroy(IntPtr pSoundBank);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTSoundBank_GetState(IntPtr pSoundBank, out uint pdwState);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWaveBank_Destroy(IntPtr pWaveBank);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWaveBank_GetState(IntPtr pWaveBank, out uint pdwState);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWaveBank_GetNumWaves(IntPtr pWaveBank, out ushort pnNumWaves);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern ushort FACTWaveBank_GetWaveIndex(IntPtr pWaveBank, byte* szFriendlyName);

	public unsafe static ushort FACTWaveBank_GetWaveIndex(IntPtr pWaveBank, string szFriendlyName)
	{
		int num = Utf8Size(szFriendlyName);
		byte* buffer = stackalloc byte[(int)(uint)num];
		return FACTWaveBank_GetWaveIndex(pWaveBank, Utf8Encode(szFriendlyName, buffer, num));
	}

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWaveBank_GetWaveProperties(IntPtr pWaveBank, ushort nWaveIndex, out FACTWaveProperties pWaveProperties);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWaveBank_Prepare(IntPtr pWaveBank, ushort nWaveIndex, uint dwFlags, uint dwPlayOffset, byte nLoopCount, out IntPtr ppWave);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWaveBank_Play(IntPtr pWaveBank, ushort nWaveIndex, uint dwFlags, uint dwPlayOffset, byte nLoopCount, out IntPtr ppWave);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWaveBank_Stop(IntPtr pWaveBank, ushort nWaveIndex, uint dwFlags);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWave_Destroy(IntPtr pWave);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWave_Play(IntPtr pWave);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWave_Stop(IntPtr pWave, uint dwFlags);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWave_Pause(IntPtr pWave, int fPause);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWave_GetState(IntPtr pWave, out uint pdwState);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWave_SetPitch(IntPtr pWave, short pitch);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWave_SetVolume(IntPtr pWave, float volume);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWave_SetMatrixCoefficients(IntPtr pWave, uint uSrcChannelCount, uint uDstChannelCount, float[] pMatrixCoefficients);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTWave_GetProperties(IntPtr pWave, out FACTWaveInstanceProperties pProperties);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTCue_Destroy(IntPtr pCue);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTCue_Play(IntPtr pCue);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTCue_Stop(IntPtr pCue, uint dwFlags);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTCue_GetState(IntPtr pCue, out uint pdwState);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTCue_SetMatrixCoefficients(IntPtr pCue, uint uSrcChannelCount, uint uDstChannelCount, float[] pMatrixCoefficients);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern ushort FACTCue_GetVariableIndex(IntPtr pCue, byte* szFriendlyName);

	public unsafe static ushort FACTCue_GetVariableIndex(IntPtr pCue, string szFriendlyName)
	{
		int num = Utf8Size(szFriendlyName);
		byte* buffer = stackalloc byte[(int)(uint)num];
		return FACTCue_GetVariableIndex(pCue, Utf8Encode(szFriendlyName, buffer, num));
	}

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTCue_SetVariable(IntPtr pCue, ushort nIndex, float nValue);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTCue_GetVariable(IntPtr pCue, ushort nIndex, out float nValue);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTCue_Pause(IntPtr pCue, int fPause);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTCue_GetProperties(IntPtr pCue, out IntPtr ppProperties);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTCue_SetOutputVoices(IntPtr pCue, IntPtr pSendList);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACTCue_SetOutputVoiceMatrix(IntPtr pCue, IntPtr pDestinationVoice, uint SourceChannels, uint DestinationChannels, float[] pLevelMatrix);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void F3DAudioInitialize(uint SpeakerChannelMask, float SpeedOfSound, byte[] Instance);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint F3DAudioInitialize8(uint SpeakerChannelMask, float SpeedOfSound, byte[] Instance);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void F3DAudioCalculate(byte[] Instance, ref F3DAUDIO_LISTENER pListener, ref F3DAUDIO_EMITTER pEmitter, uint Flags, ref F3DAUDIO_DSP_SETTINGS pDSPSettings);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACT3DInitialize(IntPtr pEngine, byte[] D3FInstance);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACT3DCalculate(byte[] F3DInstance, ref F3DAUDIO_LISTENER pListener, ref F3DAUDIO_EMITTER pEmitter, ref F3DAUDIO_DSP_SETTINGS pDSPSettings);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint FACT3DApply(ref F3DAUDIO_DSP_SETTINGS pDSPSettings, IntPtr pCue);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void XNA_SongInit();

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void XNA_SongQuit();

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern float XNA_PlaySong(byte* name);

	public unsafe static float XNA_PlaySong(string name)
	{
		int num = Utf8Size(name);
		byte* buffer = stackalloc byte[(int)(uint)num];
		return XNA_PlaySong(Utf8Encode(name, buffer, num));
	}

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void XNA_PauseSong();

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void XNA_ResumeSong();

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void XNA_StopSong();

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void XNA_SetSongVolume(float volume);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint XNA_GetSongEnded();

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void XNA_EnableVisualization(uint enable);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint XNA_VisualizationEnabled();

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void XNA_GetSongVisualizationData(float[] frequencies, float[] samples, uint count);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern IntPtr FAudio_fopen(byte* path);

	public unsafe static IntPtr FAudio_fopen(string path)
	{
		int num = Utf8Size(path);
		byte* buffer = stackalloc byte[(int)(uint)num];
		return FAudio_fopen(Utf8Encode(path, buffer, num));
	}

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr FAudio_memopen(IntPtr mem, int len);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr FAudio_memptr(IntPtr io, IntPtr offset);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void FAudio_close(IntPtr io);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern stb_vorbis_info stb_vorbis_get_info(IntPtr f);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern stb_vorbis_comment stb_vorbis_get_comment(IntPtr f);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern int stb_vorbis_get_error(IntPtr f);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern void stb_vorbis_close(IntPtr f);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern int stb_vorbis_get_sample_offset(IntPtr f);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint stb_vorbis_get_file_offset(IntPtr f);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr stb_vorbis_open_memory(IntPtr data, int len, out int error, IntPtr alloc_buffer);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	private unsafe static extern IntPtr stb_vorbis_open_filename(byte* filename, out int error, IntPtr alloc_buffer);

	public unsafe static IntPtr stb_vorbis_open_filename(string filename, out int error, IntPtr alloc_buffer)
	{
		int num = Utf8Size(filename);
		byte* buffer = stackalloc byte[(int)(uint)num];
		return stb_vorbis_open_filename(Utf8Encode(filename, buffer, num), out error, alloc_buffer);
	}

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr stb_vorbis_open_file(IntPtr f, int close_handle_on_close, out int error, IntPtr alloc_buffer);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr stb_vorbis_open_file_section(IntPtr f, int close_handle_on_close, out int error, IntPtr alloc_buffer, uint len);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern int stb_vorbis_seek_frame(IntPtr f, uint sample_number);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern int stb_vorbis_seek(IntPtr f, uint sample_number);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern int stb_vorbis_seek_start(IntPtr f);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern uint stb_vorbis_stream_length_in_samples(IntPtr f);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern float stb_vorbis_stream_length_in_seconds(IntPtr f);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern int stb_vorbis_get_frame_float(IntPtr f, out int channels, ref float[][] output);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern int stb_vorbis_get_frame_float(IntPtr f, IntPtr channels, ref float[][] output);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern int stb_vorbis_get_samples_float_interleaved(IntPtr f, int channels, float[] buffer, int num_floats);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern int stb_vorbis_get_samples_float_interleaved(IntPtr f, int channels, IntPtr buffer, int num_floats);

	[DllImport("FAudio", CallingConvention = CallingConvention.Cdecl)]
	public static extern int stb_vorbis_get_samples_float(IntPtr f, int channels, float[][] buffer, int num_samples);
}
