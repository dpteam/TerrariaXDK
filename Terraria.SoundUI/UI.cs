using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.SoundUI;

internal class UI
{
	public struct Icon(Texture2D Texture, Vector2 Position, Rectangle Source)
	{
		public Texture2D Texture = Texture;

		public Vector2 Position = Position;

		public Rectangle Source = Source;
	}

	private const int AddYSpacing = 10;

	private const float SliderStepSize = 0.05f;

	private readonly float StepSize;

	private readonly Terraria.UI ParentUI;

	private readonly ActivatableSlider SoundSlider;

	private readonly ActivatableSlider MusicSlider;

	private int UIDelay;

	private Icon SoundIcon;

	private Icon MusicIcon;

	private ActivatableSlider Selected;

	public static UI Create(Terraria.UI ParentUI)
	{
		int num = Assets.SliderEmptyRect.Height + 10;
		float x = 300f;
		float y = 300f;
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.HD:
			x = 475f;
			y = 350f;
			break;
		case ScreenHeights.FHD:
			x = 800f;
			y = 500f;
			break;
		}
		Vector2 vector = new Vector2(x, y);
		Vector2 sliderPosition = vector + new Vector2(50f, 0f);
		Slider active = new Slider(Assets.SliderTex, Assets.SliderEmptyRect, Assets.SliderFullRect, sliderPosition);
		Slider inactive = new Slider(Assets.SliderTex, Assets.SliderEmptyInactiveRect, Assets.SliderFullInactiveRect, sliderPosition);
		ActivatableSlider soundSlider = new ActivatableSlider(active, inactive);
		sliderPosition.Y += num;
		Slider active2 = new Slider(Assets.SliderTex, Assets.SliderEmptyRect, Assets.SliderFullRect, sliderPosition);
		Slider inactive2 = new Slider(Assets.SliderTex, Assets.SliderEmptyInactiveRect, Assets.SliderFullInactiveRect, sliderPosition);
		ActivatableSlider musicSlider = new ActivatableSlider(active2, inactive2);
		Vector2 position = vector + new Vector2(0f, Assets.SliderEmptyRect.Height - Assets.SoundIconRect.Height);
		Icon soundIcon = new Icon(Assets.SoundIconsTex, position, Assets.SoundIconRect);
		position.Y += num;
		Icon musicIcon = new Icon(Assets.SoundIconsTex, position, Assets.MusicIconRect);
		return new UI(ParentUI, soundIcon, musicIcon, soundSlider, musicSlider, 0.05f);
	}

	public UI(Terraria.UI ParentUI, Icon SoundIcon, Icon MusicIcon, ActivatableSlider SoundSlider, ActivatableSlider MusicSlider, float StepSize)
	{
		this.ParentUI = ParentUI;
		this.SoundIcon = SoundIcon;
		this.MusicIcon = MusicIcon;
		this.SoundSlider = SoundSlider;
		this.MusicSlider = MusicSlider;
		SelectSoundSlider();
		this.StepSize = StepSize;
	}

	public void UpdateVolumes()
	{
		SoundSlider.Progress = ParentUI.SoundVolume;
		MusicSlider.Progress = ParentUI.MusicVolume;
	}

	private void SelectMusicSlider()
	{
		SoundSlider.Active = false;
		MusicSlider.Active = true;
		Selected = MusicSlider;
	}

	private void SelectSoundSlider()
	{
		SoundSlider.Active = true;
		MusicSlider.Active = false;
		Selected = SoundSlider;
	}

	public void Update()
	{
		// Mouse first: a click on a bar selects that row and jumps to the position clicked, and
		// holding the button drags it. Before this the volume screen was the only settings screen
		// that ignored the pointer, so with keyboard+mouse the only way to change the volume was
		// the arrow keys.
		if (HandleMouse())
		{
			ParentUI.SettingsDirty = true;
			return;
		}
		if (UIDelay > 0)
		{
			UIDelay--;
			return;
		}
		bool flag = false;
		if (ParentUI.IsRightButtonDown())
		{
			Selected.Progress += StepSize;
			flag = true;
		}
		if (ParentUI.IsLeftButtonDown())
		{
			Selected.Progress -= StepSize;
			flag = true;
		}
		if (ParentUI.IsUpButtonDown() || ParentUI.IsDownButtonDown())
		{
			if (Selected == SoundSlider)
			{
				SelectMusicSlider();
			}
			else
			{
				SelectSoundSlider();
			}
			flag = true;
		}
		if (flag)
		{
			UIDelay = 12;
			Main.SoundVolume = (ParentUI.SoundVolume = SoundSlider.Progress);
			Main.MusicVolume = (ParentUI.MusicVolume = MusicSlider.Progress);
			ParentUI.SettingsDirty = true;
			Main.PlaySound(12);
		}
	}

	/// <summary>
	/// Mouse control: a click selects the bar under the pointer and sets it to the position
	/// clicked; holding the button drags it. Returns true when it did something, so the caller
	/// knows the value changed and can mark the settings dirty.
	///
	/// The bar is thin, so the hit area is padded vertically - aiming a 58px bar exactly is
	/// fussy, and nothing else on this screen is clickable.
	/// </summary>
	private bool HandleMouse()
	{
		if (!Terraria.Input.KeyboardMouseInput.Enabled)
		{
			return false;
		}
		bool flag = Terraria.Input.KeyboardMouseInput.LeftPressed;
		if (!flag)
		{
			return false;
		}
		int num = Terraria.Input.KeyboardMouseInput.MouseX;
		int num2 = Terraria.Input.KeyboardMouseInput.MouseY;
		if (Hit(SoundSlider, num, num2))
		{
			if (Selected != SoundSlider)
			{
				SelectSoundSlider();
			}
			SoundSlider.SetFromPointer(num);
			ApplyVolumes();
			return true;
		}
		if (Hit(MusicSlider, num, num2))
		{
			if (Selected != MusicSlider)
			{
				SelectMusicSlider();
			}
			MusicSlider.SetFromPointer(num);
			ApplyVolumes();
			return true;
		}
		return false;
	}

	/// <summary>True when the pointer is on a bar, with a little vertical slack.</summary>
	private static bool Hit(ActivatableSlider slider, int x, int y)
	{
		Rectangle bounds = slider.Bounds;
		int num = Math.Max(8, bounds.Height / 2);
		return bounds.X <= x && x < bounds.Right
			&& (bounds.Y - num) <= y && y < (bounds.Bottom + num);
	}

	/// <summary>Pushes both slider values into the game and the saved settings.</summary>
	private void ApplyVolumes()
	{
		UIDelay = 6;
		Main.SoundVolume = (ParentUI.SoundVolume = SoundSlider.Progress);
		Main.MusicVolume = (ParentUI.MusicVolume = MusicSlider.Progress);
		ParentUI.SettingsDirty = true;
	}

	public void Draw(SpriteBatch screen)
	{
		screen.Draw(SoundIcon.Texture, SoundIcon.Position, SoundIcon.Source, Color.White);
		screen.Draw(MusicIcon.Texture, MusicIcon.Position, MusicIcon.Source, Color.White);
		SoundSlider.Draw(screen);
		MusicSlider.Draw(screen);
	}

	public void ControlDescription(StringBuilder StrBuilder)
	{
		if (Selected == MusicSlider)
		{
			StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CHANGE_MUSIC_VOLUME));
			StrBuilder.Append(' ');
		}
		else if (Selected == SoundSlider)
		{
			StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CHANGE_SOUND_VOLUME));
			StrBuilder.Append(' ');
		}
		StrBuilder.Append(Lang.Controls(Lang.CONTROLS.BACK));
	}
}
