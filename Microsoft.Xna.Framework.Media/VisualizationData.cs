using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Media;

public class VisualizationData
{
	internal const int Size = 256;

	internal float[] freq;

	internal float[] samp;

	public ReadOnlyCollection<float> Frequencies => new ReadOnlyCollection<float>(freq);

	public ReadOnlyCollection<float> Samples => new ReadOnlyCollection<float>(samp);

	public VisualizationData()
	{
		freq = new float[256];
		samp = new float[256];
	}
}
