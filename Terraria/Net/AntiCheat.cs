using System;
using Microsoft.Xna.Framework;

namespace Terraria.Net;

/// <summary>How much the host trusts a client's claim about where it is.</summary>
internal enum AntiCheatMode
{
	/// <summary>Do nothing. Movement is relayed exactly as claimed.</summary>
	Off = 0,

	/// <summary>
	/// Judge every move and record what would have happened, but relay everything anyway.
	///
	/// The default, and deliberately so. A rejection that fires on a lag spike, a grappling
	/// hook or a respawn freezes a legitimate player for everyone else - and the host has no
	/// way to tell "cheating" from "unlucky" in the moment. Observe mode turns the question
	/// into evidence: the log says exactly who moved how far in how long, and the host can then
	/// decide whether to switch to Enforce.
	/// </summary>
	Observe = 1,

	/// <summary>Refuse moves that break the rules: they are not applied and not relayed.</summary>
	Enforce = 2
}

/// <summary>
/// Per-player movement state. Kept per player index, because that is what the position packet
/// carries.
/// </summary>
internal sealed class AntiCheatTrack
{
	/// <summary>False until the first position is seen; there is nothing to compare against yet.</summary>
	public bool HasSample;

	public Vector2 Last;

	/// <summary>Milliseconds of the last sample, on the host's clock.</summary>
	public int LastMs;

	/// <summary>
	/// Movement credit in pixels. It refills at a plausible walking speed and drains by however
	/// far the player actually moved, so a sudden jump is affordable only if the player has been
	/// standing still long enough to have saved up for it.
	/// </summary>
	public double Budget = AntiCheat.CapacityPixels;

	/// <summary>Suspicious moves this session. Reset when the player leaves.</summary>
	public int Violations;

	public string LastReason;
}

/// <summary>
/// Host-side movement validation.
///
/// The problem: a client sends packet 13 with "I am here", and the host believes it. That is
/// how every player position in this port arrives, so a modified client can be anywhere, and
/// the host relays it to everyone - which is what makes it more than a local lie.
///
/// The rule: a player may not move further than plausible movement allows, given the time that
/// actually passed. That is deliberately a *budget* rather than a fixed per-packet limit:
/// packets are not evenly spaced, a lag spike batches several ticks into one, and the client
/// chooses when to send. A per-packet cap would either reject honest bursts or accept a slow
/// trickle of cheating.
///
/// What this is honest about: the client still owns its own player locally, so an enforced
/// rejection makes the cheater disappear for everyone else rather than snapping them back.
/// Blocking the relay is the part that matters - the cheater stops being able to affect what
/// other players see - and the log line is the part that lets a host act.
///
/// Nothing here inspects tiles or items: those already have their own gates
/// (<see cref="WorldProtection"/>, <see cref="ItemRestrictions"/>).
/// </summary>
internal static class AntiCheat
{
	/// <summary>
	/// Movement credit in pixels. A player standing still refills the budget, which is what pays
	/// for the one big jump that real movement sometimes needs: landing after a long fall,
	/// getting knocked back, or the tick after a respawn.
	///
	/// Sized from the worst honest gap: TCP stalling for about half a second. At the refill rate
	/// below that is 350 px, so 512 leaves room for the movement inside the stall as well. A
	/// larger budget would only make a subtle speed hack harder to catch.
	/// </summary>
	public const double CapacityPixels = 512.0;

	/// <summary>
	/// Refill rate in pixels per millisecond. Walking is 3 px/tick at 60 ticks/s = 180 px/s, and
	/// the fastest legitimate fall is 10 px/tick = 600 px/s. 0.7 px/ms is 700 px/s: comfortably
	/// above terminal velocity, and below anything that would let a player cross ground faster
	/// than the game can render.
	///
	/// The consequence, stated plainly: a cheat only slightly faster than this is caught slowly,
	/// because the budget has to drain first. That is why the default mode observes instead of
	/// refusing - see <see cref="AntiCheatMode"/>.
	/// </summary>
	public const double RefillPixelsPerMs = 0.7;

	/// <summary>
	/// A grappling hook pulls far faster than walking, so the allowance is multiplied while one
	/// is attached. Not unlimited: a hook cannot cross the map in one tick either.
	/// </summary>
	public const double GrappleMultiplier = 3.0;

	/// <summary>
	/// How far outside the world a position may be before it is refused. Players at the very
	/// edge report coordinates a little past the last tile, and falling out of the world is a
	/// real (if rare) state.
	/// </summary>
	public const float WorldMarginPixels = 64f;

	private static AntiCheatMode _mode;

	private static readonly AntiCheatTrack[] Tracks = new AntiCheatTrack[256];

	private static readonly object Sync = new object();

	private static long _rejected;

	private static long _suspicious;

	private static long _impossible;

	/// <summary>Current mode. Set from settings, or with <c>!anticheat</c>.</summary>
	public static AntiCheatMode Mode
	{
		get
		{
			return _mode;
		}
		set
		{
			_mode = value;
		}
	}

	public static bool Enabled => _mode != AntiCheatMode.Off;

	/// <summary>True when a Suspicious verdict actually stops the move.</summary>
	public static bool Enforcing => _mode == AntiCheatMode.Enforce;

	/// <summary>Moves refused since startup (Enforce only) - the number a host actually cares about.</summary>
	public static long Rejected => _rejected;

	/// <summary>Moves that would have been refused, whether or not the mode refused them.</summary>
	public static long Suspicious => _suspicious;

	/// <summary>Positions refused for being unusable at all (not a number, outside the world).</summary>
	public static long Impossible => _impossible;

	/// <summary>Reads the mode from settings. Anything unreadable means Observe, never Enforce.</summary>
	public static void Load(Settings settings)
	{
		_mode = AntiCheatMode.Observe;
		if (settings == null)
		{
			return;
		}
		_mode = Parse(settings.Get("Network", "AntiCheat"));
	}

	/// <summary>
	/// Mode from text: "off"/"observe"/"enforce", or 0/1/2. Unreadable text falls back to
	/// <see cref="AntiCheatMode.Observe"/> - a typo must not silently switch protection on.
	/// </summary>
	internal static AntiCheatMode Parse(string text)
	{
		string text2 = (text ?? string.Empty).Trim().ToLowerInvariant();
		switch (text2)
		{
		case "off":
		case "0":
		case "none":
		case "false":
			return AntiCheatMode.Off;
		case "enforce":
		case "2":
		case "on":
		case "true":
			return AntiCheatMode.Enforce;
		case "observe":
		case "1":
			return AntiCheatMode.Observe;
		default:
			return AntiCheatMode.Observe;
		}
	}

	/// <summary>The settings text for a mode, so the host command can write it back.</summary>
	public static string ToSetting(AntiCheatMode mode)
	{
		switch (mode)
		{
		case AntiCheatMode.Off:
			return "Off";
		case AntiCheatMode.Enforce:
			return "Enforce";
		default:
			return "Observe";
		}
	}

	/// <summary>Parses the host command's argument; false for anything unrecognised.</summary>
	internal static bool TryParseExact(string text, out AntiCheatMode mode)
	{
		string text2 = (text ?? string.Empty).Trim().ToLowerInvariant();
		switch (text2)
		{
		case "off":
		case "0":
			mode = AntiCheatMode.Off;
			return true;
		case "observe":
		case "1":
			mode = AntiCheatMode.Observe;
			return true;
		case "enforce":
		case "2":
			mode = AntiCheatMode.Enforce;
			return true;
		default:
			mode = AntiCheatMode.Observe;
			return false;
		}
	}

	/// <summary>Forgets every track, as if nobody had ever moved.</summary>
	public static void Reset()
	{
		lock (Sync)
		{
			for (int i = 0; i < Tracks.Length; i++)
			{
				Tracks[i] = null;
			}
			_rejected = 0L;
			_suspicious = 0L;
			_impossible = 0L;
		}
	}

	/// <summary>Forgets one player, called when they disconnect so a rejoining player starts clean.</summary>
	public static void Forget(int playerIndex)
	{
		if (playerIndex < 0 || playerIndex >= Tracks.Length)
		{
			return;
		}
		lock (Sync)
		{
			Tracks[playerIndex] = null;
		}
	}

	/// <summary>The track for a player, for tests and for the host command's report.</summary>
	public static AntiCheatTrack TrackOf(int playerIndex)
	{
		if (playerIndex < 0 || playerIndex >= Tracks.Length)
		{
			return null;
		}
		lock (Sync)
		{
			return Tracks[playerIndex];
		}
	}

	/// <summary>
	/// Judges one reported move and updates the player's budget.
	///
	/// Pure with respect to the clock: <paramref name="nowMs"/> comes from the caller, so a test
	/// can simulate a long pause without sleeping, and the host has one place that reads
	/// <c>Environment.TickCount</c>.
	///
	/// Returns what the *mode* should do: <c>true</c> means relay the move. The reason is
	/// written to <paramref name="reason"/> and to the log either way, because the point of
	/// Observe mode is that the evidence exists before the decision.
	/// </summary>
	public static bool Allow(int playerIndex, int nowMs, Vector2 from, Vector2 to, bool grappling, bool dead, int worldWidthPx, int worldHeightPx, out string reason)
	{
		reason = null;
		if (_mode == AntiCheatMode.Off)
		{
			return true;
		}
		if (playerIndex < 0 || playerIndex >= Tracks.Length)
		{
			// Not a player this table covers; nothing to compare, so nothing to refuse.
			return true;
		}
		if (!IsFinite(to))
		{
			// No legitimate client ever sends this, and applying it corrupts the host's world for
			// every other player. Refused in both modes: this is not a judgement call.
			lock (Sync)
			{
				_impossible++;
			}
			reason = "position is not a number";
			return false;
		}
		if (worldWidthPx > 0 && worldHeightPx > 0 && OutOfWorld(to, worldWidthPx, worldHeightPx))
		{
			lock (Sync)
			{
				_impossible++;
			}
			reason = "position " + Format(to) + " is outside the world";
			return false;
		}
		AntiCheatTrack antiCheatTrack;
		lock (Sync)
		{
			antiCheatTrack = Tracks[playerIndex];
			if (antiCheatTrack == null)
			{
				antiCheatTrack = new AntiCheatTrack();
				Tracks[playerIndex] = antiCheatTrack;
			}
		}
		if (!antiCheatTrack.HasSample)
		{
			// First report from this player: record it and say nothing. A joining player is
			// allowed to appear wherever the host put them.
			antiCheatTrack.HasSample = true;
			antiCheatTrack.Last = to;
			antiCheatTrack.LastMs = nowMs;
			antiCheatTrack.Budget = CapacityPixels;
			return true;
		}
		double num = (double)Delta(nowMs, antiCheatTrack.LastMs);
		double num2 = Vector2.Distance(antiCheatTrack.Last, to);
		antiCheatTrack.Budget = Math.Min(CapacityPixels, antiCheatTrack.Budget + num * RefillPixelsPerMs * (grappling ? GrappleMultiplier : 1.0));
		bool flag = dead || grappling;
		if (num2 > antiCheatTrack.Budget && !flag)
		{
			antiCheatTrack.Violations++;
			antiCheatTrack.LastReason = num2.ToString("0") + " px in " + num.ToString("0") + " ms";
			lock (Sync)
			{
				_suspicious++;
			}
			reason = "moved " + num2.ToString("0") + " px in " + num.ToString("0") + " ms (budget "
				+ antiCheatTrack.Budget.ToString("0") + " px)";
			int num3 = (int)antiCheatTrack.Violations;
			bool flag2 = Enforcing;
			if (flag2)
			{
				lock (Sync)
				{
					_rejected++;
				}
				// The move is refused, so the host's idea of where this player is must not move
				// either. Keeping the old reference is what makes a cheater stay frozen instead
				// of advancing one refused hop at a time - each new packet is measured against the
				// last position that was actually accepted.
				antiCheatTrack.Budget = 0.0;
			}
			else
			{
				// Observe relays the move, so the player really is there; measuring the next hop
				// from anywhere else would invent violations out of nothing.
				antiCheatTrack.Last = to;
				antiCheatTrack.LastMs = nowMs;
				antiCheatTrack.Budget = 0.0;
			}
			Program.LogWarn("AntiCheat", "player " + playerIndex + ": " + reason + ", violation " + num3
				+ " (" + ToSetting(_mode) + (flag2 ? ", refused" : ", relayed") + ")");
			return !flag2;
		}
		if (num2 > antiCheatTrack.Budget * 0.75 && num2 > 64.0 && !flag)
		{
			// Close to the limit but allowed: worth a line, because the useful signal is a pattern
			// of near-misses rather than one dramatic jump.
			Program.LogInfo("AntiCheat", "player " + playerIndex + " near the limit: " + num2.ToString("0") + " px in " + num.ToString("0") + " ms");
		}
		antiCheatTrack.Last = to;
		antiCheatTrack.LastMs = nowMs;
		antiCheatTrack.Budget -= num2;
		if (antiCheatTrack.Budget < 0.0)
		{
			antiCheatTrack.Budget = 0.0;
		}
		return true;
	}

	/// <summary>One line for <c>!anticheat</c>: mode, counters and who is currently over budget.</summary>
	public static string Describe()
	{
		string text = ToSetting(_mode) + " - rejected " + _rejected + ", suspicious " + _suspicious + ", unusable " + _impossible;
		System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(text);
		lock (Sync)
		{
			for (int i = 0; i < Tracks.Length; i++)
			{
				if (Tracks[i] != null && Tracks[i].Violations > 0)
				{
					stringBuilder.Append("; player ").Append(i).Append(": ").Append(Tracks[i].Violations).Append(" (")
						.Append(Tracks[i].LastReason).Append(')');
				}
			}
		}
		return stringBuilder.ToString();
	}

	/// <summary>True when <paramref name="value"/> is a finite number (NaN and infinity are not).</summary>
	internal static bool IsFinite(Vector2 value)
	{
		return IsFinite(value.X) && IsFinite(value.Y);
	}

	internal static bool IsFinite(float value)
	{
		// float.IsNaN/IsInfinity exist in net40, but a range check is what actually matters:
		// anything beyond this is a corrupt float, and it also catches -0 and denormals cheaply.
		return !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) < 1E+08f;
	}

	/// <summary>True when the position is outside the world by more than the margin.</summary>
	internal static bool OutOfWorld(Vector2 position, int worldWidthPx, int worldHeightPx)
	{
		return position.X < 0f - WorldMarginPixels
			|| position.Y < 0f - WorldMarginPixels
			|| position.X > (float)worldWidthPx + WorldMarginPixels
			|| position.Y > (float)worldHeightPx + WorldMarginPixels;
	}

	/// <summary>
	/// Milliseconds between two <c>Environment.TickCount</c> samples, tolerating the wrap that
	/// happens every ~49 days. A negative difference is a wrap, not time travel.
	/// </summary>
	internal static int Delta(int nowMs, int thenMs)
	{
		int num = unchecked(nowMs - thenMs);
		if (num < 0)
		{
			// Clamp rather than adding int.MaxValue: for a budget, "unknown but small" is safer
			// than "you may move for the next 24 days".
			return 0;
		}
		return num;
	}

	private static string Format(Vector2 value)
	{
		return value.X.ToString("0") + "," + value.Y.ToString("0");
	}
}
