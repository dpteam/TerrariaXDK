using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework;

public class CurveKeyCollection : ICollection<CurveKey>, IEnumerable<CurveKey>, IEnumerable
{
	private bool isReadOnly = false;

	private List<CurveKey> innerlist;

	public int Count => innerlist.Count;

	public bool IsReadOnly => isReadOnly;

	public CurveKey this[int index]
	{
		get
		{
			return innerlist[index];
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (index >= innerlist.Count)
			{
				throw new IndexOutOfRangeException();
			}
			if (MathHelper.WithinEpsilon(innerlist[index].Position, value.Position))
			{
				innerlist[index] = value;
				return;
			}
			innerlist.RemoveAt(index);
			innerlist.Add(value);
		}
	}

	public CurveKeyCollection()
	{
		innerlist = new List<CurveKey>();
	}

	public void Add(CurveKey item)
	{
		if (item == null)
		{
			throw new ArgumentNullException("item");
		}
		if (innerlist.Count == 0)
		{
			innerlist.Add(item);
			return;
		}
		for (int i = 0; i < innerlist.Count; i++)
		{
			if (item.Position < innerlist[i].Position)
			{
				innerlist.Insert(i, item);
				return;
			}
		}
		innerlist.Add(item);
	}

	public void Clear()
	{
		innerlist.Clear();
	}

	public CurveKeyCollection Clone()
	{
		CurveKeyCollection curveKeyCollection = new CurveKeyCollection();
		foreach (CurveKey item in innerlist)
		{
			curveKeyCollection.Add(item);
		}
		return curveKeyCollection;
	}

	public bool Contains(CurveKey item)
	{
		return innerlist.Contains(item);
	}

	public void CopyTo(CurveKey[] array, int arrayIndex)
	{
		innerlist.CopyTo(array, arrayIndex);
	}

	public IEnumerator<CurveKey> GetEnumerator()
	{
		return innerlist.GetEnumerator();
	}

	public int IndexOf(CurveKey item)
	{
		return innerlist.IndexOf(item);
	}

	public bool Remove(CurveKey item)
	{
		return innerlist.Remove(item);
	}

	public void RemoveAt(int index)
	{
		innerlist.RemoveAt(index);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return innerlist.GetEnumerator();
	}
}
