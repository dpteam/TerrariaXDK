using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Textures;

/// <summary>
/// Pastes an <see cref="AtlasPlan"/> into an already-baked atlas.
///
/// Two halves, split on purpose:
/// <list type="bullet">
/// <item><see cref="Apply"/> is pure CPU: it takes the atlas pixels as a <see cref="Color"/>
/// array and writes the patches in. No graphics device, so the geometry, the scaling and the
/// alpha handling are all testable headlessly.</item>
/// <item><see cref="Compose"/> is four lines around it: read the texture back, apply, upload.
/// Nothing to get wrong, and nothing worth pretending a test covers.</item>
/// </list>
///
/// Why read back the atlas instead of drawing into a render target: a render target means
/// mixing with the game's own <c>SpriteBatch</c> state during loading (Begin/End nesting is a
/// classic way to lose the splash screen), and a readback is deterministic. It costs one 16 MB
/// round trip per atlas, once, and only when there is something to paste.
/// </summary>
internal static class AtlasComposer
{
	/// <summary>
	/// Writes every patch into <paramref name="pixels"/>, which must be a
	/// <paramref name="width"/>x<paramref name="height"/> atlas in the same layout
	/// <c>Texture2D.GetData</c> returns. Returns how many pixels were written.
	///
	/// Out-of-range patches are skipped rather than throwing: the plan already rejected the ones
	/// it could see, and a texture whose size changed between planning and composing must not
	/// take the game down during loading.
	/// </summary>
	public static int Apply(Color[] pixels, int width, int height, AtlasPlan plan)
	{
		if (pixels == null || plan == null || plan.IsEmpty)
		{
			return 0;
		}
		if (pixels.Length < width * height)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < plan.Patches.Count; i++)
		{
			AtlasPatch atlasPatch = plan.Patches[i];
			if (atlasPatch.Image == null)
			{
				continue;
			}
			Rectangle rect = atlasPatch.Rect;
			if (rect.X < 0 || rect.Y < 0 || rect.Right > width || rect.Bottom > height)
			{
				continue;
			}
			ImageData image = atlasPatch.Image;
			for (int j = 0; j < rect.Height; j++)
			{
				for (int k = 0; k < rect.Width; k++)
				{
					// Nearest neighbour, and by source pixel rather than by destination pixel:
					// sprite art has hard edges, and bilinear would blur every replacement.
					int num2 = (rect.Width == image.Width) ? k : (k * image.Width) / rect.Width;
					int num3 = (rect.Height == image.Height) ? j : (j * image.Height) / rect.Height;
					pixels[(rect.Y + j) * width + rect.X + k] = ToPremultiplied(image, num2, num3);
					num++;
				}
			}
		}
		return num;
	}

	/// <summary>
	/// One source pixel as an XNA colour, premultiplied.
	///
	/// Same reasoning as <see cref="TextureContentManager.ToColors"/>: the content pipeline and
	/// <c>SpriteBatch</c>'s default blend state expect premultiplied alpha, and a PNG holds
	/// straight alpha. A patch that skipped this would be the one bright square in a correct
	/// sheet, which is harder to diagnose than everything being wrong.
	/// </summary>
	internal static Color ToPremultiplied(ImageData image, int x, int y)
	{
		byte b = image.A(x, y);
		if (b == 255)
		{
			return new Color(image.R(x, y), image.G(x, y), image.B(x, y), 255);
		}
		if (b == 0)
		{
			return new Color(0, 0, 0, 0);
		}
		return new Color((image.R(x, y) * b + 127) / 255, (image.G(x, y) * b + 127) / 255, (image.B(x, y) * b + 127) / 255, b);
	}

	/// <summary>
	/// Applies the plan to a loaded texture in place, replacing its pixels.
	///
	/// Returns false when there was nothing to do, or when the device refused the readback - in
	/// which case the atlas keeps its baked content, which is always a valid game, just without
	/// the author's sprites.
	/// </summary>
	public static bool Compose(Texture2D texture, AtlasPlan plan, out int pixelsWritten)
	{
		pixelsWritten = 0;
		if (texture == null || plan == null || plan.IsEmpty)
		{
			return false;
		}
		Color[] array;
		try
		{
			array = new Color[texture.Width * texture.Height];
			texture.GetData(array);
		}
		catch (Exception ex)
		{
			Program.LogWarn("Textures", "could not read the atlas back (" + ex.GetType().Name + ": " + ex.Message
				+ "); the sprite overrides were skipped");
			return false;
		}
		int num = Apply(array, texture.Width, texture.Height, plan);
		if (num == 0)
		{
			return false;
		}
		try
		{
			texture.SetData(array);
		}
		catch (Exception ex2)
		{
			Program.LogWarn("Textures", "could not upload the patched atlas (" + ex2.GetType().Name + ": " + ex2.Message
				+ "); the sprite overrides were skipped");
			return false;
		}
		pixelsWritten = num;
		return true;
	}

	/// <summary>
	/// Plans, composes, and reports. Kept here so both atlases call one function instead of
	/// repeating the same six lines, and so the log format is the same for both.
	/// </summary>
	public static void ComposeAtlas(string atlasName, Texture2D texture, string[] names, Rectangle[] source)
	{
		if (texture == null)
		{
			return;
		}
		AtlasOverrides.EnsureFolders();
		AtlasPlan atlasPlan = AtlasOverrides.Build(atlasName, names, source, texture.Width, texture.Height);
		for (int i = 0; i < atlasPlan.Problems.Count; i++)
		{
			Program.LogWarn("Textures", Portability.Portable(AtlasOverrides.FolderFor(atlasName)) + ": " + atlasPlan.Problems[i]);
		}
		for (int j = 0; j < atlasPlan.Notes.Count; j++)
		{
			Program.LogInfo("Textures", Portability.Portable(AtlasOverrides.FolderFor(atlasName)) + ": " + atlasPlan.Notes[j]);
		}
		if (atlasPlan.IsEmpty)
		{
			return;
		}
		int num;
		if (Compose(texture, atlasPlan, out num))
		{
			Program.LogInfo("Textures", "patched " + atlasPlan.Patches.Count + " sprite(s) into " + atlasName
				+ " (" + num + " pixel(s))");
		}
	}
}
