using System;

namespace Microsoft.Xna.Framework.Net;

public sealed class QualityOfService
{
	public TimeSpan AverageRoundtripTime { get; private set; }

	public int BytesPerSecondDownstream { get; private set; }

	public int BytesPerSecondUpstream { get; private set; }

	public bool IsAvailable { get; private set; }

	public TimeSpan MinimumRoundtripTime { get; private set; }

	internal QualityOfService()
	{
		AverageRoundtripTime = TimeSpan.Zero;
		BytesPerSecondDownstream = 0;
		BytesPerSecondUpstream = 0;
		IsAvailable = true;
		MinimumRoundtripTime = TimeSpan.Zero;
	}
}
