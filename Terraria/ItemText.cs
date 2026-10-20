using Microsoft.Xna.Framework;

namespace Terraria;

public struct ItemText
{
	public const int ActiveTime = 56;

	public byte Active;

	public short LifeTime;

	public short NetID;

	public Vector2 Position;

	public float VelocityY;

	public float Alpha;

	public float AlphaDir;

	public float Scale;

	public Color Color;

	public int Stack;

	public string Text;

	public Vector2 TextSize;

	public void Init()
	{
		Active = 0;
	}

	public void Update(int WhoAmI, ItemTextPool TextPool)
	{
		Alpha += AlphaDir;
		if (Alpha <= 0.7f)
		{
			Alpha = 0.7f;
			AlphaDir = 0f - AlphaDir;
		}
		else if (Alpha >= 1f)
		{
			Alpha = 1f;
			AlphaDir = 0f - AlphaDir;
		}
		bool flag = false;
		Vector2 textSize = TextSize;
		textSize *= Scale;
		textSize.Y *= 0.8f;
		Rectangle rectangle = default;
		Rectangle value = default;
		rectangle.X = (int)(Position.X - textSize.X * 0.5f);
		rectangle.Y = (int)(Position.Y - textSize.Y * 0.5f);
		rectangle.Width = (int)textSize.X;
		rectangle.Height = (int)textSize.Y;
		for (int i = 0; i < 4; i++)
		{
			if (TextPool.ItemText[i].Active == 0 || i == WhoAmI)
			{
				continue;
			}
			Vector2 textSize2 = TextPool.ItemText[i].TextSize;
			textSize2 *= TextPool.ItemText[i].Scale;
			textSize2.Y *= 0.8f;
			value.X = (int)(TextPool.ItemText[i].Position.X - textSize2.X * 0.5f);
			value.Y = (int)(TextPool.ItemText[i].Position.Y - textSize2.Y * 0.5f);
			value.Width = (int)textSize2.X;
			value.Height = (int)textSize2.Y;
			if (rectangle.Intersects(value) && (Position.Y < TextPool.ItemText[i].Position.Y || (Position.Y == TextPool.ItemText[i].Position.Y && WhoAmI < i)))
			{
				flag = true;
				int num = TextPool.ActiveTexts;
				if (num > 3)
				{
					num = 3;
				}
				TextPool.ItemText[i].LifeTime = (LifeTime = (short)(56 + num * 14));
			}
		}
		if (!flag)
		{
			VelocityY *= 0.86f;
			if (Scale == 1f)
			{
				VelocityY *= 0.4f;
			}
		}
		else if (VelocityY > -6f)
		{
			VelocityY -= 0.2f;
		}
		else
		{
			VelocityY *= 0.86f;
		}
		Position.Y += VelocityY;
		if (--LifeTime <= 0)
		{
			Scale -= 0.03f;
			if (Scale < 0.1f)
			{
				Active = 0;
			}
			LifeTime = 0;
			return;
		}
		if (Scale < 1f)
		{
			Scale += 0.1f;
		}
		if (Scale > 1f)
		{
			Scale = 1f;
		}
	}
}
