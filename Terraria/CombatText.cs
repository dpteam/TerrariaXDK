using Microsoft.Xna.Framework;

namespace Terraria;

public struct CombatText
{
	public byte Active;

	public bool Crit;

	public short LifeTime;

	public float Alpha;

	public float AlphaDir;

	public string Text;

	public Vector2 Position;

	public Vector2 Velocity;

	public Vector2 TextSize;

	public float Scale;

	public float Rotation;

	public byte Type;

	public void Init()
	{
		Active = 0;
	}

	public static void NewText(Vector2 Position, int Width, int Height, int Value, bool Crit = false, byte Type = 0)
	{
		for (int i = 0; i < 32; i++)
		{
			if (Main.CombatTextSet[i].Active == 0)
			{
				Width >>= 1;
				Height >>= 1;
				Main.CombatTextSet[i].Text = Value.ToStringLookup();
				Vector2 textSize = UI.MeasureString(UI.CombatTextFont[Crit ? 1 : 0], Main.CombatTextSet[i].Text);
				Main.CombatTextSet[i].TextSize = textSize;
				Main.CombatTextSet[i].Alpha = 1f;
				Main.CombatTextSet[i].AlphaDir = -0.05f;
				Main.CombatTextSet[i].Active = 1;
				Main.CombatTextSet[i].Scale = 0f;
				Main.CombatTextSet[i].Position.X = Position.X + (float)Width - textSize.X * 0.5f;
				Main.CombatTextSet[i].Position.Y = Position.Y + (float)(Height >> 1) - textSize.Y * 0.5f;
				Main.CombatTextSet[i].Position.X += Main.Rand.Next(-Width, Width + 1);
				Main.CombatTextSet[i].Position.Y += Main.Rand.Next(-Height, Height + 1);
				Main.CombatTextSet[i].Crit = Crit;
				Main.CombatTextSet[i].Type = Type;
				if (Crit)
				{
					Main.CombatTextSet[i].LifeTime = 120;
					Main.CombatTextSet[i].Velocity.Y = -14f;
					Main.CombatTextSet[i].Velocity.X = (float)Main.Rand.Next(-25, 26) * 0.05f;
					Main.CombatTextSet[i].Rotation = ((Main.CombatTextSet[i].Velocity.X < 0f) ? (-0.120000005f) : 0.120000005f);
				}
				else
				{
					Main.CombatTextSet[i].Rotation = 0f;
					Main.CombatTextSet[i].Velocity.Y = -7f;
					Main.CombatTextSet[i].LifeTime = 60;
				}
				break;
			}
		}
	}

	public void Update()
	{
		Alpha += AlphaDir;
		if (Alpha <= 0.6f)
		{
			AlphaDir = 0f - AlphaDir;
		}
		else if (Alpha >= 1f)
		{
			Alpha = 1f;
			AlphaDir = 0f - AlphaDir;
		}
		Velocity.Y *= 0.92f;
		if (Crit)
		{
			Velocity.Y *= 0.92f;
		}
		Velocity.X *= 0.93f;
		Position.X += Velocity.X;
		Position.Y += Velocity.Y;
		if (--LifeTime <= 0)
		{
			Scale -= 0.1f;
			if (Scale < 0.1f)
			{
				Active = 0;
			}
			LifeTime = 0;
			if (Crit)
			{
				AlphaDir = -1f;
				Scale += 0.07f;
			}
			return;
		}
		if (Crit)
		{
			if (Velocity.X < 0f)
			{
				Rotation += 0.001f;
			}
			else
			{
				Rotation -= 0.001f;
			}
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

	public static void UpdateCombatText()
	{
		for (int num = 31; num >= 0; num--)
		{
			if (Main.CombatTextSet[num].Active != 0)
			{
				Main.CombatTextSet[num].Update();
			}
		}
	}
}
