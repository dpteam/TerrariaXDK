using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class ModelMeshCollection : ReadOnlyCollection<ModelMesh>
{
	public struct Enumerator(ModelMeshCollection collection) : IEnumerator<ModelMesh>, IDisposable, IEnumerator
	{
		private readonly ModelMeshCollection collection = collection;

		private int position = -1;

		public ModelMesh Current => collection[position];

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

	public ModelMesh this[string meshName]
	{
		get
		{
			if (!TryGetValue(meshName, out var value))
			{
				throw new KeyNotFoundException();
			}
			return value;
		}
	}

	internal ModelMeshCollection(IList<ModelMesh> list)
		: base(list)
	{
	}

	public bool TryGetValue(string meshName, out ModelMesh value)
	{
		if (string.IsNullOrEmpty(meshName))
		{
			throw new ArgumentNullException("meshName");
		}
		using (Enumerator enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				ModelMesh current = enumerator.Current;
				if (string.Compare(current.Name, meshName, StringComparison.Ordinal) == 0)
				{
					value = current;
					return true;
				}
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
