namespace Microsoft.Xna.Framework.Content;

internal class DecimalReader : ContentTypeReader<decimal>
{
	internal DecimalReader()
	{
	}

	protected internal override decimal Read(ContentReader input, decimal existingInstance)
	{
		return input.ReadDecimal();
	}
}
