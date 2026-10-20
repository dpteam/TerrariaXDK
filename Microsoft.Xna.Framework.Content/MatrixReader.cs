namespace Microsoft.Xna.Framework.Content;

internal class MatrixReader : ContentTypeReader<Matrix>
{
	protected internal override Matrix Read(ContentReader input, Matrix existingInstance)
	{
		return new Matrix(input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle(), input.ReadSingle());
	}
}
