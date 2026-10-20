using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.GamerServices;

public class AchievementCollection : IList<Achievement>, ICollection<Achievement>, IEnumerable<Achievement>, IEnumerable, IDisposable
{
	private List<Achievement> collection;

	public int Count => collection.Count;

	public bool IsDisposed { get; private set; }

	public Achievement this[int index] => collection[index];

	public Achievement this[string achievementKey]
	{
		get
		{
			foreach (Achievement item in collection)
			{
				if (item.Key == achievementKey)
				{
					return item;
				}
			}
			throw new IndexOutOfRangeException();
		}
	}

	Achievement IList<Achievement>.this[int index]
	{
		get
		{
			return collection[index];
		}
		set
		{
		}
	}

	bool ICollection<Achievement>.IsReadOnly => true;

	internal AchievementCollection(List<Achievement> collection)
	{
		this.collection = collection;
		IsDisposed = false;
	}

	public AchievementCollection()
	{
		collection = new List<Achievement>();
	}

	public void Dispose()
	{
		if (!IsDisposed)
		{
			collection.Clear();
			IsDisposed = true;
		}
	}

	public IEnumerator<Achievement> GetEnumerator()
	{
		return collection.GetEnumerator();
	}

	int IList<Achievement>.IndexOf(Achievement item)
	{
		return collection.IndexOf(item);
	}

	void IList<Achievement>.Insert(int index, Achievement item)
	{
		collection.Insert(index, item);
	}

	void IList<Achievement>.RemoveAt(int index)
	{
		collection.RemoveAt(index);
	}

	void ICollection<Achievement>.Add(Achievement item)
	{
		collection.Add(item);
	}

	bool ICollection<Achievement>.Remove(Achievement item)
	{
		return collection.Remove(item);
	}

	void ICollection<Achievement>.Clear()
	{
		collection.Clear();
	}

	bool ICollection<Achievement>.Contains(Achievement item)
	{
		return collection.Contains(item);
	}

	void ICollection<Achievement>.CopyTo(Achievement[] array, int arrayIndex)
	{
		collection.CopyTo(array, arrayIndex);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
