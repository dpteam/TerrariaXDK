using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace Terraria;

/// <summary>
/// One music track. Two backends:
///
/// * <b>OGG (preferred)</b> - streamed by FAudio/stb_vorbis through <see cref="MediaPlayer"/>.
///   Only a small decode buffer is resident, so all 19 tracks together cost a few hundred KB
///   instead of the ~80 MB a decoded WAV set needs.
/// * <b>WAV (fallback)</b> - decoded entirely into a <see cref="SoundEffect"/>. Kept so a
///   Content folder that only ships .wav still works.
///
/// The game plays at most one track at a time, so the streaming backend can map onto the
/// single global <see cref="MediaPlayer"/>: the entry that started playback owns it and every
/// other entry reports "stopped".
/// </summary>
internal class MusicEntry
{
	/// <summary>The streamed entry that currently owns MediaPlayer, if any.</summary>
	private static MusicEntry ActiveStream;

	private SoundEffect SfxSong;

	private SoundEffectInstance SongInstance;

	private Song StreamedSong;

	private string CueName;

	private bool IsStreaming;

	/// <summary>Volume to apply when this entry becomes the active stream.</summary>
	private float PendingVolume = 1f;

	/// <summary>Set after a streaming failure so we stop retrying a broken track.</summary>
	private bool streamBroken;

	internal bool IsStreamed => IsStreaming;

	internal bool Initialize(string Path)
	{
		try
		{
			if (Path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
			{
				return InitializeStreaming(Path);
			}
			return InitializeBuffered(Path);
		}
		catch (Exception ex)
		{
			Console.WriteLine("'" + Path + "' was unable to be loaded due to the below error:");
			Console.WriteLine(ex.ToString());
			return false;
		}
	}

	private bool InitializeStreaming(string path)
	{
		string text = Path.GetFullPath(path);
		if (!File.Exists(text))
		{
			return false;
		}
		CueName = Path.GetFileNameWithoutExtension(text);
		// FromUri keeps the absolute path; FAudio opens it on demand (stb_vorbis).
		StreamedSong = Song.FromUri(CueName, new Uri(text));
		IsStreaming = true;
		return true;
	}

	private bool InitializeBuffered(string path)
	{
		using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
		SfxSong = SoundEffect.FromStream(fileStream);
		SongInstance = SfxSong.CreateInstance();
		return true;
	}

	private bool IsActive => IsStreaming && ActiveStream == this;

	internal void Play()
	{
		if (!IsStreaming)
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
			return;
		}
		if (IsActive && MediaPlayer.State == MediaState.Paused)
		{
			MediaPlayer.Resume();
			return;
		}
		if (IsActive && MediaPlayer.State == MediaState.Playing)
		{
			return;
		}
		// Switching tracks: the previous stream is replaced by MediaPlayer itself.
		if (!SafeStream(delegate
		{
			ActiveStream = this;
			MediaPlayer.IsRepeating = true;
			MediaPlayer.Volume = PendingVolume;
			MediaPlayer.Play(StreamedSong);
		}))
		{
			ActiveStream = null;
		}
	}

	/// <summary>
	/// Streaming touches the native decoder and the output device, both of which can fail
	/// (missing codec, unplugged headset). A failed track must not take the game down, so
	/// every streamed operation goes through here and reports failure once.
	/// </summary>
	private bool SafeStream(Action action)
	{
		if (streamBroken)
		{
			return false;
		}
		try
		{
			action();
			return true;
		}
		catch (Exception ex)
		{
			streamBroken = true;
			Console.WriteLine("[Music] streaming failed for '" + CueName + "': " + ex.Message);
			return false;
		}
	}

	internal void Pause()
	{
		if (IsStreaming)
		{
			if (IsActive && MediaPlayer.State == MediaState.Playing)
			{
				MediaPlayer.Pause();
			}
			return;
		}
		if (SongInstance.State != SoundState.Paused && SongInstance.State != SoundState.Stopped)
		{
			SongInstance.Pause();
		}
	}

	internal void Stop()
	{
		if (IsStreaming)
		{
			if (IsActive && MediaPlayer.State != MediaState.Stopped)
			{
				MediaPlayer.Stop();
			}
			return;
		}
		if (SongInstance.State != SoundState.Stopped && SongInstance.State != SoundState.Paused)
		{
			SongInstance.Stop();
		}
	}

	internal void Resume()
	{
		if (IsStreaming)
		{
			if (IsActive && MediaPlayer.State == MediaState.Paused)
			{
				MediaPlayer.Resume();
			}
			return;
		}
		if (SongInstance.State != SoundState.Playing)
		{
			SongInstance.Resume();
		}
	}

	internal void SetVolume(float value)
	{
		PendingVolume = value;
		if (IsStreaming)
		{
			if (IsActive)
			{
				MediaPlayer.Volume = value;
			}
			return;
		}
		SongInstance.Volume = value;
	}

	internal void SetPitch(float value)
	{
		// MediaPlayer has no pitch control. Pitch was never used by the game's music path,
		// and dropping it keeps the streaming backend simple.
		if (!IsStreaming)
		{
			SongInstance.Pitch = value;
		}
	}

	internal void SetLoop(bool value)
	{
		if (IsStreaming)
		{
			if (IsActive)
			{
				MediaPlayer.IsRepeating = value;
			}
			return;
		}
		SongInstance.IsLooped = value;
	}

	internal SoundState GetState()
	{
		if (IsStreaming)
		{
			if (!IsActive)
			{
				return SoundState.Stopped;
			}
			return MediaPlayer.State switch
			{
				MediaState.Playing => SoundState.Playing,
				MediaState.Paused => SoundState.Paused,
				_ => SoundState.Stopped,
			};
		}
		return SongInstance.State;
	}
}
