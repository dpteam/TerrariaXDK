namespace Microsoft.Xna.Framework.Content;

internal class BoundingBoxReader : ContentTypeReader<BoundingBox>
{
	protected internal override BoundingBox Read(ContentReader input, BoundingBox existingInstance)
	{
		return new BoundingBox(input.ReadVector3(), input.ReadVector3());
	}
}
