namespace Microsoft.Xna.Framework.Content;

internal class SByteReader : ContentTypeReader<sbyte>
{
	internal SByteReader()
	{
	}

	protected internal override sbyte Read(ContentReader input, sbyte existingInstance)
	{
		return input.ReadSByte();
	}
}
