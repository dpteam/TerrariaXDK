using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Content;

internal class EffectReader : ContentTypeReader<Effect>
{
	private static string[] supportedExtensions = new string[1] { ".fxb" };

	internal static string Normalize(string FileName)
	{
		return ContentTypeReader.Normalize(FileName, supportedExtensions);
	}

	protected internal override Effect Read(ContentReader input, Effect existingInstance)
	{
		int count = input.ReadInt32();
		Effect effect = new Effect(input.ContentManager.GetGraphicsDevice(), input.ReadBytes(count));
		effect.Name = input.AssetName;
		return effect;
	}
}
