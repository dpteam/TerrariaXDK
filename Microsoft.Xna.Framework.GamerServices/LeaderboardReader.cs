using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace Microsoft.Xna.Framework.GamerServices;

/// <summary>
/// Local stand-in for the Xbox Live leaderboard service.
///
/// The original class only held the shape of the API; every entry point threw
/// <see cref="NotSupportedException"/>, so opening the leaderboards screen crashed the game.
/// There is no Live service to talk to in this port, so the reader now serves a deterministic
/// synthetic board: it keeps the UI fully functional (paging, selection, gamer cards) without
/// pretending to be real data. Wiring it to a real backend (the master server) is tracked in
/// IDEAS.md.
/// </summary>
public sealed class LeaderboardReader : IDisposable
{
	/// <summary>Completed IAsyncResult, so the callers' polling loops exit immediately.</summary>
	private sealed class CompletedResult : IAsyncResult
	{
		public object AsyncState { get; private set; }

		public bool CompletedSynchronously => true;

		public bool IsCompleted => true;

		public WaitHandle AsyncWaitHandle { get; private set; }

		public readonly LeaderboardIdentity Identity;

		public readonly int PageStart;

		public readonly int PageSize;

		public CompletedResult(object state, LeaderboardIdentity identity, int pageStart, int pageSize)
		{
			AsyncState = state;
			Identity = identity;
			PageStart = pageStart;
			PageSize = pageSize;
			AsyncWaitHandle = new ManualResetEvent(initialState: true);
		}
	}

	/// <summary>How many placeholder rows a synthetic board has.</summary>
	private const int SyntheticBoardSize = 500;

	private const int MaxPageSize = 50;

	private int pageSize;

	private List<LeaderboardEntry> entries;

	private List<LeaderboardEntry> entryCache;

	private bool isFriendBoard;

	public bool IsDisposed { get; private set; }

	public bool CanPageDown
	{
		get
		{
			if (PageStart + pageSize < entryCache.Count)
			{
				return true;
			}
			return PageStart + pageSize >= entryCache.Count && entryCache.Count > 0 && PageStart + pageSize < TotalLeaderboardSize;
		}
	}

	public bool CanPageUp => PageStart > 0;

	public ReadOnlyCollection<LeaderboardEntry> Entries => new ReadOnlyCollection<LeaderboardEntry>(entries);

	public LeaderboardIdentity LeaderboardIdentity { get; private set; }

	public int PageStart { get; private set; }

	public int TotalLeaderboardSize { get; private set; }

	internal LeaderboardReader(LeaderboardIdentity identity, int start, int size, List<LeaderboardEntry> entries, bool friends)
	{
		LeaderboardIdentity = identity;
		PageStart = start;
		pageSize = size;
		isFriendBoard = friends;
		entryCache = entries;
		TotalLeaderboardSize = entries.Count;
		this.entries = new List<LeaderboardEntry>(pageSize);
		RebuildPage();
		IsDisposed = false;
	}

	private void RebuildPage()
	{
		entries.Clear();
		int num = Math.Min(PageStart + pageSize, entryCache.Count);
		for (int i = PageStart; i < num; i++)
		{
			entries.Add(entryCache[i]);
		}
	}

	public void Dispose()
	{
		IsDisposed = true;
	}

	// --- Paging -------------------------------------------------------------

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
		if (!CanPageDown)
		{
			throw new InvalidOperationException("Already on the last page.");
		}
		PageStart = Math.Min(PageStart + pageSize, Math.Max(0, TotalLeaderboardSize - pageSize));
		RebuildPage();
		CompletedResult completedResult = new CompletedResult(asyncState, LeaderboardIdentity, PageStart, pageSize);
		callback?.Invoke(completedResult);
		return completedResult;
	}

	public void EndPageDown(IAsyncResult result)
	{
		if (result == null)
		{
			throw new ArgumentNullException("result");
		}
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
		if (!CanPageUp)
		{
			throw new InvalidOperationException("Already on the first page.");
		}
		PageStart = Math.Max(PageStart - pageSize, 0);
		RebuildPage();
		CompletedResult completedResult = new CompletedResult(asyncState, LeaderboardIdentity, PageStart, pageSize);
		callback?.Invoke(completedResult);
		return completedResult;
	}

	public void EndPageUp(IAsyncResult result)
	{
		if (result == null)
		{
			throw new ArgumentNullException("result");
		}
	}

	// --- Reading ------------------------------------------------------------

	public static LeaderboardReader Read(LeaderboardIdentity leaderboardId, int pageStart, int pageSize)
	{
		return EndRead(BeginRead(leaderboardId, pageStart, pageSize, null, null));
	}

	public static LeaderboardReader Read(LeaderboardIdentity leaderboardId, Gamer pivotGamer, int pageSize)
	{
		return EndRead(BeginRead(leaderboardId, pivotGamer, pageSize, null, null));
	}

	public static LeaderboardReader Read(LeaderboardIdentity leaderboardId, IEnumerable<Gamer> gamers, Gamer pivotGamer, int pageSize)
	{
		return EndRead(BeginRead(leaderboardId, gamers, pivotGamer, pageSize, null, null));
	}

	public static IAsyncResult BeginRead(LeaderboardIdentity leaderboardId, int pageStart, int pageSize, AsyncCallback callback, object asyncState)
	{
		return Complete(callback, asyncState, leaderboardId, pageStart, pageSize, friends: false);
	}

	public static IAsyncResult BeginRead(LeaderboardIdentity leaderboardId, Gamer pivotGamer, int pageSize, AsyncCallback callback, object asyncState)
	{
		// A pivot read centres the page on the gamer; the synthetic board simply starts at 0.
		return Complete(callback, asyncState, leaderboardId, 0, pageSize, friends: false);
	}

	public static IAsyncResult BeginRead(LeaderboardIdentity leaderboardId, IEnumerable<Gamer> gamers, Gamer pivotGamer, int pageSize, AsyncCallback callback, object asyncState)
	{
		return Complete(callback, asyncState, leaderboardId, 0, pageSize, friends: true);
	}

	private static IAsyncResult Complete(AsyncCallback callback, object asyncState, LeaderboardIdentity identity, int pageStart, int pageSize, bool friends)
	{
		CompletedResult completedResult = new CompletedResult(asyncState, identity, pageStart, pageSize);
		callback?.Invoke(completedResult);
		return completedResult;
	}

	public static LeaderboardReader EndRead(IAsyncResult result)
	{
		CompletedResult completedResult = result as CompletedResult;
		if (completedResult == null)
		{
			throw new ArgumentException("result was not produced by BeginRead.", "result");
		}
		List<LeaderboardEntry> list = BuildSyntheticBoard(completedResult.Identity, completedResult.PageSize);
		return new LeaderboardReader(completedResult.Identity, completedResult.PageStart, completedResult.PageSize, list, friends: false);
	}

	/// <summary>
	/// Deterministic placeholder rows. Values are stable for a given board key so paging does
	/// not shuffle the list, which is what makes the UI testable by eye.
	/// </summary>
	private static List<LeaderboardEntry> BuildSyntheticBoard(LeaderboardIdentity identity, int pageSize)
	{
		int num = Math.Max(MaxPageSize, Math.Min(pageSize, MaxPageSize)) * 10;
		if (num < MaxPageSize)
		{
			num = MaxPageSize;
		}
		num = SyntheticBoardSize;
		List<LeaderboardEntry> list = new List<LeaderboardEntry>(num);
		uint num2 = 2166136261u;
		string text = string.IsNullOrEmpty(identity.Key) ? "leaderboard" : identity.Key;
		for (int i = 0; i < text.Length; i++)
		{
			num2 = (num2 ^ text[i]) * 16777619u;
		}
		for (int j = 0; j < num; j++)
		{
			uint num3 = num2 ^ (uint)(j * 2654435761u);
			LeaderboardEntry leaderboardEntry = new LeaderboardEntry(new SignedInGamer("Player " + (j + 1)), 0L, j + 1);
			leaderboardEntry.Rating = 109999998 - j * (int)(num3 % 7919);
			list.Add(leaderboardEntry);
		}
		return list;
	}
}
