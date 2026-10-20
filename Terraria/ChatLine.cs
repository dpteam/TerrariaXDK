using Microsoft.Xna.Framework;

namespace Terraria;

public struct ChatLine
{
	public Color Color;

	public int ShowTime;

	public string Text;

	public Vector2 Size;

	public void Init()
	{
		Color = Color.White;
		ShowTime = 0;
		Text = null;
	}
}
