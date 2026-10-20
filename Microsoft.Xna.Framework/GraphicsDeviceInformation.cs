using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework;

public class GraphicsDeviceInformation
{
	public GraphicsAdapter Adapter { get; set; }

	public GraphicsProfile GraphicsProfile { get; set; }

	public PresentationParameters PresentationParameters { get; set; }

	public GraphicsDeviceInformation Clone()
	{
		return new GraphicsDeviceInformation
		{
			Adapter = Adapter,
			GraphicsProfile = GraphicsProfile,
			PresentationParameters = PresentationParameters.Clone()
		};
	}
}
