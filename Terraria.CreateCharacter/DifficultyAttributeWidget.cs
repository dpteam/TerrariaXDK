using System;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.CreateCharacter;

public class DifficultyAttributeWidget : AttributeWidget<DifficultySelector>, IAttributeWidget
{
	private readonly Action<Player, byte> Modifier;

	public static DifficultyAttributeWidget Create(Action<Player, byte> Modifier, Difficulty ResetValue, string WidgetDescription, string ControlDescription)
	{
		return new DifficultyAttributeWidget(new DifficultySelector(new Texture2D[3]
		{
			Assets.SoftcoreIcon,
			Assets.MediumcoreIcon,
			Assets.HardcoreIcon
		}, ResetValue), Modifier, WidgetDescription, ControlDescription);
	}

	private DifficultyAttributeWidget(DifficultySelector Widget, Action<Player, byte> Modifier, string WidgetDescription, string ControlDescription)
	{
		base.Widget = Widget;
		this.Modifier = Modifier;
		base.WidgetDescription = WidgetDescription;
		base.ControlDescription = ControlDescription;
	}

	public void Apply(Player Player)
	{
		Modifier(Player, (byte)Widget.Selected);
	}
}
