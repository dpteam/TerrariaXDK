using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria;

public class Slider
{
	private readonly Texture2D Texture;

	private Rectangle Empty;

	private Rectangle Filled;

	/// <summary>
	/// The bar geometry before <see cref="SliderProgress"/> splits it into filled and empty parts.
	/// Kept separately because the split mutates both rectangles, so <see cref="Bounds"/> cannot
	/// be derived from them once progress has moved.
	/// </summary>
	private readonly Rectangle OriginalEmpty;

	private readonly Rectangle OriginalFilled;

	private Rectangle LeftComponent;

	private Rectangle RightComponent;

	private Vector2 Position;

	private Vector2 RightComponentOffset;

	private float Progress;

	/// <summary>
	/// The bar's on-screen rectangle, for mouse hit-testing. The pointer is reported in the same
	/// space the bar is drawn in, so this is all a click needs.
	/// </summary>
	public Rectangle Bounds
	{
		get
		{
			// The two source rectangles are both 244 wide: the bar is drawn as a filled strip and
			// an empty strip offset by the filled width, so the total on-screen width is 244, not
			// the sum of the two.
			return new Rectangle((int)Position.X, (int)Position.Y, OriginalEmpty.Width, OriginalEmpty.Height);
		}
	}

	/// <summary>
	/// Sets progress from a horizontal position inside the bar. The volume screen uses this so a
	/// click lands where the player clicked, instead of stepping 5% from wherever it already was.
	/// </summary>
	public void SetFromPointer(int x)
	{
		Rectangle bounds = Bounds;
		if (bounds.Width <= 0)
		{
			return;
		}
		SliderProgress = (float)(x - bounds.X) / (float)bounds.Width;
	}

	public float SliderProgress
	{
		get
		{
			return Progress;
		}
		set
		{
			Progress = value;
			if (Progress < 0f)
			{
				Progress = 0f;
			}
			else if (Progress > 1f)
			{
				Progress = 1f;
			}
			LeftComponent = Filled;
			LeftComponent.Width = (int)((float)LeftComponent.Width * Progress);
			RightComponent = Empty;
			RightComponent.Width = (int)((float)RightComponent.Width * (1f - Progress));
			RightComponent.X = LeftComponent.Width;
			RightComponentOffset = new Vector2(LeftComponent.Width, 0f);
		}
	}

	public Slider(Texture2D SliderTex, Rectangle EmptyRect, Rectangle FilledRect, Vector2 SliderPosition)
	{
		Texture = SliderTex;
		Empty = EmptyRect;
		Filled = FilledRect;
		OriginalEmpty = EmptyRect;
		OriginalFilled = FilledRect;
		Position = SliderPosition;
		SliderProgress = 0f;
	}

	public void Draw(SpriteBatch Screen)
	{
		Screen.Draw(Texture, Position, LeftComponent, Color.White);
		Screen.Draw(Texture, Position + RightComponentOffset, RightComponent, Color.White);
	}
}
