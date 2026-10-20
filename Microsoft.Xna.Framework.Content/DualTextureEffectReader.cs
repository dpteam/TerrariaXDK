using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content;

internal class DualTextureEffectReader : ContentTypeReader<DualTextureEffect>
{
	protected internal override DualTextureEffect Read(ContentReader input, DualTextureEffect existingInstance)
	{
		DualTextureEffect dualTextureEffect = new DualTextureEffect(input.ContentManager.GetGraphicsDevice());
		dualTextureEffect.Texture = input.ReadExternalReference<Texture>() as Texture2D;
		dualTextureEffect.Texture2 = input.ReadExternalReference<Texture>() as Texture2D;
		dualTextureEffect.DiffuseColor = input.ReadVector3();
		dualTextureEffect.Alpha = input.ReadSingle();
		dualTextureEffect.VertexColorEnabled = input.ReadBoolean();
		return dualTextureEffect;
	}
}
