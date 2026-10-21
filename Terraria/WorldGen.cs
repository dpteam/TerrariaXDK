using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Storage;
using Terraria.Achievements;

namespace Terraria;

internal static class WorldGen
{
	public sealed class FallingSandBuffer
	{
		public int count;

		public Location[] buffer = new Location[64];

		public unsafe void Add(int i, int j)
		{
			fixed (Location* ptr = &buffer[count])
			{
				ptr->X = (short)i;
				ptr->Y = (short)j;
			}
			count++;
		}
	}

	private struct DRoom
	{
		public short X;

		public short Y;

		public short L;

		public short R;

		public short T;

		public short B;

		public float Size;
	}

	private struct DDoor
	{
		public short X;

		public short Y;

		public short Pos;
	}

	private enum Dir
	{
		LEFT = 1,
		RIGHT = 2,
		LEFT_RIGHT = 3,
		UP = 4,
		DOWN = 8,
		UP_DOWN = 12
	}

	public const int MaxNumMech = 1000;

	public const int MaxNumWire = 1000;

	public const int MaxNumPumps = 20;

	private const float CorruptionFactorForAchievement = 50f;

	private const float HallowedFactorForAchievement = 50f;

	private const int SAND_BUFFER_SIZE = 64;

	public const int MAX_ROOM_RECURSION = 400;

	public const int MAX_ROOM_TILES = 1900;

	public const int MIN_ROOM_TILES = 60;

	public const int MAX_DROOMS = 100;

	private const int MAX_DDOORS = 300;

	private const int MAX_DPLATS = 300;

	private const int MIN_JCHEST = 6;

	private const int MAX_JCHEST = 9;

	private const double JCHEST_WORLD_SIZE_MODIFIER = 1.2;

	public static int numMechs = 0;

	public static Mech[] mech = new Mech[1000];

	private static WireCheck[] wiresChecked = new WireCheck[1000];

	private static Location[] wire = new Location[1000];

	public static int numNoWire = 0;

	public static Location[] noWire = new Location[1000];

	public static int numInPump = 0;

	public static Location[] inPump = new Location[20];

	public static int numOutPump = 0;

	public static Location[] outPump = new Location[20];

	public static int totalEvil = 0;

	public static int totalGood = 0;

	public static int totalSolid = 0;

	public static int totalEvil2 = 0;

	public static int totalGood2 = 0;

	public static int totalSolid2 = 0;

	public static byte EvilCoverage = 0;

	public static byte GoodCoverage = 0;

	public static int totalX = 0;

	public static int totalD = 0;

	private static int currentSandBuffer = 0;

	private static FallingSandBuffer[] sandBuffer = new FallingSandBuffer[2];

	public static volatile bool hardLock = false;

	public static volatile bool saveLock = false;

	private static object padlock = new object();

	public static uint woodSpawned = 0u;

	public static int lavaLine;

	public static int WaterLine;

	public static int shadowOrbCount = 0;

	public static int altarCount = 0;

	public static bool ToSpawnEye = false;

	public static bool Gen = false;

	public static bool HasShadowOrbSmashed = false;

	public static bool ToSpawnMeteor = false;

	private static short lastMaxTilesX = 0;

	private static short lastMaxTilesY = 0;

	public static Time tempTime = default;

	private static bool stopDrops = false;

	private static bool mudWall = false;

	private static int grassSpread = 0;

	public static bool noLiquidCheck = false;

	public static bool ToDestroyObject = false;

	public static FastRandom genRand = new FastRandom();

	public static int spawnDelay = 0;

	public static int ToSpawnNPC = 0;

	public static int numRoomTiles;

	public static Location[] room = new Location[1900];

	public static int roomX1;

	public static int roomX2;

	public static int roomY1;

	public static int roomY2;

	public static bool canSpawn;

	public static bool[] HouseTile = new bool[150];

	public static int bestX = 0;

	public static int bestY = 0;

	public static int hiScore = 0;

	public static int dungeonX;

	public static int dungeonY;

	public static Vector2i lastDungeonHall = default;

	public static int numDRooms = 0;

	private static DRoom[] dRoom = new DRoom[100];

	private static int numDDoors;

	private static DDoor[] dDoor = new DDoor[300];

	private static int numDPlats;

	private static Location[] DPlat = new Location[300];

	private static int numJChests = 0;

	private static Location[] JChest = new Location[10];

	public static int dEnteranceX = 0;

	public static bool dSurface = false;

	private static double dxStrength1;

	private static double dyStrength1;

	private static double dxStrength2;

	private static double dyStrength2;

	private static int dMinX;

	private static int dMaxX;

	private static int dMinY;

	private static int dMaxY;

	private static int numIslandHouses = 0;

	private static int houseCount = 0;

	private static Location[] fih = new Location[300];

	private static int numMCaves = 0;

	private static Location[] mCave = new Location[300];

	private static int JungleX = 0;

	private static int hellChest = 0;

	private static bool roomTorch;

	private static bool roomDoor;

	public static bool roomChair;

	private static bool roomTable;

	private static bool roomOccupied;

	private static bool roomEvil;

	private static int checkRoomDepth;

	private static bool mergeUp;

	private static bool mergeDown;

	private static bool mergeLeft;

	private static bool mergeRight;

	private static bool tileFrameRecursion = true;

	private static bool mergeUp2;

	private static bool mergeDown2;

	private static bool mergeLeft2;

	private static bool mergeRight2;

	public unsafe static void UpdateSand()
	{
		FallingSandBuffer fallingSandBuffer = sandBuffer[currentSandBuffer];
		int count = fallingSandBuffer.count;
		if (count <= 0)
		{
			return;
		}
		fallingSandBuffer.count = 0;
		currentSandBuffer ^= 1;
		int num = 0;
		do
		{
			int x = fallingSandBuffer.buffer[num].X;
			int y = fallingSandBuffer.buffer[num].Y;
			fixed (Tile* ptr = &Main.TileSet[x, y])
			{
				ptr->IsActive = 0;
				float x2 = x * 16 + 8;
				float y2 = y * 16 + 8;
				EntityID.TileID type = (EntityID.TileID)ptr->Type;
				if (1 == 0)
				{
				}
				int type2 = type switch
				{
					EntityID.TileID.EBONSAND => 56, 
					EntityID.TileID.PEARLSAND => 67, 
					EntityID.TileID.SILT => 71, 
					_ => 31, 
				};
				if (1 == 0)
				{
				}
				int num2 = Projectile.NewProjectile(x2, y2, 0f, 2.5f, type2, 10, 0f);
				if (num2 < 0)
				{
					break;
				}
				Main.ProjectileSet[num2].velocity.Y = 0.5f;
				Main.ProjectileSet[num2].position.Y += 2f;
				Main.ProjectileSet[num2].XYWH.Y += 2;
				tileFrameRecursion = false;
				TileFrame(x, y - 1);
				TileFrame(x - 1, y);
				TileFrame(x + 1, y);
				tileFrameRecursion = true;
				NetMessage.SendTile(x, y);
			}
		}
		while (++num < count);
	}

	public static bool MoveNPC(int x, int y, int n)
	{
		if (!StartRoomCheck(x, y))
		{
			Main.NewText(Lang.InterfaceText[40], 255, 240, 20);
			return false;
		}
		if (!RoomNeeds())
		{
			if (Lang.LangOption <= 1)
			{
				int num = 0;
				string[] array = new string[4];
				if (!roomTorch)
				{
					array[num] = "a light source";
					num++;
				}
				if (!roomDoor)
				{
					array[num] = "a door";
					num++;
				}
				if (!roomTable)
				{
					array[num] = "a table";
					num++;
				}
				if (!roomChair)
				{
					array[num] = "a chair";
					num++;
				}
				string text = "";
				for (int i = 0; i < num; i++)
				{
					if (num == 2 && i == 1)
					{
						text += " and ";
					}
					else if (i > 0 && i != num - 1)
					{
						text += ", and ";
					}
					else if (i > 0)
					{
						text += ", ";
					}
					text += array[i];
				}
				Main.NewText("This housing is missing " + text + ".", 255, 240, 20);
			}
			else
			{
				Main.NewText(Lang.InterfaceText[8], 255, 240, 20);
			}
			return false;
		}
		ScoreRoom();
		if (hiScore <= 0)
		{
			if (roomOccupied)
			{
				Main.NewText(Lang.InterfaceText[41], 255, 240, 20);
			}
			else if (roomEvil)
			{
				Main.NewText(Lang.InterfaceText[42], 255, 240, 20);
			}
			else
			{
				Main.NewText(Lang.InterfaceText[8], 255, 240, 20);
			}
			return false;
		}
		return true;
	}

	public static void moveRoom(int x, int y, int n)
	{
		if (Main.NetMode >= 1)
		{
			NetMessage.CreateMessage4(60, n, x, y, 1);
			NetMessage.SendMessage();
		}
		else
		{
			ToSpawnNPC = Main.NPCSet[n].Type;
			Main.NPCSet[n].IsHomeless = true;
			SpawnNPC(x, y);
		}
	}

	public static void kickOut(int n)
	{
		if (Main.NetMode >= 1)
		{
			NetMessage.CreateMessage4(60, n, 0, 0, 0);
			NetMessage.SendMessage();
		}
		else
		{
			Main.NPCSet[n].IsHomeless = true;
		}
	}

	public static void SpawnNPC(int x, int y)
	{
		if (Main.WallHouse[Main.TileSet[x, y].WallType])
		{
			canSpawn = true;
		}
		else if (!canSpawn)
		{
			return;
		}
		if (!StartRoomCheck(x, y) || !RoomNeeds())
		{
			return;
		}
		ScoreRoom();
		if (hiScore <= 0)
		{
			return;
		}
		int num = -1;
		for (int i = 0; i < 196; i++)
		{
			if (Main.NPCSet[i].Active != 0 && Main.NPCSet[i].IsHomeless && Main.NPCSet[i].Type == ToSpawnNPC)
			{
				num = i;
				break;
			}
		}
		if (num < 0)
		{
			int num2 = bestX;
			int num3 = bestY;
			bool flag = false;
			if (!flag)
			{
				flag = true;
				Rectangle value = Main.ScreenAreaAround(num2 * 16 + 8, num3 * 16 + 8);
				for (int j = 0; j < 8; j++)
				{
					if (Main.PlayerSet[j].Active != 0 && Main.PlayerSet[j].XYWH.Intersects(value))
					{
						flag = false;
						break;
					}
				}
			}
			if (!flag && num3 <= Main.WorldSurface)
			{
				for (int k = 1; k < 500; k++)
				{
					for (int l = 0; l < 2; l++)
					{
						num2 = ((l != 0) ? (bestX - k) : (bestX + k));
						if (num2 > 10 && num2 < Main.MaxTilesX - 10)
						{
							int num4 = bestY - k;
							double num5 = bestY + k;
							if (num4 < 10)
							{
								num4 = 10;
							}
							if (num5 > (double)Main.WorldSurface)
							{
								num5 = Main.WorldSurface;
							}
							for (int m = num4; (double)m < num5; m++)
							{
								num3 = m;
								if (Main.TileSet[num2, num3].IsActive == 0 || !Main.TileSolid[Main.TileSet[num2, num3].Type])
								{
									continue;
								}
								if (Collision.SolidTiles(num2 - 1, num2 + 1, num3 - 3, num3 - 1))
								{
									break;
								}
								flag = true;
								Rectangle value2 = Main.ScreenAreaAround(num2 * 16 + 8, num3 * 16 + 8);
								for (int n = 0; n < 8; n++)
								{
									if (Main.PlayerSet[n].Active != 0 && Main.PlayerSet[n].XYWH.Intersects(value2))
									{
										flag = false;
										break;
									}
								}
								break;
							}
						}
						if (flag)
						{
							break;
						}
					}
					if (flag)
					{
						break;
					}
				}
			}
			int num6 = NPC.NewNPC(num2 * 16, num3 * 16, ToSpawnNPC, 1);
			Main.NPCSet[num6].HomeTileX = (short)bestX;
			Main.NPCSet[num6].HomeTileY = (short)bestY;
			if (num2 < bestX)
			{
				Main.NPCSet[num6].Direction = 1;
			}
			else if (num2 > bestX)
			{
				Main.NPCSet[num6].Direction = -1;
			}
			Main.NPCSet[num6].ShouldNetUpdate = true;
			string text;
			if (Main.NPCSet[num6].HasName())
			{
				text = Main.NPCSet[num6].GetName();
				if (Lang.LangOption <= 1)
				{
					text = text + " the " + Main.NPCSet[num6].TypeName;
				}
			}
			else
			{
				text = Main.NPCSet[num6].DisplayName;
			}
			NetMessage.SendText(text, 18, 50, 125, 255, -1);
		}
		else
		{
			Main.NPCSet[num].HomeTileX = (short)bestX;
			Main.NPCSet[num].HomeTileY = (short)bestY;
			Main.NPCSet[num].IsHomeless = false;
		}
		if (!Main.IsTutorial())
		{
			if (ToSpawnNPC == 22)
			{
				UI.SetTriggerStateForAll(Trigger.HouseGuide);
			}
			CheckHousedNPCs();
		}
		ToSpawnNPC = 0;
	}

	public static void CheckHousedNPCs()
	{
		bool flag = true;
		int num = 0;
		for (int i = 0; i < 196; i++)
		{
			NPC nPC = Main.NPCSet[i];
			if (nPC.Active != 0 && nPC.IsTownNPC && nPC.Type != 37 && nPC.Type != 142)
			{
				flag = flag && !nPC.IsHomeless;
				num++;
			}
		}
		if (flag && num == 10)
		{
			UI.SetTriggerStateForAll(Trigger.HousedAllNPCs);
		}
	}

	public static bool RoomNeeds()
	{
		roomChair = EntityID.RoomChairs.Any((EntityID.TileID i) => HouseTile[(int)i]);
		roomDoor = EntityID.RoomDoors.Any((EntityID.TileID i) => HouseTile[(int)i]);
		roomTable = EntityID.RoomTables.Any((EntityID.TileID i) => HouseTile[(int)i]);
		roomTorch = EntityID.RoomTorches.Any((EntityID.TileID i) => HouseTile[(int)i]);
		if (roomChair && roomTable && roomDoor && roomTorch)
		{
			canSpawn = true;
		}
		else
		{
			canSpawn = false;
		}
		return canSpawn;
	}

	public static void QuickFindHome(int npc)
	{
		if (Main.NPCSet[npc].HomeTileX <= 10 || Main.NPCSet[npc].HomeTileY <= 10 || Main.NPCSet[npc].HomeTileX >= Main.MaxTilesX - 10 || Main.NPCSet[npc].HomeTileY >= Main.MaxTilesY)
		{
			return;
		}
		canSpawn = false;
		StartRoomCheck(Main.NPCSet[npc].HomeTileX, Main.NPCSet[npc].HomeTileY - 1);
		if (!canSpawn)
		{
			for (int i = Main.NPCSet[npc].HomeTileX - 1; i < Main.NPCSet[npc].HomeTileX + 2; i++)
			{
				for (int j = Main.NPCSet[npc].HomeTileY - 1; j < Main.NPCSet[npc].HomeTileY + 2 && !StartRoomCheck(i, j); j++)
				{
				}
			}
		}
		if (!canSpawn)
		{
			int num = 10;
			for (int k = Main.NPCSet[npc].HomeTileX - num; k <= Main.NPCSet[npc].HomeTileX + num; k += 2)
			{
				for (int l = Main.NPCSet[npc].HomeTileY - num; l <= Main.NPCSet[npc].HomeTileY + num && !StartRoomCheck(k, l); l += 2)
				{
				}
			}
		}
		if (canSpawn)
		{
			RoomNeeds();
			if (canSpawn)
			{
				ScoreRoom(npc);
			}
			if (canSpawn && hiScore > 0)
			{
				Main.NPCSet[npc].HomeTileX = (short)bestX;
				Main.NPCSet[npc].HomeTileY = (short)bestY;
				Main.NPCSet[npc].IsHomeless = false;
				canSpawn = false;
			}
			else
			{
				Main.NPCSet[npc].IsHomeless = true;
			}
		}
		else
		{
			Main.NPCSet[npc].IsHomeless = true;
		}
	}

	public static void ScoreRoom(int ignoreNPC = -1)
	{
		roomOccupied = false;
		roomEvil = false;
		for (int i = 0; i < 196; i++)
		{
			if (Main.NPCSet[i].Active == 0 || !Main.NPCSet[i].IsTownNPC || ignoreNPC == i || Main.NPCSet[i].IsHomeless)
			{
				continue;
			}
			for (int j = 0; j < numRoomTiles; j++)
			{
				if (Main.NPCSet[i].HomeTileX != room[j].X || Main.NPCSet[i].HomeTileY != room[j].Y)
				{
					continue;
				}
				bool flag = false;
				for (int k = 0; k < numRoomTiles; k++)
				{
					if (Main.NPCSet[i].HomeTileX == room[k].X && Main.NPCSet[i].HomeTileY - 1 == room[k].Y)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					roomOccupied = true;
					hiScore = -1;
					return;
				}
			}
		}
		hiScore = 0;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = roomX1 - 3 - 1 - 34;
		int num5 = roomX2 + 3 + 1 + 34;
		int num6 = roomY1 - 2 - 1 - 34;
		int num7 = roomY2 + 2 + 1 + 34;
		if (num4 < 0)
		{
			num4 = 0;
		}
		if (num5 >= Main.MaxTilesX)
		{
			num5 = Main.MaxTilesX - 1;
		}
		if (num6 < 0)
		{
			num6 = 0;
		}
		if (num7 > Main.MaxTilesX)
		{
			num7 = Main.MaxTilesX;
		}
		for (int l = num4 + 1; l < num5; l++)
		{
			for (int m = num6 + 2; m < num7 + 2; m++)
			{
				if (Main.TileSet[l, m].IsActive != 0)
				{
					switch ((EntityID.TileID)Main.TileSet[l, m].Type)
					{
					case EntityID.TileID.CORRUPT_GRASS:
					case EntityID.TileID.SHORT_CORRUPT_PLANTS:
					case EntityID.TileID.EBONSTONE:
					case EntityID.TileID.CORRUPTION_THORN:
					case EntityID.TileID.EBONSAND:
						num3++;
						break;
					case EntityID.TileID.SUNFLOWER:
						num3 -= 5;
						break;
					case EntityID.TileID.HALLOWED_GRASS:
					case EntityID.TileID.SHORT_HALLOWED_PLANTS:
					case EntityID.TileID.TALL_HALLOWED_PLANTS:
					case EntityID.TileID.PEARLSAND:
						num3--;
						break;
					}
				}
			}
		}
		if (num3 < 50)
		{
			num3 = 0;
		}
		num2 = -num3;
		if (num2 <= -250)
		{
			hiScore = num2;
			roomEvil = true;
			return;
		}
		num4 = roomX1;
		num5 = roomX2;
		num6 = roomY1;
		num7 = roomY2;
		for (int n = num4 + 1; n < num5; n++)
		{
			for (int num8 = num6 + 2; num8 < num7 + 2; num8++)
			{
				if (Main.TileSet[n, num8].IsActive == 0)
				{
					continue;
				}
				num = num2;
				if (!Main.TileSolidNotSolidTop[Main.TileSet[n, num8].Type] || Collision.SolidTiles(n - 1, n + 1, num8 - 3, num8 - 1) || Main.TileSet[n - 1, num8].IsActive == 0 || !Main.TileSolid[Main.TileSet[n - 1, num8].Type] || Main.TileSet[n + 1, num8].IsActive == 0 || !Main.TileSolid[Main.TileSet[n + 1, num8].Type])
				{
					continue;
				}
				for (int num9 = n - 2; num9 < n + 3; num9++)
				{
					for (int num10 = num8 - 4; num10 < num8; num10++)
					{
						if (Main.TileSet[num9, num10].IsActive != 0)
						{
							if (num9 == n)
							{
								num -= 15;
							}
							else if (Main.TileSet[num9, num10].Type == 10 || Main.TileSet[num9, num10].Type == 11)
							{
								num -= 20;
							}
							else
							{
								num = ((!Main.TileSolid[Main.TileSet[num9, num10].Type]) ? (num + 5) : (num - 5));
							}
						}
					}
				}
				if (num <= hiScore)
				{
					continue;
				}
				bool flag2 = false;
				for (int num11 = 0; num11 < numRoomTiles; num11++)
				{
					if (room[num11].X == n && room[num11].Y == num8)
					{
						flag2 = true;
						break;
					}
				}
				if (flag2)
				{
					hiScore = num;
					bestX = n;
					bestY = num8;
				}
			}
		}
	}

	public static bool StartRoomCheck(int x, int y)
	{
		roomX1 = x;
		roomX2 = x;
		roomY1 = y;
		roomY2 = y;
		numRoomTiles = 0;
		for (int i = 0; i < 150; i++)
		{
			HouseTile[i] = false;
		}
		canSpawn = true;
		if (Main.TileSet[x, y].IsActive != 0 && Main.TileSolid[Main.TileSet[x, y].Type])
		{
			canSpawn = false;
		}
		else
		{
			checkRoomDepth = 0;
			CheckRoom(x, y);
			if (numRoomTiles < 60)
			{
				canSpawn = false;
			}
		}
		return canSpawn;
	}

	public static void CheckRoom(int x, int y)
	{
		if (x < 10 || y < 10 || x >= Main.MaxTilesX - 10 || y >= lastMaxTilesY - 10)
		{
			canSpawn = false;
			return;
		}
		for (int i = 0; i < numRoomTiles; i++)
		{
			if (room[i].X == x && room[i].Y == y)
			{
				return;
			}
		}
		room[numRoomTiles].X = (short)x;
		room[numRoomTiles].Y = (short)y;
		if (++numRoomTiles >= 1900)
		{
			canSpawn = false;
			return;
		}
		if (++checkRoomDepth >= 400)
		{
			canSpawn = false;
			return;
		}
		if (Main.TileSet[x, y].IsActive != 0)
		{
			HouseTile[Main.TileSet[x, y].Type] = true;
			if (Main.TileSolid[Main.TileSet[x, y].Type] || Main.TileSet[x, y].Type == 11)
			{
				checkRoomDepth--;
				return;
			}
		}
		if (x < roomX1)
		{
			roomX1 = x;
		}
		if (x > roomX2)
		{
			roomX2 = x;
		}
		if (y < roomY1)
		{
			roomY1 = y;
		}
		if (y > roomY2)
		{
			roomY2 = y;
		}
		int num = 0;
		for (int j = -2; j < 3; j++)
		{
			if (Main.WallHouse[Main.TileSet[x + j, y].WallType])
			{
				num |= 1;
			}
			else if (Main.TileSet[x + j, y].IsActive != 0 && (Main.TileSolid[Main.TileSet[x + j, y].Type] || Main.TileSet[x + j, y].Type == 11))
			{
				num |= 1;
			}
			if (Main.WallHouse[Main.TileSet[x, y + j].WallType])
			{
				num |= 2;
			}
			else if (Main.TileSet[x, y + j].IsActive != 0 && (Main.TileSolid[Main.TileSet[x, y + j].Type] || Main.TileSet[x, y + j].Type == 11))
			{
				num |= 2;
			}
		}
		if (num != 3)
		{
			canSpawn = false;
			return;
		}
		for (int k = -1; k < 2; k++)
		{
			for (int l = -1; l < 2; l++)
			{
				if ((k != 0 || l != 0) && canSpawn)
				{
					CheckRoom(x + k, y + l);
					if (k == 1 && l == 1 && canSpawn)
					{
						checkRoomDepth--;
					}
				}
			}
		}
	}

	public static bool StartSpaceCheck(int x, int y)
	{
		roomX1 = x;
		roomX2 = x;
		roomY1 = y;
		roomY2 = y;
		numRoomTiles = 0;
		for (int i = 0; i < 150; i++)
		{
			HouseTile[i] = false;
		}
		canSpawn = true;
		if (Main.TileSet[x, y].IsActive != 0 && Main.TileSolid[Main.TileSet[x, y].Type])
		{
			canSpawn = false;
		}
		else
		{
			checkRoomDepth = 0;
			CheckSpace(x, y);
			if (numRoomTiles < 60)
			{
				canSpawn = false;
			}
		}
		return canSpawn;
	}

	public static void CheckSpace(int x, int y)
	{
		if (x < 10 || y < 10 || x >= Main.MaxTilesX - 10 || y >= lastMaxTilesY - 10)
		{
			canSpawn = false;
			return;
		}
		for (int i = 0; i < numRoomTiles; i++)
		{
			if (room[i].X == x && room[i].Y == y)
			{
				return;
			}
		}
		room[numRoomTiles].X = (short)x;
		room[numRoomTiles].Y = (short)y;
		if (++numRoomTiles >= 1900)
		{
			canSpawn = false;
			return;
		}
		if (++checkRoomDepth >= 400)
		{
			canSpawn = false;
			return;
		}
		if (Main.TileSet[x, y].IsActive != 0)
		{
			HouseTile[Main.TileSet[x, y].Type] = true;
			if (Main.TileSolid[Main.TileSet[x, y].Type] || Main.TileSet[x, y].Type == 11)
			{
				checkRoomDepth--;
				return;
			}
		}
		if (x < roomX1)
		{
			roomX1 = x;
		}
		if (x > roomX2)
		{
			roomX2 = x;
		}
		if (y < roomY1)
		{
			roomY1 = y;
		}
		if (y > roomY2)
		{
			roomY2 = y;
		}
		for (int j = -1; j < 2; j++)
		{
			for (int k = -1; k < 2; k++)
			{
				if ((j != 0 || k != 0) && canSpawn)
				{
					CheckSpace(x + j, y + k);
					if (j == 1 && k == 1 && canSpawn)
					{
						checkRoomDepth--;
					}
				}
			}
		}
	}

	public static void DropMeteor()
	{
		bool flag = true;
		int num = 0;
		if (Main.NetMode == 1)
		{
			return;
		}
		for (int i = 0; i < 8; i++)
		{
			if (Main.PlayerSet[i].Active != 0)
			{
				flag = false;
				break;
			}
		}
		int num2 = 0;
		float num3 = (float)Main.MaxTilesX / 4200f;
		int num4 = (int)(400f * num3);
		for (int j = 5; j < Main.MaxTilesX - 5; j++)
		{
			for (int k = 5; k < Main.WorldSurface; k++)
			{
				if (Main.TileSet[j, k].IsActive != 0 && Main.TileSet[j, k].Type == 37)
				{
					num2++;
					if (num2 > num4)
					{
						return;
					}
				}
			}
		}
		while (!flag)
		{
			float num5 = (float)Main.MaxTilesX * 0.08f;
			int num6 = Main.Rand.Next(50, Main.MaxTilesX - 50);
			while ((float)num6 > (float)Main.SpawnTileX - num5 && (float)num6 < (float)Main.SpawnTileX + num5)
			{
				num6 = Main.Rand.Next(50, Main.MaxTilesX - 50);
			}
			for (int l = Main.Rand.Next(100); l < Main.MaxTilesY; l++)
			{
				if (Main.TileSet[num6, l].IsActive != 0 && Main.TileSolid[Main.TileSet[num6, l].Type])
				{
					flag = meteor(num6, l);
					break;
				}
			}
			num++;
			if (num >= 100)
			{
				break;
			}
		}
	}

	public static bool meteor(int i, int j)
	{
		if (i < 50 || i > Main.MaxTilesX - 50)
		{
			return false;
		}
		if (j < 50 || j > Main.MaxTilesY - 50)
		{
			return false;
		}
		Rectangle rectangle = new Rectangle((i - 25) * 16, (j - 25) * 16, 800, 800);
		for (int k = 0; k < 8; k++)
		{
			if (Main.PlayerSet[k].Active != 0)
			{
				Rectangle value = Main.ScreenAreaAround(Main.PlayerSet[k].XYWH.X + Main.PlayerSet[k].XYWH.Width / 2, Main.PlayerSet[k].XYWH.Y + Main.PlayerSet[k].XYWH.Height / 2);
				if (rectangle.Intersects(value))
				{
					return false;
				}
			}
		}
		for (int l = 0; l < 196; l++)
		{
			if (Main.NPCSet[l].Active != 0 && rectangle.Intersects(Main.NPCSet[l].XYWH))
			{
				return false;
			}
		}
		for (int m = i - 25; m < i + 25; m++)
		{
			for (int n = j - 25; n < j + 25; n++)
			{
				if (Main.TileSet[m, n].Type == 21 && Main.TileSet[m, n].IsActive != 0)
				{
					return false;
				}
			}
		}
		stopDrops = true;
		for (int num = i - 15; num < i + 15; num++)
		{
			for (int num2 = j - 15; num2 < j + 15; num2++)
			{
				if (num2 > j + Main.Rand.Next(-2, 3) - 5 && Math.Abs(i - num) + Math.Abs(j - num2) < 22 + Main.Rand.Next(-5, 5))
				{
					if (!Main.TileSolid[Main.TileSet[num, num2].Type])
					{
						Main.TileSet[num, num2].IsActive = 0;
					}
					Main.TileSet[num, num2].Type = 37;
				}
			}
		}
		for (int num3 = i - 10; num3 < i + 10; num3++)
		{
			for (int num4 = j - 10; num4 < j + 10; num4++)
			{
				if (num4 > j + Main.Rand.Next(-2, 3) - 5 && Math.Abs(i - num3) + Math.Abs(j - num4) < 10 + Main.Rand.Next(-3, 4))
				{
					Main.TileSet[num3, num4].IsActive = 0;
				}
			}
		}
		for (int num5 = i - 16; num5 < i + 16; num5++)
		{
			for (int num6 = j - 16; num6 < j + 16; num6++)
			{
				int type = Main.TileSet[num5, num6].Type;
				if (type == 5 || type == 32)
				{
					KillTile(num5, num6);
				}
				SquareTileFrame(num5, num6);
				SquareWallFrame(num5, num6);
			}
		}
		for (int num7 = i - 23; num7 < i + 23; num7++)
		{
			for (int num8 = j - 23; num8 < j + 23; num8++)
			{
				if (Main.TileSet[num7, num8].IsActive != 0 && Main.Rand.Next(10) == 0 && (float)(Math.Abs(i - num7) + Math.Abs(j - num8)) < 29.9f)
				{
					if (Main.TileSet[num7, num8].Type == 5 || Main.TileSet[num7, num8].Type == 32)
					{
						KillTile(num7, num8);
					}
					Main.TileSet[num7, num8].Type = 37;
					SquareTileFrame(num7, num8);
				}
			}
		}
		stopDrops = false;
		NetMessage.SendText(36, 50, 255, 130, -1);
		NetMessage.SendTileSquare(i, j, 30);
		return true;
	}

	public static void setWorldSize()
	{
		Main.BottomWorld = Main.MaxTilesY * 16;
		Main.RightWorld = Main.MaxTilesX * 16;
		Main.MaxSectionsX = Main.MaxTilesX / 40;
		Main.MaxSectionsY = Main.MaxTilesY / 30;
	}

	public static void worldGenCallBack()
	{
		clearWorld();
		generateWorld();
		everyTileFrame();
		saveWorldWhilePlaying();
		Main.StartGame();
		Main.WorldGenThread = null;
	}

	public static void CreateNewWorld()
	{
		Netplay.StopFindingSessions();
		Thread thread = new Thread(worldGenCallBack);
		thread.IsBackground = true;
		thread.Start();
		Main.WorldGenThread = thread;
	}

	public static void SaveAndQuit()
	{
		Main.PlaySound(11);
		new Thread(SaveAndQuitCallBack).Start();
	}

	public static void SaveAndQuitCallBack()
	{
		Main.IsGameStarted = false;
		for (int i = 0; i < 4; i++)
		{
			UI uI = Main.UIInstance[i];
			if (uI.isStopping)
			{
				uI.ActivePlayer.Active = 0;
				uI.ActivePlayer.Save(uI.playerPathName);
				uI.SaveSettings();
			}
		}
		Netplay.PlayDisconnect = true;
		if (WorldSelect.isLocalWorld && UI.MainUI.HasPlayerStorage())
		{
			for (int j = 0; j < 4; j++)
			{
				UI uI2 = Main.UIInstance[j];
				if (uI2.isStopping)
				{
					uI2.statusText = Lang.WorldGenText[49];
				}
			}
			saveNewWorld();
		}
		for (int k = 0; k < Main.UIInstance.Length; k++)
		{
			UI uI3 = Main.UIInstance[k];
			if (!uI3.isStopping)
			{
				continue;
			}
			uI3.isStopping = false;
			if (uI3.SignedInGamer != null)
			{
				uI3.LoadPlayers();
				if (uI3.CurMenuMode != MenuMode.ERROR)
				{
					uI3.SetMenu(MenuMode.TITLE, rememberPrevious: false, reset: true);
				}
			}
		}
	}

	public static void playWorldCallBack()
	{
		if (Main.IsTutorial())
		{
			using (Stream file = TitleContainer.OpenStream("Content/Worlds/tutorial.wld"))
			{
				loadWorld(file);
			}
			tempTime.reset(0.01f);
			Main.NPCSet[0].Position.Y -= 1120f;
			Main.NPCSet[0].XYWH.Y -= 1120;
			Main.NPCSet[1].Position.Y -= 1120f;
			Main.NPCSet[1].XYWH.Y -= 1120;
		}
		else
		{
			bool flag;
			try
			{
				using StorageContainer storageContainer = UI.MainUI.OpenPlayerStorage("Worlds");
				using Stream file2 = storageContainer.OpenFile(WorldSelect.worldPathName, FileMode.Open);
				// Remember what the file looks like before loading: the loader reports only
				// "false", so a truncated save, a damaged header and a PC-edition world would
				// otherwise all fail with the same unhelpful menu.
				WorldSelect.LastLoadError = null;
				using (Stream probe = storageContainer.OpenFile(WorldSelect.worldPathName, FileMode.Open))
				{
					Terraria.World.WorldFileReport worldFileReport = Terraria.World.WorldFileInspector.Inspect(probe);
					if (!worldFileReport.Loadable)
					{
						WorldSelect.LastLoadError = worldFileReport.Summary;
					}
				}
				flag = loadWorld(file2);
			}
			catch (ThreadAbortException)
			{
				flag = true;
			}
			catch (IOException)
			{
				UI.MainUI.ReadError();
				flag = false;
			}
			catch (Exception)
			{
				flag = false;
			}
			if (!flag)
			{
				UI.MainUI.SetMenu(MenuMode.LOAD_FAILED_NO_BACKUP, rememberPrevious: false);
				Main.WorldGenThread = null;
				return;
			}
		}
		everyTileFrame();
		Main.StartGame();
		Main.WorldGenThread = null;
	}

	public static void PlayWorld()
	{
		Netplay.StopFindingSessions();
		Thread thread = new Thread(playWorldCallBack);
		thread.Start();
		Main.WorldGenThread = thread;
	}

	public static void saveWorldWhilePlayingCallBack()
	{
		Gen = true;
		saveNewWorld();
	}

	public static void saveWorldWhilePlaying()
	{
		if (UI.MainUI.HasPlayerStorage())
		{
			new Thread(saveWorldWhilePlayingCallBack).Start();
		}
	}

	public static void savePlayerWhilePlayingCallBack()
	{
		for (int i = 0; i < Main.UIInstance.Length; i++)
		{
			UI uI = Main.UIInstance[i];
			if (uI.CurMenuType != MenuType.MAIN)
			{
				uI.ActivePlayer.Save(uI.playerPathName);
				uI.SaveSettings();
			}
		}
	}

	public static void savePlayerWhilePlaying()
	{
		new Thread(savePlayerWhilePlayingCallBack).Start();
	}

	public static void saveAllWhilePlayingCallBack()
	{
		if (WorldSelect.isLocalWorld && UI.MainUI.HasPlayerStorage())
		{
			saveNewWorld();
		}
		for (int i = 0; i < Main.UIInstance.Length; i++)
		{
			UI uI = Main.UIInstance[i];
			if (uI.CurMenuType != MenuType.MAIN)
			{
				uI.ActivePlayer.Save(uI.playerPathName);
				uI.SaveSettings();
			}
		}
	}

	public static void saveAllWhilePlaying()
	{
		new Thread(saveAllWhilePlayingCallBack).Start();
	}

	public static void clearWorld()
	{
		UI.MainUI.statusText = Lang.WorldGenText[47];
		tempTime.reset(1f);
		totalSolid2 = 0;
		totalGood2 = 0;
		totalEvil2 = 0;
		totalSolid = 0;
		totalGood = 0;
		totalEvil = 0;
		totalX = 0;
		totalD = 0;
		EvilCoverage = 0;
		GoodCoverage = 0;
		NPC.ClearNames();
		ToSpawnEye = false;
		ToSpawnNPC = 0;
		shadowOrbCount = 0;
		altarCount = 0;
		Main.WorldID = 0;
		Main.WorldTimestamp = 0;
		Main.InHardMode = false;
		Main.DungeonX = 0;
		Main.DungeonY = 0;
		NPC.HasDownedBoss1 = false;
		NPC.HasDownedBoss2 = false;
		NPC.HasDownedBoss3 = false;
		NPC.HasSavedGoblin = false;
		NPC.HasSavedWizard = false;
		NPC.HasSavedMech = false;
		NPC.HasDownedGoblins = false;
		NPC.HasDownedClown = false;
		NPC.HasDownedFrost = false;
		HasShadowOrbSmashed = false;
		ToSpawnMeteor = false;
		stopDrops = false;
		Main.InvasionDelay = 0;
		Main.InvasionType = 0;
		Main.InvasionSize = 0;
		Main.InvasionWarn = 0;
		Main.InvasionX = 0f;
		Liquid.NumLiquids = 0;
		LiquidBuffer.NumLiquidBuffer = 0;
		sandBuffer[0] = new FallingSandBuffer();
		sandBuffer[1] = new FallingSandBuffer();
		if (lastMaxTilesX > Main.MaxTilesX)
		{
			for (int i = Main.MaxTilesX; i < lastMaxTilesX; i++)
			{
				for (int j = 0; j < lastMaxTilesY; j++)
				{
					Main.TileSet[i, j].Clear();
				}
			}
		}
		if (lastMaxTilesY > Main.MaxTilesY)
		{
			for (int k = 0; k < Main.MaxTilesX; k++)
			{
				for (int l = Main.MaxTilesY; l < lastMaxTilesY; l++)
				{
					Main.TileSet[k, l].Clear();
				}
			}
		}
		lastMaxTilesX = Main.MaxTilesX;
		lastMaxTilesY = Main.MaxTilesY;
		if (Main.NetMode != 1)
		{
			for (int m = 0; m < Main.MaxTilesX; m++)
			{
				for (int n = 0; n < Main.MaxTilesY; n++)
				{
					Main.TileSet[m, n].Clear();
				}
			}
		}
		Main.DustSet.Init();
		for (int num = 0; num < 128; num++)
		{
			Main.GoreSet[num].Init();
		}
		for (int num2 = 0; num2 < 200; num2++)
		{
			Main.ItemSet[num2].Init();
		}
		for (int num3 = 0; num3 < 196; num3++)
		{
			Main.NPCSet[num3] = new NPC();
		}
		for (int num4 = 0; num4 < 512; num4++)
		{
			Main.ProjectileSet[num4].Init();
		}
		for (int num5 = 0; num5 < 1000; num5++)
		{
			Main.ChestSet[num5] = null;
		}
		for (int num6 = 0; num6 < 1000; num6++)
		{
			Main.SignSet[num6].Init();
		}
		for (int num7 = 0; num7 < 8192; num7++)
		{
			Main.LiquidBuffer[num7] = default;
		}
	}

	public static bool loadWorld(Stream file)
	{
		bool result = true;
		Time.CheckXMas();
		using MemoryStream memoryStream = new MemoryStream((int)file.Length);
		memoryStream.SetLength(file.Length);
		file.Read(memoryStream.GetBuffer(), 0, (int)file.Length);
		file.Close();
		using BinaryReader binaryReader = new BinaryReader(memoryStream);
		try
		{
			int num = binaryReader.ReadInt32();
			if (num > 49)
			{
				throw new InvalidOperationException("Invalid version");
			}
			if (num <= 46)
			{
				loadOldWorld(binaryReader, num);
			}
			else
			{
				loadNewWorld(binaryReader, num, memoryStream);
			}
			Gen = true;
			UI.MainUI.NextProgressStep(Lang.WorldGenText[52]);
			for (int i = 0; i < Main.MaxTilesX; i++)
			{
				if ((i & 0x3F) == 0)
				{
					UI.MainUI.Progress = (float)i / (float)Main.MaxTilesX;
				}
				CountTiles(i);
			}
			NPC.SetNames();
			UI.MainUI.NextProgressStep(Lang.WorldGenText[27]);
			WaterLine = Main.MaxTilesY;
			Liquid.QuickWater(0.5);
			WaterCheck();
			int num2 = 0;
			Liquid.QuickSettleOn();
			int num3 = Liquid.NumLiquids + LiquidBuffer.NumLiquidBuffer;
			float num4 = 0f;
			while (Liquid.NumLiquids > 0 && num2 < 512)
			{
				num2++;
				float num5 = (float)(num3 - (Liquid.NumLiquids + LiquidBuffer.NumLiquidBuffer)) / (float)num3;
				if (Liquid.NumLiquids + LiquidBuffer.NumLiquidBuffer > num3)
				{
					num3 = Liquid.NumLiquids + LiquidBuffer.NumLiquidBuffer;
				}
				if (num5 > num4)
				{
					num4 = num5;
				}
				else
				{
					num5 = num4;
				}
				if (num5 <= 0.5f)
				{
					UI.MainUI.Progress = num5 + 0.5f;
				}
				Liquid.UpdateLiquid();
			}
			Liquid.QuickSettleOff();
			WaterCheck();
			Gen = false;
			return result;
		}
		catch
		{
			return false;
		}
	}

	private unsafe static void loadOldWorld(BinaryReader fileIO, int release)
	{
		string text = fileIO.ReadString();
		int worldID = fileIO.ReadInt32();
		Main.RightWorld = fileIO.ReadInt32();
		Main.RightWorld = fileIO.ReadInt32();
		Main.BottomWorld = fileIO.ReadInt32();
		Main.BottomWorld = fileIO.ReadInt32();
		Main.MaxTilesY = (short)fileIO.ReadInt32();
		Main.MaxTilesX = (short)fileIO.ReadInt32();
		clearWorld();
		Main.WorldID = worldID;
		UI.MainUI.FirstProgressStep(4, Lang.WorldGenText[51]);
		Main.SpawnTileX = (short)fileIO.ReadInt32();
		Main.SpawnTileY = (short)fileIO.ReadInt32();
		Main.WorldSurface = (int)fileIO.ReadDouble();
		Main.WorldSurfacePixels = Main.WorldSurface << 4;
		Main.RockLayer = (int)fileIO.ReadDouble();
		Main.RockLayerPixels = Main.RockLayer << 4;
		UpdateMagmaLayerPos();
		tempTime.DayRate = 1f;
		tempTime.WorldTime = (float)fileIO.ReadDouble();
		tempTime.DayTime = fileIO.ReadBoolean();
		tempTime.MoonPhase = (byte)fileIO.ReadInt32();
		tempTime.IsBloodMoon = fileIO.ReadBoolean();
		Main.DungeonX = (short)fileIO.ReadInt32();
		Main.DungeonY = (short)fileIO.ReadInt32();
		NPC.HasDownedBoss1 = fileIO.ReadBoolean();
		NPC.HasDownedBoss2 = fileIO.ReadBoolean();
		NPC.HasDownedBoss3 = fileIO.ReadBoolean();
		NPC.HasSavedGoblin = fileIO.ReadBoolean();
		NPC.HasSavedWizard = fileIO.ReadBoolean();
		NPC.HasSavedMech = fileIO.ReadBoolean();
		NPC.HasDownedGoblins = fileIO.ReadBoolean();
		NPC.HasDownedClown = fileIO.ReadBoolean();
		NPC.HasDownedFrost = fileIO.ReadBoolean();
		HasShadowOrbSmashed = fileIO.ReadBoolean();
		ToSpawnMeteor = fileIO.ReadBoolean();
		shadowOrbCount = fileIO.ReadByte();
		altarCount = fileIO.ReadInt32();
		Main.InHardMode = fileIO.ReadBoolean();
		Main.InvasionDelay = fileIO.ReadInt32();
		Main.InvasionSize = fileIO.ReadInt32();
		Main.InvasionType = fileIO.ReadInt32();
		Main.InvasionX = (float)fileIO.ReadDouble();
		for (int i = 0; i < Main.MaxTilesX; i++)
		{
			if ((i & 0x1F) == 0)
			{
				UI.MainUI.Progress = (float)i / (float)Main.MaxTilesX;
			}
			fixed (Tile* ptr = &Main.TileSet[i, 0])
			{
				Tile* ptr2 = ptr;
				int num = 0;
				while (num < Main.MaxTilesY)
				{
					ptr2->CurrentFlags = ~(Tile.Flags.WALLFRAME_MASK | Tile.Flags.HIGHLIGHT_MASK | Tile.Flags.VISITED | Tile.Flags.WIRE | Tile.Flags.CHECKING_LIQUID | Tile.Flags.SKIP_LIQUID);
					ptr2->IsActive = fileIO.ReadByte();
					if (ptr2->IsActive != 0)
					{
						ptr2->Type = fileIO.ReadByte();
						if (ptr2->Type == 127)
						{
							ptr2->IsActive = 0;
						}
						if (Main.TileFrameImportant[ptr2->Type])
						{
							ptr2->FrameX = fileIO.ReadInt16();
							ptr2->FrameY = fileIO.ReadInt16();
							if (ptr2->Type == 144)
							{
								ptr2->FrameY = 0;
							}
						}
						else
						{
							ptr2->FrameX = -1;
							ptr2->FrameY = -1;
						}
					}
					if (fileIO.ReadBoolean())
					{
						ptr2->WallType = fileIO.ReadByte();
					}
					if (fileIO.ReadBoolean())
					{
						ptr2->Liquid = fileIO.ReadByte();
						if (fileIO.ReadBoolean())
						{
							ptr2->Lava = 32;
						}
					}
					if (release < 46)
					{
						if (fileIO.ReadBoolean())
						{
							ptr2->wire = 16;
						}
					}
					else
					{
						Tile.Flags* ptr3 = &ptr2->CurrentFlags;
						*ptr3 |= (Tile.Flags)fileIO.ReadByte();
						if (Main.IsTutorial())
						{
							Tile.Flags* ptr4 = &ptr2->CurrentFlags;
							*ptr4 &= ~Tile.Flags.VISITED;
						}
					}
					int num2 = fileIO.ReadInt16();
					num += num2;
					while (num2 > 0)
					{
						ptr2[1] = *ptr2;
						ptr2++;
						num2--;
					}
					num++;
					ptr2++;
				}
			}
		}
		for (int j = 0; j < 1000; j++)
		{
			if (!fileIO.ReadBoolean())
			{
				continue;
			}
			Main.ChestSet[j] = new Chest();
			Main.ChestSet[j].XPos = (short)fileIO.ReadInt32();
			Main.ChestSet[j].YPos = (short)fileIO.ReadInt32();
			for (int k = 0; k < 20; k++)
			{
				byte b = fileIO.ReadByte();
				if (b > 0)
				{
					Main.ChestSet[j].ItemSet[k].NetDefaults(fileIO.ReadInt32(), b);
					Main.ChestSet[j].ItemSet[k].SetPrefix(fileIO.ReadByte());
				}
			}
		}
		for (int l = 0; l < 1000; l++)
		{
			if (fileIO.ReadBoolean())
			{
				string text2 = fileIO.ReadString();
				int num3 = fileIO.ReadInt32();
				int num4 = fileIO.ReadInt32();
				if (Main.TileSet[num3, num4].IsActive != 0 && (Main.TileSet[num3, num4].Type == 55 || Main.TileSet[num3, num4].Type == 85))
				{
					Main.SignSet[l].SignX = (short)num3;
					Main.SignSet[l].SignY = (short)num4;
					Main.SignSet[l].SignString = text2;
				}
			}
		}
		bool flag = fileIO.ReadBoolean();
		int num5 = 0;
		while (flag)
		{
			try
			{
				string value = fileIO.ReadString();
				Main.NPCSet[num5].SetDefaults(Convert.ToUInt16(value));
				Main.NPCSet[num5].Position.X = fileIO.ReadSingle();
				Main.NPCSet[num5].Position.Y = fileIO.ReadSingle();
				Main.NPCSet[num5].XYWH.X = (int)Main.NPCSet[num5].Position.X;
				Main.NPCSet[num5].XYWH.Y = (int)Main.NPCSet[num5].Position.Y;
				Main.NPCSet[num5].IsHomeless = fileIO.ReadBoolean();
				Main.NPCSet[num5].HomeTileX = (short)fileIO.ReadInt32();
				Main.NPCSet[num5].HomeTileY = (short)fileIO.ReadInt32();
				num5++;
			}
			catch (FormatException)
			{
				fileIO.ReadBytes(17);
			}
			flag = fileIO.ReadBoolean();
		}
		NPC.TypeNames[17] = fileIO.ReadString();
		NPC.TypeNames[18] = fileIO.ReadString();
		NPC.TypeNames[19] = fileIO.ReadString();
		NPC.TypeNames[20] = fileIO.ReadString();
		NPC.TypeNames[22] = fileIO.ReadString();
		NPC.TypeNames[54] = fileIO.ReadString();
		NPC.TypeNames[38] = fileIO.ReadString();
		NPC.TypeNames[107] = fileIO.ReadString();
		NPC.TypeNames[108] = fileIO.ReadString();
		NPC.TypeNames[124] = fileIO.ReadString();
		bool flag2 = fileIO.ReadBoolean();
		string text3 = fileIO.ReadString();
		int num6 = fileIO.ReadInt32();
		if (!flag2 || text3 != text || num6 != Main.WorldID)
		{
			throw new InvalidOperationException("Invalid footer");
		}
	}

	private unsafe static void loadNewWorld(BinaryReader fileIO, int release, MemoryStream stream)
	{
		if (release < 50)
		{
			CRC32 cRC = new CRC32();
			cRC.Update(stream.GetBuffer(), 8, (int)stream.Length - 8);
			if (cRC.GetValue() != fileIO.ReadUInt32())
			{
				throw new InvalidOperationException("Invalid CRC32");
			}
		}
		else
		{
			fileIO.ReadUInt32();
		}
		fileIO.ReadString();
		int worldID = fileIO.ReadInt32();
		int worldTimestamp = ((release >= 48) ? fileIO.ReadInt32() : 0);
		Main.RightWorld = fileIO.ReadInt32();
		Main.BottomWorld = fileIO.ReadInt16();
		Main.MaxTilesY = fileIO.ReadInt16();
		Main.MaxTilesX = fileIO.ReadInt16();
		setWorldSize();
		clearWorld();
		Main.WorldID = worldID;
		Main.WorldTimestamp = worldTimestamp;
		UI.MainUI.FirstProgressStep(4, Lang.WorldGenText[51]);
		Main.SpawnTileX = fileIO.ReadInt16();
		Main.SpawnTileY = fileIO.ReadInt16();
		Main.WorldSurface = fileIO.ReadInt16();
		Main.WorldSurfacePixels = Main.WorldSurface << 4;
		Main.RockLayer = fileIO.ReadInt16();
		Main.RockLayerPixels = Main.RockLayer << 4;
		UpdateMagmaLayerPos();
		tempTime.DayRate = 1f;
		tempTime.WorldTime = fileIO.ReadSingle();
		tempTime.DayTime = fileIO.ReadBoolean();
		tempTime.MoonPhase = fileIO.ReadByte();
		tempTime.IsBloodMoon = fileIO.ReadBoolean();
		Main.DungeonX = fileIO.ReadInt16();
		Main.DungeonY = fileIO.ReadInt16();
		NPC.HasDownedBoss1 = fileIO.ReadBoolean();
		NPC.HasDownedBoss2 = fileIO.ReadBoolean();
		NPC.HasDownedBoss3 = fileIO.ReadBoolean();
		NPC.HasSavedGoblin = fileIO.ReadBoolean();
		NPC.HasSavedWizard = fileIO.ReadBoolean();
		NPC.HasSavedMech = fileIO.ReadBoolean();
		NPC.HasDownedGoblins = fileIO.ReadBoolean();
		NPC.HasDownedClown = fileIO.ReadBoolean();
		NPC.HasDownedFrost = fileIO.ReadBoolean();
		HasShadowOrbSmashed = fileIO.ReadBoolean();
		ToSpawnMeteor = fileIO.ReadBoolean();
		shadowOrbCount = fileIO.ReadByte();
		altarCount = fileIO.ReadInt32();
		Main.InHardMode = fileIO.ReadBoolean();
		Main.InvasionDelay = fileIO.ReadByte();
		Main.InvasionSize = fileIO.ReadInt16();
		Main.InvasionType = fileIO.ReadByte();
		Main.InvasionX = fileIO.ReadSingle();
		for (int i = 0; i < Main.MaxTilesX; i++)
		{
			if ((i & 0x1F) == 0)
			{
				UI.MainUI.Progress = (float)i / (float)Main.MaxTilesX;
			}
			fixed (Tile* ptr = &Main.TileSet[i, 0])
			{
				Tile* ptr2 = ptr;
				int num = 0;
				while (num < Main.MaxTilesY)
				{
					ptr2->CurrentFlags = ~(Tile.Flags.WALLFRAME_MASK | Tile.Flags.HIGHLIGHT_MASK | Tile.Flags.VISITED | Tile.Flags.WIRE | Tile.Flags.CHECKING_LIQUID | Tile.Flags.SKIP_LIQUID);
					ptr2->IsActive = fileIO.ReadByte();
					if (ptr2->IsActive != 0)
					{
						ptr2->Type = fileIO.ReadByte();
						if (ptr2->Type == 127)
						{
							ptr2->IsActive = 0;
						}
						if (Main.TileFrameImportant[ptr2->Type])
						{
							ptr2->FrameX = fileIO.ReadInt16();
							ptr2->FrameY = fileIO.ReadInt16();
							if (ptr2->Type == 144)
							{
								ptr2->FrameY = 0;
							}
						}
						else
						{
							ptr2->FrameX = -1;
							ptr2->FrameY = -1;
						}
					}
					ptr2->WallType = fileIO.ReadByte();
					ptr2->Liquid = fileIO.ReadByte();
					if (ptr2->Liquid > 0 && fileIO.ReadBoolean())
					{
						ptr2->Lava = 32;
					}
					Tile.Flags* ptr3 = &ptr2->CurrentFlags;
					*ptr3 |= (Tile.Flags)fileIO.ReadByte();
					if (Main.IsTutorial())
					{
						Tile.Flags* ptr4 = &ptr2->CurrentFlags;
						*ptr4 &= ~Tile.Flags.VISITED;
					}
					int num2 = fileIO.ReadByte();
					if ((num2 & 0x80) != 0)
					{
						num2 &= 0x7F;
						num2 |= fileIO.ReadByte() << 7;
					}
					num += num2;
					while (num2 > 0)
					{
						ptr2[1] = *ptr2;
						ptr2++;
						num2--;
					}
					num++;
					ptr2++;
				}
			}
		}
		for (int j = 0; j < 1000; j++)
		{
			if (!fileIO.ReadBoolean())
			{
				continue;
			}
			Main.ChestSet[j] = new Chest();
			Main.ChestSet[j].XPos = fileIO.ReadInt16();
			Main.ChestSet[j].YPos = fileIO.ReadInt16();
			for (int k = 0; k < 20; k++)
			{
				int num3 = 0;
				num3 = ((release <= 49) ? fileIO.ReadByte() : fileIO.ReadInt16());
				if (num3 > 0)
				{
					Main.ChestSet[j].ItemSet[k].NetDefaults(fileIO.ReadInt16(), num3);
					Main.ChestSet[j].ItemSet[k].SetPrefix(fileIO.ReadByte());
				}
			}
		}
		for (int l = 0; l < 1000; l++)
		{
			Main.SignSet[l].Read(fileIO, release);
		}
		bool flag = fileIO.ReadBoolean();
		int num4 = 0;
		while (flag)
		{
			int nPCType = fileIO.ReadByte();
			Main.NPCSet[num4].SetDefaults(nPCType);
			Main.NPCSet[num4].Position.X = fileIO.ReadSingle();
			Main.NPCSet[num4].Position.Y = fileIO.ReadSingle();
			Main.NPCSet[num4].XYWH.X = (int)Main.NPCSet[num4].Position.X;
			Main.NPCSet[num4].XYWH.Y = (int)Main.NPCSet[num4].Position.Y;
			Main.NPCSet[num4].IsHomeless = fileIO.ReadBoolean();
			Main.NPCSet[num4].HomeTileX = fileIO.ReadInt16();
			Main.NPCSet[num4].HomeTileY = fileIO.ReadInt16();
			num4++;
			flag = fileIO.ReadBoolean();
		}
		NPC.TypeNames[17] = fileIO.ReadString();
		NPC.TypeNames[18] = fileIO.ReadString();
		NPC.TypeNames[19] = fileIO.ReadString();
		NPC.TypeNames[20] = fileIO.ReadString();
		NPC.TypeNames[22] = fileIO.ReadString();
		NPC.TypeNames[54] = fileIO.ReadString();
		NPC.TypeNames[38] = fileIO.ReadString();
		NPC.TypeNames[107] = fileIO.ReadString();
		NPC.TypeNames[108] = fileIO.ReadString();
		NPC.TypeNames[124] = fileIO.ReadString();
	}

	public static void saveNewWorld()
	{
		if (saveLock)
		{
			return;
		}
		Terraria.Mods.ModLoader.WorldSaving();
		saveLock = true;
		if (hardLock)
		{
			UI.MainUI.statusText = Lang.WorldGenText[48];
			do
			{
				Thread.Sleep(16);
			}
			while (hardLock);
		}
		bool flag = false;
		lock (padlock)
		{
			UI.MainUI.FirstProgressStep(1, Lang.WorldGenText[49]);
			tempTime = Main.GameTime;
			if (Gen)
			{
				tempTime.reset(1f);
				Gen = false;
			}
			using (MemoryStream memoryStream = new MemoryStream(6291456))
			{
				using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
				binaryWriter.Write(49);
				binaryWriter.Write(0u);
				binaryWriter.Write(Main.WorldName);
				binaryWriter.Write(Main.WorldID);
				binaryWriter.Write(Main.WorldTimestamp);
				binaryWriter.Write(Main.RightWorld);
				binaryWriter.Write((short)Main.BottomWorld);
				binaryWriter.Write(Main.MaxTilesY);
				binaryWriter.Write(Main.MaxTilesX);
				binaryWriter.Write(Main.SpawnTileX);
				binaryWriter.Write(Main.SpawnTileY);
				binaryWriter.Write((short)Main.WorldSurface);
				binaryWriter.Write((short)Main.RockLayer);
				binaryWriter.Write(tempTime.WorldTime);
				binaryWriter.Write(tempTime.DayTime);
				binaryWriter.Write(tempTime.MoonPhase);
				binaryWriter.Write(tempTime.IsBloodMoon);
				binaryWriter.Write(Main.DungeonX);
				binaryWriter.Write(Main.DungeonY);
				binaryWriter.Write(NPC.HasDownedBoss1);
				binaryWriter.Write(NPC.HasDownedBoss2);
				binaryWriter.Write(NPC.HasDownedBoss3);
				binaryWriter.Write(NPC.HasSavedGoblin);
				binaryWriter.Write(NPC.HasSavedWizard);
				binaryWriter.Write(NPC.HasSavedMech);
				binaryWriter.Write(NPC.HasDownedGoblins);
				binaryWriter.Write(NPC.HasDownedClown);
				binaryWriter.Write(NPC.HasDownedFrost);
				binaryWriter.Write(HasShadowOrbSmashed);
				binaryWriter.Write(ToSpawnMeteor);
				binaryWriter.Write((byte)shadowOrbCount);
				binaryWriter.Write(altarCount);
				binaryWriter.Write(Main.InHardMode);
				binaryWriter.Write((byte)Main.InvasionDelay);
				binaryWriter.Write((short)Main.InvasionSize);
				binaryWriter.Write((byte)Main.InvasionType);
				binaryWriter.Write(Main.InvasionX);
				for (int i = 0; i < Main.MaxTilesX; i++)
				{
					if ((i & 0x1F) == 0)
					{
						UI.MainUI.Progress = (float)i / (float)Main.MaxTilesX;
					}
					int num;
					for (num = 0; num < Main.MaxTilesY; num++)
					{
						Tile tile = Main.TileSet[i, num];
						if (tile.Type == 127)
						{
							tile.IsActive = 0;
						}
						if (tile.IsActive != 0)
						{
							binaryWriter.Write(value: true);
							binaryWriter.Write(tile.Type);
							if (Main.TileFrameImportant[tile.Type])
							{
								binaryWriter.Write(tile.FrameX);
								binaryWriter.Write(tile.FrameY);
							}
						}
						else
						{
							binaryWriter.Write(value: false);
						}
						binaryWriter.Write(tile.WallType);
						binaryWriter.Write(tile.Liquid);
						if (tile.Liquid > 0)
						{
							binaryWriter.Write(tile.Lava != 0);
						}
						binaryWriter.Write((byte)(tile.CurrentFlags & (Tile.Flags.VISITED | Tile.Flags.WIRE)));
						int j;
						for (j = 1; num + j < Main.MaxTilesY && tile.isTheSameAs(ref Main.TileSet[i, num + j]); j++)
						{
						}
						j--;
						num += j;
						if (j < 128)
						{
							binaryWriter.Write((byte)j);
						}
						else
						{
							int num2 = (j & 0x7F) | 0x80;
							j >>= 7;
							binaryWriter.Write((ushort)(num2 | (j << 8)));
						}
					}
				}
				for (int k = 0; k < 1000; k++)
				{
					if (Main.ChestSet[k] == null)
					{
						binaryWriter.Write(value: false);
						continue;
					}
					Chest chest = Main.ChestSet[k];
					binaryWriter.Write(value: true);
					binaryWriter.Write(chest.XPos);
					binaryWriter.Write(chest.YPos);
					for (int l = 0; l < 20; l++)
					{
						if (chest.ItemSet[l].Type == 0)
						{
							chest.ItemSet[l].Stack = 0;
						}
						binaryWriter.Write((byte)chest.ItemSet[l].Stack);
						if (chest.ItemSet[l].Stack > 0)
						{
							binaryWriter.Write(chest.ItemSet[l].NetID);
							binaryWriter.Write(chest.ItemSet[l].PrefixType);
						}
					}
				}
				for (int m = 0; m < 1000; m++)
				{
					Sign sign = Main.SignSet[m];
					sign.Write(binaryWriter);
				}
				for (int n = 0; n < 196; n++)
				{
					NPC nPC = Main.NPCSet[n];
					if (nPC.IsTownNPC && nPC.Active != 0)
					{
						nPC = (NPC)nPC.Clone();
						binaryWriter.Write(value: true);
						binaryWriter.Write(nPC.Type);
						binaryWriter.Write(nPC.Position.X);
						binaryWriter.Write(nPC.Position.Y);
						binaryWriter.Write(nPC.IsHomeless);
						binaryWriter.Write(nPC.HomeTileX);
						binaryWriter.Write(nPC.HomeTileY);
					}
				}
				binaryWriter.Write(value: false);
				binaryWriter.Write(NPC.TypeNames[17]);
				binaryWriter.Write(NPC.TypeNames[18]);
				binaryWriter.Write(NPC.TypeNames[19]);
				binaryWriter.Write(NPC.TypeNames[20]);
				binaryWriter.Write(NPC.TypeNames[22]);
				binaryWriter.Write(NPC.TypeNames[54]);
				binaryWriter.Write(NPC.TypeNames[38]);
				binaryWriter.Write(NPC.TypeNames[107]);
				binaryWriter.Write(NPC.TypeNames[108]);
				binaryWriter.Write(NPC.TypeNames[124]);
				CRC32 cRC = new CRC32();
				cRC.Update(memoryStream.GetBuffer(), 8, (int)memoryStream.Length - 8);
				binaryWriter.Seek(4, SeekOrigin.Begin);
				binaryWriter.Write(cRC.GetValue());
				Main.ShowSaveIcon();
				try
				{
					if (UI.MainUI.TestStorageSpace("Worlds", WorldSelect.worldPathName, (int)memoryStream.Length))
					{
						using StorageContainer storageContainer = UI.MainUI.OpenPlayerStorage("Worlds");
						if (Main.IsGameStarted && storageContainer.FileExists(WorldSelect.worldPathName))
						{
							string file = WorldSelect.worldPathName + ".bak";
							using Stream stream = storageContainer.OpenFile(file, FileMode.Create);
							Stream stream2 = storageContainer.OpenFile(WorldSelect.worldPathName, FileMode.Open);
							stream2.CopyTo(stream);
							stream2.Close();
							stream.Close();
						}
						using Stream stream3 = storageContainer.OpenFile(WorldSelect.worldPathName, FileMode.Create);
						stream3.Write(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
						stream3.Close();
						flag = true;
						// A dated copy in Storage/Backups, so a corrupted save cannot take the
						// only world with it. Rate-limited inside (see [Backups] MinMinutes).
						WorldBackups.Backup(storageContainer.FilePath(WorldSelect.worldPathName), Main.WorldName);
					}
				}
				catch (IOException)
				{
					UI.MainUI.WriteError();
				}
				catch (Exception)
				{
				}
				binaryWriter.Close();
				Main.HideSaveIcon();
			}
			saveLock = false;
			if (!flag)
			{
				WorldSelect.LoadWorlds();
			}
		}
	}

	private static void resetGen()
	{
		mudWall = false;
		hellChest = 0;
		JungleX = 0;
		numMCaves = 0;
		numIslandHouses = 0;
		houseCount = 0;
		dEnteranceX = 0;
		numDRooms = 0;
		numDDoors = 0;
		numDPlats = 0;
		numJChests = 0;
	}

	public static bool placeTrap(int x2, int y2, int type = -1)
	{
		int num = y2;
		while (!SolidTileUnsafe(x2, num))
		{
			if (++num >= Main.MaxTilesY - 300)
			{
				return false;
			}
		}
		num--;
		if (Main.TileSet[x2, num].Liquid > 0 && Main.TileSet[x2, num].Lava != 0)
		{
			return false;
		}
		if (type == -1 && Main.Rand.Next(20) == 0)
		{
			type = 2;
		}
		else if (type == -1)
		{
			type = Main.Rand.Next(2);
		}
		if (Main.TileSet[x2, num].IsActive != 0 || Main.TileSet[x2 - 1, num].IsActive != 0 || Main.TileSet[x2 + 1, num].IsActive != 0 || Main.TileSet[x2, num - 1].IsActive != 0 || Main.TileSet[x2 - 1, num - 1].IsActive != 0 || Main.TileSet[x2 + 1, num - 1].IsActive != 0 || Main.TileSet[x2, num - 2].IsActive != 0 || Main.TileSet[x2 - 1, num - 2].IsActive != 0 || Main.TileSet[x2 + 1, num - 2].IsActive != 0)
		{
			return false;
		}
		if (Main.TileSet[x2, num + 1].Type == 48)
		{
			return false;
		}
		switch (type)
		{
		case 0:
		{
			int num7 = x2;
			int num8 = num;
			num8 -= genRand.Next(3);
			while (!SolidTileUnsafe(num7, num8))
			{
				if (--num7 < 0)
				{
					return false;
				}
			}
			int num9 = num7;
			num7 = x2;
			while (!SolidTileUnsafe(num7, num8))
			{
				if (++num7 >= Main.MaxTilesX)
				{
					return false;
				}
			}
			int num10 = num7;
			int num11 = x2 - num9;
			int num12 = num10 - x2;
			bool flag = num11 > 5 && num11 < 50;
			bool flag2 = num12 > 5 && num12 < 50;
			if (flag && !SolidTileUnsafe(num9, num8 + 1))
			{
				flag = false;
			}
			else if (flag && (Main.TileSet[num9, num8].Type == 10 || Main.TileSet[num9, num8].Type == 48 || Main.TileSet[num9, num8 + 1].Type == 10 || Main.TileSet[num9, num8 + 1].Type == 48))
			{
				flag = false;
			}
			if (flag2 && !SolidTileUnsafe(num10, num8 + 1))
			{
				flag2 = false;
			}
			else if (flag2 && (Main.TileSet[num10, num8].Type == 10 || Main.TileSet[num10, num8].Type == 48 || Main.TileSet[num10, num8 + 1].Type == 10 || Main.TileSet[num10, num8 + 1].Type == 48))
			{
				flag2 = false;
			}
			int num13 = 0;
			if (flag & flag2)
			{
				num13 = 1;
				num7 = num9;
				if (genRand.Next(2) == 0)
				{
					num7 = num10;
					num13 = -1;
				}
			}
			else if (flag2)
			{
				num7 = num10;
				num13 = -1;
			}
			else
			{
				if (!flag)
				{
					return false;
				}
				num7 = num9;
				num13 = 1;
			}
			PlaceTile(x2, num, 135, ToMute: true, IsForced: true, -1, (Main.TileSet[x2, num].WallType > 0) ? 2 : genRand.Next(2, 4));
			KillTile(num7, num8);
			PlaceTile(num7, num8, 137, ToMute: true, IsForced: true, -1, num13);
			int num14 = x2;
			int num15 = num;
			while (num14 != num7 || num15 != num8)
			{
				Main.TileSet[num14, num15].wire = 16;
				if (num14 > num7)
				{
					num14--;
				}
				if (num14 < num7)
				{
					num14++;
				}
				Main.TileSet[num14, num15].wire = 16;
				if (num15 > num8)
				{
					num15--;
				}
				if (num15 < num8)
				{
					num15++;
				}
				Main.TileSet[num14, num15].wire = 16;
			}
			return true;
		}
		case 1:
		{
			int num16 = x2;
			int num17 = num - 8;
			num16 += genRand.Next(-1, 2);
			bool flag3 = true;
			while (flag3)
			{
				bool flag4 = true;
				int num18 = 0;
				for (int l = num16 - 2; l <= num16 + 3; l++)
				{
					for (int m = num17; m <= num17 + 3; m++)
					{
						if (!SolidTileUnsafe(l, m))
						{
							flag4 = false;
						}
						if (Main.TileSet[l, m].IsActive != 0 && (Main.TileSet[l, m].Type == 0 || Main.TileSet[l, m].Type == 1 || Main.TileSet[l, m].Type == 59))
						{
							num18++;
						}
					}
				}
				num17--;
				if (num17 < Main.WorldSurface)
				{
					return false;
				}
				if (flag4 && num18 > 2)
				{
					flag3 = false;
				}
			}
			if (num - num17 <= 5 || num - num17 >= 40)
			{
				return false;
			}
			for (int n = num16; n <= num16 + 1; n++)
			{
				for (int num19 = num17; num19 <= num; num19++)
				{
					if (SolidTileUnsafe(n, num19))
					{
						KillTile(n, num19);
					}
				}
			}
			for (int num20 = num16 - 2; num20 <= num16 + 3; num20++)
			{
				for (int num21 = num17 - 2; num21 <= num17 + 3; num21++)
				{
					if (SolidTileUnsafe(num20, num21))
					{
						Main.TileSet[num20, num21].Type = 1;
					}
				}
			}
			PlaceTile(x2, num, 135, ToMute: true, IsForced: true, -1, genRand.Next(2, 4));
			PlaceTile(num16, num17 + 2, 130, ToMute: true);
			PlaceTile(num16 + 1, num17 + 2, 130, ToMute: true);
			PlaceTile(num16 + 1, num17 + 1, 138, ToMute: true);
			num17 += 2;
			Main.TileSet[num16, num17].wire = 16;
			Main.TileSet[num16 + 1, num17].wire = 16;
			num17++;
			PlaceTile(num16, num17, 130, ToMute: true);
			PlaceTile(num16 + 1, num17, 130, ToMute: true);
			Main.TileSet[num16, num17].wire = 16;
			Main.TileSet[num16 + 1, num17].wire = 16;
			PlaceTile(num16, num17 + 1, 130, ToMute: true);
			PlaceTile(num16 + 1, num17 + 1, 130, ToMute: true);
			Main.TileSet[num16, num17 + 1].wire = 16;
			Main.TileSet[num16 + 1, num17 + 1].wire = 16;
			int num22 = x2;
			int num23 = num;
			while (num22 != num16 || num23 != num17)
			{
				Main.TileSet[num22, num23].wire = 16;
				if (num22 > num16)
				{
					num22--;
				}
				if (num22 < num16)
				{
					num22++;
				}
				Main.TileSet[num22, num23].wire = 16;
				if (num23 > num17)
				{
					num23--;
				}
				if (num23 < num17)
				{
					num23++;
				}
				Main.TileSet[num22, num23].wire = 16;
			}
			return true;
		}
		case 2:
		{
			int num2 = Main.Rand.Next(4, 7);
			int num3 = x2;
			num3 += Main.Rand.Next(-1, 2);
			int num4 = num;
			for (int i = 0; i < num2; i++)
			{
				num4++;
				if (!SolidTileUnsafe(num3, num4))
				{
					return false;
				}
			}
			for (int j = num3 - 2; j <= num3 + 2; j++)
			{
				for (int k = num4 - 2; k <= num4 + 2; k++)
				{
					if (!SolidTileUnsafe(j, k))
					{
						return false;
					}
				}
			}
			KillTile(num3, num4);
			Main.TileSet[num3, num4].IsActive = 1;
			Main.TileSet[num3, num4].Type = 141;
			Main.TileSet[num3, num4].FrameX = 0;
			Main.TileSet[num3, num4].FrameY = (short)(18 * Main.Rand.Next(2));
			PlaceTile(x2, num, 135, ToMute: true, IsForced: true, -1, genRand.Next(2, 4));
			int num5 = x2;
			int num6 = num;
			while (num5 != num3 || num6 != num4)
			{
				Main.TileSet[num5, num6].wire = 16;
				if (num5 > num3)
				{
					num5--;
				}
				if (num5 < num3)
				{
					num5++;
				}
				Main.TileSet[num5, num6].wire = 16;
				if (num6 > num4)
				{
					num6--;
				}
				if (num6 < num4)
				{
					num6++;
				}
				Main.TileSet[num5, num6].wire = 16;
			}
			break;
		}
		}
		return false;
	}

	public unsafe static void generateWorld(int seed = -1)
	{
		Time.CheckXMas();
		NPC.ClearNames();
		NPC.SetNames();
		Gen = true;
		resetGen();
		UI.MainUI.FirstProgressStep(47, Lang.WorldGenText[0]);
		if (seed > 0)
		{
			genRand = new FastRandom((uint)seed);
		}
		Main.WorldID = genRand.Next();
		Main.WorldTimestamp = (int)(DateTime.UtcNow.Ticks / 10000000);
		int num = 0;
		int num2 = 0;
		float num3 = (float)Main.MaxTilesY * 0.3f;
		num3 *= (float)genRand.Next(90, 110) * 0.005f;
		float num4 = num3 + (float)Main.MaxTilesY * 0.2f;
		num4 *= (float)genRand.Next(90, 110) * 0.01f;
		float num5 = num3;
		float num6 = num3;
		float num7 = num4;
		float num8 = num4;
		int num9 = (genRand.Next(2) << 1) - 1;
		float num10 = 1f / (float)Main.MaxTilesX;
		fixed (Tile* tileSet = Main.TileSet)
		{
			for (int i = 0; i < Main.MaxTilesX; i++)
			{
				UI.MainUI.Progress = (float)i * num10;
				if (num3 < num5)
				{
					num5 = num3;
				}
				if (num3 > num6)
				{
					num6 = num3;
				}
				if (num4 < num7)
				{
					num7 = num4;
				}
				if (num4 > num8)
				{
					num8 = num4;
				}
				int num11 = num2 - 1;
				num2 = num11;
				if (num11 <= 0)
				{
					num = genRand.Next(5);
					num2 = genRand.Next(5, 40);
					if (num == 0)
					{
						num2 *= genRand.Next(1, 6);
					}
				}
				switch (num)
				{
				case 0:
					while (genRand.Next(7) == 0)
					{
						num3 += (float)genRand.Next(-1, 2);
					}
					break;
				case 1:
					while (genRand.Next(4) == 0)
					{
						num3--;
					}
					while (genRand.Next(10) == 0)
					{
						num3++;
					}
					break;
				case 2:
					while (genRand.Next(4) == 0)
					{
						num3++;
					}
					while (genRand.Next(10) == 0)
					{
						num3--;
					}
					break;
				case 3:
					while (genRand.Next(2) == 0)
					{
						num3--;
					}
					while (genRand.Next(6) == 0)
					{
						num3++;
					}
					break;
				default:
					while (genRand.Next(2) == 0)
					{
						num3++;
					}
					while (genRand.Next(5) == 0)
					{
						num3--;
					}
					break;
				}
				if (num3 < (float)Main.MaxTilesY * 0.17f)
				{
					num3 = (float)Main.MaxTilesY * 0.17f;
					num2 = 0;
				}
				else if (num3 > (float)Main.MaxTilesY * 0.3f)
				{
					num3 = (float)Main.MaxTilesY * 0.3f;
					num2 = 0;
				}
				if ((i < 275 || i > Main.MaxTilesX - 275) && num3 > (float)(Main.MaxTilesY >> 2))
				{
					num3 = Main.MaxTilesY >> 2;
					num2 = 1;
				}
				while (genRand.Next(3) == 0)
				{
					num4 += (float)genRand.Next(-2, 3);
				}
				if ((double)num4 < (double)num3 + (double)Main.MaxTilesY * 0.05)
				{
					num4++;
				}
				else if ((double)num4 > (double)num3 + (double)Main.MaxTilesY * 0.35)
				{
					num4--;
				}
				int num12 = Main.MaxTilesY - 1;
				Tile* ptr = tileSet + (i * 1440 + num12);
				do
				{
					ptr->IsActive = 1;
					ptr->Type = (byte)((num12 >= (int)num4) ? 1 : 0);
					ptr->FrameX = -1;
					ptr->FrameY = -1;
					ptr--;
					num11 = num12 - 1;
					num12 = num11;
				}
				while (num11 >= (int)num3);
				do
				{
					ptr->IsActive = 0;
					ptr->FrameX = -1;
					ptr->FrameY = -1;
					ptr--;
					num11 = num12 - 1;
					num12 = num11;
				}
				while (num11 >= 0);
			}
			Main.WorldSurface = (int)num6 + 25;
			Main.WorldSurfacePixels = Main.WorldSurface << 4;
			int num13 = (int)((num8 - (float)Main.WorldSurface) * (1f / 6f)) * 6;
			Main.RockLayer = Main.WorldSurface + num13;
			Main.RockLayerPixels = Main.RockLayer << 4;
			UpdateMagmaLayerPos();
			WaterLine = Main.RockLayer + Main.MaxTilesY >> 1;
			WaterLine += genRand.Next(-100, 20);
			lavaLine = WaterLine + genRand.Next(50, 80);
			int num14 = 0;
			Location[] array = new Location[10];
			for (int j = 0; j < (int)((float)Main.MaxTilesX * 0.0015f); j++)
			{
				int num15 = genRand.Next(450, Main.MaxTilesX - 450);
				int k = 0;
				for (int l = 0; l < 10; l++)
				{
					for (; Main.TileSet[num15, k].IsActive == 0; k++)
					{
					}
					array[l].X = (short)num15;
					array[l].Y = (short)(k - genRand.Next(11, 16));
					num15 += genRand.Next(5, 11);
				}
				for (int m = 0; m < 10; m++)
				{
					TileRunner(array[m].X, array[m].Y, genRand.Next(5, 8), genRand.Next(6, 9), 0, addTile: true, new Vector2(-2f, -0.3f));
					TileRunner(array[m].X, array[m].Y, genRand.Next(5, 8), genRand.Next(6, 9), 0, addTile: true, new Vector2(2f, -0.3f));
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[1]);
			int num16 = 2 + genRand.Next((int)((float)Main.MaxTilesX * 0.0008f), (int)((float)Main.MaxTilesX * 0.0025f));
			for (int n = 0; n < num16; n++)
			{
				int num17 = genRand.Next(Main.MaxTilesX);
				while ((float)num17 > (float)Main.MaxTilesX * 0.4f && (float)num17 < (float)Main.MaxTilesX * 0.6f)
				{
					num17 = genRand.Next(Main.MaxTilesX);
				}
				int num18 = genRand.Next(35, 90);
				if (n == 1)
				{
					float num19 = (float)Main.MaxTilesX / 4200f;
					num18 += (int)((float)genRand.Next(20, 40) * num19);
				}
				if (genRand.Next(3) == 0)
				{
					num18 *= 2;
				}
				if (n == 1)
				{
					num18 *= 2;
				}
				int num20 = num17 - num18;
				num18 = genRand.Next(35, 90);
				if (genRand.Next(3) == 0)
				{
					num18 *= 2;
				}
				if (n == 1)
				{
					num18 *= 2;
				}
				int num21 = num17 + num18;
				if (num20 < 0)
				{
					num20 = 0;
				}
				if (num21 > Main.MaxTilesX)
				{
					num21 = Main.MaxTilesX;
				}
				switch (n)
				{
				case 0:
					num20 = 0;
					num21 = genRand.Next(260, 300);
					if (num9 == 1)
					{
						num21 += 40;
					}
					break;
				case 2:
					num20 = Main.MaxTilesX - genRand.Next(260, 300);
					num21 = Main.MaxTilesX;
					if (num9 == -1)
					{
						num20 -= 40;
					}
					break;
				}
				int num22 = genRand.Next(50, 100);
				for (int num23 = num20; num23 < num21; num23++)
				{
					if (genRand.Next(2) == 0)
					{
						num22 += genRand.Next(-1, 2);
						if (num22 < 50)
						{
							num22 = 50;
						}
						if (num22 > 100)
						{
							num22 = 100;
						}
					}
					for (int num24 = 0; num24 < Main.WorldSurface; num24++)
					{
						if (Main.TileSet[num23, num24].IsActive == 0)
						{
							continue;
						}
						int num25 = num22;
						if (num23 - num20 < num25)
						{
							num25 = num23 - num20;
						}
						if (num21 - num23 < num25)
						{
							num25 = num21 - num23;
						}
						num25 += genRand.Next(5);
						for (int num26 = num24; num26 < num24 + num25; num26++)
						{
							if (num23 > num20 + genRand.Next(5) && num23 < num21 - genRand.Next(5))
							{
								Main.TileSet[num23, num26].Type = 53;
							}
						}
						break;
					}
				}
			}
			for (int num27 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 8E-06f); num27 > 0; num27--)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next(Main.WorldSurface, Main.RockLayer), genRand.Next(15, 70), genRand.Next(20, 130), 53);
			}
			numMCaves = 0;
			UI.MainUI.NextProgressStep(Lang.WorldGenText[2]);
			for (int num28 = 0; num28 < (int)((float)Main.MaxTilesX * 0.0008f); num28++)
			{
				int num29 = 0;
				bool flag = false;
				int num30 = genRand.Next((int)((float)Main.MaxTilesX * 0.25f), (int)((float)Main.MaxTilesX * 0.75f));
				bool flag2;
				do
				{
					flag2 = true;
					while (num30 > (Main.MaxTilesX >> 1) - 100 && num30 < (Main.MaxTilesX >> 1) + 100)
					{
						num30 = genRand.Next((int)((float)Main.MaxTilesX * 0.25f), (int)((float)Main.MaxTilesX * 0.75f));
					}
					for (int num31 = 0; num31 < numMCaves; num31++)
					{
						if (num30 > mCave[num31].X - 50 && num30 < mCave[num31].X + 50)
						{
							num29++;
							flag2 = false;
							break;
						}
					}
					if (num29 >= 200)
					{
						flag = true;
						break;
					}
				}
				while (!flag2);
				if (flag)
				{
					continue;
				}
				for (int num32 = 0; num32 < Main.WorldSurface; num32++)
				{
					if (Main.TileSet[num30, num32].IsActive != 0)
					{
						Mountinater(num30, num32);
						mCave[numMCaves].X = (short)num30;
						mCave[numMCaves].Y = (short)num32;
						numMCaves++;
						break;
					}
				}
			}
			bool flag3 = Time.xMas;
			if (genRand.Next(3) == 0)
			{
				flag3 = true;
			}
			if (flag3)
			{
				UI.MainUI.statusText = Lang.WorldGenText[56];
				int num33 = genRand.Next(Main.MaxTilesX / 3, (Main.MaxTilesX << 1) / 3);
				int num34 = genRand.Next(35, 90);
				float num35 = (float)Main.MaxTilesX / 2100f;
				num34 += (int)((float)genRand.Next(20, 40) * num35);
				int num36 = num33 - num34;
				if (num36 < 0)
				{
					num36 = 0;
				}
				num34 = genRand.Next(35, 90);
				num34 += (int)((float)genRand.Next(20, 40) * num35);
				int num37 = num33 + num34;
				if (num37 > Main.MaxTilesX)
				{
					num37 = Main.MaxTilesX;
				}
				int num38 = genRand.Next(50, 100);
				for (int num39 = num36; num39 < num37; num39++)
				{
					if (genRand.Next(2) == 0)
					{
						num38 += genRand.Next(-1, 2);
						if (num38 < 50)
						{
							num38 = 50;
						}
						if (num38 > 100)
						{
							num38 = 100;
						}
					}
					for (int num40 = 0; num40 < Main.WorldSurface; num40++)
					{
						if (Main.TileSet[num39, num40].IsActive == 0)
						{
							continue;
						}
						int num41 = num38;
						if (num39 - num36 < num41)
						{
							num41 = num39 - num36;
						}
						if (num37 - num39 < num41)
						{
							num41 = num37 - num39;
						}
						num41 += genRand.Next(5);
						for (int num42 = num40; num42 < num40 + num41; num42++)
						{
							if (num39 > num36 + genRand.Next(5) && num39 < num37 - genRand.Next(5))
							{
								Main.TileSet[num39, num42].Type = 147;
							}
						}
						break;
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[3]);
			for (int num43 = 1; num43 < Main.MaxTilesX - 1; num43++)
			{
				UI.MainUI.Progress = (float)num43 / (float)Main.MaxTilesX;
				bool flag4 = false;
				num14 += genRand.Next(-1, 2);
				if (num14 < 0)
				{
					num14 = 0;
				}
				if (num14 > 10)
				{
					num14 = 10;
				}
				for (int num44 = 0; num44 < Main.WorldSurface + 10 && num44 <= Main.WorldSurface + num14; num44++)
				{
					if (flag4)
					{
						Main.TileSet[num43, num44].WallType = 2;
					}
					if (Main.TileSet[num43, num44].IsActive != 0 && Main.TileSet[num43 - 1, num44].IsActive != 0 && Main.TileSet[num43 + 1, num44].IsActive != 0 && Main.TileSet[num43, num44 + 1].IsActive != 0 && Main.TileSet[num43 - 1, num44 + 1].IsActive != 0 && Main.TileSet[num43 + 1, num44 + 1].IsActive != 0)
					{
						flag4 = true;
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[4]);
			for (int num45 = 0; num45 < (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.0002f); num45++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num5 + 1), genRand.Next(4, 15), genRand.Next(5, 40), 1);
			}
			for (int num46 = 0; num46 < (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.0002f); num46++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num5, (int)num6 + 1), genRand.Next(4, 10), genRand.Next(5, 30), 1);
			}
			for (int num47 = 0; num47 < (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.0045f); num47++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num6, (int)num8 + 1), genRand.Next(2, 7), genRand.Next(2, 23), 1);
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[5]);
			for (int num48 = 0; num48 < (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.005f); num48++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num7, Main.MaxTilesY), genRand.Next(2, 6), genRand.Next(2, 40), 0);
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[6]);
			for (int num49 = 0; num49 < (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 2E-05f); num49++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num5), genRand.Next(4, 14), genRand.Next(10, 50), 40);
			}
			for (int num50 = 0; num50 < (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 5E-05f); num50++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num5, (int)num6 + 1), genRand.Next(8, 14), genRand.Next(15, 45), 40);
			}
			for (int num51 = 0; num51 < (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 2E-05f); num51++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num6, (int)num8 + 1), genRand.Next(8, 15), genRand.Next(5, 50), 40);
			}
			for (int num52 = 5; num52 < Main.MaxTilesX - 5; num52++)
			{
				for (int num53 = 1; num53 < Main.WorldSurface - 1; num53++)
				{
					if (Main.TileSet[num52, num53].IsActive == 0)
					{
						continue;
					}
					for (int num54 = num53; num54 < num53 + 5; num54++)
					{
						if (Main.TileSet[num52, num54].Type == 40)
						{
							Main.TileSet[num52, num54].Type = 0;
						}
					}
					break;
				}
			}
			int num55 = 0;
			UI.MainUI.NextProgressStep(Lang.WorldGenText[7]);
			int num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.0015f);
			for (int num57 = 0; num57 < num56; num57++)
			{
				UI.MainUI.Progress = (float)num57 / (float)num56;
				int type = -1;
				if (genRand.Next(5) == 0)
				{
					type = -2;
				}
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num6, Main.MaxTilesY), genRand.Next(2, 5), genRand.Next(2, 20), type);
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num6, Main.MaxTilesY), genRand.Next(8, 15), genRand.Next(7, 30), type);
			}
			if (num8 <= (float)Main.MaxTilesY)
			{
				UI.MainUI.NextProgressStep(Lang.WorldGenText[8]);
				num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 3E-05f);
				for (int num58 = 0; num58 < num56; num58++)
				{
					UI.MainUI.Progress = (float)num58 / (float)num56;
					int type2 = -1;
					if (genRand.Next(6) == 0)
					{
						type2 = -2;
					}
					TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num5, (int)num8 + 1), genRand.Next(5, 15), genRand.Next(30, 200), type2);
				}
			}
			if (num8 <= (float)Main.MaxTilesY)
			{
				UI.MainUI.NextProgressStep(Lang.WorldGenText[9]);
				num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.00013f);
				for (int num59 = 0; num59 < num56; num59++)
				{
					UI.MainUI.Progress = (float)num59 / (float)num56;
					int type3 = -1;
					if (genRand.Next(10) == 0)
					{
						type3 = -2;
					}
					TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num8, Main.MaxTilesY), genRand.Next(6, 20), genRand.Next(50, 300), type3);
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[10]);
			num56 = (int)((float)Main.MaxTilesX * 0.0025f);
			for (int num60 = 0; num60 < num56; num60++)
			{
				num55 = genRand.Next(Main.MaxTilesX);
				for (int num61 = 0; (float)num61 < num6; num61++)
				{
					if (Main.TileSet[num55, num61].IsActive != 0)
					{
						TileRunner(num55, num61, genRand.Next(3, 6), genRand.Next(5, 50), -1, addTile: false, new Vector2((float)genRand.Next(-10, 11) * 0.1f, 1f));
						break;
					}
				}
			}
			num56 = (int)((float)Main.MaxTilesX * 0.0007f);
			for (int num62 = 0; num62 < num56; num62++)
			{
				num55 = genRand.Next(Main.MaxTilesX);
				for (int num63 = 0; (float)num63 < num6; num63++)
				{
					if (Main.TileSet[num55, num63].IsActive != 0)
					{
						TileRunner(num55, num63, genRand.Next(10, 15), genRand.Next(50, 130), -1, addTile: false, new Vector2((float)genRand.Next(-10, 11) * 0.1f, 2f));
						break;
					}
				}
			}
			num56 = (int)((float)Main.MaxTilesX * 0.0003f);
			for (int num64 = 0; num64 < num56; num64++)
			{
				num55 = genRand.Next(Main.MaxTilesX);
				for (int num65 = 0; (float)num65 < num6; num65++)
				{
					if (Main.TileSet[num55, num65].IsActive != 0)
					{
						TileRunner(num55, num65, genRand.Next(12, 25), genRand.Next(150, 500), -1, addTile: false, new Vector2((float)genRand.Next(-10, 11) * 0.1f, 4f));
						TileRunner(num55, num65, genRand.Next(8, 17), genRand.Next(60, 200), -1, addTile: false, new Vector2((float)genRand.Next(-10, 11) * 0.1f, 2f));
						TileRunner(num55, num65, genRand.Next(5, 13), genRand.Next(40, 170), -1, addTile: false, new Vector2((float)genRand.Next(-10, 11) * 0.1f, 2f));
						break;
					}
				}
			}
			num56 = (int)((float)Main.MaxTilesX * 0.0004f);
			for (int num66 = 0; num66 < num56; num66++)
			{
				num55 = genRand.Next(Main.MaxTilesX);
				for (int num67 = 0; (float)num67 < num6; num67++)
				{
					if (Main.TileSet[num55, num67].IsActive != 0)
					{
						TileRunner(num55, num67, genRand.Next(7, 12), genRand.Next(150, 250), -1, addTile: false, new Vector2(0f, 1f), noYChange: true);
						break;
					}
				}
			}
			num56 = (int)((float)Main.MaxTilesX * 0.0011904762f);
			for (int num68 = 0; num68 < num56; num68++)
			{
				Caverer(genRand.Next(100, Main.MaxTilesX - 100), genRand.Next(Main.RockLayer, Main.MaxTilesY - 400));
			}
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.002f);
			for (int num69 = 0; num69 < num56; num69++)
			{
				int num70 = genRand.Next(1, Main.MaxTilesX - 1);
				int num71 = genRand.Next((int)num5, (int)num6);
				if (num71 >= Main.MaxTilesY)
				{
					num71 = Main.MaxTilesY - 2;
				}
				if (Main.TileSet[num70 - 1, num71].IsActive != 0 && Main.TileSet[num70 - 1, num71].Type == 0 && Main.TileSet[num70 + 1, num71].IsActive != 0 && Main.TileSet[num70 + 1, num71].Type == 0 && Main.TileSet[num70, num71 - 1].IsActive != 0 && Main.TileSet[num70, num71 - 1].Type == 0 && Main.TileSet[num70, num71 + 1].IsActive != 0 && Main.TileSet[num70, num71 + 1].Type == 0)
				{
					Main.TileSet[num70, num71].IsActive = 1;
					Main.TileSet[num70, num71].Type = 2;
				}
				num70 = genRand.Next(1, Main.MaxTilesX - 1);
				num71 = genRand.Next((int)num5);
				if (num71 >= Main.MaxTilesY)
				{
					num71 = Main.MaxTilesY - 2;
				}
				if (Main.TileSet[num70 - 1, num71].IsActive != 0 && Main.TileSet[num70 - 1, num71].Type == 0 && Main.TileSet[num70 + 1, num71].IsActive != 0 && Main.TileSet[num70 + 1, num71].Type == 0 && Main.TileSet[num70, num71 - 1].IsActive != 0 && Main.TileSet[num70, num71 - 1].Type == 0 && Main.TileSet[num70, num71 + 1].IsActive != 0 && Main.TileSet[num70, num71 + 1].Type == 0)
				{
					Main.TileSet[num70, num71].IsActive = 1;
					Main.TileSet[num70, num71].Type = 2;
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[11]);
			int num72 = 0;
			float num73 = (float)genRand.Next(15, 30) * 0.01f;
			if (num9 == -1)
			{
				num73 = 1f - num73;
			}
			num72 = (int)((float)Main.MaxTilesX * num73);
			int num74 = Main.MaxTilesY + Main.RockLayer >> 1;
			float num75 = (float)Main.MaxTilesX * 0.00035714285f;
			num72 += genRand.Next((int)(-100f * num75), (int)(101f * num75));
			num74 += genRand.Next((int)(-100f * num75), (int)(101f * num75));
			int num76 = num72;
			int num77 = num74;
			TileRunner(num72, num74, genRand.Next((int)(250f * num75), (int)(500f * num75)), genRand.Next(50, 150), 59, addTile: false, new Vector2(num9 * 3, 0f));
			for (int num78 = (int)(6f * num75); num78 > 0; num78--)
			{
				TileRunner(num72 + genRand.Next(-(int)(125f * num75), (int)(125f * num75)), num74 + genRand.Next(-(int)(125f * num75), (int)(125f * num75)), genRand.Next(3, 7), genRand.Next(3, 8), genRand.Next(63, 65));
			}
			UI.MainUI.Progress = 0.15f;
			num72 += genRand.Next((int)(-250f * num75), (int)(251f * num75));
			num74 += genRand.Next((int)(-150f * num75), (int)(151f * num75));
			int num79 = num72;
			int num80 = num74;
			int num81 = num72;
			int num82 = num74;
			mudWall = true;
			TileRunner(num72, num74, genRand.Next((int)(250f * num75), (int)(500f * num75)), genRand.Next(50, 150), 59);
			mudWall = false;
			for (int num83 = (int)(6f * num75); num83 > 0; num83--)
			{
				TileRunner(num72 + genRand.Next(-(int)(125f * num75), (int)(125f * num75)), num74 + genRand.Next(-(int)(125f * num75), (int)(125f * num75)), genRand.Next(3, 7), genRand.Next(3, 8), genRand.Next(65, 67));
			}
			mudWall = true;
			UI.MainUI.Progress = 0.3f;
			num72 += genRand.Next((int)(-400f * num75), (int)(401f * num75));
			num74 += genRand.Next((int)(-150f * num75), (int)(151f * num75));
			int num84 = num72;
			int num85 = num74;
			TileRunner(num72, num74, genRand.Next((int)(250f * num75), (int)(500f * num75)), genRand.Next(50, 150), 59, addTile: false, new Vector2(num9 * -3, 0f));
			mudWall = false;
			for (int num86 = (int)(6f * num75); num86 > 0; num86--)
			{
				TileRunner(num72 + genRand.Next(-(int)(125f * num75), (int)(125f * num75)), num74 + genRand.Next(-(int)(125f * num75), (int)(125f * num75)), genRand.Next(3, 7), genRand.Next(3, 8), genRand.Next(67, 69));
			}
			mudWall = true;
			UI.MainUI.Progress = 0.45f;
			num72 = (num76 + num79 + num84) / 3;
			num74 = (num77 + num80 + num85) / 3;
			TileRunner(num72, num74, genRand.Next((int)(400f * num75), (int)(600f * num75)), 10000, 59, addTile: false, new Vector2(0f, -20f), noYChange: true);
			JungleRunner(num72, num74);
			UI.MainUI.Progress = 0.6f;
			mudWall = false;
			List<uint> list = new List<uint>();
			int num87 = 0;
			for (int num88 = 20; num88 < Main.MaxTilesX - 20; num88++)
			{
				for (int rockLayer = Main.RockLayer; rockLayer < Main.MaxTilesY - 200; rockLayer++)
				{
					if (Main.TileSet[num88, rockLayer].WallType == 15)
					{
						list.Add((uint)((num72 << 16) | num74));
						num87++;
					}
				}
			}
			int num89 = 0;
			while (num87 > 0 && num89 < Main.MaxTilesX / 10)
			{
				int index = genRand.Next(num87--);
				num72 = (int)(list[index] >> 16);
				num74 = (int)(list[index] & 0xFFFF);
				list.RemoveAt(index);
				MudWallRunner(num72, num74);
				num89++;
			}
			num72 = num81;
			num74 = num82;
			num56 = (int)(20f * num75);
			for (int num90 = 0; num90 <= num56; num90++)
			{
				UI.MainUI.Progress = 0.6f + 0.2f * ((float)num90 / (float)num56);
				num72 += genRand.Next((int)(-5f * num75), (int)(6f * num75));
				num74 += genRand.Next((int)(-5f * num75), (int)(6f * num75));
				TileRunner(num72, num74, genRand.Next(40, 100), genRand.Next(300, 500), 59);
			}
			num56 = (int)(10f * num75);
			for (int num91 = 0; num91 <= num56; num91++)
			{
				UI.MainUI.Progress = 0.8f + 0.2f * ((float)num91 / (float)num56);
				num72 = num81 + genRand.Next((int)(-600f * num75), (int)(600f * num75));
				num74 = num82 + genRand.Next((int)(-200f * num75), (int)(200f * num75));
				while (num72 < 1 || num72 >= Main.MaxTilesX - 1 || num74 < 1 || num74 >= Main.MaxTilesY - 1 || Main.TileSet[num72, num74].Type != 59)
				{
					num72 = num81 + genRand.Next((int)(-600f * num75), (int)(600f * num75));
					num74 = num82 + genRand.Next((int)(-200f * num75), (int)(200f * num75));
				}
				for (int num92 = 0; (float)num92 < 8f * num75; num92++)
				{
					num72 += genRand.Next(-30, 31);
					num74 += genRand.Next(-30, 31);
					int type4 = -1;
					if (genRand.Next(7) == 0)
					{
						type4 = -2;
					}
					TileRunner(num72, num74, genRand.Next(10, 20), genRand.Next(30, 70), type4);
				}
			}
			for (int num93 = 0; (float)num93 <= 300f * num75; num93++)
			{
				num72 = num81 + genRand.Next((int)(-600f * num75), (int)(600f * num75));
				num74 = num82 + genRand.Next((int)(-200f * num75), (int)(200f * num75));
				while (num72 < 1 || num72 >= Main.MaxTilesX - 1 || num74 < 1 || num74 >= Main.MaxTilesY - 1 || Main.TileSet[num72, num74].Type != 59)
				{
					num72 = num81 + genRand.Next((int)(-600f * num75), (int)(600f * num75));
					num74 = num82 + genRand.Next((int)(-200f * num75), (int)(200f * num75));
				}
				TileRunner(num72, num74, genRand.Next(4, 10), genRand.Next(5, 30), 1);
				if (genRand.Next(4) == 0)
				{
					int type5 = genRand.Next(63, 69);
					TileRunner(num72 + genRand.Next(-1, 2), num74 + genRand.Next(-1, 2), genRand.Next(3, 7), genRand.Next(4, 8), type5);
				}
			}
			for (int num94 = (int)((float)(genRand.Next(6, 10) * Main.MaxTilesX) / 4200f) - 1; num94 >= 0; num94--)
			{
				do
				{
					num72 = genRand.Next(20, Main.MaxTilesX - 20);
					num74 = genRand.Next(Main.WorldSurface + Main.RockLayer >> 1, Main.MaxTilesY - 300);
				}
				while (Main.TileSet[num72, num74].Type != 59);
				int num95 = genRand.Next(2, 4);
				int num96 = genRand.Next(2, 4);
				for (int num97 = num72 - num95 - 1; num97 <= num72 + num95 + 1; num97++)
				{
					for (int num98 = num74 - num96 - 1; num98 <= num74 + num96 + 1; num98++)
					{
						Main.TileSet[num97, num98].IsActive = 1;
						Main.TileSet[num97, num98].Type = 45;
						Main.TileSet[num97, num98].Liquid = 0;
						Main.TileSet[num97, num98].Lava = 0;
					}
				}
				for (int num99 = num72 - num95; num99 <= num72 + num95; num99++)
				{
					for (int num100 = num74 - num96; num100 <= num74 + num96; num100++)
					{
						Main.TileSet[num99, num100].IsActive = 0;
					}
				}
				int num101 = 0;
				int num11;
				do
				{
					int i2 = genRand.Next(num72 - num95, num72 + num95 + 1);
					int j2 = genRand.Next(num74 - num96, num74 + num96 - 2);
					if (PlaceTile(i2, j2, 4, ToMute: true))
					{
						break;
					}
					num11 = num101 + 1;
					num101 = num11;
				}
				while (num11 < 100);
				for (int num102 = num74 + num96 - 2; num102 <= num74 + num96 - 1; num102++)
				{
					for (int num103 = num72 - num95 - 1; num103 <= num72 + num95 + 1; num103++)
					{
						Main.TileSet[num103, num102].IsActive = 0;
					}
				}
				for (int num104 = num72 - num95 - 1; num104 <= num72 + num95 + 1; num104++)
				{
					int num105 = 4;
					int num106 = num74 + num96 + 2;
					while (Main.TileSet[num104, num106].IsActive == 0 && num106 < Main.MaxTilesY && num105 > 0)
					{
						Main.TileSet[num104, num106].IsActive = 1;
						Main.TileSet[num104, num106].Type = 59;
						num106++;
						num105--;
					}
				}
				num95 -= genRand.Next(1, 3);
				int num107 = num74 - num96 - 2;
				while (num95 >= 0)
				{
					for (int num108 = num72 - num95 - 1; num108 <= num72 + num95 + 1; num108++)
					{
						Main.TileSet[num108, num107].IsActive = 1;
						Main.TileSet[num108, num107].Type = 45;
					}
					num95 -= genRand.Next(1, 3);
					num107--;
				}
				JChest[numJChests].X = (short)num72;
				JChest[numJChests].Y = (short)num74;
				numJChests++;
			}
			for (int num109 = 0; num109 < Main.MaxTilesY; num109++)
			{
				for (int num110 = 0; num110 < Main.MaxTilesX; num110++)
				{
					if (Main.TileSet[num110, num109].IsActive != 0)
					{
						try
						{
							grassSpread = 0;
							SpreadGrass(num110, num109, 59, 60);
						}
						catch
						{
							grassSpread = 0;
							SpreadGrass(num110, num109, 59, 60, repeat: false);
						}
					}
				}
			}
			numIslandHouses = 0;
			houseCount = 0;
			UI.MainUI.NextProgressStep(Lang.WorldGenText[12]);
			num56 = (int)((double)Main.MaxTilesX * 0.0008);
			for (int num111 = 0; num111 < num56; num111++)
			{
				int num112 = 0;
				bool flag5 = false;
				int num113 = genRand.Next((int)((double)Main.MaxTilesX * 0.1), (int)((double)Main.MaxTilesX * 0.9));
				bool flag6;
				do
				{
					flag6 = true;
					while (num113 > (Main.MaxTilesX >> 1) - 80 && num113 < (Main.MaxTilesX >> 1) + 80)
					{
						num113 = genRand.Next((int)((double)Main.MaxTilesX * 0.1), (int)((double)Main.MaxTilesX * 0.9));
					}
					for (int num114 = 0; num114 < numIslandHouses; num114++)
					{
						if (num113 > fih[num114].X - 80 && num113 < fih[num114].X + 80)
						{
							num112++;
							flag6 = false;
							break;
						}
					}
					if (num112 >= 200)
					{
						flag5 = true;
						break;
					}
				}
				while (!flag6);
				if (flag5)
				{
					continue;
				}
				for (int num115 = 200; num115 < Main.WorldSurface; num115++)
				{
					if (Main.TileSet[num113, num115].IsActive != 0)
					{
						int num116 = num113;
						int num117;
						for (num117 = genRand.Next(90, num115 - 100); (float)num117 > num5 - 50f; num117--)
						{
						}
						FloatingIsland(num116, num117);
						fih[numIslandHouses].X = (short)num116;
						fih[numIslandHouses].Y = (short)num117;
						numIslandHouses++;
						break;
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[13]);
			for (int num118 = Main.MaxTilesX / 500 - 1; num118 >= 0; num118--)
			{
				int i3 = genRand.Next((int)((float)Main.MaxTilesX * 0.3f), (int)((float)Main.MaxTilesX * 0.7f));
				int j3 = genRand.Next(Main.RockLayer, Main.MaxTilesY - 249);
				ShroomPatch(i3, j3);
			}
			for (int num119 = 0; num119 < Main.MaxTilesX; num119++)
			{
				for (int worldSurface = Main.WorldSurface; worldSurface < Main.MaxTilesY; worldSurface++)
				{
					if (Main.TileSet[num119, worldSurface].IsActive != 0)
					{
						grassSpread = 0;
						SpreadGrass(num119, worldSurface, 59, 70, repeat: false);
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[14]);
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.001f);
			for (int num120 = 0; num120 < num56; num120++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num7, Main.MaxTilesY), genRand.Next(2, 6), genRand.Next(2, 40), 59);
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[15]);
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.0001f);
			for (int num121 = 0; num121 < num56; num121++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num8, Main.MaxTilesY), genRand.Next(5, 12), genRand.Next(15, 50), 123);
			}
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.0005f);
			for (int num122 = 0; num122 < num56; num122++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num8, Main.MaxTilesY), genRand.Next(2, 5), genRand.Next(2, 5), 123);
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[16]);
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 6E-05f);
			for (int num123 = 0; num123 < num56; num123++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num5, (int)num6), genRand.Next(3, 6), genRand.Next(2, 6), 7);
			}
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 8E-05f);
			for (int num124 = 0; num124 < num56; num124++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num6, (int)num8), genRand.Next(3, 7), genRand.Next(3, 7), 7);
			}
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.0002f);
			for (int num125 = 0; num125 < num56; num125++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num7, Main.MaxTilesY), genRand.Next(4, 9), genRand.Next(4, 8), 7);
			}
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 3E-05f);
			for (int num126 = 0; num126 < num56; num126++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num5, (int)num6), genRand.Next(3, 7), genRand.Next(2, 5), 6);
			}
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 8E-05f);
			for (int num127 = 0; num127 < num56; num127++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num6, (int)num8), genRand.Next(3, 6), genRand.Next(3, 6), 6);
			}
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.0002f);
			for (int num128 = 0; num128 < num56; num128++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num7, Main.MaxTilesY), genRand.Next(4, 9), genRand.Next(4, 8), 6);
			}
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 2.6E-05f);
			for (int num129 = 0; num129 < num56; num129++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num6, (int)num8), genRand.Next(3, 6), genRand.Next(3, 6), 9);
			}
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.00015f);
			for (int num130 = 0; num130 < num56; num130++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num7, Main.MaxTilesY), genRand.Next(4, 9), genRand.Next(4, 8), 9);
			}
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.00017f);
			for (int num131 = 0; num131 < num56; num131++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num5), genRand.Next(4, 9), genRand.Next(4, 8), 9);
			}
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.00012f);
			for (int num132 = 0; num132 < num56; num132++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num7, Main.MaxTilesY), genRand.Next(4, 8), genRand.Next(4, 8), 8);
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num5 - 20), genRand.Next(4, 8), genRand.Next(4, 8), 8);
			}
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 2E-05f);
			for (int num133 = 0; num133 < num56; num133++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next((int)num7, Main.MaxTilesY), genRand.Next(2, 4), genRand.Next(3, 6), 22);
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[17]);
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.0006f);
			for (int num134 = 0; num134 < num56; num134++)
			{
				int num135 = genRand.Next(20, Main.MaxTilesX - 20);
				int num136 = genRand.Next((int)num5, Main.MaxTilesY - 20);
				if (num134 < numMCaves)
				{
					num135 = mCave[num134].X;
					num136 = mCave[num134].Y;
				}
				if (Main.TileSet[num135, num136].IsActive == 0 && (num136 > Main.WorldSurface || Main.TileSet[num135, num136].WallType > 0))
				{
					for (; Main.TileSet[num135, num136].IsActive == 0 && num136 > (int)num5; num136--)
					{
					}
					num136++;
					int num137 = 1;
					if (genRand.Next(2) == 0)
					{
						num137 = -1;
					}
					for (; Main.TileSet[num135, num136].IsActive == 0 && num135 > 10 && num135 < Main.MaxTilesX - 10; num135 += num137)
					{
					}
					num135 -= num137;
					if (num136 > Main.WorldSurface || Main.TileSet[num135, num136].WallType > 0)
					{
						TileRunner(num135, num136, genRand.Next(4, 11), genRand.Next(2, 4), 51, addTile: true, new Vector2(num137, -1f), noYChange: false, overRide: false);
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[18]);
			int num138 = Main.MaxTilesY - genRand.Next(150, 190);
			for (int num139 = 0; num139 < Main.MaxTilesX; num139++)
			{
				UI.MainUI.Progress = (float)num139 * 0.2f / (float)Main.MaxTilesX;
				num138 += genRand.Next(-3, 4);
				if (num138 < Main.MaxTilesY - 190)
				{
					num138 = Main.MaxTilesY - 190;
				}
				else if (num138 > Main.MaxTilesY - 160)
				{
					num138 = Main.MaxTilesY - 160;
				}
				int num140 = num138 - 20 - genRand.Next(3);
				Tile* ptr2 = tileSet + (num139 * 1440 + num140);
				int num11;
				do
				{
					if (num140 >= num138)
					{
						ptr2->IsActive = 0;
						ptr2->Lava = 0;
						ptr2->Liquid = 0;
					}
					else
					{
						ptr2->Type = 57;
					}
					ptr2++;
					num11 = num140 + 1;
					num140 = num11;
				}
				while (num11 < Main.MaxTilesY);
			}
			int num141 = Main.MaxTilesY - genRand.Next(40, 70);
			for (int num142 = 10; num142 < Main.MaxTilesX - 10; num142++)
			{
				num141 += genRand.Next(-10, 11);
				if (num141 > Main.MaxTilesY - 60)
				{
					num141 = Main.MaxTilesY - 60;
				}
				else if (num141 < Main.MaxTilesY - 100)
				{
					num141 = Main.MaxTilesY - 120;
				}
				int num143 = num141;
				Tile* ptr3 = tileSet + (num142 * 1440 + num143);
				int num11;
				do
				{
					if (ptr3->IsActive == 0)
					{
						ptr3->Lava = 32;
						ptr3->Liquid = byte.MaxValue;
					}
					num11 = num143 + 1;
					num143 = num11;
				}
				while (num11 < Main.MaxTilesY - 10);
			}
			for (int num144 = 0; num144 < Main.MaxTilesX; num144++)
			{
				if (genRand.Next(50) == 0)
				{
					int num145;
					for (num145 = Main.MaxTilesY - 65; Main.TileSet[num144, num145].IsActive == 0 && num145 > Main.MaxTilesY - 135; num145--)
					{
					}
					TileRunner(genRand.Next(Main.MaxTilesX), num145 + genRand.Next(20, 50), genRand.Next(15, 20), 1000, 57, addTile: true, new Vector2(0f, genRand.Next(1, 3)), noYChange: true);
					UI.MainUI.Progress = 0.2f + (float)num144 * 0.05f / (float)Main.MaxTilesX;
				}
			}
			Liquid.QuickWater(0.25, 3, -1, 0.25);
			for (int num146 = 0; num146 < Main.MaxTilesX; num146++)
			{
				if (genRand.Next(13) == 0)
				{
					int num147;
					for (num147 = Main.MaxTilesY - 65; (Main.TileSet[num146, num147].Liquid > 0 || Main.TileSet[num146, num147].IsActive != 0) && num147 > Main.MaxTilesY - 140; num147--)
					{
					}
					TileRunner(num146, num147 - genRand.Next(2, 5), genRand.Next(5, 30), 1000, 57, addTile: true, new Vector2(0f, genRand.Next(1, 3)), noYChange: true);
					int num148 = genRand.Next(1, 3);
					if (genRand.Next(3) == 0)
					{
						num148 >>= 1;
					}
					if (genRand.Next(2) == 0)
					{
						TileRunner(num146, num147 - genRand.Next(2, 5), genRand.Next(5, 15) * num148, genRand.Next(10, 15) * num148, 57, addTile: true, new Vector2(1f, 0.3f));
					}
					if (genRand.Next(2) == 0)
					{
						num148 = genRand.Next(1, 3);
						TileRunner(num146, num147 - genRand.Next(2, 5), genRand.Next(5, 15) * num148, genRand.Next(10, 15) * num148, 57, addTile: true, new Vector2(-1f, 0.3f));
					}
					TileRunner(num146 + genRand.Next(-10, 10), num147 + genRand.Next(-10, 10), genRand.Next(5, 15), genRand.Next(5, 10), -2, addTile: false, new Vector2(genRand.Next(-1, 3), genRand.Next(-1, 3)));
					if (genRand.Next(3) == 0)
					{
						TileRunner(num146 + genRand.Next(-10, 10), num147 + genRand.Next(-10, 10), genRand.Next(10, 30), genRand.Next(10, 20), -2, addTile: false, new Vector2(genRand.Next(-1, 3), genRand.Next(-1, 3)));
					}
					if (genRand.Next(5) == 0)
					{
						TileRunner(num146 + genRand.Next(-15, 15), num147 + genRand.Next(-15, 10), genRand.Next(15, 30), genRand.Next(5, 20), -2, addTile: false, new Vector2(genRand.Next(-1, 3), genRand.Next(-1, 3)));
					}
					UI.MainUI.Progress = 0.5f + (float)num146 * 0.4f / (float)Main.MaxTilesX;
				}
			}
			UI.MainUI.Progress = 0.9f;
			for (int num149 = 0; num149 < Main.MaxTilesX; num149++)
			{
				TileRunner(genRand.Next(20, Main.MaxTilesX - 20), genRand.Next(Main.MaxTilesY - 180, Main.MaxTilesY - 10), genRand.Next(2, 7), genRand.Next(2, 7), -2);
			}
			for (int num150 = 0; num150 < Main.MaxTilesX; num150++)
			{
				if (Main.TileSet[num150, Main.MaxTilesY - 145].IsActive == 0)
				{
					Main.TileSet[num150, Main.MaxTilesY - 145].Liquid = byte.MaxValue;
					Main.TileSet[num150, Main.MaxTilesY - 145].Lava = 32;
				}
				if (Main.TileSet[num150, Main.MaxTilesY - 144].IsActive == 0)
				{
					Main.TileSet[num150, Main.MaxTilesY - 144].Liquid = byte.MaxValue;
					Main.TileSet[num150, Main.MaxTilesY - 144].Lava = 32;
				}
			}
			UI.MainUI.Progress = 0.95f;
			num56 = (int)((float)(Main.MaxTilesX * Main.MaxTilesY) * 0.0008f);
			for (int num151 = 0; num151 < num56; num151++)
			{
				TileRunner(genRand.Next(Main.MaxTilesX), genRand.Next(Main.MaxTilesY - 140, Main.MaxTilesY), genRand.Next(2, 7), genRand.Next(3, 7), 58);
			}
			UI.MainUI.Progress = 0.98f;
			AddHellHouses();
			UI.MainUI.NextProgressStep(Lang.WorldGenText[19]);
			num56 = genRand.Next(2, (int)((float)Main.MaxTilesX * 0.005f));
			for (int num152 = 0; num152 < num56; num152++)
			{
				UI.MainUI.Progress = (float)num152 / (float)num56;
				int num153 = genRand.Next(300, Main.MaxTilesX - 300);
				while (num153 > (Main.MaxTilesX >> 1) - 50 && num153 < (Main.MaxTilesX >> 1) + 50)
				{
					num153 = genRand.Next(300, Main.MaxTilesX - 300);
				}
				int num154;
				for (num154 = (int)num5 - 20; Main.TileSet[num153, num154].IsActive == 0; num154++)
				{
				}
				Lakinater(num153, num154);
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[58]);
			int num155 = 0;
			if (num9 == -1)
			{
				num155 = genRand.Next((int)((float)Main.MaxTilesX * 0.05f), (int)((float)Main.MaxTilesX * 0.2f));
				num9 = -1;
			}
			else
			{
				num155 = genRand.Next((int)((float)Main.MaxTilesX * 0.8f), (int)((float)Main.MaxTilesX * 0.95f));
				num9 = 1;
			}
			int y = (Main.RockLayer + Main.MaxTilesY >> 1) + genRand.Next(-200, 200);
			MakeDungeon(num155, y);
			UI.MainUI.NextProgressStep(Lang.WorldGenText[20]);
			num56 = (int)((float)Main.MaxTilesX * 0.00045f);
			for (int num156 = 0; num156 < num56; num156++)
			{
				UI.MainUI.Progress = (float)num156 / (float)num56;
				bool flag7 = false;
				int num157 = 0;
				int num158 = 0;
				int num159 = 0;
				do
				{
					int num160 = 0;
					int num161 = Main.MaxTilesX >> 1;
					int num162 = 200;
					num157 = genRand.Next(320, Main.MaxTilesX - 320);
					num158 = num157 - genRand.Next(200) - 100;
					num159 = num157 + genRand.Next(200) + 100;
					if (num158 < 285)
					{
						num158 = 285;
					}
					if (num159 > Main.MaxTilesX - 285)
					{
						num159 = Main.MaxTilesX - 285;
					}
					if (num157 > num161 - num162 && num157 < num161 + num162)
					{
						flag7 = false;
					}
					else if (num158 > num161 - num162 && num158 < num161 + num162)
					{
						flag7 = false;
					}
					else if (num159 > num161 - num162 && num159 < num161 + num162)
					{
						flag7 = false;
					}
					else
					{
						flag7 = true;
						int num163 = num158;
						while (flag7 && num163 < num159)
						{
							for (int num164 = 0; num164 < Main.WorldSurface; num164 += 5)
							{
								if (Main.TileSet[num163, num164].IsActive != 0 && Main.TileDungeon[Main.TileSet[num163, num164].Type])
								{
									flag7 = false;
									break;
								}
							}
							num163++;
						}
					}
					if (num160 < 200 && JungleX > num158 && JungleX < num159)
					{
						num160++;
						flag7 = false;
					}
				}
				while (!flag7);
				int num165 = 0;
				for (int num166 = num158; num166 < num159; num166++)
				{
					if (num165 > 0)
					{
						num165--;
					}
					if (num166 == num157 || num165 == 0)
					{
						for (int num167 = (int)num5; num167 < Main.WorldSurface - 1; num167++)
						{
							if (Main.TileSet[num166, num167].IsActive != 0 || Main.TileSet[num166, num167].WallType > 0)
							{
								if (num166 == num157)
								{
									num165 = 20;
									ChasmRunner(num166, num167, genRand.Next(150) + 150, makeOrb: true);
								}
								else if (genRand.Next(35) == 0 && num165 == 0)
								{
									num165 = 30;
									ChasmRunner(num166, num167, genRand.Next(50) + 50, makeOrb: true);
								}
								break;
							}
						}
					}
					for (int num168 = (int)num5; num168 < Main.WorldSurface - 1; num168++)
					{
						if (Main.TileSet[num166, num168].IsActive == 0)
						{
							continue;
						}
						int num169 = num168 + genRand.Next(10, 14);
						for (int num170 = num168; num170 < num169; num170++)
						{
							if ((Main.TileSet[num166, num170].Type == 59 || Main.TileSet[num166, num170].Type == 60) && num166 >= num158 + genRand.Next(5) && num166 < num159 - genRand.Next(5))
							{
								Main.TileSet[num166, num170].Type = 0;
							}
						}
						break;
					}
				}
				double num171 = Main.WorldSurface + 40;
				for (int num172 = num158; num172 < num159; num172++)
				{
					num171 += (double)genRand.Next(-2, 3);
					if (num171 < (double)(Main.WorldSurface + 30))
					{
						num171 = Main.WorldSurface + 30;
					}
					if (num171 > (double)(Main.WorldSurface + 50))
					{
						num171 = Main.WorldSurface + 50;
					}
					num55 = num172;
					bool flag8 = false;
					for (int num173 = (int)num5; (double)num173 < num171; num173++)
					{
						if (Main.TileSet[num55, num173].IsActive != 0)
						{
							if (Main.TileSet[num55, num173].Type == 53 && num55 >= num158 + genRand.Next(5) && num55 <= num159 - genRand.Next(5))
							{
								Main.TileSet[num55, num173].Type = 0;
							}
							if (Main.TileSet[num55, num173].Type == 0 && num173 < Main.WorldSurface - 1 && !flag8)
							{
								grassSpread = 0;
								SpreadGrass(num55, num173, 0, 23);
							}
							flag8 = true;
							if (Main.TileSet[num55, num173].Type == 1 && num55 >= num158 + genRand.Next(5) && num55 <= num159 - genRand.Next(5))
							{
								Main.TileSet[num55, num173].Type = 25;
							}
							if (Main.TileSet[num55, num173].Type == 2)
							{
								Main.TileSet[num55, num173].Type = 23;
							}
						}
					}
				}
				for (int num174 = num158; num174 < num159; num174++)
				{
					for (int num175 = 0; num175 < Main.MaxTilesY - 50; num175++)
					{
						if (Main.TileSet[num174, num175].IsActive == 0 || Main.TileSet[num174, num175].Type != 31)
						{
							continue;
						}
						int num176 = num174 - 13;
						int num177 = num174 + 13;
						int num178 = num175 - 13;
						int num179 = num175 + 13;
						for (int num180 = num176; num180 < num177; num180++)
						{
							if (num180 <= 10 || num180 >= Main.MaxTilesX - 10)
							{
								continue;
							}
							for (int num181 = num178; num181 < num179; num181++)
							{
								if (genRand.Next(3) != 0 && Math.Abs(num180 - num174) + Math.Abs(num181 - num175) < 9 + genRand.Next(11) && Main.TileSet[num180, num181].Type != 31)
								{
									Main.TileSet[num180, num181].IsActive = 1;
									Main.TileSet[num180, num181].Type = 25;
									if (Math.Abs(num180 - num174) <= 1 && Math.Abs(num181 - num175) <= 1)
									{
										Main.TileSet[num180, num181].IsActive = 0;
									}
								}
								if (Main.TileSet[num180, num181].Type != 31 && Math.Abs(num180 - num174) <= 2 + genRand.Next(3) && Math.Abs(num181 - num175) <= 2 + genRand.Next(3))
								{
									Main.TileSet[num180, num181].IsActive = 0;
								}
							}
						}
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[21]);
			for (int num182 = 0; num182 < numMCaves; num182++)
			{
				short x = mCave[num182].X;
				int y2 = mCave[num182].Y;
				CaveOpenater(x, y2);
				Cavinator(x, y2, genRand.Next(40, 50));
			}
			int num183 = 0;
			int num184 = 0;
			int num185 = 20;
			int num186 = Main.MaxTilesX - 20;
			UI.MainUI.NextProgressStep(Lang.WorldGenText[22]);
			for (int num187 = 0; num187 < 2; num187++)
			{
				int num188 = 0;
				int num189 = 0;
				if (num187 == 0)
				{
					num188 = 0;
					num189 = genRand.Next(125, 200) + 50;
					if (num9 == 1)
					{
						num189 = 275;
					}
					int num190 = 0;
					float num191 = 1f;
					int num192;
					for (num192 = 0; Main.TileSet[num189 - 1, num192].IsActive == 0; num192++)
					{
					}
					num183 = num192;
					num192 += genRand.Next(1, 5);
					for (int num193 = num189 - 1; num193 >= num188; num193--)
					{
						num190++;
						if (num190 < 3)
						{
							num191 += (float)genRand.Next(10, 20) * 0.2f;
						}
						else if (num190 < 6)
						{
							num191 += (float)genRand.Next(10, 20) * 0.15f;
						}
						else if (num190 < 9)
						{
							num191 += (float)genRand.Next(10, 20) * 0.1f;
						}
						else if (num190 < 15)
						{
							num191 += (float)genRand.Next(10, 20) * 0.07f;
						}
						else if (num190 < 50)
						{
							num191 += (float)genRand.Next(10, 20) * 0.05f;
						}
						else if (num190 < 75)
						{
							num191 += (float)genRand.Next(10, 20) * 0.04f;
						}
						else if (num190 < 100)
						{
							num191 += (float)genRand.Next(10, 20) * 0.03f;
						}
						else if (num190 < 125)
						{
							num191 += (float)genRand.Next(10, 20) * 0.02f;
						}
						else if (num190 < 150)
						{
							num191 += (float)genRand.Next(10, 20) * 0.01f;
						}
						else if (num190 < 175)
						{
							num191 += (float)genRand.Next(10, 20) * 0.005f;
						}
						else if (num190 < 200)
						{
							num191 += (float)genRand.Next(10, 20) * 0.001f;
						}
						else if (num190 < 230)
						{
							num191 += (float)genRand.Next(10, 20) * 0.01f;
						}
						else if (num190 < 235)
						{
							num191 += (float)genRand.Next(10, 20) * 0.05f;
						}
						else if (num190 < 240)
						{
							num191 += (float)genRand.Next(10, 20) * 0.1f;
						}
						else if (num190 < 245)
						{
							num191 += (float)genRand.Next(10, 20) * 0.05f;
						}
						else if (num190 < 255)
						{
							num191 += (float)genRand.Next(10, 20) * 0.01f;
						}
						if (num190 == 235)
						{
							num186 = num193;
							num185 = num193;
						}
						int num194 = genRand.Next(15, 20);
						for (int num195 = 0; (float)num195 < (float)num192 + num191 + (float)num194; num195++)
						{
							if ((float)num195 < (float)num192 + num191 * 0.75f - 3f)
							{
								Main.TileSet[num193, num195].IsActive = 0;
								if (num195 > num192)
								{
									Main.TileSet[num193, num195].Liquid = byte.MaxValue;
								}
								else if (num195 == num192)
								{
									Main.TileSet[num193, num195].Liquid = 127;
								}
							}
							else if (num195 > num192)
							{
								Main.TileSet[num193, num195].Type = 53;
								Main.TileSet[num193, num195].IsActive = 1;
							}
							Main.TileSet[num193, num195].WallType = 0;
						}
					}
					continue;
				}
				num188 = Main.MaxTilesX - genRand.Next(125, 200) - 50;
				num189 = Main.MaxTilesX;
				if (num9 == -1)
				{
					num188 = Main.MaxTilesX - 275;
				}
				float num196 = 1f;
				int num197 = 0;
				int num198;
				for (num198 = 0; Main.TileSet[num188, num198].IsActive == 0; num198++)
				{
				}
				num184 = num198;
				num198 += genRand.Next(1, 5);
				for (int num199 = num188; num199 < num189; num199++)
				{
					num197++;
					if (num197 < 3)
					{
						num196 += (float)genRand.Next(10, 20) * 0.2f;
					}
					else if (num197 < 6)
					{
						num196 += (float)genRand.Next(10, 20) * 0.15f;
					}
					else if (num197 < 9)
					{
						num196 += (float)genRand.Next(10, 20) * 0.1f;
					}
					else if (num197 < 15)
					{
						num196 += (float)genRand.Next(10, 20) * 0.07f;
					}
					else if (num197 < 50)
					{
						num196 += (float)genRand.Next(10, 20) * 0.05f;
					}
					else if (num197 < 75)
					{
						num196 += (float)genRand.Next(10, 20) * 0.04f;
					}
					else if (num197 < 100)
					{
						num196 += (float)genRand.Next(10, 20) * 0.03f;
					}
					else if (num197 < 125)
					{
						num196 += (float)genRand.Next(10, 20) * 0.02f;
					}
					else if (num197 < 150)
					{
						num196 += (float)genRand.Next(10, 20) * 0.01f;
					}
					else if (num197 < 175)
					{
						num196 += (float)genRand.Next(10, 20) * 0.005f;
					}
					else if (num197 < 200)
					{
						num196 += (float)genRand.Next(10, 20) * 0.001f;
					}
					else if (num197 < 230)
					{
						num196 += (float)genRand.Next(10, 20) * 0.01f;
					}
					else if (num197 < 235)
					{
						num196 += (float)genRand.Next(10, 20) * 0.05f;
					}
					else if (num197 < 240)
					{
						num196 += (float)genRand.Next(10, 20) * 0.1f;
					}
					else if (num197 < 245)
					{
						num196 += (float)genRand.Next(10, 20) * 0.05f;
					}
					else if (num197 < 255)
					{
						num196 += (float)genRand.Next(10, 20) * 0.01f;
					}
					if (num197 == 235)
					{
						num186 = num199;
					}
					int num200 = genRand.Next(15, 20);
					for (int num201 = 0; (float)num201 < (float)num198 + num196 + (float)num200; num201++)
					{
						if ((float)num201 < (float)num198 + num196 * 0.75f - 3f && num201 < Main.WorldSurface - 2)
						{
							Main.TileSet[num199, num201].IsActive = 0;
							if (num201 > num198)
							{
								Main.TileSet[num199, num201].Liquid = byte.MaxValue;
							}
							else if (num201 == num198)
							{
								Main.TileSet[num199, num201].Liquid = 127;
							}
						}
						else if (num201 > num198)
						{
							Main.TileSet[num199, num201].Type = 53;
							Main.TileSet[num199, num201].IsActive = 1;
						}
						Main.TileSet[num199, num201].WallType = 0;
					}
				}
			}
			for (; Main.TileSet[num185, num183].IsActive == 0; num183++)
			{
			}
			num183++;
			for (; Main.TileSet[num186, num184].IsActive == 0; num184++)
			{
			}
			num184++;
			UI.MainUI.NextProgressStep(Lang.WorldGenText[23]);
			for (int num202 = 63; num202 <= 68; num202++)
			{
				float num203 = 0f;
				switch (num202)
				{
				case 67:
					num203 = (float)Main.MaxTilesX * 0.5f;
					break;
				case 66:
					num203 = (float)Main.MaxTilesX * 0.45f;
					break;
				case 63:
					num203 = (float)Main.MaxTilesX * 0.3f;
					break;
				case 65:
					num203 = (float)Main.MaxTilesX * 0.25f;
					break;
				case 64:
					num203 = (float)Main.MaxTilesX * 0.1f;
					break;
				case 68:
					num203 = (float)Main.MaxTilesX * 0.05f;
					break;
				}
				num203 *= 0.2f;
				for (int num204 = 0; (float)num204 < num203; num204++)
				{
					int num205 = genRand.Next(Main.MaxTilesX);
					int num206 = genRand.Next(Main.WorldSurface, Main.MaxTilesY);
					while (Main.TileSet[num205, num206].Type != 1)
					{
						num205 = genRand.Next(Main.MaxTilesX);
						num206 = genRand.Next(Main.WorldSurface, Main.MaxTilesY);
					}
					TileRunner(num205, num206, genRand.Next(2, 6), genRand.Next(3, 7), num202);
				}
			}
			for (int num207 = 0; num207 < 2; num207++)
			{
				int num208 = 1;
				int num209 = 5;
				int num210 = Main.MaxTilesX - 5;
				if (num207 == 1)
				{
					num208 = -1;
					num209 = Main.MaxTilesX - 5;
					num210 = 5;
				}
				for (int num211 = num209; num211 != num210; num211 += num208)
				{
					for (int num212 = 10; num212 < Main.MaxTilesY - 10; num212++)
					{
						if (Main.TileSet[num211, num212].IsActive == 0 || Main.TileSet[num211, num212].Type != 53 || Main.TileSet[num211, num212 + 1].IsActive == 0 || Main.TileSet[num211, num212 + 1].Type != 53)
						{
							continue;
						}
						int num213 = num211 + num208;
						int num214 = num212 + 1;
						if (Main.TileSet[num213, num212].IsActive == 0 && Main.TileSet[num213, num212 + 1].IsActive == 0)
						{
							for (; Main.TileSet[num213, num214].IsActive == 0; num214++)
							{
							}
							num214--;
							Main.TileSet[num211, num212].IsActive = 0;
							Main.TileSet[num213, num214].IsActive = 1;
							Main.TileSet[num213, num214].Type = 53;
						}
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[24]);
			for (int num215 = 0; num215 < Main.MaxTilesX; num215++)
			{
				UI.MainUI.Progress = (float)num215 / (float)Main.MaxTilesX;
				for (int num216 = Main.MaxTilesY - 5; num216 > 0; num216--)
				{
					if (Main.TileSet[num215, num216].IsActive == 0)
					{
						continue;
					}
					if (Main.TileSet[num215, num216].Type == 53)
					{
						for (int num217 = num216; Main.TileSet[num215, num217 + 1].IsActive == 0 && num217 < Main.MaxTilesY - 5; num217++)
						{
							Main.TileSet[num215, num217 + 1].IsActive = 1;
							Main.TileSet[num215, num217 + 1].Type = 53;
						}
					}
					else if (Main.TileSet[num215, num216].Type == 123)
					{
						for (int num218 = num216; Main.TileSet[num215, num218 + 1].IsActive == 0 && num218 < Main.MaxTilesY - 5; num218++)
						{
							Main.TileSet[num215, num218 + 1].IsActive = 1;
							Main.TileSet[num215, num218 + 1].Type = 123;
							Main.TileSet[num215, num218].IsActive = 0;
						}
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[25]);
			for (int num219 = 3; num219 < Main.MaxTilesX - 3; num219++)
			{
				UI.MainUI.Progress = (float)num219 / (float)(Main.MaxTilesX - 4);
				bool flag9 = true;
				for (int num220 = 0; num220 < Main.WorldSurface; num220++)
				{
					if (flag9)
					{
						if (Main.TileSet[num219, num220].WallType == 2)
						{
							Main.TileSet[num219, num220].WallType = 0;
						}
						if (Main.TileSet[num219, num220].Type != 53)
						{
							if (Main.TileSet[num219 - 1, num220].WallType == 2)
							{
								Main.TileSet[num219 - 1, num220].WallType = 0;
							}
							if (Main.TileSet[num219 - 2, num220].WallType == 2 && genRand.Next(2) == 0)
							{
								Main.TileSet[num219 - 2, num220].WallType = 0;
							}
							if (Main.TileSet[num219 - 3, num220].WallType == 2 && genRand.Next(2) == 0)
							{
								Main.TileSet[num219 - 3, num220].WallType = 0;
							}
							if (Main.TileSet[num219 + 1, num220].WallType == 2)
							{
								Main.TileSet[num219 + 1, num220].WallType = 0;
							}
							if (Main.TileSet[num219 + 2, num220].WallType == 2 && genRand.Next(2) == 0)
							{
								Main.TileSet[num219 + 2, num220].WallType = 0;
							}
							if (Main.TileSet[num219 + 3, num220].WallType == 2 && genRand.Next(2) == 0)
							{
								Main.TileSet[num219 + 3, num220].WallType = 0;
							}
							if (Main.TileSet[num219, num220].IsActive != 0)
							{
								flag9 = false;
							}
						}
					}
					else if (Main.TileSet[num219, num220].WallType == 0 && Main.TileSet[num219, num220 + 1].WallType == 0 && Main.TileSet[num219, num220 + 2].WallType == 0 && Main.TileSet[num219, num220 + 3].WallType == 0 && Main.TileSet[num219, num220 + 4].WallType == 0 && Main.TileSet[num219 - 1, num220].WallType == 0 && Main.TileSet[num219 + 1, num220].WallType == 0 && Main.TileSet[num219 - 2, num220].WallType == 0 && Main.TileSet[num219 + 2, num220].WallType == 0 && Main.TileSet[num219, num220].IsActive == 0 && Main.TileSet[num219, num220 + 1].IsActive == 0 && Main.TileSet[num219, num220 + 2].IsActive == 0 && Main.TileSet[num219, num220 + 3].IsActive == 0)
					{
						flag9 = true;
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[26]);
			int y3 = (int)num6 + 20;
			num56 = Main.MaxTilesX * Main.MaxTilesY / 50000;
			for (int num221 = 0; num221 < num56; num221++)
			{
				UI.MainUI.Progress = (float)num221 / (float)num56;
				for (int num222 = 0; num222 < 4096; num222++)
				{
					if (Place3x2(genRand.Next(5, Main.MaxTilesX - 5), y3, 26))
					{
						break;
					}
				}
			}
			num56 = (int)num5;
			for (int num223 = 0; num223 < Main.MaxTilesX; num223++)
			{
				int num224 = num56;
				Tile* ptr4 = tileSet + (num223 * 1440 + num224);
				int num11;
				do
				{
					if (ptr4->IsActive != 0)
					{
						if (ptr4->Type == 60)
						{
							ptr4[-1].Liquid = byte.MaxValue;
							ptr4[-2].Liquid = byte.MaxValue;
						}
						break;
					}
					ptr4++;
					num11 = num224 + 1;
					num224 = num11;
				}
				while (num11 < Main.WorldSurface - 1);
			}
			for (int num225 = 400; num225 < Main.MaxTilesX - 400; num225++)
			{
				int num226 = num56;
				Tile* ptr5 = tileSet + (num225 * 1440 + num226);
				int num11;
				do
				{
					if (ptr5->IsActive != 0)
					{
						if (ptr5->Type != 53)
						{
							break;
						}
						Tile* ptr6 = ptr5;
						while (num226 > num56)
						{
							num226--;
							ptr6--;
							if (ptr6->Liquid <= 0)
							{
								break;
							}
							ptr6->Liquid = 0;
						}
						break;
					}
					ptr5++;
					num11 = num226 + 1;
					num226 = num11;
				}
				while (num11 < Main.WorldSurface - 1);
			}
			Liquid.QuickWater(0.3333333432674408);
			WaterCheck();
			int num227 = 0;
			Liquid.QuickSettleOn();
			do
			{
				int num228 = Liquid.NumLiquids + LiquidBuffer.NumLiquidBuffer;
				num227++;
				float num229 = 0f;
				while (Liquid.NumLiquids > 0)
				{
					float num230 = (float)(num228 - (Liquid.NumLiquids + LiquidBuffer.NumLiquidBuffer)) / (float)num228;
					if (Liquid.NumLiquids + LiquidBuffer.NumLiquidBuffer > num228)
					{
						num228 = Liquid.NumLiquids + LiquidBuffer.NumLiquidBuffer;
					}
					if (num230 > num229)
					{
						num229 = num230;
					}
					else
					{
						num230 = num229;
					}
					if (num227 == 1)
					{
						UI.MainUI.Progress = num230 * (1f / 3f) + 1f / 3f;
					}
					int num231 = 10;
					if (num227 > num231)
					{
						num231 = num227;
					}
					Liquid.UpdateLiquid();
				}
				WaterCheck();
			}
			while (num227 < 10);
			Liquid.QuickSettleOff();
			UI.MainUI.NextProgressStep(Lang.WorldGenText[28]);
			num56 = Main.MaxTilesX * Main.MaxTilesY / 50000;
			for (int num232 = 0; num232 < num56; num232++)
			{
				UI.MainUI.Progress = (float)num232 / (float)num56;
				int num233 = 0;
				while (!AddLifeCrystal(genRand.Next(1, Main.MaxTilesX), genRand.Next((int)(num6 + 20f), Main.MaxTilesY)))
				{
					int num11 = num233 + 1;
					num233 = num11;
					if (num11 >= 10000)
					{
						break;
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[29]);
			float num234 = (float)Main.MaxTilesX / 4200f;
			int num235 = 0;
			num56 = (int)(num234 * 82f);
			for (int num236 = 0; num236 < num56; num236++)
			{
				if (num235 > 41)
				{
					num235 = 0;
				}
				UI.MainUI.Progress = (float)num236 / (float)num56;
				int num237 = 0;
				int num11;
				do
				{
					int num238 = genRand.Next(20, Main.MaxTilesX - 20);
					int num239;
					for (num239 = genRand.Next((int)(num6 + 20f), Main.MaxTilesY - 300); Main.TileSet[num238, num239].IsActive == 0; num239++)
					{
					}
					num239--;
					if (PlaceTile(num238, num239, 105, ToMute: true, IsForced: true, -1, num235))
					{
						num235++;
						break;
					}
					num11 = num237 + 1;
					num237 = num11;
				}
				while (num11 < 10000);
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[30]);
			num56 = Main.MaxTilesX * Main.MaxTilesY / 62500;
			for (int num240 = 0; num240 < num56; num240++)
			{
				UI.MainUI.Progress = (float)num240 / (float)num56;
				int num241 = 0;
				while (true)
				{
					int num242 = genRand.Next(1, Main.MaxTilesX);
					int num243 = ((num240 <= 3) ? genRand.Next(Main.MaxTilesY - 200, Main.MaxTilesY - 50) : genRand.Next((int)(num6 + 20f), Main.MaxTilesY - 230));
					int wallType = Main.TileSet[num242, num243].WallType;
					if (wallType >= 7 && wallType <= 9)
					{
						continue;
					}
					if (AddBuriedChest(num242, num243))
					{
						if (genRand.Next(2) == 0)
						{
							int num244;
							for (num244 = num243; Main.TileSet[num242, num244].Type != 21 && num244 < Main.MaxTilesY - 300; num244++)
							{
							}
							if (num243 < Main.MaxTilesY - 300)
							{
								MineHouse(num242, num244);
							}
						}
						break;
					}
					int num11 = num241 + 1;
					num241 = num11;
					if (num11 >= 5000)
					{
						break;
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[31]);
			num56 = Main.MaxTilesX / 200;
			for (int num245 = 0; num245 < num56; num245++)
			{
				UI.MainUI.Progress = (float)num245 / (float)num56;
				int num246 = 0;
				int num11;
				do
				{
					int num247 = genRand.Next(300, Main.MaxTilesX - 300);
					int num248 = genRand.Next((int)num5, Main.WorldSurface);
					if (Main.TileSet[num247, num248].WallType == 2 && Main.TileSet[num247, num248].IsActive == 0 && AddBuriedChest(num247, num248, 0, notNearOtherChests: true))
					{
						break;
					}
					num11 = num246 + 1;
					num246 = num11;
				}
				while (num11 < 2000);
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[32]);
			int num249 = 0;
			for (int num250 = 0; num250 < numJChests; num250++)
			{
				UI.MainUI.Progress = (float)num250 / (float)numJChests;
				int contain = 211;
				int num11 = num249 + 1;
				num249 = num11;
				switch (num11)
				{
				case 2:
					contain = 212;
					break;
				case 3:
					contain = 213;
					break;
				default:
					if (Main.Rand.Next(2) == 0)
					{
						num249 = ((Main.Rand.Next(6) != 0) ? 622 : 624);
					}
					break;
				case 1:
					break;
				}
				if (!AddBuriedChest(JChest[num250].X + genRand.Next(2), JChest[num250].Y, contain))
				{
					KillTile(JChest[num250].X, JChest[num250].Y);
					KillTile(JChest[num250].X, JChest[num250].Y + 1);
					KillTile(JChest[num250].X + 1, JChest[num250].Y);
					KillTile(JChest[num250].X + 1, JChest[num250].Y + 1);
					AddBuriedChest(JChest[num250].X, JChest[num250].Y, contain);
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[33]);
			int num251 = 0;
			num56 = (int)(9f * num234);
			for (int num252 = 0; num252 < num56; num252++)
			{
				UI.MainUI.Progress = (float)num252 / (float)num56;
				int contain2 = 187;
				int num11 = num251 + 1;
				num251 = num11;
				if (num11 == 1)
				{
					contain2 = 186;
				}
				else if (num251 == 2)
				{
					contain2 = 277;
				}
				else
				{
					num251 = 0;
				}
				bool flag10 = false;
				while (!flag10)
				{
					int num253 = genRand.Next(1, Main.MaxTilesX);
					int num254 = genRand.Next(1, Main.MaxTilesY - 200);
					while (Main.TileSet[num253, num254].Liquid < 200 || Main.TileSet[num253, num254].Lava != 0)
					{
						num253 = genRand.Next(1, Main.MaxTilesX);
						num254 = genRand.Next(1, Main.MaxTilesY - 200);
					}
					flag10 = AddBuriedChest(num253, num254, contain2);
				}
			}
			for (int num255 = 0; num255 < numIslandHouses; num255++)
			{
				IslandHouse(fih[num255].X, fih[num255].Y);
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[34]);
			num56 = (int)((float)Main.MaxTilesX * 0.05f);
			for (int num256 = 0; num256 < num56; num256++)
			{
				UI.MainUI.Progress = (float)num256 / (float)num56;
				for (int num257 = 0; num257 < 1000; num257++)
				{
					int num258 = Main.Rand.Next(200, Main.MaxTilesX - 200);
					int num259 = Main.Rand.Next(Main.WorldSurface, Main.MaxTilesY - 300);
					if (Main.TileSet[num258, num259].WallType == 0 && placeTrap(num258, num259))
					{
						break;
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[35]);
			num56 = (int)((double)(Main.MaxTilesX * Main.MaxTilesY) * 0.0008);
			for (int num260 = 0; num260 < num56; num260++)
			{
				float num261 = (float)num260 / (float)num56;
				UI.MainUI.Progress = num261;
				bool flag11 = false;
				int num262 = 0;
				while (!flag11)
				{
					int num263 = genRand.Next((int)num6, Main.MaxTilesY - 10);
					if (num261 > 0.93f)
					{
						num263 = Main.MaxTilesY - 150;
					}
					else if (num261 > 0.75f)
					{
						num263 = (int)num5;
					}
					int num264 = genRand.Next(1, Main.MaxTilesX - 1);
					bool flag12 = false;
					for (int num265 = num263; num265 < Main.MaxTilesY - 1; num265++)
					{
						if (!flag12)
						{
							if (Main.TileSet[num264, num265].IsActive != 0 && Main.TileSolid[Main.TileSet[num264, num265].Type] && Main.TileSet[num264, num265 - 1].Lava == 0)
							{
								flag12 = true;
							}
							continue;
						}
						if (PlacePot(num264, num265))
						{
							flag11 = true;
							break;
						}
						num262++;
						if (num262 >= 10000)
						{
							flag11 = true;
							break;
						}
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[36]);
			num56 = Main.MaxTilesX / 200;
			for (int num266 = 0; num266 < num56; num266++)
			{
				UI.MainUI.Progress = (float)num266 / (float)num56;
				bool flag13 = false;
				int num267 = 0;
				while (!flag13)
				{
					int num268 = genRand.Next(5, Main.MaxTilesX - 5);
					int num269 = genRand.Next(Main.MaxTilesY - 250, Main.MaxTilesY - 5);
					int num11;
					do
					{
						if (Main.TileSet[num268, num269].IsActive != 0 || Main.TileSet[num268, num269].WallType == 13 || Main.TileSet[num268, num269].WallType == 14)
						{
							num269--;
							if (!PlaceTile(num268, num269, 77))
							{
								num11 = num267 + 1;
								num267 = num11;
								if (num11 < 10000)
								{
									break;
								}
							}
							flag13 = true;
							break;
						}
						num11 = num269 + 1;
						num269 = num11;
					}
					while (num11 != Main.MaxTilesY);
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[37]);
			for (int num270 = 0; num270 < Main.MaxTilesX; num270++)
			{
				num55 = num270;
				bool flag14 = true;
				for (int num271 = 0; num271 < Main.WorldSurface - 1; num271++)
				{
					if (Main.TileSet[num55, num271].IsActive != 0)
					{
						if (flag14 && Main.TileSet[num55, num271].Type == 0)
						{
							try
							{
								grassSpread = 0;
								SpreadGrass(num55, num271);
							}
							catch
							{
								grassSpread = 0;
								SpreadGrass(num55, num271, 0, 2, repeat: false);
							}
						}
						if ((float)num271 > num6)
						{
							break;
						}
						flag14 = false;
					}
					else if (Main.TileSet[num55, num271].WallType == 0)
					{
						flag14 = true;
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[38]);
			for (int num272 = 5; num272 < Main.MaxTilesX - 5; num272++)
			{
				if (genRand.Next(8) != 0)
				{
					continue;
				}
				Tile* ptr7 = tileSet + (num272 * 1440 + 5);
				int num273 = 5;
				int num11;
				do
				{
					if (ptr7->Type == 53 && ptr7->IsActive != 0)
					{
						ptr7--;
						if (ptr7->IsActive == 0 && ptr7->WallType == 0)
						{
							if (num272 < 250 || num272 > Main.MaxTilesX - 250)
							{
								ptr7--;
								if (ptr7->Liquid == byte.MaxValue)
								{
									ptr7--;
									if (ptr7->Liquid == byte.MaxValue)
									{
										ptr7--;
										if (ptr7->Liquid == byte.MaxValue)
										{
											PlaceTile(num272, num273 - 1, 81, ToMute: true);
										}
										ptr7++;
									}
									ptr7++;
								}
								ptr7++;
							}
							else if (num272 > 400 && num272 < Main.MaxTilesX - 400)
							{
								PlantCactus(num272, num273);
							}
						}
						ptr7++;
					}
					ptr7++;
					num11 = num273 + 1;
					num273 = num11;
				}
				while (num11 < Main.WorldSurface - 1);
			}
			int num274 = 5;
			while (true)
			{
				int num275 = (Main.MaxTilesX >> 1) + genRand.Next(-num274, num274 + 1);
				for (int num276 = 5; num276 <= Main.WorldSurface; num276++)
				{
					if (Main.TileSet[num275, num276].IsActive != 0)
					{
						Main.SpawnTileX = (short)num275;
						Main.SpawnTileY = (short)num276;
						break;
					}
				}
				if (Main.TileSet[Main.SpawnTileX, Main.SpawnTileY - 1].Liquid == 0)
				{
					break;
				}
				num274++;
			}
			int num277 = NPC.NewNPC(Main.SpawnTileX * 16, Main.SpawnTileY * 16, 22);
			Main.NPCSet[num277].HomeTileX = Main.SpawnTileX;
			Main.NPCSet[num277].HomeTileY = Main.SpawnTileY;
			Main.NPCSet[num277].Direction = 1;
			Main.NPCSet[num277].IsHomeless = true;
			UI.MainUI.NextProgressStep(Lang.WorldGenText[39]);
			for (int num278 = 0; (double)num278 < (double)Main.MaxTilesX * 0.002; num278++)
			{
				int num279 = 0;
				int num280 = 0;
				_ = Main.MaxTilesX;
				int num281 = genRand.Next(Main.MaxTilesX);
				num279 = num281 - genRand.Next(10) - 7;
				num280 = num281 + genRand.Next(10) + 7;
				if (num279 < 0)
				{
					num279 = 0;
				}
				if (num280 > Main.MaxTilesX - 1)
				{
					num280 = Main.MaxTilesX - 1;
				}
				for (int num282 = num279; num282 < num280; num282++)
				{
					for (int num283 = 5; num283 < Main.WorldSurface - 1; num283++)
					{
						if (Main.TileSet[num282, num283].Type == 2 && Main.TileSet[num282, num283].IsActive != 0 && Main.TileSet[num282, num283 - 1].IsActive == 0)
						{
							PlaceTile(num282, num283 - 1, 27, ToMute: true);
						}
						if (Main.TileSet[num282, num283].IsActive != 0)
						{
							break;
						}
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[40]);
			num56 = (int)((double)Main.MaxTilesX * 0.003) - 1;
			for (int num284 = num56; num284 >= 0; num284--)
			{
				int num285 = genRand.Next(50, Main.MaxTilesX - 50);
				int num286 = genRand.Next(25, 50);
				for (int num287 = num285 - num286; num287 < num285 + num286; num287++)
				{
					for (int num288 = 20; num288 < Main.WorldSurface; num288++)
					{
						GrowEpicTree(num287, num288);
					}
				}
			}
			AddTrees();
			UI.MainUI.NextProgressStep(Lang.WorldGenText[41]);
			for (int num289 = (int)((double)Main.MaxTilesX * 1.7) - 1; num289 >= 0; num289--)
			{
				PlantAlch();
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[42]);
			AddPlants();
			for (int num290 = 0; num290 < Main.MaxTilesX; num290++)
			{
				for (int num291 = 1; num291 < Main.MaxTilesY; num291++)
				{
					if (Main.TileSet[num290, num291].IsActive == 0)
					{
						continue;
					}
					if (num291 >= Main.WorldSurface && Main.TileSet[num290, num291].Type == 70 && Main.TileSet[num290, num291 - 1].IsActive == 0)
					{
						GrowShroom(num290, num291);
						if (Main.TileSet[num290, num291 - 1].IsActive == 0)
						{
							PlaceTile(num290, num291 - 1, 71, ToMute: true);
						}
					}
					if (Main.TileSet[num290, num291].Type == 60 && Main.TileSet[num290, num291 - 1].IsActive == 0)
					{
						PlaceTile(num290, num291 - 1, 61, ToMute: true);
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[43]);
			for (int num292 = 0; num292 < Main.MaxTilesX; num292++)
			{
				int num293 = 0;
				for (int num294 = 0; num294 < Main.WorldSurface; num294++)
				{
					if (num293 > 0 && Main.TileSet[num292, num294].IsActive == 0)
					{
						Main.TileSet[num292, num294].IsActive = 1;
						Main.TileSet[num292, num294].Type = 52;
						num293--;
					}
					else
					{
						num293 = 0;
					}
					if (Main.TileSet[num292, num294].IsActive != 0 && Main.TileSet[num292, num294].Type == 2 && genRand.Next(5) < 3)
					{
						num293 = genRand.Next(1, 10);
					}
				}
				num293 = 0;
				for (int num295 = 0; num295 < Main.MaxTilesY; num295++)
				{
					if (num293 > 0 && Main.TileSet[num292, num295].IsActive == 0)
					{
						Main.TileSet[num292, num295].IsActive = 1;
						Main.TileSet[num292, num295].Type = 62;
						num293--;
					}
					else
					{
						num293 = 0;
					}
					if (Main.TileSet[num292, num295].IsActive != 0 && Main.TileSet[num292, num295].Type == 60 && genRand.Next(5) < 3)
					{
						num293 = genRand.Next(1, 10);
					}
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[44]);
			for (int num296 = Main.MaxTilesX / 200; num296 > 0; num296--)
			{
				int num297 = genRand.Next(20, Main.MaxTilesX - 20);
				int num298 = genRand.Next(5, 15);
				int num299 = genRand.Next(15, 30);
				for (int num300 = 30; num300 < Main.WorldSurface - 1; num300++)
				{
					if (Main.TileSet[num297, num300].IsActive == 0)
					{
						continue;
					}
					for (int num301 = num297 - num298; num301 < num297 + num298; num301++)
					{
						for (int num302 = num300 - num299; num302 < num300 + num299; num302++)
						{
							if (Main.TileSet[num301, num302].Type == 3 || Main.TileSet[num301, num302].Type == 24)
							{
								Main.TileSet[num301, num302].FrameX = (short)(genRand.Next(6, 8) * 18);
							}
						}
					}
					break;
				}
			}
			UI.MainUI.NextProgressStep(Lang.WorldGenText[45]);
			for (int num303 = Main.MaxTilesX / 500; num303 > 0; num303--)
			{
				int num304 = genRand.Next(20, Main.MaxTilesX - 20);
				int num305 = genRand.Next(4, 10);
				int num306 = genRand.Next(15, 30);
				for (int num307 = 30; num307 < Main.WorldSurface - 1; num307++)
				{
					if (Main.TileSet[num304, num307].IsActive == 0)
					{
						continue;
					}
					for (int num308 = num304 - num305; num308 < num304 + num305; num308++)
					{
						for (int num309 = num307 - num306; num309 < num307 + num306; num309++)
						{
							if (Main.TileSet[num308, num309].Type == 3 || Main.TileSet[num308, num309].Type == 24)
							{
								Main.TileSet[num308, num309].FrameX = 144;
							}
						}
					}
					break;
				}
			}
		}
		Gen = false;
	}

	public static void GrowEpicTree(int i, int y)
	{
		int j;
		for (j = y; Main.TileSet[i, j].Type == 20; j++)
		{
		}
		Tile tile = Main.TileSet[i, j - 1];
		Tile tile2 = Main.TileSet[i - 1, j];
		Tile tile3 = Main.TileSet[i + 1, j];
		bool flag = Main.TileSet[i, j].IsActive == 0 || Main.TileSet[i, j].Type != 2;
		bool flag2 = tile.WallType != 0 || tile.Liquid != 0;
		bool flag3 = (tile2.IsActive == 0 || !IsGrassType(tile2.Type)) && (tile3.IsActive == 0 || !IsGrassType(tile3.Type));
		if (flag | flag2 | flag3)
		{
			return;
		}
		int num = 1;
		if (!EmptyTileCheckTree(i - num, i + num, j - 55, j - 1))
		{
			return;
		}
		int num2 = genRand.Next(10);
		int num3 = genRand.Next(20, 30);
		int num4;
		for (int k = j - num3; k < j; k++)
		{
			Main.TileSet[i, k].frameNumber = (byte)genRand.Next(3);
			Main.TileSet[i, k].IsActive = 1;
			Main.TileSet[i, k].Type = 5;
			num4 = genRand.Next(3);
			if (k == j - 1 || k == j - num3)
			{
				num2 = 0;
			}
			switch (num2)
			{
			case 1:
				Main.TileSet[i, k].FrameX = 0;
				Main.TileSet[i, k].FrameY = (short)(66 + num4 * 22);
				break;
			case 2:
				Main.TileSet[i, k].FrameX = 22;
				Main.TileSet[i, k].FrameY = (short)(num4 * 22);
				break;
			case 3:
				Main.TileSet[i, k].FrameX = 44;
				Main.TileSet[i, k].FrameY = (short)(66 + num4 * 22);
				break;
			case 4:
				Main.TileSet[i, k].FrameX = 22;
				Main.TileSet[i, k].FrameY = (short)(66 + num4 * 22);
				break;
			case 5:
				Main.TileSet[i, k].FrameX = 88;
				Main.TileSet[i, k].FrameY = (short)(num4 * 22);
				break;
			case 6:
				Main.TileSet[i, k].FrameX = 66;
				Main.TileSet[i, k].FrameY = (short)(66 + num4 * 22);
				break;
			case 7:
				Main.TileSet[i, k].FrameX = 110;
				Main.TileSet[i, k].FrameY = (short)(66 + num4 * 22);
				break;
			default:
				Main.TileSet[i, k].FrameX = 0;
				Main.TileSet[i, k].FrameY = (short)(num4 * 22);
				break;
			}
			bool flag4 = num2 == 5 || num2 == 7;
			bool flag5 = num2 == 6 || num2 == 7;
			if (flag4)
			{
				Main.TileSet[i - 1, k].IsActive = 1;
				Main.TileSet[i - 1, k].Type = 5;
				num4 = genRand.Next(3);
				if (genRand.Next(3) < 2)
				{
					Main.TileSet[i - 1, k].FrameX = 44;
					Main.TileSet[i - 1, k].FrameY = (short)(198 + num4 * 22);
				}
				else
				{
					Main.TileSet[i - 1, k].FrameX = 66;
					Main.TileSet[i - 1, k].FrameY = (short)(num4 * 22);
				}
			}
			if (flag5)
			{
				Main.TileSet[i + 1, k].IsActive = 1;
				Main.TileSet[i + 1, k].Type = 5;
				num4 = genRand.Next(3);
				if (genRand.Next(3) < 2)
				{
					Main.TileSet[i + 1, k].FrameX = 66;
					Main.TileSet[i + 1, k].FrameY = (short)(198 + num4 * 22);
				}
				else
				{
					Main.TileSet[i + 1, k].FrameX = 88;
					Main.TileSet[i + 1, k].FrameY = (short)(66 + num4 * 22);
				}
			}
			do
			{
				num2 = genRand.Next(10);
			}
			while (((num2 == 5 || num2 == 7) & flag4) || ((num2 == 6 || num2 == 7) & flag5));
		}
		int num5 = genRand.Next(3);
		bool flag6 = false;
		bool flag7 = false;
		if (Main.TileSet[i - 1, j].IsActive != 0 && (Main.TileSet[i - 1, j].Type == 2 || Main.TileSet[i - 1, j].Type == 23 || Main.TileSet[i - 1, j].Type == 60 || Main.TileSet[i - 1, j].Type == 109))
		{
			flag6 = true;
		}
		if (Main.TileSet[i + 1, j].IsActive != 0 && (Main.TileSet[i + 1, j].Type == 2 || Main.TileSet[i + 1, j].Type == 23 || Main.TileSet[i + 1, j].Type == 60 || Main.TileSet[i + 1, j].Type == 109))
		{
			flag7 = true;
		}
		if (!flag6)
		{
			if (num5 == 0)
			{
				num5 = 2;
			}
			if (num5 == 1)
			{
				num5 = 3;
			}
		}
		if (!flag7)
		{
			if (num5 == 0)
			{
				num5 = 1;
			}
			if (num5 == 2)
			{
				num5 = 3;
			}
		}
		if (flag7 && !flag6)
		{
			num5 = 1;
		}
		if (flag6 && !flag7)
		{
			num5 = 2;
		}
		if (num5 == 0 || num5 == 1)
		{
			Main.TileSet[i + 1, j - 1].IsActive = 1;
			Main.TileSet[i + 1, j - 1].Type = 5;
			num4 = genRand.Next(3);
			Main.TileSet[i + 1, j - 1].FrameX = 22;
			Main.TileSet[i + 1, j - 1].FrameY = (short)(132 + 22 * num4);
		}
		if (num5 == 0 || num5 == 2)
		{
			Main.TileSet[i - 1, j - 1].IsActive = 1;
			Main.TileSet[i - 1, j - 1].Type = 5;
			num4 = genRand.Next(3);
			Main.TileSet[i - 1, j - 1].FrameX = 44;
			Main.TileSet[i - 1, j - 1].FrameY = (short)(132 + 22 * num4);
		}
		num4 = genRand.Next(3);
		switch (num5)
		{
		case 0:
			Main.TileSet[i, j - 1].FrameX = 88;
			Main.TileSet[i, j - 1].FrameY = (short)(132 + 22 * num4);
			break;
		case 1:
			Main.TileSet[i, j - 1].FrameX = 0;
			Main.TileSet[i, j - 1].FrameY = (short)(132 + 22 * num4);
			break;
		case 2:
			Main.TileSet[i, j - 1].FrameX = 66;
			Main.TileSet[i, j - 1].FrameY = (short)(132 + 22 * num4);
			break;
		}
		num4 = genRand.Next(3);
		if (genRand.Next(3) < 2)
		{
			Main.TileSet[i, j - num3].FrameX = 22;
			Main.TileSet[i, j - num3].FrameY = (short)(198 + 22 * num4);
		}
		else
		{
			Main.TileSet[i, j - num3].FrameX = 0;
			Main.TileSet[i, j - num3].FrameY = (short)(198 + 22 * num4);
		}
		RangeFrame(i - 2, j - num3 - 1, i + 2, j + 1);
		if (Main.NetMode == 2)
		{
			NetMessage.SendTileSquare(i, (int)((double)j - (double)num3 * 0.5), num3 + 1);
		}
		static bool IsGrassType(int TileType)
		{
			if (TileType != 2 && TileType != 23 && TileType != 60)
			{
				return TileType == 109;
			}
			return true;
		}
	}

	public unsafe static void GrowTree(int i, int y)
	{
		int j;
		for (j = y; Main.TileSet[i, j].Type == 20; j++)
		{
		}
		Tile tile = Main.TileSet[i - 1, j];
		Tile tile2 = Main.TileSet[i + 1, j];
		Tile tile3 = Main.TileSet[i - 1, j - 1];
		Tile tile4 = Main.TileSet[i + 1, j - 1];
		bool flag = tile3.Liquid != 0 || tile4.Liquid != 0;
		bool flag2 = (tile.IsActive == 0 || !IsValidGrass(tile.Type)) && (tile2.IsActive == 0 || !IsValidGrass(tile2.Type));
		if (((flag && Main.TileSet[i, j].Type != 60) || Main.TileSet[i, j].IsActive == 0 || !IsValidGrass(Main.TileSet[i, j].Type) || Main.TileSet[i, j - 1].WallType != 0) | flag2)
		{
			return;
		}
		int num = 1;
		int num2 = 16;
		if (Main.TileSet[i, j].Type == 60)
		{
			num2 += 5;
		}
		if (!EmptyTileCheckTree(i - num, i + num, j - num2, j - 1))
		{
			return;
		}
		int num3 = genRand.Next(10);
		int num4 = genRand.Next(5, num2 + 1);
		int num5;
		for (int k = j - num4; k < j; k++)
		{
			Main.TileSet[i, k].frameNumber = (byte)genRand.Next(3);
			Main.TileSet[i, k].IsActive = 1;
			Main.TileSet[i, k].Type = 5;
			num5 = genRand.Next(3);
			if (k == j - 1 || k == j - num4)
			{
				num3 = 0;
			}
			switch (num3)
			{
			case 1:
				Main.TileSet[i, k].FrameX = 0;
				Main.TileSet[i, k].FrameY = (short)(66 + num5 * 22);
				break;
			case 2:
				Main.TileSet[i, k].FrameX = 22;
				Main.TileSet[i, k].FrameY = (short)(num5 * 22);
				break;
			case 3:
				Main.TileSet[i, k].FrameX = 44;
				Main.TileSet[i, k].FrameY = (short)(66 + num5 * 22);
				break;
			case 4:
				Main.TileSet[i, k].FrameX = 22;
				Main.TileSet[i, k].FrameY = (short)(66 + num5 * 22);
				break;
			case 5:
				Main.TileSet[i, k].FrameX = 88;
				Main.TileSet[i, k].FrameY = (short)(num5 * 22);
				break;
			case 6:
				Main.TileSet[i, k].FrameX = 66;
				Main.TileSet[i, k].FrameY = (short)(66 + num5 * 22);
				break;
			case 7:
				Main.TileSet[i, k].FrameX = 110;
				Main.TileSet[i, k].FrameY = (short)(66 + num5 * 22);
				break;
			default:
				Main.TileSet[i, k].FrameX = 0;
				Main.TileSet[i, k].FrameY = (short)(num5 * 22);
				break;
			}
			bool flag3 = num3 == 5 || num3 == 7;
			bool flag4 = num3 == 6 || num3 == 7;
			if (flag3)
			{
				Main.TileSet[i - 1, k].IsActive = 1;
				Main.TileSet[i - 1, k].Type = 5;
				num5 = genRand.Next(3);
				if (genRand.Next(3) < 2)
				{
					Main.TileSet[i - 1, k].FrameX = 44;
					Main.TileSet[i - 1, k].FrameY = (short)(198 + num5 * 22);
				}
				else
				{
					Main.TileSet[i - 1, k].FrameX = 66;
					Main.TileSet[i - 1, k].FrameY = (short)(num5 * 22);
				}
			}
			if (flag4)
			{
				Main.TileSet[i + 1, k].IsActive = 1;
				Main.TileSet[i + 1, k].Type = 5;
				num5 = genRand.Next(3);
				if (genRand.Next(3) < 2)
				{
					Main.TileSet[i + 1, k].FrameX = 66;
					Main.TileSet[i + 1, k].FrameY = (short)(198 + num5 * 22);
				}
				else
				{
					Main.TileSet[i + 1, k].FrameX = 88;
					Main.TileSet[i + 1, k].FrameY = (short)(66 + num5 * 22);
				}
			}
			do
			{
				num3 = genRand.Next(10);
			}
			while (((num3 == 5 || num3 == 7) & flag3) || ((num3 == 6 || num3 == 7) & flag4));
		}
		int num6 = genRand.Next(3);
		bool flag5 = false;
		bool flag6 = false;
		fixed (Tile* ptr = &Main.TileSet[i - 1, j])
		{
			if (ptr->IsActive != 0)
			{
				switch ((EntityID.TileID)ptr->Type)
				{
				case EntityID.TileID.GRASS:
				case EntityID.TileID.CORRUPT_GRASS:
				case EntityID.TileID.JUNGLE_GRASS:
				case EntityID.TileID.HALLOWED_GRASS:
				case EntityID.TileID.SNOW:
					flag5 = true;
					break;
				}
			}
		}
		fixed (Tile* ptr2 = &Main.TileSet[i + 1, j])
		{
			if (ptr2->IsActive != 0)
			{
				switch ((EntityID.TileID)ptr2->Type)
				{
				case EntityID.TileID.GRASS:
				case EntityID.TileID.CORRUPT_GRASS:
				case EntityID.TileID.JUNGLE_GRASS:
				case EntityID.TileID.HALLOWED_GRASS:
				case EntityID.TileID.SNOW:
					flag6 = true;
					break;
				}
			}
		}
		if (!flag5)
		{
			if (num6 == 0)
			{
				num6 = 2;
			}
			if (num6 == 1)
			{
				num6 = 3;
			}
		}
		if (!flag6)
		{
			if (num6 == 0)
			{
				num6 = 1;
			}
			if (num6 == 2)
			{
				num6 = 3;
			}
		}
		if (flag6 && !flag5)
		{
			num6 = 1;
		}
		if (flag5 && !flag6)
		{
			num6 = 2;
		}
		if (num6 == 0 || num6 == 1)
		{
			Main.TileSet[i + 1, j - 1].IsActive = 1;
			Main.TileSet[i + 1, j - 1].Type = 5;
			num5 = genRand.Next(3);
			Main.TileSet[i + 1, j - 1].FrameX = 22;
			Main.TileSet[i + 1, j - 1].FrameY = (short)(132 + 22 * num5);
		}
		if (num6 == 0 || num6 == 2)
		{
			Main.TileSet[i - 1, j - 1].IsActive = 1;
			Main.TileSet[i - 1, j - 1].Type = 5;
			num5 = genRand.Next(3);
			Main.TileSet[i - 1, j - 1].FrameX = 44;
			Main.TileSet[i - 1, j - 1].FrameY = (short)(132 + 22 * num5);
		}
		num5 = genRand.Next(3);
		switch (num6)
		{
		case 0:
			Main.TileSet[i, j - 1].FrameX = 88;
			Main.TileSet[i, j - 1].FrameY = (short)(132 + 22 * num5);
			break;
		case 1:
			Main.TileSet[i, j - 1].FrameX = 0;
			Main.TileSet[i, j - 1].FrameY = (short)(132 + 22 * num5);
			break;
		case 2:
			Main.TileSet[i, j - 1].FrameX = 66;
			Main.TileSet[i, j - 1].FrameY = (short)(132 + 22 * num5);
			break;
		}
		num5 = genRand.Next(3);
		if (genRand.Next(4) < 3)
		{
			Main.TileSet[i, j - num4].FrameX = 22;
			Main.TileSet[i, j - num4].FrameY = (short)(198 + 22 * num5);
		}
		else
		{
			Main.TileSet[i, j - num4].FrameX = 0;
			Main.TileSet[i, j - num4].FrameY = (short)(198 + 22 * num5);
		}
		RangeFrame(i - 2, j - num4 - 1, i + 2, j + 1);
		if (Main.NetMode == 2)
		{
			NetMessage.SendTileSquare(i, (int)((double)j - (double)num4 * 0.5), num4 + 1);
		}
		static bool IsValidGrass(int TileType)
		{
			if (TileType != 2 && TileType != 23 && TileType != 60 && TileType != 109)
			{
				return TileType == 147;
			}
			return true;
		}
	}

	public static void GrowShroom(int i, int y)
	{
		Tile tile = Main.TileSet[i - 1, y];
		Tile tile2 = Main.TileSet[i + 1, y];
		bool flag = Main.TileSet[i - 1, y - 1].Lava != 0 || Main.TileSet[i + 1, y - 1].Lava != 0;
		bool flag2 = Main.TileSet[i, y].IsActive == 0 || Main.TileSet[i, y].Type != 70;
		bool flag3 = tile.IsActive == 0 || tile.Type != 70 || tile2.IsActive == 0 || tile2.Type != 70;
		if (!(flag | flag2 | flag3) && Main.TileSet[i, y - 1].WallType == 0 && EmptyTileCheckShroom(i - 2, i + 2, y - 13, y - 1))
		{
			int num = genRand.Next(4, 11);
			int num2;
			for (int j = y - num; j < y; j++)
			{
				Main.TileSet[i, j].frameNumber = (byte)genRand.Next(3);
				Main.TileSet[i, j].IsActive = 1;
				Main.TileSet[i, j].Type = 72;
				num2 = genRand.Next(3);
				Main.TileSet[i, j].FrameX = 0;
				Main.TileSet[i, j].FrameY = (short)(18 * num2);
			}
			num2 = genRand.Next(3);
			Main.TileSet[i, y - num].FrameX = 36;
			Main.TileSet[i, y - num].FrameY = (short)(18 * num2);
			RangeFrame(i - 2, y - num - 1, i + 2, y + 1);
			if (Main.NetMode == 2)
			{
				NetMessage.SendTileSquare(i, (int)((double)y - (double)num * 0.5), num + 1);
			}
		}
	}

	public static void AddTrees()
	{
		for (int i = 1; i < Main.MaxTilesX - 1; i++)
		{
			for (int j = 20; j < Main.WorldSurface; j++)
			{
				GrowTree(i, j);
			}
			int num = genRand.Next(12);
			if (num <= 6)
			{
				i++;
				if (num == 0)
				{
					i++;
				}
			}
		}
	}

	public unsafe static bool EmptyTileCheck(int startX, int endX, int startY, int endY)
	{
		if (startX < 0)
		{
			return false;
		}
		if (endX >= Main.MaxTilesX)
		{
			return false;
		}
		if (startY < 0)
		{
			return false;
		}
		if (endY >= Main.MaxTilesY)
		{
			return false;
		}
		fixed (Tile* tileSet = Main.TileSet)
		{
			do
			{
				int num = startY;
				Tile* ptr = tileSet + (startX * 1440 + num);
				do
				{
					if (ptr->IsActive != 0)
					{
						return false;
					}
					ptr++;
				}
				while (++num <= endY);
			}
			while (++startX <= endX);
		}
		return true;
	}

	public unsafe static bool EmptyTileCheckTree(int startX, int endX, int startY, int endY)
	{
		if (startX < 0)
		{
			return false;
		}
		if (endX >= Main.MaxTilesX)
		{
			return false;
		}
		if (startY < 0)
		{
			return false;
		}
		if (endY >= Main.MaxTilesY)
		{
			return false;
		}
		fixed (Tile* tileSet = Main.TileSet)
		{
			do
			{
				int num = startY;
				Tile* ptr = tileSet + (startX * 1440 + num);
				do
				{
					if (ptr->IsActive != 0)
					{
						switch ((EntityID.TileID)ptr->Type)
						{
						default:
							return false;
						case EntityID.TileID.SHORT_GRASS_PLANTS:
						case EntityID.TileID.SAPLING:
						case EntityID.TileID.SHORT_CORRUPT_PLANTS:
						case EntityID.TileID.CORRUPTION_THORN:
						case EntityID.TileID.SHORT_JUNGLE_PLANTS:
						case EntityID.TileID.JUNGLE_THORN:
						case EntityID.TileID.TALL_GRASS_PLANTS:
						case EntityID.TileID.TALL_JUNGLE_PLANTS:
						case EntityID.TileID.SHORT_HALLOWED_PLANTS:
						case EntityID.TileID.TALL_HALLOWED_PLANTS:
							break;
						}
					}
					ptr++;
				}
				while (++num <= endY);
			}
			while (++startX <= endX);
		}
		return true;
	}

	public unsafe static bool EmptyTileCheckShroom(int startX, int endX, int startY, int endY)
	{
		if (startX < 0)
		{
			return false;
		}
		if (endX >= Main.MaxTilesX)
		{
			return false;
		}
		if (startY < 0)
		{
			return false;
		}
		if (endY >= Main.MaxTilesY)
		{
			return false;
		}
		fixed (Tile* tileSet = Main.TileSet)
		{
			do
			{
				int num = startY;
				Tile* ptr = tileSet + (startX * 1440 + num);
				do
				{
					if (ptr->IsActive != 0 && ptr->Type != 71)
					{
						return false;
					}
					ptr++;
				}
				while (++num <= endY);
			}
			while (++startX <= endX);
		}
		return true;
	}

	public static void StartHardmodeCallBack()
	{
		hardLock = true;
		float num = (float)genRand.Next(300, 400) * 0.001f;
		int num2 = (int)((float)Main.MaxTilesX * num);
		int num3 = (int)((float)Main.MaxTilesX * (1f - num));
		int num4 = 1;
		if (genRand.Next(2) == 0)
		{
			num4 = num3;
			num3 = num2;
			num2 = num4;
			num4 = -1;
		}
		Vector2i minArea = new Vector2i
		{
			X = Main.MaxTilesX,
			Y = Main.MaxTilesY
		};
		Vector2i maxArea = default;
		GERunner(num2, new Vector2(3 * num4, 5f), good: true, ref minArea, ref maxArea);
		GERunner(num3, new Vector2(-3 * num4, 5f), good: false, ref minArea, ref maxArea);
		Netplay.ResetSections(ref minArea, ref maxArea);
		Main.InHardMode = true;
		hardLock = false;
	}

	public static void StartHardmode()
	{
		if (Main.NetMode != 1 && !Main.InHardMode)
		{
			Thread thread = new Thread(StartHardmodeCallBack);
			thread.IsBackground = true;
			thread.Start();
			NetMessage.SendText(15, 50, 255, 130, -1);
			UI.SetTriggerStateForAll(Trigger.UnlockedHardMode);
			if (Main.HardmodeAlert)
			{
				Main.PlaySound(10);
				UI.MainUI.CurMenuType = MenuType.PAUSE;
				UI.MainUI.SetMenu(MenuMode.HARDMODE_UPSELL);
			}
		}
	}

	public unsafe static bool PlaceDoor(int i, int j, int type)
	{
		if (j >= 2 && j < Main.MaxTilesY - 2)
		{
			fixed (Tile* ptr = &Main.TileSet[i, j])
			{
				if (ptr[-2].IsActive != 0 && Main.TileSolid[ptr[-2].Type] && ptr[2].IsActive != 0 && Main.TileSolid[ptr[2].Type])
				{
					ptr[-1].IsActive = 1;
					ptr[-1].Type = 10;
					ptr[-1].FrameY = 0;
					ptr[-1].FrameX = (short)(genRand.Next(3) * 18);
					ptr->IsActive = 1;
					ptr->Type = 10;
					ptr->FrameY = 18;
					ptr->FrameX = (short)(genRand.Next(3) * 18);
					ptr[1].IsActive = 1;
					ptr[1].Type = 10;
					ptr[1].FrameY = 36;
					ptr[1].FrameX = (short)(genRand.Next(3) * 18);
					return true;
				}
			}
		}
		return false;
	}

	public static bool CanCloseDoor(int i, int j)
	{
		int num = i;
		int num2 = j;
		switch (Main.TileSet[i, j].FrameX)
		{
		case 18:
			num--;
			break;
		case 36:
			num++;
			break;
		}
		int frameY = Main.TileSet[i, j].FrameY;
		num2 -= frameY / 18;
		return !Collision.AnyPlayerOrNPC(num, num2, 3);
	}

	public static bool CloseDoor(int i, int j, bool forced = false)
	{
		int num = 0;
		int num2 = i;
		int num3 = j;
		switch (Main.TileSet[i, j].FrameX)
		{
		case 0:
			num = 1;
			break;
		case 18:
			num2--;
			num = 1;
			break;
		case 36:
			num2++;
			num = -1;
			break;
		case 54:
			num = -1;
			break;
		}
		int frameY = Main.TileSet[i, j].FrameY;
		num3 -= frameY / 18;
		if (!forced && Collision.AnyPlayerOrNPC(num2, num3, 3))
		{
			return false;
		}
		int num4 = num2;
		if (num == -1)
		{
			num4--;
		}
		for (int k = num4; k < num4 + 2; k++)
		{
			for (int l = num3; l < num3 + 3; l++)
			{
				if (k == num2)
				{
					Main.TileSet[k, l].Type = 10;
					Main.TileSet[k, l].FrameX = (short)(genRand.Next(3) * 18);
				}
				else
				{
					Main.TileSet[k, l].IsActive = 0;
				}
			}
		}
		if (Main.NetMode != 1)
		{
			for (int m = 0; m < 3; m++)
			{
				if (numNoWire < 999)
				{
					noWire[numNoWire].X = (short)num2;
					noWire[numNoWire].Y = (short)(num3 + m);
					numNoWire++;
				}
			}
		}
		bool flag = tileFrameRecursion;
		tileFrameRecursion = false;
		TileFrame(num2 - 1, num3 - 1);
		TileFrame(num2 - 1, num3);
		TileFrame(num2 - 1, num3 + 1);
		TileFrame(num2 - 1, num3 + 2);
		TileFrame(num2, num3 - 1);
		TileFrame(num2, num3);
		TileFrame(num2, num3 + 1);
		TileFrame(num2, num3 + 2);
		TileFrame(num2 + 1, num3 - 1);
		TileFrame(num2 + 1, num3);
		TileFrame(num2 + 1, num3 + 1);
		TileFrame(num2 + 1, num3 + 2);
		tileFrameRecursion = flag;
		Main.PlaySound(9, i * 16, j * 16);
		return true;
	}

	public unsafe static bool AddLifeCrystal(int i, int j)
	{
		fixed (Tile* tileSet = Main.TileSet)
		{
			Tile* ptr = tileSet + (i * 1440 + j);
			while (j < Main.MaxTilesY)
			{
				if (ptr->IsActive != 0 && Main.TileSolid[ptr->Type])
				{
					if (ptr[-2].Lava != 0 || ptr[-1442].Lava != 0)
					{
						return false;
					}
					if (!EmptyTileCheck(i - 1, i, j - 2, j - 1))
					{
						return false;
					}
					ptr[-1442].IsActive = 1;
					ptr[-1442].Type = 12;
					ptr[-1442].FrameX = 0;
					ptr[-1442].FrameY = 0;
					ptr[-1441].IsActive = 1;
					ptr[-1441].Type = 12;
					ptr[-1441].FrameX = 0;
					ptr[-1441].FrameY = 18;
					ptr[-2].IsActive = 1;
					ptr[-2].Type = 12;
					ptr[-2].FrameX = 18;
					ptr[-2].FrameY = 0;
					ptr[-1].IsActive = 1;
					ptr[-1].Type = 12;
					ptr[-1].FrameX = 18;
					ptr[-1].FrameY = 18;
					return true;
				}
				j++;
				ptr++;
			}
		}
		return false;
	}

	public static void AddShadowOrb(int x, int y)
	{
		if (x < 10 || x > Main.MaxTilesX - 10 || y < 10 || y > Main.MaxTilesY - 10)
		{
			return;
		}
		for (int i = x - 1; i < x + 1; i++)
		{
			for (int j = y - 1; j < y + 1; j++)
			{
				if (Main.TileSet[i, j].IsActive != 0 && Main.TileSet[i, j].Type == 31)
				{
					return;
				}
			}
		}
		Main.TileSet[x - 1, y - 1].IsActive = 1;
		Main.TileSet[x - 1, y - 1].Type = 31;
		Main.TileSet[x - 1, y - 1].FrameX = 0;
		Main.TileSet[x - 1, y - 1].FrameY = 0;
		Main.TileSet[x, y - 1].IsActive = 1;
		Main.TileSet[x, y - 1].Type = 31;
		Main.TileSet[x, y - 1].FrameX = 18;
		Main.TileSet[x, y - 1].FrameY = 0;
		Main.TileSet[x - 1, y].IsActive = 1;
		Main.TileSet[x - 1, y].Type = 31;
		Main.TileSet[x - 1, y].FrameX = 0;
		Main.TileSet[x - 1, y].FrameY = 18;
		Main.TileSet[x, y].IsActive = 1;
		Main.TileSet[x, y].Type = 31;
		Main.TileSet[x, y].FrameX = 18;
		Main.TileSet[x, y].FrameY = 18;
	}

	public static void AddHellHouses()
	{
		int num = (int)((double)Main.MaxTilesX * 0.25);
		for (int i = num; i < Main.MaxTilesX - num; i++)
		{
			int num2 = Main.MaxTilesY - 40;
			while (Main.TileSet[i, num2].IsActive != 0 || Main.TileSet[i, num2].Liquid > 0)
			{
				num2--;
			}
			if (Main.TileSet[i, num2 + 1].IsActive != 0)
			{
				int type;
				int wall;
				if (genRand.Next(10) == 0)
				{
					type = 76;
					wall = 13;
				}
				else
				{
					type = 75;
					wall = 14;
				}
				HellHouse(i, num2, type, wall);
				i += genRand.Next(15, 80);
			}
		}
		float num3 = (float)Main.MaxTilesX / 4200f;
		for (int j = 0; (float)j < 200f * num3; j++)
		{
			int num4 = 0;
			bool flag = false;
			while (!flag)
			{
				num4++;
				int num5 = genRand.Next((int)((double)Main.MaxTilesX * 0.2), (int)((double)Main.MaxTilesX * 0.8));
				int num6 = genRand.Next(Main.MaxTilesY - 300, Main.MaxTilesY - 20);
				if (Main.TileSet[num5, num6].IsActive != 0 && (Main.TileSet[num5, num6].Type == 75 || Main.TileSet[num5, num6].Type == 76))
				{
					int num7 = 0;
					if (Main.TileSet[num5 - 1, num6].WallType > 0)
					{
						num7 = -1;
					}
					else if (Main.TileSet[num5 + 1, num6].WallType > 0)
					{
						num7 = 1;
					}
					if (Main.TileSet[num5 + num7, num6].IsActive == 0 && Main.TileSet[num5 + num7, num6 + 1].IsActive == 0)
					{
						bool flag2 = false;
						for (int k = num5 - 8; k < num5 + 8; k++)
						{
							for (int l = num6 - 8; l < num6 + 8; l++)
							{
								if (Main.TileSet[k, l].IsActive != 0 && Main.TileSet[k, l].Type == 4)
								{
									flag2 = true;
									break;
								}
							}
						}
						if (!flag2)
						{
							PlaceTile(num5 + num7, num6, 4, ToMute: true, IsForced: true, -1, 7);
							flag = true;
						}
					}
				}
				if (num4 > 1000)
				{
					flag = true;
				}
			}
		}
	}

	public static void HellHouse(int i, int j, int type, int wall)
	{
		int num = genRand.Next(8, 20);
		int num2 = genRand.Next(1, 3);
		int num3 = genRand.Next(4, 13);
		int num4 = j;
		for (int k = 0; k < num2; k++)
		{
			int num5 = genRand.Next(5, 9);
			HellRoom(i, num4, num, num5, type, wall);
			num4 -= num5;
		}
		num4 = j;
		for (int l = 0; l < num3; l++)
		{
			int num6 = genRand.Next(5, 9);
			num4 += num6;
			HellRoom(i, num4, num, num6, type, wall);
		}
		for (int m = i - (num >> 1); m <= i + (num >> 1); m++)
		{
			for (num4 = j; num4 < Main.MaxTilesY && ((Main.TileSet[m, num4].IsActive != 0 && (Main.TileSet[m, num4].Type == 76 || Main.TileSet[m, num4].Type == 75)) || Main.TileSet[i, num4].WallType == 13 || Main.TileSet[i, num4].WallType == 14); num4++)
			{
			}
			int num7 = 6 + genRand.Next(3);
			while (num4 < Main.MaxTilesY && Main.TileSet[m, num4].IsActive == 0)
			{
				num7--;
				Main.TileSet[m, num4].IsActive = 1;
				Main.TileSet[m, num4].Type = 57;
				num4++;
				if (num7 <= 0)
				{
					break;
				}
			}
		}
		int num8 = 0;
		int num9 = 0;
		for (num4 = j; num4 < Main.MaxTilesY && ((Main.TileSet[i, num4].IsActive != 0 && (Main.TileSet[i, num4].Type == 76 || Main.TileSet[i, num4].Type == 75)) || Main.TileSet[i, num4].WallType == 13 || Main.TileSet[i, num4].WallType == 14); num4++)
		{
		}
		num4--;
		num9 = num4;
		while ((Main.TileSet[i, num4].IsActive != 0 && (Main.TileSet[i, num4].Type == 76 || Main.TileSet[i, num4].Type == 75)) || Main.TileSet[i, num4].WallType == 13 || Main.TileSet[i, num4].WallType == 14)
		{
			num4--;
			if (Main.TileSet[i, num4].IsActive == 0 || (Main.TileSet[i, num4].Type != 76 && Main.TileSet[i, num4].Type != 75))
			{
				continue;
			}
			int num10 = genRand.Next(i - (num >> 1) + 1, i + (num >> 1) - 1);
			int num11 = genRand.Next(i - (num >> 1) + 1, i + (num >> 1) - 1);
			if (num10 > num11)
			{
				int num12 = num10;
				num10 = num11;
				num11 = num12;
			}
			if (num10 == num11)
			{
				if (num10 < i)
				{
					num11++;
				}
				else
				{
					num10--;
				}
			}
			for (int n = num10; n <= num11; n++)
			{
				if (Main.TileSet[n, num4 - 1].WallType == 13)
				{
					Main.TileSet[n, num4].WallType = 13;
				}
				if (Main.TileSet[n, num4 - 1].WallType == 14)
				{
					Main.TileSet[n, num4].WallType = 14;
				}
				Main.TileSet[n, num4].Type = 19;
				Main.TileSet[n, num4].IsActive = 1;
			}
			num4--;
		}
		num8 = num4;
		float num13 = (float)((num9 - num8) * num) * 0.02f;
		for (int num14 = 0; (float)num14 < num13; num14++)
		{
			int num15 = genRand.Next(i - (num >> 1), i + (num >> 1) + 1);
			int num16 = genRand.Next(num8, num9);
			int num17 = genRand.Next(3, 8);
			float num18 = (float)num17 * 0.4f;
			num18 *= num18;
			for (int num19 = num15 - num17; num19 <= num15 + num17; num19++)
			{
				float num20 = num19 - num15;
				num20 *= num20;
				for (int num21 = num16 - num17; num21 <= num16 + num17; num21++)
				{
					float num22 = num21 - num16;
					if (!(num20 + num22 * num22 < num18))
					{
						continue;
					}
					try
					{
						if (Main.TileSet[num19, num21].Type == 76 || Main.TileSet[num19, num21].Type == 19)
						{
							Main.TileSet[num19, num21].IsActive = 0;
						}
						Main.TileSet[num19, num21].WallType = 0;
					}
					catch
					{
					}
				}
			}
		}
	}

	public static void HellRoom(int i, int j, int width, int height, int type, int wall)
	{
		if (j > Main.MaxTilesY - 40)
		{
			return;
		}
		width >>= 1;
		for (int k = i - width; k <= i + width; k++)
		{
			for (int l = j - height; l <= j; l++)
			{
				try
				{
					Main.TileSet[k, l].IsActive = 1;
					Main.TileSet[k, l].Type = (byte)type;
					Main.TileSet[k, l].Liquid = 0;
					Main.TileSet[k, l].Lava = 0;
				}
				catch
				{
				}
			}
		}
		for (int m = i - width + 1; m <= i + width - 1; m++)
		{
			for (int n = j - height + 1; n <= j - 1; n++)
			{
				try
				{
					Main.TileSet[m, n].IsActive = 0;
					Main.TileSet[m, n].WallType = (byte)wall;
					Main.TileSet[m, n].Liquid = 0;
					Main.TileSet[m, n].Lava = 0;
				}
				catch
				{
				}
			}
		}
	}

	public static void MakeDungeon(int x, int y, int tileType = 41, int wallType = 7)
	{
		int num = genRand.Next(3);
		int num2 = genRand.Next(3);
		switch (num)
		{
		case 1:
			tileType = 43;
			break;
		case 2:
			tileType = 44;
			break;
		}
		switch (num2)
		{
		case 1:
			wallType = 8;
			break;
		case 2:
			wallType = 9;
			break;
		}
		numDDoors = 0;
		numDPlats = 0;
		numDRooms = 0;
		dungeonX = x;
		dungeonY = y;
		dMinX = x;
		dMaxX = x;
		dMinY = y;
		dMaxY = y;
		dxStrength1 = genRand.Next(25, 30);
		dyStrength1 = genRand.Next(20, 25);
		dxStrength2 = genRand.Next(35, 50);
		dyStrength2 = genRand.Next(10, 15);
		int num3 = Main.MaxTilesX / 60;
		num3 += genRand.Next(num3 / 3);
		int num4 = num3;
		int num5 = 5;
		DungeonRoom(dungeonX, dungeonY, tileType, wallType);
		while (num3 > 0)
		{
			if (dungeonX < dMinX)
			{
				dMinX = dungeonX;
			}
			if (dungeonX > dMaxX)
			{
				dMaxX = dungeonX;
			}
			if (dungeonY > dMaxY)
			{
				dMaxY = dungeonY;
			}
			num3--;
			UI.MainUI.Progress = (float)(num4 - num3) * 0.6f / (float)num4;
			if (num5 > 0)
			{
				num5--;
			}
			if ((num5 == 0) & (genRand.Next(3) == 0))
			{
				num5 = 5;
				if (genRand.Next(2) == 0)
				{
					int num6 = dungeonX;
					int num7 = dungeonY;
					DungeonHalls(dungeonX, dungeonY, tileType, wallType);
					if (genRand.Next(2) == 0)
					{
						DungeonHalls(dungeonX, dungeonY, tileType, wallType);
					}
					DungeonRoom(dungeonX, dungeonY, tileType, wallType);
					dungeonX = num6;
					dungeonY = num7;
				}
				else
				{
					DungeonRoom(dungeonX, dungeonY, tileType, wallType);
				}
			}
			else
			{
				DungeonHalls(dungeonX, dungeonY, tileType, wallType);
			}
		}
		DungeonRoom(dungeonX, dungeonY, tileType, wallType);
		int x2 = dRoom[0].X;
		int y2 = dRoom[0].Y;
		for (int i = 1; i < numDRooms; i++)
		{
			if (dRoom[i].Y < y2)
			{
				x2 = dRoom[i].X;
				y2 = dRoom[i].Y;
			}
		}
		dungeonX = x2;
		dungeonY = y2;
		dEnteranceX = x2;
		dSurface = false;
		num5 = 5;
		while (!dSurface)
		{
			if (num5 > 0)
			{
				num5--;
			}
			if (((num5 == 0) & (genRand.Next(5) == 0)) && dungeonY > Main.WorldSurface + 50)
			{
				num5 = 10;
				int num8 = dungeonX;
				int num9 = dungeonY;
				DungeonHalls(dungeonX, dungeonY, tileType, wallType, forceX: true);
				DungeonRoom(dungeonX, dungeonY, tileType, wallType);
				dungeonX = num8;
				dungeonY = num9;
			}
			DungeonStairs(dungeonX, dungeonY, tileType, wallType);
		}
		DungeonEnt(dungeonX, dungeonY, tileType, wallType);
		UI.MainUI.Progress = 0.65f;
		for (int j = 0; j < numDRooms; j++)
		{
			for (int k = dRoom[j].L; k <= dRoom[j].R; k++)
			{
				int num10 = dRoom[j].T - 1;
				if (Main.TileSet[k, num10].IsActive == 0)
				{
					DPlat[numDPlats].X = (short)k;
					DPlat[numDPlats].Y = (short)num10;
					numDPlats++;
					break;
				}
			}
			for (int l = dRoom[j].L; l <= dRoom[j].R; l++)
			{
				int num11 = dRoom[j].B + 1;
				if (Main.TileSet[l, num11].IsActive == 0)
				{
					DPlat[numDPlats].X = (short)l;
					DPlat[numDPlats].Y = (short)num11;
					numDPlats++;
					break;
				}
			}
			for (int m = dRoom[j].T; m <= dRoom[j].B; m++)
			{
				int num12 = dRoom[j].L - 1;
				if (Main.TileSet[num12, m].IsActive == 0)
				{
					dDoor[numDDoors].X = (short)num12;
					dDoor[numDDoors].Y = (short)m;
					dDoor[numDDoors].Pos = -1;
					numDDoors++;
					break;
				}
			}
			for (int n = dRoom[j].T; n <= dRoom[j].B; n++)
			{
				int num13 = dRoom[j].R + 1;
				if (Main.TileSet[num13, n].IsActive == 0)
				{
					dDoor[numDDoors].X = (short)num13;
					dDoor[numDDoors].Y = (short)n;
					dDoor[numDDoors].Pos = 1;
					numDDoors++;
					break;
				}
			}
		}
		UI.MainUI.Progress = 0.7f;
		int num14 = 0;
		int num15 = 1000;
		int num16 = 0;
		while (num16 < Main.MaxTilesX / 100)
		{
			num14++;
			int num17 = genRand.Next(dMinX, dMaxX);
			int num18 = genRand.Next(Main.WorldSurface + 25, dMaxY);
			int num19 = num17;
			if (Main.TileSet[num17, num18].WallType == wallType && Main.TileSet[num17, num18].IsActive == 0)
			{
				int num20 = 1;
				if (genRand.Next(2) == 0)
				{
					num20 = -1;
				}
				for (; Main.TileSet[num17, num18].IsActive == 0; num18 += num20)
				{
				}
				if (Main.TileSet[num17 - 1, num18].IsActive != 0 && Main.TileSet[num17 + 1, num18].IsActive != 0 && Main.TileSet[num17 - 1, num18 - num20].IsActive == 0 && Main.TileSet[num17 + 1, num18 - num20].IsActive == 0)
				{
					num16++;
					int num21 = genRand.Next(5, 13);
					while (Main.TileSet[num17 - 1, num18].IsActive != 0 && Main.TileSet[num17, num18 + num20].IsActive != 0 && Main.TileSet[num17, num18].IsActive != 0 && Main.TileSet[num17, num18 - num20].IsActive == 0 && num21 > 0)
					{
						Main.TileSet[num17, num18].Type = 48;
						if (Main.TileSet[num17 - 1, num18 - num20].IsActive == 0 && Main.TileSet[num17 + 1, num18 - num20].IsActive == 0)
						{
							Main.TileSet[num17, num18 - num20].Type = 48;
							Main.TileSet[num17, num18 - num20].IsActive = 1;
						}
						num17--;
						num21--;
					}
					num21 = genRand.Next(5, 13);
					num17 = num19 + 1;
					while (Main.TileSet[num17 + 1, num18].IsActive != 0 && Main.TileSet[num17, num18 + num20].IsActive != 0 && Main.TileSet[num17, num18].IsActive != 0 && Main.TileSet[num17, num18 - num20].IsActive == 0 && num21 > 0)
					{
						Main.TileSet[num17, num18].Type = 48;
						if (Main.TileSet[num17 - 1, num18 - num20].IsActive == 0 && Main.TileSet[num17 + 1, num18 - num20].IsActive == 0)
						{
							Main.TileSet[num17, num18 - num20].Type = 48;
							Main.TileSet[num17, num18 - num20].IsActive = 1;
						}
						num17++;
						num21--;
					}
				}
			}
			if (num14 > num15)
			{
				num14 = 0;
				num16++;
			}
		}
		num14 = 0;
		num15 = 1000;
		num16 = 0;
		UI.MainUI.Progress = 0.75f;
		while (num16 < Main.MaxTilesX / 100)
		{
			num14++;
			int num22 = genRand.Next(dMinX, dMaxX);
			int num23 = genRand.Next(Main.WorldSurface + 25, dMaxY);
			int num24 = num23;
			if (Main.TileSet[num22, num23].WallType == wallType && Main.TileSet[num22, num23].IsActive == 0)
			{
				int num25 = 1;
				if (genRand.Next(2) == 0)
				{
					num25 = -1;
				}
				for (; num22 > 5 && num22 < Main.MaxTilesX - 5 && Main.TileSet[num22, num23].IsActive == 0; num22 += num25)
				{
				}
				if (Main.TileSet[num22, num23 - 1].IsActive != 0 && Main.TileSet[num22, num23 + 1].IsActive != 0 && Main.TileSet[num22 - num25, num23 - 1].IsActive == 0 && Main.TileSet[num22 - num25, num23 + 1].IsActive == 0)
				{
					num16++;
					int num26 = genRand.Next(5, 13);
					while (Main.TileSet[num22, num23 - 1].IsActive != 0 && Main.TileSet[num22 + num25, num23].IsActive != 0 && Main.TileSet[num22, num23].IsActive != 0 && Main.TileSet[num22 - num25, num23].IsActive == 0 && num26 > 0)
					{
						Main.TileSet[num22, num23].Type = 48;
						if (Main.TileSet[num22 - num25, num23 - 1].IsActive == 0 && Main.TileSet[num22 - num25, num23 + 1].IsActive == 0)
						{
							Main.TileSet[num22 - num25, num23].Type = 48;
							Main.TileSet[num22 - num25, num23].IsActive = 1;
						}
						num23--;
						num26--;
					}
					num26 = genRand.Next(5, 13);
					num23 = num24 + 1;
					while (Main.TileSet[num22, num23 + 1].IsActive != 0 && Main.TileSet[num22 + num25, num23].IsActive != 0 && Main.TileSet[num22, num23].IsActive != 0 && Main.TileSet[num22 - num25, num23].IsActive == 0 && num26 > 0)
					{
						Main.TileSet[num22, num23].Type = 48;
						if (Main.TileSet[num22 - num25, num23 - 1].IsActive == 0 && Main.TileSet[num22 - num25, num23 + 1].IsActive == 0)
						{
							Main.TileSet[num22 - num25, num23].Type = 48;
							Main.TileSet[num22 - num25, num23].IsActive = 1;
						}
						num23++;
						num26--;
					}
				}
			}
			if (num14 > num15)
			{
				num14 = 0;
				num16++;
			}
		}
		UI.MainUI.Progress = 0.8f;
		for (int num27 = 0; num27 < numDDoors; num27++)
		{
			int num28 = dDoor[num27].X - 10;
			int num29 = dDoor[num27].X + 10;
			int num30 = 100;
			int num31 = 0;
			int num32 = 0;
			int num33 = 0;
			for (int num34 = num28; num34 < num29; num34++)
			{
				bool flag = true;
				int num35 = dDoor[num27].Y;
				while (Main.TileSet[num34, num35].IsActive == 0)
				{
					num35--;
				}
				if (!Main.TileDungeon[Main.TileSet[num34, num35].Type])
				{
					flag = false;
				}
				num32 = num35;
				for (num35 = dDoor[num27].Y; Main.TileSet[num34, num35].IsActive == 0; num35++)
				{
				}
				if (!Main.TileDungeon[Main.TileSet[num34, num35].Type])
				{
					flag = false;
				}
				num33 = num35;
				if (num33 - num32 < 3)
				{
					continue;
				}
				int num36 = num34 - 20;
				int num37 = num34 + 20;
				int num38 = num33 - 10;
				int num39 = num33 + 10;
				for (int num40 = num36; num40 < num37; num40++)
				{
					for (int num41 = num38; num41 < num39; num41++)
					{
						if (Main.TileSet[num40, num41].IsActive != 0 && Main.TileSet[num40, num41].Type == 10)
						{
							flag = false;
							break;
						}
					}
				}
				if (flag)
				{
					for (int num42 = num33 - 3; num42 < num33; num42++)
					{
						for (int num43 = num34 - 3; num43 <= num34 + 3; num43++)
						{
							if (Main.TileSet[num43, num42].IsActive != 0)
							{
								flag = false;
								break;
							}
						}
					}
				}
				if (flag && num33 - num32 < 20 && ((dDoor[num27].Pos == 0 && num33 - num32 < num30) || (dDoor[num27].Pos == -1 && num34 > num31) || (dDoor[num27].Pos == 1 && (num34 < num31 || num31 == 0))))
				{
					num31 = num34;
					num30 = num33 - num32;
				}
			}
			if (num30 >= 20)
			{
				continue;
			}
			int num44 = num31;
			int num45 = dDoor[num27].Y;
			int num46 = num45;
			for (; Main.TileSet[num44, num45].IsActive == 0; num45++)
			{
				Main.TileSet[num44, num45].IsActive = 0;
			}
			while (Main.TileSet[num44, num46].IsActive == 0)
			{
				num46--;
			}
			num45--;
			num46++;
			for (int num47 = num46; num47 < num45 - 2; num47++)
			{
				Main.TileSet[num44, num47].IsActive = 1;
				Main.TileSet[num44, num47].Type = (byte)tileType;
			}
			PlaceTile(num44, num45, 10, ToMute: true);
			num44--;
			int num48 = num45 - 3;
			while (Main.TileSet[num44, num48].IsActive == 0)
			{
				num48--;
			}
			if (num45 - num48 < num45 - num46 + 5 && Main.TileDungeon[Main.TileSet[num44, num48].Type])
			{
				for (int num49 = num45 - 4 - genRand.Next(3); num49 > num48; num49--)
				{
					Main.TileSet[num44, num49].IsActive = 1;
					Main.TileSet[num44, num49].Type = (byte)tileType;
				}
			}
			num44 += 2;
			num48 = num45 - 3;
			while (Main.TileSet[num44, num48].IsActive == 0)
			{
				num48--;
			}
			if (num45 - num48 < num45 - num46 + 5 && Main.TileDungeon[Main.TileSet[num44, num48].Type])
			{
				for (int num50 = num45 - 4 - genRand.Next(3); num50 > num48; num50--)
				{
					Main.TileSet[num44, num50].IsActive = 1;
					Main.TileSet[num44, num50].Type = (byte)tileType;
				}
			}
			num45++;
			num44--;
			Main.TileSet[num44 - 1, num45].IsActive = 1;
			Main.TileSet[num44 - 1, num45].Type = (byte)tileType;
			Main.TileSet[num44 + 1, num45].IsActive = 1;
			Main.TileSet[num44 + 1, num45].Type = (byte)tileType;
		}
		UI.MainUI.Progress = 0.85f;
		for (int num51 = 0; num51 < numDPlats; num51++)
		{
			int x3 = DPlat[num51].X;
			int y3 = DPlat[num51].Y;
			int num52 = Main.MaxTilesX;
			int num53 = 10;
			for (int num54 = y3 - 5; num54 <= y3 + 5; num54++)
			{
				int num55 = x3;
				int num56 = x3;
				bool flag2 = false;
				if (Main.TileSet[num55, num54].IsActive != 0)
				{
					flag2 = true;
				}
				else
				{
					while (Main.TileSet[num55, num54].IsActive == 0)
					{
						num55--;
						if (!Main.TileDungeon[Main.TileSet[num55, num54].Type])
						{
							flag2 = true;
						}
					}
					while (Main.TileSet[num56, num54].IsActive == 0)
					{
						num56++;
						if (!Main.TileDungeon[Main.TileSet[num56, num54].Type])
						{
							flag2 = true;
						}
					}
				}
				if (flag2 || num56 - num55 > num53)
				{
					continue;
				}
				bool flag3 = true;
				int num57 = x3 - (num53 >> 1) - 2;
				int num58 = x3 + (num53 >> 1) + 2;
				int num59 = num54 - 5;
				int num60 = num54 + 5;
				for (int num61 = num57; num61 <= num58; num61++)
				{
					for (int num62 = num59; num62 <= num60; num62++)
					{
						if (Main.TileSet[num61, num62].IsActive != 0 && Main.TileSet[num61, num62].Type == 19)
						{
							flag3 = false;
							break;
						}
					}
				}
				for (int num63 = num54 + 3; num63 >= num54 - 5; num63--)
				{
					if (Main.TileSet[x3, num63].IsActive != 0)
					{
						flag3 = false;
						break;
					}
				}
				if (flag3)
				{
					num52 = num54;
					break;
				}
			}
			if (num52 > y3 - 10 && num52 < y3 + 10)
			{
				int num64 = x3;
				int num65 = num52;
				int num66 = x3 + 1;
				while (Main.TileSet[num64, num65].IsActive == 0)
				{
					Main.TileSet[num64, num65].IsActive = 1;
					Main.TileSet[num64, num65].Type = 19;
					num64--;
				}
				for (; Main.TileSet[num66, num65].IsActive == 0; num66++)
				{
					Main.TileSet[num66, num65].IsActive = 1;
					Main.TileSet[num66, num65].Type = 19;
				}
			}
		}
		UI.MainUI.Progress = 0.9f;
		num14 = 0;
		num15 = 1000;
		num16 = 0;
		while (num16 < Main.MaxTilesX / 20)
		{
			num14++;
			int num67 = genRand.Next(dMinX, dMaxX);
			int num68 = genRand.Next(dMinY, dMaxY);
			bool flag4 = true;
			if (Main.TileSet[num67, num68].WallType == wallType && Main.TileSet[num67, num68].IsActive == 0)
			{
				int num69 = 1;
				if (genRand.Next(2) == 0)
				{
					num69 = -1;
				}
				while (flag4 && Main.TileSet[num67, num68].IsActive == 0)
				{
					num67 -= num69;
					if (num67 < 5 || num67 > Main.MaxTilesX - 5)
					{
						flag4 = false;
					}
					else if (Main.TileSet[num67, num68].IsActive != 0 && !Main.TileDungeon[Main.TileSet[num67, num68].Type])
					{
						flag4 = false;
					}
				}
				if (flag4 && Main.TileSet[num67, num68].IsActive != 0 && Main.TileDungeon[Main.TileSet[num67, num68].Type] && Main.TileSet[num67, num68 - 1].IsActive != 0 && Main.TileDungeon[Main.TileSet[num67, num68 - 1].Type] && Main.TileSet[num67, num68 + 1].IsActive != 0 && Main.TileDungeon[Main.TileSet[num67, num68 + 1].Type])
				{
					num67 += num69;
					for (int num70 = num67 - 3; num70 <= num67 + 3; num70++)
					{
						for (int num71 = num68 - 3; num71 <= num68 + 3; num71++)
						{
							if (Main.TileSet[num70, num71].IsActive != 0 && Main.TileSet[num70, num71].Type == 19)
							{
								flag4 = false;
								break;
							}
						}
					}
					if (flag4 && ((Main.TileSet[num67, num68 - 1].IsActive == 0) & (Main.TileSet[num67, num68 - 2].IsActive == 0) & (Main.TileSet[num67, num68 - 3].IsActive == 0)))
					{
						int num72 = num67;
						int num73 = num67;
						for (; num72 > dMinX && num72 < dMaxX && Main.TileSet[num72, num68].IsActive == 0 && Main.TileSet[num72, num68 - 1].IsActive == 0 && Main.TileSet[num72, num68 + 1].IsActive == 0; num72 += num69)
						{
						}
						num72 = Math.Abs(num67 - num72);
						bool flag5 = false;
						if (genRand.Next(2) == 0)
						{
							flag5 = true;
						}
						if (num72 > 5)
						{
							for (int num74 = genRand.Next(1, 4); num74 > 0; num74--)
							{
								Main.TileSet[num67, num68].IsActive = 1;
								Main.TileSet[num67, num68].Type = 19;
								if (flag5)
								{
									PlaceTile(num67, num68 - 1, 50, ToMute: true);
									if (genRand.Next(50) == 0 && Main.TileSet[num67, num68 - 1].Type == 50)
									{
										Main.TileSet[num67, num68 - 1].FrameX = 90;
									}
								}
								num67 += num69;
							}
							num14 = 0;
							num16++;
							if (!flag5 && genRand.Next(2) == 0)
							{
								num67 = num73;
								num68--;
								int num75 = 0;
								if (genRand.Next(4) == 0)
								{
									num75 = 1;
								}
								switch (num75)
								{
								case 0:
									num75 = 13;
									break;
								case 1:
									num75 = 49;
									break;
								}
								PlaceTile(num67, num68, num75, ToMute: true);
								if (Main.TileSet[num67, num68].Type == 13)
								{
									if (genRand.Next(2) == 0)
									{
										Main.TileSet[num67, num68].FrameX = 18;
									}
									else
									{
										Main.TileSet[num67, num68].FrameX = 36;
									}
								}
							}
						}
					}
				}
			}
			if (num14 > num15)
			{
				num14 = 0;
				num16++;
			}
		}
		UI.MainUI.Progress = 0.95f;
		int num76 = 0;
		for (int num77 = 0; num77 < numDRooms; num77++)
		{
			int num78 = 0;
			while (num78 < 1000)
			{
				int num79 = (int)(dRoom[num77].Size * 0.4f);
				int i2 = dRoom[num77].X + genRand.Next(-num79, num79 + 1);
				int num80 = dRoom[num77].Y + genRand.Next(-num79, num79 + 1);
				int num81 = 0;
				num76++;
				int style = 2;
				switch (num76)
				{
				case 1:
					num81 = 329;
					break;
				case 2:
					num81 = 155;
					break;
				case 3:
					num81 = 156;
					break;
				case 4:
					num81 = 157;
					break;
				case 5:
					num81 = 163;
					break;
				case 6:
					num81 = 113;
					break;
				case 7:
					num81 = 327;
					style = 0;
					break;
				default:
					num81 = 164;
					num76 = 0;
					break;
				}
				if (num80 < Main.WorldSurface + 50)
				{
					num81 = 327;
					style = 0;
				}
				if (num81 == 0 && genRand.Next(2) == 0)
				{
					num78 = 1000;
					continue;
				}
				if (AddBuriedChest(i2, num80, num81, notNearOtherChests: false, style))
				{
					num78 += 1000;
				}
				num78++;
			}
		}
		dMinX -= 25;
		dMaxX += 25;
		dMinY -= 25;
		dMaxY += 25;
		if (dMinX < 0)
		{
			dMinX = 0;
		}
		if (dMaxX > Main.MaxTilesX)
		{
			dMaxX = Main.MaxTilesX;
		}
		if (dMinY < 0)
		{
			dMinY = 0;
		}
		if (dMaxY > Main.MaxTilesY)
		{
			dMaxY = Main.MaxTilesY;
		}
		num14 = 0;
		num15 = 1000;
		num16 = 0;
		while (num16 < Main.MaxTilesX / 150)
		{
			num14++;
			int num82 = genRand.Next(dMinX, dMaxX);
			int num83 = genRand.Next(dMinY, dMaxY);
			if (Main.TileSet[num82, num83].WallType == wallType)
			{
				for (int num84 = num83; num84 > dMinY; num84--)
				{
					if (Main.TileSet[num82, num84 - 1].IsActive != 0 && Main.TileSet[num82, num84 - 1].Type == tileType)
					{
						bool flag6 = false;
						for (int num85 = num82 - 15; num85 < num82 + 15; num85++)
						{
							for (int num86 = num84 - 15; num86 < num84 + 15; num86++)
							{
								if (num85 > 0 && num85 < Main.MaxTilesX && num86 > 0 && num86 < Main.MaxTilesY && Main.TileSet[num85, num86].Type == 42)
								{
									flag6 = true;
									break;
								}
							}
						}
						if (Main.TileSet[num82 - 1, num84].IsActive != 0 || Main.TileSet[num82 + 1, num84].IsActive != 0 || Main.TileSet[num82 - 1, num84 + 1].IsActive != 0 || Main.TileSet[num82 + 1, num84 + 1].IsActive != 0 || Main.TileSet[num82, num84 + 2].IsActive != 0)
						{
							flag6 = true;
						}
						if (flag6 || !Place1x2Top(num82, num84, 42))
						{
							break;
						}
						num14 = 0;
						num16++;
						Rectangle HitboxA = default;
						Rectangle HitboxB = new Rectangle
						{
							X = num82 << 4,
							Y = (num84 << 4) + 1
						};
						HitboxA.Width = (HitboxB.Width = 16);
						HitboxA.Height = (HitboxB.Height = 16);
						for (int num87 = 0; num87 < 1000; num87++)
						{
							int num88 = num82 + genRand.Next(-12, 13);
							int num89 = num84 + genRand.Next(3, 21);
							if (Main.TileSet[num88, num89].IsActive != 0 || Main.TileSet[num88, num89 + 1].IsActive != 0 || Main.TileSet[num88 - 1, num89].Type == 48 || Main.TileSet[num88 + 1, num89].Type == 48)
							{
								continue;
							}
							HitboxA.X = num88 << 4;
							HitboxA.Y = num89 << 4;
							if (!Collision.CanHit(ref HitboxA, ref HitboxB))
							{
								continue;
							}
							PlaceTile(num88, num89, 136, ToMute: true);
							if (Main.TileSet[num88, num89].IsActive == 0)
							{
								continue;
							}
							while (num88 != num82 || num89 != num84)
							{
								Main.TileSet[num88, num89].wire = 16;
								if (num88 > num82)
								{
									num88--;
								}
								if (num88 < num82)
								{
									num88++;
								}
								Main.TileSet[num88, num89].wire = 16;
								if (num89 > num84)
								{
									num89--;
								}
								if (num89 < num84)
								{
									num89++;
								}
								Main.TileSet[num88, num89].wire = 16;
							}
							if (Main.Rand.Next(3) > 0)
							{
								Main.TileSet[num82, num84].FrameX = 18;
								Main.TileSet[num82, num84 + 1].FrameX = 18;
							}
							break;
						}
						break;
					}
				}
			}
			if (num14 > num15)
			{
				num16++;
				num14 = 0;
			}
		}
		num14 = 0;
		num15 = 1000;
		num16 = 0;
		while (num16 < Main.MaxTilesX / 500)
		{
			num14++;
			int num90 = genRand.Next(dMinX, dMaxX);
			int num91 = genRand.Next(dMinY, dMaxY);
			if (Main.TileSet[num90, num91].WallType == wallType && placeTrap(num90, num91, 0))
			{
				num14 = num15;
			}
			if (num14 > num15)
			{
				num16++;
				num14 = 0;
			}
		}
	}

	public static void DungeonStairs(int i, int j, int tileType, int wallType)
	{
		Vector2 vector = default;
		Vector2 vector2 = default;
		double num = genRand.Next(5, 9);
		int num2 = 1;
		vector.X = i;
		vector.Y = j;
		int num3 = genRand.Next(10, 30);
		num2 = ((i <= dEnteranceX) ? 1 : (-1));
		if (i > Main.MaxTilesX - 400)
		{
			num2 = -1;
		}
		else if (i < 400)
		{
			num2 = 1;
		}
		vector2.Y = -1f;
		vector2.X = num2;
		if (genRand.Next(3) == 0)
		{
			vector2.X *= 0.5f;
		}
		else if (genRand.Next(3) == 0)
		{
			vector2.Y *= 2f;
		}
		while (num3 > 0)
		{
			num3--;
			int num4 = (int)((double)vector.X - num - 4.0 - (double)genRand.Next(6));
			int num5 = (int)((double)vector.X + num + 4.0 + (double)genRand.Next(6));
			int num6 = (int)((double)vector.Y - num - 4.0);
			int num7 = (int)((double)vector.Y + num + 4.0 + (double)genRand.Next(6));
			if (num4 < 0)
			{
				num4 = 0;
			}
			if (num5 > Main.MaxTilesX)
			{
				num5 = Main.MaxTilesX;
			}
			if (num6 < 0)
			{
				num6 = 0;
			}
			if (num7 > Main.MaxTilesY)
			{
				num7 = Main.MaxTilesY;
			}
			double num8 = 1.0;
			if (vector.X > (float)(Main.MaxTilesX >> 1))
			{
				num8 = -1.0;
			}
			int num9 = (int)((double)vector.X + dxStrength1 * 0.6 * num8 + dxStrength2 * num8);
			double num10 = Math.Floor(dyStrength2 * 0.5);
			int num11 = (int)((double)vector.Y - num + num10);
			if (vector.Y < (float)(Main.WorldSurface - 5) && Main.TileSet[num9, num11 - 6].WallType == 0 && Main.TileSet[num9, num11 - 7].WallType == 0 && Main.TileSet[num9, num11 - 8].WallType == 0)
			{
				dSurface = true;
				TileRunner(num9, (int)((double)vector.Y - num - 6.0 + num10), genRand.Next(25, 35), genRand.Next(10, 20), -1, addTile: false, new Vector2(0f, -1f));
			}
			for (int k = num4; k < num5; k++)
			{
				for (int l = num6; l < num7; l++)
				{
					Main.TileSet[k, l].Liquid = 0;
					if (Main.TileSet[k, l].WallType != wallType)
					{
						Main.TileSet[k, l].WallType = 0;
						Main.TileSet[k, l].IsActive = 1;
						Main.TileSet[k, l].Type = (byte)tileType;
					}
				}
			}
			for (int m = num4 + 1; m < num5 - 1; m++)
			{
				for (int n = num6 + 1; n < num7 - 1; n++)
				{
					if (Main.TileSet[m, n].WallType == 0)
					{
						Main.TileSet[m, n].WallType = (byte)wallType;
					}
				}
			}
			int num12 = 0;
			if (genRand.Next((int)num) == 0)
			{
				num12 = genRand.Next(1, 3);
			}
			num4 = (int)((double)vector.X - num * 0.5 - (double)num12);
			num5 = (int)((double)vector.X + num * 0.5 + (double)num12);
			num6 = (int)((double)vector.Y - num * 0.5 - (double)num12);
			num7 = (int)((double)vector.Y + num * 0.5 + (double)num12);
			if (num4 < 0)
			{
				num4 = 0;
			}
			if (num5 > Main.MaxTilesX)
			{
				num5 = Main.MaxTilesX;
			}
			if (num6 < 0)
			{
				num6 = 0;
			}
			if (num7 > Main.MaxTilesY)
			{
				num7 = Main.MaxTilesY;
			}
			for (int num13 = num4; num13 < num5; num13++)
			{
				for (int num14 = num6; num14 < num7; num14++)
				{
					Main.TileSet[num13, num14].IsActive = 0;
					if (Main.TileSet[num13, num14].WallType == 0)
					{
						Main.TileSet[num13, num14].WallType = (byte)wallType;
					}
				}
			}
			if (dSurface)
			{
				num3 = 0;
			}
			vector.X += vector2.X;
			vector.Y += vector2.Y;
		}
		dungeonX = (int)vector.X;
		dungeonY = (int)vector.Y;
	}

	public static void DungeonHalls(int i, int j, int tileType, int wallType, bool forceX = false)
	{
		Vector2 vector = default;
		Vector2 vector2 = default;
		double num = genRand.Next(4, 6);
		Vector2i vector2i = default;
		Vector2i vector2i2 = default;
		int num2 = 1;
		vector.X = i;
		vector.Y = j;
		int num3 = genRand.Next(35, 80);
		if (forceX)
		{
			num3 += 20;
			lastDungeonHall = default;
		}
		else if (genRand.Next(5) == 0)
		{
			num *= 2.0;
			num3 >>= 1;
		}
		do
		{
			num2 = (genRand.Next(2) << 1) - 1;
			if (forceX || genRand.Next(2) == 0)
			{
				vector2i.Y = 0;
				vector2i.X = num2;
				vector2i2.Y = 0;
				vector2i2.X = -num2;
				vector2.Y = 0f;
				vector2.X = num2;
				if (genRand.Next(3) == 0)
				{
					if (genRand.Next(2) == 0)
					{
						vector2.Y = -0.2f;
					}
					else
					{
						vector2.Y = 0.2f;
					}
				}
				continue;
			}
			num++;
			vector2.Y = num2;
			vector2.X = 0f;
			vector2i.X = 0;
			vector2i.Y = num2;
			vector2i2.X = 0;
			vector2i2.Y = -num2;
			if (genRand.Next(2) == 0)
			{
				if (genRand.Next(2) == 0)
				{
					vector2.X = 0.3f;
				}
				else
				{
					vector2.X = -0.3f;
				}
			}
			else
			{
				num3 >>= 1;
			}
		}
		while (lastDungeonHall.X == vector2i2.X && lastDungeonHall.Y == vector2i2.Y);
		if (!forceX)
		{
			if (vector.X > (float)(lastMaxTilesX - 200))
			{
				num2 = -1;
				vector2i.Y = 0;
				vector2i.X = num2;
				vector2.Y = 0f;
				vector2.X = num2;
				if (genRand.Next(3) == 0)
				{
					if (genRand.Next(2) == 0)
					{
						vector2.Y = -0.2f;
					}
					else
					{
						vector2.Y = 0.2f;
					}
				}
			}
			else if (vector.X < 200f)
			{
				num2 = 1;
				vector2i.Y = 0;
				vector2i.X = num2;
				vector2.Y = 0f;
				vector2.X = num2;
				if (genRand.Next(3) == 0)
				{
					if (genRand.Next(2) == 0)
					{
						vector2.Y = -0.2f;
					}
					else
					{
						vector2.Y = 0.2f;
					}
				}
			}
			else if (vector.Y > (float)(lastMaxTilesY - 300))
			{
				num2 = -1;
				num++;
				vector2.Y = num2;
				vector2.X = 0f;
				vector2i.X = 0;
				vector2i.Y = num2;
				if (genRand.Next(2) == 0)
				{
					if (genRand.Next(2) == 0)
					{
						vector2.X = 0.3f;
					}
					else
					{
						vector2.X = -0.3f;
					}
				}
			}
			else if (vector.Y < (float)Main.RockLayer)
			{
				num2 = 1;
				num++;
				vector2.Y = num2;
				vector2.X = 0f;
				vector2i.X = 0;
				vector2i.Y = num2;
				if (genRand.Next(2) == 0)
				{
					if (genRand.Next(2) == 0)
					{
						vector2.X = 0.3f;
					}
					else
					{
						vector2.X = -0.3f;
					}
				}
			}
			else if (vector.X < (float)(Main.MaxTilesX >> 1) && vector.X > (float)(Main.MaxTilesX >> 2))
			{
				num2 = -1;
				vector2i.Y = 0;
				vector2i.X = num2;
				vector2.Y = 0f;
				vector2.X = num2;
				if (genRand.Next(3) == 0)
				{
					if (genRand.Next(2) == 0)
					{
						vector2.Y = -0.2f;
					}
					else
					{
						vector2.Y = 0.2f;
					}
				}
			}
			else if (vector.X > (float)(Main.MaxTilesX >> 1) && (double)vector.X < (double)Main.MaxTilesX * 0.75)
			{
				num2 = 1;
				vector2i.Y = 0;
				vector2i.X = num2;
				vector2.Y = 0f;
				vector2.X = num2;
				if (genRand.Next(3) == 0)
				{
					if (genRand.Next(2) == 0)
					{
						vector2.Y = -0.2f;
					}
					else
					{
						vector2.Y = 0.2f;
					}
				}
			}
		}
		if (vector2i.Y == 0)
		{
			dDoor[numDDoors].X = (short)vector.X;
			dDoor[numDDoors].Y = (short)vector.Y;
			dDoor[numDDoors].Pos = 0;
			numDDoors++;
		}
		else
		{
			DPlat[numDPlats].X = (short)vector.X;
			DPlat[numDPlats].Y = (short)vector.Y;
			numDPlats++;
		}
		lastDungeonHall = vector2i;
		while (num3 > 0)
		{
			if (vector2i.X > 0 && vector.X > (float)(Main.MaxTilesX - 100))
			{
				num3 = 0;
			}
			else if (vector2i.X < 0 && vector.X < 100f)
			{
				num3 = 0;
			}
			else if (vector2i.Y > 0 && vector.Y > (float)(Main.MaxTilesY - 100))
			{
				num3 = 0;
			}
			else if (vector2i.Y < 0 && vector.Y < (float)(Main.RockLayer + 50))
			{
				num3 = 0;
			}
			num3--;
			int num4 = (int)((double)vector.X - num - 4.0 - (double)genRand.Next(6));
			int num5 = (int)((double)vector.X + num + 4.0 + (double)genRand.Next(6));
			int num6 = (int)((double)vector.Y - num - 4.0 - (double)genRand.Next(6));
			int num7 = (int)((double)vector.Y + num + 4.0 + (double)genRand.Next(6));
			if (num4 < 0)
			{
				num4 = 0;
			}
			if (num5 > Main.MaxTilesX)
			{
				num5 = Main.MaxTilesX;
			}
			if (num6 < 0)
			{
				num6 = 0;
			}
			if (num7 > Main.MaxTilesY)
			{
				num7 = Main.MaxTilesY;
			}
			for (int k = num4; k < num5; k++)
			{
				for (int l = num6; l < num7; l++)
				{
					Main.TileSet[k, l].Liquid = 0;
					if (Main.TileSet[k, l].WallType == 0)
					{
						Main.TileSet[k, l].IsActive = 1;
						Main.TileSet[k, l].Type = (byte)tileType;
					}
				}
			}
			for (int m = num4 + 1; m < num5 - 1; m++)
			{
				for (int n = num6 + 1; n < num7 - 1; n++)
				{
					if (Main.TileSet[m, n].WallType == 0)
					{
						Main.TileSet[m, n].WallType = (byte)wallType;
					}
				}
			}
			int num8 = 0;
			if (vector2.Y == 0f && genRand.Next((int)num + 1) == 0)
			{
				num8 = genRand.Next(1, 3);
			}
			else if (vector2.X == 0f && genRand.Next((int)num - 1) == 0)
			{
				num8 = genRand.Next(1, 3);
			}
			else if (genRand.Next((int)num * 3) == 0)
			{
				num8 = genRand.Next(1, 3);
			}
			num4 = (int)((double)vector.X - num * 0.5 - (double)num8);
			num5 = (int)((double)vector.X + num * 0.5 + (double)num8);
			num6 = (int)((double)vector.Y - num * 0.5 - (double)num8);
			num7 = (int)((double)vector.Y + num * 0.5 + (double)num8);
			if (num4 < 0)
			{
				num4 = 0;
			}
			if (num5 > Main.MaxTilesX)
			{
				num5 = Main.MaxTilesX;
			}
			if (num6 < 0)
			{
				num6 = 0;
			}
			if (num7 > Main.MaxTilesY)
			{
				num7 = Main.MaxTilesY;
			}
			for (int num9 = num4; num9 < num5; num9++)
			{
				for (int num10 = num6; num10 < num7; num10++)
				{
					Main.TileSet[num9, num10].IsActive = 0;
					Main.TileSet[num9, num10].WallType = (byte)wallType;
				}
			}
			vector.X += vector2.X;
			vector.Y += vector2.Y;
		}
		dungeonX = (int)vector.X;
		dungeonY = (int)vector.Y;
		if (vector2i.Y == 0)
		{
			dDoor[numDDoors].X = (short)vector.X;
			dDoor[numDDoors].Y = (short)vector.Y;
			dDoor[numDDoors].Pos = 0;
			numDDoors++;
		}
		else
		{
			DPlat[numDPlats].X = (short)vector.X;
			DPlat[numDPlats].Y = (short)vector.Y;
			numDPlats++;
		}
	}

	public static void DungeonRoom(int i, int j, int tileType, int wallType)
	{
		Vector2 vector = default;
		Vector2 vector2 = default;
		float num = genRand.Next(15, 30);
		vector2.X = (float)genRand.Next(-10, 11) * 0.1f;
		vector2.Y = (float)genRand.Next(-10, 11) * 0.1f;
		vector.X = i;
		vector.Y = (float)j - num * 0.5f;
		int num2 = genRand.Next(10, 20);
		float num3 = vector.X;
		float num4 = vector.X;
		float num5 = vector.Y;
		float num6 = vector.Y;
		while (num2 > 0)
		{
			num2--;
			int num7 = (int)(vector.X - num * 0.8f - 5f);
			int num8 = (int)(vector.X + num * 0.8f + 5f);
			int num9 = (int)(vector.Y - num * 0.8f - 5f);
			int num10 = (int)(vector.Y + num * 0.8f + 5f);
			if (num7 < 0)
			{
				num7 = 0;
			}
			if (num8 > Main.MaxTilesX)
			{
				num8 = Main.MaxTilesX;
			}
			if (num9 < 0)
			{
				num9 = 0;
			}
			if (num10 > Main.MaxTilesY)
			{
				num10 = Main.MaxTilesY;
			}
			for (int k = num7; k < num8; k++)
			{
				for (int l = num9; l < num10; l++)
				{
					Main.TileSet[k, l].Liquid = 0;
					if (Main.TileSet[k, l].WallType == 0)
					{
						Main.TileSet[k, l].IsActive = 1;
						Main.TileSet[k, l].Type = (byte)tileType;
					}
				}
			}
			for (int m = num7 + 1; m < num8 - 1; m++)
			{
				for (int n = num9 + 1; n < num10 - 1; n++)
				{
					if (Main.TileSet[m, n].WallType == 0)
					{
						Main.TileSet[m, n].WallType = (byte)wallType;
					}
				}
			}
			num7 = (int)((double)vector.X - (double)num * 0.5);
			num8 = (int)((double)vector.X + (double)num * 0.5);
			num9 = (int)((double)vector.Y - (double)num * 0.5);
			num10 = (int)((double)vector.Y + (double)num * 0.5);
			if (num7 < 0)
			{
				num7 = 0;
			}
			if (num8 > Main.MaxTilesX)
			{
				num8 = Main.MaxTilesX;
			}
			if (num9 < 0)
			{
				num9 = 0;
			}
			if (num10 > Main.MaxTilesY)
			{
				num10 = Main.MaxTilesY;
			}
			if ((float)num7 < num3)
			{
				num3 = num7;
			}
			if ((float)num8 > num4)
			{
				num4 = num8;
			}
			if ((float)num9 < num5)
			{
				num5 = num9;
			}
			if ((float)num10 > num6)
			{
				num6 = num10;
			}
			for (int num11 = num7; num11 < num8; num11++)
			{
				for (int num12 = num9; num12 < num10; num12++)
				{
					Main.TileSet[num11, num12].IsActive = 0;
					Main.TileSet[num11, num12].WallType = (byte)wallType;
				}
			}
			vector.X += vector2.X;
			vector.Y += vector2.Y;
			vector2.X += (float)genRand.Next(-10, 11) * 0.05f;
			vector2.Y += (float)genRand.Next(-10, 11) * 0.05f;
			if (vector2.X > 1f)
			{
				vector2.X = 1f;
			}
			if (vector2.X < -1f)
			{
				vector2.X = -1f;
			}
			if (vector2.Y > 1f)
			{
				vector2.Y = 1f;
			}
			if (vector2.Y < -1f)
			{
				vector2.Y = -1f;
			}
		}
		dRoom[numDRooms].X = (short)vector.X;
		dRoom[numDRooms].Y = (short)vector.Y;
		dRoom[numDRooms].Size = num;
		dRoom[numDRooms].L = (short)num3;
		dRoom[numDRooms].R = (short)num4;
		dRoom[numDRooms].T = (short)num5;
		dRoom[numDRooms].B = (short)num6;
		numDRooms++;
	}

	public static void DungeonEnt(int i, int j, int tileType, int wallType)
	{
		int num = 60;
		for (int k = i - num; k < i + num; k++)
		{
			for (int l = j - num; l < j + num; l++)
			{
				Main.TileSet[k, l].Liquid = 0;
				Main.TileSet[k, l].Lava = 0;
			}
		}
		double num2 = dxStrength1;
		double num3 = dyStrength1;
		Vector2 vector = new Vector2
		{
			X = i,
			Y = (float)((double)j - num3 * 0.5)
		};
		dMinY = (int)vector.Y;
		int num4 = 1;
		if (i > Main.MaxTilesX >> 1)
		{
			num4 = -1;
		}
		int num5 = (int)((double)vector.X - num2 * 0.6000000238418579 - (double)genRand.Next(2, 5));
		int num6 = (int)((double)vector.X + num2 * 0.6000000238418579 + (double)genRand.Next(2, 5));
		int num7 = (int)((double)vector.Y - num3 * 0.6000000238418579 - (double)genRand.Next(2, 5));
		int num8 = (int)((double)vector.Y + num3 * 0.6000000238418579 + (double)genRand.Next(8, 16));
		if (num5 < 0)
		{
			num5 = 0;
		}
		if (num6 > Main.MaxTilesX)
		{
			num6 = Main.MaxTilesX;
		}
		if (num7 < 0)
		{
			num7 = 0;
		}
		if (num8 > Main.MaxTilesY)
		{
			num8 = Main.MaxTilesY;
		}
		for (int m = num5; m < num6; m++)
		{
			for (int n = num7; n < num8; n++)
			{
				Main.TileSet[m, n].Liquid = 0;
				if (Main.TileSet[m, n].WallType != wallType)
				{
					Main.TileSet[m, n].WallType = 0;
					if (m > num5 + 1 && m < num6 - 2 && n > num7 + 1 && n < num8 - 2)
					{
						Main.TileSet[m, n].WallType = (byte)wallType;
					}
					Main.TileSet[m, n].IsActive = 1;
					Main.TileSet[m, n].Type = (byte)tileType;
				}
			}
		}
		int num9 = num5;
		int num10 = num5 + 5 + genRand.Next(4);
		int num11 = num7 - 3 - genRand.Next(3);
		int num12 = num7;
		for (int num13 = num9; num13 < num10; num13++)
		{
			for (int num14 = num11; num14 < num12; num14++)
			{
				if (Main.TileSet[num13, num14].WallType != wallType)
				{
					Main.TileSet[num13, num14].IsActive = 1;
					Main.TileSet[num13, num14].Type = (byte)tileType;
				}
			}
		}
		num9 = num6 - 5 - genRand.Next(4);
		num10 = num6;
		num11 = num7 - 3 - genRand.Next(3);
		num12 = num7;
		for (int num15 = num9; num15 < num10; num15++)
		{
			for (int num16 = num11; num16 < num12; num16++)
			{
				if (Main.TileSet[num15, num16].WallType != wallType)
				{
					Main.TileSet[num15, num16].IsActive = 1;
					Main.TileSet[num15, num16].Type = (byte)tileType;
				}
			}
		}
		int num17 = 1 + genRand.Next(2);
		int num18 = 2 + genRand.Next(4);
		int num19 = 0;
		for (int num20 = num5; num20 < num6; num20++)
		{
			for (int num21 = num7 - num17; num21 < num7; num21++)
			{
				if (Main.TileSet[num20, num21].WallType != wallType)
				{
					Main.TileSet[num20, num21].IsActive = 1;
					Main.TileSet[num20, num21].Type = (byte)tileType;
				}
			}
			num19++;
			if (num19 >= num18)
			{
				num20 += num18;
				num19 = 0;
			}
		}
		for (int num22 = num5; num22 < num6; num22++)
		{
			for (int num23 = num8; num23 < num8 + 100; num23++)
			{
				if (Main.TileSet[num22, num23].WallType == 0)
				{
					Main.TileSet[num22, num23].WallType = 2;
				}
			}
		}
		num5 = (int)((double)vector.X - num2 * 0.6000000238418579);
		num6 = (int)((double)vector.X + num2 * 0.6000000238418579);
		num7 = (int)((double)vector.Y - num3 * 0.6000000238418579);
		num8 = (int)((double)vector.Y + num3 * 0.6000000238418579);
		if (num5 < 0)
		{
			num5 = 0;
		}
		if (num6 > Main.MaxTilesX)
		{
			num6 = Main.MaxTilesX;
		}
		if (num7 < 0)
		{
			num7 = 0;
		}
		if (num8 > Main.MaxTilesY)
		{
			num8 = Main.MaxTilesY;
		}
		for (int num24 = num5; num24 < num6; num24++)
		{
			for (int num25 = num7; num25 < num8; num25++)
			{
				if (Main.TileSet[num24, num25].WallType == 0)
				{
					Main.TileSet[num24, num25].WallType = (byte)wallType;
				}
			}
		}
		num5 = (int)((double)vector.X - num2 * 0.6000000238418579 - 1.0);
		num6 = (int)((double)vector.X + num2 * 0.6000000238418579 + 1.0);
		num7 = (int)((double)vector.Y - num3 * 0.6000000238418579 - 1.0);
		num8 = (int)((double)vector.Y + num3 * 0.6000000238418579 + 1.0);
		if (num5 < 0)
		{
			num5 = 0;
		}
		if (num6 > Main.MaxTilesX)
		{
			num6 = Main.MaxTilesX;
		}
		if (num7 < 0)
		{
			num7 = 0;
		}
		if (num8 > Main.MaxTilesY)
		{
			num8 = Main.MaxTilesY;
		}
		for (int num26 = num5; num26 < num6; num26++)
		{
			for (int num27 = num7; num27 < num8; num27++)
			{
				Main.TileSet[num26, num27].WallType = (byte)wallType;
			}
		}
		num5 = (int)((double)vector.X - num2 * 0.5);
		num6 = (int)((double)vector.X + num2 * 0.5);
		num7 = (int)((double)vector.Y - num3 * 0.5);
		num8 = (int)((double)vector.Y + num3 * 0.5);
		if (num5 < 0)
		{
			num5 = 0;
		}
		if (num6 > Main.MaxTilesX)
		{
			num6 = Main.MaxTilesX;
		}
		if (num7 < 0)
		{
			num7 = 0;
		}
		if (num8 > Main.MaxTilesY)
		{
			num8 = Main.MaxTilesY;
		}
		for (int num28 = num5; num28 < num6; num28++)
		{
			for (int num29 = num7; num29 < num8; num29++)
			{
				Main.TileSet[num28, num29].IsActive = 0;
				Main.TileSet[num28, num29].WallType = (byte)wallType;
			}
		}
		DPlat[numDPlats].X = (short)vector.X;
		DPlat[numDPlats].Y = (short)num8;
		numDPlats++;
		vector.X += (float)num2 * 0.6f * (float)num4;
		vector.Y += (float)num3 * 0.5f;
		num2 = dxStrength2;
		num3 = dyStrength2;
		vector.X += (float)num2 * 0.55f * (float)num4;
		vector.Y -= (float)num3 * 0.5f;
		num5 = (int)((double)vector.X - num2 * 0.6000000238418579 - (double)genRand.Next(1, 3));
		num6 = (int)((double)vector.X + num2 * 0.6000000238418579 + (double)genRand.Next(1, 3));
		num7 = (int)((double)vector.Y - num3 * 0.6000000238418579 - (double)genRand.Next(1, 3));
		num8 = (int)((double)vector.Y + num3 * 0.6000000238418579 + (double)genRand.Next(6, 16));
		if (num5 < 0)
		{
			num5 = 0;
		}
		if (num6 > Main.MaxTilesX)
		{
			num6 = Main.MaxTilesX;
		}
		if (num7 < 0)
		{
			num7 = 0;
		}
		if (num8 > Main.MaxTilesY)
		{
			num8 = Main.MaxTilesY;
		}
		for (int num30 = num5; num30 < num6; num30++)
		{
			for (int num31 = num7; num31 < num8; num31++)
			{
				if (Main.TileSet[num30, num31].WallType == wallType)
				{
					continue;
				}
				bool flag = true;
				if (num4 < 0)
				{
					if ((double)num30 < (double)vector.X - num2 * 0.5)
					{
						flag = false;
					}
				}
				else if ((double)num30 > (double)vector.X + num2 * 0.5 - 1.0)
				{
					flag = false;
				}
				if (flag)
				{
					Main.TileSet[num30, num31].WallType = 0;
					Main.TileSet[num30, num31].IsActive = 1;
					Main.TileSet[num30, num31].Type = (byte)tileType;
				}
			}
		}
		for (int num32 = num5; num32 < num6; num32++)
		{
			for (int num33 = num8; num33 < num8 + 100; num33++)
			{
				if (Main.TileSet[num32, num33].WallType == 0)
				{
					Main.TileSet[num32, num33].WallType = 2;
				}
			}
		}
		num5 = (int)((double)vector.X - num2 * 0.5);
		num6 = (int)((double)vector.X + num2 * 0.5);
		num9 = num5;
		if (num4 < 0)
		{
			num9++;
		}
		num10 = num9 + 5 + genRand.Next(4);
		num11 = num7 - 3 - genRand.Next(3);
		num12 = num7;
		for (int num34 = num9; num34 < num10; num34++)
		{
			for (int num35 = num11; num35 < num12; num35++)
			{
				if (Main.TileSet[num34, num35].WallType != wallType)
				{
					Main.TileSet[num34, num35].IsActive = 1;
					Main.TileSet[num34, num35].Type = (byte)tileType;
				}
			}
		}
		num9 = num6 - 5 - genRand.Next(4);
		num10 = num6;
		num11 = num7 - 3 - genRand.Next(3);
		num12 = num7;
		for (int num36 = num9; num36 < num10; num36++)
		{
			for (int num37 = num11; num37 < num12; num37++)
			{
				if (Main.TileSet[num36, num37].WallType != wallType)
				{
					Main.TileSet[num36, num37].IsActive = 1;
					Main.TileSet[num36, num37].Type = (byte)tileType;
				}
			}
		}
		num17 = 1 + genRand.Next(2);
		num18 = 2 + genRand.Next(4);
		num19 = 0;
		if (num4 < 0)
		{
			num6++;
		}
		for (int num38 = num5 + 1; num38 < num6 - 1; num38++)
		{
			for (int num39 = num7 - num17; num39 < num7; num39++)
			{
				if (Main.TileSet[num38, num39].WallType != wallType)
				{
					Main.TileSet[num38, num39].IsActive = 1;
					Main.TileSet[num38, num39].Type = (byte)tileType;
				}
			}
			num19++;
			if (num19 >= num18)
			{
				num38 += num18;
				num19 = 0;
			}
		}
		num5 = (int)((double)vector.X - num2 * 0.6000000238418579);
		num6 = (int)((double)vector.X + num2 * 0.6000000238418579);
		num7 = (int)((double)vector.Y - num3 * 0.6000000238418579);
		num8 = (int)((double)vector.Y + num3 * 0.6000000238418579);
		if (num5 < 0)
		{
			num5 = 0;
		}
		if (num6 > Main.MaxTilesX)
		{
			num6 = Main.MaxTilesX;
		}
		if (num7 < 0)
		{
			num7 = 0;
		}
		if (num8 > Main.MaxTilesY)
		{
			num8 = Main.MaxTilesY;
		}
		for (int num40 = num5; num40 < num6; num40++)
		{
			for (int num41 = num7; num41 < num8; num41++)
			{
				Main.TileSet[num40, num41].WallType = 0;
			}
		}
		num5 = (int)((double)vector.X - num2 * 0.5);
		num6 = (int)((double)vector.X + num2 * 0.5);
		num7 = (int)((double)vector.Y - num3 * 0.5);
		num8 = (int)((double)vector.Y + num3 * 0.5);
		if (num5 < 0)
		{
			num5 = 0;
		}
		if (num6 > Main.MaxTilesX)
		{
			num6 = Main.MaxTilesX;
		}
		if (num7 < 0)
		{
			num7 = 0;
		}
		if (num8 > Main.MaxTilesY)
		{
			num8 = Main.MaxTilesY;
		}
		for (int num42 = num5; num42 < num6; num42++)
		{
			for (int num43 = num7; num43 < num8; num43++)
			{
				Main.TileSet[num42, num43].IsActive = 0;
				Main.TileSet[num42, num43].WallType = 0;
			}
		}
		for (int num44 = num5; num44 < num6; num44++)
		{
			if (Main.TileSet[num44, num8].IsActive == 0)
			{
				Main.TileSet[num44, num8].IsActive = 1;
				Main.TileSet[num44, num8].Type = 19;
			}
		}
		Main.DungeonX = (short)vector.X;
		Main.DungeonY = (short)num8;
		int num45 = NPC.NewNPC(Main.DungeonX * 16 + 8, Main.DungeonY * 16, 37);
		Main.NPCSet[num45].IsHomeless = false;
		Main.NPCSet[num45].HomeTileX = Main.DungeonX;
		Main.NPCSet[num45].HomeTileY = Main.DungeonY;
		if (num4 == 1)
		{
			int num46 = 0;
			for (int num47 = num6; num47 < num6 + 25; num47++)
			{
				num46++;
				for (int num48 = num8 + num46; num48 < num8 + 25; num48++)
				{
					Main.TileSet[num47, num48].IsActive = 1;
					Main.TileSet[num47, num48].Type = (byte)tileType;
				}
			}
		}
		else
		{
			int num49 = 0;
			for (int num50 = num5; num50 > num5 - 25; num50--)
			{
				num49++;
				for (int num51 = num8 + num49; num51 < num8 + 25; num51++)
				{
					Main.TileSet[num50, num51].IsActive = 1;
					Main.TileSet[num50, num51].Type = (byte)tileType;
				}
			}
		}
		num17 = 1 + genRand.Next(2);
		num18 = 2 + genRand.Next(4);
		num19 = 0;
		num5 = (int)((double)vector.X - num2 * 0.5);
		num6 = (int)((double)vector.X + num2 * 0.5);
		num5 += 2;
		num6 -= 2;
		for (int num52 = num5; num52 < num6; num52++)
		{
			for (int num53 = num7; num53 < num8; num53++)
			{
				if (Main.TileSet[num52, num53].WallType == 0)
				{
					Main.TileSet[num52, num53].WallType = (byte)wallType;
				}
			}
			if (++num19 >= num18)
			{
				num52 += num18 * 2;
				num19 = 0;
			}
		}
		vector.X -= (float)num2 * 0.6f * (float)num4;
		vector.Y += (float)num3 * 0.5f;
		num2 = 15.0;
		num3 = 3.0;
		vector.Y -= (float)num3 * 0.5f;
		num5 = (int)((double)vector.X - num2 * 0.5);
		num6 = (int)((double)vector.X + num2 * 0.5);
		num7 = (int)((double)vector.Y - num3 * 0.5);
		num8 = (int)((double)vector.Y + num3 * 0.5);
		if (num5 < 0)
		{
			num5 = 0;
		}
		if (num6 > Main.MaxTilesX)
		{
			num6 = Main.MaxTilesX;
		}
		if (num7 < 0)
		{
			num7 = 0;
		}
		if (num8 > Main.MaxTilesY)
		{
			num8 = Main.MaxTilesY;
		}
		for (int num54 = num5; num54 < num6; num54++)
		{
			for (int num55 = num7; num55 < num8; num55++)
			{
				Main.TileSet[num54, num55].IsActive = 0;
			}
		}
		if (num4 < 0)
		{
			vector.X--;
		}
		PlaceTile((int)vector.X, (int)vector.Y + 1, 10);
	}

	public static bool AddBuriedChest(int i, int j, int contain = 0, bool notNearOtherChests = false, int Style = -1)
	{
		for (int k = j; k < Main.MaxTilesY; k++)
		{
			if (Main.TileSet[i, k].IsActive == 0 || !Main.TileSolid[Main.TileSet[i, k].Type])
			{
				continue;
			}
			bool flag = false;
			int num = k;
			int num2 = -1;
			int style = 0;
			if (num >= Main.WorldSurface + 25 || contain > 0)
			{
				style = 1;
			}
			if (Style >= 0)
			{
				style = Style;
			}
			if (num > Main.MaxTilesY - 205 && contain == 0)
			{
				if (hellChest == 0)
				{
					contain = 274;
					style = 4;
					flag = true;
				}
				else if (hellChest == 1)
				{
					contain = 220;
					style = 4;
					flag = true;
				}
				else if (hellChest == 2)
				{
					contain = 112;
					style = 4;
					flag = true;
				}
				else if (hellChest == 3)
				{
					contain = 218;
					style = 4;
					flag = true;
					hellChest = 0;
				}
			}
			num2 = PlaceChest(i - 1, num - 1, notNearOtherChests, style);
			if (num2 >= 0)
			{
				if (flag)
				{
					hellChest++;
				}
				int num3 = 0;
				do
				{
					if (num < Main.WorldSurface + 25)
					{
						if (contain > 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(contain);
							Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
						}
						else
						{
							switch (genRand.Next(7))
							{
							case 0:
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(280);
								Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
								break;
							case 1:
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(281);
								Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
								break;
							case 2:
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(284);
								Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
								break;
							case 3:
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(282, genRand.Next(50, 75));
								break;
							case 4:
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(279, genRand.Next(25, 50));
								break;
							default:
								if (genRand.Next(32) == 0)
								{
									Main.ChestSet[num2].ItemSet[num3].SetDefaults(603);
									break;
								}
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(285);
								Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
								break;
							}
						}
						num3++;
						if (genRand.Next(3) == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(168, genRand.Next(3, 6));
							num3++;
						}
						if (genRand.Next(2) == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults((genRand.Next(2) == 0) ? 20 : 22, genRand.Next(3, 11));
							num3++;
						}
						if (genRand.Next(2) == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults((genRand.Next(2) == 0) ? 40 : 42, genRand.Next(25, 51));
							num3++;
						}
						if (genRand.Next(2) == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(28, genRand.Next(3, 6));
							num3++;
						}
						if (genRand.Next(3) > 0)
						{
							int num4 = genRand.Next(4);
							genRand.Next(1, 3);
							ref Item reference = ref Main.ChestSet[num2].ItemSet[num3];
							if (1 == 0)
							{
							}
							int itemType = num4 switch
							{
								0 => 292, 
								1 => 298, 
								2 => 299, 
								_ => 290, 
							};
							if (1 == 0)
							{
							}
							reference.SetDefaults(itemType, genRand.Next(1, 3));
							num3++;
						}
						if (genRand.Next(2) == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults((genRand.Next(2) == 0) ? 8 : 31, genRand.Next(10, 21));
							num3++;
						}
						if (genRand.Next(2) == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(72, genRand.Next(10, 30));
							num3++;
						}
						continue;
					}
					if (num < Main.RockLayer)
					{
						if (contain > 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(contain);
							Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
							num3++;
						}
						else
						{
							int num5 = genRand.Next(7);
							if (num5 == 0)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(49);
								Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
							}
							if (num5 == 1)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(50);
								Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
							}
							if (num5 == 2)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(52);
							}
							if (num5 == 3)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(53);
								Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
							}
							if (num5 == 4)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(54);
								Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
							}
							if (num5 == 5)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(55);
								Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
							}
							if (num5 == 6)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(51, genRand.Next(26) + 25);
							}
							num3++;
							if (genRand.Next(40) == 0)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(603);
								num3++;
							}
						}
						if (genRand.Next(3) == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(166, genRand.Next(10, 20));
							num3++;
						}
						if (genRand.Next(2) == 0)
						{
							int itemType2 = genRand.Next(21, 22);
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(itemType2, genRand.Next(5, 15));
							num3++;
						}
						if (genRand.Next(2) == 0)
						{
							int num6 = genRand.Next(2);
							int itemStack = genRand.Next(25, 50);
							if (num6 == 0)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(40, itemStack);
							}
							else
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(42, itemStack);
							}
							num3++;
						}
						if (genRand.Next(2) == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(28, genRand.Next(3, 6));
							num3++;
						}
						if (genRand.Next(3) > 0)
						{
							int num7 = genRand.Next(7);
							if (1 == 0)
							{
							}
							int itemType = num7 switch
							{
								0 => 289, 
								1 => 298, 
								2 => 299, 
								3 => 290, 
								4 => 303, 
								5 => 291, 
								_ => 304, 
							};
							if (1 == 0)
							{
							}
							int itemType3 = itemType;
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(itemType3, genRand.Next(1, 3));
							num3++;
						}
						if (genRand.Next(2) == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(8, genRand.Next(10, 21));
							num3++;
						}
						if (genRand.Next(2) == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(72, genRand.Next(50, 90));
							num3++;
						}
						continue;
					}
					if (num < Main.MaxTilesY - 250)
					{
						if (contain > 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(contain);
							Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
							num3++;
						}
						else
						{
							int num8 = genRand.Next(7);
							if (num8 == 2 && genRand.Next(2) == 0)
							{
								num8 = genRand.Next(7);
							}
							switch (num8)
							{
							case 0:
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(49);
								Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
								break;
							case 1:
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(50);
								Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
								break;
							case 2:
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(52);
								Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
								break;
							case 3:
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(53);
								Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
								break;
							case 4:
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(54);
								Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
								break;
							case 5:
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(55);
								Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
								break;
							default:
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(51, genRand.Next(26) + 25);
								break;
							}
							num3++;
						}
						if (genRand.Next(5) == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(43);
							num3++;
						}
						if (genRand.Next(3) == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(167);
							num3++;
						}
						if (genRand.Next(2) == 0)
						{
							int num9 = genRand.Next(2);
							int num10 = genRand.Next(8) + 3;
							if (num9 == 0)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(19);
							}
							if (num9 == 1)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(21);
							}
							Main.ChestSet[num2].ItemSet[num3].Stack = (short)num10;
							num3++;
						}
						if (genRand.Next(2) == 0)
						{
							int num11 = genRand.Next(2);
							int num12 = genRand.Next(26) + 25;
							if (num11 == 0)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(41);
							}
							if (num11 == 1)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(279);
							}
							Main.ChestSet[num2].ItemSet[num3].Stack = (short)num12;
							num3++;
						}
						if (genRand.Next(2) == 0)
						{
							int num13 = genRand.Next(1);
							int itemStack2 = genRand.Next(3) + 3;
							if (num13 == 0)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(188, itemStack2);
							}
							num3++;
						}
						if (genRand.Next(3) > 0)
						{
							int num14 = genRand.Next(6);
							int num15 = genRand.Next(1, 3);
							if (num14 == 0)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(296);
							}
							if (num14 == 1)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(295);
							}
							if (num14 == 2)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(299);
							}
							if (num14 == 3)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(302);
							}
							if (num14 == 4)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(303);
							}
							if (num14 == 5)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(305);
							}
							Main.ChestSet[num2].ItemSet[num3].Stack = (short)num15;
							num3++;
						}
						if (genRand.Next(3) > 1)
						{
							int num16 = genRand.Next(4);
							int num17 = genRand.Next(1, 3);
							if (num16 == 0)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(301);
							}
							if (num16 == 1)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(302);
							}
							if (num16 == 2)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(297);
							}
							if (num16 == 3)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(304);
							}
							Main.ChestSet[num2].ItemSet[num3].Stack = (short)num17;
							num3++;
						}
						if (genRand.Next(2) == 0)
						{
							int num18 = genRand.Next(2);
							int num19 = genRand.Next(15) + 15;
							if (num18 == 0)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(8);
							}
							if (num18 == 1)
							{
								Main.ChestSet[num2].ItemSet[num3].SetDefaults(282);
							}
							Main.ChestSet[num2].ItemSet[num3].Stack = (short)num19;
							num3++;
						}
						if (genRand.Next(2) == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(73, genRand.Next(1, 3));
							num3++;
						}
						if (genRand.Next(32) == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(621);
						}
						else if (genRand.Next(48) == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(623);
						}
						continue;
					}
					if (contain > 0)
					{
						Main.ChestSet[num2].ItemSet[num3].SetDefaults(contain);
						Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
						num3++;
					}
					else
					{
						switch (genRand.Next(4))
						{
						case 0:
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(49);
							Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
							break;
						case 1:
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(50);
							Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
							break;
						case 2:
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(53);
							Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
							break;
						default:
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(54);
							Main.ChestSet[num2].ItemSet[num3].SetPrefix(-1);
							break;
						}
						num3++;
					}
					if (genRand.Next(3) == 0)
					{
						Main.ChestSet[num2].ItemSet[num3].SetDefaults(167);
						num3++;
					}
					if (genRand.Next(2) == 0)
					{
						int num20 = genRand.Next(2);
						int num21 = genRand.Next(15) + 15;
						if (num20 == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(117);
						}
						if (num20 == 1)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(19);
						}
						Main.ChestSet[num2].ItemSet[num3].Stack = (short)num21;
						num3++;
					}
					if (genRand.Next(2) == 0)
					{
						int num22 = genRand.Next(2);
						int num23 = genRand.Next(25) + 50;
						if (num22 == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(265);
						}
						if (num22 == 1)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(278);
						}
						Main.ChestSet[num2].ItemSet[num3].Stack = (short)num23;
						num3++;
					}
					if (genRand.Next(2) == 0)
					{
						int num24 = genRand.Next(2);
						int num25 = genRand.Next(15) + 15;
						if (num24 == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(226);
						}
						if (num24 == 1)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(227);
						}
						Main.ChestSet[num2].ItemSet[num3].Stack = (short)num25;
						num3++;
					}
					if (genRand.Next(4) > 0)
					{
						int num26 = genRand.Next(7);
						int num27 = genRand.Next(1, 3);
						if (num26 == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(296);
						}
						if (num26 == 1)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(295);
						}
						if (num26 == 2)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(293);
						}
						if (num26 == 3)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(288);
						}
						if (num26 == 4)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(294);
						}
						if (num26 == 5)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(297);
						}
						if (num26 == 6)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(304);
						}
						Main.ChestSet[num2].ItemSet[num3].Stack = (short)num27;
						num3++;
					}
					if (genRand.Next(3) > 0)
					{
						int num28 = genRand.Next(5);
						int num29 = genRand.Next(1, 3);
						if (num28 == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(305);
						}
						if (num28 == 1)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(301);
						}
						if (num28 == 2)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(302);
						}
						if (num28 == 3)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(288);
						}
						if (num28 == 4)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(300);
						}
						Main.ChestSet[num2].ItemSet[num3].Stack = (short)num29;
						num3++;
					}
					if (genRand.Next(2) == 0)
					{
						int num30 = genRand.Next(2);
						int num31 = genRand.Next(15) + 15;
						if (num30 == 0)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(8);
						}
						if (num30 == 1)
						{
							Main.ChestSet[num2].ItemSet[num3].SetDefaults(282);
						}
						Main.ChestSet[num2].ItemSet[num3].Stack = (short)num31;
						num3++;
					}
					else if (genRand.Next(48) == 0)
					{
						Main.ChestSet[num2].ItemSet[num3].SetDefaults(625);
						num3++;
					}
					if (genRand.Next(2) == 0)
					{
						Main.ChestSet[num2].ItemSet[num3].SetDefaults(73, genRand.Next(2, 5));
						num3++;
					}
				}
				while (num3 == 0);
				return true;
			}
			return false;
		}
		return false;
	}

	public static int OpenDoor(int i, int j, int direction)
	{
		if (!DoOpenDoor(i, j, direction))
		{
			direction = -direction;
			if (!DoOpenDoor(i, j, direction))
			{
				direction = 0;
			}
		}
		return direction;
	}

	public static bool CanOpenDoor(int i, int j)
	{
		bool flag = DoCanOpenDoor(i, j, 1);
		if (!flag)
		{
			flag = DoCanOpenDoor(i, j, -1);
		}
		return flag;
	}

	private static bool DoCanOpenDoor(int i, int j, int direction)
	{
		if (Main.TileSet[i, j - 1].FrameY == 0 && Main.TileSet[i, j - 1].Type == Main.TileSet[i, j].Type)
		{
			j--;
		}
		else if (Main.TileSet[i, j - 2].FrameY == 0 && Main.TileSet[i, j - 2].Type == Main.TileSet[i, j].Type)
		{
			j -= 2;
		}
		else if (Main.TileSet[i, j + 1].FrameY == 0 && Main.TileSet[i, j + 1].Type == Main.TileSet[i, j].Type)
		{
			j++;
		}
		i += direction;
		for (int k = j; k < j + 3; k++)
		{
			if (Main.TileSet[i, k].IsActive == 0)
			{
				continue;
			}
			int type = Main.TileSet[i, k].Type;
			if (!Main.TileCut[type])
			{
				switch ((EntityID.TileID)type)
				{
				case EntityID.TileID.SHORT_GRASS_PLANTS:
				case EntityID.TileID.SHORT_CORRUPT_PLANTS:
				case EntityID.TileID.VINE:
				case EntityID.TileID.SHORT_JUNGLE_PLANTS:
				case EntityID.TileID.JUNGLE_VINE:
				case EntityID.TileID.JUNGLE_THORN:
				case EntityID.TileID.GLOWING_MUSHROOM:
				case EntityID.TileID.TALL_GRASS_PLANTS:
				case EntityID.TileID.TALL_JUNGLE_PLANTS:
				case EntityID.TileID.SHORT_HALLOWED_PLANTS:
				case EntityID.TileID.TALL_HALLOWED_PLANTS:
				case EntityID.TileID.HALLOWED_VINE:
					continue;
				}
				return false;
			}
		}
		return true;
	}

	private static bool DoOpenDoor(int i, int j, int direction)
	{
		int num = j;
		if (Main.TileSet[i, j - 1].FrameY == 0 && Main.TileSet[i, j - 1].Type == Main.TileSet[i, j].Type)
		{
			num--;
		}
		else if (Main.TileSet[i, j - 2].FrameY == 0 && Main.TileSet[i, j - 2].Type == Main.TileSet[i, j].Type)
		{
			num -= 2;
		}
		else if (Main.TileSet[i, j + 1].FrameY == 0 && Main.TileSet[i, j + 1].Type == Main.TileSet[i, j].Type)
		{
			num++;
		}
		int num2 = i;
		int num3 = i;
		int num4;
		if (direction == -1)
		{
			num2--;
			num3--;
			num4 = 36;
		}
		else
		{
			num3++;
			num4 = 0;
		}
		bool flag = true;
		for (int k = num; k < num + 3; k++)
		{
			if (Main.TileSet[num3, k].IsActive == 0)
			{
				continue;
			}
			int type = Main.TileSet[num3, k].Type;
			if (!Main.TileCut[type])
			{
				switch ((EntityID.TileID)type)
				{
				default:
					flag = false;
					break;
				case EntityID.TileID.SHORT_GRASS_PLANTS:
				case EntityID.TileID.SHORT_CORRUPT_PLANTS:
				case EntityID.TileID.VINE:
				case EntityID.TileID.SHORT_JUNGLE_PLANTS:
				case EntityID.TileID.JUNGLE_VINE:
				case EntityID.TileID.JUNGLE_THORN:
				case EntityID.TileID.GLOWING_MUSHROOM:
				case EntityID.TileID.TALL_GRASS_PLANTS:
				case EntityID.TileID.TALL_JUNGLE_PLANTS:
				case EntityID.TileID.SHORT_HALLOWED_PLANTS:
				case EntityID.TileID.TALL_HALLOWED_PLANTS:
				case EntityID.TileID.HALLOWED_VINE:
					break;
				}
				if (!flag)
				{
					break;
				}
			}
			KillTile(num3, k);
		}
		if (flag)
		{
			if (Main.NetMode != 1)
			{
				for (int l = num2; l <= num2 + 1; l++)
				{
					for (int m = num; m <= num + 2; m++)
					{
						if (numNoWire < 999)
						{
							noWire[numNoWire].X = (short)l;
							noWire[numNoWire].Y = (short)m;
							numNoWire++;
						}
					}
				}
			}
			Main.PlaySound(8, i * 16, j * 16);
			Main.TileSet[num2, num].IsActive = 1;
			Main.TileSet[num2, num].Type = 11;
			Main.TileSet[num2, num].FrameY = 0;
			Main.TileSet[num2, num].FrameX = (short)num4;
			Main.TileSet[num2 + 1, num].IsActive = 1;
			Main.TileSet[num2 + 1, num].Type = 11;
			Main.TileSet[num2 + 1, num].FrameY = 0;
			Main.TileSet[num2 + 1, num].FrameX = (short)(num4 + 18);
			Main.TileSet[num2, num + 1].IsActive = 1;
			Main.TileSet[num2, num + 1].Type = 11;
			Main.TileSet[num2, num + 1].FrameY = 18;
			Main.TileSet[num2, num + 1].FrameX = (short)num4;
			Main.TileSet[num2 + 1, num + 1].IsActive = 1;
			Main.TileSet[num2 + 1, num + 1].Type = 11;
			Main.TileSet[num2 + 1, num + 1].FrameY = 18;
			Main.TileSet[num2 + 1, num + 1].FrameX = (short)(num4 + 18);
			Main.TileSet[num2, num + 2].IsActive = 1;
			Main.TileSet[num2, num + 2].Type = 11;
			Main.TileSet[num2, num + 2].FrameY = 36;
			Main.TileSet[num2, num + 2].FrameX = (short)num4;
			Main.TileSet[num2 + 1, num + 2].IsActive = 1;
			Main.TileSet[num2 + 1, num + 2].Type = 11;
			Main.TileSet[num2 + 1, num + 2].FrameY = 36;
			Main.TileSet[num2 + 1, num + 2].FrameX = (short)(num4 + 18);
			bool flag2 = tileFrameRecursion;
			tileFrameRecursion = false;
			for (int n = num2 - 1; n <= num2 + 2; n++)
			{
				for (int num5 = num - 1; num5 <= num + 2; num5++)
				{
					TileFrame(n, num5);
				}
			}
			tileFrameRecursion = flag2;
		}
		return flag;
	}

	public static void Check1xX(int x, int j, int type)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = j - Main.TileSet[x, j].FrameY / 18;
		int frameX = Main.TileSet[x, j].FrameX;
		int num2 = 3;
		if (type == 92)
		{
			num2 = 6;
		}
		int num3 = 0;
		while (true)
		{
			if (num3 < num2)
			{
				if (Main.TileSet[x, num + num3].IsActive == 0 || Main.TileSet[x, num + num3].Type != type || Main.TileSet[x, num + num3].FrameY != num3 * 18 || Main.TileSet[x, num + num3].FrameX != frameX)
				{
					break;
				}
				num3++;
				continue;
			}
			if (Main.TileSet[x, num + num2].IsActive == 0 || !Main.TileSolid[Main.TileSet[x, num + num2].Type])
			{
				break;
			}
			return;
		}
		ToDestroyObject = true;
		for (int i = 0; i < num2; i++)
		{
			if (Main.TileSet[x, num + i].Type == type)
			{
				KillTile(x, num + i);
			}
		}
		if (!Gen)
		{
			switch ((EntityID.TileID)type)
			{
			case EntityID.TileID.LAMP_POST:
				Item.NewItem(x * 16, j * 16, 32, 32, 341);
				break;
			case EntityID.TileID.TIKI_TORCH:
				Item.NewItem(x * 16, j * 16, 32, 32, 342);
				break;
			}
		}
		ToDestroyObject = false;
	}

	public static void Check2xX(int i, int j, int type)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = i;
		if (Main.TileSet[i, j].FrameX % 36 == 18)
		{
			num--;
		}
		int num2 = j - Main.TileSet[num, j].FrameY / 18;
		int frameX = Main.TileSet[num, j].FrameX;
		int num3 = 3;
		if (type == 104)
		{
			num3 = 5;
		}
		int num4 = 0;
		while (true)
		{
			if (num4 < num3)
			{
				if (Main.TileSet[num, num2 + num4].IsActive == 0 || Main.TileSet[num, num2 + num4].Type != type || Main.TileSet[num, num2 + num4].FrameY != num4 * 18 || Main.TileSet[num, num2 + num4].FrameX != frameX || Main.TileSet[num + 1, num2 + num4].IsActive == 0 || Main.TileSet[num + 1, num2 + num4].Type != type || Main.TileSet[num + 1, num2 + num4].FrameY != num4 * 18 || Main.TileSet[num + 1, num2 + num4].FrameX != frameX + 18)
				{
					break;
				}
				num4++;
				continue;
			}
			if (Main.TileSet[num, num2 + num3].IsActive == 0 || !Main.TileSolid[Main.TileSet[num, num2 + num3].Type] || Main.TileSet[num + 1, num2 + num3].IsActive == 0 || !Main.TileSolid[Main.TileSet[num + 1, num2 + num3].Type])
			{
				break;
			}
			return;
		}
		ToDestroyObject = true;
		for (int k = 0; k < num3; k++)
		{
			if (Main.TileSet[num, num2 + k].Type == type)
			{
				KillTile(num, num2 + k);
			}
			if (Main.TileSet[num + 1, num2 + k].Type == type)
			{
				KillTile(num + 1, num2 + k);
			}
		}
		if (!Gen)
		{
			switch ((EntityID.TileID)type)
			{
			case EntityID.TileID.GRANDFATHERS_CLOCK:
				Item.NewItem(num * 16, j * 16, 32, 32, 359);
				break;
			case EntityID.TileID.STATUE:
			{
				int num5 = frameX / 36;
				int x = num * 16;
				int y = j * 16;
				if (1 == 0)
				{
				}
				int type2 = num5 switch
				{
					0 => 360, 
					1 => 52, 
					_ => 438 + num5 - 2, 
				};
				if (1 == 0)
				{
				}
				Item.NewItem(x, y, 32, 32, type2);
				break;
			}
			}
		}
		ToDestroyObject = false;
	}

	public static bool Place1xX(int x, int y, int type, int style = 0)
	{
		int num = style * 18;
		int num2 = 3;
		if (type == 92)
		{
			num2 = 6;
		}
		for (int i = y - num2 + 1; i < y + 1; i++)
		{
			if (Main.TileSet[x, i].IsActive != 0 || (type == 93 && Main.TileSet[x, i].Liquid > 0))
			{
				return false;
			}
		}
		if (Main.TileSet[x, y + 1].IsActive != 0 && Main.TileSolid[Main.TileSet[x, y + 1].Type])
		{
			for (int j = 0; j < num2; j++)
			{
				Main.TileSet[x, y - num2 + 1 + j].IsActive = 1;
				Main.TileSet[x, y - num2 + 1 + j].FrameY = (short)(j * 18);
				Main.TileSet[x, y - num2 + 1 + j].FrameX = (short)num;
				Main.TileSet[x, y - num2 + 1 + j].Type = (byte)type;
			}
			return true;
		}
		return false;
	}

	public static bool Place2xX(int x, int y, int type, int style = 0)
	{
		int num = style * 36;
		int num2 = 3;
		if (type == 104)
		{
			num2 = 5;
		}
		for (int i = y - num2 + 1; i < y + 1; i++)
		{
			if (Main.TileSet[x, i].IsActive != 0 || Main.TileSet[x + 1, i].IsActive != 0)
			{
				return false;
			}
		}
		if (Main.TileSet[x, y + 1].IsActive != 0 && Main.TileSolid[Main.TileSet[x, y + 1].Type] && Main.TileSet[x + 1, y + 1].IsActive != 0 && Main.TileSolid[Main.TileSet[x + 1, y + 1].Type])
		{
			for (int j = 0; j < num2; j++)
			{
				Main.TileSet[x, y - num2 + 1 + j].IsActive = 1;
				Main.TileSet[x, y - num2 + 1 + j].FrameY = (short)(j * 18);
				Main.TileSet[x, y - num2 + 1 + j].FrameX = (short)num;
				Main.TileSet[x, y - num2 + 1 + j].Type = (byte)type;
				Main.TileSet[x + 1, y - num2 + 1 + j].IsActive = 1;
				Main.TileSet[x + 1, y - num2 + 1 + j].FrameY = (short)(j * 18);
				Main.TileSet[x + 1, y - num2 + 1 + j].FrameX = (short)(num + 18);
				Main.TileSet[x + 1, y - num2 + 1 + j].Type = (byte)type;
			}
			return true;
		}
		return false;
	}

	public static void Check1x2(int x, int j, int type)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = j;
		bool flag = true;
		short frameY = Main.TileSet[x, num].FrameY;
		int num2 = frameY / 40;
		if (frameY % 40 == 18)
		{
			num--;
		}
		if (Main.TileSet[x, num].FrameY == 40 * num2 && Main.TileSet[x, num + 1].FrameY == 40 * num2 + 18 && Main.TileSet[x, num].Type == type && Main.TileSet[x, num + 1].Type == type)
		{
			flag = false;
		}
		if (Main.TileSet[x, num + 2].IsActive == 0 || !Main.TileSolid[Main.TileSet[x, num + 2].Type])
		{
			flag = true;
		}
		if (Main.TileSet[x, num + 2].Type != 2 && Main.TileSet[x, num + 2].Type != 109 && Main.TileSet[x, num + 2].Type != 147 && Main.TileSet[x, num].Type == 20)
		{
			flag = true;
		}
		if (!flag)
		{
			return;
		}
		ToDestroyObject = true;
		if (Main.TileSet[x, num].Type == type)
		{
			KillTile(x, num);
		}
		if (Main.TileSet[x, num + 1].Type == type)
		{
			KillTile(x, num + 1);
		}
		if (!Gen)
		{
			switch ((EntityID.TileID)type)
			{
			case EntityID.TileID.CHAIR:
				if (num2 == 1)
				{
					Item.NewItem(x * 16, num * 16, 32, 32, 358);
				}
				else
				{
					Item.NewItem(x * 16, num * 16, 32, 32, 34);
				}
				break;
			case EntityID.TileID.MYTHRIL_ANVIL:
				Item.NewItem(x * 16, num * 16, 32, 32, 525);
				break;
			}
		}
		ToDestroyObject = false;
	}

	public static void CheckOnTableClaypot(int x, int y)
	{
		if ((Main.TileSet[x, y + 1].IsActive == 0 || !Main.TileTable[Main.TileSet[x, y + 1].Type]) && (Main.TileSet[x, y + 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[x, y + 1].Type]))
		{
			KillTile(x, y);
		}
	}

	public static void CheckOnTable1x1(int x, int y)
	{
		if (Main.TileSet[x, y + 1].IsActive == 0 || !Main.TileTable[Main.TileSet[x, y + 1].Type])
		{
			KillTile(x, y);
		}
	}

	public static void CheckSign(int x, int y, int type)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = x - 2;
		int num2 = x + 3;
		int num3 = y - 2;
		int num4 = y + 3;
		if (num < 0 || num2 > Main.MaxTilesX || num3 < 0 || num4 > Main.MaxTilesY)
		{
			return;
		}
		bool flag = false;
		int num5 = (Main.TileSet[x, y].FrameX / 18) & 1;
		int num6 = Main.TileSet[x, y].FrameY / 18;
		int num7 = x - num5;
		int num8 = y - num6;
		int num9 = Main.TileSet[num7, num8].FrameX / 36;
		num = num7;
		num2 = num7 + 2;
		num3 = num8;
		num4 = num8 + 2;
		num5 = 0;
		for (int i = num; i < num2; i++)
		{
			num6 = 0;
			for (int j = num3; j < num4; j++)
			{
				if (Main.TileSet[i, j].IsActive == 0 || Main.TileSet[i, j].Type != type)
				{
					flag = true;
					break;
				}
				if (Main.TileSet[i, j].FrameX / 18 != num5 + num9 * 2 || Main.TileSet[i, j].FrameY / 18 != num6)
				{
					flag = true;
					break;
				}
				num6++;
			}
			num5++;
		}
		if (!flag)
		{
			if (type == 85)
			{
				if (Main.TileSet[num7, num8 + 2].IsActive != 0 && Main.TileSolid[Main.TileSet[num7, num8 + 2].Type] && Main.TileSet[num7 + 1, num8 + 2].IsActive != 0 && Main.TileSolid[Main.TileSet[num7 + 1, num8 + 2].Type])
				{
					num9 = 0;
				}
				else
				{
					flag = true;
				}
			}
			else if (Main.TileSet[num7, num8 + 2].IsActive != 0 && Main.TileSolid[Main.TileSet[num7, num8 + 2].Type] && Main.TileSet[num7 + 1, num8 + 2].IsActive != 0 && Main.TileSolid[Main.TileSet[num7 + 1, num8 + 2].Type])
			{
				num9 = 0;
			}
			else if (Main.TileSet[num7, num8 - 1].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[num7, num8 - 1].Type] && Main.TileSet[num7 + 1, num8 - 1].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[num7 + 1, num8 - 1].Type])
			{
				num9 = 1;
			}
			else if (Main.TileSet[num7 - 1, num8].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[num7 - 1, num8].Type] && Main.TileSet[num7 - 1, num8 + 1].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[num7 - 1, num8 + 1].Type])
			{
				num9 = 2;
			}
			else if (Main.TileSet[num7 + 2, num8].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[num7 + 2, num8].Type] && Main.TileSet[num7 + 2, num8 + 1].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[num7 + 2, num8 + 1].Type])
			{
				num9 = 3;
			}
			else
			{
				flag = true;
			}
		}
		if (flag)
		{
			ToDestroyObject = true;
			for (int k = num; k < num2; k++)
			{
				for (int l = num3; l < num4; l++)
				{
					if (Main.TileSet[k, l].Type == type)
					{
						KillTile(k, l);
					}
				}
			}
			Sign.KillSign(num7, num8);
			if (!Gen)
			{
				switch ((EntityID.TileID)type)
				{
				case EntityID.TileID.TOMBSTONE:
					Item.NewItem(x * 16, y * 16, 32, 32, 321);
					break;
				case EntityID.TileID.SIGN:
					Item.NewItem(x * 16, y * 16, 32, 32, 171);
					break;
				}
			}
			ToDestroyObject = false;
			return;
		}
		int num10 = 36 * num9;
		for (int m = 0; m < 2; m++)
		{
			for (int n = 0; n < 2; n++)
			{
				Main.TileSet[num7 + m, num8 + n].IsActive = 1;
				Main.TileSet[num7 + m, num8 + n].Type = (byte)type;
				Main.TileSet[num7 + m, num8 + n].FrameX = (short)(num10 + 18 * m);
				Main.TileSet[num7 + m, num8 + n].FrameY = (short)(18 * n);
			}
		}
	}

	public static bool PlaceSign(int x, int y, int type)
	{
		int num = x - 2;
		int num2 = x + 3;
		int num3 = y - 2;
		int num4 = y + 3;
		if (num < 0)
		{
			return false;
		}
		if (num2 > Main.MaxTilesX)
		{
			return false;
		}
		if (num3 < 0)
		{
			return false;
		}
		if (num4 > Main.MaxTilesY)
		{
			return false;
		}
		int num5 = x;
		int num6 = y;
		int num7 = 0;
		switch ((EntityID.TileID)type)
		{
		case EntityID.TileID.SIGN:
			if (Main.TileSet[x, y + 1].IsActive != 0 && Main.TileSolid[Main.TileSet[x, y + 1].Type] && Main.TileSet[x + 1, y + 1].IsActive != 0 && Main.TileSolid[Main.TileSet[x + 1, y + 1].Type])
			{
				num6--;
				num7 = 0;
				break;
			}
			if (Main.TileSet[x, y - 1].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[x, y - 1].Type] && Main.TileSet[x + 1, y - 1].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[x + 1, y - 1].Type])
			{
				num7 = 1;
				break;
			}
			if (Main.TileSet[x - 1, y].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[x - 1, y].Type] && !Main.TileNoAttach[Main.TileSet[x - 1, y].Type] && Main.TileSet[x - 1, y + 1].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[x - 1, y + 1].Type] && !Main.TileNoAttach[Main.TileSet[x - 1, y + 1].Type])
			{
				num7 = 2;
				break;
			}
			if (Main.TileSet[x + 1, y].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[x + 1, y].Type] && !Main.TileNoAttach[Main.TileSet[x + 1, y].Type] && Main.TileSet[x + 1, y + 1].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[x + 1, y + 1].Type] && !Main.TileNoAttach[Main.TileSet[x + 1, y + 1].Type])
			{
				num5--;
				num7 = 3;
				break;
			}
			return false;
		case EntityID.TileID.TOMBSTONE:
			if (Main.TileSet[x, y + 1].IsActive != 0 && Main.TileSolid[Main.TileSet[x, y + 1].Type] && Main.TileSet[x + 1, y + 1].IsActive != 0 && Main.TileSolid[Main.TileSet[x + 1, y + 1].Type])
			{
				num6--;
				num7 = 0;
				break;
			}
			return false;
		}
		if (Main.TileSet[num5, num6].IsActive != 0 || Main.TileSet[num5 + 1, num6].IsActive != 0 || Main.TileSet[num5, num6 + 1].IsActive != 0 || Main.TileSet[num5 + 1, num6 + 1].IsActive != 0)
		{
			return false;
		}
		int num8 = 36 * num7;
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < 2; j++)
			{
				Main.TileSet[num5 + i, num6 + j].IsActive = 1;
				Main.TileSet[num5 + i, num6 + j].Type = (byte)type;
				Main.TileSet[num5 + i, num6 + j].FrameX = (short)(num8 + 18 * i);
				Main.TileSet[num5 + i, num6 + j].FrameY = (short)(18 * j);
			}
		}
		return true;
	}

	public static bool Place1x1(int x, int y, int type, int style = 0)
	{
		if (Main.TileSet[x, y].IsActive == 0 && SolidTileUnsafe(x, y + 1))
		{
			Main.TileSet[x, y].IsActive = 1;
			Main.TileSet[x, y].Type = (byte)type;
			if (type == 144)
			{
				Main.TileSet[x, y].FrameX = (short)(style * 18);
				Main.TileSet[x, y].FrameY = 0;
			}
			else
			{
				Main.TileSet[x, y].FrameY = (short)(style * 18);
			}
			return true;
		}
		return false;
	}

	public static void Check1x1(int x, int y, int type)
	{
		if (Main.TileSet[x, y + 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[x, y + 1].Type])
		{
			KillTile(x, y);
		}
	}

	public static bool PlaceOnTable1x1(int x, int y, int type, int style = 0)
	{
		if (Main.TileSet[x, y].IsActive == 0 && Main.TileSet[x, y + 1].IsActive != 0)
		{
			int type2 = Main.TileSet[x, y + 1].Type;
			if (Main.TileTable[type2] || (type == 78 && Main.TileSolid[type2]))
			{
				Main.TileSet[x, y].IsActive = 1;
				Main.TileSet[x, y].FrameX = (short)(style * 18);
				Main.TileSet[x, y].FrameY = 0;
				Main.TileSet[x, y].Type = (byte)type;
				if (type == 50)
				{
					Main.TileSet[x, y].FrameX = (short)(18 * genRand.Next(5));
				}
				return true;
			}
		}
		return false;
	}

	public static bool PlaceAlch(int x, int y, int style)
	{
		if (Main.TileSet[x, y].IsActive == 0 && Main.TileSet[x, y + 1].IsActive != 0)
		{
			bool flag = false;
			switch (style)
			{
			case 0:
				if (Main.TileSet[x, y + 1].Type != 2 && Main.TileSet[x, y + 1].Type != 78 && Main.TileSet[x, y + 1].Type != 109)
				{
					flag = true;
				}
				if (Main.TileSet[x, y].Liquid > 0)
				{
					flag = true;
				}
				break;
			case 1:
				if (Main.TileSet[x, y + 1].Type != 60 && Main.TileSet[x, y + 1].Type != 78)
				{
					flag = true;
				}
				if (Main.TileSet[x, y].Liquid > 0)
				{
					flag = true;
				}
				break;
			case 2:
				if (Main.TileSet[x, y + 1].Type != 0 && Main.TileSet[x, y + 1].Type != 59 && Main.TileSet[x, y + 1].Type != 78)
				{
					flag = true;
				}
				if (Main.TileSet[x, y].Liquid > 0)
				{
					flag = true;
				}
				break;
			case 3:
				if (Main.TileSet[x, y + 1].Type != 23 && Main.TileSet[x, y + 1].Type != 25 && Main.TileSet[x, y + 1].Type != 78)
				{
					flag = true;
				}
				if (Main.TileSet[x, y].Liquid > 0)
				{
					flag = true;
				}
				break;
			case 4:
				if (Main.TileSet[x, y + 1].Type != 53 && Main.TileSet[x, y + 1].Type != 78 && Main.TileSet[x, y + 1].Type != 116)
				{
					flag = true;
				}
				if (Main.TileSet[x, y].Liquid > 0 && Main.TileSet[x, y].Lava != 0)
				{
					flag = true;
				}
				break;
			case 5:
				if (Main.TileSet[x, y + 1].Type != 57 && Main.TileSet[x, y + 1].Type != 78)
				{
					flag = true;
				}
				if (Main.TileSet[x, y].Liquid > 0 && Main.TileSet[x, y].Lava == 0)
				{
					flag = true;
				}
				break;
			}
			if (!flag)
			{
				Main.TileSet[x, y].IsActive = 1;
				Main.TileSet[x, y].Type = 82;
				Main.TileSet[x, y].FrameX = (short)(18 * style);
				Main.TileSet[x, y].FrameY = 0;
				return true;
			}
		}
		return false;
	}

	public static void GrowAlch(int x, int y)
	{
		if (Main.TileSet[x, y].IsActive == 0)
		{
			return;
		}
		if (Main.TileSet[x, y].Type == 82 && genRand.Next(50) == 0)
		{
			Main.TileSet[x, y].Type = 83;
			SquareTileFrame(x, y);
			if (Main.NetMode == 2)
			{
				NetMessage.SendTile(x, y);
			}
		}
		else if (Main.TileSet[x, y].FrameX == 36)
		{
			if (Main.TileSet[x, y].Type == 83)
			{
				Main.TileSet[x, y].Type = 84;
			}
			else
			{
				Main.TileSet[x, y].Type = 83;
			}
			if (Main.NetMode == 2)
			{
				NetMessage.SendTile(x, y);
			}
		}
	}

	public static void PlantAlch()
	{
		int num = genRand.Next(20, Main.MaxTilesX - 20);
		int i;
		switch (genRand.Next(40))
		{
		case 0:
			i = genRand.Next(Main.RockLayer + Main.MaxTilesY >> 1, Main.MaxTilesY - 20);
			break;
		case 1:
		case 2:
		case 3:
		case 4:
			i = genRand.Next(Main.MaxTilesY - 20);
			break;
		default:
			i = genRand.Next(Main.WorldSurface, Main.MaxTilesY - 20);
			break;
		}
		for (; i < Main.MaxTilesY - 20 && Main.TileSet[num, i].IsActive == 0; i++)
		{
		}
		if (Main.TileSet[num, i].IsActive != 0 && Main.TileSet[num, i - 1].IsActive == 0 && Main.TileSet[num, i - 1].Liquid == 0)
		{
			int num2 = -1;
			switch ((EntityID.TileID)Main.TileSet[num, i].Type)
			{
			case EntityID.TileID.GRASS:
			case EntityID.TileID.HALLOWED_GRASS:
				num2 = 0;
				break;
			case EntityID.TileID.JUNGLE_GRASS:
				num2 = 1;
				break;
			case EntityID.TileID.DIRT:
			case EntityID.TileID.MUD:
				num2 = 2;
				break;
			case EntityID.TileID.CORRUPT_GRASS:
			case EntityID.TileID.EBONSTONE:
				num2 = 3;
				break;
			case EntityID.TileID.SAND:
			case EntityID.TileID.PEARLSAND:
				num2 = 4;
				break;
			case EntityID.TileID.ASH:
				num2 = 5;
				break;
			}
			if (num2 >= 0 && PlaceAlch(num, i - 1, num2) && Main.NetMode == 2)
			{
				NetMessage.SendTile(num, i - 1);
			}
		}
	}

	public static void CheckAlch(int x, int y)
	{
		bool flag = Main.TileSet[x, y + 1].IsActive == 0;
		Main.TileSet[x, y].FrameY = 0;
		if (!flag)
		{
			int num = Main.TileSet[x, y].FrameX / 18;
			int type = Main.TileSet[x, y + 1].Type;
			switch (num)
			{
			case 0:
				if (type != 109 && type != 2 && type != 78)
				{
					flag = true;
				}
				else if (Main.TileSet[x, y].Liquid > 0 && Main.TileSet[x, y].Lava != 0)
				{
					flag = true;
				}
				break;
			case 1:
				if (type != 60 && type != 78)
				{
					flag = true;
				}
				else if (Main.TileSet[x, y].Liquid > 0 && Main.TileSet[x, y].Lava != 0)
				{
					flag = true;
				}
				break;
			case 2:
				if (type != 0 && type != 59 && type != 78)
				{
					flag = true;
				}
				else if (Main.TileSet[x, y].Liquid > 0 && Main.TileSet[x, y].Lava != 0)
				{
					flag = true;
				}
				break;
			case 3:
				if (type != 23 && type != 25 && type != 78)
				{
					flag = true;
				}
				else if (Main.TileSet[x, y].Liquid > 0 && Main.TileSet[x, y].Lava != 0)
				{
					flag = true;
				}
				break;
			default:
			{
				int type2 = Main.TileSet[x, y].Type;
				switch (num)
				{
				case 4:
					if (type != 53 && type != 78 && type != 116)
					{
						flag = true;
					}
					else if (Main.TileSet[x, y].Liquid > 0 && Main.TileSet[x, y].Lava != 0)
					{
						flag = true;
					}
					if (type2 == 82 || Main.TileSet[x, y].Lava != 0 || Main.NetMode == 1)
					{
						break;
					}
					if (Main.TileSet[x, y].Liquid > 16)
					{
						if (type2 == 83)
						{
							Main.TileSet[x, y].Type = 84;
							if (Main.NetMode == 2)
							{
								NetMessage.SendTile(x, y);
							}
						}
					}
					else if (type2 == 84)
					{
						Main.TileSet[x, y].Type = 83;
						if (Main.NetMode == 2)
						{
							NetMessage.SendTile(x, y);
						}
					}
					break;
				case 5:
					if (type != 57 && type != 78)
					{
						flag = true;
					}
					else if (Main.TileSet[x, y].Liquid > 0 && Main.TileSet[x, y].Lava == 0)
					{
						flag = true;
					}
					if (Main.NetMode == 1 || type2 == 82 || Main.TileSet[x, y].Lava == 0)
					{
						break;
					}
					if (Main.TileSet[x, y].Liquid > 16)
					{
						if (type2 == 83)
						{
							Main.TileSet[x, y].Type = 84;
							if (Main.NetMode == 2)
							{
								NetMessage.SendTile(x, y);
							}
						}
					}
					else if (type2 == 84)
					{
						Main.TileSet[x, y].Type = 83;
						if (Main.NetMode == 2)
						{
							NetMessage.SendTile(x, y);
						}
					}
					break;
				}
				break;
			}
			}
		}
		if (flag)
		{
			KillTile(x, y);
		}
	}

	public static void CheckBanner(int x, int j)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = j - Main.TileSet[x, j].FrameY / 18;
		int frameX = Main.TileSet[x, j].FrameX;
		bool flag = false;
		for (int i = 0; i < 3; i++)
		{
			if (Main.TileSet[x, num + i].IsActive == 0)
			{
				flag = true;
				break;
			}
			if (Main.TileSet[x, num + i].Type != 91)
			{
				flag = true;
				break;
			}
			if (Main.TileSet[x, num + i].FrameY != i * 18)
			{
				flag = true;
				break;
			}
			if (Main.TileSet[x, num + i].FrameX != frameX)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			flag = Main.TileSet[x, num - 1].IsActive == 0 || !Main.TileSolidNotSolidTop[Main.TileSet[x, num - 1].Type];
		}
		if (!flag)
		{
			return;
		}
		ToDestroyObject = true;
		for (int k = 0; k < 3; k++)
		{
			if (Main.TileSet[x, num + k].Type == 91)
			{
				KillTile(x, num + k);
			}
		}
		if (!Gen)
		{
			Item.NewItem(x * 16, (num + 1) * 16, 32, 32, 337 + frameX / 18);
		}
		ToDestroyObject = false;
	}

	public static bool PlaceBanner(int x, int y, int type, int style = 0)
	{
		int num = style * 18;
		if (Main.TileSet[x, y - 1].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[x, y - 1].Type] && Main.TileSet[x, y].IsActive == 0 && Main.TileSet[x, y + 1].IsActive == 0 && Main.TileSet[x, y + 2].IsActive == 0)
		{
			Main.TileSet[x, y].IsActive = 1;
			Main.TileSet[x, y].FrameY = 0;
			Main.TileSet[x, y].FrameX = (short)num;
			Main.TileSet[x, y].Type = (byte)type;
			Main.TileSet[x, y + 1].IsActive = 1;
			Main.TileSet[x, y + 1].FrameY = 18;
			Main.TileSet[x, y + 1].FrameX = (short)num;
			Main.TileSet[x, y + 1].Type = (byte)type;
			Main.TileSet[x, y + 2].IsActive = 1;
			Main.TileSet[x, y + 2].FrameY = 36;
			Main.TileSet[x, y + 2].FrameX = (short)num;
			Main.TileSet[x, y + 2].Type = (byte)type;
			return true;
		}
		return false;
	}

	public static bool PlaceMan(int i, int j, int dir)
	{
		for (int k = i; k <= i + 1; k++)
		{
			for (int l = j - 2; l <= j; l++)
			{
				if (Main.TileSet[k, l].IsActive != 0)
				{
					return false;
				}
			}
		}
		if (!SolidTileUnsafe(i, j + 1) || !SolidTileUnsafe(i + 1, j + 1))
		{
			return false;
		}
		int num = ((dir == 1) ? 36 : 0);
		Main.TileSet[i, j - 2].IsActive = 1;
		Main.TileSet[i, j - 2].FrameY = 0;
		Main.TileSet[i, j - 2].FrameX = (byte)num;
		Main.TileSet[i, j - 2].Type = 128;
		Main.TileSet[i, j - 1].IsActive = 1;
		Main.TileSet[i, j - 1].FrameY = 18;
		Main.TileSet[i, j - 1].FrameX = (byte)num;
		Main.TileSet[i, j - 1].Type = 128;
		Main.TileSet[i, j].IsActive = 1;
		Main.TileSet[i, j].FrameY = 36;
		Main.TileSet[i, j].FrameX = (byte)num;
		Main.TileSet[i, j].Type = 128;
		Main.TileSet[i + 1, j - 2].IsActive = 1;
		Main.TileSet[i + 1, j - 2].FrameY = 0;
		Main.TileSet[i + 1, j - 2].FrameX = (byte)(18 + num);
		Main.TileSet[i + 1, j - 2].Type = 128;
		Main.TileSet[i + 1, j - 1].IsActive = 1;
		Main.TileSet[i + 1, j - 1].FrameY = 18;
		Main.TileSet[i + 1, j - 1].FrameX = (byte)(18 + num);
		Main.TileSet[i + 1, j - 1].Type = 128;
		Main.TileSet[i + 1, j].IsActive = 1;
		Main.TileSet[i + 1, j].FrameY = 36;
		Main.TileSet[i + 1, j].FrameX = (byte)(18 + num);
		Main.TileSet[i + 1, j].Type = 128;
		return true;
	}

	public static void CheckMan(int i, int j)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = i;
		int num2 = j - Main.TileSet[i, j].FrameY / 18;
		int num3 = Main.TileSet[i, j].FrameX % 100 % 36;
		num -= num3 / 18;
		bool flag = false;
		for (int k = 0; k <= 1; k++)
		{
			for (int l = 0; l <= 2; l++)
			{
				int num4 = num + k;
				int num5 = num2 + l;
				int num6 = Main.TileSet[num4, num5].FrameX % 100;
				if (num6 >= 36)
				{
					num6 -= 36;
				}
				if (Main.TileSet[num4, num5].IsActive == 0 || Main.TileSet[num4, num5].Type != 128 || Main.TileSet[num4, num5].FrameY != l * 18 || num6 != k * 18)
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag && SolidTileUnsafe(num, num2 + 3) && SolidTileUnsafe(num + 1, num2 + 3))
		{
			return;
		}
		ToDestroyObject = true;
		for (int m = 0; m <= 1; m++)
		{
			for (int n = 0; n <= 2; n++)
			{
				int num7 = num + m;
				int num8 = num2 + n;
				if (Main.TileSet[num7, num8].IsActive != 0 && Main.TileSet[num7, num8].Type == 128)
				{
					KillTile(num7, num8);
				}
			}
		}
		if (!Gen)
		{
			Item.NewItem(i * 16, j * 16, 32, 32, 498);
		}
		ToDestroyObject = false;
	}

	public static bool Place1x2(int x, int y, int type, int style)
	{
		if (Main.TileSet[x, y + 1].IsActive != 0 && Main.TileSolid[Main.TileSet[x, y + 1].Type] && Main.TileSet[x, y - 1].IsActive == 0)
		{
			int num = ((type == 20) ? (genRand.Next(3) * 18) : 0);
			int num2 = style * 40;
			Main.TileSet[x, y - 1].IsActive = 1;
			Main.TileSet[x, y - 1].FrameY = (short)num2;
			Main.TileSet[x, y - 1].FrameX = (short)num;
			Main.TileSet[x, y - 1].Type = (byte)type;
			Main.TileSet[x, y].IsActive = 1;
			Main.TileSet[x, y].FrameY = (short)(num2 + 18);
			Main.TileSet[x, y].FrameX = (short)num;
			Main.TileSet[x, y].Type = (byte)type;
			return true;
		}
		return false;
	}

	public static bool Place1x2Top(int x, int y, int type)
	{
		if (Main.TileSet[x, y - 1].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[x, y - 1].Type] && Main.TileSet[x, y + 1].IsActive == 0)
		{
			Main.TileSet[x, y].IsActive = 1;
			Main.TileSet[x, y].FrameY = 0;
			Main.TileSet[x, y].FrameX = 0;
			Main.TileSet[x, y].Type = (byte)type;
			Main.TileSet[x, y + 1].IsActive = 1;
			Main.TileSet[x, y + 1].FrameY = 18;
			Main.TileSet[x, y + 1].FrameX = 0;
			Main.TileSet[x, y + 1].Type = (byte)type;
			return true;
		}
		return false;
	}

	public static void Check1x2Top(int x, int j)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = j;
		if (Main.TileSet[x, num].FrameY == 18)
		{
			num--;
		}
		if (Main.TileSet[x, num].FrameY != 0 || Main.TileSet[x, num + 1].FrameY != 18 || Main.TileSet[x, num].Type != 42 || Main.TileSet[x, num + 1].Type != 42 || Main.TileSet[x, num - 1].IsActive == 0 || !Main.TileSolidNotSolidTop[Main.TileSet[x, num - 1].Type])
		{
			ToDestroyObject = true;
			if (Main.TileSet[x, num].Type == 42)
			{
				KillTile(x, num);
			}
			if (Main.TileSet[x, num + 1].Type == 42)
			{
				KillTile(x, num + 1);
			}
			if (!Gen)
			{
				Item.NewItem(x * 16, num * 16, 32, 32, 136);
			}
			ToDestroyObject = false;
		}
	}

	public static void Check2x1(int i, int y, int type)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = i;
		if (Main.TileSet[num, y].FrameX == 18)
		{
			num--;
		}
		if (Main.TileSet[num, y].FrameX == 0 && Main.TileSet[num + 1, y].FrameX == 18 && Main.TileSet[num, y].Type == type && Main.TileSet[num + 1, y].Type == type)
		{
			if (type == 29 || type == 103)
			{
				if (Main.TileSet[num, y + 1].IsActive != 0 && Main.TileTable[Main.TileSet[num, y + 1].Type] && Main.TileSet[num + 1, y + 1].IsActive != 0 && Main.TileTable[Main.TileSet[num + 1, y + 1].Type])
				{
					return;
				}
			}
			else if (Main.TileSet[num, y + 1].IsActive != 0 && Main.TileSolid[Main.TileSet[num, y + 1].Type] && Main.TileSet[num + 1, y + 1].IsActive != 0 && Main.TileSolid[Main.TileSet[num + 1, y + 1].Type])
			{
				return;
			}
		}
		ToDestroyObject = true;
		if (Main.TileSet[num, y].Type == type)
		{
			KillTile(num, y);
		}
		if (Main.TileSet[num + 1, y].Type == type)
		{
			KillTile(num + 1, y);
		}
		if (!Gen)
		{
			switch ((EntityID.TileID)type)
			{
			case EntityID.TileID.ANVIL:
				Item.NewItem(num * 16, y * 16, 32, 32, 35);
				break;
			case EntityID.TileID.WORK_BENCH:
				Item.NewItem(num * 16, y * 16, 32, 32, 36);
				break;
			case EntityID.TileID.PIGGYBANK:
				Item.NewItem(num * 16, y * 16, 32, 32, 87);
				Main.PlaySound(13, i * 16, y * 16);
				break;
			case EntityID.TileID.BOWL:
				Item.NewItem(num * 16, y * 16, 32, 32, 356);
				Main.PlaySound(13, i * 16, y * 16);
				break;
			case EntityID.TileID.MYTHRIL_ANVIL:
				Item.NewItem(num * 16, y * 16, 32, 32, 525);
				break;
			}
		}
		ToDestroyObject = false;
		SquareTileFrame(num, y);
		SquareTileFrame(num + 1, y);
	}

	public static bool Place2x1(int x, int y, int type)
	{
		if ((type == 29 || type == 103 || Main.TileSet[x, y + 1].IsActive == 0 || Main.TileSet[x + 1, y + 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[x, y + 1].Type] || !Main.TileSolid[Main.TileSet[x + 1, y + 1].Type] || Main.TileSet[x, y].IsActive != 0 || Main.TileSet[x + 1, y].IsActive != 0) && ((type != 29 && type != 103) || Main.TileSet[x, y + 1].IsActive == 0 || Main.TileSet[x + 1, y + 1].IsActive == 0 || !Main.TileTable[Main.TileSet[x, y + 1].Type] || !Main.TileTable[Main.TileSet[x + 1, y + 1].Type] || Main.TileSet[x, y].IsActive != 0 || Main.TileSet[x + 1, y].IsActive != 0))
		{
			return false;
		}
		Main.TileSet[x, y].IsActive = 1;
		Main.TileSet[x, y].FrameY = 0;
		Main.TileSet[x, y].FrameX = 0;
		Main.TileSet[x, y].Type = (byte)type;
		Main.TileSet[x + 1, y].IsActive = 1;
		Main.TileSet[x + 1, y].FrameY = 0;
		Main.TileSet[x + 1, y].FrameX = 18;
		Main.TileSet[x + 1, y].Type = (byte)type;
		return true;
	}

	private static void Destroy4x2(int i, int j, int x2, int y2, int type)
	{
		ToDestroyObject = true;
		for (int k = x2; k < x2 + 4; k++)
		{
			for (int l = y2; l < y2 + 3; l++)
			{
				if (Main.TileSet[k, l].Type == type && Main.TileSet[k, l].IsActive != 0)
				{
					KillTile(k, l);
				}
			}
		}
		if (!Gen)
		{
			switch ((EntityID.TileID)type)
			{
			case EntityID.TileID.BED:
				Item.NewItem(i * 16, j * 16, 32, 32, 224);
				break;
			case EntityID.TileID.BATHTUB:
				Item.NewItem(i * 16, j * 16, 32, 32, 336);
				break;
			}
		}
		ToDestroyObject = false;
		bool flag = tileFrameRecursion;
		tileFrameRecursion = false;
		for (int m = x2 - 1; m < x2 + 4; m++)
		{
			for (int n = y2 - 1; n < y2 + 4; n++)
			{
				TileFrame(m, n);
			}
		}
		tileFrameRecursion = flag;
	}

	public static void Check4x2(int i, int j, int type)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = i;
		int num2 = j;
		num -= Main.TileSet[i, j].FrameX / 18;
		if ((type == 79 || type == 90) && Main.TileSet[i, j].FrameX >= 72)
		{
			num += 4;
		}
		num2 -= Main.TileSet[i, j].FrameY / 18;
		for (int k = num; k < num + 4; k++)
		{
			for (int l = num2; l < num2 + 2; l++)
			{
				int num3 = (k - num) * 18;
				if ((type == 79 || type == 90) && Main.TileSet[i, j].FrameX >= 72)
				{
					num3 = (k - num + 4) * 18;
				}
				if (Main.TileSet[k, l].IsActive == 0 || Main.TileSet[k, l].Type != type || Main.TileSet[k, l].FrameX != num3 || Main.TileSet[k, l].FrameY != (l - num2) * 18)
				{
					Destroy4x2(i, j, num, num2, type);
					return;
				}
			}
			if (Main.TileSet[k, num2 + 2].IsActive == 0 || !Main.TileSolid[Main.TileSet[k, num2 + 2].Type])
			{
				Destroy4x2(i, j, num, num2, type);
				break;
			}
		}
	}

	private static void Destroy2x2(int i, int j, int x2, int y2, int type)
	{
		ToDestroyObject = true;
		for (int k = x2; k < x2 + 2; k++)
		{
			for (int l = y2; l < y2 + 2; l++)
			{
				if (Main.TileSet[k, l].Type == type && Main.TileSet[k, l].IsActive != 0)
				{
					KillTile(k, l);
				}
			}
		}
		if (!Gen)
		{
			switch ((EntityID.TileID)type)
			{
			case EntityID.TileID.TOMBSTONE:
				Item.NewItem(i * 16, j * 16, 32, 32, 321);
				break;
			case EntityID.TileID.KEG:
				Item.NewItem(i * 16, j * 16, 32, 32, 352);
				break;
			case EntityID.TileID.CHINESE_LANTERN:
				Item.NewItem(i * 16, j * 16, 32, 32, 344);
				break;
			case EntityID.TileID.COOKING_POT:
				Item.NewItem(i * 16, j * 16, 32, 32, 345);
				break;
			case EntityID.TileID.SAFE:
				Item.NewItem(i * 16, j * 16, 32, 32, 346);
				break;
			case EntityID.TileID.SKULL_LANTERN:
				Item.NewItem(i * 16, j * 16, 32, 32, 347);
				break;
			case EntityID.TileID.TRASH_CAN:
				Item.NewItem(i * 16, j * 16, 32, 32, 348);
				break;
			case EntityID.TileID.CANDELABRA:
				Item.NewItem(i * 16, j * 16, 32, 32, 349);
				break;
			case EntityID.TileID.CRYSTAL_BALL:
				Item.NewItem(i * 16, j * 16, 32, 32, 487);
				break;
			case EntityID.TileID.DISCO_BALL:
				Item.NewItem(i * 16, j * 16, 32, 32, 488);
				break;
			case EntityID.TileID.LEVER:
				Item.NewItem(i * 16, j * 16, 32, 32, 513);
				break;
			case EntityID.TileID.PUMP_IN:
				Item.NewItem(i * 16, j * 16, 32, 32, 581);
				break;
			case EntityID.TileID.PUMP_OUT:
				Item.NewItem(i * 16, j * 16, 32, 32, 582);
				break;
			case EntityID.TileID.BOULDER:
				if (Main.NetMode != 1)
				{
					Projectile.NewProjectile((float)(x2 * 16) + 15.5f, y2 * 16 + 16, 0f, 0f, 99, 70, 10f);
				}
				break;
			}
		}
		ToDestroyObject = false;
		bool flag = tileFrameRecursion;
		tileFrameRecursion = false;
		for (int m = x2 - 1; m < x2 + 3; m++)
		{
			for (int n = y2 - 1; n < y2 + 3; n++)
			{
				TileFrame(m, n);
			}
		}
		tileFrameRecursion = flag;
	}

	public static void Check2x2(int i, int j, int type)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = i;
		int num2 = j;
		int num3 = 0;
		num = -(Main.TileSet[i, j].FrameX / 18);
		num2 = -(Main.TileSet[i, j].FrameY / 18);
		if (num < -1)
		{
			num += 2;
			num3 = 36;
		}
		num += i;
		num2 += j;
		for (int k = num; k < num + 2; k++)
		{
			for (int l = num2; l < num2 + 2; l++)
			{
				if (Main.TileSet[k, l].IsActive == 0 || Main.TileSet[k, l].Type != type || Main.TileSet[k, l].FrameX != (k - num) * 18 + num3 || Main.TileSet[k, l].FrameY != (l - num2) * 18)
				{
					Destroy2x2(i, j, num, num2, type);
					return;
				}
			}
			switch ((EntityID.TileID)type)
			{
			case EntityID.TileID.CHINESE_LANTERN:
			case EntityID.TileID.DISCO_BALL:
				if (Main.TileSet[k, num2 - 1].IsActive == 0 || !Main.TileSolidNotSolidTop[Main.TileSet[k, num2 - 1].Type])
				{
					Destroy2x2(i, j, num, num2, type);
					return;
				}
				break;
			default:
				if (Main.TileSet[k, num2 + 2].IsActive == 0 || (!Main.TileSolid[Main.TileSet[k, num2 + 2].Type] && !Main.TileTable[Main.TileSet[k, num2 + 2].Type]))
				{
					Destroy2x2(i, j, num, num2, type);
					return;
				}
				break;
			case EntityID.TileID.BOULDER:
				break;
			}
		}
		if (type == 138 && !SolidTileUnsafe(num, num2 + 2) && !SolidTileUnsafe(num + 1, num2 + 2))
		{
			Destroy2x2(i, j, num, num2, type);
		}
	}

	public static void OreRunner(int i, int j, double strength, int steps, int type)
	{
		Vector2 vector = default;
		Vector2 vector2 = default;
		double num = strength;
		float num2 = steps;
		vector.X = i;
		vector.Y = j;
		vector2.X = (float)genRand.Next(-10, 11) * 0.1f;
		vector2.Y = (float)genRand.Next(-10, 11) * 0.1f;
		while (num > 0.0 && num2 > 0f)
		{
			if (vector.Y < 0f && num2 > 0f && type == 59)
			{
				num2 = 0f;
			}
			num = strength * (double)(num2 / (float)steps);
			num2--;
			int num3 = (int)((double)vector.X - num * 0.5);
			int num4 = (int)((double)vector.X + num * 0.5);
			int num5 = (int)((double)vector.Y - num * 0.5);
			int num6 = (int)((double)vector.Y + num * 0.5);
			if (num3 < 0)
			{
				num3 = 0;
			}
			if (num4 > Main.MaxTilesX)
			{
				num4 = Main.MaxTilesX;
			}
			if (num5 < 0)
			{
				num5 = 0;
			}
			if (num6 > Main.MaxTilesY)
			{
				num6 = Main.MaxTilesY;
			}
			for (int k = num3; k < num4; k++)
			{
				for (int l = num5; l < num6; l++)
				{
					if ((double)(Math.Abs((float)k - vector.X) + Math.Abs((float)l - vector.Y)) < strength * 0.5 * (double)(1f + (float)genRand.Next(-10, 11) * 0.015f) && Main.TileSet[k, l].IsActive != 0 && EntityID.ReplaceableGrounds.Contains((EntityID.TileID)Main.TileSet[k, l].Type))
					{
						Main.TileSet[k, l].Type = (byte)type;
						SquareTileFrame(k, l);
						if (Main.NetMode == 2)
						{
							NetMessage.SendTile(k, l);
						}
					}
				}
			}
			vector.X += vector2.X;
			vector.Y += vector2.Y;
			vector2.X += (float)genRand.Next(-10, 11) * 0.05f;
			if (vector2.X > 1f)
			{
				vector2.X = 1f;
			}
			else if (vector2.X < -1f)
			{
				vector2.X = -1f;
			}
		}
	}

	public static void SmashAltar(int i, int j)
	{
		if (!Main.InHardMode || Main.NetMode == 1)
		{
			return;
		}
		int num = altarCount % 3;
		NetMessage.SendText(12 + num, 50, 255, 130, -1);
		int num2 = altarCount / 3 + 1;
		float num3 = (float)Main.MaxTilesX / 4200f;
		int num4 = 1 - num;
		num3 = num3 * 310f - (float)(85 * num);
		num3 *= 0.85f;
		num3 /= (float)num2;
		switch (num)
		{
		case 0:
			num = 107;
			num3 *= 1.05f;
			break;
		case 1:
			num = 108;
			break;
		default:
			num = 111;
			break;
		}
		for (int k = 0; (float)k < num3; k++)
		{
			int i2 = genRand.Next(100, Main.MaxTilesX - 100);
			int lowerBound = Main.WorldSurface;
			switch (num)
			{
			case 108:
				lowerBound = Main.RockLayer;
				break;
			case 111:
				lowerBound = (Main.RockLayer + Main.RockLayer + Main.MaxTilesY) / 3;
				break;
			}
			int j2 = genRand.Next(lowerBound, Main.MaxTilesY - 150);
			OreRunner(i2, j2, genRand.Next(5, 9 + num4), genRand.Next(5, 9 + num4), num);
		}
		int num5 = genRand.Next(3);
		while (num5 != 2)
		{
			int num6 = genRand.Next(100, Main.MaxTilesX - 100);
			int num7 = genRand.Next(Main.RockLayer + 50, Main.MaxTilesY - 300);
			if (Main.TileSet[num6, num7].IsActive != 0 && Main.TileSet[num6, num7].Type == 1)
			{
				if (num5 == 0)
				{
					Main.TileSet[num6, num7].Type = 25;
				}
				else
				{
					Main.TileSet[num6, num7].Type = 117;
				}
				if (Main.NetMode == 2)
				{
					NetMessage.SendTile(num6, num7);
				}
				break;
			}
		}
		if (Main.NetMode != 1)
		{
			int num8 = Main.Rand.Next(2) + 1;
			Rectangle rect = default;
			rect.X = i << 4;
			rect.Y = j << 4;
			rect.Width = (rect.Height = 16);
			for (int l = 0; l < num8; l++)
			{
				NPC.SpawnOnPlayer(Player.FindClosest(ref rect), 82);
			}
		}
		altarCount++;
	}

	public static void Check3x2(int i, int j, int type)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = i;
		int num2 = j;
		num -= Main.TileSet[i, j].FrameX / 18;
		num2 -= Main.TileSet[i, j].FrameY / 18;
		int num3 = num;
		while (true)
		{
			if (num3 >= num + 3)
			{
				return;
			}
			if (Main.TileSet[num3, num2 + 2].IsActive == 0 || !Main.TileSolid[Main.TileSet[num3, num2 + 2].Type])
			{
				break;
			}
			bool flag = false;
			for (int k = num2; k < num2 + 2; k++)
			{
				if (Main.TileSet[num3, k].IsActive == 0 || Main.TileSet[num3, k].Type != type || Main.TileSet[num3, k].FrameX != (num3 - num) * 18 || Main.TileSet[num3, k].FrameY != (k - num2) * 18)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				break;
			}
			num3++;
		}
		ToDestroyObject = true;
		for (int l = num; l < num + 3; l++)
		{
			for (int m = num2; m < num2 + 3; m++)
			{
				if (Main.TileSet[l, m].Type == type && Main.TileSet[l, m].IsActive != 0)
				{
					KillTile(l, m);
				}
			}
		}
		if (!Gen)
		{
			switch ((EntityID.TileID)type)
			{
			case EntityID.TileID.TABLE:
				Item.NewItem(i * 16, j * 16, 32, 32, 32);
				break;
			case EntityID.TileID.TINKERERS_WORKSHOP:
				Item.NewItem(i * 16, j * 16, 32, 32, 398);
				break;
			case EntityID.TileID.DEMON_ALTAR:
				SmashAltar(i, j);
				break;
			case EntityID.TileID.FURNACE:
				Item.NewItem(i * 16, j * 16, 32, 32, 33);
				break;
			case EntityID.TileID.HELLFORGE:
				Item.NewItem(i * 16, j * 16, 32, 32, 221);
				break;
			case EntityID.TileID.LOOM:
				Item.NewItem(i * 16, j * 16, 32, 32, 332);
				break;
			case EntityID.TileID.PIANO:
				Item.NewItem(i * 16, j * 16, 32, 32, 333);
				break;
			case EntityID.TileID.DRESSER:
				Item.NewItem(i * 16, j * 16, 32, 32, 334);
				break;
			case EntityID.TileID.BENCH:
				Item.NewItem(i * 16, j * 16, 32, 32, 335);
				break;
			case EntityID.TileID.ADAMANTITE_FORGE:
				Item.NewItem(i * 16, j * 16, 32, 32, 524);
				break;
			}
		}
		ToDestroyObject = false;
		bool flag2 = tileFrameRecursion;
		tileFrameRecursion = false;
		TileFrame(num - 1, num2 - 1);
		TileFrame(num, num2 - 1);
		TileFrame(num + 1, num2 - 1);
		TileFrame(num + 2, num2 - 1);
		TileFrame(num - 1, num2);
		TileFrame(num, num2);
		TileFrame(num + 1, num2);
		TileFrame(num + 2, num2);
		TileFrame(num - 1, num2 + 1);
		TileFrame(num, num2 + 1);
		TileFrame(num + 1, num2 + 1);
		TileFrame(num + 2, num2 + 1);
		TileFrame(num - 1, num2 + 2);
		TileFrame(num, num2 + 2);
		TileFrame(num + 1, num2 + 2);
		TileFrame(num + 2, num2 + 2);
		TileFrame(num - 1, num2 + 3);
		TileFrame(num, num2 + 3);
		TileFrame(num + 1, num2 + 3);
		TileFrame(num + 2, num2 + 3);
		tileFrameRecursion = flag2;
	}

	public static void Check3x4(int i, int j, int type)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = i;
		int num2 = j;
		num -= Main.TileSet[i, j].FrameX / 18;
		num2 -= Main.TileSet[i, j].FrameY / 18;
		for (int k = num; k < num + 3; k++)
		{
			int num3 = num2;
			while (true)
			{
				if (num3 < num2 + 4)
				{
					if (Main.TileSet[k, num3].IsActive != 0 && Main.TileSet[k, num3].Type == type && Main.TileSet[k, num3].FrameX == (k - num) * 18 && Main.TileSet[k, num3].FrameY == (num3 - num2) * 18)
					{
						num3++;
						continue;
					}
				}
				else if (Main.TileSet[k, num2 + 4].IsActive != 0 && Main.TileSolid[Main.TileSet[k, num2 + 4].Type])
				{
					break;
				}
				ToDestroyObject = true;
				for (int l = num; l < num + 3; l++)
				{
					for (int m = num2; m < num2 + 4; m++)
					{
						if (Main.TileSet[l, m].Type == type && Main.TileSet[l, m].IsActive != 0)
						{
							KillTile(l, m);
						}
					}
				}
				if (!Gen)
				{
					switch ((EntityID.TileID)type)
					{
					case EntityID.TileID.BOOKCASE:
						Item.NewItem(i * 16, j * 16, 32, 32, 354);
						break;
					case EntityID.TileID.THRONE:
						Item.NewItem(i * 16, j * 16, 32, 32, 355);
						break;
					}
				}
				ToDestroyObject = false;
				bool flag = tileFrameRecursion;
				tileFrameRecursion = false;
				for (int n = num - 1; n < num + 4; n++)
				{
					for (int num4 = num2 - 1; num4 < num2 + 4; num4++)
					{
						TileFrame(n, num4);
					}
				}
				tileFrameRecursion = flag;
				return;
			}
		}
	}

	public static bool Place4x2(int x, int y, int type, int direction = -1)
	{
		if (x < 5 || x > Main.MaxTilesX - 5 || y < 5 || y > Main.MaxTilesY - 5)
		{
			return false;
		}
		for (int i = x - 1; i < x + 3; i++)
		{
			if (Main.TileSet[i, y + 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[i, y + 1].Type])
			{
				return false;
			}
			for (int j = y - 1; j < y + 1; j++)
			{
				if (Main.TileSet[i, j].IsActive != 0)
				{
					return false;
				}
			}
		}
		int num = ((direction == 1) ? 72 : 0);
		Main.TileSet[x - 1, y - 1].IsActive = 1;
		Main.TileSet[x - 1, y - 1].FrameY = 0;
		Main.TileSet[x - 1, y - 1].FrameX = (short)num;
		Main.TileSet[x - 1, y - 1].Type = (byte)type;
		Main.TileSet[x, y - 1].IsActive = 1;
		Main.TileSet[x, y - 1].FrameY = 0;
		Main.TileSet[x, y - 1].FrameX = (short)(18 + num);
		Main.TileSet[x, y - 1].Type = (byte)type;
		Main.TileSet[x + 1, y - 1].IsActive = 1;
		Main.TileSet[x + 1, y - 1].FrameY = 0;
		Main.TileSet[x + 1, y - 1].FrameX = (short)(36 + num);
		Main.TileSet[x + 1, y - 1].Type = (byte)type;
		Main.TileSet[x + 2, y - 1].IsActive = 1;
		Main.TileSet[x + 2, y - 1].FrameY = 0;
		Main.TileSet[x + 2, y - 1].FrameX = (short)(54 + num);
		Main.TileSet[x + 2, y - 1].Type = (byte)type;
		Main.TileSet[x - 1, y].IsActive = 1;
		Main.TileSet[x - 1, y].FrameY = 18;
		Main.TileSet[x - 1, y].FrameX = (short)num;
		Main.TileSet[x - 1, y].Type = (byte)type;
		Main.TileSet[x, y].IsActive = 1;
		Main.TileSet[x, y].FrameY = 18;
		Main.TileSet[x, y].FrameX = (short)(18 + num);
		Main.TileSet[x, y].Type = (byte)type;
		Main.TileSet[x + 1, y].IsActive = 1;
		Main.TileSet[x + 1, y].FrameY = 18;
		Main.TileSet[x + 1, y].FrameX = (short)(36 + num);
		Main.TileSet[x + 1, y].Type = (byte)type;
		Main.TileSet[x + 2, y].IsActive = 1;
		Main.TileSet[x + 2, y].FrameY = 18;
		Main.TileSet[x + 2, y].FrameX = (short)(54 + num);
		Main.TileSet[x + 2, y].Type = (byte)type;
		return true;
	}

	public static void SwitchMB(int i, int j)
	{
		int num = i;
		int num2 = j;
		int num3 = (Main.TileSet[i, j].FrameY / 18) & 1;
		int num4 = Main.TileSet[i, j].FrameX / 18;
		if (num4 >= 2)
		{
			num4 -= 2;
		}
		num = i - num4;
		num2 = j - num3;
		for (int k = num; k < num + 2; k++)
		{
			for (int l = num2; l < num2 + 2; l++)
			{
				if (Main.TileSet[k, l].IsActive != 0 && Main.TileSet[k, l].Type == 139)
				{
					if (Main.TileSet[k, l].FrameX < 36)
					{
						Main.TileSet[k, l].FrameX += 36;
					}
					else
					{
						Main.TileSet[k, l].FrameX -= 36;
					}
					noWire[numNoWire].X = (short)k;
					noWire[numNoWire].Y = (short)l;
					numNoWire++;
				}
			}
		}
		NetMessage.SendTileSquare(num, num2, 3);
	}

	public static void CheckMusicBox(int i, int j)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = i;
		int num2 = j;
		int num3 = Main.TileSet[i, j].FrameY / 18;
		int num4 = num3 >> 1;
		num3 &= 1;
		int num5 = Main.TileSet[i, j].FrameX / 18;
		int num6 = 0;
		if (num5 >= 2)
		{
			num5 -= 2;
			num6++;
		}
		num = i - num5;
		num2 = j - num3;
		for (int k = num; k < num + 2; k++)
		{
			int num7 = num2;
			while (true)
			{
				if (num7 < num2 + 2)
				{
					if (Main.TileSet[k, num7].IsActive != 0 && Main.TileSet[k, num7].Type == 139 && Main.TileSet[k, num7].FrameX == (k - num) * 18 + num6 * 36 && Main.TileSet[k, num7].FrameY == (num7 - num2) * 18 + num4 * 36)
					{
						num7++;
						continue;
					}
				}
				else if (Main.TileSolid[Main.TileSet[k, num2 + 2].Type])
				{
					break;
				}
				ToDestroyObject = true;
				for (int l = num; l < num + 2; l++)
				{
					for (int m = num2; m < num2 + 3; m++)
					{
						if (Main.TileSet[l, m].Type == 139 && Main.TileSet[l, m].IsActive != 0)
						{
							KillTile(l, m);
						}
					}
				}
				Item.NewItem(i * 16, j * 16, 32, 32, 562 + num4);
				bool flag = tileFrameRecursion;
				tileFrameRecursion = false;
				for (int n = num - 1; n < num + 3; n++)
				{
					for (int num8 = num2 - 1; num8 < num2 + 3; num8++)
					{
						TileFrame(n, num8);
					}
				}
				tileFrameRecursion = flag;
				ToDestroyObject = false;
				return;
			}
		}
	}

	public static bool PlaceMB(int X, int y, int type, int style)
	{
		int num = X + 1;
		if (num < 5 || num > Main.MaxTilesX - 5 || y < 5 || y > Main.MaxTilesY - 5)
		{
			return false;
		}
		for (int i = num - 1; i < num + 1; i++)
		{
			for (int j = y - 1; j < y + 1; j++)
			{
				if (Main.TileSet[i, j].IsActive != 0)
				{
					return false;
				}
			}
			if (Main.TileSet[i, y + 1].IsActive == 0 || (!Main.TileSolid[Main.TileSet[i, y + 1].Type] && !Main.TileTable[Main.TileSet[i, y + 1].Type]))
			{
				return false;
			}
		}
		Main.TileSet[num - 1, y - 1].IsActive = 1;
		Main.TileSet[num - 1, y - 1].FrameY = (short)(style * 36);
		Main.TileSet[num - 1, y - 1].FrameX = 0;
		Main.TileSet[num - 1, y - 1].Type = (byte)type;
		Main.TileSet[num, y - 1].IsActive = 1;
		Main.TileSet[num, y - 1].FrameY = (short)(style * 36);
		Main.TileSet[num, y - 1].FrameX = 18;
		Main.TileSet[num, y - 1].Type = (byte)type;
		Main.TileSet[num - 1, y].IsActive = 1;
		Main.TileSet[num - 1, y].FrameY = (short)(style * 36 + 18);
		Main.TileSet[num - 1, y].FrameX = 0;
		Main.TileSet[num - 1, y].Type = (byte)type;
		Main.TileSet[num, y].IsActive = 1;
		Main.TileSet[num, y].FrameY = (short)(style * 36 + 18);
		Main.TileSet[num, y].FrameX = 18;
		Main.TileSet[num, y].Type = (byte)type;
		return true;
	}

	public static bool Place2x2(int x, int y, int type)
	{
		if (type == 95 || type == 126)
		{
			y++;
		}
		if (x < 5 || x > Main.MaxTilesX - 5 || y < 5 || y > Main.MaxTilesY - 5)
		{
			return false;
		}
		for (int i = x - 1; i < x + 1; i++)
		{
			if (type == 95 || type == 126)
			{
				if (Main.TileSet[i, y - 2].IsActive == 0 || !Main.TileSolidNotSolidTop[Main.TileSet[i, y - 2].Type])
				{
					return false;
				}
			}
			else if (Main.TileSet[i, y + 1].IsActive == 0 || (!Main.TileSolid[Main.TileSet[i, y + 1].Type] && !Main.TileTable[Main.TileSet[i, y + 1].Type]))
			{
				return false;
			}
			for (int j = y - 1; j < y + 1; j++)
			{
				if (Main.TileSet[i, j].IsActive != 0 || (type == 98 && Main.TileSet[i, j].Liquid > 0))
				{
					return false;
				}
			}
		}
		Main.TileSet[x - 1, y - 1].IsActive = 1;
		Main.TileSet[x - 1, y - 1].FrameY = 0;
		Main.TileSet[x - 1, y - 1].FrameX = 0;
		Main.TileSet[x - 1, y - 1].Type = (byte)type;
		Main.TileSet[x, y - 1].IsActive = 1;
		Main.TileSet[x, y - 1].FrameY = 0;
		Main.TileSet[x, y - 1].FrameX = 18;
		Main.TileSet[x, y - 1].Type = (byte)type;
		Main.TileSet[x - 1, y].IsActive = 1;
		Main.TileSet[x - 1, y].FrameY = 18;
		Main.TileSet[x - 1, y].FrameX = 0;
		Main.TileSet[x - 1, y].Type = (byte)type;
		Main.TileSet[x, y].IsActive = 1;
		Main.TileSet[x, y].FrameY = 18;
		Main.TileSet[x, y].FrameX = 18;
		Main.TileSet[x, y].Type = (byte)type;
		return true;
	}

	public static bool Place3x4(int x, int y, int type)
	{
		if (x < 5 || x > Main.MaxTilesX - 5 || y < 5 || y > Main.MaxTilesY - 5)
		{
			return false;
		}
		for (int i = x - 1; i < x + 2; i++)
		{
			if (Main.TileSet[i, y + 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[i, y + 1].Type])
			{
				return false;
			}
			for (int j = y - 3; j < y + 1; j++)
			{
				if (Main.TileSet[i, j].IsActive != 0)
				{
					return false;
				}
			}
		}
		for (int k = -3; k <= 0; k++)
		{
			short frameY = (short)((3 + k) * 18);
			Main.TileSet[x - 1, y + k].IsActive = 1;
			Main.TileSet[x - 1, y + k].FrameY = frameY;
			Main.TileSet[x - 1, y + k].FrameX = 0;
			Main.TileSet[x - 1, y + k].Type = (byte)type;
			Main.TileSet[x, y + k].IsActive = 1;
			Main.TileSet[x, y + k].FrameY = frameY;
			Main.TileSet[x, y + k].FrameX = 18;
			Main.TileSet[x, y + k].Type = (byte)type;
			Main.TileSet[x + 1, y + k].IsActive = 1;
			Main.TileSet[x + 1, y + k].FrameY = frameY;
			Main.TileSet[x + 1, y + k].FrameX = 36;
			Main.TileSet[x + 1, y + k].Type = (byte)type;
		}
		return true;
	}

	public static bool Place3x2(int x, int y, int type)
	{
		if (x < 5 || x > Main.MaxTilesX - 5 || y < 5 || y > Main.MaxTilesY - 5)
		{
			return false;
		}
		for (int i = x - 1; i < x + 2; i++)
		{
			for (int j = y - 1; j < y + 1; j++)
			{
				if (Main.TileSet[i, j].IsActive != 0)
				{
					return false;
				}
			}
			if (Main.TileSet[i, y + 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[i, y + 1].Type])
			{
				return false;
			}
		}
		Main.TileSet[x - 1, y - 1].IsActive = 1;
		Main.TileSet[x - 1, y - 1].FrameY = 0;
		Main.TileSet[x - 1, y - 1].FrameX = 0;
		Main.TileSet[x - 1, y - 1].Type = (byte)type;
		Main.TileSet[x, y - 1].IsActive = 1;
		Main.TileSet[x, y - 1].FrameY = 0;
		Main.TileSet[x, y - 1].FrameX = 18;
		Main.TileSet[x, y - 1].Type = (byte)type;
		Main.TileSet[x + 1, y - 1].IsActive = 1;
		Main.TileSet[x + 1, y - 1].FrameY = 0;
		Main.TileSet[x + 1, y - 1].FrameX = 36;
		Main.TileSet[x + 1, y - 1].Type = (byte)type;
		Main.TileSet[x - 1, y].IsActive = 1;
		Main.TileSet[x - 1, y].FrameY = 18;
		Main.TileSet[x - 1, y].FrameX = 0;
		Main.TileSet[x - 1, y].Type = (byte)type;
		Main.TileSet[x, y].IsActive = 1;
		Main.TileSet[x, y].FrameY = 18;
		Main.TileSet[x, y].FrameX = 18;
		Main.TileSet[x, y].Type = (byte)type;
		Main.TileSet[x + 1, y].IsActive = 1;
		Main.TileSet[x + 1, y].FrameY = 18;
		Main.TileSet[x + 1, y].FrameX = 36;
		Main.TileSet[x + 1, y].Type = (byte)type;
		return true;
	}

	public static void Check3x3(int i, int j, int type)
	{
		if (ToDestroyObject)
		{
			return;
		}
		bool flag = false;
		int num = i;
		int num2 = j;
		num = Main.TileSet[i, j].FrameX / 18;
		int num3 = i - num;
		if (num >= 3)
		{
			num -= 3;
		}
		num = i - num;
		num2 += Main.TileSet[i, j].FrameY / 18 * -1;
		for (int k = num; k < num + 3; k++)
		{
			for (int l = num2; l < num2 + 3; l++)
			{
				if (Main.TileSet[k, l].IsActive == 0 || Main.TileSet[k, l].Type != type || Main.TileSet[k, l].FrameX != (k - num3) * 18 || Main.TileSet[k, l].FrameY != (l - num2) * 18)
				{
					flag = true;
				}
			}
		}
		if (type == 106)
		{
			for (int m = num; m < num + 3; m++)
			{
				if (Main.TileSet[m, num2 + 3].IsActive == 0 || !Main.TileSolid[Main.TileSet[m, num2 + 3].Type])
				{
					flag = true;
					break;
				}
			}
		}
		else if (Main.TileSet[num + 1, num2 - 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[num + 1, num2 - 1].Type] || Main.TileSolidTop[Main.TileSet[num + 1, num2 - 1].Type])
		{
			flag = true;
		}
		if (!flag)
		{
			return;
		}
		ToDestroyObject = true;
		for (int n = num; n < num + 3; n++)
		{
			for (int num4 = num2; num4 < num2 + 3; num4++)
			{
				if (Main.TileSet[n, num4].Type == type && Main.TileSet[n, num4].IsActive != 0)
				{
					KillTile(n, num4);
				}
			}
		}
		switch ((EntityID.TileID)type)
		{
		case EntityID.TileID.CHANDELIER:
			Item.NewItem(i * 16, j * 16, 32, 32, 106);
			break;
		case EntityID.TileID.JACK_O_LANTERN:
			Item.NewItem(i * 16, j * 16, 32, 32, 107);
			break;
		case EntityID.TileID.PRESENT:
			Item.NewItem(i * 16, j * 16, 32, 32, 108);
			break;
		case EntityID.TileID.SAWMILL:
			Item.NewItem(i * 16, j * 16, 32, 32, 363);
			break;
		}
		ToDestroyObject = false;
		bool flag2 = tileFrameRecursion;
		tileFrameRecursion = false;
		for (int num5 = num - 1; num5 < num + 4; num5++)
		{
			for (int num6 = num2 - 1; num6 < num2 + 4; num6++)
			{
				TileFrame(num5, num6);
			}
		}
		tileFrameRecursion = flag2;
	}

	public static bool Place3x3(int x, int y, int type)
	{
		int num = 0;
		if (type == 106)
		{
			num = -2;
			for (int i = x - 1; i < x + 2; i++)
			{
				for (int j = y - 2; j < y + 1; j++)
				{
					if (Main.TileSet[i, j].IsActive != 0)
					{
						return false;
					}
				}
			}
			for (int k = x - 1; k < x + 2; k++)
			{
				if (Main.TileSet[k, y + 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[k, y + 1].Type])
				{
					return false;
				}
			}
		}
		else
		{
			for (int l = x - 1; l < x + 2; l++)
			{
				for (int m = y; m < y + 3; m++)
				{
					if (Main.TileSet[l, m].IsActive != 0)
					{
						return false;
					}
				}
			}
			if (Main.TileSet[x, y - 1].IsActive == 0 || !Main.TileSolidNotSolidTop[Main.TileSet[x, y - 1].Type])
			{
				return false;
			}
		}
		Main.TileSet[x - 1, y + num].IsActive = 1;
		Main.TileSet[x - 1, y + num].FrameY = 0;
		Main.TileSet[x - 1, y + num].FrameX = 0;
		Main.TileSet[x - 1, y + num].Type = (byte)type;
		Main.TileSet[x, y + num].IsActive = 1;
		Main.TileSet[x, y + num].FrameY = 0;
		Main.TileSet[x, y + num].FrameX = 18;
		Main.TileSet[x, y + num].Type = (byte)type;
		Main.TileSet[x + 1, y + num].IsActive = 1;
		Main.TileSet[x + 1, y + num].FrameY = 0;
		Main.TileSet[x + 1, y + num].FrameX = 36;
		Main.TileSet[x + 1, y + num].Type = (byte)type;
		Main.TileSet[x - 1, y + 1 + num].IsActive = 1;
		Main.TileSet[x - 1, y + 1 + num].FrameY = 18;
		Main.TileSet[x - 1, y + 1 + num].FrameX = 0;
		Main.TileSet[x - 1, y + 1 + num].Type = (byte)type;
		Main.TileSet[x, y + 1 + num].IsActive = 1;
		Main.TileSet[x, y + 1 + num].FrameY = 18;
		Main.TileSet[x, y + 1 + num].FrameX = 18;
		Main.TileSet[x, y + 1 + num].Type = (byte)type;
		Main.TileSet[x + 1, y + 1 + num].IsActive = 1;
		Main.TileSet[x + 1, y + 1 + num].FrameY = 18;
		Main.TileSet[x + 1, y + 1 + num].FrameX = 36;
		Main.TileSet[x + 1, y + 1 + num].Type = (byte)type;
		Main.TileSet[x - 1, y + 2 + num].IsActive = 1;
		Main.TileSet[x - 1, y + 2 + num].FrameY = 36;
		Main.TileSet[x - 1, y + 2 + num].FrameX = 0;
		Main.TileSet[x - 1, y + 2 + num].Type = (byte)type;
		Main.TileSet[x, y + 2 + num].IsActive = 1;
		Main.TileSet[x, y + 2 + num].FrameY = 36;
		Main.TileSet[x, y + 2 + num].FrameX = 18;
		Main.TileSet[x, y + 2 + num].Type = (byte)type;
		Main.TileSet[x + 1, y + 2 + num].IsActive = 1;
		Main.TileSet[x + 1, y + 2 + num].FrameY = 36;
		Main.TileSet[x + 1, y + 2 + num].FrameX = 36;
		Main.TileSet[x + 1, y + 2 + num].Type = (byte)type;
		return true;
	}

	public static bool PlaceSunflower(int x, int y)
	{
		if (y > Main.WorldSurface - 1)
		{
			return false;
		}
		for (int i = x; i < x + 2; i++)
		{
			for (int j = y - 3; j < y + 1; j++)
			{
				if (Main.TileSet[i, j].IsActive != 0 || Main.TileSet[i, j].WallType > 0)
				{
					return false;
				}
			}
			if (Main.TileSet[i, y + 1].IsActive == 0 || (Main.TileSet[i, y + 1].Type != 2 && Main.TileSet[i, y + 1].Type != 109))
			{
				return false;
			}
		}
		for (int k = 0; k < 2; k++)
		{
			for (int l = -3; l < 1; l++)
			{
				int num = k * 18 + genRand.Next(3) * 36;
				int num2 = (l + 3) * 18;
				Main.TileSet[x + k, y + l].IsActive = 1;
				Main.TileSet[x + k, y + l].FrameX = (short)num;
				Main.TileSet[x + k, y + l].FrameY = (short)num2;
				Main.TileSet[x + k, y + l].Type = 27;
			}
		}
		return true;
	}

	public static void CheckSunflower(int i, int j)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = 0;
		int num2 = j;
		num += Main.TileSet[i, j].FrameX / 18;
		num2 -= Main.TileSet[i, j].FrameY / 18;
		num &= 1;
		num = -num;
		num += i;
		for (int k = num; k < num + 2; k++)
		{
			int num3 = num2;
			while (true)
			{
				if (num3 < num2 + 4)
				{
					int num4 = (Main.TileSet[k, num3].FrameX / 18) & 1;
					if (Main.TileSet[k, num3].IsActive != 0 && Main.TileSet[k, num3].Type == 27 && num4 == k - num && Main.TileSet[k, num3].FrameY == (num3 - num2) * 18)
					{
						num3++;
						continue;
					}
				}
				else if (Main.TileSet[k, num2 + 4].IsActive != 0 && (Main.TileSet[k, num2 + 4].Type == 2 || Main.TileSet[k, num2 + 4].Type == 109))
				{
					break;
				}
				ToDestroyObject = true;
				for (int l = num; l < num + 2; l++)
				{
					for (int m = num2; m < num2 + 4; m++)
					{
						if (Main.TileSet[l, m].Type == 27 && Main.TileSet[l, m].IsActive != 0)
						{
							KillTile(l, m);
						}
					}
				}
				Item.NewItem(i * 16, j * 16, 32, 32, 63);
				ToDestroyObject = false;
				return;
			}
		}
	}

	public static bool PlacePot(int x, int y)
	{
		for (int i = x; i < x + 2; i++)
		{
			for (int j = y - 1; j < y + 1; j++)
			{
				if (Main.TileSet[i, j].IsActive != 0)
				{
					return false;
				}
			}
			if (Main.TileSet[i, y + 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[i, y + 1].Type])
			{
				return false;
			}
		}
		for (int k = 0; k < 2; k++)
		{
			for (int l = -1; l < 1; l++)
			{
				Main.TileSet[x + k, y + l].IsActive = 1;
				Main.TileSet[x + k, y + l].FrameX = (short)(k * 18 + genRand.Next(3) * 36);
				Main.TileSet[x + k, y + l].FrameY = (short)((l + 1) * 18);
				Main.TileSet[x + k, y + l].Type = 28;
			}
		}
		return true;
	}

	public static bool CheckCactus(int i, int j)
	{
		int num = j;
		int num2 = i;
		while (Main.TileSet[num2, num].IsActive != 0 && Main.TileSet[num2, num].Type == 80)
		{
			num++;
			if (Main.TileSet[num2, num].IsActive == 0 || Main.TileSet[num2, num].Type != 80)
			{
				if (Main.TileSet[num2 - 1, num].IsActive != 0 && Main.TileSet[num2 - 1, num].Type == 80 && Main.TileSet[num2 - 1, num - 1].IsActive != 0 && Main.TileSet[num2 - 1, num - 1].Type == 80 && num2 >= i)
				{
					num2--;
				}
				if (Main.TileSet[num2 + 1, num].IsActive != 0 && Main.TileSet[num2 + 1, num].Type == 80 && Main.TileSet[num2 + 1, num - 1].IsActive != 0 && Main.TileSet[num2 + 1, num - 1].Type == 80 && num2 <= i)
				{
					num2++;
				}
			}
		}
		if (Main.TileSet[num2, num].IsActive == 0 || (Main.TileSet[num2, num].Type != 53 && Main.TileSet[num2, num].Type != 112 && Main.TileSet[num2, num].Type != 116))
		{
			KillTile(i, j);
			return true;
		}
		if (i != num2)
		{
			if ((Main.TileSet[i, j + 1].IsActive == 0 || Main.TileSet[i, j + 1].Type != 80) && (Main.TileSet[i - 1, j].IsActive == 0 || Main.TileSet[i - 1, j].Type != 80) && (Main.TileSet[i + 1, j].IsActive == 0 || Main.TileSet[i + 1, j].Type != 80))
			{
				KillTile(i, j);
				return true;
			}
		}
		else if (i == num2 && (Main.TileSet[i, j + 1].IsActive == 0 || (Main.TileSet[i, j + 1].Type != 80 && Main.TileSet[i, j + 1].Type != 53 && Main.TileSet[i, j + 1].Type != 112 && Main.TileSet[i, j + 1].Type != 116)))
		{
			KillTile(i, j);
			return true;
		}
		return false;
	}

	public static void PlantCactus(int i, int j)
	{
		GrowCactus(i, j);
		for (int k = 0; k < 150; k++)
		{
			int i2 = genRand.Next(i - 1, i + 2);
			int j2 = genRand.Next(j - 10, j + 2);
			GrowCactus(i2, j2);
		}
	}

	public static void CheckOrb(int i, int j, int type)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = i;
		int num2 = j;
		num = ((Main.TileSet[i, j].FrameX != 0) ? (i - 1) : i);
		num2 = ((Main.TileSet[i, j].FrameY != 0) ? (j - 1) : j);
		if (Main.TileSet[num, num2].IsActive != 0 && Main.TileSet[num, num2].Type == type && Main.TileSet[num + 1, num2].IsActive != 0 && Main.TileSet[num + 1, num2].Type == type && Main.TileSet[num, num2 + 1].IsActive != 0 && Main.TileSet[num, num2 + 1].Type == type && Main.TileSet[num + 1, num2 + 1].IsActive != 0 && Main.TileSet[num + 1, num2 + 1].Type == type)
		{
			return;
		}
		ToDestroyObject = true;
		if (Main.TileSet[num, num2].Type == type)
		{
			KillTile(num, num2);
		}
		if (Main.TileSet[num + 1, num2].Type == type)
		{
			KillTile(num + 1, num2);
		}
		if (Main.TileSet[num, num2 + 1].Type == type)
		{
			KillTile(num, num2 + 1);
		}
		if (Main.TileSet[num + 1, num2 + 1].Type == type)
		{
			KillTile(num + 1, num2 + 1);
		}
		if (!Gen)
		{
			Main.PlaySound(13, i * 16, j * 16);
			if (Main.NetMode != 1)
			{
				switch ((EntityID.TileID)type)
				{
				case EntityID.TileID.LIFE_CRYSTAL:
					Item.NewItem(num * 16, num2 * 16, 32, 32, 29);
					break;
				case EntityID.TileID.SHADOW_ORB:
				{
					if (genRand.Next(2) == 0)
					{
						ToSpawnMeteor = true;
					}
					int num3 = Main.Rand.Next(5);
					if (!HasShadowOrbSmashed)
					{
						num3 = 0;
					}
					switch (num3)
					{
					case 0:
						Item.NewItem(num * 16, num2 * 16, 32, 32, 96, 1, DoNotBroadcast: false, -1);
						Item.NewItem(num * 16, num2 * 16, 32, 32, 97, genRand.Next(25, 51));
						break;
					case 1:
						Item.NewItem(num * 16, num2 * 16, 32, 32, 64, 1, DoNotBroadcast: false, -1);
						break;
					case 2:
						Item.NewItem(num * 16, num2 * 16, 32, 32, 162, 1, DoNotBroadcast: false, -1);
						break;
					case 3:
						Item.NewItem(num * 16, num2 * 16, 32, 32, 115, 1, DoNotBroadcast: false, -1);
						break;
					case 4:
						Item.NewItem(num * 16, num2 * 16, 32, 32, 111, 1, DoNotBroadcast: false, -1);
						break;
					}
					HasShadowOrbSmashed = true;
					shadowOrbCount++;
					if (shadowOrbCount >= 3)
					{
						shadowOrbCount = 0;
						Rectangle rect = default;
						rect.X = num << 4;
						rect.Y = num2 << 4;
						rect.Width = (rect.Height = 0);
						NPC.SpawnOnPlayer(Player.FindClosest(ref rect), 13);
					}
					else
					{
						int textId = 10;
						if (shadowOrbCount == 2)
						{
							textId = 11;
						}
						NetMessage.SendText(textId, 50, 255, 130, -1);
					}
					break;
				}
				}
			}
		}
		ToDestroyObject = false;
	}

	public static void CheckTree(int i, int j)
	{
		int num = -1;
		int num2 = -1;
		int num3 = -1;
		int num4 = -1;
		if (Main.TileSet[i - 1, j].IsActive != 0)
		{
			num2 = Main.TileSet[i - 1, j].Type;
		}
		if (Main.TileSet[i + 1, j].IsActive != 0)
		{
			num3 = Main.TileSet[i + 1, j].Type;
		}
		if (Main.TileSet[i, j - 1].IsActive != 0)
		{
			num = Main.TileSet[i, j - 1].Type;
		}
		if (Main.TileSet[i, j + 1].IsActive != 0)
		{
			num4 = Main.TileSet[i, j + 1].Type;
		}
		if (num2 >= 0 && Main.TileStone[num2])
		{
			num2 = 1;
		}
		if (num3 >= 0 && Main.TileStone[num3])
		{
			num3 = 1;
		}
		if (num >= 0 && Main.TileStone[num])
		{
			num = 1;
		}
		if (num4 >= 0 && Main.TileStone[num4])
		{
			num4 = 1;
		}
		switch ((EntityID.TileID)num4)
		{
		case EntityID.TileID.CORRUPT_GRASS:
			num4 = 2;
			break;
		case EntityID.TileID.JUNGLE_GRASS:
			num4 = 2;
			break;
		case EntityID.TileID.HALLOWED_GRASS:
			num4 = 2;
			break;
		case EntityID.TileID.SNOW:
			num4 = 2;
			break;
		}
		int frameNumber = Main.TileSet[i, j].frameNumber;
		int type = Main.TileSet[i, j].Type;
		int frameX;
		int num5 = (frameX = Main.TileSet[i, j].FrameX);
		int frameY;
		int num6 = (frameY = Main.TileSet[i, j].FrameY);
		if (frameX >= 22 && frameX <= 44 && frameY >= 132 && frameY <= 176)
		{
			if (num4 != 2)
			{
				KillTile(i, j);
			}
			else if ((frameX != 22 || num2 != type) && (frameX != 44 || num3 != type))
			{
				KillTile(i, j);
			}
		}
		else if ((frameX == 88 && frameY >= 0 && frameY <= 44) || (frameX == 66 && frameY >= 66 && frameY <= 130) || (frameX == 110 && frameY >= 66 && frameY <= 110) || (frameX == 132 && frameY >= 0 && frameY <= 176))
		{
			if (num2 == type && num3 == type)
			{
				Main.TileSet[i, j].FrameX = 110;
				Main.TileSet[i, j].FrameY = (short)(66 + 22 * frameNumber);
			}
			else if (num2 == type)
			{
				Main.TileSet[i, j].FrameX = 88;
				Main.TileSet[i, j].FrameY = (short)(22 * frameNumber);
			}
			else if (num3 == type)
			{
				Main.TileSet[i, j].FrameX = 66;
				Main.TileSet[i, j].FrameY = (short)(66 + 22 * frameNumber);
			}
			else
			{
				Main.TileSet[i, j].FrameX = 0;
				Main.TileSet[i, j].FrameY = (short)(22 * frameNumber);
			}
		}
		frameX = Main.TileSet[i, j].FrameX;
		frameY = Main.TileSet[i, j].FrameY;
		if (frameY >= 132 && frameY <= 176)
		{
			if (frameX == 0 || frameX == 66 || frameX == 88)
			{
				if (num4 != 2)
				{
					KillTile(i, j);
				}
				if (num2 != type && num3 != type)
				{
					Main.TileSet[i, j].FrameX = 0;
					Main.TileSet[i, j].FrameY = (short)(22 * frameNumber);
				}
				else if (num2 != type)
				{
					Main.TileSet[i, j].FrameX = 0;
					Main.TileSet[i, j].FrameY = (short)(132 + 22 * frameNumber);
				}
				else if (num3 != type)
				{
					Main.TileSet[i, j].FrameX = 66;
					Main.TileSet[i, j].FrameY = (short)(132 + 22 * frameNumber);
				}
				else
				{
					Main.TileSet[i, j].FrameX = 88;
					Main.TileSet[i, j].FrameY = (short)(132 + 22 * frameNumber);
				}
			}
		}
		else if ((frameX == 66 && (frameY == 0 || frameY == 22 || frameY == 44)) || (frameX == 44 && (frameY == 198 || frameY == 220 || frameY == 242)))
		{
			if (num3 != type)
			{
				KillTile(i, j);
			}
		}
		else if ((frameX == 88 && (frameY == 66 || frameY == 88 || frameY == 110)) || (frameX == 66 && (frameY == 198 || frameY == 220 || frameY == 242)))
		{
			if (num2 != type)
			{
				KillTile(i, j);
			}
		}
		else if (num4 == -1 || num4 == 23)
		{
			KillTile(i, j);
		}
		else if (num != type && frameY < 198 && ((frameX != 22 && frameX != 44) || frameY < 132))
		{
			if (num2 == type || num3 == type)
			{
				if (num4 == type)
				{
					if (num2 == type && num3 == type)
					{
						Main.TileSet[i, j].FrameX = 132;
						Main.TileSet[i, j].FrameY = (short)(132 + 22 * frameNumber);
					}
					else if (num2 == type)
					{
						Main.TileSet[i, j].FrameX = 132;
						Main.TileSet[i, j].FrameY = (short)(22 * frameNumber);
					}
					else if (num3 == type)
					{
						Main.TileSet[i, j].FrameX = 132;
						Main.TileSet[i, j].FrameY = (short)(66 + 22 * frameNumber);
					}
				}
				else if (num2 == type && num3 == type)
				{
					Main.TileSet[i, j].FrameX = 154;
					Main.TileSet[i, j].FrameY = (short)(132 + 22 * frameNumber);
				}
				else if (num2 == type)
				{
					Main.TileSet[i, j].FrameX = 154;
					Main.TileSet[i, j].FrameY = (short)(22 * frameNumber);
				}
				else if (num3 == type)
				{
					Main.TileSet[i, j].FrameX = 154;
					Main.TileSet[i, j].FrameY = (short)(66 + 22 * frameNumber);
				}
			}
			else
			{
				Main.TileSet[i, j].FrameX = 110;
				Main.TileSet[i, j].FrameY = (short)(22 * frameNumber);
			}
		}
		if (num5 >= 0 && num6 >= 0 && Main.TileSet[i, j].FrameX != num5 && Main.TileSet[i, j].FrameY != num6)
		{
			TileFrame(i - 1, j);
			TileFrame(i + 1, j);
			TileFrame(i, j - 1);
			TileFrame(i, j + 1);
		}
	}

	public static void CactusFrame(int i, int j)
	{
		try
		{
			int num = j;
			int num2 = i;
			if (CheckCactus(i, j))
			{
				return;
			}
			while (Main.TileSet[num2, num].IsActive != 0 && Main.TileSet[num2, num].Type == 80)
			{
				num++;
				if (Main.TileSet[num2, num].IsActive == 0 || Main.TileSet[num2, num].Type != 80)
				{
					if (Main.TileSet[num2 - 1, num].IsActive != 0 && Main.TileSet[num2 - 1, num].Type == 80 && Main.TileSet[num2 - 1, num - 1].IsActive != 0 && Main.TileSet[num2 - 1, num - 1].Type == 80 && num2 >= i)
					{
						num2--;
					}
					if (Main.TileSet[num2 + 1, num].IsActive != 0 && Main.TileSet[num2 + 1, num].Type == 80 && Main.TileSet[num2 + 1, num - 1].IsActive != 0 && Main.TileSet[num2 + 1, num - 1].Type == 80 && num2 <= i)
					{
						num2++;
					}
				}
			}
			num--;
			int num3 = i - num2;
			num2 = i;
			num = j;
			int type = Main.TileSet[i - 2, j].Type;
			int num4 = Main.TileSet[i - 1, j].Type;
			int num5 = Main.TileSet[i + 1, j].Type;
			int num6 = Main.TileSet[i, j - 1].Type;
			int num7 = Main.TileSet[i, j + 1].Type;
			int num8 = Main.TileSet[i - 1, j + 1].Type;
			int num9 = Main.TileSet[i + 1, j + 1].Type;
			if (Main.TileSet[i - 1, j].IsActive == 0)
			{
				num4 = -1;
			}
			if (Main.TileSet[i + 1, j].IsActive == 0)
			{
				num5 = -1;
			}
			if (Main.TileSet[i, j - 1].IsActive == 0)
			{
				num6 = -1;
			}
			if (Main.TileSet[i, j + 1].IsActive == 0)
			{
				num7 = -1;
			}
			if (Main.TileSet[i - 1, j + 1].IsActive == 0)
			{
				num8 = -1;
			}
			if (Main.TileSet[i + 1, j + 1].IsActive == 0)
			{
				num9 = -1;
			}
			short num10 = Main.TileSet[i, j].FrameX;
			short num11 = Main.TileSet[i, j].FrameY;
			switch (num3)
			{
			case 0:
				if (num6 != 80)
				{
					if (num4 == 80 && num5 == 80 && num8 != 80 && num9 != 80 && type != 80)
					{
						num10 = 90;
						num11 = 0;
					}
					else if (num4 == 80 && num8 != 80 && type != 80)
					{
						num10 = 72;
						num11 = 0;
					}
					else if (num5 == 80 && num9 != 80)
					{
						num10 = 18;
						num11 = 0;
					}
					else
					{
						num10 = 0;
						num11 = 0;
					}
				}
				else if (num4 == 80 && num5 == 80 && num8 != 80 && num9 != 80 && type != 80)
				{
					num10 = 90;
					num11 = 36;
				}
				else if (num4 == 80 && num8 != 80 && type != 80)
				{
					num10 = 72;
					num11 = 36;
				}
				else if (num5 == 80 && num9 != 80)
				{
					num10 = 18;
					num11 = 36;
				}
				else if (num7 >= 0 && Main.TileSolid[num7])
				{
					num10 = 0;
					num11 = 36;
				}
				else
				{
					num10 = 0;
					num11 = 18;
				}
				break;
			case -1:
				if (num5 == 80)
				{
					if (num6 != 80 && num7 != 80)
					{
						num10 = 108;
						num11 = 36;
					}
					else if (num7 != 80)
					{
						num10 = 54;
						num11 = 36;
					}
					else if (num6 != 80)
					{
						num10 = 54;
						num11 = 0;
					}
					else
					{
						num10 = 54;
						num11 = 18;
					}
				}
				else if (num6 != 80)
				{
					num10 = 54;
					num11 = 0;
				}
				else
				{
					num10 = 54;
					num11 = 18;
				}
				break;
			case 1:
				if (num4 == 80)
				{
					if (num6 != 80 && num7 != 80)
					{
						num10 = 108;
						num11 = 16;
					}
					else if (num7 != 80)
					{
						num10 = 36;
						num11 = 36;
					}
					else if (num6 != 80)
					{
						num10 = 36;
						num11 = 0;
					}
					else
					{
						num10 = 36;
						num11 = 18;
					}
				}
				else if (num6 != 80)
				{
					num10 = 36;
					num11 = 0;
				}
				else
				{
					num10 = 36;
					num11 = 18;
				}
				break;
			}
			if (num10 != Main.TileSet[i, j].FrameX || num11 != Main.TileSet[i, j].FrameY)
			{
				Main.TileSet[i, j].FrameX = num10;
				Main.TileSet[i, j].FrameY = num11;
				SquareTileFrame(i, j);
			}
		}
		catch
		{
			Main.TileSet[i, j].FrameX = 0;
			Main.TileSet[i, j].FrameY = 0;
		}
	}

	public static void GrowCactus(int i, int j)
	{
		int num = j;
		int num2 = i;
		if (Main.TileSet[i, j].IsActive == 0 || Main.TileSet[i, j - 1].Liquid > 0 || (Main.TileSet[i, j].Type != 53 && Main.TileSet[i, j].Type != 80 && Main.TileSet[i, j].Type != 112 && Main.TileSet[i, j].Type != 116))
		{
			return;
		}
		if (Main.TileSet[i, j].Type == 53 || Main.TileSet[i, j].Type == 112 || Main.TileSet[i, j].Type == 116)
		{
			if (Main.TileSet[i, j - 1].IsActive != 0 || Main.TileSet[i - 1, j - 1].IsActive != 0 || Main.TileSet[i + 1, j - 1].IsActive != 0)
			{
				return;
			}
			int num3 = 0;
			int num4 = 0;
			for (int k = i - 6; k <= i + 6; k++)
			{
				for (int l = j - 3; l <= j + 1; l++)
				{
					try
					{
						if (Main.TileSet[k, l].IsActive == 0)
						{
							continue;
						}
						if (Main.TileSet[k, l].Type == 80)
						{
							num3++;
							if (num3 >= 4)
							{
								return;
							}
						}
						if (Main.TileSet[k, l].Type == 53 || Main.TileSet[k, l].Type == 112 || Main.TileSet[k, l].Type == 116)
						{
							num4++;
						}
					}
					catch
					{
					}
				}
			}
			if (num4 > 10)
			{
				Main.TileSet[i, j - 1].IsActive = 1;
				Main.TileSet[i, j - 1].Type = 80;
				SquareTileFrame(num2, num - 1);
				if (Main.NetMode == 2)
				{
					NetMessage.SendTile(i, j - 1);
				}
			}
		}
		else
		{
			if (Main.TileSet[i, j].Type != 80)
			{
				return;
			}
			while (Main.TileSet[num2, num].IsActive != 0 && Main.TileSet[num2, num].Type == 80)
			{
				num++;
				if (Main.TileSet[num2, num].IsActive == 0 || Main.TileSet[num2, num].Type != 80)
				{
					if (Main.TileSet[num2 - 1, num].IsActive != 0 && Main.TileSet[num2 - 1, num].Type == 80 && Main.TileSet[num2 - 1, num - 1].IsActive != 0 && Main.TileSet[num2 - 1, num - 1].Type == 80 && num2 >= i)
					{
						num2--;
					}
					if (Main.TileSet[num2 + 1, num].IsActive != 0 && Main.TileSet[num2 + 1, num].Type == 80 && Main.TileSet[num2 + 1, num - 1].IsActive != 0 && Main.TileSet[num2 + 1, num - 1].Type == 80 && num2 <= i)
					{
						num2++;
					}
				}
			}
			num--;
			int num5 = num - j;
			int num6 = i - num2;
			num2 = i - num6;
			num = j;
			int num7 = 11 - num5;
			int num8 = 0;
			for (int m = num2 - 2; m <= num2 + 2; m++)
			{
				for (int n = num - num7; n <= num + num5; n++)
				{
					if (Main.TileSet[m, n].IsActive != 0 && Main.TileSet[m, n].Type == 80)
					{
						num8++;
					}
				}
			}
			if (num8 >= genRand.Next(11, 13))
			{
				return;
			}
			num2 = i;
			num = j;
			if (num6 == 0)
			{
				if (num5 == 0)
				{
					if (Main.TileSet[num2, num - 1].IsActive == 0)
					{
						Main.TileSet[num2, num - 1].IsActive = 1;
						Main.TileSet[num2, num - 1].Type = 80;
						SquareTileFrame(num2, num - 1);
						if (Main.NetMode == 2)
						{
							NetMessage.SendTile(num2, num - 1);
						}
					}
					return;
				}
				bool flag = false;
				bool flag2 = false;
				if (Main.TileSet[num2, num - 1].IsActive != 0 && Main.TileSet[num2, num - 1].Type == 80)
				{
					if (Main.TileSet[num2 - 1, num].IsActive == 0 && Main.TileSet[num2 - 2, num + 1].IsActive == 0 && Main.TileSet[num2 - 1, num - 1].IsActive == 0 && Main.TileSet[num2 - 1, num + 1].IsActive == 0 && Main.TileSet[num2 - 2, num].IsActive == 0)
					{
						flag = true;
					}
					if (Main.TileSet[num2 + 1, num].IsActive == 0 && Main.TileSet[num2 + 2, num + 1].IsActive == 0 && Main.TileSet[num2 + 1, num - 1].IsActive == 0 && Main.TileSet[num2 + 1, num + 1].IsActive == 0 && Main.TileSet[num2 + 2, num].IsActive == 0)
					{
						flag2 = true;
					}
				}
				int num9 = genRand.Next(3);
				if ((num9 == 0) & flag)
				{
					Main.TileSet[num2 - 1, num].IsActive = 1;
					Main.TileSet[num2 - 1, num].Type = 80;
					SquareTileFrame(num2 - 1, num);
					if (Main.NetMode == 2)
					{
						NetMessage.SendTile(num2 - 1, num);
					}
				}
				else if ((num9 == 1) & flag2)
				{
					Main.TileSet[num2 + 1, num].IsActive = 1;
					Main.TileSet[num2 + 1, num].Type = 80;
					SquareTileFrame(num2 + 1, num);
					if (Main.NetMode == 2)
					{
						NetMessage.SendTile(num2 + 1, num);
					}
				}
				else if (num5 < genRand.Next(2, 8) && (Main.TileSet[num2 + 1, num - 1].IsActive == 0 || Main.TileSet[num2 + 1, num - 1].Type != 80) && Main.TileSet[num2, num - 1].IsActive == 0)
				{
					Main.TileSet[num2, num - 1].IsActive = 1;
					Main.TileSet[num2, num - 1].Type = 80;
					SquareTileFrame(num2, num - 1);
					if (Main.NetMode == 2)
					{
						NetMessage.SendTile(num2, num - 1);
					}
				}
			}
			else if (Main.TileSet[num2, num - 1].IsActive == 0 && Main.TileSet[num2, num - 2].IsActive == 0 && Main.TileSet[num2 + num6, num - 1].IsActive == 0 && Main.TileSet[num2 - num6, num - 1].IsActive != 0 && Main.TileSet[num2 - num6, num - 1].Type == 80)
			{
				Main.TileSet[num2, num - 1].IsActive = 1;
				Main.TileSet[num2, num - 1].Type = 80;
				SquareTileFrame(num2, num - 1);
				if (Main.NetMode == 2)
				{
					NetMessage.SendTile(num2, num - 1);
				}
			}
		}
	}

	public static void CheckPot(int i, int j)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = 0;
		int num2 = j;
		num += Main.TileSet[i, j].FrameX / 18;
		num2 -= Main.TileSet[i, j].FrameY / 18;
		num &= 1;
		num = -num;
		num += i;
		for (int k = num; k < num + 2; k++)
		{
			int num3 = num2;
			while (true)
			{
				if (num3 < num2 + 2)
				{
					if (((Main.TileSet[k, num3].FrameX / 18) & 1) == k - num && Main.TileSet[k, num3].IsActive != 0 && Main.TileSet[k, num3].Type == 28 && Main.TileSet[k, num3].FrameY == (num3 - num2) * 18)
					{
						num3++;
						continue;
					}
				}
				else if (Main.TileSet[k, num2 + 2].IsActive != 0 && Main.TileSolid[Main.TileSet[k, num2 + 2].Type])
				{
					break;
				}
				ToDestroyObject = true;
				for (int l = num; l < num + 2; l++)
				{
					for (int m = num2; m < num2 + 2; m++)
					{
						if (Main.TileSet[l, m].Type == 28 && Main.TileSet[l, m].IsActive != 0)
						{
							KillTile(l, m);
						}
					}
				}
				if (!Gen)
				{
					Rectangle rect = default;
					rect.X = i << 4;
					rect.Y = j << 4;
					rect.Width = (rect.Height = 16);
					Main.PlaySound(13, rect.X, rect.Y);
					Gore.NewGore(new Vector2(rect.X, rect.Y), default, 51);
					Gore.NewGore(new Vector2(rect.X, rect.Y), default, 52);
					Gore.NewGore(new Vector2(rect.X, rect.Y), default, 53);
					if (genRand.Next(40) == 0 && (Main.TileSet[num, num2].WallType == 7 || Main.TileSet[num, num2].WallType == 8 || Main.TileSet[num, num2].WallType == 9))
					{
						Item.NewItem(rect.X, rect.Y, 16, 16, 327);
					}
					else if (genRand.Next(45) == 0)
					{
						if (j < Main.WorldSurface)
						{
							int num4 = genRand.Next(4);
							if (num4 == 0)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 292);
							}
							if (num4 == 1)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 298);
							}
							if (num4 == 2)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 299);
							}
							if (num4 == 3)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 290);
							}
						}
						else if (j < Main.RockLayer)
						{
							int num5 = genRand.Next(7);
							if (num5 == 0)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 289);
							}
							if (num5 == 1)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 298);
							}
							if (num5 == 2)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 299);
							}
							if (num5 == 3)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 290);
							}
							if (num5 == 4)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 303);
							}
							if (num5 == 5)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 291);
							}
							if (num5 == 6)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 304);
							}
						}
						else if (j < Main.MaxTilesY - 200)
						{
							int num6 = genRand.Next(10);
							if (num6 == 0)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 296);
							}
							if (num6 == 1)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 295);
							}
							if (num6 == 2)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 299);
							}
							if (num6 == 3)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 302);
							}
							if (num6 == 4)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 303);
							}
							if (num6 == 5)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 305);
							}
							if (num6 == 6)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 301);
							}
							if (num6 == 7)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 302);
							}
							if (num6 == 8)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 297);
							}
							if (num6 == 9)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 304);
							}
						}
						else
						{
							int num7 = genRand.Next(12);
							if (num7 == 0)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 296);
							}
							if (num7 == 1)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 295);
							}
							if (num7 == 2)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 293);
							}
							if (num7 == 3)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 288);
							}
							if (num7 == 4)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 294);
							}
							if (num7 == 5)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 297);
							}
							if (num7 == 6)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 304);
							}
							if (num7 == 7)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 305);
							}
							if (num7 == 8)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 301);
							}
							if (num7 == 9)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 302);
							}
							if (num7 == 10)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 288);
							}
							if (num7 == 11)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 300);
							}
						}
					}
					else
					{
						int num8 = Main.Rand.Next(8);
						if (num8 == 0)
						{
							Player player = Player.FindClosest(ref rect);
							if (player.statLife < player.StatLifeMax)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 58);
							}
						}
						else if (num8 == 1)
						{
							Player player2 = Player.FindClosest(ref rect);
							if (player2.statMana < player2.statManaMax)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 184);
							}
						}
						else if (num8 == 2)
						{
							int stack = Main.Rand.Next(1, 6);
							if (Main.TileSet[i, j].Liquid > 0)
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 282, stack);
							}
							else
							{
								Item.NewItem(rect.X, rect.Y, 16, 16, 8, stack);
							}
						}
						else if (num8 == 3)
						{
							int type = 40;
							if (j < Main.RockLayer && genRand.Next(2) == 0)
							{
								type = ((!Main.InHardMode) ? 42 : 168);
							}
							if (j > Main.MaxTilesY - 200)
							{
								type = 265;
							}
							else if (Main.InHardMode)
							{
								type = ((Main.Rand.Next(2) != 0) ? 47 : 278);
							}
							Item.NewItem(rect.X, rect.Y, 16, 16, type, Main.Rand.Next(8) + 3);
						}
						else if (num8 == 4)
						{
							int type2 = 28;
							if (j > Main.MaxTilesY - 200 || Main.InHardMode)
							{
								type2 = 188;
							}
							Item.NewItem(rect.X, rect.Y, 16, 16, type2);
						}
						else if (num8 == 5 && j > Main.RockLayer)
						{
							Item.NewItem(rect.X, rect.Y, 16, 16, 166, Main.Rand.Next(4) + 1);
						}
						else
						{
							float num9 = 200 + genRand.Next(-100, 101);
							if (j < Main.WorldSurface)
							{
								num9 *= 0.5f;
							}
							else if (j < Main.RockLayer)
							{
								num9 *= 0.75f;
							}
							else if (j > Main.MaxTilesY - 250)
							{
								num9 *= 1.25f;
							}
							num9 *= 1f + (float)Main.Rand.Next(-20, 21) * 0.01f;
							if (Main.Rand.Next(5) == 0)
							{
								num9 *= 1f + (float)Main.Rand.Next(5, 11) * 0.01f;
							}
							if (Main.Rand.Next(10) == 0)
							{
								num9 *= 1f + (float)Main.Rand.Next(10, 21) * 0.01f;
							}
							if (Main.Rand.Next(15) == 0)
							{
								num9 *= 1f + (float)Main.Rand.Next(20, 41) * 0.01f;
							}
							if (Main.Rand.Next(20) == 0)
							{
								num9 *= 1f + (float)Main.Rand.Next(40, 81) * 0.01f;
							}
							if (Main.Rand.Next(25) == 0)
							{
								num9 *= 1f + (float)Main.Rand.Next(50, 101) * 0.01f;
							}
							while ((int)num9 > 0)
							{
								if (num9 > 1000000f)
								{
									int num10 = (int)(num9 / 1000000f);
									if (num10 > 50 && Main.Rand.Next(2) == 0)
									{
										num10 /= Main.Rand.Next(3) + 1;
									}
									if (Main.Rand.Next(2) == 0)
									{
										num10 /= Main.Rand.Next(3) + 1;
									}
									if (num10 > 0)
									{
										num9 -= (float)(1000000 * num10);
										Item.NewItem(rect.X, rect.Y, 16, 16, 74, num10);
									}
									continue;
								}
								if (num9 > 10000f)
								{
									int num11 = (int)(num9 / 10000f);
									if (num11 > 50 && Main.Rand.Next(2) == 0)
									{
										num11 /= Main.Rand.Next(3) + 1;
									}
									if (Main.Rand.Next(2) == 0)
									{
										num11 /= Main.Rand.Next(3) + 1;
									}
									if (num11 > 0)
									{
										num9 -= (float)(10000 * num11);
										Item.NewItem(rect.X, rect.Y, 16, 16, 73, num11);
									}
									continue;
								}
								if (num9 > 100f)
								{
									int num12 = (int)(num9 / 100f);
									if (num12 > 50 && Main.Rand.Next(2) == 0)
									{
										num12 /= Main.Rand.Next(3) + 1;
									}
									if (Main.Rand.Next(2) == 0)
									{
										num12 /= Main.Rand.Next(3) + 1;
									}
									if (num12 > 0)
									{
										num9 -= (float)(100 * num12);
										Item.NewItem(rect.X, rect.Y, 16, 16, 72, num12);
									}
									continue;
								}
								int num13 = (int)num9;
								if (num13 > 50 && Main.Rand.Next(2) == 0)
								{
									num13 /= Main.Rand.Next(3) + 1;
								}
								if (Main.Rand.Next(2) == 0)
								{
									num13 /= Main.Rand.Next(4) + 1;
								}
								if (num13 < 1)
								{
									num13 = 1;
								}
								num9 -= (float)num13;
								Item.NewItem(rect.X, rect.Y, 16, 16, 71, num13);
							}
						}
					}
				}
				ToDestroyObject = false;
				return;
			}
		}
	}

	public static int PlaceChest(int x, int y, bool notNearOtherChests = false, int style = 0)
	{
		for (int i = x; i < x + 2; i++)
		{
			for (int j = y - 1; j < y + 1; j++)
			{
				if (Main.TileSet[i, j].IsActive != 0)
				{
					return -1;
				}
				if (Main.TileSet[i, j].Lava != 0)
				{
					return -1;
				}
			}
			if (Main.TileSet[i, y + 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[i, y + 1].Type])
			{
				return -1;
			}
		}
		if (notNearOtherChests)
		{
			for (int k = x - 25; k < x + 25; k++)
			{
				for (int l = y - 8; l < y + 8; l++)
				{
					try
					{
						if (Main.TileSet[k, l].IsActive != 0 && Main.TileSet[k, l].Type == 21)
						{
							return -1;
						}
					}
					catch
					{
					}
				}
			}
		}
		int num = Chest.CreateChest(x, y - 1);
		if (num != -1)
		{
			Main.TileSet[x, y - 1].IsActive = 1;
			Main.TileSet[x, y - 1].FrameY = 0;
			Main.TileSet[x, y - 1].FrameX = (short)(36 * style);
			Main.TileSet[x, y - 1].Type = 21;
			Main.TileSet[x + 1, y - 1].IsActive = 1;
			Main.TileSet[x + 1, y - 1].FrameY = 0;
			Main.TileSet[x + 1, y - 1].FrameX = (short)(18 + 36 * style);
			Main.TileSet[x + 1, y - 1].Type = 21;
			Main.TileSet[x, y].IsActive = 1;
			Main.TileSet[x, y].FrameY = 18;
			Main.TileSet[x, y].FrameX = (short)(36 * style);
			Main.TileSet[x, y].Type = 21;
			Main.TileSet[x + 1, y].IsActive = 1;
			Main.TileSet[x + 1, y].FrameY = 18;
			Main.TileSet[x + 1, y].FrameX = (short)(18 + 36 * style);
			Main.TileSet[x + 1, y].Type = 21;
		}
		return num;
	}

	public static void CheckChest(int i, int j)
	{
		if (ToDestroyObject)
		{
			return;
		}
		int num = 0;
		int num2 = j;
		num += Main.TileSet[i, j].FrameX / 18;
		num2 -= Main.TileSet[i, j].FrameY / 18;
		num &= 1;
		num = -num;
		num += i;
		for (int k = num; k < num + 2; k++)
		{
			int num3 = num2;
			while (true)
			{
				if (num3 < num2 + 2)
				{
					int num4 = (Main.TileSet[k, num3].FrameX / 18) & 1;
					if (Main.TileSet[k, num3].IsActive != 0 && Main.TileSet[k, num3].Type == 21 && num4 == k - num && Main.TileSet[k, num3].FrameY == (num3 - num2) * 18)
					{
						num3++;
						continue;
					}
				}
				else if (Main.TileSet[k, num2 + 2].IsActive != 0 && Main.TileSolid[Main.TileSet[k, num2 + 2].Type])
				{
					break;
				}
				int type = 48;
				if (Main.TileSet[i, j].FrameX >= 216)
				{
					type = 348;
				}
				else if (Main.TileSet[i, j].FrameX >= 180)
				{
					type = 343;
				}
				else if (Main.TileSet[i, j].FrameX >= 108)
				{
					type = 328;
				}
				else if (Main.TileSet[i, j].FrameX >= 36)
				{
					type = 306;
				}
				ToDestroyObject = true;
				for (int l = num; l < num + 2; l++)
				{
					for (int m = num2; m < num2 + 3; m++)
					{
						if (Main.TileSet[l, m].Type == 21 && Main.TileSet[l, m].IsActive != 0)
						{
							Chest.DestroyChest(l, m);
							KillTile(l, m);
						}
					}
				}
				if (!Gen)
				{
					Item.NewItem(i * 16, j * 16, 32, 32, type);
				}
				ToDestroyObject = false;
				return;
			}
		}
	}

	public static bool PlaceWire(int i, int j)
	{
		if (Main.TileSet[i, j].wire == 0)
		{
			Main.TileSet[i, j].wire = 16;
			Main.PlaySound(0, i << 4, j << 4);
			return true;
		}
		return false;
	}

	public unsafe static bool KillWire(int i, int j)
	{
		if (Main.TileSet[i, j].wire != 0)
		{
			Main.TileSet[i, j].wire = 0;
			i <<= 4;
			j <<= 4;
			Main.PlaySound(0, i, j);
			if (Main.NetMode != 1)
			{
				Item.NewItem(i, j, 16, 16, 530);
			}
			for (int k = 0; k < 3; k++)
			{
				if (null == Main.DustSet.NewDust(i, j, 16, 16, 50))
				{
					break;
				}
			}
			return true;
		}
		return false;
	}

	public static bool CanPlaceTile(int i, ref int j, int type, int style = 0)
	{
		if (i >= 0 && j >= 0 && i < Main.MaxTilesX && j < Main.MaxTilesY)
		{
			if (style >= 0 && Main.TileSet[i, j].IsActive == 0 && Main.TileSolid[type] && Collision.AnyPlayerOrNPC(i, j, 1))
			{
				return false;
			}
			Tile tile = Main.TileSet[i - 1, j];
			Tile tile2 = Main.TileSet[i + 1, j];
			Tile tile3 = Main.TileSet[i, j + 1];
			switch ((EntityID.TileID)type)
			{
			case EntityID.TileID.SHORT_GRASS_PLANTS:
			case EntityID.TileID.SHORT_CORRUPT_PLANTS:
			case EntityID.TileID.SUNFLOWER:
			case EntityID.TileID.CORRUPTION_THORN:
			case EntityID.TileID.COBWEB:
			case EntityID.TileID.JUNGLE_THORN:
			case EntityID.TileID.GIANT_GLOWING_MUSHROOM:
				return Main.TileSet[i, j].Liquid == 0;
			case EntityID.TileID.TORCH:
			{
				bool flag = tile.IsActive != 0 && (Main.TileSolid[tile.Type] || tile.Type == 124 || (tile.Type == 5 && Main.TileSet[i - 1, j - 1].Type == 5 && Main.TileSet[i - 1, j + 1].Type == 5));
				bool flag2 = tile2.IsActive != 0 && (Main.TileSolid[tile2.Type] || tile2.Type == 124 || (tile2.Type == 5 && Main.TileSet[i + 1, j - 1].Type == 5 && Main.TileSet[i + 1, j + 1].Type == 5));
				if ((flag | flag2) || (tile3.IsActive != 0 && Main.TileSolid[tile3.Type]))
				{
					if (style != 8)
					{
						return Main.TileSet[i, j].Liquid == 0;
					}
					return true;
				}
				return false;
			}
			case EntityID.TileID.DOOR_CLOSED:
				if (Main.TileSet[i, j - 1].IsActive == 0 && Main.TileSet[i, j - 2].IsActive == 0 && Main.TileSet[i, j - 3].IsActive != 0 && Main.TileSolid[Main.TileSet[i, j - 3].Type])
				{
					j--;
					return true;
				}
				if (Main.TileSet[i, j + 1].IsActive == 0 && Main.TileSet[i, j + 2].IsActive == 0 && Main.TileSet[i, j + 3].IsActive != 0 && Main.TileSolid[Main.TileSet[i, j + 3].Type])
				{
					j++;
					return true;
				}
				return false;
			case EntityID.TileID.SAPLING:
				if (tile3.IsActive != 0 && (tile3.Type == 2 || tile3.Type == 109 || tile3.Type == 147))
				{
					return Main.TileSet[i, j].Liquid == 0;
				}
				return false;
			case EntityID.TileID.GRASS:
			case EntityID.TileID.CORRUPT_GRASS:
			case EntityID.TileID.HALLOWED_GRASS:
				if (Main.TileSet[i, j].Type == 0)
				{
					return Main.TileSet[i, j].IsActive != 0;
				}
				return false;
			case EntityID.TileID.JUNGLE_GRASS:
			case EntityID.TileID.MUSHROOM_GRASS:
				if (Main.TileSet[i, j].Type == 59)
				{
					return Main.TileSet[i, j].IsActive != 0;
				}
				return false;
			case EntityID.TileID.SHORT_JUNGLE_PLANTS:
			case EntityID.TileID.GLOWING_MUSHROOM:
				if (j + 1 < Main.MaxTilesY && tile3.IsActive != 0)
				{
					return tile3.Type == type - 1;
				}
				return false;
			case EntityID.TileID.CORAL:
				if (tile.IsActive != 0 || tile2.IsActive != 0 || Main.TileSet[i, j - 1].IsActive != 0)
				{
					return false;
				}
				if (tile3.IsActive == 0 || !Main.TileSolid[tile3.Type])
				{
					return false;
				}
				break;
			case EntityID.TileID.SWITCH:
			{
				bool flag3 = tile.IsActive == 0 || (!Main.TileSolid[tile.Type] && tile.Type != 124 && (tile.Type != 5 || Main.TileSet[i - 1, j - 1].Type != 5 || Main.TileSet[i - 1, j + 1].Type != 5));
				bool flag4 = tile2.IsActive == 0 || (!Main.TileSolid[tile2.Type] && tile2.Type != 124 && (tile2.Type != 5 || Main.TileSet[i + 1, j - 1].Type != 5 || Main.TileSet[i + 1, j + 1].Type != 5));
				if (flag3 & flag4)
				{
					if (tile3.IsActive != 0)
					{
						return Main.TileSolid[tile3.Type];
					}
					return false;
				}
				return true;
			}
			case EntityID.TileID.CRYSTAL_SHARD:
			case EntityID.TileID.XMAS_LIGHT:
				if (!SolidTileUnsafe(i - 1, j) && !SolidTileUnsafe(i + 1, j) && !SolidTileUnsafe(i, j - 1))
				{
					return SolidTileUnsafe(i, j + 1);
				}
				return true;
			}
			return true;
		}
		return false;
	}

	public unsafe static bool PlaceTile(int i, int j, int type, bool ToMute = false, bool IsForced = false, int plr = -1, int style = 0)
	{
		bool flag = false;
		try
		{
			fixed (Tile* ptr = &Main.TileSet[i, j])
			{
				if (CanPlaceTile(i, ref j, type, style) | IsForced)
				{
					ptr->FrameX = 0;
					ptr->FrameY = 0;
					switch ((EntityID.TileID)type)
					{
					case EntityID.TileID.SHORT_GRASS_PLANTS:
					case EntityID.TileID.SHORT_CORRUPT_PLANTS:
					case EntityID.TileID.SHORT_HALLOWED_PLANTS:
						if (j + 1 >= Main.MaxTilesY || ptr[1].IsActive == 0 || ((type != 3 || ptr[1].Type != 2) && (type != 24 || ptr[1].Type != 23) && (type != 3 || ptr[1].Type != 78) && (type != 110 || ptr[1].Type != 109)))
						{
							break;
						}
						if (type == 24 && genRand.Next(13) == 0)
						{
							type = 32;
							flag = true;
						}
						else if (ptr[1].Type == 78)
						{
							ptr->FrameX = (short)(genRand.Next(2) * 18 + 108);
							flag = true;
						}
						else if (ptr->WallType == 0 && ptr[1].WallType == 0)
						{
							if (genRand.Next(50) == 0 || (type == 24 && genRand.Next(40) == 0))
							{
								ptr->FrameX = 144;
							}
							else if (genRand.Next(35) == 0)
							{
								ptr->FrameX = (short)(genRand.Next(2) * 18 + 108);
							}
							else
							{
								ptr->FrameX = (short)(genRand.Next(6) * 18);
							}
							flag = true;
						}
						if (flag)
						{
							ptr->IsActive = 1;
							ptr->Type = (byte)type;
						}
						break;
					case EntityID.TileID.SHORT_JUNGLE_PLANTS:
						ptr->IsActive = 1;
						if (genRand.Next(16) == 0 && j > Main.WorldSurface)
						{
							ptr->Type = 69;
						}
						else
						{
							ptr->Type = (byte)type;
							if (j > Main.RockLayer && genRand.Next(60) == 0)
							{
								ptr->FrameX = 144;
							}
							else if (j > Main.RockLayer && genRand.Next(1000) == 0)
							{
								ptr->FrameX = 162;
							}
							else if (genRand.Next(15) == 0)
							{
								ptr->FrameX = (short)(genRand.Next(2) * 18 + 108);
							}
							else
							{
								ptr->FrameX = (short)(genRand.Next(6) * 18);
							}
						}
						flag = true;
						break;
					case EntityID.TileID.GLOWING_MUSHROOM:
						ptr->IsActive = 1;
						ptr->Type = (byte)type;
						ptr->FrameX = (short)(genRand.Next(5) * 18);
						flag = true;
						break;
					case EntityID.TileID.CRYSTAL_SHARD:
						ptr->IsActive = 1;
						ptr->Type = (byte)type;
						ptr->FrameX = (short)(genRand.Next(8) * 18);
						flag = true;
						break;
					case EntityID.TileID.SWITCH:
						ptr->IsActive = 1;
						ptr->Type = (byte)type;
						flag = true;
						break;
					case EntityID.TileID.TORCH:
						ptr->IsActive = 1;
						ptr->Type = (byte)type;
						ptr->FrameY = (short)(22 * style);
						flag = true;
						break;
					case EntityID.TileID.DOOR_CLOSED:
						flag = PlaceDoor(i, j, type);
						break;
					case EntityID.TileID.MANNEQUIN:
						flag = PlaceMan(i, j, style);
						break;
					case EntityID.TileID.XMAS_LIGHT:
						ptr->IsActive = 1;
						ptr->Type = (byte)type;
						ptr->FrameX = (short)(18 * style);
						flag = true;
						break;
					case EntityID.TileID.MUSIC_BOX:
						flag = PlaceMB(i, j, type, style);
						break;
					case EntityID.TileID.CHANDELIER:
					case EntityID.TileID.JACK_O_LANTERN:
					case EntityID.TileID.PRESENT:
					case EntityID.TileID.SAWMILL:
						flag = Place3x3(i, j, type);
						break;
					case EntityID.TileID.BOTTLE:
					case EntityID.TileID.CANDLE:
					case EntityID.TileID.WATER_CANDLE:
					case EntityID.TileID.BOOK:
					case EntityID.TileID.CLAY_POT:
						flag = PlaceOnTable1x1(i, j, type, style);
						break;
					case EntityID.TileID.TABLE:
					case EntityID.TileID.DEMON_ALTAR:
					case EntityID.TileID.LOOM:
					case EntityID.TileID.PIANO:
					case EntityID.TileID.DRESSER:
					case EntityID.TileID.BENCH:
					case EntityID.TileID.TINKERERS_WORKSHOP:
						flag = Place3x2(i, j, type);
						break;
					case EntityID.TileID.CHAIR:
					case EntityID.TileID.SAPLING:
						flag = Place1x2(i, j, type, style);
						break;
					case EntityID.TileID.ANVIL:
					case EntityID.TileID.WORK_BENCH:
					case EntityID.TileID.PIGGYBANK:
					case EntityID.TileID.BOWL:
					case EntityID.TileID.MYTHRIL_ANVIL:
						flag = Place2x1(i, j, type);
						break;
					case EntityID.TileID.LAMP_POST:
					case EntityID.TileID.TIKI_TORCH:
						flag = Place1xX(i, j, type);
						break;
					case EntityID.TileID.GRANDFATHERS_CLOCK:
					case EntityID.TileID.STATUE:
						flag = Place2xX(i, j, type, style);
						break;
					case EntityID.TileID.FURNACE:
					case EntityID.TileID.HELLFORGE:
					case EntityID.TileID.ADAMANTITE_FORGE:
						flag = Place3x2(i, j, type);
						break;
					case EntityID.TileID.BED:
					case EntityID.TileID.BATHTUB:
						flag = Place4x2(i, j, type, (plr < 0) ? 1 : Main.PlayerSet[plr].direction);
						break;
					case EntityID.TileID.CHEST:
						flag = PlaceChest(i, j, notNearOtherChests: false, style) >= 0;
						break;
					case EntityID.TileID.BANNER:
						flag = PlaceBanner(i, j, type, style);
						break;
					case EntityID.TileID.PRESSURE_PLATE:
					case EntityID.TileID.EXPLOSIVES:
					case EntityID.TileID.TIMER:
						flag = Place1x1(i, j, type, style);
						break;
					case EntityID.TileID.BOOKCASE:
					case EntityID.TileID.THRONE:
						flag = Place3x4(i, j, type);
						break;
					case EntityID.TileID.SUNFLOWER:
						flag = PlaceSunflower(i, j);
						break;
					case EntityID.TileID.POT:
						flag = PlacePot(i, j);
						break;
					case EntityID.TileID.CHAIN_LANTERN:
						flag = Place1x2Top(i, j, type);
						break;
					case EntityID.TileID.SIGN:
					case EntityID.TileID.TOMBSTONE:
						flag = PlaceSign(i, j, type);
						break;
					case EntityID.TileID.DAYBLOOM_GROWING:
					case EntityID.TileID.DAYBLOOM_MATURE:
					case EntityID.TileID.DAYBLOOM_BLOOMING:
						flag = PlaceAlch(i, j, style);
						break;
					case EntityID.TileID.KEG:
					case EntityID.TileID.CHINESE_LANTERN:
					case EntityID.TileID.COOKING_POT:
					case EntityID.TileID.SAFE:
					case EntityID.TileID.SKULL_LANTERN:
					case EntityID.TileID.TRASH_CAN:
					case EntityID.TileID.CANDELABRA:
					case EntityID.TileID.CRYSTAL_BALL:
					case EntityID.TileID.DISCO_BALL:
					case EntityID.TileID.LEVER:
					case EntityID.TileID.BOULDER:
					case EntityID.TileID.PUMP_IN:
					case EntityID.TileID.PUMP_OUT:
						flag = Place2x2(i, j, type);
						break;
					default:
						ptr->IsActive = 1;
						ptr->Type = (byte)type;
						switch ((EntityID.TileID)type)
						{
						case EntityID.TileID.CORAL:
							ptr->FrameX = (short)(26 * genRand.Next(6));
							break;
						case EntityID.TileID.TRAP:
							if (style == 1)
							{
								ptr->FrameX = 18;
							}
							break;
						}
						flag = true;
						break;
					}
					if (flag && !ToMute && !Gen)
					{
						SquareTileFrame(i, j);
						if (type == 127)
						{
							Main.PlaySound(2, i * 16, j * 16, 30);
						}
						else
						{
							Main.PlaySound(0, i * 16, j * 16);
							if (type == 22 || type == 140)
							{
								Main.DustSet.NewDust(i * 16, j * 16, 16, 16, 14);
								Main.DustSet.NewDust(i * 16, j * 16, 16, 16, 14);
							}
						}
					}
				}
			}
		}
		catch
		{
		}
		if (flag)
		{
			// One line, and a no-op unless a mod is loaded and a world is running.
			Terraria.Mods.ModEvents.TilePlaced(i, j);
		}
		return flag;
	}

	public static void UpdateMech()
	{
		for (int num = numMechs - 1; num >= 0; num--)
		{
			mech[num].Time--;
			if (Main.TileSet[mech[num].X, mech[num].Y].IsActive != 0 && Main.TileSet[mech[num].X, mech[num].Y].Type == 144)
			{
				if (Main.TileSet[mech[num].X, mech[num].Y].FrameY == 0)
				{
					mech[num].Time = 0;
				}
				else
				{
					int num2 = Main.TileSet[mech[num].X, mech[num].Y].FrameX / 18;
					if (mech[num].Time % (num2 * 120 + 60) == 0)
					{
						mech[num].Time = 18000;
						TripWire(mech[num].X, mech[num].Y);
					}
				}
			}
			if (mech[num].Time <= 0)
			{
				if (Main.TileSet[mech[num].X, mech[num].Y].IsActive != 0 && Main.TileSet[mech[num].X, mech[num].Y].Type == 144)
				{
					Main.TileSet[mech[num].X, mech[num].Y].FrameY = 0;
					NetMessage.SendTile(mech[num].X, mech[num].Y);
				}
				for (int i = num; i < numMechs; i++)
				{
					ref Mech reference = ref mech[i];
					reference = mech[i + 1];
				}
				numMechs--;
			}
		}
	}

	public static bool checkMech(int i, int j, int time)
	{
		for (int k = 0; k < numMechs; k++)
		{
			if (mech[k].X == i && mech[k].Y == j)
			{
				return false;
			}
		}
		if (numMechs < 999)
		{
			mech[numMechs].X = (short)i;
			mech[numMechs].Y = (short)j;
			mech[numMechs].Time = time;
			numMechs++;
			return true;
		}
		return false;
	}

	public static void HitSwitch(int i, int j)
	{
		switch ((EntityID.TileID)Main.TileSet[i, j].Type)
		{
		case EntityID.TileID.PRESSURE_PLATE:
			Main.PlaySound(28, i * 16, j * 16, 0);
			TripWire(i, j);
			break;
		case EntityID.TileID.SWITCH:
			if (Main.TileSet[i, j].FrameY == 0)
			{
				Main.TileSet[i, j].FrameY = 18;
			}
			else
			{
				Main.TileSet[i, j].FrameY = 0;
			}
			Main.PlaySound(28, i * 16, j * 16, 0);
			TripWire(i, j);
			break;
		case EntityID.TileID.TIMER:
			if (Main.TileSet[i, j].FrameY == 0)
			{
				Main.TileSet[i, j].FrameY = 18;
				if (Main.NetMode != 1)
				{
					checkMech(i, j, 18000);
				}
			}
			else
			{
				Main.TileSet[i, j].FrameY = 0;
			}
			Main.PlaySound(28, i * 16, j * 16, 0);
			break;
		case EntityID.TileID.LEVER:
		{
			int num = i;
			int num2 = j;
			short num3 = 36;
			num = Main.TileSet[i, j].FrameX / 18 * -1;
			num2 = Main.TileSet[i, j].FrameY / 18 * -1;
			if (num < -1)
			{
				num += 2;
				num3 = -36;
			}
			num += i;
			num2 += j;
			for (int k = num; k < num + 2; k++)
			{
				for (int l = num2; l < num2 + 2; l++)
				{
					if (Main.TileSet[k, l].Type == 132)
					{
						Main.TileSet[k, l].FrameX += num3;
					}
				}
			}
			TileFrame(num, num2);
			Main.PlaySound(28, i * 16, j * 16, 0);
			for (int m = num; m < num + 2; m++)
			{
				for (int n = num2; n < num2 + 2; n++)
				{
					if (Main.TileSet[m, n].Type == 132 && Main.TileSet[m, n].IsActive != 0 && Main.TileSet[m, n].wire != 0)
					{
						TripWire(m, n);
						return;
					}
				}
			}
			break;
		}
		}
	}

	public static void TripWire(int i, int j)
	{
		if (Main.NetMode != 1)
		{
			numNoWire = 0;
			numInPump = 0;
			numOutPump = 0;
			NoWire(i, j);
			wireCheck(i, j);
			if (numInPump > 0 && numOutPump > 0)
			{
				xferWater();
			}
		}
	}

	public static void xferWater()
	{
		for (int i = 0; i < numInPump; i++)
		{
			int x = inPump[i].X;
			int y = inPump[i].Y;
			int liquid = Main.TileSet[x, y].Liquid;
			if (liquid <= 0)
			{
				continue;
			}
			int lava = Main.TileSet[x, y].Lava;
			for (int j = 0; j < numOutPump; j++)
			{
				int x2 = outPump[j].X;
				int y2 = outPump[j].Y;
				int liquid2 = Main.TileSet[x2, y2].Liquid;
				if (liquid2 >= 255)
				{
					continue;
				}
				int num = Main.TileSet[x2, y2].Lava;
				if (liquid2 == 0)
				{
					num = lava;
				}
				if (lava == num)
				{
					int num2 = liquid;
					if (num2 + liquid2 > 255)
					{
						num2 = 255 - liquid2;
					}
					Main.TileSet[x2, y2].Liquid += (byte)num2;
					Main.TileSet[x, y].Liquid -= (byte)num2;
					liquid = Main.TileSet[x, y].Liquid;
					Main.TileSet[x2, y2].Lava = (byte)lava;
					SquareTileFrame(x2, y2);
					if (Main.TileSet[x, y].Liquid == 0)
					{
						Main.TileSet[x, y].Lava = 0;
						SquareTileFrame(x, y);
						break;
					}
				}
			}
			SquareTileFrame(x, y);
		}
	}

	public static void NoWire(int i, int j)
	{
		if (numNoWire < 999)
		{
			noWire[numNoWire].X = (short)i;
			noWire[numNoWire].Y = (short)j;
			numNoWire++;
		}
	}

	public static void wireCheck(int i, int j)
	{
		int num = 0;
		int num2 = 1;
		Dir dir = Dir.LEFT;
		wiresChecked[0].X = (short)i;
		wiresChecked[0].Y = (short)j;
		wiresChecked[0].DirsChecked = 0;
		wire[0].X = (short)i;
		wire[0].Y = (short)j;
		bool flag = false;
		while (true)
		{
			switch (dir)
			{
			case Dir.LEFT:
				i--;
				break;
			case Dir.RIGHT:
				i++;
				break;
			case Dir.UP:
				j--;
				break;
			case Dir.DOWN:
				j++;
				break;
			}
			wiresChecked[num].DirsChecked |= (byte)dir;
			if (Main.TileSet[i, j].wire > 0)
			{
				int num3 = num2 - 1;
				flag = false;
				while (true)
				{
					if (num3 < 0)
					{
						if (num2 == 1000)
						{
							return;
						}
						wire[num2].X = (short)i;
						wire[num2].Y = (short)j;
						num2++;
						num++;
						wiresChecked[num].X = (short)i;
						wiresChecked[num].Y = (short)j;
						switch (dir)
						{
						case Dir.LEFT:
						case Dir.RIGHT:
							wiresChecked[num].DirsChecked = (byte)(dir ^ Dir.LEFT_RIGHT);
							break;
						case Dir.UP:
						case Dir.DOWN:
							wiresChecked[num].DirsChecked = (byte)(dir ^ Dir.UP_DOWN);
							dir = Dir.LEFT;
							break;
						}
						if (Main.TileSet[i, j].IsActive == 0)
						{
							break;
						}
						for (int num4 = numNoWire - 1; num4 >= 0; num4--)
						{
							if (noWire[num4].X == i && noWire[num4].Y == j)
							{
								return;
							}
						}
						int type = Main.TileSet[i, j].Type;
						switch ((EntityID.TileID)type)
						{
						case EntityID.TileID.TIMER:
							HitSwitch(i, j);
							SquareTileFrame(i, j);
							NetMessage.SendTile(i, j);
							break;
						case EntityID.TileID.ACTIVE_STONE:
							Main.TileSet[i, j].Type = 131;
							SquareTileFrame(i, j);
							NetMessage.SendTile(i, j);
							break;
						case EntityID.TileID.INACTIVE_STONE:
							Main.TileSet[i, j].Type = 130;
							SquareTileFrame(i, j);
							NetMessage.SendTile(i, j);
							break;
						case EntityID.TileID.DOOR_OPEN:
							CloseDoor(i, j, forced: true);
							NetMessage.CreateMessage2(24, i, j);
							NetMessage.SendMessage();
							break;
						case EntityID.TileID.DOOR_CLOSED:
						{
							int direction = (Main.Rand.Next(2) << 1) - 1;
							direction = OpenDoor(i, j, direction);
							if (direction != 0)
							{
								NetMessage.CreateMessage3(19, i, j, direction);
								NetMessage.SendMessage();
							}
							break;
						}
						case EntityID.TileID.TORCH:
							if (Main.TileSet[i, j].FrameX < 66)
							{
								Main.TileSet[i, j].FrameX += 66;
							}
							else
							{
								Main.TileSet[i, j].FrameX -= 66;
							}
							NetMessage.SendTile(i, j);
							break;
						case EntityID.TileID.XMAS_LIGHT:
							if (Main.TileSet[i, j].FrameX < 54)
							{
								Main.TileSet[i, j].FrameX += 54;
							}
							else
							{
								Main.TileSet[i, j].FrameX -= 54;
							}
							NetMessage.SendTile(i, j);
							break;
						case EntityID.TileID.CHAIN_LANTERN:
						{
							int num35 = j - Main.TileSet[i, j].FrameY / 18;
							short num36 = 18;
							if (Main.TileSet[i, j].FrameX > 0)
							{
								num36 = -18;
							}
							Main.TileSet[i, num35].FrameX += num36;
							Main.TileSet[i, num35 + 1].FrameX += num36;
							NoWire(i, num35);
							NoWire(i, num35 + 1);
							NetMessage.SendTileSquare(i, j, 2);
							break;
						}
						case EntityID.TileID.TIKI_TORCH:
						{
							int num25 = j - Main.TileSet[i, j].FrameY / 18;
							short num26 = 18;
							if (Main.TileSet[i, j].FrameX > 0)
							{
								num26 = -18;
							}
							Main.TileSet[i, num25].FrameX += num26;
							Main.TileSet[i, num25 + 1].FrameX += num26;
							Main.TileSet[i, num25 + 2].FrameX += num26;
							NoWire(i, num25);
							NoWire(i, num25 + 1);
							NoWire(i, num25 + 2);
							NetMessage.SendTileSquare(i, num25 + 1, 3);
							break;
						}
						case EntityID.TileID.CHINESE_LANTERN:
						case EntityID.TileID.CANDELABRA:
						case EntityID.TileID.DISCO_BALL:
						{
							int num15 = j - Main.TileSet[i, j].FrameY / 18;
							int num16 = Main.TileSet[i, j].FrameX / 18;
							if (num16 > 1)
							{
								num16 -= 2;
							}
							num16 = i - num16;
							short num17 = 36;
							if (Main.TileSet[num16, num15].FrameX > 0)
							{
								num17 = -36;
							}
							Main.TileSet[num16, num15].FrameX += num17;
							Main.TileSet[num16, num15 + 1].FrameX += num17;
							Main.TileSet[num16 + 1, num15].FrameX += num17;
							Main.TileSet[num16 + 1, num15 + 1].FrameX += num17;
							NoWire(num16, num15);
							NoWire(num16, num15 + 1);
							NoWire(num16 + 1, num15);
							NoWire(num16 + 1, num15 + 1);
							NetMessage.SendTileSquare(num16, num15, 3);
							break;
						}
						case EntityID.TileID.CHANDELIER:
						case EntityID.TileID.JACK_O_LANTERN:
						case EntityID.TileID.PRESENT:
						{
							int num27 = j - Main.TileSet[i, j].FrameY / 18;
							int num28 = Main.TileSet[i, j].FrameX / 18;
							if (num28 > 2)
							{
								num28 -= 3;
							}
							num28 = i - num28;
							short num29 = 54;
							if (Main.TileSet[num28, num27].FrameX > 0)
							{
								num29 = -54;
							}
							for (int num30 = num28; num30 < num28 + 3; num30++)
							{
								for (int num31 = num27; num31 < num27 + 3; num31++)
								{
									Main.TileSet[num30, num31].FrameX += num29;
									NoWire(num30, num31);
								}
							}
							NetMessage.SendTileSquare(num28 + 1, num27 + 1, 3);
							break;
						}
						case EntityID.TileID.CANDLE:
						{
							short num24 = 18;
							if (Main.TileSet[i, j].FrameX > 0)
							{
								num24 = -18;
							}
							Main.TileSet[i, j].FrameX += num24;
							NetMessage.SendTileSquare(i, j, 3);
							break;
						}
						case EntityID.TileID.LAMP_POST:
						{
							int num37 = j - Main.TileSet[i, j].FrameY / 18;
							short num38 = 18;
							if (Main.TileSet[i, j].FrameX > 0)
							{
								num38 = -18;
							}
							Main.TileSet[i, num37].FrameX += num38;
							Main.TileSet[i, num37 + 1].FrameX += num38;
							Main.TileSet[i, num37 + 2].FrameX += num38;
							Main.TileSet[i, num37 + 3].FrameX += num38;
							Main.TileSet[i, num37 + 4].FrameX += num38;
							Main.TileSet[i, num37 + 5].FrameX += num38;
							NoWire(i, num37);
							NoWire(i, num37 + 1);
							NoWire(i, num37 + 2);
							NoWire(i, num37 + 3);
							NoWire(i, num37 + 4);
							NoWire(i, num37 + 5);
							NetMessage.SendTileSquare(i, num37 + 3, 7);
							break;
						}
						case EntityID.TileID.TRAP:
							if (checkMech(i, j, 180))
							{
								int num32 = ((Main.TileSet[i, j].FrameX != 0) ? 1 : (-1));
								float speedX = 12 * num32;
								int num33 = i * 16 + 8 + 10 * num32;
								int num34 = j * 16 + 9;
								Projectile.NewProjectile(num33, num34, speedX, 0f, 98, 20, 2f);
							}
							break;
						case EntityID.TileID.MUSIC_BOX:
							SwitchMB(i, j);
							break;
						case EntityID.TileID.EXPLOSIVES:
							KillTile(i, j, KillToFail: false, EffectOnly: false, noItem: true);
							NetMessage.SendTile(i, j);
							Projectile.NewProjectile(i * 16 + 8, j * 16 + 8, 0f, 0f, 108, 250, 10f);
							break;
						case EntityID.TileID.PUMP_IN:
						case EntityID.TileID.PUMP_OUT:
						{
							int num18 = j - Main.TileSet[i, j].FrameY / 18;
							int num19 = Main.TileSet[i, j].FrameX / 18;
							if (num19 > 1)
							{
								num19 -= 2;
							}
							num19 = i - num19;
							NoWire(num19, num18);
							NoWire(num19, num18 + 1);
							NoWire(num19 + 1, num18);
							NoWire(num19 + 1, num18 + 1);
							if (type == 142)
							{
								int num20 = num19;
								int num21 = num18;
								for (int m = 0; m < 4; m++)
								{
									if (numInPump >= 19)
									{
										break;
									}
									switch (m)
									{
									case 0:
										num20 = num19;
										num21 = num18 + 1;
										break;
									case 1:
										num20 = num19 + 1;
										num21 = num18 + 1;
										break;
									case 2:
										num20 = num19;
										num21 = num18;
										break;
									default:
										num20 = num19 + 1;
										num21 = num18;
										break;
									}
									inPump[numInPump].X = (short)num20;
									inPump[numInPump].Y = (short)num21;
									numInPump++;
								}
								break;
							}
							int num22 = num19;
							int num23 = num18;
							for (int n = 0; n < 4; n++)
							{
								if (numOutPump >= 19)
								{
									break;
								}
								switch (n)
								{
								case 0:
									num22 = num19;
									num23 = num18 + 1;
									break;
								case 1:
									num22 = num19 + 1;
									num23 = num18 + 1;
									break;
								case 2:
									num22 = num19;
									num23 = num18;
									break;
								default:
									num22 = num19 + 1;
									num23 = num18;
									break;
								}
								outPump[numOutPump].X = (short)num22;
								outPump[numOutPump].Y = (short)num23;
								numOutPump++;
							}
							break;
						}
						case EntityID.TileID.STATUE:
						{
							int num5 = j - Main.TileSet[i, j].FrameY / 18;
							int num6 = Main.TileSet[i, j].FrameX / 18;
							int num7 = num6 >> 1;
							num6 = i - (num6 & 1);
							NoWire(num6, num5);
							NoWire(num6, num5 + 1);
							NoWire(num6, num5 + 2);
							NoWire(num6 + 1, num5);
							NoWire(num6 + 1, num5 + 1);
							NoWire(num6 + 1, num5 + 2);
							int num8 = num6 * 16 + 16;
							int num9 = (num5 + 3) * 16;
							int num10 = -1;
							switch (num7)
							{
							case 4:
								if (checkMech(i, j, 30) && NPC.MechSpawn(num8, num9, 1))
								{
									num10 = NPC.NewNPC(num8, num9 - 12, 1);
								}
								break;
							case 7:
								if (checkMech(i, j, 30) && NPC.MechSpawn(num8, num9, 49))
								{
									num10 = NPC.NewNPC(num8 - 4, num9 - 6, 49);
								}
								break;
							case 8:
								if (checkMech(i, j, 30) && NPC.MechSpawn(num8, num9, 55))
								{
									num10 = NPC.NewNPC(num8, num9 - 12, 55);
								}
								break;
							case 9:
								if (checkMech(i, j, 30) && NPC.MechSpawn(num8, num9, 46))
								{
									num10 = NPC.NewNPC(num8, num9 - 12, 46);
								}
								break;
							case 10:
								if (checkMech(i, j, 30) && NPC.MechSpawn(num8, num9, 21))
								{
									num10 = NPC.NewNPC(num8, num9, 21);
								}
								break;
							case 18:
								if (checkMech(i, j, 30) && NPC.MechSpawn(num8, num9, 67))
								{
									num10 = NPC.NewNPC(num8, num9 - 12, 67);
								}
								break;
							case 23:
								if (checkMech(i, j, 30) && NPC.MechSpawn(num8, num9, 63))
								{
									num10 = NPC.NewNPC(num8, num9 - 12, 63);
								}
								break;
							case 27:
								if (checkMech(i, j, 30) && NPC.MechSpawn(num8, num9, 85))
								{
									num10 = NPC.NewNPC(num8 - 9, num9, 85);
								}
								break;
							case 28:
								if (checkMech(i, j, 30) && NPC.MechSpawn(num8, num9, 74))
								{
									num10 = NPC.NewNPC(num8, num9 - 12, 74);
								}
								break;
							case 42:
								if (checkMech(i, j, 30) && NPC.MechSpawn(num8, num9, 58))
								{
									num10 = NPC.NewNPC(num8, num9 - 12, 58);
								}
								break;
							case 37:
								if (checkMech(i, j, 600) && Item.MechSpawn(num8, num9, 58))
								{
									Item.NewItem(num8, num9 - 16, 0, 0, 58);
								}
								break;
							case 2:
								if (checkMech(i, j, 600) && Item.MechSpawn(num8, num9, 184))
								{
									Item.NewItem(num8, num9 - 16, 0, 0, 184);
								}
								break;
							case 17:
								if (checkMech(i, j, 600) && Item.MechSpawn(num8, num9, 166))
								{
									Item.NewItem(num8, num9 - 20, 0, 0, 166);
								}
								break;
							case 40:
							{
								if (!checkMech(i, j, 300))
								{
									break;
								}
								int[] array2 = new int[8];
								int num13 = 0;
								for (int l = 0; l < 196; l++)
								{
									if (Main.NPCSet[l].Active == 0)
									{
										continue;
									}
									int type3 = Main.NPCSet[l].Type;
									if (type3 == 17 || type3 == 19 || type3 == 22 || type3 == 38 || type3 == 54 || type3 == 107 || type3 == 108 || type3 == 142)
									{
										array2[num13] = l;
										if (++num13 == 8)
										{
											break;
										}
									}
								}
								if (num13 > 0)
								{
									int num14 = array2[Main.Rand.Next(num13)];
									Main.NPCSet[num14].XYWH.X = num8 - (Main.NPCSet[num14].Width >> 1);
									Main.NPCSet[num14].XYWH.Y = num9 - Main.NPCSet[num14].Height - 1;
									Main.NPCSet[num14].Position.X = Main.NPCSet[num14].XYWH.X;
									Main.NPCSet[num14].Position.Y = Main.NPCSet[num14].XYWH.Y;
									NetMessage.CreateMessage1(23, num14);
									NetMessage.SendMessage();
								}
								break;
							}
							case 41:
							{
								if (!checkMech(i, j, 300))
								{
									break;
								}
								int[] array = new int[4];
								int num11 = 0;
								for (int k = 0; k < 196; k++)
								{
									if (Main.NPCSet[k].Active == 0)
									{
										continue;
									}
									int type2 = Main.NPCSet[k].Type;
									if (type2 == 18 || type2 == 20 || type2 == 124)
									{
										array[num11] = k;
										if (++num11 == 4)
										{
											break;
										}
									}
								}
								if (num11 > 0)
								{
									int num12 = array[Main.Rand.Next(num11)];
									Main.NPCSet[num12].XYWH.X = num8 - (Main.NPCSet[num12].Width >> 1);
									Main.NPCSet[num12].XYWH.Y = num9 - Main.NPCSet[num12].Height - 1;
									Main.NPCSet[num12].Position.X = Main.NPCSet[num12].XYWH.X;
									Main.NPCSet[num12].Position.Y = Main.NPCSet[num12].XYWH.Y;
									NetMessage.CreateMessage1(23, num12);
									NetMessage.SendMessage();
								}
								break;
							}
							}
							if (num10 >= 0)
							{
								Main.NPCSet[num10].Value = 0f;
								Main.NPCSet[num10].NpcSlots = 0f;
							}
							break;
						}
						}
						break;
					}
					if (wire[num3].X != i || wire[num3].Y != j)
					{
						num3--;
						continue;
					}
					flag = true;
					break;
				}
				if (flag)
				{
					continue;
				}
			}
			switch (dir)
			{
			case Dir.LEFT:
				i++;
				break;
			case Dir.RIGHT:
				i--;
				break;
			case Dir.UP:
				j++;
				break;
			case Dir.DOWN:
				j--;
				break;
			}
			while (true)
			{
				dir = (Dir)wiresChecked[num].DirsChecked;
				dir = ~dir & (dir + 1);
				if (dir <= Dir.DOWN)
				{
					break;
				}
				if (--num < 0)
				{
					return;
				}
				i = wiresChecked[num].X;
				j = wiresChecked[num].Y;
			}
		}
	}

	public unsafe static bool CanKillTile(int i, int j)
	{
		fixed (Tile* ptr = &Main.TileSet[i, j])
		{
			if (ptr->IsActive != 0 && j >= 1)
			{
				Tile* ptr2 = ptr - 1;
				if (ptr2->IsActive != 0)
				{
					int type = ptr->Type;
					int type2 = ptr2->Type;
					switch ((EntityID.TileID)type2)
					{
					case EntityID.TileID.TREE:
						if (type != type2)
						{
							if ((ptr[-1].FrameX != 66 || ptr[-1].FrameY < 0 || ptr[-1].FrameY > 44) && (ptr[-1].FrameX != 88 || ptr[-1].FrameY < 66 || ptr[-1].FrameY > 110))
							{
								return ptr[-1].FrameY >= 198;
							}
							return true;
						}
						return true;
					case EntityID.TileID.LIFE_CRYSTAL:
					case EntityID.TileID.CHEST:
					case EntityID.TileID.DEMON_ALTAR:
					case EntityID.TileID.GIANT_GLOWING_MUSHROOM:
						return type == type2;
					}
				}
			}
		}
		return true;
	}

	public static bool CanKillWall(int i, int j)
	{
		int wallType = Main.TileSet[i, j].WallType;
		if (Main.WallHouse[wallType])
		{
			return true;
		}
		for (int k = i - 1; k < i + 2; k++)
		{
			for (int l = j - 1; l < j + 2; l++)
			{
				if (Main.TileSet[k, l].WallType != wallType)
				{
					return true;
				}
			}
		}
		return false;
	}

	public unsafe static void KillWall(int i, int j, bool fail = false)
	{
		if (i < 0 || j < 0 || i >= Main.MaxTilesX || j >= Main.MaxTilesY)
		{
			return;
		}
		int wallType = Main.TileSet[i, j].WallType;
		if (wallType <= 0)
		{
			return;
		}
		int type = 0;
		int soundType = 0;
		switch ((EntityID.WallID)wallType)
		{
		case EntityID.WallID.STONE:
		case EntityID.WallID.GREY_BRICK:
		case EntityID.WallID.RED_BRICK:
		case EntityID.WallID.BLUE_DUNGEON_UNSAFE:
		case EntityID.WallID.GREEN_DUNGEON_UNSAFE:
		case EntityID.WallID.PINK_DUNGEON_UNSAFE:
			type = 1;
			break;
		case EntityID.WallID.WOOD:
			type = 7;
			break;
		case EntityID.WallID.COPPER_BRICK:
			type = 9;
			break;
		case EntityID.WallID.GOLD_BRICK:
		case EntityID.WallID.SILVER_BRICK:
			type = wallType;
			break;
		case EntityID.WallID.GLASS:
			soundType = 13;
			type = 13;
			break;
		case EntityID.WallID.PEARLSTONE_BRICK:
		case EntityID.WallID.PEARLSTONE_BRICK_UNSAFE:
			type = 51;
			break;
		case EntityID.WallID.IRIDESCENT_BRICK:
			type = 38;
			break;
		case EntityID.WallID.MUDSTONE_BRICK:
			type = 36;
			break;
		case EntityID.WallID.COBALT_BRICK:
			type = 48;
			break;
		case EntityID.WallID.MYTHRIL_BRICK:
		case EntityID.WallID.GREEN_CANDY_CANE:
			type = 49;
			break;
		case EntityID.WallID.CANDY_CANE:
			type = 50;
			break;
		case EntityID.WallID.SNOW_BRICK:
			type = 51;
			break;
		}
		Main.PlaySound(soundType, i * 16, j * 16);
		for (int num = (fail ? 1 : 5); num >= 0; num--)
		{
			switch ((EntityID.WallID)wallType)
			{
			case EntityID.WallID.EBONSTONE_UNSAFE:
				type = 1 + 13 * genRand.Next(2);
				break;
			case EntityID.WallID.PLANKED:
				type = 1 + 6 * genRand.Next(2);
				break;
			}
			Main.DustSet.NewDust(i * 16, j * 16, 16, 16, type);
		}
		if (!fail)
		{
			EntityID.ItemID itemID = EntityID.ItemID.NONE;
			switch ((EntityID.WallID)wallType)
			{
			case EntityID.WallID.STONE:
				itemID = EntityID.ItemID.STONE_WALL;
				break;
			case EntityID.WallID.WOOD:
				itemID = EntityID.ItemID.WOOD_WALL;
				break;
			case EntityID.WallID.GREY_BRICK:
				itemID = EntityID.ItemID.GRAY_BRICK_WALL;
				break;
			case EntityID.WallID.RED_BRICK:
				itemID = EntityID.ItemID.RED_BRICK_WALL;
				break;
			case EntityID.WallID.BLUE_DUNGEON_UNSAFE:
				itemID = EntityID.ItemID.BLUE_BRICK_WALL;
				break;
			case EntityID.WallID.GREEN_DUNGEON_UNSAFE:
				itemID = EntityID.ItemID.GREEN_BRICK_WALL;
				break;
			case EntityID.WallID.PINK_DUNGEON_UNSAFE:
				itemID = EntityID.ItemID.PINK_BRICK_WALL;
				break;
			case EntityID.WallID.GOLD_BRICK:
				itemID = EntityID.ItemID.GOLD_BRICK_WALL;
				break;
			case EntityID.WallID.SILVER_BRICK:
				itemID = EntityID.ItemID.SILVER_BRICK_WALL;
				break;
			case EntityID.WallID.COPPER_BRICK:
				itemID = EntityID.ItemID.COPPER_BRICK_WALL;
				break;
			case EntityID.WallID.OBSIDIAN_BRICK_UNSAFE:
				itemID = EntityID.ItemID.OBSIDIAN_BRICK_WALL;
				break;
			case EntityID.WallID.DIRT:
				itemID = EntityID.ItemID.DIRT_WALL;
				break;
			case EntityID.WallID.BLUE_DUNGEON:
				itemID = EntityID.ItemID.BLUE_BRICK_WALL;
				break;
			case EntityID.WallID.GREEN_DUNGEON:
				itemID = EntityID.ItemID.GREEN_BRICK_WALL;
				break;
			case EntityID.WallID.PINK_DUNGEON:
				itemID = EntityID.ItemID.PINK_BRICK_WALL;
				break;
			case EntityID.WallID.OBSIDIAN_BRICK:
				itemID = EntityID.ItemID.OBSIDIAN_BRICK_WALL;
				break;
			case EntityID.WallID.GLASS:
				itemID = EntityID.ItemID.GLASS_WALL;
				break;
			case EntityID.WallID.PEARLSTONE_BRICK:
				itemID = EntityID.ItemID.PEARLSTONE_BRICK_WALL;
				break;
			case EntityID.WallID.IRIDESCENT_BRICK:
				itemID = EntityID.ItemID.IRIDESCENT_BRICK_WALL;
				break;
			case EntityID.WallID.MUDSTONE_BRICK:
				itemID = EntityID.ItemID.MUDSTONE_BRICK_WALL;
				break;
			case EntityID.WallID.COBALT_BRICK:
				itemID = EntityID.ItemID.COBALT_BRICK_WALL;
				break;
			case EntityID.WallID.MYTHRIL_BRICK:
				itemID = EntityID.ItemID.MYTHRIL_BRICK_WALL;
				break;
			case EntityID.WallID.PLANKED:
				itemID = EntityID.ItemID.PLANKED_WALL;
				break;
			case EntityID.WallID.CANDY_CANE:
				itemID = EntityID.ItemID.CANDY_CANE_WALL;
				break;
			case EntityID.WallID.GREEN_CANDY_CANE:
				itemID = EntityID.ItemID.GREEN_CANDY_CANE_WALL;
				break;
			case EntityID.WallID.SNOW_BRICK:
				itemID = EntityID.ItemID.SNOW_BRICK_WALL;
				break;
			}
			if (itemID > EntityID.ItemID.NONE)
			{
				Item.NewItem(i * 16, j * 16, 16, 16, (int)itemID);
			}
			Main.TileSet[i, j].WallType = 0;
			SquareWallFrame(i, j);
		}
	}

	public unsafe static void KillTileFast(int i, int j)
	{
		fixed (Tile* ptr = &Main.TileSet[i, j])
		{
			if (ptr->IsActive == 0)
			{
				return;
			}
			int type = ptr->Type;
			if (j >= 1)
			{
				Tile* ptr2 = ptr - 1;
				if (ptr2->IsActive != 0)
				{
					int type2 = ptr2->Type;
					switch ((EntityID.TileID)type2)
					{
					case EntityID.TileID.TREE:
						if (type != type2 && (ptr[-1].FrameX != 66 || ptr[-1].FrameY < 0 || ptr[-1].FrameY > 44) && (ptr[-1].FrameX != 88 || ptr[-1].FrameY < 66 || ptr[-1].FrameY > 110) && ptr[-1].FrameY < 198)
						{
							return;
						}
						break;
					case EntityID.TileID.LIFE_CRYSTAL:
					case EntityID.TileID.CHEST:
					case EntityID.TileID.DEMON_ALTAR:
					case EntityID.TileID.GIANT_GLOWING_MUSHROOM:
						if (type != type2)
						{
							return;
						}
						break;
					}
				}
			}
			switch ((EntityID.TileID)type)
			{
			case EntityID.TileID.MANNEQUIN:
			{
				int num2 = i;
				int frameX = ptr->FrameX;
				if (ptr->FrameX % 100 % 36 == 18)
				{
					frameX = ptr[-1440].FrameX;
					num2--;
				}
				if (frameX >= 100)
				{
					int num3 = frameX / 100;
					frameX %= 100;
					switch (Main.TileSet[num2, j].FrameY / 18)
					{
					case 0:
						Item.NewItem(i * 16, j * 16, 16, 16, Item.HeadType[num3]);
						break;
					case 1:
						Item.NewItem(i * 16, j * 16, 16, 16, Item.BodyType[num3]);
						break;
					case 2:
						Item.NewItem(i * 16, j * 16, 16, 16, Item.LegType[num3]);
						break;
					}
					frameX = Main.TileSet[num2, j].FrameX % 100;
					Main.TileSet[num2, j].FrameX = (short)frameX;
				}
				break;
			}
			case EntityID.TileID.CHEST:
				if (Main.NetMode != 1)
				{
					int num = ptr->FrameX / 18;
					int y = j - ptr->FrameY / 18;
					num = i - (num & 1);
					if (!Chest.DestroyChest(num, y))
					{
						return;
					}
				}
				break;
			}
			ptr->IsActive = 0;
			ptr->Type = 0;
			ptr->FrameX = -1;
			ptr->FrameY = -1;
			ptr->frameNumber = 0;
			if (type == 58 && j > Main.MaxTilesY - 200)
			{
				ptr->Lava = 32;
				ptr->Liquid = 128;
			}
			SquareTileFrame(i, j);
		}
	}

	public unsafe static bool KillTile(int i, int j)
	{
		if (i >= 0 && j >= 0 && i < Main.MaxTilesX && j < Main.MaxTilesY)
		{
			fixed (Tile* ptr = &Main.TileSet[i, j])
			{
				if (ptr->IsActive != 0)
				{
					int type = ptr->Type;
					if (j >= 1)
					{
						Tile* ptr2 = ptr - 1;
						if (ptr2->IsActive != 0)
						{
							int type2 = ptr2->Type;
							switch ((EntityID.TileID)type2)
							{
							case EntityID.TileID.TREE:
								if (type != type2 && (ptr[-1].FrameX != 66 || ptr[-1].FrameY < 0 || ptr[-1].FrameY > 44) && (ptr[-1].FrameX != 88 || ptr[-1].FrameY < 66 || ptr[-1].FrameY > 110) && ptr[-1].FrameY < 198)
								{
									return false;
								}
								break;
							case EntityID.TileID.LIFE_CRYSTAL:
							case EntityID.TileID.CHEST:
							case EntityID.TileID.DEMON_ALTAR:
							case EntityID.TileID.GIANT_GLOWING_MUSHROOM:
								if (type != type2)
								{
									return false;
								}
								break;
							}
						}
					}
					if (!Gen && !stopDrops)
					{
						switch ((EntityID.TileID)type)
						{
						case EntityID.TileID.STONE:
						case EntityID.TileID.IRON_ORE:
						case EntityID.TileID.COPPER_ORE:
						case EntityID.TileID.GOLD_ORE:
						case EntityID.TileID.SILVER_ORE:
						case EntityID.TileID.DEMONITE_ORE:
						case EntityID.TileID.EBONSTONE:
						case EntityID.TileID.METEORITE:
						case EntityID.TileID.GRAY_BRICK:
						case EntityID.TileID.RED_BRICK:
						case EntityID.TileID.BLUE_BRICK:
						case EntityID.TileID.GREEN_BRICK:
						case EntityID.TileID.PINK_BRICK:
						case EntityID.TileID.GOLD_BRICK:
						case EntityID.TileID.SILVER_BRICK:
						case EntityID.TileID.COPPER_BRICK:
						case EntityID.TileID.SPIKE:
						case EntityID.TileID.OBSIDIAN:
						case EntityID.TileID.HELLSTONE:
						case EntityID.TileID.SAPPHIRE:
						case EntityID.TileID.RUBY:
						case EntityID.TileID.EMERALD:
						case EntityID.TileID.TOPAZ:
						case EntityID.TileID.AMETHYST:
						case EntityID.TileID.DIAMOND:
						case EntityID.TileID.OBSIDIAN_BRICK:
						case EntityID.TileID.HELLSTONE_BRICK:
						case EntityID.TileID.COBALT_ORE:
						case EntityID.TileID.MYTHRIL_ORE:
						case EntityID.TileID.ADAMANTITE_ORE:
						case EntityID.TileID.PEARLSTONE:
						case EntityID.TileID.PEARLSTONE_BRICK:
						case EntityID.TileID.IRIDESCENT_BRICK:
						case EntityID.TileID.MUDSTONE:
						case EntityID.TileID.COBALT_BRICK:
						case EntityID.TileID.MYTHRIL_BRICK:
						case EntityID.TileID.DEMONITE_BRICK:
							Main.PlaySound(21, i * 16, j * 16);
							break;
						case EntityID.TileID.SHORT_GRASS_PLANTS:
						case EntityID.TileID.SHORT_HALLOWED_PLANTS:
							Main.PlaySound(6, i * 16, j * 16);
							if (ptr->FrameX == 144)
							{
								Item.NewItem(i * 16, j * 16, 16, 16, 5);
							}
							break;
						case EntityID.TileID.SHORT_CORRUPT_PLANTS:
							Main.PlaySound(6, i * 16, j * 16);
							if (ptr->FrameX == 144)
							{
								Item.NewItem(i * 16, j * 16, 16, 16, 60);
							}
							break;
						case EntityID.TileID.CORRUPTION_THORN:
						case EntityID.TileID.COBWEB:
						case EntityID.TileID.VINE:
						case EntityID.TileID.SHORT_JUNGLE_PLANTS:
						case EntityID.TileID.JUNGLE_VINE:
						case EntityID.TileID.JUNGLE_THORN:
						case EntityID.TileID.GLOWING_MUSHROOM:
						case EntityID.TileID.TALL_GRASS_PLANTS:
						case EntityID.TileID.TALL_JUNGLE_PLANTS:
						case EntityID.TileID.DAYBLOOM_GROWING:
						case EntityID.TileID.DAYBLOOM_MATURE:
						case EntityID.TileID.DAYBLOOM_BLOOMING:
						case EntityID.TileID.TALL_HALLOWED_PLANTS:
						case EntityID.TileID.HALLOWED_VINE:
							Main.PlaySound(6, i * 16, j * 16);
							break;
						case EntityID.TileID.ICE:
							Main.PlaySound(2, i * 16, j * 16, 27);
							break;
						default:
							Main.PlaySound(0, i * 16, j * 16);
							if (type == 129)
							{
								Main.PlaySound(2, i * 16, j * 16, 27);
							}
							break;
						case EntityID.TileID.BOULDER:
							break;
						}
					}
					if (type != 138)
					{
						if (type == 128)
						{
							int num = i;
							int frameX = ptr->FrameX;
							if (ptr->FrameX % 100 % 36 == 18)
							{
								frameX = ptr[-1440].FrameX;
								num--;
							}
							if (frameX >= 100)
							{
								int num2 = frameX / 100;
								frameX %= 100;
								switch (Main.TileSet[num, j].FrameY / 18)
								{
								case 0:
									Item.NewItem(i * 16, j * 16, 16, 16, Item.HeadType[num2]);
									break;
								case 1:
									Item.NewItem(i * 16, j * 16, 16, 16, Item.BodyType[num2]);
									break;
								case 2:
									Item.NewItem(i * 16, j * 16, 16, 16, Item.LegType[num2]);
									break;
								}
								frameX = Main.TileSet[num, j].FrameX % 100;
								Main.TileSet[num, j].FrameX = (short)frameX;
							}
						}
						if (!Gen)
						{
							int num3 = 0;
							switch (type)
							{
							case 1:
							case 16:
							case 17:
							case 38:
							case 39:
							case 41:
							case 43:
							case 44:
							case 48:
							case 63:
							case 64:
							case 65:
							case 66:
							case 67:
							case 68:
							case 85:
							case 90:
							case 92:
							case 96:
							case 97:
							case 99:
							case 105:
							case 117:
							case 130:
							case 131:
							case 132:
							case 135:
							case 137:
							case 142:
							case 143:
							case 144:
								num3 = 1;
								break;
							case 33:
							case 95:
							case 98:
							case 100:
								num3 = 6;
								break;
							case 5:
							case 10:
							case 11:
							case 14:
							case 15:
							case 19:
							case 30:
							case 86:
							case 87:
							case 88:
							case 89:
							case 93:
							case 94:
							case 104:
							case 106:
							case 114:
							case 124:
							case 128:
							case 139:
								num3 = 7;
								break;
							case 21:
								if (ptr->FrameX >= 108)
								{
									num3 = 37;
								}
								else
								{
									num3 = ((ptr->FrameX < 36) ? 7 : 10);
								}
								break;
							case 127:
								num3 = 67;
								break;
							case 91:
								num3 = -1;
								break;
							case 6:
							case 26:
								num3 = 8;
								break;
							case 7:
							case 34:
							case 47:
								num3 = 9;
								break;
							case 8:
							case 36:
							case 45:
							case 102:
								num3 = 10;
								break;
							case 9:
							case 35:
							case 42:
							case 46:
							case 126:
							case 136:
								num3 = 11;
								break;
							case 12:
								num3 = 12;
								break;
							case 3:
							case 52:
							case 73:
								num3 = 3;
								break;
							case 13:
							case 54:
								num3 = 13;
								break;
							case 22:
							case 112:
							case 140:
								num3 = 14;
								break;
							case 28:
							case 50:
							case 78:
								num3 = 22;
								break;
							case 29:
								num3 = 23;
								break;
							case 40:
							case 103:
								num3 = 28;
								break;
							case 49:
								num3 = 29;
								break;
							case 51:
								num3 = 30;
								break;
							case 53:
							case 81:
								num3 = 32;
								break;
							case 56:
							case 75:
								num3 = 37;
								break;
							case 57:
							case 119:
							case 141:
								num3 = 36;
								break;
							case 59:
							case 120:
								num3 = 38;
								break;
							case 61:
							case 62:
							case 74:
							case 80:
								num3 = 40;
								break;
							case 69:
								num3 = 7;
								break;
							case 71:
							case 72:
								num3 = 26;
								break;
							case 70:
								num3 = 17;
								break;
							case 123:
								num3 = 53;
								break;
							case 116:
							case 118:
							case 147:
							case 148:
								num3 = 51;
								break;
							case 110:
							case 113:
							case 115:
								num3 = 47;
								break;
							case 107:
							case 121:
								num3 = 48;
								break;
							case 108:
							case 122:
							case 134:
							case 146:
							case 149:
								num3 = 49;
								break;
							case 111:
							case 133:
							case 145:
								num3 = 50;
								break;
							case 82:
							case 83:
							case 84:
								switch (ptr->FrameX / 18)
								{
								case 0:
									num3 = 3;
									break;
								case 1:
									num3 = 3;
									break;
								case 2:
									num3 = 7;
									break;
								case 3:
									num3 = 17;
									break;
								case 4:
									num3 = 3;
									break;
								case 5:
									num3 = 6;
									break;
								}
								break;
							default:
								switch ((EntityID.TileID)type)
								{
								case EntityID.TileID.CRYSTAL_SHARD:
									if (ptr->FrameX != 0 && ptr->FrameX != 54 && ptr->FrameX != 108)
									{
										num3 = ((ptr->FrameX != 18 && ptr->FrameX != 72 && ptr->FrameX != 126) ? 70 : 69);
									}
									else
									{
										num3 = 68;
									}
									break;
								case EntityID.TileID.TORCH:
								{
									int num4 = ptr->FrameY / 22;
									if (1 == 0)
									{
									}
									int num5 = num4 switch
									{
										0 => 6, 
										8 => 75, 
										_ => 58 + num4, 
									};
									if (1 == 0)
									{
									}
									num3 = num5;
									break;
								}
								}
								break;
							case 0:
								break;
							}
							if (num3 >= 0)
							{
								for (int num6 = 4; num6 >= 0; num6--)
								{
									switch ((EntityID.TileID)type)
									{
									case EntityID.TileID.GRASS:
										num3 = genRand.Next(2) << 1;
										break;
									case EntityID.TileID.SAPLING:
										num3 = ((genRand.Next(2) == 0) ? 7 : 2);
										break;
									case EntityID.TileID.CORRUPT_GRASS:
									case EntityID.TileID.SHORT_CORRUPT_PLANTS:
										num3 = ((genRand.Next(2) == 0) ? 14 : 17);
										break;
									case EntityID.TileID.SUNFLOWER:
										num3 = ((genRand.Next(2) == 0) ? 3 : 19);
										break;
									case EntityID.TileID.EBONSTONE:
									case EntityID.TileID.SHADOW_ORB:
										num3 = ((genRand.Next(2) != 0) ? 1 : 14);
										break;
									case EntityID.TileID.CORRUPTION_THORN:
										num3 = ((genRand.Next(2) == 0) ? 14 : 24);
										break;
									case EntityID.TileID.CHANDELIER:
									case EntityID.TileID.JACK_O_LANTERN:
									case EntityID.TileID.PRESENT:
									case EntityID.TileID.CHAIN_LANTERN:
										num3 = genRand.Next(2) * 6;
										break;
									case EntityID.TileID.METEORITE:
										num3 = ((genRand.Next(2) == 0) ? 6 : 23);
										break;
									case EntityID.TileID.SHORT_JUNGLE_PLANTS:
										num3 = 38 + genRand.Next(2);
										break;
									case EntityID.TileID.HELLSTONE:
									case EntityID.TileID.HELLSTONE_BRICK:
									case EntityID.TileID.HELLFORGE:
										num3 = ((genRand.Next(2) == 0) ? 6 : 25);
										break;
									case EntityID.TileID.HALLOWED_GRASS:
										num3 = genRand.Next(2) * 47;
										break;
									}
									Main.DustSet.NewDust(i * 16, j * 16, 16, 16, num3);
								}
							}
						}
					}
					if (type == 21 && Main.NetMode != 1)
					{
						int num7 = ptr->FrameX / 18;
						int y = j - ptr->FrameY / 18;
						num7 = i - (num7 & 1);
						if (!Chest.DestroyChest(num7, y))
						{
							return false;
						}
					}
					if (!Gen && !stopDrops && Main.NetMode != 1)
					{
						int num8 = 0;
						switch ((EntityID.TileID)type)
						{
						case EntityID.TileID.DIRT:
						case EntityID.TileID.GRASS:
						case EntityID.TileID.HALLOWED_GRASS:
							num8 = 2;
							break;
						case EntityID.TileID.STONE:
							num8 = 3;
							break;
						case EntityID.TileID.SHORT_GRASS_PLANTS:
						case EntityID.TileID.TALL_GRASS_PLANTS:
							if (Main.Rand.Next(2) == 0)
							{
								Rectangle rect = default;
								rect.X = i << 4;
								rect.Y = j << 4;
								rect.Width = (rect.Height = 16);
								if (Player.FindClosest(ref rect).HasItem(281))
								{
									num8 = 283;
								}
							}
							break;
						case EntityID.TileID.TORCH:
						{
							int num11 = ptr->FrameY / 22;
							if (1 == 0)
							{
							}
							int num5 = num11 switch
							{
								0 => 8, 
								8 => 523, 
								_ => 426 + num11, 
							};
							if (1 == 0)
							{
							}
							num8 = num5;
							break;
						}
						case EntityID.TileID.TREE:
							if (ptr->FrameX >= 22 && ptr->FrameY >= 198)
							{
								if (Main.NetMode != 1)
								{
									num8 = 9;
									if (genRand.Next(2) == 0)
									{
										int num12 = j - 1;
										int type3;
										do
										{
											type3 = Main.TileSet[i, ++num12].Type;
										}
										while (!Main.TileSolidNotSolidTop[type3] || Main.TileSet[i, num12].IsActive == 0);
										if (type3 == 2 || type3 == 109)
										{
											num8 = 27;
										}
									}
								}
							}
							else
							{
								num8 = 9;
							}
							woodSpawned++;
							break;
						case EntityID.TileID.IRON_ORE:
							num8 = 11;
							break;
						case EntityID.TileID.COPPER_ORE:
							num8 = 12;
							break;
						case EntityID.TileID.GOLD_ORE:
							num8 = 13;
							break;
						case EntityID.TileID.SILVER_ORE:
							num8 = 14;
							break;
						case EntityID.TileID.SILT:
							num8 = 424;
							break;
						case EntityID.TileID.WOODEN_BEAM:
							num8 = 480;
							break;
						case EntityID.TileID.XMAS_LIGHT:
							if (ptr->FrameX == 0 || ptr->FrameX == 54)
							{
								num8 = 596;
							}
							else if (ptr->FrameX == 18 || ptr->FrameX == 72)
							{
								num8 = 597;
							}
							else if (ptr->FrameX == 36 || ptr->FrameX == 90)
							{
								num8 = 598;
							}
							break;
						case EntityID.TileID.BOTTLE:
						{
							Main.PlaySound(13, i * 16, j * 16);
							short frameX2 = ptr->FrameX;
							if (1 == 0)
							{
							}
							int num5 = frameX2 switch
							{
								18 => 28, 
								36 => 110, 
								54 => 350, 
								72 => 351, 
								_ => 31, 
							};
							if (1 == 0)
							{
							}
							num8 = num5;
							break;
						}
						case EntityID.TileID.PLATFORM:
							num8 = 94;
							break;
						case EntityID.TileID.DEMONITE_ORE:
							num8 = 56;
							break;
						case EntityID.TileID.DEMONITE_BRICK:
							num8 = 577;
							break;
						case EntityID.TileID.CORRUPT_GRASS:
							num8 = 2;
							break;
						case EntityID.TileID.EBONSTONE:
							num8 = 61;
							break;
						case EntityID.TileID.WOOD:
							num8 = 9;
							break;
						case EntityID.TileID.CANDLE:
							num8 = 105;
							break;
						case EntityID.TileID.METEORITE:
							num8 = 116;
							break;
						case EntityID.TileID.GRAY_BRICK:
							num8 = 129;
							break;
						case EntityID.TileID.RED_BRICK:
							num8 = 131;
							break;
						case EntityID.TileID.CLAY:
							num8 = 133;
							break;
						case EntityID.TileID.BLUE_BRICK:
							num8 = 134;
							break;
						case EntityID.TileID.GREEN_BRICK:
							num8 = 137;
							break;
						case EntityID.TileID.PINK_BRICK:
							num8 = 139;
							break;
						case EntityID.TileID.GOLD_BRICK:
							num8 = 141;
							break;
						case EntityID.TileID.SILVER_BRICK:
							num8 = 143;
							break;
						case EntityID.TileID.COPPER_BRICK:
							num8 = 145;
							break;
						case EntityID.TileID.SPIKE:
							num8 = 147;
							break;
						case EntityID.TileID.WATER_CANDLE:
							num8 = 148;
							break;
						case EntityID.TileID.COBWEB:
							num8 = 150;
							break;
						case EntityID.TileID.SAND:
							num8 = 169;
							break;
						case EntityID.TileID.GLASS:
							num8 = 170;
							Main.PlaySound(13, i * 16, j * 16);
							break;
						case EntityID.TileID.OBSIDIAN:
							num8 = 173;
							break;
						case EntityID.TileID.ASH:
							num8 = 172;
							break;
						case EntityID.TileID.HELLSTONE:
							num8 = 174;
							break;
						case EntityID.TileID.MUSHROOM_GRASS:
							num8 = 176;
							break;
						case EntityID.TileID.OBSIDIAN_BRICK:
							num8 = 192;
							break;
						case EntityID.TileID.HELLSTONE_BRICK:
							num8 = 214;
							break;
						case EntityID.TileID.CLAY_POT:
							num8 = 222;
							break;
						case EntityID.TileID.CORAL:
							num8 = 275;
							break;
						case EntityID.TileID.CACTUS:
							num8 = 276;
							break;
						case EntityID.TileID.COBALT_ORE:
							num8 = 364;
							break;
						case EntityID.TileID.MYTHRIL_ORE:
							num8 = 365;
							break;
						case EntityID.TileID.ADAMANTITE_ORE:
							num8 = 366;
							break;
						case EntityID.TileID.EBONSAND:
							num8 = 370;
							break;
						case EntityID.TileID.PEARLSAND:
							num8 = 408;
							break;
						case EntityID.TileID.PEARLSTONE:
							num8 = 409;
							break;
						case EntityID.TileID.PEARLSTONE_BRICK:
							num8 = 412;
							break;
						case EntityID.TileID.IRIDESCENT_BRICK:
							num8 = 413;
							break;
						case EntityID.TileID.MUDSTONE:
							num8 = 414;
							break;
						case EntityID.TileID.COBALT_BRICK:
							num8 = 415;
							break;
						case EntityID.TileID.MYTHRIL_BRICK:
							num8 = 416;
							break;
						case EntityID.TileID.CRYSTAL_SHARD:
							num8 = 502;
							break;
						case EntityID.TileID.ACTIVE_STONE:
							num8 = 511;
							break;
						case EntityID.TileID.INACTIVE_STONE:
							num8 = 512;
							break;
						case EntityID.TileID.SWITCH:
							num8 = 538;
							break;
						case EntityID.TileID.TRAP:
							num8 = 539;
							break;
						case EntityID.TileID.EXPLOSIVES:
							num8 = 580;
							break;
						case EntityID.TileID.CANDY_CANE_RED:
							num8 = 586;
							break;
						case EntityID.TileID.CANDY_CANE_GREEN:
							num8 = 591;
							break;
						case EntityID.TileID.SNOW:
							num8 = 593;
							break;
						case EntityID.TileID.SNOW_BRICK:
							num8 = 594;
							break;
						case EntityID.TileID.PRESSURE_PLATE:
							if (ptr->FrameY == 0)
							{
								num8 = 529;
							}
							else if (ptr->FrameY == 18)
							{
								num8 = 541;
							}
							else if (ptr->FrameY == 36)
							{
								num8 = 542;
							}
							else if (ptr->FrameY == 54)
							{
								num8 = 543;
							}
							break;
						case EntityID.TileID.TIMER:
							if (ptr->FrameX == 0)
							{
								num8 = 583;
							}
							else if (ptr->FrameX == 18)
							{
								num8 = 584;
							}
							else if (ptr->FrameX == 36)
							{
								num8 = 585;
							}
							break;
						case EntityID.TileID.SHORT_JUNGLE_PLANTS:
						case EntityID.TileID.TALL_JUNGLE_PLANTS:
							if (ptr->FrameX == 144)
							{
								Item.NewItem(i * 16, j * 16, 16, 16, 331, genRand.Next(2, 4));
							}
							else if (ptr->FrameX == 162)
							{
								num8 = 223;
							}
							else if (ptr->FrameX >= 108 && ptr->FrameX <= 126 && genRand.Next(100) == 0)
							{
								num8 = 208;
							}
							else if (genRand.Next(100) == 0)
							{
								num8 = 195;
							}
							break;
						case EntityID.TileID.MUD:
						case EntityID.TileID.JUNGLE_GRASS:
							num8 = 176;
							break;
						case EntityID.TileID.GLOWING_MUSHROOM:
						case EntityID.TileID.GIANT_GLOWING_MUSHROOM:
						{
							int num10 = genRand.Next(50);
							if (num10 < 25)
							{
								num8 = ((num10 != 0) ? 183 : 194);
							}
							break;
						}
						default:
							if (ptr->Type >= 63 && ptr->Type <= 68)
							{
								num8 = ptr->Type - 63 + 177;
								break;
							}
							switch ((EntityID.TileID)type)
							{
							case EntityID.TileID.BOOK:
								num8 = ((ptr->FrameX != 90) ? 149 : 165);
								break;
							case EntityID.TileID.DAYBLOOM_MATURE:
							case EntityID.TileID.DAYBLOOM_BLOOMING:
							{
								int num9 = ptr->FrameX / 18;
								bool flag = type == 84;
								if (!flag)
								{
									if (num9 == 0 && Main.GameTime.DayTime)
									{
										flag = true;
									}
									else if (num9 == 1 && !Main.GameTime.DayTime)
									{
										flag = true;
									}
									else if (num9 == 3 && Main.GameTime.IsBloodMoon)
									{
										flag = true;
									}
								}
								if (flag)
								{
									Item.NewItem(i * 16, j * 16, 16, 16, 307 + num9, genRand.Next(1, 4));
								}
								num8 = 313 + num9;
								break;
							}
							}
							break;
						}
						if (num8 > 0)
						{
							Item.NewItem(i * 16, j * 16, 16, 16, num8, 1, DoNotBroadcast: false, -1);
						}
					}
					ptr->IsActive = 0;
					ptr->Type = 0;
					ptr->FrameX = -1;
					ptr->FrameY = -1;
					ptr->frameNumber = 0;
					if (type == 58 && j > Main.MaxTilesY - 200)
					{
						ptr->Lava = 32;
						ptr->Liquid = 128;
					}
					SquareTileFrame(i, j);
					Terraria.Mods.ModEvents.TileKilled(i, j);
					return true;
				}
			}
		}
		return false;
	}

	public unsafe static void KillTile(int i, int j, bool KillToFail, bool EffectOnly = false, bool noItem = false)
	{
		if (i < 0 || j < 0 || i >= Main.MaxTilesX || j >= Main.MaxTilesY)
		{
			return;
		}
		fixed (Tile* ptr = &Main.TileSet[i, j])
		{
			if (ptr->IsActive == 0)
			{
				return;
			}
			int type = ptr->Type;
			if (j >= 1)
			{
				Tile* ptr2 = ptr - 1;
				if (ptr2->IsActive != 0)
				{
					int type2 = ptr2->Type;
					switch ((EntityID.TileID)type2)
					{
					case EntityID.TileID.TREE:
						if (type != type2 && (ptr[-1].FrameX != 66 || ptr[-1].FrameY < 0 || ptr[-1].FrameY > 44) && (ptr[-1].FrameX != 88 || ptr[-1].FrameY < 66 || ptr[-1].FrameY > 110) && ptr[-1].FrameY < 198)
						{
							return;
						}
						break;
					case EntityID.TileID.LIFE_CRYSTAL:
					case EntityID.TileID.CHEST:
					case EntityID.TileID.DEMON_ALTAR:
					case EntityID.TileID.GIANT_GLOWING_MUSHROOM:
						if (type != type2)
						{
							return;
						}
						break;
					}
				}
			}
			if (!Gen && !EffectOnly && !stopDrops)
			{
				switch ((EntityID.TileID)type)
				{
				case EntityID.TileID.STONE:
				case EntityID.TileID.IRON_ORE:
				case EntityID.TileID.COPPER_ORE:
				case EntityID.TileID.GOLD_ORE:
				case EntityID.TileID.SILVER_ORE:
				case EntityID.TileID.DEMONITE_ORE:
				case EntityID.TileID.EBONSTONE:
				case EntityID.TileID.METEORITE:
				case EntityID.TileID.GRAY_BRICK:
				case EntityID.TileID.RED_BRICK:
				case EntityID.TileID.BLUE_BRICK:
				case EntityID.TileID.GREEN_BRICK:
				case EntityID.TileID.PINK_BRICK:
				case EntityID.TileID.GOLD_BRICK:
				case EntityID.TileID.SILVER_BRICK:
				case EntityID.TileID.COPPER_BRICK:
				case EntityID.TileID.SPIKE:
				case EntityID.TileID.OBSIDIAN:
				case EntityID.TileID.HELLSTONE:
				case EntityID.TileID.SAPPHIRE:
				case EntityID.TileID.RUBY:
				case EntityID.TileID.EMERALD:
				case EntityID.TileID.TOPAZ:
				case EntityID.TileID.AMETHYST:
				case EntityID.TileID.DIAMOND:
				case EntityID.TileID.OBSIDIAN_BRICK:
				case EntityID.TileID.HELLSTONE_BRICK:
				case EntityID.TileID.COBALT_ORE:
				case EntityID.TileID.MYTHRIL_ORE:
				case EntityID.TileID.ADAMANTITE_ORE:
				case EntityID.TileID.PEARLSTONE:
				case EntityID.TileID.PEARLSTONE_BRICK:
				case EntityID.TileID.IRIDESCENT_BRICK:
				case EntityID.TileID.MUDSTONE:
				case EntityID.TileID.COBALT_BRICK:
				case EntityID.TileID.MYTHRIL_BRICK:
				case EntityID.TileID.DEMONITE_BRICK:
					Main.PlaySound(21, i * 16, j * 16);
					break;
				case EntityID.TileID.SHORT_GRASS_PLANTS:
				case EntityID.TileID.SHORT_HALLOWED_PLANTS:
					Main.PlaySound(6, i * 16, j * 16);
					if (ptr->FrameX == 144)
					{
						Item.NewItem(i * 16, j * 16, 16, 16, 5);
					}
					break;
				case EntityID.TileID.SHORT_CORRUPT_PLANTS:
					Main.PlaySound(6, i * 16, j * 16);
					if (ptr->FrameX == 144)
					{
						Item.NewItem(i * 16, j * 16, 16, 16, 60);
					}
					break;
				case EntityID.TileID.CORRUPTION_THORN:
				case EntityID.TileID.COBWEB:
				case EntityID.TileID.VINE:
				case EntityID.TileID.SHORT_JUNGLE_PLANTS:
				case EntityID.TileID.JUNGLE_VINE:
				case EntityID.TileID.JUNGLE_THORN:
				case EntityID.TileID.GLOWING_MUSHROOM:
				case EntityID.TileID.TALL_GRASS_PLANTS:
				case EntityID.TileID.TALL_JUNGLE_PLANTS:
				case EntityID.TileID.DAYBLOOM_GROWING:
				case EntityID.TileID.DAYBLOOM_MATURE:
				case EntityID.TileID.DAYBLOOM_BLOOMING:
				case EntityID.TileID.TALL_HALLOWED_PLANTS:
				case EntityID.TileID.HALLOWED_VINE:
					Main.PlaySound(6, i * 16, j * 16);
					break;
				case EntityID.TileID.ICE:
					Main.PlaySound(2, i * 16, j * 16, 27);
					break;
				default:
					Main.PlaySound(0, i * 16, j * 16);
					if (type == 129 && !KillToFail)
					{
						Main.PlaySound(2, i * 16, j * 16, 27);
					}
					break;
				case EntityID.TileID.BOULDER:
					break;
				}
			}
			if (type != 138)
			{
				if (type == 128)
				{
					int num = i;
					int frameX = ptr->FrameX;
					if (ptr->FrameX % 100 % 36 == 18)
					{
						frameX = ptr[-1440].FrameX;
						num--;
					}
					if (frameX >= 100)
					{
						int num2 = frameX / 100;
						frameX %= 100;
						switch (Main.TileSet[num, j].FrameY / 18)
						{
						case 0:
							Item.NewItem(i * 16, j * 16, 16, 16, Item.HeadType[num2]);
							break;
						case 1:
							Item.NewItem(i * 16, j * 16, 16, 16, Item.BodyType[num2]);
							break;
						case 2:
							Item.NewItem(i * 16, j * 16, 16, 16, Item.LegType[num2]);
							break;
						}
						frameX = Main.TileSet[num, j].FrameX % 100;
						Main.TileSet[num, j].FrameX = (short)frameX;
					}
				}
				if (!Gen)
				{
					int num3 = 0;
					switch (type)
					{
					case 1:
					case 16:
					case 17:
					case 38:
					case 39:
					case 41:
					case 43:
					case 44:
					case 48:
					case 63:
					case 64:
					case 65:
					case 66:
					case 67:
					case 68:
					case 85:
					case 90:
					case 92:
					case 96:
					case 97:
					case 99:
					case 105:
					case 117:
					case 130:
					case 131:
					case 132:
					case 135:
					case 137:
					case 142:
					case 143:
					case 144:
						num3 = 1;
						break;
					case 33:
					case 95:
					case 98:
					case 100:
						num3 = 6;
						break;
					case 5:
					case 10:
					case 11:
					case 14:
					case 15:
					case 19:
					case 30:
					case 86:
					case 87:
					case 88:
					case 89:
					case 93:
					case 94:
					case 104:
					case 106:
					case 114:
					case 124:
					case 128:
					case 139:
						num3 = 7;
						break;
					case 21:
						if (ptr->FrameX >= 108)
						{
							num3 = 37;
						}
						else
						{
							num3 = ((ptr->FrameX < 36) ? 7 : 10);
						}
						break;
					case 127:
						num3 = 67;
						break;
					case 91:
						num3 = -1;
						break;
					case 6:
					case 26:
						num3 = 8;
						break;
					case 7:
					case 34:
					case 47:
						num3 = 9;
						break;
					case 8:
					case 36:
					case 45:
					case 102:
						num3 = 10;
						break;
					case 9:
					case 35:
					case 42:
					case 46:
					case 126:
					case 136:
						num3 = 11;
						break;
					case 12:
						num3 = 12;
						break;
					case 3:
					case 52:
					case 73:
						num3 = 3;
						break;
					case 13:
					case 54:
						num3 = 13;
						break;
					case 22:
					case 112:
					case 140:
						num3 = 14;
						break;
					case 28:
					case 50:
					case 78:
						num3 = 22;
						break;
					case 29:
						num3 = 23;
						break;
					case 40:
					case 103:
						num3 = 28;
						break;
					case 49:
						num3 = 29;
						break;
					case 51:
						num3 = 30;
						break;
					case 53:
					case 81:
						num3 = 32;
						break;
					case 56:
					case 75:
						num3 = 37;
						break;
					case 57:
					case 119:
					case 141:
						num3 = 36;
						break;
					case 59:
					case 120:
						num3 = 38;
						break;
					case 61:
					case 62:
					case 74:
					case 80:
						num3 = 40;
						break;
					case 69:
						num3 = 7;
						break;
					case 71:
					case 72:
						num3 = 26;
						break;
					case 70:
						num3 = 17;
						break;
					case 123:
						num3 = 53;
						break;
					case 116:
					case 118:
					case 147:
					case 148:
						num3 = 51;
						break;
					case 110:
					case 113:
					case 115:
						num3 = 47;
						break;
					case 107:
					case 121:
						num3 = 48;
						break;
					case 108:
					case 122:
					case 134:
					case 146:
					case 149:
						num3 = 49;
						break;
					case 111:
					case 133:
					case 145:
						num3 = 50;
						break;
					case 82:
					case 83:
					case 84:
						switch (ptr->FrameX / 18)
						{
						case 0:
							num3 = 3;
							break;
						case 1:
							num3 = 3;
							break;
						case 2:
							num3 = 7;
							break;
						case 3:
							num3 = 17;
							break;
						case 4:
							num3 = 3;
							break;
						case 5:
							num3 = 6;
							break;
						}
						break;
					default:
						switch ((EntityID.TileID)type)
						{
						case EntityID.TileID.CRYSTAL_SHARD:
							if (ptr->FrameX != 0 && ptr->FrameX != 54 && ptr->FrameX != 108)
							{
								num3 = ((ptr->FrameX != 18 && ptr->FrameX != 72 && ptr->FrameX != 126) ? 70 : 69);
							}
							else
							{
								num3 = 68;
							}
							break;
						case EntityID.TileID.TORCH:
						{
							int num4 = ptr->FrameY / 22;
							if (1 == 0)
							{
							}
							int num5 = num4 switch
							{
								0 => 6, 
								8 => 75, 
								_ => 58 + num4, 
							};
							if (1 == 0)
							{
							}
							num3 = num5;
							break;
						}
						}
						break;
					case 0:
						break;
					}
					if (num3 >= 0)
					{
						for (int num6 = (KillToFail ? 1 : 4); num6 >= 0; num6--)
						{
							switch ((EntityID.TileID)type)
							{
							case EntityID.TileID.GRASS:
								num3 = genRand.Next(2) << 1;
								break;
							case EntityID.TileID.SAPLING:
								num3 = ((genRand.Next(2) == 0) ? 7 : 2);
								break;
							case EntityID.TileID.CORRUPT_GRASS:
							case EntityID.TileID.SHORT_CORRUPT_PLANTS:
								num3 = ((genRand.Next(2) == 0) ? 14 : 17);
								break;
							case EntityID.TileID.SUNFLOWER:
								num3 = ((genRand.Next(2) == 0) ? 3 : 19);
								break;
							case EntityID.TileID.EBONSTONE:
							case EntityID.TileID.SHADOW_ORB:
								num3 = ((genRand.Next(2) != 0) ? 1 : 14);
								break;
							case EntityID.TileID.CORRUPTION_THORN:
								num3 = ((genRand.Next(2) == 0) ? 14 : 24);
								break;
							case EntityID.TileID.CHANDELIER:
							case EntityID.TileID.JACK_O_LANTERN:
							case EntityID.TileID.PRESENT:
							case EntityID.TileID.CHAIN_LANTERN:
								num3 = genRand.Next(2) * 6;
								break;
							case EntityID.TileID.METEORITE:
								num3 = ((genRand.Next(2) == 0) ? 6 : 23);
								break;
							case EntityID.TileID.SHORT_JUNGLE_PLANTS:
								num3 = 38 + genRand.Next(2);
								break;
							case EntityID.TileID.HELLSTONE:
							case EntityID.TileID.HELLSTONE_BRICK:
							case EntityID.TileID.HELLFORGE:
								num3 = ((genRand.Next(2) == 0) ? 6 : 25);
								break;
							case EntityID.TileID.HALLOWED_GRASS:
								num3 = genRand.Next(2) * 47;
								break;
							}
							Main.DustSet.NewDust(i * 16, j * 16, 16, 16, num3);
						}
					}
				}
			}
			if (EffectOnly)
			{
				return;
			}
			if (KillToFail)
			{
				switch ((EntityID.TileID)type)
				{
				case EntityID.TileID.GRASS:
				case EntityID.TileID.CORRUPT_GRASS:
				case EntityID.TileID.HALLOWED_GRASS:
					ptr->Type = 0;
					break;
				case EntityID.TileID.JUNGLE_GRASS:
				case EntityID.TileID.MUSHROOM_GRASS:
					ptr->Type = 59;
					break;
				}
				SquareTileFrame(i, j);
				return;
			}
			if (type == 21 && Main.NetMode != 1)
			{
				int num7 = ptr->FrameX / 18;
				int y = j - ptr->FrameY / 18;
				num7 = i - (num7 & 1);
				if (!Chest.DestroyChest(num7, y))
				{
					return;
				}
			}
			if (!noItem && !Gen && !stopDrops && Main.NetMode != 1)
			{
				int num8 = 0;
				switch ((EntityID.TileID)type)
				{
				case EntityID.TileID.DIRT:
				case EntityID.TileID.GRASS:
				case EntityID.TileID.HALLOWED_GRASS:
					num8 = 2;
					break;
				case EntityID.TileID.STONE:
					num8 = 3;
					break;
				case EntityID.TileID.SHORT_GRASS_PLANTS:
				case EntityID.TileID.TALL_GRASS_PLANTS:
					if (Main.Rand.Next(2) == 0)
					{
						Rectangle rect = default;
						rect.X = i << 4;
						rect.Y = j << 4;
						rect.Width = (rect.Height = 16);
						if (Player.FindClosest(ref rect).HasItem(281))
						{
							num8 = 283;
						}
					}
					break;
				case EntityID.TileID.TORCH:
				{
					int num11 = ptr->FrameY / 22;
					if (1 == 0)
					{
					}
					int num5 = num11 switch
					{
						0 => 8, 
						8 => 523, 
						_ => 426 + num11, 
					};
					if (1 == 0)
					{
					}
					num8 = num5;
					break;
				}
				case EntityID.TileID.TREE:
					if (ptr->FrameX >= 22 && ptr->FrameY >= 198)
					{
						if (Main.NetMode != 1)
						{
							num8 = 9;
							if (genRand.Next(2) == 0)
							{
								int num12 = j - 1;
								int type3;
								do
								{
									type3 = Main.TileSet[i, ++num12].Type;
								}
								while (!Main.TileSolidNotSolidTop[type3] || Main.TileSet[i, num12].IsActive == 0);
								if (type3 == 2 || type3 == 109)
								{
									num8 = 27;
								}
							}
						}
					}
					else
					{
						num8 = 9;
					}
					woodSpawned++;
					break;
				case EntityID.TileID.IRON_ORE:
					num8 = 11;
					break;
				case EntityID.TileID.COPPER_ORE:
					num8 = 12;
					break;
				case EntityID.TileID.GOLD_ORE:
					num8 = 13;
					break;
				case EntityID.TileID.SILVER_ORE:
					num8 = 14;
					break;
				case EntityID.TileID.SILT:
					num8 = 424;
					break;
				case EntityID.TileID.WOODEN_BEAM:
					num8 = 480;
					break;
				case EntityID.TileID.XMAS_LIGHT:
					if (ptr->FrameX == 0 || ptr->FrameX == 54)
					{
						num8 = 596;
					}
					else if (ptr->FrameX == 18 || ptr->FrameX == 72)
					{
						num8 = 597;
					}
					else if (ptr->FrameX == 36 || ptr->FrameX == 90)
					{
						num8 = 598;
					}
					break;
				case EntityID.TileID.BOTTLE:
				{
					Main.PlaySound(13, i * 16, j * 16);
					short frameX2 = ptr->FrameX;
					if (1 == 0)
					{
					}
					int num5 = frameX2 switch
					{
						18 => 28, 
						36 => 110, 
						54 => 350, 
						72 => 351, 
						_ => 31, 
					};
					if (1 == 0)
					{
					}
					num8 = num5;
					break;
				}
				case EntityID.TileID.PLATFORM:
					num8 = 94;
					break;
				case EntityID.TileID.DEMONITE_ORE:
					num8 = 56;
					break;
				case EntityID.TileID.DEMONITE_BRICK:
					num8 = 577;
					break;
				case EntityID.TileID.CORRUPT_GRASS:
					num8 = 2;
					break;
				case EntityID.TileID.EBONSTONE:
					num8 = 61;
					break;
				case EntityID.TileID.WOOD:
					num8 = 9;
					break;
				case EntityID.TileID.CANDLE:
					num8 = 105;
					break;
				case EntityID.TileID.METEORITE:
					num8 = 116;
					break;
				case EntityID.TileID.GRAY_BRICK:
					num8 = 129;
					break;
				case EntityID.TileID.RED_BRICK:
					num8 = 131;
					break;
				case EntityID.TileID.CLAY:
					num8 = 133;
					break;
				case EntityID.TileID.BLUE_BRICK:
					num8 = 134;
					break;
				case EntityID.TileID.GREEN_BRICK:
					num8 = 137;
					break;
				case EntityID.TileID.PINK_BRICK:
					num8 = 139;
					break;
				case EntityID.TileID.GOLD_BRICK:
					num8 = 141;
					break;
				case EntityID.TileID.SILVER_BRICK:
					num8 = 143;
					break;
				case EntityID.TileID.COPPER_BRICK:
					num8 = 145;
					break;
				case EntityID.TileID.SPIKE:
					num8 = 147;
					break;
				case EntityID.TileID.WATER_CANDLE:
					num8 = 148;
					break;
				case EntityID.TileID.COBWEB:
					num8 = 150;
					break;
				case EntityID.TileID.SAND:
					num8 = 169;
					break;
				case EntityID.TileID.GLASS:
					num8 = 170;
					Main.PlaySound(13, i * 16, j * 16);
					break;
				case EntityID.TileID.OBSIDIAN:
					num8 = 173;
					break;
				case EntityID.TileID.ASH:
					num8 = 172;
					break;
				case EntityID.TileID.HELLSTONE:
					num8 = 174;
					break;
				case EntityID.TileID.MUSHROOM_GRASS:
					num8 = 176;
					break;
				case EntityID.TileID.OBSIDIAN_BRICK:
					num8 = 192;
					break;
				case EntityID.TileID.HELLSTONE_BRICK:
					num8 = 214;
					break;
				case EntityID.TileID.CLAY_POT:
					num8 = 222;
					break;
				case EntityID.TileID.CORAL:
					num8 = 275;
					break;
				case EntityID.TileID.CACTUS:
					num8 = 276;
					break;
				case EntityID.TileID.COBALT_ORE:
					num8 = 364;
					break;
				case EntityID.TileID.MYTHRIL_ORE:
					num8 = 365;
					break;
				case EntityID.TileID.ADAMANTITE_ORE:
					num8 = 366;
					break;
				case EntityID.TileID.EBONSAND:
					num8 = 370;
					break;
				case EntityID.TileID.PEARLSAND:
					num8 = 408;
					break;
				case EntityID.TileID.PEARLSTONE:
					num8 = 409;
					break;
				case EntityID.TileID.PEARLSTONE_BRICK:
					num8 = 412;
					break;
				case EntityID.TileID.IRIDESCENT_BRICK:
					num8 = 413;
					break;
				case EntityID.TileID.MUDSTONE:
					num8 = 414;
					break;
				case EntityID.TileID.COBALT_BRICK:
					num8 = 415;
					break;
				case EntityID.TileID.MYTHRIL_BRICK:
					num8 = 416;
					break;
				case EntityID.TileID.CRYSTAL_SHARD:
					num8 = 502;
					break;
				case EntityID.TileID.ACTIVE_STONE:
					num8 = 511;
					break;
				case EntityID.TileID.INACTIVE_STONE:
					num8 = 512;
					break;
				case EntityID.TileID.SWITCH:
					num8 = 538;
					break;
				case EntityID.TileID.TRAP:
					num8 = 539;
					break;
				case EntityID.TileID.EXPLOSIVES:
					num8 = 580;
					break;
				case EntityID.TileID.CANDY_CANE_RED:
					num8 = 586;
					break;
				case EntityID.TileID.CANDY_CANE_GREEN:
					num8 = 591;
					break;
				case EntityID.TileID.SNOW:
					num8 = 593;
					break;
				case EntityID.TileID.SNOW_BRICK:
					num8 = 594;
					break;
				case EntityID.TileID.PRESSURE_PLATE:
					if (ptr->FrameY == 0)
					{
						num8 = 529;
					}
					else if (ptr->FrameY == 18)
					{
						num8 = 541;
					}
					else if (ptr->FrameY == 36)
					{
						num8 = 542;
					}
					else if (ptr->FrameY == 54)
					{
						num8 = 543;
					}
					break;
				case EntityID.TileID.TIMER:
					if (ptr->FrameX == 0)
					{
						num8 = 583;
					}
					else if (ptr->FrameX == 18)
					{
						num8 = 584;
					}
					else if (ptr->FrameX == 36)
					{
						num8 = 585;
					}
					break;
				case EntityID.TileID.SHORT_JUNGLE_PLANTS:
				case EntityID.TileID.TALL_JUNGLE_PLANTS:
					if (ptr->FrameX == 144)
					{
						Item.NewItem(i * 16, j * 16, 16, 16, 331, genRand.Next(2, 4));
					}
					else if (ptr->FrameX == 162)
					{
						num8 = 223;
					}
					else if (ptr->FrameX >= 108 && ptr->FrameX <= 126 && genRand.Next(100) == 0)
					{
						num8 = 208;
					}
					else if (genRand.Next(100) == 0)
					{
						num8 = 195;
					}
					break;
				case EntityID.TileID.MUD:
				case EntityID.TileID.JUNGLE_GRASS:
					num8 = 176;
					break;
				case EntityID.TileID.GLOWING_MUSHROOM:
				case EntityID.TileID.GIANT_GLOWING_MUSHROOM:
				{
					int num10 = genRand.Next(50);
					if (num10 < 25)
					{
						num8 = ((num10 != 0) ? 183 : 194);
					}
					break;
				}
				default:
					if (ptr->Type >= 63 && ptr->Type <= 68)
					{
						num8 = ptr->Type - 63 + 177;
						break;
					}
					switch ((EntityID.TileID)type)
					{
					case EntityID.TileID.BOOK:
						num8 = ((ptr->FrameX != 90) ? 149 : 165);
						break;
					case EntityID.TileID.DAYBLOOM_MATURE:
					case EntityID.TileID.DAYBLOOM_BLOOMING:
					{
						int num9 = ptr->FrameX / 18;
						bool flag = type == 84;
						if (!flag)
						{
							if (num9 == 0 && Main.GameTime.DayTime)
							{
								flag = true;
							}
							else if (num9 == 1 && !Main.GameTime.DayTime)
							{
								flag = true;
							}
							else if (num9 == 3 && Main.GameTime.IsBloodMoon)
							{
								flag = true;
							}
						}
						if (flag)
						{
							Item.NewItem(i * 16, j * 16, 16, 16, 307 + num9, genRand.Next(1, 4));
						}
						num8 = 313 + num9;
						break;
					}
					}
					break;
				}
				if (num8 > 0)
				{
					Item.NewItem(i * 16, j * 16, 16, 16, num8, 1, DoNotBroadcast: false, -1);
				}
			}
			ptr->IsActive = 0;
			ptr->Type = 0;
			ptr->FrameX = -1;
			ptr->FrameY = -1;
			ptr->frameNumber = 0;
			if (type == 58 && j > Main.MaxTilesY - 200)
			{
				ptr->Lava = 32;
				ptr->Liquid = 128;
			}
			SquareTileFrame(i, j);
			// Reached only when the tile was really removed: EffectOnly returns earlier.
			Terraria.Mods.ModEvents.TileKilled(i, j);
		}
	}

	public static bool PlayerLOS(int x, int y)
	{
		Rectangle rectangle = new Rectangle(x * 16, y * 16, 16, 16);
		Rectangle value = default;
		bool result = false;
		for (int i = 0; i < 8; i++)
		{
			if (Main.PlayerSet[i].Active != 0)
			{
				value.X = (int)((double)Main.PlayerSet[i].Position.X + 10.0 - 1152.0);
				value.Y = (int)((double)Main.PlayerSet[i].Position.Y + 21.0 - 648.0);
				value.Width = 2304;
				value.Height = 1296;
				rectangle.Intersects(ref value, out result);
				if (result)
				{
					break;
				}
			}
		}
		return result;
	}

	public static void hardUpdateWorld(int i, int j)
	{
		int type = Main.TileSet[i, j].Type;
		if (type == 117 && j > Main.RockLayer && Main.Rand.Next(110) == 0)
		{
			int num = genRand.Next(4);
			int num2 = 0;
			int num3 = 0;
			switch (num)
			{
			case 0:
				num2 = -1;
				break;
			case 1:
				num2 = 1;
				break;
			default:
				num3 = ((num != 0) ? 1 : (-1));
				break;
			}
			if (Main.TileSet[i + num2, j + num3].IsActive == 0)
			{
				int num4 = 0;
				int num5 = 6;
				for (int k = i - num5; k <= i + num5; k++)
				{
					for (int l = j - num5; l <= j + num5; l++)
					{
						if (Main.TileSet[k, l].IsActive != 0 && Main.TileSet[k, l].Type == 129)
						{
							num4++;
						}
					}
				}
				if (num4 < 2)
				{
					PlaceTile(i + num2, j + num3, 129, ToMute: true);
					NetMessage.SendTile(i + num2, j + num3);
				}
			}
		}
		else if (type == 23 || type == 25 || type == 32 || type == 112)
		{
			while (true)
			{
				int num6 = i + genRand.Next(-3, 4);
				int num7 = j + genRand.Next(-3, 4);
				switch ((EntityID.TileID)Main.TileSet[num6, num7].Type)
				{
				default:
					return;
				case EntityID.TileID.GRASS:
					Main.TileSet[num6, num7].Type = 23;
					SquareTileFrame(num6, num7);
					NetMessage.SendTile(num6, num7);
					if (genRand.Next(2) != 0)
					{
						return;
					}
					break;
				case EntityID.TileID.STONE:
					Main.TileSet[num6, num7].Type = 25;
					SquareTileFrame(num6, num7);
					NetMessage.SendTile(num6, num7);
					if (genRand.Next(2) != 0)
					{
						return;
					}
					break;
				case EntityID.TileID.SAND:
					Main.TileSet[num6, num7].Type = 112;
					SquareTileFrame(num6, num7);
					NetMessage.SendTile(num6, num7);
					if (genRand.Next(2) != 0)
					{
						return;
					}
					break;
				case EntityID.TileID.MUD:
					Main.TileSet[num6, num7].Type = 0;
					SquareTileFrame(num6, num7);
					NetMessage.SendTile(num6, num7);
					if (genRand.Next(2) != 0)
					{
						return;
					}
					break;
				case EntityID.TileID.JUNGLE_GRASS:
					Main.TileSet[num6, num7].Type = 23;
					SquareTileFrame(num6, num7);
					NetMessage.SendTile(num6, num7);
					if (genRand.Next(2) != 0)
					{
						return;
					}
					break;
				case EntityID.TileID.JUNGLE_THORN:
					Main.TileSet[num6, num7].Type = 32;
					SquareTileFrame(num6, num7);
					NetMessage.SendTile(num6, num7);
					if (genRand.Next(2) != 0)
					{
						return;
					}
					break;
				}
			}
		}
		switch ((EntityID.TileID)type)
		{
		default:
			return;
		case EntityID.TileID.ADAMANTITE_ORE:
		case EntityID.TileID.EBONSAND:
		case EntityID.TileID.TINKERERS_WORKSHOP:
			return;
		case EntityID.TileID.HALLOWED_GRASS:
		case EntityID.TileID.SHORT_HALLOWED_PLANTS:
		case EntityID.TileID.TALL_HALLOWED_PLANTS:
		case EntityID.TileID.HALLOWED_VINE:
		case EntityID.TileID.PEARLSAND:
		case EntityID.TileID.PEARLSTONE:
		case EntityID.TileID.PEARLSTONE_BRICK:
			break;
		}
		while (true)
		{
			int num8 = i + genRand.Next(-3, 4);
			int num9 = j + genRand.Next(-3, 4);
			switch ((EntityID.TileID)Main.TileSet[num8, num9].Type)
			{
			default:
				return;
			case EntityID.TileID.GRASS:
				Main.TileSet[num8, num9].Type = 109;
				SquareTileFrame(num8, num9);
				NetMessage.SendTile(num8, num9);
				if (genRand.Next(2) != 0)
				{
					return;
				}
				break;
			case EntityID.TileID.STONE:
				Main.TileSet[num8, num9].Type = 117;
				SquareTileFrame(num8, num9);
				NetMessage.SendTile(num8, num9);
				if (genRand.Next(2) != 0)
				{
					return;
				}
				break;
			case EntityID.TileID.SAND:
				Main.TileSet[num8, num9].Type = 116;
				SquareTileFrame(num8, num9);
				NetMessage.SendTile(num8, num9);
				if (genRand.Next(2) != 0)
				{
					return;
				}
				break;
			}
		}
	}

	public static bool SolidTile(int i, int j)
	{
		try
		{
			return Main.TileSet[i, j].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[i, j].Type];
		}
		catch
		{
			return false;
		}
	}

	public static bool SolidTileUnsafe(int i, int j)
	{
		if (Main.TileSet[i, j].IsActive != 0)
		{
			return Main.TileSolidNotSolidTop[Main.TileSet[i, j].Type];
		}
		return false;
	}

	public static bool CanStandOnTop(int i, int j)
	{
		try
		{
			return Main.TileSet[i, j].CanStandOnTop();
		}
		catch
		{
			return false;
		}
	}

	public unsafe static void MineHouse(int i, int j)
	{
		if (i < 50 || i > Main.MaxTilesX - 50 || j < 50 || j > Main.MaxTilesY - 50 || SolidTileUnsafe(i, j) || Main.TileSet[i, j].WallType > 0)
		{
			return;
		}
		int num = genRand.Next(6, 12);
		int num2 = genRand.Next(3, 6);
		int num3 = genRand.Next(15, 30);
		int num4 = genRand.Next(15, 30);
		int num5 = j - num;
		int num6 = j + num2;
		for (int k = 0; k < 2; k++)
		{
			int num7 = i;
			int num8 = j;
			int num9 = -1;
			int num10 = num3;
			if (k == 1)
			{
				num9 = 1;
				num10 = num4;
				num7++;
			}
			do
			{
				if (num8 - num < num5)
				{
					num5 = num8 - num;
				}
				if (num8 + num2 > num6)
				{
					num6 = num8 + num2;
				}
				for (int l = 0; l < 2; l++)
				{
					int num11 = num8;
					int num12 = num;
					int num13 = -1;
					if (l == 1)
					{
						num11++;
						num12 = num2;
						num13 = 1;
					}
					bool flag = true;
					do
					{
						if (num7 != i)
						{
							fixed (Tile* ptr = &Main.TileSet[num7 - num9, num11])
							{
								if (ptr->WallType != 27 && (ptr->IsActive == 0 || Main.TileSolidNotSolidTop[ptr->Type]))
								{
									ptr->IsActive = 1;
									ptr->Type = 30;
								}
							}
						}
						if (SolidTileUnsafe(num7 - 1, num11))
						{
							Main.TileSet[num7 - 1, num11].Type = 30;
						}
						if (SolidTileUnsafe(num7 + 1, num11))
						{
							Main.TileSet[num7 + 1, num11].Type = 30;
						}
						if (SolidTileUnsafe(num7, num11))
						{
							int num14 = 0;
							if (SolidTileUnsafe(num7 - 1, num11))
							{
								num14 = 1;
							}
							if (SolidTileUnsafe(num7 + 1, num11))
							{
								num14++;
							}
							if (SolidTileUnsafe(num7, num11 - 1))
							{
								num14++;
							}
							if (SolidTileUnsafe(num7, num11 + 1))
							{
								num14++;
							}
							if (num14 < 2)
							{
								Main.TileSet[num7, num11].IsActive = 0;
							}
							else
							{
								flag = false;
								Main.TileSet[num7, num11].Type = 30;
							}
						}
						else
						{
							Main.TileSet[num7, num11].WallType = 27;
							Main.TileSet[num7, num11].Liquid = 0;
							Main.TileSet[num7, num11].Lava = 0;
						}
						num11 += num13;
						if (--num12 <= 0)
						{
							if (Main.TileSet[num7, num11].IsActive == 0)
							{
								Main.TileSet[num7, num11].IsActive = 1;
								Main.TileSet[num7, num11].Type = 30;
							}
							break;
						}
					}
					while (flag);
				}
				num10--;
				num7 += num9;
				if (!SolidTileUnsafe(num7, num8))
				{
					continue;
				}
				int num15 = 0;
				int num16 = 0;
				int num17 = num8;
				do
				{
					num17--;
					num15++;
					if (SolidTileUnsafe(num7 - num9, num17))
					{
						num15 = 999;
						break;
					}
				}
				while (SolidTileUnsafe(num7, num17));
				num17 = num8;
				do
				{
					num17++;
					num16++;
					if (SolidTileUnsafe(num7 - num9, num17))
					{
						num16 = 999;
						break;
					}
				}
				while (SolidTileUnsafe(num7, num17));
				if (num16 <= num15)
				{
					if (num16 > num2)
					{
						num10 = 0;
					}
					else
					{
						num8 += num16 + 1;
					}
				}
				else if (num15 > num)
				{
					num10 = 0;
				}
				else
				{
					num8 -= num15 + 1;
				}
			}
			while (num10 > 0);
		}
		int num18 = i - num3 - 1;
		int num19 = i + num4 + 2;
		int num20 = num5 - 1;
		int num21 = num6 + 2;
		for (int m = num18; m < num19; m++)
		{
			for (int n = num20; n < num21; n++)
			{
				if (Main.TileSet[m, n].WallType == 27 && Main.TileSet[m, n].IsActive == 0)
				{
					if (Main.TileSet[m - 1, n].WallType != 27 && m < i && !SolidTileUnsafe(m - 1, n))
					{
						PlaceTile(m, n, 30, ToMute: true);
						Main.TileSet[m, n].WallType = 0;
					}
					if (Main.TileSet[m + 1, n].WallType != 27 && m > i && !SolidTileUnsafe(m + 1, n))
					{
						PlaceTile(m, n, 30, ToMute: true);
						Main.TileSet[m, n].WallType = 0;
					}
					for (int num22 = m - 1; num22 <= m + 1; num22++)
					{
						for (int num23 = n - 1; num23 <= n + 1; num23++)
						{
							if (SolidTileUnsafe(num22, num23))
							{
								Main.TileSet[num22, num23].Type = 30;
							}
						}
					}
				}
				if (Main.TileSet[m, n].Type == 30 && Main.TileSet[m - 1, n].WallType == 27 && Main.TileSet[m + 1, n].WallType == 27 && (Main.TileSet[m, n - 1].WallType == 27 || Main.TileSet[m, n - 1].IsActive != 0) && (Main.TileSet[m, n + 1].WallType == 27 || Main.TileSet[m, n + 1].IsActive != 0))
				{
					Main.TileSet[m, n].IsActive = 0;
					Main.TileSet[m, n].WallType = 27;
				}
			}
		}
		for (int num24 = num18; num24 < num19; num24++)
		{
			for (int num25 = num20; num25 < num21; num25++)
			{
				if (Main.TileSet[num24, num25].Type == 30)
				{
					if (Main.TileSet[num24 - 1, num25].WallType == 27 && Main.TileSet[num24 + 1, num25].WallType == 27 && Main.TileSet[num24 - 1, num25].IsActive == 0 && Main.TileSet[num24 + 1, num25].IsActive == 0)
					{
						Main.TileSet[num24, num25].IsActive = 0;
						Main.TileSet[num24, num25].WallType = 27;
					}
					if (Main.TileSet[num24, num25 - 1].Type != 21 && Main.TileSet[num24 - 1, num25].WallType == 27 && Main.TileSet[num24 + 1, num25].Type == 30 && Main.TileSet[num24 + 2, num25].WallType == 27 && Main.TileSet[num24 - 1, num25].IsActive == 0 && Main.TileSet[num24 + 2, num25].IsActive == 0)
					{
						Main.TileSet[num24, num25].IsActive = 0;
						Main.TileSet[num24, num25].WallType = 27;
						Main.TileSet[num24 + 1, num25].IsActive = 0;
						Main.TileSet[num24 + 1, num25].WallType = 27;
					}
					if (Main.TileSet[num24, num25 - 1].WallType == 27 && Main.TileSet[num24, num25 + 1].WallType == 27 && Main.TileSet[num24, num25 - 1].IsActive == 0 && Main.TileSet[num24, num25 + 1].IsActive == 0)
					{
						Main.TileSet[num24, num25].IsActive = 0;
						Main.TileSet[num24, num25].WallType = 27;
					}
				}
			}
		}
		for (int num26 = num18; num26 < num19; num26++)
		{
			for (int num27 = num21; num27 > num20; num27--)
			{
				bool flag2 = false;
				if (Main.TileSet[num26, num27].IsActive != 0 && Main.TileSet[num26, num27].Type == 30)
				{
					int num28 = -1;
					for (int num29 = 0; num29 < 2; num29++)
					{
						if (!SolidTileUnsafe(num26 + num28, num27) && Main.TileSet[num26 + num28, num27].WallType == 0)
						{
							int num30 = 0;
							int num31 = num27;
							int num32 = num27;
							while (Main.TileSet[num26, num31].IsActive != 0 && Main.TileSet[num26, num31].Type == 30 && !SolidTileUnsafe(num26 + num28, num31) && Main.TileSet[num26 + num28, num31].WallType == 0)
							{
								num31--;
								num30++;
							}
							num31++;
							int num33 = num31 + 1;
							if (num30 > 4)
							{
								if (genRand.Next(2) == 0)
								{
									num31 = num32 - 1;
									bool flag3 = true;
									for (int num34 = num26 - 2; num34 <= num26 + 2; num34++)
									{
										for (int num35 = num31 - 2; num35 <= num31; num35++)
										{
											if (num34 != num26 && Main.TileSet[num34, num35].IsActive != 0)
											{
												flag3 = false;
											}
										}
									}
									if (flag3)
									{
										Main.TileSet[num26, num31].IsActive = 0;
										Main.TileSet[num26, num31 - 1].IsActive = 0;
										Main.TileSet[num26, num31 - 2].IsActive = 0;
										PlaceTile(num26, num31, 10, ToMute: true);
										flag2 = true;
									}
								}
								if (!flag2)
								{
									for (int num36 = num33; num36 < num32; num36++)
									{
										Main.TileSet[num26, num36].Type = 124;
									}
								}
							}
						}
						num28 = 1;
					}
				}
				if (flag2)
				{
					break;
				}
			}
		}
		int num37;
		for (num37 = num18; num37 < num19; num37++)
		{
			bool flag4 = true;
			for (int num38 = num20; num38 < num21; num38++)
			{
				for (int num39 = num37 - 2; num39 <= num37 + 2; num39++)
				{
					if (Main.TileSet[num39, num38].IsActive != 0 && (!SolidTileUnsafe(num39, num38) || Main.TileSet[num39, num38].Type == 10))
					{
						flag4 = false;
					}
				}
			}
			if (flag4)
			{
				for (int num40 = num20; num40 < num21; num40++)
				{
					if (Main.TileSet[num37, num40].WallType == 27 && Main.TileSet[num37, num40].IsActive == 0)
					{
						PlaceTile(num37, num40, 124, ToMute: true);
					}
				}
			}
			num37 += genRand.Next(3);
		}
		for (int num41 = 0; num41 < 4; num41++)
		{
			int num42 = genRand.Next(num18 + 2, num19 - 1);
			int num43 = genRand.Next(num20 + 2, num21 - 1);
			while (Main.TileSet[num42, num43].WallType != 27)
			{
				num42 = genRand.Next(num18 + 2, num19 - 1);
				num43 = genRand.Next(num20 + 2, num21 - 1);
			}
			while (Main.TileSet[num42, num43].IsActive != 0)
			{
				num43--;
			}
			for (; Main.TileSet[num42, num43].IsActive == 0; num43++)
			{
			}
			num43--;
			if (Main.TileSet[num42, num43].WallType != 27)
			{
				continue;
			}
			if (genRand.Next(3) == 0)
			{
				int i2 = num42;
				int j2 = num43;
				int num44 = genRand.Next(9);
				if (1 == 0)
				{
				}
				int type = num44 switch
				{
					0 => 14, 
					1 => 16, 
					2 => 18, 
					3 => 86, 
					4 => 87, 
					5 => 94, 
					6 => 101, 
					7 => 104, 
					_ => 106, 
				};
				if (1 == 0)
				{
				}
				PlaceTile(i2, j2, type, ToMute: true);
			}
			else
			{
				int style = genRand.Next(2, 43);
				PlaceTile(num42, num43, 105, ToMute: true, IsForced: true, -1, style);
			}
		}
	}

	public static void CountTiles(int X)
	{
		if (X == 0)
		{
			totalEvil = totalEvil2;
			totalSolid = totalSolid2;
			totalGood = totalGood2;
			if (!Gen)
			{
				if (totalSolid > 0)
				{
					GoodCoverage = (byte)Math.Round((float)(totalGood * 100) / (float)totalSolid);
					EvilCoverage = (byte)Math.Round((float)(totalEvil * 100) / (float)totalSolid);
					if ((float)(int)EvilCoverage >= 50f)
					{
						UI.SetTriggerStateForAll(Trigger.CorruptedWorld);
					}
					if ((float)(int)GoodCoverage >= 50f)
					{
						UI.SetTriggerStateForAll(Trigger.HallowedWorld);
					}
				}
				else
				{
					GoodCoverage = 0;
					EvilCoverage = 0;
				}
				NetMessage.CreateMessage0(57);
				NetMessage.SendMessage();
			}
			totalEvil2 = 0;
			totalSolid2 = 0;
			totalGood2 = 0;
		}
		int worldSurface = Main.WorldSurface;
		int num = Main.MaxTilesY - 1;
		do
		{
			if (SolidTileUnsafe(X, num))
			{
				switch ((EntityID.TileID)Main.TileSet[X, num].Type)
				{
				case EntityID.TileID.HALLOWED_GRASS:
				case EntityID.TileID.PEARLSAND:
				case EntityID.TileID.PEARLSTONE:
					totalGood2++;
					break;
				case EntityID.TileID.CORRUPT_GRASS:
				case EntityID.TileID.EBONSTONE:
				case EntityID.TileID.EBONSAND:
					totalEvil2++;
					break;
				}
				totalSolid2++;
			}
		}
		while (--num > worldSurface);
		do
		{
			if (SolidTileUnsafe(X, num))
			{
				switch ((EntityID.TileID)Main.TileSet[X, num].Type)
				{
				case EntityID.TileID.HALLOWED_GRASS:
				case EntityID.TileID.PEARLSAND:
				case EntityID.TileID.PEARLSTONE:
					totalGood2 += 5;
					break;
				case EntityID.TileID.CORRUPT_GRASS:
				case EntityID.TileID.EBONSTONE:
				case EntityID.TileID.EBONSAND:
					totalEvil2 += 5;
					break;
				}
				totalSolid2 += 5;
			}
		}
		while (--num >= 0);
	}

	public static void UpdateWorld()
	{
		UpdateSand();
		UpdateMech();
		if ((++Liquid.SkipCount & 1) == 0)
		{
			Liquid.UpdateLiquid();
		}
		if (hardLock)
		{
			return;
		}
		if ((++totalD & 0xF) == 0)
		{
			CountTiles(totalX);
			if (++totalX >= Main.MaxTilesX)
			{
				totalX = 0;
			}
		}
		bool flag = false;
		if (Main.InvasionType > 0)
		{
			spawnDelay = 0;
		}
		if (++spawnDelay >= 20)
		{
			flag = true;
			spawnDelay = 0;
			if (ToSpawnNPC != 37)
			{
				for (int i = 0; i < 196; i++)
				{
					if (Main.NPCSet[i].Active != 0 && Main.NPCSet[i].IsHomeless && Main.NPCSet[i].IsTownNPC)
					{
						ToSpawnNPC = Main.NPCSet[i].Type;
						break;
					}
				}
			}
		}
		float num = 3E-05f;
		float num2 = 1.5E-05f;
		for (int j = 0; (float)j < (float)(Main.MaxTilesX * Main.MaxTilesY) * num; j++)
		{
			int num3 = genRand.Next(10, Main.MaxTilesX - 10);
			int num4 = genRand.Next(10, Main.WorldSurface - 1);
			int num5 = num3 - 1;
			int num6 = num3 + 2;
			int num7 = num4 - 1;
			int num8 = num4 + 2;
			if (num5 < 10)
			{
				num5 = 10;
			}
			if (num6 > Main.MaxTilesX - 10)
			{
				num6 = Main.MaxTilesX - 10;
			}
			if (num7 < 10)
			{
				num7 = 10;
			}
			if (num8 > Main.MaxTilesY - 10)
			{
				num8 = Main.MaxTilesY - 10;
			}
			if (Main.TileSet[num3, num4].Type >= 82 && Main.TileSet[num3, num4].Type <= 84)
			{
				GrowAlch(num3, num4);
			}
			if (Main.TileSet[num3, num4].Liquid > 32)
			{
				if (Main.TileSet[num3, num4].IsActive != 0 && (Main.TileSet[num3, num4].Type == 3 || Main.TileSet[num3, num4].Type == 20 || Main.TileSet[num3, num4].Type == 24 || Main.TileSet[num3, num4].Type == 27 || Main.TileSet[num3, num4].Type == 73))
				{
					KillTile(num3, num4);
					if (Main.NetMode == 2)
					{
						NetMessage.CreateMessage5(17, 0, num3, num4, 0);
						NetMessage.SendMessage();
					}
				}
			}
			else if (Main.TileSet[num3, num4].IsActive != 0)
			{
				hardUpdateWorld(num3, num4);
				if (Main.TileSet[num3, num4].Type == 80)
				{
					if (genRand.Next(15) == 0)
					{
						GrowCactus(num3, num4);
					}
				}
				else if (Main.TileSet[num3, num4].Type == 53)
				{
					if (Main.TileSet[num3, num7].IsActive == 0)
					{
						if (num3 < 250 || num3 > Main.MaxTilesX - 250)
						{
							if (genRand.Next(500) == 0 && Main.TileSet[num3, num7].Liquid == byte.MaxValue && Main.TileSet[num3, num7 - 1].Liquid == byte.MaxValue && Main.TileSet[num3, num7 - 2].Liquid == byte.MaxValue && Main.TileSet[num3, num7 - 3].Liquid == byte.MaxValue && Main.TileSet[num3, num7 - 4].Liquid == byte.MaxValue && PlaceTile(num3, num7, 81, ToMute: true))
							{
								NetMessage.SendTile(num3, num7);
							}
						}
						else if (num3 > 400 && num3 < Main.MaxTilesX - 400 && genRand.Next(300) == 0)
						{
							GrowCactus(num3, num4);
						}
					}
				}
				else if (Main.TileSet[num3, num4].Type == 116 || Main.TileSet[num3, num4].Type == 112)
				{
					if (Main.TileSet[num3, num7].IsActive == 0 && num3 > 400 && num3 < Main.MaxTilesX - 400 && genRand.Next(300) == 0)
					{
						GrowCactus(num3, num4);
					}
				}
				else if (Main.TileSet[num3, num4].Type == 78)
				{
					if (Main.TileSet[num3, num7].IsActive == 0 && PlaceTile(num3, num7, 3, ToMute: true))
					{
						NetMessage.SendTile(num3, num7);
					}
				}
				else if (Main.TileSet[num3, num4].Type == 2 || Main.TileSet[num3, num4].Type == 23 || Main.TileSet[num3, num4].Type == 32 || Main.TileSet[num3, num4].Type == 109)
				{
					int num9 = Main.TileSet[num3, num4].Type;
					if (Main.TileSet[num3, num7].IsActive == 0 && genRand.Next(12) == 0 && num9 == 2 && PlaceTile(num3, num7, 3, ToMute: true))
					{
						NetMessage.SendTile(num3, num7);
					}
					if (Main.TileSet[num3, num7].IsActive == 0 && genRand.Next(10) == 0 && num9 == 23 && PlaceTile(num3, num7, 24, ToMute: true))
					{
						NetMessage.SendTile(num3, num7);
					}
					if (Main.TileSet[num3, num7].IsActive == 0 && genRand.Next(10) == 0 && num9 == 109 && PlaceTile(num3, num7, 110, ToMute: true))
					{
						NetMessage.SendTile(num3, num7);
					}
					bool flag2 = false;
					for (int k = num5; k < num6; k++)
					{
						for (int l = num7; l < num8; l++)
						{
							if ((num3 == k && num4 == l) || Main.TileSet[k, l].IsActive == 0)
							{
								continue;
							}
							if (num9 == 32)
							{
								num9 = 23;
							}
							if (Main.TileSet[k, l].Type == 0 || (num9 == 23 && Main.TileSet[k, l].Type == 2) || (num9 == 23 && Main.TileSet[k, l].Type == 109))
							{
								SpreadGrass(k, l, 0, num9, repeat: false);
								if (num9 == 23)
								{
									SpreadGrass(k, l, 2, num9, repeat: false);
									SpreadGrass(k, l, 109, num9, repeat: false);
								}
								if (Main.TileSet[k, l].Type == num9)
								{
									SquareTileFrame(k, l);
									flag2 = true;
								}
							}
							if (Main.TileSet[k, l].Type == 0 || (num9 == 109 && Main.TileSet[k, l].Type == 2) || (num9 == 109 && Main.TileSet[k, l].Type == 23))
							{
								SpreadGrass(k, l, 0, num9, repeat: false);
								if (num9 == 109)
								{
									SpreadGrass(k, l, 2, num9, repeat: false);
									SpreadGrass(k, l, 23, num9, repeat: false);
								}
								if (Main.TileSet[k, l].Type == num9)
								{
									SquareTileFrame(k, l);
									flag2 = true;
								}
							}
						}
					}
					if (flag2)
					{
						NetMessage.SendTileSquare(num3, num4, 3);
					}
				}
				else if (Main.TileSet[num3, num4].Type == 20 && genRand.Next(20) == 0 && !PlayerLOS(num3, num4))
				{
					GrowTree(num3, num4);
				}
				if (Main.TileSet[num3, num4].Type == 3 && genRand.Next(20) == 0 && Main.TileSet[num3, num4].FrameX < 144)
				{
					Main.TileSet[num3, num4].Type = 73;
					NetMessage.SendTileSquare(num3, num4, 3);
				}
				if (Main.TileSet[num3, num4].Type == 110 && genRand.Next(20) == 0 && Main.TileSet[num3, num4].FrameX < 144)
				{
					Main.TileSet[num3, num4].Type = 113;
					NetMessage.SendTileSquare(num3, num4, 3);
				}
				if (Main.TileSet[num3, num4].Type == 32 && genRand.Next(3) == 0)
				{
					int num10 = num3;
					int num11 = num4;
					int num12 = 0;
					if (Main.TileSet[num10 + 1, num11].IsActive != 0 && Main.TileSet[num10 + 1, num11].Type == 32)
					{
						num12++;
					}
					if (Main.TileSet[num10 - 1, num11].IsActive != 0 && Main.TileSet[num10 - 1, num11].Type == 32)
					{
						num12++;
					}
					if (Main.TileSet[num10, num11 + 1].IsActive != 0 && Main.TileSet[num10, num11 + 1].Type == 32)
					{
						num12++;
					}
					if (Main.TileSet[num10, num11 - 1].IsActive != 0 && Main.TileSet[num10, num11 - 1].Type == 32)
					{
						num12++;
					}
					if (num12 < 3 || Main.TileSet[num3, num4].Type == 23)
					{
						switch (genRand.Next(4))
						{
						case 0:
							num11--;
							break;
						case 1:
							num11++;
							break;
						case 2:
							num10--;
							break;
						case 3:
							num10++;
							break;
						}
						if (Main.TileSet[num10, num11].IsActive == 0)
						{
							num12 = 0;
							if (Main.TileSet[num10 + 1, num11].IsActive != 0 && Main.TileSet[num10 + 1, num11].Type == 32)
							{
								num12++;
							}
							if (Main.TileSet[num10 - 1, num11].IsActive != 0 && Main.TileSet[num10 - 1, num11].Type == 32)
							{
								num12++;
							}
							if (Main.TileSet[num10, num11 + 1].IsActive != 0 && Main.TileSet[num10, num11 + 1].Type == 32)
							{
								num12++;
							}
							if (Main.TileSet[num10, num11 - 1].IsActive != 0 && Main.TileSet[num10, num11 - 1].Type == 32)
							{
								num12++;
							}
							if (num12 < 2)
							{
								int num13 = 7;
								int num14 = num10 - num13;
								int num15 = num10 + num13;
								int num16 = num11 - num13;
								int num17 = num11 + num13;
								bool flag3 = false;
								for (int m = num14; m < num15; m++)
								{
									for (int n = num16; n < num17; n++)
									{
										if (Math.Abs(m - num10) * 2 + Math.Abs(n - num11) < 9 && Main.TileSet[m, n].IsActive != 0 && Main.TileSet[m, n].Type == 23 && Main.TileSet[m, n - 1].IsActive != 0 && Main.TileSet[m, n - 1].Type == 32 && Main.TileSet[m, n - 1].Liquid == 0)
										{
											flag3 = true;
											break;
										}
									}
								}
								if (flag3)
								{
									Main.TileSet[num10, num11].Type = 32;
									Main.TileSet[num10, num11].IsActive = 1;
									SquareTileFrame(num10, num11);
									NetMessage.SendTileSquare(num10, num11, 3);
								}
							}
						}
					}
				}
			}
			else if (flag && ToSpawnNPC > 0)
			{
				SpawnNPC(num3, num4);
			}
			if (Main.TileSet[num3, num4].IsActive == 0)
			{
				continue;
			}
			if ((Main.TileSet[num3, num4].Type == 2 || Main.TileSet[num3, num4].Type == 52) && genRand.Next(40) == 0 && Main.TileSet[num3, num4 + 1].IsActive == 0 && Main.TileSet[num3, num4 + 1].Lava == 0)
			{
				bool flag4 = false;
				for (int num18 = num4; num18 > num4 - 10; num18--)
				{
					if (Main.TileSet[num3, num18].IsActive != 0 && Main.TileSet[num3, num18].Type == 2)
					{
						flag4 = true;
						break;
					}
				}
				if (flag4)
				{
					int num19 = num3;
					int num20 = num4 + 1;
					Main.TileSet[num19, num20].Type = 52;
					Main.TileSet[num19, num20].IsActive = 1;
					SquareTileFrame(num19, num20);
					NetMessage.SendTileSquare(num19, num20, 3);
				}
			}
			if (Main.TileSet[num3, num4].Type == 60)
			{
				int type = Main.TileSet[num3, num4].Type;
				if (Main.TileSet[num3, num7].IsActive == 0 && genRand.Next(7) == 0)
				{
					if (PlaceTile(num3, num7, 61, ToMute: true))
					{
						NetMessage.SendTile(num3, num7);
					}
				}
				else if (genRand.Next(500) == 0 && (Main.TileSet[num3, num7].IsActive == 0 || Main.TileSet[num3, num7].Type == 61 || Main.TileSet[num3, num7].Type == 74 || Main.TileSet[num3, num7].Type == 69) && !PlayerLOS(num3, num4))
				{
					GrowTree(num3, num4);
				}
				bool flag5 = false;
				for (int num21 = num5; num21 < num6; num21++)
				{
					for (int num22 = num7; num22 < num8; num22++)
					{
						if ((num3 != num21 || num4 != num22) && Main.TileSet[num21, num22].IsActive != 0 && Main.TileSet[num21, num22].Type == 59)
						{
							SpreadGrass(num21, num22, 59, type, repeat: false);
							if (Main.TileSet[num21, num22].Type == type)
							{
								SquareTileFrame(num21, num22);
								flag5 = true;
							}
						}
					}
				}
				if (flag5)
				{
					NetMessage.SendTileSquare(num3, num4, 3);
				}
			}
			if (Main.TileSet[num3, num4].Type == 61 && genRand.Next(3) == 0 && Main.TileSet[num3, num4].FrameX < 144)
			{
				Main.TileSet[num3, num4].Type = 74;
				NetMessage.SendTileSquare(num3, num4, 3);
			}
			if ((Main.TileSet[num3, num4].Type == 60 || Main.TileSet[num3, num4].Type == 62) && genRand.Next(15) == 0 && Main.TileSet[num3, num4 + 1].IsActive == 0 && Main.TileSet[num3, num4 + 1].Lava == 0)
			{
				bool flag6 = false;
				for (int num23 = num4; num23 > num4 - 10; num23--)
				{
					if (Main.TileSet[num3, num23].IsActive != 0 && Main.TileSet[num3, num23].Type == 60)
					{
						flag6 = true;
						break;
					}
				}
				if (flag6)
				{
					int num24 = num3;
					int num25 = num4 + 1;
					Main.TileSet[num24, num25].Type = 62;
					Main.TileSet[num24, num25].IsActive = 1;
					SquareTileFrame(num24, num25);
					NetMessage.SendTileSquare(num24, num25, 3);
				}
			}
			if ((Main.TileSet[num3, num4].Type != 109 && Main.TileSet[num3, num4].Type != 115) || genRand.Next(15) != 0 || Main.TileSet[num3, num4 + 1].IsActive != 0 || Main.TileSet[num3, num4 + 1].Lava != 0)
			{
				continue;
			}
			bool flag7 = false;
			for (int num26 = num4; num26 > num4 - 10; num26--)
			{
				if (Main.TileSet[num3, num26].IsActive != 0 && Main.TileSet[num3, num26].Type == 109)
				{
					flag7 = true;
					break;
				}
			}
			if (flag7)
			{
				int num27 = num3;
				int num28 = num4 + 1;
				Main.TileSet[num27, num28].Type = 115;
				Main.TileSet[num27, num28].IsActive = 1;
				SquareTileFrame(num27, num28);
				NetMessage.SendTileSquare(num27, num28, 3);
			}
		}
		for (int num29 = 0; (float)num29 < (float)(Main.MaxTilesX * Main.MaxTilesY) * num2; num29++)
		{
			int num30 = genRand.Next(10, Main.MaxTilesX - 10);
			int num31 = genRand.Next(Main.WorldSurface - 1, Main.MaxTilesY - 20);
			int num32 = num30 - 1;
			int num33 = num30 + 2;
			int num34 = num31 - 1;
			int num35 = num31 + 2;
			if (num32 < 10)
			{
				num32 = 10;
			}
			if (num33 > Main.MaxTilesX - 10)
			{
				num33 = Main.MaxTilesX - 10;
			}
			if (num34 < 10)
			{
				num34 = 10;
			}
			if (num35 > Main.MaxTilesY - 10)
			{
				num35 = Main.MaxTilesY - 10;
			}
			if (Main.TileSet[num30, num31].Type >= 82 && Main.TileSet[num30, num31].Type <= 84)
			{
				GrowAlch(num30, num31);
			}
			if (Main.TileSet[num30, num31].Liquid > 32)
			{
				continue;
			}
			if (Main.TileSet[num30, num31].IsActive != 0)
			{
				hardUpdateWorld(num30, num31);
				if (Main.TileSet[num30, num31].Type == 23 && Main.TileSet[num30, num34].IsActive == 0 && genRand.Next(1) == 0 && PlaceTile(num30, num34, 24, ToMute: true))
				{
					NetMessage.SendTile(num30, num34);
				}
				if (Main.TileSet[num30, num31].Type == 32 && genRand.Next(3) == 0)
				{
					int num36 = num30;
					int num37 = num31;
					int num38 = 0;
					if (Main.TileSet[num36 + 1, num37].IsActive != 0 && Main.TileSet[num36 + 1, num37].Type == 32)
					{
						num38++;
					}
					if (Main.TileSet[num36 - 1, num37].IsActive != 0 && Main.TileSet[num36 - 1, num37].Type == 32)
					{
						num38++;
					}
					if (Main.TileSet[num36, num37 + 1].IsActive != 0 && Main.TileSet[num36, num37 + 1].Type == 32)
					{
						num38++;
					}
					if (Main.TileSet[num36, num37 - 1].IsActive != 0 && Main.TileSet[num36, num37 - 1].Type == 32)
					{
						num38++;
					}
					if (num38 < 3 || Main.TileSet[num30, num31].Type == 23)
					{
						switch (genRand.Next(4))
						{
						case 0:
							num37--;
							break;
						case 1:
							num37++;
							break;
						case 2:
							num36--;
							break;
						case 3:
							num36++;
							break;
						}
						if (Main.TileSet[num36, num37].IsActive == 0)
						{
							num38 = 0;
							if (Main.TileSet[num36 + 1, num37].IsActive != 0 && Main.TileSet[num36 + 1, num37].Type == 32)
							{
								num38++;
							}
							if (Main.TileSet[num36 - 1, num37].IsActive != 0 && Main.TileSet[num36 - 1, num37].Type == 32)
							{
								num38++;
							}
							if (Main.TileSet[num36, num37 + 1].IsActive != 0 && Main.TileSet[num36, num37 + 1].Type == 32)
							{
								num38++;
							}
							if (Main.TileSet[num36, num37 - 1].IsActive != 0 && Main.TileSet[num36, num37 - 1].Type == 32)
							{
								num38++;
							}
							if (num38 < 2)
							{
								int num39 = 7;
								int num40 = num36 - num39;
								int num41 = num36 + num39;
								int num42 = num37 - num39;
								int num43 = num37 + num39;
								bool flag8 = false;
								for (int num44 = num40; num44 < num41; num44++)
								{
									for (int num45 = num42; num45 < num43; num45++)
									{
										if (Math.Abs(num44 - num36) * 2 + Math.Abs(num45 - num37) < 9 && Main.TileSet[num44, num45].IsActive != 0 && Main.TileSet[num44, num45].Type == 23 && Main.TileSet[num44, num45 - 1].IsActive != 0 && Main.TileSet[num44, num45 - 1].Type == 32 && Main.TileSet[num44, num45 - 1].Liquid == 0)
										{
											flag8 = true;
											break;
										}
									}
								}
								if (flag8)
								{
									Main.TileSet[num36, num37].Type = 32;
									Main.TileSet[num36, num37].IsActive = 1;
									SquareTileFrame(num36, num37);
									NetMessage.SendTileSquare(num36, num37, 3);
								}
							}
						}
					}
				}
				if (Main.TileSet[num30, num31].Type == 60)
				{
					int type2 = Main.TileSet[num30, num31].Type;
					if (Main.TileSet[num30, num34].IsActive == 0 && genRand.Next(10) == 0 && PlaceTile(num30, num34, 61, ToMute: true))
					{
						NetMessage.SendTile(num30, num34);
					}
					bool flag9 = false;
					for (int num46 = num32; num46 < num33; num46++)
					{
						for (int num47 = num34; num47 < num35; num47++)
						{
							if ((num30 != num46 || num31 != num47) && Main.TileSet[num46, num47].IsActive != 0 && Main.TileSet[num46, num47].Type == 59)
							{
								SpreadGrass(num46, num47, 59, type2, repeat: false);
								if (Main.TileSet[num46, num47].Type == type2)
								{
									SquareTileFrame(num46, num47);
									flag9 = true;
								}
							}
						}
					}
					if (flag9)
					{
						NetMessage.SendTileSquare(num30, num31, 3);
					}
				}
				if (Main.TileSet[num30, num31].Type == 61 && genRand.Next(3) == 0 && Main.TileSet[num30, num31].FrameX < 144)
				{
					Main.TileSet[num30, num31].Type = 74;
					NetMessage.SendTileSquare(num30, num31, 3);
				}
				if ((Main.TileSet[num30, num31].Type == 60 || Main.TileSet[num30, num31].Type == 62) && genRand.Next(5) == 0 && Main.TileSet[num30, num31 + 1].IsActive == 0 && Main.TileSet[num30, num31 + 1].Lava == 0)
				{
					bool flag10 = false;
					for (int num48 = num31; num48 > num31 - 10; num48--)
					{
						if (Main.TileSet[num30, num48].IsActive != 0 && Main.TileSet[num30, num48].Type == 60)
						{
							flag10 = true;
							break;
						}
					}
					if (flag10)
					{
						int num49 = num30;
						int num50 = num31 + 1;
						Main.TileSet[num49, num50].Type = 62;
						Main.TileSet[num49, num50].IsActive = 1;
						SquareTileFrame(num49, num50);
						NetMessage.SendTileSquare(num49, num50, 3);
					}
				}
				if (Main.TileSet[num30, num31].Type == 69 && genRand.Next(3) == 0)
				{
					int num51 = num30;
					int num52 = num31;
					int num53 = 0;
					if (Main.TileSet[num51 + 1, num52].IsActive != 0 && Main.TileSet[num51 + 1, num52].Type == 69)
					{
						num53++;
					}
					if (Main.TileSet[num51 - 1, num52].IsActive != 0 && Main.TileSet[num51 - 1, num52].Type == 69)
					{
						num53++;
					}
					if (Main.TileSet[num51, num52 + 1].IsActive != 0 && Main.TileSet[num51, num52 + 1].Type == 69)
					{
						num53++;
					}
					if (Main.TileSet[num51, num52 - 1].IsActive != 0 && Main.TileSet[num51, num52 - 1].Type == 69)
					{
						num53++;
					}
					if (num53 < 3 || Main.TileSet[num30, num31].Type == 60)
					{
						switch (genRand.Next(4))
						{
						case 0:
							num52--;
							break;
						case 1:
							num52++;
							break;
						case 2:
							num51--;
							break;
						case 3:
							num51++;
							break;
						}
						if (Main.TileSet[num51, num52].IsActive == 0)
						{
							num53 = 0;
							if (Main.TileSet[num51 + 1, num52].IsActive != 0 && Main.TileSet[num51 + 1, num52].Type == 69)
							{
								num53++;
							}
							if (Main.TileSet[num51 - 1, num52].IsActive != 0 && Main.TileSet[num51 - 1, num52].Type == 69)
							{
								num53++;
							}
							if (Main.TileSet[num51, num52 + 1].IsActive != 0 && Main.TileSet[num51, num52 + 1].Type == 69)
							{
								num53++;
							}
							if (Main.TileSet[num51, num52 - 1].IsActive != 0 && Main.TileSet[num51, num52 - 1].Type == 69)
							{
								num53++;
							}
							if (num53 < 2)
							{
								int num54 = 7;
								int num55 = num51 - num54;
								int num56 = num51 + num54;
								int num57 = num52 - num54;
								int num58 = num52 + num54;
								bool flag11 = false;
								for (int num59 = num55; num59 < num56; num59++)
								{
									for (int num60 = num57; num60 < num58; num60++)
									{
										if (Math.Abs(num59 - num51) * 2 + Math.Abs(num60 - num52) < 9 && Main.TileSet[num59, num60].IsActive != 0 && Main.TileSet[num59, num60].Type == 60 && Main.TileSet[num59, num60 - 1].IsActive != 0 && Main.TileSet[num59, num60 - 1].Type == 69 && Main.TileSet[num59, num60 - 1].Liquid == 0)
										{
											flag11 = true;
											break;
										}
									}
								}
								if (flag11)
								{
									Main.TileSet[num51, num52].Type = 69;
									Main.TileSet[num51, num52].IsActive = 1;
									SquareTileFrame(num51, num52);
									NetMessage.SendTileSquare(num51, num52, 3);
								}
							}
						}
					}
				}
				if (Main.TileSet[num30, num31].Type != 70)
				{
					continue;
				}
				int type3 = Main.TileSet[num30, num31].Type;
				if (Main.TileSet[num30, num34].IsActive == 0 && genRand.Next(10) == 0 && PlaceTile(num30, num34, 71, ToMute: true))
				{
					NetMessage.SendTile(num30, num34);
				}
				if (genRand.Next(200) == 0 && !PlayerLOS(num30, num31))
				{
					GrowShroom(num30, num31);
				}
				bool flag12 = false;
				for (int num61 = num32; num61 < num33; num61++)
				{
					for (int num62 = num34; num62 < num35; num62++)
					{
						if ((num30 != num61 || num31 != num62) && Main.TileSet[num61, num62].IsActive != 0 && Main.TileSet[num61, num62].Type == 59)
						{
							SpreadGrass(num61, num62, 59, type3, repeat: false);
							if (Main.TileSet[num61, num62].Type == type3)
							{
								SquareTileFrame(num61, num62);
								flag12 = true;
							}
						}
					}
				}
				if (flag12)
				{
					NetMessage.SendTileSquare(num30, num31, 3);
				}
			}
			else if (flag && ToSpawnNPC > 0)
			{
				SpawnNPC(num30, num31);
			}
		}
		if (Main.Rand.Next(100) == 0)
		{
			PlantAlch();
		}
		if (!Main.GameTime.DayTime)
		{
			float num63 = (float)Main.MaxTilesX / 4200f;
			if ((float)Main.Rand.Next(8000) < 10f * num63)
			{
				int num64 = Main.Rand.Next(Main.MaxTilesX - 50) + 100;
				num64 *= 16;
				int num65 = Main.Rand.Next((int)((double)Main.MaxTilesY * 0.05));
				num65 *= 16;
				Vector2 vector = new Vector2(num64, num65);
				float num66 = Main.Rand.Next(-100, 101);
				float num67 = Main.Rand.Next(200) + 100;
				float num68 = (float)Math.Sqrt(num66 * num66 + num67 * num67);
				num68 = 12f / num68;
				num66 *= num68;
				num67 *= num68;
				Projectile.NewProjectile(vector.X, vector.Y, num66, num67, 12, 1000, 10f);
			}
		}
	}

	public static bool PlaceWall(int i, int j, int type)
	{
		if (i <= 1 || j <= 1 || i >= Main.MaxTilesX - 2 || j >= Main.MaxTilesY - 2)
		{
			return false;
		}
		if (Main.TileSet[i, j].WallType == 0)
		{
			Main.TileSet[i, j].WallType = (byte)type;
			WallFrame(i - 1, j - 1);
			WallFrame(i - 1, j);
			WallFrame(i - 1, j + 1);
			WallFrame(i, j - 1);
			WallFrame(i, j, resetFrame: true);
			WallFrame(i, j + 1);
			WallFrame(i + 1, j - 1);
			WallFrame(i + 1, j);
			WallFrame(i + 1, j + 1);
			Main.PlaySound(0, i << 4, j << 4);
			return true;
		}
		return false;
	}

	public unsafe static void AddPlants()
	{
		fixed (Tile* tileSet = Main.TileSet)
		{
			for (int i = 0; i < Main.MaxTilesX; i++)
			{
				Tile* ptr = tileSet + (i * 1440 + 5);
				int num = 5;
				while (num < Main.MaxTilesY)
				{
					if (ptr->IsActive != 0)
					{
						if (ptr->Type == 2)
						{
							ptr--;
							if (ptr->IsActive == 0)
							{
								PlaceTile(i, num - 1, 3, ToMute: true);
							}
							ptr++;
						}
						else if (ptr->Type == 23)
						{
							ptr--;
							if (ptr->IsActive == 0)
							{
								PlaceTile(i, num - 1, 24, ToMute: true);
							}
							ptr++;
						}
					}
					num++;
					ptr++;
				}
			}
		}
	}

	public static void SpreadGrass(int i, int j, int dirt = 0, int grass = 2, bool repeat = true)
	{
		try
		{
			if (Main.TileSet[i, j].Type != dirt || Main.TileSet[i, j].IsActive == 0 || (j < Main.WorldSurface && grass == 70) || (j >= Main.WorldSurface && dirt == 0))
			{
				return;
			}
			int num = i - 1;
			int num2 = i + 2;
			int num3 = j - 1;
			int num4 = j + 2;
			if (num < 0)
			{
				num = 0;
			}
			if (num2 > Main.MaxTilesX)
			{
				num2 = Main.MaxTilesX;
			}
			if (num3 < 0)
			{
				num3 = 0;
			}
			if (num4 > Main.MaxTilesY)
			{
				num4 = Main.MaxTilesY;
			}
			bool flag = true;
			for (int k = num; k < num2; k++)
			{
				for (int l = num3; l < num4; l++)
				{
					if (Main.TileSet[k, l].IsActive == 0 || !Main.TileSolid[Main.TileSet[k, l].Type])
					{
						flag = false;
					}
					if (Main.TileSet[k, l].Lava != 0 && Main.TileSet[k, l].Liquid > 0)
					{
						flag = true;
						break;
					}
				}
			}
			if (flag || (grass == 23 && Main.TileSet[i, j - 1].Type == 27))
			{
				return;
			}
			Main.TileSet[i, j].Type = (byte)grass;
			for (int m = num; m < num2; m++)
			{
				for (int n = num3; n < num4; n++)
				{
					if (Main.TileSet[m, n].IsActive == 0 || Main.TileSet[m, n].Type != dirt)
					{
						continue;
					}
					try
					{
						if (repeat && grassSpread < 400)
						{
							grassSpread++;
							SpreadGrass(m, n, dirt, grass);
							grassSpread--;
						}
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
	}

	public static void ChasmRunnerSideways(int i, int j, int direction, int steps)
	{
		Vector2 vector = default;
		Vector2 vector2 = default;
		float num = steps;
		vector.X = i;
		vector.Y = j;
		vector2.X = (float)genRand.Next(10, 21) * 0.1f * (float)direction;
		vector2.Y = (float)genRand.Next(-10, 10) * 0.01f;
		int num2 = genRand.Next(5) + 7;
		while (num2 > 0)
		{
			if (num > 0f)
			{
				num2 += genRand.Next(3);
				num2 -= genRand.Next(3);
				if (num2 < 7)
				{
					num2 = 7;
				}
				else if (num2 > 20)
				{
					num2 = 20;
				}
				if (num == 1f && num2 < 10)
				{
					num2 = 10;
				}
			}
			else
			{
				num2 -= genRand.Next(4);
			}
			if (vector.Y > (float)Main.RockLayer && num > 0f)
			{
				num = 0f;
			}
			num--;
			int num3 = (int)(vector.X - (float)num2 * 0.5f);
			int num4 = (int)(vector.X + (float)num2 * 0.5f);
			int num5 = (int)(vector.Y - (float)num2 * 0.5f);
			int num6 = (int)(vector.Y + (float)num2 * 0.5f);
			if (num3 < 0)
			{
				num3 = 0;
			}
			if (num4 > Main.MaxTilesX - 1)
			{
				num4 = Main.MaxTilesX - 1;
			}
			if (num5 < 0)
			{
				num5 = 0;
			}
			if (num6 > Main.MaxTilesY)
			{
				num6 = Main.MaxTilesY;
			}
			for (int k = num3; k < num4; k++)
			{
				for (int l = num5; l < num6; l++)
				{
					if (Math.Abs((float)k - vector.X) + Math.Abs((float)l - vector.Y) < (float)num2 * 0.5f * (1f + (float)genRand.Next(-10, 11) * 0.015f) && Main.TileSet[k, l].Type != 31 && Main.TileSet[k, l].Type != 22)
					{
						Main.TileSet[k, l].IsActive = 0;
					}
				}
			}
			vector.X += vector2.X;
			vector.Y += vector2.Y;
			vector2.Y += (float)genRand.Next(-10, 10) * 0.1f;
			if (vector.Y < (float)(j - 20))
			{
				vector2.Y += (float)genRand.Next(20) * 0.01f;
			}
			else if (vector.Y > (float)(j + 20))
			{
				vector2.Y -= (float)genRand.Next(20) * 0.01f;
			}
			if ((double)vector2.Y < -0.5)
			{
				vector2.Y = -0.5f;
			}
			else if ((double)vector2.Y > 0.5)
			{
				vector2.Y = 0.5f;
			}
			vector2.X += (float)genRand.Next(-10, 11) * 0.01f;
			switch (direction)
			{
			case -1:
				if ((double)vector2.X > -0.5)
				{
					vector2.X = -0.5f;
				}
				else if (vector2.X < -2f)
				{
					vector2.X = -2f;
				}
				break;
			case 1:
				if ((double)vector2.X < 0.5)
				{
					vector2.X = 0.5f;
				}
				else if (vector2.X > 2f)
				{
					vector2.X = 2f;
				}
				break;
			}
			num3 = (int)(vector.X - (float)num2 * 1.1f);
			num4 = (int)(vector.X + (float)num2 * 1.1f);
			num5 = (int)(vector.Y - (float)num2 * 1.1f);
			num6 = (int)(vector.Y + (float)num2 * 1.1f);
			if (num3 < 1)
			{
				num3 = 1;
			}
			if (num4 > Main.MaxTilesX - 1)
			{
				num4 = Main.MaxTilesX - 1;
			}
			if (num5 < 0)
			{
				num5 = 0;
			}
			if (num6 > Main.MaxTilesY)
			{
				num6 = Main.MaxTilesY;
			}
			for (int m = num3; m < num4; m++)
			{
				for (int n = num5; n < num6; n++)
				{
					if (Math.Abs((float)m - vector.X) + Math.Abs((float)n - vector.Y) < (float)num2 * 1.1f * (1f + (float)genRand.Next(-10, 11) * 0.015f) && Main.TileSet[m, n].WallType != 3)
					{
						if (Main.TileSet[m, n].Type != 25 && n > j + genRand.Next(3, 20))
						{
							Main.TileSet[m, n].IsActive = 1;
						}
						Main.TileSet[m, n].IsActive = 1;
						if (Main.TileSet[m, n].Type != 31 && Main.TileSet[m, n].Type != 22)
						{
							Main.TileSet[m, n].Type = 25;
						}
						if (Main.TileSet[m, n].WallType == 2)
						{
							Main.TileSet[m, n].WallType = 0;
						}
					}
				}
			}
			for (int num7 = num3; num7 < num4; num7++)
			{
				for (int num8 = num5; num8 < num6; num8++)
				{
					if (Math.Abs((float)num7 - vector.X) + Math.Abs((float)num8 - vector.Y) < (float)num2 * 1.1f * (1f + (float)genRand.Next(-10, 11) * 0.015f) && Main.TileSet[num7, num8].WallType != 3)
					{
						if (Main.TileSet[num7, num8].Type != 31 && Main.TileSet[num7, num8].Type != 22)
						{
							Main.TileSet[num7, num8].Type = 25;
						}
						Main.TileSet[num7, num8].IsActive = 1;
						if (Main.TileSet[num7, num8].WallType == 0)
						{
							Main.TileSet[num7, num8].WallType = 3;
						}
					}
				}
			}
		}
		if (genRand.Next(3) == 0)
		{
			int num9 = (int)vector.X;
			int num10;
			for (num10 = (int)vector.Y; Main.TileSet[num9, num10].IsActive == 0; num10++)
			{
			}
			TileRunner(num9, num10, genRand.Next(2, 6), genRand.Next(3, 7), 22);
		}
	}

	public static void ChasmRunner(int i, int j, int steps, bool makeOrb = false)
	{
		bool flag = false;
		bool flag2 = !makeOrb;
		Vector2 vector = new Vector2(i, j);
		Vector2 vector2 = default;
		float num = steps;
		vector2.X = (float)genRand.Next(-10, 11) * 0.1f;
		vector2.Y = (float)genRand.Next(11) * 0.2f + 0.5f;
		int num2 = 5;
		int num3 = genRand.Next(5) + 7;
		while (num3 > 0)
		{
			if (num > 0f)
			{
				num3 += genRand.Next(3);
				num3 -= genRand.Next(3);
				if (num3 < 7)
				{
					num3 = 7;
				}
				else if (num3 > 20)
				{
					num3 = 20;
				}
				if (num == 1f && num3 < 10)
				{
					num3 = 10;
				}
			}
			else if (vector.Y > (float)(Main.WorldSurface + 45))
			{
				num3 -= genRand.Next(4);
			}
			if (vector.Y > (float)Main.RockLayer && num > 0f)
			{
				num = 0f;
			}
			num--;
			if (!flag && vector.Y > (float)(Main.WorldSurface + 20))
			{
				flag = true;
				ChasmRunnerSideways((int)vector.X, (int)vector.Y, -1, genRand.Next(20, 40));
				ChasmRunnerSideways((int)vector.X, (int)vector.Y, 1, genRand.Next(20, 40));
			}
			int num4;
			int num5;
			int num6;
			int num7;
			if (num > (float)num2)
			{
				num4 = (int)(vector.X - (float)num3 * 0.5f);
				num5 = (int)(vector.X + (float)num3 * 0.5f);
				num6 = (int)(vector.Y - (float)num3 * 0.5f);
				num7 = (int)(vector.Y + (float)num3 * 0.5f);
				if (num4 < 0)
				{
					num4 = 0;
				}
				if (num5 > Main.MaxTilesX - 1)
				{
					num5 = Main.MaxTilesX - 1;
				}
				if (num6 < 0)
				{
					num6 = 0;
				}
				if (num7 > Main.MaxTilesY)
				{
					num7 = Main.MaxTilesY;
				}
				for (int k = num4; k < num5; k++)
				{
					for (int l = num6; l < num7; l++)
					{
						if (Math.Abs((float)k - vector.X) + Math.Abs((float)l - vector.Y) < (float)num3 * 0.5f * (1f + (float)genRand.Next(-10, 11) * 0.015f) && Main.TileSet[k, l].Type != 31 && Main.TileSet[k, l].Type != 22)
						{
							Main.TileSet[k, l].IsActive = 0;
						}
					}
				}
			}
			if (num <= 2f && vector.Y < (float)(Main.WorldSurface + 45))
			{
				num = 2f;
			}
			if (num <= 0f)
			{
				if (!flag2)
				{
					flag2 = true;
					AddShadowOrb((int)vector.X, (int)vector.Y);
				}
				else
				{
					for (int m = 0; m < 10000; m++)
					{
						int num8 = genRand.Next((int)vector.Y - 50, (int)vector.Y);
						if (num8 <= Main.WorldSurface)
						{
							break;
						}
						if (num8 > Main.MaxTilesY - 5)
						{
							num8 = Main.MaxTilesY - 5;
						}
						int num9 = genRand.Next((int)vector.X - 25, (int)vector.X + 25);
						if (num9 < 5)
						{
							num9 = 5;
						}
						else if (num9 > Main.MaxTilesX - 5)
						{
							num9 = Main.MaxTilesX - 5;
						}
						if (Place3x2(num9, num8, 26))
						{
							break;
						}
					}
				}
			}
			vector.X += vector2.X;
			vector.Y += vector2.Y;
			vector2.X += (float)genRand.Next(-10, 11) * 0.01f;
			if (vector2.X > 0.3f)
			{
				vector2.X = 0.3f;
			}
			else if (vector2.X < -0.3f)
			{
				vector2.X = -0.3f;
			}
			num4 = (int)(vector.X - (float)num3 * 1.1f);
			num5 = (int)(vector.X + (float)num3 * 1.1f);
			num6 = (int)(vector.Y - (float)num3 * 1.1f);
			num7 = (int)(vector.Y + (float)num3 * 1.1f);
			if (num4 < 1)
			{
				num4 = 1;
			}
			if (num5 > Main.MaxTilesX - 1)
			{
				num5 = Main.MaxTilesX - 1;
			}
			if (num6 < 0)
			{
				num6 = 0;
			}
			if (num7 > Main.MaxTilesY)
			{
				num7 = Main.MaxTilesY;
			}
			for (int n = num4; n < num5; n++)
			{
				for (int num10 = num6; num10 < num7; num10++)
				{
					if (Math.Abs((float)n - vector.X) + Math.Abs((float)num10 - vector.Y) < (float)num3 * 1.1f * (1f + (float)genRand.Next(-10, 11) * 0.015f))
					{
						if (Main.TileSet[n, num10].Type != 25 && num10 > j + genRand.Next(3, 20))
						{
							Main.TileSet[n, num10].IsActive = 1;
						}
						if (steps <= num2)
						{
							Main.TileSet[n, num10].IsActive = 1;
						}
						if (Main.TileSet[n, num10].Type != 31)
						{
							Main.TileSet[n, num10].Type = 25;
						}
						if (Main.TileSet[n, num10].WallType == 2)
						{
							Main.TileSet[n, num10].WallType = 0;
						}
					}
				}
			}
			for (int num11 = num4; num11 < num5; num11++)
			{
				for (int num12 = num6; num12 < num7; num12++)
				{
					if (Math.Abs((float)num11 - vector.X) + Math.Abs((float)num12 - vector.Y) < (float)num3 * 1.1f * (1f + (float)genRand.Next(-10, 11) * 0.015f))
					{
						if (Main.TileSet[num11, num12].Type != 31)
						{
							Main.TileSet[num11, num12].Type = 25;
						}
						if (steps <= num2)
						{
							Main.TileSet[num11, num12].IsActive = 1;
						}
						if (num12 > j + genRand.Next(3, 20) && Main.TileSet[num11, num12].WallType == 0)
						{
							Main.TileSet[num11, num12].WallType = 3;
						}
					}
				}
			}
		}
	}

	public static void JungleRunner(int i, int j)
	{
		Vector2 vector = new Vector2(i, j);
		Vector2 vector2 = default;
		double num = genRand.Next(5, 11);
		vector2.X = (float)genRand.Next(-10, 11) * 0.1f;
		vector2.Y = (float)genRand.Next(10, 20) * 0.1f;
		int num2 = 0;
		bool flag = true;
		do
		{
			int num3 = (int)vector.X;
			int num4 = (int)vector.Y;
			if (num4 < Main.WorldSurface && Main.TileSet[num3, num4].WallType == 0 && Main.TileSet[num3, num4].IsActive == 0 && Main.TileSet[num3, num4 - 3].WallType == 0 && Main.TileSet[num3, num4 - 3].IsActive == 0 && Main.TileSet[num3, num4 - 1].WallType == 0 && Main.TileSet[num3, num4 - 1].IsActive == 0 && Main.TileSet[num3, num4 - 4].WallType == 0 && Main.TileSet[num3, num4 - 4].IsActive == 0 && Main.TileSet[num3, num4 - 2].WallType == 0 && Main.TileSet[num3, num4 - 2].IsActive == 0 && Main.TileSet[num3, num4 - 5].WallType == 0 && Main.TileSet[num3, num4 - 5].IsActive == 0)
			{
				flag = false;
			}
			JungleX = num3;
			num += (double)((float)genRand.Next(-20, 21) * 0.1f);
			if (num < 5.0)
			{
				num = 5.0;
			}
			else if (num > 10.0)
			{
				num = 10.0;
			}
			int num5 = (int)((double)vector.X - num * 0.5);
			int num6 = (int)((double)vector.X + num * 0.5);
			int num7 = (int)((double)vector.Y - num * 0.5);
			int num8 = (int)((double)vector.Y + num * 0.5);
			if (num5 < 0)
			{
				num5 = 0;
			}
			if (num6 > Main.MaxTilesX)
			{
				num6 = Main.MaxTilesX;
			}
			if (num7 < 0)
			{
				num7 = 0;
			}
			if (num8 > Main.MaxTilesY)
			{
				num8 = Main.MaxTilesY;
			}
			for (int k = num5; k < num6; k++)
			{
				for (int l = num7; l < num8; l++)
				{
					if ((double)(Math.Abs((float)k - vector.X) + Math.Abs((float)l - vector.Y)) < num * 0.5 * (1.0 + (double)genRand.Next(-10, 11) * 0.015))
					{
						KillTileFast(k, l);
					}
				}
			}
			if (++num2 > 10 && genRand.Next(50) < num2)
			{
				num2 = 0;
				int num9 = -2;
				if (genRand.Next(2) == 0)
				{
					num9 = 2;
				}
				TileRunner((int)vector.X, (int)vector.Y, genRand.Next(3, 20), genRand.Next(10, 100), -1, addTile: false, new Vector2(num9, 0f));
			}
			vector.X += vector2.X;
			vector.Y += vector2.Y;
			vector2.Y += (float)genRand.Next(-10, 11) * 0.01f;
			if (vector2.Y > 0f)
			{
				vector2.Y = 0f;
			}
			else if (vector2.Y < -2f)
			{
				vector2.Y = -2f;
			}
			vector2.X += (float)genRand.Next(-10, 11) * 0.1f;
			if (vector.X < (float)(i - 200))
			{
				vector2.X += (float)genRand.Next(5, 21) * 0.1f;
			}
			if (vector.X > (float)(i + 200))
			{
				vector2.X -= (float)genRand.Next(5, 21) * 0.1f;
			}
			if (vector2.X > 1.5f)
			{
				vector2.X = 1.5f;
			}
			else if (vector2.X < -1.5f)
			{
				vector2.X = -1.5f;
			}
		}
		while (flag);
	}

	public static void GERunner(int i, Vector2 speed, bool good, ref Vector2i minArea, ref Vector2i maxArea)
	{
		Vector2 vector = new Vector2(i, 0f);
		Vector2 vector2 = speed;
		int num = genRand.Next(200, 250);
		float num2 = (float)Main.MaxTilesX / 4200f;
		num = (int)((float)num * num2);
		int num3 = num;
		while (true)
		{
			int num4 = (int)(vector.X - (float)num3 * 0.5f);
			int num5 = (int)(vector.X + (float)num3 * 0.5f);
			int num6 = (int)(vector.Y - (float)num3 * 0.5f);
			int num7 = (int)(vector.Y + (float)num3 * 0.5f);
			if (num4 < 0)
			{
				num4 = 0;
			}
			if (num5 > Main.MaxTilesX)
			{
				num5 = Main.MaxTilesX;
			}
			if (num6 < 0)
			{
				num6 = 0;
			}
			if (num7 > Main.MaxTilesY)
			{
				num7 = Main.MaxTilesY;
			}
			for (int j = num4; j < num5; j++)
			{
				for (int k = num6; k < num7; k++)
				{
					if (!((double)(Math.Abs((float)j - vector.X) + Math.Abs((float)k - vector.Y)) < (double)num * 0.5 * (1.0 + (double)genRand.Next(-10, 11) * 0.015)))
					{
						continue;
					}
					EntityID.TileID tileID = EntityID.TileID.DIRT;
					if (good)
					{
						if (Main.TileSet[j, k].WallType == 3)
						{
							Main.TileSet[j, k].WallType = 28;
						}
						switch ((EntityID.TileID)Main.TileSet[j, k].Type)
						{
						case EntityID.TileID.STONE:
						case EntityID.TileID.EBONSTONE:
							tileID = EntityID.TileID.PEARLSTONE;
							break;
						case EntityID.TileID.GRASS:
						case EntityID.TileID.CORRUPT_GRASS:
							tileID = EntityID.TileID.HALLOWED_GRASS;
							break;
						case EntityID.TileID.SAND:
						case EntityID.TileID.EBONSAND:
						case EntityID.TileID.SILT:
							tileID = EntityID.TileID.PEARLSAND;
							break;
						}
					}
					else
					{
						switch ((EntityID.TileID)Main.TileSet[j, k].Type)
						{
						case EntityID.TileID.STONE:
						case EntityID.TileID.PEARLSTONE:
							tileID = EntityID.TileID.EBONSTONE;
							break;
						case EntityID.TileID.GRASS:
						case EntityID.TileID.HALLOWED_GRASS:
							tileID = EntityID.TileID.CORRUPT_GRASS;
							break;
						case EntityID.TileID.SAND:
						case EntityID.TileID.PEARLSAND:
						case EntityID.TileID.SILT:
							tileID = EntityID.TileID.EBONSAND;
							break;
						}
					}
					if ((byte)tileID > 0)
					{
						if (j < minArea.X)
						{
							minArea.X = j;
						}
						if (k < minArea.Y)
						{
							minArea.Y = k;
						}
						if (j > maxArea.X)
						{
							maxArea.X = j;
						}
						if (k > maxArea.Y)
						{
							maxArea.Y = k;
						}
						Main.TileSet[j, k].Type = (byte)tileID;
						SquareTileFrameNoLiquid(j, k);
					}
				}
			}
			vector.X += vector2.X;
			vector.Y += vector2.Y;
			if (!(vector.X < (float)(-num)) && !(vector.Y < (float)(-num)) && !(vector.X > (float)(Main.MaxTilesX + num)) && !(vector.Y > (float)(Main.MaxTilesX + num)))
			{
				vector2.X += (float)genRand.Next(-10, 11) * 0.05f;
				if (vector2.X > speed.X + 1f)
				{
					vector2.X = speed.X + 1f;
				}
				else if (vector2.X < speed.X - 1f)
				{
					vector2.X = speed.X - 1f;
				}
				continue;
			}
			break;
		}
	}

	public unsafe static void TileRunner(int i, int j, int strength, int steps, int type, bool addTile = false, Vector2 velocity = default(Vector2), bool noYChange = false, bool overRide = true)
	{
		Vector2 vector = new Vector2(i, j);
		float num = strength;
		int num2 = steps;
		float num3 = 1f / (float)steps;
		if (velocity.X == 0f && velocity.Y == 0f)
		{
			velocity.X = (float)genRand.Next(-10, 11) * 0.1f;
			velocity.Y = (float)genRand.Next(-10, 11) * 0.1f;
		}
		while (num > 0f && num2 > 0)
		{
			if (vector.Y < 0f && type == 59)
			{
				num2 = 0;
			}
			num = (float)strength * ((float)num2 * num3);
			num2--;
			int num4 = (int)(vector.Y - num * 0.5f);
			int num5 = (int)(vector.Y + num * 0.5f);
			if (num4 < 0)
			{
				num4 = 0;
			}
			if (num5 > Main.MaxTilesY)
			{
				num5 = Main.MaxTilesY;
			}
			if (num4 < num5)
			{
				int num6 = (int)(vector.X - num * 0.5f);
				int num7 = (int)(vector.X + num * 0.5f);
				if (num6 < 0)
				{
					num6 = 0;
				}
				if (num7 > Main.MaxTilesX)
				{
					num7 = Main.MaxTilesX;
				}
				fixed (Tile* tileSet = Main.TileSet)
				{
					for (int k = num6; k < num7; k++)
					{
						int num8 = num4;
						Tile* ptr = tileSet + (k * 1440 + num8);
						do
						{
							if (Math.Abs((float)k - vector.X) + Math.Abs((float)num8 - vector.Y) < (float)strength * 0.5f * (1f + (float)genRand.Next(-10, 11) * 0.015f))
							{
								if (mudWall && num8 > Main.WorldSurface && num8 < Main.MaxTilesY - 210 - genRand.Next(3) && ptr->WallType == 0)
								{
									ptr->WallType = 15;
								}
								if (type < 0)
								{
									if (type == -2 && ptr->IsActive != 0 && (num8 < WaterLine || num8 > lavaLine))
									{
										ptr->Liquid = byte.MaxValue;
										if (num8 > lavaLine)
										{
											ptr->Lava = 32;
										}
									}
									ptr->IsActive = 0;
								}
								else
								{
									if (overRide || ptr->IsActive == 0)
									{
										int type2 = ptr->Type;
										if ((type != 40 || type2 != 53) && (!Main.TileStone[type] || type2 == 1) && type2 != 45 && type2 != 147 && (type2 != 1 || type != 59 || num8 >= Main.WorldSurface + genRand.Next(-50, 50)))
										{
											if (type2 != 53 || num8 >= Main.WorldSurface)
											{
												ptr->Type = (byte)type;
											}
											else if (type == 59)
											{
												ptr->Type = (byte)type;
											}
										}
									}
									if (addTile)
									{
										ptr->IsActive = 1;
										ptr->Liquid = 0;
										ptr->Lava = 0;
									}
									if (type == 59)
									{
										if (num8 > WaterLine && ptr->Liquid > 0)
										{
											ptr->Liquid = 0;
											ptr->Lava = 0;
										}
									}
									else if (noYChange && num8 < Main.WorldSurface)
									{
										ptr->WallType = 2;
									}
								}
							}
							ptr++;
						}
						while (++num8 < num5);
					}
				}
			}
			vector.X += velocity.X;
			vector.Y += velocity.Y;
			int num9 = (int)Math.Max(0.0, Math.Min(6.0, Math.Ceiling(((double)num - 50.0) / 50.0)));
			int num10 = (int)Math.Max(0.0, Math.Min(6.0, Math.Ceiling(((double)num - 400.0) / 100.0)));
			for (int l = 0; l < num9 + num10; l++)
			{
				vector.X += velocity.X;
				vector.Y += velocity.Y;
				num2--;
				velocity.Y += (float)genRand.Next(-10, 11) * 0.05f;
				velocity.X += (float)genRand.Next(-10, 11) * 0.05f;
			}
			velocity.X += (float)genRand.Next(-10, 11) * 0.05f;
			if (velocity.X > 1f)
			{
				velocity.X = 1f;
			}
			else if (velocity.X < -1f)
			{
				velocity.X = -1f;
			}
			if (!noYChange)
			{
				velocity.Y += (float)genRand.Next(-10, 11) * 0.05f;
				if (velocity.Y > 1f)
				{
					velocity.Y = 1f;
				}
				else if (velocity.Y < -1f)
				{
					velocity.Y = -1f;
				}
				if (type == 59)
				{
					int num11 = (int)vector.Y;
					if (num11 < Main.RockLayer + 100)
					{
						velocity.Y = 1f;
					}
					else if (num11 > Main.MaxTilesY - 300)
					{
						velocity.Y = -1f;
					}
					else if (velocity.Y > 0.5f)
					{
						velocity.Y = 0.5f;
					}
					else if (velocity.Y < -0.5f)
					{
						velocity.Y = -0.5f;
					}
				}
			}
			else if (type != 59 && num < 3f)
			{
				if (velocity.Y > 1f)
				{
					velocity.Y = 1f;
				}
				else if (velocity.Y < -1f)
				{
					velocity.Y = -1f;
				}
			}
		}
	}

	public static void MudWallRunner(int i, int j)
	{
		Vector2 vector = new Vector2(i, j);
		Vector2 vector2 = default;
		float num = genRand.Next(5, 15);
		float num2 = genRand.Next(5, 20);
		float num3 = num2;
		vector2.X = (float)genRand.Next(-10, 11) * 0.1f;
		vector2.Y = (float)genRand.Next(-10, 11) * 0.1f;
		while (num > 0f && num3 > 0f)
		{
			float num4 = num * (num3 / num2);
			num3--;
			int num5 = (int)(vector.X - num4 * 0.5f);
			int num6 = (int)(vector.X + num4 * 0.5f);
			int num7 = (int)(vector.Y - num4 * 0.5f);
			int num8 = (int)(vector.Y + num4 * 0.5f);
			if (num5 < 0)
			{
				num5 = 0;
			}
			if (num6 > Main.MaxTilesX)
			{
				num6 = Main.MaxTilesX;
			}
			if (num7 < 0)
			{
				num7 = 0;
			}
			if (num8 > Main.MaxTilesY)
			{
				num8 = Main.MaxTilesY;
			}
			for (int k = num5; k < num6; k++)
			{
				float num9 = Math.Abs((float)k - vector.X);
				for (int l = num7; l < num8; l++)
				{
					if (num9 + Math.Abs((float)l - vector.Y) < num * 0.5f * (1f + (float)genRand.Next(-10, 11) * 0.015f))
					{
						Main.TileSet[k, l].WallType = 0;
					}
				}
			}
			vector.X += vector2.X;
			vector.Y += vector2.Y;
			vector2.X += (float)genRand.Next(-10, 11) * 0.05f;
			if (vector2.X > 1f)
			{
				vector2.X = 1f;
			}
			else if (vector2.X < -1f)
			{
				vector2.X = -1f;
			}
			vector2.Y += (float)genRand.Next(-10, 11) * 0.05f;
			if (vector2.Y > 1f)
			{
				vector2.Y = 1f;
			}
			else if (vector2.Y < -1f)
			{
				vector2.Y = -1f;
			}
		}
	}

	public static void FloatingIsland(int i, int j)
	{
		Vector2 vector = new Vector2(i, j);
		Vector2 vector2 = default;
		float num = genRand.Next(80, 120);
		float num2 = num;
		float num3 = genRand.Next(20, 25);
		vector2.X = (float)genRand.Next(-20, 21) * 0.2f;
		while (vector2.X > -2f && vector2.X < 2f)
		{
			vector2.X = (float)genRand.Next(-20, 21) * 0.2f;
		}
		vector2.Y = (float)genRand.Next(-20, -10) * 0.02f;
		while (num > 0f && num3 > 0f)
		{
			num -= (float)genRand.Next(4);
			num3--;
			int num4 = (int)(vector.X - num * 0.5f);
			int num5 = (int)(vector.X + num * 0.5f);
			int num6 = (int)(vector.Y - num * 0.5f);
			int num7 = (int)(vector.Y + num * 0.5f);
			if (num4 < 0)
			{
				num4 = 0;
			}
			if (num5 > Main.MaxTilesX)
			{
				num5 = Main.MaxTilesX;
			}
			if (num7 > Main.MaxTilesY)
			{
				num7 = Main.MaxTilesY;
			}
			num2 = num * (float)genRand.Next(80, 120) * 0.01f;
			float num8 = num2 * 0.4f;
			num8 *= num8;
			int num9 = (int)vector.Y + 1;
			for (int k = num4; k < num5; k++)
			{
				if (genRand.Next(2) == 0)
				{
					num9 += genRand.Next(-1, 2);
				}
				if (num9 < (int)vector.Y)
				{
					num9 = (int)vector.Y;
				}
				else if (num9 > (int)vector.Y + 2)
				{
					num9 = (int)vector.Y + 2;
				}
				float num10 = (float)k - vector.X;
				num10 *= num10;
				for (int l = ((num6 < num9) ? num9 : num6); l < num7; l++)
				{
					float num11 = ((float)l - vector.Y) * 2f;
					if (num10 + num11 * num11 < num8)
					{
						Main.TileSet[k, l].IsActive = 1;
						if (Main.TileSet[k, l].Type == 59)
						{
							Main.TileSet[k, l].Type = 0;
						}
					}
				}
			}
			TileRunner(genRand.Next(num4 + 10, num5 - 10), (int)(vector.Y + num2 * 0.1f + 5f), genRand.Next(5, 10), genRand.Next(10, 15), 0, addTile: true, new Vector2(0f, 2f), noYChange: true);
			num4 = (int)(vector.X - num * 0.4f);
			num5 = (int)(vector.X + num * 0.4f);
			num6 = (int)(vector.Y - num * 0.4f);
			num7 = (int)(vector.Y + num * 0.4f);
			num9 = (int)vector.Y + 2;
			if (num4 < 0)
			{
				num4 = 0;
			}
			if (num5 > Main.MaxTilesX)
			{
				num5 = Main.MaxTilesX;
			}
			if (num6 < num9)
			{
				num6 = num9;
			}
			if (num7 > Main.MaxTilesY)
			{
				num7 = Main.MaxTilesY;
			}
			num2 = num * (float)genRand.Next(80, 120) * 0.01f;
			num2 *= 0.4f;
			num2 *= num2;
			for (int m = num4; m < num5; m++)
			{
				float num12 = (float)m - vector.X;
				num12 *= num12;
				for (int n = num6; n < num7; n++)
				{
					float num13 = ((float)n - vector.Y) * 2f;
					if (num12 + num13 * num13 < num2)
					{
						Main.TileSet[m, n].WallType = 2;
					}
				}
			}
			vector.X += vector2.X;
			vector.Y += vector2.Y;
			vector2.Y += (float)genRand.Next(-10, 11) * 0.05f;
			if (vector2.X > 1f)
			{
				vector2.X = 1f;
			}
			else if (vector2.X < -1f)
			{
				vector2.X = -1f;
			}
			if (vector2.Y > 0.2f)
			{
				vector2.Y = -0.2f;
			}
			else if (vector2.Y < -0.2f)
			{
				vector2.Y = -0.2f;
			}
		}
	}

	public static void Caverer(int X, int Y)
	{
		int num = genRand.Next(2);
		double num2 = (double)genRand.Next(100) * 0.01;
		double num3 = 1.0 - num2;
		if (genRand.Next(2) == 0)
		{
			num2 = 0.0 - num2;
		}
		if (genRand.Next(2) == 0)
		{
			num3 = 0.0 - num3;
		}
		Vector2 pos = new Vector2(X, Y);
		if (num == 0)
		{
			for (int num4 = genRand.Next(6, 8); num4 >= 0; num4--)
			{
				digTunnel(ref pos, num2, num3, genRand.Next(6, 20), genRand.Next(4, 9));
				num2 += (double)genRand.Next(-20, 21) * 0.1;
				num3 += (double)genRand.Next(-20, 21) * 0.1;
				if (num2 < -1.5)
				{
					num2 = -1.5;
				}
				else if (num2 > 1.5)
				{
					num2 = 1.5;
				}
				if (num3 < -1.5)
				{
					num3 = -1.5;
				}
				else if (num3 > 1.5)
				{
					num3 = 1.5;
				}
				double num5 = (double)genRand.Next(100) * 0.01;
				double num6 = 1.0 - num5;
				if (genRand.Next(2) == 0)
				{
					num5 = 0.0 - num5;
				}
				if (genRand.Next(2) == 0)
				{
					num6 = 0.0 - num6;
				}
				Vector2 pos2 = pos;
				digTunnel(ref pos2, num5, num6, genRand.Next(30, 50), genRand.Next(3, 6));
				TileRunner((int)pos2.X, (int)pos2.Y, genRand.Next(10, 20), genRand.Next(5, 10), -1);
			}
			return;
		}
		for (int num7 = genRand.Next(14, 29); num7 >= 0; num7--)
		{
			digTunnel(ref pos, num2, num3, genRand.Next(5, 15), genRand.Next(2, 6), Wet: true);
			num2 += (double)genRand.Next(-20, 21) * 0.1;
			num3 += (double)genRand.Next(-20, 21) * 0.1;
			if (num2 < -1.5)
			{
				num2 = -1.5;
			}
			else if (num2 > 1.5)
			{
				num2 = 1.5;
			}
			if (num3 < -1.5)
			{
				num3 = -1.5;
			}
			else if (num3 > 1.5)
			{
				num3 = 1.5;
			}
		}
	}

	public static void digTunnel(ref Vector2 pos, double xDir, double yDir, int Steps, int Size, bool Wet = false)
	{
		try
		{
			double num = 0.0;
			double num2 = 0.0;
			double num3 = Size;
			while (Steps > 0)
			{
				Steps--;
				for (int i = (int)((double)pos.X - num3); (double)i <= (double)pos.X + num3; i++)
				{
					float num4 = Math.Abs((float)i - pos.X);
					for (int j = (int)((double)pos.Y - num3); (double)j <= (double)pos.Y + num3; j++)
					{
						if ((double)(num4 + Math.Abs((float)j - pos.Y)) < num3 * (1.0 + (double)genRand.Next(-10, 11) * 0.005))
						{
							Main.TileSet[i, j].IsActive = 0;
							if (Wet)
							{
								Main.TileSet[i, j].Liquid = byte.MaxValue;
							}
						}
					}
				}
				num3 += (double)genRand.Next(-50, 51) * 0.03;
				if (num3 < (double)Size * 0.6)
				{
					num3 = (double)Size * 0.6;
				}
				else if (num3 > (double)(Size * 2))
				{
					num3 = Size * 2;
				}
				num += (double)genRand.Next(-20, 21) * 0.01;
				num2 += (double)genRand.Next(-20, 21) * 0.01;
				if (num < -1.0)
				{
					num = -1.0;
				}
				else if (num > 1.0)
				{
					num = 1.0;
				}
				if (num2 < -1.0)
				{
					num2 = -1.0;
				}
				else if (num2 > 1.0)
				{
					num2 = 1.0;
				}
				pos.X = (float)((double)pos.X + (xDir + num) * 0.6);
				pos.Y = (float)((double)pos.Y + (yDir + num2) * 0.6);
			}
		}
		catch
		{
		}
	}

	public static void IslandHouse(int i, int j)
	{
		byte type = (byte)genRand.Next(45, 48);
		byte wallType = (byte)genRand.Next(10, 13);
		Vector2 vector = new Vector2(i, j);
		int num = 1;
		if (genRand.Next(2) == 0)
		{
			num = -1;
		}
		int num2 = genRand.Next(7, 12);
		int num3 = genRand.Next(5, 7);
		vector.X = i + (num2 + 2) * num;
		for (int k = j - 15; k < j + 30; k++)
		{
			if (Main.TileSet[(int)vector.X, k].IsActive != 0)
			{
				vector.Y = k - 1;
				break;
			}
		}
		vector.X = i;
		int num4 = (int)(vector.X - (float)num2 - 2f);
		int num5 = (int)(vector.X + (float)num2 + 2f);
		int num6 = (int)(vector.Y - (float)num3 - 2f);
		int num7 = (int)(vector.Y + 2f + (float)genRand.Next(3, 5));
		if (num4 < 0)
		{
			num4 = 0;
		}
		if (num5 > Main.MaxTilesX)
		{
			num5 = Main.MaxTilesX;
		}
		if (num6 < 0)
		{
			num6 = 0;
		}
		if (num7 > Main.MaxTilesY)
		{
			num7 = Main.MaxTilesY;
		}
		for (int l = num4; l <= num5; l++)
		{
			for (int m = num6; m < num7; m++)
			{
				Main.TileSet[l, m].IsActive = 1;
				Main.TileSet[l, m].Type = type;
				Main.TileSet[l, m].WallType = 0;
			}
		}
		num4 = (int)(vector.X - (float)num2);
		num5 = (int)(vector.X + (float)num2);
		num6 = (int)(vector.Y - (float)num3);
		num7 = (int)(vector.Y + 1f);
		if (num4 < 0)
		{
			num4 = 0;
		}
		if (num5 > Main.MaxTilesX)
		{
			num5 = Main.MaxTilesX;
		}
		if (num6 < 0)
		{
			num6 = 0;
		}
		if (num7 > Main.MaxTilesY)
		{
			num7 = Main.MaxTilesY;
		}
		for (int n = num4; n <= num5; n++)
		{
			for (int num8 = num6; num8 < num7; num8++)
			{
				if (Main.TileSet[n, num8].WallType == 0)
				{
					Main.TileSet[n, num8].IsActive = 0;
					Main.TileSet[n, num8].WallType = wallType;
				}
			}
		}
		int num9 = i + (num2 + 1) * num;
		int num10 = (int)vector.Y;
		for (int num11 = num9 - 2; num11 <= num9 + 2; num11++)
		{
			Main.TileSet[num11, num10].IsActive = 0;
			Main.TileSet[num11, num10 - 1].IsActive = 0;
			Main.TileSet[num11, num10 - 2].IsActive = 0;
		}
		PlaceTile(num9, num10, 10, ToMute: true);
		int contain = 0;
		int num12 = houseCount;
		if (num12 > 2)
		{
			num12 = genRand.Next(3);
		}
		switch (num12)
		{
		case 0:
			contain = 159;
			break;
		case 1:
			contain = 65;
			break;
		case 2:
			contain = 158;
			break;
		}
		AddBuriedChest(i, num10 - 3, contain, notNearOtherChests: false, 2);
		houseCount++;
	}

	public static void Mountinater(int i, int j)
	{
		Vector2 vector = new Vector2(i, j);
		Vector2 vector2 = default;
		double num = genRand.Next(80, 120);
		double num2 = num;
		float num3 = genRand.Next(40, 55);
		vector.Y += num3 * 0.5f;
		vector2.X = (float)genRand.Next(-10, 11) * 0.1f;
		vector2.Y = (float)genRand.Next(-20, -10) * 0.1f;
		while (num > 0.0 && num3 > 0f)
		{
			num -= (double)genRand.Next(4);
			num3--;
			int num4 = (int)((double)vector.X - num * 0.5);
			int num5 = (int)((double)vector.X + num * 0.5);
			int num6 = (int)((double)vector.Y - num * 0.5);
			int num7 = (int)((double)vector.Y + num * 0.5);
			if (num4 < 0)
			{
				num4 = 0;
			}
			if (num5 > Main.MaxTilesX)
			{
				num5 = Main.MaxTilesX;
			}
			if (num6 < 0)
			{
				num6 = 0;
			}
			if (num7 > Main.MaxTilesY)
			{
				num7 = Main.MaxTilesY;
			}
			num2 = num * (double)genRand.Next(80, 120) * 0.01;
			num2 *= 0.4;
			num2 *= num2;
			for (int k = num4; k < num5; k++)
			{
				double num8 = (float)k - vector.X;
				num8 *= num8;
				for (int l = num6; l < num7; l++)
				{
					double num9 = (float)l - vector.Y;
					if (num8 + num9 * num9 < num2 && Main.TileSet[k, l].IsActive == 0)
					{
						Main.TileSet[k, l].IsActive = 1;
						Main.TileSet[k, l].Type = 0;
					}
				}
			}
			vector.X += vector2.X;
			vector.Y += vector2.Y;
			vector2.X += (float)genRand.Next(-10, 11) * 0.05f;
			vector2.Y += (float)genRand.Next(-10, 11) * 0.05f;
			if (vector2.X > 0.5f)
			{
				vector2.X = 0.5f;
			}
			else if (vector2.X < -0.5f)
			{
				vector2.X = -0.5f;
			}
			if (vector2.Y > -0.5f)
			{
				vector2.Y = -0.5f;
			}
			else if (vector2.Y < -1.5f)
			{
				vector2.Y = -1.5f;
			}
		}
	}

	public static void Lakinater(int i, int j)
	{
		Vector2 vector = new Vector2(i, j);
		Vector2 vector2 = default;
		double num = genRand.Next(25, 50);
		double num2 = num;
		double num3 = genRand.Next(30, 80);
		if (genRand.Next(5) == 0)
		{
			num *= 1.5;
			num2 *= 1.5;
			num3 *= 1.2;
		}
		vector.Y = (float)((double)vector.Y - num3 * 0.3);
		vector2.X = (float)genRand.Next(-10, 11) * 0.1f;
		vector2.Y = (float)genRand.Next(-20, -10) * 0.1f;
		while (num > 0.0 && num3 > 0.0)
		{
			if ((double)vector.Y + num2 * 0.5 > (double)Main.WorldSurface)
			{
				num3 = 0.0;
			}
			num -= (double)genRand.Next(3);
			num3--;
			int num4 = (int)((double)vector.X - num * 0.5);
			int num5 = (int)((double)vector.X + num * 0.5);
			int num6 = (int)((double)vector.Y - num * 0.5);
			int num7 = (int)((double)vector.Y + num * 0.5);
			if (num4 < 0)
			{
				num4 = 0;
			}
			if (num5 > Main.MaxTilesX)
			{
				num5 = Main.MaxTilesX;
			}
			if (num6 < 0)
			{
				num6 = 0;
			}
			if (num7 > Main.MaxTilesY)
			{
				num7 = Main.MaxTilesY;
			}
			num2 = num * (double)genRand.Next(80, 120) * 0.01;
			num2 *= 0.4;
			num2 *= num2;
			for (int k = num4; k < num5; k++)
			{
				double num8 = (float)k - vector.X;
				num8 *= num8;
				for (int l = num6; l < num7; l++)
				{
					double num9 = (float)l - vector.Y;
					if (num8 + num9 * num9 < num2 && Main.TileSet[k, l].IsActive != 0)
					{
						Main.TileSet[k, l].IsActive = 0;
						Main.TileSet[k, l].Liquid = byte.MaxValue;
					}
				}
			}
			vector.X += vector2.X;
			vector.Y += vector2.Y;
			vector2.X += (float)genRand.Next(-10, 11) * 0.05f;
			vector2.Y += (float)genRand.Next(-10, 11) * 0.05f;
			if (vector2.X > 0.5f)
			{
				vector2.X = 0.5f;
			}
			else if (vector2.X < -0.5f)
			{
				vector2.X = -0.5f;
			}
			if (vector2.Y > 1.5f)
			{
				vector2.Y = 1.5f;
			}
			else if (vector2.Y < 0.5f)
			{
				vector2.Y = 0.5f;
			}
		}
	}

	public static void ShroomPatch(int i, int j)
	{
		Vector2 vector = new Vector2(i, j);
		Vector2 vector2 = default;
		double num = genRand.Next(40, 70);
		double num2 = num;
		double num3 = genRand.Next(20, 30);
		if (genRand.Next(5) == 0)
		{
			num *= 1.5;
			num2 *= 1.5;
			num3 *= 1.2;
		}
		vector.Y -= (float)(num3 * 0.3);
		vector2.X = (float)genRand.Next(-10, 11) * 0.1f;
		vector2.Y = (float)genRand.Next(-20, -10) * 0.1f;
		while (num > 0.0 && num3 > 0.0)
		{
			num -= (double)genRand.Next(3);
			num3--;
			int num4 = (int)((double)vector.X - num * 0.5);
			int num5 = (int)((double)vector.X + num * 0.5);
			int num6 = (int)((double)vector.Y - num * 0.5);
			int num7 = (int)((double)vector.Y + num * 0.5);
			if (num4 < 0)
			{
				num4 = 0;
			}
			if (num5 > Main.MaxTilesX)
			{
				num5 = Main.MaxTilesX;
			}
			if (num6 < 0)
			{
				num6 = 0;
			}
			if (num7 > Main.MaxTilesY)
			{
				num7 = Main.MaxTilesY;
			}
			num2 = num * (double)genRand.Next(80, 120) * 0.01;
			float num8 = (float)num2 * 0.4f;
			num8 *= num8;
			for (int k = num4; k < num5; k++)
			{
				float num9 = (float)k - vector.X;
				num9 *= num9;
				for (int l = num6; l < num7; l++)
				{
					float num10 = ((float)l - vector.Y) * 2.3f;
					if (!(num9 + num10 * num10 < num8))
					{
						continue;
					}
					if ((double)l < (double)vector.Y + num2 * 0.02)
					{
						if (Main.TileSet[k, l].Type != 59)
						{
							Main.TileSet[k, l].IsActive = 0;
						}
					}
					else
					{
						Main.TileSet[k, l].Type = 59;
					}
					Main.TileSet[k, l].Liquid = 0;
					Main.TileSet[k, l].Lava = 0;
				}
			}
			vector.X += vector2.X;
			vector.X += vector2.X;
			vector.Y += vector2.Y;
			vector2.X += (float)genRand.Next(-10, 11) * 0.05f;
			vector2.Y -= (float)genRand.Next(11) * 0.05f;
			if (vector2.X > -0.5f && vector2.X < 0.5f)
			{
				if (vector2.X < 0f)
				{
					vector2.X = -0.5f;
				}
				else
				{
					vector2.X = 0.5f;
				}
			}
			if (vector2.X > 2f)
			{
				vector2.X = 1f;
			}
			else if (vector2.X < -2f)
			{
				vector2.X = -1f;
			}
			if (vector2.Y > 1f)
			{
				vector2.Y = 1f;
			}
			else if (vector2.Y < -1f)
			{
				vector2.Y = -1f;
			}
			int num11 = (int)vector.X;
			int num12 = (int)vector.Y;
			for (int m = 0; m < 2; m++)
			{
				int num13;
				int num14;
				do
				{
					num13 = num11 + genRand.Next(-20, 20);
					num14 = num12 + genRand.Next(20);
				}
				while (Main.TileSet[num13, num14].IsActive == 0 && Main.TileSet[num13, num14].Type != 59);
				int num15 = genRand.Next(7, 10);
				int num16 = genRand.Next(7, 10);
				TileRunner(num13, num14, num15, num16, 59, addTile: false, new Vector2(0f, 2f), noYChange: true);
				if (genRand.Next(3) == 0)
				{
					TileRunner(num13, num14, num15 - 3, num16 - 3, -1, addTile: false, new Vector2(0f, 2f), noYChange: true);
				}
			}
		}
	}

	public static void Cavinator(int i, int j, int steps)
	{
		Vector2 vector = new Vector2(i, j);
		Vector2 vector2 = default;
		double num = genRand.Next(7, 15);
		double num2 = num;
		int num3 = 1;
		if (genRand.Next(2) == 0)
		{
			num3 = -1;
		}
		int num4 = genRand.Next(20, 40);
		vector2.Y = (float)genRand.Next(10, 20) * 0.01f;
		vector2.X = num3;
		while (num4 > 0)
		{
			num4--;
			int num5 = (int)((double)vector.X - num * 0.5);
			int num6 = (int)((double)vector.X + num * 0.5);
			int num7 = (int)((double)vector.Y - num * 0.5);
			int num8 = (int)((double)vector.Y + num * 0.5);
			if (num5 < 0)
			{
				num5 = 0;
			}
			if (num6 > Main.MaxTilesX)
			{
				num6 = Main.MaxTilesX;
			}
			if (num7 < 0)
			{
				num7 = 0;
			}
			if (num8 > Main.MaxTilesY)
			{
				num8 = Main.MaxTilesY;
			}
			num2 = num * (double)genRand.Next(80, 120) * 0.01;
			num2 *= 0.4;
			num2 *= num2;
			for (int k = num5; k < num6; k++)
			{
				double num9 = (float)k - vector.X;
				num9 *= num9;
				for (int l = num7; l < num8; l++)
				{
					double num10 = (float)l - vector.Y;
					if (num9 + num10 * num10 < num2)
					{
						Main.TileSet[k, l].IsActive = 0;
					}
				}
			}
			vector.X += vector2.X;
			vector.Y += vector2.Y;
			vector2.X += (float)genRand.Next(-10, 11) * 0.05f;
			vector2.Y += (float)genRand.Next(-10, 11) * 0.05f;
			if (vector2.X > (float)num3 + 0.5f)
			{
				vector2.X = (float)num3 + 0.5f;
			}
			else if (vector2.X < (float)num3 - 0.5f)
			{
				vector2.X = (float)num3 - 0.5f;
			}
			if (vector2.Y > 2f)
			{
				vector2.Y = 2f;
			}
			else if (vector2.Y < 0f)
			{
				vector2.Y = 0f;
			}
		}
		if (steps > 0 && (int)vector.Y < Main.RockLayer + 50)
		{
			Cavinator((int)vector.X, (int)vector.Y, steps - 1);
		}
	}

	public static void CaveOpenater(int i, int j)
	{
		int num = (genRand.Next(2) << 1) - 1;
		Vector2 vector = new Vector2(i, j);
		Vector2 vector2 = new Vector2(0f, num);
		double num2 = genRand.Next(7, 12);
		double num3 = num2;
		int num4 = 100;
		do
		{
			num4 = ((Main.TileSet[(int)vector.X, (int)vector.Y].WallType != 0) ? (num4 - 1) : 0);
			int num5 = (int)((double)vector.X - num2 * 0.5);
			int num6 = (int)((double)vector.X + num2 * 0.5);
			int num7 = (int)((double)vector.Y - num2 * 0.5);
			int num8 = (int)((double)vector.Y + num2 * 0.5);
			if (num5 < 0)
			{
				num5 = 0;
			}
			if (num6 > Main.MaxTilesX)
			{
				num6 = Main.MaxTilesX;
			}
			if (num7 < 0)
			{
				num7 = 0;
			}
			if (num8 > Main.MaxTilesY)
			{
				num8 = Main.MaxTilesY;
			}
			num3 = num2 * (double)genRand.Next(80, 120) * 0.01;
			num3 *= 0.4;
			num3 *= num3;
			for (int k = num5; k < num6; k++)
			{
				double num9 = (float)k - vector.X;
				num9 *= num9;
				for (int l = num7; l < num8; l++)
				{
					double num10 = (float)l - vector.Y;
					if (num9 + num10 * num10 < num3)
					{
						Main.TileSet[k, l].IsActive = 0;
					}
				}
			}
			vector.X += vector2.X;
			vector.Y += vector2.Y;
			vector2.X += (float)genRand.Next(-10, 11) * 0.05f;
			vector2.Y += (float)genRand.Next(-10, 11) * 0.05f;
			if (vector2.X > (float)num + 0.5f)
			{
				vector2.X = (float)num + 0.5f;
			}
			else if (vector2.X < (float)num - 0.5f)
			{
				vector2.X = (float)num - 0.5f;
			}
			if (vector2.Y > 0f)
			{
				vector2.Y = 0f;
			}
			else if (vector2.Y < -0.5f)
			{
				vector2.Y = -0.5f;
			}
		}
		while (num4 > 0);
	}

	public static void SquareTileFrame(int i, int j, int frameNumber = -1)
	{
		if (!Gen)
		{
			bool flag = tileFrameRecursion;
			tileFrameRecursion = false;
			TileFrame(i - 1, j - 1);
			TileFrame(i - 1, j);
			TileFrame(i - 1, j + 1);
			TileFrame(i, j - 1);
			TileFrame(i, j, frameNumber);
			TileFrame(i, j + 1);
			TileFrame(i + 1, j - 1);
			TileFrame(i + 1, j);
			TileFrame(i + 1, j + 1);
			tileFrameRecursion = flag;
		}
	}

	public static void SquareTileFrameNoLiquid(int i, int j, int frameNumber = -1)
	{
		TileFrameNoLiquid(i - 1, j - 1);
		TileFrameNoLiquid(i - 1, j);
		TileFrameNoLiquid(i - 1, j + 1);
		TileFrameNoLiquid(i, j - 1);
		TileFrameNoLiquid(i, j, frameNumber);
		TileFrameNoLiquid(i, j + 1);
		TileFrameNoLiquid(i + 1, j - 1);
		TileFrameNoLiquid(i + 1, j);
		TileFrameNoLiquid(i + 1, j + 1);
	}

	public static void SquareWallFrame(int i, int j, bool resetFrame = true)
	{
		WallFrame(i - 1, j - 1);
		WallFrame(i - 1, j);
		WallFrame(i - 1, j + 1);
		WallFrame(i, j - 1);
		WallFrame(i, j, resetFrame);
		WallFrame(i, j + 1);
		WallFrame(i + 1, j - 1);
		WallFrame(i + 1, j);
		WallFrame(i + 1, j + 1);
	}

	public static void SectionTileFrame(int startX, int startY)
	{
		int num = startX - 1;
		int num2 = startX + 40;
		int num3 = startY - 1;
		int num4 = startY + 30;
		if (num < 5)
		{
			num = 5;
		}
		if (num3 < 5)
		{
			num3 = 5;
		}
		if (num2 > Main.MaxTilesX - 5)
		{
			num2 = Main.MaxTilesX - 5;
		}
		if (num4 > Main.MaxTilesY - 5)
		{
			num4 = Main.MaxTilesY - 5;
		}
		tileFrameRecursion = false;
		for (int i = num; i <= num2; i++)
		{
			for (int j = num3; j <= num4; j++)
			{
				int type = Main.TileSet[i, j].Type;
				if (type == 4 || !Main.TileFrameImportant[type])
				{
					TileFrame(i, j, -1);
				}
				WallFrame(i, j, resetFrame: true);
			}
		}
		tileFrameRecursion = true;
	}

	public static void RangeFrame(int startX, int startY, int endX, int endY)
	{
		if (Gen)
		{
			return;
		}
		bool flag = tileFrameRecursion;
		tileFrameRecursion = false;
		for (int i = startX - 1; i < endX + 2; i++)
		{
			for (int j = startY - 1; j < endY + 2; j++)
			{
				TileFrame(i, j);
				WallFrame(i, j);
			}
		}
		tileFrameRecursion = flag;
	}

	public unsafe static void WaterCheck()
	{
		Liquid.NumLiquids = 0;
		LiquidBuffer.NumLiquidBuffer = 0;
		fixed (Tile* tileSet = Main.TileSet)
		{
			for (int num = Main.MaxTilesX - 2; num > 0; num--)
			{
				Tile* ptr = tileSet + (num * 1440 + Main.MaxTilesY - 2);
				int num2 = Main.MaxTilesY - 2;
				while (num2 > 0)
				{
					ptr->CheckingLiquid = 0;
					if (ptr->Liquid > 0)
					{
						if (ptr->IsActive != 0 && Main.TileSolidNotSolidTop[ptr->Type])
						{
							ptr->Liquid = 0;
						}
						else
						{
							if (ptr->IsActive != 0)
							{
								if (Main.TileWaterDeath[ptr->Type] && (ptr->Type != 4 || ptr->FrameY != 176))
								{
									KillTile(num, num2);
								}
								if (ptr->Lava != 0 && Main.TileLavaDeath[ptr->Type])
								{
									KillTile(num, num2);
								}
							}
							Tile* ptr2 = ptr + 1;
							if ((ptr2->IsActive == 0 || !Main.TileSolidNotSolidTop[ptr2->Type]) && ptr2->Liquid < byte.MaxValue)
							{
								if (ptr2->Liquid > 250)
								{
									ptr2->Liquid = byte.MaxValue;
								}
								else
								{
									Liquid.AddWater(num, num2);
								}
							}
							ptr2 = ptr - 1440;
							if ((ptr2->IsActive == 0 || !Main.TileSolidNotSolidTop[ptr2->Type]) && ptr2->Liquid != ptr->Liquid)
							{
								Liquid.AddWater(num, num2);
							}
							else
							{
								ptr2 = ptr + 1440;
								if ((ptr2->IsActive == 0 || !Main.TileSolidNotSolidTop[ptr2->Type]) && ptr2->Liquid != ptr->Liquid)
								{
									Liquid.AddWater(num, num2);
								}
							}
							if (ptr->Lava != 0)
							{
								ptr2 = ptr - 1;
								if (ptr2->Liquid > 0 && ptr2->Lava == 0)
								{
									Liquid.AddWater(num, num2);
								}
								else
								{
									ptr2 = ptr + 1;
									if (ptr2->Liquid > 0 && ptr2->Lava == 0)
									{
										Liquid.AddWater(num, num2);
									}
									else
									{
										ptr2 = ptr - 1440;
										if (ptr2->Liquid > 0 && ptr2->Lava == 0)
										{
											Liquid.AddWater(num, num2);
										}
										else
										{
											ptr2 = ptr + 1440;
											if (ptr2->Liquid > 0 && ptr2->Lava == 0)
											{
												Liquid.AddWater(num, num2);
											}
										}
									}
								}
							}
						}
					}
					num2--;
					ptr--;
				}
			}
		}
	}

	public unsafe static void everyTileFrame()
	{
		UI.MainUI.NextProgressStep(Lang.WorldGenText[55]);
		Gen = true;
		fixed (Tile* tileSet = Main.TileSet)
		{
			for (int i = 0; i < Main.MaxTilesX; i++)
			{
				if ((i & 0x3F) == 0)
				{
					UI.MainUI.Progress = (float)i / (float)Main.MaxTilesX;
				}
				Tile* ptr = tileSet + (1440 * i + Main.MaxTilesY);
				for (int num = Main.MaxTilesY - 1; num >= 0; num--)
				{
					ptr--;
					if (ptr->IsActive != 0)
					{
						TileFrameNoLiquid(i, num, -1);
					}
					if (ptr->WallType > 0)
					{
						WallFrame(i, num, resetFrame: true);
					}
				}
			}
		}
		Gen = false;
	}

	private static void PlantCheck(int i, int j)
	{
		EntityID.TileID tileID = EntityID.TileID.NONE;
		EntityID.TileID tileID2 = (EntityID.TileID)Main.TileSet[i, j].Type;
		if (j + 1 >= Main.MaxTilesY)
		{
			tileID = tileID2;
		}
		if (j + 1 < Main.MaxTilesY && Main.TileSet[i, j + 1].IsActive != 0)
		{
			tileID = (EntityID.TileID)Main.TileSet[i, j + 1].Type;
		}
		if ((tileID2 != EntityID.TileID.SHORT_GRASS_PLANTS || tileID == EntityID.TileID.GRASS || tileID == EntityID.TileID.CLAY_POT) && (tileID2 != EntityID.TileID.SHORT_CORRUPT_PLANTS || tileID == EntityID.TileID.CORRUPT_GRASS) && (tileID2 != EntityID.TileID.SHORT_JUNGLE_PLANTS || tileID == EntityID.TileID.JUNGLE_GRASS) && (tileID2 != EntityID.TileID.GLOWING_MUSHROOM || tileID == EntityID.TileID.MUSHROOM_GRASS) && (tileID2 != EntityID.TileID.TALL_GRASS_PLANTS || tileID == EntityID.TileID.GRASS || tileID == EntityID.TileID.CLAY_POT) && (tileID2 != EntityID.TileID.TALL_JUNGLE_PLANTS || tileID == EntityID.TileID.JUNGLE_GRASS) && (tileID2 != EntityID.TileID.SHORT_HALLOWED_PLANTS || tileID == EntityID.TileID.HALLOWED_GRASS) && (tileID2 != EntityID.TileID.TALL_HALLOWED_PLANTS || tileID == EntityID.TileID.HALLOWED_GRASS))
		{
			return;
		}
		switch (tileID)
		{
		case EntityID.TileID.CORRUPT_GRASS:
			tileID2 = EntityID.TileID.SHORT_CORRUPT_PLANTS;
			if (Main.TileSet[i, j].FrameX >= 162)
			{
				Main.TileSet[i, j].FrameX = 126;
			}
			break;
		case EntityID.TileID.GRASS:
			tileID2 = ((tileID2 != EntityID.TileID.TALL_HALLOWED_PLANTS) ? EntityID.TileID.SHORT_GRASS_PLANTS : EntityID.TileID.TALL_GRASS_PLANTS);
			break;
		case EntityID.TileID.HALLOWED_GRASS:
			tileID2 = ((tileID2 != EntityID.TileID.TALL_GRASS_PLANTS) ? EntityID.TileID.SHORT_HALLOWED_PLANTS : EntityID.TileID.TALL_HALLOWED_PLANTS);
			break;
		}
		if (tileID2 != (EntityID.TileID)Main.TileSet[i, j].Type)
		{
			Main.TileSet[i, j].Type = (byte)tileID2;
		}
		else
		{
			KillTile(i, j);
		}
	}

	public unsafe static void WallFrame(int i, int j, bool resetFrame = false)
	{
		if (i < 0 || j < 0 || i >= Main.MaxTilesX || j >= Main.MaxTilesY)
		{
			return;
		}
		fixed (Tile* ptr = &Main.TileSet[i, j])
		{
			int wallType = ptr->WallType;
			if (wallType == 0)
			{
				return;
			}
			int num = wallType;
			int num2 = wallType;
			int num3 = wallType;
			int num4 = wallType;
			int num5 = wallType;
			int num6 = wallType;
			int num7 = wallType;
			int num8 = wallType;
			int num9 = -1;
			int num10 = -1;
			if (j - 1 >= 0)
			{
				num2 = ptr[-1].WallType;
			}
			if (j + 1 < Main.MaxTilesY)
			{
				num7 = ptr[1].WallType;
			}
			if (i - 1 >= 0)
			{
				num4 = ptr[-1440].WallType;
				if (j - 1 >= 0)
				{
					num = ptr[-1441].WallType;
				}
				if (j + 1 < Main.MaxTilesY)
				{
					num6 = ptr[-1439].WallType;
				}
			}
			if (i + 1 < Main.MaxTilesX)
			{
				num5 = ptr[1440].WallType;
				if (j - 1 >= 0)
				{
					num3 = ptr[1439].WallType;
				}
				if (j + 1 < Main.MaxTilesY)
				{
					num8 = ptr[1441].WallType;
				}
			}
			if (wallType == 2 && j >= Main.WorldSurface)
			{
				num7 = wallType;
				num6 = wallType;
				num8 = wallType;
				if (j > Main.WorldSurface)
				{
					num2 = wallType;
					num = wallType;
					num3 = wallType;
					num4 = wallType;
					num5 = wallType;
				}
			}
			else
			{
				if (num7 > 0)
				{
					num7 = wallType;
				}
				if (num6 > 0)
				{
					num6 = wallType;
				}
				if (num8 > 0)
				{
					num8 = wallType;
				}
			}
			if (num2 > 0)
			{
				num2 = wallType;
			}
			if (num > 0)
			{
				num = wallType;
			}
			if (num3 > 0)
			{
				num3 = wallType;
			}
			if (num4 > 0)
			{
				num4 = wallType;
			}
			if (num5 > 0)
			{
				num5 = wallType;
			}
			int num11;
			if (resetFrame)
			{
				num11 = genRand.Next(3);
				Main.TileSet[i, j].wallFrameNumber = num11;
			}
			else
			{
				num11 = Main.TileSet[i, j].wallFrameNumber;
			}
			if (num9 < 0 || num10 < 0)
			{
				if (num2 == wallType && num7 == wallType && num4 == wallType && num5 == wallType)
				{
					if (num != wallType && num3 != wallType)
					{
						num9 = 108 + num11 * 18;
						num10 = 18;
					}
					else if (num6 != wallType && num8 != wallType)
					{
						num9 = 108 + num11 * 18;
						num10 = 36;
					}
					else if (num != wallType && num6 != wallType)
					{
						num9 = 180;
						num10 = num11 * 18;
					}
					else if (num3 != wallType && num8 != wallType)
					{
						num9 = 198;
						num10 = num11 * 18;
					}
					else
					{
						num9 = 18 + num11 * 18;
						num10 = 18;
					}
				}
				else if (num2 != wallType && num7 == wallType && num4 == wallType && num5 == wallType)
				{
					num9 = 18 + num11 * 18;
					num10 = 0;
				}
				else if (num2 == wallType && num7 != wallType && num4 == wallType && num5 == wallType)
				{
					num9 = 18 + num11 * 18;
					num10 = 36;
				}
				else if (num2 == wallType && num7 == wallType && num4 != wallType && num5 == wallType)
				{
					num9 = 0;
					num10 = num11 * 18;
				}
				else if (num2 == wallType && num7 == wallType && num4 == wallType && num5 != wallType)
				{
					num9 = 72;
					num10 = num11 * 18;
				}
				else if (num2 != wallType && num7 == wallType && num4 != wallType && num5 == wallType)
				{
					num9 = num11 * 36;
					num10 = 54;
				}
				else if (num2 != wallType && num7 == wallType && num4 == wallType && num5 != wallType)
				{
					num9 = 18 + num11 * 36;
					num10 = 54;
				}
				else if (num2 == wallType && num7 != wallType && num4 != wallType && num5 == wallType)
				{
					num9 = num11 * 36;
					num10 = 72;
				}
				else if (num2 == wallType && num7 != wallType && num4 == wallType && num5 != wallType)
				{
					num9 = 18 + num11 * 36;
					num10 = 72;
				}
				else if (num2 == wallType && num7 == wallType && num4 != wallType && num5 != wallType)
				{
					num9 = 90;
					num10 = num11 * 18;
				}
				else if (num2 != wallType && num7 != wallType && num4 == wallType && num5 == wallType)
				{
					num9 = 108 + num11 * 18;
					num10 = 72;
				}
				else if (num2 != wallType && num7 == wallType && num4 != wallType && num5 != wallType)
				{
					num9 = 108 + num11 * 18;
					num10 = 0;
				}
				else if (num2 == wallType && num7 != wallType && num4 != wallType && num5 != wallType)
				{
					num9 = 108 + num11 * 18;
					num10 = 54;
				}
				else if (num2 != wallType && num7 != wallType && num4 != wallType && num5 == wallType)
				{
					num9 = 162;
					num10 = num11 * 18;
				}
				else if (num2 != wallType && num7 != wallType && num4 == wallType && num5 != wallType)
				{
					num9 = 216;
					num10 = num11 * 18;
				}
				else if (num2 != wallType && num7 != wallType && num4 != wallType && num5 != wallType)
				{
					num9 = 162 + num11 * 18;
					num10 = 54;
				}
			}
			if (num9 < 0 || num10 < 0)
			{
				num9 = 18 + num11 * 18;
				num10 = 18;
			}
			ptr->wallFrameX = (ushort)num9;
			ptr->wallFrameY = (byte)num10;
		}
	}

	public unsafe static void TileFrame(int i, int j, int frameNumber = 0)
	{
		if (i <= 5 || j <= 5 || i >= Main.MaxTilesX - 5 || j >= Main.MaxTilesY - 5)
		{
			return;
		}
		fixed (Tile* ptr = &Main.TileSet[i, j])
		{
			if (ptr->Liquid > 0 && Main.NetMode != 1)
			{
				Liquid.AddWater(i, j);
			}
			if (ptr->IsActive == 0)
			{
				return;
			}
			int num = ptr->Type;
			if (Main.TileStone[num])
			{
				num = 1;
			}
			int frameX = ptr->FrameX;
			int frameY = ptr->FrameY;
			int num2 = -1;
			int num3 = -1;
			if (Main.TileFrameImportant[num])
			{
				switch ((EntityID.TileID)num)
				{
				case EntityID.TileID.POT:
					CheckPot(i, j);
					break;
				case EntityID.TileID.TREE:
					CheckTree(i, j);
					break;
				case EntityID.TileID.SHORT_GRASS_PLANTS:
				case EntityID.TileID.SHORT_CORRUPT_PLANTS:
				case EntityID.TileID.SHORT_JUNGLE_PLANTS:
				case EntityID.TileID.GLOWING_MUSHROOM:
				case EntityID.TileID.TALL_GRASS_PLANTS:
				case EntityID.TileID.TALL_JUNGLE_PLANTS:
				case EntityID.TileID.SHORT_HALLOWED_PLANTS:
				case EntityID.TileID.TALL_HALLOWED_PLANTS:
					PlantCheck(i, j);
					break;
				case EntityID.TileID.TORCH:
				{
					int num19 = ((ptr->FrameX >= 66) ? 66 : 0);
					int num20 = -1;
					int num21 = -1;
					int num22 = -1;
					int num23 = -1;
					int num24 = -1;
					int num25 = -1;
					int num26 = -1;
					if (ptr[-1].IsActive != 0)
					{
						_ = ptr[-1];
					}
					if (ptr[1].IsActive != 0)
					{
						num20 = ptr[1].Type;
					}
					if (ptr[-1440].IsActive != 0)
					{
						num21 = ptr[-1440].Type;
					}
					if (ptr[1440].IsActive != 0)
					{
						num22 = ptr[1440].Type;
					}
					if (ptr[-1439].IsActive != 0)
					{
						num23 = ptr[-1439].Type;
					}
					if (ptr[1441].IsActive != 0)
					{
						num24 = ptr[1441].Type;
					}
					if (ptr[-1441].IsActive != 0)
					{
						num25 = ptr[-1441].Type;
					}
					if (ptr[1439].IsActive != 0)
					{
						num26 = ptr[1439].Type;
					}
					if (num20 >= 0 && Main.TileSolidAndAttach[num20])
					{
						ptr->FrameX = (short)num19;
					}
					else if (num21 >= 0 && (Main.TileSolidAndAttach[num21] || num21 == 124 || (num21 == 5 && num25 == 5 && num23 == 5)))
					{
						ptr->FrameX = (short)(22 + num19);
					}
					else if (num22 >= 0 && (Main.TileSolidAndAttach[num22] || num22 == 124 || (num22 == 5 && num26 == 5 && num24 == 5)))
					{
						ptr->FrameX = (short)(44 + num19);
					}
					else
					{
						KillTile(i, j);
					}
					break;
				}
				case EntityID.TileID.SWITCH:
				{
					int num28 = -1;
					int num29 = -1;
					int num30 = -1;
					if (ptr[-1].IsActive != 0)
					{
						_ = ptr[-1];
					}
					if (ptr[1].IsActive != 0)
					{
						num28 = ptr[1].Type;
					}
					if (ptr[-1440].IsActive != 0)
					{
						num29 = ptr[-1440].Type;
					}
					if (ptr[1440].IsActive != 0)
					{
						num30 = ptr[1440].Type;
					}
					if (num28 >= 0 && Main.TileSolidAndAttach[num28])
					{
						ptr->FrameX = 0;
					}
					else if (num29 >= 0 && (Main.TileSolidAndAttach[num29] || num29 == 124 || num29 == 5))
					{
						ptr->FrameX = 18;
					}
					else if (num30 >= 0 && (Main.TileSolidAndAttach[num30] || num30 == 124 || num30 == 5))
					{
						ptr->FrameX = 36;
					}
					else
					{
						KillTile(i, j);
					}
					break;
				}
				case EntityID.TileID.CRYSTAL_SHARD:
				case EntityID.TileID.XMAS_LIGHT:
				{
					int num15 = -1;
					int num16 = -1;
					int num17 = -1;
					int num18 = -1;
					if (ptr[-1].IsActive != 0)
					{
						num16 = ptr[-1].Type;
					}
					if (ptr[1].IsActive != 0)
					{
						num15 = ptr[1].Type;
					}
					if (ptr[-1440].IsActive != 0)
					{
						num17 = ptr[-1440].Type;
					}
					if (ptr[1440].IsActive != 0)
					{
						num18 = ptr[1440].Type;
					}
					if (num15 >= 0 && Main.TileSolidNotSolidTop[num15])
					{
						ptr->FrameY = 0;
					}
					else if (num17 >= 0 && Main.TileSolidNotSolidTop[num17])
					{
						ptr->FrameY = 54;
					}
					else if (num18 >= 0 && Main.TileSolidNotSolidTop[num18])
					{
						ptr->FrameY = 36;
					}
					else if (num16 >= 0 && Main.TileSolidNotSolidTop[num16])
					{
						ptr->FrameY = 18;
					}
					else
					{
						KillTile(i, j);
					}
					break;
				}
				case EntityID.TileID.LIFE_CRYSTAL:
				case EntityID.TileID.SHADOW_ORB:
					CheckOrb(i, j, num);
					break;
				case EntityID.TileID.DOOR_CLOSED:
				{
					if (ToDestroyObject)
					{
						break;
					}
					int num27 = j - frameY / 18;
					bool flag2 = false;
					if (Main.TileSet[i, num27 - 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[i, num27 - 1].Type])
					{
						flag2 = true;
					}
					else if (Main.TileSet[i, num27 + 3].IsActive == 0 || !Main.TileSolid[Main.TileSet[i, num27 + 3].Type])
					{
						flag2 = true;
					}
					else if (Main.TileSet[i, num27].IsActive == 0 || Main.TileSet[i, num27].Type != num)
					{
						flag2 = true;
					}
					else if (Main.TileSet[i, num27 + 1].IsActive == 0 || Main.TileSet[i, num27 + 1].Type != num)
					{
						flag2 = true;
					}
					else if (Main.TileSet[i, num27 + 2].IsActive == 0 || Main.TileSet[i, num27 + 2].Type != num)
					{
						flag2 = true;
					}
					if (flag2)
					{
						ToDestroyObject = true;
						KillTile(i, num27);
						KillTile(i, num27 + 1);
						KillTile(i, num27 + 2);
						if (!Gen)
						{
							Item.NewItem(i * 16, j * 16, 16, 16, 25);
						}
					}
					ToDestroyObject = false;
					break;
				}
				case EntityID.TileID.DOOR_OPEN:
				{
					if (ToDestroyObject)
					{
						break;
					}
					int num10 = 0;
					int num11 = 0;
					int num12 = i;
					int num13 = j;
					bool flag = false;
					switch (frameX)
					{
					case 0:
						num10 = 1;
						break;
					case 18:
						num12 = i - 1;
						num11 = -1440;
						num10 = 1;
						break;
					case 36:
						num11 = 1440;
						num12 = i + 1;
						num10 = -1;
						break;
					case 54:
						num10 = -1;
						break;
					}
					switch (frameY)
					{
					case 18:
						num11--;
						num13 = j - 1;
						break;
					case 36:
						num13 = j - 2;
						num11 -= 2;
						break;
					}
					if (ptr[num11 - 1].IsActive == 0 || !Main.TileSolid[ptr[num11 - 1].Type] || ptr[num11 + 3].IsActive == 0 || !Main.TileSolid[ptr[num11 + 3].Type])
					{
						flag = true;
						ToDestroyObject = true;
						if (!Gen)
						{
							Item.NewItem(i * 16, j * 16, 16, 16, 25);
						}
					}
					int num14 = num12;
					if (num10 == -1)
					{
						num14 = num12 - 1;
					}
					for (int k = num14; k < num14 + 2; k++)
					{
						for (int l = num13; l < num13 + 3; l++)
						{
							if (!flag)
							{
								fixed (Tile* ptr2 = &Main.TileSet[k, l])
								{
									if (ptr2->Type != 11 || ptr2->IsActive == 0)
									{
										ToDestroyObject = true;
										flag = true;
										k = num14;
										l = num13;
										if (!Gen)
										{
											Item.NewItem(i * 16, j * 16, 16, 16, 25);
										}
									}
								}
							}
							if (flag)
							{
								KillTile(k, l);
							}
						}
					}
					ToDestroyObject = false;
					break;
				}
				case EntityID.TileID.CHANDELIER:
				case EntityID.TileID.JACK_O_LANTERN:
				case EntityID.TileID.PRESENT:
				case EntityID.TileID.SAWMILL:
					Check3x3(i, j, num);
					break;
				case EntityID.TileID.CHAIR:
				case EntityID.TileID.SAPLING:
					Check1x2(i, j, num);
					break;
				case EntityID.TileID.TABLE:
				case EntityID.TileID.FURNACE:
				case EntityID.TileID.DEMON_ALTAR:
				case EntityID.TileID.HELLFORGE:
				case EntityID.TileID.LOOM:
				case EntityID.TileID.PIANO:
				case EntityID.TileID.DRESSER:
				case EntityID.TileID.BENCH:
				case EntityID.TileID.TINKERERS_WORKSHOP:
				case EntityID.TileID.ADAMANTITE_FORGE:
					Check3x2(i, j, num);
					break;
				case EntityID.TileID.PRESSURE_PLATE:
				case EntityID.TileID.EXPLOSIVES:
				case EntityID.TileID.TIMER:
					Check1x1(i, j, num);
					break;
				case EntityID.TileID.ANVIL:
				case EntityID.TileID.WORK_BENCH:
				case EntityID.TileID.PIGGYBANK:
				case EntityID.TileID.BOWL:
				case EntityID.TileID.MYTHRIL_ANVIL:
					Check2x1(i, j, num);
					break;
				case EntityID.TileID.BOTTLE:
				case EntityID.TileID.CANDLE:
				case EntityID.TileID.BOOK:
					CheckOnTable1x1(i, j);
					break;
				case EntityID.TileID.CLAY_POT:
					CheckOnTableClaypot(i, j);
					break;
				case EntityID.TileID.CHEST:
					CheckChest(i, j);
					break;
				case EntityID.TileID.SUNFLOWER:
					CheckSunflower(i, j);
					break;
				case EntityID.TileID.MANNEQUIN:
					CheckMan(i, j);
					break;
				case EntityID.TileID.KEG:
				case EntityID.TileID.CHINESE_LANTERN:
				case EntityID.TileID.COOKING_POT:
				case EntityID.TileID.SAFE:
				case EntityID.TileID.SKULL_LANTERN:
				case EntityID.TileID.TRASH_CAN:
				case EntityID.TileID.CANDELABRA:
				case EntityID.TileID.CRYSTAL_BALL:
				case EntityID.TileID.DISCO_BALL:
				case EntityID.TileID.LEVER:
				case EntityID.TileID.BOULDER:
				case EntityID.TileID.PUMP_IN:
				case EntityID.TileID.PUMP_OUT:
					Check2x2(i, j, num);
					break;
				case EntityID.TileID.BANNER:
					CheckBanner(i, j);
					break;
				case EntityID.TileID.MUSIC_BOX:
					CheckMusicBox(i, j);
					break;
				case EntityID.TileID.LAMP_POST:
				case EntityID.TileID.TIKI_TORCH:
					Check1xX(i, j, num);
					break;
				case EntityID.TileID.BOOKCASE:
				case EntityID.TileID.THRONE:
					Check3x4(i, j, num);
					break;
				case EntityID.TileID.GRANDFATHERS_CLOCK:
				case EntityID.TileID.STATUE:
					Check2xX(i, j, num);
					break;
				case EntityID.TileID.CHAIN_LANTERN:
					Check1x2Top(i, j);
					break;
				case EntityID.TileID.DAYBLOOM_GROWING:
				case EntityID.TileID.DAYBLOOM_MATURE:
				case EntityID.TileID.DAYBLOOM_BLOOMING:
					CheckAlch(i, j);
					break;
				case EntityID.TileID.SIGN:
				case EntityID.TileID.TOMBSTONE:
					CheckSign(i, j, num);
					break;
				case EntityID.TileID.BED:
				case EntityID.TileID.BATHTUB:
					Check4x2(i, j, num);
					break;
				case EntityID.TileID.GIANT_GLOWING_MUSHROOM:
				{
					int num8 = -1;
					int num9 = -1;
					if (ptr[-1].IsActive != 0)
					{
						num9 = ptr[-1].Type;
					}
					if (ptr[1].IsActive != 0)
					{
						num8 = ptr[1].Type;
					}
					if (num8 != num && num8 != 70)
					{
						KillTile(i, j);
					}
					else if (num9 != num && ptr->FrameX == 0)
					{
						ptr->frameNumber = (byte)genRand.Next(3);
						ptr->FrameX = 18;
						ptr->FrameY = (short)(18 * ptr->frameNumber);
					}
					break;
				}
				case EntityID.TileID.CORAL:
				{
					int num4 = -1;
					int num5 = -1;
					int num6 = -1;
					int num7 = -1;
					if (ptr[-1].IsActive != 0)
					{
						num5 = ptr[-1].Type;
					}
					if (ptr[1].IsActive != 0)
					{
						num4 = ptr[1].Type;
					}
					if (ptr[-1440].IsActive != 0)
					{
						num6 = ptr[-1440].Type;
					}
					if (ptr[1440].IsActive != 0)
					{
						num7 = ptr[1440].Type;
					}
					if (num6 != -1 || num5 != -1 || num7 != -1)
					{
						KillTile(i, j);
					}
					else if (num4 < 0 || !Main.TileSolid[num4])
					{
						KillTile(i, j);
					}
					break;
				}
				case EntityID.TileID.IRON_ORE:
				case EntityID.TileID.COPPER_ORE:
				case EntityID.TileID.GOLD_ORE:
				case EntityID.TileID.SILVER_ORE:
				case EntityID.TileID.PLATFORM:
				case EntityID.TileID.DEMONITE_ORE:
				case EntityID.TileID.CORRUPT_GRASS:
				case EntityID.TileID.EBONSTONE:
				case EntityID.TileID.WOOD:
				case EntityID.TileID.CORRUPTION_THORN:
				case EntityID.TileID.METEORITE:
				case EntityID.TileID.GRAY_BRICK:
				case EntityID.TileID.RED_BRICK:
				case EntityID.TileID.CLAY:
				case EntityID.TileID.BLUE_BRICK:
				case EntityID.TileID.GREEN_BRICK:
				case EntityID.TileID.PINK_BRICK:
				case EntityID.TileID.GOLD_BRICK:
				case EntityID.TileID.SILVER_BRICK:
				case EntityID.TileID.COPPER_BRICK:
				case EntityID.TileID.SPIKE:
				case EntityID.TileID.WATER_CANDLE:
				case EntityID.TileID.COBWEB:
				case EntityID.TileID.VINE:
				case EntityID.TileID.SAND:
				case EntityID.TileID.GLASS:
				case EntityID.TileID.OBSIDIAN:
				case EntityID.TileID.ASH:
				case EntityID.TileID.HELLSTONE:
				case EntityID.TileID.MUD:
				case EntityID.TileID.JUNGLE_GRASS:
				case EntityID.TileID.JUNGLE_VINE:
				case EntityID.TileID.SAPPHIRE:
				case EntityID.TileID.RUBY:
				case EntityID.TileID.EMERALD:
				case EntityID.TileID.TOPAZ:
				case EntityID.TileID.AMETHYST:
				case EntityID.TileID.DIAMOND:
				case EntityID.TileID.JUNGLE_THORN:
				case EntityID.TileID.MUSHROOM_GRASS:
				case EntityID.TileID.OBSIDIAN_BRICK:
				case EntityID.TileID.HELLSTONE_BRICK:
				case EntityID.TileID.CACTUS:
				case EntityID.TileID.COBALT_ORE:
				case EntityID.TileID.MYTHRIL_ORE:
				case EntityID.TileID.HALLOWED_GRASS:
				case EntityID.TileID.ADAMANTITE_ORE:
				case EntityID.TileID.EBONSAND:
				case EntityID.TileID.HALLOWED_VINE:
				case EntityID.TileID.PEARLSAND:
				case EntityID.TileID.PEARLSTONE:
				case EntityID.TileID.PEARLSTONE_BRICK:
				case EntityID.TileID.IRIDESCENT_BRICK:
				case EntityID.TileID.MUDSTONE:
				case EntityID.TileID.COBALT_BRICK:
				case EntityID.TileID.MYTHRIL_BRICK:
				case EntityID.TileID.SILT:
				case EntityID.TileID.WOODEN_BEAM:
				case EntityID.TileID.ICE:
				case EntityID.TileID.ACTIVE_STONE:
				case EntityID.TileID.INACTIVE_STONE:
				case EntityID.TileID.TRAP:
				case EntityID.TileID.DEMONITE_BRICK:
				case EntityID.TileID.CANDY_CANE_RED:
				case EntityID.TileID.CANDY_CANE_GREEN:
				case EntityID.TileID.SNOW:
				case EntityID.TileID.SNOW_BRICK:
					break;
				}
				return;
			}
			int num31 = -1;
			int num32 = -1;
			int num33 = -1;
			int num34 = -1;
			int num35 = -1;
			int num36 = -1;
			int num37 = -1;
			int num38 = -1;
			if (ptr[-1440].IsActive != 0)
			{
				int type = ptr[-1440].Type;
				num34 = (Main.TileStone[type] ? 1 : type);
			}
			if (ptr[1440].IsActive != 0)
			{
				int type2 = ptr[1440].Type;
				num35 = (Main.TileStone[type2] ? 1 : type2);
			}
			if (ptr[-1].IsActive != 0)
			{
				int type3 = ptr[-1].Type;
				num32 = (Main.TileStone[type3] ? 1 : type3);
			}
			if (ptr[1].IsActive != 0)
			{
				int type4 = ptr[1].Type;
				num37 = (Main.TileStone[type4] ? 1 : type4);
			}
			if (ptr[-1441].IsActive != 0)
			{
				int type5 = ptr[-1441].Type;
				num31 = (Main.TileStone[type5] ? 1 : type5);
			}
			if (ptr[1439].IsActive != 0)
			{
				int type6 = ptr[1439].Type;
				num33 = (Main.TileStone[type6] ? 1 : type6);
			}
			if (ptr[-1439].IsActive != 0)
			{
				int type7 = ptr[-1439].Type;
				num36 = (Main.TileStone[type7] ? 1 : type7);
			}
			if (ptr[1441].IsActive != 0)
			{
				int type8 = ptr[1441].Type;
				num38 = (Main.TileStone[type8] ? 1 : type8);
			}
			switch ((EntityID.TileID)num)
			{
			case EntityID.TileID.PLATFORM:
				if (num35 >= 0 && !Main.TileSolid[num35])
				{
					num35 = -1;
				}
				if (num34 >= 0 && !Main.TileSolid[num34])
				{
					num34 = -1;
				}
				if (num34 == num)
				{
					num2 = ((num35 != num) ? ((num35 >= 0) ? 72 : 18) : 0);
				}
				else if (num34 < 0)
				{
					if (num35 == num)
					{
						num2 = 36;
					}
					else
					{
						num2 = ((num35 <= 0) ? 90 : 126);
					}
				}
				else if (num35 == num)
				{
					num2 = 54;
				}
				else
				{
					num2 = ((num35 >= 0) ? 90 : 108);
				}
				num3 = 18 * ptr->frameNumber;
				break;
			case EntityID.TileID.CACTUS:
				CactusFrame(i, j);
				return;
			case EntityID.TileID.WATER_CANDLE:
				CheckOnTable1x1(i, j);
				return;
			}
			mergeUp = false;
			mergeDown = false;
			mergeLeft = false;
			mergeRight = false;
			if (frameNumber < 0)
			{
				frameNumber = genRand.Next(3);
				ptr->frameNumber = (byte)frameNumber;
			}
			else
			{
				frameNumber = ptr->frameNumber;
			}
			if (num == 0)
			{
				if (num32 >= 0 && Main.TileMergeDirt[num32])
				{
					TileFrame(i, j - 1);
					if (mergeDown)
					{
						num32 = num;
					}
				}
				if (num37 >= 0 && Main.TileMergeDirt[num37])
				{
					TileFrame(i, j + 1);
					if (mergeUp)
					{
						num37 = num;
					}
				}
				if (num34 >= 0 && Main.TileMergeDirt[num34])
				{
					TileFrame(i - 1, j);
					if (mergeRight)
					{
						num34 = num;
					}
				}
				if (num35 >= 0 && Main.TileMergeDirt[num35])
				{
					TileFrame(i + 1, j);
					if (mergeLeft)
					{
						num35 = num;
					}
				}
				if (num32 == 2 || num32 == 23 || num32 == 109)
				{
					num32 = num;
				}
				if (num37 == 2 || num37 == 23 || num37 == 109)
				{
					num37 = num;
				}
				if (num34 == 2 || num34 == 23 || num34 == 109)
				{
					num34 = num;
				}
				if (num35 == 2 || num35 == 23 || num35 == 109)
				{
					num35 = num;
				}
				if (num31 >= 0 && Main.TileMergeDirt[num31])
				{
					num31 = num;
				}
				else if (num31 == 2 || num31 == 23 || num31 == 109)
				{
					num31 = num;
				}
				if (num33 >= 0 && Main.TileMergeDirt[num33])
				{
					num33 = num;
				}
				else if (num33 == 2 || num33 == 23 || num33 == 109)
				{
					num33 = num;
				}
				if (num36 >= 0 && Main.TileMergeDirt[num36])
				{
					num36 = num;
				}
				else if (num36 == 2 || num36 == 23 || num33 == 109)
				{
					num36 = num;
				}
				if (num38 >= 0 && Main.TileMergeDirt[num38])
				{
					num38 = num;
				}
				else if (num38 == 2 || num38 == 23 || num38 == 109)
				{
					num38 = num;
				}
				if (j < Main.RockLayer)
				{
					if (num32 == 59)
					{
						num32 = -2;
					}
					if (num37 == 59)
					{
						num37 = -2;
					}
					if (num34 == 59)
					{
						num34 = -2;
					}
					if (num35 == 59)
					{
						num35 = -2;
					}
					if (num31 == 59)
					{
						num31 = -2;
					}
					if (num33 == 59)
					{
						num33 = -2;
					}
					if (num36 == 59)
					{
						num36 = -2;
					}
					if (num38 == 59)
					{
						num38 = -2;
					}
				}
			}
			else if (Main.TileMergeDirt[num])
			{
				if (num32 == 0)
				{
					num32 = -2;
				}
				if (num37 == 0)
				{
					num37 = -2;
				}
				if (num34 == 0)
				{
					num34 = -2;
				}
				if (num35 == 0)
				{
					num35 = -2;
				}
				if (num31 == 0)
				{
					num31 = -2;
				}
				if (num33 == 0)
				{
					num33 = -2;
				}
				if (num36 == 0)
				{
					num36 = -2;
				}
				if (num38 == 0)
				{
					num38 = -2;
				}
				if (num == 1)
				{
					if (j > Main.RockLayer)
					{
						if (num32 == 59)
						{
							TileFrame(i, j - 1);
							if (mergeDown)
							{
								num32 = num;
							}
						}
						if (num37 == 59)
						{
							TileFrame(i, j + 1);
							if (mergeUp)
							{
								num37 = num;
							}
						}
						if (num34 == 59)
						{
							TileFrame(i - 1, j);
							if (mergeRight)
							{
								num34 = num;
							}
						}
						if (num35 == 59)
						{
							TileFrame(i + 1, j);
							if (mergeLeft)
							{
								num35 = num;
							}
						}
						if (num31 == 59)
						{
							num31 = num;
						}
						if (num33 == 59)
						{
							num33 = num;
						}
						if (num36 == 59)
						{
							num36 = num;
						}
						if (num38 == 59)
						{
							num38 = num;
						}
					}
					if (num32 == 57)
					{
						TileFrame(i, j - 1);
						if (mergeDown)
						{
							num32 = num;
						}
					}
					if (num37 == 57)
					{
						TileFrame(i, j + 1);
						if (mergeUp)
						{
							num37 = num;
						}
					}
					if (num34 == 57)
					{
						TileFrame(i - 1, j);
						if (mergeRight)
						{
							num34 = num;
						}
					}
					if (num35 == 57)
					{
						TileFrame(i + 1, j);
						if (mergeLeft)
						{
							num35 = num;
						}
					}
					if (num31 == 57)
					{
						num31 = num;
					}
					if (num33 == 57)
					{
						num33 = num;
					}
					if (num36 == 57)
					{
						num36 = num;
					}
					if (num38 == 57)
					{
						num38 = num;
					}
				}
			}
			else
			{
				switch ((EntityID.TileID)num)
				{
				case EntityID.TileID.HELLSTONE:
				case EntityID.TileID.OBSIDIAN_BRICK:
				case EntityID.TileID.HELLSTONE_BRICK:
					if (num32 == 57)
					{
						num32 = -2;
					}
					if (num37 == 57)
					{
						num37 = -2;
					}
					if (num34 == 57)
					{
						num34 = -2;
					}
					if (num35 == 57)
					{
						num35 = -2;
					}
					if (num31 == 57)
					{
						num31 = -2;
					}
					if (num33 == 57)
					{
						num33 = -2;
					}
					if (num36 == 57)
					{
						num36 = -2;
					}
					if (num38 == 57)
					{
						num38 = -2;
					}
					break;
				case EntityID.TileID.MUD:
					if (j > Main.RockLayer)
					{
						if (num32 == 1)
						{
							num32 = -2;
						}
						if (num37 == 1)
						{
							num37 = -2;
						}
						if (num34 == 1)
						{
							num34 = -2;
						}
						if (num35 == 1)
						{
							num35 = -2;
						}
						if (num31 == 1)
						{
							num31 = -2;
						}
						if (num33 == 1)
						{
							num33 = -2;
						}
						if (num36 == 1)
						{
							num36 = -2;
						}
						if (num38 == 1)
						{
							num38 = -2;
						}
					}
					if (num32 == 60 || num32 == 70)
					{
						num32 = num;
					}
					if (num37 == 60 || num37 == 70)
					{
						num37 = num;
					}
					if (num34 == 60 || num34 == 70)
					{
						num34 = num;
					}
					if (num35 == 60 || num35 == 70)
					{
						num35 = num;
					}
					if (num31 == 60 || num31 == 70)
					{
						num31 = num;
					}
					if (num33 == 60 || num33 == 70)
					{
						num33 = num;
					}
					if (num36 == 60 || num36 == 70)
					{
						num36 = num;
					}
					if (num38 == 60 || num38 == 70)
					{
						num38 = num;
					}
					if (j >= Main.RockLayer)
					{
						break;
					}
					if (num32 == 0)
					{
						TileFrame(i, j - 1);
						if (mergeDown)
						{
							num32 = num;
						}
					}
					if (num37 == 0)
					{
						TileFrame(i, j + 1);
						if (mergeUp)
						{
							num37 = num;
						}
					}
					if (num34 == 0)
					{
						TileFrame(i - 1, j);
						if (mergeRight)
						{
							num34 = num;
						}
					}
					if (num35 == 0)
					{
						TileFrame(i + 1, j);
						if (mergeLeft)
						{
							num35 = num;
						}
					}
					if (num31 == 0)
					{
						num31 = num;
					}
					if (num33 == 0)
					{
						num33 = num;
					}
					if (num36 == 0)
					{
						num36 = num;
					}
					if (num38 == 0)
					{
						num38 = num;
					}
					break;
				case EntityID.TileID.ASH:
					if (num32 == 1)
					{
						num32 = -2;
					}
					if (num37 == 1)
					{
						num37 = -2;
					}
					if (num34 == 1)
					{
						num34 = -2;
					}
					if (num35 == 1)
					{
						num35 = -2;
					}
					if (num31 == 1)
					{
						num31 = -2;
					}
					if (num33 == 1)
					{
						num33 = -2;
					}
					if (num36 == 1)
					{
						num36 = -2;
					}
					if (num38 == 1)
					{
						num38 = -2;
					}
					if (num32 == 58 || num32 == 76 || num32 == 75)
					{
						TileFrame(i, j - 1);
						if (mergeDown)
						{
							num32 = num;
						}
					}
					if (num37 == 58 || num37 == 76 || num37 == 75)
					{
						TileFrame(i, j + 1);
						if (mergeUp)
						{
							num37 = num;
						}
					}
					if (num34 == 58 || num34 == 76 || num34 == 75)
					{
						TileFrame(i - 1, j);
						if (mergeRight)
						{
							num34 = num;
						}
					}
					if (num35 == 58 || num35 == 76 || num35 == 75)
					{
						TileFrame(i + 1, j);
						if (mergeLeft)
						{
							num35 = num;
						}
					}
					if (num31 == 58 || num31 == 76 || num31 == 75)
					{
						num31 = num;
					}
					if (num33 == 58 || num33 == 76 || num33 == 75)
					{
						num33 = num;
					}
					if (num36 == 58 || num36 == 76 || num36 == 75)
					{
						num36 = num;
					}
					if (num38 == 58 || num38 == 76 || num38 == 75)
					{
						num38 = num;
					}
					break;
				case EntityID.TileID.CORRUPTION_THORN:
					if (num37 == 23)
					{
						num37 = num;
					}
					break;
				case EntityID.TileID.JUNGLE_THORN:
					if (num37 == 60)
					{
						num37 = num;
					}
					break;
				case EntityID.TileID.COBWEB:
					if (num32 >= 0 && !Main.TileNoAttach[num32])
					{
						num32 = num;
					}
					if (num37 >= 0 && !Main.TileNoAttach[num37])
					{
						num37 = num;
					}
					if (num34 >= 0 && !Main.TileNoAttach[num34])
					{
						num34 = num;
					}
					if (num35 >= 0 && !Main.TileNoAttach[num35])
					{
						num35 = num;
					}
					if (num31 >= 0 && !Main.TileNoAttach[num31])
					{
						num31 = num;
					}
					if (num33 >= 0 && !Main.TileNoAttach[num33])
					{
						num33 = num;
					}
					if (num36 >= 0 && !Main.TileNoAttach[num36])
					{
						num36 = num;
					}
					if (num38 >= 0 && !Main.TileNoAttach[num38])
					{
						num38 = num;
					}
					break;
				}
			}
			bool flag3 = false;
			if (num == 2 || num == 23 || num == 60 || num == 70 || num == 109)
			{
				flag3 = true;
				if (num32 >= 0 && num32 != num && !Main.TileSolid[num32])
				{
					num32 = -1;
				}
				if (num37 >= 0 && num37 != num && !Main.TileSolid[num37])
				{
					num37 = -1;
				}
				if (num34 >= 0 && num34 != num && !Main.TileSolid[num34])
				{
					num34 = -1;
				}
				if (num35 >= 0 && num35 != num && !Main.TileSolid[num35])
				{
					num35 = -1;
				}
				if (num31 >= 0 && num31 != num && !Main.TileSolid[num31])
				{
					num31 = -1;
				}
				if (num33 >= 0 && num33 != num && !Main.TileSolid[num33])
				{
					num33 = -1;
				}
				if (num36 >= 0 && num36 != num && !Main.TileSolid[num36])
				{
					num36 = -1;
				}
				if (num38 >= 0 && num38 != num && !Main.TileSolid[num38])
				{
					num38 = -1;
				}
				int num39 = 0;
				switch ((EntityID.TileID)num)
				{
				case EntityID.TileID.JUNGLE_GRASS:
				case EntityID.TileID.MUSHROOM_GRASS:
					num39 = 59;
					break;
				case EntityID.TileID.GRASS:
					if (num32 == 23)
					{
						num32 = num39;
					}
					if (num37 == 23)
					{
						num37 = num39;
					}
					if (num34 == 23)
					{
						num34 = num39;
					}
					if (num35 == 23)
					{
						num35 = num39;
					}
					if (num31 == 23)
					{
						num31 = num39;
					}
					if (num33 == 23)
					{
						num33 = num39;
					}
					if (num36 == 23)
					{
						num36 = num39;
					}
					if (num38 == 23)
					{
						num38 = num39;
					}
					break;
				case EntityID.TileID.CORRUPT_GRASS:
					if (num32 == 2)
					{
						num32 = num39;
					}
					if (num37 == 2)
					{
						num37 = num39;
					}
					if (num34 == 2)
					{
						num34 = num39;
					}
					if (num35 == 2)
					{
						num35 = num39;
					}
					if (num31 == 2)
					{
						num31 = num39;
					}
					if (num33 == 2)
					{
						num33 = num39;
					}
					if (num36 == 2)
					{
						num36 = num39;
					}
					if (num38 == 2)
					{
						num38 = num39;
					}
					break;
				}
				if (num32 != num && num32 != num39 && (num37 == num || num37 == num39))
				{
					if (num34 == num39 && num35 == num)
					{
						num2 = 18 * frameNumber;
						num3 = 198;
					}
					else if (num34 == num && num35 == num39)
					{
						num2 = 54 + 18 * frameNumber;
						num3 = 198;
					}
				}
				else if (num37 != num && num37 != num39 && (num32 == num || num32 == num39))
				{
					if (num34 == num39 && num35 == num)
					{
						num2 = 18 * frameNumber;
						num3 = 216;
					}
					else if (num34 == num && num35 == num39)
					{
						num2 = 54 + 18 * frameNumber;
						num3 = 216;
					}
				}
				else if (num34 != num && num34 != num39 && (num35 == num || num35 == num39))
				{
					if (num32 == num39 && num37 == num)
					{
						num2 = 72;
						num3 = 144 + 18 * frameNumber;
					}
					else if (num37 == num && num35 == num32)
					{
						num2 = 72;
						num3 = 90 + 18 * frameNumber;
					}
				}
				else if (num35 != num && num35 != num39 && (num34 == num || num34 == num39))
				{
					if (num32 == num39 && num37 == num)
					{
						num2 = 90;
						num3 = 144 + 18 * frameNumber;
					}
					else if (num37 == num && num35 == num32)
					{
						num2 = 90;
						num3 = 90 + 18 * frameNumber;
					}
				}
				else if (num32 == num && num37 == num && num34 == num && num35 == num)
				{
					if (num31 != num && num33 != num && num36 != num && num38 != num)
					{
						if (num38 == num39)
						{
							num3 = 324;
							num2 = 108 + 18 * frameNumber;
						}
						else if (num33 == num39)
						{
							num3 = 342;
							num2 = 108 + 18 * frameNumber;
						}
						else if (num36 == num39)
						{
							num3 = 360;
							num2 = 108 + 18 * frameNumber;
						}
						else if (num31 == num39)
						{
							num3 = 378;
							num2 = 108 + 18 * frameNumber;
						}
						else
						{
							num3 = 234;
							num2 = 144 + 54 * frameNumber;
						}
					}
					else if (num31 != num && num38 != num)
					{
						num3 = 306;
						num2 = 36 + 18 * frameNumber;
					}
					else if (num33 != num && num36 != num)
					{
						num3 = 306;
						num2 = 90 + 18 * frameNumber;
					}
					else if (num31 != num && num33 == num && num36 == num && num38 == num)
					{
						num2 = 54;
						num3 = 108 + 36 * frameNumber;
					}
					else if (num31 == num && num33 != num && num36 == num && num38 == num)
					{
						num2 = 36;
						num3 = 108 + 36 * frameNumber;
					}
					else if (num31 == num && num33 == num && num36 != num && num38 == num)
					{
						num2 = 54;
						num3 = 90 + 36 * frameNumber;
					}
					else if (num31 == num && num33 == num && num36 == num && num38 != num)
					{
						num2 = 36;
						num3 = 90 + 36 * frameNumber;
					}
				}
				else if (num32 == num && num37 == num39 && num34 == num && num35 == num && num31 == -1 && num33 == -1)
				{
					num3 = 18;
					num2 = 108 + 18 * frameNumber;
				}
				else if (num32 == num39 && num37 == num && num34 == num && num35 == num && num36 == -1 && num38 == -1)
				{
					num3 = 36;
					num2 = 108 + 18 * frameNumber;
				}
				else if (num32 == num && num37 == num && num34 == num39 && num35 == num && num33 == -1 && num38 == -1)
				{
					num2 = 198;
					num3 = 18 * frameNumber;
				}
				else if (num32 == num && num37 == num && num34 == num && num35 == num39 && num31 == -1 && num36 == -1)
				{
					num2 = 180;
					num3 = 18 * frameNumber;
				}
				else if (num32 == num && num37 == num39 && num34 == num && num35 == num)
				{
					if (num33 != -1)
					{
						num2 = 54;
						num3 = 108 + 36 * frameNumber;
					}
					else if (num31 != -1)
					{
						num2 = 36;
						num3 = 108 + 36 * frameNumber;
					}
				}
				else if (num32 == num39 && num37 == num && num34 == num && num35 == num)
				{
					if (num38 != -1)
					{
						num2 = 54;
						num3 = 90 + 36 * frameNumber;
					}
					else if (num36 != -1)
					{
						num2 = 36;
						num3 = 90 + 36 * frameNumber;
					}
				}
				else if (num32 == num && num37 == num && num34 == num && num35 == num39)
				{
					if (num31 != -1)
					{
						num2 = 54;
						num3 = 90 + 36 * frameNumber;
					}
					else if (num36 != -1)
					{
						num2 = 54;
						num3 = 108 + 36 * frameNumber;
					}
				}
				else if (num32 == num && num37 == num && num34 == num39 && num35 == num)
				{
					if (num33 != -1)
					{
						num2 = 36;
						num3 = 90 + 36 * frameNumber;
					}
					else if (num38 != -1)
					{
						num2 = 36;
						num3 = 108 + 36 * frameNumber;
					}
				}
				else if ((num32 == num39 && num37 == num && num34 == num && num35 == num) || (num32 == num && num37 == num39 && num34 == num && num35 == num) || (num32 == num && num37 == num && num34 == num39 && num35 == num) || (num32 == num && num37 == num && num34 == num && num35 == num39))
				{
					num3 = 18;
					num2 = 18 + 18 * frameNumber;
				}
				if ((num32 == num || num32 == num39) && (num37 == num || num37 == num39) && (num34 == num || num34 == num39) && (num35 == num || num35 == num39))
				{
					if (num31 != num && num31 != num39 && (num33 == num || num33 == num39) && (num36 == num || num36 == num39) && (num38 == num || num38 == num39))
					{
						num2 = 54;
						num3 = 108 + 36 * frameNumber;
					}
					else if (num33 != num && num33 != num39 && (num31 == num || num31 == num39) && (num36 == num || num36 == num39) && (num38 == num || num38 == num39))
					{
						num2 = 36;
						num3 = 108 + 36 * frameNumber;
					}
					else if (num36 != num && num36 != num39 && (num31 == num || num31 == num39) && (num33 == num || num33 == num39) && (num38 == num || num38 == num39))
					{
						num2 = 54;
						num3 = 90 + 36 * frameNumber;
					}
					else if (num38 != num && num38 != num39 && (num31 == num || num31 == num39) && (num36 == num || num36 == num39) && (num33 == num || num33 == num39))
					{
						num2 = 36;
						num3 = 90 + 36 * frameNumber;
					}
				}
				if (num32 != num39 && num32 != num && num37 == num && num34 != num39 && num34 != num && num35 == num && num38 != num39 && num38 != num)
				{
					num3 = 270;
					num2 = 90 + 18 * frameNumber;
				}
				else if (num32 != num39 && num32 != num && num37 == num && num34 == num && num35 != num39 && num35 != num && num36 != num39 && num36 != num)
				{
					num3 = 270;
					num2 = 144 + 18 * frameNumber;
				}
				else if (num37 != num39 && num37 != num && num32 == num && num34 != num39 && num34 != num && num35 == num && num33 != num39 && num33 != num)
				{
					num3 = 288;
					num2 = 90 + 18 * frameNumber;
				}
				else if (num37 != num39 && num37 != num && num32 == num && num34 == num && num35 != num39 && num35 != num && num31 != num39 && num31 != num)
				{
					num3 = 288;
					num2 = 144 + 18 * frameNumber;
				}
				else if (num32 != num && num32 != num39 && num37 == num && num34 == num && num35 == num && num36 != num && num36 != num39 && num38 != num && num38 != num39)
				{
					num3 = 216;
					num2 = 144 + 54 * frameNumber;
				}
				else if (num37 != num && num37 != num39 && num32 == num && num34 == num && num35 == num && num31 != num && num31 != num39 && num33 != num && num33 != num39)
				{
					num3 = 252;
					num2 = 144 + 54 * frameNumber;
				}
				else if (num34 != num && num34 != num39 && num37 == num && num32 == num && num35 == num && num33 != num && num33 != num39 && num38 != num && num38 != num39)
				{
					num3 = 234;
					num2 = 126 + 54 * frameNumber;
				}
				else if (num35 != num && num35 != num39 && num37 == num && num32 == num && num34 == num && num31 != num && num31 != num39 && num36 != num && num36 != num39)
				{
					num3 = 234;
					num2 = 162 + 54 * frameNumber;
				}
				else if (num32 != num39 && num32 != num && (num37 == num39 || num37 == num) && num34 == num39 && num35 == num39)
				{
					num3 = 270;
					num2 = 36 + 18 * frameNumber;
				}
				else if (num37 != num39 && num37 != num && (num32 == num39 || num32 == num) && num34 == num39 && num35 == num39)
				{
					num3 = 288;
					num2 = 36 + 18 * frameNumber;
				}
				else if (num34 != num39 && num34 != num && (num35 == num39 || num35 == num) && num32 == num39 && num37 == num39)
				{
					num2 = 0;
					num3 = 270 + 18 * frameNumber;
				}
				else if (num35 != num39 && num35 != num && (num34 == num39 || num34 == num) && num32 == num39 && num37 == num39)
				{
					num2 = 18;
					num3 = 270 + 18 * frameNumber;
				}
				else if (num32 == num && num37 == num39 && num34 == num39 && num35 == num39)
				{
					num3 = 288;
					num2 = 198 + 18 * frameNumber;
				}
				else if (num32 == num39 && num37 == num && num34 == num39 && num35 == num39)
				{
					num3 = 270;
					num2 = 198 + 18 * frameNumber;
				}
				else if (num32 == num39 && num37 == num39 && num34 == num && num35 == num39)
				{
					num3 = 306;
					num2 = 198 + 18 * frameNumber;
				}
				else if (num32 == num39 && num37 == num39 && num34 == num39 && num35 == num)
				{
					num3 = 306;
					num2 = 144 + 18 * frameNumber;
				}
				if (num32 != num && num32 != num39 && num37 == num && num34 == num && num35 == num)
				{
					if ((num36 == num39 || num36 == num) && num38 != num39 && num38 != num)
					{
						num3 = 324;
						num2 = 18 * frameNumber;
					}
					else if ((num38 == num39 || num38 == num) && num36 != num39 && num36 != num)
					{
						num3 = 324;
						num2 = 54 + 18 * frameNumber;
					}
				}
				else if (num37 != num && num37 != num39 && num32 == num && num34 == num && num35 == num)
				{
					if ((num31 == num39 || num31 == num) && num33 != num39 && num33 != num)
					{
						num3 = 342;
						num2 = 18 * frameNumber;
					}
					else if ((num33 == num39 || num33 == num) && num31 != num39 && num31 != num)
					{
						num3 = 342;
						num2 = 54 + 18 * frameNumber;
					}
				}
				else if (num34 != num && num34 != num39 && num32 == num && num37 == num && num35 == num)
				{
					if ((num33 == num39 || num33 == num) && num38 != num39 && num38 != num)
					{
						num3 = 360;
						num2 = 54 + 18 * frameNumber;
					}
					else if ((num38 == num39 || num38 == num) && num33 != num39 && num33 != num)
					{
						num3 = 360;
						num2 = 18 * frameNumber;
					}
				}
				else if (num35 != num && num35 != num39 && num32 == num && num37 == num && num34 == num)
				{
					if ((num31 == num39 || num31 == num) && num36 != num39 && num36 != num)
					{
						num3 = 378;
						num2 = 18 * frameNumber;
					}
					else if ((num36 == num39 || num36 == num) && num31 != num39 && num31 != num)
					{
						num3 = 378;
						num2 = 54 + 18 * frameNumber;
					}
				}
				if ((num32 == num || num32 == num39) && (num37 == num || num37 == num39) && (num34 == num || num34 == num39) && (num35 == num || num35 == num39) && num31 != -1 && num33 != -1 && num36 != -1 && num38 != -1)
				{
					num3 = 18;
					num2 = 18 + 18 * frameNumber;
				}
				if (num32 == num39)
				{
					num32 = -2;
				}
				if (num37 == num39)
				{
					num37 = -2;
				}
				if (num34 == num39)
				{
					num34 = -2;
				}
				if (num35 == num39)
				{
					num35 = -2;
				}
				if (num31 == num39)
				{
					num31 = -2;
				}
				if (num33 == num39)
				{
					num33 = -2;
				}
				if (num36 == num39)
				{
					num36 = -2;
				}
				if (num38 == num39)
				{
					num38 = -2;
				}
			}
			if (num2 == -1 && (Main.TileMergeDirt[num] || num == 0 || num == 2 || num == 57 || num == 58 || num == 59 || num == 60 || num == 70 || num == 109 || num == 76 || num == 75))
			{
				if (!flag3)
				{
					flag3 = true;
					if (num31 >= 0 && num31 != num && !Main.TileSolid[num31])
					{
						num31 = -1;
					}
					if (num33 >= 0 && num33 != num && !Main.TileSolid[num33])
					{
						num33 = -1;
					}
					if (num36 >= 0 && num36 != num && !Main.TileSolid[num36])
					{
						num36 = -1;
					}
					if (num38 >= 0 && num38 != num && !Main.TileSolid[num38])
					{
						num38 = -1;
					}
				}
				if (num32 >= 0 && num32 != num)
				{
					num32 = -1;
				}
				if (num37 >= 0 && num37 != num)
				{
					num37 = -1;
				}
				if (num34 >= 0 && num34 != num)
				{
					num34 = -1;
				}
				if (num35 >= 0 && num35 != num)
				{
					num35 = -1;
				}
				if (num32 != -1 && num37 != -1 && num34 != -1 && num35 != -1)
				{
					if (num32 == -2 && num37 == num && num34 == num && num35 == num)
					{
						num3 = 108;
						num2 = 144 + 18 * frameNumber;
						mergeUp = true;
					}
					else if (num32 == num && num37 == -2 && num34 == num && num35 == num)
					{
						num3 = 90;
						num2 = 144 + 18 * frameNumber;
						mergeDown = true;
					}
					else if (num32 == num && num37 == num && num34 == -2 && num35 == num)
					{
						num2 = 162;
						num3 = 126 + 18 * frameNumber;
						mergeLeft = true;
					}
					else if (num32 == num && num37 == num && num34 == num && num35 == -2)
					{
						num2 = 144;
						num3 = 126 + 18 * frameNumber;
						mergeRight = true;
					}
					else if (num32 == -2 && num37 == num && num34 == -2 && num35 == num)
					{
						num2 = 36;
						num3 = 90 + 36 * frameNumber;
						mergeUp = true;
						mergeLeft = true;
					}
					else if (num32 == -2 && num37 == num && num34 == num && num35 == -2)
					{
						num2 = 54;
						num3 = 90 + 36 * frameNumber;
						mergeUp = true;
						mergeRight = true;
					}
					else if (num32 == num && num37 == -2 && num34 == -2 && num35 == num)
					{
						num2 = 36;
						num3 = 108 + 36 * frameNumber;
						mergeDown = true;
						mergeLeft = true;
					}
					else if (num32 == num && num37 == -2 && num34 == num && num35 == -2)
					{
						num2 = 54;
						num3 = 108 + 36 * frameNumber;
						mergeDown = true;
						mergeRight = true;
					}
					else if (num32 == num && num37 == num && num34 == -2 && num35 == -2)
					{
						num2 = 180;
						num3 = 126 + 18 * frameNumber;
						mergeLeft = true;
						mergeRight = true;
					}
					else if (num32 == -2 && num37 == -2 && num34 == num && num35 == num)
					{
						num3 = 180;
						num2 = 144 + 18 * frameNumber;
						mergeUp = true;
						mergeDown = true;
					}
					else if (num32 == -2 && num37 == num && num34 == -2 && num35 == -2)
					{
						num2 = 198;
						num3 = 90 + 18 * frameNumber;
						mergeUp = true;
						mergeLeft = true;
						mergeRight = true;
					}
					else if (num32 == num && num37 == -2 && num34 == -2 && num35 == -2)
					{
						num2 = 198;
						num3 = 144 + 18 * frameNumber;
						mergeDown = true;
						mergeLeft = true;
						mergeRight = true;
					}
					else if (num32 == -2 && num37 == -2 && num34 == num && num35 == -2)
					{
						num2 = 216;
						num3 = 144 + 18 * frameNumber;
						mergeUp = true;
						mergeDown = true;
						mergeRight = true;
					}
					else if (num32 == -2 && num37 == -2 && num34 == -2 && num35 == num)
					{
						num2 = 216;
						num3 = 90 + 18 * frameNumber;
						mergeUp = true;
						mergeDown = true;
						mergeLeft = true;
					}
					else if (num32 == -2 && num37 == -2 && num34 == -2 && num35 == -2)
					{
						num3 = 198;
						num2 = 108 + 18 * frameNumber;
						mergeUp = true;
						mergeDown = true;
						mergeLeft = true;
						mergeRight = true;
					}
					else if (num32 == num && num37 == num && num34 == num && num35 == num)
					{
						if (num38 == -2)
						{
							num2 = 0;
							num3 = 90 + 36 * frameNumber;
						}
						else if (num36 == -2)
						{
							num2 = 18;
							num3 = 90 + 36 * frameNumber;
						}
						else if (num33 == -2)
						{
							num2 = 0;
							num3 = 108 + 36 * frameNumber;
						}
						else if (num31 == -2)
						{
							num2 = 18;
							num3 = 108 + 36 * frameNumber;
						}
					}
				}
				else
				{
					if (num != 2 && num != 23 && num != 60 && num != 70 && num != 109)
					{
						if (num32 == -1 && num37 == -2 && num34 == num && num35 == num)
						{
							num3 = 0;
							num2 = 234 + 18 * frameNumber;
							mergeDown = true;
						}
						else if (num32 == -2 && num37 == -1 && num34 == num && num35 == num)
						{
							num3 = 18;
							num2 = 234 + 18 * frameNumber;
							mergeUp = true;
						}
						else if (num32 == num && num37 == num && num34 == -1 && num35 == -2)
						{
							num3 = 36;
							num2 = 234 + 18 * frameNumber;
							mergeRight = true;
						}
						else if (num32 == num && num37 == num && num34 == -2 && num35 == -1)
						{
							num3 = 54;
							num2 = 234 + 18 * frameNumber;
							mergeLeft = true;
						}
					}
					if (num32 != -1 && num37 != -1 && num34 == -1 && num35 == num)
					{
						if (num32 == -2 && num37 == num)
						{
							num2 = 72;
							num3 = 144 + 18 * frameNumber;
							mergeUp = true;
						}
						else if (num37 == -2 && num32 == num)
						{
							num2 = 72;
							num3 = 90 + 18 * frameNumber;
							mergeDown = true;
						}
					}
					else if (num32 != -1 && num37 != -1 && num34 == num && num35 == -1)
					{
						if (num32 == -2 && num37 == num)
						{
							num2 = 90;
							num3 = 144 + 18 * frameNumber;
							mergeUp = true;
						}
						else if (num37 == -2 && num32 == num)
						{
							num2 = 90;
							num3 = 90 + 18 * frameNumber;
							mergeDown = true;
						}
					}
					else if (num32 == -1 && num37 == num && num34 != -1 && num35 != -1)
					{
						if (num34 == -2 && num35 == num)
						{
							num2 = 18 * frameNumber;
							num3 = 198;
							mergeLeft = true;
						}
						else if (num35 == -2 && num34 == num)
						{
							num2 = 54 + 18 * frameNumber;
							num3 = 198;
							mergeRight = true;
						}
					}
					else if (num32 == num && num37 == -1 && num34 != -1 && num35 != -1)
					{
						if (num34 == -2 && num35 == num)
						{
							num2 = 18 * frameNumber;
							num3 = 216;
							mergeLeft = true;
						}
						else if (num35 == -2 && num34 == num)
						{
							num2 = 54 + 18 * frameNumber;
							num3 = 216;
							mergeRight = true;
						}
					}
					else if (num32 != -1 && num37 != -1 && num34 == -1 && num35 == -1)
					{
						if (num32 == -2 && num37 == -2)
						{
							num2 = 108;
							num3 = 216 + 18 * frameNumber;
							mergeUp = true;
							mergeDown = true;
						}
						else if (num32 == -2)
						{
							num2 = 126;
							num3 = 144 + 18 * frameNumber;
							mergeUp = true;
						}
						else if (num37 == -2)
						{
							num2 = 126;
							num3 = 90 + 18 * frameNumber;
							mergeDown = true;
						}
					}
					else if (num32 == -1 && num37 == -1 && num34 != -1 && num35 != -1)
					{
						if (num34 == -2 && num35 == -2)
						{
							num3 = 198;
							num2 = 162 + 18 * frameNumber;
							mergeLeft = true;
							mergeRight = true;
						}
						else if (num34 == -2)
						{
							num3 = 252;
							num2 = 18 * frameNumber;
							mergeLeft = true;
						}
						else if (num35 == -2)
						{
							num3 = 252;
							num2 = 54 + 18 * frameNumber;
							mergeRight = true;
						}
					}
					else if (num32 == -2 && num37 == -1 && num34 == -1 && num35 == -1)
					{
						num2 = 108;
						num3 = 144 + 18 * frameNumber;
						mergeUp = true;
					}
					else if (num32 == -1 && num37 == -2 && num34 == -1 && num35 == -1)
					{
						num2 = 108;
						num3 = 90 + 18 * frameNumber;
						mergeDown = true;
					}
					else if (num32 == -1 && num37 == -1 && num34 == -2 && num35 == -1)
					{
						num3 = 234;
						num2 = 18 * frameNumber;
						mergeLeft = true;
					}
					else if (num32 == -1 && num37 == -1 && num34 == -1 && num35 == -2)
					{
						num3 = 234;
						num2 = 54 + 18 * frameNumber;
						mergeRight = true;
					}
				}
			}
			if (num2 < 0)
			{
				if (!flag3)
				{
					flag3 = true;
					if (num32 >= 0 && num32 != num && !Main.TileSolid[num32])
					{
						num32 = -1;
					}
					if (num37 >= 0 && num37 != num && !Main.TileSolid[num37])
					{
						num37 = -1;
					}
					if (num34 >= 0 && num34 != num && !Main.TileSolid[num34])
					{
						num34 = -1;
					}
					if (num35 >= 0 && num35 != num && !Main.TileSolid[num35])
					{
						num35 = -1;
					}
					if (num31 >= 0 && num31 != num && !Main.TileSolid[num31])
					{
						num31 = -1;
					}
					if (num33 >= 0 && num33 != num && !Main.TileSolid[num33])
					{
						num33 = -1;
					}
					if (num36 >= 0 && num36 != num && !Main.TileSolid[num36])
					{
						num36 = -1;
					}
					if (num38 >= 0 && num38 != num && !Main.TileSolid[num38])
					{
						num38 = -1;
					}
				}
				if (num == 2 || num == 23 || num == 60 || num == 70 || num == 109)
				{
					if (num32 == -2)
					{
						num32 = num;
					}
					if (num37 == -2)
					{
						num37 = num;
					}
					if (num34 == -2)
					{
						num34 = num;
					}
					if (num35 == -2)
					{
						num35 = num;
					}
					if (num31 == -2)
					{
						num31 = num;
					}
					if (num33 == -2)
					{
						num33 = num;
					}
					if (num36 == -2)
					{
						num36 = num;
					}
					if (num38 == -2)
					{
						num38 = num;
					}
				}
				if (num32 == num && num37 == num && num34 == num && num35 == num)
				{
					if (num31 != num && num33 != num)
					{
						num3 = 18;
						num2 = 108 + 18 * frameNumber;
					}
					else if (num36 != num && num38 != num)
					{
						num3 = 36;
						num2 = 108 + 18 * frameNumber;
					}
					else if (num31 != num && num36 != num)
					{
						num2 = 180;
						num3 = 18 * frameNumber;
					}
					else if (num33 != num && num38 != num)
					{
						num2 = 198;
						num3 = 18 * frameNumber;
					}
					else
					{
						num3 = 18;
						num2 = 18 + 18 * frameNumber;
					}
				}
				else if (num32 != num && num37 == num && num34 == num && num35 == num)
				{
					num3 = 0;
					num2 = 18 + 18 * frameNumber;
				}
				else if (num32 == num && num37 != num && num34 == num && num35 == num)
				{
					num3 = 36;
					num2 = 18 + 18 * frameNumber;
				}
				else if (num32 == num && num37 == num && num34 != num && num35 == num)
				{
					num2 = 0;
					num3 = 18 * frameNumber;
				}
				else if (num32 == num && num37 == num && num34 == num && num35 != num)
				{
					num2 = 72;
					num3 = 18 * frameNumber;
				}
				else if (num32 != num && num37 == num && num34 != num && num35 == num)
				{
					num2 = 36 * frameNumber;
					num3 = 54;
				}
				else if (num32 != num && num37 == num && num34 == num && num35 != num)
				{
					num2 = 18 + 36 * frameNumber;
					num3 = 54;
				}
				else if (num32 == num && num37 != num && num34 != num && num35 == num)
				{
					num2 = 36 * frameNumber;
					num3 = 72;
				}
				else if (num32 == num && num37 != num && num34 == num && num35 != num)
				{
					num2 = 18 + 36 * frameNumber;
					num3 = 72;
				}
				else if (num32 == num && num37 == num && num34 != num && num35 != num)
				{
					num2 = 90;
					num3 = 18 * frameNumber;
				}
				else if (num32 != num && num37 != num && num34 == num && num35 == num)
				{
					num2 = 108 + 18 * frameNumber;
					num3 = 72;
				}
				else if (num32 != num && num37 == num && num34 != num && num35 != num)
				{
					num2 = 108 + 18 * frameNumber;
					num3 = 0;
				}
				else if (num32 == num && num37 != num && num34 != num && num35 != num)
				{
					num2 = 108 + 18 * frameNumber;
					num3 = 54;
				}
				else if (num32 != num && num37 != num && num34 != num && num35 == num)
				{
					num2 = 162;
					num3 = 18 * frameNumber;
				}
				else if (num32 != num && num37 != num && num34 == num && num35 != num)
				{
					num2 = 216;
					num3 = 18 * frameNumber;
				}
				else if (num32 != num && num37 != num && num34 != num && num35 != num)
				{
					num2 = 162 + 18 * frameNumber;
					num3 = 54;
				}
			}
			if (num2 < 0)
			{
				num3 = 18;
				num2 = 18 + 18 * frameNumber;
			}
			ptr->FrameX = (short)num2;
			ptr->FrameY = (short)num3;
			if (num == 52 || num == 62 || num == 115)
			{
				num32 = ((ptr[-1].IsActive != 0) ? ptr[-1].Type : (-1));
				if (num == 52 && (num32 == 109 || num32 == 115))
				{
					ptr->Type = 115;
					SquareTileFrame(i, j);
					return;
				}
				if (num == 115 && (num32 == 2 || num32 == 52))
				{
					ptr->Type = 52;
					SquareTileFrame(i, j);
					return;
				}
				if (num32 != num && (num32 == -1 || (num == 52 && num32 != 2) || (num == 62 && num32 != 60) || (num == 115 && num32 != 109)))
				{
					KillTile(i, j);
				}
			}
			if (!Gen && Main.NetMode != 1 && (num == 53 || num == 112 || num == 116 || num == 123))
			{
				Tile* ptr3 = ptr + 1;
				if (ptr3->IsActive == 0)
				{
					ptr3 = ptr - 1;
					if (ptr3->IsActive == 0 || ptr3->Type != 21)
					{
						ptr->IsActive = 0;
						sandBuffer[currentSandBuffer].Add(i, j);
						return;
					}
				}
			}
			if (frameX >= 0 && num2 != frameX && frameY >= 0 && num3 != frameY && tileFrameRecursion)
			{
				bool flag4 = mergeUp;
				bool flag5 = mergeDown;
				bool flag6 = mergeLeft;
				bool flag7 = mergeRight;
				TileFrame(i, j - 1);
				TileFrame(i, j + 1);
				TileFrame(i - 1, j);
				TileFrame(i + 1, j);
				mergeUp = flag4;
				mergeDown = flag5;
				mergeLeft = flag6;
				mergeRight = flag7;
			}
		}
	}

	public unsafe static void TileFrameNoLiquid(int i, int j, int frameNumber = 0)
	{
		if (i <= 5 || j <= 5 || i >= Main.MaxTilesX - 5 || j >= Main.MaxTilesY - 5)
		{
			return;
		}
		fixed (Tile* ptr = &Main.TileSet[i, j])
		{
			if (ptr->IsActive == 0)
			{
				return;
			}
			int num = ptr->Type;
			if (Main.TileStone[num])
			{
				num = 1;
			}
			int frameX = ptr->FrameX;
			int frameY = ptr->FrameY;
			int num2 = -1;
			int num3 = -1;
			if (Main.TileFrameImportant[num])
			{
				switch ((EntityID.TileID)num)
				{
				case EntityID.TileID.SHORT_GRASS_PLANTS:
				case EntityID.TileID.SHORT_CORRUPT_PLANTS:
				case EntityID.TileID.SHORT_JUNGLE_PLANTS:
				case EntityID.TileID.GLOWING_MUSHROOM:
				case EntityID.TileID.TALL_GRASS_PLANTS:
				case EntityID.TileID.TALL_JUNGLE_PLANTS:
				case EntityID.TileID.SHORT_HALLOWED_PLANTS:
				case EntityID.TileID.TALL_HALLOWED_PLANTS:
					PlantCheck(i, j);
					break;
				case EntityID.TileID.TORCH:
				{
					int num8 = ((ptr->FrameX >= 66) ? 66 : 0);
					int num9 = -1;
					int num10 = -1;
					int num11 = -1;
					int num12 = -1;
					int num13 = -1;
					int num14 = -1;
					int num15 = -1;
					if (ptr[-1].IsActive != 0)
					{
						_ = ptr[-1];
					}
					if (ptr[1].IsActive != 0)
					{
						num9 = ptr[1].Type;
					}
					if (ptr[-1440].IsActive != 0)
					{
						num10 = ptr[-1440].Type;
					}
					if (ptr[1440].IsActive != 0)
					{
						num11 = ptr[1440].Type;
					}
					if (ptr[-1439].IsActive != 0)
					{
						num12 = ptr[-1439].Type;
					}
					if (ptr[1441].IsActive != 0)
					{
						num13 = ptr[1441].Type;
					}
					if (ptr[-1441].IsActive != 0)
					{
						num14 = ptr[-1441].Type;
					}
					if (ptr[1439].IsActive != 0)
					{
						num15 = ptr[1439].Type;
					}
					if (num9 >= 0 && Main.TileSolidAndAttach[num9])
					{
						ptr->FrameX = (short)num8;
					}
					else if (num10 >= 0 && (Main.TileSolidAndAttach[num10] || num10 == 124 || (num10 == 5 && num14 == 5 && num12 == 5)))
					{
						ptr->FrameX = (short)(22 + num8);
					}
					else if (num11 >= 0 && (Main.TileSolidAndAttach[num11] || num11 == 124 || (num11 == 5 && num15 == 5 && num13 == 5)))
					{
						ptr->FrameX = (short)(44 + num8);
					}
					else
					{
						KillTile(i, j);
					}
					break;
				}
				case EntityID.TileID.SWITCH:
				{
					int num20 = -1;
					int num21 = -1;
					int num22 = -1;
					if (ptr[-1].IsActive != 0)
					{
						_ = ptr[-1];
					}
					if (ptr[1].IsActive != 0)
					{
						num20 = ptr[1].Type;
					}
					if (ptr[-1440].IsActive != 0)
					{
						num21 = ptr[-1440].Type;
					}
					if (ptr[1440].IsActive != 0)
					{
						num22 = ptr[1440].Type;
					}
					if (num20 >= 0 && Main.TileSolidAndAttach[num20])
					{
						ptr->FrameX = 0;
					}
					else if (num21 >= 0 && (Main.TileSolidAndAttach[num21] || num21 == 124 || num21 == 5))
					{
						ptr->FrameX = 18;
					}
					else if (num22 >= 0 && (Main.TileSolidAndAttach[num22] || num22 == 124 || num22 == 5))
					{
						ptr->FrameX = 36;
					}
					else
					{
						KillTile(i, j);
					}
					break;
				}
				case EntityID.TileID.CRYSTAL_SHARD:
				case EntityID.TileID.XMAS_LIGHT:
				{
					int num16 = -1;
					int num17 = -1;
					int num18 = -1;
					int num19 = -1;
					if (ptr[-1].IsActive != 0)
					{
						num17 = ptr[-1].Type;
					}
					if (ptr[1].IsActive != 0)
					{
						num16 = ptr[1].Type;
					}
					if (ptr[-1440].IsActive != 0)
					{
						num18 = ptr[-1440].Type;
					}
					if (ptr[1440].IsActive != 0)
					{
						num19 = ptr[1440].Type;
					}
					if (num16 >= 0 && Main.TileSolidNotSolidTop[num16])
					{
						ptr->FrameY = 0;
					}
					else if (num18 >= 0 && Main.TileSolidNotSolidTop[num18])
					{
						ptr->FrameY = 54;
					}
					else if (num19 >= 0 && Main.TileSolidNotSolidTop[num19])
					{
						ptr->FrameY = 36;
					}
					else if (num17 >= 0 && Main.TileSolidNotSolidTop[num17])
					{
						ptr->FrameY = 18;
					}
					else
					{
						KillTile(i, j);
					}
					break;
				}
				case EntityID.TileID.DOOR_CLOSED:
				{
					if (ToDestroyObject)
					{
						break;
					}
					int num28 = j - frameY / 18;
					bool flag2 = false;
					if (Main.TileSet[i, num28 - 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[i, num28 - 1].Type])
					{
						flag2 = true;
					}
					else if (Main.TileSet[i, num28 + 3].IsActive == 0 || !Main.TileSolid[Main.TileSet[i, num28 + 3].Type])
					{
						flag2 = true;
					}
					else if (Main.TileSet[i, num28].IsActive == 0 || Main.TileSet[i, num28].Type != num)
					{
						flag2 = true;
					}
					else if (Main.TileSet[i, num28 + 1].IsActive == 0 || Main.TileSet[i, num28 + 1].Type != num)
					{
						flag2 = true;
					}
					else if (Main.TileSet[i, num28 + 2].IsActive == 0 || Main.TileSet[i, num28 + 2].Type != num)
					{
						flag2 = true;
					}
					if (flag2)
					{
						ToDestroyObject = true;
						KillTile(i, num28);
						KillTile(i, num28 + 1);
						KillTile(i, num28 + 2);
						if (!Gen)
						{
							Item.NewItem(i * 16, j * 16, 16, 16, 25);
						}
					}
					ToDestroyObject = false;
					break;
				}
				case EntityID.TileID.DOOR_OPEN:
				{
					if (ToDestroyObject)
					{
						break;
					}
					int num23 = 0;
					int num24 = 0;
					int num25 = i;
					int num26 = j;
					bool flag = false;
					switch (frameX)
					{
					case 0:
						num23 = 1;
						break;
					case 18:
						num25 = i - 1;
						num24 = -1440;
						num23 = 1;
						break;
					case 36:
						num24 = 1440;
						num25 = i + 1;
						num23 = -1;
						break;
					case 54:
						num23 = -1;
						break;
					}
					switch (frameY)
					{
					case 18:
						num24--;
						num26 = j - 1;
						break;
					case 36:
						num26 = j - 2;
						num24 -= 2;
						break;
					}
					if (ptr[num24 - 1].IsActive == 0 || !Main.TileSolid[ptr[num24 - 1].Type] || ptr[num24 + 3].IsActive == 0 || !Main.TileSolid[ptr[num24 + 3].Type])
					{
						flag = true;
						ToDestroyObject = true;
						if (!Gen)
						{
							Item.NewItem(i * 16, j * 16, 16, 16, 25);
						}
					}
					int num27 = num25;
					if (num23 == -1)
					{
						num27 = num25 - 1;
					}
					for (int k = num27; k < num27 + 2; k++)
					{
						for (int l = num26; l < num26 + 3; l++)
						{
							if (!flag)
							{
								fixed (Tile* ptr2 = &Main.TileSet[k, l])
								{
									if (ptr2->Type != 11 || ptr2->IsActive == 0)
									{
										ToDestroyObject = true;
										flag = true;
										k = num27;
										l = num26;
										if (!Gen)
										{
											Item.NewItem(i * 16, j * 16, 16, 16, 25);
										}
									}
								}
							}
							if (flag)
							{
								KillTile(k, l);
							}
						}
					}
					ToDestroyObject = false;
					break;
				}
				case EntityID.TileID.GIANT_GLOWING_MUSHROOM:
				{
					int num29 = -1;
					int num30 = -1;
					if (ptr[-1].IsActive != 0)
					{
						num30 = ptr[-1].Type;
					}
					if (ptr[1].IsActive != 0)
					{
						num29 = ptr[1].Type;
					}
					if (num29 != num && num29 != 70)
					{
						KillTile(i, j);
					}
					else if (num30 != num && ptr->FrameX == 0)
					{
						ptr->frameNumber = (byte)genRand.Next(3);
						ptr->FrameX = 18;
						ptr->FrameY = (short)(18 * ptr->frameNumber);
					}
					break;
				}
				case EntityID.TileID.CORAL:
				{
					int num4 = -1;
					int num5 = -1;
					int num6 = -1;
					int num7 = -1;
					if (ptr[-1].IsActive != 0)
					{
						num5 = ptr[-1].Type;
					}
					if (ptr[1].IsActive != 0)
					{
						num4 = ptr[1].Type;
					}
					if (ptr[-1440].IsActive != 0)
					{
						num6 = ptr[-1440].Type;
					}
					if (ptr[1440].IsActive != 0)
					{
						num7 = ptr[1440].Type;
					}
					if (num6 != -1 || num5 != -1 || num7 != -1)
					{
						KillTile(i, j);
					}
					else if (num4 < 0 || !Main.TileSolid[num4])
					{
						KillTile(i, j);
					}
					break;
				}
				case EntityID.TileID.TREE:
				case EntityID.TileID.IRON_ORE:
				case EntityID.TileID.COPPER_ORE:
				case EntityID.TileID.GOLD_ORE:
				case EntityID.TileID.SILVER_ORE:
				case EntityID.TileID.LIFE_CRYSTAL:
				case EntityID.TileID.BOTTLE:
				case EntityID.TileID.TABLE:
				case EntityID.TileID.CHAIR:
				case EntityID.TileID.ANVIL:
				case EntityID.TileID.FURNACE:
				case EntityID.TileID.WORK_BENCH:
				case EntityID.TileID.PLATFORM:
				case EntityID.TileID.SAPLING:
				case EntityID.TileID.CHEST:
				case EntityID.TileID.DEMONITE_ORE:
				case EntityID.TileID.CORRUPT_GRASS:
				case EntityID.TileID.EBONSTONE:
				case EntityID.TileID.DEMON_ALTAR:
				case EntityID.TileID.SUNFLOWER:
				case EntityID.TileID.POT:
				case EntityID.TileID.PIGGYBANK:
				case EntityID.TileID.WOOD:
				case EntityID.TileID.SHADOW_ORB:
				case EntityID.TileID.CORRUPTION_THORN:
				case EntityID.TileID.CANDLE:
				case EntityID.TileID.CHANDELIER:
				case EntityID.TileID.JACK_O_LANTERN:
				case EntityID.TileID.PRESENT:
				case EntityID.TileID.METEORITE:
				case EntityID.TileID.GRAY_BRICK:
				case EntityID.TileID.RED_BRICK:
				case EntityID.TileID.CLAY:
				case EntityID.TileID.BLUE_BRICK:
				case EntityID.TileID.CHAIN_LANTERN:
				case EntityID.TileID.GREEN_BRICK:
				case EntityID.TileID.PINK_BRICK:
				case EntityID.TileID.GOLD_BRICK:
				case EntityID.TileID.SILVER_BRICK:
				case EntityID.TileID.COPPER_BRICK:
				case EntityID.TileID.SPIKE:
				case EntityID.TileID.WATER_CANDLE:
				case EntityID.TileID.BOOK:
				case EntityID.TileID.COBWEB:
				case EntityID.TileID.VINE:
				case EntityID.TileID.SAND:
				case EntityID.TileID.GLASS:
				case EntityID.TileID.SIGN:
				case EntityID.TileID.OBSIDIAN:
				case EntityID.TileID.ASH:
				case EntityID.TileID.HELLSTONE:
				case EntityID.TileID.MUD:
				case EntityID.TileID.JUNGLE_GRASS:
				case EntityID.TileID.JUNGLE_VINE:
				case EntityID.TileID.SAPPHIRE:
				case EntityID.TileID.RUBY:
				case EntityID.TileID.EMERALD:
				case EntityID.TileID.TOPAZ:
				case EntityID.TileID.AMETHYST:
				case EntityID.TileID.DIAMOND:
				case EntityID.TileID.JUNGLE_THORN:
				case EntityID.TileID.MUSHROOM_GRASS:
				case EntityID.TileID.OBSIDIAN_BRICK:
				case EntityID.TileID.HELLSTONE_BRICK:
				case EntityID.TileID.HELLFORGE:
				case EntityID.TileID.CLAY_POT:
				case EntityID.TileID.BED:
				case EntityID.TileID.CACTUS:
				case EntityID.TileID.DAYBLOOM_GROWING:
				case EntityID.TileID.DAYBLOOM_MATURE:
				case EntityID.TileID.DAYBLOOM_BLOOMING:
				case EntityID.TileID.TOMBSTONE:
				case EntityID.TileID.LOOM:
				case EntityID.TileID.PIANO:
				case EntityID.TileID.DRESSER:
				case EntityID.TileID.BENCH:
				case EntityID.TileID.BATHTUB:
				case EntityID.TileID.BANNER:
				case EntityID.TileID.LAMP_POST:
				case EntityID.TileID.TIKI_TORCH:
				case EntityID.TileID.KEG:
				case EntityID.TileID.CHINESE_LANTERN:
				case EntityID.TileID.COOKING_POT:
				case EntityID.TileID.SAFE:
				case EntityID.TileID.SKULL_LANTERN:
				case EntityID.TileID.TRASH_CAN:
				case EntityID.TileID.CANDELABRA:
				case EntityID.TileID.BOOKCASE:
				case EntityID.TileID.THRONE:
				case EntityID.TileID.BOWL:
				case EntityID.TileID.GRANDFATHERS_CLOCK:
				case EntityID.TileID.STATUE:
				case EntityID.TileID.SAWMILL:
				case EntityID.TileID.COBALT_ORE:
				case EntityID.TileID.MYTHRIL_ORE:
				case EntityID.TileID.HALLOWED_GRASS:
				case EntityID.TileID.ADAMANTITE_ORE:
				case EntityID.TileID.EBONSAND:
				case EntityID.TileID.TINKERERS_WORKSHOP:
				case EntityID.TileID.HALLOWED_VINE:
				case EntityID.TileID.PEARLSAND:
				case EntityID.TileID.PEARLSTONE:
				case EntityID.TileID.PEARLSTONE_BRICK:
				case EntityID.TileID.IRIDESCENT_BRICK:
				case EntityID.TileID.MUDSTONE:
				case EntityID.TileID.COBALT_BRICK:
				case EntityID.TileID.MYTHRIL_BRICK:
				case EntityID.TileID.SILT:
				case EntityID.TileID.WOODEN_BEAM:
				case EntityID.TileID.CRYSTAL_BALL:
				case EntityID.TileID.DISCO_BALL:
				case EntityID.TileID.ICE:
				case EntityID.TileID.MANNEQUIN:
				case EntityID.TileID.ACTIVE_STONE:
				case EntityID.TileID.INACTIVE_STONE:
				case EntityID.TileID.LEVER:
				case EntityID.TileID.ADAMANTITE_FORGE:
				case EntityID.TileID.MYTHRIL_ANVIL:
				case EntityID.TileID.PRESSURE_PLATE:
				case EntityID.TileID.TRAP:
				case EntityID.TileID.BOULDER:
				case EntityID.TileID.MUSIC_BOX:
				case EntityID.TileID.DEMONITE_BRICK:
				case EntityID.TileID.EXPLOSIVES:
				case EntityID.TileID.PUMP_IN:
				case EntityID.TileID.PUMP_OUT:
				case EntityID.TileID.TIMER:
				case EntityID.TileID.CANDY_CANE_RED:
				case EntityID.TileID.CANDY_CANE_GREEN:
				case EntityID.TileID.SNOW:
				case EntityID.TileID.SNOW_BRICK:
					break;
				}
				return;
			}
			int num31 = -1;
			int num32 = -1;
			int num33 = -1;
			int num34 = -1;
			int num35 = -1;
			int num36 = -1;
			int num37 = -1;
			int num38 = -1;
			if (ptr[-1440].IsActive != 0)
			{
				int type = ptr[-1440].Type;
				num34 = (Main.TileStone[type] ? 1 : type);
			}
			if (ptr[1440].IsActive != 0)
			{
				int type2 = ptr[1440].Type;
				num35 = (Main.TileStone[type2] ? 1 : type2);
			}
			if (ptr[-1].IsActive != 0)
			{
				int type3 = ptr[-1].Type;
				num32 = (Main.TileStone[type3] ? 1 : type3);
			}
			if (ptr[1].IsActive != 0)
			{
				int type4 = ptr[1].Type;
				num37 = (Main.TileStone[type4] ? 1 : type4);
			}
			if (ptr[-1441].IsActive != 0)
			{
				int type5 = ptr[-1441].Type;
				num31 = (Main.TileStone[type5] ? 1 : type5);
			}
			if (ptr[1439].IsActive != 0)
			{
				int type6 = ptr[1439].Type;
				num33 = (Main.TileStone[type6] ? 1 : type6);
			}
			if (ptr[-1439].IsActive != 0)
			{
				int type7 = ptr[-1439].Type;
				num36 = (Main.TileStone[type7] ? 1 : type7);
			}
			if (ptr[1441].IsActive != 0)
			{
				int type8 = ptr[1441].Type;
				num38 = (Main.TileStone[type8] ? 1 : type8);
			}
			switch ((EntityID.TileID)num)
			{
			case EntityID.TileID.PLATFORM:
				if (num35 >= 0 && !Main.TileSolid[num35])
				{
					num35 = -1;
				}
				if (num34 >= 0 && !Main.TileSolid[num34])
				{
					num34 = -1;
				}
				if (num34 == num)
				{
					num2 = ((num35 != num) ? ((num35 >= 0) ? 72 : 18) : 0);
				}
				else if (num34 < 0)
				{
					if (num35 == num)
					{
						num2 = 36;
					}
					else
					{
						num2 = ((num35 <= 0) ? 90 : 126);
					}
				}
				else if (num35 == num)
				{
					num2 = 54;
				}
				else
				{
					num2 = ((num35 >= 0) ? 90 : 108);
				}
				num3 = 18 * ptr->frameNumber;
				break;
			case EntityID.TileID.CACTUS:
				CactusFrame(i, j);
				return;
			case EntityID.TileID.WATER_CANDLE:
				return;
			}
			mergeUp2 = false;
			mergeDown2 = false;
			mergeLeft2 = false;
			mergeRight2 = false;
			if (frameNumber < 0)
			{
				frameNumber = genRand.Next(3);
				ptr->frameNumber = (byte)frameNumber;
			}
			else
			{
				frameNumber = ptr->frameNumber;
			}
			if (num == 0)
			{
				if (num32 >= 0 && Main.TileMergeDirt[num32])
				{
					TileFrameNoLiquid(i, j - 1);
					if (mergeDown2)
					{
						num32 = num;
					}
				}
				if (num37 >= 0 && Main.TileMergeDirt[num37])
				{
					TileFrameNoLiquid(i, j + 1);
					if (mergeUp2)
					{
						num37 = num;
					}
				}
				if (num34 >= 0 && Main.TileMergeDirt[num34])
				{
					TileFrameNoLiquid(i - 1, j);
					if (mergeRight2)
					{
						num34 = num;
					}
				}
				if (num35 >= 0 && Main.TileMergeDirt[num35])
				{
					TileFrameNoLiquid(i + 1, j);
					if (mergeLeft2)
					{
						num35 = num;
					}
				}
				if (num32 == 2 || num32 == 23 || num32 == 109)
				{
					num32 = num;
				}
				if (num37 == 2 || num37 == 23 || num37 == 109)
				{
					num37 = num;
				}
				if (num34 == 2 || num34 == 23 || num34 == 109)
				{
					num34 = num;
				}
				if (num35 == 2 || num35 == 23 || num35 == 109)
				{
					num35 = num;
				}
				if (num31 >= 0 && Main.TileMergeDirt[num31])
				{
					num31 = num;
				}
				else if (num31 == 2 || num31 == 23 || num31 == 109)
				{
					num31 = num;
				}
				if (num33 >= 0 && Main.TileMergeDirt[num33])
				{
					num33 = num;
				}
				else if (num33 == 2 || num33 == 23 || num33 == 109)
				{
					num33 = num;
				}
				if (num36 >= 0 && Main.TileMergeDirt[num36])
				{
					num36 = num;
				}
				else if (num36 == 2 || num36 == 23 || num33 == 109)
				{
					num36 = num;
				}
				if (num38 >= 0 && Main.TileMergeDirt[num38])
				{
					num38 = num;
				}
				else if (num38 == 2 || num38 == 23 || num38 == 109)
				{
					num38 = num;
				}
				if (j < Main.RockLayer)
				{
					if (num32 == 59)
					{
						num32 = -2;
					}
					if (num37 == 59)
					{
						num37 = -2;
					}
					if (num34 == 59)
					{
						num34 = -2;
					}
					if (num35 == 59)
					{
						num35 = -2;
					}
					if (num31 == 59)
					{
						num31 = -2;
					}
					if (num33 == 59)
					{
						num33 = -2;
					}
					if (num36 == 59)
					{
						num36 = -2;
					}
					if (num38 == 59)
					{
						num38 = -2;
					}
				}
			}
			else if (Main.TileMergeDirt[num])
			{
				if (num32 == 0)
				{
					num32 = -2;
				}
				if (num37 == 0)
				{
					num37 = -2;
				}
				if (num34 == 0)
				{
					num34 = -2;
				}
				if (num35 == 0)
				{
					num35 = -2;
				}
				if (num31 == 0)
				{
					num31 = -2;
				}
				if (num33 == 0)
				{
					num33 = -2;
				}
				if (num36 == 0)
				{
					num36 = -2;
				}
				if (num38 == 0)
				{
					num38 = -2;
				}
				if (num == 1)
				{
					if (j > Main.RockLayer)
					{
						if (num32 == 59)
						{
							TileFrameNoLiquid(i, j - 1);
							if (mergeDown2)
							{
								num32 = num;
							}
						}
						if (num37 == 59)
						{
							TileFrameNoLiquid(i, j + 1);
							if (mergeUp2)
							{
								num37 = num;
							}
						}
						if (num34 == 59)
						{
							TileFrameNoLiquid(i - 1, j);
							if (mergeRight2)
							{
								num34 = num;
							}
						}
						if (num35 == 59)
						{
							TileFrameNoLiquid(i + 1, j);
							if (mergeLeft2)
							{
								num35 = num;
							}
						}
						if (num31 == 59)
						{
							num31 = num;
						}
						if (num33 == 59)
						{
							num33 = num;
						}
						if (num36 == 59)
						{
							num36 = num;
						}
						if (num38 == 59)
						{
							num38 = num;
						}
					}
					if (num32 == 57)
					{
						TileFrameNoLiquid(i, j - 1);
						if (mergeDown2)
						{
							num32 = num;
						}
					}
					if (num37 == 57)
					{
						TileFrameNoLiquid(i, j + 1);
						if (mergeUp2)
						{
							num37 = num;
						}
					}
					if (num34 == 57)
					{
						TileFrameNoLiquid(i - 1, j);
						if (mergeRight2)
						{
							num34 = num;
						}
					}
					if (num35 == 57)
					{
						TileFrameNoLiquid(i + 1, j);
						if (mergeLeft2)
						{
							num35 = num;
						}
					}
					if (num31 == 57)
					{
						num31 = num;
					}
					if (num33 == 57)
					{
						num33 = num;
					}
					if (num36 == 57)
					{
						num36 = num;
					}
					if (num38 == 57)
					{
						num38 = num;
					}
				}
			}
			else
			{
				switch ((EntityID.TileID)num)
				{
				case EntityID.TileID.HELLSTONE:
				case EntityID.TileID.OBSIDIAN_BRICK:
				case EntityID.TileID.HELLSTONE_BRICK:
					if (num32 == 57)
					{
						num32 = -2;
					}
					if (num37 == 57)
					{
						num37 = -2;
					}
					if (num34 == 57)
					{
						num34 = -2;
					}
					if (num35 == 57)
					{
						num35 = -2;
					}
					if (num31 == 57)
					{
						num31 = -2;
					}
					if (num33 == 57)
					{
						num33 = -2;
					}
					if (num36 == 57)
					{
						num36 = -2;
					}
					if (num38 == 57)
					{
						num38 = -2;
					}
					break;
				case EntityID.TileID.MUD:
					if (j > Main.RockLayer)
					{
						if (num32 == 1)
						{
							num32 = -2;
						}
						if (num37 == 1)
						{
							num37 = -2;
						}
						if (num34 == 1)
						{
							num34 = -2;
						}
						if (num35 == 1)
						{
							num35 = -2;
						}
						if (num31 == 1)
						{
							num31 = -2;
						}
						if (num33 == 1)
						{
							num33 = -2;
						}
						if (num36 == 1)
						{
							num36 = -2;
						}
						if (num38 == 1)
						{
							num38 = -2;
						}
					}
					if (num32 == 60 || num32 == 70)
					{
						num32 = num;
					}
					if (num37 == 60 || num37 == 70)
					{
						num37 = num;
					}
					if (num34 == 60 || num34 == 70)
					{
						num34 = num;
					}
					if (num35 == 60 || num35 == 70)
					{
						num35 = num;
					}
					if (num31 == 60 || num31 == 70)
					{
						num31 = num;
					}
					if (num33 == 60 || num33 == 70)
					{
						num33 = num;
					}
					if (num36 == 60 || num36 == 70)
					{
						num36 = num;
					}
					if (num38 == 60 || num38 == 70)
					{
						num38 = num;
					}
					if (j >= Main.RockLayer)
					{
						break;
					}
					if (num32 == 0)
					{
						TileFrameNoLiquid(i, j - 1);
						if (mergeDown2)
						{
							num32 = num;
						}
					}
					if (num37 == 0)
					{
						TileFrameNoLiquid(i, j + 1);
						if (mergeUp2)
						{
							num37 = num;
						}
					}
					if (num34 == 0)
					{
						TileFrameNoLiquid(i - 1, j);
						if (mergeRight2)
						{
							num34 = num;
						}
					}
					if (num35 == 0)
					{
						TileFrameNoLiquid(i + 1, j);
						if (mergeLeft2)
						{
							num35 = num;
						}
					}
					if (num31 == 0)
					{
						num31 = num;
					}
					if (num33 == 0)
					{
						num33 = num;
					}
					if (num36 == 0)
					{
						num36 = num;
					}
					if (num38 == 0)
					{
						num38 = num;
					}
					break;
				case EntityID.TileID.ASH:
					if (num32 == 1)
					{
						num32 = -2;
					}
					if (num37 == 1)
					{
						num37 = -2;
					}
					if (num34 == 1)
					{
						num34 = -2;
					}
					if (num35 == 1)
					{
						num35 = -2;
					}
					if (num31 == 1)
					{
						num31 = -2;
					}
					if (num33 == 1)
					{
						num33 = -2;
					}
					if (num36 == 1)
					{
						num36 = -2;
					}
					if (num38 == 1)
					{
						num38 = -2;
					}
					if (num32 == 58 || num32 == 76 || num32 == 75)
					{
						TileFrameNoLiquid(i, j - 1);
						if (mergeDown2)
						{
							num32 = num;
						}
					}
					if (num37 == 58 || num37 == 76 || num37 == 75)
					{
						TileFrameNoLiquid(i, j + 1);
						if (mergeUp2)
						{
							num37 = num;
						}
					}
					if (num34 == 58 || num34 == 76 || num34 == 75)
					{
						TileFrameNoLiquid(i - 1, j);
						if (mergeRight2)
						{
							num34 = num;
						}
					}
					if (num35 == 58 || num35 == 76 || num35 == 75)
					{
						TileFrameNoLiquid(i + 1, j);
						if (mergeLeft2)
						{
							num35 = num;
						}
					}
					if (num31 == 58 || num31 == 76 || num31 == 75)
					{
						num31 = num;
					}
					if (num33 == 58 || num33 == 76 || num33 == 75)
					{
						num33 = num;
					}
					if (num36 == 58 || num36 == 76 || num36 == 75)
					{
						num36 = num;
					}
					if (num38 == 58 || num38 == 76 || num38 == 75)
					{
						num38 = num;
					}
					break;
				case EntityID.TileID.CORRUPTION_THORN:
					if (num37 == 23)
					{
						num37 = num;
					}
					break;
				case EntityID.TileID.JUNGLE_THORN:
					if (num37 == 60)
					{
						num37 = num;
					}
					break;
				case EntityID.TileID.COBWEB:
					if (num32 >= 0 && !Main.TileNoAttach[num32])
					{
						num32 = num;
					}
					if (num37 >= 0 && !Main.TileNoAttach[num37])
					{
						num37 = num;
					}
					if (num34 >= 0 && !Main.TileNoAttach[num34])
					{
						num34 = num;
					}
					if (num35 >= 0 && !Main.TileNoAttach[num35])
					{
						num35 = num;
					}
					if (num31 >= 0 && !Main.TileNoAttach[num31])
					{
						num31 = num;
					}
					if (num33 >= 0 && !Main.TileNoAttach[num33])
					{
						num33 = num;
					}
					if (num36 >= 0 && !Main.TileNoAttach[num36])
					{
						num36 = num;
					}
					if (num38 >= 0 && !Main.TileNoAttach[num38])
					{
						num38 = num;
					}
					break;
				}
			}
			bool flag3 = false;
			if (num == 2 || num == 23 || num == 60 || num == 70 || num == 109)
			{
				flag3 = true;
				if (num32 >= 0 && num32 != num && !Main.TileSolid[num32])
				{
					num32 = -1;
				}
				if (num37 >= 0 && num37 != num && !Main.TileSolid[num37])
				{
					num37 = -1;
				}
				if (num34 >= 0 && num34 != num && !Main.TileSolid[num34])
				{
					num34 = -1;
				}
				if (num35 >= 0 && num35 != num && !Main.TileSolid[num35])
				{
					num35 = -1;
				}
				if (num31 >= 0 && num31 != num && !Main.TileSolid[num31])
				{
					num31 = -1;
				}
				if (num33 >= 0 && num33 != num && !Main.TileSolid[num33])
				{
					num33 = -1;
				}
				if (num36 >= 0 && num36 != num && !Main.TileSolid[num36])
				{
					num36 = -1;
				}
				if (num38 >= 0 && num38 != num && !Main.TileSolid[num38])
				{
					num38 = -1;
				}
				int num39 = 0;
				switch ((EntityID.TileID)num)
				{
				case EntityID.TileID.JUNGLE_GRASS:
				case EntityID.TileID.MUSHROOM_GRASS:
					num39 = 59;
					break;
				case EntityID.TileID.GRASS:
					if (num32 == 23)
					{
						num32 = num39;
					}
					if (num37 == 23)
					{
						num37 = num39;
					}
					if (num34 == 23)
					{
						num34 = num39;
					}
					if (num35 == 23)
					{
						num35 = num39;
					}
					if (num31 == 23)
					{
						num31 = num39;
					}
					if (num33 == 23)
					{
						num33 = num39;
					}
					if (num36 == 23)
					{
						num36 = num39;
					}
					if (num38 == 23)
					{
						num38 = num39;
					}
					break;
				case EntityID.TileID.CORRUPT_GRASS:
					if (num32 == 2)
					{
						num32 = num39;
					}
					if (num37 == 2)
					{
						num37 = num39;
					}
					if (num34 == 2)
					{
						num34 = num39;
					}
					if (num35 == 2)
					{
						num35 = num39;
					}
					if (num31 == 2)
					{
						num31 = num39;
					}
					if (num33 == 2)
					{
						num33 = num39;
					}
					if (num36 == 2)
					{
						num36 = num39;
					}
					if (num38 == 2)
					{
						num38 = num39;
					}
					break;
				}
				if (num32 != num && num32 != num39 && (num37 == num || num37 == num39))
				{
					if (num34 == num39 && num35 == num)
					{
						num2 = 18 * frameNumber;
						num3 = 198;
					}
					else if (num34 == num && num35 == num39)
					{
						num2 = 54 + 18 * frameNumber;
						num3 = 198;
					}
				}
				else if (num37 != num && num37 != num39 && (num32 == num || num32 == num39))
				{
					if (num34 == num39 && num35 == num)
					{
						num2 = 18 * frameNumber;
						num3 = 216;
					}
					else if (num34 == num && num35 == num39)
					{
						num2 = 54 + 18 * frameNumber;
						num3 = 216;
					}
				}
				else if (num34 != num && num34 != num39 && (num35 == num || num35 == num39))
				{
					if (num32 == num39 && num37 == num)
					{
						num2 = 72;
						num3 = 144 + 18 * frameNumber;
					}
					else if (num37 == num && num35 == num32)
					{
						num2 = 72;
						num3 = 90 + 18 * frameNumber;
					}
				}
				else if (num35 != num && num35 != num39 && (num34 == num || num34 == num39))
				{
					if (num32 == num39 && num37 == num)
					{
						num2 = 90;
						num3 = 144 + 18 * frameNumber;
					}
					else if (num37 == num && num35 == num32)
					{
						num2 = 90;
						num3 = 90 + 18 * frameNumber;
					}
				}
				else if (num32 == num && num37 == num && num34 == num && num35 == num)
				{
					if (num31 != num && num33 != num && num36 != num && num38 != num)
					{
						if (num38 == num39)
						{
							num3 = 324;
							num2 = 108 + 18 * frameNumber;
						}
						else if (num33 == num39)
						{
							num3 = 342;
							num2 = 108 + 18 * frameNumber;
						}
						else if (num36 == num39)
						{
							num3 = 360;
							num2 = 108 + 18 * frameNumber;
						}
						else if (num31 == num39)
						{
							num3 = 378;
							num2 = 108 + 18 * frameNumber;
						}
						else
						{
							num3 = 234;
							num2 = 144 + 54 * frameNumber;
						}
					}
					else if (num31 != num && num38 != num)
					{
						num3 = 306;
						num2 = 36 + 18 * frameNumber;
					}
					else if (num33 != num && num36 != num)
					{
						num3 = 306;
						num2 = 90 + 18 * frameNumber;
					}
					else if (num31 != num && num33 == num && num36 == num && num38 == num)
					{
						num2 = 54;
						num3 = 108 + 36 * frameNumber;
					}
					else if (num31 == num && num33 != num && num36 == num && num38 == num)
					{
						num2 = 36;
						num3 = 108 + 36 * frameNumber;
					}
					else if (num31 == num && num33 == num && num36 != num && num38 == num)
					{
						num2 = 54;
						num3 = 90 + 36 * frameNumber;
					}
					else if (num31 == num && num33 == num && num36 == num && num38 != num)
					{
						num2 = 36;
						num3 = 90 + 36 * frameNumber;
					}
				}
				else if (num32 == num && num37 == num39 && num34 == num && num35 == num && num31 == -1 && num33 == -1)
				{
					num3 = 18;
					num2 = 108 + 18 * frameNumber;
				}
				else if (num32 == num39 && num37 == num && num34 == num && num35 == num && num36 == -1 && num38 == -1)
				{
					num3 = 36;
					num2 = 108 + 18 * frameNumber;
				}
				else if (num32 == num && num37 == num && num34 == num39 && num35 == num && num33 == -1 && num38 == -1)
				{
					num2 = 198;
					num3 = 18 * frameNumber;
				}
				else if (num32 == num && num37 == num && num34 == num && num35 == num39 && num31 == -1 && num36 == -1)
				{
					num2 = 180;
					num3 = 18 * frameNumber;
				}
				else if (num32 == num && num37 == num39 && num34 == num && num35 == num)
				{
					if (num33 != -1)
					{
						num2 = 54;
						num3 = 108 + 36 * frameNumber;
					}
					else if (num31 != -1)
					{
						num2 = 36;
						num3 = 108 + 36 * frameNumber;
					}
				}
				else if (num32 == num39 && num37 == num && num34 == num && num35 == num)
				{
					if (num38 != -1)
					{
						num2 = 54;
						num3 = 90 + 36 * frameNumber;
					}
					else if (num36 != -1)
					{
						num2 = 36;
						num3 = 90 + 36 * frameNumber;
					}
				}
				else if (num32 == num && num37 == num && num34 == num && num35 == num39)
				{
					if (num31 != -1)
					{
						num2 = 54;
						num3 = 90 + 36 * frameNumber;
					}
					else if (num36 != -1)
					{
						num2 = 54;
						num3 = 108 + 36 * frameNumber;
					}
				}
				else if (num32 == num && num37 == num && num34 == num39 && num35 == num)
				{
					if (num33 != -1)
					{
						num2 = 36;
						num3 = 90 + 36 * frameNumber;
					}
					else if (num38 != -1)
					{
						num2 = 36;
						num3 = 108 + 36 * frameNumber;
					}
				}
				else if ((num32 == num39 && num37 == num && num34 == num && num35 == num) || (num32 == num && num37 == num39 && num34 == num && num35 == num) || (num32 == num && num37 == num && num34 == num39 && num35 == num) || (num32 == num && num37 == num && num34 == num && num35 == num39))
				{
					num3 = 18;
					num2 = 18 + 18 * frameNumber;
				}
				if ((num32 == num || num32 == num39) && (num37 == num || num37 == num39) && (num34 == num || num34 == num39) && (num35 == num || num35 == num39))
				{
					if (num31 != num && num31 != num39 && (num33 == num || num33 == num39) && (num36 == num || num36 == num39) && (num38 == num || num38 == num39))
					{
						num2 = 54;
						num3 = 108 + 36 * frameNumber;
					}
					else if (num33 != num && num33 != num39 && (num31 == num || num31 == num39) && (num36 == num || num36 == num39) && (num38 == num || num38 == num39))
					{
						num2 = 36;
						num3 = 108 + 36 * frameNumber;
					}
					else if (num36 != num && num36 != num39 && (num31 == num || num31 == num39) && (num33 == num || num33 == num39) && (num38 == num || num38 == num39))
					{
						num2 = 54;
						num3 = 90 + 36 * frameNumber;
					}
					else if (num38 != num && num38 != num39 && (num31 == num || num31 == num39) && (num36 == num || num36 == num39) && (num33 == num || num33 == num39))
					{
						num2 = 36;
						num3 = 90 + 36 * frameNumber;
					}
				}
				if (num32 != num39 && num32 != num && num37 == num && num34 != num39 && num34 != num && num35 == num && num38 != num39 && num38 != num)
				{
					num3 = 270;
					num2 = 90 + 18 * frameNumber;
				}
				else if (num32 != num39 && num32 != num && num37 == num && num34 == num && num35 != num39 && num35 != num && num36 != num39 && num36 != num)
				{
					num3 = 270;
					num2 = 144 + 18 * frameNumber;
				}
				else if (num37 != num39 && num37 != num && num32 == num && num34 != num39 && num34 != num && num35 == num && num33 != num39 && num33 != num)
				{
					num3 = 288;
					num2 = 90 + 18 * frameNumber;
				}
				else if (num37 != num39 && num37 != num && num32 == num && num34 == num && num35 != num39 && num35 != num && num31 != num39 && num31 != num)
				{
					num3 = 288;
					num2 = 144 + 18 * frameNumber;
				}
				else if (num32 != num && num32 != num39 && num37 == num && num34 == num && num35 == num && num36 != num && num36 != num39 && num38 != num && num38 != num39)
				{
					num3 = 216;
					num2 = 144 + 54 * frameNumber;
				}
				else if (num37 != num && num37 != num39 && num32 == num && num34 == num && num35 == num && num31 != num && num31 != num39 && num33 != num && num33 != num39)
				{
					num3 = 252;
					num2 = 144 + 54 * frameNumber;
				}
				else if (num34 != num && num34 != num39 && num37 == num && num32 == num && num35 == num && num33 != num && num33 != num39 && num38 != num && num38 != num39)
				{
					num3 = 234;
					num2 = 126 + 54 * frameNumber;
				}
				else if (num35 != num && num35 != num39 && num37 == num && num32 == num && num34 == num && num31 != num && num31 != num39 && num36 != num && num36 != num39)
				{
					num3 = 234;
					num2 = 162 + 54 * frameNumber;
				}
				else if (num32 != num39 && num32 != num && (num37 == num39 || num37 == num) && num34 == num39 && num35 == num39)
				{
					num3 = 270;
					num2 = 36 + 18 * frameNumber;
				}
				else if (num37 != num39 && num37 != num && (num32 == num39 || num32 == num) && num34 == num39 && num35 == num39)
				{
					num3 = 288;
					num2 = 36 + 18 * frameNumber;
				}
				else if (num34 != num39 && num34 != num && (num35 == num39 || num35 == num) && num32 == num39 && num37 == num39)
				{
					num2 = 0;
					num3 = 270 + 18 * frameNumber;
				}
				else if (num35 != num39 && num35 != num && (num34 == num39 || num34 == num) && num32 == num39 && num37 == num39)
				{
					num2 = 18;
					num3 = 270 + 18 * frameNumber;
				}
				else if (num32 == num && num37 == num39 && num34 == num39 && num35 == num39)
				{
					num3 = 288;
					num2 = 198 + 18 * frameNumber;
				}
				else if (num32 == num39 && num37 == num && num34 == num39 && num35 == num39)
				{
					num3 = 270;
					num2 = 198 + 18 * frameNumber;
				}
				else if (num32 == num39 && num37 == num39 && num34 == num && num35 == num39)
				{
					num3 = 306;
					num2 = 198 + 18 * frameNumber;
				}
				else if (num32 == num39 && num37 == num39 && num34 == num39 && num35 == num)
				{
					num3 = 306;
					num2 = 144 + 18 * frameNumber;
				}
				if (num32 != num && num32 != num39 && num37 == num && num34 == num && num35 == num)
				{
					if ((num36 == num39 || num36 == num) && num38 != num39 && num38 != num)
					{
						num3 = 324;
						num2 = 18 * frameNumber;
					}
					else if ((num38 == num39 || num38 == num) && num36 != num39 && num36 != num)
					{
						num3 = 324;
						num2 = 54 + 18 * frameNumber;
					}
				}
				else if (num37 != num && num37 != num39 && num32 == num && num34 == num && num35 == num)
				{
					if ((num31 == num39 || num31 == num) && num33 != num39 && num33 != num)
					{
						num3 = 342;
						num2 = 18 * frameNumber;
					}
					else if ((num33 == num39 || num33 == num) && num31 != num39 && num31 != num)
					{
						num3 = 342;
						num2 = 54 + 18 * frameNumber;
					}
				}
				else if (num34 != num && num34 != num39 && num32 == num && num37 == num && num35 == num)
				{
					if ((num33 == num39 || num33 == num) && num38 != num39 && num38 != num)
					{
						num3 = 360;
						num2 = 54 + 18 * frameNumber;
					}
					else if ((num38 == num39 || num38 == num) && num33 != num39 && num33 != num)
					{
						num3 = 360;
						num2 = 18 * frameNumber;
					}
				}
				else if (num35 != num && num35 != num39 && num32 == num && num37 == num && num34 == num)
				{
					if ((num31 == num39 || num31 == num) && num36 != num39 && num36 != num)
					{
						num3 = 378;
						num2 = 18 * frameNumber;
					}
					else if ((num36 == num39 || num36 == num) && num31 != num39 && num31 != num)
					{
						num3 = 378;
						num2 = 54 + 18 * frameNumber;
					}
				}
				if ((num32 == num || num32 == num39) && (num37 == num || num37 == num39) && (num34 == num || num34 == num39) && (num35 == num || num35 == num39) && num31 != -1 && num33 != -1 && num36 != -1 && num38 != -1)
				{
					num3 = 18;
					num2 = 18 + 18 * frameNumber;
				}
				if (num32 == num39)
				{
					num32 = -2;
				}
				if (num37 == num39)
				{
					num37 = -2;
				}
				if (num34 == num39)
				{
					num34 = -2;
				}
				if (num35 == num39)
				{
					num35 = -2;
				}
				if (num31 == num39)
				{
					num31 = -2;
				}
				if (num33 == num39)
				{
					num33 = -2;
				}
				if (num36 == num39)
				{
					num36 = -2;
				}
				if (num38 == num39)
				{
					num38 = -2;
				}
			}
			if (num2 == -1 && (Main.TileMergeDirt[num] || num == 0 || num == 2 || num == 57 || num == 58 || num == 59 || num == 60 || num == 70 || num == 109 || num == 76 || num == 75))
			{
				if (!flag3)
				{
					flag3 = true;
					if (num31 >= 0 && num31 != num && !Main.TileSolid[num31])
					{
						num31 = -1;
					}
					if (num33 >= 0 && num33 != num && !Main.TileSolid[num33])
					{
						num33 = -1;
					}
					if (num36 >= 0 && num36 != num && !Main.TileSolid[num36])
					{
						num36 = -1;
					}
					if (num38 >= 0 && num38 != num && !Main.TileSolid[num38])
					{
						num38 = -1;
					}
				}
				if (num32 >= 0 && num32 != num)
				{
					num32 = -1;
				}
				if (num37 >= 0 && num37 != num)
				{
					num37 = -1;
				}
				if (num34 >= 0 && num34 != num)
				{
					num34 = -1;
				}
				if (num35 >= 0 && num35 != num)
				{
					num35 = -1;
				}
				if (num32 != -1 && num37 != -1 && num34 != -1 && num35 != -1)
				{
					if (num32 == -2 && num37 == num && num34 == num && num35 == num)
					{
						num3 = 108;
						num2 = 144 + 18 * frameNumber;
						mergeUp2 = true;
					}
					else if (num32 == num && num37 == -2 && num34 == num && num35 == num)
					{
						num3 = 90;
						num2 = 144 + 18 * frameNumber;
						mergeDown2 = true;
					}
					else if (num32 == num && num37 == num && num34 == -2 && num35 == num)
					{
						num2 = 162;
						num3 = 126 + 18 * frameNumber;
						mergeLeft2 = true;
					}
					else if (num32 == num && num37 == num && num34 == num && num35 == -2)
					{
						num2 = 144;
						num3 = 126 + 18 * frameNumber;
						mergeRight2 = true;
					}
					else if (num32 == -2 && num37 == num && num34 == -2 && num35 == num)
					{
						num2 = 36;
						num3 = 90 + 36 * frameNumber;
						mergeUp2 = true;
						mergeLeft2 = true;
					}
					else if (num32 == -2 && num37 == num && num34 == num && num35 == -2)
					{
						num2 = 54;
						num3 = 90 + 36 * frameNumber;
						mergeUp2 = true;
						mergeRight2 = true;
					}
					else if (num32 == num && num37 == -2 && num34 == -2 && num35 == num)
					{
						num2 = 36;
						num3 = 108 + 36 * frameNumber;
						mergeDown2 = true;
						mergeLeft2 = true;
					}
					else if (num32 == num && num37 == -2 && num34 == num && num35 == -2)
					{
						num2 = 54;
						num3 = 108 + 36 * frameNumber;
						mergeDown2 = true;
						mergeRight2 = true;
					}
					else if (num32 == num && num37 == num && num34 == -2 && num35 == -2)
					{
						num2 = 180;
						num3 = 126 + 18 * frameNumber;
						mergeLeft2 = true;
						mergeRight2 = true;
					}
					else if (num32 == -2 && num37 == -2 && num34 == num && num35 == num)
					{
						num3 = 180;
						num2 = 144 + 18 * frameNumber;
						mergeUp2 = true;
						mergeDown2 = true;
					}
					else if (num32 == -2 && num37 == num && num34 == -2 && num35 == -2)
					{
						num2 = 198;
						num3 = 90 + 18 * frameNumber;
						mergeUp2 = true;
						mergeLeft2 = true;
						mergeRight2 = true;
					}
					else if (num32 == num && num37 == -2 && num34 == -2 && num35 == -2)
					{
						num2 = 198;
						num3 = 144 + 18 * frameNumber;
						mergeDown2 = true;
						mergeLeft2 = true;
						mergeRight2 = true;
					}
					else if (num32 == -2 && num37 == -2 && num34 == num && num35 == -2)
					{
						num2 = 216;
						num3 = 144 + 18 * frameNumber;
						mergeUp2 = true;
						mergeDown2 = true;
						mergeRight2 = true;
					}
					else if (num32 == -2 && num37 == -2 && num34 == -2 && num35 == num)
					{
						num2 = 216;
						num3 = 90 + 18 * frameNumber;
						mergeUp2 = true;
						mergeDown2 = true;
						mergeLeft2 = true;
					}
					else if (num32 == -2 && num37 == -2 && num34 == -2 && num35 == -2)
					{
						num3 = 198;
						num2 = 108 + 18 * frameNumber;
						mergeUp2 = true;
						mergeDown2 = true;
						mergeLeft2 = true;
						mergeRight2 = true;
					}
					else if (num32 == num && num37 == num && num34 == num && num35 == num)
					{
						if (num38 == -2)
						{
							num2 = 0;
							num3 = 90 + 36 * frameNumber;
						}
						else if (num36 == -2)
						{
							num2 = 18;
							num3 = 90 + 36 * frameNumber;
						}
						else if (num33 == -2)
						{
							num2 = 0;
							num3 = 108 + 36 * frameNumber;
						}
						else if (num31 == -2)
						{
							num2 = 18;
							num3 = 108 + 36 * frameNumber;
						}
					}
				}
				else
				{
					if (num != 2 && num != 23 && num != 60 && num != 70 && num != 109)
					{
						if (num32 == -1 && num37 == -2 && num34 == num && num35 == num)
						{
							num3 = 0;
							num2 = 234 + 18 * frameNumber;
							mergeDown2 = true;
						}
						else if (num32 == -2 && num37 == -1 && num34 == num && num35 == num)
						{
							num3 = 18;
							num2 = 234 + 18 * frameNumber;
							mergeUp2 = true;
						}
						else if (num32 == num && num37 == num && num34 == -1 && num35 == -2)
						{
							num3 = 36;
							num2 = 234 + 18 * frameNumber;
							mergeRight2 = true;
						}
						else if (num32 == num && num37 == num && num34 == -2 && num35 == -1)
						{
							num3 = 54;
							num2 = 234 + 18 * frameNumber;
							mergeLeft2 = true;
						}
					}
					if (num32 != -1 && num37 != -1 && num34 == -1 && num35 == num)
					{
						if (num32 == -2 && num37 == num)
						{
							num2 = 72;
							num3 = 144 + 18 * frameNumber;
							mergeUp2 = true;
						}
						else if (num37 == -2 && num32 == num)
						{
							num2 = 72;
							num3 = 90 + 18 * frameNumber;
							mergeDown2 = true;
						}
					}
					else if (num32 != -1 && num37 != -1 && num34 == num && num35 == -1)
					{
						if (num32 == -2 && num37 == num)
						{
							num2 = 90;
							num3 = 144 + 18 * frameNumber;
							mergeUp2 = true;
						}
						else if (num37 == -2 && num32 == num)
						{
							num2 = 90;
							num3 = 90 + 18 * frameNumber;
							mergeDown2 = true;
						}
					}
					else if (num32 == -1 && num37 == num && num34 != -1 && num35 != -1)
					{
						if (num34 == -2 && num35 == num)
						{
							num2 = 18 * frameNumber;
							num3 = 198;
							mergeLeft2 = true;
						}
						else if (num35 == -2 && num34 == num)
						{
							num2 = 54 + 18 * frameNumber;
							num3 = 198;
							mergeRight2 = true;
						}
					}
					else if (num32 == num && num37 == -1 && num34 != -1 && num35 != -1)
					{
						if (num34 == -2 && num35 == num)
						{
							num2 = 18 * frameNumber;
							num3 = 216;
							mergeLeft2 = true;
						}
						else if (num35 == -2 && num34 == num)
						{
							num2 = 54 + 18 * frameNumber;
							num3 = 216;
							mergeRight2 = true;
						}
					}
					else if (num32 != -1 && num37 != -1 && num34 == -1 && num35 == -1)
					{
						if (num32 == -2 && num37 == -2)
						{
							num2 = 108;
							num3 = 216 + 18 * frameNumber;
							mergeUp2 = true;
							mergeDown2 = true;
						}
						else if (num32 == -2)
						{
							num2 = 126;
							num3 = 144 + 18 * frameNumber;
							mergeUp2 = true;
						}
						else if (num37 == -2)
						{
							num2 = 126;
							num3 = 90 + 18 * frameNumber;
							mergeDown2 = true;
						}
					}
					else if (num32 == -1 && num37 == -1 && num34 != -1 && num35 != -1)
					{
						if (num34 == -2 && num35 == -2)
						{
							num3 = 198;
							num2 = 162 + 18 * frameNumber;
							mergeLeft2 = true;
							mergeRight2 = true;
						}
						else if (num34 == -2)
						{
							num3 = 252;
							num2 = 18 * frameNumber;
							mergeLeft2 = true;
						}
						else if (num35 == -2)
						{
							num3 = 252;
							num2 = 54 + 18 * frameNumber;
							mergeRight2 = true;
						}
					}
					else if (num32 == -2 && num37 == -1 && num34 == -1 && num35 == -1)
					{
						num2 = 108;
						num3 = 144 + 18 * frameNumber;
						mergeUp2 = true;
					}
					else if (num32 == -1 && num37 == -2 && num34 == -1 && num35 == -1)
					{
						num2 = 108;
						num3 = 90 + 18 * frameNumber;
						mergeDown2 = true;
					}
					else if (num32 == -1 && num37 == -1 && num34 == -2 && num35 == -1)
					{
						num3 = 234;
						num2 = 18 * frameNumber;
						mergeLeft2 = true;
					}
					else if (num32 == -1 && num37 == -1 && num34 == -1 && num35 == -2)
					{
						num3 = 234;
						num2 = 54 + 18 * frameNumber;
						mergeRight2 = true;
					}
				}
			}
			if (num2 < 0)
			{
				if (!flag3)
				{
					flag3 = true;
					if (num32 >= 0 && num32 != num && !Main.TileSolid[num32])
					{
						num32 = -1;
					}
					if (num37 >= 0 && num37 != num && !Main.TileSolid[num37])
					{
						num37 = -1;
					}
					if (num34 >= 0 && num34 != num && !Main.TileSolid[num34])
					{
						num34 = -1;
					}
					if (num35 >= 0 && num35 != num && !Main.TileSolid[num35])
					{
						num35 = -1;
					}
					if (num31 >= 0 && num31 != num && !Main.TileSolid[num31])
					{
						num31 = -1;
					}
					if (num33 >= 0 && num33 != num && !Main.TileSolid[num33])
					{
						num33 = -1;
					}
					if (num36 >= 0 && num36 != num && !Main.TileSolid[num36])
					{
						num36 = -1;
					}
					if (num38 >= 0 && num38 != num && !Main.TileSolid[num38])
					{
						num38 = -1;
					}
				}
				if (num == 2 || num == 23 || num == 60 || num == 70 || num == 109)
				{
					if (num32 == -2)
					{
						num32 = num;
					}
					if (num37 == -2)
					{
						num37 = num;
					}
					if (num34 == -2)
					{
						num34 = num;
					}
					if (num35 == -2)
					{
						num35 = num;
					}
					if (num31 == -2)
					{
						num31 = num;
					}
					if (num33 == -2)
					{
						num33 = num;
					}
					if (num36 == -2)
					{
						num36 = num;
					}
					if (num38 == -2)
					{
						num38 = num;
					}
				}
				if (num32 == num && num37 == num && num34 == num && num35 == num)
				{
					if (num31 != num && num33 != num)
					{
						num3 = 18;
						num2 = 108 + 18 * frameNumber;
					}
					else if (num36 != num && num38 != num)
					{
						num3 = 36;
						num2 = 108 + 18 * frameNumber;
					}
					else if (num31 != num && num36 != num)
					{
						num2 = 180;
						num3 = 18 * frameNumber;
					}
					else if (num33 != num && num38 != num)
					{
						num2 = 198;
						num3 = 18 * frameNumber;
					}
					else
					{
						num3 = 18;
						num2 = 18 + 18 * frameNumber;
					}
				}
				else if (num32 != num && num37 == num && num34 == num && num35 == num)
				{
					num3 = 0;
					num2 = 18 + 18 * frameNumber;
				}
				else if (num32 == num && num37 != num && num34 == num && num35 == num)
				{
					num3 = 36;
					num2 = 18 + 18 * frameNumber;
				}
				else if (num32 == num && num37 == num && num34 != num && num35 == num)
				{
					num2 = 0;
					num3 = 18 * frameNumber;
				}
				else if (num32 == num && num37 == num && num34 == num && num35 != num)
				{
					num2 = 72;
					num3 = 18 * frameNumber;
				}
				else if (num32 != num && num37 == num && num34 != num && num35 == num)
				{
					num2 = 36 * frameNumber;
					num3 = 54;
				}
				else if (num32 != num && num37 == num && num34 == num && num35 != num)
				{
					num2 = 18 + 36 * frameNumber;
					num3 = 54;
				}
				else if (num32 == num && num37 != num && num34 != num && num35 == num)
				{
					num2 = 36 * frameNumber;
					num3 = 72;
				}
				else if (num32 == num && num37 != num && num34 == num && num35 != num)
				{
					num2 = 18 + 36 * frameNumber;
					num3 = 72;
				}
				else if (num32 == num && num37 == num && num34 != num && num35 != num)
				{
					num2 = 90;
					num3 = 18 * frameNumber;
				}
				else if (num32 != num && num37 != num && num34 == num && num35 == num)
				{
					num2 = 108 + 18 * frameNumber;
					num3 = 72;
				}
				else if (num32 != num && num37 == num && num34 != num && num35 != num)
				{
					num2 = 108 + 18 * frameNumber;
					num3 = 0;
				}
				else if (num32 == num && num37 != num && num34 != num && num35 != num)
				{
					num2 = 108 + 18 * frameNumber;
					num3 = 54;
				}
				else if (num32 != num && num37 != num && num34 != num && num35 == num)
				{
					num2 = 162;
					num3 = 18 * frameNumber;
				}
				else if (num32 != num && num37 != num && num34 == num && num35 != num)
				{
					num2 = 216;
					num3 = 18 * frameNumber;
				}
				else if (num32 != num && num37 != num && num34 != num && num35 != num)
				{
					num2 = 162 + 18 * frameNumber;
					num3 = 54;
				}
			}
			if (num2 < 0)
			{
				num3 = 18;
				num2 = 18 + 18 * frameNumber;
			}
			ptr->FrameX = (short)num2;
			ptr->FrameY = (short)num3;
			if (num == 52 || num == 62 || num == 115)
			{
				num32 = ((ptr[-1].IsActive != 0) ? ptr[-1].Type : (-1));
				if (num == 52 && (num32 == 109 || num32 == 115))
				{
					ptr->Type = 115;
					SquareTileFrameNoLiquid(i, j);
					return;
				}
				if (num == 115 && (num32 == 2 || num32 == 52))
				{
					ptr->Type = 52;
					SquareTileFrameNoLiquid(i, j);
					return;
				}
				if (num32 != num && (num32 == -1 || (num == 52 && num32 != 2) || (num == 62 && num32 != 60) || (num == 115 && num32 != 109)))
				{
					KillTile(i, j);
				}
			}
			if (Gen || Main.NetMode == 1 || (num != 53 && num != 112 && num != 116 && num != 123))
			{
				return;
			}
			Tile* ptr3 = ptr + 1;
			if (ptr3->IsActive == 0)
			{
				ptr3 = ptr - 1;
				if (ptr3->IsActive == 0 || ptr3->Type != 21)
				{
					ptr->IsActive = 0;
					sandBuffer[currentSandBuffer].Add(i, j);
					return;
				}
			}
		}
	}

	public static void UpdateMagmaLayerPos()
	{
		int num = (int)((double)(Main.MaxTilesY - 230 - Main.WorldSurface) / 6.0) * 6;
		Main.MagmaLayer = Main.WorldSurface + num - 5;
		Main.MagmaLayerPixels = Main.MagmaLayer << 4;
	}
}
