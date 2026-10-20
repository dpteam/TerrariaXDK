using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content;

internal class EnvironmentMapEffectReader : ContentTypeReader<EnvironmentMapEffect>
{
	protected internal override EnvironmentMapEffect Read(ContentReader input, EnvironmentMapEffect existingInstance)
	{
		EnvironmentMapEffect environmentMapEffect = new EnvironmentMapEffect(input.ContentManager.GetGraphicsDevice());
		environmentMapEffect.Texture = input.ReadExternalReference<Texture>() as Texture2D;
		environmentMapEffect.EnvironmentMap = input.ReadExternalReference<TextureCube>();
		environmentMapEffect.EnvironmentMapAmount = input.ReadSingle();
		environmentMapEffect.EnvironmentMapSpecular = input.ReadVector3();
		environmentMapEffect.FresnelFactor = input.ReadSingle();
		environmentMapEffect.DiffuseColor = input.ReadVector3();
		environmentMapEffect.EmissiveColor = input.ReadVector3();
		environmentMapEffect.Alpha = input.ReadSingle();
		return environmentMapEffect;
	}
}
