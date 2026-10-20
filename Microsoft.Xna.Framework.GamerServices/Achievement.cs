using System;
using System.IO;

namespace Microsoft.Xna.Framework.GamerServices;

public sealed class Achievement
{
	public string Description { get; private set; }

	public bool DisplayBeforeEarned { get; private set; }

	public DateTime EarnedDateTime { get; private set; }

	public bool EarnedOnline { get; private set; }

	public int GamerScore { get; private set; }

	public string HowToEarn { get; private set; }

	public bool IsEarned { get; private set; }

	public string Key { get; private set; }

	public string Name { get; private set; }

	internal Achievement(string key, string name, string description, bool showBeforeEarned, bool earned, DateTime earnedDateTime)
	{
		Key = key;
		Name = name;
		Description = description;
		DisplayBeforeEarned = showBeforeEarned;
		IsEarned = earned;
		EarnedDateTime = earnedDateTime;
		EarnedOnline = true;
		GamerScore = 0;
		HowToEarn = string.Empty;
	}

	public Stream GetPicture()
	{
		throw new NotImplementedException();
	}
}
