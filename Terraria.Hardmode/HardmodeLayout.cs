using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.HowToPlay;

namespace Terraria.Hardmode;

public class HardmodeLayout
{
	private struct Image
	{
		public Vector2 Position;

		public Texture2D Texture;
	}

	private static readonly int MaxWidth = Terraria.UI.UsableWidth;

	private static readonly int MaxHeight = Terraria.UI.USABLE_HEIGHT;

	private readonly Image SideImage;

	private static Color TextColor = Color.White;

	private static Color AccentColor = new Color(255, 212, 64, 255);

	private static Color BackColor = new Color(42, 101, 43, 192);

	public TextBlock Block;

	public static HardmodeLayout SideBySideLayout(string Text, int WidthOffset, Texture2D Image, int Padding = 20)
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
		Image image = new Image
		{
			Position = new Vector2(num2, y),
			Texture = Image
		};
		return new HardmodeLayout(block, image);
	}

	private HardmodeLayout(TextBlock Block, Image Image)
	{
		this.Block = Block;
		SideImage = Image;
	}

	public void Draw(int OffsetX, int OffsetY, int ScrollY)
	{
		Block.Draw(OffsetX, OffsetY, ScrollY);
		Image sideImage = SideImage;
		Vector2 position = sideImage.Position;
		position.X += OffsetX;
		position.Y += OffsetY;
		Main.SpriteBatch.Draw(sideImage.Texture, position, Color.White);
	}

	public void GenerateCache(GraphicsDevice GraphicsDevice)
	{
		Block.GenerateCache(GraphicsDevice);
	}
}
