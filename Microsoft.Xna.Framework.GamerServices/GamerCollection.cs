using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.GamerServices;

public class GamerCollection<T> : ReadOnlyCollection<T>, IEnumerable<T>, IEnumerable where T : Gamer
{
	public struct GamerCollectionEnumerator : IEnumerator<T>, IDisposable, IEnumerator
	{
		private GamerCollection<T> collection;

		private int position;

		public T Current => collection[position];

		object IEnumerator.Current => collection[position];

		internal GamerCollectionEnumerator(GamerCollection<T> collection)
		{
			this.collection = collection;
			position = -1;
		}

		public void Dispose()
		{
			collection = null;
		}

		public bool MoveNext()
		{
			position++;
			return position < collection.Count;
		}

		void IEnumerator.Reset()
		{
			position = -1;
		}
	}

	internal List<T> collection;

	internal GamerCollection(List<T> collection)
		: base((IList<T>)collection)
	{
		this.collection = collection;
	}

	internal GamerCollection()
		: base((IList<T>)new List<T>())
	{
	}

	public new GamerCollectionEnumerator GetEnumerator()
	{
		return new GamerCollectionEnumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
