using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class ModelEffectCollection : ReadOnlyCollection<Effect>
{
	public struct Enumerator : IEnumerator<Effect>, IDisposable, IEnumerator
	{
		private List<Effect>.Enumerator enumerator;

		private bool disposed;

		public Effect Current => enumerator.Current;

		object IEnumerator.Current => Current;

		internal Enumerator(List<Effect> list)
		{
			enumerator = list.GetEnumerator();
			disposed = false;
		}

		public void Dispose()
		{
			if (!disposed)
			{
				enumerator.Dispose();
				disposed = true;
			}
		}

		public bool MoveNext()
		{
			return enumerator.MoveNext();
		}

		void IEnumerator.Reset()
		{
			IEnumerator enumerator = this.enumerator;
			enumerator.Reset();
			this.enumerator = (List<Effect>.Enumerator)(object)enumerator;
		}
	}

	internal ModelEffectCollection(IList<Effect> list)
		: base(list)
	{
	}

	internal ModelEffectCollection()
		: base((IList<Effect>)new List<Effect>())
	{
	}

	public new Enumerator GetEnumerator()
	{
		return new Enumerator((List<Effect>)Items);
	}

	internal void Add(Effect item)
	{
		Items.Add(item);
	}

	internal void Remove(Effect item)
	{
		Items.Remove(item);
	}
}
