using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content;

internal class BasicEffectReader : ContentTypeReader<BasicEffect>
{
	protected internal override BasicEffect Read(ContentReader input, BasicEffect existingInstance)
	{
		BasicEffect basicEffect = new BasicEffect(input.ContentManager.GetGraphicsDevice());
		if (input.ReadExternalReference<Texture>() is Texture2D texture)
		{
			basicEffect.Texture = texture;
			basicEffect.TextureEnabled = true;
		}
		basicEffect.DiffuseColor = input.ReadVector3();
		basicEffect.EmissiveColor = input.ReadVector3();
		basicEffect.SpecularColor = input.ReadVector3();
		basicEffect.SpecularPower = input.ReadSingle();
		basicEffect.Alpha = input.ReadSingle();
		basicEffect.VertexColorEnabled = input.ReadBoolean();
		return basicEffect;
	}
}
