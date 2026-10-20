using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content;

internal class SkinnedEffectReader : ContentTypeReader<SkinnedEffect>
{
	protected internal override SkinnedEffect Read(ContentReader input, SkinnedEffect existingInstance)
	{
		SkinnedEffect skinnedEffect = new SkinnedEffect(input.ContentManager.GetGraphicsDevice());
		skinnedEffect.Texture = input.ReadExternalReference<Texture>() as Texture2D;
		skinnedEffect.WeightsPerVertex = input.ReadInt32();
		skinnedEffect.DiffuseColor = input.ReadVector3();
		skinnedEffect.EmissiveColor = input.ReadVector3();
		skinnedEffect.SpecularColor = input.ReadVector3();
		skinnedEffect.SpecularPower = input.ReadSingle();
		skinnedEffect.Alpha = input.ReadSingle();
		return skinnedEffect;
	}
}
