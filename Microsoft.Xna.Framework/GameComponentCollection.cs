using System;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework;

public sealed class GameComponentCollection : Collection<IGameComponent>
{
	public event EventHandler<GameComponentCollectionEventArgs> ComponentAdded;

	public event EventHandler<GameComponentCollectionEventArgs> ComponentRemoved;

	protected override void ClearItems()
	{
		for (int i = 0; i < Count; i++)
		{
			OnComponentRemoved(new GameComponentCollectionEventArgs(base[i]));
		}
		base.ClearItems();
	}

	protected override void InsertItem(int index, IGameComponent item)
	{
		if (IndexOf(item) != -1)
		{
			throw new ArgumentException("Cannot Add Same Component Multiple Times");
		}
		base.InsertItem(index, item);
		if (item != null)
		{
			OnComponentAdded(new GameComponentCollectionEventArgs(item));
		}
	}

	protected override void RemoveItem(int index)
	{
		IGameComponent gameComponent = base[index];
		base.RemoveItem(index);
		if (gameComponent != null)
		{
			OnComponentRemoved(new GameComponentCollectionEventArgs(gameComponent));
		}
	}

	protected override void SetItem(int index, IGameComponent item)
	{
		throw new NotSupportedException();
	}

	private void OnComponentAdded(GameComponentCollectionEventArgs eventArgs)
	{
		if (ComponentAdded != null)
		{
			ComponentAdded(this, eventArgs);
		}
	}

	private void OnComponentRemoved(GameComponentCollectionEventArgs eventArgs)
	{
		if (ComponentRemoved != null)
		{
			ComponentRemoved(this, eventArgs);
		}
	}
}
