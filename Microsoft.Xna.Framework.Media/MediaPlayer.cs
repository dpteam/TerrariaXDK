using System;
using System.Diagnostics;

namespace Microsoft.Xna.Framework.Media;

public static class MediaPlayer
{
	private static bool INTERNAL_isMuted;

	private static MediaState INTERNAL_state;

	private static float INTERNAL_volume;

	private static bool initialized;

	private static int numSongsInQueuePlayed;

	private static Stopwatch timer;

	private static readonly Random random;

	public static bool GameHasControl => true;

	public static bool IsMuted
	{
		get
		{
			return INTERNAL_isMuted;
		}
		set
		{
			INTERNAL_isMuted = value;
			FAudio.XNA_SetSongVolume(INTERNAL_isMuted ? 0f : INTERNAL_volume);
		}
	}

	public static bool IsRepeating { get; set; }

	public static bool IsShuffled { get; set; }

	public static TimeSpan PlayPosition => timer.Elapsed;

	public static MediaQueue Queue { get; private set; }

	public static MediaState State
	{
		get
		{
			return INTERNAL_state;
		}
		private set
		{
			if (INTERNAL_state != value)
			{
				INTERNAL_state = value;
				FrameworkDispatcher.MediaStateChanged = true;
			}
		}
	}

	public static float Volume
	{
		get
		{
			return INTERNAL_volume;
		}
		set
		{
			INTERNAL_volume = MathHelper.Clamp(value, 0f, 1f);
			FAudio.XNA_SetSongVolume(IsMuted ? 0f : INTERNAL_volume);
		}
	}

	public static bool IsVisualizationEnabled
	{
		get
		{
			return FAudio.XNA_VisualizationEnabled() == 1;
		}
		set
		{
			FAudio.XNA_EnableVisualization(value ? 1u : 0u);
		}
	}

	public static event EventHandler<EventArgs> ActiveSongChanged;

	public static event EventHandler<EventArgs> MediaStateChanged;

	static MediaPlayer()
	{
		INTERNAL_isMuted = false;
		INTERNAL_state = MediaState.Stopped;
		INTERNAL_volume = 1f;
		initialized = false;
		numSongsInQueuePlayed = 0;
		timer = new Stopwatch();
		random = new Random();
		Queue = new MediaQueue();
	}

	public static void MoveNext()
	{
		NextSong(1);
	}

	public static void MovePrevious()
	{
		NextSong(-1);
	}

	public static void Pause()
	{
		if (State == MediaState.Playing && !(Queue.ActiveSong == null))
		{
			FAudio.XNA_PauseSong();
			timer.Stop();
			State = MediaState.Paused;
		}
	}

	public static void Play(Song song)
	{
		Song song2 = ((Queue.Count > 0) ? Queue[0] : null);
		Queue.Clear();
		numSongsInQueuePlayed = 0;
		LoadSong(song);
		Queue.ActiveSongIndex = 0;
		PlaySong(song);
		if (song2 != song)
		{
			FrameworkDispatcher.ActiveSongChanged = true;
		}
	}

	public static void Play(SongCollection songs)
	{
		Play(songs, 0);
	}

	public static void Play(SongCollection songs, int index)
	{
		Queue.Clear();
		numSongsInQueuePlayed = 0;
		foreach (Song song in songs)
		{
			LoadSong(song);
		}
		Queue.ActiveSongIndex = index;
		PlaySong(Queue.ActiveSong);
	}

	public static void Resume()
	{
		if (State == MediaState.Paused)
		{
			FAudio.XNA_ResumeSong();
			timer.Start();
			State = MediaState.Playing;
		}
	}

	public static void Stop()
	{
		if (State != MediaState.Stopped)
		{
			FAudio.XNA_StopSong();
			timer.Stop();
			timer.Reset();
			for (int i = 0; i < Queue.Count; i++)
			{
				Queue[i].PlayCount = 0;
			}
			State = MediaState.Stopped;
		}
	}

	public static void GetVisualizationData(VisualizationData data)
	{
		FAudio.XNA_GetSongVisualizationData(data.freq, data.samp, 256u);
	}

	internal static void Update()
	{
		if (Queue == null || Queue.ActiveSong == null || State != MediaState.Playing || FAudio.XNA_GetSongEnded() == 0)
		{
			return;
		}
		numSongsInQueuePlayed++;
		if (numSongsInQueuePlayed >= Queue.Count)
		{
			numSongsInQueuePlayed = 0;
			if (!IsRepeating)
			{
				Stop();
				FrameworkDispatcher.ActiveSongChanged = true;
				return;
			}
		}
		MoveNext();
	}

	internal static void DisposeIfNecessary()
	{
		if (initialized)
		{
			FAudio.XNA_SongQuit();
			initialized = false;
		}
	}

	internal static void OnActiveSongChanged()
	{
		if (ActiveSongChanged != null)
		{
			ActiveSongChanged(null, EventArgs.Empty);
		}
	}

	internal static void OnMediaStateChanged()
	{
		if (MediaStateChanged != null)
		{
			MediaStateChanged(null, EventArgs.Empty);
		}
	}

	private static void LoadSong(Song song)
	{
		Queue.Add(new Song(song.handle, song.Name));
	}

	private static void NextSong(int direction)
	{
		Stop();
		if (IsRepeating && Queue.ActiveSongIndex >= Queue.Count - 1)
		{
			Queue.ActiveSongIndex = 0;
			direction = 0;
		}
		if (IsShuffled)
		{
			Queue.ActiveSongIndex = random.Next(Queue.Count);
		}
		else
		{
			Queue.ActiveSongIndex = MathHelper.Clamp(Queue.ActiveSongIndex + direction, 0, Queue.Count - 1);
		}
		Song song = Queue[Queue.ActiveSongIndex];
		if (song != null)
		{
			PlaySong(song);
		}
		FrameworkDispatcher.ActiveSongChanged = true;
	}

	private static void PlaySong(Song song)
	{
		if (!initialized)
		{
			FAudio.XNA_SongInit();
			initialized = true;
		}
		song.Duration = TimeSpan.FromSeconds(FAudio.XNA_PlaySong(song.handle));
		timer.Start();
		State = MediaState.Playing;
	}
}
