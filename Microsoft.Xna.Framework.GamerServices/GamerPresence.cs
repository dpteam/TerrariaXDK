namespace Microsoft.Xna.Framework.GamerServices;

public sealed class GamerPresence
{
	private string presence;

	private GamerPresenceMode presenceMode;

	private int presenceValue;

	private static readonly string[] presenceModeStrings = new string[60]
	{
		"Arcade Mode",
		"At Menu",
		"Battling Boss",
		"Campaign Mode",
		"Challenge Mode",
		"Configuring Settings",
		"Co-Op: Level {0}",
		"Co-Op: Stage {0}",
		"Cornflower Blue",
		"Customizing Player",
		"Difficulty: Easy",
		"Difficulty: Extreme",
		"Difficulty: Hard",
		"Difficulty: Medium",
		"Editing Level",
		"Exploration Mode",
		"Found Secret",
		"Free Play",
		"Game Over",
		"In Combat",
		"In Game Store",
		"Level {0}",
		"Local Co-Op",
		"Local Versus",
		"Looking For Games",
		"Losing",
		"Multiplayer",
		"Nearly Finished",
		string.Empty,
		"On a Roll",
		"Online Co-Op",
		"Online Versus",
		"Outnumbered",
		"Paused",
		"Playing Minigame",
		"Playing With Friends",
		"Practice Mode",
		"Puzzle Mode",
		"Scenario Mode",
		"Score {0}",
		"Score is Tied",
		"Setting Up Match",
		"Single Player",
		"Stage {0}",
		"Starting Game",
		"Story Mode",
		"Stuck on a Hard Bit",
		"Survival Mode",
		"Time Attack",
		"Trying For Record",
		"Tutorial Mode",
		"Versus Computer",
		"Versus: Score {0}",
		"Waiting For Players",
		"Waiting In Lobby",
		"Wasting Time",
		"Watching Credits",
		"Watching Cutscene",
		"Winning",
		"Won the Game"
	};

	public GamerPresenceMode PresenceMode
	{
		get
		{
			return presenceMode;
		}
		set
		{
			string text = presenceModeStrings[(int)value];
			if (text != presence)
			{
				presence = text;
				SetPresenceModeStringEXT(presence);
			}
			presenceMode = value;
		}
	}

	public int PresenceValue
	{
		get
		{
			return presenceValue;
		}
		set
		{
			if (value != presenceValue)
			{
				presenceValue = value;
				SetPresenceModeStringEXT(presence);
			}
		}
	}

	internal GamerPresence()
	{
		presenceMode = GamerPresenceMode.None;
		PresenceValue = 0;
	}

	public void SetPresenceModeStringEXT(string mode)
	{
	}
}
