using System;

namespace Terraria.Leaderboards;

public class MockReader
{
	private const int SEED = 1234567;

	private readonly int PageSize;

	private readonly LeaderboardData.Row[] Database;

	public int PageStart;

	public bool Loaded;

	public bool CanPageDown => PageStart + PageSize < TotalLeaderboardSize;

	public bool CanPageUp => PageStart > 0;

	public int TotalLeaderboardSize
	{
		get
		{
			if (!Loaded)
			{
				return 0;
			}
			return Database.Length;
		}
	}

	public LeaderboardData.Row[] Entries
	{
		get
		{
			LeaderboardData.Row[] array = new LeaderboardData.Row[PageSize];
			for (int num = PageSize - 1; num >= 0; num--)
			{
				array[num] = Database[PageStart + num];
			}
			return array;
		}
	}

	public static MockReader Create(int NumEntries, int PageSize, int NumColumns)
	{
		FastRandom fastRandom = new FastRandom(1234567u);
		LeaderboardData.Row[] array = new LeaderboardData.Row[NumEntries];
		for (int num = NumEntries - 1; num >= 0; num--)
		{
			array[num] = new LeaderboardData.Row
			{
				Available = true,
				Rank = num + 1,
				Statistics = new uint[NumColumns]
			};
			for (int i = 0; i < NumColumns; i++)
			{
				array[num].Statistics[i] = (uint)fastRandom.Next(109999998);
			}
		}
		return new MockReader(array, 0, Math.Min(NumEntries, PageSize));
	}

	private MockReader(LeaderboardData.Row[] Database, int PageStart, int PageSize)
	{
		this.Database = Database;
		this.PageSize = PageSize;
		this.PageStart = PageStart;
	}

	public void PageDown()
	{
		PageStart = Math.Min(PageStart + PageSize, TotalLeaderboardSize - PageSize);
	}

	public void PageUp()
	{
		PageStart = Math.Max(PageStart - PageSize, 0);
	}
}
