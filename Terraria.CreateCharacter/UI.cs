using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Terraria.CreateCharacter;

public class UI
{
	private const int FlashDuration = 10;

	private readonly Terraria.UI ParentUI;

	private readonly CategorySelector Widget;

	private readonly IAttributeWidget[] Attributes;

	private int UIDelay;

	private readonly FastRandom RandGen;

	private Player SelPlayer;

	private IAttributeWidget PrevAttribute;

	private string Header;

	private Vector2 HeaderOrigin;

	private string Footer;

	private Vector2 FooterOrigin;

	private readonly SpriteFont Font;

	private readonly float FontScale;

	private IAttributeWidget SelectedAttribute => Attributes[Widget.SelectedIndex];

	private UI(Terraria.UI Base, CategorySelector CatWidget, IAttributeWidget[] CatAttributes)
	{
		ParentUI = Base;
		Widget = CatWidget;
		Attributes = CatAttributes;
		UIDelay = 0;
		RandGen = new FastRandom();
		Font = Terraria.UI.BigFont;
		FontScale = 0.525f;
	}

	public static UI Create(Terraria.UI Base)
	{
		CategorySelector catWidget = new CategorySelector(Assets.CategoryIcons, Assets.CategoryContainer, Assets.SelectedCategoryContainer, new Vector2(54f * Main.ScreenMultiplier, 0f));
		IAttributeWidget[] array = new IAttributeWidget[10];
		array[1] = HairAttributeWidget.Create(PlayerModifier.Hair, new Vector2i(4, 1), Lang.Controls(Lang.CONTROLS.HAIR_TYPE), Lang.Controls(Lang.CONTROLS.SELECT_TYPE));
		array[2] = ColorAttributeWidget.Create(PlayerModifier.HairColor, new Vector2i(4, 6), Lang.Controls(Lang.CONTROLS.HAIR_COLOR), Lang.Controls(Lang.CONTROLS.SELECT_COLOR));
		array[5] = ColorAttributeWidget.Create(PlayerModifier.Shirt, new Vector2i(4, 2), Lang.Controls(Lang.CONTROLS.SHIRT_COLOR), Lang.Controls(Lang.CONTROLS.SELECT_COLOR));
		array[6] = ColorAttributeWidget.Create(PlayerModifier.Undershirt, new Vector2i(2, 6), Lang.Controls(Lang.CONTROLS.UNDERSHIRT_COLOR), Lang.Controls(Lang.CONTROLS.SELECT_COLOR));
		array[7] = ColorAttributeWidget.Create(PlayerModifier.Pants, new Vector2i(9, 7), Lang.Controls(Lang.CONTROLS.PANTS_COLOR), Lang.Controls(Lang.CONTROLS.SELECT_COLOR));
		array[8] = ColorAttributeWidget.Create(PlayerModifier.Shoes, new Vector2i(0, 0), Lang.Controls(Lang.CONTROLS.SHOE_COLOR), Lang.Controls(Lang.CONTROLS.SELECT_COLOR));
		array[3] = ColorAttributeWidget.Create(PlayerModifier.Eyes, new Vector2i(9, 2), Lang.Controls(Lang.CONTROLS.EYE_COLOR), Lang.Controls(Lang.CONTROLS.SELECT_COLOR));
		array[4] = ColorAttributeWidget.Create(PlayerModifier.Skin, new Vector2i(3, 3), Lang.Controls(Lang.CONTROLS.SKIN_COLOR), Lang.Controls(Lang.CONTROLS.SELECT_COLOR));
		array[0] = GenderAttributeWidget.Create(PlayerModifier.Gender, GenderAttributeWidget.Gender.MALE, Lang.Controls(Lang.CONTROLS.GENDER), Lang.Controls(Lang.CONTROLS.SELECT_GENDER));
		array[9] = DifficultyAttributeWidget.Create(PlayerModifier.Difficulty, Difficulty.SOFTCORE, Lang.Controls(Lang.CONTROLS.DIFFICULTY), Lang.Controls(Lang.CONTROLS.SELECT_DIFFICULTY));
		return new UI(Base, catWidget, array);
	}

	public void Update(Player ActivePlayer)
	{
		SelPlayer = null;
		if (UIDelay > 0)
		{
			UIDelay--;
		}
		bool flag = UpdateCategoryInput(ActivePlayer);
		bool flag2 = UpdateWidgetInput(ActivePlayer);
		if (flag | flag2)
		{
			if ((Widget.SelectedIndex == 0) & flag2)
			{
				Main.PlaySound(ActivePlayer.male ? 1 : 20);
			}
			else
			{
				Main.PlaySound(12);
			}
			UIDelay = 12;
		}
		Widget.Update();
		if (PrevAttribute != null)
		{
			PrevAttribute.Update();
		}
		SelectedAttribute.Update();
		if (ParentUI.IsButtonTriggered(Buttons.Start))
		{
			ParentUI.ClearInput();
			ParentUI.SetMenu(MenuMode.NAME_CHARACTER);
		}
		if (ParentUI.IsBackButtonTriggered())
		{
			ParentUI.SetMenu(MenuMode.CONFIRM_LEAVE_CREATE_CHARACTER);
		}
		SelPlayer = ActivePlayer;
	}

	private bool IsButtonDown(Buttons Button)
	{
		if (UIDelay == 0)
		{
			return ParentUI.IsButtonDown(Button);
		}
		return false;
	}

	private bool UpdateCategoryInput(Player ActivePlayer)
	{
		bool result = false;
		IAttributeWidget selectedAttribute = SelectedAttribute;
		if ((IsButtonDown(Buttons.RightShoulder) || ParentUI.IsButtonTriggered(Buttons.A)) && Widget.SelectNext())
		{
			result = true;
			PrevAttribute = selectedAttribute;
			SelectedAttribute.Show();
			UpdateHeaderAndFooter();
			if (ParentUI.IsButtonTriggered(Buttons.A))
			{
				PrevAttribute.FlashSelection(10);
			}
		}
		if (IsButtonDown(Buttons.LeftShoulder) && Widget.SelectPrevious())
		{
			result = true;
			PrevAttribute = selectedAttribute;
			SelectedAttribute.Show();
			UpdateHeaderAndFooter();
		}
		if (ParentUI.IsButtonTriggered(Buttons.Y))
		{
			Randomize(ActivePlayer);
			result = true;
		}
		return result;
	}

	private bool UpdateWidgetInput(Player ActivePlayer)
	{
		bool flag = false;
		IAttributeWidget selectedAttribute = SelectedAttribute;
		if (UIDelay == 0)
		{
			if (ParentUI.IsDownButtonDown() && selectedAttribute.SelectDown())
			{
				flag = true;
			}
			if (ParentUI.IsUpButtonDown() && selectedAttribute.SelectUp())
			{
				flag = true;
			}
			if (ParentUI.IsLeftButtonDown() && selectedAttribute.SelectLeft())
			{
				flag = true;
			}
			if (ParentUI.IsRightButtonDown() && selectedAttribute.SelectRight())
			{
				flag = true;
			}
		}
		if (flag)
		{
			selectedAttribute.Apply(ActivePlayer);
		}
		return flag;
	}

	private void UpdateHeaderAndFooter()
	{
		Header = SelectedAttribute.WidgetDescription;
		HeaderOrigin = Font.MeasureString(Header) * 0.5f;
		Footer = $"{Widget.SelectedIndex + 1} / {Attributes.Length}";
		FooterOrigin = Font.MeasureString(Footer) * 0.5f;
	}

	public void Draw(WorldView CurView)
	{
		int num = (int)((float)(Assets.Frame.Width >> 1) * Main.ScreenMultiplier);
		Vector2 vector = new Vector2(Main.ResolutionWidth / 2 - num, CurView.SafeAreaOffsetTop + 32);
		Vector2 zero = Vector2.Zero;
		Main.SpriteBatch.Draw(Assets.Frame, vector, null, Color.White, 0f, default, Main.ScreenMultiplier, SpriteEffects.None, 0f);
		zero.X = 228f * Main.ScreenMultiplier;
		zero.Y = 32f * Main.ScreenMultiplier;
		Widget.Draw(vector + zero);
		zero.Y = 204f * Main.ScreenMultiplier;
		if (Widget.Scrolling)
		{
			float scrollTween = Widget.ScrollTween;
			float num2 = Math.Min(scrollTween, 0.5f) / 0.5f;
			float alpha = (Math.Max(0.5f, scrollTween) - 0.5f) / 0.5f;
			PrevAttribute.Draw(vector + zero, alpha);
			SelectedAttribute.Draw(vector + zero, 1f - num2);
		}
		else
		{
			SelectedAttribute.Draw(vector + zero, 1f);
		}
		zero.Y = 78f * Main.ScreenMultiplier;
		Main.SpriteBatch.DrawString(Font, Header, vector + zero, Color.White, 0f, HeaderOrigin, FontScale, SpriteEffects.None, 0f);
		zero.Y = 340f * Main.ScreenMultiplier;
		Main.SpriteBatch.DrawString(Font, Footer, vector + zero, Color.White, 0f, FooterOrigin, FontScale, SpriteEffects.None, 0f);
		zero.Y = 385f * Main.ScreenMultiplier;
		zero.X = num;
		string text = Lang.Controls(Lang.CONTROLS.CREATE_CHARACTER);
		Vector2 vector2 = Font.MeasureString(text);
		Main.SpriteBatch.DrawString(Font, text, vector + zero, Color.White, 0f, vector2 * 0.5f, FontScale, SpriteEffects.None, 0f);
		if (SelPlayer != null)
		{
			SelPlayer.velocity.X = 1f;
			SelPlayer.PlayerFrame();
			zero.X = 520f * Main.ScreenMultiplier;
			zero.Y = 110f * Main.ScreenMultiplier;
			ParentUI.DrawPlayer(SelPlayer, vector + zero, 4f * Main.ScreenMultiplier);
		}
	}

	public void Randomize(Player ActivePlayer)
	{
		Vector2i[] array = RandomCharacter.RandCreate(RandGen);
		for (int i = 0; i < Attributes.Length; i++)
		{
			if (i != 9)
			{
				IAttributeWidget attributeWidget = Attributes[i];
				attributeWidget.SetCursor(array[i]);
				attributeWidget.Apply(ActivePlayer);
			}
		}
	}

	public void ApplyDefaultAttributes(Player ActivePlayer)
	{
		Randomize(ActivePlayer);
		IAttributeWidget attributeWidget = Attributes[9];
		attributeWidget.Reset();
		attributeWidget.Apply(ActivePlayer);
		Widget.SelectedIndex = 0;
		UpdateHeaderAndFooter();
	}

	public void ControlDescription(StringBuilder StrBuilder)
	{
		StrBuilder.Append(Lang.Controls(Lang.CONTROLS.CHANGE_CATEGORY));
		StrBuilder.Append(' ');
		StrBuilder.Append(Lang.Controls(Lang.CONTROLS.BACK));
		StrBuilder.Append(' ');
		StrBuilder.Append(Lang.Controls(Lang.CONTROLS.RANDOMIZE));
	}
}
