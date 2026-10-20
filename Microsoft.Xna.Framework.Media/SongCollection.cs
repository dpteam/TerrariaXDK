using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Media;

public sealed class SongCollection : IEnumerable<Song>, IEnumerable, IDisposable
{
	private List<Song> innerlist;

	public Song this[int index] => innerlist[index];

	public int Count => innerlist.Count;

	public bool IsDisposed { get; private set; }

	internal SongCollection(List<Song> songs)
	{
		innerlist = songs;
		IsDisposed = false;
	}

	public void Dispose()
	{
		innerlist.Clear();
		IsDisposed = true;
	}

	public IEnumerator<Song> GetEnumerator()
	{
		return innerlist.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return innerlist.GetEnumerator();
	}
}
