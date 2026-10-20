using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.GamerServices;

public sealed class LeaderboardReader : IDisposable
{
	private int pageSize;

	private List<LeaderboardEntry> entries;

	private List<LeaderboardEntry> entryCache;

	private bool isFriendBoard;

	public bool IsDisposed { get; private set; }

	public bool CanPageDown
	{
		get
		{
			if (entryCache.Count == 0)
			{
				return false;
			}
			if (isFriendBoard)
			{
				return PageStart + pageSize < entryCache.Count;
			}
			if (PageStart >= entryCache.Count)
			{
				return entryCache[entryCache.Count - 1].RankingEXT < TotalLeaderboardSize;
			}
			return true;
		}
	}

	public bool CanPageUp
	{
		get
		{
			if (entryCache.Count == 0)
			{
				return false;
			}
			if (isFriendBoard)
			{
				return PageStart - pageSize >= 0;
			}
			if (PageStart <= 0)
			{
				return entryCache[0].RankingEXT > 1;
			}
			return true;
		}
	}

	public ReadOnlyCollection<LeaderboardEntry> Entries => new ReadOnlyCollection<LeaderboardEntry>(entries);

	public LeaderboardIdentity LeaderboardIdentity { get; private set; }

	public int PageStart { get; private set; }

	public int TotalLeaderboardSize { get; private set; }

	internal LeaderboardReader(LeaderboardIdentity identity, int start, int size, List<LeaderboardEntry> entries, bool friends)
	{
		LeaderboardIdentity = identity;
		PageStart = start;
		pageSize = size;
		TotalLeaderboardSize = 0;
		isFriendBoard = friends;
		entryCache = entries;
		this.entries = new List<LeaderboardEntry>(pageSize);
		for (int i = PageStart; i < pageSize && i < entryCache.Count; i++)
		{
			this.entries.Add(entryCache[i]);
		}
		IsDisposed = false;
	}

	public void Dispose()
	{
		IsDisposed = true;
	}

	public void PageDown()
	{
		IAsyncResult asyncResult = BeginPageDown(null, null);
		while (!asyncResult.IsCompleted)
		{
			GamerServicesDispatcher.UpdateAsync();
		}
		EndPageDown(asyncResult);
	}

	public IAsyncResult BeginPageDown(AsyncCallback callback, object asyncState)
	{
		throw new NotSupportedException();
	}

	public void EndPageDown(IAsyncResult result)
	{
		throw new NotSupportedException();
	}

	public void PageUp()
	{
		IAsyncResult asyncResult = BeginPageUp(null, null);
		while (!asyncResult.IsCompleted)
		{
			GamerServicesDispatcher.UpdateAsync();
		}
		EndPageUp(asyncResult);
	}

	public IAsyncResult BeginPageUp(AsyncCallback callback, object asyncState)
	{
		throw new NotSupportedException();
	}

	public void EndPageUp(IAsyncResult result)
	{
		throw new NotSupportedException();
	}

	public static LeaderboardReader Read(LeaderboardIdentity leaderboardId, int pageStart, int pageSize)
	{
		IAsyncResult asyncResult = BeginRead(leaderboardId, pageStart, pageSize, null, null);
		while (!asyncResult.IsCompleted)
		{
			GamerServicesDispatcher.UpdateAsync();
		}
		return EndRead(asyncResult);
	}

	public static LeaderboardReader Read(LeaderboardIdentity leaderboardId, Gamer pivotGamer, int pageSize)
	{
		IAsyncResult asyncResult = BeginRead(leaderboardId, pivotGamer, pageSize, null, null);
		while (!asyncResult.IsCompleted)
		{
			GamerServicesDispatcher.UpdateAsync();
		}
		return EndRead(asyncResult);
	}

	public static LeaderboardReader Read(LeaderboardIdentity leaderboardId, IEnumerable<Gamer> gamers, Gamer pivotGamer, int pageSize)
	{
		IAsyncResult asyncResult = BeginRead(leaderboardId, gamers, pivotGamer, pageSize, null, null);
		while (!asyncResult.IsCompleted)
		{
			GamerServicesDispatcher.UpdateAsync();
		}
		return EndRead(asyncResult);
	}

	public static IAsyncResult BeginRead(LeaderboardIdentity leaderboardId, int pageStart, int pageSize, AsyncCallback callback, object asyncState)
	{
		throw new NotSupportedException();
	}

	public static IAsyncResult BeginRead(LeaderboardIdentity leaderboardId, Gamer pivotGamer, int pageSize, AsyncCallback callback, object asyncState)
	{
		throw new NotSupportedException();
	}

	public static IAsyncResult BeginRead(LeaderboardIdentity leaderboardId, IEnumerable<Gamer> gamers, Gamer pivotGamer, int pageSize, AsyncCallback callback, object asyncState)
	{
		throw new NotSupportedException();
	}

	public static LeaderboardReader EndRead(IAsyncResult result)
	{
		throw new NotSupportedException();
	}
}
