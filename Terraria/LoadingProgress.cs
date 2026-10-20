namespace Terraria;

/// <summary>
/// Smoothing for the loading bar.
///
/// Two problems had to be solved at once:
/// <list type="bullet">
/// <item>a plain lerp approaches the target asymptotically and never arrives, so the bar
/// stopped a few pixels short of the end;</item>
/// <item>individual loading steps are large (a tile atlas is 30% of the work), so easing
/// alone still produced a visible rush whenever a heavy step finished.</item>
/// </list>
/// The step below moves at a bounded speed with ease-out, so big jumps are spread over
/// several frames and the motion reads as continuous, while the target is still reached
/// exactly.
/// </summary>
internal static class LoadingProgress
{
	/// <summary>Fraction of the remaining distance covered per frame (ease-out).</summary>
	public const float Smoothing = 0.08f;

	/// <summary>Never advance faster than this per frame, to hide large step boundaries.</summary>
	public const float MaxStepPerFrame = 0.012f;

	/// <summary>Always advance at least this much, so the bar never visually stalls.</summary>
	public const float MinStepPerFrame = 0.0015f;

	/// <summary>Distance at which the animation snaps exactly to the target.</summary>
	public const float SnapThreshold = 0.0005f;

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
		if (display < 0f)
		{
			display = 0f;
		}
		float num = target - display;
		if (num <= 0f)
		{
			return display;
		}
		if (num <= SnapThreshold)
		{
			return target;
		}
		float num2 = num * Smoothing;
		if (num2 < MinStepPerFrame)
		{
			num2 = MinStepPerFrame;
		}
		if (num2 > MaxStepPerFrame)
		{
			num2 = MaxStepPerFrame;
		}
		if (num2 > num)
		{
			num2 = num;
		}
		return display + num2;
	}
}
