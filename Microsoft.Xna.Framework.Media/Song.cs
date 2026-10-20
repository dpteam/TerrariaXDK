using System;
using System.IO;

namespace Microsoft.Xna.Framework.Media;

public sealed class Song : IEquatable<Song>, IDisposable
{
	internal string handle;

	public string Name { get; private set; }

	public TimeSpan Duration { get; internal set; }

	public bool IsProtected => false;

	public bool IsRated => false;

	public int PlayCount { get; internal set; }

	public int Rating => 0;

	public int TrackNumber => 0;

	public bool IsDisposed { get; private set; }

	internal Song(string fileName, string name = null)
	{
		if (!File.Exists(fileName))
		{
			throw new FileNotFoundException(fileName);
		}
		handle = fileName;
		Name = name;
		IsDisposed = false;
	}

	internal Song(string fileName, int durationMS)
		: this(fileName)
	{
		Duration = TimeSpan.FromMilliseconds(durationMS);
	}

	~Song()
	{
		Dispose();
	}

	public void Dispose()
	{
		IsDisposed = true;
	}

	public bool Equals(Song song)
	{
		return (object)song != null && handle == song.handle;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		return Equals(obj as Song);
	}

	public static bool operator ==(Song song1, Song song2)
	{
		return song1?.Equals(song2) ?? ((object)song2 == null);
	}

	public static bool operator !=(Song song1, Song song2)
	{
		return !(song1 == song2);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public static Song FromUri(string name, Uri uri)
	{
		string fileName;
		if (uri.IsAbsoluteUri)
		{
			if (!uri.IsFile)
			{
				throw new InvalidOperationException("Only local file URIs are supported for now");
			}
			fileName = uri.LocalPath;
		}
		else
		{
			fileName = Path.Combine(TitleLocation.Path, uri.ToString());
		}
		return new Song(fileName, name);
	}
}
