namespace Microsoft.Xna.Framework.Content;

internal class BoundingFrustumReader : ContentTypeReader<BoundingFrustum>
{
	internal BoundingFrustumReader()
	{
	}

	protected internal override BoundingFrustum Read(ContentReader input, BoundingFrustum existingInstance)
	{
		return new BoundingFrustum(input.ReadMatrix());
	}
}
