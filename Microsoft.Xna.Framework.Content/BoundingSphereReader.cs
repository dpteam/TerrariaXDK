namespace Microsoft.Xna.Framework.Content;

internal class BoundingSphereReader : ContentTypeReader<BoundingSphere>
{
	internal BoundingSphereReader()
	{
	}

	protected internal override BoundingSphere Read(ContentReader input, BoundingSphere existingInstance)
	{
		Vector3 center = input.ReadVector3();
		float radius = input.ReadSingle();
		return new BoundingSphere(center, radius);
	}
}
