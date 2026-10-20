namespace Microsoft.Xna.Framework.Content;

internal class ColorReader : ContentTypeReader<Color>
{
	internal ColorReader()
	{
	}

	protected internal override Color Read(ContentReader input, Color existingInstance)
	{
		byte r = input.ReadByte();
		byte g = input.ReadByte();
		byte b = input.ReadByte();
		byte alpha = input.ReadByte();
		return new Color(r, g, b, alpha);
	}
}
