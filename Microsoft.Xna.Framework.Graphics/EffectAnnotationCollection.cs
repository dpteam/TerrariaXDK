using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class EffectAnnotationCollection : IEnumerable<EffectAnnotation>, IEnumerable
{
	private List<EffectAnnotation> elements;

	public int Count => elements.Count;

	public EffectAnnotation this[int index] => elements[index];

	public EffectAnnotation this[string name]
	{
		get
		{
			foreach (EffectAnnotation element in elements)
			{
				if (name.Equals(element.Name))
				{
					return element;
				}
			}
			return null;
		}
	}

	internal EffectAnnotationCollection(List<EffectAnnotation> value)
	{
		elements = value;
	}

	public List<EffectAnnotation>.Enumerator GetEnumerator()
	{
		return elements.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return elements.GetEnumerator();
	}

	IEnumerator<EffectAnnotation> IEnumerable<EffectAnnotation>.GetEnumerator()
	{
		return elements.GetEnumerator();
	}
}
