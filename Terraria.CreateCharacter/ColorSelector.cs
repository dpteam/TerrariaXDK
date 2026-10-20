using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.CreateCharacter;

public class ColorSelector : ISelector
{
	private const int ItemWidth = 16;

	private const int ItemHeight = 16;

	private const int SelectedItemWidth = 20;

	private const int SelectedItemHeight = 20;

	private const int ItemSpacing = 6;

	private readonly Texture2D Palette;

	private readonly Texture2D Picker;

	private Rectangle PaletteBounds;

	private Rectangle PickerBounds;

	private Vector2i Selected;

	private Vector2i RevertValue;

	private Vector2i ResetValue;

	private int FlashTimer;

	public Color SelectedColor;

	public ColorSelector(Texture2D Palette, Texture2D Picker, Vector2i ResetValue)
	{
		this.Palette = Palette;
		PaletteBounds = Palette.Bounds;
		this.Picker = Picker;
		PickerBounds = Picker.Bounds;
		Selected = ResetValue;
		this.ResetValue = ResetValue;
		UpdateColor();
	}

	public void Update()
	{
		if (FlashTimer > 0)
		{
			FlashTimer--;
		}
	}

	public void Draw(Vector2 Position, float Alpha)
	{
		Rectangle paletteBounds = PaletteBounds;
		Rectangle value = new Rectangle(0, 0, 1, 1);
		Rectangle destinationRectangle = new Rectangle(0, 0, (int)(16f * Main.ScreenMultiplier), (int)(16f * Main.ScreenMultiplier));
		Vector2 position = default;
		Vector2i vector2i = new Vector2i(8, 8);
		int num = (int)(22f * Main.ScreenMultiplier);
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			vector2i.X = (int)((float)vector2i.X * Main.ScreenMultiplier);
			vector2i.Y = (int)((float)vector2i.Y * Main.ScreenMultiplier);
			break;
		case ScreenHeights.FHD:
			vector2i.X *= 2;
			vector2i.Y *= 2;
			break;
		}
		int num2 = paletteBounds.Width * num - (int)(6f * Main.ScreenMultiplier);
		int num3 = paletteBounds.Height * num - (int)(6f * Main.ScreenMultiplier);
		Position.X -= num2 >> 1;
		Position.Y -= num3 >> 1;
		Position.X += 8f * Main.ScreenMultiplier;
		Position.Y += 8f * Main.ScreenMultiplier;
		Color color = Color.White * Alpha;
		Color color2 = Color.Black * Alpha;
		for (int num4 = paletteBounds.Height - 1; num4 > -1; num4--)
		{
			for (int num5 = paletteBounds.Width - 1; num5 > -1; num5--)
			{
				if (Selected.X != num5 || Selected.Y != num4)
				{
					value.X = num5;
					value.Y = num4;
					destinationRectangle.X = (int)Position.X + num5 * num - vector2i.X;
					destinationRectangle.Y = (int)Position.Y + num4 * num - vector2i.Y;
					Main.SpriteBatch.Draw(Palette, destinationRectangle, value, color);
					position.X = destinationRectangle.X - (PickerBounds.Width - (16 - 2 * (int)Main.ScreenHeightPtr) >> 1);
					position.Y = destinationRectangle.Y - (PickerBounds.Height - (16 - 2 * (int)Main.ScreenHeightPtr) >> 1);
					Main.SpriteBatch.Draw(Picker, position, null, color2, 0f, default, Main.ScreenMultiplier, SpriteEffects.None, 0f);
				}
			}
		}
		Vector2 position2 = new Vector2
		{
			X = Position.X + (float)(Selected.X * num),
			Y = Position.Y + (float)(Selected.Y * num)
		};
		value.X = Selected.X;
		value.Y = Selected.Y;
		float num6 = ((FlashTimer > 0) ? (1f + 0.1f * (float)FlashTimer) : 1f);
		destinationRectangle.X = (int)Position.X + Selected.X * num - (int)((float)vector2i.X * num6);
		destinationRectangle.Y = (int)Position.Y + Selected.Y * num - (int)((float)vector2i.Y * num6);
		destinationRectangle.Width = (int)((float)destinationRectangle.Width * num6);
		destinationRectangle.Height = (int)((float)destinationRectangle.Height * num6);
		Main.SpriteBatch.Draw(Palette, destinationRectangle, value, color);
		Main.SpriteBatch.Draw(Picker, position2, null, color, 0f, new Vector2(PickerBounds.Width >> 1, PickerBounds.Height >> 1), num6 * Main.ScreenMultiplier, SpriteEffects.None, 0f);
	}

	private void UpdateColor()
	{
		Rectangle value = new Rectangle(Selected.X, Selected.Y, 1, 1);
		Color[] array = new Color[1];
		Palette.GetData(0, value, array, 0, 1);
		SelectedColor = array[0];
	}

	public bool SelectLeft()
	{
		if (Selected.X > 0)
		{
			Selected.X--;
		}
		else
		{
			Selected.X = PaletteBounds.Width - 1;
		}
		UpdateColor();
		return true;
	}

	public bool SelectRight()
	{
		if (Selected.X < PaletteBounds.Width - 1)
		{
			Selected.X++;
		}
		else
		{
			Selected.X = 0;
		}
		UpdateColor();
		return true;
	}

	public bool SelectUp()
	{
		if (Selected.Y > 0)
		{
			Selected.Y--;
		}
		else
		{
			Selected.Y = PaletteBounds.Height - 1;
		}
		UpdateColor();
		return true;
	}

	public bool SelectDown()
	{
		if (Selected.Y < PaletteBounds.Height - 1)
		{
			Selected.Y++;
		}
		else
		{
			Selected.Y = 0;
		}
		UpdateColor();
		return true;
	}

	public void SetCursor(Vector2i cursor)
	{
		Selected = cursor;
		UpdateColor();
	}

	public void Reset()
	{
		Selected = ResetValue;
		UpdateColor();
	}

	public void Show()
	{
		RevertValue = Selected;
		FlashTimer = 0;
	}

	public void FlashSelection(int duration)
	{
		FlashTimer = duration;
	}

	public void CancelSelection()
	{
		Selected = RevertValue;
		UpdateColor();
	}
}
