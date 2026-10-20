using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Audio;
using SDL2;

namespace Terraria;

internal class SoundManager
{
	private const string MusicError = "The number of loaded music files does not equate to the number of music entries expected.\nMusic may be played during incorrect events as a result.";

	private readonly Dictionary<string, MusicEntry> LoadedMusic;

	private readonly int MusicCount;

	private MusicEntry Song;

	internal bool IsSilentMode { get; private set; } = false;

	internal SoundManager(string[] MusicList)
	{
		Program.Log($"[SoundManager] Constructor start, {MusicList?.Length ?? 0} tracks requested");
		MusicCount = MusicList?.Length ?? 0;
		if (MusicList == null || MusicList.Length == 0)
		{
			Program.Log("[SoundManager] No music list provided, skipping");
			LoadedMusic = new Dictionary<string, MusicEntry>();
			return;
		}
		FileInfo[] array = null;
		try
		{
			Program.Log("[SoundManager] Calling GetMatchingFiles...");
			array = GetMatchingFiles("Content/ExtMusic", MusicList);
			Program.Log($"[SoundManager] GetMatchingFiles returned {array?.Length ?? 0} files");
		}
		catch (Exception ex)
		{
			Program.Log("[SoundManager] \ud83d\udca5 GetMatchingFiles failed: " + ex.Message);
			array = new FileInfo[0];
		}
		LoadedMusic = new Dictionary<string, MusicEntry>();
		try
		{
			Program.Log("[SoundManager] Running pre-flight audio check...");
			byte[] buffer = new byte[2];
			using (new SoundEffect(buffer, 44100, AudioChannels.Mono))
			{
				Program.Log("[SoundManager] Pre-flight audio check PASSED.");
			}
		}
		catch (Exception ex2)
		{
			Program.Log("[SoundManager] ❌ Pre-flight audio check FAILED. Audio subsystem is incompatible. Running Silent. Error: " + ex2.Message);
			IsSilentMode = true;
			return;
		}
		for (int i = 0; i < array.Length; i++)
		{
			Program.Log($"[SoundManager] Processing track {i + 1}/{array.Length}: {MusicList[i]}");
			MusicEntry musicEntry = new MusicEntry();
			string fileName = Path.GetFileName(array[i].Name);
			string text = "Content/ExtMusic/" + fileName;
			Program.Log("[SoundManager] Attempting path: " + text);
			try
			{
				if (musicEntry.Initialize(text))
				{
					LoadedMusic.Add(MusicList[i], musicEntry);
					Program.Log("[SoundManager] ✅ Loaded: " + MusicList[i]);
				}
				else
				{
					Program.Log("[SoundManager] ⚠\ufe0f Initialize returned false for: " + MusicList[i]);
				}
			}
			catch (NoAudioHardwareException ex3)
			{
				Console.WriteLine("[SoundManager] ❌ No Audio Hardware detected during load! Running in silent mode. Error: " + ex3.Message);
				IsSilentMode = true;
				Stop(null, HaltAll: true);
				LoadedMusic.Clear();
				return;
			}
			catch (Exception ex4)
			{
				Program.Log("[SoundManager] \ud83d\udca5 Exception loading " + MusicList[i] + ": " + ex4.Message);
			}
		}
		Program.Log($"[SoundManager] Constructor done. {LoadedMusic.Count}/{MusicCount} tracks loaded");
	}

	private static FileInfo[] GetMatchingFiles(string DirPath, string[] FileList)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(DirPath);
		List<FileInfo> list = new List<FileInfo>();
		foreach (string Name in FileList)
		{
			FileInfo fileInfo = directoryInfo.GetFiles().FirstOrDefault((FileInfo f) => string.Equals(Path.GetFileNameWithoutExtension(f.Name), Name, StringComparison.OrdinalIgnoreCase));
			if (fileInfo != null)
			{
				list.Add(fileInfo);
			}
		}
		return list.ToArray();
	}

	internal bool MatchCounted(IntPtr GameWindow)
	{
		if (IsSilentMode)
		{
			return true;
		}
		if (LoadedMusic.Count != MusicCount)
		{
			SDL.SDL_ShowSimpleMessageBox(SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING, "Loaded music mismatch", "The number of loaded music files does not equate to the number of music entries expected.\nMusic may be played during incorrect events as a result.", GameWindow);
			return false;
		}
		return true;
	}

	internal void Play(string Cue)
	{
		if (!IsSilentMode && LoadedMusic.TryGetValue(Cue, out Song))
		{
			Song.Play();
		}
	}

	internal void Pause(string Cue)
	{
		if (!IsSilentMode && LoadedMusic.TryGetValue(Cue, out Song))
		{
			Song.Pause();
		}
	}

	internal void Stop(string Cue, bool HaltAll = false)
	{
		if (IsSilentMode)
		{
			return;
		}
		if (HaltAll)
		{
			foreach (KeyValuePair<string, MusicEntry> item in LoadedMusic)
			{
				item.Value.Stop();
			}
			return;
		}
		if (LoadedMusic.TryGetValue(Cue, out Song))
		{
			Song.Stop();
		}
	}

	internal void Resume(string Cue)
	{
		if (!IsSilentMode && LoadedMusic.TryGetValue(Cue, out Song))
		{
			Song.Resume();
		}
	}

	internal void SetVolume(string Cue, float Value)
	{
		if (!IsSilentMode && LoadedMusic.TryGetValue(Cue, out Song))
		{
			Song.SetVolume(Value);
		}
	}

	internal void SetPitch(string Cue, float Value)
	{
		if (!IsSilentMode && LoadedMusic.TryGetValue(Cue, out Song))
		{
			Song.SetPitch(Value);
		}
	}

	internal void SetLoop(string Cue, bool ShouldLoop)
	{
		if (!IsSilentMode && LoadedMusic.TryGetValue(Cue, out Song))
		{
			Song.SetLoop(ShouldLoop);
		}
	}

	internal bool CheckState(string Cue, SoundState State)
	{
		if (IsSilentMode)
		{
			return false;
		}
		if (LoadedMusic.TryGetValue(Cue, out Song))
		{
			return Song.GetState() == State;
		}
		return false;
	}
}
