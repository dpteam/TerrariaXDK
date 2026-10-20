namespace Microsoft.Xna.Framework.Content;

internal class ByteReader : ContentTypeReader<byte>
{
	internal ByteReader()
	{
	}

	protected internal override byte Read(ContentReader input, byte existingInstance)
	{
		return input.ReadByte();
	}
}
