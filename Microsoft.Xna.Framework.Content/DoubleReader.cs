namespace Microsoft.Xna.Framework.Content;

internal class DoubleReader : ContentTypeReader<double>
{
	internal DoubleReader()
	{
	}

	protected internal override double Read(ContentReader input, double existingInstance)
	{
		return input.ReadDouble();
	}
}
