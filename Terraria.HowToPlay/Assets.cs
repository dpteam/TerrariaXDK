using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.HowToPlay;

public class Assets
{
	public static int TextBGBorderWidth;

	public static Texture2D TextBG;

	public static Texture2D GameLogo;

	public static Texture2D MovementSection;

	public static Texture2D HotbarSection;

	public static Texture2D BuffSection;

	public static Texture2D InventorySection;

	public static void LoadContent(ContentManager Content)
	{
		TextBG = Content.Load<Texture2D>("UI/HowToPlay/Text_Back");
		switch (Main.ScreenHeightPtr)
		{
		case ScreenHeights.BASE:
			TextBGBorderWidth = 20;
			break;
		case ScreenHeights.HD:
			TextBGBorderWidth = 24;
			break;
		case ScreenHeights.FHD:
			TextBGBorderWidth = 32;
			break;
		}
		MovementSection = Content.Load<Texture2D>("UI/HowToPlay/Howto_Movement");
		HotbarSection = Content.Load<Texture2D>("UI/HowToPlay/Howto_Hotbar2");
		BuffSection = Content.Load<Texture2D>("UI/HowToPlay/Howto_Debuff");
		InventorySection = Content.Load<Texture2D>("UI/HowToPlay/Howto_Inventory");
		GameLogo = Content.Load<Texture2D>("UI/HowToPlay/Howto_Logo");
	}
}
