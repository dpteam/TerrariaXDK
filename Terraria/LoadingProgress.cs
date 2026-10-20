namespace Terraria;

/// <summary>
/// Smoothing for the loading bar.
///
/// A plain lerp towards the target approaches it asymptotically and never arrives, which is
/// why the bar used to stop a few pixels short of the end. The step below snaps once it is
/// close enough, so a target of 1.0 is always reached exactly.
/// </summary>
internal static class LoadingProgress
{
	/// <summary>How much of the remaining distance is covered per frame.</summary>
	public const float Smoothing = 0.12f;

	/// <summary>Distance at which the animation jumps straight to the target.</summary>
	public const float SnapThreshold = 0.004f;

	public static float Step(float display, float target)
	{
		if (target > 1f)
		{
			target = 1f;
		}
		else if (target < 0f)
		{
			target = 0f;
		}
		display += (target - display) * Smoothing;
		if (target - display < SnapThreshold)
		{
			display = target;
		}
		if (display > 1f)
		{
			display = 1f;
		}
		else if (display < 0f)
		{
			display = 0f;
		}
		return display;
	}
}
