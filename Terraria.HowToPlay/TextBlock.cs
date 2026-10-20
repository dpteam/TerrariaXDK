using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.HowToPlay;

public class TextBlock
{
	private readonly BoxGraphic Background;

	private readonly CompiledText CompText;

	public bool IsScrollable;

	public short MinOffsetY;

	private Texture2D TextTexture;

	private Color TextColor;

	private Color AccentColor;

	private Color BackColor;

	private Rectangle TextArea;

	private Rectangle BGDet;

	private Vector2i DialogPosition;

	public TextBlock(ref Rectangle DialogArea, CompiledText Text, ref Rectangle TextArea, Texture2D BG, int BorderWidth, Color BackColor, Color TextColor, Color AccentColor)
	{
		Background = BoxGraphic.Create(DialogArea.Width, DialogArea.Height, BG, BorderWidth, BackColor);
		DialogPosition = new Vector2i(DialogArea.X, DialogArea.Y);
		CompText = Text;
		this.TextArea = TextArea;
		IsScrollable = Text.Height > TextArea.Height;
		MinOffsetY = (short)(-Math.Max(0, Text.Height - TextArea.Height));
		this.TextColor = TextColor;
		this.AccentColor = AccentColor;
		TextTexture = null;
		BGDet = DialogArea;
		this.BackColor = BackColor;
	}

	public void Draw(int FrontX = 0, int FrontY = 0, int ScrollY = 0)
	{
		Rectangle textArea = TextArea;
		textArea.X = 0;
		textArea.Y = -ScrollY;
		Vector2 vector = default;
		Rectangle textArea2;
		if (Main.ScreenHeightPtr == ScreenHeights.BASE)
		{
			textArea2 = TextArea;
			textArea2.X += FrontX;
			textArea2.Y += FrontY;
			Vector2i dialogPosition = DialogPosition;
			dialogPosition.X += FrontX;
			dialogPosition.Y += FrontY;
			Background.Draw(dialogPosition, 1f);
			vector.X = dialogPosition.X;
			vector.Y = dialogPosition.Y;
		}
		else
		{
			textArea.Height += 8;
			Vector2 position = new Vector2(DialogPosition.X, DialogPosition.Y);
			position.X += FrontX;
			position.Y += FrontY;
			BackColor.A = 196;
			BackColor.R = BackColor.A;
			BackColor.G = BackColor.A;
			BackColor.B = BackColor.A;
			Rectangle bGDet = BGDet;
			if (!IsScrollable)
			{
				position.Y += 4f * Main.ScreenMultiplier;
				bGDet.Height -= 8;
			}
			if (Terraria.UI.MainUI.CurMenuMode == MenuMode.HARDMODE_UPSELL)
			{
				Main.DrawRect(442, position, bGDet, BackColor);
			}
			else
			{
				Main.DrawRect(451, position, bGDet, BackColor);
			}
			textArea2 = TextArea;
			textArea2.X += FrontX - 6;
			textArea2.Y += FrontY - 6;
			textArea2.Height += 8;
			if (!IsScrollable)
			{
				textArea2.Y += 6;
				textArea2.Width += 8;
			}
			vector.X = position.X;
			vector.Y = position.Y;
		}
		Main.SpriteBatch.Draw(TextTexture, textArea2, textArea, Color.White);
		if (IsScrollable)
		{
			int x = (int)(vector.X + (float)(Background.Width >> 1) - 8f);
			Rectangle rect = new Rectangle(x, (int)(vector.Y + 2f), 16, 16);
			if (ScrollY < 0)
			{
				SpriteSheet<_sheetSprites>.DrawCentered(135, ref rect, SpriteEffects.FlipVertically);
			}
			if (ScrollY > MinOffsetY)
			{
				rect.Y = (int)(vector.Y + (float)Background.Height - 16f - 2f);
				SpriteSheet<_sheetSprites>.DrawCentered(135, ref rect);
			}
		}
	}

	public void GenerateCache(GraphicsDevice GraphicsDevice)
	{
		RenderTarget2D renderTarget2D = new RenderTarget2D(GraphicsDevice, CompText.Width, CompText.Height + Terraria.UI.BoldSmallFont.LineSpacing);
		GraphicsDevice.SetRenderTarget(renderTarget2D);
		GraphicsDevice.Clear(Color.Transparent);
		Main.SpriteBatch.Begin();
		CompText.Draw(Main.SpriteBatch, new Rectangle(0, 0, CompText.Width, CompText.Height), TextColor, AccentColor);
		Main.SpriteBatch.End();
		GraphicsDevice.SetRenderTarget(null);
		TextTexture = renderTarget2D;
	}
}
