using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Hardmode;

public class Assets
{
	public static int TextBGBorderWidth;

	public static Texture2D TextBG;

	public static Texture2D HardmodeUpsell;

	public static void LoadContent(ContentManager Content)
	{
		TextBG = Content.Load<Texture2D>("UI/HowToPlay/Text_Back");
		TextBGBorderWidth = ((Main.ScreenHeightPtr == ScreenHeights.FHD) ? 32 : 20);
		HardmodeUpsell = Content.Load<Texture2D>("Images/UpsellHM");
	}
}
