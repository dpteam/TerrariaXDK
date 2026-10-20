namespace Microsoft.Xna.Framework.Content;

internal class UInt64Reader : ContentTypeReader<ulong>
{
	internal UInt64Reader()
	{
	}

	protected internal override ulong Read(ContentReader input, ulong existingInstance)
	{
		return input.ReadUInt64();
	}
}
