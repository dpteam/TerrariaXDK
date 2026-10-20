namespace Microsoft.Xna.Framework.Content;

internal class Vector4Reader : ContentTypeReader<Vector4>
{
	internal Vector4Reader()
	{
	}

	protected internal override Vector4 Read(ContentReader input, Vector4 existingInstance)
	{
		return input.ReadVector4();
	}
}
