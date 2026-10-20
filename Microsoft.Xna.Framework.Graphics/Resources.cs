using System.IO;

namespace Microsoft.Xna.Framework.Graphics;

internal class Resources
{
	private static byte[] alphaTestEffect;

	private static byte[] basicEffect;

	private static byte[] dualTextureEffect;

	private static byte[] environmentMapEffect;

	private static byte[] skinnedEffect;

	private static byte[] spriteEffect;

	private static byte[] yuvToRGBAEffect;

	public static byte[] AlphaTestEffect
	{
		get
		{
			if (alphaTestEffect == null)
			{
				alphaTestEffect = GetResource("AlphaTestEffect");
			}
			return alphaTestEffect;
		}
	}

	public static byte[] BasicEffect
	{
		get
		{
			if (basicEffect == null)
			{
				basicEffect = GetResource("BasicEffect");
			}
			return basicEffect;
		}
	}

	public static byte[] DualTextureEffect
	{
		get
		{
			if (dualTextureEffect == null)
			{
				dualTextureEffect = GetResource("DualTextureEffect");
			}
			return dualTextureEffect;
		}
	}

	public static byte[] EnvironmentMapEffect
	{
		get
		{
			if (environmentMapEffect == null)
			{
				environmentMapEffect = GetResource("EnvironmentMapEffect");
			}
			return environmentMapEffect;
		}
	}

	public static byte[] SkinnedEffect
	{
		get
		{
			if (skinnedEffect == null)
			{
				skinnedEffect = GetResource("SkinnedEffect");
			}
			return skinnedEffect;
		}
	}

	public static byte[] SpriteEffect
	{
		get
		{
			if (spriteEffect == null)
			{
				spriteEffect = GetResource("SpriteEffect");
			}
			return spriteEffect;
		}
	}

	public static byte[] YUVToRGBAEffect
	{
		get
		{
			if (yuvToRGBAEffect == null)
			{
				yuvToRGBAEffect = GetResource("YUVToRGBAEffect");
			}
			return yuvToRGBAEffect;
		}
	}

	private static byte[] GetResource(string name)
	{
		Stream manifestResourceStream = typeof(Resources).Assembly.GetManifestResourceStream("Microsoft.Xna.Framework.Graphics.Effect.Resources." + name + ".fxb");
		using MemoryStream memoryStream = new MemoryStream();
		manifestResourceStream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}
}
