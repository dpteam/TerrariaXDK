using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Media;

public sealed class MediaQueue
{
	private List<Song> songs = new List<Song>();

	public Song ActiveSong
	{
		get
		{
			if (songs.Count == 0 || ActiveSongIndex < 0)
			{
				return null;
			}
			return songs[ActiveSongIndex];
		}
	}

	public int ActiveSongIndex { get; set; }

	public int Count => songs.Count;

	public Song this[int index] => songs[index];

	internal MediaQueue()
	{
		ActiveSongIndex = -1;
	}

	internal void Add(Song song)
	{
		songs.Add(song);
	}

	internal void Clear()
	{
		songs.Clear();
	}
}
