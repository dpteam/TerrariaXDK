using System;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Media;

public sealed class Video
{
	internal IntPtr theora;

	internal int yWidth;

	internal int yHeight;

	internal int uvWidth;

	internal int uvHeight;

	internal double fps;

	internal bool needsDurationHack;

	public int Width => yWidth;

	public int Height => yHeight;

	public float FramesPerSecond => (float)fps;

	public VideoSoundtrackType VideoSoundtrackType { get; private set; }

	public TimeSpan Duration { get; internal set; }

	internal GraphicsDevice GraphicsDevice { get; private set; }

	internal Video(string fileName, GraphicsDevice device)
	{
		GraphicsDevice = device;
		Theorafile.tf_fopen(fileName, out theora);
		Theorafile.tf_videoinfo(theora, out yWidth, out yHeight, out fps, out var fmt);
		switch (fmt)
		{
		case Theorafile.th_pixel_fmt.TH_PF_420:
			uvWidth = yWidth / 2;
			uvHeight = yHeight / 2;
			break;
		case Theorafile.th_pixel_fmt.TH_PF_422:
			uvWidth = yWidth / 2;
			uvHeight = yHeight;
			break;
		case Theorafile.th_pixel_fmt.TH_PF_444:
			uvWidth = yWidth;
			uvHeight = yHeight;
			break;
		default:
			throw new NotSupportedException("Unrecognized YUV format!");
		}
		Duration = TimeSpan.MaxValue;
		needsDurationHack = true;
	}

	internal Video(string fileName, GraphicsDevice device, int durationMS, int width, int height, float framesPerSecond, VideoSoundtrackType soundtrackType)
		: this(fileName, device)
	{
		if (width != Width || height != Height)
		{
			throw new InvalidOperationException("XNB/OGV width/height mismatch! Width: " + Width + " Height: " + Height);
		}
		if (Math.Abs(FramesPerSecond - framesPerSecond) >= 1f)
		{
			throw new InvalidOperationException("XNB/OGV framesPerSecond mismatch! FPS: " + FramesPerSecond);
		}
		Duration = TimeSpan.FromMilliseconds(durationMS);
		needsDurationHack = false;
		VideoSoundtrackType = soundtrackType;
	}

	public void SetAudioTrackEXT(int track)
	{
		if (theora != IntPtr.Zero)
		{
			Theorafile.tf_setaudiotrack(theora, track);
		}
	}

	public void SetVideoTrackEXT(int track)
	{
		if (theora != IntPtr.Zero)
		{
			Theorafile.tf_setvideotrack(theora, track);
		}
	}

	~Video()
	{
		if (theora != IntPtr.Zero)
		{
			Theorafile.tf_close(ref theora);
		}
	}
}
