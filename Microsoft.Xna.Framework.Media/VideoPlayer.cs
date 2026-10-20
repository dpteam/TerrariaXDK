using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Media;

public sealed class VideoPlayer : IDisposable
{
	private Effect shaderProgram;

	private IntPtr stateChangesPtr;

	private Texture2D[] yuvTextures = new Texture2D[3];

	private Viewport viewport;

	private static VertexPositionTexture[] vertices = new VertexPositionTexture[4]
	{
		new VertexPositionTexture(new Vector3(-1f, -1f, 0f), new Vector2(0f, 1f)),
		new VertexPositionTexture(new Vector3(1f, -1f, 0f), new Vector2(1f, 1f)),
		new VertexPositionTexture(new Vector3(-1f, 1f, 0f), new Vector2(0f, 0f)),
		new VertexPositionTexture(new Vector3(1f, 1f, 0f), new Vector2(1f, 0f))
	};

	private VertexBufferBinding vertBuffer;

	private Texture[] oldTextures = new Texture[3];

	private SamplerState[] oldSamplers = new SamplerState[3];

	private RenderTargetBinding[] oldTargets;

	private VertexBufferBinding[] oldBuffers;

	private BlendState prevBlend;

	private DepthStencilState prevDepthStencil;

	private RasterizerState prevRasterizer;

	private Viewport prevViewport;

	private FNA3D.FNA3D_RenderTargetBinding[] nativeVideoTexture = new FNA3D.FNA3D_RenderTargetBinding[3];

	private FNA3D.FNA3D_RenderTargetBinding[] nativeOldTargets = new FNA3D.FNA3D_RenderTargetBinding[4];

	private bool backing_ismuted;

	private float backing_volume;

	private Stopwatch timer;

	private RenderTargetBinding[] videoTexture;

	private GraphicsDevice currentDevice;

	private IntPtr yuvData;

	private int yuvDataLen;

	private int currentFrame;

	private const int AUDIO_BUFFER_SIZE = 8192;

	private static readonly float[] audioData = new float[8192];

	private static GCHandle audioHandle = GCHandle.Alloc(audioData, GCHandleType.Pinned);

	private IntPtr audioDataPtr = audioHandle.AddrOfPinnedObject();

	private DynamicSoundEffectInstance audioStream;

	public bool IsDisposed { get; private set; }

	public bool IsLooped { get; set; }

	public bool IsMuted
	{
		get
		{
			return backing_ismuted;
		}
		set
		{
			backing_ismuted = value;
			UpdateVolume();
		}
	}

	public TimeSpan PlayPosition => timer.Elapsed;

	public MediaState State { get; private set; }

	public Video Video { get; private set; }

	public float Volume
	{
		get
		{
			return backing_volume;
		}
		set
		{
			if (value > 1f)
			{
				backing_volume = 1f;
			}
			else if (value < 0f)
			{
				backing_volume = 0f;
			}
			else
			{
				backing_volume = value;
			}
			UpdateVolume();
		}
	}

	private unsafe void GL_initialize()
	{
		shaderProgram = new Effect(currentDevice, Resources.YUVToRGBAEffect);
		stateChangesPtr = Marshal.AllocHGlobal(sizeof(Effect.MOJOSHADER_effectStateChanges));
		vertBuffer = new VertexBufferBinding(new VertexBuffer(currentDevice, VertexPositionTexture.VertexDeclaration, 4, BufferUsage.WriteOnly));
		vertBuffer.VertexBuffer.SetData(vertices);
	}

	private void GL_dispose()
	{
		if (currentDevice == null)
		{
			return;
		}
		currentDevice = null;
		if (shaderProgram != null)
		{
			shaderProgram.Dispose();
		}
		if (stateChangesPtr != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(stateChangesPtr);
		}
		if (vertBuffer.VertexBuffer != null)
		{
			vertBuffer.VertexBuffer.Dispose();
		}
		for (int i = 0; i < 3; i++)
		{
			if (yuvTextures[i] != null)
			{
				yuvTextures[i].Dispose();
			}
		}
	}

	private void GL_setupTextures(int yWidth, int yHeight, int uvWidth, int uvHeight)
	{
		for (int i = 0; i < 3; i++)
		{
			if (yuvTextures[i] != null)
			{
				yuvTextures[i].Dispose();
			}
		}
		yuvTextures[0] = new Texture2D(currentDevice, yWidth, yHeight, mipMap: false, SurfaceFormat.Alpha8);
		yuvTextures[1] = new Texture2D(currentDevice, uvWidth, uvHeight, mipMap: false, SurfaceFormat.Alpha8);
		yuvTextures[2] = new Texture2D(currentDevice, uvWidth, uvHeight, mipMap: false, SurfaceFormat.Alpha8);
		viewport = new Viewport(0, 0, yWidth, yHeight);
	}

	private unsafe void GL_pushState()
	{
		FNA3D.FNA3D_BeginPassRestore(currentDevice.GLDevice, shaderProgram.glEffect, stateChangesPtr);
		for (int i = 0; i < 3; i++)
		{
			oldTextures[i] = currentDevice.Textures[i];
			oldSamplers[i] = currentDevice.SamplerStates[i];
			currentDevice.Textures[i] = yuvTextures[i];
			currentDevice.SamplerStates[i] = SamplerState.LinearClamp;
		}
		oldBuffers = currentDevice.GetVertexBuffers();
		currentDevice.SetVertexBuffers(vertBuffer);
		oldTargets = currentDevice.GetRenderTargets();
		fixed (FNA3D.FNA3D_RenderTargetBinding* ptr = &nativeVideoTexture[0])
		{
			GraphicsDevice.PrepareRenderTargetBindings(ptr, videoTexture);
			FNA3D.FNA3D_SetRenderTargets(currentDevice.GLDevice, ptr, videoTexture.Length, IntPtr.Zero, DepthFormat.None, 0);
		}
		prevBlend = currentDevice.BlendState;
		prevDepthStencil = currentDevice.DepthStencilState;
		prevRasterizer = currentDevice.RasterizerState;
		currentDevice.BlendState = BlendState.Opaque;
		currentDevice.DepthStencilState = DepthStencilState.None;
		currentDevice.RasterizerState = RasterizerState.CullNone;
		prevViewport = currentDevice.Viewport;
		FNA3D.FNA3D_SetViewport(currentDevice.GLDevice, ref viewport.viewport);
	}

	private unsafe void GL_popState()
	{
		FNA3D.FNA3D_EndPassRestore(currentDevice.GLDevice, shaderProgram.glEffect);
		currentDevice.BlendState = prevBlend;
		currentDevice.DepthStencilState = prevDepthStencil;
		currentDevice.RasterizerState = prevRasterizer;
		prevBlend = null;
		prevDepthStencil = null;
		prevRasterizer = null;
		if (oldTargets == null || oldTargets.Length == 0)
		{
			FNA3D.FNA3D_SetRenderTargets(currentDevice.GLDevice, IntPtr.Zero, 0, IntPtr.Zero, DepthFormat.None, 0);
		}
		else
		{
			IRenderTarget renderTarget = oldTargets[0].RenderTarget as IRenderTarget;
			fixed (FNA3D.FNA3D_RenderTargetBinding* ptr = &nativeOldTargets[0])
			{
				GraphicsDevice.PrepareRenderTargetBindings(ptr, oldTargets);
				FNA3D.FNA3D_SetRenderTargets(currentDevice.GLDevice, ptr, oldTargets.Length, renderTarget.DepthStencilBuffer, renderTarget.DepthStencilFormat, (renderTarget.RenderTargetUsage != RenderTargetUsage.DiscardContents) ? ((byte)1) : ((byte)0));
			}
		}
		oldTargets = null;
		FNA3D.FNA3D_SetViewport(currentDevice.GLDevice, ref prevViewport.viewport);
		currentDevice.SetVertexBuffers(oldBuffers);
		oldBuffers = null;
		currentDevice.Textures.ignoreTargets = true;
		for (int i = 0; i < 3; i++)
		{
			if (oldTextures[i] == null || !oldTextures[i].IsDisposed)
			{
				currentDevice.Textures[i] = oldTextures[i];
			}
			currentDevice.SamplerStates[i] = oldSamplers[i];
			oldTextures[i] = null;
			oldSamplers[i] = null;
		}
		currentDevice.Textures.ignoreTargets = false;
	}

	private void checkDisposed()
	{
		if (IsDisposed)
		{
			throw new ObjectDisposedException("VideoPlayer");
		}
	}

	private void UpdateVolume()
	{
		if (audioStream != null)
		{
			if (IsMuted)
			{
				audioStream.Volume = 0f;
			}
			else
			{
				audioStream.Volume = Volume * (1f / SoundEffect.MasterVolume);
			}
		}
	}

	public VideoPlayer()
	{
		IsDisposed = false;
		IsLooped = false;
		IsMuted = false;
		State = MediaState.Stopped;
		Volume = 1f;
		timer = new Stopwatch();
		videoTexture = new RenderTargetBinding[1];
	}

	public void Dispose()
	{
		if (!IsDisposed)
		{
			Stop();
			GL_dispose();
			if (audioStream != null)
			{
				audioStream.Dispose();
				audioStream = null;
			}
			if (videoTexture[0].RenderTarget != null)
			{
				videoTexture[0].RenderTarget.Dispose();
			}
			if (yuvData != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(yuvData);
				yuvData = IntPtr.Zero;
			}
			IsDisposed = true;
		}
	}

	public Texture2D GetTexture()
	{
		checkDisposed();
		if (Video == null)
		{
			throw new InvalidOperationException();
		}
		if (State == MediaState.Stopped || Video.theora == IntPtr.Zero || Theorafile.tf_hasvideo(Video.theora) == 0)
		{
			return videoTexture[0].RenderTarget as Texture2D;
		}
		int num = (int)(timer.Elapsed.TotalMilliseconds / (1000.0 / Video.fps));
		if (num > currentFrame)
		{
			if (Theorafile.tf_readvideo(Video.theora, yuvData, num - currentFrame) == 1 || currentFrame == -1)
			{
				UpdateTexture();
			}
			currentFrame = num;
		}
		bool flag = Theorafile.tf_eos(Video.theora) == 1;
		if (audioStream != null)
		{
			flag &= audioStream.PendingBufferCount == 0;
		}
		if (flag)
		{
			if (Video.needsDurationHack)
			{
				Video.Duration = timer.Elapsed;
			}
			timer.Stop();
			timer.Reset();
			if (audioStream != null)
			{
				audioStream.Stop();
				audioStream.Dispose();
				audioStream = null;
			}
			Theorafile.tf_reset(Video.theora);
			if (IsLooped)
			{
				InitializeTheoraStream();
				timer.Start();
				if (audioStream != null)
				{
					audioStream.Play();
				}
			}
			else
			{
				State = MediaState.Stopped;
			}
		}
		return videoTexture[0].RenderTarget as Texture2D;
	}

	public void Play(Video video)
	{
		checkDisposed();
		Video = video;
		if (Video.needsDurationHack)
		{
			Video.Duration = TimeSpan.MaxValue;
		}
		if (State != MediaState.Stopped)
		{
			return;
		}
		State = MediaState.Playing;
		if (yuvData != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(yuvData);
		}
		yuvDataLen = Video.yWidth * Video.yHeight + Video.uvWidth * Video.uvHeight * 2;
		yuvData = Marshal.AllocHGlobal(yuvDataLen);
		InitializeTheoraStream();
		if (Theorafile.tf_hasvideo(Video.theora) == 1)
		{
			if (currentDevice != Video.GraphicsDevice)
			{
				GL_dispose();
				currentDevice = Video.GraphicsDevice;
				GL_initialize();
			}
			RenderTargetBinding renderTargetBinding = videoTexture[0];
			videoTexture[0] = new RenderTargetBinding(new RenderTarget2D(currentDevice, Video.yWidth, Video.yHeight, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents));
			if (renderTargetBinding.RenderTarget != null)
			{
				renderTargetBinding.RenderTarget.Dispose();
			}
			GL_setupTextures(Video.yWidth, Video.yHeight, Video.uvWidth, Video.uvHeight);
		}
		timer.Start();
		if (audioStream != null)
		{
			audioStream.Play();
		}
	}

	public void Stop()
	{
		checkDisposed();
		if (State != MediaState.Stopped)
		{
			State = MediaState.Stopped;
			timer.Stop();
			timer.Reset();
			if (audioStream != null)
			{
				audioStream.Stop();
				audioStream.Dispose();
				audioStream = null;
			}
			Theorafile.tf_reset(Video.theora);
		}
	}

	public void Pause()
	{
		checkDisposed();
		if (State == MediaState.Playing)
		{
			State = MediaState.Paused;
			timer.Stop();
			if (audioStream != null)
			{
				audioStream.Pause();
			}
		}
	}

	public void Resume()
	{
		checkDisposed();
		if (State == MediaState.Paused)
		{
			State = MediaState.Playing;
			timer.Start();
			if (audioStream != null)
			{
				audioStream.Resume();
			}
		}
	}

	private void OnBufferRequest(object sender, EventArgs args)
	{
		int num = Theorafile.tf_readaudio(Video.theora, audioDataPtr, 8192);
		if (num > 0)
		{
			audioStream.SubmitFloatBufferEXT(audioData, 0, num);
		}
		else if (Theorafile.tf_eos(Video.theora) == 1)
		{
			audioStream.BufferNeeded -= OnBufferRequest;
		}
	}

	private void UpdateTexture()
	{
		FNA3D.FNA3D_SetTextureDataYUV(currentDevice.GLDevice, yuvTextures[0].texture, yuvTextures[1].texture, yuvTextures[2].texture, Video.yWidth, Video.yHeight, Video.uvWidth, Video.uvHeight, yuvData, yuvDataLen);
		GL_pushState();
		currentDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
		GL_popState();
	}

	private void InitializeTheoraStream()
	{
		while (Theorafile.tf_readvideo(Video.theora, yuvData, 1) == 0)
		{
		}
		if (Theorafile.tf_hasaudio(Video.theora) == 1)
		{
			Theorafile.tf_audioinfo(Video.theora, out var channels, out var samplerate);
			audioStream = new DynamicSoundEffectInstance(samplerate, (AudioChannels)channels);
			audioStream.BufferNeeded += OnBufferRequest;
			UpdateVolume();
			for (int i = 0; i < 4; i++)
			{
				OnBufferRequest(audioStream, EventArgs.Empty);
				if (audioStream.PendingBufferCount == i)
				{
					break;
				}
			}
		}
		currentFrame = -1;
	}
}
