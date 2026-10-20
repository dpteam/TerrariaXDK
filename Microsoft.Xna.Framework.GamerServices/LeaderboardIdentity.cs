namespace Microsoft.Xna.Framework.GamerServices;

public struct LeaderboardIdentity
{
	public string Key { get; set; }

	public int GameMode { get; set; }

	public static LeaderboardIdentity Create(LeaderboardKey key)
	{
		return new LeaderboardIdentity
		{
			Key = key.ToString(),
			GameMode = 0
		};
	}

	public static LeaderboardIdentity Create(LeaderboardKey key, int gameMode)
	{
		return new LeaderboardIdentity
		{
			Key = key.ToString(),
			GameMode = gameMode
		};
	}
}
