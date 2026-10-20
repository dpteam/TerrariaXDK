using System.Collections.Generic;

namespace Microsoft.Xna.Framework.GamerServices;

public sealed class LeaderboardEntry
{
	private long rating;

	public PropertyDictionary Columns { get; private set; }

	public Gamer Gamer { get; private set; }

	public long Rating
	{
		get
		{
			return rating;
		}
		set
		{
			rating = value;
		}
	}

	public int RankingEXT { get; private set; }

	internal LeaderboardEntry(Gamer gamer, long rating, int ranking)
	{
		Gamer = gamer;
		this.rating = rating;
		RankingEXT = ranking;
		Columns = new PropertyDictionary(new Dictionary<string, object>());
	}
}
