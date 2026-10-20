using System;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.GamerServices;

namespace Terraria.Leaderboards;

public class LeaderboardData
{
	public class Row
	{
		public bool Available;

		public int Rank;

		public uint[] Statistics;

		public Gamer Gamer;

		public string Gamertag => Gamer.Gamertag;
	}

	private class Cache
	{
		public Row[] Entries;

		public int StartIndex;

		public int NumEntries;
	}

	private enum AsyncRequest
	{
		None,
		FullRead,
		NextPage,
		PreviousPage
	}

	private const int PageSize = 50;

	public int NumEntries;

	private Cache StaleCache;

	private Cache CurrentCache;

	private Cache MergedCache;

	private LeaderboardReader Reader;

	private IAsyncResult ReqResult;

	private AsyncRequest ReadStateReq;

	private readonly int DefaultBatchSize;

	public int BatchSize;

	public int BatchStart;

	public int Selected;

	public Column[] Columns;

	public string LeaderboardName;

	public bool Ready => Reader != null;

	private bool CanPageDown => Reader.CanPageDown;

	private bool CanPageUp => Reader.CanPageUp;

	private static void ProcessEntries(LeaderboardReader Reader, Column[] Columns, ref Cache BoardCache)
	{
		ReadOnlyCollection<LeaderboardEntry> entries = Reader.Entries;
		int num = entries.Count;
		int num2 = 0;
		int num3 = Columns.Length;
		string[] array = new string[num3];
		for (int num4 = num3 - 1; num4 > -1; num4--)
		{
			array[num4] = Columns[num4].ToString();
		}
		for (int i = 0; i < entries.Count; i++)
		{
			LeaderboardEntry leaderboardEntry = entries[i];
			if (leaderboardEntry.RankingEXT == 0)
			{
				num--;
				continue;
			}
			Row row = BoardCache.Entries[num2];
			row.Rank = leaderboardEntry.RankingEXT;
			row.Gamer = leaderboardEntry.Gamer;
			row.Statistics = new uint[num3];
			for (int num5 = num3 - 1; num5 > -1; num5--)
			{
				// Columns are optional: a board entry that does not carry them (or a local
				// synthetic board) must render as zeros instead of throwing.
				object obj;
				row.Statistics[num5] = leaderboardEntry.Columns.TryGetValue(array[num5], out obj) && obj != null ? (uint)obj : 0u;
			}
			row.Available = true;
			num2++;
		}
		BoardCache.StartIndex = Reader.PageStart;
		BoardCache.NumEntries = num;
	}

	public LeaderboardData(int Size)
	{
		DefaultBatchSize = Size;
		BatchStart = 0;
		MergedCache = new Cache
		{
			Entries = new Row[0],
			StartIndex = 0
		};
	}

	public void LoadLeaderboard(Leaderboard BoardType)
	{
		LeaderboardIdentity identity = LeaderboardInfo.GetIdentity(BoardType);
		ReqResult = LeaderboardReader.BeginRead(identity, 0, 50, null, null);
		Reader = null;
		ReadStateReq = AsyncRequest.FullRead;
		NumEntries = 0;
		ResetCaches();
		Columns = LeaderboardInfo.GetColumns(BoardType);
		LeaderboardName = LeaderboardInfo.GetName(BoardType);
	}

	public void LoadLeaderboard(Leaderboard BoardType, Gamer CurrentGamer)
	{
		LeaderboardIdentity identity = LeaderboardInfo.GetIdentity(BoardType);
		ReqResult = LeaderboardReader.BeginRead(identity, CurrentGamer, 50, null, CurrentGamer);
		Reader = null;
		ReadStateReq = AsyncRequest.FullRead;
		NumEntries = 0;
		ResetCaches();
		Columns = LeaderboardInfo.GetColumns(BoardType);
		LeaderboardName = LeaderboardInfo.GetName(BoardType);
	}

	public void LoadLeaderboard(Leaderboard BoardType, FriendCollection FriendsList, Gamer CurrentGamer)
	{
		LeaderboardIdentity identity = LeaderboardInfo.GetIdentity(BoardType);
		ReqResult = LeaderboardReader.BeginRead(identity, FriendsList, CurrentGamer, FriendsList.Count + 1, null, CurrentGamer);
		Reader = null;
		ReadStateReq = AsyncRequest.FullRead;
		NumEntries = 0;
		ResetCaches();
		Columns = LeaderboardInfo.GetColumns(BoardType);
		LeaderboardName = LeaderboardInfo.GetName(BoardType);
	}

	private void ResetCaches()
	{
		Row[] array = new Row[50];
		Row[] array2 = new Row[50];
		for (int num = 49; num >= 0; num--)
		{
			array[num] = new Row
			{
				Available = false
			};
			array2[num] = new Row
			{
				Available = false
			};
		}
		CurrentCache = new Cache
		{
			Entries = array,
			StartIndex = 0,
			NumEntries = 0
		};
		StaleCache = new Cache
		{
			Entries = array2,
			StartIndex = 0,
			NumEntries = 0
		};
		MergedCache = new Cache
		{
			Entries = array,
			StartIndex = 0,
			NumEntries = 0
		};
		BatchStart = 0;
	}

	public bool Update()
	{
		if (ReadStateReq == AsyncRequest.None || !ReqResult.IsCompleted)
		{
			return true;
		}
		if (ReadStateReq == AsyncRequest.FullRead)
		{
			try
			{
				Reader = LeaderboardReader.EndRead(ReqResult);
			}
			catch
			{
				return false;
			}
			BatchStart = Reader.PageStart;
			Selected = 0;
			if (ReqResult.AsyncState != null)
			{
				Gamer gamer = (Gamer)ReqResult.AsyncState;
				int num = BatchStart;
				foreach (LeaderboardEntry entry in Reader.Entries)
				{
					if (entry.Gamer.Gamertag == gamer.Gamertag)
					{
						Selected = num;
						BatchStart = num;
						break;
					}
					num++;
				}
			}
		}
		else if (ReadStateReq == AsyncRequest.NextPage)
		{
			Reader.EndPageDown(ReqResult);
		}
		else if (ReadStateReq == AsyncRequest.PreviousPage)
		{
			Reader.EndPageUp(ReqResult);
		}
		ProcessEntries(Reader, Columns, ref CurrentCache);
		if (StaleCache.StartIndex <= CurrentCache.StartIndex)
		{
			CreateMergedCache(StaleCache, CurrentCache);
		}
		else
		{
			CreateMergedCache(CurrentCache, StaleCache);
		}
		if (ReadStateReq == AsyncRequest.FullRead)
		{
			BatchSize = Math.Min(CurrentCache.NumEntries, DefaultBatchSize);
			if (CurrentCache.NumEntries > 0 && (Reader.CanPageDown || Reader.CanPageUp))
			{
				NumEntries = Reader.TotalLeaderboardSize;
			}
			else
			{
				NumEntries = CurrentCache.NumEntries;
			}
			if (BatchStart > NumEntries - BatchSize)
			{
				BatchStart = NumEntries - BatchSize;
			}
		}
		ReadStateReq = AsyncRequest.None;
		ReqResult = null;
		return true;
	}

	public bool MoveDown()
	{
		Selected++;
		if (Selected < BatchStart + BatchSize)
		{
			return true;
		}
		BatchStart++;
		bool flag = CurrentCache.StartIndex + CurrentCache.NumEntries < BatchStart + BatchSize;
		if (flag && !CanPageDown && ReadStateReq == AsyncRequest.None)
		{
			Selected--;
			BatchStart--;
			return false;
		}
		bool result = true;
		if (flag)
		{
			if (ReadStateReq == AsyncRequest.None)
			{
				CacheNextPage();
			}
			else
			{
				BatchStart--;
				Selected--;
				result = false;
			}
		}
		return result;
	}

	public bool MoveUp()
	{
		Selected--;
		if (Selected >= BatchStart)
		{
			return true;
		}
		BatchStart--;
		bool flag = BatchStart < CurrentCache.StartIndex;
		if (flag && !CanPageUp && ReadStateReq == AsyncRequest.None)
		{
			Selected++;
			BatchStart++;
			return false;
		}
		bool result = true;
		if (flag)
		{
			if (ReadStateReq == AsyncRequest.None)
			{
				CachePreviousPage();
			}
			else
			{
				BatchStart++;
				Selected++;
				result = false;
			}
		}
		return result;
	}

	private void CacheNextPage()
	{
		int newCacheStart = CurrentCache.StartIndex + CurrentCache.NumEntries;
		FreeCache(newCacheStart);
		CreateMergedCache(StaleCache, CurrentCache);
		ReqResult = Reader.BeginPageDown(null, null);
		ReadStateReq = AsyncRequest.NextPage;
	}

	private void CachePreviousPage()
	{
		int num = Math.Min(CurrentCache.NumEntries, CurrentCache.StartIndex);
		int newCacheStart = CurrentCache.StartIndex - num;
		FreeCache(newCacheStart);
		CreateMergedCache(CurrentCache, StaleCache);
		ReqResult = Reader.BeginPageUp(null, null);
		ReadStateReq = AsyncRequest.PreviousPage;
	}

	private void FreeCache(int NewCacheStart)
	{
		Cache currentCache = CurrentCache;
		CurrentCache = StaleCache;
		StaleCache = currentCache;
		if (CurrentCache.StartIndex != NewCacheStart)
		{
			CurrentCache.StartIndex = NewCacheStart;
			for (int i = 0; i < CurrentCache.NumEntries; i++)
			{
				CurrentCache.Entries[i].Available = false;
			}
		}
	}

	private void CreateMergedCache(Cache FirstCache, Cache SecondCache)
	{
		int num = SecondCache.StartIndex - FirstCache.StartIndex + SecondCache.NumEntries;
		Cache cache = new Cache();
		cache.Entries = new Row[num];
		cache.StartIndex = FirstCache.StartIndex;
		cache.NumEntries = num;
		Cache cache2 = cache;
		for (int num2 = FirstCache.NumEntries - 1; num2 >= 0; num2--)
		{
			cache2.Entries[num2] = FirstCache.Entries[num2];
		}
		int num3 = SecondCache.StartIndex - FirstCache.StartIndex;
		for (int num4 = SecondCache.NumEntries - 1; num4 >= 0; num4--)
		{
			Row row = cache2.Entries[num3 + num4];
			if (row == null || !row.Available)
			{
				cache2.Entries[num3 + num4] = SecondCache.Entries[num4];
			}
		}
		MergedCache = cache2;
	}

	public Row[] GetRows()
	{
		int num = Math.Min(BatchSize, MergedCache.NumEntries);
		int num2 = BatchStart - MergedCache.StartIndex;
		if (num2 < 0)
		{
			num2 = 0;
		}
		if (num2 + num > MergedCache.NumEntries)
		{
			num2 = MergedCache.NumEntries - num;
		}
		Row[] Destination = new Row[num];
		CopyRows(ref MergedCache.Entries, num2, ref Destination, 0, num);
		return Destination;
	}

	private void CopyRows(ref Row[] Source, int SourceIdx, ref Row[] Destination, int DestinationIdx, int RowCount)
	{
		for (int num = RowCount - 1; num >= 0; num--)
		{
			Destination[DestinationIdx + num] = Source[SourceIdx + num];
		}
	}

	public Gamer GetSelectedGamer()
	{
		int num = Selected - MergedCache.StartIndex;
		if (num >= 0 && num < MergedCache.NumEntries)
		{
			return MergedCache.Entries[num].Gamer;
		}
		return null;
	}
}
