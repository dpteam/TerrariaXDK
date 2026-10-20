namespace Microsoft.Xna.Framework.Content;

internal class Int64Reader : ContentTypeReader<long>
{
	internal Int64Reader()
	{
	}

	protected internal override long Read(ContentReader input, long existingInstance)
	{
		return input.ReadInt64();
	}
}
