using Microsoft.Xna.Framework;

namespace Terraria;

public struct Gore
{
	public const int MaxNumGoreTypes = 175;

	public const int MaxNumGore = 128;

	public const int GoreTime = 360;

	private static int LastGoreIndex;

	public Vector2 Position;

	public Vector2 Velocity;

	public float Rotation;

	public float Scale;

	public float Light;

	public byte Active;

	public byte Type;

	public bool Sticky;

	public short Alpha;

	public short TimeLeft;

	public void Init()
	{
		Active = 0;
	}

	public void Update()
	{
		if ((Type >= 11 && Type <= 13) || (Type >= 61 && Type <= 63) || Type == 99)
		{
			Velocity.Y *= 0.98f;
			Velocity.X *= 0.98f;
			Scale -= 0.007f;
			if (Scale < 0.1f)
			{
				Active = 0;
				return;
			}
		}
		else if (Type == 16 || Type == 17)
		{
			Velocity.Y *= 0.98f;
			Velocity.X *= 0.98f;
			Scale -= 0.01f;
			if (Scale < 0.1f)
			{
				Active = 0;
				return;
			}
		}
		else
		{
			Velocity.Y += 0.2f;
		}
		Rotation += Velocity.X * 0.1f;
		if (Sticky)
		{
			int num = SpriteSheet<_sheetSprites>.Source[256 + Type].Width;
			if (SpriteSheet<_sheetSprites>.Source[256 + Type].Height < num)
			{
				num = SpriteSheet<_sheetSprites>.Source[256 + Type].Height;
			}
			num = (int)((float)num * Scale * 0.9f);
			Collision.TileCollision(ref Position, ref Velocity, num, num);
			if (Velocity.Y == 0f)
			{
				Velocity.X *= 0.97f;
				if (Velocity.X > -0.01f && Velocity.X < 0.01f)
				{
					Velocity.X = 0f;
				}
			}
			if (TimeLeft > 0)
			{
				TimeLeft--;
			}
			else
			{
				Alpha++;
			}
		}
		else
		{
			Alpha += 2;
		}
		if (Alpha >= 255)
		{
			Active = 0;
			return;
		}
		Position.X += Velocity.X;
		Position.Y += Velocity.Y;
		if (Light > 0f)
		{
			float num2 = Light * Scale;
			Vector3 rGB = new Vector3(num2, num2, num2);
			if (Type == 16)
			{
				rGB.Y *= 0.8f;
				rGB.Z *= 0.3f;
			}
			else if (Type == 17)
			{
				rGB.X *= 0.3f;
				rGB.Y *= 0.6f;
			}
			Lighting.AddLight((int)(Position.X + (float)SpriteSheet<_sheetSprites>.Source[256 + Type].Width * Scale * 0.5f) >> 4, (int)(Position.Y + (float)SpriteSheet<_sheetSprites>.Source[256 + Type].Height * Scale * 0.5f) >> 4, rGB);
		}
	}

	public unsafe static int NewGore(Vector2 Position, Vector2 Velocity, int Type, double Scale = 1.0)
	{
		int num = LastGoreIndex++ & 0x7F;
		fixed (Gore* ptr = &Main.GoreSet[num])
		{
			ptr->Position = Position;
			ptr->Velocity = Velocity;
			ptr->Velocity.Y -= (float)Main.Rand.Next(10, 31) * 0.1f;
			ptr->Velocity.X += (float)Main.Rand.Next(-20, 21) * 0.1f;
			ptr->Type = (byte)Type;
			ptr->Active = 1;
			ptr->Rotation = 0f;
			if (Type == 16 || Type == 17)
			{
				ptr->Sticky = false;
				ptr->Alpha = 100;
				ptr->Scale = 0.7f;
				ptr->Light = 1f;
				return num;
			}
			if ((Type >= 11 && Type <= 13) || (Type >= 61 && Type <= 63) || Type == 99)
			{
				ptr->Sticky = false;
			}
			else
			{
				ptr->Sticky = true;
				ptr->TimeLeft = 360;
			}
			ptr->Scale = (float)Scale;
			ptr->Alpha = 0;
			ptr->Light = 0f;
		}
		return num;
	}

	public static int NewGore(int X, int Y, Vector2 Velocity, int Type)
	{
		return NewGore(new Vector2(X, Y), Velocity, Type);
	}

	public Color GetAlpha(Color GoreColour)
	{
		int r;
		int g;
		int b;
		if (Type == 16 || Type == 17)
		{
			r = GoreColour.R;
			g = GoreColour.G;
			b = GoreColour.B;
		}
		else
		{
			double num = (255.0 - (double)Alpha) / 255.0;
			r = (int)((double)(int)GoreColour.R * num);
			g = (int)((double)(int)GoreColour.G * num);
			b = (int)((double)(int)GoreColour.B * num);
		}
		int num2 = GoreColour.A - Alpha;
		if (num2 < 0)
		{
			num2 = 0;
		}
		if (num2 > 255)
		{
			num2 = 255;
		}
		return new Color(r, g, b, num2);
	}
}
