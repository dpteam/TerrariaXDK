using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.HowToPlay;

namespace Terraria;

public sealed class CompiledText
{
	public enum MarkupType
	{
		Plain,
		Html
	}

	[Flags]
	public enum Attributes
	{
		None = 0,
		Italic = 1,
		Bold = 2,
		Highlighted = 4,
		Center = 8,
		Right = 0x10,
		TextStyle = Italic | Bold | Highlighted,
		Alignment = Center | Right
	}

	public class Style
	{
		public Color ForegroundColor;

		public SpriteFont Font;

		public Attributes TextAttributes;

		public Style(SpriteFont FontType)
		{
			ForegroundColor = Color.Transparent;
			Font = FontType;
			TextAttributes = Attributes.None;
		}

		public Style(Style ActiveStyle)
		{
			ForegroundColor = ActiveStyle.ForegroundColor;
			Font = ActiveStyle.Font;
			TextAttributes = ActiveStyle.TextAttributes;
		}
	}

	private sealed class Segment
	{
		public string SegmentText;

		public short SourceCharIndex;

		public short SourceCharLength;

		public Rectangle Position;

		public Style Style;
	}

	private enum TagType
	{
		None,
		Bold,
		Italic,
		Underline,
		Link,
		Center,
		Right,
		Font,
		Paragraph,
		Highlighted,
		FontTitle
	}

	private sealed class OpenTag
	{
		public TagType Tag;

		public Style Style;

		public OpenTag(TagType FontTag, Style FontStyle)
		{
			Tag = FontTag;
			Style = FontStyle;
		}
	}

	public const short WidthOffset = 5;

	public const short HeightOffset = 12;

	private const int ParagraphBreakHeight = 1;

	public string TextString;

	public short Width;

	public short Height;

	private readonly List<Segment> Segments = new List<Segment>();

	private readonly Stack<OpenTag> Tags = new Stack<OpenTag>();

	public CompiledText(string Text, int WrapWidth, Style FontStyle, MarkupType MarkupType = MarkupType.Html)
	{
		Tags.Push(new OpenTag(TagType.None, FontStyle));
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		TextString = Text;
		Width = 0;
		Height = 0;
		int i = 0;
		while (i < Text.Length)
		{
			if (MarkupType == MarkupType.Html && Text[i] == '<' && Text[i + 1] == '/')
			{
				i += 2;
				SkipTag(Text, MarkupType, ref i, ShouldClose: true, out var Type, out var _);
				while (Tags.Count > 1 && Tags.Pop().Tag != Type)
				{
				}
				continue;
			}
			if (MarkupType == MarkupType.Html && Text[i] == '<')
			{
				i++;
				SkipTag(Text, MarkupType, ref i, ShouldClose: false, out var Type2, out var TextColor2);
				Style style = new Style(Tags.Peek().Style);
				switch (Type2)
				{
				case TagType.Bold:
					style.TextAttributes |= Attributes.Bold;
					break;
				case TagType.Center:
					style.TextAttributes |= Attributes.Center;
					break;
				case TagType.Font:
					style.ForegroundColor = TextColor2.Value;
					break;
				case TagType.Italic:
					style.TextAttributes |= Attributes.Italic;
					break;
				case TagType.Highlighted:
					style.TextAttributes |= Attributes.Highlighted;
					break;
				case TagType.Right:
					style.TextAttributes |= Attributes.Right;
					break;
				case TagType.FontTitle:
					style.Font = UI.BigFont;
					break;
				}
				if (Type2 != TagType.Paragraph)
				{
					Tags.Push(new OpenTag(Type2, style));
					continue;
				}
				num2 += Math.Max(UI.LineSpacing(Tags.Peek().Style.Font), num3) + 1;
				num = 0;
				num3 = 0;
				continue;
			}
			int num4 = i;
			int num5 = 0;
			while (i < Text.Length && !IsEol(Text[i]) && (MarkupType != MarkupType.Html || Text[i] != '<'))
			{
				i++;
				num5++;
			}
			if (WrapWidth == 0)
			{
				Vector2 vector = UI.MeasureString(Tags.Peek().Style.Font, Text.Substring(num4, num5));
				vector.X += UI.Spacing(Tags.Peek().Style.Font);
				vector.Y = UI.LineSpacing(Tags.Peek().Style.Font);
				if (Terraria.HowToPlay.UI.AdjustText)
				{
					vector.Y += 7f;
				}
				AddSegment(Text, num4, num5, Tags.Peek().Style, num, num2, (int)vector.X, (int)vector.Y);
				num += (int)vector.X;
				num3 = Math.Max(num3, (int)vector.Y);
			}
			else
			{
				while (num5 != 0)
				{
					int num6;
					if ((num6 = AnimationUtils.SqueezeText(Tags.Peek().Style.Font, Text.Substring(num4, num5), WrapWidth - num, AnimationUtils.SqueezeTextFlags.WordBreak)) == 0 && num != 0)
					{
						num2 += Math.Max(num3, UI.LineSpacing(Tags.Peek().Style.Font));
						num = 0;
						num3 = 0;
						num6 = AnimationUtils.SqueezeText(Tags.Peek().Style.Font, Text.Substring(num4, num5), WrapWidth - num, AnimationUtils.SqueezeTextFlags.WordBreak);
					}
					if (num6 == 0)
					{
						num6 = AnimationUtils.SqueezeText(Tags.Peek().Style.Font, Text.Substring(num4, num5), WrapWidth - num, AnimationUtils.SqueezeTextFlags.NoWordBreak);
					}
					if (num6 == 0)
					{
						break;
					}
					Vector2 vector2 = UI.MeasureString(Tags.Peek().Style.Font, Text.Substring(num4, num6));
					vector2.X += UI.Spacing(Tags.Peek().Style.Font);
					vector2.Y = UI.LineSpacing(Tags.Peek().Style.Font);
					if (Terraria.HowToPlay.UI.AdjustText)
					{
						vector2.Y += 7f;
					}
					AddSegment(Text, num4, num6, Tags.Peek().Style, num, num2, (int)vector2.X, (int)vector2.Y);
					num += (int)vector2.X;
					num3 = Math.Max(num3, (int)vector2.Y);
					if (num6 < num5)
					{
						num2 += Math.Max(num3, UI.LineSpacing(Tags.Peek().Style.Font));
						num = 0;
						num3 = 0;
					}
					num4 += num6;
					num5 -= num6;
					while (num5 != 0 && Text[num4] == ' ')
					{
						num5--;
						num4++;
					}
				}
			}
			for (; i < Text.Length && IsEol(Text[i]); i++)
			{
				if (Text[i] == '\n')
				{
					num2 += Math.Max(num3, UI.LineSpacing(Tags.Peek().Style.Font));
					num = 0;
					num3 = 0;
				}
			}
		}
		if (WrapWidth == 0)
		{
			return;
		}
		int num7 = 0;
		int num8 = -1;
		int num9 = -1;
		Attributes attributes = Attributes.None;
		for (int num10 = 0; num10 < Segments.Count; num10 = num9)
		{
			num9 = num10 + 1;
			if ((Segments[num10].Style.TextAttributes & Attributes.Alignment) != 0)
			{
				if (num8 == -1)
				{
					num8 = num10;
					num7 = Segments[num10].Position.Y;
					attributes = Segments[num10].Style.TextAttributes & Attributes.Alignment;
				}
				if (num9 == Segments.Count || Segments[num9].Position.Y != num7 || (Segments[num9].Style.TextAttributes & Attributes.Alignment) != attributes)
				{
					int num11 = Segments[num10].Position.Right - Segments[num8].Position.X;
					int num12 = (WrapWidth - num11) / (((attributes & Attributes.Center) == 0) ? 1 : 2);
					for (int j = num8; j != num9; j++)
					{
						Segments[j].Position.X += num12;
					}
					num8 = -1;
				}
			}
		}
	}

	public void Draw(SpriteBatch UnusedBatchTarget, Rectangle StringClip, Color BaseColour, Color AccentColour)
	{
		if (StringClip.Width == 0 || StringClip.Height == 0)
		{
			return;
		}
		foreach (Segment segment in Segments)
		{
			Color color = segment.Style.ForegroundColor;
			if ((segment.Style.TextAttributes & Attributes.Italic) != 0)
			{
				color = AccentColour;
			}
			else if ((segment.Style.TextAttributes & Attributes.Highlighted) != 0)
			{
				color = new Color(64, 255, 255, 255);
			}
			if (color == Color.Transparent)
			{
				color = BaseColour;
			}
			if (segment.Position.X < StringClip.Width && segment.Position.Y < StringClip.Height)
			{
				UI.DrawStringLT(segment.Style.Font, segment.SegmentText, StringClip.X + segment.Position.X, StringClip.Y + segment.Position.Y, color);
				if ((segment.Style.TextAttributes & Attributes.Bold) != 0)
				{
					UI.DrawStringLT(segment.Style.Font, segment.SegmentText, StringClip.X + segment.Position.X + 1, StringClip.Y + segment.Position.Y, color);
				}
			}
		}
	}

	public int CharHitTest(Point CharPoint, bool IsFuzzy)
	{
		int num = -1;
		int num2 = 0;
		int num3 = -1;
		int num4 = -1;
		for (int i = 0; i < Segments.Count; i++)
		{
			Segment segment = Segments[i];
			bool flag = i + 1 < Segments.Count && segment.Position.Y != Segments[i + 1].Position.Y;
			if (segment.Position.Contains(CharPoint))
			{
				return segment.SourceCharIndex + AnimationUtils.SqueezeText(segment.Style.Font, segment.SegmentText, CharPoint.X - segment.Position.Left, AnimationUtils.SqueezeTextFlags.NoWordBreak);
			}
			if (IsFuzzy)
			{
				if (segment.Position.Contains(new Point(segment.Position.X, CharPoint.Y)))
				{
					if (CharPoint.X >= segment.Position.Left)
					{
						num4 = segment.SourceCharIndex + segment.SourceCharLength - (flag ? 1 : 0);
					}
					else
					{
						num4 = ((num3 == segment.Position.Top) ? num4 : segment.SourceCharIndex);
					}
					num3 = segment.Position.Top;
				}
				else if (segment.Position.Top <= CharPoint.Y)
				{
					if (CharPoint.Y >= segment.Position.Top)
					{
						num2 = segment.SourceCharIndex + segment.SourceCharLength - (flag ? 1 : 0);
					}
					else
					{
						num2 = ((num == segment.Position.Top) ? num2 : segment.SourceCharIndex);
					}
					num = segment.Position.Top;
				}
			}
		}
		if (IsFuzzy)
		{
			if (num4 == -1)
			{
				return num2;
			}
			return num4;
		}
		return -1;
	}

	public Point CharFind(int CharIdx)
	{
		for (int i = 0; i < Segments.Count; i++)
		{
			Segment segment = Segments[i];
			if (i + 1 == Segments.Count || Segments[i + 1].SourceCharIndex > CharIdx)
			{
				int num = segment.Position.X;
				int y = segment.Position.Y;
				if (CharIdx > segment.SourceCharIndex)
				{
					num += (int)UI.MeasureString(segment.Style.Font, segment.SegmentText.Substring(0, CharIdx - segment.SourceCharIndex)).X;
				}
				return new Point(num, y);
			}
		}
		return new Point(-1, -1);
	}

	private void SkipTag(string Text, MarkupType TagMarkup, ref int CharIdx, bool ShouldClose, out TagType Type, out Color? TextColor)
	{
		switch (Text[CharIdx])
		{
		case 'B':
		case 'b':
			if (Text[CharIdx + 1] == 'r' || Text[CharIdx + 1] == 'R')
			{
				Type = TagType.Paragraph;
			}
			else
			{
				Type = TagType.Bold;
			}
			break;
		case 'I':
		case 'i':
			Type = TagType.Italic;
			break;
		case 'C':
		case 'c':
			Type = TagType.Center;
			break;
		case 'F':
		case 'f':
			Type = TagType.Font;
			break;
		case 'R':
		case 'r':
			Type = TagType.Right;
			break;
		case 'P':
		case 'p':
			Type = TagType.Paragraph;
			break;
		case 'H':
		case 'h':
			Type = TagType.Highlighted;
			break;
		case 'T':
		case 't':
			Type = TagType.FontTitle;
			break;
		default:
			Type = TagType.None;
			break;
		}
		while (Text[CharIdx] != '=' && Text[CharIdx] != ' ' && Text[CharIdx] != '>')
		{
			CharIdx++;
		}
		if (Type == TagType.Font && TagMarkup == MarkupType.Html && Text[CharIdx] == ' ' && Text[CharIdx + 1] == 'c')
		{
			while (Text[CharIdx] != '=' && Text[CharIdx] != '>')
			{
				CharIdx++;
			}
		}
		TextColor = null;
		if (Type == TagType.Font && Text[CharIdx] == '=')
		{
			CharIdx++;
			TextColor = AnimationUtils.StringToColor(PopQuotedWord(Text, ref CharIdx, '>', DoStripQuotes: true));
		}
		CharIdx++;
	}

	private string PopQuotedWord(string Text, ref int CharIdx, char EndChar, bool DoStripQuotes)
	{
		int num = CharIdx;
		char c = Text[CharIdx];
		string result;
		if (c == '"' || c == '\'')
		{
			do
			{
				CharIdx++;
			}
			while (CharIdx < Text.Length && Text[CharIdx] != c);
			CharIdx++;
			result = ((!DoStripQuotes) ? Text.Substring(num, CharIdx - num) : Text.Substring(num + 1, CharIdx - num - 2));
		}
		else
		{
			while (CharIdx < Text.Length && Text[CharIdx] != EndChar && !IsWhiteEol(Text[CharIdx]))
			{
				CharIdx++;
			}
			result = Text.Substring(num, CharIdx - num);
		}
		while (CharIdx < Text.Length && IsWhiteEol(Text[CharIdx]))
		{
			CharIdx++;
		}
		return result;
	}

	private void AddSegment(string Text, int StartIndex, int TextLength, Style TextStyle, int BaseX, int BaseY, int ClipX, int ClipY)
	{
		Segment segment = new Segment
		{
			SegmentText = Text.Substring(StartIndex, TextLength),
			SourceCharIndex = (short)StartIndex,
			SourceCharLength = (short)TextLength,
			Position = new Rectangle(BaseX, BaseY, ClipX, ClipY),
			Style = TextStyle
		};
		Segments.Add(segment);
		Width = (short)Math.Max(Width, segment.Position.Right + 5);
		Height = (short)Math.Max(Height, segment.Position.Bottom + 12);
	}

	private bool IsWhiteEol(char TextChar)
	{
		if (TextChar != ' ' && TextChar != '\t' && TextChar != '\r')
		{
			return TextChar == '\n';
		}
		return true;
	}

	private bool IsEol(char TextChar)
	{
		if (TextChar != '\r')
		{
			return TextChar == '\n';
		}
		return true;
	}
}
