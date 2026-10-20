using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria;

public static class AnimationUtils
{
	public enum SqueezeTextFlags
	{
		NoWordBreak,
		WordBreak
	}

	public static Color StringToColor(string Text)
	{
		if (Text.Length == 7 && Text[0] == '#')
		{
			int r = HexValue(Text[1]) * 16 + HexValue(Text[2]);
			int g = HexValue(Text[3]) * 16 + HexValue(Text[4]);
			int b = HexValue(Text[5]) * 16 + HexValue(Text[6]);
			return new Color(r, g, b);
		}
		if (Text.Length >= 11 && Text[0] == '{' && Text[Text.Length - 1] == '}')
		{
			int r2 = 0;
			int g2 = 0;
			int b2 = 0;
			int alpha = 0;
			int i = 1;
			while (i + 2 < Text.Length && (char.ToLower(Text[i]) == 'a' || char.ToLower(Text[i]) == 'r' || char.ToLower(Text[i]) == 'g' || char.ToLower(Text[i]) == 'b') && Text[i + 1] == ':')
			{
				char c = char.ToLower(Text[i]);
				int num = 0;
				for (i += 2; char.IsDigit(Text[i]); i++)
				{
					num = num * 10 + (Text[i] - 48);
				}
				switch (c)
				{
				case 'r':
					r2 = num;
					break;
				case 'g':
					g2 = num;
					break;
				case 'b':
					b2 = num;
					break;
				default:
					alpha = num;
					break;
				}
				for (; Text[i] == ' '; i++)
				{
				}
			}
			return new Color(r2, g2, b2, alpha);
		}
		return Color.White;
	}

	private static int HexValue(char TextChar)
	{
		if (TextChar < '0' || TextChar > '9')
		{
			if (TextChar < 'a' || TextChar > 'z')
			{
				if (TextChar < 'A' || TextChar > 'Z')
				{
					return 0;
				}
				return TextChar - 65 + 10;
			}
			return TextChar - 97 + 10;
		}
		return TextChar - 48;
	}

	public static int SqueezeText(SpriteFont Font, string Text, int Width, SqueezeTextFlags ActiveFlags)
	{
		if (Width <= 0 || Text.Length == 0)
		{
			return 0;
		}
		if ((float)(int)UI.MeasureString(Font, Text).X + UI.Spacing(Font) <= (float)Width)
		{
			return Text.Length;
		}
		int num = 0;
		int num2 = Text.Length;
		do
		{
			int num3 = num + (num2 - num + 1 >> 1);
			if ((float)(int)UI.MeasureString(Font, Text.Substring(0, num3)).X + UI.Spacing(Font) <= (float)Width)
			{
				num = num3;
			}
			else
			{
				num2 = num3;
			}
		}
		while (num2 > num + 1);
		if ((ActiveFlags & SqueezeTextFlags.WordBreak) != 0)
		{
			while (num >= 1)
			{
				char c = Text[num - 1];
				if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
				{
					break;
				}
				num--;
			}
		}
		return num;
	}
}
