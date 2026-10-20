namespace Microsoft.Xna.Framework.Content;

internal class PlaneReader : ContentTypeReader<Plane>
{
	internal PlaneReader()
	{
	}

	protected internal override Plane Read(ContentReader input, Plane existingInstance)
	{
		existingInstance.Normal = input.ReadVector3();
		existingInstance.D = input.ReadSingle();
		return existingInstance;
	}
}
