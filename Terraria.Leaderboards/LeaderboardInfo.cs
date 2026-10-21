using System.Text;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

namespace Terraria.Leaderboards;

internal class LeaderboardInfo
{
	private enum TitleTextID
	{
		DISTANCE = 58,
		MINING_GATHERING,
		CRAFTING,
		USED,
		NORMAL_BOSSES,
		HARD_BOSSES,
		DEATHS
	}

	private class ColumnMapping
	{
		public StatisticEntry Statistic;

		public Column ColumnType;

		public string ColumnName;

		public ColumnMapping(StatisticEntry Entry, Column Column, string Name)
		{
			Statistic = Entry;
			ColumnType = Column;
			ColumnName = Name;
		}
	}

	private readonly LeaderboardIdentity Identity;

	private readonly ColumnMapping[] Columns;

	private readonly int NameID;

	private static readonly LeaderboardInfo[] LeaderboardsInfo = CreateAll();

	private static ColumnMapping Map(StatisticEntry Entry, Column Column)
	{
		string name = Column.ToString();
		return new ColumnMapping(Entry, Column, name);
	}

	private static LeaderboardIdentity CreateId(string Key)
	{
		return new LeaderboardIdentity
		{
			Key = Key,
			GameMode = 0
		};
	}

	private LeaderboardInfo(LeaderboardIdentity Identity, ColumnMapping[] Columns, TitleTextID NameID)
	{
		this.Identity = Identity;
		this.Columns = Columns;
		this.NameID = (int)NameID;
	}

	public static LeaderboardInfo[] CreateAll()
	{
		LeaderboardInfo[] array = new LeaderboardInfo[5];
		ColumnMapping[] columns = new ColumnMapping[4]
		{
			Map(StatisticEntry.AirTravel, Column.AIR_COLUMN),
			Map(StatisticEntry.GroundTravel, Column.GROUND_COLUMN),
			Map(StatisticEntry.WaterTravel, Column.WATER_COLUMN),
			Map(StatisticEntry.LavaTravel, Column.LAVA_COLUMN)
		};
		array[0] = new LeaderboardInfo(CreateId("Distance"), columns, TitleTextID.DISTANCE);
		columns = new ColumnMapping[4]
		{
			Map(StatisticEntry.Ore, Column.ORE_COLUMN),
			Map(StatisticEntry.Gems, Column.GEMS_COLUMN),
			Map(StatisticEntry.Soils, Column.SOILS_COLUMN),
			Map(StatisticEntry.Wood, Column.WOOD_COLUMN)
		};
		array[1] = new LeaderboardInfo(CreateId("Mining"), columns, TitleTextID.MINING_GATHERING);
		columns = new ColumnMapping[6]
		{
			Map(StatisticEntry.FurnitureCrafted, Column.FURNITURE_COLUMN),
			Map(StatisticEntry.ToolsCrafted, Column.TOOLS_COLUMN),
			Map(StatisticEntry.WeaponsCrafted, Column.WEAPONS_COLUMN),
			Map(StatisticEntry.ArmorCrafted, Column.ARMOR_COLUMN),
			Map(StatisticEntry.ConsumablesCrafted, Column.CONSUMABLES_COLUMN),
			Map(StatisticEntry.MiscCrafted, Column.MISC_COLUMN)
		};
		array[2] = new LeaderboardInfo(CreateId("Crafting"), columns, TitleTextID.CRAFTING);
		columns = new ColumnMapping[5]
		{
			Map(StatisticEntry.KingSlime, Column.KING_SLIME_COLUMN),
			Map(StatisticEntry.EyeOfCthulhu, Column.CTHULHU_COLUMN),
			Map(StatisticEntry.EaterOfWorlds, Column.EATER_COLUMN),
			Map(StatisticEntry.Skeletron, Column.SKELETRON_COLUMN),
			Map(StatisticEntry.WallOfFlesh, Column.WALL_COLUMN)
		};
		array[3] = new LeaderboardInfo(CreateId("Normal bosses"), columns, TitleTextID.NORMAL_BOSSES);
		columns = new ColumnMapping[4]
		{
			Map(StatisticEntry.TheTwins, Column.TWINS_COLUMN),
			Map(StatisticEntry.TheDestroyer, Column.DESTROYER_COLUMN),
			Map(StatisticEntry.SkeletronPrime, Column.SKELETRON_PRIME_COLUMN),
			Map(StatisticEntry.Ocram, Column.OCRAM_COLUMN)
		};
		array[4] = new LeaderboardInfo(CreateId("Hard bosses"), columns, TitleTextID.HARD_BOSSES);
		return array;
	}

	public static LeaderboardIdentity GetIdentity(Leaderboard BoardType)
	{
		return LeaderboardsInfo[(int)BoardType].Identity;
	}

	public static Column[] GetColumns(Leaderboard BoardType)
	{
		LeaderboardInfo leaderboardInfo = LeaderboardsInfo[(int)BoardType];
		Column[] array = new Column[leaderboardInfo.Columns.Length];
		for (int num = leaderboardInfo.Columns.Length - 1; num > -1; num--)
		{
			array[num] = leaderboardInfo.Columns[num].ColumnType;
		}
		return array;
	}

	public static string GetName(Leaderboard BoardType)
	{
		LeaderboardInfo leaderboardInfo = LeaderboardsInfo[(int)BoardType];
		return Lang.MenuText[leaderboardInfo.NameID];
	}

	public static void SubmitStatistics(Statistics Stats, NetworkGamer Gamer)
	{
		if (Netplay.Session.SessionState != NetworkSessionState.Playing)
		{
			return;
		}
		LeaderboardWriter leaderboardWriter = Gamer.LeaderboardWriter;
		if (leaderboardWriter != null)
		{
			LeaderboardInfo[] leaderboardsInfo = LeaderboardsInfo;
			for (int i = 0; i < leaderboardsInfo.Length; i++)
			{
				leaderboardsInfo[i].Submit(leaderboardWriter, Stats);
			}
		}
	}

	public static void SubmitStatisticsToLeaderboard(Leaderboard BoardType, Statistics Stats, Gamer CurrentGamer)
	{
		LeaderboardWriter leaderboardWriter = CurrentGamer.LeaderboardWriter;
		if (leaderboardWriter != null)
		{
			LeaderboardsInfo[(int)BoardType].Submit(leaderboardWriter, Stats);
		}
	}

	/// <summary>
	/// The statistic names of this board's columns, in display order.
	///
	/// Names, not column ids: they are what the master server ranks by, and a name survives the
	/// column order changing. The board's rating is the sum of these, which is what the console
	/// build submitted to Xbox Live (<c>Submit</c> sets <c>Rating = sum</c>).
	/// </summary>
	public static string[] BoardStatistics(Leaderboard BoardType)
	{
		if ((int)BoardType < 0 || (int)BoardType >= LeaderboardsInfo.Length)
		{
			return null;
		}
		ColumnMapping[] columns = LeaderboardsInfo[(int)BoardType].Columns;
		string[] array = new string[columns.Length];
		for (int i = 0; i < columns.Length; i++)
		{
			array[i] = columns[i].Statistic.ToString();
		}
		return array;
	}

	private void Submit(LeaderboardWriter Writer, Statistics Stats)
	{
		long num = 0L;
		LeaderboardEntry leaderboard = Writer.GetLeaderboard(Identity);
		ColumnMapping[] columns = Columns;
		ColumnMapping[] array = columns;
		foreach (ColumnMapping columnMapping in array)
		{
			uint num2 = Stats[columnMapping.Statistic];
			leaderboard.Columns[columnMapping.ColumnName] = (int)num2;
			num += num2;
		}
		leaderboard.Rating = num;
	}
}
