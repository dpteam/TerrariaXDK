namespace Microsoft.Xna.Framework.Content;

internal class UInt32Reader : ContentTypeReader<uint>
{
	internal UInt32Reader()
	{
	}

	protected internal override uint Read(ContentReader input, uint existingInstance)
	{
		return input.ReadUInt32();
	}
}
