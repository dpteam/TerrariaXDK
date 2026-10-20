using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class EffectTechniqueCollection : IEnumerable<EffectTechnique>, IEnumerable
{
	private List<EffectTechnique> elements;

	public int Count => elements.Count;

	public EffectTechnique this[int index] => elements[index];

	public EffectTechnique this[string name]
	{
		get
		{
			foreach (EffectTechnique element in elements)
			{
				if (name.Equals(element.Name))
				{
					return element;
				}
			}
			return null;
		}
	}

	internal EffectTechniqueCollection(List<EffectTechnique> value)
	{
		elements = value;
	}

	public List<EffectTechnique>.Enumerator GetEnumerator()
	{
		return elements.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return elements.GetEnumerator();
	}

	IEnumerator<EffectTechnique> IEnumerable<EffectTechnique>.GetEnumerator()
	{
		return elements.GetEnumerator();
	}
}
