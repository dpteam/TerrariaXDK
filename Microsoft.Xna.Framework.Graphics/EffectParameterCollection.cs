using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class EffectParameterCollection : IEnumerable<EffectParameter>, IEnumerable
{
	private List<EffectParameter> elements;

	public int Count => elements.Count;

	public EffectParameter this[int index] => elements[index];

	public EffectParameter this[string name]
	{
		get
		{
			foreach (EffectParameter element in elements)
			{
				if (name.Equals(element.Name))
				{
					return element;
				}
			}
			return null;
		}
	}

	internal EffectParameterCollection(List<EffectParameter> value)
	{
		elements = value;
	}

	public List<EffectParameter>.Enumerator GetEnumerator()
	{
		return elements.GetEnumerator();
	}

	public EffectParameter GetParameterBySemantic(string semantic)
	{
		foreach (EffectParameter element in elements)
		{
			if (semantic.Equals(element.Semantic))
			{
				return element;
			}
		}
		return null;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return elements.GetEnumerator();
	}

	IEnumerator<EffectParameter> IEnumerable<EffectParameter>.GetEnumerator()
	{
		return elements.GetEnumerator();
	}
}
