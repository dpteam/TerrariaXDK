using System;
using System.Globalization;
using System.IO;

namespace Microsoft.Xna.Framework.GamerServices;

public sealed class GamerProfile : IDisposable
{
	public int GamerScore { get; private set; }

	public GamerZone GamerZone { get; private set; }

	public string Motto { get; private set; }

	public RegionInfo Region { get; private set; }

	public float Reputation { get; private set; }

	public int TitlesPlayed { get; private set; }

	public int TotalAchievements { get; private set; }

	public bool IsDisposed { get; private set; }

	internal GamerProfile()
	{
		IsDisposed = false;
		GamerScore = 0;
		GamerZone = GamerZone.Pro;
		Motto = string.Empty;
		Region = RegionInfo.CurrentRegion;
		Reputation = 5f;
		TitlesPlayed = 1;
		TotalAchievements = 0;
	}

	public void Dispose()
	{
		IsDisposed = true;
	}

	public Stream GetGamerPicture()
	{
		return null;
	}
}
