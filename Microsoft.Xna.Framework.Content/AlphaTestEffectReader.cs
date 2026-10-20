using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content;

internal class AlphaTestEffectReader : ContentTypeReader<AlphaTestEffect>
{
	protected internal override AlphaTestEffect Read(ContentReader input, AlphaTestEffect existingInstance)
	{
		AlphaTestEffect alphaTestEffect = new AlphaTestEffect(input.ContentManager.GetGraphicsDevice());
		alphaTestEffect.Texture = input.ReadExternalReference<Texture>() as Texture2D;
		alphaTestEffect.AlphaFunction = (CompareFunction)input.ReadInt32();
		alphaTestEffect.ReferenceAlpha = (int)input.ReadUInt32();
		alphaTestEffect.DiffuseColor = input.ReadVector3();
		alphaTestEffect.Alpha = input.ReadSingle();
		alphaTestEffect.VertexColorEnabled = input.ReadBoolean();
		return alphaTestEffect;
	}
}
