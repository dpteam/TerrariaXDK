using System;
using System.Collections.Generic;

namespace Terraria.Achievements;

public class AchievementData
{
	public class Row
	{
		public bool Available;

		public int Rank;
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

	private MockReader Reader;

	public static Dictionary<Achievement, AchievementSystem.TerrariaAchievement> CurrentAchievements;

	private IAsyncResult ReqResult;

	private AsyncRequest ReadStateReq;

	private readonly int DefaultBatchSize;

	public int BatchSize;

	public int BatchStart;

	public int Selected;

	public bool Ready => Reader != null;

	private bool CanPageDown => Reader.CanPageDown;

	private bool CanPageUp => Reader.CanPageUp;

	private static void ProcessEntries(MockReader Reader, ref Cache BoardCache)
	{
		Row[] entries = Reader.Entries;
		int num = entries.Length;
		int num2 = 0;
		Row[] array = entries;
		foreach (Row row in array)
		{
			if (row.Rank == 0)
			{
				num--;
				continue;
			}
			Row row2 = BoardCache.Entries[num2];
			row2.Rank = row.Rank;
			row2.Available = true;
			num2++;
		}
		BoardCache.StartIndex = Reader.PageStart;
		BoardCache.NumEntries = num;
	}

	public static void SetAchievements()
	{
		CurrentAchievements = Main.AchievementSystem.GetCurrentAchievements();
	}

	public AchievementSystem.TerrariaAchievement GetAchievement(Achievement Achievement)
	{
		return CurrentAchievements[Achievement];
	}

	public AchievementData(int Size)
	{
		CurrentAchievements = null;
		DefaultBatchSize = Size;
		BatchStart = 0;
		MergedCache = new Cache
		{
			Entries = new Row[0],
			StartIndex = 0
		};
	}

	public void LoadLeaderboard()
	{
		ReqResult = new CompletedAsyncResult(true);
		Reader = null;
		ReadStateReq = AsyncRequest.FullRead;
		NumEntries = 0;
		ResetCaches();
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
				Reader = MockReader.Create(39, 50);
			}
			catch
			{
				return false;
			}
			BatchStart = Reader.PageStart;
			Selected = 0;
			if (ReqResult.AsyncState != null)
			{
				bool flag = (bool)ReqResult.AsyncState;
				int num = BatchStart;
				Row[] entries = Reader.Entries;
				for (int i = 0; i < entries.Length; i++)
				{
					if (entries[i].Available == flag)
					{
						Selected = num;
						BatchStart = num;
						break;
					}
					num++;
				}
			}
		}
		ProcessEntries(Reader, ref CurrentCache);
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
				NumEntries = 39;
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
		ReadStateReq = AsyncRequest.NextPage;
	}

	private void CachePreviousPage()
	{
		int num = Math.Min(CurrentCache.NumEntries, CurrentCache.StartIndex);
		int newCacheStart = CurrentCache.StartIndex - num;
		FreeCache(newCacheStart);
		CreateMergedCache(CurrentCache, StaleCache);
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
}
