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

		/// <summary>
		/// Name for a row that did not come from Xbox Live (a row read from the master server).
		/// <see cref="Gamer"/> is an abstract XNA type and cannot be constructed here, so the name
		/// is carried directly rather than faked with a stub gamer.
		/// </summary>
		public string Name;

		public string Gamertag => (Gamer != null) ? Gamer.Gamertag : Name;
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

	private readonly int DefaultBatchSize;

	public int BatchSize;

	public int BatchStart;

	public int Selected;

	public Column[] Columns;

	public string LeaderboardName;

	/// <summary>
	/// True once a master read has been installed. Nothing pages: the master answers with at most
	/// <see cref="PageSize"/> rows and the screen scrolls inside them.
	/// </summary>
	public bool Ready => _fromMaster;

	/// <summary>
	/// True when the table came from the master server. It is a single batch, so there is nothing
	/// to page to: the whole point is to show something where Live would have shown nothing.
	/// </summary>
	private bool _fromMaster;

	/// <summary>The board currently loaded, so an asynchronous failure can still fall back.</summary>
	private Leaderboard _board;

	// Master-fallback state. Volatile because the fetch runs on its own thread; the game thread
	// only reads them in Update(). _masterRows is published by the volatile write of
	// _masterLoading = false, which is a release: the list is complete before the flag drops.
	private volatile bool _masterLoading;

	/// <summary>True from the moment a master read starts until its result has been applied or
	/// reported. Without it, a successful Live read would look like a failed master read.</summary>
	private volatile bool _masterAttempted;

	private volatile int _masterGeneration;

	private System.Collections.Generic.List<Terraria.Net.BoardRow> _masterRows;

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

	/// <summary>
	/// Loads a board. The master server is the source, not Xbox Live.
	///
	/// The reason is not laziness: the Live reader in this port is a stub that manufactures
	/// plausible rows (<c>Microsoft.Xna.Framework.GamerServices.LeaderboardReader</c>), so a
	/// Live-first screen showed invented players and never touched the real numbers. The stub
	/// stays for the achievements list, where it only enumerates real local achievements, but a
	/// ranking made of made-up names is worse than an empty one.
	/// </summary>
	public void LoadLeaderboard(Leaderboard BoardType)
	{
		ResetCaches();
		Columns = LeaderboardInfo.GetColumns(BoardType);
		LeaderboardName = LeaderboardInfo.GetName(BoardType);
		NumEntries = 0;
		_board = BoardType;
		_fromMaster = false;
		_masterAttempted = false;
		BeginLoadFromMaster(BoardType);
	}

	/// <summary>
	/// "My rank" pivot. The master's table is global, so the pivot changes nothing here; the
	/// board is still the real one.
	/// </summary>
	public void LoadLeaderboard(Leaderboard BoardType, Gamer CurrentGamer)
	{
		LoadLeaderboard(BoardType);
	}

	/// <summary>
	/// Friends board. There is no friends service in this port, and inventing one would mean
	/// inventing friend rows - so the table stays empty until a real source exists.
	/// </summary>
	public void LoadLeaderboard(Leaderboard BoardType, FriendCollection FriendsList, Gamer CurrentGamer)
	{
		ResetCaches();
		Columns = LeaderboardInfo.GetColumns(BoardType);
		LeaderboardName = LeaderboardInfo.GetName(BoardType);
		NumEntries = 0;
		_board = BoardType;
		_fromMaster = false;
		_masterAttempted = false;
		BatchSize = 0;
	}

	/// <summary>
	/// Starts reading the board from the master server's ranking, on a background thread.
	///
	/// Background because a master that is configured but does not answer costs a full HTTP
	/// timeout, and doing that here would freeze the menu the moment it opens. The result is
	/// picked up by <see cref="Update"/>, which the screen already polls every frame.
	/// </summary>
	private void BeginLoadFromMaster(Leaderboard BoardType)
	{
		string[] statistics = Terraria.Net.StatSync.BoardStatistics(BoardType);
		if (statistics == null)
		{
			return;
		}
		_masterGeneration++;
		int generation = _masterGeneration;
		_masterRows = null;
		_masterAttempted = true;
		_masterLoading = true;
		_unavailable = false;
		System.Threading.Thread thread = new System.Threading.Thread(delegate()
		{
			try
			{
				System.Collections.Generic.List<Terraria.Net.BoardRow> rows = Terraria.Net.StatSync.FetchBoard(statistics, PageSize);
				// A stale answer for a board the player has already left is dropped.
				if (generation != _masterGeneration)
				{
					return;
				}
				_masterRows = rows;
			}
			catch (Exception ex)
			{
				Program.LogDebug("Leaderboard", "master read failed: " + ex.Message);
				if (generation == _masterGeneration)
				{
					_masterRows = null;
				}
			}
			finally
			{
				if (generation == _masterGeneration)
				{
					_masterLoading = false;
				}
			}
		});
		thread.IsBackground = true;
		thread.Name = "TerrariaXDK.Board";
		thread.Start();
	}

	/// <summary>
	/// Installs rows fetched from the master. Returns false when there was nothing to install,
	/// in which case the caller keeps its "no leaderboard" path.
	///
	/// Rows are ranked by the sum of the board's columns, which is the rating the console build
	/// submitted to Xbox Live, so the order reads the same as it did there.
	/// </summary>
	private bool ApplyMasterRows()
	{
		System.Collections.Generic.List<Terraria.Net.BoardRow> list = _masterRows;
		if (list == null)
		{
			// No answer at all - the master is down or has no such board. That is a failure.
			return false;
		}
		// An empty answer is a real answer: the master is up and nobody has reported this board
		// yet. Showing an empty table beats showing an error box that talks about Xbox Live.
		Row[] array2 = new Row[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			Terraria.Net.BoardRow boardRow = list[i];
			long[] values = boardRow.Values;
			uint[] array3 = new uint[values.Length];
			for (int j = 0; j < values.Length; j++)
			{
				array3[j] = ((values[j] < 0L) ? 0u : ((values[j] > 4294967295L) ? uint.MaxValue : (uint)values[j]));
			}
			array2[i] = new Row
			{
				Available = true,
				Rank = i + 1,
				Name = boardRow.Gamertag,
				Statistics = array3
			};
		}
		CurrentCache = new Cache
		{
			Entries = array2,
			StartIndex = 0,
			NumEntries = array2.Length
		};
		StaleCache = new Cache
		{
			Entries = new Row[0],
			StartIndex = 0,
			NumEntries = 0
		};
		MergedCache = new Cache
		{
			Entries = array2,
			StartIndex = 0,
			NumEntries = array2.Length
		};
		_fromMaster = true;
		NumEntries = array2.Length;
		BatchSize = Math.Min(array2.Length, DefaultBatchSize);
		BatchStart = 0;
		Selected = 0;
		return true;
	}

	/// <summary>
	/// True when the table came from the master and the master had nothing to show. The screen
	/// distinguishes "nobody has played this board yet" from "we could not reach the server".
	/// </summary>
	public bool IsEmptyRanking => _fromMaster && MergedCache != null && MergedCache.NumEntries == 0;

	/// <summary>True when the master could not be reached. Nothing to draw, but the screen works.</summary>
	public bool IsUnavailable => _unavailable && !_fromMaster;

	private volatile bool _unavailable;

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
		// There is no Xbox Live read to poll: the table comes from the master server, on a
		// background thread. All this does is notice when that thread has an answer.
		if (_fromMaster || _masterLoading)
		{
			return true;
		}
		if (_masterRows != null)
		{
			return ApplyMasterRows();
		}
		if (_masterAttempted)
		{
			// Started, finished, no rows: the fetch failed. The screen stays open and says so - a
			// modal dialog here made a temporarily unreachable server look like a fatal error.
			_masterAttempted = false;
			_unavailable = true;
		}
		return true;
	}

	public bool MoveDown()
	{
		// One batch from the master: scrolling past the last row is a refusal, not a page fetch.
		Selected++;
		if (Selected < BatchStart + BatchSize)
		{
			return true;
		}
		int nextStart = BatchStart + 1;
		if (nextStart + BatchSize > NumEntries)
		{
			Selected--;
			return false;
		}
		BatchStart = nextStart;
		return true;
	}

	public bool MoveUp()
	{
		Selected--;
		if (Selected >= BatchStart)
		{
			return true;
		}
		if (BatchStart <= 0)
		{
			Selected++;
			return false;
		}
		BatchStart--;
		return true;
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
