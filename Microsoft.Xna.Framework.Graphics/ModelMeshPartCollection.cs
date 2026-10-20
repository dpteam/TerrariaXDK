using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class ModelMeshPartCollection : ReadOnlyCollection<ModelMeshPart>
{
	public struct Enumerator(ModelMeshPartCollection collection) : IEnumerator<ModelMeshPart>, IDisposable, IEnumerator
	{
		private readonly ModelMeshPartCollection collection = collection;

		private int position = -1;

		public ModelMeshPart Current => collection[position];

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

	internal ModelMeshPartCollection(IList<ModelMeshPart> list)
		: base(list)
	{
	}

	public new Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}
}
