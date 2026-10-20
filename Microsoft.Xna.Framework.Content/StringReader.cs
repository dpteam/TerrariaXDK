namespace Microsoft.Xna.Framework.Content;

internal class StringReader : ContentTypeReader<string>
{
	internal StringReader()
	{
	}

	protected internal override string Read(ContentReader input, string existingInstance)
	{
		return input.ReadString();
	}
}
