namespace Microsoft.Xna.Framework.Graphics;

public sealed class SamplerStateCollection
{
	private readonly SamplerState[] samplers;

	private readonly bool[] modifiedSamplers;

	public SamplerState this[int index]
	{
		get
		{
			return samplers[index];
		}
		set
		{
			samplers[index] = value;
			modifiedSamplers[index] = true;
		}
	}

	internal SamplerStateCollection(int slots, bool[] modSamplers)
	{
		samplers = new SamplerState[slots];
		modifiedSamplers = modSamplers;
		for (int i = 0; i < samplers.Length; i++)
		{
			samplers[i] = SamplerState.LinearWrap;
		}
	}
}
