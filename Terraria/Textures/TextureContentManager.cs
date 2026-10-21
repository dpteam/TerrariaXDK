using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Textures;

/// <summary>
/// A <see cref="ContentManager"/> that lets a loose PNG stand in for a compiled texture.
///
/// Subclassing rather than wrapping is deliberate: there are 26 <c>Content.Load&lt;Texture2D&gt;</c>
/// call sites, several of them building asset names by concatenation
/// (<c>"Images/Background_" + index</c>, <c>"UI/Upsell/0" + n + "_" + Lang.LanguageId</c>). A
/// helper function would have to be threaded through every one of them and would silently be
/// forgotten in the next list of backgrounds. Overriding <c>Load</c> covers all of them at once,
/// including ones added later.
///
/// Everything else passes straight through, so this is not a reimplementation of the content
/// pipeline - it is one hook in front of it.
/// </summary>
internal sealed class TextureContentManager : ContentManager
{
	public TextureContentManager(IServiceProvider services)
		: base(services)
	{
		Terraria.Textures.TextureOverrides.EnsureFolder();
	}

	public override T Load<T>(string assetName)
	{
		// typeof(T) is compared, not asked for by name: this must work for the closed generic
		// calls the compiler generates, and casting through object is the only way to return a
		// Texture2D as T without reflection.
		if (typeof(T) == typeof(Texture2D))
		{
			string path;
			ImageData imageData = Terraria.Textures.TextureOverrides.Load(assetName, out path);
			if (imageData != null)
			{
				Texture2D texture = Upload(imageData);
				if (texture != null)
				{
					return (T)(object)texture;
				}
			}
		}
		return base.Load<T>(assetName);
	}

	/// <summary>
	/// Copies decoded pixels into a GPU texture. The only part of this feature that needs a
	/// device, and therefore the only part the headless suite cannot exercise - which is exactly
	/// why the decoding and the colour conversion were kept out of it.
	/// </summary>
	private Texture2D Upload(ImageData image)
	{
		try
		{
			IGraphicsDeviceService graphicsDeviceService = ServiceProvider == null
				? null
				: (ServiceProvider.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService);
			if (graphicsDeviceService == null || graphicsDeviceService.GraphicsDevice == null)
			{
				Program.LogWarn("Textures", "no graphics device yet; using the built-in texture");
				return null;
			}
			Texture2D texture = new Texture2D(graphicsDeviceService.GraphicsDevice, image.Width, image.Height, mipMap: false, SurfaceFormat.Color);
			texture.SetData(ToColors(image));
			return texture;
		}
		catch (Exception ex)
		{
			Program.LogWarn("Textures", "could not upload an override (" + ex.GetType().Name + ": " + ex.Message + "); using the built-in texture");
			return null;
		}
	}

	/// <summary>
	/// RGBA bytes to <see cref="Color"/>, premultiplying alpha.
	///
	/// The alpha multiplier is not optional. XNA's content pipeline stores textures with
	/// premultiplied alpha and <c>SpriteBatch</c>'s default blend state assumes it; handing it
	/// straight alpha makes every soft edge and translucent pixel too bright, which looks like a
	/// colour-space bug rather than a loading bug. A PNG keeps straight alpha, so the conversion
	/// has to do it.
	///
	/// Static and free of graphics types so a test can check the arithmetic directly.
	/// </summary>
	internal static Color[] ToColors(ImageData image)
	{
		if (image == null)
		{
			throw new ArgumentNullException("image");
		}
		Color[] array = new Color[image.Width * image.Height];
		byte[] pixels = image.Pixels;
		for (int i = 0; i < array.Length; i++)
		{
			int num = i * ImageData.BytesPerPixel;
			byte b = pixels[num + 3];
			if (b == 255)
			{
				// Fully opaque: the common case, and premultiplying by 255/255 would be a no-op.
				array[i] = new Color(pixels[num], pixels[num + 1], pixels[num + 2], 255);
				continue;
			}
			if (b == 0)
			{
				// Fully transparent: the RGB is meaningless (and often black); keep it zero so a
				// later bilinear sample cannot pull colour out of thin air.
				array[i] = new Color(0, 0, 0, 0);
				continue;
			}
			int num2 = (pixels[num] * b + 127) / 255;
			int num3 = (pixels[num + 1] * b + 127) / 255;
			int num4 = (pixels[num + 2] * b + 127) / 255;
			array[i] = new Color(num2, num3, num4, b);
		}
		return array;
	}
}
