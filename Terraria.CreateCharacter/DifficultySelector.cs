using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.CreateCharacter;

public class DifficultySelector : ISelector
{
	private enum Direction
	{
		PREVIOUS = -1,
		NEXT = 1
	}

	private const float TransitionSpeed = 0.1f;

	private const int DifficultyTitleStartIdx = 24;

	private const int DifficultyDescStartIdx = 29;

	private const int DifficultySettings = 2;

	private static int ArrowOffset = 10;

	private static Vector2 TitleOffset = new Vector2(0f, -50f * Main.ScreenMultiplier);

	private static Vector2 DescOffset = new Vector2(0f, 50f * Main.ScreenMultiplier);

	private static Vector2 SlideOffset = new Vector2(120f * Main.ScreenMultiplier, 0f);

	public Difficulty Selected;

	private Difficulty ResetValue;

	private Difficulty PreviousSelected;

	private readonly Texture2D[] DifficultyIcons;

	private int FlashTimer;

	private float TransitionTween;

	private Direction TransitionDirection;

	public DifficultySelector(Texture2D[] Icons, Difficulty ResetDefault)
	{
		DifficultyIcons = Icons;
		ResetValue = ResetDefault;
		Selected = ResetDefault;
		TransitionTween = 0f;
	}

	public void Draw(Vector2 Position, float Alpha)
	{
		SpriteFont boldSmallFont = Terraria.UI.BoldSmallFont;
		float num = 1f;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			num = 1.1f;
			ArrowOffset = 18;
			break;
		case ScreenHeights.FHD:
			num = 1.3f;
			ArrowOffset = 35;
			break;
		}
		if (TransitionTween > 0f)
		{
			float num2 = 1f - Math.Min(TransitionTween, 0.5f) / 0.5f;
			float num3 = (Math.Max(0.5f, TransitionTween) - 0.5f) / 0.5f;
			DrawDescription(PreviousSelected, Position, num, Alpha * num3);
			DrawDescription(Selected, Position, num, Alpha * num2);
			Vector2 position = Position + TitleOffset - SlideOffset * (1f - TransitionTween) * (float)TransitionDirection;
			DrawTitle(PreviousSelected, position, num, TransitionTween);
			Vector2 position2 = Position + TitleOffset + SlideOffset * TransitionTween * (float)TransitionDirection;
			DrawTitle(Selected, position2, num, 1f - TransitionTween);
		}
		else
		{
			DrawTitle(Selected, Position + TitleOffset, num + 0.1f * (float)FlashTimer, Alpha);
			string text = Lang.MenuText[(int)(26 - Selected)];
			Vector2 vector = Terraria.UI.MeasureString(boldSmallFont, text);
			if (Alpha > 0.9f)
			{
				DrawArrows(Position + TitleOffset, new Vector2(vector.X * 0.5f + (float)ArrowOffset, 0f));
			}
			DrawDescription(Selected, Position, num, Alpha);
		}
	}

	private void DrawTitle(Difficulty Difficulty, Vector2 Position, float Scale, float Alpha)
	{
		SpriteFont boldSmallFont = Terraria.UI.BoldSmallFont;
		string text = Lang.MenuText[(int)(26 - Difficulty)];
		Vector2 pivot = Terraria.UI.MeasureString(boldSmallFont, text);
		Color color = ((Difficulty != Difficulty.HARDCORE) ? Color.White : Color.Red);
		pivot.X = (float)Math.Round((double)pivot.X * 0.5);
		pivot.Y = (float)Math.Round((double)pivot.Y * 0.5);
		Terraria.UI.DrawString(boldSmallFont, text, Position, color * Alpha, 0f, pivot, Scale);
	}

	private void DrawArrows(Vector2 Position, Vector2 Spacing)
	{
		Vector2 vector = Position - Spacing - new Vector2(8f, 8f);
		Vector2 vector2 = Position + Spacing - new Vector2(8f, 8f);
		Rectangle rect = new Rectangle((int)vector.X, (int)vector.Y, 16, 16);
		SpriteSheet<_sheetSprites>.DrawCentered(136, ref rect, Main.ScreenMultiplier, SpriteEffects.FlipHorizontally);
		rect.X = (int)vector2.X;
		rect.Y = (int)vector2.Y;
		SpriteSheet<_sheetSprites>.DrawCentered(136, ref rect, Main.ScreenMultiplier);
	}

	private void DrawDescription(Difficulty Difficulty, Vector2 Position, float Scale, float Alpha)
	{
		Texture2D texture2D = DifficultyIcons[(int)Difficulty];
		SpriteFont boldSmallFont = Terraria.UI.BoldSmallFont;
		string text = Lang.MenuText[(int)(31 - Difficulty)];
		Vector2 pivot = Terraria.UI.MeasureString(boldSmallFont, text);
		Vector2 origin = new Vector2(texture2D.Width >> 1, texture2D.Height >> 1);
		Main.SpriteBatch.Draw(texture2D, Position, null, Color.White * Alpha, 0f, origin, 1f * Main.ScreenMultiplier + 0.1f * (float)FlashTimer, SpriteEffects.None, 0f);
		pivot.X = (float)Math.Round((double)pivot.X * 0.5);
		pivot.Y = (float)Math.Round((double)pivot.Y * 0.5);
		Terraria.UI.DrawString(boldSmallFont, text, Position + DescOffset, Color.White * Alpha, 0f, pivot, Scale);
	}

	public void Update()
	{
		if (TransitionTween > 0f)
		{
			TransitionTween -= 0.1f;
		}
		else
		{
			TransitionTween = 0f;
		}
		if (FlashTimer > 0)
		{
			FlashTimer--;
		}
	}

	public bool SelectLeft()
	{
		PreviousSelected = Selected;
		int selected = (int)Selected;
		selected--;
		if (selected < 0)
		{
			selected = 2;
		}
		Selected = (Difficulty)selected;
		TransitionTween = 1f;
		TransitionDirection = Direction.PREVIOUS;
		return true;
	}

	public bool SelectRight()
	{
		PreviousSelected = Selected;
		int selected = (int)Selected;
		selected++;
		if (selected > 2)
		{
			selected = 0;
		}
		Selected = (Difficulty)selected;
		TransitionTween = 1f;
		TransitionDirection = Direction.NEXT;
		return true;
	}

	public bool SelectUp()
	{
		return false;
	}

	public bool SelectDown()
	{
		return false;
	}

	public void SetCursor(Vector2i CursorVector)
	{
		int val = Math.Max(CursorVector.X, 0);
		Selected = (Difficulty)Math.Min(val, 2);
	}

	public void Reset()
	{
		Selected = ResetValue;
	}

	public void Show()
	{
		ResetValue = Selected;
		FlashTimer = 0;
	}

	public void FlashSelection(int Duration)
	{
		FlashTimer = Duration;
	}

	public void CancelSelection()
	{
		Selected = ResetValue;
	}
}
