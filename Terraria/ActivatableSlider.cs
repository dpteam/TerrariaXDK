using Microsoft.Xna.Framework.Graphics;

namespace Terraria;

public class ActivatableSlider
{
	private Slider Selected;

	private readonly Slider ActiveSlider;

	private readonly Slider InactiveSlider;

	public float Progress
	{
		get
		{
			return ActiveSlider.SliderProgress;
		}
		set
		{
			ActiveSlider.SliderProgress = value;
			InactiveSlider.SliderProgress = value;
		}
	}

	public bool Active
	{
		get
		{
			return Selected == ActiveSlider;
		}
		set
		{
			if (value)
			{
				Selected = ActiveSlider;
			}
			else
			{
				Selected = InactiveSlider;
			}
		}
	}

	public ActivatableSlider(Slider Active, Slider Inactive)
	{
		ActiveSlider = Active;
		InactiveSlider = Inactive;
		Selected = Active;
	}

	public void Draw(SpriteBatch Screen)
	{
		Selected.Draw(Screen);
	}

	/// <summary>The on-screen bar of the slider that is currently shown, for mouse hit-testing.</summary>
	public Microsoft.Xna.Framework.Rectangle Bounds => Selected.Bounds;

	/// <summary>Moves this slider's value to where the player clicked.</summary>
	public void SetFromPointer(int x)
	{
		Selected.SetFromPointer(x);
		// Progress writes both sliders, so the inactive one (shown after switching rows) does not
		// jump back to an old value.
		Progress = Selected.SliderProgress;
	}
}
