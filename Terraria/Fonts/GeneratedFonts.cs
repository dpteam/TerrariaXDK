using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Fonts;

/// <summary>
/// Builds the game's text fonts from <c>Storage/Fonts/</c> when the selected language has a
/// generated atlas there, and reports what it found.
///
/// The baked atlases stay the default. This only replaces them for a language that has a set, so
/// a missing, broken or half-written file set degrades to exactly what the game does today rather
/// than to no text at all. That is the whole reason this is a separate step instead of a rewrite
/// of <c>UI.BuildFonts</c>: the new path must never be able to make things worse.
/// </summary>
internal static class GeneratedFonts
{
	/// <summary>The three text fonts a language set has to provide.</summary>
	internal static readonly string[] Kinds = new string[3] { "big", "small", "small2" };

	/// <summary>Why the last attempt did nothing, or empty when it worked.</summary>
	public static string LastProblem { get; private set; } = string.Empty;

	/// <summary>Language code whose set was loaded, or empty.</summary>
	public static string LoadedCode { get; private set; } = string.Empty;

	/// <summary>Path of the metrics file for one font of one language.</summary>
	internal static string MetricsPath(string code, string kind)
	{
		return Path.Combine(Terraria.Storage.AppPaths.Fonts, code + "." + kind + ".metrics");
	}

	/// <summary>True when every font of the set is present, so the caller knows before it tries.</summary>
	internal static bool IsComplete(string code)
	{
		if (string.IsNullOrEmpty(code))
		{
			return false;
		}
		for (int i = 0; i < Kinds.Length; i++)
		{
			if (!File.Exists(MetricsPath(code, Kinds[i])))
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Reads one metrics file and its texture. Returns null with a reason for anything wrong -
	/// including a texture that cannot be decoded, which is the common case when someone copies a
	/// PNG that is not one.
	///
	/// <paramref name="texture"/> is handed back because a <see cref="SpriteFont"/> has no
	/// <c>Dispose</c> in this build: whoever fails later has to release the texture itself.
	/// </summary>
	internal static SpriteFont Build(GraphicsDevice device, string code, string kind, out Texture2D texture, out string error)
	{
		texture = null;
		error = null;
		string text = MetricsPath(code, kind);
		string text2;
		try
		{
			text2 = File.ReadAllText(text);
		}
		catch (Exception ex)
		{
			error = "cannot read " + text + ": " + ex.Message;
			return null;
		}
		FontAtlas fontAtlas = FontAtlas.Parse(text2, out error);
		if (fontAtlas == null)
		{
			error = Path.GetFileName(text) + ": " + error;
			return null;
		}
		string text3 = Path.Combine(Path.GetDirectoryName(text), fontAtlas.TexturePath);
		Texture2D texture2D;
		try
		{
			using FileStream stream = File.OpenRead(text3);
			texture2D = Texture2D.FromStream(device, stream);
		}
		catch (Exception ex2)
		{
			error = "cannot load " + text3 + ": " + ex2.Message;
			return null;
		}
		if (texture2D == null)
		{
			error = "cannot load " + text3;
			return null;
		}
		// Every glyph must be inside the texture. A metrics file that was edited by hand, or a
		// texture that was resized, otherwise draws garbage from the edge of the image.
		for (int i = 0; i < fontAtlas.Count; i++)
		{
			Microsoft.Xna.Framework.Rectangle rectangle = fontAtlas.GlyphBounds[i];
			if (rectangle.X + rectangle.Width > texture2D.Width || rectangle.Y + rectangle.Height > texture2D.Height)
			{
				error = Path.GetFileName(text) + ": glyph " + ((int)fontAtlas.Characters[i]).ToString()
					+ " lies outside the " + texture2D.Width + "x" + texture2D.Height + " texture";
				texture2D.Dispose();
				return null;
			}
		}
		try
		{
			SpriteFont result = FontAtlas.BuildSpriteFont(texture2D, fontAtlas);
			texture = texture2D;
			return result;
		}
		catch (Exception ex3)
		{
			error = "cannot build the font: " + ex3.Message;
			texture2D.Dispose();
			return null;
		}
	}

	/// <summary>
	/// Tries to build all three fonts for <paramref name="code"/>. All or nothing: a language with
	/// only two of the three fonts would render the menu and leave the HUD blank.
	/// </summary>
	internal static bool TryBuildAll(GraphicsDevice device, string code, out SpriteFont big, out SpriteFont small, out SpriteFont small2)
	{
		big = null;
		small = null;
		small2 = null;
		LastProblem = string.Empty;
		LoadedCode = string.Empty;
		if (device == null)
		{
			LastProblem = "no graphics device";
			return false;
		}
		if (!IsComplete(code))
		{
			// Not an error: most languages do not have a generated set, and never will.
			return false;
		}
		string error;
		Texture2D texture;
		Texture2D texture2;
		Texture2D texture3;
		SpriteFont spriteFont = Build(device, code, "big", out texture, out error);
		if (spriteFont == null)
		{
			LastProblem = error;
			return false;
		}
		SpriteFont spriteFont2 = Build(device, code, "small", out texture2, out error);
		if (spriteFont2 == null)
		{
			LastProblem = error;
			texture.Dispose();
			return false;
		}
		SpriteFont spriteFont3 = Build(device, code, "small2", out texture3, out error);
		if (spriteFont3 == null)
		{
			LastProblem = error;
			texture.Dispose();
			texture2.Dispose();
			return false;
		}
		big = spriteFont;
		small = spriteFont2;
		small2 = spriteFont3;
		LoadedCode = code;
		return true;
	}
}
