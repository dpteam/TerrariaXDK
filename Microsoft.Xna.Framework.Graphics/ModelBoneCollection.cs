using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics;

public class ModelBoneCollection : ReadOnlyCollection<ModelBone>
{
	public struct Enumerator(ModelBoneCollection collection) : IEnumerator<ModelBone>, IDisposable, IEnumerator
	{
		private readonly ModelBoneCollection collection = collection;

		private int position = -1;

		public ModelBone Current => collection[position];

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

	public ModelBone this[string boneName]
	{
		get
		{
			if (TryGetValue(boneName, out var value))
			{
				return value;
			}
			throw new KeyNotFoundException();
		}
	}

	internal ModelBoneCollection(IList<ModelBone> list)
		: base(list)
	{
	}

	public bool TryGetValue(string boneName, out ModelBone value)
	{
		foreach (ModelBone item in Items)
		{
			if (item.Name == boneName)
			{
				value = item;
				return true;
			}
		}
		value = null;
		return false;
	}

	public new Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}
}
