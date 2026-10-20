namespace Microsoft.Xna.Framework.Content;

internal class Int32Reader : ContentTypeReader<int>
{
	internal Int32Reader()
	{
	}

	protected internal override int Read(ContentReader input, int existingInstance)
	{
		return input.ReadInt32();
	}
}
