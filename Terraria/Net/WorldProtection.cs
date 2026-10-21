using System;

namespace Terraria.Net;

/// <summary>
/// Stops players from changing the world.
///
/// Scope, stated plainly: this protects the world against **remote** players. The host's own
/// edits never travel through the network - they are applied locally and then broadcast - so
/// there is nothing to intercept, and gating them would only make the owner's own game feel
/// broken. That is the same trade TShock makes, and it is the honest one: nobody needs
/// protection from themselves.
///
/// Three independent rules, each with a clear reason:
/// <list type="number">
/// <item><b>Build permission.</b> A player whose group does not grant <see cref="Permission.Build"/>
/// cannot place or break anything.</item>
/// <item><b>Spawn protection.</b> The tiles around the world spawn cannot be touched by
/// remote players. That is where a new player arrives and where a griefer does the most
/// visible damage in the least time.</item>
/// <item><b>DisableBuild.</b> An emergency switch: while it is on, only the host changes the
/// world, whatever the groups say. Deliberately absolute - "I am away, do not touch my
/// world" should not depend on who was promoted last week.</item>
/// </list>
/// </summary>
internal static class WorldProtection
{
	/// <summary>[Network] DisableBuild. Only the host may change the world while set.</summary>
	public static bool DisableBuild;

	/// <summary>[Network] ProtectSpawnRadius, in tiles. 0 disables spawn protection.</summary>
	public static int SpawnRadius = 16;

	/// <summary>How often the same player is told why their edit was refused, in seconds.</summary>
	private const int NoticeSeconds = 15;

	private static readonly System.Collections.Generic.Dictionary<string, int> LastNotice =
		new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private static readonly object Sync = new object();

	public static void Load(Settings settings)
	{
		DisableBuild = settings.Get("Network", "DisableBuild").ToLower().Equals("true");
		int result;
		if (int.TryParse(settings.Get("Network", "ProtectSpawnRadius"), out result) && result >= 0 && result <= 200)
		{
			SpawnRadius = result;
		}
	}

	public static void Save(Settings settings)
	{
		settings.Update("Network", "DisableBuild", DisableBuild ? "True" : "False", "While set, only the host can change the world.\n; An emergency switch for when you step away from a public server.");
		settings.Update("Network", "ProtectSpawnRadius", SpawnRadius.ToString(), "Tiles around the world spawn that remote players cannot modify (0 = no protection).");
	}

	/// <summary>
	/// Decides whether one player may change the tile at (x, y). Returns false with a reason
	/// the caller can show. Kept pure except for the permission lookup, so every combination
	/// is testable without a world or a connection.
	/// </summary>
	internal static bool CanEdit(bool isHost, string gamertag, int x, int y, out string reason)
	{
		reason = null;
		if (isHost)
		{
			return true;
		}
		if (DisableBuild)
		{
			reason = "The host has disabled building.";
			return false;
		}
		if (!Permissions.Allows(gamertag, Permission.Build, isLocalHost: false))
		{
			reason = "You may not build on this server.";
			return false;
		}
		if (IsSpawnProtected(x, y))
		{
			reason = "The area around spawn is protected.";
			return false;
		}
		// Named regions: "this is my house". Checked after spawn protection so the spawn
		// message stays the one players actually expect there.
		Region region = Regions.FindAt(x, y);
		if (region != null && !Regions.AllowsIn(region, isHost: false, gamertag))
		{
			reason = "Region '" + region.Name + "' is protected.";
			return false;
		}
		return true;
	}

	/// <summary>
	/// True when the tile is inside the protected box around the world spawn. A square box,
	/// not a circle: it is what the player can see on the map, so "why is this tile blocked"
	/// has an answer they can check.
	/// </summary>
	internal static bool IsSpawnProtected(int x, int y)
	{
		return IsWithinSpawnBox(x, y, Main.SpawnTileX, Main.SpawnTileY, SpawnRadius);
	}

	/// <summary>The spawn box as a pure function, for the self-test.</summary>
	internal static bool IsWithinSpawnBox(int x, int y, int spawnX, int spawnY, int radius)
	{
		if (radius <= 0)
		{
			return false;
		}
		// A generous vertical reach: a spawn near a hill would otherwise leave the surface
		// right above the player's head editable.
		int num = radius;
		return x >= spawnX - num && x <= spawnX + num && y >= spawnY - num && y <= spawnY + num;
	}

	/// <summary>
	/// Remembers that a player was just refused, and returns true when they should be told
	/// again. Without the throttle a griefing attempt would flood their own chat, which also
	/// floods the host's log.
	/// </summary>
	internal static bool ShouldNotify(string gamertag)
	{
		if (string.IsNullOrEmpty(gamertag))
		{
			return false;
		}
		int tickCount = Environment.TickCount;
		lock (Sync)
		{
			int num;
			if (LastNotice.TryGetValue(gamertag, out num) && unchecked((uint)(tickCount - num)) < (uint)(NoticeSeconds * 1000) && num != 0)
			{
				return false;
			}
			LastNotice[gamertag] = tickCount;
			return true;
		}
	}

	internal static void ResetNotices()
	{
		lock (Sync)
		{
			LastNotice.Clear();
		}
	}
}
