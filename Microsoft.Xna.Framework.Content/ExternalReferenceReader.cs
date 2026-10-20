namespace Microsoft.Xna.Framework.Content;

internal class ExternalReferenceReader : ContentTypeReader
{
	public ExternalReferenceReader()
		: base(null)
	{
	}

	protected internal override object Read(ContentReader input, object existingInstance)
	{
		return input.ReadExternalReference<object>();
	}
}
