using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.CreateCharacter;

public class HairSelector : ISelector
{
	private const int Spacing = 2;

	private readonly Texture2D Container;

	private readonly Texture2D Picker;

	private readonly Vector2 PickerOrigin;

	private readonly int Rows;

	private readonly int Columns;

	private readonly Rectangle[] Slots;

	private int FlashTimer;

	// Layout captured during Draw so mouse hit-testing matches what is on screen exactly.
	private Vector2 LayoutCenter;

	private float LayoutCellWidth;

	private float LayoutCellHeight;

	private Vector2i SelectedVect;

	private Vector2i RevertValue;

	private Vector2i ResetValue;

	public int Selected => SelectedVect.X + SelectedVect.Y * Columns;

	public HairSelector(int Columns, Rectangle[] Sources, Texture2D Background, Texture2D Picker, Vector2i Default)
	{
		Container = Background;
		this.Picker = Picker;
		PickerOrigin = new Vector2(Picker.Bounds.Width >> 1, Picker.Bounds.Height >> 1);
		this.Columns = Columns;
		Rows = Sources.Length / Columns;
		Slots = Sources;
		SelectedVect = Default;
		RevertValue = Default;
		ResetValue = Default;
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
		Rectangle bounds = Container.Bounds;
		Color color = Color.White * Alpha;
		Vector2 position = default;
		Vector2 position2 = default;
		int num = (int)((float)(bounds.Width + 2) * Main.ScreenMultiplier);
		int num2 = (int)((float)(bounds.Height + 2) * Main.ScreenMultiplier);
		int num3 = (int)((float)(bounds.Width >> 1) * Main.ScreenMultiplier);
		int num4 = (int)((float)(bounds.Height >> 1) * Main.ScreenMultiplier);
		int num5 = num * Columns - 2;
		int num6 = num2 * Rows - 2;
		Position.X -= num5 >> 1;
		Position.Y -= num6 >> 1;
		Position.X += num3;
		Position.Y += num4;
		LayoutCenter = Position;
		LayoutCellWidth = num;
		LayoutCellHeight = num2;
		for (int num7 = Rows - 1; num7 > -1; num7--)
		{
			int num8 = (int)Position.Y + num7 * num2;
			for (int num9 = Columns - 1; num9 > -1; num9--)
			{
				if (num7 != SelectedVect.Y || num9 != SelectedVect.X)
				{
					int num10 = (int)Position.X + num9 * num;
					int num11 = num7 * Columns + num9;
					position2.X = num10 - num3;
					position2.Y = num8 - num4;
					Main.SpriteBatch.Draw(Container, position2, null, color, 0f, default, Main.ScreenMultiplier, SpriteEffects.None, 0f);
					SpriteSheet<_sheetSprites>.DrawCentered(1269 + num11, num10, num8, Slots[num11], color, Main.ScreenMultiplier);
				}
			}
		}
		float num12 = ((FlashTimer > 0) ? (1f + 0.1f * (float)FlashTimer) : 1f);
		position.X = Position.X + (float)(SelectedVect.X * num);
		position.Y = Position.Y + (float)(SelectedVect.Y * num2);
		Main.SpriteBatch.Draw(Picker, position, null, color, 0f, PickerOrigin, num12 * Main.ScreenMultiplier, SpriteEffects.None, 0f);
		SpriteSheet<_sheetSprites>.DrawCentered(1269 + Selected, (int)position.X, (int)position.Y, Slots[Selected], color, num12 * Main.ScreenMultiplier);
	}

	public bool SelectLeft()
	{
		if (SelectedVect.X > 0)
		{
			SelectedVect.X--;
		}
		else
		{
			SelectedVect.X = Columns - 1;
		}
		return true;
	}

	public bool SelectRight()
	{
		if (SelectedVect.X < Columns - 1)
		{
			SelectedVect.X++;
		}
		else
		{
			SelectedVect.X = 0;
		}
		return true;
	}

	public bool SelectUp()
	{
		if (SelectedVect.Y > 0)
		{
			SelectedVect.Y--;
		}
		else
		{
			SelectedVect.Y = Rows - 1;
		}
		return true;
	}

	public bool SelectDown()
	{
		if (SelectedVect.Y < Rows - 1)
		{
			SelectedVect.Y++;
		}
		else
		{
			SelectedVect.Y = 0;
		}
		return true;
	}

	public void SetCursor(Vector2i CursorVect)
	{
		SelectedVect = CursorVect;
	}

	public bool HitTest(Vector2 MousePosition)
	{
		if (LayoutCellWidth <= 0f || LayoutCellHeight <= 0f)
		{
			return false;
		}
		float num = (MousePosition.X - LayoutCenter.X) / LayoutCellWidth;
		float num2 = (MousePosition.Y - LayoutCenter.Y) / LayoutCellHeight;
		int num3 = (int)System.Math.Round(num);
		int num4 = (int)System.Math.Round(num2);
		if (num3 < 0 || num3 >= Columns || num4 < 0 || num4 >= Rows)
		{
			return false;
		}
		// Stay inside the cell the pointer is over, so gaps between slots are not clickable.
		if (System.Math.Abs(num - (float)num3) > 0.5f || System.Math.Abs(num2 - (float)num4) > 0.5f)
		{
			return false;
		}
		if (SelectedVect.X == num3 && SelectedVect.Y == num4)
		{
			return false;
		}
		SelectedVect = new Vector2i(num3, num4);
		return true;
	}

	public void Reset()
	{
		SelectedVect = ResetValue;
	}

	public void Show()
	{
		RevertValue = SelectedVect;
		FlashTimer = 0;
	}

	public void FlashSelection(int Duration)
	{
		FlashTimer = Duration;
	}

	public void CancelSelection()
	{
		SelectedVect = RevertValue;
	}
}
