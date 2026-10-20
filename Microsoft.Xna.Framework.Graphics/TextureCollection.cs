using System;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class TextureCollection
{
	internal bool ignoreTargets;

	private readonly Texture[] textures;

	private readonly bool[] modifiedSamplers;

	public Texture this[int index]
	{
		get
		{
			return textures[index];
		}
		set
		{
			if (value != null)
			{
				if (value.IsDisposed)
				{
					throw new ObjectDisposedException(value.GetType().ToString());
				}
				if (!ignoreTargets)
				{
					for (int i = 0; i < value.GraphicsDevice.renderTargetCount; i++)
					{
						if (value == value.GraphicsDevice.renderTargetBindings[i].RenderTarget)
						{
							throw new InvalidOperationException("The render target must not be set on the device when it is used as a texture.");
						}
					}
				}
			}
			textures[index] = value;
			modifiedSamplers[index] = true;
		}
	}

	internal TextureCollection(int slots, bool[] modSamplers)
	{
		textures = new Texture[slots];
		modifiedSamplers = modSamplers;
		for (int i = 0; i < textures.Length; i++)
		{
			textures[i] = null;
		}
		ignoreTargets = false;
	}

	internal void RemoveDisposedTexture(Texture tex)
	{
		for (int i = 0; i < textures.Length; i++)
		{
			if (tex == textures[i])
			{
				this[i] = null;
			}
		}
	}
}
