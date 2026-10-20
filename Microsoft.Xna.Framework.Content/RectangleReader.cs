namespace Microsoft.Xna.Framework.Content;

internal class RectangleReader : ContentTypeReader<Rectangle>
{
	internal RectangleReader()
	{
	}

	protected internal override Rectangle Read(ContentReader input, Rectangle existingInstance)
	{
		int x = input.ReadInt32();
		int y = input.ReadInt32();
		int width = input.ReadInt32();
		int height = input.ReadInt32();
		return new Rectangle(x, y, width, height);
	}
}
