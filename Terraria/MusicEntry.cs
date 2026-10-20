using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;

namespace Terraria;

internal class MusicEntry
{
	private SoundEffect SfxSong;

	private SoundEffectInstance SongInstance;

	internal MusicEntry()
	{
	}

	internal bool Initialize(string Path)
	{
		try
		{
			FileStream fileStream = new FileStream(Path, FileMode.Open, FileAccess.Read);
			SfxSong = SoundEffect.FromStream(fileStream);
			fileStream.Dispose();
			SongInstance = SfxSong.CreateInstance();
		}
		catch (Exception ex)
		{
			Console.WriteLine("'" + Path + "' was unable to be loaded due to the below error:");
			Console.WriteLine(ex.ToString());
			return false;
		}
		return true;
	}

	internal void Play()
	{
		if (SongInstance.State == SoundState.Paused)
		{
			SongInstance.Resume();
			return;
		}
		if (SongInstance.State == SoundState.Stopped)
		{
			SongInstance.Play();
			return;
		}
		SongInstance.Stop();
		SongInstance.Play();
	}

	internal void Pause()
	{
		if (SongInstance.State != SoundState.Paused && SongInstance.State != SoundState.Stopped)
		{
			SongInstance.Pause();
		}
	}

	internal void Stop()
	{
		if (SongInstance.State != SoundState.Stopped && SongInstance.State != SoundState.Paused)
		{
			SongInstance.Stop();
		}
	}

	internal void Resume()
	{
		if (SongInstance.State != SoundState.Playing)
		{
			SongInstance.Resume();
		}
	}

	internal void SetVolume(float value)
	{
		SongInstance.Volume = value;
	}

	internal void SetPitch(float value)
	{
		SongInstance.Pitch = value;
	}

	internal void SetLoop(bool value)
	{
		SongInstance.IsLooped = value;
	}

	internal SoundState GetState()
	{
		return SongInstance.State;
	}
}
