namespace Microsoft.Xna.Framework.Content;

internal class PointReader : ContentTypeReader<Point>
{
	internal PointReader()
	{
	}

	protected internal override Point Read(ContentReader input, Point existingInstance)
	{
		int x = input.ReadInt32();
		int y = input.ReadInt32();
		return new Point(x, y);
	}
}
