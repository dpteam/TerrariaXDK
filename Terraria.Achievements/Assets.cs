using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Terraria.Achievements;

internal class Assets
{
	public static Texture2D[] Advancements;

	public static void LoadContent(ContentManager Content)
	{
		Advancements = new Texture2D[39];
		string text = "Achievements/";
		for (byte b = 0; b < Advancements.Length; b++)
		{
			string text2 = b.ToString();
			if (b < 10)
			{
				text2 = "0" + text2;
			}
			Advancements[b] = Content.Load<Texture2D>(text + "Achievement" + text2);
		}
	}
}
