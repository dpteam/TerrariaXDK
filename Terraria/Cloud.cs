using Microsoft.Xna.Framework;

namespace Terraria;

public struct Cloud
{
	public const int MaxNumClouds = 20;

	public const int MaxNumCloudTypes = 4;

	public static Cloud[] ActiveCloud = new Cloud[20];

	public static int NumClouds = 20;

	public static float WindSpeed = 0f;

	public static float SpeedOfWind = 0f;

	public static bool ResetClouds = true;

	public Vector2 CloudPos;

	public float CloudScale;

	public bool IsActive;

	public byte CloudType;

	public ushort CloudWidth;

	public ushort CloudHeight;

	public static void Initialize()
	{
		for (int i = 0; i < 20; i++)
		{
			ActiveCloud[i].Init();
		}
	}

	public void Init()
	{
		IsActive = false;
		CloudType = 0;
		CloudWidth = 0;
		CloudHeight = 0;
	}

	public unsafe static void AddCloud()
	{
		fixed (Cloud* activeCloud = ActiveCloud)
		{
			Cloud* ptr = activeCloud;
			for (int num = 19; num >= 0; num--)
			{
				if (!ptr->IsActive)
				{
					ptr->CloudType = (byte)Main.Rand.Next(4);
					int num2 = ptr->CloudType + 8;
					ptr->CloudScale = (float)Main.Rand.Next(50, 131) * 0.01f;
					ptr->CloudWidth = (ushort)((float)SpriteSheet<_sheetTiles>.Source[num2].Width * ptr->CloudScale);
					ptr->CloudHeight = (ushort)((float)SpriteSheet<_sheetTiles>.Source[num2].Height * ptr->CloudScale);
					if (WindSpeed > 0f)
					{
						ptr->CloudPos.X = -ptr->CloudWidth;
					}
					else
					{
						ptr->CloudPos.X = Main.ResolutionWidth;
					}
					ptr->CloudPos.Y = Main.Rand.Next(-135, 135) - Main.Rand.Next(180);
					ptr->IsActive = true;
					break;
				}
				ptr++;
			}
		}
	}

	public Color CloudColor(Color BgColor)
	{
		float num = (CloudScale - 0.4f) * 0.9f;
		float num2 = 255f - (float)(255 - BgColor.R) * 1.1f;
		float num3 = 255f - (float)(255 - BgColor.G) * 1.1f;
		float num4 = 255f - (float)(255 - BgColor.B) * 1.1f;
		float num5 = 255f;
		num2 *= num;
		num3 *= num;
		num4 *= num;
		num5 *= num;
		if (num2 < 0f)
		{
			num2 = 0f;
		}
		if (num3 < 0f)
		{
			num3 = 0f;
		}
		if (num4 < 0f)
		{
			num4 = 0f;
		}
		if (num5 < 0f)
		{
			num5 = 0f;
		}
		return new Color((byte)num2, (byte)num3, (byte)num4, (byte)num5);
	}

	public unsafe static void UpdateClouds()
	{
		if (ResetClouds)
		{
			ResetClouds = false;
			NumClouds = Main.Rand.Next(10, 20);
			do
			{
				WindSpeed = (float)Main.Rand.Next(-100, 101) * 0.01f;
			}
			while (WindSpeed == 0f);
			for (int i = 0; i < 20; i++)
			{
				ActiveCloud[i].IsActive = false;
			}
			for (int j = 0; j < NumClouds; j++)
			{
				AddCloud();
			}
			for (int k = 0; k < NumClouds; k++)
			{
				int num = Main.Rand.Next(Main.ResolutionWidth);
				if (ActiveCloud[k].CloudPos.X < 0f)
				{
					ActiveCloud[k].CloudPos.X += num;
				}
				else
				{
					ActiveCloud[k].CloudPos.X -= num;
				}
			}
		}
		SpeedOfWind += (float)Main.Rand.Next(-20, 21) * 0.0001f;
		if ((double)SpeedOfWind < -0.002)
		{
			SpeedOfWind = -0.002f;
		}
		else if ((double)SpeedOfWind > 0.002)
		{
			SpeedOfWind = 0.002f;
		}
		WindSpeed += SpeedOfWind;
		if ((double)WindSpeed < -0.4)
		{
			WindSpeed = -0.4f;
		}
		else if ((double)WindSpeed > 0.4)
		{
			WindSpeed = 0.4f;
		}
		NumClouds += Main.Rand.Next(-1, 2);
		if (NumClouds < 0)
		{
			NumClouds = 0;
		}
		else if (NumClouds > 20)
		{
			NumClouds = 20;
		}
		int num2 = 0;
		for (int l = 0; l < 20; l++)
		{
			fixed (Cloud* ptr = &ActiveCloud[l])
			{
				if (ptr->IsActive)
				{
					ptr->Update();
					num2++;
				}
			}
		}
		if (num2 < NumClouds)
		{
			AddCloud();
		}
	}

	public void Update()
	{
		CloudPos.X += WindSpeed * CloudScale * 2f;
		if (WindSpeed > 0f)
		{
			if (CloudPos.X > (float)Main.ResolutionWidth)
			{
				IsActive = false;
			}
		}
		else if (CloudPos.X < (float)(-CloudWidth))
		{
			IsActive = false;
		}
	}
}
