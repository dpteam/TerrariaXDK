using System;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.CreateCharacter;

public class GenderAttributeWidget : AttributeWidget<HorizontalListSelector>, IAttributeWidget
{
	public enum Gender
	{
		MALE,
		FEMALE
	}

	private readonly Action<Player, bool> Modifier;

	public static GenderAttributeWidget Create(Action<Player, bool> Modifier, Gender Default, string WidgetDescription, string ControlDescription)
	{
		return new GenderAttributeWidget(new HorizontalListSelector(new Texture2D[2]
		{
			Assets.MaleIcon,
			Assets.FemaleIcon
		}, Assets.HorizontalContainer, Assets.SelectedHorizontalContainer, (Default != Gender.MALE) ? 1 : 0), Modifier, WidgetDescription, ControlDescription);
	}

	private GenderAttributeWidget(HorizontalListSelector Widget, Action<Player, bool> Modifier, string WidgetDescription, string ControlDescription)
	{
		base.Widget = Widget;
		this.Modifier = Modifier;
		base.WidgetDescription = WidgetDescription;
		base.ControlDescription = ControlDescription;
	}

	public void Apply(Player Player)
	{
		Modifier(Player, Widget.Selected == 0);
	}
}
