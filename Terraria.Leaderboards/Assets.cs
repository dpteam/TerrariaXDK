using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Leaderboards;

internal class Assets
{
	public static Texture2D[] ColumnIcons;

	public static void LoadContent(ContentManager Content)
	{
		ColumnIcons = new Texture2D[32];
		string text = "UI/Leaderboards/";
		for (int num = 31; num >= 0; num--)
		{
			Column column = (Column)num;
			string text2 = column.ToString().ToLower();
			string assetName = text + text2;
			ColumnIcons[num] = Content.Load<Texture2D>(assetName);
		}
	}
}
