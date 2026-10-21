using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MasterServer;

/// <summary>One player's reported progress.</summary>
internal sealed class PlayerRecord
{
	public string Gamertag = string.Empty;

	/// <summary>Indices into the game's achievement list that this player has earned.</summary>
	public readonly HashSet<int> Achievements = new HashSet<int>();

	/// <summary>
	/// Named counters ("AirTravel", "Deaths", …). Kept at their maximum: a statistic is a running
	/// total and never goes down, so a lower report is a stale one, not a correction.
	/// </summary>
	public readonly Dictionary<string, long> Stats = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

	public DateTime LastSeenUtc;

	public string Key => Gamertag.ToLowerInvariant();
}

/// <summary>
/// Achievement and leaderboard aggregation.
///
/// Why the master and not the client: "how many players earned this" is a fact about the whole
/// population, and the game has no Xbox Live to ask. Keeping it here also means the answers survive
/// a reinstall and are the same for everyone, which is the only way a leaderboard means anything.
///
/// What it is not: an anti-cheat. A modified client can report any number it likes. Making that
/// trustworthy needs server-side gameplay, which this port does not have - so the numbers are
/// presented as "reported by players", not as verified.
/// </summary>
internal sealed class PlayerStats
{
	/// <summary>
	/// Upper bound on tracked players. Beyond this the oldest reporter is dropped, so a hostile
	/// client cannot grow the file without limit.
	/// </summary>
	public const int MaxPlayers = 20000;

	/// <summary>Longest stat name accepted, so a hostile announce cannot fill the file with keys.</summary>
	private const int MaxStatName = 40;

	/// <summary>Sanity bound for a single counter: a hundred trillion is already nonsense.</summary>
	private const long MaxStatValue = 100000000000000L;

	private readonly Dictionary<string, PlayerRecord> _players = new Dictionary<string, PlayerRecord>(StringComparer.OrdinalIgnoreCase);

	private readonly object _sync = new object();

	private readonly string _dataPath;

	public PlayerStats(string dataPath)
	{
		_dataPath = dataPath;
	}

	public int PlayerCount
	{
		get
		{
			lock (_sync)
			{
				return _players.Count;
			}
		}
	}

	/// <summary>
	/// Merges one report. Achievements are sticky (an achievement is never lost), stats keep the
	/// maximum. Returns how many achievements were newly added, which is what the console prints.
	/// </summary>
	public int Report(string gamertag, int[] achievements, Dictionary<string, long> stats, out string error)
	{
		error = null;
		gamertag = (gamertag ?? string.Empty).Trim();
		if (gamertag.Length == 0 || gamertag.Length > 64)
		{
			error = "gamertag must be 1-64 characters";
			return -1;
		}
		int num = 0;
		lock (_sync)
		{
			PlayerRecord playerRecord;
			if (!_players.TryGetValue(gamertag, out playerRecord))
			{
				if (_players.Count >= MaxPlayers)
				{
					EvictOldestLocked();
				}
				playerRecord = new PlayerRecord();
				playerRecord.Gamertag = gamertag;
				_players[gamertag] = playerRecord;
			}
			playerRecord.LastSeenUtc = DateTime.UtcNow;
			if (achievements != null)
			{
				for (int i = 0; i < achievements.Length; i++)
				{
					int num2 = achievements[i];
					if (num2 < 0 || num2 > 4096)
					{
						continue;
					}
					if (playerRecord.Achievements.Add(num2))
					{
						num++;
					}
				}
			}
			if (stats != null)
			{
				foreach (KeyValuePair<string, long> stat in stats)
				{
					string key = (stat.Key ?? string.Empty).Trim();
					if (key.Length == 0 || key.Length > MaxStatName)
					{
						continue;
					}
					long num3 = (stat.Value < 0L) ? 0L : ((stat.Value > MaxStatValue) ? MaxStatValue : stat.Value);
					long num4;
					if (!playerRecord.Stats.TryGetValue(key, out num4) || num3 > num4)
					{
						playerRecord.Stats[key] = num3;
					}
				}
			}
		}
		return num;
	}

	/// <summary>
	/// How many players earned each achievement, as a dense array by index, plus the population it
	/// is out of. Dense because the client draws "12/40" next to achievement <c>i</c>.
	/// </summary>
	public int[] AchievementCounts(out int totalPlayers)
	{
		lock (_sync)
		{
			totalPlayers = _players.Count;
			int num = 0;
			foreach (PlayerRecord value in _players.Values)
			{
				foreach (int achievement in value.Achievements)
				{
					if (achievement + 1 > num)
					{
						num = achievement + 1;
					}
				}
			}
			int[] array = new int[num];
			foreach (PlayerRecord value2 in _players.Values)
			{
				foreach (int achievement2 in value2.Achievements)
				{
					array[achievement2]++;
				}
			}
			return array;
		}
	}

	/// <summary>
	/// Players ranked by the sum of several statistics, highest first. That sum is the board
	/// rating the console build submitted, so the order here matches what a player would expect
	/// from a board whose columns are those statistics.
	///
	/// Only players who have at least one of them are included: a board full of zeroes is noise.
	/// </summary>
	public List<PlayerRecord> RankBySum(List<string> statNames, int limit)
	{
		List<PlayerRecord> list = new List<PlayerRecord>();
		if (statNames == null || statNames.Count == 0 || limit <= 0)
		{
			return list;
		}
		List<KeyValuePair<PlayerRecord, long>> list2 = new List<KeyValuePair<PlayerRecord, long>>();
		lock (_sync)
		{
			foreach (PlayerRecord value in _players.Values)
			{
				long num = 0L;
				for (int i = 0; i < statNames.Count; i++)
				{
					long num2;
					if (value.Stats.TryGetValue(statNames[i], out num2))
					{
						num += num2;
					}
				}
				if (num > 0L)
				{
					list2.Add(new KeyValuePair<PlayerRecord, long>(value, num));
				}
			}
		}
		list2.Sort(delegate(KeyValuePair<PlayerRecord, long> a, KeyValuePair<PlayerRecord, long> b)
		{
			int num3 = b.Value.CompareTo(a.Value);
			return (num3 != 0) ? num3 : string.Compare(a.Key.Gamertag, b.Key.Gamertag, StringComparison.OrdinalIgnoreCase);
		});
		for (int j = 0; j < list2.Count && j < limit; j++)
		{
			list.Add(list2[j].Key);
		}
		return list;
	}

	/// <summary>One player's own record, or null. Used by the console and by tests.</summary>
	public PlayerRecord Find(string gamertag)
	{
		PlayerRecord playerRecord;
		lock (_sync)
		{
			_players.TryGetValue((gamertag ?? string.Empty).Trim(), out playerRecord);
		}
		return playerRecord;
	}

	/// <summary>Drops the player who reported least recently. Called under the lock.</summary>
	private void EvictOldestLocked()
	{
		string text = null;
		DateTime dateTime = DateTime.MaxValue;
		foreach (PlayerRecord value in _players.Values)
		{
			if (value.LastSeenUtc < dateTime)
			{
				dateTime = value.LastSeenUtc;
				text = value.Gamertag;
			}
		}
		if (text != null)
		{
			_players.Remove(text);
		}
	}

	// --- persistence --------------------------------------------------------

	public void Save()
	{
		if (string.IsNullOrEmpty(_dataPath))
		{
			return;
		}
		try
		{
			List<PlayerRecord> list;
			lock (_sync)
			{
				list = new List<PlayerRecord>(_players.Values);
			}
			StringBuilder stringBuilder = new StringBuilder(1024 + list.Count * 256);
			stringBuilder.Append("{\"savedUtc\":").Append(Json.Quote(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)));
			stringBuilder.Append(",\"players\":[");
			for (int i = 0; i < list.Count; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(',');
				}
				PlayerRecord playerRecord = list[i];
				stringBuilder.Append("{\"gamertag\":").Append(Json.Quote(playerRecord.Gamertag));
				stringBuilder.Append(",\"lastSeenUtc\":").Append(Json.Quote(playerRecord.LastSeenUtc.ToString("o", CultureInfo.InvariantCulture)));
				stringBuilder.Append(",\"achievements\":[");
				bool flag = true;
				foreach (int achievement in playerRecord.Achievements)
				{
					if (!flag)
					{
						stringBuilder.Append(',');
					}
					flag = false;
					stringBuilder.Append(achievement.ToString(CultureInfo.InvariantCulture));
				}
				stringBuilder.Append(']');
				stringBuilder.Append(",\"stats\":{");
				bool flag2 = true;
				foreach (KeyValuePair<string, long> stat in playerRecord.Stats)
				{
					if (!flag2)
					{
						stringBuilder.Append(',');
					}
					flag2 = false;
					stringBuilder.Append(Json.Quote(stat.Key)).Append(':').Append(stat.Value.ToString(CultureInfo.InvariantCulture));
				}
				stringBuilder.Append("}}");
			}
			stringBuilder.Append("]}");
			File.WriteAllText(_dataPath, stringBuilder.ToString(), Encoding.UTF8);
		}
		catch (Exception)
		{
			// A stats file that cannot be written must not take the server down; the numbers are
			// still in memory and the next save may succeed.
		}
	}
}
