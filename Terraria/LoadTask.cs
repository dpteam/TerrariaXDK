using System;

namespace Terraria;

/// <summary>
/// One unit of content loading. The loading screen executes exactly one task per frame, which
/// is what makes the bar advance continuously: a single blocking call per "phase" left the
/// bar frozen for seconds and then jumping forward.
/// </summary>
public sealed class LoadTask
{
	public readonly string Label;

	/// <summary>Relative cost. Used to make the bar reflect real work, not task count.</summary>
	public readonly float Weight;

	public readonly Action Run;

	public LoadTask(string label, float weight, Action run)
	{
		Label = label;
		Weight = weight;
		Run = run;
	}
}
