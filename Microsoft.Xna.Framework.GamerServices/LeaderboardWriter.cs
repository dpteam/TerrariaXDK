using System;

namespace Microsoft.Xna.Framework.GamerServices;

/// <summary>
/// Local sink for leaderboard writes.
///
/// On Xbox 360 this class forwarded entries to Xbox Live. That service does not exist in
/// this port, so a submitted entry has nowhere to go. Returning a throw-away entry keeps
/// the caller's write path intact (it fills in columns and a rating, then drops the object)
/// instead of throwing and taking the game down - which is what used to happen the moment a
/// world started, because <see cref="Terraria.Player.Spawn"/> submits statistics.
///
/// Persisting scores locally is tracked in IDEAS.md.
/// </summary>
public sealed class LeaderboardWriter
{
	public LeaderboardEntry GetLeaderboard(LeaderboardIdentity leaderboardId)
	{
		return new LeaderboardEntry(null, 0L, 0);
	}
}
