using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.CreateCharacter;

public class HorizontalListSelector : ISelector
{
	private const int Spacing = 4;

	private readonly Texture2D[] Options;

	private readonly Texture2D Background;

	private Vector2 BackgroundOrigin;

	private readonly Texture2D Picker;

	private Vector2 PickerOrigin;

	private int FlashTimer;

	// Layout captured during Draw so mouse hit-testing matches what is on screen exactly.
	private Vector2 LayoutStart;

	private float LayoutSpacing;

	public int Selected;

	private int RevertValue;

	private readonly int DefResetValue;

	public HorizontalListSelector(Texture2D[] OptionTexArr, Texture2D BGTex, Texture2D PickerTex, int ResetValue)
	{
		Background = BGTex;
		BackgroundOrigin = new Vector2(BGTex.Width >> 1, BGTex.Height >> 1);
		Options = OptionTexArr;
		Picker = PickerTex;
		PickerOrigin = new Vector2(PickerTex.Bounds.Width >> 1, PickerTex.Bounds.Height >> 1);
		Selected = ResetValue;
		RevertValue = ResetValue;
		DefResetValue = ResetValue;
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
		float num = 2f;
		float scale = ((FlashTimer > 0) ? (2f + 0.1f * (float)FlashTimer) : 2f) * Main.ScreenMultiplier;
		float num2 = ((float)Background.Width * 2f + 4f) * Main.ScreenMultiplier;
		num *= Main.ScreenMultiplier;
		Color color = Color.White * Alpha;
		int num3 = (Options.Length - 1) * (int)num2 - 4;
		Position.X -= num3 >> 1;
		LayoutStart = Position;
		LayoutSpacing = num2;
		Vector2 position = Vector2.Zero;
		Vector2 origin = Vector2.Zero;
		Texture2D texture2D = null;
		for (int i = 0; i < Options.Length; i++)
		{
			Texture2D texture2D2 = Options[i];
			Vector2 vector = new Vector2(texture2D2.Width >> 1, texture2D2.Height >> 1);
			if (i == Selected)
			{
				texture2D = texture2D2;
				origin = vector;
				position = Position;
			}
			else
			{
				Main.SpriteBatch.Draw(Background, Position, null, color, 0f, BackgroundOrigin, num, SpriteEffects.None, 0f);
				Main.SpriteBatch.Draw(texture2D2, Position, null, color, 0f, vector, num, SpriteEffects.None, 0f);
			}
			Position.X += num2;
		}
		if (texture2D != null)
		{
			Main.SpriteBatch.Draw(Picker, position, null, color, 0f, PickerOrigin, scale, SpriteEffects.None, 0f);
			Main.SpriteBatch.Draw(texture2D, position, null, color, 0f, origin, scale, SpriteEffects.None, 0f);
		}
	}

	public bool SelectLeft()
	{
		if (Selected > 0)
		{
			Selected--;
		}
		else
		{
			Selected = Options.Length - 1;
		}
		return true;
	}

	public bool SelectRight()
	{
		if (Selected < Options.Length - 1)
		{
			Selected++;
		}
		else
		{
			Selected = 0;
		}
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

	public void Reset()
	{
		Selected = DefResetValue;
	}

	public void Show()
	{
		RevertValue = Selected;
		FlashTimer = 0;
	}

	public void FlashSelection(int Duration)
	{
		FlashTimer = Duration;
	}

	public void CancelSelection()
	{
		Selected = RevertValue;
	}

	public void SetCursor(Vector2i Cursor)
	{
		int num = Cursor.X;
		if (num < 0)
		{
			num = 0;
		}
		else if (num > Options.Length - 1)
		{
			num = Options.Length - 1;
		}
		Selected = num;
	}

	public bool HitTest(Vector2 MousePosition)
	{
		if (LayoutSpacing <= 0f || Options.Length == 0)
		{
			return false;
		}
		float num = (MousePosition.X - LayoutStart.X) / LayoutSpacing;
		int num2 = (int)System.Math.Round(num);
		if (num2 < 0 || num2 >= Options.Length)
		{
			return false;
		}
		if (System.Math.Abs(num - (float)num2) > 0.5f)
		{
			return false;
		}
		if (Selected == num2)
		{
			return false;
		}
		Selected = num2;
		return true;
	}
}
