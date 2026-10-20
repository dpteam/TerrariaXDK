using System.Threading;
using Microsoft.Xna.Framework;

namespace Terraria;

public sealed class Lighting
{
	private struct TempLight
	{
		public short X;

		public short Y;

		public Vector3 ColorRGB;
	}

	public const int MaxRenderCount = 4;

	public const int OffScreenTiles = 34;

	public const int OffScreenTiles2 = 14;

	private int MaxLightArrayY;

	private int ColourH;

	public const float MaxBrightness = 1.2f;

	private const int FirstToLightY7 = 0;

	private const int MaxTempLights = 1024;

	private int MaxLightArrayX;

	private int ColourW;

	public float BrightnessLevel;

	public float DefBrightness;

	public float OldSkyColor;

	public float SkyColor;

	private float LightColorR;

	private float LightColorG;

	private float LightColorB;

	public Vector3[,] Colour;

	public Vector3[,] Colour2;

	public byte[] StopAndWetLight;

	private int FirstTileX;

	private int FirstTileY;

	private int LastTileX;

	private int LastTileY;

	private int FirstToLightX;

	private int FirstToLightY;

	public short ScreenX = -1;

	public short ScreenY;

	public int MinimumX;

	public int MaximumX;

	public int MinimumY;

	public int MaximumY;

	private float NegLight = 0.04f;

	private float NegLight2 = 0.16f;

	private float WetLightR = 0.16f;

	private float WetLightG = 0.16f;

	private int MinimumX7;

	private int MaximumX7;

	private int MinimumY7;

	private int MaximumY7;

	private int FirstTileX7;

	private int LastTileX7;

	private int LastTileY7;

	private int FirstTileY7;

	private int LastToLightY7;

	private int FirstToLightX27;

	private int LastToLightX27;

	private int FirstToLightY27;

	private int LastToLightY27;

	private Thread ActiveWorkerThread;

	private readonly AutoResetEvent WorkerThreadReady = new AutoResetEvent(initialState: false);

	private readonly AutoResetEvent WorkerThreadGo = new AutoResetEvent(initialState: false);

	private volatile bool QuitWorkerThread;

	public static int TempLightCount;

	private static readonly TempLight[] TempLightSet = new TempLight[1024];

	public void StartWorkerThread()
	{
		if (ActiveWorkerThread != null)
		{
			StopWorkerThread();
		}
		QuitWorkerThread = false;
		WorkerThreadReady.Reset();
		WorkerThreadGo.Reset();
		ActiveWorkerThread = new Thread(WorkerThread);
		ActiveWorkerThread.IsBackground = true;
		ActiveWorkerThread.Start();
	}

	public void StopWorkerThread()
	{
		if (ActiveWorkerThread != null)
		{
			QuitWorkerThread = true;
			WorkerThreadGo.Set();
			ActiveWorkerThread.Join();
			ActiveWorkerThread = null;
		}
	}

	private void WorkerThread()
	{
		do
		{
			WorkerThreadReady.Set();
			WorkerThreadGo.WaitOne();
			lock (this)
			{
				DoColors();
			}
		}
		while (!QuitWorkerThread);
		WorkerThreadReady.Set();
	}

	public void SetWidth(int ResolutionWidth)
	{
		lock (this)
		{
			MaxLightArrayX = (ResolutionWidth + (int)(64f * Main.ScreenMultiplier) >> 4) + 68;
			ColourW = MaxLightArrayX + 10;
			MaxLightArrayY = (Main.ResolutionHeight + (int)(64f * Main.ScreenMultiplier) >> 4) + 68 + (byte)(2f * Main.ScreenMultiplier);
			ColourH = MaxLightArrayY + 10;
			Colour = new Vector3[MaxLightArrayX, MaxLightArrayY];
			Colour2 = new Vector3[ColourW, ColourH];
			StopAndWetLight = new byte[ColourW * ColourH];
		}
	}

	public unsafe void LightTiles(WorldView CurrentView)
	{
		FirstTileX = CurrentView.FirstTileX;
		LastTileX = CurrentView.LastTileX;
		FirstTileY = CurrentView.FirstTileY;
		LastTileY = CurrentView.LastTileY;
		FirstToLightX = FirstTileX - 34;
		FirstToLightY = FirstTileY - 34;
		int num = LastTileX + 34;
		int num2 = LastTileY + 34;
		if (FirstToLightX < 0)
		{
			FirstToLightX = 0;
		}
		if (num >= Main.MaxTilesX)
		{
			num = Main.MaxTilesX - 1;
		}
		if (FirstToLightY < 0)
		{
			FirstToLightY = 0;
		}
		if (num2 >= Main.MaxTilesY)
		{
			num2 = Main.MaxTilesY - 1;
		}
		if (Main.RenderCount <= 4)
		{
			int num3 = (CurrentView.ScreenPosition.X >> 4) - (CurrentView.ScreenLastPosition.X >> 4);
			if (num3 < 0 && num3 >= -4)
			{
				fixed (Vector3* colour = Colour)
				{
					int num4 = (MaxLightArrayX + num3) * MaxLightArrayY - 1;
					num3 *= MaxLightArrayY;
					Vector3* ptr = colour + (MaxLightArrayX * MaxLightArrayY - 1);
					do
					{
						*ptr = ptr[num3];
						ptr--;
					}
					while (--num4 >= 0);
				}
			}
			else if (num3 > 0 && num3 <= 4)
			{
				fixed (Vector3* colour2 = Colour)
				{
					int num5 = (MaxLightArrayX - num3) * MaxLightArrayY - 1;
					num3 *= MaxLightArrayY;
					Vector3* ptr2 = colour2;
					do
					{
						*ptr2 = ptr2[num3];
						ptr2++;
					}
					while (--num5 >= 0);
				}
			}
			num3 = (CurrentView.ScreenPosition.Y >> 4) - (CurrentView.ScreenLastPosition.Y >> 4);
			if (num3 < 0 && num3 >= -4)
			{
				for (int i = 0; i < MaxLightArrayX; i++)
				{
					fixed (Vector3* ptr3 = &Colour[i, 0])
					{
						for (int num6 = MaxLightArrayY + num3; num6 > -num3; num6--)
						{
							ptr3[num6] = ptr3[num6 + num3];
						}
					}
				}
			}
			else if (num3 > 0 && num3 <= 4)
			{
				for (int j = 0; j < MaxLightArrayX; j++)
				{
					fixed (Vector3* ptr4 = &Colour[j, 0])
					{
						for (int k = 0; k < MaxLightArrayY - num3; k++)
						{
							ptr4[k] = ptr4[k + num3];
						}
					}
				}
			}
			OldSkyColor = SkyColor;
			SkyColor = (CurrentView.WorldTime.TileColorFore.X + CurrentView.WorldTime.TileColorFore.Y + CurrentView.WorldTime.TileColorFore.Z) * (1f / 3f);
			if (OldSkyColor == SkyColor)
			{
				return;
			}
			int num7 = ((num2 <= Main.WorldSurface) ? num2 : Main.WorldSurface);
			fixed (Tile* tileSet = Main.TileSet)
			{
				for (int l = FirstToLightX; l < num; l++)
				{
					Tile* ptr5 = tileSet + (l * 1440 + FirstToLightY);
					for (int m = FirstToLightY; m < num7; m++)
					{
						if ((ptr5->IsActive == 0 || !Main.TileNoSunLight[ptr5->Type]) && (ptr5->WallType == 0 || ptr5->WallType == 21) && ptr5->Liquid < 200)
						{
							fixed (Vector3* ptr6 = &Colour[l - FirstToLightX, m - FirstToLightY])
							{
								if (ptr6->X < SkyColor)
								{
									ptr6->X = CurrentView.WorldTime.TileColorFore.X;
									if (ptr6->Y < SkyColor)
									{
										ptr6->Y = CurrentView.WorldTime.TileColorFore.Y;
									}
									if (ptr6->Z < SkyColor)
									{
										ptr6->Z = CurrentView.WorldTime.TileColorFore.Z;
									}
								}
							}
						}
						ptr5++;
					}
				}
			}
			return;
		}
		WorkerThreadReady.WaitOne();
		int num8 = CurrentView.ScreenPosition.X >> 4;
		int num9 = CurrentView.ScreenPosition.Y >> 4;
		if (ScreenX >= 0)
		{
			num8 -= ScreenX;
			num9 -= ScreenY;
			int num10 = ((num8 < 0) ? (-num8) : 0);
			int num11 = ((num9 < 0) ? (-num9) : 0);
			fixed (Vector3* colour3 = Colour)
			{
				fixed (Vector3* colour4 = Colour2)
				{
					for (int n = num10; n < MaxLightArrayX; n++)
					{
						Vector3* ptr7 = colour3 + (n * MaxLightArrayY + num11);
						Vector3* ptr8 = colour4 + ((n + num8) * ColourH + num11 + num9);
						for (int num12 = num11; num12 < MaxLightArrayY; num12++)
						{
							*(ptr7++) = *(ptr8++);
						}
					}
				}
			}
		}
		fixed (Vector3* colour5 = Colour2)
		{
			fixed (byte* stopAndWetLight = StopAndWetLight)
			{
				Vector3* ptr9 = colour5;
				byte* ptr10 = stopAndWetLight;
				for (int num13 = ColourW * ColourH - 1; num13 >= 0; num13--)
				{
					ptr9->X = 0f;
					ptr9->Y = 0f;
					ptr9->Z = 0f;
					*ptr10 = 0;
					ptr9++;
					ptr10++;
				}
			}
		}
		fixed (TempLight* tempLightSet = TempLightSet)
		{
			fixed (Vector3* colour6 = Colour2)
			{
				TempLight* ptr11 = tempLightSet;
				for (int num14 = TempLightCount - 1; num14 >= 0; num14--)
				{
					int num15 = ptr11->X - FirstToLightX;
					if (num15 >= 0 && num15 < ColourW)
					{
						int num16 = ptr11->Y - FirstToLightY;
						if (num16 >= 0 && num16 < ColourH)
						{
							Vector3* ptr12 = colour6 + (num16 + num15 * ColourH);
							ptr12->X = ptr11->ColorRGB.X;
							ptr12->Y = ptr11->ColorRGB.Y;
							ptr12->Z = ptr11->ColorRGB.Z;
						}
					}
					ptr11++;
				}
			}
		}
		int num17 = FirstTileX - 14;
		int num18 = FirstTileY - 14;
		int num19 = LastTileX + 14;
		int num20 = LastTileY + 14;
		if (num17 < 0)
		{
			num17 = 0;
		}
		if (num19 >= Main.MaxTilesX)
		{
			num19 = Main.MaxTilesX - 1;
		}
		if (num18 < 0)
		{
			num18 = 0;
		}
		if (num20 >= Main.MaxTilesY)
		{
			num20 = Main.MaxTilesY - 1;
		}
		if (NPC.WoF >= 0 && CurrentView.Player.IsHorrified)
		{
			try
			{
				int num21 = (CurrentView.ScreenPosition.Y >> 4) - 10;
				int num22 = (CurrentView.ScreenPosition.Y + Main.ResolutionHeight >> 4) + 10;
				int num23 = Main.NPCSet[NPC.WoF].XYWH.X >> 4;
				num23 = ((Main.NPCSet[NPC.WoF].Direction <= 0) ? (num23 + 2) : (num23 - 3));
				int num24 = num23 + 8;
				Vector3 value = new Vector3(0.2f * (0.5f * Main.DemonTorch + 1f * (1f - Main.DemonTorch)), 0.03f, 0.3f * (Main.DemonTorch + 0.5f * (1f - Main.DemonTorch)));
				for (int num25 = num23; num25 <= num24; num25++)
				{
					for (int num26 = num21; num26 <= num22; num26++)
					{
						Vector3.Max(ref value, ref Colour2[num25 - FirstToLightX, num26 - FirstToLightY], out Colour2[num25 - FirstToLightX, num26 - FirstToLightY]);
					}
				}
			}
			catch
			{
			}
		}
		int firstToLightX = FirstToLightX;
		int num27 = num;
		int firstToLightY = FirstToLightY;
		int num28 = num2;
		int num29 = ((num28 < Main.WorldSurface) ? num28 : Main.WorldSurface);
		fixed (Tile* tileSet2 = Main.TileSet)
		{
			for (int num30 = firstToLightX; num30 < num27; num30++)
			{
				Tile* ptr13 = tileSet2 + (num30 * 1440 + firstToLightY);
				for (int num31 = firstToLightY; num31 < num29; num31++)
				{
					int wallType = ptr13->WallType;
					if ((wallType == 0 || wallType == 21) && ptr13->Liquid < 200 && (ptr13->IsActive == 0 || !Main.TileNoSunLight[ptr13->Type]))
					{
						fixed (Vector3* ptr14 = &Colour2[num30 - FirstToLightX, num31 - FirstToLightY])
						{
							if (ptr14->X < SkyColor)
							{
								ptr14->X = CurrentView.WorldTime.TileColorFore.X;
								if (ptr14->Y < SkyColor)
								{
									ptr14->Y = CurrentView.WorldTime.TileColorFore.Y;
								}
								if (ptr14->Z < SkyColor)
								{
									ptr14->Z = CurrentView.WorldTime.TileColorFore.Z;
								}
							}
						}
					}
					ptr13++;
				}
			}
		}
		NegLight = 0.91f;
		NegLight2 = 0.72f;
		WetLightG = 0.97f * NegLight * UI.BlueWave;
		WetLightR = 0.88f * NegLight * UI.BlueWave;
		if (CurrentView.Player.NightVision)
		{
			NegLight *= 1.03f;
			NegLight2 *= 1.03f;
		}
		if (CurrentView.Player.blind)
		{
			NegLight *= 0.95f;
			NegLight2 *= 0.95f;
		}
		CurrentView.InactiveTiles = 0;
		CurrentView.SandTiles = 0;
		CurrentView.EvilTiles = 0;
		CurrentView.SnowTiles = 0;
		CurrentView.HolyTiles = 0;
		CurrentView.MeteorTiles = 0;
		CurrentView.JungleTiles = 0;
		CurrentView.DungeonTiles = 0;
		CurrentView.SkyTiles = 0;
		CurrentView.MusicBox = -1;
		MinimumX = ColourW;
		MaximumX = 0;
		MinimumY = ColourH;
		MaximumY = 0;
		fixed (Tile* tileSet3 = Main.TileSet)
		{
			for (int num32 = firstToLightX; num32 < num27; num32++)
			{
				Tile* ptr15 = tileSet3 + (num32 * 1440 + firstToLightY);
				for (int num33 = firstToLightY; num33 < num28; num33++)
				{
					int num34 = num32 - FirstToLightX;
					int num35 = num33 - FirstToLightY;
					fixed (Vector3* ptr16 = &Colour2[num34, num35])
					{
						if (ptr15->IsActive == 0)
						{
							CurrentView.InactiveTiles++;
						}
						else
						{
							int type = ptr15->Type;
							int num36 = num27 - firstToLightX - 99 >> 1;
							int num37 = num28 - firstToLightY - 87 >> 1;
							if (num32 > firstToLightX + num36 && num32 < num27 - num36 && num33 > firstToLightY + num37 && num33 < num28 - num37)
							{
								switch ((EntityID.TileID)type)
								{
								case EntityID.TileID.CORRUPT_GRASS:
								case EntityID.TileID.SHORT_CORRUPT_PLANTS:
								case EntityID.TileID.EBONSTONE:
								case EntityID.TileID.CORRUPTION_THORN:
									CurrentView.EvilTiles++;
									break;
								case EntityID.TileID.EBONSAND:
									CurrentView.SandTiles++;
									CurrentView.EvilTiles++;
									break;
								case EntityID.TileID.HALLOWED_GRASS:
								case EntityID.TileID.SHORT_HALLOWED_PLANTS:
								case EntityID.TileID.TALL_HALLOWED_PLANTS:
								case EntityID.TileID.PEARLSTONE:
									CurrentView.HolyTiles++;
									break;
								case EntityID.TileID.PEARLSAND:
									CurrentView.SandTiles++;
									CurrentView.HolyTiles++;
									break;
								case EntityID.TileID.SUNFLOWER:
									CurrentView.EvilTiles -= 5;
									break;
								case EntityID.TileID.METEORITE:
									CurrentView.MeteorTiles++;
									break;
								case EntityID.TileID.BLUE_BRICK:
								case EntityID.TileID.GREEN_BRICK:
								case EntityID.TileID.PINK_BRICK:
									CurrentView.DungeonTiles++;
									break;
								case EntityID.TileID.JUNGLE_GRASS:
								case EntityID.TileID.SHORT_JUNGLE_PLANTS:
								case EntityID.TileID.JUNGLE_VINE:
								case EntityID.TileID.DAYBLOOM_BLOOMING:
									CurrentView.JungleTiles++;
									break;
								case EntityID.TileID.SAND:
									CurrentView.SandTiles++;
									break;
								case EntityID.TileID.SNOW:
								case EntityID.TileID.SNOW_BRICK:
									CurrentView.SnowTiles++;
									break;
								case EntityID.TileID.GOLD_BRICK:
								case EntityID.TileID.SILVER_BRICK:
								case EntityID.TileID.COPPER_BRICK:
									CurrentView.SkyTiles++;
									break;
								case EntityID.TileID.MUSIC_BOX:
									if (ptr15->FrameX >= 36)
									{
										CurrentView.MusicBox = ptr15->FrameY / 36;
									}
									break;
								}
							}
							if (Main.TileBlockLight[type])
							{
								StopAndWetLight[num34 * ColourH + num35] = 1;
							}
							if (Main.TileLighted[type])
							{
								switch ((EntityID.TileID)type)
								{
								case EntityID.TileID.LAMP_POST:
									if (ptr15->FrameY <= 18 && ptr15->FrameX == 0)
									{
										float num83 = 1f;
										float num84 = 1f;
										float num85 = 1f;
										if (num83 > ptr16->X)
										{
											ptr16->X = num83;
										}
										if (num84 > ptr16->Y)
										{
											ptr16->Y = num84;
										}
										if (num85 > ptr16->Z)
										{
											ptr16->Z = num85;
										}
									}
									break;
								case EntityID.TileID.TIKI_TORCH:
									if (ptr15->FrameY == 0 && ptr15->FrameX == 0)
									{
										float num62 = 1f;
										float num63 = 0.97f;
										float num64 = 0.85f;
										if (num62 > ptr16->X)
										{
											ptr16->X = num62;
										}
										if (num63 > ptr16->Y)
										{
											ptr16->Y = num63;
										}
										if (num64 > ptr16->Z)
										{
											ptr16->Z = num64;
										}
									}
									break;
								case EntityID.TileID.SKULL_LANTERN:
									if (ptr15->FrameY == 0)
									{
										float num68 = 1f;
										float num69 = 0.97f;
										float num70 = 0.85f;
										if (num68 > ptr16->X)
										{
											ptr16->X = num68;
										}
										if (num69 > ptr16->Y)
										{
											ptr16->Y = num69;
										}
										if (num70 > ptr16->Z)
										{
											ptr16->Z = num70;
										}
									}
									break;
								case EntityID.TileID.TORCH:
									if (ptr15->FrameX < 66)
									{
										float num86;
										float num87;
										float num88;
										switch (ptr15->FrameY)
										{
										case 22:
											num86 = 0f;
											num87 = 0.1f;
											num88 = 1.3f;
											break;
										case 44:
											num86 = 1f;
											num87 = 0.1f;
											num88 = 0.1f;
											break;
										case 66:
											num86 = 0f;
											num87 = 1f;
											num88 = 0.1f;
											break;
										case 88:
											num86 = 0.95f;
											num87 = 0.1f;
											num88 = 0.95f;
											break;
										case 110:
											num86 = 1.3f;
											num87 = 1.3f;
											num88 = 1.3f;
											break;
										case 132:
											num86 = 1f;
											num87 = 1f;
											num88 = 0.1f;
											break;
										case 154:
											num86 = 0.5f * Main.DemonTorch + 1f * (1f - Main.DemonTorch);
											num87 = 0.3f;
											num88 = Main.DemonTorch + 0.5f * (1f - Main.DemonTorch);
											break;
										case 176:
											num86 = 0.85f;
											num87 = 1f;
											num88 = 0.7f;
											break;
										default:
											num86 = 1f;
											num87 = 0.95f;
											num88 = 0.8f;
											break;
										}
										if (num86 > ptr16->X)
										{
											ptr16->X = num86;
										}
										if (num87 > ptr16->Y)
										{
											ptr16->Y = num87;
										}
										if (num88 > ptr16->Z)
										{
											ptr16->Z = num88;
										}
									}
									break;
								case EntityID.TileID.CANDLE:
									if (ptr15->FrameX == 0)
									{
										float num65 = 1f;
										float num66 = 0.95f;
										float num67 = 0.65f;
										if (num65 > ptr16->X)
										{
											ptr16->X = num65;
										}
										if (num66 > ptr16->Y)
										{
											ptr16->Y = num66;
										}
										if (num67 > ptr16->Z)
										{
											ptr16->Z = num67;
										}
									}
									break;
								case EntityID.TileID.PRESENT:
									if (ptr15->FrameX < 54)
									{
										float num52 = 1f;
										float num53 = 0.95f;
										float num54 = 0.65f;
										if (num52 > ptr16->X)
										{
											ptr16->X = num52;
										}
										if (num53 > ptr16->Y)
										{
											ptr16->Y = num53;
										}
										if (num54 > ptr16->Z)
										{
											ptr16->Z = num54;
										}
									}
									break;
								case EntityID.TileID.CANDELABRA:
									if (ptr15->FrameX < 36)
									{
										float num108 = 1f;
										float num109 = 0.95f;
										float num110 = 0.65f;
										if (num108 > ptr16->X)
										{
											ptr16->X = num108;
										}
										if (num109 > ptr16->Y)
										{
											ptr16->Y = num109;
										}
										if (num110 > ptr16->Z)
										{
											ptr16->Z = num110;
										}
									}
									break;
								case EntityID.TileID.CHANDELIER:
								case EntityID.TileID.JACK_O_LANTERN:
									if (ptr15->FrameX < 54)
									{
										float num77 = 1f;
										float num78 = 0.95f;
										float num79 = 0.8f;
										if (num77 > ptr16->X)
										{
											ptr16->X = num77;
										}
										if (num78 > ptr16->Y)
										{
											ptr16->Y = num78;
										}
										if (num79 > ptr16->Z)
										{
											ptr16->Z = num79;
										}
									}
									break;
								case EntityID.TileID.CHINESE_LANTERN:
									if (ptr15->FrameX < 36)
									{
										float num49 = 1f;
										float num50 = 0.95f;
										float num51 = 0.8f;
										if (num49 > ptr16->X)
										{
											ptr16->X = num49;
										}
										if (num50 > ptr16->Y)
										{
											ptr16->Y = num50;
										}
										if (num51 > ptr16->Z)
										{
											ptr16->Z = num51;
										}
									}
									break;
								case EntityID.TileID.FURNACE:
								case EntityID.TileID.ADAMANTITE_FORGE:
								{
									float num96 = 0.83f;
									float num97 = 0.6f;
									float num98 = 0.5f;
									if (num96 > ptr16->X)
									{
										ptr16->X = num96;
									}
									if (num97 > ptr16->Y)
									{
										ptr16->Y = num97;
									}
									if (num98 > ptr16->Z)
									{
										ptr16->Z = num98;
									}
									break;
								}
								case EntityID.TileID.HELLFORGE:
								{
									float num105 = 0.75f;
									float num106 = 0.45f;
									float num107 = 0.25f;
									if (num105 > ptr16->X)
									{
										ptr16->X = num105;
									}
									if (num106 > ptr16->Y)
									{
										ptr16->Y = num106;
									}
									if (num107 > ptr16->Z)
									{
										ptr16->Z = num107;
									}
									break;
								}
								case EntityID.TileID.METEORITE:
								{
									float num102 = 0.56f;
									float num103 = 0.43f;
									float num104 = 0.15f;
									if (num102 > ptr16->X)
									{
										ptr16->X = num102;
									}
									if (num103 > ptr16->Y)
									{
										ptr16->Y = num103;
									}
									if (num104 > ptr16->Z)
									{
										ptr16->Z = num104;
									}
									break;
								}
								case EntityID.TileID.DEMONITE_ORE:
								case EntityID.TileID.DEMONITE_BRICK:
								{
									float num93 = 0.12f;
									float num94 = 0.07f;
									float num95 = 0.32f;
									if (num93 > ptr16->X)
									{
										ptr16->X = num93;
									}
									if (num94 > ptr16->Y)
									{
										ptr16->Y = num94;
									}
									if (num95 > ptr16->Z)
									{
										ptr16->Z = num95;
									}
									break;
								}
								case EntityID.TileID.CHAIN_LANTERN:
									if (ptr15->FrameX == 0)
									{
										float num99 = 0.65f;
										float num100 = 0.8f;
										float num101 = 0.54f;
										if (num99 > ptr16->X)
										{
											ptr16->X = num99;
										}
										if (num100 > ptr16->Y)
										{
											ptr16->Y = num100;
										}
										if (num101 > ptr16->Z)
										{
											ptr16->Z = num101;
										}
									}
									break;
								case EntityID.TileID.WATER_CANDLE:
								{
									float num45 = 0.3f;
									float num46 = 0.3f;
									float num47 = 0.75f;
									if (num45 > ptr16->X)
									{
										ptr16->X = num45;
									}
									if (num46 > ptr16->Y)
									{
										ptr16->Y = num46;
									}
									if (num47 > ptr16->Z)
									{
										ptr16->Z = num47;
									}
									break;
								}
								case EntityID.TileID.MUSHROOM_GRASS:
								case EntityID.TileID.GLOWING_MUSHROOM:
								case EntityID.TileID.GIANT_GLOWING_MUSHROOM:
								{
									float num89 = (float)Main.Rand.Next(28, 42) * 0.005f;
									num89 += (float)(270 - UI.MouseTextBrightness) * 0.002f;
									float num90 = 0.1f;
									float num91 = 0.3f + num89;
									float num92 = 0.6f + num89;
									if (num90 > ptr16->X)
									{
										ptr16->X = num90;
									}
									if (num91 > ptr16->Y)
									{
										ptr16->Y = num91;
									}
									if (num92 > ptr16->Z)
									{
										ptr16->Z = num92;
									}
									break;
								}
								case EntityID.TileID.SHORT_JUNGLE_PLANTS:
									if (ptr15->FrameX == 144)
									{
										float num71 = 0.42f;
										float num72 = 0.81f;
										float num73 = 0.52f;
										if (num71 > ptr16->X)
										{
											ptr16->X = num71;
										}
										if (num72 > ptr16->Y)
										{
											ptr16->Y = num72;
										}
										if (num73 > ptr16->Z)
										{
											ptr16->Z = num73;
										}
									}
									break;
								case EntityID.TileID.DEMON_ALTAR:
								case EntityID.TileID.SHADOW_ORB:
								{
									float num41 = (float)Main.Rand.Next(-5, 6) * 0.0025f;
									float num42 = 0.31f + num41;
									float num43 = 0.1f;
									float num44 = 0.44f + num41;
									if (num42 > ptr16->X)
									{
										ptr16->X = num42;
									}
									if (num43 > ptr16->Y)
									{
										ptr16->Y = num43;
									}
									if (num44 > ptr16->Z)
									{
										ptr16->Z = num44;
									}
									break;
								}
								case EntityID.TileID.DAYBLOOM_BLOOMING:
									switch (ptr15->FrameX / 18)
									{
									case 2:
									{
										float num58 = (float)(270 - UI.MouseTextBrightness) * 0.00125f;
										if (num58 > 1f)
										{
											num58 = 1f;
										}
										else if (num58 < 0f)
										{
											num58 = 0f;
										}
										float num59 = 0.7f * num58;
										float num60 = num58;
										float num61 = 0.1f * num58;
										if (num59 > ptr16->X)
										{
											ptr16->X = num59;
										}
										if (num60 > ptr16->Y)
										{
											ptr16->Y = num60;
										}
										if (num61 > ptr16->Z)
										{
											ptr16->Z = num61;
										}
										break;
									}
									case 5:
									{
										float num55 = 0.9f;
										float num56 = 0.72f;
										float num57 = 0.18f;
										if (num55 > ptr16->X)
										{
											ptr16->X = num55;
										}
										if (num56 > ptr16->Y)
										{
											ptr16->Y = num56;
										}
										if (num57 > ptr16->Z)
										{
											ptr16->Z = num57;
										}
										break;
									}
									}
									break;
								case EntityID.TileID.DAYBLOOM_MATURE:
									if (ptr15->FrameX == 18 && !CurrentView.WorldTime.DayTime)
									{
										float num80 = 0.1f;
										float num81 = 0.4f;
										float num82 = 0.6f;
										if (num80 > ptr16->X)
										{
											ptr16->X = num80;
										}
										if (num81 > ptr16->Y)
										{
											ptr16->Y = num81;
										}
										if (num82 > ptr16->Z)
										{
											ptr16->Z = num82;
										}
									}
									break;
								case EntityID.TileID.DISCO_BALL:
									if (ptr15->FrameX < 36)
									{
										if (Main.DiscoRGB.X > ptr16->X)
										{
											ptr16->X = Main.DiscoRGB.X;
										}
										if (Main.DiscoRGB.Y > ptr16->Y)
										{
											ptr16->Y = Main.DiscoRGB.Y;
										}
										if (Main.DiscoRGB.Z > ptr16->Z)
										{
											ptr16->Z = Main.DiscoRGB.Z;
										}
									}
									break;
								case EntityID.TileID.CRYSTAL_BALL:
								{
									float num48 = (float)Main.Rand.Next(28, 42) * 0.01f;
									num48 += (float)(270 - UI.MouseTextBrightness) * 0.00125f;
									if (ptr16->Y < 0.1f * num48)
									{
										ptr16->Y = 0.3f * num48;
									}
									if (ptr16->Z < 0.3f * num48)
									{
										ptr16->Z = 0.6f * num48;
									}
									break;
								}
								case EntityID.TileID.CRYSTAL_SHARD:
								{
									float num74;
									float num75;
									float num76;
									if (ptr15->FrameX == 0 || ptr15->FrameX == 54 || ptr15->FrameX == 108)
									{
										num74 = 0f;
										num75 = 0.05f;
										num76 = 0.25f;
									}
									else if (ptr15->FrameX == 18 || ptr15->FrameX == 72 || ptr15->FrameX == 126)
									{
										num74 = 0.2f;
										num75 = 0f;
										num76 = 0.15f;
									}
									else
									{
										num74 = 0.1f;
										num75 = 0f;
										num76 = 0.2f;
									}
									if (ptr16->X < num74)
									{
										ptr16->X = num74 * (float)Main.Rand.Next(970, 1031) * 0.001f;
									}
									if (ptr16->Y < num75)
									{
										ptr16->Y = num75 * (float)Main.Rand.Next(970, 1031) * 0.001f;
									}
									if (ptr16->Z < num76)
									{
										ptr16->Z = num76 * (float)Main.Rand.Next(970, 1031) * 0.001f;
									}
									break;
								}
								case EntityID.TileID.XMAS_LIGHT:
									if (ptr15->FrameX <= 36)
									{
										float num38;
										float num39;
										float num40;
										if (ptr15->FrameX == 0)
										{
											num38 = 0.1f;
											num39 = 0.2f;
											num40 = 0.5f;
										}
										else if (ptr15->FrameX == 18)
										{
											num38 = 0.5f;
											num39 = 0.1f;
											num40 = 0.1f;
										}
										else
										{
											num38 = 0.2f;
											num39 = 0.5f;
											num40 = 0.1f;
										}
										if (ptr16->X < num38)
										{
											ptr16->X = num38 * (float)Main.Rand.Next(970, 1031) * 0.001f;
										}
										if (ptr16->Y < num39)
										{
											ptr16->Y = num39 * (float)Main.Rand.Next(970, 1031) * 0.001f;
										}
										if (ptr16->Z < num40)
										{
											ptr16->Z = num40 * (float)Main.Rand.Next(970, 1031) * 0.001f;
										}
									}
									break;
								}
							}
						}
						if (ptr15->Liquid > 0)
						{
							if (ptr15->Lava != 0)
							{
								float num111 = 0.55f;
								num111 += (float)(270 - UI.MouseTextBrightness) / 900f;
								if (ptr16->X < num111)
								{
									ptr16->X = num111;
								}
								if (ptr16->Y < num111)
								{
									ptr16->Y = num111 * 0.6f;
								}
								if (ptr16->Z < num111)
								{
									ptr16->Z = num111 * 0.2f;
								}
							}
							else if (ptr15->Liquid > 128)
							{
								StopAndWetLight[num34 * ColourH + num35] |= 2;
							}
						}
						if (ptr16->X > 0f || ptr16->Y > 0f || ptr16->Z > 0f)
						{
							if (MinimumX > num34)
							{
								MinimumX = num34;
							}
							if (MaximumX < num34 + 1)
							{
								MaximumX = num34 + 1;
							}
							if (MinimumY > num35)
							{
								MinimumY = num35;
							}
							if (MaximumY < num35 + 1)
							{
								MaximumY = num35 + 1;
							}
						}
					}
					ptr15++;
				}
			}
		}
		if (CurrentView.EvilTiles < 0)
		{
			CurrentView.EvilTiles = 0;
		}
		int holyTiles = CurrentView.HolyTiles;
		CurrentView.HolyTiles -= CurrentView.EvilTiles;
		CurrentView.EvilTiles -= holyTiles;
		if (CurrentView.HolyTiles < 0)
		{
			CurrentView.HolyTiles = 0;
		}
		if (CurrentView.EvilTiles < 0)
		{
			CurrentView.EvilTiles = 0;
		}
		MinimumX7 = MinimumX;
		MaximumX7 = MaximumX;
		MinimumY7 = MinimumY;
		MaximumY7 = MaximumY;
		MinimumX += FirstToLightX;
		MaximumX += FirstToLightX;
		MinimumY += FirstToLightY;
		MaximumY += FirstToLightY;
		FirstTileX7 = FirstTileX - FirstToLightX;
		LastTileX7 = LastTileX - FirstToLightX;
		LastTileY7 = LastTileY - FirstToLightY;
		FirstTileY7 = FirstTileY - FirstToLightY;
		LastToLightY7 = num2 - FirstToLightY;
		FirstToLightX27 = num17 - FirstToLightX;
		LastToLightX27 = num19 - FirstToLightX;
		FirstToLightY27 = num18 - FirstToLightY;
		LastToLightY27 = num20 - FirstToLightY;
		WorkerThreadGo.Set();
		ScreenX = (short)(CurrentView.ScreenPosition.X >> 4);
		ScreenY = (short)(CurrentView.ScreenPosition.Y >> 4);
	}

	private unsafe void DoColors()
	{
		fixed (Vector3* colour = Colour2)
		{
			int num = MinimumX7 * ColourH;
			for (int i = MinimumX7; i < MaximumX7; i++)
			{
				LightColorR = 0f;
				LightColorG = 0f;
				LightColorB = 0f;
				for (int j = MinimumY7; j < LastToLightY27 + 4; j++)
				{
					int num2 = num + j;
					LightColor(colour + num2, num2, 1);
				}
				LightColorR = 0f;
				LightColorG = 0f;
				LightColorB = 0f;
				for (int num3 = MaximumY7; num3 >= FirstTileY7 - 4; num3--)
				{
					int num4 = num + num3;
					LightColor(colour + num4, num4, -1);
				}
				num += ColourH;
			}
		}
		fixed (Vector3* colour2 = Colour2)
		{
			for (int k = 0; k < LastToLightY7; k++)
			{
				LightColorR = 0f;
				LightColorG = 0f;
				LightColorB = 0f;
				int num5 = k + MinimumX7 * ColourH;
				for (int l = MinimumX7; l < LastTileX7 + 4; l++)
				{
					LightColor(colour2 + num5, num5, ColourH);
					num5 += ColourH;
				}
				LightColorR = 0f;
				LightColorG = 0f;
				LightColorB = 0f;
				num5 = k + MaximumX7 * ColourH;
				for (int num6 = MaximumX7; num6 >= FirstTileX7 - 4; num6--)
				{
					LightColor(colour2 + num5, num5, -ColourH);
					num5 -= ColourH;
				}
			}
		}
		fixed (Vector3* colour3 = Colour2)
		{
			int num7 = FirstToLightX27 * ColourH;
			for (int m = FirstToLightX27; m < LastToLightX27; m++)
			{
				LightColorR = 0f;
				LightColorG = 0f;
				LightColorB = 0f;
				for (int n = FirstToLightY27; n < LastTileY7 + 4; n++)
				{
					int num8 = n + num7;
					LightColor(colour3 + num8, num8, 1);
				}
				LightColorR = 0f;
				LightColorG = 0f;
				LightColorB = 0f;
				for (int num9 = LastToLightY27; num9 >= FirstTileY7 - 4; num9--)
				{
					int num10 = num9 + num7;
					LightColor(colour3 + num10, num10, -1);
				}
				num7 += ColourH;
			}
		}
		fixed (Vector3* colour4 = Colour2)
		{
			for (int num11 = FirstToLightY27; num11 < LastToLightY27; num11++)
			{
				LightColorR = 0f;
				LightColorG = 0f;
				LightColorB = 0f;
				int num12 = num11 + FirstToLightX27 * ColourH;
				for (int num13 = FirstToLightX27; num13 < LastTileX7 + 4; num13++)
				{
					LightColor(colour4 + num12, num12, ColourH);
					num12 += ColourH;
				}
				LightColorR = 0f;
				LightColorG = 0f;
				LightColorB = 0f;
				num12 = num11 + LastToLightX27 * ColourH;
				for (int num14 = LastToLightX27; num14 >= FirstTileX7 - 4; num14--)
				{
					LightColor(colour4 + num12, num12, -ColourH);
					num12 -= ColourH;
				}
			}
		}
	}

	public unsafe static void AddLight(int LightX, int LightY, Vector3 RGB)
	{
		if (TempLightCount == 1024 || !WorldView.AnyViewContains(LightX << 4, LightY << 4))
		{
			return;
		}
		fixed (TempLight* tempLightSet = TempLightSet)
		{
			int num = TempLightCount - 1;
			TempLight* ptr = tempLightSet + num;
			while (num >= 0)
			{
				if (ptr->X == LightX && ptr->Y == LightY)
				{
					if (ptr->ColorRGB.X < RGB.X)
					{
						ptr->ColorRGB.X = RGB.X;
					}
					if (ptr->ColorRGB.Y < RGB.Y)
					{
						ptr->ColorRGB.Y = RGB.Y;
					}
					if (ptr->ColorRGB.Z < RGB.Z)
					{
						ptr->ColorRGB.Z = RGB.Z;
					}
					return;
				}
				ptr--;
				num--;
			}
			ptr = tempLightSet + TempLightCount++;
			ptr->X = (short)LightX;
			ptr->Y = (short)LightY;
			ptr->ColorRGB = RGB;
		}
	}

	private unsafe void LightColor(Vector3* RGB, int NoLight, int LightIdx)
	{
		NoLight = StopAndWetLight[NoLight];
		float num = LightColorR;
		if (RGB->X > num)
		{
			num = RGB->X;
		}
		if (num > 0.0185f)
		{
			RGB->X = num;
			if (RGB[LightIdx].X <= num)
			{
				if (NoLight != 0)
				{
					num = (((NoLight & 1) == 0) ? (num * (WetLightR * (float)Main.Rand.Next(98, 100) * 0.01f)) : (num * NegLight2));
				}
				else
				{
					num *= NegLight;
				}
			}
			LightColorR = num;
		}
		num = LightColorG;
		if (RGB->Y > num)
		{
			num = RGB->Y;
		}
		if (num > 0.0185f)
		{
			RGB->Y = num;
			if (RGB[LightIdx].Y <= num)
			{
				if (NoLight != 0)
				{
					num = (((NoLight & 1) == 0) ? (num * (WetLightG * (float)Main.Rand.Next(97, 100) * 0.01f)) : (num * NegLight2));
				}
				else
				{
					num *= NegLight;
				}
			}
			LightColorG = num;
		}
		num = LightColorB;
		if (RGB->Z > num)
		{
			num = RGB->Z;
		}
		if (num > 0.0185f)
		{
			RGB->Z = num;
			if (RGB[LightIdx].Z < num)
			{
				num = (((NoLight & 1) == 0) ? (num * NegLight) : (num * NegLight2));
			}
			LightColorB = num;
		}
	}

	public Color GetColorPlayer(int X, int Y, Color OldColor)
	{
		int num = X - FirstToLightX;
		int num2 = Y - FirstToLightY;
		if (num < 0 || num2 < 0 || num >= MaxLightArrayX || num2 >= MaxLightArrayY)
		{
			return Color.Black;
		}
		float num3 = Colour[num, num2].X * 2.5f;
		if (num3 > 1f)
		{
			num3 = 1f;
		}
		int r = (int)((float)(int)OldColor.R * num3 * BrightnessLevel);
		num3 = Colour[num, num2].Y * 2.5f;
		if (num3 > 1f)
		{
			num3 = 1f;
		}
		int g = (int)((float)(int)OldColor.G * num3 * BrightnessLevel);
		num3 = Colour[num, num2].Z * 2.5f;
		if (num3 > 1f)
		{
			num3 = 1f;
		}
		int b = (int)((float)(int)OldColor.B * num3 * BrightnessLevel);
		return new Color(r, g, b, 255);
	}

	public Color GetColorPlayer(int X, int Y)
	{
		int num = X - FirstToLightX;
		int num2 = Y - FirstToLightY;
		if (num < 0 || num2 < 0 || num >= MaxLightArrayX || num2 >= MaxLightArrayY)
		{
			return Color.Black;
		}
		return new Color(Colour[num, num2] * (BrightnessLevel * 2.5f));
	}

	public Color GetColorUnsafe(int X, int Y)
	{
		return new Color(Colour[X - FirstToLightX, Y - FirstToLightY] * BrightnessLevel);
	}

	public Color GetColor(int X, int Y)
	{
		int num = X - FirstToLightX;
		int num2 = Y - FirstToLightY;
		if (num < 0 || num2 < 0 || num >= MaxLightArrayX || num2 >= MaxLightArrayY)
		{
			return Color.Black;
		}
		return new Color(Colour[num, num2] * BrightnessLevel);
	}

	public unsafe float Brightness(int X, int Y)
	{
		int num = X - FirstToLightX;
		int num2 = Y - FirstToLightY;
		if (num < 0 || num2 < 0 || num >= MaxLightArrayX || num2 >= MaxLightArrayY)
		{
			return 0f;
		}
		fixed (Vector3* ptr = &Colour[num, num2])
		{
			return (ptr->X + ptr->Y + ptr->Z) * (1f / 3f);
		}
	}

	public unsafe float BrightnessUnsafe(int X, int Y)
	{
		fixed (Vector3* ptr = &Colour[X - FirstToLightX, Y - FirstToLightY])
		{
			return (ptr->X + ptr->Y + ptr->Z) * (1f / 3f);
		}
	}

	public unsafe bool IsNotBlackUnsafe(int X, int Y)
	{
		fixed (Vector3* ptr = &Colour[X - FirstToLightX, Y - FirstToLightY])
		{
			if (!(ptr->X > 0f) && !(ptr->Y > 0f))
			{
				return ptr->Z > 0f;
			}
			return true;
		}
	}

	public unsafe bool Brighter(int X, int Y, int X2, int Y2)
	{
		int num = X - FirstToLightX;
		int num2 = Y - FirstToLightY;
		if (num < 0 || num2 < 0 || num >= MaxLightArrayX || num2 >= MaxLightArrayY)
		{
			return false;
		}
		int num3 = X2 - FirstToLightX;
		int num4 = Y2 - FirstToLightY;
		if (num3 < 0 || num4 < 0 || num3 >= MaxLightArrayX || num4 >= MaxLightArrayY)
		{
			return true;
		}
		fixed (Vector3* ptr = &Colour[num, num2])
		{
			fixed (Vector3* ptr2 = &Colour2[num3, num4])
			{
				return ptr->X + ptr->Y + ptr->Z >= ptr2->X + ptr2->Y + ptr2->Z;
			}
		}
	}
}
