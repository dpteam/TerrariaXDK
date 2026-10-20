using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Input.Touch;

public struct TouchCollection : IList<TouchLocation>, ICollection<TouchLocation>, IEnumerable<TouchLocation>, IEnumerable
{
	public struct Enumerator(TouchCollection collection) : IEnumerator<TouchLocation>, IDisposable, IEnumerator
	{
		private TouchCollection collection = collection;

		private int position = -1;

		public TouchLocation Current => collection[position];

		object IEnumerator.Current => collection[position];

		public bool MoveNext()
		{
			position++;
			return position < collection.Count;
		}

		public void Dispose()
		{
		}

		void IEnumerator.Reset()
		{
			position = -1;
		}
	}

	private readonly List<TouchLocation> touches;

	public int Count => touches.Count;

	public bool IsConnected => TouchPanel.TouchDeviceExists;

	public bool IsReadOnly => true;

	public TouchLocation this[int index]
	{
		get
		{
			return touches[index];
		}
		set
		{
			touches[index] = value;
		}
	}

	public TouchCollection(TouchLocation[] touches)
	{
		this.touches = new List<TouchLocation>(touches);
	}

	public void Add(TouchLocation item)
	{
		touches.Add(item);
	}

	public void Clear()
	{
		touches.Clear();
	}

	public bool Contains(TouchLocation item)
	{
		return touches.Contains(item);
	}

	public void CopyTo(TouchLocation[] array, int arrayIndex)
	{
		touches.CopyTo(array, arrayIndex);
	}

	public bool FindById(int id, out TouchLocation touchLocation)
	{
		foreach (TouchLocation touch in touches)
		{
			if (touch.Id == id)
			{
				touchLocation = touch;
				return true;
			}
		}
		touchLocation = new TouchLocation(-1, TouchLocationState.Invalid, Vector2.Zero);
		return false;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	public int IndexOf(TouchLocation item)
	{
		return touches.IndexOf(item);
	}

	public void Insert(int index, TouchLocation item)
	{
		touches.Insert(index, item);
	}

	public bool Remove(TouchLocation item)
	{
		return touches.Remove(item);
	}

	public void RemoveAt(int index)
	{
		touches.RemoveAt(index);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator<TouchLocation> IEnumerable<TouchLocation>.GetEnumerator()
	{
		return new Enumerator(this);
	}
}
