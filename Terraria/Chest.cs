namespace Terraria;

public sealed class Chest
{
	public const int MaxNumItems = 20;

	public Item[] ItemSet = new Item[20];

	public short XPos;

	public short YPos;

	public Chest()
	{
	}

	public Chest(int X, int Y)
	{
		XPos = (short)X;
		YPos = (short)Y;
		for (int i = 0; i < 20; i++)
		{
			ItemSet[i].Init();
		}
	}

	public unsafe static void Unlock(int X, int Y)
	{
		Main.PlaySound(22, X * 16, Y * 16);
		for (int i = X; i <= X + 1; i++)
		{
			for (int j = Y; j <= Y + 1; j++)
			{
				if ((Main.TileSet[i, j].FrameX < 72 || Main.TileSet[i, j].FrameX > 106) && (Main.TileSet[i, j].FrameX < 144 || Main.TileSet[i, j].FrameX > 178))
				{
					continue;
				}
				Main.TileSet[i, j].FrameX -= 36;
				for (int k = 0; k < 3; k++)
				{
					if (null == Main.DustSet.NewDust(i * 16, j * 16, 16, 16, 11))
					{
						break;
					}
				}
			}
		}
	}

	public static int UsingChest(int SelectedChest)
	{
		if (Main.ChestSet[SelectedChest] != null)
		{
			for (int i = 0; i < 8; i++)
			{
				if (Main.PlayerSet[i].Active != 0 && Main.PlayerSet[i].PlayerChest == SelectedChest)
				{
					return i;
				}
			}
		}
		return -1;
	}

	public static int FindChest(int X, int Y)
	{
		for (int i = 0; i < 1000; i++)
		{
			if (Main.ChestSet[i] != null && Main.ChestSet[i].XPos == X && Main.ChestSet[i].YPos == Y)
			{
				return i;
			}
		}
		return -1;
	}

	public static int CreateChest(int X, int Y)
	{
		for (int i = 0; i < 1000; i++)
		{
			if (Main.ChestSet[i] != null && Main.ChestSet[i].XPos == X && Main.ChestSet[i].YPos == Y)
			{
				return -1;
			}
		}
		for (int j = 0; j < 1000; j++)
		{
			if (Main.ChestSet[j] == null)
			{
				Main.ChestSet[j] = new Chest(X, Y);
				return j;
			}
		}
		return -1;
	}

	public static bool DestroyChest(int X, int Y)
	{
		for (int i = 0; i < 1000; i++)
		{
			if (Main.ChestSet[i] == null || Main.ChestSet[i].XPos != X || Main.ChestSet[i].YPos != Y)
			{
				continue;
			}
			for (int j = 0; j < 20; j++)
			{
				if (Main.ChestSet[i].ItemSet[j].Type > 0 && Main.ChestSet[i].ItemSet[j].Stack > 0)
				{
					return false;
				}
			}
			Main.ChestSet[i] = null;
			break;
		}
		return true;
	}

	public void AddShop(ref Item NewItem)
	{
		for (int i = 0; i < 19; i++)
		{
			if (ItemSet[i].Type != 0)
			{
				continue;
			}
			ref Item reference = ref ItemSet[i];
			reference = NewItem;
			ItemSet[i].OnlyBuyOnce = true;
			if (ItemSet[i].Value > 0)
			{
				ItemSet[i].Value = ItemSet[i].Value / 5;
				if (ItemSet[i].Value < 1)
				{
					ItemSet[i].Value = 1;
				}
			}
			break;
		}
	}

	public static int GetShopOwnerHeadTextureId(int NPCVendor)
	{
		if (1 == 0)
		{
		}
		int result = NPCVendor switch
		{
			1 => 1257, 
			2 => 1261, 
			3 => 1260, 
			4 => 1259, 
			5 => 1262, 
			6 => 1264, 
			7 => 1265, 
			8 => 1263, 
			9 => 1266, 
			_ => -1, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public void SetupShop(int Type, Player CurrentPlayer = null)
	{
		int num = 20;
		while (num > 0)
		{
			ItemSet[--num].Init();
		}
		switch (Type)
		{
		case 1:
			ItemSet[num++].SetDefaults(88);
			ItemSet[num++].SetDefaults(87);
			ItemSet[num++].SetDefaults(35);
			ItemSet[num++].NetDefaults(-13);
			ItemSet[num++].NetDefaults(-16);
			ItemSet[num++].SetDefaults(8);
			ItemSet[num++].SetDefaults(28);
			if (CurrentPlayer != null && CurrentPlayer.statManaMax >= 200)
			{
				ItemSet[num++].SetDefaults(110);
			}
			ItemSet[num++].SetDefaults(40);
			ItemSet[num++].SetDefaults(42);
			if (Main.GameTime.IsBloodMoon)
			{
				ItemSet[num++].SetDefaults(279);
			}
			if (!Main.GameTime.DayTime)
			{
				ItemSet[num++].SetDefaults(282);
			}
			if (NPC.HasDownedBoss3)
			{
				ItemSet[num++].SetDefaults(346);
			}
			if (Main.InHardMode)
			{
				ItemSet[num++].SetDefaults(488);
			}
			break;
		case 2:
			ItemSet[num++].SetDefaults(97);
			if (Main.GameTime.IsBloodMoon || Main.InHardMode)
			{
				ItemSet[num++].SetDefaults(278);
			}
			if ((NPC.HasDownedBoss2 && !Main.GameTime.DayTime) || Main.InHardMode)
			{
				ItemSet[num++].SetDefaults(47);
			}
			ItemSet[num++].SetDefaults(95);
			ItemSet[num++].SetDefaults(98);
			if (!Main.GameTime.DayTime)
			{
				ItemSet[num++].SetDefaults(324);
			}
			if (Main.InHardMode)
			{
				ItemSet[num++].SetDefaults(534);
			}
			break;
		case 3:
			if (Main.GameTime.IsBloodMoon)
			{
				ItemSet[num++].SetDefaults(67);
				ItemSet[num++].SetDefaults(59);
			}
			else
			{
				ItemSet[num++].SetDefaults(66);
				ItemSet[num++].SetDefaults(62);
				ItemSet[num++].SetDefaults(63);
			}
			ItemSet[num++].SetDefaults(27);
			ItemSet[num++].SetDefaults(114);
			if (Main.InHardMode)
			{
				ItemSet[num++].SetDefaults(369);
			}
			break;
		case 4:
			ItemSet[num++].SetDefaults(168);
			ItemSet[num++].SetDefaults(166);
			ItemSet[num++].SetDefaults(167);
			if (Main.InHardMode)
			{
				ItemSet[num++].SetDefaults(265);
			}
			break;
		case 5:
			ItemSet[num++].SetDefaults(254);
			if (Main.GameTime.DayTime)
			{
				ItemSet[num++].SetDefaults(242);
			}
			if (Main.GameTime.MoonPhase == 0)
			{
				ItemSet[num++].SetDefaults(245);
				ItemSet[num++].SetDefaults(246);
			}
			else if (Main.GameTime.MoonPhase == 1)
			{
				ItemSet[num++].SetDefaults(325);
				ItemSet[num++].SetDefaults(326);
			}
			ItemSet[num++].SetDefaults(269);
			ItemSet[num++].SetDefaults(270);
			ItemSet[num++].SetDefaults(271);
			if (NPC.HasDownedClown)
			{
				ItemSet[num++].SetDefaults(503);
				ItemSet[num++].SetDefaults(504);
				ItemSet[num++].SetDefaults(505);
			}
			if (Main.GameTime.IsBloodMoon)
			{
				ItemSet[num++].SetDefaults(322);
			}
			break;
		case 6:
			ItemSet[num++].SetDefaults(128);
			ItemSet[num++].SetDefaults(486);
			ItemSet[num++].SetDefaults(398);
			ItemSet[num++].SetDefaults(84);
			ItemSet[num++].SetDefaults(407);
			ItemSet[num++].SetDefaults(161);
			break;
		case 7:
			ItemSet[num++].SetDefaults(487);
			ItemSet[num++].SetDefaults(496);
			ItemSet[num++].SetDefaults(500);
			ItemSet[num++].SetDefaults(507);
			ItemSet[num++].SetDefaults(508);
			ItemSet[num++].SetDefaults(531);
			ItemSet[num++].SetDefaults(576);
			break;
		case 8:
			ItemSet[num++].SetDefaults(509);
			ItemSet[num++].SetDefaults(510);
			ItemSet[num++].SetDefaults(530);
			ItemSet[num++].SetDefaults(513);
			ItemSet[num++].SetDefaults(538);
			ItemSet[num++].SetDefaults(529);
			ItemSet[num++].SetDefaults(541);
			ItemSet[num++].SetDefaults(542);
			ItemSet[num++].SetDefaults(543);
			break;
		case 9:
			ItemSet[num++].SetDefaults(588);
			ItemSet[num++].SetDefaults(589);
			ItemSet[num++].SetDefaults(590);
			ItemSet[num++].SetDefaults(597);
			ItemSet[num++].SetDefaults(598);
			ItemSet[num++].SetDefaults(596);
			break;
		}
	}

	private void ConvertCoins(int ID)
	{
		for (int i = 0; i < 20; i++)
		{
			if (ItemSet[i].Stack != ItemSet[i].MaxStack || !ItemSet[i].CanBePlacedInCoinSlot())
			{
				continue;
			}
			ItemSet[i].SetDefaults(ItemSet[i].Type + 1);
			for (int j = 0; j < 20; j++)
			{
				if (j != i && ItemSet[j].Type == ItemSet[i].Type && ItemSet[j].Stack < ItemSet[j].MaxStack)
				{
					if (ID >= 0)
					{
						NetMessage.CreateMessage2(32, ID, j);
						NetMessage.SendMessage();
					}
					ItemSet[j].Stack++;
					ItemSet[i].Init();
					ConvertCoins(ID);
				}
			}
		}
	}

	public void LootAll(Player CurrentPlayer)
	{
		int playerChest = CurrentPlayer.PlayerChest;
		for (int i = 0; i < 20; i++)
		{
			if (ItemSet[i].Type > 0)
			{
				CurrentPlayer.GetItem(ref ItemSet[i]);
				if (playerChest >= 0)
				{
					NetMessage.CreateMessage2(32, playerChest, i);
					NetMessage.SendMessage();
				}
			}
		}
	}

	public void Deposit(Player CurrentPlayer)
	{
		int playerChest = CurrentPlayer.PlayerChest;
		for (int num = 40; num >= 10; num--)
		{
			if (CurrentPlayer.Inventory[num].Stack > 0 && CurrentPlayer.Inventory[num].Type > 0)
			{
				if (CurrentPlayer.Inventory[num].MaxStack > 1)
				{
					for (int i = 0; i < 20; i++)
					{
						if (ItemSet[i].Stack >= ItemSet[i].MaxStack || CurrentPlayer.Inventory[num].NetID != ItemSet[i].NetID)
						{
							continue;
						}
						short num2 = CurrentPlayer.Inventory[num].Stack;
						if (CurrentPlayer.Inventory[num].Stack + ItemSet[i].Stack > ItemSet[i].MaxStack)
						{
							num2 = (short)(ItemSet[i].MaxStack - ItemSet[i].Stack);
						}
						CurrentPlayer.Inventory[num].Stack -= num2;
						ItemSet[i].Stack += num2;
						ConvertCoins(playerChest);
						Main.PlaySound(7);
						if (CurrentPlayer.Inventory[num].Stack <= 0)
						{
							CurrentPlayer.Inventory[num].Init();
							if (playerChest >= 0)
							{
								NetMessage.CreateMessage2(32, playerChest, i);
								NetMessage.SendMessage();
							}
							break;
						}
						if (ItemSet[i].Type == 0)
						{
							ref Item reference = ref ItemSet[i];
							reference = CurrentPlayer.Inventory[num];
							CurrentPlayer.Inventory[num].Init();
						}
						if (playerChest >= 0)
						{
							NetMessage.CreateMessage2(32, playerChest, i);
							NetMessage.SendMessage();
						}
					}
				}
				if (CurrentPlayer.Inventory[num].Stack > 0)
				{
					for (int j = 0; j < 20; j++)
					{
						if (ItemSet[j].Type == 0)
						{
							Main.PlaySound(7);
							ref Item reference2 = ref ItemSet[j];
							reference2 = CurrentPlayer.Inventory[j];
							CurrentPlayer.Inventory[j].Init();
							if (playerChest >= 0)
							{
								NetMessage.CreateMessage2(32, playerChest, j);
								NetMessage.SendMessage();
							}
							break;
						}
					}
				}
			}
		}
	}

	public void QuickStack(Player CurrentPlayer)
	{
		int playerChest = CurrentPlayer.PlayerChest;
		for (int i = 0; i < 20; i++)
		{
			if (ItemSet[i].Type <= 0 || ItemSet[i].Stack >= ItemSet[i].MaxStack)
			{
				continue;
			}
			for (int j = 0; j < 48; j++)
			{
				if (ItemSet[i].NetID == CurrentPlayer.Inventory[j].NetID)
				{
					short num = CurrentPlayer.Inventory[j].Stack;
					if (ItemSet[i].Stack + num > ItemSet[i].MaxStack)
					{
						num = (short)(ItemSet[i].MaxStack - ItemSet[i].Stack);
					}
					Main.PlaySound(7);
					ItemSet[i].Stack += num;
					CurrentPlayer.Inventory[j].Stack -= num;
					ConvertCoins(playerChest);
					if (CurrentPlayer.Inventory[j].Stack == 0)
					{
						CurrentPlayer.Inventory[j].Init();
					}
					else if (ItemSet[i].Type == 0)
					{
						ref Item reference = ref ItemSet[i];
						reference = CurrentPlayer.Inventory[j];
						CurrentPlayer.Inventory[j].Init();
					}
					if (playerChest >= 0)
					{
						NetMessage.CreateMessage2(32, playerChest, i);
						NetMessage.SendMessage();
					}
				}
			}
		}
	}
}
