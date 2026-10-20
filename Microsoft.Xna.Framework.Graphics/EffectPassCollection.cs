using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class EffectPassCollection : IEnumerable<EffectPass>, IEnumerable
{
	private List<EffectPass> elements;

	public int Count => elements.Count;

	public EffectPass this[int index] => elements[index];

	public EffectPass this[string name]
	{
		get
		{
			foreach (EffectPass element in elements)
			{
				if (name.Equals(element.Name))
				{
					return element;
				}
			}
			return null;
		}
	}

	internal EffectPassCollection(List<EffectPass> value)
	{
		elements = value;
	}

	public List<EffectPass>.Enumerator GetEnumerator()
	{
		return elements.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return elements.GetEnumerator();
	}

	IEnumerator<EffectPass> IEnumerable<EffectPass>.GetEnumerator()
	{
		return elements.GetEnumerator();
	}
}
