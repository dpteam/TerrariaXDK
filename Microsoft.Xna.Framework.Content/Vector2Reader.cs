namespace Microsoft.Xna.Framework.Content;

internal class Vector2Reader : ContentTypeReader<Vector2>
{
	internal Vector2Reader()
	{
	}

	protected internal override Vector2 Read(ContentReader input, Vector2 existingInstance)
	{
		return input.ReadVector2();
	}
}
