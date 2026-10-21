using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Terraria.Achievements;
using Terraria.Leaderboards;

namespace Terraria.Net;

/// <summary>
/// One row of a board, as the master server reports it: a player and their value in every column
/// of that board, in the order the columns were asked for.
/// </summary>
internal sealed class BoardRow
{
	public string Gamertag = string.Empty;

	public long[] Values = new long[0];

	/// <summary>The board's rating: the sum of the columns, which is what the ranking is by.</summary>
	public long Total
	{
		get
		{
			long num = 0L;
			for (int i = 0; i < Values.Length; i++)
			{
				num += Values[i];
			}
			return num;
		}
	}
}

/// <summary>
/// Publishes the local player's achievements and statistics to the master server, and reads the
/// population totals back.
///
/// Why it exists: the console build got "earned by N% of players" and the leaderboard tables from
/// Xbox Live. This port has no Live, so those screens had nothing to show - achievements read from
/// a service that never answers and the leaderboard fell back to generated numbers. The master
/// server is already in the picture for the world list, and it is the only thing that sees more
/// than one player, so it is the honest place for these numbers.
///
/// Honesty about what these numbers are: they are <b>reported by clients</b>. A modified client can
/// claim any statistic it likes, and nothing here can tell. That is why the achievements screen
/// says "reported by" rather than presenting them as verified, and why the leaderboard is a
/// curiosity rather than a competition with prizes.
///
/// Threading: never blocks the game. Reporting runs on its own background thread with a single
/// in-flight guard; reading happens on the caller's thread when a screen opens, with a timeout and
/// a cached answer from the previous run.
/// </summary>
internal static class StatSync
{
	/// <summary>How often the same player is re-reported while playing, in seconds.</summary>
	private const int ReportIntervalSeconds = 300;

	/// <summary>Counts are refetched at most this often while a screen stays open.</summary>
	private const int CountsFreshSeconds = 120;

	private static readonly object Sync = new object();

	private static int[] _counts = new int[0];

	private static int _totalPlayers;

	private static DateTime _countsLoadedUtc = DateTime.MinValue;

	private static DateTime _lastReportUtc = DateTime.MinValue;

	private static bool _reporting;

	private static bool _countsLoading;

	/// <summary>Total players the counts are out of; 0 when unknown.</summary>
	public static int TotalPlayers
	{
		get
		{
			lock (Sync)
			{
				return _totalPlayers;
			}
		}
	}

	/// <summary>
	/// How many players earned achievement <paramref name="index"/>, or -1 when unknown (no master
	/// configured, never loaded, or that achievement not reported by anyone yet).
	/// </summary>
	public static int AchievementCount(int index)
	{
		if (index < 0)
		{
			return -1;
		}
		lock (Sync)
		{
			if (_totalPlayers <= 0 || index >= _counts.Length)
			{
				return -1;
			}
			return _counts[index];
		}
	}

	/// <summary>True when the counts are usable right now.</summary>
	public static bool HasCounts
	{
		get
		{
			lock (Sync)
			{
				return _totalPlayers > 0;
			}
		}
	}

	/// <summary>
	/// "12 of 40 players earned this", or null when the numbers are not available. One function so
	/// the wording lives in one place and the screen does not have to assemble it.
	/// </summary>
	public static string AchievementLabel(int index)
	{
		int num = AchievementCount(index);
		int totalPlayers = TotalPlayers;
		if (num < 0 || totalPlayers <= 0)
		{
			return null;
		}
		return Texts.Format("ui.achievements.earned.by", num, totalPlayers);
	}

	/// <summary>
	/// Starts a background refresh of the counts unless one is already running or the cached answer
	/// is fresh. Safe to call every frame; that is how the screen uses it.
	/// </summary>
	public static void RefreshCounts()
	{
		lock (Sync)
		{
			if (_countsLoading)
			{
				return;
			}
			if (_totalPlayers > 0 && (DateTime.UtcNow - _countsLoadedUtc).TotalSeconds < CountsFreshSeconds)
			{
				return;
			}
			_countsLoading = true;
		}
		Thread thread = new Thread(delegate()
		{
			try
			{
				int totalPlayers;
				int[] array = MasterServerClient.FetchAchievementCounts(out totalPlayers);
				lock (Sync)
				{
					if (array != null)
					{
						_counts = array;
						_totalPlayers = totalPlayers;
						_countsLoadedUtc = DateTime.UtcNow;
					}
				}
			}
			catch (Exception ex)
			{
				Program.LogDebug("Stats", "could not refresh achievement counts: " + ex.Message);
			}
			finally
			{
				lock (Sync)
				{
					_countsLoading = false;
				}
			}
		});
		thread.IsBackground = true;
		thread.Name = "TerrariaXDK.StatCounts";
		thread.Start();
	}

	/// <summary>
	/// Reports one player's progress, at most every <see cref="ReportIntervalSeconds"/> and never
	/// twice at once. Called after a save, which is the moment the numbers are already on disk.
	/// </summary>
	public static void Report(Statistics statistics, string gamertag)
	{
		if (statistics == null || string.IsNullOrEmpty(gamertag))
		{
			return;
		}
		lock (Sync)
		{
			if (_reporting)
			{
				return;
			}
			if ((DateTime.UtcNow - _lastReportUtc).TotalSeconds < ReportIntervalSeconds)
			{
				return;
			}
			_reporting = true;
			_lastReportUtc = DateTime.UtcNow;
		}
		// Snapshot on the game thread: the background thread must not read game state while the
		// game is running.
		int[] achievements = SnapshotAchievements();
		Dictionary<string, long> stats = SnapshotStats(statistics);
		string name = gamertag;
		Thread thread = new Thread(delegate()
		{
			try
			{
				string error;
				int num = MasterServerClient.ReportStats(name, achievements, stats, out error);
				if (num < 0)
				{
					Program.LogDebug("Stats", "the master refused the statistics: " + error);
				}
				else if (num > 0)
				{
					Program.LogInfo("Stats", "reported " + num + " new achievement(s) for " + name);
				}
			}
			catch (Exception ex)
			{
				Program.LogDebug("Stats", "could not report statistics: " + ex.Message);
			}
			finally
			{
				lock (Sync)
				{
					_reporting = false;
				}
			}
		});
		thread.IsBackground = true;
		thread.Name = "TerrariaXDK.Stats";
		thread.Start();
	}

	/// <summary>Indices of the achievements this installation has earned.</summary>
	private static int[] SnapshotAchievements()
	{
		try
		{
			AchievementSystem achievementSystem = Main.AchievementSystem;
			if (achievementSystem == null)
			{
				return new int[0];
			}
			System.Collections.BitArray earnedAchievements = achievementSystem.GetEarnedAchievements();
			if (earnedAchievements == null)
			{
				return new int[0];
			}
			List<int> list = new List<int>();
			for (int i = 0; i < earnedAchievements.Length; i++)
			{
				if (earnedAchievements[i])
				{
					list.Add(i);
				}
			}
			return list.ToArray();
		}
		catch
		{
			return new int[0];
		}
	}

	/// <summary>
	/// The player's counters, keyed by the enum name (<c>AirTravel</c>, …).
	///
	/// Names, not indices: the index is a position in an enum that this port has already changed
	/// once, and a leaderboard whose columns shift is worse than no leaderboard. A name also reads
	/// correctly when somebody looks at the master's JSON by hand.
	/// </summary>
	private static Dictionary<string, long> SnapshotStats(Statistics statistics)
	{
		Dictionary<string, long> dictionary = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
		try
		{
			Array values = Enum.GetValues(typeof(StatisticEntry));
			for (int i = 0; i < values.Length; i++)
			{
				StatisticEntry statisticEntry = (StatisticEntry)values.GetValue(i);
				if (statisticEntry == StatisticEntry.Unknown)
				{
					continue;
				}
				uint num = statistics[statisticEntry];
				if (num != 0)
				{
					dictionary[statisticEntry.ToString()] = num;
				}
			}
		}
		catch (Exception ex)
		{
			Program.LogDebug("Stats", "could not read the statistics: " + ex.Message);
		}
		return dictionary;
	}

	/// <summary>
	/// Fetches one board from the master, or null when it is not available. The caller is expected
	/// to be a screen that has already decided it has no Xbox Live.
	/// </summary>
	public static List<BoardRow> FetchBoard(string[] statistics, int limit)
	{
		try
		{
			return MasterServerClient.FetchBoard(statistics, limit);
		}
		catch (Exception ex)
		{
			Program.LogDebug("Stats", "could not read the leaderboard: " + ex.Message);
			return null;
		}
	}

	/// <summary>Column names of a board, in display order - the keys the master ranks by.</summary>
	public static string[] BoardStatistics(Leaderboard board)
	{
		try
		{
			if ((int)board < 0 || (int)board >= (int)Leaderboard.LEADERBOARD_COUNT)
			{
				return null;
			}
			return LeaderboardInfo.BoardStatistics(board);
		}
		catch
		{
			return null;
		}
	}

	/// <summary>Test hook: forgets the cached counts and the report timer.</summary>
	internal static void ResetForTests()
	{
		lock (Sync)
		{
			_counts = new int[0];
			_totalPlayers = 0;
			_countsLoadedUtc = DateTime.MinValue;
			_lastReportUtc = DateTime.MinValue;
			_reporting = false;
			_countsLoading = false;
		}
	}

	/// <summary>Test hook: installs counts without a network round trip.</summary>
	internal static void SetCountsForTests(int[] counts, int totalPlayers)
	{
		lock (Sync)
		{
			_counts = (counts == null) ? new int[0] : counts;
			_totalPlayers = totalPlayers;
			_countsLoadedUtc = DateTime.UtcNow;
		}
	}
}
