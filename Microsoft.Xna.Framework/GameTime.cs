using System;

namespace Microsoft.Xna.Framework;

public class GameTime
{
	public TimeSpan TotalGameTime { get; internal set; }

	public TimeSpan ElapsedGameTime { get; internal set; }

	public bool IsRunningSlowly { get; internal set; }

	public GameTime()
	{
		TotalGameTime = TimeSpan.Zero;
		ElapsedGameTime = TimeSpan.Zero;
		IsRunningSlowly = false;
	}

	public GameTime(TimeSpan totalGameTime, TimeSpan elapsedGameTime)
	{
		TotalGameTime = totalGameTime;
		ElapsedGameTime = elapsedGameTime;
		IsRunningSlowly = false;
	}

	public GameTime(TimeSpan totalRealTime, TimeSpan elapsedRealTime, bool isRunningSlowly)
	{
		TotalGameTime = totalRealTime;
		ElapsedGameTime = elapsedRealTime;
		IsRunningSlowly = isRunningSlowly;
	}
}
