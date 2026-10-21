using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.Achievements;

namespace Terraria;

public sealed class NPC
{
	public const int MaxNumNPCs = 196;

	private const int SpawnSpaceX = 3;

	private const int SpawnSpaceY = 3;

	public const int SpawnWidth = 1920;

	public const int SpawnHeight = 1080;

	public const int SafeRangeX = 62;

	public const int SafeRangeY = 34;

	private const int SpawnRangeX = 84;

	private const int SpawnRangeY = 46;

	private const int ActiveRangeX = 3264;

	private const int ActiveRangeY = 1836;

	private const int TownRangeX = 1920;

	private const int TownRangeY = 1080;

	private const int NPCActiveTime = 750;

	private const int DefaultSpawnRate = 600;

	private const int DefaultMaxSpawns = 5;

	private const int DrawOnStrike = 96;

	public const int DrawNameWhenNearby = 32;

	public const int MaxNumNPCTypes = 168;

	public const int MaxNumNamedNPCs = 125;

	public const int MaxNumNPCBuffs = 5;

	public const int MaxNumTownNPCs = 10;

	public static string[] TypeNames = new string[125];

	public static byte[] NpcFrameCount = new byte[168]
	{
		1, 2, 2, 3, 6, 2, 2, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 2, 16, 14, 16,
		14, 15, 16, 2, 10, 1, 16, 16, 16, 3,
		1, 15, 3, 1, 3, 1, 1, 16, 16, 1,
		1, 1, 3, 3, 15, 3, 7, 7, 4, 5,
		5, 5, 3, 3, 16, 6, 3, 6, 6, 2,
		5, 3, 2, 7, 7, 4, 2, 8, 1, 5,
		1, 2, 4, 16, 5, 4, 4, 15, 15, 15,
		15, 2, 4, 6, 6, 24, 16, 1, 1, 1,
		1, 1, 1, 4, 3, 1, 1, 1, 1, 1,
		1, 5, 6, 7, 16, 1, 1, 16, 16, 12,
		20, 21, 1, 2, 2, 3, 6, 1, 1, 1,
		15, 4, 11, 1, 14, 6, 6, 3, 1, 2,
		2, 1, 3, 4, 1, 2, 1, 4, 2, 1,
		15, 3, 16, 4, 5, 7, 3, 5, 4, 15,
		2, 6, 15, 11, 15, 15, 3, 3, 3, 1,
		1, 1, 1, 1, 1, 2, 6, 2
	};

	public static int WoF = -1;

	public static int WoFTop;

	public static int WoFBottom;

	public static int WoFFront = 0;

	private static bool NoSpawnCycle = false;

	public static short CheckForSpawnsTimer = 0;

	public Vector2[] OldPos = new Vector2[10];

	public short NetSpam;

	public short NetSkip;

	public bool NetAlways;

	public int RealLife = -1;

	public float NpcSlots = 1f;

	public bool IsWet;

	public byte WetCount;

	public bool LavaWet;

	public Buff[] ActiveBuffs = new Buff[5];

	public bool[] BuffImmune = new bool[41];

	public bool IsPoisoned;

	public bool IsConfused;

	public int LifeRegenCount;

	public static bool HasDownedBoss1 = false;

	public static bool HasDownedBoss2 = false;

	public static bool HasDownedBoss3 = false;

	public static bool HasSavedGoblin = false;

	public static bool HasSavedWizard = false;

	public static bool HasSavedMech = false;

	public static bool HasDownedGoblins = false;

	public static bool HasDownedFrost = false;

	public static bool HasDownedClown = false;

	private static int SpawnRate = 600;

	private static int MaxSpawns = 5;

	public byte Active;

	public byte Type;

	public bool WasJustHit;

	public bool HasNoGravity;

	public bool HasNoTileCollide;

	public bool ShouldNetUpdate;

	public bool ShouldNetUpdate2;

	public bool HasXCollision;

	public bool HasYCollision;

	public bool IsBoss;

	public bool IsBehindTiles;

	public bool IsLavaImmune;

	public bool DontTakeDamage;

	public short DrawMyName;

	public bool IsTownNPC;

	public bool IsHomeless;

	public bool IsFriendly;

	private bool HasClosedDoor;

	private bool WasHomeless;

	public Vector2 OldPosition;

	public Vector2 OldVelocity;

	public Vector2 Position;

	public Vector2 Velocity;

	public Rectangle XYWH;

	public ushort Width;

	public ushort Height;

	public byte[] Immunities = new byte[9];

	public sbyte Direction = 1;

	public sbyte DirectionY = 1;

	public byte AIAction;

	public byte AIStyle;

	public byte Target = 8;

	public float AI0;

	public float AI1;

	public float AI2;

	public float AI3;

	public int LocalAI0;

	public int LocalAI1;

	public int LocalAI2;

	public int LocalAI3;

	public int TimeLeft;

	public int Damage;

	public int Defense;

	public int DefDamage;

	public short DefDefense;

	public short SoundDelay;

	public short SoundHit;

	public short SoundKilled;

	public int HealthBarLife;

	public int Life;

	public int LifeMax;

	public Rectangle TargetRect;

	public float FrameCounter;

	public short FrameY;

	public short FrameHeight;

	public Color Colour;

	public float Scale = 1f;

	public float KnockBackResist = 1f;

	public byte Alpha;

	public sbyte SpriteDirection = -1;

	public sbyte OldDirection;

	public sbyte OldDirectionY;

	public short OldTarget;

	public short WhoAmI;

	public float Rotation;

	public float Value;

	public short NetID;

	public short HomeTileX = -1;

	public short HomeTileY = -1;

	public short OldHomeTileX = -1;

	public short OldHomeTileY = -1;

	public short DoorX;

	public short DoorY;

	public short FriendlyRegen;

	public string TypeName;

	public string DisplayName;

	public static void ClearNames()
	{
		for (int i = 0; i < 125; i++)
		{
			TypeNames[i] = null;
		}
	}

	public bool HasName()
	{
		if (Type < 125)
		{
			return TypeNames[Type] != null;
		}
		return false;
	}

	public string GetName()
	{
		return TypeNames[Type];
	}

	public static void SetNames()
	{
		if (TypeNames[18] == null)
		{
			int num = WorldGen.genRand.Next(23);
			if (1 == 0)
			{
			}
			string text = num switch
			{
				0 => "Molly", 
				1 => "Amy", 
				2 => "Claire", 
				3 => "Emily", 
				4 => "Katie", 
				5 => "Madeline", 
				6 => "Katelyn", 
				7 => "Emma", 
				8 => "Abigail", 
				9 => "Carly", 
				10 => "Jenna", 
				11 => "Heather", 
				12 => "Katherine", 
				13 => "Caitlin", 
				14 => "Kaitlin", 
				15 => "Holly", 
				16 => "Kaitlyn", 
				17 => "Hannah", 
				18 => "Kathryn", 
				19 => "Lorraine", 
				20 => "Helen", 
				21 => "Kayla", 
				_ => "Allison", 
			};
			if (1 == 0)
			{
			}
			string text2 = text;
			TypeNames[18] = text2;
		}
		if (TypeNames[124] == null)
		{
			int num2 = WorldGen.genRand.Next(24);
			if (1 == 0)
			{
			}
			string text = num2 switch
			{
				0 => "Shayna", 
				1 => "Korrie", 
				2 => "Ginger", 
				3 => "Brooke", 
				4 => "Jenny", 
				5 => "Autumn", 
				6 => "Nancy", 
				7 => "Ella", 
				8 => "Kayla", 
				9 => "Beth", 
				10 => "Sophia", 
				11 => "Marshanna", 
				12 => "Lauren", 
				13 => "Trisha", 
				14 => "Shirlena", 
				15 => "Sheena", 
				16 => "Ellen", 
				17 => "Amy", 
				18 => "Dawn", 
				19 => "Susana", 
				20 => "Meredith", 
				21 => "Selene", 
				22 => "Terra", 
				_ => "Sally", 
			};
			if (1 == 0)
			{
			}
			string text3 = text;
			TypeNames[124] = text3;
		}
		if (TypeNames[19] == null)
		{
			int num3 = WorldGen.genRand.Next(23);
			if (1 == 0)
			{
			}
			string text = num3 switch
			{
				0 => "DeShawn", 
				1 => "DeAndre", 
				2 => "Marquis", 
				3 => "Darnell", 
				4 => "Terrell", 
				5 => "Malik", 
				6 => "Trevon", 
				7 => "Tyrone", 
				8 => "Willie", 
				9 => "Dominique", 
				10 => "Demetrius", 
				11 => "Reginald", 
				12 => "Jamal", 
				13 => "Maurice", 
				14 => "Jalen", 
				15 => "Darius", 
				16 => "Xavier", 
				17 => "Terrance", 
				18 => "Andre", 
				19 => "Dante", 
				20 => "Brimst", 
				21 => "Bronson", 
				_ => "Darryl", 
			};
			if (1 == 0)
			{
			}
			string text4 = text;
			TypeNames[19] = text4;
		}
		if (TypeNames[22] == null)
		{
			int num4 = WorldGen.genRand.Next(35);
			if (1 == 0)
			{
			}
			string text = num4 switch
			{
				0 => "Jake", 
				1 => "Connor", 
				2 => "Tanner", 
				3 => "Wyatt", 
				4 => "Cody", 
				5 => "Dustin", 
				6 => "Luke", 
				7 => "Jack", 
				8 => "Scott", 
				9 => "Logan", 
				10 => "Cole", 
				11 => "Lucas", 
				12 => "Bradley", 
				13 => "Jacob", 
				14 => "Garrett", 
				15 => "Dylan", 
				16 => "Maxwell", 
				17 => "Steve", 
				18 => "Brett", 
				19 => "Andrew", 
				20 => "Harley", 
				21 => "Kyle", 
				22 => "Jake", 
				23 => "Ryan", 
				24 => "Jeffrey", 
				25 => "Seth", 
				26 => "Marty", 
				27 => "Brandon", 
				28 => "Zach", 
				29 => "Jeff", 
				30 => "Daniel", 
				31 => "Trent", 
				32 => "Kevin", 
				33 => "Brian", 
				_ => "Colin", 
			};
			if (1 == 0)
			{
			}
			string text5 = text;
			TypeNames[22] = text5;
		}
		if (TypeNames[20] == null)
		{
			int num5 = WorldGen.genRand.Next(22);
			if (1 == 0)
			{
			}
			string text = num5 switch
			{
				0 => "Alalia", 
				1 => "Alalia", 
				2 => "Alura", 
				3 => "Ariella", 
				4 => "Caelia", 
				5 => "Calista", 
				6 => "Chryseis", 
				7 => "Emerenta", 
				8 => "Elysia", 
				9 => "Evvie", 
				10 => "Faye", 
				11 => "Felicitae", 
				12 => "Lunette", 
				13 => "Nata", 
				14 => "Nissa", 
				15 => "Tatiana", 
				16 => "Rosalva", 
				17 => "Shea", 
				18 => "Tania", 
				19 => "Isis", 
				20 => "Celestia", 
				_ => "Xylia", 
			};
			if (1 == 0)
			{
			}
			string text6 = text;
			TypeNames[20] = text6;
		}
		if (TypeNames[38] == null)
		{
			int num6 = WorldGen.genRand.Next(22);
			if (1 == 0)
			{
			}
			string text = num6 switch
			{
				0 => "Dolbere", 
				1 => "Bazdin", 
				2 => "Durim", 
				3 => "Tordak", 
				4 => "Garval", 
				5 => "Morthal", 
				6 => "Oten", 
				7 => "Dolgen", 
				8 => "Gimli", 
				9 => "Gimut", 
				10 => "Duerthen", 
				11 => "Beldin", 
				12 => "Jarut", 
				13 => "Ovbere", 
				14 => "Norkas", 
				15 => "Dolgrim", 
				16 => "Boften", 
				17 => "Norsun", 
				18 => "Dias", 
				19 => "Fikod", 
				20 => "Urist", 
				_ => "Darur", 
			};
			if (1 == 0)
			{
			}
			string text7 = text;
			TypeNames[38] = text7;
		}
		if (TypeNames[108] == null)
		{
			int num7 = WorldGen.genRand.Next(21);
			if (1 == 0)
			{
			}
			string text = num7 switch
			{
				0 => "Dalamar", 
				1 => "Dulais", 
				2 => "Elric", 
				3 => "Arddun", 
				4 => "Maelor", 
				5 => "Leomund", 
				6 => "Hirael", 
				7 => "Gwentor", 
				8 => "Greum", 
				9 => "Gearroid", 
				10 => "Fizban", 
				11 => "Ningauble", 
				12 => "Seonag", 
				13 => "Sargon", 
				14 => "Merlyn", 
				15 => "Magius", 
				16 => "Berwyn", 
				17 => "Arwyn", 
				18 => "Alasdair", 
				19 => "Tagar", 
				_ => "Xanadu", 
			};
			if (1 == 0)
			{
			}
			string text8 = text;
			TypeNames[108] = text8;
		}
		if (TypeNames[17] == null)
		{
			int num8 = WorldGen.genRand.Next(23);
			if (1 == 0)
			{
			}
			string text = num8 switch
			{
				0 => "Alfred", 
				1 => "Barney", 
				2 => "Calvin", 
				3 => "Edmund", 
				4 => "Edwin", 
				5 => "Eugene", 
				6 => "Frank", 
				7 => "Frederick", 
				8 => "Gilbert", 
				9 => "Gus", 
				10 => "Wilbur", 
				11 => "Seymour", 
				12 => "Louis", 
				13 => "Humphrey", 
				14 => "Harold", 
				15 => "Milton", 
				16 => "Mortimer", 
				17 => "Howard", 
				18 => "Walter", 
				19 => "Finn", 
				20 => "Isacc", 
				21 => "Joseph", 
				_ => "Ralph", 
			};
			if (1 == 0)
			{
			}
			string text9 = text;
			TypeNames[17] = text9;
		}
		if (TypeNames[54] == null)
		{
			int num9 = WorldGen.genRand.Next(24);
			if (1 == 0)
			{
			}
			string text = num9 switch
			{
				0 => "Sebastian", 
				1 => "Rupert", 
				2 => "Clive", 
				3 => "Nigel", 
				4 => "Mervyn", 
				5 => "Cedric", 
				6 => "Pip", 
				7 => "Cyril", 
				8 => "Fitz", 
				9 => "Lloyd", 
				10 => "Arthur", 
				11 => "Rodney", 
				12 => "Graham", 
				13 => "Edward", 
				14 => "Alfred", 
				15 => "Edmund", 
				16 => "Henry", 
				17 => "Herald", 
				18 => "Roland", 
				19 => "Lincoln", 
				20 => "Lloyd", 
				21 => "Edgar", 
				22 => "Eustace", 
				_ => "Rodrick", 
			};
			if (1 == 0)
			{
			}
			string text10 = text;
			TypeNames[54] = text10;
		}
		if (TypeNames[107] == null)
		{
			int num10 = WorldGen.genRand.Next(25);
			if (1 == 0)
			{
			}
			string text = num10 switch
			{
				0 => "Grodax", 
				1 => "Sarx", 
				2 => "Xon", 
				3 => "Mrunok", 
				4 => "Nuxatk", 
				5 => "Tgerd", 
				6 => "Darz", 
				7 => "Smador", 
				8 => "Stazen", 
				9 => "Mobart", 
				10 => "Knogs", 
				11 => "Tkanus", 
				12 => "Negurk", 
				13 => "Nort", 
				14 => "Durnok", 
				15 => "Trogem", 
				16 => "Stezom", 
				17 => "Gnudar", 
				18 => "Ragz", 
				19 => "Fahd", 
				20 => "Xanos", 
				21 => "Arback", 
				22 => "Fjell", 
				23 => "Dalek", 
				_ => "Knub", 
			};
			if (1 == 0)
			{
			}
			string text11 = text;
			TypeNames[107] = text11;
		}
	}

	public void NetDefaults(int NPCType)
	{
		if (NPCType < 0)
		{
			switch (NPCType)
			{
			case -1:
				SetDefaults("Slimeling");
				break;
			case -2:
				SetDefaults("Slimer2");
				break;
			case -3:
				SetDefaults("Green Slime");
				break;
			case -4:
				SetDefaults("Pinky");
				break;
			case -5:
				SetDefaults("Baby Slime");
				break;
			case -6:
				SetDefaults("Black Slime");
				break;
			case -7:
				SetDefaults("Purple Slime");
				break;
			case -8:
				SetDefaults("Red Slime");
				break;
			case -9:
				SetDefaults("Yellow Slime");
				break;
			case -10:
				SetDefaults("Jungle Slime");
				break;
			case -11:
				SetDefaults("Little Eater");
				break;
			case -12:
				SetDefaults("Big Eater");
				break;
			case -13:
				SetDefaults("Short Bones");
				break;
			case -14:
				SetDefaults("Big Boned");
				break;
			case -15:
				SetDefaults("Heavy Skeleton");
				break;
			case -16:
				SetDefaults("Little Stinger");
				break;
			case -17:
				SetDefaults("Big Stinger");
				break;
			case -69:
				SetDefaults("Slimeling2");
				break;
			}
		}
		else
		{
			SetDefaults(NPCType);
		}
	}

	public void SetDefaults(string Name)
	{
		switch (Name)
		{
		case "Slimeling":
			SetDefaults(81, 0.6);
			TypeName = Name;
			Damage = 45;
			Defense = 10;
			Life = 90;
			KnockBackResist = 1.2f;
			Value = 100f;
			NetID = -1;
			break;
		case "Slimeling2":
			SetDefaults(150, 0.6);
			TypeName = Name;
			Damage = 45;
			Defense = 10;
			Life = 105;
			KnockBackResist = 1.2f;
			Value = 100f;
			NetID = -69;
			break;
		case "Slimer2":
			SetDefaults(81, 0.9);
			TypeName = Name;
			Damage = 45;
			Defense = 20;
			Life = 90;
			KnockBackResist = 1.2f;
			Value = 100f;
			NetID = -2;
			break;
		case "Green Slime":
			SetDefaults(1, 0.9);
			TypeName = Name;
			Damage = 6;
			Defense = 0;
			Life = 14;
			KnockBackResist = 1.2f;
			Colour = new Color(0, 220, 40, 100);
			Value = 3f;
			NetID = -3;
			break;
		case "Pinky":
			SetDefaults(1, 0.6);
			TypeName = Name;
			Damage = 5;
			Defense = 5;
			Life = 150;
			KnockBackResist = 1.4f;
			Colour = new Color(250, 30, 90, 90);
			Value = 10000f;
			NetID = -4;
			break;
		case "Baby Slime":
			SetDefaults(1, 0.9);
			TypeName = Name;
			Damage = 13;
			Defense = 4;
			Life = 30;
			KnockBackResist = 0.95f;
			Alpha = 120;
			Colour = new Color(0, 0, 0, 50);
			Value = 10f;
			NetID = -5;
			break;
		case "Black Slime":
			SetDefaults(1);
			TypeName = Name;
			Damage = 15;
			Defense = 4;
			Life = 45;
			Colour = new Color(0, 0, 0, 50);
			Value = 20f;
			NetID = -6;
			break;
		case "Purple Slime":
			SetDefaults(1, 1.2);
			TypeName = Name;
			Damage = 12;
			Defense = 6;
			Life = 40;
			KnockBackResist = 0.9f;
			Colour = new Color(200, 0, 255, 150);
			Value = 10f;
			NetID = -7;
			break;
		case "Red Slime":
			SetDefaults(1);
			TypeName = Name;
			Damage = 12;
			Defense = 4;
			Life = 35;
			Colour = new Color(255, 30, 0, 100);
			Value = 8f;
			NetID = -8;
			break;
		case "Yellow Slime":
			SetDefaults(1, 1.2);
			TypeName = Name;
			Damage = 15;
			Defense = 7;
			Life = 45;
			Colour = new Color(255, 255, 0, 100);
			Value = 10f;
			NetID = -9;
			break;
		case "Jungle Slime":
			SetDefaults(1, 1.1);
			TypeName = Name;
			Damage = 18;
			Defense = 6;
			Life = 60;
			Colour = new Color(143, 215, 93, 100);
			Value = 500f;
			NetID = -10;
			break;
		case "Little Eater":
			SetDefaults(6, 0.85);
			TypeName = Name;
			Defense = (int)((float)Defense * Scale);
			Damage = (int)((float)Damage * Scale);
			Life = (int)((float)Life * Scale);
			Value = (int)(Value * Scale);
			NpcSlots *= Scale;
			KnockBackResist *= 2f - Scale;
			NetID = -11;
			break;
		case "Big Eater":
			SetDefaults(6, 1.15);
			TypeName = Name;
			Defense = (int)((float)Defense * Scale);
			Damage = (int)((float)Damage * Scale);
			Life = (int)((float)Life * Scale);
			Value = (int)(Value * Scale);
			NpcSlots *= Scale;
			KnockBackResist *= 2f - Scale;
			NetID = -12;
			break;
		case "Short Bones":
			SetDefaults(31, 0.9);
			TypeName = Name;
			Defense = (int)((float)Defense * Scale);
			Damage = (int)((float)Damage * Scale);
			Life = (int)((float)Life * Scale);
			Value = (int)(Value * Scale);
			NetID = -13;
			break;
		case "Big Boned":
			SetDefaults(31, 1.15);
			TypeName = Name;
			Defense = (int)((float)Defense * Scale);
			Damage = (int)((double)((float)Damage * Scale) * 1.1);
			Life = (int)((double)((float)Life * Scale) * 1.1);
			Value = (int)(Value * Scale);
			NpcSlots = 2f;
			KnockBackResist *= 2f - Scale;
			NetID = -14;
			break;
		case "Heavy Skeleton":
			SetDefaults(77, 1.15);
			TypeName = Name;
			Defense = (int)((float)Defense * Scale);
			Damage = (int)((double)((float)Damage * Scale) * 1.1);
			Life = 400;
			Value = (int)(Value * Scale);
			NpcSlots = 2f;
			KnockBackResist *= 2f - Scale;
			Height = 44;
			NetID = -15;
			break;
		case "Little Stinger":
			SetDefaults(42, 0.85);
			TypeName = Name;
			Defense = (int)((float)Defense * Scale);
			Damage = (int)((float)Damage * Scale);
			Life = (int)((float)Life * Scale);
			Value = (int)(Value * Scale);
			NpcSlots *= Scale;
			KnockBackResist *= 2f - Scale;
			NetID = -16;
			break;
		case "Big Stinger":
			SetDefaults(42, 1.2);
			TypeName = Name;
			Defense = (int)((float)Defense * Scale);
			Damage = (int)((float)Damage * Scale);
			Life = (int)((float)Life * Scale);
			Value = (int)(Value * Scale);
			NpcSlots *= Scale;
			KnockBackResist *= 2f - Scale;
			NetID = -17;
			break;
		}
		DisplayName = Lang.NpcName(NetID);
		LifeMax = Life;
		HealthBarLife = Life;
		DefDamage = Damage;
		DefDefense = (short)Defense;
	}

	public bool CanTalk()
	{
		if (!IsTownNPC && Type != 105 && Type != 106)
		{
			return Type == 123;
		}
		return true;
	}

	public static bool MechSpawn(int NPCX, int NPCY, int NPCType)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < 196; i++)
		{
			if (Main.NPCSet[i].Active != 0 && Main.NPCSet[i].Type == NPCType)
			{
				num++;
				Vector2 vector = new Vector2(NPCX, NPCY);
				float num4 = Main.NPCSet[i].Position.X - vector.X;
				float num5 = Main.NPCSet[i].Position.Y - vector.Y;
				float num6 = num4 * num4 + num5 * num5;
				if (num6 < 40000f)
				{
					num2++;
				}
				if (num6 < 360000f)
				{
					num3++;
				}
			}
		}
		if (num2 >= 3 || num3 >= 6 || num >= 10)
		{
			return false;
		}
		return true;
	}

	public int GetHeadTextureID()
	{
		EntityID.NPCID type = (EntityID.NPCID)Type;
		if (1 == 0)
		{
		}
		int result = type switch
		{
			EntityID.NPCID.MERCHANT => 2, 
			EntityID.NPCID.NURSE => 3, 
			EntityID.NPCID.ARMS_DEALER => 6, 
			EntityID.NPCID.DRYAD => 5, 
			EntityID.NPCID.GUIDE => 1, 
			EntityID.NPCID.DEMOLITIONIST => 4, 
			EntityID.NPCID.CLOTHIER => 7, 
			EntityID.NPCID.GOBLIN_TINKERER => 9, 
			EntityID.NPCID.WIZARD => 10, 
			EntityID.NPCID.MECHANIC => 8, 
			EntityID.NPCID.SANTA_CLAUS => 11, 
			_ => -1, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public void SetDefaults(int NPCType, double ScaleOverride = -1.0)
	{
		Type = (byte)NPCType;
		NetID = (short)NPCType;
		NetAlways = false;
		NetSpam = 0;
		DrawMyName = 0;
		for (int i = 0; i < OldPos.Length; i++)
		{
			OldPos[i].X = 0f;
			OldPos[i].Y = 0f;
		}
		for (int j = 0; j < 5; j++)
		{
			ActiveBuffs[j].Time = 0;
			ActiveBuffs[j].Type = 0;
		}
		for (int k = 0; k < 41; k++)
		{
			BuffImmune[k] = false;
		}
		BuffImmune[31] = true;
		NetSkip = -2;
		RealLife = -1;
		LifeRegenCount = 0;
		IsPoisoned = false;
		IsConfused = false;
		WasJustHit = false;
		DontTakeDamage = false;
		NpcSlots = 1f;
		IsLavaImmune = false;
		LavaWet = false;
		WetCount = 0;
		IsWet = false;
		IsTownNPC = false;
		IsHomeless = false;
		HomeTileX = -1;
		HomeTileY = -1;
		IsFriendly = false;
		IsBehindTiles = false;
		IsBoss = false;
		HasNoTileCollide = false;
		Rotation = 0f;
		Active = 1;
		Alpha = 0;
		Colour = default;
		HasXCollision = false;
		HasYCollision = false;
		Direction = 0;
		OldDirection = 0;
		FrameCounter = 0f;
		ShouldNetUpdate = true;
		ShouldNetUpdate2 = false;
		KnockBackResist = 1f;
		TypeName = "";
		HasNoGravity = false;
		Scale = 1f;
		SoundHit = 0;
		SoundKilled = 0;
		SpriteDirection = -1;
		Target = 8;
		OldTarget = Target;
		TargetRect = default;
		TimeLeft = 750;
		Value = 0f;
		AI0 = 0f;
		AI1 = 0f;
		AI2 = 0f;
		AI3 = 0f;
		LocalAI0 = 0;
		LocalAI1 = 0;
		LocalAI2 = 0;
		LocalAI3 = 0;
		switch ((EntityID.NPCID)NPCType)
		{
		case EntityID.NPCID.SLIME:
			TypeName = "Blue Slime";
			Width = 24;
			Height = 18;
			AIStyle = 1;
			Damage = 7;
			Defense = 2;
			LifeMax = 25;
			SoundHit = 1;
			SoundKilled = 1;
			Alpha = 175;
			Colour = new Color(0, 80, 255, 100);
			Value = 25f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.DEMON_EYE:
			TypeName = "Demon Eye";
			Width = 30;
			Height = 32;
			AIStyle = 2;
			Damage = 18;
			Defense = 2;
			LifeMax = 60;
			SoundHit = 1;
			KnockBackResist = 0.8f;
			SoundKilled = 1;
			Value = 75f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.ZOMBIE:
			TypeName = "Zombie";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 14;
			Defense = 6;
			LifeMax = 45;
			SoundHit = 1;
			SoundKilled = 2;
			KnockBackResist = 0.5f;
			Value = 60f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.EYE_OF_CTHULHU:
			TypeName = "Eye of Cthulhu";
			Width = 100;
			Height = 110;
			AIStyle = 4;
			Damage = 15;
			Defense = 12;
			LifeMax = 2800;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0f;
			HasNoGravity = true;
			HasNoTileCollide = true;
			TimeLeft = 22500;
			IsBoss = true;
			Value = 30000f;
			NpcSlots = 5f;
			break;
		case EntityID.NPCID.OCRAM:
			TypeName = "Ocram";
			Width = 100;
			Height = 110;
			AIStyle = 39;
			Damage = 65;
			Defense = 20;
			LifeMax = 35000;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0f;
			HasNoGravity = true;
			HasNoTileCollide = true;
			TimeLeft = 22500;
			IsBoss = true;
			Value = 100000f;
			NpcSlots = 5f;
			BuffImmune[20] = true;
			break;
		case EntityID.NPCID.SERVANT_OF_CTHULHU:
			TypeName = "Servant of Cthulhu";
			Width = 20;
			Height = 20;
			AIStyle = 5;
			Damage = 13;
			Defense = 0;
			LifeMax = 10;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			break;
		case EntityID.NPCID.SERVANT_OF_OCRAM:
			TypeName = "Servant of Ocram";
			Width = 20;
			Height = 20;
			AIStyle = 5;
			Damage = 35;
			Defense = 5;
			LifeMax = 130;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			break;
		case EntityID.NPCID.EATER_OF_SOULS:
			NpcSlots = 1f;
			TypeName = "Eater of Souls";
			Width = 30;
			Height = 30;
			AIStyle = 5;
			Damage = 22;
			Defense = 8;
			LifeMax = 40;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			KnockBackResist = 0.5f;
			Value = 90f;
			break;
		case EntityID.NPCID.DEVOURER_HEAD:
			NpcSlots = 3.5f;
			TypeName = "Devourer Head";
			Width = 22;
			Height = 22;
			AIStyle = 6;
			Damage = 31;
			Defense = 2;
			LifeMax = 100;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 140f;
			NetAlways = true;
			break;
		case EntityID.NPCID.DEVOURER_BODY:
			TypeName = "Devourer Body";
			Width = 22;
			Height = 22;
			AIStyle = 6;
			NetAlways = true;
			Damage = 16;
			Defense = 6;
			LifeMax = 100;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 140f;
			break;
		case EntityID.NPCID.DEVOURER_TAIL:
			TypeName = "Devourer Tail";
			Width = 22;
			Height = 22;
			AIStyle = 6;
			NetAlways = true;
			Damage = 13;
			Defense = 10;
			LifeMax = 100;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 140f;
			break;
		case EntityID.NPCID.GIANT_WORM_HEAD:
			TypeName = "Giant Worm Head";
			Width = 14;
			Height = 14;
			AIStyle = 6;
			NetAlways = true;
			Damage = 8;
			Defense = 0;
			LifeMax = 30;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 40f;
			break;
		case EntityID.NPCID.GIANT_WORM_BODY:
			TypeName = "Giant Worm Body";
			Width = 14;
			Height = 14;
			AIStyle = 6;
			NetAlways = true;
			Damage = 4;
			Defense = 4;
			LifeMax = 30;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 40f;
			break;
		case EntityID.NPCID.GIANT_WORM_TAIL:
			TypeName = "Giant Worm Tail";
			Width = 14;
			Height = 14;
			AIStyle = 6;
			NetAlways = true;
			Damage = 4;
			Defense = 6;
			LifeMax = 30;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 40f;
			break;
		case EntityID.NPCID.EATER_OF_WORLDS_HEAD:
			NpcSlots = 5f;
			TypeName = "Eater of Worlds Head";
			Width = 38;
			Height = 38;
			AIStyle = 6;
			NetAlways = true;
			Damage = 22;
			Defense = 2;
			LifeMax = 65;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 300f;
			Scale = 1f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.EATER_OF_WORLDS_BODY:
			TypeName = "Eater of Worlds Body";
			Width = 38;
			Height = 38;
			AIStyle = 6;
			NetAlways = true;
			Damage = 13;
			Defense = 4;
			LifeMax = 150;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 300f;
			Scale = 1f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.EATER_OF_WORLDS_TAIL:
			TypeName = "Eater of Worlds Tail";
			Width = 38;
			Height = 38;
			AIStyle = 6;
			NetAlways = true;
			Damage = 11;
			Defense = 8;
			LifeMax = 220;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 300f;
			Scale = 1f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.MOTHER_SLIME:
			NpcSlots = 2f;
			TypeName = "Mother Slime";
			Width = 36;
			Height = 24;
			AIStyle = 1;
			Damage = 20;
			Defense = 7;
			LifeMax = 90;
			SoundHit = 1;
			SoundKilled = 1;
			Alpha = 120;
			Colour = new Color(0, 0, 0, 50);
			Value = 75f;
			Scale = 1.25f;
			KnockBackResist = 0.6f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.MERCHANT:
			IsTownNPC = true;
			IsFriendly = true;
			TypeName = "Merchant";
			Width = 18;
			Height = 40;
			AIStyle = 7;
			Damage = 10;
			Defense = 15;
			LifeMax = 250;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			break;
		case EntityID.NPCID.NURSE:
			IsTownNPC = true;
			IsFriendly = true;
			TypeName = "Nurse";
			Width = 18;
			Height = 40;
			AIStyle = 7;
			Damage = 10;
			Defense = 15;
			LifeMax = 250;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			break;
		case EntityID.NPCID.ARMS_DEALER:
			IsTownNPC = true;
			IsFriendly = true;
			TypeName = "Arms Dealer";
			Width = 18;
			Height = 40;
			AIStyle = 7;
			Damage = 10;
			Defense = 15;
			LifeMax = 250;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			break;
		case EntityID.NPCID.DRYAD:
			IsTownNPC = true;
			IsFriendly = true;
			TypeName = "Dryad";
			Width = 18;
			Height = 40;
			AIStyle = 7;
			Damage = 10;
			Defense = 15;
			LifeMax = 250;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			break;
		case EntityID.NPCID.SKELETON:
			TypeName = "Skeleton";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 20;
			Defense = 8;
			LifeMax = 60;
			SoundHit = 2;
			SoundKilled = 2;
			KnockBackResist = 0.5f;
			Value = 100f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.GUIDE:
			IsTownNPC = true;
			IsFriendly = true;
			TypeName = "Guide";
			Width = 18;
			Height = 40;
			AIStyle = 7;
			Damage = 10;
			Defense = 15;
			LifeMax = 250;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			break;
		case EntityID.NPCID.METEOR_HEAD:
			TypeName = "Meteor Head";
			Width = 22;
			Height = 22;
			AIStyle = 5;
			Damage = 40;
			Defense = 6;
			LifeMax = 26;
			SoundHit = 3;
			SoundKilled = 3;
			HasNoGravity = true;
			HasNoTileCollide = true;
			Value = 80f;
			KnockBackResist = 0.4f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.FIRE_IMP:
			NpcSlots = 3f;
			TypeName = "Fire Imp";
			Width = 18;
			Height = 40;
			AIStyle = 8;
			Damage = 30;
			Defense = 16;
			LifeMax = 70;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			IsLavaImmune = true;
			Value = 350f;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.BURNING_SPHERE:
			TypeName = "Burning Sphere";
			Width = 16;
			Height = 16;
			AIStyle = 9;
			Damage = 30;
			Defense = 0;
			LifeMax = 1;
			SoundHit = 3;
			SoundKilled = 3;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			Alpha = 100;
			break;
		case EntityID.NPCID.GOBLIN_PEON:
			TypeName = "Goblin Peon";
			Scale = 0.9f;
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 12;
			Defense = 4;
			LifeMax = 60;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.8f;
			Value = 100f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.GOBLIN_THIEF:
			TypeName = "Goblin Thief";
			Scale = 0.95f;
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 20;
			Defense = 6;
			LifeMax = 80;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.7f;
			Value = 200f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.GOBLIN_WARRIOR:
			TypeName = "Goblin Warrior";
			Scale = 1.1f;
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 25;
			Defense = 8;
			LifeMax = 110;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			Value = 150f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.GOBLIN_SORCERER:
			TypeName = "Goblin Sorcerer";
			Width = 18;
			Height = 40;
			AIStyle = 8;
			Damage = 20;
			Defense = 2;
			LifeMax = 40;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.6f;
			Value = 200f;
			break;
		case EntityID.NPCID.CHAOS_BALL:
			TypeName = "Chaos Ball";
			Width = 16;
			Height = 16;
			AIStyle = 9;
			Damage = 20;
			Defense = 0;
			LifeMax = 1;
			SoundHit = 3;
			SoundKilled = 3;
			HasNoGravity = true;
			HasNoTileCollide = true;
			Alpha = 100;
			KnockBackResist = 0f;
			break;
		case EntityID.NPCID.BONES:
			TypeName = "Angry Bones";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 26;
			Defense = 8;
			LifeMax = 80;
			SoundHit = 2;
			SoundKilled = 2;
			KnockBackResist = 0.8f;
			Value = 130f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.DARK_CASTER:
			TypeName = "Dark Caster";
			Width = 18;
			Height = 40;
			AIStyle = 8;
			Damage = 20;
			Defense = 2;
			LifeMax = 50;
			SoundHit = 2;
			SoundKilled = 2;
			KnockBackResist = 0.6f;
			Value = 140f;
			NpcSlots = 2f;
			BuffImmune[20] = true;
			break;
		case EntityID.NPCID.WATER_SPHERE:
			TypeName = "Water Sphere";
			Width = 16;
			Height = 16;
			AIStyle = 9;
			Damage = 20;
			Defense = 0;
			LifeMax = 1;
			SoundHit = 3;
			SoundKilled = 3;
			HasNoGravity = true;
			HasNoTileCollide = true;
			Alpha = 100;
			KnockBackResist = 0f;
			break;
		case EntityID.NPCID.CURSED_SKULL:
			TypeName = "Cursed Skull";
			Width = 26;
			Height = 28;
			AIStyle = 10;
			Damage = 35;
			Defense = 6;
			LifeMax = 40;
			SoundHit = 2;
			SoundKilled = 2;
			HasNoGravity = true;
			HasNoTileCollide = true;
			Value = 150f;
			KnockBackResist = 0.2f;
			NpcSlots = 0.75f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.DRAGON_SKULL:
			TypeName = "Dragon Skull";
			Width = 56;
			Height = 28;
			AIStyle = 10;
			Damage = 45;
			Defense = 8;
			LifeMax = 50;
			SoundHit = 2;
			SoundKilled = 2;
			HasNoGravity = true;
			HasNoTileCollide = true;
			Value = 150f;
			KnockBackResist = 0.2f;
			NpcSlots = 0.75f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.SKELETRON_HEAD:
			TypeName = "Skeletron Head";
			Width = 80;
			Height = 102;
			AIStyle = 11;
			Damage = 32;
			Defense = 10;
			LifeMax = 4400;
			SoundHit = 2;
			SoundKilled = 2;
			HasNoGravity = true;
			HasNoTileCollide = true;
			Value = 50000f;
			KnockBackResist = 0f;
			IsBoss = true;
			NpcSlots = 6f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.SKELETRON_HAND:
			TypeName = "Skeletron Hand";
			Width = 52;
			Height = 52;
			AIStyle = 12;
			Damage = 20;
			Defense = 14;
			LifeMax = 600;
			SoundHit = 2;
			SoundKilled = 2;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.OLD_MAN:
			IsTownNPC = true;
			IsFriendly = true;
			TypeName = "Old Man";
			Width = 18;
			Height = 40;
			AIStyle = 7;
			Damage = 10;
			Defense = 15;
			LifeMax = 250;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			break;
		case EntityID.NPCID.DEMOLITIONIST:
			IsTownNPC = true;
			IsFriendly = true;
			TypeName = "Demolitionist";
			Width = 18;
			Height = 40;
			AIStyle = 7;
			Damage = 10;
			Defense = 15;
			LifeMax = 250;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			break;
		case EntityID.NPCID.BONE_SERPENT_HEAD:
			NpcSlots = 6f;
			TypeName = "Bone Serpent Head";
			Width = 22;
			Height = 22;
			AIStyle = 6;
			NetAlways = true;
			Damage = 30;
			Defense = 10;
			LifeMax = 250;
			SoundHit = 2;
			SoundKilled = 5;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 1200f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.BONE_SERPENT_BODY:
			TypeName = "Bone Serpent Body";
			Width = 22;
			Height = 22;
			AIStyle = 6;
			NetAlways = true;
			Damage = 15;
			Defense = 12;
			LifeMax = 250;
			SoundHit = 2;
			SoundKilled = 5;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 1200f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.BONE_SERPENT_TAIL:
			TypeName = "Bone Serpent Tail";
			Width = 22;
			Height = 22;
			AIStyle = 6;
			NetAlways = true;
			Damage = 10;
			Defense = 18;
			LifeMax = 250;
			SoundHit = 2;
			SoundKilled = 5;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 1200f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.HORNET:
			TypeName = "Hornet";
			Width = 34;
			Height = 32;
			AIStyle = 5;
			Damage = 34;
			Defense = 12;
			LifeMax = 50;
			SoundHit = 1;
			KnockBackResist = 0.5f;
			SoundKilled = 1;
			Value = 200f;
			HasNoGravity = true;
			BuffImmune[20] = true;
			break;
		case EntityID.NPCID.DRAGON_HORNET:
			TypeName = "Dragon Hornet";
			Width = 34;
			Height = 32;
			AIStyle = 5;
			Damage = 39;
			Defense = 17;
			LifeMax = 65;
			SoundHit = 1;
			KnockBackResist = 0.5f;
			SoundKilled = 1;
			Value = 200f;
			HasNoGravity = true;
			BuffImmune[20] = true;
			break;
		case EntityID.NPCID.MAN_EATER:
			HasNoGravity = true;
			HasNoTileCollide = true;
			TypeName = "Man Eater";
			Width = 30;
			Height = 30;
			AIStyle = 13;
			Damage = 42;
			Defense = 14;
			LifeMax = 130;
			SoundHit = 1;
			KnockBackResist = 0f;
			SoundKilled = 1;
			Value = 350f;
			BuffImmune[20] = true;
			break;
		case EntityID.NPCID.UNDEAD_MINER:
			TypeName = "Undead Miner";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 22;
			Defense = 9;
			LifeMax = 70;
			SoundHit = 2;
			SoundKilled = 2;
			KnockBackResist = 0.5f;
			Value = 250f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.VAMPIRE_MINER:
			TypeName = "Vampire Miner";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 30;
			Defense = 9;
			LifeMax = 90;
			SoundHit = 2;
			SoundKilled = 2;
			KnockBackResist = 0.7f;
			Value = 250f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.TIM:
			TypeName = "Tim";
			Width = 18;
			Height = 40;
			AIStyle = 8;
			Damage = 20;
			Defense = 4;
			LifeMax = 200;
			SoundHit = 2;
			SoundKilled = 2;
			KnockBackResist = 0.6f;
			Value = 5000f;
			BuffImmune[20] = true;
			break;
		case EntityID.NPCID.BUNNY:
			TypeName = "Bunny";
			Width = 18;
			Height = 20;
			AIStyle = 7;
			Damage = 0;
			Defense = 0;
			LifeMax = 5;
			SoundHit = 1;
			SoundKilled = 1;
			break;
		case EntityID.NPCID.CORRUPT_BUNNY:
			TypeName = "Corrupt Bunny";
			Width = 18;
			Height = 20;
			AIStyle = 3;
			Damage = 20;
			Defense = 4;
			LifeMax = 70;
			SoundHit = 1;
			SoundKilled = 1;
			Value = 500f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.HARPY:
			TypeName = "Harpy";
			Width = 24;
			Height = 34;
			AIStyle = 14;
			Damage = 25;
			Defense = 8;
			LifeMax = 100;
			SoundHit = 1;
			KnockBackResist = 0.6f;
			SoundKilled = 1;
			Value = 300f;
			break;
		case EntityID.NPCID.CAVE_BAT:
			NpcSlots = 0.5f;
			TypeName = "Cave Bat";
			Width = 22;
			Height = 18;
			AIStyle = 14;
			Damage = 13;
			Defense = 2;
			LifeMax = 16;
			SoundHit = 1;
			KnockBackResist = 0.8f;
			SoundKilled = 4;
			Value = 90f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.KING_SLIME:
			IsBoss = true;
			TypeName = "King Slime";
			Width = 98;
			Height = 92;
			AIStyle = 15;
			Damage = 40;
			Defense = 10;
			LifeMax = 2000;
			KnockBackResist = 0f;
			SoundHit = 1;
			SoundKilled = 1;
			Alpha = 30;
			Value = 10000f;
			Scale = 1.25f;
			BuffImmune[20] = true;
			break;
		case EntityID.NPCID.JUNGLE_BAT:
			NpcSlots = 0.5f;
			TypeName = "Jungle Bat";
			Width = 22;
			Height = 18;
			AIStyle = 14;
			Damage = 20;
			Defense = 4;
			LifeMax = 34;
			SoundHit = 1;
			KnockBackResist = 0.8f;
			SoundKilled = 4;
			Value = 80f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.DOCTOR_BONES:
			TypeName = "Doctor Bones";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 20;
			Defense = 10;
			LifeMax = 500;
			SoundHit = 1;
			SoundKilled = 2;
			KnockBackResist = 0.5f;
			Value = 1000f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.THE_GROOM:
			TypeName = "The Groom";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 14;
			Defense = 8;
			LifeMax = 200;
			SoundHit = 1;
			SoundKilled = 2;
			KnockBackResist = 0.5f;
			Value = 1000f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.CLOTHIER:
			IsTownNPC = true;
			IsFriendly = true;
			TypeName = "Clothier";
			Width = 18;
			Height = 40;
			AIStyle = 7;
			Damage = 10;
			Defense = 15;
			LifeMax = 250;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			break;
		case EntityID.NPCID.GOLDFISH:
			HasNoGravity = true;
			TypeName = "Goldfish";
			Width = 20;
			Height = 18;
			AIStyle = 16;
			Damage = 0;
			Defense = 0;
			LifeMax = 5;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			break;
		case EntityID.NPCID.SNATCHER:
			HasNoTileCollide = true;
			HasNoGravity = true;
			TypeName = "Snatcher";
			Width = 30;
			Height = 30;
			AIStyle = 13;
			Damage = 25;
			Defense = 10;
			LifeMax = 60;
			SoundHit = 1;
			KnockBackResist = 0f;
			SoundKilled = 1;
			Value = 90f;
			BuffImmune[20] = true;
			break;
		case EntityID.NPCID.DRAGON_SNATCHER:
			HasNoTileCollide = true;
			HasNoGravity = true;
			TypeName = "Dragon Snatcher";
			Width = 30;
			Height = 30;
			AIStyle = 13;
			Damage = 30;
			Defense = 15;
			LifeMax = 75;
			SoundHit = 1;
			KnockBackResist = 0f;
			SoundKilled = 1;
			Value = 90f;
			BuffImmune[20] = true;
			break;
		case EntityID.NPCID.CORRUPT_GOLDFISH:
			HasNoGravity = true;
			TypeName = "Corrupt Goldfish";
			Width = 18;
			Height = 20;
			AIStyle = 16;
			Damage = 30;
			Defense = 6;
			LifeMax = 100;
			SoundHit = 1;
			SoundKilled = 1;
			Value = 500f;
			break;
		case EntityID.NPCID.PIRANHA:
			NpcSlots = 0.5f;
			HasNoGravity = true;
			TypeName = "Piranha";
			Width = 18;
			Height = 20;
			AIStyle = 16;
			Damage = 25;
			Defense = 2;
			LifeMax = 30;
			SoundHit = 1;
			SoundKilled = 1;
			Value = 50f;
			break;
		case EntityID.NPCID.LAVA_SLIME:
			TypeName = "Lava Slime";
			Width = 24;
			Height = 18;
			AIStyle = 1;
			Damage = 15;
			Defense = 10;
			LifeMax = 50;
			SoundHit = 1;
			SoundKilled = 1;
			Scale = 1.1f;
			Alpha = 50;
			IsLavaImmune = true;
			Value = 120f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.HELLBAT:
			NpcSlots = 0.5f;
			TypeName = "Hellbat";
			Width = 22;
			Height = 18;
			AIStyle = 14;
			Damage = 35;
			Defense = 8;
			LifeMax = 46;
			SoundHit = 1;
			KnockBackResist = 0.8f;
			SoundKilled = 4;
			Value = 120f;
			Scale = 1.1f;
			IsLavaImmune = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.VULTURE:
			TypeName = "Vulture";
			Width = 36;
			Height = 36;
			AIStyle = 17;
			Damage = 15;
			Defense = 4;
			LifeMax = 40;
			SoundHit = 1;
			KnockBackResist = 0.8f;
			SoundKilled = 1;
			Value = 60f;
			break;
		case EntityID.NPCID.DEMON:
			NpcSlots = 2f;
			TypeName = "Demon";
			Width = 28;
			Height = 48;
			AIStyle = 14;
			Damage = 32;
			Defense = 8;
			LifeMax = 120;
			SoundHit = 1;
			KnockBackResist = 0.8f;
			SoundKilled = 1;
			Value = 300f;
			IsLavaImmune = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.ARCH_DEMON:
			NpcSlots = 2f;
			TypeName = "Arch Demon";
			Width = 28;
			Height = 48;
			AIStyle = 14;
			Damage = 42;
			Defense = 8;
			LifeMax = 140;
			SoundHit = 1;
			KnockBackResist = 0.8f;
			SoundKilled = 1;
			Value = 300f;
			IsLavaImmune = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.BLUE_JELLYFISH:
			HasNoGravity = true;
			TypeName = "Blue Jellyfish";
			Width = 26;
			Height = 26;
			AIStyle = 18;
			Damage = 20;
			Defense = 2;
			LifeMax = 30;
			SoundHit = 1;
			SoundKilled = 1;
			Value = 100f;
			Alpha = 20;
			break;
		case EntityID.NPCID.PINK_JELLYFISH:
			HasNoGravity = true;
			TypeName = "Pink Jellyfish";
			Width = 26;
			Height = 26;
			AIStyle = 18;
			Damage = 30;
			Defense = 6;
			LifeMax = 70;
			SoundHit = 1;
			SoundKilled = 1;
			Value = 100f;
			Alpha = 20;
			break;
		case EntityID.NPCID.SHARK:
			HasNoGravity = true;
			TypeName = "Shark";
			Width = 100;
			Height = 24;
			AIStyle = 16;
			Damage = 40;
			Defense = 2;
			LifeMax = 300;
			SoundHit = 1;
			SoundKilled = 1;
			Value = 400f;
			KnockBackResist = 0.7f;
			break;
		case EntityID.NPCID.ORKA:
			HasNoGravity = true;
			TypeName = "Orka";
			Width = 100;
			Height = 24;
			AIStyle = 16;
			Damage = 30;
			Defense = 4;
			LifeMax = 350;
			SoundHit = 1;
			SoundKilled = 1;
			Value = 400f;
			KnockBackResist = 0.6f;
			break;
		case EntityID.NPCID.VOODOO_DEMON:
			NpcSlots = 2f;
			TypeName = "Voodoo Demon";
			Width = 28;
			Height = 48;
			AIStyle = 14;
			Damage = 32;
			Defense = 8;
			LifeMax = 140;
			SoundHit = 1;
			KnockBackResist = 0.8f;
			SoundKilled = 1;
			Value = 1000f;
			IsLavaImmune = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.CRAB:
			TypeName = "Crab";
			Width = 28;
			Height = 20;
			AIStyle = 3;
			Damage = 20;
			Defense = 10;
			LifeMax = 40;
			SoundHit = 1;
			SoundKilled = 1;
			Value = 60f;
			break;
		case EntityID.NPCID.DUNGEON_GUARDIAN:
			TypeName = "Dungeon Guardian";
			Width = 80;
			Height = 102;
			AIStyle = 11;
			Damage = 9000;
			Defense = 9000;
			LifeMax = 9999;
			SoundHit = 2;
			SoundKilled = 2;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.ANTLION:
			TypeName = "Antlion";
			Width = 24;
			Height = 24;
			AIStyle = 19;
			Damage = 10;
			Defense = 6;
			LifeMax = 45;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0f;
			Value = 60f;
			IsBehindTiles = true;
			break;
		case EntityID.NPCID.ALBINO_ANTLION:
			TypeName = "Albino Antlion";
			Width = 24;
			Height = 24;
			AIStyle = 19;
			Damage = 12;
			Defense = 8;
			LifeMax = 60;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0f;
			Value = 60f;
			IsBehindTiles = true;
			break;
		case EntityID.NPCID.SPIKE_BALL:
			NpcSlots = 0.3f;
			TypeName = "Spike Ball";
			Width = 34;
			Height = 34;
			AIStyle = 20;
			Damage = 32;
			Defense = 100;
			LifeMax = 100;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0f;
			HasNoGravity = true;
			HasNoTileCollide = true;
			DontTakeDamage = true;
			Scale = 1.5f;
			break;
		case EntityID.NPCID.DUNGEON_SLIME:
			NpcSlots = 2f;
			TypeName = "Dungeon Slime";
			Width = 36;
			Height = 24;
			AIStyle = 1;
			Damage = 30;
			Defense = 7;
			LifeMax = 150;
			SoundHit = 1;
			SoundKilled = 1;
			Alpha = 60;
			Value = 150f;
			Scale = 1.25f;
			KnockBackResist = 0.6f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.BLAZING_WHEEL:
			NpcSlots = 0.3f;
			TypeName = "Blazing Wheel";
			Width = 34;
			Height = 34;
			AIStyle = 21;
			Damage = 24;
			Defense = 100;
			LifeMax = 100;
			Alpha = 100;
			IsBehindTiles = true;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0f;
			HasNoGravity = true;
			DontTakeDamage = true;
			Scale = 1.2f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.GOBLIN_SCOUT:
			TypeName = "Goblin Scout";
			Scale = 0.95f;
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 20;
			Defense = 6;
			LifeMax = 80;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.7f;
			Value = 200f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.BIRD:
			TypeName = "Bird";
			Width = 14;
			Height = 14;
			AIStyle = 24;
			Damage = 0;
			Defense = 0;
			LifeMax = 5;
			SoundHit = 1;
			KnockBackResist = 0.8f;
			SoundKilled = 1;
			break;
		case EntityID.NPCID.PIXIE:
			HasNoGravity = true;
			TypeName = "Pixie";
			Width = 20;
			Height = 20;
			AIStyle = 22;
			Damage = 55;
			Defense = 20;
			LifeMax = 150;
			SoundHit = 5;
			KnockBackResist = 0.6f;
			SoundKilled = 7;
			Value = 350f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.ARMORED_SKELETON:
			TypeName = "Armored Skeleton";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 60;
			Defense = 36;
			LifeMax = 340;
			SoundHit = 2;
			SoundKilled = 2;
			KnockBackResist = 0.4f;
			Value = 400f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.MUMMY:
			TypeName = "Mummy";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 50;
			Defense = 16;
			LifeMax = 130;
			SoundHit = 1;
			SoundKilled = 6;
			KnockBackResist = 0.6f;
			Value = 600f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.DARK_MUMMY:
			TypeName = "Dark Mummy";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 60;
			Defense = 18;
			LifeMax = 180;
			SoundHit = 1;
			SoundKilled = 6;
			KnockBackResist = 0.5f;
			Value = 700f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.SHADOW_MUMMY:
			TypeName = "Shadow Mummy";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 60;
			Defense = 25;
			LifeMax = 190;
			SoundHit = 1;
			SoundKilled = 6;
			KnockBackResist = 0.5f;
			Value = 700f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.LIGHT_MUMMY:
			TypeName = "Light Mummy";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 55;
			Defense = 18;
			LifeMax = 200;
			SoundHit = 1;
			SoundKilled = 6;
			KnockBackResist = 0.55f;
			Value = 700f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.SPECTRAL_MUMMY:
			TypeName = "Spectral Mummy";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 65;
			Defense = 10;
			LifeMax = 270;
			SoundHit = 1;
			SoundKilled = 6;
			KnockBackResist = 0.55f;
			Value = 700f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.CORRUPT_SLIME:
			TypeName = "Corrupt Slime";
			Width = 40;
			Height = 30;
			AIStyle = 1;
			Damage = 55;
			Defense = 20;
			LifeMax = 170;
			SoundHit = 1;
			SoundKilled = 1;
			Alpha = 55;
			Value = 400f;
			Scale = 1.1f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.SHADOW_SLIME:
			TypeName = "Shadow Slime";
			Width = 40;
			Height = 30;
			AIStyle = 1;
			Damage = 60;
			Defense = 25;
			LifeMax = 180;
			SoundHit = 1;
			SoundKilled = 1;
			Alpha = 55;
			Value = 400f;
			Scale = 1.1f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.WRAITH:
			HasNoGravity = true;
			HasNoTileCollide = true;
			TypeName = "Wraith";
			Width = 24;
			Height = 44;
			AIStyle = 22;
			Damage = 75;
			Defense = 18;
			LifeMax = 200;
			SoundHit = 1;
			SoundKilled = 6;
			Alpha = 100;
			Value = 500f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			KnockBackResist = 0.7f;
			break;
		case EntityID.NPCID.CURSED_HAMMER:
			TypeName = "Cursed Hammer";
			Width = 40;
			Height = 40;
			AIStyle = 23;
			Damage = 80;
			Defense = 18;
			LifeMax = 200;
			SoundHit = 4;
			SoundKilled = 6;
			Value = 1000f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			KnockBackResist = 0.4f;
			break;
		case EntityID.NPCID.SHADOW_HAMMER:
			TypeName = "Shadow Hammer";
			Width = 40;
			Height = 40;
			AIStyle = 23;
			Damage = 95;
			Defense = 18;
			LifeMax = 180;
			SoundHit = 4;
			SoundKilled = 6;
			Value = 1000f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			KnockBackResist = 0.4f;
			break;
		case EntityID.NPCID.ENCHANTED_SWORD:
			TypeName = "Enchanted Sword";
			Width = 40;
			Height = 40;
			AIStyle = 23;
			Damage = 80;
			Defense = 18;
			LifeMax = 200;
			SoundHit = 4;
			SoundKilled = 6;
			Value = 1000f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			KnockBackResist = 0.4f;
			break;
		case EntityID.NPCID.MIMIC:
			TypeName = "Mimic";
			Width = 24;
			Height = 24;
			AIStyle = 25;
			Damage = 80;
			Defense = 30;
			LifeMax = 500;
			SoundHit = 4;
			SoundKilled = 6;
			Value = 100000f;
			KnockBackResist = 0.3f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.UNICORN:
			TypeName = "Unicorn";
			Width = 46;
			Height = 42;
			AIStyle = 26;
			Damage = 65;
			Defense = 30;
			LifeMax = 400;
			SoundHit = 10;
			SoundKilled = 1;
			KnockBackResist = 0.3f;
			Value = 1000f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.WYVERN_HEAD:
			HasNoTileCollide = true;
			NpcSlots = 5f;
			TypeName = "Wyvern Head";
			Width = 32;
			Height = 32;
			AIStyle = 6;
			NetAlways = true;
			Damage = 80;
			Defense = 10;
			LifeMax = 4000;
			SoundHit = 7;
			SoundKilled = 8;
			HasNoGravity = true;
			KnockBackResist = 0f;
			Value = 10000f;
			Scale = 1f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.ARCH_WYVERN_HEAD:
			HasNoTileCollide = true;
			NpcSlots = 5f;
			TypeName = "Arch Wyvern Head";
			Width = 32;
			Height = 32;
			AIStyle = 6;
			NetAlways = true;
			Damage = 100;
			Defense = 15;
			LifeMax = 4700;
			SoundHit = 7;
			SoundKilled = 8;
			HasNoGravity = true;
			KnockBackResist = 0f;
			Value = 10000f;
			Scale = 1f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.WYVERN_LEGS:
			HasNoTileCollide = true;
			TypeName = "Wyvern Legs";
			Width = 32;
			Height = 32;
			AIStyle = 6;
			NetAlways = true;
			Damage = 40;
			Defense = 20;
			LifeMax = 4000;
			SoundHit = 7;
			SoundKilled = 8;
			HasNoGravity = true;
			KnockBackResist = 0f;
			Value = 10000f;
			Scale = 1f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.ARCH_WYVERN_LEGS:
			HasNoTileCollide = true;
			TypeName = "Arch Wyvern Legs";
			Width = 32;
			Height = 32;
			AIStyle = 6;
			NetAlways = true;
			Damage = 50;
			Defense = 25;
			LifeMax = 4500;
			SoundHit = 7;
			SoundKilled = 8;
			HasNoGravity = true;
			KnockBackResist = 0f;
			Value = 10000f;
			Scale = 1f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.WYVERN_BODY1:
			HasNoTileCollide = true;
			TypeName = "Wyvern Body";
			Width = 32;
			Height = 32;
			AIStyle = 6;
			NetAlways = true;
			Damage = 40;
			Defense = 20;
			LifeMax = 4000;
			SoundHit = 7;
			SoundKilled = 8;
			HasNoGravity = true;
			KnockBackResist = 0f;
			Value = 2000f;
			Scale = 1f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.ARCH_WYVERN_BODY1:
			HasNoTileCollide = true;
			TypeName = "Arch Wyvern Body";
			Width = 32;
			Height = 32;
			AIStyle = 6;
			NetAlways = true;
			Damage = 45;
			Defense = 20;
			LifeMax = 4300;
			SoundHit = 7;
			SoundKilled = 8;
			HasNoGravity = true;
			KnockBackResist = 0f;
			Value = 2000f;
			Scale = 1f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.WYVERN_BODY2:
			HasNoTileCollide = true;
			TypeName = "Wyvern Body 2";
			Width = 32;
			Height = 32;
			AIStyle = 6;
			NetAlways = true;
			Damage = 40;
			Defense = 20;
			LifeMax = 4000;
			SoundHit = 7;
			SoundKilled = 8;
			HasNoGravity = true;
			KnockBackResist = 0f;
			Value = 10000f;
			Scale = 1f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.ARCH_WYVERN_BODY2:
			HasNoTileCollide = true;
			TypeName = "Arch Wyvern Body 2";
			Width = 32;
			Height = 32;
			AIStyle = 6;
			NetAlways = true;
			Damage = 40;
			Defense = 20;
			LifeMax = 4000;
			SoundHit = 7;
			SoundKilled = 8;
			HasNoGravity = true;
			KnockBackResist = 0f;
			Value = 10000f;
			Scale = 1f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.WYVERN_BODY3:
			HasNoTileCollide = true;
			TypeName = "Wyvern Body 3";
			Width = 32;
			Height = 32;
			AIStyle = 6;
			NetAlways = true;
			Damage = 40;
			Defense = 20;
			LifeMax = 4000;
			SoundHit = 7;
			SoundKilled = 8;
			HasNoGravity = true;
			KnockBackResist = 0f;
			Value = 10000f;
			Scale = 1f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.ARCH_WYVERN_BODY3:
			HasNoTileCollide = true;
			TypeName = "Arch Wyvern Body 3";
			Width = 32;
			Height = 32;
			AIStyle = 6;
			NetAlways = true;
			Damage = 45;
			Defense = 20;
			LifeMax = 4300;
			SoundHit = 7;
			SoundKilled = 8;
			HasNoGravity = true;
			KnockBackResist = 0f;
			Value = 10000f;
			Scale = 1f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.WYVERN_TAIL:
			HasNoTileCollide = true;
			TypeName = "Wyvern Tail";
			Width = 32;
			Height = 32;
			AIStyle = 6;
			NetAlways = true;
			Damage = 40;
			Defense = 20;
			LifeMax = 4000;
			SoundHit = 7;
			SoundKilled = 8;
			HasNoGravity = true;
			KnockBackResist = 0f;
			Value = 10000f;
			Scale = 1f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.ARCH_WYVERN_TAIL:
			HasNoTileCollide = true;
			TypeName = "Arch Wyvern Tail";
			Width = 32;
			Height = 32;
			AIStyle = 6;
			NetAlways = true;
			Damage = 55;
			Defense = 15;
			LifeMax = 4000;
			SoundHit = 7;
			SoundKilled = 8;
			HasNoGravity = true;
			KnockBackResist = 0f;
			Value = 10000f;
			Scale = 1f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.GIANT_BAT:
			NpcSlots = 0.5f;
			TypeName = "Giant Bat";
			Width = 26;
			Height = 20;
			AIStyle = 14;
			Damage = 70;
			Defense = 20;
			LifeMax = 160;
			SoundHit = 1;
			KnockBackResist = 0.75f;
			SoundKilled = 4;
			Value = 400f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.CORRUPTOR:
			NpcSlots = 1f;
			TypeName = "Corruptor";
			Width = 44;
			Height = 44;
			AIStyle = 5;
			Damage = 60;
			Defense = 32;
			LifeMax = 230;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			KnockBackResist = 0.55f;
			Value = 500f;
			break;
		case EntityID.NPCID.DIGGER_HEAD:
			TypeName = "Digger Head";
			Width = 22;
			Height = 22;
			AIStyle = 6;
			NetAlways = true;
			Damage = 45;
			Defense = 10;
			LifeMax = 200;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Scale = 0.9f;
			Value = 300f;
			break;
		case EntityID.NPCID.DIGGER_BODY:
			TypeName = "Digger Body";
			Width = 22;
			Height = 22;
			AIStyle = 6;
			NetAlways = true;
			Damage = 28;
			Defense = 20;
			LifeMax = 200;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Scale = 0.9f;
			Value = 300f;
			break;
		case EntityID.NPCID.DIGGER_TAIL:
			TypeName = "Digger Tail";
			Width = 22;
			Height = 22;
			AIStyle = 6;
			NetAlways = true;
			Damage = 26;
			Defense = 30;
			LifeMax = 200;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Scale = 0.9f;
			Value = 300f;
			break;
		case EntityID.NPCID.SEEKER_HEAD:
			NpcSlots = 3.5f;
			TypeName = "Seeker Head";
			Width = 22;
			Height = 22;
			AIStyle = 6;
			NetAlways = true;
			Damage = 70;
			Defense = 36;
			LifeMax = 500;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 700f;
			break;
		case EntityID.NPCID.SEEKER_BODY:
			TypeName = "Seeker Body";
			Width = 22;
			Height = 22;
			AIStyle = 6;
			NetAlways = true;
			Damage = 55;
			Defense = 40;
			LifeMax = 500;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 700f;
			break;
		case EntityID.NPCID.SEEKER_TAIL:
			TypeName = "Seeker Tail";
			Width = 22;
			Height = 22;
			AIStyle = 6;
			NetAlways = true;
			Damage = 40;
			Defense = 44;
			LifeMax = 500;
			SoundHit = 1;
			SoundKilled = 1;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 700f;
			break;
		case EntityID.NPCID.CLINGER:
			HasNoGravity = true;
			HasNoTileCollide = true;
			IsBehindTiles = true;
			TypeName = "Clinger";
			Width = 30;
			Height = 30;
			AIStyle = 13;
			Damage = 70;
			Defense = 30;
			LifeMax = 320;
			SoundHit = 1;
			KnockBackResist = 0.2f;
			SoundKilled = 1;
			Value = 600f;
			break;
		case EntityID.NPCID.ANGLER_FISH:
			NpcSlots = 0.5f;
			HasNoGravity = true;
			TypeName = "Angler Fish";
			Width = 18;
			Height = 20;
			AIStyle = 16;
			Damage = 80;
			Defense = 22;
			LifeMax = 90;
			SoundHit = 1;
			SoundKilled = 1;
			Value = 500f;
			break;
		case EntityID.NPCID.GREEN_JELLYFISH:
			HasNoGravity = true;
			TypeName = "Green Jellyfish";
			Width = 26;
			Height = 26;
			AIStyle = 18;
			Damage = 80;
			Defense = 30;
			LifeMax = 120;
			SoundHit = 1;
			SoundKilled = 1;
			Value = 800f;
			Alpha = 20;
			break;
		case EntityID.NPCID.WEREWOLF:
			TypeName = "Werewolf";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 70;
			Defense = 40;
			LifeMax = 400;
			SoundHit = 6;
			SoundKilled = 1;
			KnockBackResist = 0.4f;
			Value = 1000f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.BOUND_GOBLIN:
			IsFriendly = true;
			TypeName = "Bound Goblin";
			Width = 18;
			Height = 34;
			AIStyle = 0;
			Damage = 10;
			Defense = 15;
			LifeMax = 250;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			Scale = 0.9f;
			break;
		case EntityID.NPCID.BOUND_WIZARD:
			IsFriendly = true;
			TypeName = "Bound Wizard";
			Width = 18;
			Height = 40;
			AIStyle = 0;
			Damage = 10;
			Defense = 15;
			LifeMax = 250;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			break;
		case EntityID.NPCID.GOBLIN_TINKERER:
			IsTownNPC = true;
			IsFriendly = true;
			TypeName = "Goblin Tinkerer";
			Width = 18;
			Height = 40;
			AIStyle = 7;
			Damage = 10;
			Defense = 15;
			LifeMax = 250;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			Scale = 0.9f;
			break;
		case EntityID.NPCID.WIZARD:
			IsTownNPC = true;
			IsFriendly = true;
			TypeName = "Wizard";
			Width = 18;
			Height = 40;
			AIStyle = 7;
			Damage = 10;
			Defense = 15;
			LifeMax = 250;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			break;
		case EntityID.NPCID.CLOWN:
			TypeName = "Clown";
			Width = 34;
			Height = 78;
			AIStyle = 3;
			Damage = 50;
			Defense = 20;
			LifeMax = 400;
			SoundHit = 1;
			SoundKilled = 2;
			KnockBackResist = 0.4f;
			Value = 8000f;
			break;
		case EntityID.NPCID.SKELETON_ARCHER:
			TypeName = "Skeleton Archer";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 55;
			Defense = 28;
			LifeMax = 260;
			SoundHit = 2;
			SoundKilled = 2;
			KnockBackResist = 0.55f;
			Value = 400f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.GOBLIN_ARCHER:
			TypeName = "Goblin Archer";
			Scale = 0.95f;
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 20;
			Defense = 6;
			LifeMax = 80;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.7f;
			Value = 200f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.VILE_SPIT:
			TypeName = "Vile Spit";
			Width = 16;
			Height = 16;
			AIStyle = 9;
			Damage = 65;
			Defense = 0;
			LifeMax = 1;
			SoundHit = 0;
			SoundKilled = 9;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			Scale = 0.9f;
			Alpha = 80;
			break;
		case EntityID.NPCID.WALL_OF_FLESH:
			NpcSlots = 10f;
			TypeName = "Wall of Flesh";
			Width = 100;
			Height = 100;
			AIStyle = 27;
			Damage = 50;
			Defense = 12;
			LifeMax = 8000;
			SoundHit = 8;
			SoundKilled = 10;
			HasNoGravity = true;
			HasNoTileCollide = true;
			IsBehindTiles = true;
			KnockBackResist = 0f;
			Scale = 1.2f;
			IsBoss = true;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			Value = 80000f;
			break;
		case EntityID.NPCID.WALL_OF_FLESH_EYE:
			TypeName = "Wall of Flesh Eye";
			Width = 100;
			Height = 100;
			AIStyle = 28;
			Damage = 50;
			Defense = 0;
			LifeMax = 8000;
			SoundHit = 8;
			SoundKilled = 10;
			HasNoGravity = true;
			HasNoTileCollide = true;
			IsBehindTiles = true;
			KnockBackResist = 0f;
			Scale = 1.2f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			Value = 80000f;
			break;
		case EntityID.NPCID.THE_HUNGRY:
			TypeName = "The Hungry";
			Width = 30;
			Height = 30;
			AIStyle = 29;
			Damage = 30;
			Defense = 10;
			LifeMax = 240;
			SoundHit = 9;
			SoundKilled = 11;
			HasNoGravity = true;
			IsBehindTiles = true;
			HasNoTileCollide = true;
			KnockBackResist = 1.1f;
			break;
		case EntityID.NPCID.THE_HUNGRY_II:
			TypeName = "The Hungry II";
			Width = 30;
			Height = 32;
			AIStyle = 2;
			Damage = 30;
			Defense = 6;
			LifeMax = 80;
			SoundHit = 9;
			KnockBackResist = 0.8f;
			SoundKilled = 12;
			break;
		case EntityID.NPCID.LEECH_HEAD:
			TypeName = "Leech Head";
			Width = 14;
			Height = 14;
			AIStyle = 6;
			NetAlways = true;
			Damage = 26;
			Defense = 2;
			LifeMax = 60;
			SoundHit = 9;
			SoundKilled = 12;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			break;
		case EntityID.NPCID.LEECH_BODY:
			TypeName = "Leech Body";
			Width = 14;
			Height = 14;
			AIStyle = 6;
			NetAlways = true;
			Damage = 22;
			Defense = 6;
			LifeMax = 60;
			SoundHit = 9;
			SoundKilled = 12;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			break;
		case EntityID.NPCID.LEECH_TAIL:
			TypeName = "Leech Tail";
			Width = 14;
			Height = 14;
			AIStyle = 6;
			NetAlways = true;
			Damage = 18;
			Defense = 10;
			LifeMax = 60;
			SoundHit = 9;
			SoundKilled = 12;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			break;
		case EntityID.NPCID.CHAOS_ELEMENTAL:
			TypeName = "Chaos Elemental";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 40;
			Defense = 30;
			LifeMax = 370;
			SoundHit = 1;
			SoundKilled = 6;
			KnockBackResist = 0.4f;
			Value = 600f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.SPECTRAL_ELEMENTAL:
			TypeName = "Spectral Elemental";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 50;
			Defense = 35;
			LifeMax = 400;
			SoundHit = 1;
			SoundKilled = 6;
			KnockBackResist = 0.4f;
			Value = 600f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.SLIMER:
			TypeName = "Slimer";
			Width = 40;
			Height = 30;
			AIStyle = 14;
			Damage = 45;
			Defense = 20;
			LifeMax = 60;
			SoundHit = 1;
			Alpha = 55;
			KnockBackResist = 0.8f;
			Scale = 1.1f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.GASTROPOD:
			HasNoGravity = true;
			TypeName = "Gastropod";
			Width = 20;
			Height = 20;
			AIStyle = 22;
			Damage = 60;
			Defense = 22;
			LifeMax = 220;
			SoundHit = 1;
			KnockBackResist = 0.8f;
			SoundKilled = 1;
			Value = 600f;
			BuffImmune[20] = true;
			break;
		case EntityID.NPCID.SPECTRAL_GASTROPOD:
			HasNoGravity = true;
			TypeName = "Spectral Gastropod";
			Width = 20;
			Height = 20;
			AIStyle = 22;
			Damage = 60;
			Defense = 22;
			LifeMax = 220;
			SoundHit = 1;
			KnockBackResist = 0.8f;
			SoundKilled = 1;
			Value = 600f;
			BuffImmune[20] = true;
			break;
		case EntityID.NPCID.BOUND_MECHANIC:
			IsFriendly = true;
			TypeName = "Bound Mechanic";
			Width = 18;
			Height = 34;
			AIStyle = 0;
			Damage = 10;
			Defense = 15;
			LifeMax = 250;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			Scale = 0.9f;
			break;
		case EntityID.NPCID.MECHANIC:
			IsTownNPC = true;
			IsFriendly = true;
			TypeName = "Mechanic";
			Width = 18;
			Height = 40;
			AIStyle = 7;
			Damage = 10;
			Defense = 15;
			LifeMax = 250;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			break;
		case EntityID.NPCID.RETINAZER:
			TypeName = "Retinazer";
			Width = 100;
			Height = 110;
			AIStyle = 30;
			Damage = 50;
			Defense = 10;
			LifeMax = 24000;
			SoundHit = 1;
			SoundKilled = 14;
			KnockBackResist = 0f;
			HasNoGravity = true;
			HasNoTileCollide = true;
			TimeLeft = 22500;
			IsBoss = true;
			Value = 120000f;
			NpcSlots = 5f;
			break;
		case EntityID.NPCID.SPAZMATISM:
			TypeName = "Spazmatism";
			Width = 100;
			Height = 110;
			AIStyle = 31;
			Damage = 50;
			Defense = 10;
			LifeMax = 24000;
			SoundHit = 1;
			SoundKilled = 14;
			KnockBackResist = 0f;
			HasNoGravity = true;
			HasNoTileCollide = true;
			TimeLeft = 22500;
			IsBoss = true;
			Value = 120000f;
			NpcSlots = 5f;
			break;
		case EntityID.NPCID.SKELETRON_PRIME:
			TypeName = "Skeletron Prime";
			Width = 80;
			Height = 102;
			AIStyle = 32;
			Damage = 50;
			Defense = 25;
			LifeMax = 30000;
			SoundHit = 4;
			SoundKilled = 14;
			HasNoGravity = true;
			HasNoTileCollide = true;
			Value = 120000f;
			KnockBackResist = 0f;
			IsBoss = true;
			NpcSlots = 6f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.PRIME_CANNON:
			TypeName = "Prime Cannon";
			Width = 52;
			Height = 52;
			AIStyle = 35;
			Damage = 30;
			Defense = 25;
			LifeMax = 7000;
			SoundHit = 4;
			SoundKilled = 14;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			NetAlways = true;
			break;
		case EntityID.NPCID.PRIME_SAW:
			TypeName = "Prime Saw";
			Width = 52;
			Height = 52;
			AIStyle = 33;
			Damage = 52;
			Defense = 40;
			LifeMax = 10000;
			SoundHit = 4;
			SoundKilled = 14;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			NetAlways = true;
			break;
		case EntityID.NPCID.PRIME_VICE:
			TypeName = "Prime Vice";
			Width = 52;
			Height = 52;
			AIStyle = 34;
			Damage = 45;
			Defense = 35;
			LifeMax = 10000;
			SoundHit = 4;
			SoundKilled = 14;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			NetAlways = true;
			break;
		case EntityID.NPCID.PRIME_LASER:
			TypeName = "Prime Laser";
			Width = 52;
			Height = 52;
			AIStyle = 36;
			Damage = 29;
			Defense = 20;
			LifeMax = 6000;
			SoundHit = 4;
			SoundKilled = 14;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			NetAlways = true;
			break;
		case EntityID.NPCID.BALD_ZOMBIE:
			TypeName = "Bald Zombie";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 14;
			Defense = 6;
			LifeMax = 45;
			SoundHit = 1;
			SoundKilled = 2;
			KnockBackResist = 0.5f;
			Value = 60f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.WANDERING_EYE:
			TypeName = "Wandering Eye";
			Width = 30;
			Height = 32;
			AIStyle = 2;
			Damage = 40;
			Defense = 20;
			LifeMax = 300;
			SoundHit = 1;
			KnockBackResist = 0.8f;
			SoundKilled = 1;
			Value = 500f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.THE_DESTROYER_HEAD:
			NpcSlots = 5f;
			TypeName = "The Destroyer";
			Width = 38;
			Height = 38;
			AIStyle = 37;
			Damage = 60;
			Defense = 0;
			LifeMax = 80000;
			SoundHit = 4;
			SoundKilled = 14;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Value = 120000f;
			Scale = 1.25f;
			IsBoss = true;
			NetAlways = true;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.THE_DESTROYER_BODY:
			NpcSlots = 5f;
			TypeName = "The Destroyer Body";
			Width = 38;
			Height = 38;
			AIStyle = 37;
			Damage = 40;
			Defense = 30;
			LifeMax = 80000;
			SoundHit = 4;
			SoundKilled = 14;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			NetAlways = true;
			Scale = 1.25f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.THE_DESTROYER_TAIL:
			NpcSlots = 5f;
			TypeName = "The Destroyer Tail";
			Width = 38;
			Height = 38;
			AIStyle = 37;
			Damage = 20;
			Defense = 35;
			LifeMax = 80000;
			SoundHit = 4;
			SoundKilled = 14;
			HasNoGravity = true;
			HasNoTileCollide = true;
			KnockBackResist = 0f;
			IsBehindTiles = true;
			Scale = 1.25f;
			NetAlways = true;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.ILLUMINANT_BAT:
			TypeName = "Illuminant Bat";
			Width = 26;
			Height = 20;
			AIStyle = 14;
			Damage = 75;
			Defense = 30;
			LifeMax = 200;
			SoundHit = 1;
			KnockBackResist = 0.75f;
			SoundKilled = 6;
			Value = 500f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.ILLUMINANT_SLIME:
			TypeName = "Illuminant Slime";
			Width = 24;
			Height = 18;
			AIStyle = 1;
			Damage = 70;
			Defense = 30;
			LifeMax = 180;
			SoundHit = 1;
			SoundKilled = 6;
			Alpha = 100;
			Value = 400f;
			BuffImmune[20] = true;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			KnockBackResist = 0.85f;
			Scale = 1.05f;
			BuffImmune[31] = false;
			break;
		case EntityID.NPCID.PROBE:
			NpcSlots = 1f;
			TypeName = "Probe";
			Width = 30;
			Height = 30;
			AIStyle = 5;
			Damage = 50;
			Defense = 20;
			LifeMax = 200;
			SoundHit = 4;
			SoundKilled = 14;
			HasNoGravity = true;
			KnockBackResist = 0.8f;
			HasNoTileCollide = true;
			break;
		case EntityID.NPCID.POSSESSED_ARMOR:
			TypeName = "Possessed Armor";
			Width = 18;
			Height = 40;
			AIStyle = 3;
			Damage = 55;
			Defense = 28;
			LifeMax = 260;
			SoundHit = 4;
			SoundKilled = 6;
			KnockBackResist = 0.4f;
			Value = 400f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			BuffImmune[24] = true;
			break;
		case EntityID.NPCID.TOXIC_SLUDGE:
			TypeName = "Toxic Sludge";
			Width = 34;
			Height = 28;
			AIStyle = 1;
			Damage = 50;
			Defense = 18;
			LifeMax = 150;
			SoundHit = 1;
			SoundKilled = 1;
			Alpha = 55;
			Value = 400f;
			Scale = 1.1f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			KnockBackResist = 0.8f;
			break;
		case EntityID.NPCID.SANTA_CLAUS:
			IsTownNPC = true;
			IsFriendly = true;
			TypeName = "Santa Claus";
			Width = 18;
			Height = 40;
			AIStyle = 7;
			Damage = 10;
			Defense = 15;
			LifeMax = 250;
			SoundHit = 1;
			SoundKilled = 1;
			KnockBackResist = 0.5f;
			break;
		case EntityID.NPCID.SNOWMAN_GANGSTA:
			TypeName = "Snowman Gangsta";
			Width = 26;
			Height = 40;
			AIStyle = 38;
			Damage = 50;
			Defense = 20;
			LifeMax = 200;
			SoundHit = 11;
			SoundKilled = 15;
			KnockBackResist = 0.6f;
			Value = 400f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.MISTER_STABBY:
			TypeName = "Mister Stabby";
			Width = 26;
			Height = 40;
			AIStyle = 38;
			Damage = 65;
			Defense = 26;
			LifeMax = 240;
			SoundHit = 11;
			SoundKilled = 15;
			KnockBackResist = 0.6f;
			Value = 400f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		case EntityID.NPCID.SNOW_BALLA:
			TypeName = "Snow Balla";
			Width = 26;
			Height = 40;
			AIStyle = 38;
			Damage = 55;
			Defense = 22;
			LifeMax = 220;
			SoundHit = 11;
			SoundKilled = 15;
			KnockBackResist = 0.6f;
			Value = 400f;
			BuffImmune[20] = true;
			BuffImmune[31] = false;
			BuffImmune[24] = true;
			BuffImmune[39] = true;
			break;
		}
		FrameY = 0;
		FrameHeight = (short)(SpriteSheet<_sheetSprites>.Source[1088 + Type].Height / NpcFrameCount[Type]);
		if (ScaleOverride > 0.0)
		{
			int num = (int)((float)(int)Width * Scale);
			int num2 = (int)((float)(int)Height * Scale);
			Position.X += num >> 1;
			Position.Y += num2;
			Scale = (float)ScaleOverride;
			Width = (ushort)((float)(int)Width * Scale);
			Height = (ushort)((float)(int)Height * Scale);
			if (Height == 16 || Height == 32)
			{
				Height++;
			}
			Position.X -= Width >> 1;
			Position.Y -= (int)Height;
		}
		else
		{
			Width = (ushort)((float)(int)Width * Scale);
			Height = (ushort)((float)(int)Height * Scale);
		}
		XYWH.X = (int)Position.X;
		XYWH.Y = (int)Position.Y;
		XYWH.Width = Width;
		XYWH.Height = Height;
		HealthBarLife = (Life = LifeMax);
		DefDamage = Damage;
		DefDefense = (short)Defense;
		DisplayName = Lang.NpcName(NetID);
	}

	private void BoundAI()
	{
		if (Main.NetMode != 1)
		{
			for (int i = 0; i < 8; i++)
			{
				if (Main.PlayerSet[i].Active != 0 && Main.PlayerSet[i].TalkNPC == WhoAmI)
				{
					if (Type == 105)
					{
						Transform(107);
						return;
					}
					if (Type == 106)
					{
						Transform(108);
						return;
					}
					if (Type == 123)
					{
						Transform(124);
						return;
					}
				}
			}
		}
		Velocity.X *= 0.93f;
		if ((double)Velocity.X > -0.10000000149011612 && (double)Velocity.X < 0.10000000149011612)
		{
			Velocity.X = 0f;
		}
		TargetClosest();
		SpriteDirection = Direction;
	}

	private unsafe void SlimeAI()
	{
		bool flag = !Main.GameTime.DayTime || Life != LifeMax || XYWH.Y > Main.WorldSurface << 4;
		if (Type == 81)
		{
			flag = true;
			if (Main.Rand.Next(32) == 0)
			{
				Dust* ptr = Main.DustSet.NewDust(14, ref XYWH, 0.0, 0.0, Alpha, Colour);
				if (ptr != null)
				{
					ptr->Velocity.X *= 0.3f;
					ptr->Velocity.Y *= 0.3f;
				}
			}
		}
		else if (Type == 59)
		{
			Lighting.AddLight(XYWH.X + (Width >> 1) >> 4, XYWH.Y + (Height >> 1) >> 4, new Vector3(1f, 0.3f, 0.1f));
			Dust* ptr2 = Main.DustSet.NewDust(6, ref XYWH, Velocity.X * 0.2f, Velocity.Y * 0.2f, 100, default, 1.7000000476837158);
			if (ptr2 != null)
			{
				ptr2->NoGravity = true;
			}
		}
		if (AI2 > 1f)
		{
			AI2--;
		}
		if (IsWet)
		{
			if (HasYCollision)
			{
				Velocity.Y = -2f;
			}
			if (Velocity.Y < 0f)
			{
				if (AI3 == Position.X)
				{
					Direction = (sbyte)(-Direction);
					AI2 = 200f;
				}
			}
			else if (Velocity.Y > 0f)
			{
				AI3 = Position.X;
			}
			if (Type == 59)
			{
				if (Velocity.Y > 2f)
				{
					Velocity.Y *= 0.9f;
				}
				else if (DirectionY < 0)
				{
					Velocity.Y -= 0.8f;
				}
				Velocity.Y -= 0.5f;
				if (Velocity.Y < -10f)
				{
					Velocity.Y = -10f;
				}
			}
			else
			{
				if (Velocity.Y > 2f)
				{
					Velocity.Y *= 0.9f;
				}
				Velocity.Y -= 0.5f;
				if (Velocity.Y < -4f)
				{
					Velocity.Y = -4f;
				}
			}
			if ((AI2 == 1f) & flag)
			{
				TargetClosest();
			}
		}
		AIAction = 0;
		if (AI2 == 0f)
		{
			AI0 = -100f;
			AI2 = 1f;
			TargetClosest();
		}
		if (Velocity.Y == 0f)
		{
			if (AI3 == Position.X)
			{
				Direction = (sbyte)(-Direction);
				AI2 = 200f;
			}
			AI3 = 0f;
			Velocity.X *= 0.8f;
			if ((double)Velocity.X > -0.1 && (double)Velocity.X < 0.1)
			{
				Velocity.X = 0f;
			}
			if (flag)
			{
				AI0++;
			}
			AI0++;
			if (Type == 59)
			{
				AI0 += 2f;
			}
			else if (Type == 71)
			{
				AI0 += 3f;
			}
			else if (Type == 138)
			{
				AI0 += 2f;
			}
			else if (Type == 81)
			{
				if (Scale >= 0f)
				{
					AI0 += 4f;
				}
				else
				{
					AI0++;
				}
			}
			if (AI0 >= 0f)
			{
				ShouldNetUpdate = true;
				if (flag && AI2 == 1f)
				{
					TargetClosest();
				}
				if (AI1 == 2f)
				{
					if (Type == 59)
					{
						Velocity.X += 3.5f * (float)Direction;
						Velocity.Y = -10f;
					}
					else
					{
						Velocity.X += 3 * Direction;
						Velocity.Y = -8f;
					}
					AI0 = -200f;
					AI1 = 0f;
					AI3 = Position.X;
				}
				else
				{
					Velocity.Y = -6f;
					Velocity.X += 2 * Direction;
					if (Type == 59)
					{
						Velocity.X += 2 * Direction;
					}
					AI0 = -120f;
					AI1++;
				}
				if (Type == 141)
				{
					Velocity.Y *= 1.3f;
					Velocity.X *= 1.2f;
				}
			}
			else if (AI0 >= -30f)
			{
				AIAction = 1;
			}
		}
		else if (Target < 8 && ((Direction == 1 && Velocity.X < 3f) || (Direction == -1 && Velocity.X > -3f)))
		{
			if ((Direction == -1 && (double)Velocity.X < 0.1) || (Direction == 1 && (double)Velocity.X > -0.1))
			{
				Velocity.X += 0.2f * (float)Direction;
			}
			else
			{
				Velocity.X *= 0.93f;
			}
		}
	}

	private unsafe void FloatingEyeballAI()
	{
		HasNoGravity = true;
		if (!HasNoTileCollide)
		{
			if (HasXCollision)
			{
				Velocity.X = OldVelocity.X * -0.5f;
				if (Direction == -1 && Velocity.X > 0f && Velocity.X < 2f)
				{
					Velocity.X = 2f;
				}
				else if (Direction == 1 && Velocity.X < 0f && Velocity.X > -2f)
				{
					Velocity.X = -2f;
				}
			}
			if (HasYCollision)
			{
				Velocity.Y = OldVelocity.Y * -0.5f;
				if (Velocity.Y > 0f && Velocity.Y < 1f)
				{
					Velocity.Y = 1f;
				}
				else if (Velocity.Y < 0f && Velocity.Y > -1f)
				{
					Velocity.Y = -1f;
				}
			}
		}
		if ((EntityID.DemonEyeTypes.Contains((EntityID.NPCID)Type) || Type == 133) && Main.GameTime.DayTime && XYWH.Y <= Main.WorldSurfacePixels)
		{
			if (TimeLeft > 10)
			{
				TimeLeft = 10;
			}
			DirectionY = (sbyte)((Velocity.Y > 0f) ? 1 : (-1));
			Direction = (sbyte)((Velocity.X > 0f) ? 1 : (-1));
		}
		else
		{
			TargetClosest();
		}
		if (Type == 116)
		{
			TargetClosest();
			Lighting.AddLight(XYWH.X + (Width >> 1) >> 4, XYWH.Y + (Height >> 1) >> 4, new Vector3(0.3f, 0.2f, 0.1f));
			if (Direction == -1 && Velocity.X > -6f)
			{
				Velocity.X -= 0.1f;
				if (Velocity.X > 6f)
				{
					Velocity.X -= 0.1f;
				}
				else if (Velocity.X > 0f)
				{
					Velocity.X -= 0.2f;
				}
				if (Velocity.X < -6f)
				{
					Velocity.X = -6f;
				}
			}
			else if (Direction == 1 && Velocity.X < 6f)
			{
				Velocity.X += 0.1f;
				if (Velocity.X < -6f)
				{
					Velocity.X += 0.1f;
				}
				else if (Velocity.X < 0f)
				{
					Velocity.X += 0.2f;
				}
				if (Velocity.X > 6f)
				{
					Velocity.X = 6f;
				}
			}
			if (DirectionY == -1 && (double)Velocity.Y > -2.5)
			{
				Velocity.Y -= 0.04f;
				if ((double)Velocity.Y > 2.5)
				{
					Velocity.Y -= 0.05f;
				}
				else if (Velocity.Y > 0f)
				{
					Velocity.Y -= 0.15f;
				}
				if ((double)Velocity.Y < -2.5)
				{
					Velocity.Y = -2.5f;
				}
			}
			else if (DirectionY == 1 && (double)Velocity.Y < 1.5)
			{
				Velocity.Y += 0.04f;
				if ((double)Velocity.Y < -2.5)
				{
					Velocity.Y += 0.05f;
				}
				else if (Velocity.Y < 0f)
				{
					Velocity.Y += 0.15f;
				}
				if ((double)Velocity.Y > 2.5)
				{
					Velocity.Y = 2.5f;
				}
			}
			if (Main.Rand.Next(40) == 0)
			{
				Dust* ptr = Main.DustSet.NewDust(XYWH.X, XYWH.Y + (Height >> 2), Width, Height >> 1, 5, Velocity.X, 2.0);
				if (ptr != null)
				{
					ptr->Velocity.X *= 0.5f;
					ptr->Velocity.Y *= 0.1f;
				}
			}
		}
		else if (Type == 133)
		{
			if (Life < LifeMax >> 1)
			{
				if (Direction == -1 && Velocity.X > -6f)
				{
					Velocity.X -= 0.1f;
					if (Velocity.X > 6f)
					{
						Velocity.X -= 0.1f;
					}
					else if (Velocity.X > 0f)
					{
						Velocity.X += 0.05f;
					}
					if (Velocity.X < -6f)
					{
						Velocity.X = -6f;
					}
				}
				else if (Direction == 1 && Velocity.X < 6f)
				{
					Velocity.X += 0.1f;
					if (Velocity.X < -6f)
					{
						Velocity.X += 0.1f;
					}
					else if (Velocity.X < 0f)
					{
						Velocity.X -= 0.05f;
					}
					if (Velocity.X > 6f)
					{
						Velocity.X = 6f;
					}
				}
				if (DirectionY == -1 && Velocity.Y > -4f)
				{
					Velocity.Y -= 0.1f;
					if (Velocity.Y > 4f)
					{
						Velocity.Y -= 0.1f;
					}
					else if (Velocity.Y > 0f)
					{
						Velocity.Y += 0.05f;
					}
					if (Velocity.Y < -4f)
					{
						Velocity.Y = -4f;
					}
				}
				else if (DirectionY == 1 && Velocity.Y < 4f)
				{
					Velocity.Y += 0.1f;
					if (Velocity.Y < -4f)
					{
						Velocity.Y += 0.1f;
					}
					else if (Velocity.Y < 0f)
					{
						Velocity.Y -= 0.05f;
					}
					if (Velocity.Y > 4f)
					{
						Velocity.Y = 4f;
					}
				}
			}
			else
			{
				if (Direction == -1 && Velocity.X > -4f)
				{
					Velocity.X -= 0.1f;
					if (Velocity.X > 4f)
					{
						Velocity.X -= 0.1f;
					}
					else if (Velocity.X > 0f)
					{
						Velocity.X += 0.05f;
					}
					if (Velocity.X < -4f)
					{
						Velocity.X = -4f;
					}
				}
				else if (Direction == 1 && Velocity.X < 4f)
				{
					Velocity.X += 0.1f;
					if (Velocity.X < -4f)
					{
						Velocity.X += 0.1f;
					}
					else if (Velocity.X < 0f)
					{
						Velocity.X -= 0.05f;
					}
					else if (Velocity.X > 4f)
					{
						Velocity.X = 4f;
					}
				}
				if (DirectionY == -1 && (double)Velocity.Y > -1.5)
				{
					Velocity.Y -= 0.04f;
					if ((double)Velocity.Y > 1.5)
					{
						Velocity.Y -= 0.05f;
					}
					else if (Velocity.Y > 0f)
					{
						Velocity.Y += 0.03f;
					}
					else if ((double)Velocity.Y < -1.5)
					{
						Velocity.Y = -1.5f;
					}
				}
				else if (DirectionY == 1 && (double)Velocity.Y < 1.5)
				{
					Velocity.Y += 0.04f;
					if ((double)Velocity.Y < -1.5)
					{
						Velocity.Y += 0.05f;
					}
					else if (Velocity.Y < 0f)
					{
						Velocity.Y -= 0.03f;
					}
					else if ((double)Velocity.Y > 1.5)
					{
						Velocity.Y = 1.5f;
					}
				}
			}
		}
		else
		{
			float num = 4f;
			float num2 = 1.5f;
			num *= 1f + (1f - Scale);
			num2 *= 1f + (1f - Scale);
			if (Direction == -1 && Velocity.X > 0f - num)
			{
				Velocity.X -= 0.1f;
				if (Velocity.X > num)
				{
					Velocity.X -= 0.1f;
				}
				else if (Velocity.X > 0f)
				{
					Velocity.X += 0.05f;
				}
				if (Velocity.X < 0f - num)
				{
					Velocity.X = 0f - num;
				}
			}
			else if (Direction == 1 && Velocity.X < num)
			{
				Velocity.X += 0.1f;
				if (Velocity.X < 0f - num)
				{
					Velocity.X += 0.1f;
				}
				else if (Velocity.X < 0f)
				{
					Velocity.X -= 0.05f;
				}
				if (Velocity.X > num)
				{
					Velocity.X = num;
				}
			}
			if (DirectionY == -1 && Velocity.Y > 0f - num2)
			{
				Velocity.Y -= 0.04f;
				if (Velocity.Y > num2)
				{
					Velocity.Y -= 0.05f;
				}
				else if (Velocity.Y > 0f)
				{
					Velocity.Y += 0.03f;
				}
				if (Velocity.Y < 0f - num2)
				{
					Velocity.Y = 0f - num2;
				}
			}
			else if (DirectionY == 1 && Velocity.Y < num2)
			{
				Velocity.Y += 0.04f;
				if (Velocity.Y < 0f - num2)
				{
					Velocity.Y += 0.05f;
				}
				else if (Velocity.Y < 0f)
				{
					Velocity.Y -= 0.03f;
				}
				if (Velocity.Y > num2)
				{
					Velocity.Y = num2;
				}
			}
		}
		if ((EntityID.DemonEyeTypes.Contains((EntityID.NPCID)Type) || Type == 133) && Main.Rand.Next(40) == 0)
		{
			Dust* ptr2 = Main.DustSet.NewDust(XYWH.X, XYWH.Y + (Height >> 2), Width, Height >> 1, 5, Velocity.X, 2.0);
			if (ptr2 != null)
			{
				ptr2->Velocity.X *= 0.5f;
				ptr2->Velocity.Y *= 0.1f;
			}
		}
		if (IsWet)
		{
			if (Velocity.Y > 0f)
			{
				Velocity.Y *= 0.95f;
			}
			Velocity.Y -= 0.5f;
			if (Velocity.Y < -4f)
			{
				Velocity.Y = -4f;
			}
			TargetClosest();
		}
	}

	private unsafe void WalkAI()
	{
		int num = 60;
		if (Type == 120 || Type == 154)
		{
			num = 20;
			if (AI3 == -120f)
			{
				Velocity.X = 0f;
				Velocity.Y = 0f;
				AI3 = 0f;
				Main.PlaySound(2, XYWH.X, XYWH.Y, 8);
				Vector2 vector = new Vector2(Position.X + (float)(Width >> 1), Position.Y + (float)(Height >> 1));
				float num2 = OldPos[2].X + (float)(Width >> 1) - vector.X;
				float num3 = OldPos[2].Y + (float)(Height >> 1) - vector.Y;
				float num4 = (float)Math.Sqrt(num2 * num2 + num3 * num3);
				num4 = 2f / num4;
				num2 *= num4;
				num3 *= num4;
				for (int i = 0; i < 16; i++)
				{
					Dust* ptr = Main.DustSet.NewDust(71, ref XYWH, num2, num3, 200, default, 2.0);
					if (ptr == null)
					{
						break;
					}
					ptr->NoGravity = true;
					ptr->Velocity.X *= 2f;
				}
				for (int j = 0; j < 16; j++)
				{
					Dust* ptr2 = Main.DustSet.NewDust((int)OldPos[2].X, (int)OldPos[2].Y, Width, Height, 71, 0f - num2, 0f - num3, 200, default, 2.0);
					if (ptr2 == null)
					{
						break;
					}
					ptr2->NoGravity = true;
					ptr2->Velocity.X *= 2f;
				}
			}
		}
		bool flag = false;
		bool flag2 = true;
		if (Type == 47 || Type == 67 || Type == 109 || Type == 110 || Type == 111 || Type == 120 || Type == 154)
		{
			flag2 = false;
		}
		if ((Type != 110 && Type != 111) || !(AI2 > 0f))
		{
			if (Velocity.Y == 0f && ((Velocity.X > 0f && Direction < 0) || (Velocity.X < 0f && Direction > 0)))
			{
				flag = true;
			}
			if ((Position.X == OldPosition.X || AI3 >= (float)num) | flag)
			{
				AI3++;
			}
			else if ((double)Math.Abs(Velocity.X) > 0.9 && AI3 > 0f)
			{
				AI3--;
			}
			if (AI3 > (float)(num * 10))
			{
				AI3 = 0f;
			}
			if (WasJustHit)
			{
				AI3 = 0f;
			}
			if (AI3 == (float)num)
			{
				ShouldNetUpdate = true;
			}
		}
		if (AI3 < (float)num && (!Main.GameTime.DayTime || XYWH.Y > Main.WorldSurfacePixels || EntityID.MummyTypes.Contains((EntityID.NPCID)Type) || Type == 26 || Type == 27 || Type == 28 || Type == 31 || Type == 47 || Type == 67 || Type == 73 || Type == 77 || Type == 110 || Type == 111 || Type == 120 || Type == 154))
		{
			if (Type == 21 || Type == 31 || Type == 77 || Type == 110 || EntityID.ZombieTypes.Contains((EntityID.NPCID)Type))
			{
				if (Main.Rand.Next(1000) == 0)
				{
					Main.PlaySound(14, XYWH.X, XYWH.Y);
				}
			}
			else if (EntityID.MummyTypes.Contains((EntityID.NPCID)Type) && Main.Rand.Next(500) == 0)
			{
				Main.PlaySound(26, XYWH.X, XYWH.Y);
			}
			TargetClosest();
		}
		else if ((Type != 110 && Type != 111) || !(AI2 > 0f))
		{
			if (Main.GameTime.DayTime && XYWH.Y >> 4 < Main.WorldSurface && TimeLeft > 10)
			{
				TimeLeft = 10;
			}
			if (Velocity.X == 0f)
			{
				if (Velocity.Y == 0f)
				{
					AI0++;
					if (AI0 >= 2f)
					{
						Direction = (sbyte)(-Direction);
						SpriteDirection = Direction;
						AI0 = 0f;
					}
				}
			}
			else
			{
				AI0 = 0f;
			}
			if (Direction == 0)
			{
				Direction = 1;
			}
		}
		if (Type == 120)
		{
			if (Velocity.X < -3f || Velocity.X > 3f)
			{
				if (Velocity.Y == 0f)
				{
					Velocity.X *= 0.8f;
				}
			}
			else if (Velocity.X < 3f && Direction == 1)
			{
				if (Velocity.Y == 0f && Velocity.X < 0f)
				{
					Velocity.X *= 0.99f;
				}
				Velocity.X += 0.07f;
				if (Velocity.X > 3f)
				{
					Velocity.X = 3f;
				}
			}
			else if (Velocity.X > -3f && Direction == -1)
			{
				if (Velocity.Y == 0f && Velocity.X > 0f)
				{
					Velocity.X *= 0.99f;
				}
				Velocity.X -= 0.07f;
				if (Velocity.X < -3f)
				{
					Velocity.X = -3f;
				}
			}
		}
		else if (Type == 27 || Type == 77 || Type == 104)
		{
			if (Velocity.X < -2f || Velocity.X > 2f)
			{
				if (Velocity.Y == 0f)
				{
					Velocity.X *= 0.8f;
				}
			}
			else if (Velocity.X < 2f && Direction == 1)
			{
				Velocity.X += 0.07f;
				if (Velocity.X > 2f)
				{
					Velocity.X = 2f;
				}
			}
			else if (Velocity.X > -2f && Direction == -1)
			{
				Velocity.X -= 0.07f;
				if (Velocity.X < -2f)
				{
					Velocity.X = -2f;
				}
			}
		}
		else if (Type == 109)
		{
			if (Velocity.X < -2f || Velocity.X > 2f)
			{
				if (Velocity.Y == 0f)
				{
					Velocity.X *= 0.8f;
				}
			}
			else if (Velocity.X < 2f && Direction == 1)
			{
				Velocity.X += 0.04f;
				if (Velocity.X > 2f)
				{
					Velocity.X = 2f;
				}
			}
			else if (Velocity.X > -2f && Direction == -1)
			{
				Velocity.X -= 0.04f;
				if (Velocity.X < -2f)
				{
					Velocity.X = -2f;
				}
			}
		}
		else if (Type == 21 || Type == 26 || Type == 31 || Type == 47 || Type == 73 || Type == 140)
		{
			if (Velocity.X < -1.5f || Velocity.X > 1.5f)
			{
				if (Velocity.Y == 0f)
				{
					Velocity.X *= 0.8f;
				}
			}
			else if (Velocity.X < 1.5f && Direction == 1)
			{
				Velocity.X += 0.07f;
				if (Velocity.X > 1.5f)
				{
					Velocity.X = 1.5f;
				}
			}
			else if (Velocity.X > -1.5f && Direction == -1)
			{
				Velocity.X -= 0.07f;
				if (Velocity.X < -1.5f)
				{
					Velocity.X = -1.5f;
				}
			}
		}
		else if (Type == 67)
		{
			if (Velocity.X < -0.5f || Velocity.X > 0.5f)
			{
				if (Velocity.Y == 0f)
				{
					Velocity.X *= 0.7f;
				}
			}
			else if (Velocity.X < 0.5f && Direction == 1)
			{
				Velocity.X += 0.03f;
				if (Velocity.X > 0.5f)
				{
					Velocity.X = 0.5f;
				}
			}
			else if (Velocity.X > -0.5f && Direction == -1)
			{
				Velocity.X -= 0.03f;
				if (Velocity.X < -0.5f)
				{
					Velocity.X = -0.5f;
				}
			}
		}
		else if (EntityID.MummyTypes.Contains((EntityID.NPCID)Type))
		{
			float num5 = 1f;
			float num6 = 0.05f;
			if (Life < LifeMax >> 1)
			{
				num5 = 2f;
				num6 = 0.1f;
			}
			if (Type == 79 || Type == 152)
			{
				num5 *= 1.5f;
			}
			if (Velocity.X < 0f - num5 || Velocity.X > num5)
			{
				if (Velocity.Y == 0f)
				{
					Velocity.X *= 0.7f;
				}
			}
			else if (Velocity.X < num5 && Direction == 1)
			{
				Velocity.X += num6;
				if (Velocity.X > num5)
				{
					Velocity.X = num5;
				}
			}
			else if (Velocity.X > 0f - num5 && Direction == -1)
			{
				Velocity.X -= num6;
				if (Velocity.X < 0f - num5)
				{
					Velocity.X = 0f - num5;
				}
			}
		}
		else if (Type != 110 && Type != 111)
		{
			if (Velocity.X < -1f || Velocity.X > 1f)
			{
				if (Velocity.Y == 0f)
				{
					Velocity.X *= 0.8f;
				}
			}
			else if (Velocity.X < 1f && Direction == 1)
			{
				Velocity.X += 0.07f;
				if (Velocity.X > 1f)
				{
					Velocity.X = 1f;
				}
			}
			else if (Velocity.X > -1f && Direction == -1)
			{
				Velocity.X -= 0.07f;
				if (Velocity.X < -1f)
				{
					Velocity.X = -1f;
				}
			}
		}
		if (Type == 110 || Type == 111)
		{
			if (IsConfused)
			{
				AI2 = 0f;
			}
			else
			{
				if (AI1 > 0f)
				{
					AI1--;
				}
				if (WasJustHit)
				{
					AI1 = 30f;
					AI2 = 0f;
				}
				int num7 = ((Type == 111) ? 180 : 70);
				if (AI2 > 0f)
				{
					TargetClosest();
					if (AI1 == (float)(num7 >> 1))
					{
						float num8 = 11f;
						int damage = 35;
						int type = 82;
						if (Type == 111)
						{
							num8 = 9f;
							damage = 11;
							type = 81;
						}
						Vector2 vector2 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
						float num9 = Main.PlayerSet[Target].Position.X + 10f - vector2.X;
						float num10 = Math.Abs(num9) * 0.1f;
						float num11 = Main.PlayerSet[Target].Position.Y + 21f - vector2.Y - num10;
						num9 += (float)Main.Rand.Next(-40, 41);
						num11 += (float)Main.Rand.Next(-40, 41);
						float num12 = (float)Math.Sqrt(num9 * num9 + num11 * num11);
						ShouldNetUpdate = true;
						num12 = num8 / num12;
						num9 *= num12;
						num11 *= num12;
						vector2.X += num9;
						vector2.Y += num11;
						if (Main.NetMode != 1)
						{
							Projectile.NewProjectile(vector2.X, vector2.Y, num9, num11, type, damage, 0f);
						}
						if (Math.Abs(num11) > Math.Abs(num9) * 2f)
						{
							if (num11 > 0f)
							{
								AI2 = 1f;
							}
							else
							{
								AI2 = 5f;
							}
						}
						else if (Math.Abs(num9) > Math.Abs(num11) * 2f)
						{
							AI2 = 3f;
						}
						else if (num11 > 0f)
						{
							AI2 = 2f;
						}
						else
						{
							AI2 = 4f;
						}
					}
					if (Velocity.Y != 0f || AI1 <= 0f)
					{
						AI1 = 0f;
						AI2 = 0f;
					}
					else
					{
						Velocity.X *= 0.9f;
						SpriteDirection = Direction;
					}
				}
				if (AI2 <= 0f && Velocity.Y == 0f && AI1 <= 0f && !Main.PlayerSet[Target].IsDead && Collision.CanHit(ref XYWH, ref Main.PlayerSet[Target].XYWH))
				{
					Vector2 vector3 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
					float num13 = Main.PlayerSet[Target].Position.X + 10f - vector3.X;
					float num14 = Math.Abs(num13) * 0.1f;
					float num15 = Main.PlayerSet[Target].Position.Y + 21f - vector3.Y - num14;
					num13 += (float)Main.Rand.Next(-40, 41);
					num15 += (float)Main.Rand.Next(-40, 41);
					float num16 = num13 * num13 + num15 * num15;
					if (num16 < 490000f)
					{
						ShouldNetUpdate = true;
						Velocity.X *= 0.5f;
						num16 = 10f / (float)Math.Sqrt(num16);
						num13 *= num16;
						num15 *= num16;
						AI2 = 3f;
						AI1 = num7;
						if (Math.Abs(num15) > Math.Abs(num13) * 2f)
						{
							if (num15 > 0f)
							{
								AI2 = 1f;
							}
							else
							{
								AI2 = 5f;
							}
						}
						else if (Math.Abs(num13) > Math.Abs(num15) * 2f)
						{
							AI2 = 3f;
						}
						else if (num15 > 0f)
						{
							AI2 = 2f;
						}
						else
						{
							AI2 = 4f;
						}
					}
				}
				if (AI2 <= 0f)
				{
					if (Velocity.X < -1f || Velocity.X > 1f)
					{
						if (Velocity.Y == 0f)
						{
							Velocity.X *= 0.8f;
						}
					}
					else if (Velocity.X < 1f && Direction == 1)
					{
						Velocity.X += 0.07f;
						if (Velocity.X > 1f)
						{
							Velocity.X = 1f;
						}
					}
					else if (Velocity.X > -1f && Direction == -1)
					{
						Velocity.X -= 0.07f;
						if (Velocity.X < -1f)
						{
							Velocity.X = -1f;
						}
					}
				}
			}
		}
		else if (Type == 109 && Main.NetMode != 1 && !Main.PlayerSet[Target].IsDead)
		{
			if (WasJustHit)
			{
				AI2 = 0f;
			}
			AI2++;
			if (AI2 > 450f)
			{
				Vector2 vector4 = new Vector2(Position.X + (float)(int)Width * 0.5f - (float)(Direction * 24), Position.Y + 4f);
				int num17 = 3 * Direction;
				int num18 = -5;
				int num19 = Projectile.NewProjectile(vector4.X, vector4.Y, num17, num18, 75, 0, 0f);
				if (num19 >= 0)
				{
					Main.ProjectileSet[num19].timeLeft = 300;
				}
				AI2 = 0f;
			}
		}
		bool flag3 = false;
		if (Velocity.Y == 0f)
		{
			int num20 = XYWH.Y + Height + 8 >> 4;
			int num21 = XYWH.X >> 4;
			int num22 = XYWH.X + Width >> 4;
			for (int k = num21; k <= num22; k++)
			{
				if (Main.TileSet[k, num20].IsActive != 0 && Main.TileSolid[Main.TileSet[k, num20].Type])
				{
					flag3 = true;
					break;
				}
			}
		}
		if (flag3)
		{
			int num23 = XYWH.Y + Height - 15 >> 4;
			int num24 = ((Type != 109) ? (XYWH.X + (Width >> 1) + 15 * Direction) : (XYWH.X + (Width >> 1) + ((Width >> 1) + 16) * Direction));
			num24 >>= 4;
			if (flag2 && Main.TileSet[num24, num23 - 1].Type == 10 && Main.TileSet[num24, num23 - 1].IsActive != 0)
			{
				AI2++;
				AI3 = 0f;
				if (AI2 >= 60f)
				{
					if (!Main.GameTime.IsBloodMoon && EntityID.ZombieTypes.Contains((EntityID.NPCID)Type))
					{
						AI1 = 0f;
					}
					Velocity.X = -0.5f * (float)Direction;
					AI1++;
					if (Type == 27)
					{
						AI1++;
					}
					else if (Type == 31)
					{
						AI1 += 6f;
					}
					AI2 = 0f;
					bool flag4 = false;
					if (AI1 >= 10f)
					{
						flag4 = true;
						AI1 = 10f;
					}
					WorldGen.KillTile(num24, num23 - 1, KillToFail: true);
					if (((Main.NetMode != 1 || !flag4) & flag4) && Main.NetMode != 1)
					{
						if (Type == 26)
						{
							WorldGen.KillTile(num24, num23 - 1);
							NetMessage.CreateMessage5(17, 0, num24, num23 - 1, 0);
							NetMessage.SendMessage();
						}
						else
						{
							int num25 = WorldGen.OpenDoor(num24, num23, Direction);
							if (num25 != 0)
							{
								NetMessage.CreateMessage3(19, num24, num23, num25);
								NetMessage.SendMessage();
							}
							else
							{
								AI3 = num;
								ShouldNetUpdate = true;
							}
						}
					}
				}
			}
			else
			{
				if ((Velocity.X < 0f && SpriteDirection == -1) || (Velocity.X > 0f && SpriteDirection == 1))
				{
					if (Main.TileSet[num24, num23 - 2].IsActive != 0 && Main.TileSolid[Main.TileSet[num24, num23 - 2].Type])
					{
						if (Main.TileSet[num24, num23 - 3].IsActive != 0 && Main.TileSolid[Main.TileSet[num24, num23 - 3].Type])
						{
							Velocity.Y = -8f;
						}
						else
						{
							Velocity.Y = -7f;
						}
						ShouldNetUpdate = true;
					}
					else if (Main.TileSet[num24, num23 - 1].IsActive != 0 && Main.TileSolid[Main.TileSet[num24, num23 - 1].Type])
					{
						Velocity.Y = -6f;
						ShouldNetUpdate = true;
					}
					else if (Main.TileSet[num24, num23].IsActive != 0 && Main.TileSolid[Main.TileSet[num24, num23].Type])
					{
						Velocity.Y = -5f;
						ShouldNetUpdate = true;
					}
					else if (DirectionY < 0 && Type != 67 && (Main.TileSet[num24, num23 + 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[num24, num23 + 1].Type]) && (Main.TileSet[num24 + Direction, num23 + 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[num24 + Direction, num23 + 1].Type]))
					{
						Velocity.Y = -8f;
						Velocity.X *= 1.5f;
						ShouldNetUpdate = true;
					}
					else if (flag2)
					{
						AI1 = 0f;
						AI2 = 0f;
					}
				}
				if (Type == 31 || Type == 47 || Type == 77 || Type == 104)
				{
					if (Velocity.Y == 0f && Math.Abs(Position.X + (float)(Width >> 1) - (Main.PlayerSet[Target].Position.X + 10f)) < 100f && Math.Abs(Position.Y + (float)(Height >> 1) - (Main.PlayerSet[Target].Position.Y + 21f)) < 50f && ((Direction > 0 && Velocity.X >= 1f) || (Direction < 0 && Velocity.X <= -1f)))
					{
						Velocity.X *= 2f;
						if (Velocity.X > 3f)
						{
							Velocity.X = 3f;
						}
						if (Velocity.X < -3f)
						{
							Velocity.X = -3f;
						}
						Velocity.Y = -4f;
						ShouldNetUpdate = true;
					}
				}
				else if ((Type == 120 || Type == 154) && Velocity.Y < 0f)
				{
					Velocity.Y *= 1.1f;
				}
			}
		}
		else if (flag2)
		{
			AI1 = 0f;
			AI2 = 0f;
		}
		if ((Type != 120 && Type != 154) || Main.NetMode == 1 || !(AI3 >= (float)num))
		{
			return;
		}
		int num26 = Main.PlayerSet[Target].XYWH.X >> 4;
		int num27 = Main.PlayerSet[Target].XYWH.Y >> 4;
		int num28 = XYWH.X >> 4;
		int num29 = XYWH.Y >> 4;
		if (Math.Abs(XYWH.X - Main.PlayerSet[Target].XYWH.X) + Math.Abs(XYWH.Y - Main.PlayerSet[Target].XYWH.Y) > 2000)
		{
			return;
		}
		int num30 = 0;
		do
		{
			int num31 = Main.Rand.Next(num26 - 20, num26 + 20);
			for (int l = Main.Rand.Next(num27 - 20, num27 + 20); l < num27 + 20; l++)
			{
				if ((l < num27 - 4 || l > num27 + 4 || num31 < num26 - 4 || num31 > num26 + 4) && (l < num29 - 1 || l > num29 + 1 || num31 < num28 - 1 || num31 > num28 + 1) && Main.TileSet[num31, l].IsActive != 0 && (Type != 32 || Main.TileSet[num31, l - 1].WallType != 0) && Main.TileSet[num31, l - 1].Lava == 0 && Main.TileSolid[Main.TileSet[num31, l].Type] && !Collision.SolidTiles(num31 - 1, num31 + 1, l - 4, l - 1))
				{
					Position.X = (XYWH.X = num31 * 16 - (Width >> 1));
					Position.Y = (XYWH.Y = l * 16 - Height);
					ShouldNetUpdate = true;
					AI3 = -120f;
					num30 = 32;
					break;
				}
			}
		}
		while (++num30 < 32);
	}

	private unsafe void EyeOfCthulhuAI()
	{
		if (Target == 8 || Main.PlayerSet[Target].IsDead || Main.PlayerSet[Target].Active == 0)
		{
			TargetClosest();
		}
		bool isDead = Main.PlayerSet[Target].IsDead;
		float num = Position.X + (float)(Width >> 1) - Main.PlayerSet[Target].Position.X - 10f;
		float num2 = (float)Math.Atan2(Position.Y + (float)(int)Height - 59f - Main.PlayerSet[Target].Position.Y - 21f, num) + 1.57f;
		if (num2 < 0f)
		{
			num2 += 6.283f;
		}
		else if (num2 > 6.283f)
		{
			num2 -= 6.283f;
		}
		float num3 = 0f;
		if (AI0 == 0f && AI1 == 0f)
		{
			num3 = 0.02f;
		}
		if (AI0 == 0f && AI1 == 2f && AI2 > 40f)
		{
			num3 = 0.05f;
		}
		if (AI0 == 3f && AI1 == 0f)
		{
			num3 = 0.05f;
		}
		if (AI0 == 3f && AI1 == 2f && AI2 > 40f)
		{
			num3 = 0.08f;
		}
		if (Rotation < num2)
		{
			if ((double)(num2 - Rotation) > Math.PI)
			{
				Rotation -= num3;
			}
			else
			{
				Rotation += num3;
			}
		}
		else if (Rotation > num2)
		{
			if ((double)(Rotation - num2) > Math.PI)
			{
				Rotation += num3;
			}
			else
			{
				Rotation -= num3;
			}
		}
		if (Rotation > num2 - num3 && Rotation < num2 + num3)
		{
			Rotation = num2;
		}
		if (Rotation < 0f)
		{
			Rotation += 6.283f;
		}
		else if (Rotation > 6.283f)
		{
			Rotation -= 6.283f;
		}
		if (Rotation > num2 - num3 && Rotation < num2 + num3)
		{
			Rotation = num2;
		}
		if (Main.Rand.Next(6) == 0)
		{
			Dust* ptr = Main.DustSet.NewDust(XYWH.X, XYWH.Y + (Height >> 2), Width, Height >> 1, 5, Velocity.X, 2.0);
			if (ptr != null)
			{
				ptr->Velocity.X *= 0.5f;
				ptr->Velocity.Y *= 0.1f;
			}
		}
		if (Main.GameTime.DayTime | isDead)
		{
			Velocity.Y -= 0.04f;
			if (TimeLeft > 10)
			{
				TimeLeft = 10;
			}
			return;
		}
		if (AI0 == 0f)
		{
			if (AI1 == 0f)
			{
				float num4 = 0.04f;
				Vector2 vector = new Vector2(Position.X + (float)(Width >> 1), Position.Y + (float)(Height >> 1));
				float num5 = Main.PlayerSet[Target].Position.X + 10f - vector.X;
				float num6 = Main.PlayerSet[Target].Position.Y + 21f - 200f - vector.Y;
				float num7 = (float)Math.Sqrt(num5 * num5 + num6 * num6);
				float num8 = num7;
				num7 = 5f / num7;
				num5 *= num7;
				num6 *= num7;
				if (Velocity.X < num5)
				{
					Velocity.X += num4;
					if (Velocity.X < 0f && num5 > 0f)
					{
						Velocity.X += num4;
					}
				}
				else if (Velocity.X > num5)
				{
					Velocity.X -= num4;
					if (Velocity.X > 0f && num5 < 0f)
					{
						Velocity.X -= num4;
					}
				}
				if (Velocity.Y < num6)
				{
					Velocity.Y += num4;
					if (Velocity.Y < 0f && num6 > 0f)
					{
						Velocity.Y += num4;
					}
				}
				else if (Velocity.Y > num6)
				{
					Velocity.Y -= num4;
					if (Velocity.Y > 0f && num6 < 0f)
					{
						Velocity.Y -= num4;
					}
				}
				AI2++;
				if (AI2 >= 600f)
				{
					AI1 = 1f;
					AI2 = 0f;
					AI3 = 0f;
					Target = 8;
					ShouldNetUpdate = true;
				}
				else if (XYWH.Y + Height < Main.PlayerSet[Target].XYWH.Y && num8 < 500f)
				{
					if (!Main.PlayerSet[Target].IsDead)
					{
						AI3++;
					}
					if (AI3 >= 110f)
					{
						AI3 = 0f;
						Rotation = num2;
						float num9 = Main.PlayerSet[Target].Position.X + 10f - vector.X;
						float num10 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
						float num11 = (float)Math.Sqrt(num9 * num9 + num10 * num10);
						num11 = 5f / num11;
						Vector2 vector2 = vector;
						Vector2 vector3 = new Vector2
						{
							X = num9 * num11,
							Y = num10 * num11
						};
						vector2.X += vector3.X * 10f;
						vector2.Y += vector3.Y * 10f;
						if (Main.NetMode != 1)
						{
							int num12 = NewNPC((int)vector2.X, (int)vector2.Y, 5);
							if (num12 < 196)
							{
								Main.NPCSet[num12].Velocity.X = vector3.X;
								Main.NPCSet[num12].Velocity.Y = vector3.Y;
								NetMessage.CreateMessage1(23, num12);
								NetMessage.SendMessage();
							}
						}
						Main.PlaySound(3, (int)vector2.X, (int)vector2.Y);
						for (int i = 0; i < 8; i++)
						{
							if (null == Main.DustSet.NewDust((int)vector2.X, (int)vector2.Y, 20, 20, 5, vector3.X * 0.4f, vector3.Y * 0.4f))
							{
								break;
							}
						}
					}
				}
			}
			else if (AI1 == 1f)
			{
				Rotation = num2;
				Vector2 vector4 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
				float num13 = Main.PlayerSet[Target].Position.X + 10f - vector4.X;
				float num14 = Main.PlayerSet[Target].Position.Y + 21f - vector4.Y;
				float num15 = (float)Math.Sqrt(num13 * num13 + num14 * num14);
				num15 = 6f / num15;
				Velocity.X = num13 * num15;
				Velocity.Y = num14 * num15;
				AI1 = 2f;
			}
			else if (AI1 == 2f)
			{
				AI2++;
				if (AI2 >= 40f)
				{
					Velocity.X *= 0.98f;
					Velocity.Y *= 0.98f;
					if ((double)Velocity.X > -0.1 && (double)Velocity.X < 0.1)
					{
						Velocity.X = 0f;
					}
					if ((double)Velocity.Y > -0.1 && (double)Velocity.Y < 0.1)
					{
						Velocity.Y = 0f;
					}
				}
				else
				{
					Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X) - 1.57f;
				}
				if (AI2 >= 150f)
				{
					AI3++;
					AI2 = 0f;
					Target = 8;
					Rotation = num2;
					if (AI3 >= 3f)
					{
						AI1 = 0f;
						AI3 = 0f;
					}
					else
					{
						AI1 = 1f;
					}
				}
			}
			if (Life < LifeMax >> 1)
			{
				AI0 = 1f;
				AI1 = 0f;
				AI2 = 0f;
				AI3 = 0f;
				ShouldNetUpdate = true;
			}
			return;
		}
		if (AI0 == 1f || AI0 == 2f)
		{
			if (AI0 == 1f)
			{
				AI2 += 0.005f;
				if ((double)AI2 > 0.5)
				{
					AI2 = 0.5f;
				}
			}
			else
			{
				AI2 -= 0.005f;
				if (AI2 < 0f)
				{
					AI2 = 0f;
				}
			}
			Rotation += AI2;
			AI1++;
			if (AI1 == 100f)
			{
				AI0++;
				AI1 = 0f;
				if (AI0 == 3f)
				{
					AI2 = 0f;
				}
				else
				{
					Main.PlaySound(3, XYWH.X, XYWH.Y);
					Main.PlaySound(15, XYWH.X, XYWH.Y, 0);
					for (int j = 0; j < 2; j++)
					{
						Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 8);
						Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 7);
						Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 6);
					}
					for (int k = 0; k < 16; k++)
					{
						if (null == Main.DustSet.NewDust(5, ref XYWH, (double)Main.Rand.Next(-30, 31) * 0.2, (double)Main.Rand.Next(-30, 31) * 0.2))
						{
							break;
						}
					}
				}
			}
			Main.DustSet.NewDust(5, ref XYWH, (double)Main.Rand.Next(-30, 31) * 0.2, (double)Main.Rand.Next(-30, 31) * 0.2);
			Velocity.X *= 0.98f;
			Velocity.Y *= 0.98f;
			if ((double)Velocity.X > -0.1 && (double)Velocity.X < 0.1)
			{
				Velocity.X = 0f;
			}
			if ((double)Velocity.Y > -0.1 && (double)Velocity.Y < 0.1)
			{
				Velocity.Y = 0f;
			}
			return;
		}
		Damage = 23;
		Defense = 0;
		if (AI1 == 0f)
		{
			float num16 = 0.07f;
			Vector2 vector5 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num17 = Main.PlayerSet[Target].Position.X + 10f - vector5.X;
			float num18 = Main.PlayerSet[Target].Position.Y + 21f - 120f - vector5.Y;
			float num19 = (float)Math.Sqrt(num17 * num17 + num18 * num18);
			num19 = 6f / num19;
			num17 *= num19;
			num18 *= num19;
			if (Velocity.X < num17)
			{
				Velocity.X += num16;
				if (Velocity.X < 0f && num17 > 0f)
				{
					Velocity.X += num16;
				}
			}
			else if (Velocity.X > num17)
			{
				Velocity.X -= num16;
				if (Velocity.X > 0f && num17 < 0f)
				{
					Velocity.X -= num16;
				}
			}
			if (Velocity.Y < num18)
			{
				Velocity.Y += num16;
				if (Velocity.Y < 0f && num18 > 0f)
				{
					Velocity.Y += num16;
				}
			}
			else if (Velocity.Y > num18)
			{
				Velocity.Y -= num16;
				if (Velocity.Y > 0f && num18 < 0f)
				{
					Velocity.Y -= num16;
				}
			}
			AI2++;
			if (AI2 >= 200f)
			{
				AI1 = 1f;
				AI2 = 0f;
				AI3 = 0f;
				Target = 8;
				ShouldNetUpdate = true;
			}
		}
		else if (AI1 == 1f)
		{
			Main.PlaySound(15, XYWH.X, XYWH.Y, 0);
			Rotation = num2;
			Vector2 vector6 = new Vector2(Position.X + (float)(Width >> 1), Position.Y + (float)(Height >> 1));
			float num20 = Main.PlayerSet[Target].Position.X + 10f - vector6.X;
			float num21 = Main.PlayerSet[Target].Position.Y + 21f - vector6.Y;
			float num22 = (float)Math.Sqrt(num20 * num20 + num21 * num21);
			num22 = 6.8f / num22;
			Velocity.X = num20 * num22;
			Velocity.Y = num21 * num22;
			AI1 = 2f;
		}
		else
		{
			if (AI1 != 2f)
			{
				return;
			}
			AI2++;
			if (AI2 >= 40f)
			{
				Velocity.X *= 0.97f;
				Velocity.Y *= 0.97f;
				if ((double)Velocity.X > -0.1 && (double)Velocity.X < 0.1)
				{
					Velocity.X = 0f;
				}
				if ((double)Velocity.Y > -0.1 && (double)Velocity.Y < 0.1)
				{
					Velocity.Y = 0f;
				}
			}
			else
			{
				Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X) - 1.57f;
			}
			if (AI2 >= 130f)
			{
				AI3++;
				AI2 = 0f;
				Target = 8;
				Rotation = num2;
				if (AI3 >= 3f)
				{
					AI1 = 0f;
					AI3 = 0f;
				}
				else
				{
					AI1 = 1f;
				}
			}
		}
	}

	private unsafe void AggressiveFlyerAI()
	{
		if (Target == 8 || Main.PlayerSet[Target].IsDead)
		{
			TargetClosest();
		}
		float num;
		float num2;
		switch (Type)
		{
		case 5:
			num = 5f;
			num2 = 0.03f;
			break;
		case 167:
			Lighting.AddLight(XYWH.X >> 4, XYWH.Y >> 4, new Vector3(1f, 1f, 1f));
			num = 9f;
			num2 = 0.1f;
			break;
		case 6:
			num = 4f;
			num2 = 0.02f;
			break;
		case 42:
		case 157:
			num = 3.5f;
			num2 = 0.021f;
			break;
		case 23:
			num = 1f;
			num2 = 0.03f;
			break;
		case 94:
			num = 4.2f;
			num2 = 0.022f;
			break;
		default:
			num = 6f;
			num2 = 0.05f;
			break;
		}
		int num3 = (XYWH.X + (Width >> 1)) & -8;
		int num4 = (XYWH.Y + (Height >> 1)) & -8;
		float num5 = ((Main.PlayerSet[Target].XYWH.X + 10) & -8) - num3;
		float num6 = ((Main.PlayerSet[Target].XYWH.Y + 21) & -8) - num4;
		float num7 = num5 * num5 + num6 * num6;
		float num8 = num7;
		bool flag = false;
		if (num7 == 0f)
		{
			num5 = Velocity.X;
			num6 = Velocity.Y;
		}
		else
		{
			if (num7 > 360000f)
			{
				flag = true;
			}
			num7 = num / (float)Math.Sqrt(num7);
			num5 *= num7;
			num6 *= num7;
		}
		if (Type == 6 || Type == 42 || Type == 157 || Type == 94 || Type == 139)
		{
			if (Type == 42 || Type == 157 || Type == 94 || num8 > 10000f)
			{
				AI0++;
				if (AI0 > 0f)
				{
					Velocity.Y += 0.023f;
				}
				else
				{
					Velocity.Y -= 0.023f;
				}
				if (AI0 < -100f || AI0 > 100f)
				{
					Velocity.X += 0.023f;
				}
				else
				{
					Velocity.X -= 0.023f;
				}
				if (AI0 > 200f)
				{
					AI0 = -200f;
				}
			}
			if ((Type == 6 || Type == 94) && num8 < 22500f)
			{
				Velocity.X += num5 * 0.007f;
				Velocity.Y += num6 * 0.007f;
			}
		}
		if (Main.PlayerSet[Target].IsDead)
		{
			num5 = (float)Direction * num * 0.5f;
			num6 = num * -0.5f;
		}
		if (Velocity.X < num5)
		{
			Velocity.X += num2;
			if (Velocity.X < 0f && num5 > 0f && Type != 6 && Type != 42 && Type != 157 && Type != 94 && Type != 139)
			{
				Velocity.X += num2;
			}
		}
		else if (Velocity.X > num5)
		{
			Velocity.X -= num2;
			if (Velocity.X > 0f && num5 < 0f && Type != 6 && Type != 42 && Type != 157 && Type != 94 && Type != 139)
			{
				Velocity.X -= num2;
			}
		}
		if (Velocity.Y < num6)
		{
			Velocity.Y += num2;
			if (Velocity.Y < 0f && num6 > 0f && Type != 6 && Type != 42 && Type != 157 && Type != 94 && Type != 139)
			{
				Velocity.Y += num2;
			}
		}
		else if (Velocity.Y > num6)
		{
			Velocity.Y -= num2;
			if (Velocity.Y > 0f && num6 < 0f && Type != 6 && Type != 42 && Type != 157 && Type != 94 && Type != 139)
			{
				Velocity.Y -= num2;
			}
		}
		if (Type == 23)
		{
			if (num5 > 0f)
			{
				SpriteDirection = 1;
				Rotation = (float)Math.Atan2(num6, num5);
			}
			else if (num5 < 0f)
			{
				SpriteDirection = -1;
				Rotation = (float)Math.Atan2(num6, num5) + 3.14f;
			}
		}
		else if (Type == 139)
		{
			if (WasJustHit)
			{
				LocalAI0 = 0;
			}
			else if (++LocalAI0 >= 120 && Main.NetMode != 1)
			{
				LocalAI0 = 0;
				if (Collision.CanHit(ref XYWH, ref Main.PlayerSet[Target].XYWH))
				{
					int damage = 25;
					int type = 84;
					Projectile.NewProjectile(num3, num4, num5, num6, type, damage, 0f);
				}
			}
			int num9 = XYWH.X + (Width >> 1);
			int num10 = XYWH.Y + (Height >> 1);
			int i = num9 >> 4;
			num10 >>= 4;
			if (!WorldGen.SolidTile(i, num10))
			{
				Lighting.AddLight(XYWH.X + (Width >> 1) >> 4, XYWH.Y + (Height >> 1) >> 4, new Vector3(0.3f, 0.1f, 0.05f));
			}
			if (num5 > 0f)
			{
				SpriteDirection = 1;
				Rotation = (float)Math.Atan2(num6, num5);
			}
			if (num5 < 0f)
			{
				SpriteDirection = -1;
				Rotation = (float)Math.Atan2(num6, num5) + 3.14f;
			}
		}
		else if (Type == 6 || Type == 94)
		{
			Rotation = (float)Math.Atan2(num6, num5) - 1.57f;
		}
		else if (Type == 42 || Type == 157)
		{
			if (num5 > 0f)
			{
				SpriteDirection = 1;
			}
			else if (num5 < 0f)
			{
				SpriteDirection = -1;
			}
			Rotation = Velocity.X * 0.1f;
		}
		else
		{
			Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X) - 1.57f;
		}
		if (Type == 6 || Type == 23 || Type == 42 || Type == 157 || Type == 94 || Type == 139)
		{
			float num11 = 0.7f;
			if (Type == 6)
			{
				num11 = 0.4f;
			}
			if (HasXCollision)
			{
				ShouldNetUpdate = true;
				Velocity.X = OldVelocity.X * (0f - num11);
				if (Direction == -1 && Velocity.X > 0f && Velocity.X < 2f)
				{
					Velocity.X = 2f;
				}
				else if (Direction == 1 && Velocity.X < 0f && Velocity.X > -2f)
				{
					Velocity.X = -2f;
				}
			}
			if (HasYCollision)
			{
				ShouldNetUpdate = true;
				Velocity.Y = OldVelocity.Y * (0f - num11);
				if (Velocity.Y > 0f && (double)Velocity.Y < 1.5)
				{
					Velocity.Y = 2f;
				}
				else if (Velocity.Y < 0f && (double)Velocity.Y > -1.5)
				{
					Velocity.Y = -2f;
				}
			}
			if (Type == 23)
			{
				Dust* ptr = Main.DustSet.NewDust((int)(Position.X - Velocity.X), (int)(Position.Y - Velocity.Y), Width, Height, 6, Velocity.X * 0.2f, Velocity.Y * 0.2f, 100, default, 2.0);
				if (ptr != null)
				{
					ptr->NoGravity = true;
					ptr->Velocity.X *= 0.3f;
					ptr->Velocity.Y *= 0.3f;
				}
			}
			else if (Type != 42 && Type != 157 && Type != 139 && Main.Rand.Next(24) == 0)
			{
				Dust* ptr2 = Main.DustSet.NewDust(XYWH.X, XYWH.Y + (Height >> 2), Width, Height >> 1, 18, Velocity.X, 2.0, 75, Colour, Scale);
				if (ptr2 != null)
				{
					ptr2->Velocity.X *= 0.5f;
					ptr2->Velocity.Y *= 0.1f;
				}
			}
		}
		else if (Main.Rand.Next(48) == 0)
		{
			Dust* ptr3 = Main.DustSet.NewDust(XYWH.X, XYWH.Y + (Height >> 2), Width, Height >> 1, 5, Velocity.X, 2.0);
			if (ptr3 != null)
			{
				ptr3->Velocity.X *= 0.5f;
				ptr3->Velocity.Y *= 0.1f;
			}
		}
		if (Type == 6 || Type == 94)
		{
			if (IsWet)
			{
				if (Velocity.Y > 0f)
				{
					Velocity.Y *= 0.95f;
				}
				Velocity.Y -= 0.3f;
				if (Velocity.Y < -2f)
				{
					Velocity.Y = -2f;
				}
			}
		}
		else if (Type == 42 || Type == 157)
		{
			if (IsWet)
			{
				if (Velocity.Y > 0f)
				{
					Velocity.Y *= 0.95f;
				}
				Velocity.Y -= 0.5f;
				if (Velocity.Y < -4f)
				{
					Velocity.Y = -4f;
				}
				TargetClosest();
			}
			if (AI1 == 101f)
			{
				Main.PlaySound(2, XYWH.X, XYWH.Y, 17);
				AI1 = 0f;
			}
			if (Main.NetMode != 1)
			{
				AI1 += (float)Main.Rand.Next(5, 20) * 0.1f * Scale;
				if (AI1 >= 130f)
				{
					if (Collision.CanHit(ref XYWH, ref Main.PlayerSet[Target].XYWH))
					{
						Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(Height >> 1));
						float num12 = Main.PlayerSet[Target].Position.X + 10f - vector.X + (float)Main.Rand.Next(-20, 21);
						float num13 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y + (float)Main.Rand.Next(-20, 21);
						if ((num12 < 0f && Velocity.X < 0f) || (num12 > 0f && Velocity.X > 0f))
						{
							float num14 = (float)Math.Sqrt(num12 * num12 + num13 * num13);
							num14 = 8f / num14;
							num12 *= num14;
							num13 *= num14;
							int damage2 = (int)(13f * Scale);
							int type2 = 55;
							int num15 = Projectile.NewProjectile(vector.X, vector.Y, num12, num13, type2, damage2, 0f);
							if (num15 >= 0)
							{
								Main.ProjectileSet[num15].timeLeft = 300;
							}
							AI1 = 101f;
							ShouldNetUpdate = true;
						}
						else
						{
							AI1 = 0f;
						}
					}
					else
					{
						AI1 = 0f;
					}
				}
			}
		}
		else if ((Type == 139) & flag)
		{
			if ((Velocity.X > 0f && num5 > 0f) || (Velocity.X < 0f && num5 < 0f))
			{
				if (Math.Abs(Velocity.X) < 12f)
				{
					Velocity.X *= 1.05f;
				}
			}
			else
			{
				Velocity.X *= 0.9f;
			}
		}
		if (Main.NetMode != 1 && Type == 94 && !Main.PlayerSet[Target].IsDead)
		{
			if (WasJustHit)
			{
				LocalAI0 = 0;
			}
			LocalAI0++;
			if (LocalAI0 == 180)
			{
				if (Collision.CanHit(ref XYWH, ref Main.PlayerSet[Target].XYWH))
				{
					NewNPC((int)(Position.X + Velocity.X) + (Width >> 1), (int)(Position.Y + Velocity.Y) + (Height >> 1), 112);
				}
				LocalAI0 = 0;
			}
		}
		if ((Main.GameTime.DayTime && Type != 6 && Type != 23 && Type != 42 && Type != 157 && Type != 94) || Main.PlayerSet[Target].IsDead)
		{
			Velocity.Y -= num2 * 2f;
			if (TimeLeft > 10)
			{
				TimeLeft = 10;
			}
		}
		if (((Velocity.X > 0f && OldVelocity.X < 0f) || (Velocity.X < 0f && OldVelocity.X > 0f) || (Velocity.Y > 0f && OldVelocity.Y < 0f) || (Velocity.Y < 0f && OldVelocity.Y > 0f)) && !WasJustHit)
		{
			ShouldNetUpdate = true;
		}
	}

	private unsafe void WormAI()
	{
		if (Type == 117 && LocalAI1 == 0)
		{
			LocalAI1 = 1;
			Main.PlaySound(4, XYWH.X, XYWH.Y, 13);
			int num = 1;
			if (Velocity.X < 0f)
			{
				num = -1;
			}
			for (int i = 0; i < 16; i++)
			{
				if (null == Main.DustSet.NewDust(XYWH.X - 20, XYWH.Y - 20, Width + 40, Height + 40, 5, num * 8, -1.0))
				{
					break;
				}
			}
		}
		if (Type >= 13 && Type <= 15)
		{
			RealLife = -1;
		}
		else if (AI3 > 0f)
		{
			RealLife = (int)AI3;
		}
		if (Target == 8 || Main.PlayerSet[Target].IsDead)
		{
			TargetClosest();
		}
		if (Main.PlayerSet[Target].IsDead && TimeLeft > 300)
		{
			TimeLeft = 300;
		}
		if (Main.NetMode != 1)
		{
			if (Type == 87 || Type == 159)
			{
				if (AI0 == 0f)
				{
					AI3 = WhoAmI;
					RealLife = WhoAmI;
					int num2 = 0;
					int num3 = WhoAmI;
					int num4 = Type - 87;
					for (int j = 0; j < 14; j++)
					{
						int num5 = 89;
						switch (j)
						{
						case 1:
						case 8:
							num5 = 88;
							break;
						case 11:
							num5 = 90;
							break;
						case 12:
							num5 = 91;
							break;
						case 13:
							num5 = 92;
							break;
						}
						num2 = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + Height, num5 + num4, WhoAmI);
						Main.NPCSet[num2].AI3 = WhoAmI;
						Main.NPCSet[num2].RealLife = WhoAmI;
						Main.NPCSet[num2].AI1 = num3;
						Main.NPCSet[num3].AI0 = num2;
						NetMessage.CreateMessage1(23, num2);
						NetMessage.SendMessage();
						num3 = num2;
					}
				}
			}
			else if ((Type == 7 || Type == 8 || Type == 10 || Type == 11 || Type == 13 || Type == 14 || Type == 39 || Type == 40 || Type == 95 || Type == 96 || Type == 98 || Type == 99 || Type == 117 || Type == 118) && AI0 == 0f)
			{
				if (Type == 7 || Type == 10 || Type == 13 || Type == 39 || Type == 95 || Type == 98 || Type == 117)
				{
					if (Type < 13 || Type > 15)
					{
						AI3 = WhoAmI;
						RealLife = WhoAmI;
					}
					AI2 = Main.Rand.Next(8, 13);
					if (Type == 10)
					{
						AI2 = Main.Rand.Next(4, 7);
					}
					else if (Type == 13)
					{
						AI2 = Main.Rand.Next(45, 56);
					}
					else if (Type == 39)
					{
						AI2 = Main.Rand.Next(12, 19);
					}
					else if (Type == 95)
					{
						AI2 = Main.Rand.Next(6, 12);
					}
					else if (Type == 98)
					{
						AI2 = Main.Rand.Next(20, 26);
					}
					else if (Type == 117)
					{
						AI2 = Main.Rand.Next(3, 6);
					}
					AI0 = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + Height, Type + 1, WhoAmI);
				}
				else if ((Type == 8 || Type == 11 || Type == 14 || Type == 40 || Type == 96 || Type == 99 || Type == 118) && AI2 > 0f)
				{
					AI0 = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + Height, Type, WhoAmI);
				}
				else
				{
					AI0 = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + Height, Type + 1, WhoAmI);
				}
				if (Type < 13 || Type > 15)
				{
					Main.NPCSet[(int)AI0].AI3 = AI3;
					Main.NPCSet[(int)AI0].RealLife = RealLife;
				}
				Main.NPCSet[(int)AI0].AI1 = WhoAmI;
				Main.NPCSet[(int)AI0].AI2 = AI2 - 1f;
				ShouldNetUpdate = true;
			}
			if ((Type == 8 || Type == 9 || Type == 11 || Type == 12 || Type == 40 || Type == 41 || Type == 96 || Type == 97 || Type == 99 || Type == 100 || (Type > 87 && Type <= 92) || (Type > 159 && Type <= 164) || Type == 118 || Type == 119) && (Main.NPCSet[(int)AI1].Active == 0 || Main.NPCSet[(int)AI1].AIStyle != AIStyle))
			{
				Life = 0;
				HitEffect();
				Active = 0;
				if (Main.NetMode == 2)
				{
					NetMessage.SendNpcHurt(WhoAmI, -1);
				}
				return;
			}
			if (Type == 7 || Type == 8 || Type == 10 || Type == 11 || Type == 39 || Type == 40 || Type == 95 || Type == 96 || Type == 98 || Type == 99 || (Type >= 87 && Type < 92) || (Type >= 159 && Type < 164) || Type == 117 || Type == 118)
			{
				if (Main.NPCSet[(int)AI0].Active == 0 || Main.NPCSet[(int)AI0].AIStyle != AIStyle)
				{
					Life = 0;
					HitEffect();
					Active = 0;
					if (Main.NetMode == 2)
					{
						NetMessage.SendNpcHurt(WhoAmI, -1);
					}
					return;
				}
			}
			else if (Type >= 13 && Type <= 15)
			{
				if ((Main.NPCSet[(int)AI1].Active == 0 && Main.NPCSet[(int)AI0].Active == 0) || (Type == 13 && Main.NPCSet[(int)AI0].Active == 0) || (Type == 15 && Main.NPCSet[(int)AI1].Active == 0))
				{
					Life = 0;
					HitEffect();
					Active = 0;
				}
				if (Type == 14)
				{
					if (Main.NPCSet[(int)AI1].Active == 0 || Main.NPCSet[(int)AI1].AIStyle != AIStyle)
					{
						Type = 13;
						int whoAmI = WhoAmI;
						float num6 = (float)Life / (float)LifeMax;
						float aI = AI0;
						SetDefaults(Type);
						Life = (int)((float)LifeMax * num6);
						AI0 = aI;
						TargetClosest();
						ShouldNetUpdate = true;
						WhoAmI = (short)whoAmI;
					}
					else if (Main.NPCSet[(int)AI0].Active == 0 || Main.NPCSet[(int)AI0].AIStyle != AIStyle)
					{
						int whoAmI2 = WhoAmI;
						float num7 = (float)Life / (float)LifeMax;
						float aI2 = AI1;
						SetDefaults(Type);
						Life = (int)((float)LifeMax * num7);
						AI1 = aI2;
						TargetClosest();
						ShouldNetUpdate = true;
						WhoAmI = (short)whoAmI2;
					}
				}
				if (Life == 0)
				{
					bool flag = true;
					for (int k = 0; k < 196; k++)
					{
						if (Main.NPCSet[k].Type >= 13 && Main.NPCSet[k].Type <= 15 && Main.NPCSet[k].Active != 0)
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						IsBoss = true;
						NPCLoot();
					}
					if (Active == 0)
					{
						NetMessage.SendNpcHurt(WhoAmI, -1);
						return;
					}
				}
			}
		}
		int num8 = (XYWH.X >> 4) - 1;
		int num9 = (XYWH.X + Width >> 4) + 2;
		int num10 = (XYWH.Y >> 4) - 1;
		int num11 = (XYWH.Y + Height >> 4) + 2;
		if (num8 < 0)
		{
			num8 = 0;
		}
		if (num9 > Main.MaxTilesX)
		{
			num9 = Main.MaxTilesX;
		}
		if (num10 < 0)
		{
			num10 = 0;
		}
		if (num11 > Main.MaxTilesY)
		{
			num11 = Main.MaxTilesY;
		}
		bool flag2 = (Type >= 87 && Type <= 92) || (Type >= 159 && Type <= 164);
		if (!flag2)
		{
			Vector2 vector = default;
			for (int l = num8; l < num9; l++)
			{
				for (int m = num10; m < num11; m++)
				{
					if (!Main.TileSet[l, m].CanStandOnTop() && Main.TileSet[l, m].Liquid <= 64)
					{
						continue;
					}
					vector.X = l * 16;
					vector.Y = m * 16;
					if (Position.X + (float)(int)Width > vector.X && Position.X < vector.X + 16f && Position.Y + (float)(int)Height > vector.Y && Position.Y < vector.Y + 16f)
					{
						flag2 = true;
						if (Main.Rand.Next(100) == 0 && Type != 117 && Main.TileSet[l, m].IsActive != 0)
						{
							WorldGen.KillTile(l, m, KillToFail: true, EffectOnly: true);
						}
					}
				}
			}
		}
		if (!flag2 && (Type == 7 || Type == 10 || Type == 13 || Type == 39 || Type == 95 || Type == 98 || Type == 117))
		{
			bool flag3 = true;
			for (int n = 0; n < 8; n++)
			{
				if (Main.PlayerSet[n].Active != 0)
				{
					Rectangle value = new Rectangle(Main.PlayerSet[n].XYWH.X - 1000, Main.PlayerSet[n].XYWH.Y - 1000, 2000, 2000);
					if (XYWH.Intersects(value))
					{
						flag3 = false;
						break;
					}
				}
			}
			if (flag3)
			{
				flag2 = true;
			}
		}
		if ((Type >= 87 && Type <= 92) || (Type >= 159 && Type <= 164))
		{
			if (Velocity.X < 0f)
			{
				SpriteDirection = 1;
			}
			else if (Velocity.X > 0f)
			{
				SpriteDirection = -1;
			}
		}
		float num12 = 8f;
		float num13 = 0.07f;
		if (Type == 95)
		{
			num12 = 5.5f;
			num13 = 0.045f;
		}
		else if (Type == 10)
		{
			num12 = 6f;
			num13 = 0.05f;
		}
		else if (Type == 13)
		{
			num12 = 10f;
			num13 = 0.07f;
		}
		else if (Type == 87 || Type == 159)
		{
			num12 = 11f;
			num13 = 0.25f;
		}
		else if (Type == 117 && WoF >= 0)
		{
			float num14 = (float)Main.NPCSet[WoF].Life / (float)Main.NPCSet[WoF].LifeMax;
			if ((double)num14 < 0.5)
			{
				num12++;
				num13 += 0.1f;
			}
			if ((double)num14 < 0.25)
			{
				num12++;
				num13 += 0.1f;
			}
			if ((double)num14 < 0.1)
			{
				num12 += 2f;
				num13 += 0.1f;
			}
		}
		Vector2 position = Position;
		position.X += Width >> 1;
		position.Y += Height >> 1;
		float num15;
		float num16;
		if (AI1 > 0f && AI1 < 196f)
		{
			position.X = Position.X + (float)(Width >> 1);
			position.Y = Position.Y + (float)(Height >> 1);
			num15 = Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - position.X;
			num16 = Main.NPCSet[(int)AI1].Position.Y + (float)(Main.NPCSet[(int)AI1].Height >> 1) - position.Y;
			Rotation = (float)(Math.Atan2(num16, num15) + Math.PI / 2.0);
			float num17 = num15 * num15 + num16 * num16;
			bool flag4 = (Type >= 87 && Type <= 92) || (Type >= 159 && Type <= 164);
			if (num17 > 0f)
			{
				num17 = (float)Math.Sqrt(num17);
				num17 = (num17 - (float)(flag4 ? 30 : Width)) / num17;
				num15 *= num17;
				num16 *= num17;
				Position.X += num15;
				Position.Y += num16;
				XYWH.X = (int)Position.X;
				XYWH.Y = (int)Position.Y;
			}
			Velocity.X = 0f;
			Velocity.Y = 0f;
			if (flag4)
			{
				if (num15 < 0f)
				{
					SpriteDirection = 1;
				}
				else if (num15 > 0f)
				{
					SpriteDirection = -1;
				}
			}
			return;
		}
		num15 = (Main.PlayerSet[Target].XYWH.X + 10) & -16;
		num16 = (Main.PlayerSet[Target].XYWH.Y + 21) & -16;
		position.X = (int)position.X & -16;
		position.Y = (int)position.Y & -16;
		num15 -= position.X;
		num16 -= position.Y;
		if (!flag2)
		{
			TargetClosest();
			Velocity.Y += 0.11f;
			if (Velocity.Y > num12)
			{
				Velocity.Y = num12;
			}
			if ((double)(Math.Abs(Velocity.X) + Math.Abs(Velocity.Y)) < (double)num12 * 0.4)
			{
				if (Velocity.X < 0f)
				{
					Velocity.X -= num13 * 1.1f;
				}
				else
				{
					Velocity.X += num13 * 1.1f;
				}
			}
			else if (Velocity.Y == num12)
			{
				if (Velocity.X < num15)
				{
					Velocity.X += num13;
				}
				else if (Velocity.X > num15)
				{
					Velocity.X -= num13;
				}
			}
			else if (Velocity.Y > 4f)
			{
				if (Velocity.X < 0f)
				{
					Velocity.X += num13 * 0.9f;
				}
				else
				{
					Velocity.X -= num13 * 0.9f;
				}
			}
		}
		else
		{
			float num18 = (float)Math.Sqrt(num15 * num15 + num16 * num16);
			if (SoundDelay == 0 && Type != 87 && Type != 159 && Type != 117)
			{
				int num19 = (int)(num18 * 0.025f);
				if (num19 < 10)
				{
					num19 = 10;
				}
				else if (num19 > 20)
				{
					num19 = 20;
				}
				SoundDelay = (short)num19;
				Main.PlaySound(15, XYWH.X, XYWH.Y);
			}
			float num20 = Math.Abs(num15);
			float num21 = Math.Abs(num16);
			float num22 = num12 / num18;
			num15 *= num22;
			num16 *= num22;
			if ((Type == 7 || Type == 13) && !Main.PlayerSet[Target].ZoneEvil)
			{
				bool flag5 = true;
				for (int num23 = 0; num23 < 8; num23++)
				{
					if (Main.PlayerSet[num23].Active != 0 && !Main.PlayerSet[num23].IsDead && Main.PlayerSet[num23].ZoneEvil)
					{
						flag5 = false;
						break;
					}
				}
				if (flag5)
				{
					if (Main.NetMode != 1 && XYWH.Y >> 4 > Main.RockLayer + Main.MaxTilesY >> 1)
					{
						Active = 0;
						int num24 = (int)AI0;
						while (num24 > 0 && num24 < 196 && Main.NPCSet[num24].Active != 0 && Main.NPCSet[num24].AIStyle == AIStyle)
						{
							int num25 = (int)Main.NPCSet[num24].AI0;
							Main.NPCSet[num24].Active = 0;
							Life = 0;
							NetMessage.CreateMessage1(23, num24);
							NetMessage.SendMessage();
							num24 = num25;
						}
						NetMessage.CreateMessage1(23, WhoAmI);
						NetMessage.SendMessage();
						return;
					}
					num15 = 0f;
					num16 = num12;
				}
			}
			bool flag6 = false;
			if (Type == 87 || Type == 159)
			{
				if (((Velocity.X > 0f && num15 < 0f) || (Velocity.X < 0f && num15 > 0f) || (Velocity.Y > 0f && num16 < 0f) || (Velocity.Y < 0f && num16 > 0f)) && Math.Abs(Velocity.X) + Math.Abs(Velocity.Y) > num13 * 0.5f && num18 < 300f)
				{
					flag6 = true;
					if (Math.Abs(Velocity.X) + Math.Abs(Velocity.Y) < num12)
					{
						Velocity.X *= 1.1f;
						Velocity.Y *= 1.1f;
					}
				}
				if (XYWH.Y > Main.PlayerSet[Target].XYWH.Y || Main.PlayerSet[Target].XYWH.Y > Main.WorldSurfacePixels || Main.PlayerSet[Target].IsDead)
				{
					flag6 = true;
					if (Math.Abs(Velocity.X) < num12 * 0.5f)
					{
						if (Velocity.X == 0f)
						{
							Velocity.X -= Direction;
						}
						Velocity.X *= 1.1f;
					}
					else if (Velocity.Y > 0f - num12)
					{
						Velocity.Y -= num13;
					}
				}
			}
			if (!flag6)
			{
				if ((Velocity.X > 0f && num15 > 0f) || (Velocity.X < 0f && num15 < 0f) || (Velocity.Y > 0f && num16 > 0f) || (Velocity.Y < 0f && num16 < 0f))
				{
					if (Velocity.X < num15)
					{
						Velocity.X += num13;
					}
					else if (Velocity.X > num15)
					{
						Velocity.X -= num13;
					}
					if (Velocity.Y < num16)
					{
						Velocity.Y += num13;
					}
					else if (Velocity.Y > num16)
					{
						Velocity.Y -= num13;
					}
					if ((double)Math.Abs(num16) < (double)num12 * 0.2 && ((Velocity.X > 0f && num15 < 0f) || (Velocity.X < 0f && num15 > 0f)))
					{
						if (Velocity.Y > 0f)
						{
							Velocity.Y += num13 * 2f;
						}
						else
						{
							Velocity.Y -= num13 * 2f;
						}
					}
					if ((double)Math.Abs(num15) < (double)num12 * 0.2 && ((Velocity.Y > 0f && num16 < 0f) || (Velocity.Y < 0f && num16 > 0f)))
					{
						if (Velocity.X > 0f)
						{
							Velocity.X += num13 * 2f;
						}
						else
						{
							Velocity.X -= num13 * 2f;
						}
					}
				}
				else if (num20 > num21)
				{
					if (Velocity.X < num15)
					{
						Velocity.X += num13 * 1.1f;
					}
					else if (Velocity.X > num15)
					{
						Velocity.X -= num13 * 1.1f;
					}
					if ((double)(Math.Abs(Velocity.X) + Math.Abs(Velocity.Y)) < (double)num12 * 0.5)
					{
						if (Velocity.Y > 0f)
						{
							Velocity.Y += num13;
						}
						else
						{
							Velocity.Y -= num13;
						}
					}
				}
				else
				{
					if (Velocity.Y < num16)
					{
						Velocity.Y += num13 * 1.1f;
					}
					else if (Velocity.Y > num16)
					{
						Velocity.Y -= num13 * 1.1f;
					}
					if ((double)(Math.Abs(Velocity.X) + Math.Abs(Velocity.Y)) < (double)num12 * 0.5)
					{
						if (Velocity.X > 0f)
						{
							Velocity.X += num13;
						}
						else
						{
							Velocity.X -= num13;
						}
					}
				}
			}
		}
		Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X) + 1.57f;
		if (Type != 7 && Type != 10 && Type != 13 && Type != 39 && Type != 95 && Type != 98 && Type != 117)
		{
			return;
		}
		if (flag2)
		{
			if (LocalAI0 != 1)
			{
				ShouldNetUpdate = true;
			}
			LocalAI0 = 1;
		}
		else
		{
			if (LocalAI0 != 0)
			{
				ShouldNetUpdate = true;
			}
			LocalAI0 = 0;
		}
		if (((Velocity.X > 0f && OldVelocity.X < 0f) || (Velocity.X < 0f && OldVelocity.X > 0f) || (Velocity.Y > 0f && OldVelocity.Y < 0f) || (Velocity.Y < 0f && OldVelocity.Y > 0f)) && !WasJustHit)
		{
			ShouldNetUpdate = true;
		}
	}

	private void TownsfolkAI()
	{
		if (Type == 46)
		{
			if (Target == 8)
			{
				TargetClosest();
			}
		}
		else if (Type == 107)
		{
			HasSavedGoblin = true;
		}
		else if (Type == 108)
		{
			HasSavedWizard = true;
		}
		else if (Type == 124)
		{
			HasSavedMech = true;
		}
		else if (Type == 142 && Main.NetMode != 1 && !Time.xMas)
		{
			StrikeNPC(9999, 0f, 0);
			NetMessage.SendNpcHurt(WhoAmI, 9999, 0.0, 0);
		}
		int num = XYWH.X + (Width >> 1) >> 4;
		int num2 = XYWH.Y + Height + 1 >> 4;
		bool flag = false;
		DirectionY = -1;
		Direction |= 1;
		for (int i = 0; i < 8; i++)
		{
			if (Main.PlayerSet[i].Active != 0 && Main.PlayerSet[i].TalkNPC == WhoAmI)
			{
				flag = true;
				if (AI0 != 0f)
				{
					ShouldNetUpdate = true;
				}
				AI0 = 0f;
				AI1 = 300f;
				AI2 = 100f;
				if (Main.PlayerSet[i].XYWH.X + 10 < XYWH.X + (Width >> 1))
				{
					Direction = -1;
				}
				else
				{
					Direction = 1;
				}
			}
		}
		if (AI3 > 0f && Active != 0)
		{
			Life = -1;
			HitEffect();
			Active = 0;
			if (Type == 37)
			{
				Main.PlaySound(15, XYWH.X, XYWH.Y, 0);
			}
		}
		if (Type == 37 && Main.NetMode != 1)
		{
			IsHomeless = false;
			HomeTileX = Main.DungeonX;
			HomeTileY = Main.DungeonY;
			if (HasDownedBoss3)
			{
				AI3 = 1f;
				ShouldNetUpdate = true;
			}
		}
		int j = HomeTileY;
		if (Main.NetMode != 1 && j > 0)
		{
			for (; !WorldGen.SolidTile(HomeTileX, j) && j < Main.MaxTilesY - 20; j++)
			{
			}
		}
		if (Main.NetMode != 1 && IsTownNPC && !IsHomeless && (num != HomeTileX || num2 != j) && (!Main.GameTime.DayTime || Main.TileDungeon[Main.TileSet[num, num2].Type]))
		{
			bool flag2 = true;
			Rectangle rectangle = new Rectangle
			{
				X = XYWH.X + (Width >> 1) - 960 - 62,
				Y = XYWH.Y + (Height >> 1) - 540 - 34,
				Width = 2044,
				Height = 1148
			};
			for (int k = 0; k < 8; k++)
			{
				if (Main.PlayerSet[k].Active != 0 && rectangle.Intersects(Main.PlayerSet[k].XYWH))
				{
					flag2 = false;
					break;
				}
			}
			if (flag2)
			{
				rectangle.X = HomeTileX * 16 + 8 - 960 - 62;
				rectangle.Y = j * 16 + 8 - 540 - 34;
				for (int l = 0; l < 8; l++)
				{
					if (Main.PlayerSet[l].Active != 0 && rectangle.Intersects(Main.PlayerSet[l].XYWH))
					{
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					if (Type == 37 || !Collision.SolidTiles(HomeTileX - 1, HomeTileX + 1, j - 3, j - 1))
					{
						Velocity.X = 0f;
						Velocity.Y = 0f;
						Position.X = (XYWH.X = (HomeTileX << 4) + 8 - (Width >> 1));
						Position.Y = (float)((j << 4) - Height) - 0.1f;
						XYWH.Y = (int)Position.Y;
						ShouldNetUpdate = true;
					}
					else
					{
						IsHomeless = true;
						WorldGen.QuickFindHome(WhoAmI);
					}
				}
			}
		}
		if (AI0 == 0f)
		{
			if (AI2 > 0f)
			{
				AI2--;
			}
			if (!Main.GameTime.DayTime && !flag && Type != 46)
			{
				if (Main.NetMode != 1)
				{
					if (num == HomeTileX && num2 == j)
					{
						if (Velocity.X != 0f)
						{
							ShouldNetUpdate = true;
						}
						if ((double)Velocity.X > 0.1)
						{
							Velocity.X -= 0.1f;
						}
						else if ((double)Velocity.X < -0.1)
						{
							Velocity.X += 0.1f;
						}
						else
						{
							Velocity.X = 0f;
						}
					}
					else if (!flag)
					{
						if (num > HomeTileX)
						{
							Direction = -1;
						}
						else
						{
							Direction = 1;
						}
						AI0 = 1f;
						AI1 = 200 + Main.Rand.Next(200);
						AI2 = 0f;
						ShouldNetUpdate = true;
					}
				}
			}
			else
			{
				if ((double)Velocity.X > 0.1)
				{
					Velocity.X -= 0.1f;
				}
				else if ((double)Velocity.X < -0.1)
				{
					Velocity.X += 0.1f;
				}
				else
				{
					Velocity.X = 0f;
				}
				if (Main.NetMode != 1)
				{
					if (AI1 > 0f)
					{
						AI1--;
					}
					if (AI1 <= 0f)
					{
						AI0 = 1f;
						AI1 = 200 + Main.Rand.Next(200);
						if (Type == 46)
						{
							AI1 += Main.Rand.Next(200, 400);
						}
						AI2 = 0f;
						ShouldNetUpdate = true;
					}
				}
			}
			if (Main.NetMode == 1 || (!Main.GameTime.DayTime && (num != HomeTileX || num2 != j)))
			{
				return;
			}
			if (num < HomeTileX - 25 || num > HomeTileX + 25)
			{
				if (AI2 == 0f)
				{
					if (num < HomeTileX - 50 && Direction == -1)
					{
						Direction = 1;
						ShouldNetUpdate = true;
					}
					else if (num > HomeTileX + 50 && Direction == 1)
					{
						Direction = -1;
						ShouldNetUpdate = true;
					}
				}
			}
			else if (Main.Rand.Next(80) == 0 && AI2 == 0f)
			{
				AI2 = 200f;
				Direction = (sbyte)(-Direction);
				ShouldNetUpdate = true;
			}
		}
		else
		{
			if (AI0 != 1f)
			{
				return;
			}
			if (Main.NetMode != 1 && !Main.GameTime.DayTime && num == HomeTileX && num2 == HomeTileY && Type != 46)
			{
				AI0 = 0f;
				AI1 = 200 + Main.Rand.Next(200);
				AI2 = 60f;
				ShouldNetUpdate = true;
				return;
			}
			if (Main.NetMode != 1 && !IsHomeless && !Main.TileDungeon[Main.TileSet[num, num2].Type] && (num < HomeTileX - 35 || num > HomeTileX + 35))
			{
				if (XYWH.X < HomeTileX << 4 && Direction == -1)
				{
					AI1 -= 5f;
				}
				else if (XYWH.X > HomeTileX << 4 && Direction == 1)
				{
					AI1 -= 5f;
				}
			}
			AI1--;
			if (AI1 <= 0f)
			{
				AI0 = 0f;
				AI1 = 300 + Main.Rand.Next(300);
				if (Type == 46)
				{
					AI1 -= Main.Rand.Next(100);
				}
				AI2 = 60f;
				ShouldNetUpdate = true;
			}
			if (HasClosedDoor)
			{
				int num3 = XYWH.X + (Width >> 1) >> 4;
				if (num3 > DoorX + 2 || num3 < DoorX - 2)
				{
					if (WorldGen.CloseDoor(DoorX, DoorY))
					{
						HasClosedDoor = false;
						NetMessage.CreateMessage2(24, DoorX, DoorY);
						NetMessage.SendMessage();
					}
					else
					{
						int num4 = XYWH.Y + (Height >> 1) >> 4;
						if (num3 > DoorX + 4 || num3 < DoorX - 4 || num4 > DoorY + 4 || num4 < DoorY - 4)
						{
							HasClosedDoor = false;
						}
					}
				}
			}
			if (Velocity.X < -1f || Velocity.X > 1f)
			{
				if (Velocity.Y == 0f)
				{
					Velocity.X *= 0.8f;
				}
			}
			else if ((double)Velocity.X < 1.15 && Direction == 1)
			{
				Velocity.X += 0.07f;
				if (Velocity.X > 1f)
				{
					Velocity.X = 1f;
				}
			}
			else if (Velocity.X > -1f && Direction == -1)
			{
				Velocity.X -= 0.07f;
				if (Velocity.X > 1f)
				{
					Velocity.X = 1f;
				}
			}
			if (Velocity.Y != 0f)
			{
				return;
			}
			if (Position.X == AI2)
			{
				Direction = (sbyte)(-Direction);
			}
			AI2 = -1f;
			int num5 = XYWH.X + (Width >> 1) + 15 * Direction >> 4;
			int num6 = XYWH.Y + Height - 16 >> 4;
			if (IsTownNPC && Main.TileSet[num5, num6 - 2].IsActive != 0 && Main.TileSet[num5, num6 - 2].Type == 10 && (Main.Rand.Next(10) == 0 || !Main.GameTime.DayTime))
			{
				if (Main.NetMode != 1)
				{
					int num7 = WorldGen.OpenDoor(num5, num6 - 2, Direction);
					if (num7 != 0)
					{
						HasClosedDoor = true;
						DoorX = (short)num5;
						DoorY = (short)(num6 - 2);
						NetMessage.CreateMessage3(19, num5, num6 - 2, num7);
						NetMessage.SendMessage();
						AI1 += 80f;
					}
					else
					{
						Direction = (sbyte)(-Direction);
					}
					ShouldNetUpdate = true;
				}
				return;
			}
			if ((Velocity.X < 0f && SpriteDirection == -1) || (Velocity.X > 0f && SpriteDirection == 1))
			{
				if (Main.TileSet[num5, num6 - 2].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[num5, num6 - 2].Type])
				{
					if ((Direction == 1 && !Collision.SolidTiles(num5 - 2, num5 - 1, num6 - 5, num6 - 1)) || (Direction == -1 && !Collision.SolidTiles(num5 + 1, num5 + 2, num6 - 5, num6 - 1)))
					{
						if (!Collision.SolidTiles(num5, num5, num6 - 5, num6 - 3))
						{
							Velocity.Y = -6f;
						}
						else
						{
							Direction = (sbyte)(-Direction);
						}
					}
					else
					{
						Direction = (sbyte)(-Direction);
					}
					ShouldNetUpdate = true;
				}
				else if (Main.TileSet[num5, num6 - 1].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[num5, num6 - 1].Type])
				{
					if ((Direction == 1 && !Collision.SolidTiles(num5 - 2, num5 - 1, num6 - 4, num6 - 1)) || (Direction == -1 && !Collision.SolidTiles(num5 + 1, num5 + 2, num6 - 4, num6 - 1)))
					{
						if (!Collision.SolidTiles(num5, num5, num6 - 4, num6 - 2))
						{
							Velocity.Y = -5f;
						}
						else
						{
							Direction = (sbyte)(-Direction);
						}
					}
					else
					{
						Direction = (sbyte)(-Direction);
					}
					ShouldNetUpdate = true;
				}
				else if (Main.TileSet[num5, num6].IsActive != 0 && Main.TileSolidNotSolidTop[Main.TileSet[num5, num6].Type])
				{
					if ((Direction == 1 && !Collision.SolidTiles(num5 - 2, num5, num6 - 3, num6 - 1)) || (Direction == -1 && !Collision.SolidTiles(num5, num5 + 2, num6 - 3, num6 - 1)))
					{
						Velocity.Y = -3.6f;
					}
					else
					{
						Direction = (sbyte)(-Direction);
					}
					ShouldNetUpdate = true;
				}
				if (num >= HomeTileX - 35 && num <= HomeTileX + 35 && (Main.TileSet[num5, num6 + 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[num5, num6 + 1].Type]) && (Main.TileSet[num5 - Direction, num6 + 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[num5 - Direction, num6 + 1].Type]) && (Main.TileSet[num5, num6 + 2].IsActive == 0 || !Main.TileSolid[Main.TileSet[num5, num6 + 2].Type]) && (Main.TileSet[num5 - Direction, num6 + 2].IsActive == 0 || !Main.TileSolid[Main.TileSet[num5 - Direction, num6 + 2].Type]) && (Main.TileSet[num5, num6 + 3].IsActive == 0 || !Main.TileSolid[Main.TileSet[num5, num6 + 3].Type]) && (Main.TileSet[num5 - Direction, num6 + 3].IsActive == 0 || !Main.TileSolid[Main.TileSet[num5 - Direction, num6 + 3].Type]) && (Main.TileSet[num5, num6 + 4].IsActive == 0 || !Main.TileSolid[Main.TileSet[num5, num6 + 4].Type]) && (Main.TileSet[num5 - Direction, num6 + 4].IsActive == 0 || !Main.TileSolid[Main.TileSet[num5 - Direction, num6 + 4].Type]) && Type != 46)
				{
					Direction = (sbyte)(-Direction);
					Velocity.X = 0f - Velocity.X;
					ShouldNetUpdate = true;
				}
				if (Velocity.Y < 0f)
				{
					AI2 = Position.X;
				}
			}
			if (Velocity.Y < 0f)
			{
				if (IsWet)
				{
					Velocity.Y *= 1.2f;
				}
				if (Type == 46)
				{
					Velocity.Y *= 1.2f;
				}
			}
		}
	}

	private unsafe void SorcererAI()
	{
		TargetClosest();
		Velocity.X *= 0.93f;
		if ((double)Velocity.X > -0.1 && (double)Velocity.X < 0.1)
		{
			Velocity.X = 0f;
		}
		if (AI0 == 0f)
		{
			AI0 = 500f;
		}
		if (AI2 != 0f && AI3 != 0f)
		{
			Main.PlaySound(2, XYWH.X, XYWH.Y, 8);
			for (int i = 0; i < 42; i++)
			{
				Dust* ptr;
				if (Type == 29 || Type == 45)
				{
					ptr = Main.DustSet.NewDust(27, ref XYWH, 0.0, 0.0, 100, default, Main.Rand.Next(1, 3));
					if (ptr == null)
					{
						break;
					}
					if (ptr->Scale > 1f)
					{
						ptr->NoGravity = true;
					}
				}
				else if (Type == 32)
				{
					ptr = Main.DustSet.NewDust(29, ref XYWH, 0.0, 0.0, 100, default, 2.5);
					if (ptr == null)
					{
						break;
					}
					ptr->NoGravity = true;
				}
				else
				{
					ptr = Main.DustSet.NewDust(6, ref XYWH, 0.0, 0.0, 100, default, 2.5);
					if (ptr == null)
					{
						break;
					}
					ptr->NoGravity = true;
				}
				ptr->Velocity.X *= 3f;
				ptr->Velocity.Y *= 3f;
			}
			Position.X = AI2 * 16f - (float)(Width >> 1) + 8f;
			Position.Y = AI3 * 16f - (float)(int)Height;
			XYWH.X = (int)Position.X;
			XYWH.Y = (int)Position.Y;
			Velocity.X = 0f;
			Velocity.Y = 0f;
			AI2 = 0f;
			AI3 = 0f;
			Main.PlaySound(2, XYWH.X, XYWH.Y, 8);
			for (int j = 0; j < 42; j++)
			{
				Dust* ptr2;
				if (Type == 29 || Type == 45)
				{
					ptr2 = Main.DustSet.NewDust(27, ref XYWH, 0.0, 0.0, 100, default, Main.Rand.Next(1, 3));
					if (ptr2 == null)
					{
						break;
					}
					if (ptr2->Scale > 1f)
					{
						ptr2->NoGravity = true;
					}
				}
				else if (Type == 32)
				{
					ptr2 = Main.DustSet.NewDust(29, ref XYWH, 0.0, 0.0, 100, default, 2.5);
					if (ptr2 == null)
					{
						break;
					}
					ptr2->NoGravity = true;
				}
				else
				{
					ptr2 = Main.DustSet.NewDust(6, ref XYWH, 0.0, 0.0, 100, default, 2.5);
					if (ptr2 == null)
					{
						break;
					}
					ptr2->NoGravity = true;
				}
				ptr2->Velocity.X *= 3f;
				ptr2->Velocity.Y *= 3f;
			}
		}
		AI0++;
		if (AI0 == 100f || AI0 == 200f || AI0 == 300f)
		{
			AI1 = 30f;
			ShouldNetUpdate = true;
		}
		else if (AI0 >= 650f && Main.NetMode != 1)
		{
			AI0 = 1f;
			int num = Main.PlayerSet[Target].XYWH.X >> 4;
			int num2 = Main.PlayerSet[Target].XYWH.Y >> 4;
			int num3 = XYWH.X >> 4;
			int num4 = XYWH.Y >> 4;
			int num5 = 20;
			int num6 = 0;
			bool flag = false;
			if (Math.Abs(XYWH.X - Main.PlayerSet[Target].XYWH.X) + Math.Abs(XYWH.Y - Main.PlayerSet[Target].XYWH.Y) > 2000)
			{
				num6 = 100;
				flag = true;
			}
			while (!flag && num6 < 100)
			{
				num6++;
				int num7 = Main.Rand.Next(num - num5, num + num5);
				for (int k = Main.Rand.Next(num2 - num5, num2 + num5); k < num2 + num5; k++)
				{
					if ((k < num2 - 4 || k > num2 + 4 || num7 < num - 4 || num7 > num + 4) && (k < num4 - 1 || k > num4 + 1 || num7 < num3 - 1 || num7 > num3 + 1) && Main.TileSet[num7, k].IsActive != 0)
					{
						bool flag2 = true;
						if (Type == 32 && Main.TileSet[num7, k - 1].WallType == 0)
						{
							flag2 = false;
						}
						else if (Main.TileSet[num7, k - 1].Lava != 0)
						{
							flag2 = false;
						}
						if (flag2 && Main.TileSolid[Main.TileSet[num7, k].Type] && !Collision.SolidTiles(num7 - 1, num7 + 1, k - 4, k - 1))
						{
							AI1 = 20f;
							AI2 = num7;
							AI3 = k;
							flag = true;
							break;
						}
					}
				}
			}
			ShouldNetUpdate = true;
		}
		if (AI1 > 0f)
		{
			AI1--;
			if (AI1 == 25f)
			{
				Main.PlaySound(2, XYWH.X, XYWH.Y, 8);
				if (Main.NetMode != 1)
				{
					if (Type == 29 || Type == 45)
					{
						NewNPC(XYWH.X + (Width >> 1), XYWH.Y - 8, 30);
					}
					else if (Type == 32)
					{
						NewNPC(XYWH.X + (Width >> 1), XYWH.Y - 8, 33);
					}
					else
					{
						NewNPC(XYWH.X + (Width >> 1) + Direction * 8, XYWH.Y + 20, 25);
					}
				}
			}
		}
		if (Type == 29 || Type == 45)
		{
			if (Main.Rand.Next(5) == 0)
			{
				Dust* ptr3 = Main.DustSet.NewDust(XYWH.X, XYWH.Y + 2, Width, Height, 27, Velocity.X * 0.2f, Velocity.Y * 0.2f, 100, default, 1.5);
				if (ptr3 != null)
				{
					ptr3->NoGravity = true;
					ptr3->Velocity.X *= 0.5f;
					ptr3->Velocity.Y = -2f;
				}
			}
		}
		else if (Type == 32)
		{
			if (Main.Rand.Next(2) == 0)
			{
				Dust* ptr4 = Main.DustSet.NewDust(XYWH.X, XYWH.Y + 2, Width, Height, 29, Velocity.X * 0.2f, Velocity.Y * 0.2f, 100, default, 2.0);
				if (ptr4 != null)
				{
					ptr4->NoGravity = true;
					ptr4->Velocity.X *= 1f;
					ptr4->Velocity.Y *= 1f;
				}
			}
		}
		else if (Main.Rand.Next(2) == 0)
		{
			Dust* ptr5 = Main.DustSet.NewDust(XYWH.X, XYWH.Y + 2, Width, Height, 6, Velocity.X * 0.2f, Velocity.Y * 0.2f, 100, default, 2.0);
			if (ptr5 != null)
			{
				ptr5->NoGravity = true;
				ptr5->Velocity.X *= 1f;
				ptr5->Velocity.Y *= 1f;
			}
		}
	}

	private unsafe void SphereAI()
	{
		if (Target == 8)
		{
			TargetClosest();
			float num = 6f;
			if (Type == 25)
			{
				num = 5f;
			}
			if (Type == 112)
			{
				num = 7f;
			}
			Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num2 = Main.PlayerSet[Target].Position.X + 10f - vector.X;
			float num3 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
			float num4 = (float)Math.Sqrt(num2 * num2 + num3 * num3);
			num4 = num / num4;
			Velocity.X = num2 * num4;
			Velocity.Y = num3 * num4;
		}
		if (Type == 112)
		{
			AI0++;
			if (AI0 > 3f)
			{
				AI0 = 3f;
			}
			if (AI0 == 2f)
			{
				Position.X += Velocity.X;
				Position.Y += Velocity.Y;
				XYWH.X = (int)Position.X;
				XYWH.Y = (int)Position.Y;
				Main.PlaySound(4, XYWH.X, XYWH.Y, 9);
				for (int i = 0; i < 16; i++)
				{
					Dust* ptr = Main.DustSet.NewDust(XYWH.X, XYWH.Y + 2, Width, Height, 18, 0.0, 0.0, 100, default, 1.7999999523162842);
					if (ptr == null)
					{
						break;
					}
					ptr->Velocity.X *= 1.3f;
					ptr->Velocity.Y *= 1.3f;
					ptr->Velocity.X += Velocity.X;
					ptr->Velocity.Y += Velocity.Y;
					ptr->NoGravity = true;
				}
			}
			if (Collision.SolidCollision(ref Position, Width, Height))
			{
				if (Main.NetMode != 1)
				{
					int num5 = XYWH.X + (Width >> 1) >> 4;
					int num6 = XYWH.Y + (Height >> 1) >> 4;
					int num7 = 8;
					for (int j = num5 - num7; j <= num5 + num7; j++)
					{
						for (int k = num6 - num7; k < num6 + num7; k++)
						{
							if ((double)(Math.Abs(j - num5) + Math.Abs(k - num6)) < (double)num7 * 0.5)
							{
								if (Main.TileSet[j, k].Type == 2)
								{
									Main.TileSet[j, k].Type = 23;
									WorldGen.SquareTileFrame(j, k);
									NetMessage.SendTile(j, k);
								}
								else if (Main.TileSet[j, k].Type == 1)
								{
									Main.TileSet[j, k].Type = 25;
									WorldGen.SquareTileFrame(j, k);
									NetMessage.SendTile(j, k);
								}
								else if (Main.TileSet[j, k].Type == 53)
								{
									Main.TileSet[j, k].Type = 112;
									WorldGen.SquareTileFrame(j, k);
									NetMessage.SendTile(j, k);
								}
								else if (Main.TileSet[j, k].Type == 109)
								{
									Main.TileSet[j, k].Type = 23;
									WorldGen.SquareTileFrame(j, k);
									NetMessage.SendTile(j, k);
								}
								else if (Main.TileSet[j, k].Type == 117)
								{
									Main.TileSet[j, k].Type = 25;
									WorldGen.SquareTileFrame(j, k);
									NetMessage.SendTile(j, k);
								}
								else if (Main.TileSet[j, k].Type == 116)
								{
									Main.TileSet[j, k].Type = 112;
									WorldGen.SquareTileFrame(j, k);
									NetMessage.SendTile(j, k);
								}
							}
						}
					}
				}
				StrikeNPC(999, 0f, 0);
			}
		}
		if (TimeLeft > 100)
		{
			TimeLeft = 100;
		}
		for (int l = 0; l < 2; l++)
		{
			Dust* ptr2;
			if (Type == 30)
			{
				ptr2 = Main.DustSet.NewDust(XYWH.X, XYWH.Y + 2, Width, Height, 27, Velocity.X * 0.2f, Velocity.Y * 0.2f, 100, default, 2.0);
				if (ptr2 == null)
				{
					break;
				}
			}
			else if (Type == 33)
			{
				ptr2 = Main.DustSet.NewDust(XYWH.X, XYWH.Y + 2, Width, Height, 29, Velocity.X * 0.2f, Velocity.Y * 0.2f, 100, default, 2.0);
				if (ptr2 == null)
				{
					break;
				}
			}
			else if (Type == 112)
			{
				ptr2 = Main.DustSet.NewDust(XYWH.X, XYWH.Y + 2, Width, Height, 18, Velocity.X * 0.1f, Velocity.Y * 0.1f, 80, default, 1.2999999523162842);
				if (ptr2 == null)
				{
					break;
				}
			}
			else
			{
				Lighting.AddLight(XYWH.X + (Width >> 1) >> 4, XYWH.Y + (Height >> 1) >> 4, new Vector3(1f, 0.3f, 0.1f));
				ptr2 = Main.DustSet.NewDust(XYWH.X, XYWH.Y + 2, Width, Height, 6, Velocity.X * 0.2f, Velocity.Y * 0.2f, 100, default, 2.0);
			}
			if (ptr2 != null)
			{
				ptr2->NoGravity = true;
				ptr2->Velocity.X *= 0.3f;
				ptr2->Velocity.Y *= 0.3f;
				if (Type == 30)
				{
					ptr2->Velocity.X -= Velocity.X * 0.2f;
					ptr2->Velocity.Y -= Velocity.Y * 0.2f;
				}
			}
		}
		Rotation += 0.4f * (float)Direction;
	}

	private void SkullHeadAI()
	{
		float num = 1f;
		float num2 = 0.011f;
		TargetClosest();
		Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
		float num3 = Main.PlayerSet[Target].Position.X + 10f - vector.X;
		float num4 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
		float num5 = (float)Math.Sqrt(num3 * num3 + num4 * num4);
		float num6 = num5;
		AI1++;
		if (AI1 > 600f)
		{
			num2 *= 8f;
			num = 4f;
			if (AI1 > 650f)
			{
				AI1 = 0f;
			}
		}
		else if (num6 < 250f)
		{
			AI0 += 0.9f;
			if (AI0 > 0f)
			{
				Velocity.Y += 0.019f;
			}
			else
			{
				Velocity.Y -= 0.019f;
			}
			if (AI0 < -100f || AI0 > 100f)
			{
				Velocity.X += 0.019f;
			}
			else
			{
				Velocity.X -= 0.019f;
			}
			if (AI0 > 200f)
			{
				AI0 = -200f;
			}
		}
		if (num6 > 350f)
		{
			num = 5f;
			num2 = 0.3f;
		}
		else if (num6 > 300f)
		{
			num = 3f;
			num2 = 0.2f;
		}
		else if (num6 > 250f)
		{
			num = 1.5f;
			num2 = 0.1f;
		}
		num5 = num / num5;
		num3 *= num5;
		num4 *= num5;
		if (Main.PlayerSet[Target].IsDead)
		{
			num3 = (float)Direction * num * 0.5f;
			num4 = num * -0.5f;
		}
		if (Velocity.X < num3)
		{
			Velocity.X += num2;
		}
		else if (Velocity.X > num3)
		{
			Velocity.X -= num2;
		}
		if (Velocity.Y < num4)
		{
			Velocity.Y += num2;
		}
		else if (Velocity.Y > num4)
		{
			Velocity.Y -= num2;
		}
		if (num3 > 0f)
		{
			SpriteDirection = -1;
			Rotation = (float)Math.Atan2(num4, num3);
		}
		else if (num3 < 0f)
		{
			SpriteDirection = 1;
			Rotation = (float)Math.Atan2(num4, num3) + 3.14f;
		}
	}

	private unsafe void SkeletronAI()
	{
		if (AI0 == 0f && Main.NetMode != 1)
		{
			TargetClosest();
			AI0 = 1f;
			if (Type != 68)
			{
				int num = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + (Height >> 1), 36, WhoAmI);
				Main.NPCSet[num].AI0 = -1f;
				Main.NPCSet[num].AI1 = WhoAmI;
				Main.NPCSet[num].Target = Target;
				Main.NPCSet[num].ShouldNetUpdate = true;
				num = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + (Height >> 1), 36, WhoAmI);
				Main.NPCSet[num].AI0 = 1f;
				Main.NPCSet[num].AI1 = WhoAmI;
				Main.NPCSet[num].AI3 = 150f;
				Main.NPCSet[num].Target = Target;
				Main.NPCSet[num].ShouldNetUpdate = true;
			}
		}
		if (Type == 68 && AI1 != 3f && AI1 != 2f)
		{
			Main.PlaySound(15, XYWH.X, XYWH.Y, 0);
			AI1 = 2f;
		}
		if (Main.PlayerSet[Target].IsDead || Math.Abs(XYWH.X - Main.PlayerSet[Target].XYWH.X) > 2000 || Math.Abs(XYWH.Y - Main.PlayerSet[Target].XYWH.Y) > 2000)
		{
			TargetClosest();
			if (Main.PlayerSet[Target].IsDead || Math.Abs(XYWH.X - Main.PlayerSet[Target].XYWH.X) > 2000 || Math.Abs(XYWH.Y - Main.PlayerSet[Target].XYWH.Y) > 2000)
			{
				AI1 = 3f;
			}
		}
		if (Main.GameTime.DayTime && AI1 != 3f && AI1 != 2f)
		{
			AI1 = 2f;
			Main.PlaySound(15, XYWH.X, XYWH.Y, 0);
		}
		if (AI1 == 0f)
		{
			Defense = 10;
			AI2++;
			if (AI2 >= 800f)
			{
				AI2 = 0f;
				AI1 = 1f;
				TargetClosest();
				ShouldNetUpdate = true;
			}
			Rotation = Velocity.X * (1f / 15f);
			if (XYWH.Y > Main.PlayerSet[Target].XYWH.Y - 250)
			{
				if (Velocity.Y > 0f)
				{
					Velocity.Y *= 0.98f;
				}
				Velocity.Y -= 0.02f;
				if (Velocity.Y > 2f)
				{
					Velocity.Y = 2f;
				}
			}
			else if (XYWH.Y < Main.PlayerSet[Target].XYWH.Y - 250)
			{
				if (Velocity.Y < 0f)
				{
					Velocity.Y *= 0.98f;
				}
				Velocity.Y += 0.02f;
				if (Velocity.Y < -2f)
				{
					Velocity.Y = -2f;
				}
			}
			if (XYWH.X + (Width >> 1) > Main.PlayerSet[Target].XYWH.X + 10)
			{
				if (Velocity.X > 0f)
				{
					Velocity.X *= 0.98f;
				}
				Velocity.X -= 0.05f;
				if (Velocity.X > 8f)
				{
					Velocity.X = 8f;
				}
			}
			else if (XYWH.X + (Width >> 1) < Main.PlayerSet[Target].XYWH.X + 10)
			{
				if (Velocity.X < 0f)
				{
					Velocity.X *= 0.98f;
				}
				Velocity.X += 0.05f;
				if (Velocity.X < -8f)
				{
					Velocity.X = -8f;
				}
			}
		}
		else if (AI1 == 1f)
		{
			Defense = 0;
			AI2++;
			if (AI2 == 2f)
			{
				Main.PlaySound(15, XYWH.X, XYWH.Y, 0);
			}
			if (AI2 >= 400f)
			{
				AI2 = 0f;
				AI1 = 0f;
			}
			Rotation += (float)Direction * 0.3f;
			Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num2 = Main.PlayerSet[Target].Position.X + 10f - vector.X;
			float num3 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
			float num4 = (float)Math.Sqrt(num2 * num2 + num3 * num3);
			num4 = 1.5f / num4;
			Velocity.X = num2 * num4;
			Velocity.Y = num3 * num4;
		}
		else if (AI1 == 2f)
		{
			Damage = 9999;
			Defense = 9999;
			Rotation += (float)Direction * 0.3f;
			Vector2 vector2 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num5 = Main.PlayerSet[Target].Position.X + 10f - vector2.X;
			float num6 = Main.PlayerSet[Target].Position.Y + 21f - vector2.Y;
			float num7 = (float)Math.Sqrt(num5 * num5 + num6 * num6);
			num7 = 8f / num7;
			Velocity.X = num5 * num7;
			Velocity.Y = num6 * num7;
		}
		else if (AI1 == 3f)
		{
			Velocity.Y += 0.1f;
			if (Velocity.Y < 0f)
			{
				Velocity.Y *= 0.95f;
			}
			Velocity.X *= 0.95f;
			if (TimeLeft > 500)
			{
				TimeLeft = 500;
			}
		}
		if (AI1 == 2f || AI1 == 3f || Type == 68)
		{
			return;
		}
		Dust* ptr = Main.DustSet.NewDust(XYWH.X + (Width >> 1) - 15 - (int)(Velocity.X * 5f), XYWH.Y + Height - 2, 30, 10, 5, Velocity.X * -0.2f, 3.0, 0, default, 2.0);
		if (ptr != null)
		{
			ptr->NoGravity = true;
			ptr->Velocity.X *= 1.3f;
			ptr->Velocity.X += Velocity.X * 0.4f;
			ptr->Velocity.Y += 2f + Velocity.Y;
		}
		for (int i = 0; i < 2; i++)
		{
			ptr = Main.DustSet.NewDust(XYWH.X, XYWH.Y + 120, Width, 60, 5, Velocity.X, Velocity.Y, 0, default, 2.0);
			if (ptr != null)
			{
				ptr->NoGravity = true;
				ptr->Velocity -= Velocity;
				ptr->Velocity.Y += 5f;
				continue;
			}
			break;
		}
	}

	private void SkeletronHandAI()
	{
		SpriteDirection = (sbyte)(0f - AI0);
		if (Main.NPCSet[(int)AI1].Active == 0 || Main.NPCSet[(int)AI1].AIStyle != 11)
		{
			AI2 += 10f;
			if (AI2 > 50f || Main.NetMode != 2)
			{
				Life = -1;
				HitEffect();
				Active = 0;
				return;
			}
		}
		if (AI2 == 0f || AI2 == 3f)
		{
			if (Main.NPCSet[(int)AI1].AI1 == 3f && TimeLeft > 10)
			{
				TimeLeft = 10;
			}
			if (Main.NPCSet[(int)AI1].AI1 != 0f)
			{
				if (XYWH.Y > Main.NPCSet[(int)AI1].XYWH.Y - 100)
				{
					if (Velocity.Y > 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y -= 0.07f;
					if (Velocity.Y > 6f)
					{
						Velocity.Y = 6f;
					}
				}
				else if (XYWH.Y < Main.NPCSet[(int)AI1].XYWH.Y - 100)
				{
					if (Velocity.Y < 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y += 0.07f;
					if (Velocity.Y < -6f)
					{
						Velocity.Y = -6f;
					}
				}
				if (XYWH.X + (Width >> 1) > Main.NPCSet[(int)AI1].XYWH.X + (Main.NPCSet[(int)AI1].Width >> 1) - 120 * (int)AI0)
				{
					if (Velocity.X > 0f)
					{
						Velocity.X *= 0.96f;
					}
					Velocity.X -= 0.1f;
					if (Velocity.X > 8f)
					{
						Velocity.X = 8f;
					}
				}
				else if (XYWH.X + (Width >> 1) < Main.NPCSet[(int)AI1].XYWH.X + (Main.NPCSet[(int)AI1].Width >> 1) - 120 * (int)AI0)
				{
					if (Velocity.X < 0f)
					{
						Velocity.X *= 0.96f;
					}
					Velocity.X += 0.1f;
					if (Velocity.X < -8f)
					{
						Velocity.X = -8f;
					}
				}
			}
			else
			{
				AI3++;
				if (AI3 >= 300f)
				{
					AI2++;
					AI3 = 0f;
					ShouldNetUpdate = true;
				}
				if (XYWH.Y > Main.NPCSet[(int)AI1].XYWH.Y + 230)
				{
					if (Velocity.Y > 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y -= 0.04f;
					if (Velocity.Y > 3f)
					{
						Velocity.Y = 3f;
					}
				}
				else if (XYWH.Y < Main.NPCSet[(int)AI1].XYWH.Y + 230)
				{
					if (Velocity.Y < 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y += 0.04f;
					if (Velocity.Y < -3f)
					{
						Velocity.Y = -3f;
					}
				}
				if (XYWH.X + (Width >> 1) > Main.NPCSet[(int)AI1].XYWH.X + (Main.NPCSet[(int)AI1].Width >> 1) - 200 * (int)AI0)
				{
					if (Velocity.X > 0f)
					{
						Velocity.X *= 0.96f;
					}
					Velocity.X -= 0.07f;
					if (Velocity.X > 8f)
					{
						Velocity.X = 8f;
					}
				}
				else if (XYWH.X + (Width >> 1) < Main.NPCSet[(int)AI1].XYWH.X + (Main.NPCSet[(int)AI1].Width >> 1) - 200 * (int)AI0)
				{
					if (Velocity.X < 0f)
					{
						Velocity.X *= 0.96f;
					}
					Velocity.X += 0.07f;
					if (Velocity.X < -8f)
					{
						Velocity.X = -8f;
					}
				}
			}
			Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num = Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 200f * AI0 - vector.X;
			float num2 = Main.NPCSet[(int)AI1].Position.Y + 230f - vector.Y;
			Rotation = (float)Math.Atan2(num2, num) + 1.57f;
		}
		else if (AI2 == 1f)
		{
			Vector2 vector2 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num3 = Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 200f * AI0 - vector2.X;
			float num4 = Main.NPCSet[(int)AI1].Position.Y + 230f - vector2.Y;
			Rotation = (float)Math.Atan2(num4, num3) + 1.57f;
			Velocity.X *= 0.95f;
			Velocity.Y -= 0.1f;
			if (Velocity.Y < -8f)
			{
				Velocity.Y = -8f;
			}
			if (XYWH.Y < Main.NPCSet[(int)AI1].XYWH.Y - 200)
			{
				TargetClosest();
				AI2 = 2f;
				vector2 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
				num3 = Main.PlayerSet[Target].Position.X + 10f - vector2.X;
				num4 = Main.PlayerSet[Target].Position.Y + 21f - vector2.Y;
				float num5 = (float)Math.Sqrt(num3 * num3 + num4 * num4);
				num5 = 18f / num5;
				Velocity.X = num3 * num5;
				Velocity.Y = num4 * num5;
				ShouldNetUpdate = true;
			}
		}
		else if (AI2 == 2f)
		{
			if (XYWH.Y > Main.PlayerSet[Target].XYWH.Y || Velocity.Y < 0f)
			{
				AI2 = 3f;
			}
		}
		else if (AI2 == 4f)
		{
			Vector2 vector3 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num6 = Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 200f * AI0 - vector3.X;
			float num7 = Main.NPCSet[(int)AI1].Position.Y + 230f - vector3.Y;
			Rotation = (float)Math.Atan2(num7, num6) + 1.57f;
			Velocity.Y *= 0.95f;
			Velocity.X += 0.1f * (0f - AI0);
			if (Velocity.X < -8f)
			{
				Velocity.X = -8f;
			}
			if (Velocity.X > 8f)
			{
				Velocity.X = 8f;
			}
			if (XYWH.X + (Width >> 1) < Main.NPCSet[(int)AI1].XYWH.X + (Main.NPCSet[(int)AI1].Width >> 1) - 500 || XYWH.X + (Width >> 1) > Main.NPCSet[(int)AI1].XYWH.X + (Main.NPCSet[(int)AI1].Width >> 1) + 500)
			{
				TargetClosest();
				AI2 = 5f;
				vector3 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
				num6 = Main.PlayerSet[Target].Position.X + 10f - vector3.X;
				num7 = Main.PlayerSet[Target].Position.Y + 21f - vector3.Y;
				float num8 = (float)Math.Sqrt(num6 * num6 + num7 * num7);
				num8 = 17f / num8;
				Velocity.X = num6 * num8;
				Velocity.Y = num7 * num8;
				ShouldNetUpdate = true;
			}
		}
		else if (AI2 == 5f && ((Velocity.X > 0f && XYWH.X + (Width >> 1) > Main.PlayerSet[Target].XYWH.X + 10) || (Velocity.X < 0f && XYWH.X + (Width >> 1) < Main.PlayerSet[Target].XYWH.X + 10)))
		{
			AI2 = 0f;
		}
	}

	private void PlantAI()
	{
		if (Main.TileSet[(int)AI0, (int)AI1].IsActive == 0)
		{
			Life = -1;
			HitEffect();
			Active = 0;
			return;
		}
		TargetClosest();
		float num = 0.035f;
		float num2 = 150f;
		if (Type == 43)
		{
			num2 = 250f;
		}
		if (Type == 101)
		{
			num2 = 175f;
		}
		AI2++;
		if (AI2 > 300f)
		{
			num2 = (int)(num2 * 1.3f);
			if (AI2 > 450f)
			{
				AI2 = 0f;
			}
		}
		Vector2 vector = new Vector2(AI0 * 16f + 8f, AI1 * 16f + 8f);
		float num3 = Main.PlayerSet[Target].Position.X + 10f - (float)(Width >> 1) - vector.X;
		float num4 = Main.PlayerSet[Target].Position.Y + 21f - (float)(Height >> 1) - vector.Y;
		float num5 = num3 * num3 + num4 * num4;
		if (num5 > num2 * num2)
		{
			num5 = num2 / (float)Math.Sqrt(num5);
			num3 *= num5;
			num4 *= num5;
		}
		if (Position.X < AI0 * 16f + 8f + num3)
		{
			Velocity.X += num;
			if (Velocity.X < 0f && num3 > 0f)
			{
				Velocity.X += num * 1.5f;
			}
		}
		else if (Position.X > AI0 * 16f + 8f + num3)
		{
			Velocity.X -= num;
			if (Velocity.X > 0f && num3 < 0f)
			{
				Velocity.X -= num * 1.5f;
			}
		}
		if (Position.Y < AI1 * 16f + 8f + num4)
		{
			Velocity.Y += num;
			if (Velocity.Y < 0f && num4 > 0f)
			{
				Velocity.Y += num * 1.5f;
			}
		}
		else if (Position.Y > AI1 * 16f + 8f + num4)
		{
			Velocity.Y -= num;
			if (Velocity.Y > 0f && num4 < 0f)
			{
				Velocity.Y -= num * 1.5f;
			}
		}
		if (Type == 43)
		{
			if (Velocity.X > 3f)
			{
				Velocity.X = 3f;
			}
			else if (Velocity.X < -3f)
			{
				Velocity.X = -3f;
			}
			if (Velocity.Y > 3f)
			{
				Velocity.Y = 3f;
			}
			else if (Velocity.Y < -3f)
			{
				Velocity.Y = -3f;
			}
		}
		else
		{
			if (Velocity.X > 2f)
			{
				Velocity.X = 2f;
			}
			else if (Velocity.X < -2f)
			{
				Velocity.X = -2f;
			}
			if (Velocity.Y > 2f)
			{
				Velocity.Y = 2f;
			}
			else if (Velocity.Y < -2f)
			{
				Velocity.Y = -2f;
			}
		}
		if (num3 > 0f)
		{
			SpriteDirection = 1;
			Rotation = (float)Math.Atan2(num4, num3);
		}
		else if (num3 < 0f)
		{
			SpriteDirection = -1;
			Rotation = (float)Math.Atan2(num4, num3) + 3.14f;
		}
		if (HasXCollision)
		{
			ShouldNetUpdate = true;
			Velocity.X = OldVelocity.X * -0.7f;
			if (Velocity.X > 0f && Velocity.X < 2f)
			{
				Velocity.X = 2f;
			}
			else if (Velocity.X < 0f && Velocity.X > -2f)
			{
				Velocity.X = -2f;
			}
		}
		if (HasYCollision)
		{
			ShouldNetUpdate = true;
			Velocity.Y = OldVelocity.Y * -0.7f;
			if (Velocity.Y > 0f && Velocity.Y < 2f)
			{
				Velocity.Y = 2f;
			}
			else if (Velocity.Y < 0f && Velocity.Y > -2f)
			{
				Velocity.Y = -2f;
			}
		}
		if (Main.NetMode == 1 || Type != 101 || Main.PlayerSet[Target].IsDead)
		{
			return;
		}
		if (WasJustHit)
		{
			LocalAI0 = 0;
		}
		if (++LocalAI0 < 120)
		{
			return;
		}
		if (!Collision.SolidCollision(ref Position, Width, Height) && Collision.CanHit(ref XYWH, ref Main.PlayerSet[Target].XYWH))
		{
			vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			num3 = Main.PlayerSet[Target].Position.X + 10f - vector.X + (float)Main.Rand.Next(-10, 11);
			num4 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y + (float)Main.Rand.Next(-10, 11);
			num5 = (float)Math.Sqrt(num3 * num3 + num4 * num4);
			num5 = 10f / num5;
			num3 *= num5;
			num4 *= num5;
			int num6 = Projectile.NewProjectile(vector.X, vector.Y, num3, num4, 96, 22, 0f);
			if (num6 >= 0)
			{
				Main.ProjectileSet[num6].timeLeft = 300;
			}
			LocalAI0 = 0;
		}
		else
		{
			LocalAI0 = 100;
		}
	}

	private unsafe void FlyerAI()
	{
		if (Type == 60)
		{
			Dust* ptr = Main.DustSet.NewDust(6, ref XYWH, Velocity.X * 0.2f, Velocity.Y * 0.2f, 100, default, 2.0);
			if (ptr != null)
			{
				ptr->NoGravity = true;
			}
		}
		HasNoGravity = true;
		if (HasXCollision)
		{
			Velocity.X = OldVelocity.X * -0.5f;
			if (Direction == -1 && Velocity.X > 0f && Velocity.X < 2f)
			{
				Velocity.X = 2f;
			}
			else if (Direction == 1 && Velocity.X < 0f && Velocity.X > -2f)
			{
				Velocity.X = -2f;
			}
		}
		if (HasYCollision)
		{
			Velocity.Y = OldVelocity.Y * -0.5f;
			if (Velocity.Y > 0f && Velocity.Y < 1f)
			{
				Velocity.Y = 1f;
			}
			else if (Velocity.Y < 0f && Velocity.Y > -1f)
			{
				Velocity.Y = -1f;
			}
		}
		TargetClosest();
		if (Direction == -1 && Velocity.X > -4f)
		{
			Velocity.X -= 0.1f;
			if (Velocity.X > 4f)
			{
				Velocity.X -= 0.1f;
			}
			else if (Velocity.X > 0f)
			{
				Velocity.X += 0.05f;
			}
			else if (Velocity.X < -4f)
			{
				Velocity.X = -4f;
			}
		}
		else if (Direction == 1 && Velocity.X < 4f)
		{
			Velocity.X += 0.1f;
			if (Velocity.X < -4f)
			{
				Velocity.X += 0.1f;
			}
			else if (Velocity.X < 0f)
			{
				Velocity.X -= 0.05f;
			}
			else if (Velocity.X > 4f)
			{
				Velocity.X = 4f;
			}
		}
		if (DirectionY == -1 && (double)Velocity.Y > -1.5)
		{
			Velocity.Y -= 0.04f;
			if ((double)Velocity.Y > 1.5)
			{
				Velocity.Y -= 0.05f;
			}
			else if (Velocity.Y > 0f)
			{
				Velocity.Y += 0.03f;
			}
			else if ((double)Velocity.Y < -1.5)
			{
				Velocity.Y = -1.5f;
			}
		}
		else if (DirectionY == 1 && (double)Velocity.Y < 1.5)
		{
			Velocity.Y += 0.04f;
			if ((double)Velocity.Y < -1.5)
			{
				Velocity.Y += 0.05f;
			}
			else if (Velocity.Y < 0f)
			{
				Velocity.Y -= 0.03f;
			}
			else if ((double)Velocity.Y > 1.5)
			{
				Velocity.Y = 1.5f;
			}
		}
		if (Type == 49 || Type == 51 || Type == 60 || Type == 62 || Type == 165 || Type == 66 || Type == 93 || Type == 137)
		{
			if (IsWet)
			{
				if (Velocity.Y > 0f)
				{
					Velocity.Y *= 0.95f;
				}
				Velocity.Y -= 0.5f;
				if (Velocity.Y < -4f)
				{
					Velocity.Y = -4f;
				}
				TargetClosest();
			}
			if (Type == 60)
			{
				if (Direction == -1 && Velocity.X > -4f)
				{
					Velocity.X -= 0.1f;
					if (Velocity.X > 4f)
					{
						Velocity.X -= 0.07f;
					}
					else if (Velocity.X > 0f)
					{
						Velocity.X += 0.03f;
					}
					if (Velocity.X < -4f)
					{
						Velocity.X = -4f;
					}
				}
				else if (Direction == 1 && Velocity.X < 4f)
				{
					Velocity.X += 0.1f;
					if (Velocity.X < -4f)
					{
						Velocity.X += 0.07f;
					}
					else if (Velocity.X < 0f)
					{
						Velocity.X -= 0.03f;
					}
					if (Velocity.X > 4f)
					{
						Velocity.X = 4f;
					}
				}
				if (DirectionY == -1 && (double)Velocity.Y > -1.5)
				{
					Velocity.Y -= 0.04f;
					if ((double)Velocity.Y > 1.5)
					{
						Velocity.Y -= 0.03f;
					}
					else if (Velocity.Y > 0f)
					{
						Velocity.Y += 0.02f;
					}
					if ((double)Velocity.Y < -1.5)
					{
						Velocity.Y = -1.5f;
					}
				}
				else if (DirectionY == 1 && (double)Velocity.Y < 1.5)
				{
					Velocity.Y += 0.04f;
					if ((double)Velocity.Y < -1.5)
					{
						Velocity.Y += 0.03f;
					}
					else if (Velocity.Y < 0f)
					{
						Velocity.Y -= 0.02f;
					}
					if ((double)Velocity.Y > 1.5)
					{
						Velocity.Y = 1.5f;
					}
				}
			}
			else
			{
				if (Direction == -1 && Velocity.X > -4f)
				{
					Velocity.X -= 0.1f;
					if (Velocity.X > 4f)
					{
						Velocity.X -= 0.1f;
					}
					else if (Velocity.X > 0f)
					{
						Velocity.X += 0.05f;
					}
					if (Velocity.X < -4f)
					{
						Velocity.X = -4f;
					}
				}
				else if (Direction == 1 && Velocity.X < 4f)
				{
					Velocity.X += 0.1f;
					if (Velocity.X < -4f)
					{
						Velocity.X += 0.1f;
					}
					else if (Velocity.X < 0f)
					{
						Velocity.X -= 0.05f;
					}
					if (Velocity.X > 4f)
					{
						Velocity.X = 4f;
					}
				}
				if (DirectionY == -1 && (double)Velocity.Y > -1.5)
				{
					Velocity.Y -= 0.04f;
					if ((double)Velocity.Y > 1.5)
					{
						Velocity.Y -= 0.05f;
					}
					else if (Velocity.Y > 0f)
					{
						Velocity.Y += 0.03f;
					}
					if ((double)Velocity.Y < -1.5)
					{
						Velocity.Y = -1.5f;
					}
				}
				else if (DirectionY == 1 && (double)Velocity.Y < 1.5)
				{
					Velocity.Y += 0.04f;
					if ((double)Velocity.Y < -1.5)
					{
						Velocity.Y += 0.05f;
					}
					else if (Velocity.Y < 0f)
					{
						Velocity.Y -= 0.03f;
					}
					if ((double)Velocity.Y > 1.5)
					{
						Velocity.Y = 1.5f;
					}
				}
			}
		}
		AI1++;
		if (AI1 > 200f)
		{
			if (!Main.PlayerSet[Target].IsWet && Collision.CanHit(ref XYWH, ref Main.PlayerSet[Target].XYWH))
			{
				AI1 = 0f;
			}
			float num = 0.2f;
			float num2 = 0.1f;
			float num3 = 4f;
			float num4 = 1.5f;
			if (Type == 48 || Type == 62 || Type == 165 || Type == 66)
			{
				num = 0.12f;
				num2 = 0.07f;
				num3 = 3f;
				num4 = 1.25f;
			}
			if (AI1 > 1000f)
			{
				AI1 = 0f;
			}
			AI2++;
			if (AI2 > 0f)
			{
				if (Velocity.Y < num4)
				{
					Velocity.Y += num2;
				}
			}
			else if (Velocity.Y > 0f - num4)
			{
				Velocity.Y -= num2;
			}
			if (AI2 < -150f || AI2 > 150f)
			{
				if (Velocity.X < num3)
				{
					Velocity.X += num;
				}
			}
			else if (Velocity.X > 0f - num3)
			{
				Velocity.X -= num;
			}
			if (AI2 > 300f)
			{
				AI2 = -300f;
			}
		}
		if (Main.NetMode == 1)
		{
			return;
		}
		if (Type == 48)
		{
			AI0++;
			if (AI0 == 30f || AI0 == 60f || AI0 == 90f)
			{
				if (Collision.CanHit(ref XYWH, ref Main.PlayerSet[Target].XYWH))
				{
					Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
					float num5 = Main.PlayerSet[Target].Position.X + 10f - vector.X + (float)Main.Rand.Next(-100, 101);
					float num6 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y + (float)Main.Rand.Next(-100, 101);
					float num7 = (float)Math.Sqrt(num5 * num5 + num6 * num6);
					num7 = 6f / num7;
					num5 *= num7;
					num6 *= num7;
					int num8 = Projectile.NewProjectile(vector.X, vector.Y, num5, num6, 38, 15, 0f);
					if (num8 >= 0)
					{
						Main.ProjectileSet[num8].timeLeft = 300;
					}
				}
			}
			else if (AI0 >= (float)(400 + Main.Rand.Next(400)))
			{
				AI0 = 0f;
			}
		}
		else
		{
			if (Type != 62 && Type != 165 && Type != 66)
			{
				return;
			}
			AI0++;
			if (AI0 == 20f || AI0 == 40f || AI0 == 60f || AI0 == 80f)
			{
				if (Collision.CanHit(ref XYWH, ref Main.PlayerSet[Target].XYWH))
				{
					Vector2 vector2 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
					float num9 = Main.PlayerSet[Target].Position.X + 10f - vector2.X + (float)Main.Rand.Next(-100, 101);
					float num10 = Main.PlayerSet[Target].Position.Y + 21f - vector2.Y + (float)Main.Rand.Next(-100, 101);
					float num11 = (float)Math.Sqrt(num9 * num9 + num10 * num10);
					num11 = 0.2f / num11;
					num9 *= num11;
					num10 *= num11;
					int num12 = Projectile.NewProjectile(vector2.X, vector2.Y, num9, num10, 44, 21, 0f);
					if (num12 >= 0)
					{
						Main.ProjectileSet[num12].timeLeft = 300;
					}
				}
			}
			else if (AI0 >= (float)(300 + Main.Rand.Next(300)))
			{
				AI0 = 0f;
			}
		}
	}

	private unsafe void KingSlimeAI()
	{
		AIAction = 0;
		if (AI3 == 0f && Life > 0)
		{
			AI3 = LifeMax;
		}
		if (AI2 == 0f)
		{
			AI0 = -100f;
			AI2 = 1f;
			TargetClosest();
		}
		if (Velocity.Y == 0f)
		{
			Velocity.X *= 0.8f;
			if ((double)Velocity.X > -0.1 && (double)Velocity.X < 0.1)
			{
				Velocity.X = 0f;
			}
			double num = (double)Life / (double)LifeMax;
			if (num < 0.1)
			{
				AI0 += 13f;
			}
			else if (num < 0.2)
			{
				AI0 += 9f;
			}
			else if (num < 0.4)
			{
				AI0 += 6f;
			}
			else if (num < 0.6)
			{
				AI0 += 4f;
			}
			else if (num < 0.8)
			{
				AI0 += 3f;
			}
			else
			{
				AI0 += 2f;
			}
			if (AI0 >= 0f)
			{
				ShouldNetUpdate = true;
				TargetClosest();
				if (AI1 == 3f)
				{
					Velocity.Y = -13f;
					Velocity.X += 3.5f * (float)Direction;
					AI0 = -200f;
					AI1 = 0f;
				}
				else if (AI1 == 2f)
				{
					Velocity.Y = -6f;
					Velocity.X += 4.5f * (float)Direction;
					AI0 = -120f;
					AI1++;
				}
				else
				{
					Velocity.Y = -8f;
					Velocity.X += 4f * (float)Direction;
					AI0 = -120f;
					AI1++;
				}
			}
			else if (AI0 >= -30f)
			{
				AIAction = 1;
			}
		}
		else if (Target < 8 && ((Direction == 1 && Velocity.X < 3f) || (Direction == -1 && Velocity.X > -3f)))
		{
			if ((Direction == -1 && (double)Velocity.X < 0.1) || (Direction == 1 && (double)Velocity.X > -0.1))
			{
				Velocity.X += 0.2f * (float)Direction;
			}
			else
			{
				Velocity.X *= 0.93f;
			}
		}
		Dust* ptr = Main.DustSet.NewDust(4, ref XYWH, Velocity.X, Velocity.Y, 255, new Color(0, 80, 255, 80), Scale * 1.2f);
		if (ptr != null)
		{
			ptr->NoGravity = true;
			ptr->Velocity.X *= 0.5f;
			ptr->Velocity.Y *= 0.5f;
		}
		if (Life <= 0)
		{
			return;
		}
		float num2 = (float)Life / (float)LifeMax;
		num2 = num2 * 0.5f + 0.75f;
		if (num2 != Scale)
		{
			Position.X += Width >> 1;
			Position.Y += (int)Height;
			Scale = num2;
			Width = (ushort)(98f * Scale);
			Height = (ushort)(92f * Scale);
			Position.X -= Width >> 1;
			Position.Y -= (int)Height;
			XYWH.X = (int)Position.X;
			XYWH.Y = (int)Position.Y;
			XYWH.Width = Width;
			XYWH.Height = Height;
		}
		if (Main.NetMode == 1)
		{
			return;
		}
		int num3 = (int)((double)LifeMax * 0.05);
		if (!((float)(Life + num3) < AI3))
		{
			return;
		}
		AI3 = Life;
		int num4 = Main.Rand.Next(1, 4);
		for (int i = 0; i < num4; i++)
		{
			int x = XYWH.X + Main.Rand.Next(Width - 32);
			int y = XYWH.Y + Main.Rand.Next(Height - 32);
			int num5 = NewNPC(x, y, 1);
			if (num5 < 196)
			{
				Main.NPCSet[num5].SetDefaults(1);
				Main.NPCSet[num5].Velocity.X = (float)Main.Rand.Next(-15, 16) * 0.1f;
				Main.NPCSet[num5].Velocity.Y = (float)Main.Rand.Next(-30, 1) * 0.1f;
				Main.NPCSet[num5].AI1 = Main.Rand.Next(3);
				NetMessage.CreateMessage1(23, num5);
				NetMessage.SendMessage();
			}
		}
	}

	private void FishAI()
	{
		if (Direction == 0)
		{
			TargetClosest();
		}
		if (IsWet)
		{
			bool flag = false;
			if (Type != 55)
			{
				TargetClosest(ShouldFaceTarget: false);
				if (Main.PlayerSet[Target].IsWet && !Main.PlayerSet[Target].IsDead)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				if (HasXCollision)
				{
					Velocity.X = 0f - Velocity.X;
					Direction = (sbyte)(-Direction);
					ShouldNetUpdate = true;
				}
				if (HasYCollision)
				{
					ShouldNetUpdate = true;
					Velocity.Y = 0f - Velocity.Y;
					if (Velocity.Y < 0f)
					{
						DirectionY = -1;
						AI0 = -1f;
					}
					else if (Velocity.Y > 0f)
					{
						DirectionY = 1;
						AI0 = 1f;
					}
				}
			}
			if (Type == 102)
			{
				Lighting.AddLight(XYWH.X + (Width >> 1) + Direction * (Width + 8) >> 4, XYWH.Y + 2 >> 4, new Vector3(0.07f, 0.04f, 0.025f));
			}
			if (flag)
			{
				TargetClosest();
				if (Type == 65 || Type == 102 || Type == 148)
				{
					Velocity.X += (float)Direction * 0.15f;
					Velocity.Y += (float)DirectionY * 0.15f;
					if (Velocity.X > 5f)
					{
						Velocity.X = 5f;
					}
					else if (Velocity.X < -5f)
					{
						Velocity.X = -5f;
					}
					if (Velocity.Y > 3f)
					{
						Velocity.Y = 3f;
					}
					else if (Velocity.Y < -3f)
					{
						Velocity.Y = -3f;
					}
				}
				else
				{
					Velocity.X += (float)Direction * 0.1f;
					Velocity.Y += (float)DirectionY * 0.1f;
					if (Velocity.X > 3f)
					{
						Velocity.X = 3f;
					}
					else if (Velocity.X < -3f)
					{
						Velocity.X = -3f;
					}
					if (Velocity.Y > 2f)
					{
						Velocity.Y = 2f;
					}
					else if (Velocity.Y < -2f)
					{
						Velocity.Y = -2f;
					}
				}
			}
			else
			{
				Velocity.X += (float)Direction * 0.1f;
				if (Velocity.X < -1f || Velocity.X > 1f)
				{
					Velocity.X *= 0.95f;
				}
				if (AI0 == -1f)
				{
					Velocity.Y -= 0.01f;
					if ((double)Velocity.Y < -0.3)
					{
						AI0 = 1f;
					}
				}
				else
				{
					Velocity.Y += 0.01f;
					if ((double)Velocity.Y > 0.3)
					{
						AI0 = -1f;
					}
				}
				int num = XYWH.X + (Width >> 1) >> 4;
				int num2 = XYWH.Y + (Height >> 1) >> 4;
				if (Main.TileSet[num, num2 - 1].Liquid > 128)
				{
					if (Main.TileSet[num, num2 + 1].IsActive != 0)
					{
						AI0 = -1f;
					}
					else if (Main.TileSet[num, num2 + 2].IsActive != 0)
					{
						AI0 = -1f;
					}
				}
				if ((double)Velocity.Y > 0.4 || (double)Velocity.Y < -0.4)
				{
					Velocity.Y *= 0.95f;
				}
			}
		}
		else
		{
			if (Velocity.Y == 0f)
			{
				if (Type == 65 || Type == 148)
				{
					Velocity.X *= 0.94f;
					if (Velocity.X > -0.2f && Velocity.X < 0.2f)
					{
						Velocity.X = 0f;
					}
				}
				else if (Main.NetMode != 1)
				{
					Velocity.Y = (float)Main.Rand.Next(-50, -20) * 0.1f;
					Velocity.X = (float)Main.Rand.Next(-20, 20) * 0.1f;
					ShouldNetUpdate = true;
				}
			}
			Velocity.Y += 0.3f;
			if (Velocity.Y > 10f)
			{
				Velocity.Y = 10f;
			}
			AI0 = 1f;
		}
		Rotation = Velocity.Y * (float)Direction * 0.1f;
		if (Rotation < -0.2f)
		{
			Rotation = -0.2f;
		}
		else if (Rotation > 0.2f)
		{
			Rotation = 0.2f;
		}
	}

	private void VultureAI()
	{
		HasNoGravity = true;
		if (AI0 == 0f)
		{
			HasNoGravity = false;
			TargetClosest();
			if (Main.NetMode != 1)
			{
				if (Velocity.X != 0f || Velocity.Y < 0f || Velocity.Y > 0.3f)
				{
					AI0 = 1f;
					ShouldNetUpdate = true;
				}
				else if (Life < LifeMax || Main.PlayerSet[Target].XYWH.Intersects(new Rectangle(XYWH.X - 100, XYWH.Y - 100, Width + 200, Height + 200)))
				{
					AI0 = 1f;
					Velocity.Y -= 6f;
					ShouldNetUpdate = true;
				}
			}
		}
		else if (!Main.PlayerSet[Target].IsDead)
		{
			if (HasXCollision)
			{
				Velocity.X = OldVelocity.X * -0.5f;
				if (Direction == -1 && Velocity.X > 0f && Velocity.X < 2f)
				{
					Velocity.X = 2f;
				}
				else if (Direction == 1 && Velocity.X < 0f && Velocity.X > -2f)
				{
					Velocity.X = -2f;
				}
			}
			if (HasYCollision)
			{
				Velocity.Y = OldVelocity.Y * -0.5f;
				if (Velocity.Y > 0f && Velocity.Y < 1f)
				{
					Velocity.Y = 1f;
				}
				else if (Velocity.Y < 0f && Velocity.Y > -1f)
				{
					Velocity.Y = -1f;
				}
			}
			TargetClosest();
			if (Direction == -1 && Velocity.X > -3f)
			{
				Velocity.X -= 0.1f;
				if (Velocity.X > 3f)
				{
					Velocity.X -= 0.1f;
				}
				else if (Velocity.X > 0f)
				{
					Velocity.X -= 0.05f;
				}
				else if (Velocity.X < -3f)
				{
					Velocity.X = -3f;
				}
			}
			else if (Direction == 1 && Velocity.X < 3f)
			{
				Velocity.X += 0.1f;
				if (Velocity.X < -3f)
				{
					Velocity.X += 0.1f;
				}
				else if (Velocity.X < 0f)
				{
					Velocity.X += 0.05f;
				}
				else if (Velocity.X > 3f)
				{
					Velocity.X = 3f;
				}
			}
			int num = Math.Abs(XYWH.X + (Width >> 1) - (Main.PlayerSet[Target].XYWH.X + 10));
			int num2 = Main.PlayerSet[Target].XYWH.Y - (Height >> 1);
			if (num > 50)
			{
				num2 -= 100;
			}
			if (XYWH.Y < num2)
			{
				Velocity.Y += 0.05f;
				if (Velocity.Y < 0f)
				{
					Velocity.Y += 0.01f;
				}
			}
			else
			{
				Velocity.Y -= 0.05f;
				if (Velocity.Y > 0f)
				{
					Velocity.Y -= 0.01f;
				}
			}
			if (Velocity.Y < -3f)
			{
				Velocity.Y = -3f;
			}
			if (Velocity.Y > 3f)
			{
				Velocity.Y = 3f;
			}
		}
		if (IsWet)
		{
			if (Velocity.Y > 0f)
			{
				Velocity.Y *= 0.95f;
			}
			Velocity.Y -= 0.5f;
			if (Velocity.Y < -4f)
			{
				Velocity.Y = -4f;
			}
			TargetClosest();
		}
	}

	private void JellyfishAI()
	{
		Vector3 rGB;
		if (Type == 63)
		{
			rGB = new Vector3(0.05f, 0.15f, 0.4f);
		}
		else
		{
			rGB = ((Type == 103) ? new Vector3(0.05f, 0.45f, 0.1f) : new Vector3(0.35f, 0.05f, 0.2f));
		}
		Lighting.AddLight(XYWH.X + (Height >> 1) >> 4, XYWH.Y + (Height >> 1) >> 4, rGB);
		if (Direction == 0)
		{
			TargetClosest();
		}
		if (IsWet)
		{
			if (HasXCollision)
			{
				Velocity.X = 0f - Velocity.X;
				Direction = (sbyte)(-Direction);
			}
			if (HasYCollision)
			{
				Velocity.Y = 0f - Velocity.Y;
				if (Velocity.Y < 0f)
				{
					DirectionY = -1;
					AI0 = -1f;
				}
				else if (Velocity.Y > 0f)
				{
					DirectionY = 1;
					AI0 = 1f;
				}
			}
			bool flag = false;
			if (!IsFriendly)
			{
				TargetClosest(ShouldFaceTarget: false);
				if (Main.PlayerSet[Target].IsWet && !Main.PlayerSet[Target].IsDead)
				{
					flag = true;
				}
			}
			if (flag)
			{
				Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X) + 1.57f;
				Velocity.X *= 0.98f;
				Velocity.Y *= 0.98f;
				float num = 0.2f;
				if (Type == 103)
				{
					Velocity.X *= 0.98f;
					Velocity.Y *= 0.98f;
					num = 0.6f;
				}
				if (Velocity.X > 0f - num && Velocity.X < num && Velocity.Y > 0f - num && Velocity.Y < num)
				{
					TargetClosest();
					float num2 = ((Type == 103) ? 9 : 7);
					Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
					float num3 = Main.PlayerSet[Target].Position.X + 10f - vector.X;
					float num4 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
					float num5 = (float)Math.Sqrt(num3 * num3 + num4 * num4);
					num5 = num2 / num5;
					num3 *= num5;
					num4 *= num5;
					Velocity.X = num3;
					Velocity.Y = num4;
				}
				return;
			}
			Velocity.X += (float)Direction * 0.02f;
			Rotation = Velocity.X * 0.4f;
			if (Velocity.X < -1f || Velocity.X > 1f)
			{
				Velocity.X *= 0.95f;
			}
			if (AI0 == -1f)
			{
				Velocity.Y -= 0.01f;
				if (Velocity.Y < -1f)
				{
					AI0 = 1f;
				}
			}
			else
			{
				Velocity.Y += 0.01f;
				if (Velocity.Y > 1f)
				{
					AI0 = -1f;
				}
			}
			int num6 = XYWH.X + (Width >> 1) >> 4;
			int num7 = XYWH.Y + (Height >> 1) >> 4;
			if (Main.TileSet[num6, num7 - 1].Liquid > 128)
			{
				if (Main.TileSet[num6, num7 + 1].IsActive != 0)
				{
					AI0 = -1f;
				}
				else if (Main.TileSet[num6, num7 + 2].IsActive != 0)
				{
					AI0 = -1f;
				}
			}
			else
			{
				AI0 = 1f;
			}
			if ((double)Velocity.Y > 1.2 || (double)Velocity.Y < -1.2)
			{
				Velocity.Y *= 0.99f;
			}
			return;
		}
		Rotation += Velocity.X * 0.1f;
		if (Velocity.Y == 0f)
		{
			Velocity.X *= 0.98f;
			if ((double)Velocity.X > -0.01 && (double)Velocity.X < 0.01)
			{
				Velocity.X = 0f;
			}
		}
		Velocity.Y += 0.2f;
		if (Velocity.Y > 10f)
		{
			Velocity.Y = 10f;
		}
		AI0 = 1f;
	}

	private unsafe void AntlionAI()
	{
		TargetClosest();
		Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
		float num = Main.PlayerSet[Target].Position.X + 10f - vector.X;
		float num2 = Main.PlayerSet[Target].Position.Y - vector.Y;
		float num3 = (float)Math.Sqrt(num * num + num2 * num2);
		num3 = 12f / num3;
		num *= num3;
		num2 *= num3;
		bool flag = false;
		if (DirectionY < 0)
		{
			Rotation = (float)(Math.Atan2(num2, num) + 1.57);
			flag = ((!((double)Rotation < -1.2) && !((double)Rotation > 1.2)) ? true : false);
			if ((double)Rotation < -0.8)
			{
				Rotation = -0.8f;
			}
			else if ((double)Rotation > 0.8)
			{
				Rotation = 0.8f;
			}
			if (Velocity.X != 0f)
			{
				Velocity.X *= 0.9f;
				if ((double)Velocity.X > -0.1 || (double)Velocity.X < 0.1)
				{
					ShouldNetUpdate = true;
					Velocity.X = 0f;
				}
			}
		}
		if (AI0 > 0f)
		{
			if (AI0 == 200f)
			{
				Main.PlaySound(2, XYWH.X, XYWH.Y, 5);
			}
			AI0--;
		}
		if (((Main.NetMode != 1) & flag) && AI0 == 0f && Collision.CanHit(ref XYWH, ref Main.PlayerSet[Target].XYWH))
		{
			AI0 = 200f;
			int damage = 10;
			int type = 31;
			int num4 = Projectile.NewProjectile(vector.X, vector.Y, num, num2, type, damage, 0f, 8, send: false);
			if (num4 >= 0)
			{
				Main.ProjectileSet[num4].ai0 = 2f;
				Main.ProjectileSet[num4].timeLeft = 300;
				Main.ProjectileSet[num4].friendly = false;
				NetMessage.SendProjectile(num4);
				ShouldNetUpdate = true;
			}
		}
		try
		{
			int num5 = XYWH.X >> 4;
			int num6 = XYWH.X + (Width >> 1) >> 4;
			int num7 = XYWH.X + Width >> 4;
			int num8 = XYWH.Y + Height >> 4;
			if ((Main.TileSet[num5, num8].IsActive != 0 && Main.TileSolid[Main.TileSet[num5, num8].Type]) || (Main.TileSet[num6, num8].IsActive != 0 && Main.TileSolid[Main.TileSet[num6, num8].Type]) || (Main.TileSet[num7, num8].IsActive != 0 && Main.TileSolid[Main.TileSet[num7, num8].Type]))
			{
				HasNoGravity = true;
				HasNoTileCollide = true;
				Velocity.Y = -0.2f;
				return;
			}
			HasNoGravity = false;
			HasNoTileCollide = false;
			if (Main.Rand.Next(3) != 0)
			{
				return;
			}
			Dust* ptr = Main.DustSet.NewDust(XYWH.X - 4, XYWH.Y + Height - 8, Width + 8, 24, 32, 0.0, Velocity.Y * 0.5f);
			if (ptr != null)
			{
				ptr->Velocity.X *= 0.4f;
				ptr->Velocity.Y *= -1f;
				if (Main.Rand.Next(2) == 0)
				{
					ptr->NoGravity = true;
					ptr->Scale += 0.2f;
				}
			}
		}
		catch
		{
		}
	}

	private void SpinningSpikeballAI()
	{
		if (AI0 == 0f)
		{
			if (Main.NetMode != 1)
			{
				TargetClosest();
				Direction = (sbyte)(-Direction);
				DirectionY = (sbyte)(-DirectionY);
				Position.Y += (Height >> 1) + 8;
				AI1 = Position.X + (float)(Width >> 1);
				AI2 = Position.Y + (float)(Height >> 1);
				if (Direction == 0)
				{
					Direction = 1;
				}
				if (DirectionY == 0)
				{
					DirectionY = 1;
				}
				AI3 = 1f + (float)Main.Rand.Next(15) * 0.1f;
				Velocity.Y = (float)(DirectionY * 6) * AI3;
				AI0++;
				ShouldNetUpdate = true;
			}
			else
			{
				AI1 = Position.X + (float)(Width >> 1);
				AI2 = Position.Y + (float)(Height >> 1);
			}
			return;
		}
		float num = 6f * AI3;
		float num2 = 0.2f * AI3;
		float num3 = num / num2 * 0.5f;
		if (AI0 >= 1f && AI0 < (float)(int)num3)
		{
			Velocity.Y = (float)DirectionY * num;
			AI0++;
			return;
		}
		if (AI0 >= (float)(int)num3)
		{
			ShouldNetUpdate = true;
			Velocity.Y = 0f;
			DirectionY = (sbyte)(-DirectionY);
			Velocity.X = num * (float)Direction;
			AI0 = -1f;
			return;
		}
		if (DirectionY > 0)
		{
			if (Velocity.Y >= num)
			{
				ShouldNetUpdate = true;
				DirectionY = (sbyte)(-DirectionY);
				Velocity.Y = num;
			}
		}
		else if (DirectionY < 0 && Velocity.Y <= 0f - num)
		{
			DirectionY = (sbyte)(-DirectionY);
			Velocity.Y = 0f - num;
		}
		if (Direction > 0)
		{
			if (Velocity.X >= num)
			{
				Direction = (sbyte)(-Direction);
				Velocity.X = num;
			}
		}
		else if (Direction < 0 && Velocity.X <= 0f - num)
		{
			Direction = (sbyte)(-Direction);
			Velocity.X = 0f - num;
		}
		Velocity.X += num2 * (float)Direction;
		Velocity.Y += num2 * (float)DirectionY;
	}

	private void GravityDiskAI()
	{
		if (AI0 == 0f)
		{
			TargetClosest();
			DirectionY = 1;
			AI0 = 1f;
		}
		int num = 6;
		if (AI1 == 0f)
		{
			Rotation += (float)(Direction * DirectionY) * 0.13f;
			if (HasYCollision)
			{
				AI0 = 2f;
			}
			if (!HasYCollision && AI0 == 2f)
			{
				Direction = (sbyte)(-Direction);
				AI1 = 1f;
				AI0 = 1f;
			}
			if (HasXCollision)
			{
				DirectionY = (sbyte)(-DirectionY);
				AI1 = 1f;
			}
		}
		else
		{
			Rotation -= (float)(Direction * DirectionY) * 0.13f;
			if (HasXCollision)
			{
				AI0 = 2f;
			}
			if (!HasXCollision && AI0 == 2f)
			{
				DirectionY = (sbyte)(-DirectionY);
				AI1 = 0f;
				AI0 = 1f;
			}
			if (HasYCollision)
			{
				Direction = (sbyte)(-Direction);
				AI1 = 0f;
			}
		}
		Velocity.X = num * Direction;
		Velocity.Y = num * DirectionY;
		float num2 = (float)(270 - UI.MouseTextBrightness) * 0.0025f;
		Lighting.AddLight(XYWH.X + (Width >> 1) >> 4, XYWH.Y + (Height >> 1) >> 4, new Vector3(0.9f, 0.3f + num2, 0.2f));
	}

	private unsafe void MoreFlyerAI()
	{
		bool flag = false;
		if (WasJustHit)
		{
			AI2 = 0f;
		}
		if (AI2 >= 0f)
		{
			float num = 16f;
			bool flag2 = false;
			bool flag3 = false;
			if (Position.X > AI0 - num && Position.X < AI0 + num)
			{
				flag2 = true;
			}
			else if ((Velocity.X < 0f && Direction > 0) || (Velocity.X > 0f && Direction < 0))
			{
				flag2 = true;
			}
			num += 24f;
			if (Position.Y > AI1 - num && Position.Y < AI1 + num)
			{
				flag3 = true;
			}
			if (flag2 & flag3)
			{
				AI2++;
				if (AI2 >= 30f && num == 16f)
				{
					flag = true;
				}
				if (AI2 >= 60f)
				{
					AI2 = -200f;
					Direction = (sbyte)(-Direction);
					Velocity.X = 0f - Velocity.X;
					HasXCollision = false;
				}
			}
			else
			{
				AI0 = Position.X;
				AI1 = Position.Y;
				AI2 = 0f;
			}
			TargetClosest();
		}
		else
		{
			AI2++;
			if (Main.PlayerSet[Target].XYWH.X + 10 > XYWH.X + (Width >> 1))
			{
				Direction = -1;
			}
			else
			{
				Direction = 1;
			}
		}
		int num2 = (XYWH.X + (Width >> 1) >> 4) + (Direction << 1);
		int num3 = XYWH.Y + Height >> 4;
		bool flag4 = true;
		bool flag5 = false;
		int num4 = 3;
		if (Type == 122 || Type == 153)
		{
			if (WasJustHit)
			{
				AI3 = 0f;
				LocalAI1 = 0;
			}
			Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num5 = Main.PlayerSet[Target].Position.X + 10f - vector.X;
			float num6 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
			float num7 = (float)Math.Sqrt(num5 * num5 + num6 * num6);
			num7 = 7f / num7;
			num5 *= num7;
			num6 *= num7;
			if (Main.NetMode != 1 && AI3 == 32f)
			{
				Projectile.NewProjectile(vector.X, vector.Y, num5, num6, 84, 25, 0f);
			}
			num4 = 8;
			if (AI3 > 0f)
			{
				AI3++;
				if (AI3 >= 64f)
				{
					AI3 = 0f;
				}
			}
			if (Main.NetMode != 1 && AI3 == 0f)
			{
				LocalAI1++;
				if (LocalAI1 > 120 && Collision.CanHit(ref XYWH, ref Main.PlayerSet[Target].XYWH))
				{
					LocalAI1 = 0;
					AI3 = 1f;
					ShouldNetUpdate = true;
				}
			}
		}
		else if (Type == 75)
		{
			num4 = 4;
			if (Main.Rand.Next(7) == 0)
			{
				Dust* ptr = Main.DustSet.NewDust(55, ref XYWH, 0.0, 0.0, 200, Colour);
				if (ptr != null)
				{
					ptr->Velocity.X *= 0.3f;
					ptr->Velocity.Y *= 0.3f;
				}
			}
			if (Main.Rand.Next(42) == 0)
			{
				Main.PlaySound(27, XYWH.X, XYWH.Y);
			}
		}
		for (int i = num3; i < num3 + num4; i++)
		{
			if ((Main.TileSet[num2, i].IsActive != 0 && Main.TileSolid[Main.TileSet[num2, i].Type]) || Main.TileSet[num2, i].Liquid > 0)
			{
				if (i <= num3 + 1)
				{
					flag5 = true;
				}
				flag4 = false;
				break;
			}
		}
		if (flag)
		{
			flag5 = false;
			flag4 = true;
		}
		if (flag4)
		{
			if (Type == 75)
			{
				Velocity.Y += 0.2f;
				if (Velocity.Y > 2f)
				{
					Velocity.Y = 2f;
				}
			}
			else
			{
				Velocity.Y += 0.1f;
				if (Velocity.Y > 3f)
				{
					Velocity.Y = 3f;
				}
			}
		}
		else
		{
			if (Type == 75)
			{
				if ((DirectionY < 0 && Velocity.Y > 0f) | flag5)
				{
					Velocity.Y -= 0.2f;
				}
			}
			else if (DirectionY < 0 && Velocity.Y > 0f)
			{
				Velocity.Y -= 0.1f;
			}
			if (Velocity.Y < -4f)
			{
				Velocity.Y = -4f;
			}
		}
		if (Type == 75 && IsWet)
		{
			Velocity.Y -= 0.2f;
			if (Velocity.Y < -2f)
			{
				Velocity.Y = -2f;
			}
		}
		if (HasXCollision)
		{
			Velocity.X = OldVelocity.X * -0.4f;
			if (Direction == -1 && Velocity.X > 0f && Velocity.X < 1f)
			{
				Velocity.X = 1f;
			}
			else if (Direction == 1 && Velocity.X < 0f && Velocity.X > -1f)
			{
				Velocity.X = -1f;
			}
		}
		if (HasYCollision)
		{
			Velocity.Y = OldVelocity.Y * -0.25f;
			if (Velocity.Y > 0f && Velocity.Y < 1f)
			{
				Velocity.Y = 1f;
			}
			else if (Velocity.Y < 0f && Velocity.Y > -1f)
			{
				Velocity.Y = -1f;
			}
		}
		float num8 = ((Type == 75) ? 3 : 2);
		if (Direction == -1 && Velocity.X > 0f - num8)
		{
			Velocity.X -= 0.1f;
			if (Velocity.X > num8)
			{
				Velocity.X -= 0.1f;
			}
			else if (Velocity.X > 0f)
			{
				Velocity.X += 0.05f;
			}
			else if (Velocity.X < 0f - num8)
			{
				Velocity.X = 0f - num8;
			}
		}
		else if (Direction == 1 && Velocity.X < num8)
		{
			Velocity.X += 0.1f;
			if (Velocity.X < 0f - num8)
			{
				Velocity.X += 0.1f;
			}
			else if (Velocity.X < 0f)
			{
				Velocity.X -= 0.05f;
			}
			else if (Velocity.X > num8)
			{
				Velocity.X = num8;
			}
		}
		if (DirectionY == -1 && (double)Velocity.Y > -1.5)
		{
			Velocity.Y -= 0.04f;
			if ((double)Velocity.Y > 1.5)
			{
				Velocity.Y -= 0.05f;
			}
			else if (Velocity.Y > 0f)
			{
				Velocity.Y += 0.03f;
			}
			else if ((double)Velocity.Y < -1.5)
			{
				Velocity.Y = -1.5f;
			}
		}
		else if (DirectionY == 1 && (double)Velocity.Y < 1.5)
		{
			Velocity.Y += 0.04f;
			if ((double)Velocity.Y < -1.5)
			{
				Velocity.Y += 0.05f;
			}
			else if (Velocity.Y < 0f)
			{
				Velocity.Y -= 0.03f;
			}
			else if ((double)Velocity.Y > 1.5)
			{
				Velocity.Y = 1.5f;
			}
		}
		if (Type == 122 || Type == 153)
		{
			Lighting.AddLight(XYWH.X >> 4, XYWH.Y >> 4, new Vector3(0.4f, 0f, 0.25f));
		}
	}

	private void EnchantedWeaponAI()
	{
		HasNoGravity = true;
		HasNoTileCollide = true;
		Vector3 rGB = new Vector3(0.05f, 0.2f, 0.3f);
		switch (Type)
		{
		case 83:
			rGB = new Vector3(0.2f, 0.05f, 0.3f);
			break;
		case 151:
			rGB = new Vector3(0.3f, 0.05f, 0.2f);
			break;
		}
		Lighting.AddLight(XYWH.X + (Width >> 1) >> 4, XYWH.Y + (Height >> 1) >> 4, rGB);
		if (Target == 8 || Main.PlayerSet[Target].IsDead)
		{
			TargetClosest();
		}
		if (AI0 == 0f)
		{
			Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num = Main.PlayerSet[Target].Position.X + 10f - vector.X;
			float num2 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
			float num3 = (float)Math.Sqrt(num * num + num2 * num2);
			num3 = 9f / num3;
			num *= num3;
			num2 *= num3;
			Velocity.X = num;
			Velocity.Y = num2;
			Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X) + 0.785f;
			AI0 = 1f;
			AI1 = 0f;
			return;
		}
		if (AI0 == 1f)
		{
			if (WasJustHit)
			{
				AI0 = 2f;
				AI1 = 0f;
			}
			Velocity.X *= 0.99f;
			Velocity.Y *= 0.99f;
			AI1++;
			if (AI1 >= 100f)
			{
				AI0 = 2f;
				AI1 = 0f;
				Velocity.X = 0f;
				Velocity.Y = 0f;
			}
			return;
		}
		if (WasJustHit)
		{
			AI0 = 2f;
			AI1 = 0f;
		}
		Velocity.X *= 0.96f;
		Velocity.Y *= 0.96f;
		AI1++;
		float num4 = AI1 / 120f;
		num4 = 0.1f + num4 * 0.4f;
		Rotation += num4 * (float)Direction;
		if (AI1 >= 120f)
		{
			ShouldNetUpdate = true;
			AI0 = 0f;
			AI1 = 0f;
		}
	}

	private void BirdAI()
	{
		HasNoGravity = true;
		if (AI0 == 0f)
		{
			HasNoGravity = false;
			TargetClosest();
			if (Main.NetMode != 1)
			{
				if (Velocity.X != 0f || Velocity.Y < 0f || (double)Velocity.Y > 0.3)
				{
					AI0 = 1f;
					ShouldNetUpdate = true;
					Direction = (sbyte)(-Direction);
				}
				else if (Life < LifeMax || Main.PlayerSet[Target].XYWH.Intersects(new Rectangle(XYWH.X - 100, XYWH.Y - 100, Width + 200, Height + 200)))
				{
					AI0 = 1f;
					Velocity.Y -= 6f;
					ShouldNetUpdate = true;
					Direction = (sbyte)(-Direction);
				}
			}
		}
		else if (!Main.PlayerSet[Target].IsDead)
		{
			if (HasXCollision)
			{
				Direction = (sbyte)(-Direction);
				Velocity.X = OldVelocity.X * -0.5f;
				if (Direction == -1 && Velocity.X > 0f && Velocity.X < 2f)
				{
					Velocity.X = 2f;
				}
				else if (Direction == 1 && Velocity.X < 0f && Velocity.X > -2f)
				{
					Velocity.X = -2f;
				}
			}
			if (HasYCollision)
			{
				Velocity.Y = OldVelocity.Y * -0.5f;
				if (Velocity.Y > 0f && Velocity.Y < 1f)
				{
					Velocity.Y = 1f;
				}
				else if (Velocity.Y < 0f && Velocity.Y > -1f)
				{
					Velocity.Y = -1f;
				}
			}
			if (Direction == -1 && Velocity.X > -3f)
			{
				Velocity.X -= 0.1f;
				if (Velocity.X > 3f)
				{
					Velocity.X -= 0.1f;
				}
				else if (Velocity.X > 0f)
				{
					Velocity.X -= 0.05f;
				}
				else if (Velocity.X < -3f)
				{
					Velocity.X = -3f;
				}
			}
			else if (Direction == 1 && Velocity.X < 3f)
			{
				Velocity.X += 0.1f;
				if (Velocity.X < -3f)
				{
					Velocity.X += 0.1f;
				}
				else if (Velocity.X < 0f)
				{
					Velocity.X += 0.05f;
				}
				else if (Velocity.X > 3f)
				{
					Velocity.X = 3f;
				}
			}
			int num = (XYWH.X + (Width >> 1) >> 4) + Direction;
			int num2 = XYWH.Y + Height >> 4;
			bool flag = true;
			bool flag2 = false;
			try
			{
				for (int i = num2; i < num2 + 15; i++)
				{
					if ((Main.TileSet[num, i].IsActive != 0 && Main.TileSolid[Main.TileSet[num, i].Type]) || Main.TileSet[num, i].Liquid > 0)
					{
						if (i < num2 + 5)
						{
							flag2 = true;
						}
						flag = false;
						break;
					}
				}
			}
			catch
			{
				flag2 = true;
				flag = false;
			}
			if (flag)
			{
				Velocity.Y += 0.1f;
			}
			else
			{
				Velocity.Y -= 0.1f;
			}
			if (flag2)
			{
				Velocity.Y -= 0.2f;
			}
			if (Velocity.Y > 3f)
			{
				Velocity.Y = 3f;
			}
			else if (Velocity.Y < -4f)
			{
				Velocity.Y = -4f;
			}
		}
		if (IsWet)
		{
			if (Velocity.Y > 0f)
			{
				Velocity.Y *= 0.95f;
			}
			Velocity.Y -= 0.5f;
			if (Velocity.Y < -4f)
			{
				Velocity.Y = -4f;
			}
			TargetClosest();
		}
	}

	private void MimicAI()
	{
		if (AI3 == 0f)
		{
			Position.X += 8f;
			XYWH.X += 8;
			int num = XYWH.Y >> 4;
			if (num > Main.MaxTilesY - 200)
			{
				AI3 = 3f;
			}
			else if (num > Main.WorldSurface)
			{
				AI3 = 2f;
			}
			else
			{
				AI3 = 1f;
			}
		}
		if (AI0 == 0f)
		{
			TargetClosest();
			if (Main.NetMode != 1)
			{
				if (Velocity.X != 0f || Velocity.Y < 0f || (double)Velocity.Y > 0.3)
				{
					AI0 = 1f;
					ShouldNetUpdate = true;
				}
				else if (Life < LifeMax || new Rectangle(XYWH.X - 100, XYWH.Y - 100, Width + 200, Height + 200).Intersects(Main.PlayerSet[Target].XYWH))
				{
					AI0 = 1f;
					ShouldNetUpdate = true;
				}
			}
		}
		else if (Velocity.Y == 0f)
		{
			AI2++;
			int num2 = 20;
			if (AI1 == 0f)
			{
				num2 = 12;
			}
			if (AI2 < (float)num2)
			{
				Velocity.X *= 0.9f;
				return;
			}
			AI2 = 0f;
			TargetClosest();
			SpriteDirection = Direction;
			AI1++;
			if (AI1 == 2f)
			{
				Velocity.X = (float)Direction * 2.5f;
				Velocity.Y = -8f;
				AI1 = 0f;
			}
			else
			{
				Velocity.X = (float)Direction * 3.5f;
				Velocity.Y = -4f;
			}
			ShouldNetUpdate = true;
		}
		else if (Direction == 1 && Velocity.X < 1f)
		{
			Velocity.X += 0.1f;
		}
		else if (Direction == -1 && Velocity.X > -1f)
		{
			Velocity.X -= 0.1f;
		}
	}

	private void UnicornAI()
	{
		int num = 30;
		bool flag = false;
		if (Velocity.Y == 0f && ((Velocity.X > 0f && Direction < 0) || (Velocity.X < 0f && Direction > 0)))
		{
			flag = true;
			AI3++;
		}
		if ((Position.X == OldPosition.X || AI3 >= (float)num) | flag)
		{
			AI3++;
		}
		else if (AI3 > 0f)
		{
			AI3--;
		}
		if (AI3 > (float)(num * 10))
		{
			AI3 = 0f;
		}
		if (WasJustHit)
		{
			AI3 = 0f;
		}
		if (AI3 == (float)num)
		{
			ShouldNetUpdate = true;
		}
		if (AI3 < (float)num)
		{
			TargetClosest();
		}
		else
		{
			if (Velocity.X == 0f)
			{
				if (Velocity.Y == 0f)
				{
					AI0++;
					if (AI0 >= 2f)
					{
						Direction = (sbyte)(-Direction);
						SpriteDirection = Direction;
						AI0 = 0f;
					}
				}
			}
			else
			{
				AI0 = 0f;
			}
			DirectionY = -1;
			if (Direction == 0)
			{
				Direction = 1;
			}
		}
		if (Velocity.Y == 0f || IsWet || (Velocity.X <= 0f && Direction < 0) || (Velocity.X >= 0f && Direction > 0))
		{
			if (Velocity.X < -6f || Velocity.X > 6f)
			{
				if (Velocity.Y == 0f)
				{
					Velocity.X *= 0.8f;
				}
			}
			else if (Velocity.X < 6f && Direction == 1)
			{
				Velocity.X += 0.07f;
				if (Velocity.X > 6f)
				{
					Velocity.X = 6f;
				}
			}
			else if (Velocity.X > -6f && Direction == -1)
			{
				Velocity.X -= 0.07f;
				if (Velocity.X < -6f)
				{
					Velocity.X = -6f;
				}
			}
		}
		if (Velocity.Y != 0f)
		{
			return;
		}
		int num2 = (int)(Position.X + Velocity.X * 5f) + (Width >> 1) + ((Width >> 1) + 2) * Direction >> 4;
		int num3 = XYWH.Y + Height - 15 >> 4;
		if ((!(Velocity.X < 0f) || SpriteDirection != -1) && (!(Velocity.X > 0f) || SpriteDirection != 1))
		{
			return;
		}
		if (Main.TileSet[num2, num3 - 2].IsActive != 0 && Main.TileSolid[Main.TileSet[num2, num3 - 2].Type])
		{
			if (Main.TileSet[num2, num3 - 3].IsActive != 0 && Main.TileSolid[Main.TileSet[num2, num3 - 3].Type])
			{
				Velocity.Y = -8.5f;
				ShouldNetUpdate = true;
			}
			else
			{
				Velocity.Y = -7.5f;
				ShouldNetUpdate = true;
			}
		}
		else if (Main.TileSet[num2, num3 - 1].IsActive != 0 && Main.TileSolid[Main.TileSet[num2, num3 - 1].Type])
		{
			Velocity.Y = -7f;
			ShouldNetUpdate = true;
		}
		else if (Main.TileSet[num2, num3].IsActive != 0 && Main.TileSolid[Main.TileSet[num2, num3].Type])
		{
			Velocity.Y = -6f;
			ShouldNetUpdate = true;
		}
		else if ((DirectionY < 0 || Math.Abs(Velocity.X) > 3f) && (Main.TileSet[num2, num3 + 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[num2, num3 + 1].Type]) && (Main.TileSet[num2 + Direction, num3 + 1].IsActive == 0 || !Main.TileSolid[Main.TileSet[num2 + Direction, num3 + 1].Type]))
		{
			Velocity.Y = -8f;
			ShouldNetUpdate = true;
		}
	}

	private void WallOfFleshMouthAI()
	{
		if (XYWH.X < 160 || XYWH.X > (Main.MaxTilesX - 10) * 16)
		{
			Active = 0;
			return;
		}
		if (LocalAI0 == 0)
		{
			LocalAI0 = 1;
			WoFBottom = -1;
			WoFTop = -1;
		}
		AI1++;
		if (AI2 == 0f)
		{
			if ((double)Life < (double)LifeMax * 0.5)
			{
				AI1++;
			}
			if ((double)Life < (double)LifeMax * 0.2)
			{
				AI1++;
			}
			if (AI1 > 2700f)
			{
				AI2 = 1f;
			}
		}
		if (AI2 > 0f && AI1 > 60f)
		{
			int num = 3;
			if ((double)Life < (double)LifeMax * 0.3)
			{
				num++;
			}
			AI2++;
			AI1 = 0f;
			if (AI2 > (float)num)
			{
				AI2 = 0f;
			}
			if (Main.NetMode != 1)
			{
				int num2 = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + (Height >> 1) + 20, 117, 1);
				Main.NPCSet[num2].Velocity.X = Direction << 3;
			}
		}
		LocalAI3++;
		if (LocalAI3 >= 600 + Main.Rand.Next(1000))
		{
			LocalAI3 = -Main.Rand.Next(200);
			Main.PlaySound(4, XYWH.X, XYWH.Y, 10);
		}
		WoF = WhoAmI;
		int num3 = XYWH.X >> 4;
		int num4 = XYWH.X + Width >> 4;
		int num5 = XYWH.Y + (Height >> 1) >> 4;
		int num6 = 0;
		int num7 = num5 + 7;
		while (num6 < 15 && num7 < Main.MaxTilesY - 10)
		{
			num7++;
			for (int i = num3; i <= num4; i++)
			{
				try
				{
					if (WorldGen.SolidTile(i, num7) || Main.TileSet[i, num7].Liquid > 0)
					{
						num6++;
					}
				}
				catch
				{
					num6 += 15;
				}
			}
		}
		num7 += 4;
		if (WoFBottom == -1)
		{
			WoFBottom = num7 * 16;
		}
		else if (WoFBottom > num7 * 16)
		{
			WoFBottom--;
			if (WoFBottom < num7 * 16)
			{
				WoFBottom = num7 * 16;
			}
		}
		else if (WoFBottom < num7 * 16)
		{
			WoFBottom++;
			if (WoFBottom > num7 * 16)
			{
				WoFBottom = num7 * 16;
			}
		}
		num6 = 0;
		num7 = num5 - 7;
		while (num6 < 15 && num7 > Main.MaxTilesY - 200)
		{
			num7--;
			for (int j = num3; j <= num4; j++)
			{
				try
				{
					if (WorldGen.SolidTile(j, num7) || Main.TileSet[j, num7].Liquid > 0)
					{
						num6++;
					}
				}
				catch
				{
					num6 += 15;
				}
			}
		}
		num7 -= 4;
		if (WoFTop == -1)
		{
			WoFTop = num7 * 16;
		}
		else if (WoFTop > num7 * 16)
		{
			WoFTop--;
			if (WoFTop < num7 * 16)
			{
				WoFTop = num7 * 16;
			}
		}
		else if (WoFTop < num7 * 16)
		{
			WoFTop++;
			if (WoFTop > num7 * 16)
			{
				WoFTop = num7 * 16;
			}
		}
		int num8 = (WoFBottom + WoFTop >> 1) - (Height >> 1);
		Velocity.Y = 0f;
		Position.Y = num8;
		XYWH.Y = num8;
		float num9 = 1.5f;
		if (Life < (LifeMax >> 1) + (LifeMax >> 2))
		{
			num9 += 0.25f;
		}
		if (Life < LifeMax >> 1)
		{
			num9 += 0.4f;
		}
		if (Life < LifeMax >> 2)
		{
			num9 += 0.5f;
		}
		if (Life < LifeMax / 10)
		{
			num9 += 0.6f;
		}
		if (Velocity.X == 0f)
		{
			TargetClosest();
			Velocity.X = Direction;
		}
		if (Velocity.X < 0f)
		{
			Velocity.X = 0f - num9;
			Direction = -1;
		}
		else
		{
			Velocity.X = num9;
			Direction = 1;
		}
		SpriteDirection = Direction;
		Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
		float num10 = Main.PlayerSet[Target].Position.X + 10f - vector.X;
		float num11 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
		float num12 = (float)Math.Sqrt(num10 * num10 + num11 * num11);
		num10 *= num12;
		num11 *= num12;
		if (Direction > 0)
		{
			if (Main.PlayerSet[Target].XYWH.X + 10 > XYWH.X + (Width >> 1))
			{
				Rotation = (float)Math.Atan2(0f - num11, 0f - num10) + 3.14f;
			}
			else
			{
				Rotation = 0f;
			}
		}
		else if (Main.PlayerSet[Target].XYWH.X + 10 < XYWH.X + (Width >> 1))
		{
			Rotation = (float)Math.Atan2(num11, num10) + 3.14f;
		}
		else
		{
			Rotation = 0f;
		}
		if (LocalAI0 == 1 && Main.NetMode != 1)
		{
			LocalAI0 = 2;
			num8 = WoFBottom + WoFTop >> 1;
			num8 = num8 + WoFTop >> 1;
			int num13 = NewNPC(XYWH.X, num8, 114, WhoAmI);
			Main.NPCSet[num13].AI0 = 1f;
			num8 = WoFBottom + WoFTop >> 1;
			num8 = num8 + WoFBottom >> 1;
			num13 = NewNPC(XYWH.X, num8, 114, WhoAmI);
			Main.NPCSet[num13].AI0 = -1f;
			num8 = WoFBottom + WoFTop >> 1;
			num8 = num8 + WoFBottom >> 1;
			for (int k = 0; k < 11; k++)
			{
				num13 = NewNPC(XYWH.X, num8, 115, WhoAmI);
				Main.NPCSet[num13].AI0 = (float)k * 0.1f - 0.05f;
			}
		}
	}

	private void WallOfFleshEyesAI()
	{
		if (WoF < 0)
		{
			Active = 0;
			return;
		}
		RealLife = WoF;
		TargetClosest();
		Position.X = Main.NPCSet[WoF].Position.X;
		XYWH.X = Main.NPCSet[WoF].XYWH.X;
		Direction = Main.NPCSet[WoF].Direction;
		SpriteDirection = Direction;
		int num = WoFBottom + WoFTop >> 1;
		num = ((!(AI0 > 0f)) ? (num + WoFBottom >> 1) : (num + WoFTop >> 1));
		num -= Height >> 1;
		if (XYWH.Y > num + 1)
		{
			Velocity.Y = -1f;
		}
		else if (XYWH.Y < num - 1)
		{
			Velocity.Y = 1f;
		}
		else
		{
			Velocity.Y = 0f;
			XYWH.Y = num;
			Position.Y = num;
		}
		if (Velocity.Y > 5f)
		{
			Velocity.Y = 5f;
		}
		else if (Velocity.Y < -5f)
		{
			Velocity.Y = -5f;
		}
		Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
		float num2 = Main.PlayerSet[Target].Position.X + 10f - vector.X;
		float num3 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
		float num4 = (float)Math.Sqrt(num2 * num2 + num3 * num3);
		num2 *= num4;
		num3 *= num4;
		bool flag = true;
		if (Direction > 0)
		{
			if (Main.PlayerSet[Target].XYWH.X + 10 > XYWH.X + (Width >> 1))
			{
				Rotation = (float)Math.Atan2(0f - num3, 0f - num2) + 3.14f;
			}
			else
			{
				Rotation = 0f;
				flag = false;
			}
		}
		else if (Main.PlayerSet[Target].XYWH.X + 10 < XYWH.X + (Width >> 1))
		{
			Rotation = (float)Math.Atan2(num3, num2) + 3.14f;
		}
		else
		{
			Rotation = 0f;
			flag = false;
		}
		if (Main.NetMode == 1)
		{
			return;
		}
		int num5 = 4;
		LocalAI1++;
		if ((double)Main.NPCSet[WoF].Life < (double)Main.NPCSet[WoF].LifeMax * 0.75)
		{
			LocalAI1++;
			num5++;
		}
		if ((double)Main.NPCSet[WoF].Life < (double)Main.NPCSet[WoF].LifeMax * 0.5)
		{
			LocalAI1++;
			num5++;
		}
		if ((double)Main.NPCSet[WoF].Life < (double)Main.NPCSet[WoF].LifeMax * 0.25)
		{
			LocalAI1++;
			num5 += 2;
		}
		if ((double)Main.NPCSet[WoF].Life < (double)Main.NPCSet[WoF].LifeMax * 0.1)
		{
			LocalAI1 += 2;
			num5 += 3;
		}
		if (LocalAI2 == 0)
		{
			if (LocalAI1 > 600)
			{
				LocalAI2 = 1;
				LocalAI1 = 0;
			}
		}
		else
		{
			if (LocalAI1 <= 45 || !Collision.CanHit(ref XYWH, ref Main.PlayerSet[Target].XYWH))
			{
				return;
			}
			LocalAI1 = 0;
			LocalAI2++;
			if (LocalAI2 >= num5)
			{
				LocalAI2 = 0;
			}
			if (flag)
			{
				float num6 = 9f;
				int num7 = 11;
				if ((double)Main.NPCSet[WoF].Life < (double)Main.NPCSet[WoF].LifeMax * 0.5)
				{
					num7++;
					num6++;
				}
				if ((double)Main.NPCSet[WoF].Life < (double)Main.NPCSet[WoF].LifeMax * 0.25)
				{
					num7++;
					num6++;
				}
				if ((double)Main.NPCSet[WoF].Life < (double)Main.NPCSet[WoF].LifeMax * 0.1)
				{
					num7 += 2;
					num6 += 2f;
				}
				vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
				num2 = Main.PlayerSet[Target].Position.X + 10f - vector.X;
				num3 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
				num4 = (float)Math.Sqrt(num2 * num2 + num3 * num3);
				num4 = num6 / num4;
				num2 *= num4;
				num3 *= num4;
				vector.X += num2;
				vector.Y += num3;
				Projectile.NewProjectile(vector.X, vector.Y, num2, num3, 83, num7, 0f);
			}
		}
	}

	private void WallOfFleshTentacleAI()
	{
		if (WoF < 0)
		{
			Active = 0;
			return;
		}
		if (WasJustHit)
		{
			AI1 = 10f;
		}
		TargetClosest();
		float num = 0.1f;
		float num2 = 300f;
		if (Main.NPCSet[WoF].Life < Main.NPCSet[WoF].LifeMax >> 2)
		{
			Damage = 75;
			Defense = 40;
			num2 = 900f;
		}
		else if (Main.NPCSet[WoF].Life < Main.NPCSet[WoF].LifeMax >> 1)
		{
			Damage = 60;
			Defense = 30;
			num2 = 700f;
		}
		else if (Main.NPCSet[WoF].Life < (Main.NPCSet[WoF].LifeMax >> 1) + (Main.NPCSet[WoF].LifeMax >> 2))
		{
			Damage = 45;
			Defense = 20;
			num2 = 500f;
		}
		float num3 = Main.NPCSet[WoF].Position.X + (float)(Main.NPCSet[WoF].Width >> 1);
		float y = Main.NPCSet[WoF].Position.Y;
		float num4 = WoFBottom - WoFTop;
		y = (float)WoFTop + num4 * AI0;
		AI2++;
		if (AI2 > 100f)
		{
			num2 = (int)(num2 * 1.3f);
			if (AI2 > 200f)
			{
				AI2 = 0f;
			}
		}
		Vector2 vector = new Vector2(num3, y);
		float num5 = Main.PlayerSet[Target].Position.X + 10f - (float)(Width >> 1) - vector.X;
		float num6 = Main.PlayerSet[Target].Position.Y + 21f - (float)(Height >> 1) - vector.Y;
		if (AI1 == 0f)
		{
			float num7 = num5 * num5 + num6 * num6;
			if (num7 > num2 * num2)
			{
				num7 = num2 / (float)Math.Sqrt(num7);
				num5 *= num7;
				num6 *= num7;
			}
			if (Position.X < num3 + num5)
			{
				Velocity.X += num;
				if (Velocity.X < 0f && num5 > 0f)
				{
					Velocity.X += num * 2.5f;
				}
			}
			else if (Position.X > num3 + num5)
			{
				Velocity.X -= num;
				if (Velocity.X > 0f && num5 < 0f)
				{
					Velocity.X -= num * 2.5f;
				}
			}
			if (Position.Y < y + num6)
			{
				Velocity.Y += num;
				if (Velocity.Y < 0f && num6 > 0f)
				{
					Velocity.Y += num * 2.5f;
				}
			}
			else if (Position.Y > y + num6)
			{
				Velocity.Y -= num;
				if (Velocity.Y > 0f && num6 < 0f)
				{
					Velocity.Y -= num * 2.5f;
				}
			}
			if (Velocity.X > 4f)
			{
				Velocity.X = 4f;
			}
			else if (Velocity.X < -4f)
			{
				Velocity.X = -4f;
			}
			if (Velocity.Y > 4f)
			{
				Velocity.Y = 4f;
			}
			else if (Velocity.Y < -4f)
			{
				Velocity.Y = -4f;
			}
		}
		else if (AI1 > 0f)
		{
			AI1--;
		}
		else
		{
			AI1 = 0f;
		}
		if (num5 > 0f)
		{
			SpriteDirection = 1;
			Rotation = (float)Math.Atan2(num6, num5);
		}
		else if (num5 < 0f)
		{
			SpriteDirection = -1;
			Rotation = (float)(Math.Atan2(num6, num5) + Math.PI);
		}
		Lighting.AddLight(XYWH.X + (Width >> 1) >> 4, XYWH.Y + (Height >> 1) >> 4, new Vector3(0.3f, 0.2f, 0.1f));
	}

	private unsafe void RetinazerAI()
	{
		if (Target == 8 || Main.PlayerSet[Target].IsDead || Main.PlayerSet[Target].Active == 0)
		{
			TargetClosest();
		}
		bool isDead = Main.PlayerSet[Target].IsDead;
		float num = Position.X + (float)(Width >> 1) - Main.PlayerSet[Target].Position.X - 10f;
		float num2 = (float)Math.Atan2(Position.Y + (float)(int)Height - 59f - Main.PlayerSet[Target].Position.Y - 21f, num) + 1.57f;
		if (num2 < 0f)
		{
			num2 += 6.283f;
		}
		else if (num2 > 6.283f)
		{
			num2 -= 6.283f;
		}
		if (Rotation < num2)
		{
			if ((double)(num2 - Rotation) > Math.PI)
			{
				Rotation -= 0.1f;
			}
			else
			{
				Rotation += 0.1f;
			}
		}
		else if (Rotation > num2)
		{
			if ((double)(Rotation - num2) > Math.PI)
			{
				Rotation += 0.1f;
			}
			else
			{
				Rotation -= 0.1f;
			}
		}
		if (Rotation > num2 - 0.1f && Rotation < num2 + 0.1f)
		{
			Rotation = num2;
		}
		if (Rotation < 0f)
		{
			Rotation += 6.283f;
		}
		else if (Rotation > 6.283f)
		{
			Rotation -= 6.283f;
		}
		if (Rotation > num2 - 0.1f && Rotation < num2 + 0.1f)
		{
			Rotation = num2;
		}
		if (Main.Rand.Next(6) == 0)
		{
			Dust* ptr = Main.DustSet.NewDust(XYWH.X, XYWH.Y + (Height >> 2), Width, Height >> 1, 5, Velocity.X, 2.0);
			if (ptr != null)
			{
				ptr->Velocity.X *= 0.5f;
				ptr->Velocity.Y *= 0.1f;
			}
		}
		if (Main.GameTime.DayTime | isDead)
		{
			Velocity.Y -= 0.04f;
			if (TimeLeft > 10)
			{
				TimeLeft = 10;
			}
			return;
		}
		if (AI0 == 0f)
		{
			if (AI1 == 0f)
			{
				float num3 = 0.1f;
				int num4 = 1;
				if (XYWH.X + (Width >> 1) < Main.PlayerSet[Target].XYWH.X + 20)
				{
					num4 = -1;
				}
				Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
				float num5 = Main.PlayerSet[Target].Position.X + 10f + (float)(num4 * 300) - vector.X;
				float num6 = Main.PlayerSet[Target].Position.Y + 21f - 300f - vector.Y;
				float num7 = (float)Math.Sqrt(num5 * num5 + num6 * num6);
				float num8 = num7;
				num7 = 7f / num7;
				num5 *= num7;
				num6 *= num7;
				if (Velocity.X < num5)
				{
					Velocity.X += num3;
					if (Velocity.X < 0f && num5 > 0f)
					{
						Velocity.X += num3;
					}
				}
				else if (Velocity.X > num5)
				{
					Velocity.X -= num3;
					if (Velocity.X > 0f && num5 < 0f)
					{
						Velocity.X -= num3;
					}
				}
				if (Velocity.Y < num6)
				{
					Velocity.Y += num3;
					if (Velocity.Y < 0f && num6 > 0f)
					{
						Velocity.Y += num3;
					}
				}
				else if (Velocity.Y > num6)
				{
					Velocity.Y -= num3;
					if (Velocity.Y > 0f && num6 < 0f)
					{
						Velocity.Y -= num3;
					}
				}
				AI2++;
				if (AI2 >= 600f)
				{
					AI1 = 1f;
					AI2 = 0f;
					AI3 = 0f;
					Target = 8;
					ShouldNetUpdate = true;
				}
				else if (XYWH.Y + Height < Main.PlayerSet[Target].XYWH.Y && num8 < 400f)
				{
					if (!Main.PlayerSet[Target].IsDead)
					{
						AI3++;
					}
					if (AI3 >= 60f)
					{
						AI3 = 0f;
						vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
						num5 = Main.PlayerSet[Target].Position.X + 10f - vector.X;
						num6 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
						if (Main.NetMode != 1)
						{
							num7 = (float)Math.Sqrt(num5 * num5 + num6 * num6);
							num7 = 9f / num7;
							num5 *= num7;
							num6 *= num7;
							num5 += (float)Main.Rand.Next(-40, 41) * 0.08f;
							num6 += (float)Main.Rand.Next(-40, 41) * 0.08f;
							vector.X += num5 * 15f;
							vector.Y += num6 * 15f;
							Projectile.NewProjectile(vector.X, vector.Y, num5, num6, 83, 20, 0f);
						}
					}
				}
			}
			else if (AI1 == 1f)
			{
				Rotation = num2;
				Vector2 vector2 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
				float num9 = Main.PlayerSet[Target].Position.X + 10f - vector2.X;
				float num10 = Main.PlayerSet[Target].Position.Y + 21f - vector2.Y;
				float num11 = (float)Math.Sqrt(num9 * num9 + num10 * num10);
				num11 = 12f / num11;
				Velocity.X = num9 * num11;
				Velocity.Y = num10 * num11;
				AI1 = 2f;
			}
			else if (AI1 == 2f)
			{
				AI2++;
				if (AI2 >= 25f)
				{
					Velocity.X *= 0.96f;
					Velocity.Y *= 0.96f;
					if ((double)Velocity.X > -0.1 && (double)Velocity.X < 0.1)
					{
						Velocity.X = 0f;
					}
					if ((double)Velocity.Y > -0.1 && (double)Velocity.Y < 0.1)
					{
						Velocity.Y = 0f;
					}
				}
				else
				{
					Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X) - 1.57f;
				}
				if (AI2 >= 70f)
				{
					AI3++;
					AI2 = 0f;
					Target = 8;
					Rotation = num2;
					if (AI3 >= 4f)
					{
						AI1 = 0f;
						AI3 = 0f;
					}
					else
					{
						AI1 = 1f;
					}
				}
			}
			if ((double)Life < (double)LifeMax * 0.5)
			{
				AI0 = 1f;
				AI1 = 0f;
				AI2 = 0f;
				AI3 = 0f;
				ShouldNetUpdate = true;
			}
			return;
		}
		if (AI0 == 1f || AI0 == 2f)
		{
			if (AI0 == 1f)
			{
				AI2 += 0.005f;
				if ((double)AI2 > 0.5)
				{
					AI2 = 0.5f;
				}
			}
			else
			{
				AI2 -= 0.005f;
				if (AI2 < 0f)
				{
					AI2 = 0f;
				}
			}
			Rotation += AI2;
			AI1++;
			if (AI1 == 100f)
			{
				AI0++;
				AI1 = 0f;
				if (AI0 == 3f)
				{
					AI2 = 0f;
				}
				else
				{
					Main.PlaySound(3, XYWH.X, XYWH.Y);
					Main.PlaySound(15, XYWH.X, XYWH.Y, 0);
					for (int i = 0; i < 2; i++)
					{
						Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 143);
						Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 7);
						Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 6);
					}
					for (int j = 0; j < 16; j++)
					{
						if (null == Main.DustSet.NewDust(5, ref XYWH, (float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f))
						{
							break;
						}
					}
				}
			}
			Main.DustSet.NewDust(5, ref XYWH, (float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f);
			Velocity.X *= 0.98f;
			Velocity.Y *= 0.98f;
			if ((double)Velocity.X > -0.1 && (double)Velocity.X < 0.1)
			{
				Velocity.X = 0f;
			}
			if ((double)Velocity.Y > -0.1 && (double)Velocity.Y < 0.1)
			{
				Velocity.Y = 0f;
			}
			return;
		}
		Damage = (int)((double)DefDamage * 1.5);
		Defense = DefDefense + 15;
		SoundHit = 4;
		if (AI1 == 0f)
		{
			float num12 = 0.15f;
			Vector2 vector3 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num13 = Main.PlayerSet[Target].Position.X + 10f - vector3.X;
			float num14 = Main.PlayerSet[Target].Position.Y + 21f - 300f - vector3.Y;
			float num15 = (float)Math.Sqrt(num13 * num13 + num14 * num14);
			num15 = 8f / num15;
			num13 *= num15;
			num14 *= num15;
			if (Velocity.X < num13)
			{
				Velocity.X += num12;
				if (Velocity.X < 0f && num13 > 0f)
				{
					Velocity.X += num12;
				}
			}
			else if (Velocity.X > num13)
			{
				Velocity.X -= num12;
				if (Velocity.X > 0f && num13 < 0f)
				{
					Velocity.X -= num12;
				}
			}
			if (Velocity.Y < num14)
			{
				Velocity.Y += num12;
				if (Velocity.Y < 0f && num14 > 0f)
				{
					Velocity.Y += num12;
				}
			}
			else if (Velocity.Y > num14)
			{
				Velocity.Y -= num12;
				if (Velocity.Y > 0f && num14 < 0f)
				{
					Velocity.Y -= num12;
				}
			}
			AI2++;
			if (AI2 >= 300f)
			{
				AI1 = 1f;
				AI2 = 0f;
				AI3 = 0f;
				TargetClosest();
				ShouldNetUpdate = true;
			}
			vector3 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			num13 = Main.PlayerSet[Target].Position.X + 10f - vector3.X;
			num14 = Main.PlayerSet[Target].Position.Y + 21f - vector3.Y;
			Rotation = (float)Math.Atan2(num14, num13) - 1.57f;
			if (Main.NetMode != 1)
			{
				LocalAI1++;
				if ((double)Life < (double)LifeMax * 0.75)
				{
					LocalAI1++;
				}
				if ((double)Life < (double)LifeMax * 0.5)
				{
					LocalAI1++;
				}
				if ((double)Life < (double)LifeMax * 0.25)
				{
					LocalAI1++;
				}
				if ((double)Life < (double)LifeMax * 0.1)
				{
					LocalAI1 += 2;
				}
				if (LocalAI1 > 140 && Collision.CanHit(ref XYWH, ref Main.PlayerSet[Target].XYWH))
				{
					LocalAI1 = 0;
					num15 = (float)Math.Sqrt(num13 * num13 + num14 * num14);
					num15 = 9f / num15;
					num13 *= num15;
					num14 *= num15;
					vector3.X += num13 * 15f;
					vector3.Y += num14 * 15f;
					Projectile.NewProjectile(vector3.X, vector3.Y, num13, num14, 100, 25, 0f);
				}
			}
			return;
		}
		int num16 = 1;
		if (XYWH.X + (Width >> 1) < Main.PlayerSet[Target].XYWH.X + 20)
		{
			num16 = -1;
		}
		float num17 = 0.2f;
		Vector2 vector4 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
		float num18 = Main.PlayerSet[Target].Position.X + 10f + (float)(num16 * 340) - vector4.X;
		float num19 = Main.PlayerSet[Target].Position.Y + 21f - vector4.Y;
		float num20 = (float)Math.Sqrt(num18 * num18 + num19 * num19);
		num20 = 8f / num20;
		num18 *= num20;
		num19 *= num20;
		if (Velocity.X < num18)
		{
			Velocity.X += num17;
			if (Velocity.X < 0f && num18 > 0f)
			{
				Velocity.X += num17;
			}
		}
		else if (Velocity.X > num18)
		{
			Velocity.X -= num17;
			if (Velocity.X > 0f && num18 < 0f)
			{
				Velocity.X -= num17;
			}
		}
		if (Velocity.Y < num19)
		{
			Velocity.Y += num17;
			if (Velocity.Y < 0f && num19 > 0f)
			{
				Velocity.Y += num17;
			}
		}
		else if (Velocity.Y > num19)
		{
			Velocity.Y -= num17;
			if (Velocity.Y > 0f && num19 < 0f)
			{
				Velocity.Y -= num17;
			}
		}
		vector4 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
		num18 = Main.PlayerSet[Target].Position.X + 10f - vector4.X;
		num19 = Main.PlayerSet[Target].Position.Y + 21f - vector4.Y;
		Rotation = (float)Math.Atan2(num19, num18) - 1.57f;
		if (Main.NetMode != 1)
		{
			LocalAI1++;
			if ((double)Life < (double)LifeMax * 0.75)
			{
				LocalAI1++;
			}
			if ((double)Life < (double)LifeMax * 0.5)
			{
				LocalAI1++;
			}
			if ((double)Life < (double)LifeMax * 0.25)
			{
				LocalAI1++;
			}
			if ((double)Life < (double)LifeMax * 0.1)
			{
				LocalAI1 += 2;
			}
			if (LocalAI1 > 45 && Collision.CanHit(ref XYWH, ref Main.PlayerSet[Target].XYWH))
			{
				LocalAI1 = 0;
				num20 = (float)Math.Sqrt(num18 * num18 + num19 * num19);
				num20 = 9f / num20;
				num18 *= num20;
				num19 *= num20;
				vector4.X += num18 * 15f;
				vector4.Y += num19 * 15f;
				Projectile.NewProjectile(vector4.X, vector4.Y, num18, num19, 100, 20, 0f);
			}
		}
		AI2++;
		if (AI2 >= 200f)
		{
			AI1 = 0f;
			AI2 = 0f;
			AI3 = 0f;
			TargetClosest();
			ShouldNetUpdate = true;
		}
	}

	private unsafe void SpazmatismAI()
	{
		if (Target == 8 || Main.PlayerSet[Target].IsDead || Main.PlayerSet[Target].Active == 0)
		{
			TargetClosest();
		}
		bool isDead = Main.PlayerSet[Target].IsDead;
		float num = Position.X + (float)(Width >> 1) - Main.PlayerSet[Target].Position.X - 10f;
		float num2 = (float)Math.Atan2(Position.Y + (float)(int)Height - 59f - Main.PlayerSet[Target].Position.Y - 21f, num) + 1.57f;
		if (num2 < 0f)
		{
			num2 += 6.283f;
		}
		else if (num2 > 6.283f)
		{
			num2 -= 6.283f;
		}
		if (Rotation < num2)
		{
			if ((double)(num2 - Rotation) > Math.PI)
			{
				Rotation -= 0.15f;
			}
			else
			{
				Rotation += 0.15f;
			}
		}
		else if (Rotation > num2)
		{
			if ((double)(Rotation - num2) > Math.PI)
			{
				Rotation += 0.15f;
			}
			else
			{
				Rotation -= 0.15f;
			}
		}
		if (Rotation > num2 - 0.15f && Rotation < num2 + 0.15f)
		{
			Rotation = num2;
		}
		if (Rotation < 0f)
		{
			Rotation += 6.283f;
		}
		else if (Rotation > 6.283f)
		{
			Rotation -= 6.283f;
		}
		if (Rotation > num2 - 0.15f && Rotation < num2 + 0.15f)
		{
			Rotation = num2;
		}
		if (Main.Rand.Next(6) == 0)
		{
			Dust* ptr = Main.DustSet.NewDust(XYWH.X, XYWH.Y + (Height >> 2), Width, Height >> 1, 5, Velocity.X, 2.0);
			if (ptr != null)
			{
				ptr->Velocity.X *= 0.5f;
				ptr->Velocity.Y *= 0.1f;
			}
		}
		if (Main.GameTime.DayTime | isDead)
		{
			Velocity.Y -= 0.04f;
			if (TimeLeft > 10)
			{
				TimeLeft = 10;
			}
			return;
		}
		if (AI0 == 0f)
		{
			if (AI1 == 0f)
			{
				TargetClosest();
				float num3 = 0.4f;
				int num4 = 1;
				if (XYWH.X + (Width >> 1) < Main.PlayerSet[Target].XYWH.X + 20)
				{
					num4 = -1;
				}
				Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
				float num5 = Main.PlayerSet[Target].Position.X + 10f + (float)(num4 * 400) - vector.X;
				float num6 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
				float num7 = (float)Math.Sqrt(num5 * num5 + num6 * num6);
				num7 = 12f / num7;
				num5 *= num7;
				num6 *= num7;
				if (Velocity.X < num5)
				{
					Velocity.X += num3;
					if (Velocity.X < 0f && num5 > 0f)
					{
						Velocity.X += num3;
					}
				}
				else if (Velocity.X > num5)
				{
					Velocity.X -= num3;
					if (Velocity.X > 0f && num5 < 0f)
					{
						Velocity.X -= num3;
					}
				}
				if (Velocity.Y < num6)
				{
					Velocity.Y += num3;
					if (Velocity.Y < 0f && num6 > 0f)
					{
						Velocity.Y += num3;
					}
				}
				else if (Velocity.Y > num6)
				{
					Velocity.Y -= num3;
					if (Velocity.Y > 0f && num6 < 0f)
					{
						Velocity.Y -= num3;
					}
				}
				AI2++;
				if (AI2 >= 600f)
				{
					AI1 = 1f;
					AI2 = 0f;
					AI3 = 0f;
					Target = 8;
					ShouldNetUpdate = true;
				}
				else
				{
					if (!Main.PlayerSet[Target].IsDead)
					{
						AI3++;
					}
					if (AI3 >= 60f)
					{
						AI3 = 0f;
						vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
						num5 = Main.PlayerSet[Target].Position.X + 10f - vector.X;
						num6 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
						if (Main.NetMode != 1)
						{
							num7 = (float)Math.Sqrt(num5 * num5 + num6 * num6);
							num7 = 12f / num7;
							num5 *= num7;
							num6 *= num7;
							num5 += (float)Main.Rand.Next(-40, 41) * 0.05f;
							num6 += (float)Main.Rand.Next(-40, 41) * 0.05f;
							vector.X += num5 * 4f;
							vector.Y += num6 * 4f;
							Projectile.NewProjectile(vector.X, vector.Y, num5, num6, 96, 25, 0f);
						}
					}
				}
			}
			else if (AI1 == 1f)
			{
				Rotation = num2;
				Vector2 vector2 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
				float num8 = Main.PlayerSet[Target].Position.X + 10f - vector2.X;
				float num9 = Main.PlayerSet[Target].Position.Y + 21f - vector2.Y;
				float num10 = (float)Math.Sqrt(num8 * num8 + num9 * num9);
				num10 = 13f / num10;
				Velocity.X = num8 * num10;
				Velocity.Y = num9 * num10;
				AI1 = 2f;
			}
			else if (AI1 == 2f)
			{
				AI2++;
				if (AI2 >= 8f)
				{
					Velocity.X *= 0.9f;
					Velocity.Y *= 0.9f;
					if ((double)Velocity.X > -0.1 && (double)Velocity.X < 0.1)
					{
						Velocity.X = 0f;
					}
					if ((double)Velocity.Y > -0.1 && (double)Velocity.Y < 0.1)
					{
						Velocity.Y = 0f;
					}
				}
				else
				{
					Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X) - 1.57f;
				}
				if (AI2 >= 42f)
				{
					AI3++;
					AI2 = 0f;
					Target = 8;
					Rotation = num2;
					if (AI3 >= 10f)
					{
						AI1 = 0f;
						AI3 = 0f;
					}
					else
					{
						AI1 = 1f;
					}
				}
			}
			if ((double)Life < (double)LifeMax * 0.5)
			{
				AI0 = 1f;
				AI1 = 0f;
				AI2 = 0f;
				AI3 = 0f;
				ShouldNetUpdate = true;
			}
			return;
		}
		if (AI0 == 1f || AI0 == 2f)
		{
			if (AI0 == 1f)
			{
				AI2 += 0.005f;
				if ((double)AI2 > 0.5)
				{
					AI2 = 0.5f;
				}
			}
			else
			{
				AI2 -= 0.005f;
				if (AI2 < 0f)
				{
					AI2 = 0f;
				}
			}
			Rotation += AI2;
			AI1++;
			if (AI1 == 100f)
			{
				AI0++;
				AI1 = 0f;
				if (AI0 == 3f)
				{
					AI2 = 0f;
				}
				else
				{
					Main.PlaySound(3, XYWH.X, XYWH.Y);
					Main.PlaySound(15, XYWH.X, XYWH.Y, 0);
					for (int i = 0; i < 2; i++)
					{
						Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 144);
						Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 7);
						Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 6);
					}
					for (int j = 0; j < 16; j++)
					{
						if (null == Main.DustSet.NewDust(5, ref XYWH, (float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f))
						{
							break;
						}
					}
				}
			}
			Main.DustSet.NewDust(5, ref XYWH, (float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f);
			Velocity.X *= 0.98f;
			Velocity.Y *= 0.98f;
			if ((double)Velocity.X > -0.1 && (double)Velocity.X < 0.1)
			{
				Velocity.X = 0f;
			}
			if ((double)Velocity.Y > -0.1 && (double)Velocity.Y < 0.1)
			{
				Velocity.Y = 0f;
			}
			return;
		}
		SoundHit = 4;
		Damage = DefDamage + (DefDamage >> 1);
		Defense = DefDefense + 25;
		if (AI1 == 0f)
		{
			float num11 = 0.1f;
			int num12 = 1;
			if (XYWH.X + (Width >> 1) < Main.PlayerSet[Target].XYWH.X + 20)
			{
				num12 = -1;
			}
			Vector2 vector3 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num13 = Main.PlayerSet[Target].Position.X + 10f + (float)(num12 * 180) - vector3.X;
			float num14 = Main.PlayerSet[Target].Position.Y + 21f - vector3.Y;
			float num15 = (float)Math.Sqrt(num13 * num13 + num14 * num14);
			num15 = 4f / num15;
			num13 *= num15;
			num14 *= num15;
			if (Velocity.X < num13)
			{
				Velocity.X += num11;
				if (Velocity.X < 0f && num13 > 0f)
				{
					Velocity.X += num11;
				}
			}
			else if (Velocity.X > num13)
			{
				Velocity.X -= num11;
				if (Velocity.X > 0f && num13 < 0f)
				{
					Velocity.X -= num11;
				}
			}
			if (Velocity.Y < num14)
			{
				Velocity.Y += num11;
				if (Velocity.Y < 0f && num14 > 0f)
				{
					Velocity.Y += num11;
				}
			}
			else if (Velocity.Y > num14)
			{
				Velocity.Y -= num11;
				if (Velocity.Y > 0f && num14 < 0f)
				{
					Velocity.Y -= num11;
				}
			}
			AI2++;
			if (AI2 >= 400f)
			{
				AI1 = 1f;
				AI2 = 0f;
				AI3 = 0f;
				Target = 8;
				ShouldNetUpdate = true;
			}
			if (!Collision.CanHit(ref XYWH, ref Main.PlayerSet[Target].XYWH))
			{
				return;
			}
			LocalAI2++;
			if (LocalAI2 > 22)
			{
				LocalAI2 = 0;
				Main.PlaySound(2, XYWH.X, XYWH.Y, 34);
			}
			if (Main.NetMode != 1)
			{
				LocalAI1++;
				if ((float)Life < (float)LifeMax * 0.75f)
				{
					LocalAI1++;
				}
				if ((float)Life < (float)LifeMax * 0.5f)
				{
					LocalAI1++;
				}
				if ((float)Life < (float)LifeMax * 0.25f)
				{
					LocalAI1++;
				}
				if ((float)Life < (float)LifeMax * 0.1f)
				{
					LocalAI1 += 2;
				}
				if (LocalAI1 > 8)
				{
					LocalAI1 = 0;
					vector3 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
					num13 = Main.PlayerSet[Target].Position.X + 10f - vector3.X;
					num14 = Main.PlayerSet[Target].Position.Y + 21f - vector3.Y;
					num15 = (float)Math.Sqrt(num13 * num13 + num14 * num14);
					num15 = 6f / num15;
					num13 *= num15;
					num14 *= num15;
					num14 += (float)Main.Rand.Next(-40, 41) * 0.01f;
					num13 += (float)Main.Rand.Next(-40, 41) * 0.01f;
					num14 += Velocity.Y * 0.5f;
					num13 += Velocity.X * 0.5f;
					vector3.X -= num13 * 1f;
					vector3.Y -= num14 * 1f;
					Projectile.NewProjectile(vector3.X, vector3.Y, num13, num14, 101, 30, 0f);
				}
			}
		}
		else if (AI1 == 1f)
		{
			Main.PlaySound(15, XYWH.X, XYWH.Y, 0);
			Rotation = num2;
			Vector2 vector4 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num16 = Main.PlayerSet[Target].Position.X + 10f - vector4.X;
			float num17 = Main.PlayerSet[Target].Position.Y + 21f - vector4.Y;
			float num18 = (float)Math.Sqrt(num16 * num16 + num17 * num17);
			num18 = 14f / num18;
			Velocity.X = num16 * num18;
			Velocity.Y = num17 * num18;
			AI1 = 2f;
		}
		else
		{
			if (AI1 != 2f)
			{
				return;
			}
			AI2++;
			if (AI2 >= 50f)
			{
				Velocity.X *= 0.93f;
				Velocity.Y *= 0.93f;
				if ((double)Velocity.X > -0.1 && (double)Velocity.X < 0.1)
				{
					Velocity.X = 0f;
				}
				if ((double)Velocity.Y > -0.1 && (double)Velocity.Y < 0.1)
				{
					Velocity.Y = 0f;
				}
			}
			else
			{
				Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X) - 1.57f;
			}
			if (AI2 >= 80f)
			{
				AI3++;
				AI2 = 0f;
				Target = 8;
				Rotation = num2;
				if (AI3 >= 6f)
				{
					AI1 = 0f;
					AI3 = 0f;
				}
				else
				{
					AI1 = 1f;
				}
			}
		}
	}

	private void SkeletronPrimeAI()
	{
		Damage = DefDamage;
		Defense = DefDefense;
		if (AI0 == 0f && Main.NetMode != 1)
		{
			TargetClosest();
			AI0 = 1f;
			if (Type != 68)
			{
				int num = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + (Height >> 1), 128, WhoAmI);
				Main.NPCSet[num].AI0 = -1f;
				Main.NPCSet[num].AI1 = WhoAmI;
				Main.NPCSet[num].Target = Target;
				Main.NPCSet[num].ShouldNetUpdate = true;
				num = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + (Height >> 1), 129, WhoAmI);
				Main.NPCSet[num].AI0 = 1f;
				Main.NPCSet[num].AI1 = WhoAmI;
				Main.NPCSet[num].Target = Target;
				Main.NPCSet[num].ShouldNetUpdate = true;
				num = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + (Height >> 1), 130, WhoAmI);
				Main.NPCSet[num].AI0 = -1f;
				Main.NPCSet[num].AI1 = WhoAmI;
				Main.NPCSet[num].Target = Target;
				Main.NPCSet[num].AI3 = 150f;
				Main.NPCSet[num].ShouldNetUpdate = true;
				num = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + (Height >> 1), 131, WhoAmI);
				Main.NPCSet[num].AI0 = 1f;
				Main.NPCSet[num].AI1 = WhoAmI;
				Main.NPCSet[num].Target = Target;
				Main.NPCSet[num].ShouldNetUpdate = true;
				Main.NPCSet[num].AI3 = 150f;
			}
		}
		if (Type == 68 && AI1 != 3f && AI1 != 2f)
		{
			Main.PlaySound(15, XYWH.X, XYWH.Y, 0);
			AI1 = 2f;
		}
		if (Main.PlayerSet[Target].IsDead || Math.Abs(XYWH.X - Main.PlayerSet[Target].XYWH.X) > 6000 || Math.Abs(XYWH.Y - Main.PlayerSet[Target].XYWH.Y) > 6000)
		{
			TargetClosest();
			if (Main.PlayerSet[Target].IsDead || Math.Abs(XYWH.X - Main.PlayerSet[Target].XYWH.X) > 6000 || Math.Abs(XYWH.Y - Main.PlayerSet[Target].XYWH.Y) > 6000)
			{
				AI1 = 3f;
			}
		}
		if (Main.GameTime.DayTime && AI1 != 3f && AI1 != 2f)
		{
			AI1 = 2f;
			Main.PlaySound(15, XYWH.X, XYWH.Y, 0);
		}
		if (AI1 == 0f)
		{
			AI2++;
			if (AI2 >= 600f)
			{
				AI2 = 0f;
				AI1 = 1f;
				TargetClosest();
				ShouldNetUpdate = true;
			}
			Rotation = Velocity.X * (1f / 15f);
			if (XYWH.Y > Main.PlayerSet[Target].XYWH.Y - 200)
			{
				if (Velocity.Y > 0f)
				{
					Velocity.Y *= 0.98f;
				}
				Velocity.Y -= 0.1f;
				if (Velocity.Y > 2f)
				{
					Velocity.Y = 2f;
				}
			}
			else if (XYWH.Y < Main.PlayerSet[Target].XYWH.Y - 500)
			{
				if (Velocity.Y < 0f)
				{
					Velocity.Y *= 0.98f;
				}
				Velocity.Y += 0.1f;
				if (Velocity.Y < -2f)
				{
					Velocity.Y = -2f;
				}
			}
			if (XYWH.X + (Width >> 1) > Main.PlayerSet[Target].XYWH.X + 10 + 100)
			{
				if (Velocity.X > 0f)
				{
					Velocity.X *= 0.98f;
				}
				Velocity.X -= 0.1f;
				if (Velocity.X > 8f)
				{
					Velocity.X = 8f;
				}
			}
			else if (XYWH.X + (Width >> 1) < Main.PlayerSet[Target].XYWH.X + 10 - 100)
			{
				if (Velocity.X < 0f)
				{
					Velocity.X *= 0.98f;
				}
				Velocity.X += 0.1f;
				if (Velocity.X < -8f)
				{
					Velocity.X = -8f;
				}
			}
		}
		else if (AI1 == 1f)
		{
			Defense *= 2;
			Damage *= 2;
			AI2++;
			if (AI2 == 2f)
			{
				Main.PlaySound(15, XYWH.X, XYWH.Y, 0);
			}
			if (AI2 >= 400f)
			{
				AI2 = 0f;
				AI1 = 0f;
			}
			Rotation += (float)Direction * 0.3f;
			Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num2 = Main.PlayerSet[Target].Position.X + 10f - vector.X;
			float num3 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
			float num4 = (float)Math.Sqrt(num2 * num2 + num3 * num3);
			num4 = 2f / num4;
			Velocity.X = num2 * num4;
			Velocity.Y = num3 * num4;
		}
		else if (AI1 == 2f)
		{
			Damage = 9999;
			Defense = 9999;
			Rotation += (float)Direction * 0.3f;
			Vector2 vector2 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num5 = Main.PlayerSet[Target].Position.X + 10f - vector2.X;
			float num6 = Main.PlayerSet[Target].Position.Y + 21f - vector2.Y;
			float num7 = (float)Math.Sqrt(num5 * num5 + num6 * num6);
			num7 = 8f / num7;
			Velocity.X = num5 * num7;
			Velocity.Y = num6 * num7;
		}
		else if (AI1 == 3f)
		{
			Velocity.Y += 0.1f;
			if (Velocity.Y < 0f)
			{
				Velocity.Y *= 0.95f;
			}
			Velocity.X *= 0.95f;
			if (TimeLeft > 500)
			{
				TimeLeft = 500;
			}
		}
	}

	private void SkeletronPrimeSawHand()
	{
		Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
		float num = Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 200f * AI0 - vector.X;
		float num2 = Main.NPCSet[(int)AI1].Position.Y + 230f - vector.Y;
		float num3 = (float)Math.Sqrt(num * num + num2 * num2);
		if (AI2 != 99f)
		{
			if (num3 > 800f)
			{
				AI2 = 99f;
			}
		}
		else if (num3 < 400f)
		{
			AI2 = 0f;
		}
		SpriteDirection = (sbyte)(0f - AI0);
		if (Main.NPCSet[(int)AI1].Active == 0 || Main.NPCSet[(int)AI1].AIStyle != 32)
		{
			AI2 += 10f;
			if (AI2 > 50f || Main.NetMode != 2)
			{
				Life = -1;
				HitEffect();
				Active = 0;
				return;
			}
		}
		if (AI2 == 99f)
		{
			if (XYWH.Y > Main.NPCSet[(int)AI1].XYWH.Y)
			{
				if (Velocity.Y > 0f)
				{
					Velocity.Y *= 0.96f;
				}
				Velocity.Y -= 0.1f;
				if (Velocity.Y > 8f)
				{
					Velocity.Y = 8f;
				}
			}
			else if (XYWH.Y < Main.NPCSet[(int)AI1].XYWH.Y)
			{
				if (Velocity.Y < 0f)
				{
					Velocity.Y *= 0.96f;
				}
				Velocity.Y += 0.1f;
				if (Velocity.Y < -8f)
				{
					Velocity.Y = -8f;
				}
			}
			if (XYWH.X + (Width >> 1) > Main.NPCSet[(int)AI1].XYWH.X + (Main.NPCSet[(int)AI1].Width >> 1))
			{
				if (Velocity.X > 0f)
				{
					Velocity.X *= 0.96f;
				}
				Velocity.X -= 0.5f;
				if (Velocity.X > 12f)
				{
					Velocity.X = 12f;
				}
			}
			else if (XYWH.X + (Width >> 1) < Main.NPCSet[(int)AI1].XYWH.X + (Main.NPCSet[(int)AI1].Width >> 1))
			{
				if (Velocity.X < 0f)
				{
					Velocity.X *= 0.96f;
				}
				Velocity.X += 0.5f;
				if (Velocity.X < -12f)
				{
					Velocity.X = -12f;
				}
			}
		}
		else if (AI2 == 0f || AI2 == 3f)
		{
			if (Main.NPCSet[(int)AI1].AI1 == 3f && TimeLeft > 10)
			{
				TimeLeft = 10;
			}
			if (Main.NPCSet[(int)AI1].AI1 != 0f)
			{
				TargetClosest();
				if (Main.PlayerSet[Target].IsDead)
				{
					Velocity.Y += 0.1f;
					if (Velocity.Y > 16f)
					{
						Velocity.Y = 16f;
					}
				}
				else
				{
					Vector2 vector2 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
					float num4 = Main.PlayerSet[Target].Position.X + 10f - vector2.X;
					float num5 = Main.PlayerSet[Target].Position.Y + 21f - vector2.Y;
					float num6 = (float)Math.Sqrt(num4 * num4 + num5 * num5);
					num6 = 7f / num6;
					num4 *= num6;
					num5 *= num6;
					Rotation = (float)Math.Atan2(num5, num4) - 1.57f;
					if (Velocity.X > num4)
					{
						if (Velocity.X > 0f)
						{
							Velocity.X *= 0.97f;
						}
						Velocity.X -= 0.05f;
					}
					if (Velocity.X < num4)
					{
						if (Velocity.X < 0f)
						{
							Velocity.X *= 0.97f;
						}
						Velocity.X += 0.05f;
					}
					if (Velocity.Y > num5)
					{
						if (Velocity.Y > 0f)
						{
							Velocity.Y *= 0.97f;
						}
						Velocity.Y -= 0.05f;
					}
					if (Velocity.Y < num5)
					{
						if (Velocity.Y < 0f)
						{
							Velocity.Y *= 0.97f;
						}
						Velocity.Y += 0.05f;
					}
				}
				AI3++;
				if (AI3 >= 600f)
				{
					AI2 = 0f;
					AI3 = 0f;
					ShouldNetUpdate = true;
				}
			}
			else
			{
				AI3++;
				if (AI3 >= 300f)
				{
					AI2++;
					AI3 = 0f;
					ShouldNetUpdate = true;
				}
				if (XYWH.Y > Main.NPCSet[(int)AI1].XYWH.Y + 320)
				{
					if (Velocity.Y > 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y -= 0.04f;
					if (Velocity.Y > 3f)
					{
						Velocity.Y = 3f;
					}
				}
				else if (XYWH.Y < Main.NPCSet[(int)AI1].XYWH.Y + 260)
				{
					if (Velocity.Y < 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y += 0.04f;
					if (Velocity.Y < -3f)
					{
						Velocity.Y = -3f;
					}
				}
				if (XYWH.X + (Width >> 1) > Main.NPCSet[(int)AI1].XYWH.X + (Main.NPCSet[(int)AI1].Width >> 1))
				{
					if (Velocity.X > 0f)
					{
						Velocity.X *= 0.96f;
					}
					Velocity.X -= 0.3f;
					if (Velocity.X > 12f)
					{
						Velocity.X = 12f;
					}
				}
				else if (XYWH.X + (Width >> 1) < Main.NPCSet[(int)AI1].XYWH.X + (Main.NPCSet[(int)AI1].Width >> 1) - 250)
				{
					if (Velocity.X < 0f)
					{
						Velocity.X *= 0.96f;
					}
					Velocity.X += 0.3f;
					if (Velocity.X < -12f)
					{
						Velocity.X = -12f;
					}
				}
			}
			Vector2 vector3 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num7 = Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 200f * AI0 - vector3.X;
			float num8 = Main.NPCSet[(int)AI1].Position.Y + 230f - vector3.Y;
			Math.Sqrt(num7 * num7 + num8 * num8);
			Rotation = (float)Math.Atan2(num8, num7) + 1.57f;
		}
		else if (AI2 == 1f)
		{
			Vector2 vector4 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num9 = Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 200f * AI0 - vector4.X;
			float num10 = Main.NPCSet[(int)AI1].Position.Y + 230f - vector4.Y;
			float num11 = (float)Math.Sqrt(num9 * num9 + num10 * num10);
			Rotation = (float)Math.Atan2(num10, num9) + 1.57f;
			Velocity.X *= 0.95f;
			Velocity.Y -= 0.1f;
			if (Velocity.Y < -8f)
			{
				Velocity.Y = -8f;
			}
			if (XYWH.Y < Main.NPCSet[(int)AI1].XYWH.Y - 200)
			{
				TargetClosest();
				AI2 = 2f;
				vector4 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
				num9 = Main.PlayerSet[Target].Position.X + 10f - vector4.X;
				num10 = Main.PlayerSet[Target].Position.Y + 21f - vector4.Y;
				num11 = (float)Math.Sqrt(num9 * num9 + num10 * num10);
				num11 = 22f / num11;
				Velocity.X = num9 * num11;
				Velocity.Y = num10 * num11;
				ShouldNetUpdate = true;
			}
		}
		else if (AI2 == 2f)
		{
			if (Velocity.Y < 0f || XYWH.Y > Main.PlayerSet[Target].XYWH.Y)
			{
				AI2 = 3f;
			}
		}
		else if (AI2 == 4f)
		{
			TargetClosest();
			Vector2 vector5 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num12 = Main.PlayerSet[Target].Position.X + 10f - vector5.X;
			float num13 = Main.PlayerSet[Target].Position.Y + 21f - vector5.Y;
			float num14 = (float)Math.Sqrt(num12 * num12 + num13 * num13);
			num14 = 7f / num14;
			num12 *= num14;
			num13 *= num14;
			if (Velocity.X > num12)
			{
				if (Velocity.X > 0f)
				{
					Velocity.X *= 0.97f;
				}
				Velocity.X -= 0.05f;
			}
			if (Velocity.X < num12)
			{
				if (Velocity.X < 0f)
				{
					Velocity.X *= 0.97f;
				}
				Velocity.X += 0.05f;
			}
			if (Velocity.Y > num13)
			{
				if (Velocity.Y > 0f)
				{
					Velocity.Y *= 0.97f;
				}
				Velocity.Y -= 0.05f;
			}
			if (Velocity.Y < num13)
			{
				if (Velocity.Y < 0f)
				{
					Velocity.Y *= 0.97f;
				}
				Velocity.Y += 0.05f;
			}
			AI3++;
			if (AI3 >= 600f)
			{
				AI2 = 0f;
				AI3 = 0f;
				ShouldNetUpdate = true;
			}
			vector5 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			num12 = Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 200f * AI0 - vector5.X;
			num13 = Main.NPCSet[(int)AI1].Position.Y + 230f - vector5.Y;
			num14 = (float)Math.Sqrt(num12 * num12 + num13 * num13);
			Rotation = (float)Math.Atan2(num13, num12) + 1.57f;
		}
		else if (AI2 == 5f && ((Velocity.X > 0f && XYWH.X + (Width >> 1) > Main.PlayerSet[Target].XYWH.X + 10) || (Velocity.X < 0f && XYWH.X + (Width >> 1) < Main.PlayerSet[Target].XYWH.X + 10)))
		{
			AI2 = 0f;
		}
	}

	private void SkeletronPrimeViceHand()
	{
		SpriteDirection = (sbyte)(0f - AI0);
		Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
		float num = Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 200f * AI0 - vector.X;
		float num2 = Main.NPCSet[(int)AI1].Position.Y + 230f - vector.Y;
		float num3 = (float)Math.Sqrt(num * num + num2 * num2);
		if (AI2 != 99f)
		{
			if (num3 > 800f)
			{
				AI2 = 99f;
			}
		}
		else if (num3 < 400f)
		{
			AI2 = 0f;
		}
		if (Main.NPCSet[(int)AI1].Active == 0 || Main.NPCSet[(int)AI1].AIStyle != 32)
		{
			AI2 += 10f;
			if (AI2 > 50f || Main.NetMode != 2)
			{
				Life = -1;
				HitEffect();
				Active = 0;
				return;
			}
		}
		if (AI2 == 99f)
		{
			if (Position.Y > Main.NPCSet[(int)AI1].Position.Y)
			{
				if (Velocity.Y > 0f)
				{
					Velocity.Y *= 0.96f;
				}
				Velocity.Y -= 0.1f;
				if (Velocity.Y > 8f)
				{
					Velocity.Y = 8f;
				}
			}
			else if (Position.Y < Main.NPCSet[(int)AI1].Position.Y)
			{
				if (Velocity.Y < 0f)
				{
					Velocity.Y *= 0.96f;
				}
				Velocity.Y += 0.1f;
				if (Velocity.Y < -8f)
				{
					Velocity.Y = -8f;
				}
			}
			if (Position.X + (float)(Width >> 1) > Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1))
			{
				if (Velocity.X > 0f)
				{
					Velocity.X *= 0.96f;
				}
				Velocity.X -= 0.5f;
				if (Velocity.X > 12f)
				{
					Velocity.X = 12f;
				}
			}
			if (Position.X + (float)(Width >> 1) < Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1))
			{
				if (Velocity.X < 0f)
				{
					Velocity.X *= 0.96f;
				}
				Velocity.X += 0.5f;
				if (Velocity.X < -12f)
				{
					Velocity.X = -12f;
				}
			}
		}
		else if (AI2 == 0f || AI2 == 3f)
		{
			if (Main.NPCSet[(int)AI1].AI1 == 3f && TimeLeft > 10)
			{
				TimeLeft = 10;
			}
			if (Main.NPCSet[(int)AI1].AI1 != 0f)
			{
				TargetClosest();
				TargetClosest();
				if (Main.PlayerSet[Target].IsDead)
				{
					Velocity.Y += 0.1f;
					if (Velocity.Y > 16f)
					{
						Velocity.Y = 16f;
					}
				}
				else
				{
					Vector2 vector2 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
					float num4 = Main.PlayerSet[Target].Position.X + 10f - vector2.X;
					float num5 = Main.PlayerSet[Target].Position.Y + 21f - vector2.Y;
					float num6 = (float)Math.Sqrt(num4 * num4 + num5 * num5);
					num6 = 12f / num6;
					num4 *= num6;
					num5 *= num6;
					Rotation = (float)Math.Atan2(num5, num4) - 1.57f;
					if (Math.Abs(Velocity.X) + Math.Abs(Velocity.Y) < 2f)
					{
						Rotation = (float)Math.Atan2(num5, num4) - 1.57f;
						Velocity.X = num4;
						Velocity.Y = num5;
						ShouldNetUpdate = true;
					}
					else
					{
						Velocity.X *= 0.97f;
						Velocity.Y *= 0.97f;
					}
					AI3++;
					if (AI3 >= 600f)
					{
						AI2 = 0f;
						AI3 = 0f;
						ShouldNetUpdate = true;
					}
				}
			}
			else
			{
				AI3++;
				if (AI3 >= 600f)
				{
					AI2++;
					AI3 = 0f;
					ShouldNetUpdate = true;
				}
				if (Position.Y > Main.NPCSet[(int)AI1].Position.Y + 300f)
				{
					if (Velocity.Y > 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y -= 0.1f;
					if (Velocity.Y > 3f)
					{
						Velocity.Y = 3f;
					}
				}
				else if (Position.Y < Main.NPCSet[(int)AI1].Position.Y + 230f)
				{
					if (Velocity.Y < 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y += 0.1f;
					if (Velocity.Y < -3f)
					{
						Velocity.Y = -3f;
					}
				}
				if (Position.X + (float)(Width >> 1) > Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) + 250f)
				{
					if (Velocity.X > 0f)
					{
						Velocity.X *= 0.94f;
					}
					Velocity.X -= 0.3f;
					if (Velocity.X > 9f)
					{
						Velocity.X = 9f;
					}
				}
				if (Position.X + (float)(Width >> 1) < Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1))
				{
					if (Velocity.X < 0f)
					{
						Velocity.X *= 0.94f;
					}
					Velocity.X += 0.2f;
					if (Velocity.X < -8f)
					{
						Velocity.X = -8f;
					}
				}
			}
			Vector2 vector3 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num7 = Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 200f * AI0 - vector3.X;
			float num8 = Main.NPCSet[(int)AI1].Position.Y + 230f - vector3.Y;
			Math.Sqrt(num7 * num7 + num8 * num8);
			Rotation = (float)Math.Atan2(num8, num7) + 1.57f;
		}
		else if (AI2 == 1f)
		{
			if (Velocity.Y > 0f)
			{
				Velocity.Y *= 0.9f;
			}
			Vector2 vector4 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num9 = Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 280f * AI0 - vector4.X;
			float num10 = Main.NPCSet[(int)AI1].Position.Y + 230f - vector4.Y;
			float num11 = (float)Math.Sqrt(num9 * num9 + num10 * num10);
			Rotation = (float)Math.Atan2(num10, num9) + 1.57f;
			Velocity.X = (Velocity.X * 5f + Main.NPCSet[(int)AI1].Velocity.X) / 6f;
			Velocity.X += 0.5f;
			Velocity.Y -= 0.5f;
			if (Velocity.Y < -9f)
			{
				Velocity.Y = -9f;
			}
			if (Position.Y < Main.NPCSet[(int)AI1].Position.Y - 280f)
			{
				TargetClosest();
				AI2 = 2f;
				vector4 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
				num9 = Main.PlayerSet[Target].Position.X + 10f - vector4.X;
				num10 = Main.PlayerSet[Target].Position.Y + 21f - vector4.Y;
				num11 = (float)Math.Sqrt(num9 * num9 + num10 * num10);
				num11 = 20f / num11;
				Velocity.X = num9 * num11;
				Velocity.Y = num10 * num11;
				ShouldNetUpdate = true;
			}
		}
		else if (AI2 == 2f)
		{
			if (Position.Y > Main.PlayerSet[Target].Position.Y || Velocity.Y < 0f)
			{
				if (AI3 >= 4f)
				{
					AI2 = 3f;
					AI3 = 0f;
				}
				else
				{
					AI2 = 1f;
					AI3++;
				}
			}
		}
		else if (AI2 == 4f)
		{
			Vector2 vector5 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num12 = Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 200f * AI0 - vector5.X;
			float num13 = Main.NPCSet[(int)AI1].Position.Y + 230f - vector5.Y;
			float num14 = (float)Math.Sqrt(num12 * num12 + num13 * num13);
			Rotation = (float)Math.Atan2(num13, num12) + 1.57f;
			Velocity.Y = (Velocity.Y * 5f + Main.NPCSet[(int)AI1].Velocity.Y) / 6f;
			Velocity.X += 0.5f;
			if (Velocity.X > 12f)
			{
				Velocity.X = 12f;
			}
			if (Position.X + (float)(Width >> 1) < Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 500f || Position.X + (float)(Width >> 1) > Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) + 500f)
			{
				TargetClosest();
				AI2 = 5f;
				vector5 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
				num12 = Main.PlayerSet[Target].Position.X + 10f - vector5.X;
				num13 = Main.PlayerSet[Target].Position.Y + 21f - vector5.Y;
				num14 = (float)Math.Sqrt(num12 * num12 + num13 * num13);
				num14 = 17f / num14;
				Velocity.X = num12 * num14;
				Velocity.Y = num13 * num14;
				ShouldNetUpdate = true;
			}
		}
		else if (AI2 == 5f && Position.X + (float)(Width >> 1) < Main.PlayerSet[Target].Position.X + 10f - 100f)
		{
			if (AI3 >= 4f)
			{
				AI2 = 0f;
				AI3 = 0f;
			}
			else
			{
				AI2 = 4f;
				AI3++;
			}
		}
	}

	private void SkeletronPrimeCannonHand()
	{
		SpriteDirection = (sbyte)(0f - AI0);
		if (Main.NPCSet[(int)AI1].Active == 0 || Main.NPCSet[(int)AI1].AIStyle != 32)
		{
			AI2 += 10f;
			if (AI2 > 50f || Main.NetMode != 2)
			{
				Life = -1;
				HitEffect();
				Active = 0;
				return;
			}
		}
		if (AI2 == 0f)
		{
			if (Main.NPCSet[(int)AI1].AI1 == 3f && TimeLeft > 10)
			{
				TimeLeft = 10;
			}
			if (Main.NPCSet[(int)AI1].AI1 != 0f)
			{
				LocalAI0 += 2;
				if (Position.Y > Main.NPCSet[(int)AI1].Position.Y - 100f)
				{
					if (Velocity.Y > 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y -= 0.07f;
					if (Velocity.Y > 6f)
					{
						Velocity.Y = 6f;
					}
				}
				else if (Position.Y < Main.NPCSet[(int)AI1].Position.Y - 100f)
				{
					if (Velocity.Y < 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y += 0.07f;
					if (Velocity.Y < -6f)
					{
						Velocity.Y = -6f;
					}
				}
				if (Position.X + (float)(Width >> 1) > Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 120f * AI0)
				{
					if (Velocity.X > 0f)
					{
						Velocity.X *= 0.96f;
					}
					Velocity.X -= 0.1f;
					if (Velocity.X > 8f)
					{
						Velocity.X = 8f;
					}
				}
				if (Position.X + (float)(Width >> 1) < Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 120f * AI0)
				{
					if (Velocity.X < 0f)
					{
						Velocity.X *= 0.96f;
					}
					Velocity.X += 0.1f;
					if (Velocity.X < -8f)
					{
						Velocity.X = -8f;
					}
				}
			}
			else
			{
				AI3++;
				if (AI3 >= 1100f)
				{
					LocalAI0 = 0;
					AI2 = 1f;
					AI3 = 0f;
					ShouldNetUpdate = true;
				}
				if (Position.Y > Main.NPCSet[(int)AI1].Position.Y - 150f)
				{
					if (Velocity.Y > 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y -= 0.04f;
					if (Velocity.Y > 3f)
					{
						Velocity.Y = 3f;
					}
				}
				else if (Position.Y < Main.NPCSet[(int)AI1].Position.Y - 150f)
				{
					if (Velocity.Y < 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y += 0.04f;
					if (Velocity.Y < -3f)
					{
						Velocity.Y = -3f;
					}
				}
				if (Position.X + (float)(Width >> 1) > Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) + 200f)
				{
					if (Velocity.X > 0f)
					{
						Velocity.X *= 0.96f;
					}
					Velocity.X -= 0.2f;
					if (Velocity.X > 8f)
					{
						Velocity.X = 8f;
					}
				}
				if (Position.X + (float)(Width >> 1) < Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) + 160f)
				{
					if (Velocity.X < 0f)
					{
						Velocity.X *= 0.96f;
					}
					Velocity.X += 0.2f;
					if (Velocity.X < -8f)
					{
						Velocity.X = -8f;
					}
				}
			}
			Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num = Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 200f * AI0 - vector.X;
			float num2 = Main.NPCSet[(int)AI1].Position.Y + 230f - vector.Y;
			float num3 = (float)Math.Sqrt(num * num + num2 * num2);
			Rotation = (float)Math.Atan2(num2, num) + 1.57f;
			if (Main.NetMode != 1)
			{
				LocalAI0++;
				if (LocalAI0 > 140)
				{
					LocalAI0 = 0;
					num3 = 12f / num3;
					num = (0f - num) * num3;
					num2 = (0f - num2) * num3;
					num += (float)Main.Rand.Next(-40, 41) * 0.01f;
					num2 += (float)Main.Rand.Next(-40, 41) * 0.01f;
					vector.X += num * 4f;
					vector.Y += num2 * 4f;
					Projectile.NewProjectile(vector.X, vector.Y, num, num2, 102, 0, 0f);
				}
			}
		}
		else
		{
			if (AI2 != 1f)
			{
				return;
			}
			AI3++;
			if (AI3 >= 300f)
			{
				LocalAI0 = 0;
				AI2 = 0f;
				AI3 = 0f;
				ShouldNetUpdate = true;
			}
			Vector2 vector2 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num4 = Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - vector2.X;
			float num5 = Main.NPCSet[(int)AI1].Position.Y - vector2.Y;
			num5 = Main.PlayerSet[Target].Position.Y + 21f - 80f - vector2.Y;
			float num6 = (float)Math.Sqrt(num4 * num4 + num5 * num5);
			num6 = 6f / num6;
			num4 *= num6;
			num5 *= num6;
			if (Velocity.X > num4)
			{
				if (Velocity.X > 0f)
				{
					Velocity.X *= 0.9f;
				}
				Velocity.X -= 0.04f;
			}
			if (Velocity.X < num4)
			{
				if (Velocity.X < 0f)
				{
					Velocity.X *= 0.9f;
				}
				Velocity.X += 0.04f;
			}
			if (Velocity.Y > num5)
			{
				if (Velocity.Y > 0f)
				{
					Velocity.Y *= 0.9f;
				}
				Velocity.Y -= 0.08f;
			}
			if (Velocity.Y < num5)
			{
				if (Velocity.Y < 0f)
				{
					Velocity.Y *= 0.9f;
				}
				Velocity.Y += 0.08f;
			}
			TargetClosest();
			vector2 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			num4 = Main.PlayerSet[Target].Position.X + 10f - vector2.X;
			num5 = Main.PlayerSet[Target].Position.Y + 21f - vector2.Y;
			num6 = (float)Math.Sqrt(num4 * num4 + num5 * num5);
			Rotation = (float)Math.Atan2(num5, num4) - 1.57f;
			if (Main.NetMode != 1)
			{
				LocalAI0++;
				if (LocalAI0 > 40)
				{
					LocalAI0 = 0;
					num6 = 10f / num6;
					num4 *= num6;
					num5 *= num6;
					num4 += (float)Main.Rand.Next(-40, 41) * 0.01f;
					num5 += (float)Main.Rand.Next(-40, 41) * 0.01f;
					vector2.X += num4 * 4f;
					vector2.Y += num5 * 4f;
					Projectile.NewProjectile(vector2.X, vector2.Y, num4, num5, 102, 0, 0f);
				}
			}
		}
	}

	private void SkeletronPrimeLaserHand()
	{
		SpriteDirection = (sbyte)(0f - AI0);
		if (Main.NPCSet[(int)AI1].Active == 0 || Main.NPCSet[(int)AI1].AIStyle != 32)
		{
			AI2 += 10f;
			if (AI2 > 50f || Main.NetMode != 2)
			{
				Life = -1;
				HitEffect();
				Active = 0;
				return;
			}
		}
		if (AI2 == 0f || AI2 == 3f)
		{
			if (Main.NPCSet[(int)AI1].AI1 == 3f && TimeLeft > 10)
			{
				TimeLeft = 10;
			}
			if (Main.NPCSet[(int)AI1].AI1 != 0f)
			{
				LocalAI0 += 3;
				if (Position.Y > Main.NPCSet[(int)AI1].Position.Y - 100f)
				{
					if (Velocity.Y > 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y -= 0.07f;
					if (Velocity.Y > 6f)
					{
						Velocity.Y = 6f;
					}
				}
				else if (Position.Y < Main.NPCSet[(int)AI1].Position.Y - 100f)
				{
					if (Velocity.Y < 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y += 0.07f;
					if (Velocity.Y < -6f)
					{
						Velocity.Y = -6f;
					}
				}
				if (Position.X + (float)(Width >> 1) > Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 120f * AI0)
				{
					if (Velocity.X > 0f)
					{
						Velocity.X *= 0.96f;
					}
					Velocity.X -= 0.1f;
					if (Velocity.X > 8f)
					{
						Velocity.X = 8f;
					}
				}
				if (Position.X + (float)(Width >> 1) < Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 120f * AI0)
				{
					if (Velocity.X < 0f)
					{
						Velocity.X *= 0.96f;
					}
					Velocity.X += 0.1f;
					if (Velocity.X < -8f)
					{
						Velocity.X = -8f;
					}
				}
			}
			else
			{
				AI3++;
				if (AI3 >= 800f)
				{
					AI2++;
					AI3 = 0f;
					ShouldNetUpdate = true;
				}
				if (Position.Y > Main.NPCSet[(int)AI1].Position.Y - 100f)
				{
					if (Velocity.Y > 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y -= 0.1f;
					if (Velocity.Y > 3f)
					{
						Velocity.Y = 3f;
					}
				}
				else if (Position.Y < Main.NPCSet[(int)AI1].Position.Y - 100f)
				{
					if (Velocity.Y < 0f)
					{
						Velocity.Y *= 0.96f;
					}
					Velocity.Y += 0.1f;
					if (Velocity.Y < -3f)
					{
						Velocity.Y = -3f;
					}
				}
				if (Position.X + (float)(Width >> 1) > Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 180f * AI0)
				{
					if (Velocity.X > 0f)
					{
						Velocity.X *= 0.96f;
					}
					Velocity.X -= 0.14f;
					if (Velocity.X > 8f)
					{
						Velocity.X = 8f;
					}
				}
				if (Position.X + (float)(Width >> 1) < Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - 180f * AI0)
				{
					if (Velocity.X < 0f)
					{
						Velocity.X *= 0.96f;
					}
					Velocity.X += 0.14f;
					if (Velocity.X < -8f)
					{
						Velocity.X = -8f;
					}
				}
			}
			TargetClosest();
			Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num = Main.PlayerSet[Target].Position.X + 10f - vector.X;
			float num2 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
			float num3 = (float)Math.Sqrt(num * num + num2 * num2);
			Rotation = (float)Math.Atan2(num2, num) - 1.57f;
			if (Main.NetMode != 1)
			{
				LocalAI0++;
				if (LocalAI0 > 200)
				{
					LocalAI0 = 0;
					num3 = 8f / num3;
					num *= num3;
					num2 *= num3;
					num += (float)Main.Rand.Next(-40, 41) * 0.05f;
					num2 += (float)Main.Rand.Next(-40, 41) * 0.05f;
					vector.X += num * 8f;
					vector.Y += num2 * 8f;
					Projectile.NewProjectile(vector.X, vector.Y, num, num2, 100, 25, 0f);
				}
			}
		}
		else
		{
			if (AI2 != 1f)
			{
				return;
			}
			AI3++;
			if (AI3 >= 200f)
			{
				LocalAI0 = 0;
				AI2 = 0f;
				AI3 = 0f;
				ShouldNetUpdate = true;
			}
			Vector2 vector2 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num4 = Main.PlayerSet[Target].Position.X + 10f - 350f - vector2.X;
			float num5 = Main.PlayerSet[Target].Position.Y + 21f - 20f - vector2.Y;
			float num6 = (float)Math.Sqrt(num4 * num4 + num5 * num5);
			num6 = 7f / num6;
			num4 *= num6;
			num5 *= num6;
			if (Velocity.X > num4)
			{
				if (Velocity.X > 0f)
				{
					Velocity.X *= 0.9f;
				}
				Velocity.X -= 0.1f;
			}
			if (Velocity.X < num4)
			{
				if (Velocity.X < 0f)
				{
					Velocity.X *= 0.9f;
				}
				Velocity.X += 0.1f;
			}
			if (Velocity.Y > num5)
			{
				if (Velocity.Y > 0f)
				{
					Velocity.Y *= 0.9f;
				}
				Velocity.Y -= 0.03f;
			}
			if (Velocity.Y < num5)
			{
				if (Velocity.Y < 0f)
				{
					Velocity.Y *= 0.9f;
				}
				Velocity.Y += 0.03f;
			}
			TargetClosest();
			vector2 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			num4 = Main.PlayerSet[Target].Position.X + 10f - vector2.X;
			num5 = Main.PlayerSet[Target].Position.Y + 21f - vector2.Y;
			num6 = (float)Math.Sqrt(num4 * num4 + num5 * num5);
			Rotation = (float)Math.Atan2(num5, num4) - 1.57f;
			if (Main.NetMode == 1)
			{
				LocalAI0++;
				if (LocalAI0 > 80)
				{
					LocalAI0 = 0;
					num6 = 10f / num6;
					num4 *= num6;
					num5 *= num6;
					num4 += (float)Main.Rand.Next(-40, 41) * 0.05f;
					num5 += (float)Main.Rand.Next(-40, 41) * 0.05f;
					vector2.X += num4 * 8f;
					vector2.Y += num5 * 8f;
					Projectile.NewProjectile(vector2.X, vector2.Y, num4, num5, 100, 25, 0f);
				}
			}
		}
	}

	private void DestroyerAI()
	{
		if (AI3 > 0f)
		{
			RealLife = (int)AI3;
		}
		if (Target == 8 || Main.PlayerSet[Target].IsDead)
		{
			TargetClosest();
		}
		if (Type > 134)
		{
			bool flag = false;
			if (AI1 <= 0f)
			{
				flag = true;
			}
			else if (Main.NPCSet[(int)AI1].Life <= 0)
			{
				flag = true;
			}
			if (flag)
			{
				Life = 0;
				if (Active != 0)
				{
					HitEffect();
				}
				CheckDead();
			}
		}
		if (Main.NetMode != 1)
		{
			if (AI0 == 0f && Type == 134)
			{
				AI3 = WhoAmI;
				RealLife = WhoAmI;
				int num = 0;
				int num2 = WhoAmI;
				int num3 = 80;
				for (int i = 0; i <= num3; i++)
				{
					int type = 135;
					if (i == num3)
					{
						type = 136;
					}
					num = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + Height, type, WhoAmI);
					Main.NPCSet[num].AI3 = WhoAmI;
					Main.NPCSet[num].RealLife = WhoAmI;
					Main.NPCSet[num].AI1 = num2;
					Main.NPCSet[num2].AI0 = num;
					NetMessage.CreateMessage1(23, num);
					NetMessage.SendMessage();
					num2 = num;
				}
			}
			if (Type == 135)
			{
				LocalAI0 += Main.Rand.Next(4);
				if (LocalAI0 >= Main.Rand.Next(1400, 26000))
				{
					LocalAI0 = 0;
					TargetClosest();
					if (Collision.CanHit(ref XYWH, ref Main.PlayerSet[Target].XYWH))
					{
						Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(Height >> 1));
						float num4 = Main.PlayerSet[Target].Position.X + 10f - vector.X + (float)Main.Rand.Next(-20, 21);
						float num5 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y + (float)Main.Rand.Next(-20, 21);
						float num6 = (float)Math.Sqrt(num4 * num4 + num5 * num5);
						num6 = 8f / num6;
						num4 *= num6;
						num5 *= num6;
						num4 += (float)Main.Rand.Next(-20, 21) * 0.05f;
						num5 += (float)Main.Rand.Next(-20, 21) * 0.05f;
						vector.X += num4 * 5f;
						vector.Y += num5 * 5f;
						int num7 = Projectile.NewProjectile(vector.X, vector.Y, num4, num5, 100, 22, 0f);
						if (num7 >= 0)
						{
							Main.ProjectileSet[num7].timeLeft = 300;
							ShouldNetUpdate = true;
						}
					}
				}
			}
		}
		int num8 = ((int)Position.X >> 4) - 1;
		int num9 = ((int)Position.X + Width >> 4) + 2;
		int num10 = ((int)Position.Y >> 4) - 1;
		int num11 = ((int)Position.Y + Height >> 4) + 2;
		if (num8 < 0)
		{
			num8 = 0;
		}
		if (num9 > Main.MaxTilesX)
		{
			num9 = Main.MaxTilesX;
		}
		if (num10 < 0)
		{
			num10 = 0;
		}
		if (num11 > Main.MaxTilesY)
		{
			num11 = Main.MaxTilesY;
		}
		bool flag2 = false;
		if (!flag2)
		{
			Vector2 vector2 = default;
			for (int j = num8; j < num9; j++)
			{
				for (int k = num10; k < num11; k++)
				{
					if (Main.TileSet[j, k].CanStandOnTop() || Main.TileSet[j, k].Liquid > 64)
					{
						vector2.X = j * 16;
						vector2.Y = k * 16;
						if (Position.X + (float)(int)Width > vector2.X && Position.X < vector2.X + 16f && Position.Y + (float)(int)Height > vector2.Y && Position.Y < vector2.Y + 16f)
						{
							flag2 = true;
							break;
						}
					}
				}
			}
		}
		if (!flag2)
		{
			if (Type != 135 || AI2 != 1f)
			{
				Lighting.AddLight((int)Position.X + (Width >> 1) >> 4, (int)Position.Y + (Height >> 1) >> 4, new Vector3(0.3f, 0.1f, 0.05f));
			}
			LocalAI1 = 1;
			if (Type == 134)
			{
				Rectangle rectangle = new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
				bool flag3 = true;
				if (Position.Y > Main.PlayerSet[Target].Position.Y)
				{
					for (int l = 0; l < 8; l++)
					{
						if (Main.PlayerSet[l].Active != 0)
						{
							Rectangle value = new Rectangle(Main.PlayerSet[l].XYWH.X - 1000, Main.PlayerSet[l].XYWH.Y - 1000, 2000, 2000);
							if (rectangle.Intersects(value))
							{
								flag3 = false;
								break;
							}
						}
					}
					if (flag3)
					{
						flag2 = true;
					}
				}
			}
		}
		else
		{
			LocalAI1 = 0;
		}
		float num12 = 16f;
		if (Main.GameTime.DayTime || Main.PlayerSet[Target].IsDead)
		{
			flag2 = false;
			Velocity.Y++;
			if (Position.Y > (float)(Main.WorldSurface << 4))
			{
				Velocity.Y++;
				num12 = 32f;
			}
			if (Position.Y > (float)(Main.RockLayer << 4))
			{
				for (int m = 0; m < 196; m++)
				{
					if (Main.NPCSet[m].AIStyle == AIStyle)
					{
						Main.NPCSet[m].Active = 0;
					}
				}
			}
		}
		Vector2 vector3 = new Vector2(Position.X + (float)(Width >> 1), Position.Y + (float)(Height >> 1));
		float num13;
		float num14;
		if (AI1 > 0f && AI1 < 196f)
		{
			num13 = Main.NPCSet[(int)AI1].Position.X + (float)(Main.NPCSet[(int)AI1].Width >> 1) - vector3.X;
			num14 = Main.NPCSet[(int)AI1].Position.Y + (float)(Main.NPCSet[(int)AI1].Height >> 1) - vector3.Y;
			Rotation = (float)(Math.Atan2(num14, num13) + Math.PI / 2.0);
			float num15 = num13 * num13 + num14 * num14;
			if (num15 > 0f)
			{
				num15 = (float)Math.Sqrt(num15);
				num15 = (num15 - 44f * Scale) / num15;
				num13 *= num15;
				num14 *= num15;
				Position.X += num13;
				Position.Y += num14;
				XYWH.X = (int)Position.X;
				XYWH.Y = (int)Position.Y;
			}
			Velocity.X = 0f;
			Velocity.Y = 0f;
			return;
		}
		num13 = (Main.PlayerSet[Target].XYWH.X + 10) & -16;
		num14 = (Main.PlayerSet[Target].XYWH.Y + 21) & -16;
		vector3.X = (int)vector3.X & -16;
		vector3.Y = (int)vector3.Y & -16;
		num13 -= vector3.X;
		num14 -= vector3.Y;
		if (!flag2)
		{
			TargetClosest();
			Velocity.Y += 0.15f;
			if (Velocity.Y > num12)
			{
				Velocity.Y = num12;
			}
			if ((double)(Math.Abs(Velocity.X) + Math.Abs(Velocity.Y)) < (double)num12 * 0.4)
			{
				if (Velocity.X < 0f)
				{
					Velocity.X -= 0.11f;
				}
				else
				{
					Velocity.X += 0.11f;
				}
			}
			else if (Velocity.Y == num12)
			{
				if (Velocity.X < num13)
				{
					Velocity.X += 0.1f;
				}
				else if (Velocity.X > num13)
				{
					Velocity.X -= 0.1f;
				}
			}
			else if (Velocity.Y > 4f)
			{
				if (Velocity.X < 0f)
				{
					Velocity.X += 0.09f;
				}
				else
				{
					Velocity.X -= 0.09f;
				}
			}
		}
		else
		{
			float num16 = (float)Math.Sqrt(num13 * num13 + num14 * num14);
			if (SoundDelay == 0)
			{
				float num17 = num16 * 0.025f;
				if (num17 < 10f)
				{
					num17 = 10f;
				}
				else if (num17 > 20f)
				{
					num17 = 20f;
				}
				SoundDelay = (short)num17;
				Main.PlaySound(15, XYWH.X, XYWH.Y);
			}
			float num18 = Math.Abs(num13);
			float num19 = Math.Abs(num14);
			float num20 = num12 / num16;
			num13 *= num20;
			num14 *= num20;
			if (((Velocity.X > 0f && num13 > 0f) || (Velocity.X < 0f && num13 < 0f)) && ((Velocity.Y > 0f && num14 > 0f) || (Velocity.Y < 0f && num14 < 0f)))
			{
				if (Velocity.X < num13)
				{
					Velocity.X += 0.15f;
				}
				else if (Velocity.X > num13)
				{
					Velocity.X -= 0.15f;
				}
				if (Velocity.Y < num14)
				{
					Velocity.Y += 0.15f;
				}
				else if (Velocity.Y > num14)
				{
					Velocity.Y -= 0.15f;
				}
			}
			if ((Velocity.X > 0f && num13 > 0f) || (Velocity.X < 0f && num13 < 0f) || (Velocity.Y > 0f && num14 > 0f) || (Velocity.Y < 0f && num14 < 0f))
			{
				if (Velocity.X < num13)
				{
					Velocity.X += 0.1f;
				}
				else if (Velocity.X > num13)
				{
					Velocity.X -= 0.1f;
				}
				if (Velocity.Y < num14)
				{
					Velocity.Y += 0.1f;
				}
				else if (Velocity.Y > num14)
				{
					Velocity.Y -= 0.1f;
				}
				if ((double)Math.Abs(num14) < (double)num12 * 0.2 && ((Velocity.X > 0f && num13 < 0f) || (Velocity.X < 0f && num13 > 0f)))
				{
					if (Velocity.Y > 0f)
					{
						Velocity.Y += 0.2f;
					}
					else
					{
						Velocity.Y -= 0.2f;
					}
				}
				if ((double)Math.Abs(num13) < (double)num12 * 0.2 && ((Velocity.Y > 0f && num14 < 0f) || (Velocity.Y < 0f && num14 > 0f)))
				{
					if (Velocity.X > 0f)
					{
						Velocity.X += 0.2f;
					}
					else
					{
						Velocity.X -= 0.2f;
					}
				}
			}
			else if (num18 > num19)
			{
				if (Velocity.X < num13)
				{
					Velocity.X += 0.11f;
				}
				else if (Velocity.X > num13)
				{
					Velocity.X -= 0.11f;
				}
				if ((double)(Math.Abs(Velocity.X) + Math.Abs(Velocity.Y)) < (double)num12 * 0.5)
				{
					if (Velocity.Y > 0f)
					{
						Velocity.Y += 0.1f;
					}
					else
					{
						Velocity.Y -= 0.1f;
					}
				}
			}
			else
			{
				if (Velocity.Y < num14)
				{
					Velocity.Y += 0.11f;
				}
				else if (Velocity.Y > num14)
				{
					Velocity.Y -= 0.11f;
				}
				if ((double)(Math.Abs(Velocity.X) + Math.Abs(Velocity.Y)) < (double)num12 * 0.5)
				{
					if (Velocity.X > 0f)
					{
						Velocity.X += 0.1f;
					}
					else
					{
						Velocity.X -= 0.1f;
					}
				}
			}
		}
		Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X) + 1.57f;
		if (Type != 134)
		{
			return;
		}
		if (flag2)
		{
			if (LocalAI0 != 1)
			{
				ShouldNetUpdate = true;
			}
			LocalAI0 = 1;
		}
		else
		{
			if (LocalAI0 != 0)
			{
				ShouldNetUpdate = true;
			}
			LocalAI0 = 0;
		}
		if (((Velocity.X > 0f && OldVelocity.X < 0f) || (Velocity.X < 0f && OldVelocity.X > 0f) || (Velocity.Y > 0f && OldVelocity.Y < 0f) || (Velocity.Y < 0f && OldVelocity.Y > 0f)) && !WasJustHit)
		{
			ShouldNetUpdate = true;
		}
	}

	private void SnowmanAI()
	{
		float num = 4f;
		float num2 = 1f;
		if (Type == 143)
		{
			num = 3f;
			num2 = 0.7f;
		}
		if (Type == 145)
		{
			num = 3.5f;
			num2 = 0.8f;
		}
		if (Type == 143)
		{
			AI2++;
			if (AI2 >= 120f)
			{
				AI2 = 0f;
				if (Main.NetMode != 1)
				{
					Vector2 vector = new Vector2(Position.X + (float)(int)Width * 0.5f - (float)(Direction * 12), Position.Y + (float)(int)Height * 0.5f);
					float speedX = 12 * SpriteDirection;
					float speedY = 0f;
					if (Main.NetMode != 1)
					{
						int num3 = Projectile.NewProjectile(vector.X, vector.Y, speedX, speedY, 110, 25, 0f, 8, send: false);
						if (num3 >= 0)
						{
							Main.ProjectileSet[num3].ai0 = 2f;
							Main.ProjectileSet[num3].timeLeft = 300;
							Main.ProjectileSet[num3].friendly = false;
							NetMessage.SendProjectile(num3);
							ShouldNetUpdate = true;
						}
					}
				}
			}
		}
		if (Type == 144 && AI1 >= 3f)
		{
			TargetClosest();
			SpriteDirection = Direction;
			if (Velocity.Y == 0f)
			{
				Velocity.X *= 0.9f;
				AI2++;
				if ((double)Velocity.X > -0.3 && (double)Velocity.X < 0.3)
				{
					Velocity.X = 0f;
				}
				if (AI2 >= 200f)
				{
					AI2 = 0f;
					AI1 = 0f;
				}
			}
		}
		else if (Type == 145 && AI1 >= 3f)
		{
			TargetClosest();
			if (Velocity.Y == 0f)
			{
				Velocity.X *= 0.9f;
				AI2++;
				if ((double)Velocity.X > -0.3 && (double)Velocity.X < 0.3)
				{
					Velocity.X = 0f;
				}
				if (AI2 >= 16f)
				{
					AI2 = 0f;
					AI1 = 0f;
				}
			}
			if (Velocity.X == 0f && Velocity.Y == 0f && AI2 == 8f)
			{
				Vector2 vector2 = new Vector2(Position.X + (float)(int)Width * 0.5f - (float)(Direction * 12), Position.Y + (float)(int)Height * 0.25f);
				float num4 = Main.PlayerSet[Target].Position.X + 10f - vector2.X;
				float num5 = Main.PlayerSet[Target].Position.Y - vector2.Y;
				float num6 = (float)Math.Sqrt(num4 * num4 + num5 * num5);
				num6 = 10f / num6;
				num4 *= num6;
				num5 *= num6;
				if (Main.NetMode != 1)
				{
					int num7 = Projectile.NewProjectile(vector2.X, vector2.Y, num4, num5, 109, 35, 0f);
					if (num7 >= 0)
					{
						Main.ProjectileSet[num7].ai0 = 2f;
						Main.ProjectileSet[num7].timeLeft = 300;
						Main.ProjectileSet[num7].friendly = false;
						NetMessage.SendProjectile(num7);
						ShouldNetUpdate = true;
					}
				}
			}
		}
		else
		{
			if (Velocity.Y == 0f)
			{
				if (LocalAI2 == XYWH.X)
				{
					Direction = (sbyte)(-Direction);
					AI3 = 60f;
				}
				LocalAI2 = XYWH.X;
				if (AI3 == 0f)
				{
					TargetClosest();
				}
				AI0++;
				if (AI0 > 2f)
				{
					AI0 = 0f;
					AI1++;
					Velocity.Y = -8.2f;
					Velocity.X += (float)Direction * num2 * 1.1f;
				}
				else
				{
					Velocity.Y = -6f;
					Velocity.X += (float)Direction * num2 * 0.9f;
				}
				SpriteDirection = Direction;
			}
			Velocity.X += (float)Direction * num2 * 0.01f;
		}
		if (AI3 > 0f)
		{
			AI3--;
		}
		if (Velocity.X > num && Direction > 0)
		{
			Velocity.X = 4f;
		}
		else if (Velocity.X < 0f - num && Direction < 0)
		{
			Velocity.X = -4f;
		}
	}

	private unsafe void OcramAI()
	{
		Lighting.AddLight(XYWH.X >> 4, XYWH.Y >> 4, new Vector3(1f, 1f, 1f));
		if (Target == 8 || Main.PlayerSet[Target].IsDead || Main.PlayerSet[Target].Active == 0)
		{
			TargetClosest();
		}
		bool isDead = Main.PlayerSet[Target].IsDead;
		float num = Position.X + (float)(Width >> 1) - Main.PlayerSet[Target].Position.X - 10f;
		float num2 = (float)Math.Atan2(Position.Y + (float)(int)Height - 59f - Main.PlayerSet[Target].Position.Y - 21f, num) + 1.57f;
		if (num2 < 0f)
		{
			num2 += 6.283f;
		}
		else if (num2 > 6.283f)
		{
			num2 -= 6.283f;
		}
		float num3 = 0f;
		if (AI0 == 0f && AI1 == 0f)
		{
			num3 = 0.02f;
		}
		if (AI0 == 0f && AI1 == 2f && AI2 > 40f)
		{
			num3 = 0.05f;
		}
		if (AI0 == 3f && AI1 == 0f)
		{
			num3 = 0.05f;
		}
		if (AI0 == 3f && AI1 == 2f && AI2 > 40f)
		{
			num3 = 0.08f;
		}
		if (Rotation < num2)
		{
			if ((double)(num2 - Rotation) > Math.PI)
			{
				Rotation -= num3;
			}
			else
			{
				Rotation += num3;
			}
		}
		else if (Rotation > num2)
		{
			if ((double)(Rotation - num2) > Math.PI)
			{
				Rotation += num3;
			}
			else
			{
				Rotation -= num3;
			}
		}
		if (Rotation > num2 - num3 && Rotation < num2 + num3)
		{
			Rotation = num2;
		}
		if (Rotation < 0f)
		{
			Rotation += 6.283f;
		}
		else if (Rotation > 6.283f)
		{
			Rotation -= 6.283f;
		}
		if (Rotation > num2 - num3 && Rotation < num2 + num3)
		{
			Rotation = num2;
		}
		if (Main.Rand.Next(6) == 0)
		{
			Dust* ptr = Main.DustSet.NewDust(XYWH.X, XYWH.Y + (Height >> 2), Width, Height >> 1, 5, Velocity.X, 2.0);
			if (ptr != null)
			{
				ptr->Velocity.X *= 0.5f;
				ptr->Velocity.Y *= 0.1f;
			}
		}
		if (Main.GameTime.DayTime | isDead)
		{
			Velocity.Y -= 0.04f;
			if (TimeLeft > 10)
			{
				TimeLeft = 10;
			}
			return;
		}
		if (AI0 == 0f)
		{
			if (AI1 == 0f)
			{
				float num4 = 0.12f;
				Vector2 vector = new Vector2(Position.X + (float)(Width >> 1), Position.Y + (float)(Height >> 1));
				float num5 = Main.PlayerSet[Target].Position.X + 10f - vector.X;
				float num6 = Main.PlayerSet[Target].Position.Y + 21f - 200f - vector.Y;
				float num7 = (float)Math.Sqrt(num5 * num5 + num6 * num6);
				float num8 = num7;
				num7 = 8f / num7;
				num5 *= num7;
				num6 *= num7;
				if (Velocity.X < num5)
				{
					Velocity.X += num4;
					if (Velocity.X < 0f && num5 > 0f)
					{
						Velocity.X += num4;
					}
				}
				else if (Velocity.X > num5)
				{
					Velocity.X -= num4;
					if (Velocity.X > 0f && num5 < 0f)
					{
						Velocity.X -= num4;
					}
				}
				if (Velocity.Y < num6)
				{
					Velocity.Y += num4;
					if (Velocity.Y < 0f && num6 > 0f)
					{
						Velocity.Y += num4;
					}
				}
				else if (Velocity.Y > num6)
				{
					Velocity.Y -= num4;
					if (Velocity.Y > 0f && num6 < 0f)
					{
						Velocity.Y -= num4;
					}
				}
				AI2++;
				if (AI2 >= 600f)
				{
					AI1 = 1f;
					AI2 = 0f;
					AI3 = 0f;
					Target = 8;
					ShouldNetUpdate = true;
				}
				else if (XYWH.Y + Height < Main.PlayerSet[Target].XYWH.Y && num8 < 500f)
				{
					if (!Main.PlayerSet[Target].IsDead)
					{
						AI3++;
					}
					if (AI3 >= 90f)
					{
						TargetClosest();
						num7 = (float)Math.Sqrt(num5 * num5 + num6 * num6);
						num7 = 9f / num7;
						num5 *= num7;
						num6 *= num7;
						vector.X += num5 * 15f;
						vector.Y += num6 * 15f;
						Projectile.NewProjectile(vector.X, vector.Y, num5, num6, 100, 20, 0f);
					}
					if (AI3 == 60f || AI3 == 70f || AI3 == 80f || AI3 == 90f)
					{
						Rotation = num2;
						float num9 = Main.PlayerSet[Target].Position.X + 10f - vector.X;
						float num10 = Main.PlayerSet[Target].Position.Y + 21f - vector.Y;
						float num11 = (float)Math.Sqrt(num9 * num9 + num10 * num10);
						num11 = 5f / num11;
						Vector2 vector2 = vector;
						Vector2 vector3 = new Vector2
						{
							X = num9 * num11,
							Y = num10 * num11
						};
						vector2.X += vector3.X * 10f;
						vector2.Y += vector3.Y * 10f;
						if (Main.NetMode != 1)
						{
							int num12 = NewNPC((int)vector2.X, (int)vector2.Y, 167);
							if (num12 < 196)
							{
								Main.NPCSet[num12].Velocity.X = vector3.X;
								Main.NPCSet[num12].Velocity.Y = vector3.Y;
								NetMessage.CreateMessage1(23, num12);
								NetMessage.SendMessage();
							}
						}
						Main.PlaySound(3, (int)vector2.X, (int)vector2.Y);
						for (int i = 0; i < 8; i++)
						{
							if (null == Main.DustSet.NewDust((int)vector2.X, (int)vector2.Y, 20, 20, 5, vector3.X * 0.4f, vector3.Y * 0.4f))
							{
								break;
							}
						}
					}
					if (AI3 == 103f)
					{
						AI3 = 0f;
					}
				}
			}
			else if (AI1 == 1f)
			{
				Rotation = num2;
				Vector2 vector4 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
				float num13 = Main.PlayerSet[Target].Position.X + 10f - vector4.X;
				float num14 = Main.PlayerSet[Target].Position.Y + 21f - vector4.Y;
				float num15 = (float)Math.Sqrt(num13 * num13 + num14 * num14);
				num15 = 6f / num15;
				Velocity.X = num13 * num15;
				Velocity.Y = num14 * num15;
				AI1 = 2f;
			}
			else if (AI1 == 2f)
			{
				AI2++;
				if (AI2 >= 40f)
				{
					Velocity.X *= 0.98f;
					Velocity.Y *= 0.98f;
					if ((double)Velocity.X > -0.1 && (double)Velocity.X < 0.1)
					{
						Velocity.X = 0f;
					}
					if ((double)Velocity.Y > -0.1 && (double)Velocity.Y < 0.1)
					{
						Velocity.Y = 0f;
					}
				}
				else
				{
					Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X) - 1.57f;
				}
				if (AI2 >= 150f)
				{
					AI3++;
					AI2 = 0f;
					Target = 8;
					Rotation = num2;
					if (AI3 >= 3f)
					{
						AI1 = 0f;
						AI3 = 0f;
					}
					else
					{
						AI1 = 1f;
					}
				}
			}
			if (Life < LifeMax >> 1)
			{
				AI0 = 1f;
				AI1 = 0f;
				AI2 = 0f;
				AI3 = 0f;
				ShouldNetUpdate = true;
			}
			return;
		}
		if (AI0 == 1f || AI0 == 2f)
		{
			if (AI0 == 1f)
			{
				AI2 += 0.005f;
				if ((double)AI2 > 0.5)
				{
					AI2 = 0.5f;
				}
			}
			else
			{
				AI2 -= 0.005f;
				if (AI2 < 0f)
				{
					AI2 = 0f;
				}
			}
			Rotation += AI2;
			AI1++;
			if (AI1 == 100f)
			{
				AI0++;
				AI1 = 0f;
				if (AI0 == 3f)
				{
					AI2 = 0f;
				}
				else
				{
					Main.PlaySound(3, XYWH.X, XYWH.Y);
					for (int j = 0; j < 2; j++)
					{
						Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 174);
						Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 173);
						Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 172);
					}
					for (int k = 0; k < 16; k++)
					{
						if (null == Main.DustSet.NewDust(5, ref XYWH, (float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f))
						{
							break;
						}
					}
					Main.PlaySound(15, XYWH.X, XYWH.Y, 0);
				}
			}
			Main.DustSet.NewDust(5, ref XYWH, (float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f);
			Velocity.X *= 0.98f;
			Velocity.Y *= 0.98f;
			if ((double)Velocity.X > -0.1 && (double)Velocity.X < 0.1)
			{
				Velocity.X = 0f;
			}
			if ((double)Velocity.Y > -0.1 && (double)Velocity.Y < 0.1)
			{
				Velocity.Y = 0f;
			}
			return;
		}
		Damage = 50;
		Defense = 0;
		if (AI1 == 0f)
		{
			float num16 = 0.2f;
			Vector2 vector5 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num17 = Main.PlayerSet[Target].Position.X + 10f - vector5.X;
			float num18 = Main.PlayerSet[Target].Position.Y + 21f - 120f - vector5.Y;
			float num19 = (float)Math.Sqrt(num17 * num17 + num18 * num18);
			num19 = 9f / num19;
			num17 *= num19;
			num18 *= num19;
			if (Velocity.X < num17)
			{
				Velocity.X += num16;
				if (Velocity.X < 0f && num17 > 0f)
				{
					Velocity.X += num16;
				}
			}
			else if (Velocity.X > num17)
			{
				Velocity.X -= num16;
				if (Velocity.X > 0f && num17 < 0f)
				{
					Velocity.X -= num16;
				}
			}
			if (Velocity.Y < num18)
			{
				Velocity.Y += num16;
				if (Velocity.Y < 0f && num18 > 0f)
				{
					Velocity.Y += num16;
				}
			}
			else if (Velocity.Y > num18)
			{
				Velocity.Y -= num16;
				if (Velocity.Y > 0f && num18 < 0f)
				{
					Velocity.Y -= num16;
				}
			}
			AI2++;
			if (AI2 >= 100f)
			{
				if (AI2 >= 200f)
				{
					AI1 = 1f;
					AI2 = 0f;
					AI3 = 0f;
					Target = 8;
					ShouldNetUpdate = true;
				}
				num19 = (float)Math.Sqrt(num17 * num17 + num18 * num18);
				num19 = 9f / num19;
				num17 *= num19;
				num18 *= num19;
				num17 += (float)Main.Rand.Next(-40, 41) * 0.08f;
				num18 += (float)Main.Rand.Next(-40, 41) * 0.08f;
				vector5.X += num17 * 15f;
				vector5.Y += num18 * 15f;
				Projectile.NewProjectile(vector5.X, vector5.Y, num17, num18, 83, 45, 0f);
			}
		}
		else if (AI1 == 1f)
		{
			Main.PlaySound(15, (int)Position.X, (int)Position.Y, 0);
			Rotation = num2;
			Vector2 vector6 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
			float num20 = Main.PlayerSet[Target].Position.X + 10f - vector6.X;
			float num21 = Main.PlayerSet[Target].Position.Y + 21f - vector6.Y;
			float num22 = (float)Math.Sqrt(num20 * num20 + num21 * num21);
			num22 = 6.8f / num22;
			Velocity.X = num20 * num22;
			Velocity.Y = num21 * num22;
			if (AI1 == 1f)
			{
				num22 = (float)Math.Sqrt(num20 * num20 + num21 * num21);
				num22 = 6f / num22;
				num20 *= num22;
				num21 *= num22;
				num20 += (float)Main.Rand.Next(-40, 41) * 0.08f;
				num21 += (float)Main.Rand.Next(-40, 41) * 0.08f;
				for (int l = 1; l <= 10; l++)
				{
					vector6.X += (float)Main.Rand.Next(-50, 50) * 2f;
					vector6.Y += (float)Main.Rand.Next(-50, 50) * 2f;
					Projectile.NewProjectile(vector6.X, vector6.Y, num20, num21, 44, 45, 0f);
				}
			}
			AI1 = 2f;
		}
		else
		{
			if (AI1 != 2f)
			{
				return;
			}
			AI2++;
			if (AI2 >= 40f)
			{
				Velocity.X *= 1f;
				Velocity.Y *= 1f;
				if ((double)Velocity.X > -0.1 && (double)Velocity.X < 0.1)
				{
					Velocity.X = 0f;
				}
				if ((double)Velocity.Y > -0.1 && (double)Velocity.Y < 0.1)
				{
					Velocity.Y = 0f;
				}
				if (AI2 >= 135f)
				{
					AI3++;
					AI2 = 0f;
					Target = 8;
					Rotation = num2;
					if (AI3 >= 3f)
					{
						AI1 = 0f;
						AI3 = 0f;
					}
					else
					{
						AI1 = 1f;
					}
				}
				if (AI2 != 110f && AI2 != 100f && AI2 != 130f && AI2 != 120f)
				{
					return;
				}
				Rotation = num2;
				Vector2 vector7 = new Vector2(Position.X + (float)(int)Width * 0.5f, Position.Y + (float)(int)Height * 0.5f);
				float num23 = Main.PlayerSet[Target].Position.X + 10f - vector7.X;
				float num24 = Main.PlayerSet[Target].Position.Y + 21f - vector7.Y;
				float num25 = (float)Math.Sqrt(num23 * num23 + num24 * num24);
				num25 = 5f / num25;
				Vector2 vector8 = vector7;
				Vector2 vector9 = new Vector2
				{
					X = num23 * num25,
					Y = num24 * num25
				};
				vector8.X += vector9.X * 10f;
				vector8.Y += vector9.Y * 10f;
				if (Main.NetMode != 1)
				{
					int num26 = NewNPC((int)vector8.X, (int)vector8.Y, 167);
					if (num26 < 196)
					{
						Main.NPCSet[num26].Velocity.X = vector9.X;
						Main.NPCSet[num26].Velocity.Y = vector9.Y;
						NetMessage.CreateMessage1(23, num26);
						NetMessage.SendMessage();
					}
				}
			}
			else
			{
				Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X) - 1.57f;
			}
		}
	}

	public void FindFrame()
	{
		int num = 0;
		if (AIAction == 0)
		{
			if (Velocity.Y < 0f)
			{
				num = 2;
			}
			else
			{
				num = ((Velocity.Y > 0f) ? 3 : ((Velocity.X != 0f) ? 1 : 0));
			}
		}
		else if (AIAction == 1)
		{
			num = 4;
		}
		switch ((EntityID.NPCID)Type)
		{
		case EntityID.NPCID.SLIME:
		case EntityID.NPCID.MOTHER_SLIME:
		case EntityID.NPCID.LAVA_SLIME:
		case EntityID.NPCID.DUNGEON_SLIME:
		case EntityID.NPCID.CORRUPT_SLIME:
		case EntityID.NPCID.ILLUMINANT_SLIME:
		case EntityID.NPCID.SHADOW_SLIME:
			FrameCounter++;
			if (num > 0)
			{
				FrameCounter++;
			}
			if (num == 4)
			{
				FrameCounter++;
			}
			if (FrameCounter >= 8f)
			{
				FrameY += FrameHeight;
				FrameCounter = 0f;
			}
			if (FrameY >= FrameHeight * NpcFrameCount[Type])
			{
				FrameY = 0;
			}
			break;
		case EntityID.NPCID.TOXIC_SLUDGE:
			SpriteDirection = Direction;
			if (Velocity.Y != 0f)
			{
				FrameY = (short)(FrameHeight << 1);
				break;
			}
			FrameCounter++;
			if (FrameCounter >= 8f)
			{
				FrameY += FrameHeight;
				FrameCounter = 0f;
			}
			if (FrameY > FrameHeight)
			{
				FrameY = 0;
			}
			break;
		case EntityID.NPCID.SNOWMAN_GANGSTA:
			if (Velocity.Y > 0f)
			{
				FrameCounter++;
			}
			else if (Velocity.Y < 0f)
			{
				FrameCounter--;
			}
			if (FrameCounter < 6f)
			{
				FrameY = FrameHeight;
			}
			else if (FrameCounter < 12f)
			{
				FrameY = (short)(FrameHeight << 1);
			}
			else if (FrameCounter < 18f)
			{
				FrameY = (short)(FrameHeight * 3);
			}
			if (FrameCounter < 0f)
			{
				FrameCounter = 0f;
			}
			if (FrameCounter > 17f)
			{
				FrameCounter = 17f;
			}
			break;
		case EntityID.NPCID.MISTER_STABBY:
			if (Velocity.X == 0f && Velocity.Y == 0f)
			{
				LocalAI3++;
				if (LocalAI3 < 6)
				{
					FrameY = 0;
				}
				else if (LocalAI3 < 12)
				{
					FrameY = FrameHeight;
				}
				if (LocalAI3 >= 11)
				{
					LocalAI3 = 0;
				}
				break;
			}
			if (Velocity.Y > 0f)
			{
				FrameCounter++;
			}
			else if (Velocity.Y < 0f)
			{
				FrameCounter--;
			}
			if (FrameCounter < 6f)
			{
				FrameY = (short)(FrameHeight << 1);
			}
			else if (FrameCounter < 12f)
			{
				FrameY = (short)(FrameHeight * 3);
			}
			else if (FrameCounter < 18f)
			{
				FrameY = (short)(FrameHeight << 2);
			}
			if (FrameCounter < 0f)
			{
				FrameCounter = 0f;
			}
			else if (FrameCounter > 17f)
			{
				FrameCounter = 17f;
			}
			break;
		case EntityID.NPCID.SNOW_BALLA:
			if (Velocity.X == 0f && Velocity.Y == 0f)
			{
				if (AI2 < 4f)
				{
					FrameY = 0;
				}
				else if (AI2 < 8f)
				{
					FrameY = FrameHeight;
				}
				else if (AI2 < 12f)
				{
					FrameY = (short)(FrameHeight << 1);
				}
				else if (AI2 < 16f)
				{
					FrameY = (short)(FrameHeight * 3);
				}
				break;
			}
			if (Velocity.Y > 0f)
			{
				FrameCounter++;
			}
			else if (Velocity.Y < 0f)
			{
				FrameCounter--;
			}
			if (FrameCounter < 6f)
			{
				FrameY = (short)(FrameHeight << 2);
			}
			else if (FrameCounter < 12f)
			{
				FrameY = (short)(FrameHeight * 5);
			}
			else if (FrameCounter < 18f)
			{
				FrameY = (short)(FrameHeight * 6);
			}
			if (FrameCounter < 0f)
			{
				FrameCounter = 0f;
			}
			if (FrameCounter > 17f)
			{
				FrameCounter = 17f;
			}
			break;
		case EntityID.NPCID.KING_SLIME:
			if (Velocity.Y != 0f)
			{
				FrameY = (short)(FrameHeight << 2);
				break;
			}
			FrameCounter++;
			if (num > 0)
			{
				FrameCounter++;
			}
			if (num == 4)
			{
				FrameCounter++;
			}
			if (FrameCounter >= 8f)
			{
				FrameY += FrameHeight;
				FrameCounter = 0f;
			}
			if (FrameY >= FrameHeight * 4)
			{
				FrameY = 0;
			}
			break;
		case EntityID.NPCID.THE_DESTROYER_BODY:
			if (AI2 == 0f)
			{
				FrameY = 0;
			}
			else
			{
				FrameY = FrameHeight;
			}
			break;
		case EntityID.NPCID.MIMIC:
			if (AI0 == 0f)
			{
				FrameCounter = 0f;
				FrameY = 0;
			}
			else
			{
				if (Velocity.Y == 0f)
				{
					FrameCounter--;
				}
				else
				{
					FrameCounter++;
				}
				if (FrameCounter < 0f)
				{
					FrameCounter = 0f;
				}
				else if (FrameCounter > 12f)
				{
					FrameCounter = 12f;
				}
				if (FrameCounter < 3f)
				{
					FrameY = FrameHeight;
				}
				else if (FrameCounter < 6f)
				{
					FrameY = (short)(FrameHeight << 1);
				}
				else if (FrameCounter < 9f)
				{
					FrameY = (short)(FrameHeight * 3);
				}
				else if (FrameCounter < 12f)
				{
					FrameY = (short)(FrameHeight << 2);
				}
				else if (FrameCounter < 15f)
				{
					FrameY = (short)(FrameHeight * 5);
				}
				else if (FrameCounter < 18f)
				{
					FrameY = (short)(FrameHeight << 2);
				}
				else if (FrameCounter < 21f)
				{
					FrameY = (short)(FrameHeight * 3);
				}
				else
				{
					FrameY = (short)(FrameHeight << 1);
					if (FrameCounter >= 24f)
					{
						FrameCounter = 3f;
					}
				}
			}
			if (AI3 == 2f)
			{
				FrameY = (short)(FrameY + FrameHeight * 6);
			}
			else if (AI3 == 3f)
			{
				FrameY = (short)(FrameY + FrameHeight * 12);
			}
			break;
		case EntityID.NPCID.WALL_OF_FLESH:
		case EntityID.NPCID.WALL_OF_FLESH_EYE:
			if (AI2 == 0f)
			{
				FrameCounter++;
				if (FrameCounter >= 12f)
				{
					FrameY += FrameHeight;
					FrameCounter = 0f;
				}
				if (FrameY >= FrameHeight * NpcFrameCount[Type])
				{
					FrameY = 0;
				}
			}
			else
			{
				FrameY = 0;
				FrameCounter = -60f;
			}
			break;
		case EntityID.NPCID.VULTURE:
			SpriteDirection = Direction;
			Rotation = Velocity.X * 0.1f;
			if (Velocity.X == 0f && Velocity.Y == 0f)
			{
				FrameY = 0;
				FrameCounter = 0f;
				break;
			}
			FrameCounter++;
			if (FrameCounter < 4f)
			{
				FrameY = FrameHeight;
				break;
			}
			FrameY = (short)(FrameHeight << 1);
			if (FrameCounter >= 7f)
			{
				FrameCounter = 0f;
			}
			break;
		case EntityID.NPCID.GASTROPOD:
		case EntityID.NPCID.SPECTRAL_GASTROPOD:
			SpriteDirection = Direction;
			Rotation = Velocity.X * 0.05f;
			if (AI3 > 0f)
			{
				FrameCounter = 0f;
				FrameY = (short)((((int)AI3 >> 3) + 3) * FrameHeight);
				break;
			}
			FrameCounter++;
			if (FrameCounter >= 8f)
			{
				FrameY += FrameHeight;
				FrameCounter = 0f;
			}
			if (FrameY >= FrameHeight * 3)
			{
				FrameY = 0;
			}
			break;
		case EntityID.NPCID.BIRD:
			SpriteDirection = Direction;
			Rotation = Velocity.X * 0.1f;
			if (Velocity.X == 0f && Velocity.Y == 0f)
			{
				FrameY = (short)(FrameHeight << 2);
				FrameCounter = 0f;
				break;
			}
			FrameCounter++;
			if (FrameCounter >= 4f)
			{
				FrameY += FrameHeight;
				FrameCounter = 0f;
			}
			if (FrameY >= FrameHeight * NpcFrameCount[Type])
			{
				FrameY = 0;
			}
			break;
		case EntityID.NPCID.DEMON:
		case EntityID.NPCID.VOODOO_DEMON:
		case EntityID.NPCID.ARCH_DEMON:
			SpriteDirection = Direction;
			Rotation = Velocity.X * 0.1f;
			FrameCounter++;
			if (FrameCounter < 6f)
			{
				FrameY = 0;
				break;
			}
			FrameY = FrameHeight;
			if (FrameCounter >= 11f)
			{
				FrameCounter = 0f;
			}
			break;
		case EntityID.NPCID.BLUE_JELLYFISH:
		case EntityID.NPCID.PINK_JELLYFISH:
		case EntityID.NPCID.GREEN_JELLYFISH:
			FrameCounter++;
			if (FrameCounter < 6f)
			{
				FrameY = 0;
				break;
			}
			if (FrameCounter < 12f)
			{
				FrameY = FrameHeight;
				break;
			}
			if (FrameCounter < 18f)
			{
				FrameY = (short)(FrameHeight << 1);
				break;
			}
			FrameY = (short)(FrameHeight * 3);
			if (FrameCounter >= 23f)
			{
				FrameCounter = 0f;
			}
			break;
		case EntityID.NPCID.DEMON_EYE:
		case EntityID.NPCID.METEOR_HEAD:
		case EntityID.NPCID.SLIMER:
			if (EntityID.DemonEyeTypes.Contains((EntityID.NPCID)Type))
			{
				if (Velocity.X > 0f)
				{
					SpriteDirection = 1;
					Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X);
				}
				if (Velocity.X < 0f)
				{
					SpriteDirection = -1;
					Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X) + 3.14f;
				}
			}
			else if (Type == 121)
			{
				if (Velocity.X > 0f)
				{
					SpriteDirection = 1;
				}
				if (Velocity.X < 0f)
				{
					SpriteDirection = -1;
				}
				Rotation = Velocity.X * 0.1f;
			}
			if (++FrameCounter >= 8f)
			{
				FrameY += FrameHeight;
				FrameCounter = 0f;
			}
			if (FrameY >= FrameHeight * NpcFrameCount[Type])
			{
				FrameY = 0;
			}
			break;
		case EntityID.NPCID.WANDERING_EYE:
			if (Velocity.X > 0f)
			{
				SpriteDirection = 1;
				Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X);
			}
			if (Velocity.X < 0f)
			{
				SpriteDirection = -1;
				Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X) + 3.14f;
			}
			if (++FrameCounter >= 8f)
			{
				FrameY = FrameHeight;
			}
			else
			{
				FrameY = 0;
			}
			if (FrameCounter >= 16f)
			{
				FrameY = 0;
				FrameCounter = 0f;
			}
			if ((double)Life < (double)LifeMax * 0.5)
			{
				FrameY = (short)(FrameY + (FrameHeight << 1));
			}
			break;
		case EntityID.NPCID.THE_HUNGRY_II:
			if (Velocity.X > 0f)
			{
				SpriteDirection = 1;
				Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X);
			}
			if (Velocity.X < 0f)
			{
				SpriteDirection = -1;
				Rotation = (float)Math.Atan2(Velocity.Y, Velocity.X) + 3.14f;
			}
			if (++FrameCounter >= 5f)
			{
				FrameY += FrameHeight;
				FrameCounter = 0f;
			}
			if (FrameY >= FrameHeight * NpcFrameCount[Type])
			{
				FrameY = 0;
			}
			break;
		case EntityID.NPCID.PIXIE:
			if (Velocity.X > 0f)
			{
				SpriteDirection = 1;
			}
			else
			{
				SpriteDirection = -1;
			}
			Rotation = Velocity.X * 0.1f;
			if (++FrameCounter >= 4f)
			{
				FrameY += FrameHeight;
				FrameCounter = 0f;
			}
			if (FrameY >= FrameHeight * NpcFrameCount[Type])
			{
				FrameY = 0;
			}
			break;
		case EntityID.NPCID.GOLDFISH:
		case EntityID.NPCID.CORRUPT_GOLDFISH:
		case EntityID.NPCID.PIRANHA:
		case EntityID.NPCID.ANGLER_FISH:
			SpriteDirection = Direction;
			FrameCounter++;
			if (IsWet)
			{
				if (FrameCounter < 6f)
				{
					FrameY = 0;
				}
				else if (FrameCounter < 12f)
				{
					FrameY = FrameHeight;
				}
				else if (FrameCounter < 18f)
				{
					FrameY = (short)(FrameHeight << 1);
				}
				else if (FrameCounter < 24f)
				{
					FrameY = (short)(FrameHeight * 3);
				}
				else
				{
					FrameCounter = 0f;
				}
			}
			else if (FrameCounter < 6f)
			{
				FrameY = (short)(FrameHeight << 2);
			}
			else if (FrameCounter < 12f)
			{
				FrameY = (short)(FrameHeight * 5);
			}
			else
			{
				FrameCounter = 0f;
			}
			break;
		case EntityID.NPCID.ANTLION:
		case EntityID.NPCID.ALBINO_ANTLION:
			if (AI0 < 190f)
			{
				if (++FrameCounter >= 6f)
				{
					FrameCounter = 0f;
					FrameY += FrameHeight;
					if (FrameY / FrameHeight >= NpcFrameCount[Type] - 1)
					{
						FrameY = 0;
					}
				}
			}
			else
			{
				FrameCounter = 0f;
				FrameY = (short)(FrameHeight * (NpcFrameCount[Type] - 1));
			}
			break;
		case EntityID.NPCID.UNICORN:
			if (Velocity.Y == 0f || IsWet)
			{
				if (Velocity.X < -2f)
				{
					SpriteDirection = -1;
				}
				else if (Velocity.X > 2f)
				{
					SpriteDirection = 1;
				}
				else
				{
					SpriteDirection = Direction;
				}
			}
			if (Velocity.Y != 0f)
			{
				FrameY = (short)(FrameHeight * 15);
				FrameCounter = 0f;
				break;
			}
			if (Velocity.X == 0f)
			{
				FrameCounter = 0f;
				FrameY = 0;
				break;
			}
			if (Math.Abs(Velocity.X) < 3f)
			{
				FrameCounter += Math.Abs(Velocity.X);
				if (FrameCounter >= 6f)
				{
					FrameCounter = 0f;
					FrameY += FrameHeight;
					if (FrameY / FrameHeight >= 9)
					{
						FrameY = FrameHeight;
					}
					if (FrameY / FrameHeight <= 0)
					{
						FrameY = FrameHeight;
					}
				}
				break;
			}
			FrameCounter += Math.Abs(Velocity.X);
			if (FrameCounter >= 10f)
			{
				FrameCounter = 0f;
				FrameY += FrameHeight;
				if (FrameY / FrameHeight >= 15)
				{
					FrameY = (short)(FrameHeight * 9);
				}
				if (FrameY / FrameHeight <= 8)
				{
					FrameY = (short)(FrameHeight * 9);
				}
			}
			break;
		case EntityID.NPCID.SKELETRON_PRIME:
			if (AI1 == 0f)
			{
				FrameCounter++;
				if (FrameCounter >= 12f)
				{
					FrameCounter = 0f;
					FrameY += FrameHeight;
					if (FrameY / FrameHeight >= 2)
					{
						FrameY = 0;
					}
				}
			}
			else
			{
				FrameCounter = 0f;
				FrameY = (short)(FrameHeight << 1);
			}
			break;
		case EntityID.NPCID.PRIME_SAW:
			if (Velocity.Y == 0f)
			{
				SpriteDirection = Direction;
			}
			FrameCounter++;
			if (FrameCounter >= 2f)
			{
				FrameCounter = 0f;
				FrameY += FrameHeight;
				if (FrameY / FrameHeight >= NpcFrameCount[Type])
				{
					FrameY = 0;
				}
			}
			break;
		case EntityID.NPCID.PRIME_VICE:
			if (Velocity.Y == 0f)
			{
				SpriteDirection = Direction;
			}
			FrameCounter++;
			if (FrameCounter >= 8f)
			{
				FrameCounter = 0f;
				FrameY += FrameHeight;
				if (FrameY / FrameHeight >= NpcFrameCount[Type])
				{
					FrameY = 0;
				}
			}
			break;
		case EntityID.NPCID.CRAB:
			if (Velocity.Y == 0f)
			{
				SpriteDirection = Direction;
			}
			FrameCounter++;
			if (FrameCounter >= 6f)
			{
				FrameCounter = 0f;
				FrameY += FrameHeight;
				if (FrameY / FrameHeight >= NpcFrameCount[Type])
				{
					FrameY = 0;
				}
			}
			break;
		case EntityID.NPCID.CLOWN:
			if (Velocity.Y == 0f && ((Velocity.X <= 0f && Direction < 0) || (Velocity.X >= 0f && Direction > 0)))
			{
				SpriteDirection = Direction;
			}
			FrameCounter += Math.Abs(Velocity.X);
			if (FrameCounter >= 7f)
			{
				FrameCounter -= 7f;
				FrameY += FrameHeight;
				if (FrameY / FrameHeight >= NpcFrameCount[Type])
				{
					FrameY = 0;
				}
			}
			break;
		case EntityID.NPCID.CURSED_HAMMER:
		case EntityID.NPCID.ENCHANTED_SWORD:
		case EntityID.NPCID.SHADOW_HAMMER:
			if (AI0 == 2f)
			{
				FrameCounter = 0f;
				FrameY = 0;
				break;
			}
			FrameCounter++;
			if (FrameCounter >= 4f)
			{
				FrameCounter = 0f;
				FrameY += FrameHeight;
				if (FrameY / FrameHeight >= NpcFrameCount[Type])
				{
					FrameY = 0;
				}
			}
			break;
		case EntityID.NPCID.BLAZING_WHEEL:
			FrameCounter++;
			if (FrameCounter >= 3f)
			{
				FrameCounter = 0f;
				FrameY += FrameHeight;
				if (FrameY / FrameHeight >= NpcFrameCount[Type])
				{
					FrameY = 0;
				}
			}
			break;
		case EntityID.NPCID.SHARK:
		case EntityID.NPCID.ORKA:
			SpriteDirection = Direction;
			FrameCounter++;
			if (IsWet)
			{
				if (FrameCounter < 6f)
				{
					FrameY = 0;
				}
				else if (FrameCounter < 12f)
				{
					FrameY = FrameHeight;
				}
				else if (FrameCounter < 18f)
				{
					FrameY = (short)(FrameHeight << 1);
				}
				else if (FrameCounter < 24f)
				{
					FrameY = (short)(FrameHeight * 3);
				}
				else
				{
					FrameCounter = 0f;
				}
			}
			break;
		case EntityID.NPCID.HARPY:
		case EntityID.NPCID.CAVE_BAT:
		case EntityID.NPCID.JUNGLE_BAT:
		case EntityID.NPCID.HELLBAT:
		case EntityID.NPCID.WRAITH:
		case EntityID.NPCID.GIANT_BAT:
		case EntityID.NPCID.ILLUMINANT_BAT:
			if (Velocity.X > 0f)
			{
				SpriteDirection = 1;
			}
			if (Velocity.X < 0f)
			{
				SpriteDirection = -1;
			}
			Rotation = Velocity.X * 0.1f;
			FrameCounter++;
			if (FrameCounter >= 6f)
			{
				FrameY += FrameHeight;
				FrameCounter = 0f;
			}
			if (FrameY >= FrameHeight * 4)
			{
				FrameY = 0;
			}
			break;
		case EntityID.NPCID.HORNET:
		case EntityID.NPCID.DRAGON_HORNET:
			FrameCounter++;
			if (FrameCounter < 2f)
			{
				FrameY = 0;
			}
			else if (FrameCounter < 4f)
			{
				FrameY = FrameHeight;
			}
			else if (FrameCounter < 6f)
			{
				FrameY = (short)(FrameHeight << 1);
			}
			else if (FrameCounter < 8f)
			{
				FrameY = FrameHeight;
			}
			else
			{
				FrameCounter = 0f;
			}
			break;
		case EntityID.NPCID.MAN_EATER:
		case EntityID.NPCID.SNATCHER:
		case EntityID.NPCID.DRAGON_SNATCHER:
			FrameCounter++;
			if (FrameCounter < 6f)
			{
				FrameY = 0;
			}
			else if (FrameCounter < 12f)
			{
				FrameY = FrameHeight;
			}
			else if (FrameCounter < 18f)
			{
				FrameY = (short)(FrameHeight << 1);
			}
			else if (FrameCounter < 24f)
			{
				FrameY = FrameHeight;
			}
			if (FrameCounter == 23f)
			{
				FrameCounter = 0f;
			}
			break;
		case EntityID.NPCID.THE_HUNGRY:
			FrameCounter++;
			if (FrameCounter < 3f)
			{
				FrameY = 0;
			}
			else if (FrameCounter < 6f)
			{
				FrameY = FrameHeight;
			}
			else if (FrameCounter < 12f)
			{
				FrameY = (short)(FrameHeight << 1);
			}
			else if (FrameCounter < 15f)
			{
				FrameY = FrameHeight;
			}
			if (FrameCounter == 15f)
			{
				FrameCounter = 0f;
			}
			break;
		case EntityID.NPCID.CLINGER:
			FrameCounter++;
			if (FrameCounter > 6f)
			{
				FrameY = (short)(FrameY + (FrameHeight << 1));
				FrameCounter = 0f;
			}
			if (FrameY > FrameHeight * 2)
			{
				FrameY = 0;
			}
			break;
		case EntityID.NPCID.MERCHANT:
		case EntityID.NPCID.NURSE:
		case EntityID.NPCID.ARMS_DEALER:
		case EntityID.NPCID.DRYAD:
		case EntityID.NPCID.SKELETON:
		case EntityID.NPCID.GUIDE:
		case EntityID.NPCID.GOBLIN_PEON:
		case EntityID.NPCID.GOBLIN_THIEF:
		case EntityID.NPCID.GOBLIN_WARRIOR:
		case EntityID.NPCID.BONES:
		case EntityID.NPCID.OLD_MAN:
		case EntityID.NPCID.DEMOLITIONIST:
		case EntityID.NPCID.UNDEAD_MINER:
		case EntityID.NPCID.CLOTHIER:
		case EntityID.NPCID.GOBLIN_SCOUT:
		case EntityID.NPCID.ARMORED_SKELETON:
		case EntityID.NPCID.MUMMY:
		case EntityID.NPCID.DARK_MUMMY:
		case EntityID.NPCID.LIGHT_MUMMY:
		case EntityID.NPCID.WEREWOLF:
		case EntityID.NPCID.GOBLIN_TINKERER:
		case EntityID.NPCID.WIZARD:
		case EntityID.NPCID.CHAOS_ELEMENTAL:
		case EntityID.NPCID.MECHANIC:
		case EntityID.NPCID.POSSESSED_ARMOR:
		case EntityID.NPCID.SANTA_CLAUS:
		case EntityID.NPCID.VAMPIRE_MINER:
		case EntityID.NPCID.SHADOW_MUMMY:
		case EntityID.NPCID.SPECTRAL_ELEMENTAL:
		case EntityID.NPCID.SPECTRAL_MUMMY:
			if (Velocity.Y == 0f)
			{
				if (Direction == 1)
				{
					SpriteDirection = 1;
				}
				else if (Direction == -1)
				{
					SpriteDirection = -1;
				}
				if (Velocity.X == 0f)
				{
					if (Type == 140)
					{
						FrameY = FrameHeight;
					}
					else
					{
						FrameY = 0;
					}
					FrameCounter = 0f;
					break;
				}
				FrameCounter += Math.Abs(Velocity.X) * 2f;
				if (++FrameCounter > 6f)
				{
					FrameY += FrameHeight;
					FrameCounter = 0f;
				}
				if (FrameY / FrameHeight >= NpcFrameCount[Type])
				{
					FrameY = (short)(FrameHeight << 1);
				}
			}
			else
			{
				FrameCounter = 0f;
				if (EntityID.MummyTypes.Contains((EntityID.NPCID)Type) || Type == 21 || Type == 31 || Type == 44 || Type == 149 || Type == 77 || Type == 120 || Type == 154 || Type == 140)
				{
					FrameY = 0;
				}
				else
				{
					FrameY = FrameHeight;
				}
			}
			break;
		case EntityID.NPCID.SKELETON_ARCHER:
			if (Velocity.Y == 0f)
			{
				if (Direction != 0)
				{
					SpriteDirection = Direction;
				}
				if (AI2 > 0f)
				{
					SpriteDirection = Direction;
					FrameY = (short)(FrameHeight * (int)AI2);
					FrameCounter = 0f;
					break;
				}
				if (FrameY < FrameHeight * 6)
				{
					FrameY = (short)(FrameHeight * 6);
				}
				FrameCounter += Math.Abs(Velocity.X) * 2f;
				FrameCounter += Velocity.X;
				if (FrameCounter > 6f)
				{
					FrameY += FrameHeight;
					FrameCounter = 0f;
				}
				if (FrameY / FrameHeight >= NpcFrameCount[Type])
				{
					FrameY = (short)(FrameHeight * 6);
				}
			}
			else
			{
				FrameCounter = 0f;
				FrameY = 0;
			}
			break;
		case EntityID.NPCID.GOBLIN_ARCHER:
			if (Velocity.Y == 0f)
			{
				if (Direction != 0)
				{
					SpriteDirection = Direction;
				}
				if (AI2 > 0f)
				{
					SpriteDirection = Direction;
					FrameY = (short)(FrameHeight * ((int)AI2 - 1));
					FrameCounter = 0f;
					break;
				}
				if (FrameY < FrameHeight * 7)
				{
					FrameY = (short)(FrameHeight * 7);
				}
				FrameCounter += Math.Abs(Velocity.X) * 2f;
				FrameCounter += Velocity.X * 1.3f;
				if (FrameCounter > 6f)
				{
					FrameY += FrameHeight;
					FrameCounter = 0f;
				}
				if (FrameY / FrameHeight >= NpcFrameCount[Type])
				{
					FrameY = (short)(FrameHeight * 7);
				}
			}
			else
			{
				FrameCounter = 0f;
				FrameY = (short)(FrameHeight * 6);
			}
			break;
		case EntityID.NPCID.ZOMBIE:
		case EntityID.NPCID.DOCTOR_BONES:
		case EntityID.NPCID.THE_GROOM:
		case EntityID.NPCID.BALD_ZOMBIE:
			if (Velocity.Y == 0f && Direction != 0)
			{
				SpriteDirection = Direction;
			}
			if (Velocity.Y != 0f || (Direction == -1 && Velocity.X > 0f) || (Direction == 1 && Velocity.X < 0f))
			{
				FrameCounter = 0f;
				FrameY = (short)(FrameHeight << 1);
				break;
			}
			if (Velocity.X == 0f)
			{
				FrameCounter = 0f;
				FrameY = 0;
				break;
			}
			FrameCounter += Math.Abs(Velocity.X);
			if (FrameCounter < 8f)
			{
				FrameY = 0;
			}
			else if (FrameCounter < 16f)
			{
				FrameY = FrameHeight;
			}
			else if (FrameCounter < 24f)
			{
				FrameY = (short)(FrameHeight << 1);
			}
			else if (FrameCounter < 32f)
			{
				FrameY = FrameHeight;
			}
			else
			{
				FrameCounter = 0f;
			}
			break;
		case EntityID.NPCID.BUNNY:
		case EntityID.NPCID.CORRUPT_BUNNY:
			if (Velocity.Y == 0f)
			{
				if (Direction != 0)
				{
					SpriteDirection = Direction;
				}
				if (Velocity.X == 0f)
				{
					FrameY = 0;
					FrameCounter = 0f;
					break;
				}
				FrameCounter += Math.Abs(Velocity.X);
				FrameCounter++;
				if (FrameCounter > 6f)
				{
					FrameY += FrameHeight;
					FrameCounter = 0f;
				}
				if (FrameY / FrameHeight >= NpcFrameCount[Type])
				{
					FrameY = 0;
				}
			}
			else if (Velocity.Y < 0f)
			{
				FrameCounter = 0f;
				FrameY = (short)(FrameHeight << 2);
			}
			else if (Velocity.Y > 0f)
			{
				FrameCounter = 0f;
				FrameY = (short)(FrameHeight * 6);
			}
			break;
		case EntityID.NPCID.EYE_OF_CTHULHU:
		case EntityID.NPCID.RETINAZER:
		case EntityID.NPCID.SPAZMATISM:
		case EntityID.NPCID.OCRAM:
			if (++FrameCounter < 7f)
			{
				FrameY = 0;
			}
			else if (FrameCounter < 14f)
			{
				FrameY = FrameHeight;
			}
			else if (FrameCounter < 21f)
			{
				FrameY = (short)(FrameHeight << 1);
			}
			else
			{
				FrameCounter = 0f;
				FrameY = 0;
			}
			if (AI0 > 1f)
			{
				FrameY = (short)(FrameY + FrameHeight * 3);
			}
			break;
		case EntityID.NPCID.SERVANT_OF_CTHULHU:
		case EntityID.NPCID.SERVANT_OF_OCRAM:
			if (++FrameCounter >= 8f)
			{
				FrameY += FrameHeight;
				FrameCounter = 0f;
			}
			if (FrameY >= FrameHeight * NpcFrameCount[Type])
			{
				FrameY = 0;
			}
			break;
		case EntityID.NPCID.CORRUPTOR:
			if (++FrameCounter < 6f)
			{
				FrameY = 0;
				break;
			}
			if (FrameCounter < 12f)
			{
				FrameY = FrameHeight;
				break;
			}
			if (FrameCounter < 18f)
			{
				FrameY = (short)(FrameHeight << 1);
				break;
			}
			FrameY = FrameHeight;
			if (FrameCounter >= 23f)
			{
				FrameCounter = 0f;
			}
			break;
		case EntityID.NPCID.EATER_OF_SOULS:
			FrameCounter++;
			if (FrameCounter >= 8f)
			{
				FrameY += FrameHeight;
				FrameCounter = 0f;
			}
			if (FrameY >= FrameHeight * NpcFrameCount[Type])
			{
				FrameY = 0;
			}
			break;
		case EntityID.NPCID.FIRE_IMP:
			if (Velocity.Y == 0f && Direction != 0)
			{
				SpriteDirection = Direction;
			}
			if (AI1 > 0f)
			{
				if (FrameY < 4)
				{
					FrameCounter = 0f;
				}
				FrameCounter++;
				if (FrameCounter <= 4f)
				{
					FrameY = (short)(FrameHeight << 2);
					break;
				}
				if (FrameCounter <= 8f)
				{
					FrameY = (short)(FrameHeight * 5);
					break;
				}
				if (FrameCounter <= 12f)
				{
					FrameY = (short)(FrameHeight * 6);
					break;
				}
				if (FrameCounter <= 16f)
				{
					FrameY = (short)(FrameHeight * 7);
					break;
				}
				if (FrameCounter <= 20f)
				{
					FrameY = (short)(FrameHeight << 3);
					break;
				}
				FrameY = (short)(FrameHeight * 9);
				FrameCounter = 100f;
				break;
			}
			FrameCounter++;
			if (FrameCounter <= 4f)
			{
				FrameY = 0;
				break;
			}
			if (FrameCounter <= 8f)
			{
				FrameY = FrameHeight;
				break;
			}
			if (FrameCounter <= 12f)
			{
				FrameY = (short)(FrameHeight << 1);
				break;
			}
			FrameY = (short)(FrameHeight * 3);
			if (FrameCounter >= 16f)
			{
				FrameCounter = 0f;
			}
			break;
		case EntityID.NPCID.GOBLIN_SORCERER:
		case EntityID.NPCID.DARK_CASTER:
		case EntityID.NPCID.TIM:
			if (Velocity.Y == 0f && Direction != 0)
			{
				SpriteDirection = Direction;
			}
			FrameY = 0;
			if (Velocity.Y != 0f)
			{
				FrameY += FrameHeight;
			}
			else if (AI1 > 0f)
			{
				FrameY = (short)(FrameY + (FrameHeight << 1));
			}
			break;
		case EntityID.NPCID.CURSED_SKULL:
		case EntityID.NPCID.DRAGON_SKULL:
			if (++FrameCounter >= 4f)
			{
				FrameY += FrameHeight;
				FrameCounter = 0f;
			}
			if (FrameY >= FrameHeight * NpcFrameCount[Type])
			{
				FrameY = 0;
			}
			break;
		case EntityID.NPCID.DEVOURER_HEAD:
		case EntityID.NPCID.DEVOURER_BODY:
		case EntityID.NPCID.DEVOURER_TAIL:
		case EntityID.NPCID.GIANT_WORM_HEAD:
		case EntityID.NPCID.GIANT_WORM_BODY:
		case EntityID.NPCID.GIANT_WORM_TAIL:
		case EntityID.NPCID.EATER_OF_WORLDS_HEAD:
		case EntityID.NPCID.EATER_OF_WORLDS_BODY:
		case EntityID.NPCID.EATER_OF_WORLDS_TAIL:
		case EntityID.NPCID.BURNING_SPHERE:
		case EntityID.NPCID.CHAOS_BALL:
		case EntityID.NPCID.WATER_SPHERE:
		case EntityID.NPCID.SKELETRON_HEAD:
		case EntityID.NPCID.SKELETRON_HAND:
		case EntityID.NPCID.BONE_SERPENT_HEAD:
		case EntityID.NPCID.BONE_SERPENT_BODY:
		case EntityID.NPCID.BONE_SERPENT_TAIL:
		case EntityID.NPCID.DUNGEON_GUARDIAN:
		case EntityID.NPCID.SPIKE_BALL:
		case EntityID.NPCID.XXX_UNUSED_XXX:
		case EntityID.NPCID.WYVERN_HEAD:
		case EntityID.NPCID.WYVERN_LEGS:
		case EntityID.NPCID.WYVERN_BODY1:
		case EntityID.NPCID.WYVERN_BODY2:
		case EntityID.NPCID.WYVERN_BODY3:
		case EntityID.NPCID.WYVERN_TAIL:
		case EntityID.NPCID.DIGGER_HEAD:
		case EntityID.NPCID.DIGGER_BODY:
		case EntityID.NPCID.DIGGER_TAIL:
		case EntityID.NPCID.SEEKER_HEAD:
		case EntityID.NPCID.SEEKER_BODY:
		case EntityID.NPCID.SEEKER_TAIL:
		case EntityID.NPCID.BOUND_GOBLIN:
		case EntityID.NPCID.BOUND_WIZARD:
		case EntityID.NPCID.VILE_SPIT:
		case EntityID.NPCID.LEECH_HEAD:
		case EntityID.NPCID.LEECH_BODY:
		case EntityID.NPCID.LEECH_TAIL:
		case EntityID.NPCID.BOUND_MECHANIC:
		case EntityID.NPCID.PRIME_CANNON:
		case EntityID.NPCID.PRIME_LASER:
		case EntityID.NPCID.THE_DESTROYER_HEAD:
		case EntityID.NPCID.THE_DESTROYER_TAIL:
		case EntityID.NPCID.PROBE:
		case EntityID.NPCID.SUICIDE_SNOWMAN:
		case EntityID.NPCID.ARCH_WYVERN_HEAD:
		case EntityID.NPCID.ARCH_WYVERN_LEGS:
		case EntityID.NPCID.ARCH_WYVERN_BODY1:
		case EntityID.NPCID.ARCH_WYVERN_BODY2:
		case EntityID.NPCID.ARCH_WYVERN_BODY3:
		case EntityID.NPCID.ARCH_WYVERN_TAIL:
			break;
		}
	}

	public void TargetClosest(bool ShouldFaceTarget = true)
	{
		int num = -1;
		Target = 0;
		for (int i = 0; i < 8; i++)
		{
			if (Main.PlayerSet[i].Active != 0 && !Main.PlayerSet[i].IsDead && (num == -1 || Math.Abs(Main.PlayerSet[i].XYWH.X + 10 - XYWH.X + (Width >> 1)) + Math.Abs(Main.PlayerSet[i].XYWH.Y + 21 - XYWH.Y + (Height >> 1)) < num))
			{
				num = Math.Abs(Main.PlayerSet[i].XYWH.X + 10 - XYWH.X + (Width >> 1)) + Math.Abs(Main.PlayerSet[i].XYWH.Y + 21 - XYWH.Y + (Height >> 1));
				Target = (byte)i;
			}
		}
		TargetRect = Main.PlayerSet[Target].XYWH;
		if (Main.PlayerSet[Target].IsDead)
		{
			ShouldFaceTarget = false;
		}
		if (ShouldFaceTarget)
		{
			Direction = 1;
			if (TargetRect.X + (TargetRect.Width >> 1) < XYWH.X + (Width >> 1))
			{
				Direction = -1;
			}
			DirectionY = 1;
			if (TargetRect.Y + (TargetRect.Height >> 1) < XYWH.Y + (Height >> 1))
			{
				DirectionY = -1;
			}
		}
		if (IsConfused)
		{
			Direction *= -1;
		}
		if ((Direction != OldDirection || DirectionY != OldDirectionY || Target != OldTarget) && !HasXCollision && !HasYCollision)
		{
			ShouldNetUpdate = true;
		}
	}

	public void CheckActive()
	{
		if (Active == 0 || Type == 8 || Type == 9 || Type == 11 || Type == 12 || Type == 14 || Type == 15 || Type == 40 || Type == 41 || Type == 96 || Type == 97 || Type == 99 || Type == 100 || (Type > 87 && Type <= 92) || (Type > 159 && Type <= 164) || Type == 118 || Type == 119 || Type == 113 || Type == 114 || Type == 115 || (Type >= 134 && Type <= 136))
		{
			return;
		}
		if (IsTownNPC)
		{
			Rectangle rectangle = new Rectangle(XYWH.X + (Width >> 1) - 1920, XYWH.Y + (Height >> 1) - 1080, 3840, 2160);
			for (int i = 0; i < 8; i++)
			{
				if (Main.PlayerSet[i].Active != 0 && rectangle.Intersects(Main.PlayerSet[i].XYWH))
				{
					Main.PlayerSet[i].TownNPCs += NpcSlots;
				}
			}
			return;
		}
		bool flag = false;
		Rectangle rectangle2 = new Rectangle(XYWH.X + (Width >> 1) - 3264, XYWH.Y + (Height >> 1) - 1836, 6528, 3672);
		Rectangle rectangle3 = new Rectangle(XYWH.X + (Width >> 1) - 960 - Width, XYWH.Y + (Height >> 1) - 540 - Height, 1920 + Width * 2, 1080 + Height * 2);
		for (int j = 0; j < 8; j++)
		{
			if (Main.PlayerSet[j].Active == 0)
			{
				continue;
			}
			if (rectangle2.Intersects(Main.PlayerSet[j].XYWH))
			{
				flag = true;
				if (Type != 25 && Type != 30 && Type != 33 && LifeMax > 0)
				{
					Main.PlayerSet[j].ActiveNPCs += NpcSlots;
				}
			}
			else if (IsBoss || Type == 7 || Type == 10 || Type == 13 || Type == 39 || Type == 87 || Type == 159 || Type == 35 || Type == 36 || (Type >= 127 && Type <= 131))
			{
				flag = true;
			}
			if (rectangle3.Intersects(Main.PlayerSet[j].XYWH))
			{
				TimeLeft = 750;
			}
		}
		TimeLeft--;
		if (TimeLeft <= 0)
		{
			flag = false;
		}
		if (flag || Main.NetMode == 1)
		{
			return;
		}
		NoSpawnCycle = true;
		Active = 0;
		NetSkip = -1;
		Life = 0;
		NetMessage.CreateMessage1(23, WhoAmI);
		NetMessage.SendMessage();
		if (AIStyle != 6)
		{
			return;
		}
		for (int num = (int)AI0; num > 0; num = (int)Main.NPCSet[num].AI0)
		{
			if (Main.NPCSet[num].Active != 0)
			{
				Main.NPCSet[num].Active = 0;
				Main.NPCSet[num].Life = 0;
				Main.NPCSet[num].NetSkip = -1;
				NetMessage.CreateMessage1(23, num);
				NetMessage.SendMessage();
			}
		}
	}

	public static void SpawnNPC()
	{
		if (NoSpawnCycle)
		{
			NoSpawnCycle = false;
			return;
		}
		bool flag = false;
		bool flag2 = false;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < 8; i++)
		{
			if (Main.PlayerSet[i].Active != 0)
			{
				num3++;
			}
		}
		for (int j = 0; j < 8; j++)
		{
			if (Main.PlayerSet[j].Active == 0 || Main.PlayerSet[j].IsDead)
			{
				continue;
			}
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			if (Main.InvasionType > 0 && Main.InvasionDelay == 0 && Main.InvasionSize > 0 && Main.PlayerSet[j].Position.Y < (float)(Main.WorldSurfacePixels + 1080))
			{
				int num4 = 3000;
				if (Main.PlayerSet[j].Position.X > Main.InvasionX * 16f - (float)num4 && Main.PlayerSet[j].Position.X < Main.InvasionX * 16f + (float)num4)
				{
					flag4 = true;
				}
			}
			flag = false;
			SpawnRate = 600;
			MaxSpawns = 5;
			if (Main.InHardMode)
			{
				SpawnRate = 540;
				MaxSpawns = 6;
			}
			if (Main.PlayerSet[j].Position.Y > (float)((Main.MaxTilesY - 200) * 16))
			{
				MaxSpawns *= 2;
			}
			else if (Main.PlayerSet[j].Position.Y > (float)((Main.RockLayer << 4) + 1080))
			{
				SpawnRate = (int)((double)SpawnRate * 0.4);
				MaxSpawns = (int)((double)MaxSpawns * 1.9);
			}
			else if (Main.PlayerSet[j].Position.Y > (float)((Main.WorldSurface << 4) + 1080))
			{
				if (Main.InHardMode)
				{
					SpawnRate = (int)((double)SpawnRate * 0.45);
					MaxSpawns = (int)((double)MaxSpawns * 1.8);
				}
				else
				{
					SpawnRate >>= 1;
					MaxSpawns = (int)((double)MaxSpawns * 1.7);
				}
			}
			else if (!Main.GameTime.DayTime)
			{
				SpawnRate = (int)((double)SpawnRate * 0.6);
				MaxSpawns = (int)((double)MaxSpawns * 1.3);
				if (Main.GameTime.IsBloodMoon)
				{
					SpawnRate = (int)((double)SpawnRate * 0.3);
					MaxSpawns = (int)((double)MaxSpawns * 1.8);
				}
			}
			if (Main.PlayerSet[j].ZoneDungeon)
			{
				SpawnRate = (int)((double)SpawnRate * 0.4);
				MaxSpawns = (int)((float)MaxSpawns * 1.7f);
			}
			else if (Main.PlayerSet[j].ZoneJungle)
			{
				SpawnRate = (int)((double)SpawnRate * 0.4);
				MaxSpawns = (int)((float)MaxSpawns * 1.5f);
			}
			else if (Main.PlayerSet[j].ZoneEvil)
			{
				SpawnRate = (int)((double)SpawnRate * 0.65);
				MaxSpawns = (int)((float)MaxSpawns * 1.3f);
			}
			else if (Main.PlayerSet[j].ZoneMeteor)
			{
				SpawnRate = (int)((double)SpawnRate * 0.4);
				MaxSpawns = (int)((float)MaxSpawns * 1.1f);
			}
			if (Main.PlayerSet[j].zoneHoly && Main.PlayerSet[j].Position.Y > (float)((Main.RockLayer << 4) + 1080))
			{
				SpawnRate = (int)((double)SpawnRate * 0.65);
				MaxSpawns = (int)((float)MaxSpawns * 1.3f);
			}
			if (WoF >= 0 && Main.PlayerSet[j].Position.Y > (float)((Main.MaxTilesY - 200) * 16))
			{
				MaxSpawns = (int)((float)MaxSpawns * 0.3f);
				SpawnRate *= 3;
			}
			if (Main.PlayerSet[j].ActiveNPCs < (float)(int)((double)MaxSpawns * 0.2))
			{
				SpawnRate = (int)((float)SpawnRate * 0.6f);
			}
			else if (Main.PlayerSet[j].ActiveNPCs < (float)(int)((double)MaxSpawns * 0.4))
			{
				SpawnRate = (int)((float)SpawnRate * 0.7f);
			}
			else if (Main.PlayerSet[j].ActiveNPCs < (float)(int)((double)MaxSpawns * 0.6))
			{
				SpawnRate = (int)((float)SpawnRate * 0.8f);
			}
			else if (Main.PlayerSet[j].ActiveNPCs < (float)(int)((double)MaxSpawns * 0.8))
			{
				SpawnRate = (int)((float)SpawnRate * 0.9f);
			}
			if (Main.PlayerSet[j].Position.Y * 16f > (float)(Main.WorldSurface + Main.RockLayer >> 1) || Main.PlayerSet[j].ZoneEvil)
			{
				if ((double)Main.PlayerSet[j].ActiveNPCs < (double)MaxSpawns * 0.2)
				{
					SpawnRate = (int)((float)SpawnRate * 0.7f);
				}
				else if ((double)Main.PlayerSet[j].ActiveNPCs < (double)MaxSpawns * 0.4)
				{
					SpawnRate = (int)((float)SpawnRate * 0.9f);
				}
			}
			if (Main.PlayerSet[j].Inventory[Main.PlayerSet[j].SelectedItem].Type == 148)
			{
				SpawnRate = (int)((double)SpawnRate * 0.75);
				MaxSpawns = (int)((float)MaxSpawns * 1.5f);
			}
			if (Main.PlayerSet[j].enemySpawns)
			{
				SpawnRate = (int)((double)SpawnRate * 0.5);
				MaxSpawns = (int)((float)MaxSpawns * 2f);
			}
			if ((double)SpawnRate < 60.0)
			{
				SpawnRate = 60;
			}
			if (MaxSpawns > 15)
			{
				MaxSpawns = 15;
			}
			if (flag4)
			{
				MaxSpawns = (int)(5.0 * (2.0 + 0.3 * (double)num3));
				SpawnRate = 20;
			}
			if (Main.PlayerSet[j].ZoneDungeon && !HasDownedBoss3)
			{
				SpawnRate = 10;
			}
			bool flag6 = false;
			if (!flag4 && (!Main.GameTime.IsBloodMoon || Main.GameTime.DayTime) && !Main.PlayerSet[j].ZoneDungeon && !Main.PlayerSet[j].ZoneEvil && !Main.PlayerSet[j].ZoneMeteor)
			{
				if (Main.PlayerSet[j].TownNPCs == 1f)
				{
					flag3 = true;
					if (Main.Rand.Next(3) <= 1)
					{
						flag6 = true;
						MaxSpawns = (int)((double)(float)MaxSpawns * 0.6);
					}
					else
					{
						SpawnRate = (int)((float)SpawnRate * 2f);
					}
				}
				else if (Main.PlayerSet[j].TownNPCs == 2f)
				{
					flag3 = true;
					if (Main.Rand.Next(3) == 0)
					{
						flag6 = true;
						MaxSpawns = (int)((double)(float)MaxSpawns * 0.6);
					}
					else
					{
						SpawnRate = (int)((float)SpawnRate * 3f);
					}
				}
				else if (Main.PlayerSet[j].TownNPCs >= 3f)
				{
					flag3 = true;
					flag6 = true;
					MaxSpawns = (int)((double)(float)MaxSpawns * 0.6);
				}
			}
			if (Main.PlayerSet[j].ActiveNPCs < (float)MaxSpawns && Main.Rand.Next(SpawnRate) == 0)
			{
				int num5 = Main.PlayerSet[j].XYWH.X >> 4;
				int num6 = Main.PlayerSet[j].XYWH.Y >> 4;
				int num7 = num5 - 84;
				int num8 = num5 + 84;
				int num9 = num6 - 46;
				int num10 = num6 + 46;
				int num11 = num5 - 62;
				int num12 = num5 + 62;
				int num13 = num6 - 34;
				int num14 = num6 + 34;
				if (num7 < 0)
				{
					num7 = 0;
				}
				else if (num8 > Main.MaxTilesX)
				{
					num8 = Main.MaxTilesX;
				}
				if (num9 < 0)
				{
					num9 = 0;
				}
				else if (num10 > Main.MaxTilesY)
				{
					num10 = Main.MaxTilesY;
				}
				for (int k = 0; k < 48; k++)
				{
					int num15 = Main.Rand.Next(num7, num8);
					int num16 = Main.Rand.Next(num9, num10);
					if ((Main.TileSet[num15, num16].IsActive != 0 && Main.TileSolid[Main.TileSet[num15, num16].Type]) || Main.WallHouse[Main.TileSet[num15, num16].WallType])
					{
						continue;
					}
					if (!flag4 && !flag6 && ((num16 < (int)((float)Main.WorldSurface * 0.35f) && (num15 < (int)((double)Main.MaxTilesX * 0.45) || num15 > (int)((double)Main.MaxTilesX * 0.55) || Main.InHardMode)) || (num16 < (int)((float)Main.WorldSurface * 0.45f) && Main.InHardMode && Main.Rand.Next(10) == 0)))
					{
						num = num15;
						num2 = num16;
						flag = true;
						flag2 = true;
					}
					if (!flag)
					{
						for (int l = num16; l < Main.MaxTilesY; l++)
						{
							if (Main.TileSet[num15, l].IsActive != 0 && Main.TileSolid[Main.TileSet[num15, l].Type])
							{
								if (num15 < num11 || num15 > num12 || l < num13 || l > num14)
								{
									num = num15;
									num2 = l;
									flag = true;
								}
								break;
							}
						}
					}
					if (!flag)
					{
						continue;
					}
					int num17 = num - 1;
					int num18 = num + 1;
					int num19 = num2 - 3;
					int num20 = num2;
					if (num17 < 0 || num18 > Main.MaxTilesX || num19 < 0 || num20 > Main.MaxTilesY)
					{
						flag = false;
					}
					else
					{
						for (int m = num17; m < num18; m++)
						{
							for (int n = num19; n < num20; n++)
							{
								if (Main.TileSet[m, n].IsActive != 0 && Main.TileSolid[Main.TileSet[m, n].Type])
								{
									flag = false;
									break;
								}
								if (Main.TileSet[m, n].Lava != 0)
								{
									flag = false;
									break;
								}
							}
							if (!flag)
							{
								break;
							}
						}
					}
					if (flag)
					{
						break;
					}
				}
			}
			if (flag)
			{
				Rectangle rectangle = new Rectangle(num * 16, num2 * 16, 16, 16);
				for (int num21 = 0; num21 < 8; num21++)
				{
					if (Main.PlayerSet[num21].Active != 0)
					{
						Rectangle value = new Rectangle(Main.PlayerSet[num21].XYWH.X + Main.PlayerSet[num21].XYWH.Width / 2 - 960 - 62, Main.PlayerSet[num21].XYWH.Y + Main.PlayerSet[num21].XYWH.Height / 2 - 540 - 34, 2044, 1148);
						if (rectangle.Intersects(value))
						{
							flag = false;
							break;
						}
					}
				}
			}
			if (flag)
			{
				if (Main.PlayerSet[j].ZoneDungeon && (!Main.TileDungeon[Main.TileSet[num, num2].Type] || Main.TileSet[num, num2 - 1].WallType == 0))
				{
					flag = false;
				}
				if (Main.TileSet[num, num2 - 1].Liquid > 0 && Main.TileSet[num, num2 - 2].Liquid > 0 && Main.TileSet[num, num2 - 1].Lava == 0)
				{
					flag5 = true;
				}
			}
			if (!flag)
			{
				continue;
			}
			flag = false;
			int type = Main.TileSet[num, num2].Type;
			int num22 = 196;
			int num23 = (num << 4) + 8;
			int num24 = num2 << 4;
			if (flag2)
			{
				if (Main.InHardMode && Main.Rand.Next(10) == 0 && !AnyNPCs(87, 159))
				{
					int type2 = ((Main.Rand.Next(2) == 0) ? 87 : 159);
					NewNPC(num23, num24, type2, 1);
				}
				else
				{
					NewNPC(num23, num24, 48);
				}
			}
			else if (flag4)
			{
				if (Main.InvasionType == 1)
				{
					if (Main.Rand.Next(9) == 0)
					{
						NewNPC(num23, num24, 29);
					}
					else if (Main.Rand.Next(5) == 0)
					{
						NewNPC(num23, num24, 26);
					}
					else if (Main.Rand.Next(3) == 0)
					{
						NewNPC(num23, num24, 111);
					}
					else if (Main.Rand.Next(3) == 0)
					{
						NewNPC(num23, num24, 27);
					}
					else
					{
						NewNPC(num23, num24, 28);
					}
				}
				else if (Main.InvasionType == 2)
				{
					if (Main.Rand.Next(7) == 0)
					{
						NewNPC(num23, num24, 145);
					}
					else if (Main.Rand.Next(3) == 0)
					{
						NewNPC(num23, num24, 143);
					}
					else
					{
						NewNPC(num23, num24, 144);
					}
				}
			}
			else if (flag5 && (num < 250 || num > Main.MaxTilesX - 250) && type == 53 && num2 < Main.RockLayer)
			{
				int type3;
				switch (Main.Rand.Next(16))
				{
				case 0:
					type3 = 65;
					break;
				case 1:
					type3 = 148;
					break;
				case 2:
				case 3:
				case 4:
				case 5:
				case 6:
					type3 = 67;
					break;
				default:
					type3 = 64;
					break;
				}
				NewNPC(num23, num24, type3);
			}
			else if (flag5 && ((num2 > Main.RockLayer && Main.Rand.Next(2) == 0) || type == 60))
			{
				if (Main.InHardMode && Main.Rand.Next(3) > 0)
				{
					NewNPC(num23, num24, 102);
				}
				else
				{
					NewNPC(num23, num24, 58);
				}
			}
			else if (flag5 && num2 > Main.WorldSurface && Main.Rand.Next(3) == 0)
			{
				if (Main.InHardMode)
				{
					NewNPC(num23, num24, 103);
				}
				else
				{
					NewNPC(num23, num24, 63);
				}
			}
			else if (flag5 && Main.Rand.Next(4) == 0)
			{
				if (Main.PlayerSet[j].ZoneEvil)
				{
					NewNPC(num23, num24, 57);
				}
				else
				{
					NewNPC(num23, num24, 55);
				}
			}
			else if (HasDownedGoblins && Main.Rand.Next(20) == 0 && !flag5 && num2 >= Main.RockLayer && num2 < Main.MaxTilesY - 210 && !HasSavedGoblin && !AnyNPCs(105))
			{
				NewNPC(num23, num24, 105);
			}
			else if (Main.InHardMode && Main.Rand.Next(20) == 0 && !flag5 && num2 >= Main.RockLayer && num2 < Main.MaxTilesY - 210 && !HasSavedWizard && !AnyNPCs(106))
			{
				NewNPC(num23, num24, 106);
			}
			else if (flag6)
			{
				if (flag5)
				{
					NewNPC(num23, num24, 55);
				}
				else
				{
					if (type != 2 && type != 109 && type != 147 && num2 <= Main.WorldSurface)
					{
						break;
					}
					if (Main.Rand.Next(2) == 0 && num2 <= Main.WorldSurface)
					{
						NewNPC(num23, num24, 74);
					}
					else
					{
						NewNPC(num23, num24, 46);
					}
				}
			}
			else if (Main.PlayerSet[j].ZoneDungeon)
			{
				if (!HasDownedBoss3)
				{
					num22 = NewNPC(num23, num24, 68);
				}
				else if (!HasSavedMech && !flag5 && Main.Rand.Next(5) == 0 && num2 > Main.RockLayer && !AnyNPCs(123))
				{
					NewNPC(num23, num24, 123);
				}
				else if (Main.Rand.Next(37) == 0)
				{
					num22 = NewNPC(num23, num24, 71);
				}
				else if (Main.Rand.Next(4) == 0 && !NearSpikeBall(num, num2))
				{
					num22 = NewNPC(num23, num24, 70);
				}
				else if (Main.Rand.Next(15) == 0)
				{
					num22 = NewNPC(num23, num24, 72);
				}
				else if (Main.Rand.Next(9) == 0)
				{
					num22 = NewNPC(num23, num24, (Main.Rand.Next(2) == 0) ? 34 : 158);
				}
				else if (Main.Rand.Next(7) == 0)
				{
					num22 = NewNPC(num23, num24, 32);
				}
				else
				{
					num22 = NewNPC(num23, num24, 31);
					if (Main.Rand.Next(4) == 0)
					{
						Main.NPCSet[num22].SetDefaults("Big Boned");
					}
					else if (Main.Rand.Next(5) == 0)
					{
						Main.NPCSet[num22].SetDefaults("Short Bones");
					}
				}
			}
			else if (Main.PlayerSet[j].ZoneMeteor)
			{
				num22 = NewNPC(num23, num24, 23);
			}
			else if (Main.PlayerSet[j].ZoneEvil && Main.Rand.Next(65) == 0)
			{
				num22 = ((!Main.InHardMode || Main.Rand.Next(4) == 0) ? NewNPC(num23, num24, 7, 1) : NewNPC(num23, num24, 98, 1));
			}
			else if (Main.InHardMode && num2 > Main.WorldSurface && Main.Rand.Next(75) == 0)
			{
				num22 = NewNPC(num23, num24, 85);
			}
			else if (Main.InHardMode && Main.TileSet[num, num2 - 1].WallType == 2 && Main.Rand.Next(20) == 0)
			{
				num22 = NewNPC(num23, num24, 85);
			}
			else if (Main.InHardMode && num2 <= Main.WorldSurface && !Main.GameTime.DayTime && (Main.Rand.Next(20) == 0 || (Main.Rand.Next(5) == 0 && Main.GameTime.MoonPhase == 4)))
			{
				num22 = NewNPC(num23, num24, 82);
			}
			else if (type == 60 && Main.Rand.Next(500) == 0 && !Main.GameTime.DayTime)
			{
				num22 = NewNPC(num23, num24, 52);
			}
			else if (type == 60 && num2 > Main.WorldSurface + Main.RockLayer >> 1)
			{
				if (Main.Rand.Next(3) == 0)
				{
					num22 = NewNPC(num23, num24, 43);
					Main.NPCSet[num22].AI0 = num;
					Main.NPCSet[num22].AI1 = num2;
					Main.NPCSet[num22].ShouldNetUpdate = true;
				}
				else if (Main.Rand.Next(2) == 0)
				{
					num22 = NewNPC(num23, num24, 42);
					switch (Main.Rand.Next(8))
					{
					case 0:
					case 1:
						Main.NPCSet[num22].SetDefaults("Little Stinger");
						break;
					case 2:
						Main.NPCSet[num22].SetDefaults("Big Stinger");
						break;
					}
				}
				else
				{
					num22 = NewNPC(num23, num24, 157);
				}
			}
			else if (type == 60 && Main.Rand.Next(4) == 0)
			{
				num22 = NewNPC(num23, num24, 51);
			}
			else if (type == 60 && Main.Rand.Next(8) == 0)
			{
				num22 = NewNPC(num23, num24, (Main.Rand.Next(2) == 0) ? 56 : 156);
				Main.NPCSet[num22].AI0 = num;
				Main.NPCSet[num22].AI1 = num2;
				Main.NPCSet[num22].ShouldNetUpdate = true;
			}
			else if (Main.InHardMode && type == 53 && Main.Rand.Next(3) == 0)
			{
				num22 = NewNPC(num23, num24, 78);
			}
			else if (Main.InHardMode && type == 112 && Main.Rand.Next(2) == 0)
			{
				num22 = NewNPC(num23, num24, (Main.Rand.Next(2) == 0) ? 79 : 152);
			}
			else if (Main.InHardMode && type == 116 && Main.Rand.Next(2) == 0)
			{
				num22 = NewNPC(num23, num24, (Main.Rand.Next(2) == 0) ? 80 : 155);
			}
			else if (Main.InHardMode && !flag5 && num2 < Main.RockLayer && (type == 116 || type == 117 || type == 109))
			{
				if (!Main.GameTime.DayTime && Main.Rand.Next(2) == 0)
				{
					num22 = NewNPC(num23, num24, (Main.Rand.Next(2) == 0) ? 122 : 153);
				}
				else
				{
					num22 = ((Main.Rand.Next(10) != 0) ? NewNPC(num23, num24, 75) : NewNPC(num23, num24, 86));
				}
			}
			else if (!flag3 && Main.InHardMode && Main.Rand.Next(50) == 0 && !flag5 && num2 >= Main.RockLayer && (type == 116 || type == 117 || type == 109))
			{
				num22 = NewNPC(num23, num24, 84);
			}
			else if ((type == 22 && Main.PlayerSet[j].ZoneEvil) || type == 23 || type == 25 || type == 112)
			{
				if (Main.InHardMode && num2 >= Main.RockLayer && Main.Rand.Next(3) == 0)
				{
					num22 = NewNPC(num23, num24, 101);
					Main.NPCSet[num22].AI0 = num;
					Main.NPCSet[num22].AI1 = num2;
					Main.NPCSet[num22].ShouldNetUpdate = true;
				}
				else if (Main.InHardMode && Main.Rand.Next(3) == 0)
				{
					int x = num23;
					int y = num24;
					int num25 = Main.Rand.Next(3);
					if (1 == 0)
					{
					}
					int type4 = num25 switch
					{
						0 => 150, 
						1 => 81, 
						_ => 121, 
					};
					if (1 == 0)
					{
					}
					num22 = NewNPC(x, y, type4);
				}
				else if (Main.InHardMode && num2 >= Main.RockLayer && Main.Rand.Next(40) == 0)
				{
					num22 = ((Main.Rand.Next(2) != 0) ? NewNPC(num23, num24, 151) : NewNPC(num23, num24, 83));
				}
				else if (Main.InHardMode && (Main.Rand.Next(2) == 0 || num2 > Main.RockLayer))
				{
					num22 = NewNPC(num23, num24, 94);
				}
				else
				{
					num22 = NewNPC(num23, num24, 6);
					if (Main.Rand.Next(3) == 0)
					{
						Main.NPCSet[num22].SetDefaults("Little Eater");
					}
					else if (Main.Rand.Next(3) == 0)
					{
						Main.NPCSet[num22].SetDefaults("Big Eater");
					}
				}
			}
			else if (num2 <= Main.WorldSurface)
			{
				if (Main.GameTime.DayTime)
				{
					int num26 = Math.Abs(num - Main.SpawnTileX);
					if (num26 < Main.MaxTilesX / 3 && Main.Rand.Next(15) == 0 && (type == 2 || type == 109 || type == 147))
					{
						NewNPC(num23, num24, 46);
					}
					else if (num26 < Main.MaxTilesX / 3 && Main.Rand.Next(15) == 0 && (type == 2 || type == 109 || type == 147))
					{
						NewNPC(num23, num24, 74);
					}
					else if (num26 > Main.MaxTilesX / 3 && type == 2 && Main.Rand.Next(300) == 0 && !AnyNPCs(50))
					{
						num22 = NewNPC(num23, num24, 50);
					}
					else if (type == 53 && Main.Rand.Next(5) == 0 && !flag5)
					{
						num22 = NewNPC(num23, num24, (Main.Rand.Next(2) == 0) ? 69 : 147);
					}
					else if (type == 53 && !flag5)
					{
						num22 = NewNPC(num23, num24, 61);
					}
					else if (num26 > Main.MaxTilesX / 3 && Main.Rand.Next(15) == 0)
					{
						num22 = NewNPC(num23, num24, 73);
					}
					else
					{
						num22 = NewNPC(num23, num24, 1);
						if (type == 60)
						{
							Main.NPCSet[num22].SetDefaults("Jungle Slime");
						}
						else if (Main.Rand.Next(3) == 0 || num26 < 200)
						{
							Main.NPCSet[num22].SetDefaults("Green Slime");
						}
						else if (Main.Rand.Next(10) == 0 && num26 > 400)
						{
							Main.NPCSet[num22].SetDefaults("Purple Slime");
						}
					}
				}
				else if (Main.Rand.Next(6) == 0 || (Main.GameTime.MoonPhase == 4 && Main.Rand.Next(2) == 0))
				{
					num22 = ((!Main.InHardMode || Main.Rand.Next(3) != 0) ? NewNPC(num23, num24, 2) : NewNPC(num23, num24, 133));
				}
				else if (Main.InHardMode && Main.Rand.Next(50) == 0 && Main.GameTime.IsBloodMoon && !AnyNPCs(109))
				{
					NewNPC(num23, num24, 109);
				}
				else if (Main.Rand.Next(250) == 0 && Main.GameTime.IsBloodMoon)
				{
					NewNPC(num23, num24, 53);
				}
				else if (Main.GameTime.MoonPhase == 0 && Main.InHardMode && Main.Rand.Next(3) != 0)
				{
					NewNPC(num23, num24, 104);
				}
				else if (Main.InHardMode && Main.Rand.Next(3) == 0)
				{
					NewNPC(num23, num24, 140);
				}
				else if (Main.Rand.Next(3) == 0)
				{
					NewNPC(num23, num24, 132);
				}
				else
				{
					NewNPC(num23, num24, 3);
				}
			}
			else if (num2 <= Main.RockLayer)
			{
				if (!flag3 && Main.Rand.Next(50) == 0)
				{
					num22 = ((!Main.InHardMode) ? NewNPC(num23, num24, 10, 1) : NewNPC(num23, num24, 95, 1));
				}
				else if (Main.InHardMode && Main.Rand.Next(3) == 0)
				{
					num22 = NewNPC(num23, num24, 140);
				}
				else if (Main.InHardMode && Main.Rand.Next(4) != 0)
				{
					num22 = NewNPC(num23, num24, 141);
				}
				else
				{
					num22 = NewNPC(num23, num24, 1);
					if (Main.Rand.Next(5) == 0)
					{
						Main.NPCSet[num22].SetDefaults("Yellow Slime");
					}
					else if (Main.Rand.Next(2) == 0)
					{
						Main.NPCSet[num22].SetDefaults("Red Slime");
					}
				}
			}
			else if (num2 > Main.MaxTilesY - 190)
			{
				int type5 = 60;
				int start = 0;
				if (Main.Rand.Next(40) == 0 && !AnyNPCs(39))
				{
					type5 = 39;
					start = 1;
				}
				else if (Main.Rand.Next(14) == 0)
				{
					type5 = 24;
				}
				else if (Main.Rand.Next(8) == 0)
				{
					switch (Main.Rand.Next(7))
					{
					case 0:
						type5 = 66;
						break;
					case 1:
					case 2:
					case 3:
						type5 = 62;
						break;
					default:
						type5 = 165;
						break;
					}
				}
				else if (Main.Rand.Next(3) == 0)
				{
					type5 = 59;
				}
				num22 = NewNPC(num23, num24, type5, start);
			}
			else if ((type == 116 || type == 117) && !flag3 && Main.Rand.Next(8) == 0)
			{
				num22 = NewNPC(num23, num24, (Main.Rand.Next(2) == 0) ? 120 : 154);
			}
			else if (!flag3 && Main.Rand.Next(75) == 0 && !Main.PlayerSet[j].zoneHoly)
			{
				num22 = NewNPC(num23, num24, Main.InHardMode ? 95 : 10, 1);
			}
			else if (!Main.InHardMode && Main.Rand.Next(10) == 0)
			{
				num22 = NewNPC(num23, num24, 16);
			}
			else if (!Main.InHardMode && Main.Rand.Next(4) == 0)
			{
				num22 = NewNPC(num23, num24, 1);
				if (Main.PlayerSet[j].ZoneJungle)
				{
					Main.NPCSet[num22].SetDefaults("Jungle Slime");
				}
				else
				{
					Main.NPCSet[num22].SetDefaults("Black Slime");
				}
			}
			else if (Main.Rand.Next(2) != 0)
			{
				if (Main.InHardMode && (Main.PlayerSet[j].zoneHoly & (Main.Rand.Next(2) == 0)))
				{
					num22 = NewNPC(num23, num24, 138);
				}
				else if (Main.PlayerSet[j].ZoneJungle)
				{
					num22 = NewNPC(num23, num24, 51);
				}
				else if (Main.InHardMode && Main.PlayerSet[j].zoneHoly)
				{
					num22 = NewNPC(num23, num24, 137);
				}
				else
				{
					num22 = ((!Main.InHardMode || Main.Rand.Next(6) <= 0) ? NewNPC(num23, num24, 49) : NewNPC(num23, num24, 93));
				}
			}
			else if (num2 > Main.RockLayer + Main.MaxTilesY >> 1 && Main.Rand.Next(700) == 0)
			{
				num22 = NewNPC(num23, num24, 45);
			}
			else if (Main.InHardMode && Main.Rand.Next(10) != 0)
			{
				if (Main.Rand.Next(2) == 0)
				{
					num22 = NewNPC(num23, num24, 77);
					if (num2 > Main.RockLayer + Main.MaxTilesY >> 1 && Main.Rand.Next(5) == 0)
					{
						Main.NPCSet[num22].SetDefaults("Heavy Skeleton");
					}
				}
				else
				{
					num22 = NewNPC(num23, num24, 110);
				}
			}
			else if (Main.Rand.Next(15) == 0)
			{
				int type6 = ((Main.Rand.Next(2) == 0) ? 44 : 149);
				num22 = NewNPC(num23, num24, type6);
			}
			else
			{
				num22 = NewNPC(num23, num24, 21);
			}
			if (num22 < 196)
			{
				if (Main.NPCSet[num22].Type == 1 && Main.Rand.Next(250) == 0)
				{
					Main.NPCSet[num22].SetDefaults("Pinky");
				}
				NetMessage.CreateMessage1(23, num22);
				NetMessage.SendMessage();
			}
			break;
		}
	}

	public static bool SpawnWOF(ref Vector2 pos, bool force = false)
	{
		if ((!force && (int)pos.Y >> 4 < Main.MaxTilesY - 205) || WoF >= 0 || Main.NetMode == 1)
		{
			return false;
		}
		int num = -16;
		int num2 = (int)pos.X;
		if (num2 >> 4 > Main.MaxTilesX >> 1)
		{
			num = 16;
		}
		int num3 = num2 + 1200;
		bool flag;
		do
		{
			flag = true;
			for (int i = 0; i < 8; i++)
			{
				if (Main.PlayerSet[i].Active != 0 && Main.PlayerSet[i].XYWH.X > -2400 + num3 && Main.PlayerSet[i].XYWH.X < num3)
				{
					num2 += num;
					num3 += num;
					flag = false;
					break;
				}
			}
			if ((num < 0 && num2 >> 4 < 42) || (num > 0 && num2 >> 4 > Main.MaxTilesX - 34))
			{
				flag = true;
			}
		}
		while (!flag);
		int num4 = (int)pos.Y;
		int num5 = num2 >> 4;
		int num6 = num4 >> 4;
		int num7 = 0;
		try
		{
			while (true)
			{
				if (!WorldGen.SolidTile(num5, num6 - num7) && Main.TileSet[num5, num6 - num7].Liquid < 100)
				{
					num6 -= num7;
					break;
				}
				if (!WorldGen.SolidTile(num5, num6 + num7) && Main.TileSet[num5, num6 + num7].Liquid < 100)
				{
					num6 += num7;
					break;
				}
				num7++;
			}
		}
		catch
		{
		}
		num4 = num6 << 4;
		int num8 = NewNPC(num2, num4, 113);
		Main.NPCSet[num8].Direction = 1;
		if (num > -1)
		{
			Main.NPCSet[num8].Direction = -1;
		}
		if (Main.NPCSet[num8].DisplayName.Length == 0)
		{
			Main.NPCSet[num8].DisplayName = Main.NPCSet[num8].TypeName;
		}
		NetMessage.SendText(Main.NPCSet[num8].DisplayName, 16, 175, 75, 255, -1);
		return true;
	}

	public static void SpawnOnPlayer(Player p, int Type)
	{
		if (Main.NetMode == 1)
		{
			return;
		}
		bool flag = false;
		int num = 0;
		int num2 = 0;
		int num3 = (p.XYWH.X >> 4) - 168;
		int num4 = (p.XYWH.X >> 4) + 168;
		int num5 = (p.XYWH.Y >> 4) - 92;
		int num6 = (p.XYWH.Y >> 4) + 92;
		int num7 = (p.XYWH.X >> 4) - 62;
		int num8 = (p.XYWH.X >> 4) + 62;
		int num9 = (p.XYWH.Y >> 4) - 34;
		int num10 = (p.XYWH.Y >> 4) + 34;
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
		for (int i = 0; i < 1000; i++)
		{
			for (int j = 0; j < 100; j++)
			{
				int num11 = Main.Rand.Next(num3, num4);
				int num12 = Main.Rand.Next(num5, num6);
				if (Main.TileSet[num11, num12].IsActive == 0 || !Main.TileSolid[Main.TileSet[num11, num12].Type])
				{
					if (Main.WallHouse[Main.TileSet[num11, num12].WallType] && i < 999)
					{
						continue;
					}
					for (int k = num12; k < Main.MaxTilesY; k++)
					{
						if (Main.TileSet[num11, k].IsActive != 0 && Main.TileSolid[Main.TileSet[num11, k].Type])
						{
							if (num11 < num7 || num11 > num8 || k < num9 || k > num10 || i == 999)
							{
								num = num11;
								num2 = k;
								flag = true;
							}
							break;
						}
					}
					if (flag && i < 999)
					{
						int num13 = num - 1;
						int num14 = num + 1;
						int num15 = num2 - 3;
						int num16 = num2;
						if (num13 < 0)
						{
							flag = false;
						}
						if (num14 > Main.MaxTilesX)
						{
							flag = false;
						}
						if (num15 < 0)
						{
							flag = false;
						}
						if (num16 > Main.MaxTilesY)
						{
							flag = false;
						}
						if (flag)
						{
							for (int l = num13; l < num14; l++)
							{
								for (int m = num15; m < num16; m++)
								{
									if (Main.TileSet[l, m].IsActive != 0 && Main.TileSolid[Main.TileSet[l, m].Type])
									{
										flag = false;
										break;
									}
								}
							}
						}
					}
				}
				if (flag | flag)
				{
					break;
				}
			}
			if (flag && i < 999)
			{
				Rectangle rectangle = new Rectangle(num * 16, num2 * 16, 16, 16);
				for (int n = 0; n < 8; n++)
				{
					if (Main.PlayerSet[n].Active != 0)
					{
						Rectangle value = new Rectangle(Main.PlayerSet[n].XYWH.X + Main.PlayerSet[n].XYWH.Width / 2 - 960 - 62, Main.PlayerSet[n].XYWH.Y + Main.PlayerSet[n].XYWH.Height / 2 - 540 - 34, 2044, 1148);
						if (rectangle.Intersects(value))
						{
							flag = false;
						}
					}
				}
			}
			if (flag)
			{
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		int num17 = 196;
		num17 = NewNPC(num * 16 + 8, num2 * 16, Type, 1);
		if (num17 != 196)
		{
			Main.NPCSet[num17].Target = p.WhoAmI;
			Main.NPCSet[num17].TimeLeft *= 20;
			string text = Main.NPCSet[num17].DisplayName;
			if (text.Length == 0)
			{
				text = Main.NPCSet[num17].TypeName;
			}
			NetMessage.CreateMessage1(23, num17);
			NetMessage.SendMessage();
			switch (Type)
			{
			case 125:
				NetMessage.SendText(34, 175, 75, 255, -1);
				break;
			default:
				NetMessage.SendText(text, 16, 175, 75, 255, -1);
				break;
			case 50:
			case 82:
			case 126:
				break;
			}
		}
	}

	public static int NewNPC(int X, int Y, int Type, int Start = 0)
	{
		int num = 196;
		for (int i = Start; i < 196; i++)
		{
			if (Main.NPCSet[i].Active == 0)
			{
				num = i;
				break;
			}
		}
		if (num < 196)
		{
			Main.NPCSet[num].SetDefaults(Type);
			Main.NPCSet[num].Position.X = (Main.NPCSet[num].XYWH.X = X - (Main.NPCSet[num].Width >> 1));
			Main.NPCSet[num].Position.Y = (Main.NPCSet[num].XYWH.Y = Y - Main.NPCSet[num].Height);
			Main.NPCSet[num].Active = 1;
			Main.NPCSet[num].TimeLeft = 750;
			Main.NPCSet[num].IsWet = Collision.WetCollision(ref Main.NPCSet[num].Position, Main.NPCSet[num].Width, Main.NPCSet[num].Height);
			if (Type == 50)
			{
				NetMessage.SendText(Main.NPCSet[num].TypeName, 16, 175, 75, 255, -1);
			}
		}
		return num;
	}

	public void Transform(int newType)
	{
		Vector2 velocity = Velocity;
		Position.Y += (int)Height;
		sbyte spriteDirection = SpriteDirection;
		SetDefaults(newType);
		SpriteDirection = spriteDirection;
		TargetClosest();
		Velocity = velocity;
		Position.Y -= (int)Height;
		if (newType == 107 || newType == 108)
		{
			HomeTileX = (short)((int)Position.X + (Width >> 1) >> 4);
			HomeTileY = (short)((int)Position.Y + Height >> 4);
			IsHomeless = true;
		}
		ShouldNetUpdate = true;
		NetMessage.CreateMessage1(23, WhoAmI);
		NetMessage.SendMessage();
	}

	public double StrikeNPC(int Damage, float knockBack, int hitDirection, bool crit = false, bool noEffect = false)
	{
		if (Active == 0 || Life <= 0)
		{
			return 0.0;
		}
		double num = Main.CalculateDamage(Damage, Defense);
		if (crit)
		{
			num *= 2.0;
		}
		if (Damage != 9999 && LifeMax > 1)
		{
			if (IsFriendly)
			{
				CombatText.NewText(Position, Width, Height, (int)num, crit, 0);
			}
			else
			{
				CombatText.NewText(Position, Width, Height, (int)num, crit, 0);
			}
			if (DrawMyName < 96)
			{
				DrawMyName = 96;
			}
		}
		if (num >= 1.0)
		{
			WasJustHit = true;
			if (IsTownNPC)
			{
				AI0 = 1f;
				AI1 = 300 + Main.Rand.Next(300);
				AI2 = 0f;
				Direction = (sbyte)hitDirection;
				ShouldNetUpdate = true;
			}
			if (AIStyle == 8 && Main.NetMode != 1)
			{
				AI0 = 400f;
				TargetClosest();
			}
			if (RealLife >= 0)
			{
				Main.NPCSet[RealLife].Life -= (int)num;
				Life = Main.NPCSet[RealLife].Life;
				LifeMax = Main.NPCSet[RealLife].LifeMax;
			}
			else
			{
				Life -= (int)num;
			}
			if (knockBack > 0f && KnockBackResist > 0f)
			{
				float num2 = knockBack * KnockBackResist;
				if (num2 > 8f)
				{
					num2 = 8f;
				}
				if (crit)
				{
					num2 *= 1.4f;
				}
				if (num * 10.0 < (double)LifeMax)
				{
					if (hitDirection < 0 && Velocity.X > 0f - num2)
					{
						if (Velocity.X > 0f)
						{
							Velocity.X -= num2;
						}
						Velocity.X -= num2;
						if (Velocity.X < 0f - num2)
						{
							Velocity.X = 0f - num2;
						}
					}
					else if (hitDirection > 0 && Velocity.X < num2)
					{
						if (Velocity.X < 0f)
						{
							Velocity.X += num2;
						}
						Velocity.X += num2;
						if (Velocity.X > num2)
						{
							Velocity.X = num2;
						}
					}
					num2 = (HasNoGravity ? (num2 * -0.5f) : (num2 * -0.75f));
					if (Velocity.Y > num2)
					{
						Velocity.Y += num2;
						if (Velocity.Y < num2)
						{
							Velocity.Y = num2;
						}
					}
				}
				else
				{
					if (!HasNoGravity)
					{
						Velocity.Y = (0f - num2) * 0.75f * KnockBackResist;
					}
					else
					{
						Velocity.Y = (0f - num2) * 0.5f * KnockBackResist;
					}
					Velocity.X = num2 * (float)hitDirection * KnockBackResist;
				}
			}
			if ((Type == 113 || Type == 114) && Life <= 0)
			{
				for (int i = 0; i < 196; i++)
				{
					if (Main.NPCSet[i].Active != 0 && (Main.NPCSet[i].Type == 113 || Main.NPCSet[i].Type == 114))
					{
						Main.NPCSet[i].HitEffect(hitDirection, num);
					}
				}
			}
			else if (Active != 0)
			{
				HitEffect(hitDirection, num);
			}
			if (SoundHit > 0)
			{
				Main.PlaySound(3, (int)Position.X, (int)Position.Y, SoundHit);
			}
			if (RealLife >= 0)
			{
				Main.NPCSet[RealLife].CheckDead();
			}
			else
			{
				CheckDead();
			}
			Terraria.Mods.ModEvents.NpcHurt(this, Damage);
			return num;
		}
		return 0.0;
	}

	public void CheckDead()
	{
		if (Active == 0 || (RealLife >= 0 && RealLife != WhoAmI) || Life > 0)
		{
			return;
		}
		NoSpawnCycle = true;
		if (IsTownNPC && Type != 37 && Main.NetMode != 1)
		{
			string prefix = DisplayName;
			if (DisplayName.Length == 0)
			{
				prefix = TypeName;
			}
			NetMessage.SendText(prefix, 19, 255, 25, 25, -1);
			TypeNames[Type] = null;
			SetNames();
			NetMessage.CreateMessage1(56, Type);
			NetMessage.SendMessage();
		}
		if (IsTownNPC && Main.NetMode != 1 && IsHomeless && WorldGen.ToSpawnNPC == Type)
		{
			WorldGen.ToSpawnNPC = 0;
		}
		if (SoundKilled > 0)
		{
			Main.PlaySound(4, (int)Position.X, (int)Position.Y, SoundKilled);
		}
		NPCLoot();
		if ((Type >= 26 && Type <= 29) || Type == 111 || (Type >= 143 && Type <= 145))
		{
			Main.InvasionSize--;
		}
		Active = 0;
	}

	public unsafe void NPCLoot()
	{
		if (Main.InHardMode && LifeMax > 1 && Damage > 0 && !IsFriendly && XYWH.Y > Main.RockLayerPixels && Type != 121 && Value > 0f && Main.Rand.Next(7) == 0)
		{
			Player player = Player.FindClosest(ref XYWH);
			if (player.ZoneEvil)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 521);
			}
			if (player.zoneHoly)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 520);
			}
		}
		if (Time.xMas && LifeMax > 1 && Damage > 0 && !IsFriendly && Type != 121 && Value > 0f && Main.Rand.Next(13) == 0)
		{
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, Main.Rand.Next(599, 602));
		}
		switch ((EntityID.NPCID)Type)
		{
		case EntityID.NPCID.CLOWN:
			if (!HasDownedClown)
			{
				HasDownedClown = true;
				if (Main.NetMode == 2)
				{
					NetMessage.CreateMessage0(7);
					NetMessage.SendMessage();
				}
			}
			break;
		case EntityID.NPCID.MIMIC:
			if (Value > 0f)
			{
				switch (Main.Rand.Next(7))
				{
				case 0:
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 437, 1, DoNotBroadcast: false, -1);
					break;
				case 1:
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 517, 1, DoNotBroadcast: false, -1);
					break;
				case 2:
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 535, 1, DoNotBroadcast: false, -1);
					break;
				case 3:
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 536, 1, DoNotBroadcast: false, -1);
					break;
				case 4:
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 532, 1, DoNotBroadcast: false, -1);
					break;
				case 5:
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 393, 1, DoNotBroadcast: false, -1);
					break;
				default:
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 554, 1, DoNotBroadcast: false, -1);
					break;
				}
			}
			break;
		case EntityID.NPCID.WYVERN_HEAD:
		case EntityID.NPCID.ARCH_WYVERN_HEAD:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 575, Main.Rand.Next(5, 11));
			break;
		case EntityID.NPCID.SNOWMAN_GANGSTA:
		case EntityID.NPCID.MISTER_STABBY:
		case EntityID.NPCID.SNOW_BALLA:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 593, Main.Rand.Next(5, 11));
			break;
		case EntityID.NPCID.DARK_MUMMY:
			if (Main.Rand.Next(10) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 527);
			}
			break;
		case EntityID.NPCID.LIGHT_MUMMY:
			if (Main.Rand.Next(10) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 528);
			}
			break;
		case EntityID.NPCID.SEEKER_HEAD:
		case EntityID.NPCID.CLINGER:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 522, Main.Rand.Next(2, 6));
			break;
		case EntityID.NPCID.UNICORN:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 526);
			break;
		case EntityID.NPCID.WALL_OF_FLESH:
		{
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 367, 1, DoNotBroadcast: false, -1);
			if (Main.Rand.Next(2) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, Main.Rand.Next(489, 492), 1, DoNotBroadcast: false, -1);
			}
			else
			{
				int num3 = Main.Rand.Next(3);
				int x = XYWH.X;
				int y = XYWH.Y;
				int width = Width;
				int height = Height;
				if (1 == 0)
				{
				}
				int type = num3 switch
				{
					0 => 514, 
					1 => 426, 
					_ => 434, 
				};
				if (1 == 0)
				{
				}
				Item.NewItem(x, y, width, height, type, 1, DoNotBroadcast: false, -1);
			}
			if (Main.NetMode == 1)
			{
				break;
			}
			int num4 = XYWH.X + (Width >> 1) >> 4;
			int num5 = XYWH.Y + (Height >> 1) >> 4;
			int num6 = (Width >> 5) + 1;
			for (int i = num4 - num6; i <= num4 + num6; i++)
			{
				for (int j = num5 - num6; j <= num5 + num6; j++)
				{
					bool flag = false;
					fixed (Tile* ptr = &Main.TileSet[i, j])
					{
						if ((i == num4 - num6 || i == num4 + num6 || j == num5 - num6 || j == num5 + num6) && ptr->IsActive == 0)
						{
							ptr->IsActive = 1;
							ptr->Type = 140;
							WorldGen.SquareTileFrame(i, j);
							flag = true;
						}
						if (ptr->Liquid > 0)
						{
							ptr->Lava = 0;
							ptr->Liquid = 0;
							flag = true;
						}
					}
					if (flag)
					{
						NetMessage.SendTile(i, j);
					}
				}
			}
			break;
		}
		case EntityID.NPCID.SLIME:
		case EntityID.NPCID.MOTHER_SLIME:
		case EntityID.NPCID.ILLUMINANT_SLIME:
		case EntityID.NPCID.TOXIC_SLUDGE:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 23, Main.Rand.Next(1, 3));
			break;
		case EntityID.NPCID.CORRUPT_SLIME:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 23, Main.Rand.Next(2, 5));
			break;
		case EntityID.NPCID.GASTROPOD:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 23, Main.Rand.Next(5, 11));
			break;
		case EntityID.NPCID.DUNGEON_SLIME:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 327);
			break;
		case EntityID.NPCID.PIXIE:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 501, Main.Rand.Next(1, 4));
			break;
		case EntityID.NPCID.DEMON_EYE:
		{
			int num7 = Main.Rand.Next(150);
			if (num7 < 50)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, (num7 == 38) ? 236 : 38);
			}
			break;
		}
		case EntityID.NPCID.WEREWOLF:
			if (Main.Rand.Next(60) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 485, 1, DoNotBroadcast: false, -1);
			}
			break;
		case EntityID.NPCID.PIRANHA:
		{
			int num2 = Main.Rand.Next(500);
			if (num2 < 13)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, (num2 == 0) ? 263 : 118);
			}
			break;
		}
		case EntityID.NPCID.ANGLER_FISH:
			if (Main.Rand.Next(500) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 263);
			}
			break;
		case EntityID.NPCID.ZOMBIE:
		case EntityID.NPCID.BALD_ZOMBIE:
			if (Main.Rand.Next(50) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 216, 1, DoNotBroadcast: false, -1);
			}
			break;
		case EntityID.NPCID.DEMON:
			if (Main.Rand.Next(50) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 272, 1, DoNotBroadcast: false, -1);
			}
			break;
		case EntityID.NPCID.VOODOO_DEMON:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 267);
			break;
		case EntityID.NPCID.DOCTOR_BONES:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 251);
			break;
		case EntityID.NPCID.THE_GROOM:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 239);
			break;
		case EntityID.NPCID.CLOTHIER:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 260);
			break;
		case EntityID.NPCID.GOLDFISH:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 261);
			break;
		case EntityID.NPCID.ANTLION:
		case EntityID.NPCID.ALBINO_ANTLION:
			if (Main.Rand.Next(7) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 323);
			}
			break;
		case EntityID.NPCID.GOBLIN_SCOUT:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 362, Main.Rand.Next(1, 3));
			break;
		case EntityID.NPCID.EYE_OF_CTHULHU:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 47, Main.Rand.Next(20, 50));
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 56, Main.Rand.Next(10, 30));
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 56, Main.Rand.Next(10, 30));
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 56, Main.Rand.Next(10, 30));
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 59, Main.Rand.Next(1, 4));
			break;
		case EntityID.NPCID.OCRAM:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 366, Main.Rand.Next(10, 30));
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 620, Main.Rand.Next(5, 10));
			if (Main.Rand.Next(3) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 604 + Main.Rand.Next(9));
			}
			break;
		case EntityID.NPCID.EATER_OF_SOULS:
		case EntityID.NPCID.CORRUPTOR:
			if (Main.Rand.Next(3) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 68);
			}
			break;
		case EntityID.NPCID.DEVOURER_HEAD:
		case EntityID.NPCID.DEVOURER_BODY:
		case EntityID.NPCID.DEVOURER_TAIL:
			if (Main.Rand.Next(3) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 68, Main.Rand.Next(1, 3));
			}
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 69, Main.Rand.Next(3, 9));
			break;
		case EntityID.NPCID.GIANT_WORM_HEAD:
		case EntityID.NPCID.GIANT_WORM_BODY:
		case EntityID.NPCID.GIANT_WORM_TAIL:
		case EntityID.NPCID.DIGGER_HEAD:
		case EntityID.NPCID.DIGGER_BODY:
		case EntityID.NPCID.DIGGER_TAIL:
			if (Main.Rand.Next(500) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 215);
			}
			break;
		case EntityID.NPCID.CORRUPT_BUNNY:
			if (Main.Rand.Next(75) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 243);
			}
			break;
		case EntityID.NPCID.EATER_OF_WORLDS_HEAD:
		case EntityID.NPCID.EATER_OF_WORLDS_BODY:
		case EntityID.NPCID.EATER_OF_WORLDS_TAIL:
			if (Main.Rand.Next(2) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 86, Main.Rand.Next(1, 3));
			}
			if (Main.Rand.Next(2) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 56, Main.Rand.Next(2, 6));
			}
			if (IsBoss)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 56, Main.Rand.Next(10, 30));
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 56, Main.Rand.Next(10, 31));
			}
			if (Main.Rand.Next(3) == 0 && Player.FindClosest(ref XYWH).canHeal())
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 58);
			}
			break;
		case EntityID.NPCID.THE_HUNGRY_II:
		case EntityID.NPCID.LEECH_HEAD:
		case EntityID.NPCID.LEECH_BODY:
		case EntityID.NPCID.LEECH_TAIL:
		case EntityID.NPCID.PROBE:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 58);
			break;
		case EntityID.NPCID.BLUE_JELLYFISH:
		case EntityID.NPCID.PINK_JELLYFISH:
		case EntityID.NPCID.GREEN_JELLYFISH:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 282, Main.Rand.Next(1, 5));
			break;
		case EntityID.NPCID.SKELETON:
		case EntityID.NPCID.UNDEAD_MINER:
		case EntityID.NPCID.VAMPIRE_MINER:
			if (Main.Rand.Next(25) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 118);
			}
			else if (Type != 21)
			{
				if (Main.Rand.Next(20) == 0)
				{
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, Main.Rand.Next(410, 412));
				}
				else
				{
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 166, Main.Rand.Next(1, 4));
				}
			}
			break;
		case EntityID.NPCID.TIM:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 238);
			break;
		case EntityID.NPCID.KING_SLIME:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, Main.Rand.Next(256, 259));
			break;
		case EntityID.NPCID.METEOR_HEAD:
			if (Main.Rand.Next(50) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 116);
			}
			break;
		case EntityID.NPCID.FIRE_IMP:
			if (Main.Rand.Next(300) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 244);
			}
			break;
		case EntityID.NPCID.BONES:
		case EntityID.NPCID.DARK_CASTER:
		case EntityID.NPCID.CURSED_SKULL:
			if (Main.Rand.Next(65) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 327);
			}
			else
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 154, Main.Rand.Next(1, 4));
			}
			break;
		case EntityID.NPCID.GOBLIN_PEON:
		case EntityID.NPCID.GOBLIN_THIEF:
		case EntityID.NPCID.GOBLIN_WARRIOR:
		case EntityID.NPCID.GOBLIN_SORCERER:
		case EntityID.NPCID.GOBLIN_ARCHER:
		{
			int num = Main.Rand.Next(200);
			if (num < 100)
			{
				if (num == 0)
				{
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 160);
				}
				else
				{
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 161, Main.Rand.Next(1, 6));
				}
			}
			break;
		}
		case EntityID.NPCID.HORNET:
			if (Main.Rand.Next(2) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 209);
			}
			break;
		case EntityID.NPCID.MAN_EATER:
			if (Main.Rand.Next(4) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 210);
			}
			break;
		case EntityID.NPCID.SHARK:
			if (Main.Rand.Next(50) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 268);
			}
			else
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 319);
			}
			break;
		case EntityID.NPCID.ORKA:
			if (Main.Rand.Next(25) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 268);
			}
			break;
		case EntityID.NPCID.HARPY:
			if (Main.Rand.Next(2) == 0)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 320);
			}
			break;
		case EntityID.NPCID.RETINAZER:
		case EntityID.NPCID.SPAZMATISM:
			if (!AnyNPCs((Type == 125) ? 126 : 125))
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 549, Main.Rand.Next(20, 31));
				break;
			}
			Value = 0f;
			IsBoss = false;
			break;
		case EntityID.NPCID.SKELETRON_PRIME:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 547, Main.Rand.Next(20, 31));
			break;
		case EntityID.NPCID.THE_DESTROYER_HEAD:
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 548, Main.Rand.Next(20, 31));
			break;
		}
		if (IsBoss)
		{
			string prefix = DisplayName;
			switch ((EntityID.NPCID)Type)
			{
			case EntityID.NPCID.EYE_OF_CTHULHU:
				HasDownedBoss1 = true;
				break;
			case EntityID.NPCID.EATER_OF_WORLDS_HEAD:
			case EntityID.NPCID.EATER_OF_WORLDS_BODY:
			case EntityID.NPCID.EATER_OF_WORLDS_TAIL:
				HasDownedBoss2 = true;
				break;
			case EntityID.NPCID.SKELETRON_HEAD:
				HasDownedBoss3 = true;
				break;
			case EntityID.NPCID.RETINAZER:
			case EntityID.NPCID.SPAZMATISM:
				prefix = Lang.MiscText[20];
				UI.SetTriggerStateForAll(Trigger.KilledTheTwins);
				break;
			case EntityID.NPCID.SKELETRON_PRIME:
				UI.SetTriggerStateForAll(Trigger.KilledSkeletronPrime);
				break;
			case EntityID.NPCID.THE_DESTROYER_HEAD:
				UI.SetTriggerStateForAll(Trigger.KilledDestroyer);
				break;
			}
			short netID = NetID;
			if (RealLife > 0)
			{
				netID = Main.NPCSet[RealLife].NetID;
			}
			UI.IncreaseStatisticForAll(Statistics.GetBossStatisticEntryFromNetID(netID));
			int type2 = 28;
			if (Type == 113)
			{
				type2 = 188;
			}
			else if (Type > 113)
			{
				type2 = 499;
			}
			Item.NewItem(XYWH.X, XYWH.Y, Width, Height, type2, Main.Rand.Next(5, 16));
			for (int num8 = Main.Rand.Next(5, 10); num8 > 0; num8--)
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 58);
			}
			if (Main.NetMode != 1)
			{
				if (Type == 113)
				{
					WorldGen.StartHardmode();
				}
				NetMessage.SendText(prefix, 17, 175, 75, 255, -1);
				NetMessage.CreateMessage0(7);
				NetMessage.SendMessage();
			}
		}
		if (LifeMax > 1 && Damage > 0)
		{
			Player player2 = Player.FindClosest(ref XYWH);
			if (Main.Rand.Next(6) == 0)
			{
				if (Main.Rand.Next(2) == 0 && player2.canUseMana())
				{
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 184);
				}
				else if (Main.Rand.Next(2) == 0 && player2.canHeal())
				{
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 58);
				}
			}
			if (Main.Rand.Next(2) == 0 && player2.canUseMana())
			{
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 184);
			}
		}
		float value = Value;
		value *= 1f + (float)Main.Rand.Next(-20, 21) * 0.01f;
		if (Main.Rand.Next(5) == 0)
		{
			value *= 1f + (float)Main.Rand.Next(5, 11) * 0.01f;
		}
		if (Main.Rand.Next(10) == 0)
		{
			value *= 1f + (float)Main.Rand.Next(10, 21) * 0.01f;
		}
		if (Main.Rand.Next(15) == 0)
		{
			value *= 1f + (float)Main.Rand.Next(15, 31) * 0.01f;
		}
		if (Main.Rand.Next(20) == 0)
		{
			value *= 1f + (float)Main.Rand.Next(20, 41) * 0.01f;
		}
		int num9 = (int)value;
		while (num9 > 0)
		{
			if (num9 > 1000000)
			{
				int num10 = num9 / 1000000;
				if (num10 > 50 && Main.Rand.Next(5) == 0)
				{
					num10 /= Main.Rand.Next(1, 4);
				}
				if (Main.Rand.Next(5) == 0)
				{
					num10 /= Main.Rand.Next(1, 4);
				}
				if (num10 > 0)
				{
					num9 -= 1000000 * num10;
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 74, num10);
				}
			}
			else if (num9 > 10000)
			{
				int num11 = num9 / 10000;
				if (num11 > 50 && Main.Rand.Next(5) == 0)
				{
					num11 /= Main.Rand.Next(1, 4);
				}
				if (Main.Rand.Next(5) == 0)
				{
					num11 /= Main.Rand.Next(1, 4);
				}
				if (num11 > 0)
				{
					num9 -= 10000 * num11;
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 73, num11);
				}
			}
			else if (num9 > 100)
			{
				int num12 = num9 / 100;
				if (num12 > 50 && Main.Rand.Next(5) == 0)
				{
					num12 /= Main.Rand.Next(1, 4);
				}
				if (Main.Rand.Next(5) == 0)
				{
					num12 /= Main.Rand.Next(1, 4);
				}
				if (num12 > 0)
				{
					num9 -= 100 * num12;
					Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 72, num12);
				}
			}
			else if (num9 > 0)
			{
				int num13 = num9;
				if (num13 > 50 && Main.Rand.Next(5) == 0)
				{
					num13 /= Main.Rand.Next(1, 4);
				}
				if (Main.Rand.Next(5) == 0)
				{
					num13 /= Main.Rand.Next(1, 4);
				}
				if (num13 < 1)
				{
					num13 = 1;
				}
				num9 -= num13;
				Item.NewItem(XYWH.X, XYWH.Y, Width, Height, 71, num13);
			}
		}
	}

	public unsafe void HitEffect(int hitDirection = 0, double dmg = 10.0)
	{
		switch ((EntityID.NPCID)Type)
		{
		case EntityID.NPCID.SLIME:
		case EntityID.NPCID.MOTHER_SLIME:
		case EntityID.NPCID.DUNGEON_SLIME:
		{
			if (Life > 0)
			{
				for (int num53 = (int)(dmg / (double)LifeMax * 80.0); num53 > 0; num53--)
				{
					if (null == Main.DustSet.NewDust(4, ref XYWH, hitDirection, -1.0, Alpha, Colour))
					{
						break;
					}
				}
				return;
			}
			for (int num54 = 0; num54 < 48; num54++)
			{
				if (null == Main.DustSet.NewDust(4, ref XYWH, 2 * hitDirection, -2.0, Alpha, Colour))
				{
					break;
				}
			}
			if (Type != 16 || Main.NetMode == 1)
			{
				return;
			}
			for (int num55 = Main.Rand.Next(1, 3); num55 >= 0; num55--)
			{
				int num56 = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + Height, 1);
				if (num56 < 196)
				{
					Main.NPCSet[num56].SetDefaults("Baby Slime");
					Main.NPCSet[num56].Velocity.X = Velocity.X * 2f;
					Main.NPCSet[num56].Velocity.Y = Velocity.Y;
					Main.NPCSet[num56].Velocity.X += (float)Main.Rand.Next(-20, 20) * 0.1f + (float)(num55 * Direction) * 0.3f;
					Main.NPCSet[num56].Velocity.Y -= (float)Main.Rand.Next(10) * 0.1f + (float)num55;
					Main.NPCSet[num56].AI1 = num55;
					NetMessage.CreateMessage1(23, num56);
					NetMessage.SendMessage();
				}
			}
			return;
		}
		case EntityID.NPCID.SNOWMAN_GANGSTA:
		case EntityID.NPCID.MISTER_STABBY:
		case EntityID.NPCID.SNOW_BALLA:
		{
			if (Life > 0)
			{
				for (int num112 = (int)(dmg / (double)LifeMax * 80.0); num112 > 0; num112--)
				{
					Dust* ptr20 = Main.DustSet.NewDust(76, ref XYWH, hitDirection, -1.0);
					if (ptr20 != null)
					{
						ptr20->NoGravity = true;
						continue;
					}
					break;
				}
				return;
			}
			for (int num113 = 0; num113 < 32; num113++)
			{
				Dust* ptr21 = Main.DustSet.NewDust(76, ref XYWH, hitDirection, -1.0);
				if (ptr21 != null)
				{
					ptr21->NoGravity = true;
					ptr21->Scale *= 1.2f;
					continue;
				}
				break;
			}
			return;
		}
		case EntityID.NPCID.TOXIC_SLUDGE:
		{
			if (Life > 0)
			{
				for (int num8 = (int)(dmg / (double)LifeMax * 80.0); num8 > 0; num8--)
				{
					if (null == Main.DustSet.NewDust(4, ref XYWH, hitDirection, -1.0, Alpha, new Color(210, 230, 140)))
					{
						break;
					}
				}
				return;
			}
			for (int num9 = 0; num9 < 40; num9++)
			{
				if (null == Main.DustSet.NewDust(4, ref XYWH, 2 * hitDirection, -2.0, Alpha, new Color(210, 230, 140)))
				{
					break;
				}
			}
			return;
		}
		case EntityID.NPCID.VILE_SPIT:
		{
			for (int num114 = 0; num114 < 16; num114++)
			{
				Dust* ptr22 = Main.DustSet.NewDust(XYWH.X, XYWH.Y + 2, Width, Height, 18, 0.0, 0.0, 100, default, 2.0);
				if (ptr22 != null)
				{
					if (Main.Rand.Next(2) == 0)
					{
						ptr22->Scale *= 0.6f;
						continue;
					}
					ptr22->Velocity.X *= 1.4f;
					ptr22->Velocity.Y *= 1.4f;
					ptr22->NoGravity = true;
					continue;
				}
				break;
			}
			return;
		}
		case EntityID.NPCID.CORRUPT_SLIME:
		case EntityID.NPCID.SLIMER:
		case EntityID.NPCID.SHADOW_SLIME:
		{
			if (Life > 0)
			{
				for (int num21 = (int)(dmg / (double)LifeMax * 80.0); num21 > 0; num21--)
				{
					if (null == Main.DustSet.NewDust(14, ref XYWH, 0.0, 0.0, Alpha, Colour))
					{
						break;
					}
				}
				return;
			}
			for (int num22 = 0; num22 < 42; num22++)
			{
				Dust* ptr6 = Main.DustSet.NewDust(14, ref XYWH, hitDirection, 0.0, Alpha, Colour);
				if (ptr6 == null)
				{
					break;
				}
				ptr6->Velocity.X *= 2f;
				ptr6->Velocity.Y *= 2f;
			}
			if (Main.NetMode == 1)
			{
				return;
			}
			if (Type == 121)
			{
				int num23 = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + Height, 81);
				if (num23 < 196)
				{
					Main.NPCSet[num23].SetDefaults("Slimer2");
					Main.NPCSet[num23].Velocity.X = Velocity.X;
					Main.NPCSet[num23].Velocity.Y = Velocity.Y;
					Gore.NewGore(Position, Velocity, 94, Scale);
					NetMessage.CreateMessage1(23, num23);
					NetMessage.SendMessage();
				}
			}
			else
			{
				if (!(Scale >= 1f))
				{
					return;
				}
				string defaults = ((Type == 81) ? "Slimeling" : "Slimeling2");
				for (int num24 = Main.Rand.Next(1, 3); num24 >= 0; num24--)
				{
					int num25 = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + Height, 1);
					if (num25 < 196)
					{
						Main.NPCSet[num25].SetDefaults(defaults);
						Main.NPCSet[num25].Velocity.X = Velocity.X * 3f;
						Main.NPCSet[num25].Velocity.Y = Velocity.Y;
						Main.NPCSet[num25].Velocity.X += (float)Main.Rand.Next(-10, 10) * 0.1f + (float)(num24 * Direction) * 0.3f;
						Main.NPCSet[num25].Velocity.Y -= (float)Main.Rand.Next(10) * 0.1f + (float)num24;
						Main.NPCSet[num25].AI1 = num24;
						NetMessage.CreateMessage1(23, num25);
						NetMessage.SendMessage();
						continue;
					}
					break;
				}
			}
			return;
		}
		case EntityID.NPCID.CHAOS_ELEMENTAL:
		case EntityID.NPCID.ILLUMINANT_BAT:
		case EntityID.NPCID.ILLUMINANT_SLIME:
		case EntityID.NPCID.SPECTRAL_ELEMENTAL:
		{
			if (Life > 0)
			{
				for (int num122 = 0; (double)num122 < dmg / (double)LifeMax * 50.0; num122++)
				{
					Dust* ptr26 = Main.DustSet.NewDust(71, ref XYWH, 0.0, 0.0, 200);
					if (ptr26 != null)
					{
						ptr26->Velocity.X *= 1.5f;
						ptr26->Velocity.Y *= 1.5f;
						continue;
					}
					break;
				}
				return;
			}
			for (int num123 = 0; num123 < 42; num123++)
			{
				Dust* ptr27 = Main.DustSet.NewDust(71, ref XYWH, hitDirection, 0.0, 200);
				if (ptr27 != null)
				{
					ptr27->Velocity.X *= 1.5f;
					ptr27->Velocity.Y *= 1.5f;
					continue;
				}
				break;
			}
			return;
		}
		case EntityID.NPCID.GASTROPOD:
		case EntityID.NPCID.SPECTRAL_GASTROPOD:
		{
			if (Life > 0)
			{
				for (int num17 = 0; (double)num17 < dmg / (double)LifeMax * 50.0; num17++)
				{
					Dust* ptr4 = Main.DustSet.NewDust(72, ref XYWH, 0.0, 0.0, 200);
					if (ptr4 != null)
					{
						ptr4->Velocity.X *= 1.5f;
						ptr4->Velocity.Y *= 1.5f;
						continue;
					}
					break;
				}
				return;
			}
			for (int num18 = 0; num18 < 42; num18++)
			{
				Dust* ptr5 = Main.DustSet.NewDust(72, ref XYWH, hitDirection, 0.0, 200);
				if (ptr5 != null)
				{
					ptr5->Velocity.X *= 1.5f;
					ptr5->Velocity.Y *= 1.5f;
					continue;
				}
				break;
			}
			return;
		}
		case EntityID.NPCID.PIXIE:
		{
			if (Life > 0)
			{
				for (int num6 = 0; (double)num6 < dmg / (double)LifeMax * 50.0; num6++)
				{
					if (null == Main.DustSet.NewDust(55, ref XYWH, 0.0, 0.0, 200, Colour))
					{
						break;
					}
				}
				return;
			}
			for (int num7 = 0; num7 < 42; num7++)
			{
				Dust* ptr3 = Main.DustSet.NewDust(55, ref XYWH, hitDirection, 0.0, 200, Colour);
				if (ptr3 != null)
				{
					ptr3->Velocity.X *= 2f;
					ptr3->Velocity.Y *= 2f;
					continue;
				}
				break;
			}
			return;
		}
		case EntityID.NPCID.BLUE_JELLYFISH:
		case EntityID.NPCID.PINK_JELLYFISH:
		case EntityID.NPCID.GREEN_JELLYFISH:
		{
			Color dustColor = new Color(50, 120, 255, 100);
			if (Type == 64)
			{
				dustColor = new Color(225, 70, 140, 100);
			}
			else if (Type == 103)
			{
				dustColor = new Color(70, 225, 140, 100);
			}
			if (Life > 0)
			{
				for (int num96 = 0; (double)num96 < dmg / (double)LifeMax * 50.0; num96++)
				{
					if (null == Main.DustSet.NewDust(4, ref XYWH, hitDirection, -1.0, 0, dustColor))
					{
						break;
					}
				}
				return;
			}
			for (int num97 = 0; num97 < 16; num97++)
			{
				if (null == Main.DustSet.NewDust(4, ref XYWH, 2 * hitDirection, -2.0, 0, dustColor))
				{
					break;
				}
			}
			return;
		}
		case EntityID.NPCID.LAVA_SLIME:
		case EntityID.NPCID.HELLBAT:
		{
			if (Life > 0)
			{
				for (int num158 = 0; (double)num158 < dmg / (double)LifeMax * 80.0; num158++)
				{
					if (null == Main.DustSet.NewDust(6, ref XYWH, hitDirection * 2, -1.0, Alpha, default, 1.5))
					{
						break;
					}
				}
				return;
			}
			for (int num159 = 0; num159 < 32; num159++)
			{
				if (null == Main.DustSet.NewDust(6, ref XYWH, hitDirection * 2, -1.0, Alpha, default, 1.5))
				{
					break;
				}
			}
			return;
		}
		case EntityID.NPCID.KING_SLIME:
		{
			if (Life > 0)
			{
				for (int num46 = 0; (double)num46 < dmg / (double)LifeMax * 300.0; num46++)
				{
					if (null == Main.DustSet.NewDust(4, ref XYWH, hitDirection, -1.0, 175, new Color(0, 80, 255, 100)))
					{
						break;
					}
				}
				return;
			}
			for (int num47 = 0; num47 < 128; num47++)
			{
				if (null == Main.DustSet.NewDust(4, ref XYWH, hitDirection << 1, -2.0, 175, new Color(0, 80, 255, 100)))
				{
					break;
				}
			}
			if (Main.NetMode == 1)
			{
				return;
			}
			for (int num48 = Main.Rand.Next(3, 7); num48 >= 0; num48--)
			{
				int x = XYWH.X + Main.Rand.Next(Width - 32);
				int y = XYWH.Y + Main.Rand.Next(Height - 32);
				int num49 = NewNPC(x, y, 1);
				if (num49 < 196)
				{
					Main.NPCSet[num49].SetDefaults(1);
					Main.NPCSet[num49].Velocity.X = (float)Main.Rand.Next(-15, 16) * 0.1f;
					Main.NPCSet[num49].Velocity.Y = (float)Main.Rand.Next(-30, 1) * 0.1f;
					Main.NPCSet[num49].AI1 = Main.Rand.Next(3);
					NetMessage.CreateMessage1(23, num49);
					NetMessage.SendMessage();
				}
			}
			return;
		}
		case EntityID.NPCID.CAVE_BAT:
		case EntityID.NPCID.JUNGLE_BAT:
		case EntityID.NPCID.GIANT_BAT:
		{
			if (Life > 0)
			{
				for (int num74 = 0; (double)num74 < dmg / (double)LifeMax * 30.0; num74++)
				{
					if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0))
					{
						break;
					}
				}
				return;
			}
			for (int num75 = 0; num75 < 12; num75++)
			{
				if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection << 1, -2.0))
				{
					break;
				}
			}
			if (Type == 51)
			{
				Gore.NewGore(Position, Velocity, 83);
			}
			else if (Type == 93)
			{
				Gore.NewGore(Position, Velocity, 107);
			}
			else
			{
				Gore.NewGore(Position, Velocity, 82);
			}
			return;
		}
		case EntityID.NPCID.BUNNY:
		case EntityID.NPCID.GOLDFISH:
		case EntityID.NPCID.CRAB:
		case EntityID.NPCID.BIRD:
		case EntityID.NPCID.ANGLER_FISH:
		{
			if (Life > 0)
			{
				for (int num88 = 0; (double)num88 < dmg / (double)LifeMax * 20.0; num88++)
				{
					if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0))
					{
						break;
					}
				}
				return;
			}
			for (int num89 = 0; num89 < 8; num89++)
			{
				if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection << 1, -2.0))
				{
					break;
				}
			}
			if (Type == 46)
			{
				Gore.NewGore(Position, Velocity, 76);
				Gore.NewGore(Position, Velocity, 77);
			}
			else if (Type == 67)
			{
				Gore.NewGore(Position, Velocity, 95);
				Gore.NewGore(Position, Velocity, 95);
				Gore.NewGore(Position, Velocity, 96);
			}
			else if (Type == 74)
			{
				Gore.NewGore(Position, Velocity, 100);
			}
			else if (Type == 102)
			{
				Gore.NewGore(Position, Velocity, 116);
			}
			return;
		}
		case EntityID.NPCID.CORRUPT_BUNNY:
		case EntityID.NPCID.CORRUPT_GOLDFISH:
		case EntityID.NPCID.PIRANHA:
		{
			if (Life > 0)
			{
				for (int num94 = 0; (double)num94 < dmg / (double)LifeMax * 20.0; num94++)
				{
					if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0))
					{
						break;
					}
				}
				return;
			}
			for (int num95 = 0; num95 < 8; num95++)
			{
				if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection << 1, -2.0))
				{
					break;
				}
			}
			if (Type == 57)
			{
				Gore.NewGore(Position, Velocity, 84);
				return;
			}
			if (Type == 58)
			{
				Gore.NewGore(Position, Velocity, 85);
				return;
			}
			Gore.NewGore(Position, Velocity, 78);
			Gore.NewGore(Position, Velocity, 79);
			return;
		}
		case EntityID.NPCID.DEMON_EYE:
		{
			if (Life > 0)
			{
				for (int num70 = (int)(dmg / (double)LifeMax * 80.0); num70 > 0; num70--)
				{
					if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0))
					{
						break;
					}
				}
				return;
			}
			for (int num71 = 0; num71 < 42; num71++)
			{
				if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection << 1, -2.0))
				{
					break;
				}
			}
			Gore.NewGore(Position, Velocity, 1, Scale);
			Gore.NewGore(new Vector2(Position.X + 14f, Position.Y), Velocity, 2, Scale);
			return;
		}
		case EntityID.NPCID.WANDERING_EYE:
		{
			if (Life > 0)
			{
				for (int num126 = (int)(dmg / (double)LifeMax * 80.0); num126 > 0; num126--)
				{
					if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0))
					{
						break;
					}
				}
				if (Life < LifeMax >> 1 && LocalAI0 == 0)
				{
					LocalAI0 = 1;
					Gore.NewGore(Position, Velocity, 1);
				}
				return;
			}
			for (int num127 = 0; num127 < 48; num127++)
			{
				if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection << 1, -2.0))
				{
					break;
				}
			}
			Gore.NewGore(Position, Velocity, 155);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 14f), Velocity, 155);
			return;
		}
		case EntityID.NPCID.ANTLION:
		case EntityID.NPCID.ALBINO_ANTLION:
		{
			if (Life > 0)
			{
				for (int num61 = (int)(dmg / (double)LifeMax * 80.0); num61 > 0; num61--)
				{
					if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0))
					{
						break;
					}
				}
				return;
			}
			for (int num62 = 0; num62 < 42; num62++)
			{
				if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection << 1, -2.0))
				{
					break;
				}
			}
			int num63 = ((Type == 69) ? 97 : 160);
			Gore.NewGore(Position, Velocity, num63);
			Vector2 position2 = Position;
			Vector2 velocity2 = Velocity;
			int num60 = num63 + 1;
			num63 = num60;
			Gore.NewGore(position2, velocity2, num60);
			return;
		}
		case EntityID.NPCID.VULTURE:
		{
			if (Life > 0)
			{
				for (int num156 = (int)(dmg / (double)LifeMax * 80.0); num156 > 0; num156--)
				{
					if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0))
					{
						break;
					}
				}
				return;
			}
			for (int num157 = 0; num157 < 42; num157++)
			{
				if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection << 1, -2.0))
				{
					break;
				}
			}
			Gore.NewGore(Position, Velocity, 86);
			Gore.NewGore(new Vector2(Position.X + 14f, Position.Y), Velocity, 87);
			Gore.NewGore(new Vector2(Position.X + 14f, Position.Y), Velocity, 88);
			return;
		}
		case EntityID.NPCID.SHARK:
		case EntityID.NPCID.ORKA:
		{
			if (Life > 0)
			{
				for (int num76 = 0; (double)num76 < dmg / (double)LifeMax * 150.0; num76++)
				{
					if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0))
					{
						break;
					}
				}
				return;
			}
			for (int num77 = 0; num77 < 60; num77++)
			{
				if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection << 1, -2.0))
				{
					break;
				}
			}
			int num78 = ((Type == 65) ? 89 : 162);
			Vector2 velocity5 = Velocity;
			velocity5.X *= 0.8f;
			velocity5.Y *= 0.8f;
			Gore.NewGore(Position, velocity5, num78);
			Vector2 position5 = new Vector2(Position.X + 14f, Position.Y);
			Vector2 velocity6 = velocity5;
			int num60 = num78 + 1;
			num78 = num60;
			Gore.NewGore(position5, velocity6, num60);
			Vector2 position6 = new Vector2(Position.X + 14f, Position.Y);
			Vector2 velocity7 = velocity5;
			num60 = num78 + 1;
			num78 = num60;
			Gore.NewGore(position6, velocity7, num60);
			Vector2 position7 = new Vector2(Position.X + 14f, Position.Y);
			Vector2 velocity8 = velocity5;
			num60 = num78 + 1;
			num78 = num60;
			Gore.NewGore(position7, velocity8, num60);
			return;
		}
		case EntityID.NPCID.ZOMBIE:
		case EntityID.NPCID.DOCTOR_BONES:
		case EntityID.NPCID.THE_GROOM:
		case EntityID.NPCID.WEREWOLF:
		case EntityID.NPCID.CLOWN:
		case EntityID.NPCID.BALD_ZOMBIE:
		{
			if (Life > 0)
			{
				for (int num10 = (int)(dmg / (double)LifeMax * 80.0); num10 > 0; num10--)
				{
					if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0))
					{
						break;
					}
				}
				return;
			}
			for (int num11 = 0; num11 < 42; num11++)
			{
				if (null == Main.DustSet.NewDust(5, ref XYWH, 2.5 * (double)hitDirection, -2.5))
				{
					break;
				}
			}
			switch ((EntityID.NPCID)Type)
			{
			case EntityID.NPCID.WEREWOLF:
				Gore.NewGore(Position, Velocity, 117, Scale);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 118, Scale);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 118, Scale);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 119, Scale);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 119, Scale);
				break;
			case EntityID.NPCID.CLOWN:
				Gore.NewGore(Position, Velocity, 121, Scale);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 122, Scale);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 122, Scale);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 123, Scale);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 123, Scale);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 46f), Velocity, 120, Scale);
				break;
			default:
				if (Type == 132)
				{
					Gore.NewGore(Position, Velocity, 154);
				}
				Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 4);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 4);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 5);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 5);
				break;
			}
			return;
		}
		case EntityID.NPCID.CURSED_HAMMER:
		case EntityID.NPCID.ENCHANTED_SWORD:
		case EntityID.NPCID.SHADOW_HAMMER:
		{
			if (Life > 0)
			{
				for (int num145 = 0; (double)num145 < dmg / (double)LifeMax * 50.0; num145++)
				{
					Dust* ptr32 = Main.DustSet.NewDust(31, ref XYWH, 0.0, 0.0, 0, default, 1.5);
					if (ptr32 != null)
					{
						ptr32->NoGravity = true;
						continue;
					}
					break;
				}
				return;
			}
			for (int num146 = 0; num146 < 16; num146++)
			{
				Dust* ptr33 = Main.DustSet.NewDust(31, ref XYWH, 0.0, 0.0, 0, default, 1.5);
				if (ptr33 == null)
				{
					break;
				}
				ptr33->Velocity.X *= 2f;
				ptr33->Velocity.Y *= 2f;
				ptr33->NoGravity = true;
			}
			int num147 = Gore.NewGore(new Vector2(Position.X, Position.Y + (float)((Height >> 1) - 10)), new Vector2(Main.Rand.Next(-2, 3), Main.Rand.Next(-2, 3)), 61, Scale);
			Main.GoreSet[num147].Velocity *= 0.5f;
			num147 = Gore.NewGore(new Vector2(Position.X, Position.Y + (float)((Height >> 1) - 10)), new Vector2(Main.Rand.Next(-2, 3), Main.Rand.Next(-2, 3)), 61, Scale);
			Main.GoreSet[num147].Velocity *= 0.5f;
			num147 = Gore.NewGore(new Vector2(Position.X, Position.Y + (float)((Height >> 1) - 10)), new Vector2(Main.Rand.Next(-2, 3), Main.Rand.Next(-2, 3)), 61, Scale);
			Main.GoreSet[num147].Velocity *= 0.5f;
			return;
		}
		case EntityID.NPCID.EYE_OF_CTHULHU:
		case EntityID.NPCID.RETINAZER:
		case EntityID.NPCID.SPAZMATISM:
		{
			if (Life > 0)
			{
				for (int num40 = (int)(dmg / (double)LifeMax * 80.0); num40 > 0; num40--)
				{
					if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0))
					{
						break;
					}
				}
				return;
			}
			for (int num41 = 0; num41 < 128; num41++)
			{
				if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection << 1, -2.0))
				{
					break;
				}
			}
			for (int num42 = 0; num42 < 2; num42++)
			{
				Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 2);
				Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 7);
				Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 9);
				if (Type == 4)
				{
					Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 10);
					Main.PlaySound(15, XYWH.X, XYWH.Y, 0);
				}
				else if (Type == 125)
				{
					Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 146);
				}
				else if (Type == 126)
				{
					Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 145);
				}
			}
			if (Type != 125 && Type != 126)
			{
				return;
			}
			for (int num43 = 0; num43 < 8; num43++)
			{
				Dust* ptr9 = Main.DustSet.NewDust(31, ref XYWH, 0.0, 0.0, 100, default, 1.5);
				if (ptr9 == null)
				{
					break;
				}
				ptr9->Velocity.X *= 1.4f;
				ptr9->Velocity.Y *= 1.4f;
			}
			for (int num44 = 0; num44 < 4; num44++)
			{
				Dust* ptr10 = Main.DustSet.NewDust(6, ref XYWH, 0.0, 0.0, 100, default, 2.5);
				if (ptr10 == null)
				{
					break;
				}
				ptr10->NoGravity = true;
				ptr10->Velocity.X *= 5f;
				ptr10->Velocity.Y *= 5f;
				ptr10 = Main.DustSet.NewDust(6, ref XYWH, 0.0, 0.0, 100, default, 1.5);
				if (ptr10 == null)
				{
					break;
				}
				ptr10->Velocity.X *= 3f;
				ptr10->Velocity.Y *= 3f;
			}
			int num45 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num45].Velocity.X *= 0.4f;
			Main.GoreSet[num45].Velocity.X++;
			Main.GoreSet[num45].Velocity.Y *= 0.4f;
			Main.GoreSet[num45].Velocity.Y++;
			num45 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num45].Velocity.X *= 0.4f;
			Main.GoreSet[num45].Velocity.X--;
			Main.GoreSet[num45].Velocity.Y *= 0.4f;
			Main.GoreSet[num45].Velocity.Y++;
			num45 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num45].Velocity.X *= 0.4f;
			Main.GoreSet[num45].Velocity.X++;
			Main.GoreSet[num45].Velocity.Y *= 0.4f;
			Main.GoreSet[num45].Velocity.Y--;
			num45 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num45].Velocity.X *= 0.4f;
			Main.GoreSet[num45].Velocity.X--;
			Main.GoreSet[num45].Velocity.Y *= 0.4f;
			Main.GoreSet[num45].Velocity.Y--;
			return;
		}
		case EntityID.NPCID.OCRAM:
		{
			if (Life > 0)
			{
				for (int num139 = (int)(dmg / (double)LifeMax * 80.0); num139 > 0; num139--)
				{
					if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0))
					{
						break;
					}
				}
				return;
			}
			for (int num140 = 0; num140 < 128; num140++)
			{
				if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection << 1, -2.0))
				{
					break;
				}
			}
			for (int num141 = 0; num141 < 2; num141++)
			{
				Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 172);
				Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 173);
				Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 174);
				Gore.NewGore(Position, new Vector2((float)Main.Rand.Next(-30, 31) * 0.2f, (float)Main.Rand.Next(-30, 31) * 0.2f), 172);
				Main.PlaySound(15, XYWH.X, XYWH.Y, 0);
			}
			return;
		}
		case EntityID.NPCID.SERVANT_OF_CTHULHU:
		{
			if (Life > 0)
			{
				for (int num124 = 0; (double)num124 < dmg / (double)LifeMax * 50.0; num124++)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num125 = 0; num125 < 16; num125++)
			{
				Main.DustSet.NewDust(5, ref XYWH, hitDirection << 1, -2.0);
			}
			Gore.NewGore(Position, Velocity, 6);
			Gore.NewGore(Position, Velocity, 7);
			return;
		}
		case EntityID.NPCID.SERVANT_OF_OCRAM:
			if (Life > 0)
			{
				for (int num82 = 0; (double)num82 < dmg / (double)LifeMax * 50.0; num82++)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
			}
			else
			{
				for (int num83 = 0; num83 < 16; num83++)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection << 1, -2.0);
				}
			}
			return;
		case EntityID.NPCID.WALL_OF_FLESH:
		case EntityID.NPCID.WALL_OF_FLESH_EYE:
		{
			if (Life > 0)
			{
				for (int num32 = 0; num32 < 16; num32++)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num33 = 0; num33 < 42; num33++)
			{
				Main.DustSet.NewDust(5, ref XYWH, hitDirection << 1, -1.0);
			}
			Gore.NewGore(Position, Velocity, 137, Scale);
			if (Type == 114)
			{
				Gore.NewGore(new Vector2(Position.X, Position.Y + (float)(Height >> 1)), Velocity, 139, Scale);
				Gore.NewGore(new Vector2(Position.X + (float)(Width >> 1), Position.Y), Velocity, 139, Scale);
				Gore.NewGore(new Vector2(Position.X + (float)(Width >> 1), Position.Y + (float)(Height >> 1)), Velocity, 137, Scale);
			}
			else
			{
				Gore.NewGore(new Vector2(Position.X, Position.Y + (float)(Height >> 1)), Velocity, 138, Scale);
				Gore.NewGore(new Vector2(Position.X + (float)(Width >> 1), Position.Y), Velocity, 138, Scale);
				Gore.NewGore(new Vector2(Position.X + (float)(Width >> 1), Position.Y + (float)(Height >> 1)), Velocity, 137, Scale);
			}
			return;
		}
		case EntityID.NPCID.THE_HUNGRY:
		case EntityID.NPCID.THE_HUNGRY_II:
		{
			if (Life > 0)
			{
				for (int num131 = 0; num131 < 4; num131++)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			if (Type == 115 && Main.NetMode != 1)
			{
				NewNPC(XYWH.X + (Width >> 1), XYWH.Y + Height, 116);
				for (int num132 = 0; num132 < 8; num132++)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num133 = 0; num133 < 16; num133++)
			{
				Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
			}
			Gore.NewGore(Position, Velocity, 132, Scale);
			Gore.NewGore(Position, Velocity, 133, Scale);
			return;
		}
		case EntityID.NPCID.LEECH_HEAD:
		case EntityID.NPCID.LEECH_BODY:
		case EntityID.NPCID.LEECH_TAIL:
		{
			if (Life > 0)
			{
				for (int num101 = 0; num101 < 4; num101++)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num102 = 0; num102 < 8; num102++)
			{
				Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
			}
			Gore.NewGore(Position, Velocity, 134 + Type - 117, Scale);
			return;
		}
		case EntityID.NPCID.EATER_OF_SOULS:
		case EntityID.NPCID.CORRUPTOR:
		{
			if (Life > 0)
			{
				for (int num64 = (int)(dmg / (double)LifeMax * 80.0); num64 > 0; num64--)
				{
					Main.DustSet.NewDust(18, ref XYWH, hitDirection, -1.0, Alpha, Colour, Scale);
				}
				return;
			}
			for (int num65 = 0; num65 < 42; num65++)
			{
				Main.DustSet.NewDust(18, ref XYWH, hitDirection, -2.0, Alpha, Colour, Scale);
			}
			if (Type == 94)
			{
				Gore.NewGore(Position, Velocity, 108, Scale);
				Gore.NewGore(Position, Velocity, 108, Scale);
				Gore.NewGore(Position, Velocity, 109, Scale);
				Gore.NewGore(Position, Velocity, 110, Scale);
			}
			else
			{
				int num66 = Gore.NewGore(Position, Velocity, 14, Scale);
				Main.GoreSet[num66].Alpha = Alpha;
				num66 = Gore.NewGore(Position, Velocity, 15, Scale);
				Main.GoreSet[num66].Alpha = Alpha;
			}
			return;
		}
		case EntityID.NPCID.CLINGER:
		{
			if (Life > 0)
			{
				for (int num34 = (int)(dmg / (double)LifeMax * 80.0); num34 > 0; num34--)
				{
					Main.DustSet.NewDust(18, ref XYWH, hitDirection, -1.0, Alpha, Colour, Scale);
				}
				return;
			}
			for (int num35 = 0; num35 < 42; num35++)
			{
				Main.DustSet.NewDust(18, ref XYWH, hitDirection, -2.0, Alpha, Colour, Scale);
			}
			Gore.NewGore(Position, Velocity, 110, Scale);
			Gore.NewGore(Position, Velocity, 114, Scale);
			Gore.NewGore(Position, Velocity, 114, Scale);
			Gore.NewGore(Position, Velocity, 115, Scale);
			return;
		}
		case EntityID.NPCID.DEVOURER_HEAD:
		case EntityID.NPCID.DEVOURER_BODY:
		case EntityID.NPCID.DEVOURER_TAIL:
		{
			if (Life > 0)
			{
				for (int i = (int)(dmg / (double)LifeMax * 80.0); i > 0; i--)
				{
					Main.DustSet.NewDust(18, ref XYWH, hitDirection, -1.0, Alpha, Colour, Scale);
				}
				return;
			}
			for (int j = 0; j < 42; j++)
			{
				Main.DustSet.NewDust(18, ref XYWH, hitDirection, -2.0, Alpha, Colour, Scale);
			}
			int num = Gore.NewGore(Position, Velocity, Type - 7 + 18);
			Main.GoreSet[num].Alpha = Alpha;
			return;
		}
		case EntityID.NPCID.SEEKER_HEAD:
		case EntityID.NPCID.SEEKER_BODY:
		case EntityID.NPCID.SEEKER_TAIL:
		{
			if (Life > 0)
			{
				for (int num142 = (int)(dmg / (double)LifeMax * 80.0); num142 > 0; num142--)
				{
					Main.DustSet.NewDust(18, ref XYWH, hitDirection, -1.0, Alpha, Colour, Scale);
				}
				return;
			}
			for (int num143 = 0; num143 < 42; num143++)
			{
				Main.DustSet.NewDust(18, ref XYWH, hitDirection, -2.0, Alpha, Colour, Scale);
			}
			int num144 = Gore.NewGore(Position, Velocity, 110);
			Main.GoreSet[num144].Alpha = Alpha;
			return;
		}
		case EntityID.NPCID.GIANT_WORM_HEAD:
		case EntityID.NPCID.GIANT_WORM_BODY:
		case EntityID.NPCID.GIANT_WORM_TAIL:
		{
			if (Life > 0)
			{
				for (int num110 = 0; (double)num110 < dmg / (double)LifeMax * 50.0; num110++)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num111 = 0; num111 < 8; num111++)
			{
				Main.DustSet.NewDust(5, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			Gore.NewGore(Position, Velocity, Type - 10 + 21);
			return;
		}
		case EntityID.NPCID.DIGGER_HEAD:
		case EntityID.NPCID.DIGGER_BODY:
		case EntityID.NPCID.DIGGER_TAIL:
		{
			if (Life > 0)
			{
				for (int num90 = 0; (double)num90 < dmg / (double)LifeMax * 50.0; num90++)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num91 = 0; num91 < 8; num91++)
			{
				Main.DustSet.NewDust(5, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			Gore.NewGore(Position, Velocity, Type - 95 + 111);
			return;
		}
		case EntityID.NPCID.EATER_OF_WORLDS_HEAD:
		case EntityID.NPCID.EATER_OF_WORLDS_BODY:
		case EntityID.NPCID.EATER_OF_WORLDS_TAIL:
		{
			if (Life > 0)
			{
				for (int num26 = (int)(dmg / (double)LifeMax * 80.0); num26 > 0; num26--)
				{
					Main.DustSet.NewDust(18, ref XYWH, hitDirection, -1.0, Alpha, Colour, Scale);
				}
				return;
			}
			for (int num27 = 0; num27 < 42; num27++)
			{
				Main.DustSet.NewDust(18, ref XYWH, hitDirection, -2.0, Alpha, Colour, Scale);
			}
			if (Type == 13)
			{
				Gore.NewGore(Position, Velocity, 24);
				Gore.NewGore(Position, Velocity, 25);
			}
			else if (Type == 14)
			{
				Gore.NewGore(Position, Velocity, 26);
				Gore.NewGore(Position, Velocity, 27);
			}
			else
			{
				Gore.NewGore(Position, Velocity, 28);
				Gore.NewGore(Position, Velocity, 29);
			}
			return;
		}
		case EntityID.NPCID.MERCHANT:
		{
			if (Life > 0)
			{
				for (int num4 = (int)(dmg / (double)LifeMax * 80.0); num4 > 0; num4--)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num5 = 0; num5 < 42; num5++)
			{
				Main.DustSet.NewDust(5, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			Gore.NewGore(Position, Velocity, 30);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 31);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 31);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 32);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 32);
			return;
		}
		case EntityID.NPCID.UNICORN:
		{
			if (Life > 0)
			{
				for (int num154 = (int)(dmg / (double)LifeMax * 80.0); num154 > 0; num154--)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num155 = 0; num155 < 42; num155++)
			{
				Main.DustSet.NewDust(5, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			Gore.NewGore(Position, Velocity, 101);
			Gore.NewGore(Position, Velocity, 102);
			Gore.NewGore(Position, Velocity, 103);
			Gore.NewGore(Position, Velocity, 103);
			Gore.NewGore(Position, Velocity, 104);
			Gore.NewGore(Position, Velocity, 104);
			Gore.NewGore(Position, Velocity, 105);
			return;
		}
		case EntityID.NPCID.BOUND_GOBLIN:
		case EntityID.NPCID.BOUND_WIZARD:
		case EntityID.NPCID.GOBLIN_TINKERER:
		case EntityID.NPCID.WIZARD:
		{
			if (Life > 0)
			{
				for (int num108 = (int)(dmg / (double)LifeMax * 80.0); num108 > 0; num108--)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num109 = 0; num109 < 42; num109++)
			{
				Main.DustSet.NewDust(5, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			if (Type == 105 || Type == 107)
			{
				Gore.NewGore(Position, Velocity, 124);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 125);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 125);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 126);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 126);
			}
			else
			{
				Gore.NewGore(Position, Velocity, 127);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 128);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 128);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 129);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 129);
			}
			return;
		}
		case EntityID.NPCID.BOUND_MECHANIC:
		case EntityID.NPCID.MECHANIC:
		{
			if (Life > 0)
			{
				for (int num86 = (int)(dmg / (double)LifeMax * 80.0); num86 > 0; num86--)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num87 = 0; num87 < 42; num87++)
			{
				Main.DustSet.NewDust(5, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			Gore.NewGore(Position, Velocity, 151);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 151);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 152);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 153);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 153);
			return;
		}
		case EntityID.NPCID.GUIDE:
		{
			if (Life > 0)
			{
				for (int num79 = (int)(dmg / (double)LifeMax * 80.0); num79 > 0; num79--)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num80 = 0; num80 < 42; num80++)
			{
				Main.DustSet.NewDust(5, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			Gore.NewGore(Position, Velocity, 73);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 74);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 74);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 75);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 75);
			return;
		}
		case EntityID.NPCID.SANTA_CLAUS:
		{
			if (Life > 0)
			{
				for (int num38 = (int)(dmg / (double)LifeMax * 80.0); num38 > 0; num38--)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num39 = 0; num39 < 42; num39++)
			{
				Main.DustSet.NewDust(5, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			Gore.NewGore(Position, Velocity, 157);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 158);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 158);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 159);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 159);
			return;
		}
		case EntityID.NPCID.OLD_MAN:
		case EntityID.NPCID.CLOTHIER:
		{
			if (Life > 0)
			{
				for (int num12 = (int)(dmg / (double)LifeMax * 80.0); num12 > 0; num12--)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num13 = 0; num13 < 42; num13++)
			{
				Main.DustSet.NewDust(5, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			Gore.NewGore(Position, Velocity, 58);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 59);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 59);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 60);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 60);
			return;
		}
		case EntityID.NPCID.NURSE:
		{
			if (Life > 0)
			{
				for (int m = (int)(dmg / (double)LifeMax * 80.0); m > 0; m--)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int n = 0; n < 42; n++)
			{
				Main.DustSet.NewDust(5, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			Gore.NewGore(Position, Velocity, 33);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 34);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 34);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 35);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 35);
			return;
		}
		case EntityID.NPCID.ARMS_DEALER:
		{
			if (Life > 0)
			{
				for (int num137 = (int)(dmg / (double)LifeMax * 80.0); num137 > 0; num137--)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num138 = 0; num138 < 42; num138++)
			{
				Main.DustSet.NewDust(5, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			Gore.NewGore(Position, Velocity, 36);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 37);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 37);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 38);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 38);
			return;
		}
		case EntityID.NPCID.DEMOLITIONIST:
		{
			if (Life > 0)
			{
				for (int num115 = (int)(dmg / (double)LifeMax * 80.0); num115 > 0; num115--)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num116 = 0; num116 < 42; num116++)
			{
				Main.DustSet.NewDust(5, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			Gore.NewGore(Position, Velocity, 64);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 65);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 65);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 66);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 66);
			return;
		}
		case EntityID.NPCID.DRYAD:
		{
			if (Life > 0)
			{
				for (int num98 = (int)(dmg / (double)LifeMax * 80.0); num98 > 0; num98--)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num99 = 0; num99 < 42; num99++)
			{
				Main.DustSet.NewDust(5, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			Gore.NewGore(Position, Velocity, 39);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 40);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 40);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 41);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 41);
			return;
		}
		case EntityID.NPCID.SKELETON:
		case EntityID.NPCID.BONES:
		case EntityID.NPCID.DARK_CASTER:
		case EntityID.NPCID.UNDEAD_MINER:
		case EntityID.NPCID.TIM:
		case EntityID.NPCID.ARMORED_SKELETON:
		case EntityID.NPCID.SKELETON_ARCHER:
		case EntityID.NPCID.VAMPIRE_MINER:
		{
			if (Life > 0)
			{
				for (int num67 = 0; (double)num67 < dmg / (double)LifeMax * 50.0; num67++)
				{
					Main.DustSet.NewDust(26, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num68 = 0; num68 < 16; num68++)
			{
				Main.DustSet.NewDust(26, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			int num69 = ((Type == 149) ? 166 : 42);
			Gore.NewGore(Position, Velocity, num69, Scale);
			Vector2 position3 = new Vector2(Position.X, Position.Y + 20f);
			Vector2 velocity3 = Velocity;
			int num60 = num69 + 1;
			num69 = num60;
			Gore.NewGore(position3, velocity3, num60, Scale);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, num69, Scale);
			Vector2 position4 = new Vector2(Position.X, Position.Y + 34f);
			Vector2 velocity4 = Velocity;
			num60 = num69 + 1;
			num69 = num60;
			Gore.NewGore(position4, velocity4, num60, Scale);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, num69, Scale);
			if (Type == 77)
			{
				Gore.NewGore(Position, Velocity, 106, Scale);
			}
			else if (Type == 110)
			{
				Gore.NewGore(Position, Velocity, 130, Scale);
				Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 131, Scale);
			}
			return;
		}
		case EntityID.NPCID.MIMIC:
		{
			int type = 7;
			if (AI3 == 2f)
			{
				type = 10;
			}
			else if (AI3 == 3f)
			{
				type = 37;
			}
			if (Life > 0)
			{
				for (int num14 = 0; (double)num14 < dmg / (double)LifeMax * 50.0; num14++)
				{
					if (null == Main.DustSet.NewDust(type, ref XYWH))
					{
						break;
					}
				}
				return;
			}
			for (int num15 = 0; num15 < 16; num15++)
			{
				if (null == Main.DustSet.NewDust(type, ref XYWH))
				{
					break;
				}
			}
			int num16 = Gore.NewGore(new Vector2(Position.X, Position.Y - 10f), new Vector2(hitDirection, 0f), 61, Scale);
			Main.GoreSet[num16].Velocity *= 0.3f;
			num16 = Gore.NewGore(new Vector2(Position.X, Position.Y + (float)(Height >> 1) - 10f), new Vector2(hitDirection, 0f), 62, Scale);
			Main.GoreSet[num16].Velocity *= 0.3f;
			num16 = Gore.NewGore(new Vector2(Position.X, Position.Y + (float)(int)Height - 10f), new Vector2(hitDirection, 0f), 63, Scale);
			Main.GoreSet[num16].Velocity *= 0.3f;
			return;
		}
		case EntityID.NPCID.WYVERN_HEAD:
		case EntityID.NPCID.WYVERN_LEGS:
		case EntityID.NPCID.WYVERN_BODY1:
		case EntityID.NPCID.WYVERN_BODY2:
		case EntityID.NPCID.WYVERN_BODY3:
		case EntityID.NPCID.WYVERN_TAIL:
		case EntityID.NPCID.ARCH_WYVERN_HEAD:
		case EntityID.NPCID.ARCH_WYVERN_LEGS:
		case EntityID.NPCID.ARCH_WYVERN_BODY1:
		case EntityID.NPCID.ARCH_WYVERN_BODY2:
		case EntityID.NPCID.ARCH_WYVERN_BODY3:
		case EntityID.NPCID.ARCH_WYVERN_TAIL:
		{
			if (Life > 0)
			{
				for (int num28 = 0; (double)num28 < dmg / (double)LifeMax * 50.0; num28++)
				{
					Dust* ptr7 = Main.DustSet.NewDust(16, ref XYWH, 0.0, 0.0, 0, default, 1.5);
					if (ptr7 != null)
					{
						ptr7->Velocity.X *= 1.5f;
						ptr7->Velocity.Y *= 1.5f;
						ptr7->NoGravity = true;
						continue;
					}
					break;
				}
				return;
			}
			for (int num29 = 0; num29 < 8; num29++)
			{
				Dust* ptr8 = Main.DustSet.NewDust(16, ref XYWH, 0.0, 0.0, 0, default, 1.5);
				if (ptr8 == null)
				{
					break;
				}
				ptr8->Velocity.X *= 2f;
				ptr8->Velocity.Y *= 2f;
				ptr8->NoGravity = true;
			}
			for (int num30 = Main.Rand.Next(1, 4); num30 > 0; num30--)
			{
				int num31 = Gore.NewGore(new Vector2(Position.X, Position.Y + (float)(Height >> 1) - 10f), new Vector2(hitDirection, 0f), Main.Rand.Next(11, 14), Scale);
				Main.GoreSet[num31].Velocity *= 0.8f;
			}
			return;
		}
		case EntityID.NPCID.MUMMY:
		case EntityID.NPCID.DARK_MUMMY:
		case EntityID.NPCID.LIGHT_MUMMY:
		case EntityID.NPCID.SHADOW_MUMMY:
		case EntityID.NPCID.SPECTRAL_MUMMY:
		{
			if (Life > 0)
			{
				for (int num148 = 0; (double)num148 < dmg / (double)LifeMax * 50.0; num148++)
				{
					Dust* ptr34 = Main.DustSet.NewDust(31, ref XYWH, 0.0, 0.0, 0, default, 1.5);
					if (ptr34 != null)
					{
						ptr34->Velocity.X *= 2f;
						ptr34->Velocity.Y *= 2f;
						ptr34->NoGravity = true;
						continue;
					}
					break;
				}
				return;
			}
			for (int num149 = 0; num149 < 16; num149++)
			{
				Dust* ptr35 = Main.DustSet.NewDust(31, ref XYWH, 0.0, 0.0, 0, default, 1.5);
				if (ptr35 == null)
				{
					break;
				}
				ptr35->Velocity.X *= 2f;
				ptr35->Velocity.Y *= 2f;
				ptr35->NoGravity = true;
			}
			int num150 = Gore.NewGore(new Vector2(Position.X, Position.Y - 10f), new Vector2(hitDirection, 0f), 61, Scale);
			Main.GoreSet[num150].Velocity *= 0.3f;
			num150 = Gore.NewGore(new Vector2(Position.X, Position.Y + (float)(Height >> 1) - 10f), new Vector2(hitDirection, 0f), 62, Scale);
			Main.GoreSet[num150].Velocity *= 0.3f;
			num150 = Gore.NewGore(new Vector2(Position.X, Position.Y + (float)(int)Height - 10f), new Vector2(hitDirection, 0f), 63, Scale);
			Main.GoreSet[num150].Velocity *= 0.3f;
			return;
		}
		case EntityID.NPCID.WRAITH:
		{
			if (Life > 0)
			{
				for (int num128 = 0; (double)num128 < dmg / (double)LifeMax * 50.0; num128++)
				{
					Dust* ptr28 = Main.DustSet.NewDust(54, ref XYWH, 0.0, 0.0, 50, default, 1.5);
					if (ptr28 != null)
					{
						ptr28->Velocity.X *= 2f;
						ptr28->Velocity.Y *= 2f;
						ptr28->NoGravity = true;
						continue;
					}
					break;
				}
				return;
			}
			for (int num129 = 0; num129 < 16; num129++)
			{
				Dust* ptr29 = Main.DustSet.NewDust(54, ref XYWH, 0.0, 0.0, 50, default, 1.5);
				if (ptr29 == null)
				{
					break;
				}
				ptr29->Velocity.X *= 2f;
				ptr29->Velocity.Y *= 2f;
				ptr29->NoGravity = true;
			}
			int num130 = Gore.NewGore(new Vector2(Position.X, Position.Y - 10f), new Vector2(hitDirection, 0f), 99, Scale);
			Main.GoreSet[num130].Velocity *= 0.3f;
			num130 = Gore.NewGore(new Vector2(Position.X, Position.Y + (float)(Height >> 1) - 15f), new Vector2(hitDirection, 0f), 99, Scale);
			Main.GoreSet[num130].Velocity *= 0.3f;
			num130 = Gore.NewGore(new Vector2(Position.X, Position.Y + (float)(int)Height - 20f), new Vector2(hitDirection, 0f), 99, Scale);
			Main.GoreSet[num130].Velocity *= 0.3f;
			return;
		}
		case EntityID.NPCID.POSSESSED_ARMOR:
		{
			if (Life > 0)
			{
				return;
			}
			for (int num120 = 0; num120 < 16; num120++)
			{
				Dust* ptr25 = Main.DustSet.NewDust(54, ref XYWH, 0.0, 0.0, 50, default, 1.5);
				if (ptr25 == null)
				{
					break;
				}
				ptr25->Velocity.X *= 2f;
				ptr25->Velocity.Y *= 2f;
				ptr25->NoGravity = true;
			}
			int num121 = Gore.NewGore(new Vector2(Position.X, Position.Y - 10f), new Vector2(hitDirection, 0f), 99, Scale);
			Main.GoreSet[num121].Velocity *= 0.3f;
			num121 = Gore.NewGore(new Vector2(Position.X, Position.Y + (float)(Height >> 1) - 15f), new Vector2(hitDirection, 0f), 99, Scale);
			Main.GoreSet[num121].Velocity *= 0.3f;
			num121 = Gore.NewGore(new Vector2(Position.X, Position.Y + (float)(int)Height - 20f), new Vector2(hitDirection, 0f), 99, Scale);
			Main.GoreSet[num121].Velocity *= 0.3f;
			return;
		}
		case EntityID.NPCID.BONE_SERPENT_HEAD:
		case EntityID.NPCID.BONE_SERPENT_BODY:
		case EntityID.NPCID.BONE_SERPENT_TAIL:
		{
			if (Life > 0)
			{
				for (int num104 = 0; (double)num104 < dmg / (double)LifeMax * 50.0; num104++)
				{
					Main.DustSet.NewDust(26, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num105 = 0; num105 < 16; num105++)
			{
				Main.DustSet.NewDust(26, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			Gore.NewGore(Position, Velocity, Type - 39 + 67);
			return;
		}
		case EntityID.NPCID.CURSED_SKULL:
		case EntityID.NPCID.DRAGON_SKULL:
		{
			if (Life > 0)
			{
				for (int num92 = 0; (double)num92 < dmg / (double)LifeMax * 30.0; num92++)
				{
					Dust* ptr14 = Main.DustSet.NewDust(15, ref XYWH, (double)Velocity.X * -0.2, (double)Velocity.Y * -0.2, 100, default, 1.8);
					if (ptr14 != null)
					{
						ptr14->NoLight = true;
						ptr14->NoGravity = true;
						ptr14->Velocity.X *= 1.3f;
						ptr14->Velocity.Y *= 1.3f;
						ptr14 = Main.DustSet.NewDust(26, ref XYWH, (double)Velocity.X * -0.2, (double)Velocity.Y * -0.2, 0, default, 0.9);
						if (ptr14 != null)
						{
							ptr14->NoLight = true;
							ptr14->Velocity.X *= 1.3f;
							ptr14->Velocity.Y *= 1.3f;
							continue;
						}
						break;
					}
					break;
				}
				return;
			}
			for (int num93 = 0; num93 < 12; num93++)
			{
				Dust* ptr15 = Main.DustSet.NewDust(15, ref XYWH, (double)Velocity.X * -0.2, (double)Velocity.Y * -0.2, 100, default, 1.8);
				if (ptr15 != null)
				{
					ptr15->NoLight = true;
					ptr15->NoGravity = true;
					ptr15->Velocity.X *= 1.3f;
					ptr15->Velocity.Y *= 1.3f;
					ptr15 = Main.DustSet.NewDust(26, ref XYWH, (double)Velocity.X * -0.2, (double)Velocity.Y * -0.2, 0, default, 0.9);
					if (ptr15 != null)
					{
						ptr15->NoLight = true;
						ptr15->Velocity.X *= 1.3f;
						ptr15->Velocity.Y *= 1.3f;
						continue;
					}
					break;
				}
				break;
			}
			return;
		}
		case EntityID.NPCID.SKELETRON_HEAD:
		case EntityID.NPCID.SKELETRON_HAND:
		{
			if (Life > 0)
			{
				for (int num72 = (int)(dmg / (double)LifeMax * 80.0); num72 > 0; num72--)
				{
					Main.DustSet.NewDust(26, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num73 = 0; num73 < 128; num73++)
			{
				Main.DustSet.NewDust(26, ref XYWH, 2.5 * (double)hitDirection, -2.5);
			}
			if (Type == 35)
			{
				Gore.NewGore(Position, Velocity, 54);
				Gore.NewGore(Position, Velocity, 55);
				return;
			}
			Gore.NewGore(Position, Velocity, 56);
			Gore.NewGore(Position, Velocity, 57);
			Gore.NewGore(Position, Velocity, 57);
			Gore.NewGore(Position, Velocity, 57);
			return;
		}
		case EntityID.NPCID.PROBE:
		{
			if (Life > 0)
			{
				return;
			}
			for (int num50 = 0; num50 < 8; num50++)
			{
				Dust* ptr11 = Main.DustSet.NewDust(31, ref XYWH, 0.0, 0.0, 100, default, 1.5);
				if (ptr11 == null)
				{
					break;
				}
				ptr11->Velocity.X *= 1.4f;
				ptr11->Velocity.Y *= 1.4f;
			}
			for (int num51 = 0; num51 < 4; num51++)
			{
				Dust* ptr12 = Main.DustSet.NewDust(6, ref XYWH, 0.0, 0.0, 100, default, 2.5);
				if (ptr12 == null)
				{
					break;
				}
				ptr12->NoGravity = true;
				ptr12->Velocity.X *= 5f;
				ptr12->Velocity.Y *= 5f;
				ptr12 = Main.DustSet.NewDust(6, ref XYWH, 0.0, 0.0, 100, default, 1.5);
				if (ptr12 == null)
				{
					break;
				}
				ptr12->Velocity.X *= 3f;
				ptr12->Velocity.Y *= 3f;
			}
			int num52 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num52].Velocity *= 0.4f;
			Main.GoreSet[num52].Velocity.X++;
			Main.GoreSet[num52].Velocity.Y++;
			num52 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num52].Velocity *= 0.4f;
			Main.GoreSet[num52].Velocity.X--;
			Main.GoreSet[num52].Velocity.Y++;
			num52 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num52].Velocity *= 0.4f;
			Main.GoreSet[num52].Velocity.X++;
			Main.GoreSet[num52].Velocity.Y--;
			num52 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num52].Velocity *= 0.4f;
			Main.GoreSet[num52].Velocity.X--;
			Main.GoreSet[num52].Velocity.Y--;
			return;
		}
		case EntityID.NPCID.THE_DESTROYER_HEAD:
		case EntityID.NPCID.THE_DESTROYER_BODY:
		case EntityID.NPCID.THE_DESTROYER_TAIL:
		{
			if (Type == 135 && Life > 0 && Main.NetMode != 1 && AI2 == 0f && Main.Rand.Next(25) == 0)
			{
				AI2 = 1f;
				int num2 = NewNPC(XYWH.X + (Width >> 1), XYWH.Y + Height, 139);
				if (Main.NetMode == 2 && num2 < 196)
				{
					NetMessage.CreateMessage1(23, num2);
					NetMessage.SendMessage();
				}
				ShouldNetUpdate = true;
			}
			if (Life > 0)
			{
				return;
			}
			Gore.NewGore(Position, Velocity, 156);
			if (Main.Rand.Next(2) != 0)
			{
				return;
			}
			for (int k = 0; k < 8; k++)
			{
				Dust* ptr = Main.DustSet.NewDust(31, ref XYWH, 0.0, 0.0, 100, default, 1.5);
				if (ptr == null)
				{
					break;
				}
				ptr->Velocity.X *= 1.4f;
				ptr->Velocity.Y *= 1.4f;
			}
			for (int l = 0; l < 4; l++)
			{
				Dust* ptr2 = Main.DustSet.NewDust(6, ref XYWH, 0.0, 0.0, 100, default, 2.5);
				if (ptr2 == null)
				{
					break;
				}
				ptr2->NoGravity = true;
				ptr2->Velocity.X *= 5f;
				ptr2->Velocity.Y *= 5f;
				ptr2 = Main.DustSet.NewDust(6, ref XYWH, 0.0, 0.0, 100, default, 1.5);
				if (ptr2 == null)
				{
					break;
				}
				ptr2->Velocity.X *= 3f;
				ptr2->Velocity.Y *= 3f;
			}
			int num3 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num3].Velocity.X *= 0.4f;
			Main.GoreSet[num3].Velocity.X++;
			Main.GoreSet[num3].Velocity.Y *= 0.4f;
			Main.GoreSet[num3].Velocity.Y++;
			num3 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num3].Velocity.X *= 0.4f;
			Main.GoreSet[num3].Velocity.X--;
			Main.GoreSet[num3].Velocity.Y *= 0.4f;
			Main.GoreSet[num3].Velocity.Y++;
			num3 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num3].Velocity.X *= 0.4f;
			Main.GoreSet[num3].Velocity.X++;
			Main.GoreSet[num3].Velocity.Y *= 0.4f;
			Main.GoreSet[num3].Velocity.Y--;
			num3 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num3].Velocity.X *= 0.4f;
			Main.GoreSet[num3].Velocity.X--;
			Main.GoreSet[num3].Velocity.Y *= 0.4f;
			Main.GoreSet[num3].Velocity.Y--;
			return;
		}
		case EntityID.NPCID.SKELETRON_PRIME:
		{
			if (Life > 0)
			{
				return;
			}
			Gore.NewGore(Position, Velocity, 149);
			Gore.NewGore(Position, Velocity, 150);
			for (int num151 = 0; num151 < 8; num151++)
			{
				Dust* ptr36 = Main.DustSet.NewDust(31, ref XYWH, 0.0, 0.0, 100, default, 1.5);
				if (ptr36 == null)
				{
					break;
				}
				ptr36->Velocity.X *= 1.4f;
				ptr36->Velocity.Y *= 1.4f;
			}
			for (int num152 = 0; num152 < 4; num152++)
			{
				Dust* ptr37 = Main.DustSet.NewDust(6, ref XYWH, 0.0, 0.0, 100, default, 2.5);
				if (ptr37 == null)
				{
					break;
				}
				ptr37->NoGravity = true;
				ptr37->Velocity.X *= 5f;
				ptr37->Velocity.Y *= 5f;
				ptr37 = Main.DustSet.NewDust(6, ref XYWH, 0.0, 0.0, 100, default, 1.5);
				if (ptr37 == null)
				{
					break;
				}
				ptr37->Velocity.X *= 3f;
				ptr37->Velocity.Y *= 3f;
			}
			int num153 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num153].Velocity.X *= 0.4f;
			Main.GoreSet[num153].Velocity.X++;
			Main.GoreSet[num153].Velocity.Y *= 0.4f;
			Main.GoreSet[num153].Velocity.Y++;
			num153 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num153].Velocity.X *= 0.4f;
			Main.GoreSet[num153].Velocity.X--;
			Main.GoreSet[num153].Velocity.Y *= 0.4f;
			Main.GoreSet[num153].Velocity.Y++;
			num153 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num153].Velocity.X *= 0.4f;
			Main.GoreSet[num153].Velocity.X++;
			Main.GoreSet[num153].Velocity.Y *= 0.4f;
			Main.GoreSet[num153].Velocity.Y--;
			num153 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num153].Velocity.X *= 0.4f;
			Main.GoreSet[num153].Velocity.X--;
			Main.GoreSet[num153].Velocity.Y *= 0.4f;
			Main.GoreSet[num153].Velocity.Y--;
			return;
		}
		case EntityID.NPCID.PRIME_CANNON:
		case EntityID.NPCID.PRIME_SAW:
		case EntityID.NPCID.PRIME_VICE:
		case EntityID.NPCID.PRIME_LASER:
		{
			if (Life > 0)
			{
				return;
			}
			Gore.NewGore(Position, Velocity, 147);
			Gore.NewGore(Position, Velocity, 148);
			for (int num134 = 0; num134 < 8; num134++)
			{
				Dust* ptr30 = Main.DustSet.NewDust(31, ref XYWH, 0.0, 0.0, 100, default, 1.5);
				if (ptr30 == null)
				{
					break;
				}
				ptr30->Velocity.X *= 1.4f;
				ptr30->Velocity.Y *= 1.4f;
			}
			for (int num135 = 0; num135 < 4; num135++)
			{
				Dust* ptr31 = Main.DustSet.NewDust(6, ref XYWH, 0.0, 0.0, 100, default, 2.5);
				if (ptr31 == null)
				{
					break;
				}
				ptr31->NoGravity = true;
				ptr31->Velocity.X *= 5f;
				ptr31->Velocity.Y *= 5f;
				ptr31 = Main.DustSet.NewDust(6, ref XYWH, 0.0, 0.0, 100, default, 1.5);
				if (ptr31 == null)
				{
					break;
				}
				ptr31->Velocity.X *= 3f;
				ptr31->Velocity.Y *= 3f;
			}
			int num136 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num136].Velocity.X *= 0.4f;
			Main.GoreSet[num136].Velocity.X++;
			Main.GoreSet[num136].Velocity.Y *= 0.4f;
			Main.GoreSet[num136].Velocity.Y++;
			num136 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num136].Velocity.X *= 0.4f;
			Main.GoreSet[num136].Velocity.X--;
			Main.GoreSet[num136].Velocity.Y *= 0.4f;
			Main.GoreSet[num136].Velocity.Y++;
			num136 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num136].Velocity.X *= 0.4f;
			Main.GoreSet[num136].Velocity.X++;
			Main.GoreSet[num136].Velocity.Y *= 0.4f;
			Main.GoreSet[num136].Velocity.Y--;
			num136 = Gore.NewGore(Position, default, Main.Rand.Next(61, 64));
			Main.GoreSet[num136].Velocity.X *= 0.4f;
			Main.GoreSet[num136].Velocity.X--;
			Main.GoreSet[num136].Velocity.Y *= 0.4f;
			Main.GoreSet[num136].Velocity.Y--;
			return;
		}
		case EntityID.NPCID.METEOR_HEAD:
		{
			if (Life > 0)
			{
				for (int num117 = (int)(dmg / (double)LifeMax * 80.0); num117 > 0; num117--)
				{
					int type2 = ((Main.Rand.Next(2) == 0) ? 6 : 25);
					Main.DustSet.NewDust(type2, ref XYWH, hitDirection, -1.0);
					Dust* ptr23 = Main.DustSet.NewDust(6, ref XYWH, (double)Velocity.X * 0.2, (double)Velocity.Y * 0.2, 100, default, 2.0);
					if (ptr23 != null)
					{
						ptr23->NoGravity = true;
						continue;
					}
					break;
				}
				return;
			}
			for (int num118 = 0; num118 < 42; num118++)
			{
				int type3 = ((Main.Rand.Next(2) == 0) ? 6 : 25);
				if (null == Main.DustSet.NewDust(type3, ref XYWH, hitDirection << 1, -2.0))
				{
					break;
				}
			}
			for (int num119 = 0; num119 < 42; num119++)
			{
				Dust* ptr24 = Main.DustSet.NewDust(6, ref XYWH, (double)Velocity.X * 0.2, (double)Velocity.Y * 0.2, 100, default, 2.5);
				if (ptr24 != null)
				{
					ptr24->Velocity.X *= 6f;
					ptr24->Velocity.Y *= 6f;
					ptr24->NoGravity = true;
					continue;
				}
				break;
			}
			return;
		}
		case EntityID.NPCID.FIRE_IMP:
		{
			if (Life > 0)
			{
				for (int num106 = (int)(dmg / (double)LifeMax * 80.0); num106 > 0; num106--)
				{
					Dust* ptr18 = Main.DustSet.NewDust(6, ref XYWH, Velocity.X, Velocity.Y, 100, default, 2.5);
					if (ptr18 != null)
					{
						ptr18->NoGravity = true;
						continue;
					}
					break;
				}
				return;
			}
			for (int num107 = 0; num107 < 42; num107++)
			{
				Dust* ptr19 = Main.DustSet.NewDust(6, ref XYWH, Velocity.X, Velocity.Y, 100, default, 2.5);
				if (ptr19 == null)
				{
					break;
				}
				ptr19->NoGravity = true;
				ptr19->Velocity.X *= 2f;
				ptr19->Velocity.Y *= 2f;
			}
			Gore.NewGore(Position, Velocity, 45);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 46);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 46);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 47);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 47);
			return;
		}
		case EntityID.NPCID.BURNING_SPHERE:
		{
			Main.PlaySound(2, XYWH.X, XYWH.Y, 10);
			for (int num103 = 0; num103 < 16; num103++)
			{
				Dust* ptr17 = Main.DustSet.NewDust(6, ref XYWH, (double)Velocity.X * -0.2, (double)Velocity.Y * -0.2, 100, default, 2.0);
				if (ptr17 != null)
				{
					ptr17->NoGravity = true;
					ptr17->Velocity *= 2f;
					ptr17 = Main.DustSet.NewDust(6, ref XYWH, (double)Velocity.X * -0.2, (double)Velocity.Y * -0.2, 100);
					if (ptr17 != null)
					{
						ptr17->Velocity.X *= 2f;
						ptr17->Velocity.Y *= 2f;
						continue;
					}
					break;
				}
				break;
			}
			return;
		}
		case EntityID.NPCID.WATER_SPHERE:
		{
			Main.PlaySound(2, XYWH.X, XYWH.Y, 10);
			for (int num100 = 0; num100 < 16; num100++)
			{
				Dust* ptr16 = Main.DustSet.NewDust(29, ref XYWH, Velocity.X * -0.2f, (double)Velocity.Y * -0.2, 100, default, 2.0);
				if (ptr16 != null)
				{
					ptr16->NoGravity = true;
					ptr16->Velocity.X *= 2f;
					ptr16->Velocity.Y *= 2f;
					ptr16 = Main.DustSet.NewDust(29, ref XYWH, Velocity.X * -0.2f, (double)Velocity.Y * -0.2, 100);
					if (ptr16 != null)
					{
						ptr16->Velocity.X *= 2f;
						ptr16->Velocity.Y *= 2f;
						continue;
					}
					break;
				}
				break;
			}
			return;
		}
		case EntityID.NPCID.GOBLIN_PEON:
		case EntityID.NPCID.GOBLIN_THIEF:
		case EntityID.NPCID.GOBLIN_WARRIOR:
		case EntityID.NPCID.GOBLIN_SORCERER:
		case EntityID.NPCID.GOBLIN_SCOUT:
		case EntityID.NPCID.GOBLIN_ARCHER:
		{
			if (Life > 0)
			{
				for (int num84 = (int)(dmg / (double)LifeMax * 80.0); num84 > 0; num84--)
				{
					if (null == Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0))
					{
						break;
					}
				}
				return;
			}
			for (int num85 = 0; num85 < 42; num85++)
			{
				if (null == Main.DustSet.NewDust(5, ref XYWH, 2.5 * (double)hitDirection, -2.5))
				{
					break;
				}
			}
			Gore.NewGore(Position, Velocity, 48, Scale);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 49, Scale);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 20f), Velocity, 49, Scale);
			if (Type == 111)
			{
				Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 131, Scale);
			}
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 50, Scale);
			Gore.NewGore(new Vector2(Position.X, Position.Y + 34f), Velocity, 50, Scale);
			return;
		}
		case EntityID.NPCID.CHAOS_BALL:
		{
			Main.PlaySound(2, (int)Position.X, (int)Position.Y, 10);
			for (int num81 = 0; num81 < 15; num81++)
			{
				Dust* ptr13 = Main.DustSet.NewDust(27, ref XYWH, (double)Velocity.X * -0.2, (double)Velocity.Y * -0.2, 100, default, 2.0);
				if (ptr13 != null)
				{
					ptr13->NoGravity = true;
					ptr13->Velocity.X *= 2f;
					ptr13->Velocity.Y *= 2f;
					ptr13 = Main.DustSet.NewDust(27, ref XYWH, (double)Velocity.X * -0.2, (double)Velocity.Y * -0.2, 100);
					if (ptr13 != null)
					{
						ptr13->Velocity.X *= 2f;
						ptr13->Velocity.Y *= 2f;
						continue;
					}
					break;
				}
				break;
			}
			return;
		}
		case EntityID.NPCID.HORNET:
		case EntityID.NPCID.DRAGON_HORNET:
		{
			if (Life > 0)
			{
				for (int num57 = (int)(dmg / (double)LifeMax * 80.0); num57 > 0; num57--)
				{
					Main.DustSet.NewDust(18, ref XYWH, hitDirection, -1.0, Alpha, Colour, Scale);
				}
				return;
			}
			for (int num58 = 0; num58 < 42; num58++)
			{
				Main.DustSet.NewDust(18, ref XYWH, hitDirection, -2.0, Alpha, Colour, Scale);
			}
			int num59 = ((Type == 42) ? 70 : 169);
			Gore.NewGore(Position, Velocity, num59, Scale);
			Vector2 position = Position;
			Vector2 velocity = Velocity;
			int num60 = num59 + 1;
			num59 = num60;
			Gore.NewGore(position, velocity, num60, Scale);
			return;
		}
		case EntityID.NPCID.MAN_EATER:
		case EntityID.NPCID.SNATCHER:
		case EntityID.NPCID.DRAGON_SNATCHER:
		{
			if (Life > 0)
			{
				for (int num36 = (int)(dmg / (double)LifeMax * 80.0); num36 > 0; num36--)
				{
					Main.DustSet.NewDust(40, ref XYWH, hitDirection, -1.0, Alpha, Colour, 1.2);
				}
				return;
			}
			for (int num37 = 0; num37 < 42; num37++)
			{
				Main.DustSet.NewDust(40, ref XYWH, hitDirection, -2.0, Alpha, Colour, 1.2);
			}
			Gore.NewGore(Position, Velocity, 72);
			Gore.NewGore(Position, Velocity, 72);
			return;
		}
		case EntityID.NPCID.HARPY:
		{
			if (Life > 0)
			{
				for (int num19 = (int)(dmg / (double)LifeMax * 80.0); num19 > 0; num19--)
				{
					Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
				}
				return;
			}
			for (int num20 = 0; num20 < 42; num20++)
			{
				Main.DustSet.NewDust(5, ref XYWH, hitDirection << 1, -2.0);
			}
			Gore.NewGore(Position, Velocity, 80);
			Gore.NewGore(Position, Velocity, 81);
			return;
		}
		}
		if (Type != 62 && Type != 165 && Type != 66)
		{
			return;
		}
		if (Life > 0)
		{
			for (int num160 = (int)(dmg / (double)LifeMax * 80.0); num160 > 0; num160--)
			{
				Main.DustSet.NewDust(5, ref XYWH, hitDirection, -1.0);
			}
			return;
		}
		for (int num161 = 0; num161 < 42; num161++)
		{
			Main.DustSet.NewDust(5, ref XYWH, hitDirection << 1, -2.0);
		}
		if (Type == 165)
		{
			Gore.NewGore(Position, Velocity, 171);
			Gore.NewGore(Position, Velocity, 0);
			Gore.NewGore(Position, Velocity, 0);
		}
		else
		{
			Gore.NewGore(Position, Velocity, 93);
			Gore.NewGore(Position, Velocity, 94);
			Gore.NewGore(Position, Velocity, 94);
		}
	}

	public static bool AnyNPCs(int Type)
	{
		for (int num = 195; num >= 0; num--)
		{
			if (Main.NPCSet[num].Type == Type && Main.NPCSet[num].Active != 0)
			{
				return true;
			}
		}
		return false;
	}

	public static bool AnyNPCs(int Type1, int Type2)
	{
		for (int num = 195; num >= 0; num--)
		{
			if (Main.NPCSet[num].Active != 0 && (Main.NPCSet[num].Type == Type1 || Main.NPCSet[num].Type == Type2))
			{
				return true;
			}
		}
		return false;
	}

	public static void SpawnSkeletron()
	{
		int num = -1;
		for (int i = 0; i < 196; i++)
		{
			if (Main.NPCSet[i].Active != 0)
			{
				if (Main.NPCSet[i].Type == 35)
				{
					return;
				}
				if (Main.NPCSet[i].Type == 37)
				{
					num = i;
					break;
				}
			}
		}
		if (num >= 0)
		{
			Main.NPCSet[num].AI3 = 1f;
			int num2 = NewNPC(Main.NPCSet[num].XYWH.X + (Main.NPCSet[num].Width >> 1), Main.NPCSet[num].XYWH.Y + (Main.NPCSet[num].Height >> 1), 35);
			Main.NPCSet[num2].ShouldNetUpdate = true;
			NetMessage.CreateMessage1(23, num);
			NetMessage.SendMessage();
			NetMessage.SendText("Skeletron", 16, 175, 75, 255, -1);
		}
	}

	public static bool NearSpikeBall(int x, int y)
	{
		Rectangle rectangle = new Rectangle(x * 16 - 300, y * 16 - 300, 600, 600);
		for (int i = 0; i < 196; i++)
		{
			if (Main.NPCSet[i].AIStyle == 20 && Main.NPCSet[i].Active != 0)
			{
				Rectangle value = new Rectangle((int)Main.NPCSet[i].AI1, (int)Main.NPCSet[i].AI2, 20, 20);
				if (rectangle.Intersects(value))
				{
					return true;
				}
			}
		}
		return false;
	}

	public void AddBuff(int BuffType, int time, bool quiet = false)
	{
		if (BuffImmune[BuffType])
		{
			return;
		}
		if (!quiet)
		{
			if (Main.NetMode == 1)
			{
				NetMessage.CreateMessage3(53, WhoAmI, BuffType, time);
			}
			else
			{
				NetMessage.CreateMessage1(54, WhoAmI);
			}
			NetMessage.SendMessage();
		}
		for (int i = 0; i < 5; i++)
		{
			if (ActiveBuffs[i].Type == BuffType)
			{
				if (ActiveBuffs[i].Time < time)
				{
					ActiveBuffs[i].Time = (ushort)time;
				}
				return;
			}
		}
		int num = -1;
		do
		{
			int num2 = -1;
			for (int j = 0; j < 5; j++)
			{
				if (!ActiveBuffs[j].IsDebuff())
				{
					num2 = j;
					break;
				}
			}
			if (num2 == -1)
			{
				return;
			}
			for (int k = num2; k < 5; k++)
			{
				if (ActiveBuffs[k].Type == 0)
				{
					num = k;
					break;
				}
			}
			if (num == -1)
			{
				DelBuff(num2);
			}
		}
		while (num == -1);
		ActiveBuffs[num].Type = (ushort)BuffType;
		ActiveBuffs[num].Time = (ushort)time;
	}

	public void DelBuff(int b)
	{
		ActiveBuffs[b].Time = 0;
		ActiveBuffs[b].Type = 0;
		for (int i = 0; i < 4; i++)
		{
			if (ActiveBuffs[i].Time == 0 || ActiveBuffs[i].Type == 0)
			{
				for (int j = i + 1; j < 5; j++)
				{
					ref Buff reference = ref ActiveBuffs[j - 1];
					reference = ActiveBuffs[j];
					ActiveBuffs[j].Time = 0;
					ActiveBuffs[j].Type = 0;
				}
			}
		}
		if (Main.NetMode == 2)
		{
			NetMessage.CreateMessage1(54, WhoAmI);
			NetMessage.SendMessage();
		}
	}

	private unsafe void FireEffect(int particleType)
	{
		if (Main.Rand.Next(4) < 2)
		{
			Dust* ptr = Main.DustSet.NewDust((int)Position.X - 2, (int)Position.Y - 2, Width + 4, Height + 4, particleType, Velocity.X * 0.4f, Velocity.Y * 0.4f, 100, default, 3.5);
			if (ptr != null)
			{
				ptr->NoGravity = true;
				ptr->Velocity.X *= 1.8f;
				ptr->Velocity.Y *= 1.8f;
				ptr->Velocity.Y -= 0.5f;
				if (Main.Rand.Next(4) == 0)
				{
					ptr->NoGravity = false;
					ptr->Scale *= 0.5f;
				}
			}
		}
		Lighting.AddLight((int)Position.X >> 4, ((int)Position.Y >> 4) + 1, new Vector3(1f, 0.3f, 0.1f));
	}

	public unsafe void UpdateNPC(int i)
	{
		WhoAmI = (short)i;
		if (XYWH.X <= 0 || XYWH.X + Width >= Main.RightWorld || XYWH.Y <= 0 || XYWH.Y + Height >= Main.BottomWorld)
		{
			Active = 0;
			return;
		}
		int num = 0;
		bool flag = false;
		IsPoisoned = false;
		IsConfused = false;
		for (int j = 0; j < 5; j++)
		{
			if (ActiveBuffs[j].Type <= 0 || ActiveBuffs[j].Time <= 0)
			{
				continue;
			}
			switch (ActiveBuffs[j].Type)
			{
			case 31:
				IsConfused = true;
				break;
			case 20:
				IsPoisoned = true;
				if (num > -4)
				{
					num = -4;
				}
				if (Main.Rand.Next(30) == 0)
				{
					Dust* ptr2 = Main.DustSet.NewDust(46, ref XYWH, 0.0, 0.0, 120, default, 0.2);
					if (ptr2 != null)
					{
						ptr2->NoGravity = true;
						ptr2->FadeIn = 1.9f;
					}
				}
				break;
			case 24:
				flag = true;
				if (num > -8)
				{
					num = -8;
				}
				FireEffect(6);
				break;
			case 39:
				if (num > -12)
				{
					num = -12;
				}
				FireEffect(75);
				break;
			case 30:
				if (num > -16)
				{
					num = -16;
				}
				if (Main.Rand.Next(30) == 0)
				{
					Dust* ptr = Main.DustSet.NewDust(5, ref XYWH);
					if (ptr != null)
					{
						ptr->Velocity.Y += 0.5f;
						ptr->Velocity *= 0.25f;
					}
				}
				break;
			}
		}
		if (Main.NetMode != 1)
		{
			for (int k = 0; k < 5; k++)
			{
				if (ActiveBuffs[k].Type > 0 && ActiveBuffs[k].Time == 0)
				{
					DelBuff(k);
				}
			}
		}
		if (!DontTakeDamage)
		{
			LifeRegenCount += num;
			while (LifeRegenCount <= -120)
			{
				LifeRegenCount += 120;
				int num2 = WhoAmI;
				if (RealLife >= 0)
				{
					num2 = RealLife;
				}
				if (--Main.NPCSet[num2].Life <= 0)
				{
					Main.NPCSet[num2].Life = 1;
					if (Main.NetMode != 1)
					{
						Main.NPCSet[num2].StrikeNPC(9999, 0f, 0);
						NetMessage.SendNpcHurt(num2, 9999, 0.0, 0);
					}
				}
			}
		}
		if (Main.NetMode != 1 && Main.GameTime.IsBloodMoon)
		{
			if (Type == 46)
			{
				Transform(47);
			}
			else if (Type == 55)
			{
				Transform(57);
			}
		}
		float num3 = 10f;
		float num4 = 0.3f;
		float num5 = (float)Main.MaxTilesX / 4200f;
		num5 *= num5;
		float num6 = (this.Position.Y * 0.0625f - (60f + 10f * num5)) / (float)(Main.WorldSurface / 6);
		if ((double)num6 < 0.25)
		{
			num6 = 0.25f;
		}
		else if (num6 > 1f)
		{
			num6 = 1f;
		}
		num4 *= num6;
		if (IsWet)
		{
			num4 = 0.2f;
			num3 = 7f;
		}
		if (SoundDelay > 0)
		{
			SoundDelay--;
		}
		if (Life <= 0)
		{
			Active = 0;
			return;
		}
		OldTarget = Target;
		OldDirection = Direction;
		OldDirectionY = DirectionY;
		try
		{
			switch ((EntityID.NPCStyleID)AIStyle)
			{
			case EntityID.NPCStyleID.BOUND:
				BoundAI();
				break;
			case EntityID.NPCStyleID.SLIME:
				SlimeAI();
				break;
			case EntityID.NPCStyleID.DEMON_EYE:
				FloatingEyeballAI();
				break;
			case EntityID.NPCStyleID.WALKER:
				WalkAI();
				break;
			case EntityID.NPCStyleID.EYE_OF_CTHULHU:
				EyeOfCthulhuAI();
				break;
			case EntityID.NPCStyleID.AGGRESSIVE_FLYER:
				AggressiveFlyerAI();
				break;
			case EntityID.NPCStyleID.WORM:
				WormAI();
				break;
			case EntityID.NPCStyleID.TOWNSFOLK:
				TownsfolkAI();
				break;
			case EntityID.NPCStyleID.SORCERER:
				SorcererAI();
				break;
			case EntityID.NPCStyleID.SPHERE:
				SphereAI();
				break;
			case EntityID.NPCStyleID.CURSED_SKULL:
				SkullHeadAI();
				break;
			case EntityID.NPCStyleID.SKELETRON_HEAD:
				SkeletronAI();
				break;
			case EntityID.NPCStyleID.SKELETRON_HAND:
				SkeletronHandAI();
				break;
			case EntityID.NPCStyleID.PLANT:
				PlantAI();
				break;
			case EntityID.NPCStyleID.FLYER:
				FlyerAI();
				break;
			case EntityID.NPCStyleID.KING_SLIME:
				KingSlimeAI();
				break;
			case EntityID.NPCStyleID.FISH:
				FishAI();
				break;
			case EntityID.NPCStyleID.VULTURE:
				VultureAI();
				break;
			case EntityID.NPCStyleID.JELLYFISH:
				JellyfishAI();
				break;
			case EntityID.NPCStyleID.ANTLION:
				AntlionAI();
				break;
			case EntityID.NPCStyleID.SPIKE_BALL:
				SpinningSpikeballAI();
				break;
			case EntityID.NPCStyleID.BLAZE_WHEEL:
				GravityDiskAI();
				break;
			case EntityID.NPCStyleID.HOVERING:
				MoreFlyerAI();
				break;
			case EntityID.NPCStyleID.ENCHANTED_WEAPON:
				EnchantedWeaponAI();
				break;
			case EntityID.NPCStyleID.BIRD:
				BirdAI();
				break;
			case EntityID.NPCStyleID.MIMIC:
				MimicAI();
				break;
			case EntityID.NPCStyleID.UNICORN:
				UnicornAI();
				break;
			case EntityID.NPCStyleID.WALL_OF_FLESH_MOUTH:
				WallOfFleshMouthAI();
				break;
			case EntityID.NPCStyleID.WALL_OF_FLESH_EYES:
				WallOfFleshEyesAI();
				break;
			case EntityID.NPCStyleID.WALL_OF_FLESH_TENTACLE:
				WallOfFleshTentacleAI();
				break;
			case EntityID.NPCStyleID.RETINAZER:
				RetinazerAI();
				break;
			case EntityID.NPCStyleID.SPAZMATISM:
				SpazmatismAI();
				break;
			case EntityID.NPCStyleID.SKELETRON_PRIME_HEAD:
				SkeletronPrimeAI();
				break;
			case EntityID.NPCStyleID.SKELETRON_PRIME_SAW:
				SkeletronPrimeSawHand();
				break;
			case EntityID.NPCStyleID.SKELETRON_PRIME_VICE:
				SkeletronPrimeViceHand();
				break;
			case EntityID.NPCStyleID.SKELETRON_PRIME_CANNON:
				SkeletronPrimeCannonHand();
				break;
			case EntityID.NPCStyleID.SKELETRON_PRIME_LASER:
				SkeletronPrimeLaserHand();
				break;
			case EntityID.NPCStyleID.DESTROYER:
				DestroyerAI();
				break;
			case EntityID.NPCStyleID.SNOWMAN:
				SnowmanAI();
				break;
			case EntityID.NPCStyleID.OCRAM:
				OcramAI();
				break;
			}
		}
		catch (Exception)
		{
			Active = 0;
			return;
		}
		if (Type == 44 || Type == 149)
		{
			Lighting.AddLight(XYWH.X + (Width >> 1) >> 4, XYWH.Y + 4 >> 4, new Vector3(0.9f, 0.75f, 0.5f));
		}
		for (int l = 0; l < 9; l++)
		{
			if (Immunities[l] > 0)
			{
				Immunities[l]--;
			}
		}
		if (!HasNoGravity)
		{
			Velocity.Y += num4;
			if (Velocity.Y > num3)
			{
				Velocity.Y = num3;
			}
		}
		if (Velocity.X < 0.005f && Velocity.X > -0.005f)
		{
			Velocity.X = 0f;
		}
		if (Main.NetMode != 1 && Type != 37 && (IsFriendly || Type == 46 || Type == 55 || Type == 74))
		{
			if (Life < LifeMax)
			{
				FriendlyRegen++;
				if (FriendlyRegen > 300)
				{
					FriendlyRegen = 0;
					Life++;
					ShouldNetUpdate = true;
				}
			}
			if (Immunities[8] == 0)
			{
				for (int m = 0; m < 196; m++)
				{
					if (Main.NPCSet[m].Active != 0 && !Main.NPCSet[m].IsFriendly && Main.NPCSet[m].Damage > 0 && XYWH.Intersects(Main.NPCSet[m].XYWH))
					{
						int damage = Main.NPCSet[m].Damage;
						int num7 = 6;
						int num8 = 1;
						if (Main.NPCSet[m].XYWH.X + (Main.NPCSet[m].Width >> 1) > XYWH.X + (Width >> 1))
						{
							num8 = -1;
						}
						Main.NPCSet[i].StrikeNPC(damage, num7, num8);
						NetMessage.SendNpcHurt(i, damage, num7, num8);
						ShouldNetUpdate = true;
						Immunities[8] = 30;
					}
				}
			}
		}
		if (!HasNoTileCollide)
		{
			bool flag2 = Collision.LavaCollision(ref this.Position, Width, Height);
			if (flag2)
			{
				LavaWet = true;
				if (!IsLavaImmune && !DontTakeDamage && Main.NetMode != 1 && Immunities[8] == 0)
				{
					AddBuff(24, 420);
					Immunities[8] = 30;
					StrikeNPC(50, 0f, 0);
					NetMessage.SendNpcHurt(WhoAmI, 50, 0.0, 0);
					if (Type == 22 && Life <= 0)
					{
						UI.SetTriggerStateForAll(Trigger.OldFashioned);
					}
				}
			}
			bool flag3 = false;
			if (Type == 72)
			{
				flag3 = false;
				WetCount = 0;
				flag2 = false;
			}
			else
			{
				flag3 = Collision.WetCollision(ref this.Position, Width, Height);
			}
			if (flag3)
			{
				if (flag && !LavaWet && Main.NetMode != 1)
				{
					for (int n = 0; n < 5; n++)
					{
						if (ActiveBuffs[n].Type == 24)
						{
							DelBuff(n);
							break;
						}
					}
				}
				if (!IsWet && WetCount == 0)
				{
					WetCount = 10;
					if (!flag2)
					{
						for (int num9 = 0; num9 < 24; num9++)
						{
							Dust* ptr3 = Main.DustSet.NewDust(XYWH.X - 6, XYWH.Y + (Height >> 1) - 8, Width + 12, 24, 33);
							if (ptr3 == null)
							{
								break;
							}
							ptr3->Velocity.Y -= 4f;
							ptr3->Velocity.X *= 2.5f;
							ptr3->Scale = 1.3f;
							ptr3->Alpha = 100;
							ptr3->NoGravity = true;
						}
						if (Type != 1 && Type != 59 && !HasNoGravity)
						{
							Main.PlaySound(19, XYWH.X, XYWH.Y, 0);
						}
					}
					else
					{
						for (int num10 = 0; num10 < 7; num10++)
						{
							Dust* ptr4 = Main.DustSet.NewDust(XYWH.X - 6, XYWH.Y + (Height >> 1) - 8, Width + 12, 24, 35);
							if (ptr4 == null)
							{
								break;
							}
							ptr4->Velocity.Y -= 1.5f;
							ptr4->Velocity.X *= 2.5f;
							ptr4->Scale = 1.3f;
							ptr4->Alpha = 100;
							ptr4->NoGravity = true;
						}
						if (Type != 1 && Type != 59 && !HasNoGravity)
						{
							Main.PlaySound(19, XYWH.X, XYWH.Y);
						}
					}
				}
				IsWet = true;
			}
			else if (IsWet)
			{
				Velocity.X *= 0.5f;
				IsWet = false;
				if (WetCount == 0)
				{
					WetCount = 10;
					if (!LavaWet)
					{
						for (int num11 = 0; num11 < 24; num11++)
						{
							Dust* ptr5 = Main.DustSet.NewDust(XYWH.X - 6, XYWH.Y + (Height >> 1) - 8, Width + 12, 24, 33);
							if (ptr5 == null)
							{
								break;
							}
							ptr5->Velocity.Y -= 4f;
							ptr5->Velocity.X *= 2.5f;
							ptr5->Scale = 1.3f;
							ptr5->Alpha = 100;
							ptr5->NoGravity = true;
						}
						if (Type != 1 && Type != 59 && !HasNoGravity)
						{
							Main.PlaySound(19, XYWH.X, XYWH.Y, 0);
						}
					}
					else
					{
						for (int num12 = 0; num12 < 7; num12++)
						{
							Dust* ptr6 = Main.DustSet.NewDust(XYWH.X - 6, XYWH.Y + (Height >> 1) - 8, Width + 12, 24, 35);
							if (ptr6 == null)
							{
								break;
							}
							ptr6->Velocity.Y -= 1.5f;
							ptr6->Velocity.X *= 2.5f;
							ptr6->Scale = 1.3f;
							ptr6->Alpha = 100;
							ptr6->NoGravity = true;
						}
						if (Type != 1 && Type != 59 && !HasNoGravity)
						{
							Main.PlaySound(19, XYWH.X, XYWH.Y);
						}
					}
				}
			}
			if (!IsWet)
			{
				LavaWet = false;
			}
			if (WetCount > 0)
			{
				WetCount--;
			}
			bool flag4 = false;
			if (AIStyle == 10)
			{
				flag4 = true;
			}
			else if (AIStyle == 14)
			{
				flag4 = true;
			}
			else if (AIStyle == 3 && DirectionY == 1)
			{
				flag4 = true;
			}
			OldVelocity = Velocity;
			HasXCollision = false;
			HasYCollision = false;
			if (IsWet)
			{
				Vector2 velocity = Velocity;
				Collision.TileCollision(ref this.Position, ref Velocity, Width, Height, flag4, flag4);
				if (Collision.UpCol)
				{
					Velocity.Y = 0.01f;
				}
				Vector2 velocity2 = Velocity;
				velocity2.X *= 0.5f;
				velocity2.Y *= 0.5f;
				if (Velocity.X != velocity.X)
				{
					velocity2.X = Velocity.X;
					HasXCollision = true;
				}
				if (Velocity.Y != velocity.Y)
				{
					velocity2.Y = Velocity.Y;
					HasYCollision = true;
				}
				OldPosition = this.Position;
				this.Position.X += velocity2.X;
				this.Position.Y += velocity2.Y;
			}
			else
			{
				if (Type == 72)
				{
					Vector2 Position = new Vector2(this.Position.X + (float)(Width >> 1), this.Position.Y + (float)(Height >> 1));
					int num13 = 12;
					int num14 = 12;
					Position.X -= num13 >> 1;
					Position.Y -= num14 >> 1;
					Collision.TileCollision(ref Position, ref Velocity, num13, num14, CanFallThrough: true, CanAllowFall: true);
				}
				else
				{
					Collision.TileCollision(ref this.Position, ref Velocity, Width, Height, flag4, flag4);
				}
				if (Collision.UpCol)
				{
					Velocity.Y = 0.01f;
				}
				if (OldVelocity.X != Velocity.X)
				{
					HasXCollision = true;
				}
				if (OldVelocity.Y != Velocity.Y)
				{
					HasYCollision = true;
				}
				OldPosition = this.Position;
				this.Position.X += Velocity.X;
				this.Position.Y += Velocity.Y;
			}
		}
		else
		{
			OldPosition = this.Position;
			this.Position.X += Velocity.X;
			this.Position.Y += Velocity.Y;
		}
		XYWH.X = (int)this.Position.X;
		XYWH.Y = (int)this.Position.Y;
		if (Main.NetMode != 1 && !HasNoTileCollide && LifeMax > 1 && Collision.SwitchTiles(this.Position, Width, Height, OldPosition) && Type == 46)
		{
			AI0 = 1f;
			AI1 = 400f;
			AI2 = 0f;
		}
		if (Active == 0)
		{
			ShouldNetUpdate = true;
		}
		if (Main.NetMode == 2)
		{
			if (IsTownNPC)
			{
				NetSpam = 0;
			}
			if (ShouldNetUpdate2)
			{
				ShouldNetUpdate = true;
			}
			if (Active == 0)
			{
				NetSpam = 0;
			}
			if (ShouldNetUpdate)
			{
				if (NetSpam <= 180)
				{
					NetSpam += 60;
					NetMessage.CreateMessage1(23, i);
					NetMessage.SendMessage();
					ShouldNetUpdate2 = false;
				}
				else
				{
					ShouldNetUpdate2 = true;
				}
			}
			if (NetSpam > 0)
			{
				NetSpam--;
			}
			if (Active != 0 && IsTownNPC && GetHeadTextureID() != -1)
			{
				if (IsHomeless != WasHomeless || HomeTileX != OldHomeTileX || HomeTileY != OldHomeTileY)
				{
					NetMessage.CreateMessage4(60, i, Main.NPCSet[i].HomeTileX, Main.NPCSet[i].HomeTileY, IsHomeless ? 1 : 0);
					NetMessage.SendMessage();
				}
				WasHomeless = IsHomeless;
				OldHomeTileX = HomeTileX;
				OldHomeTileY = HomeTileY;
			}
		}
		FindFrame();
		CheckActive();
		ShouldNetUpdate = false;
		WasJustHit = false;
		if (Type == 120 || Type == 154 || Type == 137 || Type == 138)
		{
			for (int num15 = OldPos.Length - 1; num15 > 0; num15--)
			{
				ref Vector2 reference = ref OldPos[num15];
				reference = OldPos[num15 - 1];
				Lighting.AddLight(XYWH.X >> 4, XYWH.Y >> 4, new Vector3(0.3f, 0f, 0.2f));
			}
			ref Vector2 reference2 = ref OldPos[0];
			reference2 = this.Position;
		}
		else if (Type == 94 || (Type >= 125 && Type <= 131) || Type == 139 || Type == 140)
		{
			for (int num16 = OldPos.Length - 1; num16 > 0; num16--)
			{
				ref Vector2 reference3 = ref OldPos[num16];
				reference3 = OldPos[num16 - 1];
			}
			ref Vector2 reference4 = ref OldPos[0];
			reference4 = this.Position;
		}
	}

	public Color GetAlpha(Color newColor)
	{
		float num = (float)(255 - Alpha) / 255f;
		int num2 = (int)((float)(int)newColor.R * num);
		int num3 = (int)((float)(int)newColor.G * num);
		int num4 = (int)((float)(int)newColor.B * num);
		int num5 = newColor.A - Alpha;
		if (Type == 25 || Type == 30 || Type == 33 || Type == 59 || Type == 60)
		{
			return new Color(200, 200, 200, 0);
		}
		if (Type == 72)
		{
			num2 = newColor.R;
			num3 = newColor.G;
			num4 = newColor.B;
		}
		else if (Type == 64 || Type == 63 || Type == 75 || Type == 103)
		{
			num2 = (int)((double)(int)newColor.R * 1.5);
			num3 = (int)((double)(int)newColor.G * 1.5);
			num4 = (int)((double)(int)newColor.B * 1.5);
			if (num2 > 255)
			{
				num2 = 255;
			}
			if (num3 > 255)
			{
				num3 = 255;
			}
			if (num4 > 255)
			{
				num4 = 255;
			}
		}
		if (num5 < 0)
		{
			num5 = 0;
		}
		if (num5 > 255)
		{
			num5 = 255;
		}
		return new Color(num2, num3, num4, num5);
	}

	public Color GetColor(Color newColor)
	{
		int num = Colour.R - (255 - newColor.R);
		int num2 = Colour.G - (255 - newColor.G);
		int num3 = Colour.B - (255 - newColor.B);
		int num4 = Colour.A - (255 - newColor.A);
		if (num < 0)
		{
			num = 0;
		}
		if (num > 255)
		{
			num = 255;
		}
		if (num2 < 0)
		{
			num2 = 0;
		}
		if (num2 > 255)
		{
			num2 = 255;
		}
		if (num3 < 0)
		{
			num3 = 0;
		}
		if (num3 > 255)
		{
			num3 = 255;
		}
		if (num4 < 0)
		{
			num4 = 0;
		}
		if (num4 > 255)
		{
			num4 = 255;
		}
		return new Color(num, num2, num3, num4);
	}

	public string GetChat(Player player)
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		bool flag6 = false;
		bool flag7 = false;
		bool flag8 = false;
		bool flag9 = false;
		for (int i = 0; i < 196; i++)
		{
			if (Main.NPCSet[i].Active != 0)
			{
				if (Main.NPCSet[i].Type == 17)
				{
					flag = true;
				}
				else if (Main.NPCSet[i].Type == 18)
				{
					flag2 = true;
				}
				else if (Main.NPCSet[i].Type == 19)
				{
					flag3 = true;
				}
				else if (Main.NPCSet[i].Type == 20)
				{
					flag4 = true;
				}
				else if (Main.NPCSet[i].Type == 37)
				{
					flag5 = true;
				}
				else if (Main.NPCSet[i].Type == 38)
				{
					flag6 = true;
				}
				else if (Main.NPCSet[i].Type == 124)
				{
					flag7 = true;
				}
				else if (Main.NPCSet[i].Type == 107)
				{
					flag8 = true;
				}
				else if (Main.NPCSet[i].Type == 22)
				{
					flag9 = true;
				}
			}
		}
		string result = "";
		if (Type == 17)
		{
			string text;
			if (!HasDownedBoss1 && Main.Rand.Next(3) == 0)
			{
				if (player.StatLifeMax < 200)
				{
					text = Lang.NPCDialog(player, 1);
				}
				else
				{
					text = ((player.StatDefense > 10) ? Lang.NPCDialog(player, 3) : Lang.NPCDialog(player, 2));
				}
			}
			else
			{
				string text5;
				if (Main.GameTime.DayTime)
				{
					string text3;
					if ((double)Main.GameTime.WorldTime < 16200.0)
					{
						int num = Main.Rand.Next(3);
						if (1 == 0)
						{
						}
						string text2 = num switch
						{
							0 => Lang.NPCDialog(player, 4), 
							1 => Lang.NPCDialog(player, 5), 
							_ => Lang.NPCDialog(player, 6), 
						};
						if (1 == 0)
						{
						}
						text3 = text2;
					}
					else
					{
						string text4;
						if ((double)Main.GameTime.WorldTime > 37800.0)
						{
							int num2 = Main.Rand.Next(3);
							if (1 == 0)
							{
							}
							string text2 = num2 switch
							{
								0 => Lang.NPCDialog(player, 7), 
								1 => Lang.NPCDialog(player, 8), 
								_ => Lang.NPCDialog(player, 9), 
							};
							if (1 == 0)
							{
							}
							text4 = text2;
						}
						else
						{
							int num3 = Main.Rand.Next(3);
							if (1 == 0)
							{
							}
							string text2 = num3 switch
							{
								0 => Lang.NPCDialog(player, 10), 
								1 => Lang.NPCDialog(player, 11), 
								_ => Lang.NPCDialog(player, 12), 
							};
							if (1 == 0)
							{
							}
							text4 = text2;
						}
						text3 = text4;
					}
					text5 = text3;
				}
				else
				{
					string text7;
					if (Main.GameTime.IsBloodMoon)
					{
						string text6;
						if ((flag2 & flag7) && Main.Rand.Next(3) == 0)
						{
							text6 = Lang.NPCDialog(player, 13);
						}
						else
						{
							int num4 = Main.Rand.Next(4);
							if (1 == 0)
							{
							}
							string text2 = num4 switch
							{
								0 => Lang.NPCDialog(player, 14), 
								1 => Lang.NPCDialog(player, 15), 
								2 => Lang.NPCDialog(player, 16), 
								_ => Lang.NPCDialog(player, 17), 
							};
							if (1 == 0)
							{
							}
							text6 = text2;
						}
						text7 = text6;
					}
					else
					{
						string text8;
						if ((double)Main.GameTime.WorldTime < 9720.0)
						{
							text8 = ((Main.Rand.Next(2) != 0) ? Lang.NPCDialog(player, 19) : Lang.NPCDialog(player, 18));
						}
						else
						{
							string text9;
							if ((double)Main.GameTime.WorldTime > 22680.0)
							{
								text9 = ((Main.Rand.Next(2) != 0) ? Lang.NPCDialog(player, 21) : Lang.NPCDialog(player, 20));
							}
							else
							{
								int num5 = Main.Rand.Next(3);
								if (1 == 0)
								{
								}
								string text2 = num5 switch
								{
									0 => Lang.NPCDialog(player, 22), 
									1 => Lang.NPCDialog(player, 23), 
									_ => Lang.NPCDialog(player, 24), 
								};
								if (1 == 0)
								{
								}
								text9 = text2;
							}
							text8 = text9;
						}
						text7 = text8;
					}
					text5 = text7;
				}
				text = text5;
			}
			result = text;
		}
		else if (Type == 18)
		{
			string text11;
			if (Main.GameTime.IsBloodMoon)
			{
				string text10;
				if ((double)player.statLife < (double)player.StatLifeMax * 0.66)
				{
					int num6 = Main.Rand.Next(3);
					if (1 == 0)
					{
					}
					string text2 = num6 switch
					{
						0 => Lang.NPCDialog(player, 25), 
						1 => Lang.NPCDialog(player, 26), 
						_ => Lang.NPCDialog(player, 27), 
					};
					if (1 == 0)
					{
					}
					text10 = text2;
				}
				else
				{
					int num7 = Main.Rand.Next(4);
					if (1 == 0)
					{
					}
					string text2 = num7 switch
					{
						0 => Lang.NPCDialog(player, 28), 
						1 => Lang.NPCDialog(player, 29), 
						2 => Lang.NPCDialog(player, 30), 
						_ => Lang.NPCDialog(player, 31), 
					};
					if (1 == 0)
					{
					}
					text10 = text2;
				}
				text11 = text10;
			}
			else
			{
				string text12;
				if (Main.Rand.Next(3) == 0 && !HasDownedBoss3)
				{
					text12 = Lang.NPCDialog(player, 32);
				}
				else
				{
					string text13;
					if (flag6 && Main.Rand.Next(4) == 0)
					{
						text13 = Lang.NPCDialog(player, 33);
					}
					else
					{
						string text14;
						if (flag3 && Main.Rand.Next(4) == 0)
						{
							text14 = Lang.NPCDialog(player, 34);
						}
						else
						{
							string text15;
							if (flag9 && Main.Rand.Next(4) == 0)
							{
								text15 = Lang.NPCDialog(player, 35);
							}
							else
							{
								string text16;
								if ((double)player.statLife < (double)player.StatLifeMax * 0.33)
								{
									int num8 = Main.Rand.Next(5);
									if (1 == 0)
									{
									}
									string text2 = num8 switch
									{
										0 => Lang.NPCDialog(player, 36), 
										1 => Lang.NPCDialog(player, 37), 
										2 => Lang.NPCDialog(player, 38), 
										3 => Lang.NPCDialog(player, 39), 
										_ => Lang.NPCDialog(player, 40), 
									};
									if (1 == 0)
									{
									}
									text16 = text2;
								}
								else
								{
									string text17;
									if ((double)player.statLife < (double)player.StatLifeMax * 0.66)
									{
										int num9 = Main.Rand.Next(7);
										if (1 == 0)
										{
										}
										string text2 = num9 switch
										{
											0 => Lang.NPCDialog(player, 41), 
											1 => Lang.NPCDialog(player, 42), 
											2 => Lang.NPCDialog(player, 43), 
											3 => Lang.NPCDialog(player, 44), 
											4 => Lang.NPCDialog(player, 45), 
											5 => Lang.NPCDialog(player, 46), 
											_ => Lang.NPCDialog(player, 47), 
										};
										if (1 == 0)
										{
										}
										text17 = text2;
									}
									else
									{
										int num10 = Main.Rand.Next(4);
										if (1 == 0)
										{
										}
										string text2 = num10 switch
										{
											0 => Lang.NPCDialog(player, 48), 
											1 => Lang.NPCDialog(player, 49), 
											2 => Lang.NPCDialog(player, 50), 
											_ => Lang.NPCDialog(player, 51), 
										};
										if (1 == 0)
										{
										}
										text17 = text2;
									}
									text16 = text17;
								}
								text15 = text16;
							}
							text14 = text15;
						}
						text13 = text14;
					}
					text12 = text13;
				}
				text11 = text12;
			}
			result = text11;
		}
		else if (Type == 19)
		{
			string text18;
			if (HasDownedBoss3 && !Main.InHardMode)
			{
				text18 = Lang.NPCDialog(player, 58);
			}
			else
			{
				string text19;
				if (flag2 && Main.Rand.Next(5) == 0)
				{
					text19 = Lang.NPCDialog(player, 59);
				}
				else
				{
					string text20;
					if (flag2 && Main.Rand.Next(5) == 0)
					{
						text20 = Lang.NPCDialog(player, 60);
					}
					else
					{
						string text21;
						if (flag4 && Main.Rand.Next(5) == 0)
						{
							text21 = Lang.NPCDialog(player, 61);
						}
						else
						{
							string text22;
							if (flag6 && Main.Rand.Next(5) == 0)
							{
								text22 = Lang.NPCDialog(player, 62);
							}
							else
							{
								string text23;
								if (flag6 && Main.Rand.Next(5) == 0)
								{
									text23 = Lang.NPCDialog(player, 63);
								}
								else
								{
									string text24;
									if (Main.GameTime.IsBloodMoon)
									{
										text24 = ((Main.Rand.Next(2) != 0) ? Lang.NPCDialog(player, 65) : Lang.NPCDialog(player, 64));
									}
									else
									{
										int num11 = Main.Rand.Next(3);
										if (1 == 0)
										{
										}
										string text2 = num11 switch
										{
											0 => Lang.NPCDialog(player, 66), 
											1 => Lang.NPCDialog(player, 67), 
											_ => Lang.NPCDialog(player, 68), 
										};
										if (1 == 0)
										{
										}
										text24 = text2;
									}
									text23 = text24;
								}
								text22 = text23;
							}
							text21 = text22;
						}
						text20 = text21;
					}
					text19 = text20;
				}
				text18 = text19;
			}
			result = text18;
		}
		else if (Type == 20)
		{
			string text25;
			if (!HasDownedBoss2 && Main.Rand.Next(3) == 0)
			{
				text25 = Lang.NPCDialog(player, 69);
			}
			else
			{
				string text26;
				if (flag3 && Main.Rand.Next(4) == 0)
				{
					text26 = Lang.NPCDialog(player, 70);
				}
				else
				{
					string text27;
					if (flag && Main.Rand.Next(4) == 0)
					{
						text27 = Lang.NPCDialog(player, 71);
					}
					else
					{
						string text28;
						if (flag5 && Main.Rand.Next(4) == 0)
						{
							text28 = Lang.NPCDialog(player, 72);
						}
						else
						{
							string text29;
							if (Main.GameTime.IsBloodMoon)
							{
								int num12 = Main.Rand.Next(4);
								if (1 == 0)
								{
								}
								string text2 = num12 switch
								{
									0 => Lang.NPCDialog(player, 73), 
									1 => Lang.NPCDialog(player, 74), 
									2 => Lang.NPCDialog(player, 75), 
									_ => Lang.NPCDialog(player, 76), 
								};
								if (1 == 0)
								{
								}
								text29 = text2;
							}
							else
							{
								int num13 = Main.Rand.Next(5);
								if (1 == 0)
								{
								}
								string text2 = num13 switch
								{
									0 => Lang.NPCDialog(player, 77), 
									1 => Lang.NPCDialog(player, 78), 
									2 => Lang.NPCDialog(player, 79), 
									3 => Lang.NPCDialog(player, 80), 
									_ => Lang.NPCDialog(player, 81), 
								};
								if (1 == 0)
								{
								}
								text29 = text2;
							}
							text28 = text29;
						}
						text27 = text28;
					}
					text26 = text27;
				}
				text25 = text26;
			}
			result = text25;
		}
		else if (Type == 37)
		{
			string text30;
			if (Main.GameTime.DayTime)
			{
				int num14 = Main.Rand.Next(3);
				if (1 == 0)
				{
				}
				string text2 = num14 switch
				{
					0 => Lang.NPCDialog(player, 82), 
					1 => Lang.NPCDialog(player, 83), 
					_ => Lang.NPCDialog(player, 84), 
				};
				if (1 == 0)
				{
				}
				text30 = text2;
			}
			else
			{
				string text31;
				if (player.StatLifeMax < 300 || player.StatDefense < 10)
				{
					int num15 = Main.Rand.Next(4);
					if (1 == 0)
					{
					}
					string text2 = num15 switch
					{
						0 => Lang.NPCDialog(player, 85), 
						1 => Lang.NPCDialog(player, 86), 
						2 => Lang.NPCDialog(player, 87), 
						_ => Lang.NPCDialog(player, 88), 
					};
					if (1 == 0)
					{
					}
					text31 = text2;
				}
				else
				{
					int num16 = Main.Rand.Next(4);
					if (1 == 0)
					{
					}
					string text2 = num16 switch
					{
						0 => Lang.NPCDialog(player, 89), 
						1 => Lang.NPCDialog(player, 90), 
						2 => Lang.NPCDialog(player, 91), 
						_ => Lang.NPCDialog(player, 92), 
					};
					if (1 == 0)
					{
					}
					text31 = text2;
				}
				text30 = text31;
			}
			result = text30;
		}
		else if (Type == 38)
		{
			if (!HasDownedBoss2 && Main.Rand.Next(3) == 0)
			{
				result = Lang.NPCDialog(player, 93);
			}
			string text32;
			if (Main.GameTime.IsBloodMoon)
			{
				int num17 = Main.Rand.Next(3);
				if (1 == 0)
				{
				}
				string text2 = num17 switch
				{
					0 => Lang.NPCDialog(player, 94), 
					1 => Lang.NPCDialog(player, 95), 
					_ => Lang.NPCDialog(player, 96), 
				};
				if (1 == 0)
				{
				}
				text32 = text2;
			}
			else
			{
				string text33;
				if (flag3 && Main.Rand.Next(5) == 0)
				{
					text33 = Lang.NPCDialog(player, 97);
				}
				else
				{
					string text34;
					if (flag3 && Main.Rand.Next(5) == 0)
					{
						text34 = Lang.NPCDialog(player, 98);
					}
					else
					{
						string text35;
						if (flag2 && Main.Rand.Next(4) == 0)
						{
							text35 = Lang.NPCDialog(player, 99);
						}
						else
						{
							string text36;
							if (flag4 && Main.Rand.Next(4) == 0)
							{
								text36 = Lang.NPCDialog(player, 100);
							}
							else
							{
								string text37;
								if (!Main.GameTime.DayTime)
								{
									int num18 = Main.Rand.Next(4);
									if (1 == 0)
									{
									}
									string text2 = num18 switch
									{
										0 => Lang.NPCDialog(player, 101), 
										1 => Lang.NPCDialog(player, 102), 
										2 => Lang.NPCDialog(player, 103), 
										_ => Lang.NPCDialog(player, 104), 
									};
									if (1 == 0)
									{
									}
									text37 = text2;
								}
								else
								{
									int num19 = Main.Rand.Next(5);
									if (1 == 0)
									{
									}
									string text2 = num19 switch
									{
										0 => Lang.NPCDialog(player, 105), 
										1 => Lang.NPCDialog(player, 106), 
										2 => Lang.NPCDialog(player, 107), 
										3 => Lang.NPCDialog(player, 108), 
										_ => Lang.NPCDialog(player, 109), 
									};
									if (1 == 0)
									{
									}
									text37 = text2;
								}
								text36 = text37;
							}
							text35 = text36;
						}
						text34 = text35;
					}
					text33 = text34;
				}
				text32 = text33;
			}
			result = text32;
		}
		else if (Type == 54)
		{
			string text38;
			if (!flag7 && Main.Rand.Next(2) == 0)
			{
				text38 = Lang.NPCDialog(player, 110);
			}
			else
			{
				string text39;
				if (Main.GameTime.IsBloodMoon)
				{
					text39 = Lang.NPCDialog(player, 111);
				}
				else
				{
					string text40;
					if (flag2 && Main.Rand.Next(4) == 0)
					{
						text40 = Lang.NPCDialog(player, 112);
					}
					else
					{
						string text41;
						if (player.head == 24)
						{
							text41 = Lang.NPCDialog(player, 113);
						}
						else
						{
							int num20 = Main.Rand.Next(6);
							if (1 == 0)
							{
							}
							string text2 = num20 switch
							{
								0 => Lang.NPCDialog(player, 114), 
								1 => Lang.NPCDialog(player, 115), 
								2 => Lang.NPCDialog(player, 116), 
								3 => Lang.NPCDialog(player, 117), 
								4 => Lang.NPCDialog(player, 118), 
								_ => Lang.NPCDialog(player, 119), 
							};
							if (1 == 0)
							{
							}
							text41 = text2;
						}
						text40 = text41;
					}
					text39 = text40;
				}
				text38 = text39;
			}
			result = text38;
		}
		else if (Type == 105)
		{
			result = Lang.NPCDialog(player, 120);
		}
		else if (Type == 107)
		{
			string text42;
			if (IsHomeless)
			{
				int num21 = Main.Rand.Next(5);
				if (1 == 0)
				{
				}
				string text2 = num21 switch
				{
					0 => Lang.NPCDialog(player, 121), 
					1 => Lang.NPCDialog(player, 122), 
					2 => Lang.NPCDialog(player, 123), 
					3 => Lang.NPCDialog(player, 124), 
					_ => Lang.NPCDialog(player, 125), 
				};
				if (1 == 0)
				{
				}
				text42 = text2;
			}
			else
			{
				string text43;
				if (flag7 && Main.Rand.Next(4) == 0)
				{
					text43 = Lang.NPCDialog(player, 126);
				}
				else
				{
					string text44;
					if (!Main.GameTime.DayTime)
					{
						int num22 = Main.Rand.Next(5);
						if (1 == 0)
						{
						}
						string text2 = num22 switch
						{
							0 => Lang.NPCDialog(player, 127), 
							1 => Lang.NPCDialog(player, 128), 
							2 => Lang.NPCDialog(player, 129), 
							3 => Lang.NPCDialog(player, 130), 
							_ => Lang.NPCDialog(player, 131), 
						};
						if (1 == 0)
						{
						}
						text44 = text2;
					}
					else
					{
						int num23 = Main.Rand.Next(5);
						if (1 == 0)
						{
						}
						string text2 = num23 switch
						{
							0 => Lang.NPCDialog(player, 132), 
							1 => Lang.NPCDialog(player, 133), 
							2 => Lang.NPCDialog(player, 134), 
							3 => Lang.NPCDialog(player, 135), 
							_ => Lang.NPCDialog(player, 136), 
						};
						if (1 == 0)
						{
						}
						text44 = text2;
					}
					text43 = text44;
				}
				text42 = text43;
			}
			result = text42;
		}
		else if (Type == 106)
		{
			result = Lang.NPCDialog(player, 137);
		}
		else if (Type == 108)
		{
			if (IsHomeless)
			{
				int num24 = Main.Rand.Next(3);
				if (num24 == 0)
				{
					result = Lang.NPCDialog(player, 138);
				}
				else if (num24 == 1 && !player.male)
				{
					result = Lang.NPCDialog(player, 139);
				}
				else
				{
					switch (num24)
					{
					case 1:
						result = Lang.NPCDialog(player, 140);
						break;
					case 2:
						result = Lang.NPCDialog(player, 141);
						break;
					}
				}
			}
			else if ((player.male & flag9) && Main.Rand.Next(6) == 0)
			{
				result = Lang.NPCDialog(player, 142);
			}
			else if ((player.male & flag6) && Main.Rand.Next(6) == 0)
			{
				result = Lang.NPCDialog(player, 143);
			}
			else if ((player.male & flag8) && Main.Rand.Next(6) == 0)
			{
				result = Lang.NPCDialog(player, 144);
			}
			else if ((!player.male & flag2) && Main.Rand.Next(6) == 0)
			{
				result = Lang.NPCDialog(player, 145);
			}
			else if ((!player.male & flag7) && Main.Rand.Next(6) == 0)
			{
				result = Lang.NPCDialog(player, 146);
			}
			else if ((!player.male & flag4) && Main.Rand.Next(6) == 0)
			{
				result = Lang.NPCDialog(player, 147);
			}
			else if (Main.GameTime.DayTime)
			{
				int num25 = Main.Rand.Next(5);
				if (1 == 0)
				{
				}
				string text2 = num25 switch
				{
					0 => Lang.NPCDialog(player, 151), 
					1 => Lang.NPCDialog(player, 152), 
					2 => Lang.NPCDialog(player, 153), 
					3 => Lang.NPCDialog(player, 154), 
					_ => Lang.NPCDialog(player, 155), 
				};
				if (1 == 0)
				{
				}
				result = text2;
			}
			else
			{
				switch (Main.Rand.Next(3))
				{
				case 0:
					result = Lang.NPCDialog(player, 148);
					break;
				case 1:
					result = Lang.NPCDialog(player, 149);
					break;
				case 2:
					result = Lang.NPCDialog(player, 150);
					break;
				}
			}
		}
		else if (Type == 123)
		{
			result = Lang.NPCDialog(player, 156);
		}
		else if (Type == 124)
		{
			string text45;
			if (IsHomeless)
			{
				int num26 = Main.Rand.Next(4);
				if (1 == 0)
				{
				}
				string text2 = num26 switch
				{
					0 => Lang.NPCDialog(player, 157), 
					1 => Lang.NPCDialog(player, 158), 
					2 => Lang.NPCDialog(player, 159), 
					_ => Lang.NPCDialog(player, 160), 
				};
				if (1 == 0)
				{
				}
				text45 = text2;
			}
			else
			{
				string text46;
				if (Main.GameTime.IsBloodMoon)
				{
					int num27 = Main.Rand.Next(4);
					if (1 == 0)
					{
					}
					string text2 = num27 switch
					{
						0 => Lang.NPCDialog(player, 161), 
						1 => Lang.NPCDialog(player, 162), 
						2 => Lang.NPCDialog(player, 163), 
						_ => Lang.NPCDialog(player, 164), 
					};
					if (1 == 0)
					{
					}
					text46 = text2;
				}
				else
				{
					string text47;
					if (flag8 && Main.Rand.Next(6) == 0)
					{
						text47 = Lang.NPCDialog(player, 165);
					}
					else
					{
						string text48;
						if (flag3 && Main.Rand.Next(6) == 0)
						{
							text48 = Lang.NPCDialog(player, 166);
						}
						else
						{
							int num28 = Main.Rand.Next(3);
							if (1 == 0)
							{
							}
							string text2 = num28 switch
							{
								0 => Lang.NPCDialog(player, 167), 
								1 => Lang.NPCDialog(player, 168), 
								_ => Lang.NPCDialog(player, 169), 
							};
							if (1 == 0)
							{
							}
							text48 = text2;
						}
						text47 = text48;
					}
					text46 = text47;
				}
				text45 = text46;
			}
			result = text45;
		}
		else if (Type == 22)
		{
			string text49;
			if (Main.GameTime.IsBloodMoon)
			{
				int num29 = Main.Rand.Next(3);
				if (1 == 0)
				{
				}
				string text2 = num29 switch
				{
					0 => Lang.NPCDialog(player, 170), 
					1 => Lang.NPCDialog(player, 171), 
					_ => Lang.NPCDialog(player, 172), 
				};
				if (1 == 0)
				{
				}
				text49 = text2;
			}
			else
			{
				string text50;
				if (!Main.GameTime.DayTime)
				{
					text50 = Lang.NPCDialog(player, 173);
				}
				else
				{
					int num30 = Main.Rand.Next(3);
					if (1 == 0)
					{
					}
					string text2 = num30 switch
					{
						0 => Lang.NPCDialog(player, 174), 
						1 => Lang.NPCDialog(player, 175), 
						_ => Lang.NPCDialog(player, 176), 
					};
					if (1 == 0)
					{
					}
					text50 = text2;
				}
				text49 = text50;
			}
			result = text49;
		}
		else if (Type == 142)
		{
			switch (Main.Rand.Next(3))
			{
			case 0:
				result = Lang.NPCDialog(player, 224);
				break;
			case 1:
				result = Lang.NPCDialog(player, 225);
				break;
			case 2:
				result = Lang.NPCDialog(player, 226);
				break;
			}
		}
		return result;
	}

	public static void CheckForTownSpawns()
	{
		if (++CheckForSpawnsTimer < 7200)
		{
			return;
		}
		CheckForSpawnsTimer = 0;
		int num = 0;
		for (int i = 0; i < 8; i++)
		{
			if (Main.PlayerSet[i].Active != 0)
			{
				num++;
			}
		}
		WorldGen.ToSpawnNPC = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		int num9 = 0;
		int num10 = 0;
		int num11 = 0;
		int num12 = 0;
		int num13 = 0;
		int num14 = 0;
		bool flag = true;
		for (int j = 0; j < 196; j++)
		{
			if (Main.NPCSet[j].Active != 0 && Main.NPCSet[j].IsTownNPC)
			{
				if (Main.NPCSet[j].Type != 37 && !Main.NPCSet[j].IsHomeless)
				{
					WorldGen.QuickFindHome(j);
				}
				bool flag2 = Main.NPCSet[j].IsHomeless;
				if (Main.NPCSet[j].Type == 37)
				{
					num7++;
					flag2 = false;
				}
				else if (Main.NPCSet[j].Type == 17)
				{
					num2++;
				}
				else if (Main.NPCSet[j].Type == 18)
				{
					num3++;
				}
				else if (Main.NPCSet[j].Type == 19)
				{
					num5++;
				}
				else if (Main.NPCSet[j].Type == 20)
				{
					num4++;
				}
				else if (Main.NPCSet[j].Type == 22)
				{
					num6++;
				}
				else if (Main.NPCSet[j].Type == 38)
				{
					num8++;
				}
				else if (Main.NPCSet[j].Type == 54)
				{
					num9++;
				}
				else if (Main.NPCSet[j].Type == 107)
				{
					num11++;
				}
				else if (Main.NPCSet[j].Type == 108)
				{
					num10++;
				}
				else if (Main.NPCSet[j].Type == 124)
				{
					num12++;
				}
				else if (Main.NPCSet[j].Type == 142)
				{
					num13++;
					flag2 = false;
				}
				flag = flag && !flag2;
				num14++;
			}
		}
		if (WorldGen.ToSpawnNPC != 0)
		{
			return;
		}
		int num15 = 0;
		bool flag3 = false;
		int num16 = 0;
		bool flag4 = false;
		bool flag5 = false;
		for (int k = 0; k < 8; k++)
		{
			if (Main.PlayerSet[k].Active == 0)
			{
				continue;
			}
			for (int l = 0; l < 48; l++)
			{
				if (Main.PlayerSet[k].Inventory[l].Type > 0 && Main.PlayerSet[k].Inventory[l].Stack > 0)
				{
					if (Main.PlayerSet[k].Inventory[l].Type == 71)
					{
						num15 += Main.PlayerSet[k].Inventory[l].Stack;
					}
					else if (Main.PlayerSet[k].Inventory[l].Type == 72)
					{
						num15 += Main.PlayerSet[k].Inventory[l].Stack * 100;
					}
					else if (Main.PlayerSet[k].Inventory[l].Type == 73)
					{
						num15 += Main.PlayerSet[k].Inventory[l].Stack * 10000;
					}
					else if (Main.PlayerSet[k].Inventory[l].Type == 74)
					{
						num15 += Main.PlayerSet[k].Inventory[l].Stack * 1000000;
					}
					if (Main.PlayerSet[k].Inventory[l].Ammo == 14 || Main.PlayerSet[k].Inventory[l].UseAmmo == 14)
					{
						flag4 = true;
					}
					if (Main.PlayerSet[k].Inventory[l].Type == 166 || Main.PlayerSet[k].Inventory[l].Type == 167 || Main.PlayerSet[k].Inventory[l].Type == 168 || Main.PlayerSet[k].Inventory[l].Type == 235)
					{
						flag5 = true;
					}
				}
			}
			int num17 = Main.PlayerSet[k].StatLifeMax / 20;
			if (num17 > 5)
			{
				flag3 = true;
			}
			num16 += num17;
		}
		if (!HasDownedBoss3 && num7 == 0)
		{
			int num18 = NewNPC(Main.DungeonX * 16 + 8, Main.DungeonY * 16, 37);
			Main.NPCSet[num18].IsHomeless = false;
			Main.NPCSet[num18].HomeTileX = Main.DungeonX;
			Main.NPCSet[num18].HomeTileY = Main.DungeonY;
		}
		if (num6 < 1)
		{
			WorldGen.ToSpawnNPC = 22;
		}
		else if ((double)num15 > 5000.0 && num2 < 1)
		{
			WorldGen.ToSpawnNPC = 17;
		}
		else if (flag3 && num3 < 1)
		{
			WorldGen.ToSpawnNPC = 18;
		}
		else if (flag4 && num5 < 1)
		{
			WorldGen.ToSpawnNPC = 19;
		}
		else if ((HasDownedBoss1 || HasDownedBoss2 || HasDownedBoss3) && num4 < 1)
		{
			WorldGen.ToSpawnNPC = 20;
		}
		else if (flag5 && num2 > 0 && num8 < 1)
		{
			WorldGen.ToSpawnNPC = 38;
		}
		else if (HasDownedBoss3 && num9 < 1)
		{
			WorldGen.ToSpawnNPC = 54;
		}
		else if (HasSavedGoblin && num11 < 1)
		{
			WorldGen.ToSpawnNPC = 107;
		}
		else if (HasSavedWizard && num10 < 1)
		{
			WorldGen.ToSpawnNPC = 108;
		}
		else if (HasSavedMech && num12 < 1)
		{
			WorldGen.ToSpawnNPC = 124;
		}
		else if (HasDownedFrost && num13 < 1 && Time.xMas)
		{
			WorldGen.ToSpawnNPC = 142;
		}
	}

	public void ApplyProjectileBuff(int ProjType)
	{
		switch ((EntityID.ProjectileID)ProjType)
		{
		case EntityID.ProjectileID.FIRE_ARROW:
			if (Main.Rand.Next(3) == 0)
			{
				AddBuff(24, 180);
			}
			break;
		case EntityID.ProjectileID.BALL_OF_FIRE:
			if (Main.Rand.Next(2) == 0)
			{
				AddBuff(24, 300);
			}
			break;
		case EntityID.ProjectileID.FLAMARANG:
			if (Main.Rand.Next(5) == 0)
			{
				AddBuff(24, 180);
			}
			break;
		case EntityID.ProjectileID.THORN_CHAKRAM:
			if (Main.Rand.Next(5) == 0)
			{
				AddBuff(20, 420);
			}
			break;
		case EntityID.ProjectileID.FLAMELASH:
			if (Main.Rand.Next(2) == 0)
			{
				AddBuff(24, 240);
			}
			break;
		case EntityID.ProjectileID.SUNFURY:
			if (Main.Rand.Next(4) == 0)
			{
				AddBuff(24, 180);
			}
			break;
		case EntityID.ProjectileID.POISONED_KNIFE:
			if (Main.Rand.Next(2) == 0)
			{
				AddBuff(20, 600);
			}
			break;
		case EntityID.ProjectileID.THE_DAO_OF_POW:
			if (Main.Rand.Next(3) != 0)
			{
				AddBuff(31, 120);
			}
			break;
		case EntityID.ProjectileID.FLAMES:
			AddBuff(24, 1200);
			break;
		case EntityID.ProjectileID.CURSED_FLAME_FRIENDLY:
		case EntityID.ProjectileID.CURSED_ARROW:
		case EntityID.ProjectileID.CURSED_BULLET:
		case EntityID.ProjectileID.SPECTRAL_ARROW:
			AddBuff(39, 420);
			break;
		case EntityID.ProjectileID.POISON_DART:
			AddBuff(20, 600);
			break;
		}
	}

	public void ApplyWeaponBuff(int WeaponType)
	{
		switch ((EntityID.ItemID)WeaponType)
		{
		case EntityID.ItemID.FIERY_GREATSWORD:
			if (Main.Rand.Next(2) == 0)
			{
				AddBuff(24, 180);
			}
			break;
		case EntityID.ItemID.MOLTEN_PICKAXE:
			if (Main.Rand.Next(10) == 0)
			{
				AddBuff(24, 180);
			}
			break;
		case EntityID.ItemID.BLADE_OF_GRASS:
		case EntityID.ItemID.TONBOGIRI:
			if (Main.Rand.Next(4) == 0)
			{
				AddBuff(20, 420);
			}
			break;
		case EntityID.ItemID.MOLTEN_HAMAXE:
			if (Main.Rand.Next(5) == 0)
			{
				AddBuff(24, 180);
			}
			break;
		case EntityID.ItemID.TIZONA:
			if (Main.Rand.Next(5) == 0)
			{
				AddBuff(30, 600);
			}
			break;
		}
	}

	public object Clone()
	{
		return MemberwiseClone();
	}

	public void DrawInfo(WorldView view)
	{
		if (RealLife >= 0 && RealLife != WhoAmI)
		{
			if (view.drawNpcName[RealLife])
			{
				Main.NPCSet[RealLife].DrawInfo(view);
			}
			return;
		}
		view.drawNpcName[WhoAmI] = false;
		string s = ((!HasName()) ? DisplayName : GetName());
		int num = XYWH.X + (Width >> 1) - view.ScreenPosition.X;
		int num2 = XYWH.Y + Height - view.ScreenPosition.Y - 10;
		num2 += (int)UI.DrawStringCT(UI.SmallFont, s, num, num2, UI.mouseTextColor);
		if (LifeMax <= 1 || DontTakeDamage)
		{
			return;
		}
		int num3 = Life - HealthBarLife;
		if (num3 != 0)
		{
			if (Math.Abs(num3) > 1)
			{
				HealthBarLife += num3 >> 2;
			}
			else
			{
				HealthBarLife = Life;
			}
		}
		Rectangle DestRect = new Rectangle
		{
			X = num - 22,
			Y = num2 - 4,
			Height = 10,
			Width = 52
		};
		Color wINDOW_OUTLINE = UI.WINDOW_OUTLINE;
		Main.DrawRect(DestRect, wINDOW_OUTLINE, ToCenter: false);
		DestRect.X += 2;
		DestRect.Y += 2;
		DestRect.Width = HealthBarLife * 48 / LifeMax;
		DestRect.Height = 6;
		wINDOW_OUTLINE = new Color((48 - DestRect.Width) * 5, DestRect.Width * 5, 16, 128);
		Main.DrawSolidRect(ref DestRect, wINDOW_OUTLINE);
		if (DestRect.Width < 48)
		{
			wINDOW_OUTLINE = new Color(0, 0, 0, 128);
			DestRect.X += DestRect.Width;
			DestRect.Width = 48 - DestRect.Width;
			Main.DrawSolidRect(ref DestRect, wINDOW_OUTLINE);
		}
	}
}
