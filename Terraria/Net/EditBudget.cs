using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Terraria.Net;

/// <summary>
/// A token bucket: credit that refills at a steady rate and is spent in bursts.
///
/// Chosen over a per-second counter because traffic is not evenly spaced. A player who mines for
/// ten seconds and then dynamites a wall produces a burst, and a per-second cap would refuse the
/// honest burst while a patient cheater stays under it. A bucket pays for a burst out of credit
/// saved while nothing happened, which is what real play looks like.
///
/// Pure and driven by an explicit clock, so the whole behaviour is testable without waiting.
/// </summary>
internal sealed class TokenBucket
{
	private readonly double _capacity;

	private readonly double _refillPerMs;

	private double _credit;

	private long _lastMs;

	public TokenBucket(double capacity, double refillPerSecond, long nowMs)
	{
		_capacity = capacity;
		_refillPerMs = refillPerSecond / 1000.0;
		_credit = capacity;
		_lastMs = nowMs;
	}

	/// <summary>Credit available right now, after refilling up to the clock.</summary>
	public double CreditAt(long nowMs)
	{
		Refill(nowMs);
		return _credit;
	}

	/// <summary>
	/// Spends <paramref name="cost"/>, or refuses without spending anything when there is not
	/// enough. Refusing without charging matters: a client that asks for something too large must
	/// not be able to drain its own budget by asking.
	/// </summary>
	public bool TryTake(int cost, long nowMs)
	{
		if (cost <= 0)
		{
			return true;
		}
		if (cost > _capacity)
		{
			return false;
		}
		Refill(nowMs);
		if (_credit < cost)
		{
			return false;
		}
		_credit -= cost;
		return true;
	}

	private void Refill(long nowMs)
	{
		if (nowMs <= _lastMs)
		{
			// A clock that went backwards must not hand out credit. Leaving _lastMs alone means
			// the next forward tick still refills from the real position.
			return;
		}
		_credit += (nowMs - _lastMs) * _refillPerMs;
		if (_credit > _capacity)
		{
			_credit = _capacity;
		}
		_lastMs = nowMs;
	}
}

/// <summary>
/// How fast one player may change the world.
///
/// The problem: world protection answers "may this player edit here", never "how often". A
/// modified client can send packet 17 thousands of times a second, and every one of those is a
/// world change that the host applies and then relays to everyone else. That is a denial of
/// service on the host and on the other players, and it needs no permission at all - it works
/// perfectly well on a server where the player is allowed to build.
///
/// Two budgets rather than one, because the two kinds of edit are not the same act:
/// <list type="bullet">
/// <item><b>single edits</b> - one tile, wall or wire. The fastest honest case is an endgame
/// drill at 20 tiles per second, and a dynamite blast destroys on the order of a hundred tiles at
/// once, so the bucket holds 200 and refills at 60 per second. A flooder is then capped at 60 per
/// second instead of thousands.</item>
/// <item><b>bulk writes</b> - packet 20, a whole section at once. The host is the only legitimate
/// sender of that packet (it exists so the host can stream the world to a client), so a handful in
/// a burst is generous and a stream of them is not play at all. Refusing it outright would be
/// tempting, but "the client never sends this" is a claim about code I would rather not bet the
/// world sync on, so it gets a small budget and a log line instead.</item>
/// </list>
/// </summary>
internal static class EditBudget
{
	/// <summary>Single edits a player may make in a burst.</summary>
	public const int BurstEdits = 200;

	/// <summary>Single edits a player may sustain, per second.</summary>
	public const double EditsPerSecond = 60.0;

	/// <summary>Bulk section writes a player may make in a burst.</summary>
	public const int BurstSections = 8;

	/// <summary>Bulk section writes a player may sustain, per second.</summary>
	public const double SectionsPerSecond = 0.5;

	private sealed class PlayerBudget
	{
		public TokenBucket Edits;

		public TokenBucket Sections;

		/// <summary>Last time a refusal was logged, so a flooder cannot flood the log too.</summary>
		public long LastNoticeMs = -1000000L;
	}

	private static readonly Dictionary<string, PlayerBudget> ByPlayer =
		new Dictionary<string, PlayerBudget>(StringComparer.OrdinalIgnoreCase);

	private static readonly object Sync = new object();

	private static readonly Stopwatch Clock = Stopwatch.StartNew();

	/// <summary>
	/// Milliseconds since this class was first touched. Monotonic, unlike the wall clock: a system
	/// time change must not hand out or withhold credit.
	/// </summary>
	internal static long NowMs => (long)Clock.Elapsed.TotalMilliseconds;

	/// <summary>Spends a single edit. False with a reason when the player is going too fast.</summary>
	internal static bool AllowsEdit(string gamertag, out string reason)
	{
		return Allows(gamertag, isSection: false, out reason);
	}

	/// <summary>Spends a bulk section write.</summary>
	internal static bool AllowsSection(string gamertag, out string reason)
	{
		return Allows(gamertag, isSection: true, out reason);
	}

	private static bool Allows(string gamertag, bool isSection, out string reason)
	{
		reason = null;
		if (string.IsNullOrEmpty(gamertag))
		{
			// No name means no budget to keep, and refusing would break normal play against a
			// state that should not exist - the handshake already gave everyone a name.
			return true;
		}
		PlayerBudget playerBudget;
		lock (Sync)
		{
			if (!ByPlayer.TryGetValue(gamertag, out playerBudget))
			{
				playerBudget = new PlayerBudget();
				playerBudget.Edits = new TokenBucket(BurstEdits, EditsPerSecond, NowMs);
				playerBudget.Sections = new TokenBucket(BurstSections, SectionsPerSecond, NowMs);
				ByPlayer[gamertag] = playerBudget;
			}
		}
		TokenBucket tokenBucket = isSection ? playerBudget.Sections : playerBudget.Edits;
		if (tokenBucket.TryTake(1, NowMs))
		{
			return true;
		}
		reason = isSection
			? "You are sending world data faster than the server accepts."
			: "You are editing the world faster than the server accepts.";
		return false;
	}

	/// <summary>
	/// True when a refusal should be logged and told to the player. Throttled per player: a client
	/// that keeps flooding must not turn the refusal itself into a flood.
	/// </summary>
	internal static bool ShouldNotify(string gamertag, int intervalMs = 5000)
	{
		if (string.IsNullOrEmpty(gamertag))
		{
			return false;
		}
		lock (Sync)
		{
			PlayerBudget playerBudget;
			if (!ByPlayer.TryGetValue(gamertag, out playerBudget))
			{
				return true;
			}
			long nowMs = NowMs;
			if (nowMs - playerBudget.LastNoticeMs < intervalMs)
			{
				return false;
			}
			playerBudget.LastNoticeMs = nowMs;
			return true;
		}
	}

	/// <summary>Drops a player's budget. Called when they leave, so a reconnect starts clean.</summary>
	internal static void Reset(string gamertag)
	{
		if (string.IsNullOrEmpty(gamertag))
		{
			return;
		}
		lock (Sync)
		{
			ByPlayer.Remove(gamertag);
		}
	}

	/// <summary>Drops every budget, for a session ending.</summary>
	internal static void ResetAll()
	{
		lock (Sync)
		{
			ByPlayer.Clear();
		}
	}

	/// <summary>Players currently tracked. For the self-test.</summary>
	internal static int TrackedPlayers
	{
		get
		{
			lock (Sync)
			{
				return ByPlayer.Count;
			}
		}
	}
}
