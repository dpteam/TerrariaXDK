namespace Microsoft.Xna.Framework.Content;

internal class Int16Reader : ContentTypeReader<short>
{
	internal Int16Reader()
	{
	}

	protected internal override short Read(ContentReader input, short existingInstance)
	{
		return input.ReadInt16();
	}
}
