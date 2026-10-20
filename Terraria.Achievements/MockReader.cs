using System;

namespace Terraria.Achievements;

public class MockReader
{
	private readonly int PageSize;

	private readonly AchievementData.Row[] Database;

	public int PageStart;

	public bool Loaded;

	public bool CanPageDown => PageStart + PageSize < Database.Length;

	public bool CanPageUp => PageStart > 0;

	public AchievementData.Row[] Entries
	{
		get
		{
			AchievementData.Row[] array = new AchievementData.Row[PageSize];
			for (int num = PageSize - 1; num >= 0; num--)
			{
				array[num] = Database[PageStart + num];
			}
			return array;
		}
	}

	public static MockReader Create(int NumEntries, int PageSize)
	{
		AchievementData.Row[] array = new AchievementData.Row[NumEntries];
		for (int num = NumEntries - 1; num >= 0; num--)
		{
			array[num] = new AchievementData.Row
			{
				Available = true,
				Rank = num + 1
			};
		}
		return new MockReader(array, 0, Math.Min(NumEntries, PageSize));
	}

	private MockReader(AchievementData.Row[] Database, int PageStart, int PageSize)
	{
		this.Database = Database;
		this.PageSize = PageSize;
		this.PageStart = PageStart;
	}
}
