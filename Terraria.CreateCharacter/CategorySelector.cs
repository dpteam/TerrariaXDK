using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.CreateCharacter;

internal class CategorySelector
{
	private enum ScrollDirection
	{
		PREVIOUS = -1,
		NEXT = 1
	}

	private const float ScrollSpeed = 0.075f;

	private const float UnselectedSizeOffset = 0.75f;

	private readonly Texture2D[] Options;

	private readonly Texture2D Background;

	private readonly Texture2D SelectionBackground;

	private Vector2 Spacing;

	public float ScrollTween;

	public bool Scrolling;

	private ScrollDirection ScrollDestination;

	private int PreviouslySelected;

	private int Selected;

	// Layout captured during Draw: the strip is a centred row of icons around Position.
	private Vector2 LayoutCenter;

	private float LayoutSpacing;

	public int SelectedIndex
	{
		get
		{
			return Selected;
		}
		set
		{
			Selected = Math.Min(Options.Length - 1, Math.Max(0, value));
		}
	}

	public CategorySelector(Texture2D[] Icons, Texture2D Container, Texture2D SelectionContainer, Vector2 SpaceOffset)
	{
		Options = Icons;
		Background = Container;
		SelectionBackground = SelectionContainer;
		Spacing = SpaceOffset;
		Selected = 0;
		Scrolling = false;
		ScrollTween = 0f;
		ScrollDestination = ScrollDirection.PREVIOUS;
		PreviouslySelected = 0;
	}

	public void Update()
	{
		if (Scrolling)
		{
			ScrollTween -= 0.075f;
			if (ScrollTween < 0f)
			{
				Scrolling = false;
				ScrollTween = 0f;
			}
		}
	}

	/// <summary>
	/// Picks the category icon under the cursor. Icons are laid out as a centred row of
	/// <c>Options.Length</c> slots spaced <see cref="Spacing"/> apart, so a nearest-slot lookup
	/// is enough. Returns true only when the selection actually changed.
	/// </summary>
	public bool HitTest(Vector2 MousePosition)
	{
		if (Scrolling || LayoutSpacing <= 0f || Options.Length == 0)
		{
			return false;
		}
		float num = (MousePosition.X - LayoutCenter.X) / LayoutSpacing;
		float num2 = MousePosition.Y - LayoutCenter.Y;
		int num3 = (int)Math.Round(num);
		if (num3 < -4 || num3 > 4)
		{
			return false;
		}
		// Vertical band of the icon row: generous, but rejects clicks far above or below.
		if (Math.Abs(num2) > LayoutSpacing)
		{
			return false;
		}
		int num4 = Selected + num3;
		if (num4 < 0)
		{
			num4 += Options.Length;
		}
		else if (num4 >= Options.Length)
		{
			num4 -= Options.Length;
		}
		if (num4 == Selected)
		{
			return false;
		}
		Selected = num4;
		if (Math.Abs(num3) == 1)
		{
			// Adjacent slot: animate the slide so it reads the same as gamepad navigation.
			StartScrolling((num3 < 0) ? ScrollDirection.PREVIOUS : ScrollDirection.NEXT);
		}
		else
		{
			// Jumping several slots: the slide animation only covers one step, so skip it.
			Scrolling = false;
			ScrollTween = 0f;
		}
		return true;
	}

	public void Draw(Vector2 Position)
	{
		LayoutCenter = Position;
		LayoutSpacing = Spacing.X;
		Vector2 origin = new Vector2(Background.Width >> 1, Background.Height >> 1);
		Vector2 origin2 = new Vector2(SelectionBackground.Width >> 1, SelectionBackground.Height >> 1);
		int num = 4;
		int num2 = num + 1;
		for (int num3 = num * 2 + 1; num3 > 0; num3--)
		{
			int num4 = Selected - num2 + num3;
			float num5 = (float)num - Math.Abs((float)(num3 - num2) + ScrollTween * (float)ScrollDestination);
			float val = 1f - 0.25f * Math.Abs((float)(num3 - num2) + ScrollTween * (float)ScrollDestination);
			val = Math.Max(0.75f, val);
			if (num4 < 0)
			{
				num4 += Options.Length;
			}
			else if (num4 >= Options.Length)
			{
				num4 -= Options.Length;
			}
			if (!(num5 < 0f) && !(val < 0f))
			{
				Texture2D texture2D = Options[num4];
				Vector2 origin3 = new Vector2(texture2D.Width >> 1, texture2D.Height >> 1);
				Vector2 vector = Spacing * ScrollTween * (float)ScrollDestination;
				Vector2 position = Position + Spacing * (num3 - num2) + vector;
				if (num4 == Selected)
				{
					Main.SpriteBatch.Draw(SelectionBackground, position, null, Color.White * num5, 0f, origin2, val * Main.ScreenMultiplier, SpriteEffects.None, 0f);
				}
				else
				{
					Main.SpriteBatch.Draw(Background, position, null, Color.White * num5, 0f, origin, 1f * Main.ScreenMultiplier, SpriteEffects.None, 0f);
				}
				Main.SpriteBatch.Draw(texture2D, position, null, Color.White * num5, 0f, origin3, 1f * Main.ScreenMultiplier, SpriteEffects.None, 0f);
			}
		}
		if (!Scrolling)
		{
			Vector2 vector2 = Spacing * ((float)num - 0.25f);
			Vector2 vector3 = Position - vector2 - new Vector2(8f, 8f);
			Rectangle rect = new Rectangle((int)vector3.X, (int)vector3.Y, 16, 16);
			SpriteSheet<_sheetSprites>.DrawCentered(136, ref rect, Main.ScreenMultiplier, SpriteEffects.FlipHorizontally);
			Vector2 vector4 = Position + vector2 - new Vector2(8f, 8f);
			rect.X = (int)vector4.X;
			rect.Y = (int)vector4.Y;
			SpriteSheet<_sheetSprites>.DrawCentered(136, ref rect, Main.ScreenMultiplier);
		}
	}

	private void StartScrolling(ScrollDirection Direction)
	{
		Scrolling = true;
		ScrollTween = 1f;
		ScrollDestination = Direction;
	}

	public bool SelectNext()
	{
		bool result = false;
		if (!Scrolling)
		{
			PreviouslySelected = Selected;
			Selected++;
			if (Selected >= Options.Length)
			{
				Selected -= Options.Length;
			}
			result = true;
			StartScrolling(ScrollDirection.NEXT);
		}
		return result;
	}

	public bool SelectPrevious()
	{
		bool result = false;
		if (!Scrolling)
		{
			PreviouslySelected = Selected;
			Selected--;
			if (Selected < 0)
			{
				Selected += Options.Length;
			}
			result = true;
			StartScrolling(ScrollDirection.PREVIOUS);
		}
		return result;
	}
}
