using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.HowToPlay;

public class HowToPlayLayout
{
	private struct Image
	{
		public Vector2 Position;

		public Texture2D Texture;
	}

	private static readonly int MaxWidth = Terraria.UI.UsableWidth;

	private static readonly int MaxHeight = Terraria.UI.USABLE_HEIGHT;

	private readonly Image[] HowToPlayAssets;

	private static Color TextColor = Color.White;

	private static Color AccentColor = new Color(255, 212, 64, 255);

	private static Color BackColor = Terraria.UI.DefaultDialogColor;

	public TextBlock Block;

	public static HowToPlayLayout TextOnlyLayout(string PageText, int WidthOffset)
	{
		int textBGBorderWidth = Assets.TextBGBorderWidth;
		CompiledText compiledText = new CompiledText(PageText, WidthOffset - textBGBorderWidth * 2, Terraria.UI.BoldSmallTextStyle);
		int num = Math.Min(compiledText.Height + Terraria.UI.BoldSmallFont.LineSpacing + textBGBorderWidth * 2, MaxHeight);
		int x = MaxWidth - WidthOffset >> 1;
		int y = MaxHeight - num >> 1;
		Rectangle DialogArea = new Rectangle(x, y, WidthOffset, num);
		Rectangle TextArea = DialogArea;
		TextArea.Inflate(-textBGBorderWidth, -textBGBorderWidth);
		return new HowToPlayLayout(new TextBlock(ref DialogArea, compiledText, ref TextArea, Assets.TextBG, textBGBorderWidth, BackColor, TextColor, AccentColor));
	}

	public static HowToPlayLayout SideBySideLayout(string Text, int WidthOffset, Texture2D Image, int Padding = 20)
	{
		int textBGBorderWidth = Assets.TextBGBorderWidth;
		CompiledText compiledText = new CompiledText(Text, WidthOffset - textBGBorderWidth * 2, Terraria.UI.BoldSmallTextStyle);
		int num = Math.Min(compiledText.Height + Terraria.UI.BoldSmallFont.LineSpacing + textBGBorderWidth * 2, MaxHeight);
		int num2 = MaxWidth - (Image.Width + Padding + WidthOffset) >> 1;
		int y = MaxHeight - num >> 1;
		Rectangle DialogArea = new Rectangle(num2, y, WidthOffset, num);
		Rectangle TextArea = DialogArea;
		TextArea.Inflate(-textBGBorderWidth, -textBGBorderWidth);
		TextBlock block = new TextBlock(ref DialogArea, compiledText, ref TextArea, Assets.TextBG, textBGBorderWidth, BackColor, TextColor, AccentColor);
		num2 += WidthOffset + Padding;
		y = MaxHeight - Image.Height >> 1;
		Image[] previews = new Image[1]
		{
			new Image
			{
				Position = new Vector2(num2, y),
				Texture = Image
			}
		};
		return new HowToPlayLayout(block, previews);
	}

	public static HowToPlayLayout StackedLayout(string Text, int DialogWidth, int DialogHeight, Texture2D Image, int Padding = 20)
	{
		if (Main.ScreenHeightPtr == ScreenHeights.FHD)
		{
			DialogWidth += 15;
		}
		int textBGBorderWidth = Assets.TextBGBorderWidth;
		CompiledText compiledText = new CompiledText(Text, DialogWidth - textBGBorderWidth * 2, Terraria.UI.BoldSmallTextStyle);
		DialogHeight = Math.Min(compiledText.Height + Terraria.UI.BoldSmallFont.LineSpacing + textBGBorderWidth * 2, DialogHeight);
		int x = MaxWidth - compiledText.Width >> 1;
		int num = MaxHeight - (DialogHeight + Padding + Image.Height) >> 1;
		Rectangle DialogArea = new Rectangle(x, num, DialogWidth, DialogHeight);
		Rectangle TextArea = DialogArea;
		TextArea.Inflate(-textBGBorderWidth, -textBGBorderWidth);
		TextBlock block = new TextBlock(ref DialogArea, compiledText, ref TextArea, Assets.TextBG, textBGBorderWidth, BackColor, TextColor, AccentColor);
		x = MaxWidth - Image.Width >> 1;
		num += DialogHeight + Padding;
		Image[] previews = new Image[1]
		{
			new Image
			{
				Position = new Vector2(x, num),
				Texture = Image
			}
		};
		return new HowToPlayLayout(block, previews);
	}

	private HowToPlayLayout(TextBlock Block)
	{
		this.Block = Block;
		HowToPlayAssets = new Image[0];
	}

	private HowToPlayLayout(TextBlock Block, Image[] Previews)
	{
		this.Block = Block;
		HowToPlayAssets = Previews;
	}

	public void Draw(int OffsetX, int OffsetY, int ScrollY)
	{
		Block.Draw(OffsetX, OffsetY, ScrollY);
		Image[] howToPlayAssets = HowToPlayAssets;
		for (int i = 0; i < howToPlayAssets.Length; i++)
		{
			Image image = howToPlayAssets[i];
			Vector2 position = image.Position;
			position.X += OffsetX;
			position.Y += OffsetY;
			Main.SpriteBatch.Draw(image.Texture, position, Color.White);
		}
	}

	public void GenerateCache(GraphicsDevice GraphicsDevice)
	{
		Block.GenerateCache(GraphicsDevice);
	}
}
