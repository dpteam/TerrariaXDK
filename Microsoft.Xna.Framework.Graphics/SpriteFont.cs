using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class SpriteFont
{
	internal Texture2D textureValue;

	internal List<Rectangle> glyphData;

	internal List<Rectangle> croppingData;

	internal List<Vector3> kerning;

	internal List<char> characterMap;

	internal int lineSpacing;

	internal float spacing;

	internal Dictionary<char, int> characterIndexMap;

	public ReadOnlyCollection<char> Characters { get; private set; }

	public char? DefaultCharacter { get; set; }

	public int LineSpacing
	{
		get
		{
			return lineSpacing;
		}
		set
		{
			lineSpacing = value;
		}
	}

	public float Spacing
	{
		get
		{
			return spacing;
		}
		set
		{
			spacing = value;
		}
	}

	internal SpriteFont(Texture2D texture, List<Rectangle> glyphBounds, List<Rectangle> cropping, List<char> characters, int lineSpacing, float spacing, List<Vector3> kerningData, char? defaultCharacter)
	{
		Characters = new ReadOnlyCollection<char>(characters.ToArray());
		DefaultCharacter = defaultCharacter;
		LineSpacing = lineSpacing;
		Spacing = spacing;
		textureValue = texture;
		glyphData = glyphBounds;
		croppingData = cropping;
		kerning = kerningData;
		characterMap = characters;
		characterIndexMap = new Dictionary<char, int>(characters.Count);
		for (int i = 0; i < characters.Count; i++)
		{
			characterIndexMap[characters[i]] = i;
		}
	}

	public Vector2 MeasureString(string text)
	{
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		if (text.Length == 0)
		{
			return Vector2.Zero;
		}
		Vector2 zero = Vector2.Zero;
		float num = 0f;
		float num2 = LineSpacing;
		bool flag = true;
		foreach (char c in text)
		{
			switch (c)
			{
			case '\n':
				zero.X = Math.Max(zero.X, num);
				zero.Y += LineSpacing;
				num = 0f;
				num2 = LineSpacing;
				flag = true;
				continue;
			case '\r':
				continue;
			}
			if (!characterIndexMap.TryGetValue(c, out var value))
			{
				if (!DefaultCharacter.HasValue)
				{
					throw new ArgumentException("Text contains characters that cannot be resolved by this SpriteFont.", "text");
				}
				value = characterIndexMap[DefaultCharacter.Value];
			}
			Vector3 vector = kerning[value];
			if (flag)
			{
				num += Math.Abs(vector.X);
				flag = false;
			}
			else
			{
				num += Spacing + vector.X;
			}
			num += vector.Y + vector.Z;
			int height = croppingData[value].Height;
			if ((float)height > num2)
			{
				num2 = height;
			}
		}
		zero.X = Math.Max(zero.X, num);
		zero.Y += num2;
		return zero;
	}

	public Vector2 MeasureString(StringBuilder text)
	{
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		if (text.Length == 0)
		{
			return Vector2.Zero;
		}
		Vector2 zero = Vector2.Zero;
		float num = 0f;
		float num2 = LineSpacing;
		bool flag = true;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			switch (c)
			{
			case '\n':
				zero.X = Math.Max(zero.X, num);
				zero.Y += LineSpacing;
				num = 0f;
				num2 = LineSpacing;
				flag = true;
				continue;
			case '\r':
				continue;
			}
			if (!characterIndexMap.TryGetValue(c, out var value))
			{
				if (!DefaultCharacter.HasValue)
				{
					throw new ArgumentException("Text contains characters that cannot be resolved by this SpriteFont.", "text");
				}
				value = characterIndexMap[DefaultCharacter.Value];
			}
			Vector3 vector = kerning[value];
			if (flag)
			{
				num += Math.Abs(vector.X);
				flag = false;
			}
			else
			{
				num += Spacing + vector.X;
			}
			num += vector.Y + vector.Z;
			int height = croppingData[value].Height;
			if ((float)height > num2)
			{
				num2 = height;
			}
		}
		zero.X = Math.Max(zero.X, num);
		zero.Y += num2;
		return zero;
	}
}
