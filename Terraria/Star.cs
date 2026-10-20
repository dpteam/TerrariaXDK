using Microsoft.Xna.Framework;

namespace Terraria;

public struct Star
{
	public const int MaxNumStars = 96;

	public const int NumStarTypes = 5;

	public static Star[] star = new Star[96];

	public Vector2 position;

	public float scale;

	public float rotation;

	public int type;

	public float twinkle;

	public float twinkleSpeed;

	public float rotationSpeed;

	public static void SpawnStars()
	{
		for (int i = 0; i < 96; i++)
		{
			star[i] = new Star
			{
				type = Main.Rand.Next(5),
				rotation = (float)Main.Rand.Next(628) * 0.01f,
				scale = (float)Main.Rand.Next(50, 120) * 0.01f,
				twinkle = (float)Main.Rand.Next(101) * 0.01f,
				twinkleSpeed = (float)Main.Rand.Next(40, 100) * 0.0001f,
				rotationSpeed = (float)Main.Rand.Next(10, 40) * 0.0001f
			};
			int width = SpriteSheet<_sheetTiles>.Source[star[i].type + 19].Width;
			int height = SpriteSheet<_sheetTiles>.Source[star[i].type + 19].Height;
			int num;
			do
			{
				num = Main.Rand.Next(Main.ResolutionWidth - width);
			}
			while ((num < Main.ResolutionHeight / 20 || num > Main.ResolutionHeight - Main.ResolutionHeight / 20) && Main.Rand.Next(4) != 0);
			star[i].position.X = num + (width >> 1);
			do
			{
				num = Main.Rand.Next(Main.ResolutionHeight - height);
			}
			while ((num < Main.ResolutionHeight / 20 || num > Main.ResolutionHeight - Main.ResolutionHeight / 20) && Main.Rand.Next(4) != 0);
			star[i].position.Y = num + (height >> 1);
			if (Main.Rand.Next(2) == 0)
			{
				star[i].twinkleSpeed *= -1f;
			}
			if (Main.Rand.Next(2) == 0)
			{
				star[i].rotationSpeed *= -1f;
			}
		}
	}

	public static void UpdateStars()
	{
		for (int i = 0; i < 96; i++)
		{
			star[i].twinkle += star[i].twinkleSpeed;
			if (star[i].twinkle > 1f)
			{
				star[i].twinkle = 1f;
				star[i].twinkleSpeed *= -1f;
			}
			else if ((double)star[i].twinkle < 0.5)
			{
				star[i].twinkle = 0.5f;
				star[i].twinkleSpeed *= -1f;
			}
			star[i].rotation += star[i].rotationSpeed;
			if ((double)star[i].rotation > 6.28000020980835)
			{
				star[i].rotation -= 6.28f;
			}
			if (star[i].rotation < 0f)
			{
				star[i].rotation += 6.28f;
			}
		}
	}
}
