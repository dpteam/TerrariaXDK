namespace Terraria;

public struct Liquid
{
	public const int MaxNumLiquids = 4096;

	public static int SkipCount = 0;

	public static int StuckCount = 0;

	public static int StuckAmount = 0;

	private static int Cycles = 7;

	public static int NumLiquids;

	public static bool IsStuck = false;

	public static bool QuickFall = false;

	private static bool QuickSettle = false;

	private static int WetCounter;

	public static int PanicCounter = 0;

	public static bool PanicMode = false;

	public static int PanicY = 0;

	public short X;

	public short Y;

	public short Kill;

	public short Delay;

	public void Init(int NewX, int NewY)
	{
		X = (short)NewX;
		Y = (short)NewY;
		Kill = 0;
		Delay = 0;
	}

	public static void QuickSettleOn()
	{
		QuickSettle = true;
		Cycles = 1;
	}

	public static void QuickSettleOff()
	{
		QuickSettle = false;
		Cycles = 14;
	}

	public static void QuickWater(double ProgressionRate, int MinY = 3, int MaxY = -1, double StartPercent = 0.0)
	{
		if (MaxY == -1)
		{
			MaxY = Main.MaxTilesY - 3;
		}
		for (int num = MaxY; num >= MinY; num--)
		{
			UI.MainUI.Progress = (float)((double)(MaxY - num) / (double)(MaxY - MinY + 1) * ProgressionRate + StartPercent);
			for (int i = 0; i < 2; i++)
			{
				int num2 = 2;
				int num3 = Main.MaxTilesX - 2;
				int num4 = 1;
				if (i == 1)
				{
					num2 = num3;
					num3 = 2;
					num4 = -1;
				}
				for (int j = num2; j != num3; j += num4)
				{
					int num5 = Main.TileSet[j, num].Liquid;
					if (num5 <= 0)
					{
						continue;
					}
					Main.TileSet[j, num].Liquid = 0;
					int num6 = -num4;
					bool flag = false;
					int num7 = j;
					int num8 = num;
					int num9 = Main.TileSet[j, num].Lava;
					bool flag2 = true;
					int num10 = 0;
					while (flag2 && num7 > 3 && num7 < Main.MaxTilesX - 3 && num8 < Main.MaxTilesY - 3)
					{
						flag2 = false;
						while (Main.TileSet[num7, num8 + 1].Liquid == 0 && num8 < Main.MaxTilesY - 5 && (Main.TileSet[num7, num8 + 1].IsActive == 0 || !Main.TileSolidNotSolidTop[Main.TileSet[num7, num8 + 1].Type]))
						{
							flag2 = true;
							flag = true;
							num6 = num4;
							num10 = 0;
							num8++;
							if (num8 > WorldGen.WaterLine)
							{
								num9 = 32;
							}
						}
						if (Main.TileSet[num7, num8 + 1].Liquid > 0 && Main.TileSet[num7, num8 + 1].Liquid < byte.MaxValue && Main.TileSet[num7, num8 + 1].Lava == num9)
						{
							int num11 = 255 - Main.TileSet[num7, num8 + 1].Liquid;
							if (num11 > num5)
							{
								num11 = num5;
							}
							Main.TileSet[num7, num8 + 1].Liquid += (byte)num11;
							num5 -= (byte)num11;
							if (num5 <= 0)
							{
								break;
							}
						}
						if (num10 == 0)
						{
							if (Main.TileSet[num7 + num6, num8].Liquid == 0 && (Main.TileSet[num7 + num6, num8].IsActive == 0 || !Main.TileSolidNotSolidTop[Main.TileSet[num7 + num6, num8].Type]))
							{
								num10 = num6;
							}
							else if (Main.TileSet[num7 - num6, num8].Liquid == 0 && (Main.TileSet[num7 - num6, num8].IsActive == 0 || !Main.TileSolidNotSolidTop[Main.TileSet[num7 - num6, num8].Type]))
							{
								num10 = -num6;
							}
						}
						if (num10 != 0 && Main.TileSet[num7 + num10, num8].Liquid == 0 && (Main.TileSet[num7 + num10, num8].IsActive == 0 || !Main.TileSolidNotSolidTop[Main.TileSet[num7 + num10, num8].Type]))
						{
							flag2 = true;
							num7 += num10;
						}
						if (flag && !flag2)
						{
							flag = false;
							flag2 = true;
							num6 = -num4;
							num10 = 0;
						}
					}
					Main.TileSet[num7, num8].Liquid = (byte)num5;
					Main.TileSet[num7, num8].Lava = (byte)num9;
					if (Main.TileSet[num7 - 1, num8].Liquid > 0 && Main.TileSet[num7 - 1, num8].Lava != num9)
					{
						if (num9 != 0)
						{
							LavaCheck(num7, num8);
						}
						else
						{
							LavaCheck(num7 - 1, num8);
						}
					}
					else if (Main.TileSet[num7 + 1, num8].Liquid > 0 && Main.TileSet[num7 + 1, num8].Lava != num9)
					{
						if (num9 != 0)
						{
							LavaCheck(num7, num8);
						}
						else
						{
							LavaCheck(num7 + 1, num8);
						}
					}
					else if (Main.TileSet[num7, num8 - 1].Liquid > 0 && Main.TileSet[num7, num8 - 1].Lava != num9)
					{
						if (num9 != 0)
						{
							LavaCheck(num7, num8);
						}
						else
						{
							LavaCheck(num7, num8 - 1);
						}
					}
					else if (Main.TileSet[num7, num8 + 1].Liquid > 0 && Main.TileSet[num7, num8 + 1].Lava != num9)
					{
						if (num9 != 0)
						{
							LavaCheck(num7, num8);
						}
						else
						{
							LavaCheck(num7, num8 + 1);
						}
					}
				}
			}
		}
	}

	public unsafe void Update()
	{
		fixed (Tile* ptr = &Main.TileSet[X, Y])
		{
			if (ptr->IsActive != 0 && Main.TileSolidNotSolidTop[ptr->Type])
			{
				Kill = 9;
				return;
			}
			int num = ptr->Liquid;
			int num2 = num;
			int num3 = 0;
			if (Y > Main.MaxTilesY - 200 && num > 0 && ptr->Lava == 0)
			{
				int num4 = 2;
				if (num < num4)
				{
					num4 = num;
				}
				num -= num4;
				ptr->Liquid = (byte)num;
			}
			if (num == 0)
			{
				Kill = 9;
				return;
			}
			if (ptr->Lava != 0)
			{
				LavaCheck(X, Y);
				if (!QuickFall)
				{
					if (Delay < 5)
					{
						Delay++;
						return;
					}
					Delay = 0;
				}
			}
			else
			{
				if (ptr[-1440].Lava != 0)
				{
					AddWater(X - 1, Y);
				}
				if (ptr[1440].Lava != 0)
				{
					AddWater(X + 1, Y);
				}
				if (ptr[-1].Lava != 0)
				{
					AddWater(X, Y - 1);
				}
				if (ptr[1].Lava != 0)
				{
					AddWater(X, Y + 1);
				}
			}
			if (ptr[1].IsActive == 0 || !Main.TileSolidNotSolidTop[ptr[1].Type])
			{
				num = ptr[1].Liquid;
				if ((num <= 0 || ptr[1].Lava == ptr->Lava) && num < 255)
				{
					num3 = 255 - num;
					if (num3 > ptr->Liquid)
					{
						num3 = ptr->Liquid;
					}
					byte* ptr2 = &ptr->Liquid;
					*ptr2 -= (byte)num3;
					num += num3;
					ptr[1].Liquid = (byte)num;
					ptr[1].Lava = ptr->Lava;
					AddWater(X, Y + 1);
					ptr[1].SkipLiquid = 128;
					ptr->SkipLiquid = 128;
					if (ptr->Liquid > 250)
					{
						ptr->Liquid = byte.MaxValue;
					}
					else
					{
						AddWater(X - 1, Y);
						AddWater(X + 1, Y);
					}
				}
			}
			if (ptr->Liquid > 0)
			{
				bool flag = true;
				bool flag2 = true;
				bool flag3 = true;
				bool flag4 = true;
				if (ptr[-1440].IsActive != 0 && Main.TileSolidNotSolidTop[ptr[-1440].Type])
				{
					flag = false;
				}
				else if (ptr[-1440].Liquid > 0 && ptr[-1440].Lava != ptr->Lava)
				{
					flag = false;
				}
				else if (ptr[-2880].IsActive != 0 && Main.TileSolidNotSolidTop[ptr[-2880].Type])
				{
					flag3 = false;
				}
				else if (ptr[-2880].Liquid == 0)
				{
					flag3 = false;
				}
				else if (ptr[-2880].Liquid > 0 && ptr[-2880].Lava != ptr->Lava)
				{
					flag3 = false;
				}
				if (ptr[1440].IsActive != 0 && Main.TileSolidNotSolidTop[ptr[1440].Type])
				{
					flag2 = false;
				}
				else if (ptr[1440].Liquid > 0 && ptr[1440].Lava != ptr->Lava)
				{
					flag2 = false;
				}
				else if (ptr[2880].IsActive != 0 && Main.TileSolidNotSolidTop[ptr[2880].Type])
				{
					flag4 = false;
				}
				else if (ptr[2880].Liquid == 0)
				{
					flag4 = false;
				}
				else if (ptr[2880].Liquid > 0 && ptr[2880].Lava != ptr->Lava)
				{
					flag4 = false;
				}
				int num5 = 0;
				if (ptr->Liquid < 3)
				{
					num5 = -1;
				}
				if (flag & flag2)
				{
					if (flag3 & flag4)
					{
						bool flag5 = true;
						bool flag6 = true;
						if (ptr[-4320].IsActive != 0 && Main.TileSolidNotSolidTop[ptr[-4320].Type])
						{
							flag5 = false;
						}
						else if (ptr[-4320].Liquid == 0)
						{
							flag5 = false;
						}
						else if (ptr[-4320].Lava != ptr->Lava)
						{
							flag5 = false;
						}
						if (ptr[4320].IsActive != 0 && Main.TileSolidNotSolidTop[ptr[4320].Type])
						{
							flag6 = false;
						}
						else if (ptr[4320].Liquid == 0)
						{
							flag6 = false;
						}
						else if (ptr[4320].Lava != ptr->Lava)
						{
							flag6 = false;
						}
						if (flag5 & flag6)
						{
							num3 = ptr[-1440].Liquid + ptr[1440].Liquid + ptr[-2880].Liquid + ptr[2880].Liquid + ptr[-4320].Liquid + ptr[4320].Liquid + ptr->Liquid + num5;
							num3 = (num3 + 3) / 7;
							int num6 = 0;
							ptr[-1440].Lava = ptr->Lava;
							if (ptr[-1440].Liquid != num3)
							{
								AddWater(X - 1, Y);
								ptr[-1440].Liquid = (byte)num3;
							}
							else
							{
								num6++;
							}
							ptr[1440].Lava = ptr->Lava;
							if (ptr[1440].Liquid != num3)
							{
								AddWater(X + 1, Y);
								ptr[1440].Liquid = (byte)num3;
							}
							else
							{
								num6++;
							}
							ptr[-2880].Lava = ptr->Lava;
							if (ptr[-2880].Liquid != num3)
							{
								AddWater(X - 2, Y);
								ptr[-2880].Liquid = (byte)num3;
							}
							else
							{
								num6++;
							}
							ptr[2880].Lava = ptr->Lava;
							if (ptr[2880].Liquid != num3)
							{
								AddWater(X + 2, Y);
								ptr[2880].Liquid = (byte)num3;
							}
							else
							{
								num6++;
							}
							ptr[-4320].Lava = ptr->Lava;
							if (ptr[-4320].Liquid != num3)
							{
								AddWater(X - 3, Y);
								ptr[-4320].Liquid = (byte)num3;
							}
							else
							{
								num6++;
							}
							ptr[4320].Lava = ptr->Lava;
							if (ptr[4320].Liquid != num3)
							{
								AddWater(X + 3, Y);
								ptr[4320].Liquid = (byte)num3;
							}
							else
							{
								num6++;
							}
							if (ptr->Liquid != num3 || ptr[-1440].Liquid != num3)
							{
								AddWater(X - 1, Y);
							}
							if (ptr->Liquid != num3 || ptr[1440].Liquid != num3)
							{
								AddWater(X + 1, Y);
							}
							if (ptr->Liquid != num3 || ptr[-2880].Liquid != num3)
							{
								AddWater(X - 2, Y);
							}
							if (ptr->Liquid != num3 || ptr[2880].Liquid != num3)
							{
								AddWater(X + 2, Y);
							}
							if (ptr->Liquid != num3 || ptr[-4320].Liquid != num3)
							{
								AddWater(X - 3, Y);
							}
							if (ptr->Liquid != num3 || ptr[4320].Liquid != num3)
							{
								AddWater(X + 3, Y);
							}
							if (num6 != 6 || ptr[-1].Liquid <= 0)
							{
								ptr->Liquid = (byte)num3;
							}
						}
						else
						{
							int num7 = 0;
							num3 = ptr[-1440].Liquid + ptr[1440].Liquid + ptr[-2880].Liquid + ptr[2880].Liquid + ptr->Liquid + num5;
							num3 = (num3 + 2) / 5;
							ptr[-1440].Lava = ptr->Lava;
							if (ptr[-1440].Liquid != num3)
							{
								AddWater(X - 1, Y);
								ptr[-1440].Liquid = (byte)num3;
							}
							else
							{
								num7++;
							}
							ptr[1440].Lava = ptr->Lava;
							if (ptr[1440].Liquid != num3)
							{
								AddWater(X + 1, Y);
								ptr[1440].Liquid = (byte)num3;
							}
							else
							{
								num7++;
							}
							ptr[-2880].Lava = ptr->Lava;
							if (ptr[-2880].Liquid != num3)
							{
								AddWater(X - 2, Y);
								ptr[-2880].Liquid = (byte)num3;
							}
							else
							{
								num7++;
							}
							ptr[2880].Lava = ptr->Lava;
							if (ptr[2880].Liquid != num3)
							{
								AddWater(X + 2, Y);
								ptr[2880].Liquid = (byte)num3;
							}
							else
							{
								num7++;
							}
							if (ptr[-1440].Liquid != num3 || ptr->Liquid != num3)
							{
								AddWater(X - 1, Y);
							}
							if (ptr[1440].Liquid != num3 || ptr->Liquid != num3)
							{
								AddWater(X + 1, Y);
							}
							if (ptr[-2880].Liquid != num3 || ptr->Liquid != num3)
							{
								AddWater(X - 2, Y);
							}
							if (ptr[2880].Liquid != num3 || ptr->Liquid != num3)
							{
								AddWater(X + 2, Y);
							}
							if (num7 != 4 || ptr[-1].Liquid <= 0)
							{
								ptr->Liquid = (byte)num3;
							}
						}
					}
					else if (flag3)
					{
						num3 = ptr[-1440].Liquid + ptr[1440].Liquid + ptr[-2880].Liquid + ptr->Liquid + num5;
						num3 = num3 + 2 >> 2;
						ptr[-1440].Lava = ptr->Lava;
						if (ptr[-1440].Liquid != num3 || ptr->Liquid != num3)
						{
							AddWater(X - 1, Y);
							ptr[-1440].Liquid = (byte)num3;
						}
						ptr[1440].Lava = ptr->Lava;
						if (ptr[1440].Liquid != num3 || ptr->Liquid != num3)
						{
							AddWater(X + 1, Y);
							ptr[1440].Liquid = (byte)num3;
						}
						ptr[-2880].Lava = ptr->Lava;
						if (ptr[-2880].Liquid != num3 || ptr->Liquid != num3)
						{
							ptr[-2880].Liquid = (byte)num3;
							AddWater(X - 2, Y);
						}
						ptr->Liquid = (byte)num3;
					}
					else if (flag4)
					{
						num3 = ptr[-1440].Liquid + ptr[1440].Liquid + ptr[2880].Liquid + ptr->Liquid + num5;
						num3 = num3 + 2 >> 2;
						ptr[-1440].Lava = ptr->Lava;
						if (ptr[-1440].Liquid != num3 || ptr->Liquid != num3)
						{
							AddWater(X - 1, Y);
							ptr[-1440].Liquid = (byte)num3;
						}
						ptr[1440].Lava = ptr->Lava;
						if (ptr[1440].Liquid != num3 || ptr->Liquid != num3)
						{
							AddWater(X + 1, Y);
							ptr[1440].Liquid = (byte)num3;
						}
						ptr[2880].Lava = ptr->Lava;
						if (ptr[2880].Liquid != num3 || ptr->Liquid != num3)
						{
							ptr[2880].Liquid = (byte)num3;
							AddWater(X + 2, Y);
						}
						ptr->Liquid = (byte)num3;
					}
					else
					{
						num3 = ptr[-1440].Liquid + ptr[1440].Liquid + ptr->Liquid + num5;
						num3 = (num3 + 1) / 3;
						ptr[-1440].Lava = ptr->Lava;
						if (ptr[-1440].Liquid != num3)
						{
							ptr[-1440].Liquid = (byte)num3;
						}
						if (ptr->Liquid != num3 || ptr[-1440].Liquid != num3)
						{
							AddWater(X - 1, Y);
						}
						ptr[1440].Lava = ptr->Lava;
						if (ptr[1440].Liquid != num3)
						{
							ptr[1440].Liquid = (byte)num3;
						}
						if (ptr->Liquid != num3 || ptr[1440].Liquid != num3)
						{
							AddWater(X + 1, Y);
						}
						ptr->Liquid = (byte)num3;
					}
				}
				else if (flag)
				{
					num3 = ptr[-1440].Liquid + ptr->Liquid + num5;
					num3 = num3 + 1 >> 1;
					if (ptr[-1440].Liquid != num3)
					{
						ptr[-1440].Liquid = (byte)num3;
					}
					ptr[-1440].Lava = ptr->Lava;
					if (ptr->Liquid != num3 || ptr[-1440].Liquid != num3)
					{
						AddWater(X - 1, Y);
					}
					ptr->Liquid = (byte)num3;
				}
				else if (flag2)
				{
					num3 = ptr[1440].Liquid + ptr->Liquid + num5;
					num3 = num3 + 1 >> 1;
					if (ptr[1440].Liquid != num3)
					{
						ptr[1440].Liquid = (byte)num3;
					}
					ptr[1440].Lava = ptr->Lava;
					if (ptr->Liquid != num3 || ptr[1440].Liquid != num3)
					{
						AddWater(X + 1, Y);
					}
					ptr->Liquid = (byte)num3;
				}
			}
			if (ptr->Liquid != num2)
			{
				if (ptr->Liquid == 254 && num2 == 255)
				{
					ptr->Liquid = byte.MaxValue;
					Kill++;
				}
				else
				{
					AddWater(X, Y - 1);
					Kill = 0;
				}
			}
			else
			{
				Kill++;
			}
		}
	}

	public static void StartPanic()
	{
		if (!PanicMode)
		{
			WorldGen.WaterLine = Main.MaxTilesY;
			NumLiquids = 0;
			LiquidBuffer.NumLiquidBuffer = 0;
			PanicCounter = 0;
			PanicMode = true;
			PanicY = Main.MaxTilesY - 3;
		}
	}

	public static void UpdateLiquid()
	{
		if (!WorldGen.Gen)
		{
			if (!PanicMode)
			{
				if (NumLiquids + LiquidBuffer.NumLiquidBuffer > 4000)
				{
					PanicCounter++;
					if (PanicCounter > 1800 || NumLiquids + LiquidBuffer.NumLiquidBuffer > 13500)
					{
						StartPanic();
					}
				}
				else
				{
					PanicCounter = 0;
				}
			}
			if (PanicMode)
			{
				int num = 0;
				while (PanicY >= 3 && num < 5)
				{
					num++;
					QuickWater(0.0, PanicY, PanicY);
					PanicY--;
					if (PanicY < 3)
					{
						PanicCounter = 0;
						PanicMode = false;
						WorldGen.WaterCheck();
						if (Main.NetMode == 2)
						{
							Netplay.ResetSections();
						}
					}
				}
				return;
			}
		}
		QuickFall = QuickSettle || NumLiquids > 2000;
		int num2 = 4096 / Cycles;
		int num3 = num2 * WetCounter;
		int num4 = num2 * ++WetCounter;
		if (WetCounter == Cycles)
		{
			num4 = NumLiquids;
		}
		if (num4 > NumLiquids)
		{
			num4 = NumLiquids;
			WetCounter = Cycles;
		}
		if (QuickFall)
		{
			for (int i = num3; i < num4; i++)
			{
				Main.Liquid[i].Delay = 10;
				Main.Liquid[i].Update();
				Main.TileSet[Main.Liquid[i].X, Main.Liquid[i].Y].SkipLiquid = 0;
			}
		}
		else
		{
			for (int j = num3; j < num4; j++)
			{
				if (Main.TileSet[Main.Liquid[j].X, Main.Liquid[j].Y].SkipLiquid == 0)
				{
					Main.Liquid[j].Update();
				}
				else
				{
					Main.TileSet[Main.Liquid[j].X, Main.Liquid[j].Y].SkipLiquid = 0;
				}
			}
		}
		if (WetCounter < Cycles)
		{
			return;
		}
		WetCounter = 0;
		for (int num5 = NumLiquids - 1; num5 >= 0; num5--)
		{
			if (Main.Liquid[num5].Kill > 3)
			{
				DelWater(num5);
			}
		}
		int num6 = 4096 - (4096 - NumLiquids);
		int numLiquidBuffer = LiquidBuffer.NumLiquidBuffer;
		if (num6 > numLiquidBuffer)
		{
			num6 = numLiquidBuffer;
		}
		numLiquidBuffer = (LiquidBuffer.NumLiquidBuffer = numLiquidBuffer - num6);
		while (--num6 >= 0)
		{
			Main.TileSet[Main.LiquidBuffer[numLiquidBuffer].X, Main.LiquidBuffer[numLiquidBuffer].Y].CheckingLiquid = 0;
			AddWater(Main.LiquidBuffer[numLiquidBuffer].X, Main.LiquidBuffer[numLiquidBuffer].Y);
			numLiquidBuffer++;
		}
		if (NumLiquids > 0 && NumLiquids > StuckAmount - 50 && NumLiquids < StuckAmount + 50)
		{
			if (++StuckCount >= 10000)
			{
				IsStuck = true;
				for (int num7 = NumLiquids - 1; num7 >= 0; num7--)
				{
					DelWater(num7);
				}
				IsStuck = false;
				StuckCount = 0;
			}
		}
		else
		{
			StuckCount = 0;
			StuckAmount = NumLiquids;
		}
	}

	public unsafe static void AddWater(int WaterX, int WaterY)
	{
		if (WaterX < 5 || WaterY < 5 || WaterX >= Main.MaxTilesX - 5 || WaterY >= Main.MaxTilesY - 5)
		{
			return;
		}
		fixed (Tile* ptr = &Main.TileSet[WaterX, WaterY])
		{
			if (ptr->Liquid == 0 || ptr->CheckingLiquid != 0)
			{
				return;
			}
			if (NumLiquids >= 4095)
			{
				LiquidBuffer.AddBuffer(WaterX, WaterY);
				return;
			}
			ptr->CheckingLiquid = 64;
			Main.Liquid[NumLiquids].Init(WaterX, WaterY);
			ptr->SkipLiquid = 0;
			NumLiquids++;
			if (Main.NetMode == 2 && NumLiquids < 1365)
			{
				NetMessage.SendWater(WaterX, WaterY);
			}
			if (ptr->IsActive == 0)
			{
				return;
			}
			int type = ptr->Type;
			if ((type != 4 || ptr->FrameY != 176) && (Main.TileWaterDeath[type] || (ptr->Lava != 0 && Main.TileLavaDeath[type])))
			{
				if (WorldGen.Gen)
				{
					ptr->IsActive = 0;
				}
				else if (WorldGen.KillTile(WaterX, WaterY) && Main.NetMode == 2)
				{
					NetMessage.CreateMessage5(17, 0, WaterX, WaterY, 0);
					NetMessage.SendMessage();
				}
			}
		}
	}

	public static void LavaCheck(int TileX, int TileY)
	{
		int liquid = Main.TileSet[TileX, TileY - 1].Liquid;
		bool flag = Main.TileSet[TileX, TileY - 1].Lava == 0;
		int liquid2 = Main.TileSet[TileX - 1, TileY].Liquid;
		bool flag2 = Main.TileSet[TileX - 1, TileY].Lava == 0;
		int liquid3 = Main.TileSet[TileX + 1, TileY].Liquid;
		bool flag3 = Main.TileSet[TileX + 1, TileY].Lava == 0;
		if (((liquid2 > 0) & flag2) || ((liquid3 > 0) & flag3) || ((liquid > 0) & flag))
		{
			int num = 0;
			if (flag2)
			{
				num = liquid2;
				Main.TileSet[TileX - 1, TileY].Liquid = 0;
			}
			if (flag3)
			{
				num += liquid3;
				Main.TileSet[TileX + 1, TileY].Liquid = 0;
			}
			if (flag)
			{
				num += liquid;
				Main.TileSet[TileX, TileY - 1].Liquid = 0;
			}
			if (num >= 32 && Main.TileSet[TileX, TileY].IsActive == 0)
			{
				Main.TileSet[TileX, TileY].Liquid = 0;
				Main.TileSet[TileX, TileY].Lava = 0;
				WorldGen.PlaceTile(TileX, TileY, 56, ToMute: true, IsForced: true);
				WorldGen.SquareTileFrame(TileX, TileY);
				if (Main.NetMode == 2)
				{
					NetMessage.SendTileSquare(TileX - 1, TileY - 1, 3);
				}
			}
		}
		else if (Main.TileSet[TileX, TileY + 1].IsActive == 0 && Main.TileSet[TileX, TileY + 1].Liquid > 0 && Main.TileSet[TileX, TileY + 1].Lava == 0)
		{
			Main.TileSet[TileX, TileY].Liquid = 0;
			Main.TileSet[TileX, TileY].Lava = 0;
			Main.TileSet[TileX, TileY + 1].Liquid = 0;
			WorldGen.PlaceTile(TileX, TileY + 1, 56, ToMute: true, IsForced: true);
			WorldGen.SquareTileFrame(TileX, TileY + 1);
			if (Main.NetMode == 2)
			{
				NetMessage.SendTileSquare(TileX - 1, TileY, 3);
			}
		}
	}

	public unsafe static void DelWater(int LiquidIdx)
	{
		int x = Main.Liquid[LiquidIdx].X;
		int y = Main.Liquid[LiquidIdx].Y;
		fixed (Tile* ptr = &Main.TileSet[x, y])
		{
			int num = ptr->Liquid;
			if (num < 2)
			{
				ptr->Liquid = 0;
				num = 0;
				if (ptr[-1440].Liquid == 1)
				{
					ptr[-1440].Liquid = 0;
				}
				if (ptr[1440].Liquid == 1)
				{
					ptr[1440].Liquid = 0;
				}
			}
			else if (num < 20)
			{
				if ((ptr[1].Liquid < byte.MaxValue && (ptr[1].IsActive == 0 || !Main.TileSolidNotSolidTop[ptr[1].Type])) || (ptr[-1440].Liquid < num && (ptr[-1440].IsActive == 0 || !Main.TileSolidNotSolidTop[ptr[-1440].Type])) || (ptr[1440].Liquid < num && (ptr[1440].IsActive == 0 || !Main.TileSolidNotSolidTop[ptr[1440].Type])))
				{
					ptr->Liquid = 0;
					num = 0;
				}
			}
			else if (ptr[1].Liquid < byte.MaxValue && (ptr[1].IsActive == 0 || !Main.TileSolidNotSolidTop[ptr[1].Type]) && !IsStuck)
			{
				Main.Liquid[LiquidIdx].Kill = 0;
				return;
			}
			if (num < 250 && ptr[-1].Liquid > 0)
			{
				AddWater(x, y - 1);
			}
			if (num == 0)
			{
				ptr->Lava = 0;
			}
			else
			{
				if ((ptr[1440].Liquid > 0 && ptr[1441].Liquid < 250 && ptr[1441].IsActive == 0) || (ptr[-1440].Liquid > 0 && ptr[-1439].Liquid < 250 && ptr[-1439].IsActive == 0))
				{
					AddWater(x - 1, y);
					AddWater(x + 1, y);
				}
				if (ptr->Lava != 0)
				{
					LavaCheck(x, y);
					for (int i = x - 1; i <= x + 1; i++)
					{
						for (int j = y - 1; j <= y + 1; j++)
						{
							if (Main.TileSet[i, j].IsActive == 0)
							{
								continue;
							}
							switch ((EntityID.TileID)Main.TileSet[i, j].Type)
							{
							case EntityID.TileID.GRASS:
							case EntityID.TileID.CORRUPT_GRASS:
							case EntityID.TileID.HALLOWED_GRASS:
								Main.TileSet[i, j].Type = 0;
								WorldGen.SquareTileFrame(i, j);
								if (Main.NetMode == 2)
								{
									NetMessage.SendTileSquare(x, y, 3);
								}
								break;
							case EntityID.TileID.JUNGLE_GRASS:
							case EntityID.TileID.MUSHROOM_GRASS:
								Main.TileSet[i, j].Type = 59;
								WorldGen.SquareTileFrame(i, j);
								if (Main.NetMode == 2)
								{
									NetMessage.SendTileSquare(x, y, 3);
								}
								break;
							}
						}
					}
				}
			}
			if (Main.NetMode == 2)
			{
				NetMessage.SendWater(x, y);
			}
			NumLiquids--;
			ptr->CheckingLiquid = 0;
			Main.Liquid[LiquidIdx].X = Main.Liquid[NumLiquids].X;
			Main.Liquid[LiquidIdx].Y = Main.Liquid[NumLiquids].Y;
			Main.Liquid[LiquidIdx].Kill = Main.Liquid[NumLiquids].Kill;
			if (ptr->Type >= 82 && ptr->Type <= 84)
			{
				WorldGen.CheckAlch(x, y);
			}
		}
	}
}
