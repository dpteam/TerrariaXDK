using System;
using System.Collections;

namespace Terraria;

public class Statistics
{
	private const int FirstNonSlimeIndex = 19;

	private const int FirstBossIndex = 18;

	private const int LastBossIndex = 26;

	public bool AllSlimeTypesKilled;

	public bool AllBossesKilled;

	public bool MechBossesKilledTwice;

	private readonly BitArray SlimesKilled;

	private readonly BitArray BossesKilled;

	private readonly uint[] Counters;

	public uint this[StatisticEntry Statistic] => Counters[(int)Statistic];

	public static StatisticEntry GetStatisticEntryFromNetID(short NetID)
	{
		StatisticEntry result = StatisticEntry.Unknown;
		switch ((EntityID.NPCID)NetID)
		{
		case EntityID.NPCID.SLIME:
			result = StatisticEntry.BlueSlime;
			break;
		case EntityID.NPCID.SLIMELING:
			result = StatisticEntry.Slimeling;
			break;
		case EntityID.NPCID.SLIMER2:
			result = StatisticEntry.Slimer;
			break;
		case EntityID.NPCID.GREEN_SLIME:
			result = StatisticEntry.GreenSlime;
			break;
		case EntityID.NPCID.PINKY:
			result = StatisticEntry.Pinky;
			break;
		case EntityID.NPCID.BABY_SLIME:
			result = StatisticEntry.BabySlime;
			break;
		case EntityID.NPCID.BLACK_SLIME:
			result = StatisticEntry.BlackSlime;
			break;
		case EntityID.NPCID.PURPLE_SLIME:
			result = StatisticEntry.PurpleSlime;
			break;
		case EntityID.NPCID.RED_SLIME:
			result = StatisticEntry.RedSlime;
			break;
		case EntityID.NPCID.YELLOW_SLIME:
			result = StatisticEntry.YellowSlime;
			break;
		case EntityID.NPCID.JUNGLE_SLIME:
			result = StatisticEntry.JungleSlime;
			break;
		case EntityID.NPCID.SLIMELING2:
			result = StatisticEntry.Slimeling;
			break;
		case EntityID.NPCID.MOTHER_SLIME:
			result = StatisticEntry.MotherSlime;
			break;
		case EntityID.NPCID.LAVA_SLIME:
			result = StatisticEntry.LavaSlime;
			break;
		case EntityID.NPCID.CORRUPT_SLIME:
			result = StatisticEntry.CorruptSlime;
			break;
		case EntityID.NPCID.TOXIC_SLUDGE:
			result = StatisticEntry.ToxicSludge;
			break;
		case EntityID.NPCID.SLIMER:
			result = StatisticEntry.Slimer;
			break;
		case EntityID.NPCID.SHADOW_SLIME:
			result = StatisticEntry.ShadowSlime;
			break;
		case EntityID.NPCID.ILLUMINANT_SLIME:
			result = StatisticEntry.IlluminantSlime;
			break;
		case EntityID.NPCID.DUNGEON_SLIME:
			result = StatisticEntry.DungeonSlime;
			break;
		}
		return result;
	}

	public static StatisticEntry GetBossStatisticEntryFromNetID(short NetID)
	{
		StatisticEntry result = StatisticEntry.Unknown;
		switch ((EntityID.NPCID)NetID)
		{
		case EntityID.NPCID.KING_SLIME:
			result = StatisticEntry.KingSlime;
			break;
		case EntityID.NPCID.EYE_OF_CTHULHU:
			result = StatisticEntry.EyeOfCthulhu;
			break;
		case EntityID.NPCID.EATER_OF_WORLDS_HEAD:
		case EntityID.NPCID.EATER_OF_WORLDS_BODY:
		case EntityID.NPCID.EATER_OF_WORLDS_TAIL:
			result = StatisticEntry.EaterOfWorlds;
			break;
		case EntityID.NPCID.SKELETRON_HEAD:
			result = StatisticEntry.Skeletron;
			break;
		case EntityID.NPCID.WALL_OF_FLESH:
			result = StatisticEntry.WallOfFlesh;
			break;
		case EntityID.NPCID.RETINAZER:
		case EntityID.NPCID.SPAZMATISM:
			result = StatisticEntry.TheTwins;
			break;
		case EntityID.NPCID.THE_DESTROYER_HEAD:
			result = StatisticEntry.TheDestroyer;
			break;
		case EntityID.NPCID.SKELETRON_PRIME:
			result = StatisticEntry.SkeletronPrime;
			break;
		case EntityID.NPCID.OCRAM:
			result = StatisticEntry.Ocram;
			break;
		}
		return result;
	}

	public static Statistics Create()
	{
		BitArray slimeTypesKilled = new BitArray(19);
		BitArray bossTypesKilled = new BitArray(9);
		uint[] statCounters = new uint[50];
		return new Statistics(slimeTypesKilled, bossTypesKilled, statCounters);
	}

	public Statistics(BitArray SlimeTypesKilled, BitArray BossTypesKilled, uint[] StatCounters)
	{
		SlimesKilled = SlimeTypesKilled;
		BossesKilled = BossTypesKilled;
		Counters = StatCounters;
		UpdateAllSlimesKilled();
		UpdateAllBossesKilled();
		UpdateMechBossesKilledTwice();
	}

	private void UpdateMechBossesKilledTwice()
	{
		bool mechBossesKilledTwice = true;
		if (Counters[23] < 2)
		{
			mechBossesKilledTwice = false;
		}
		if (Counters[24] < 2)
		{
			mechBossesKilledTwice = false;
		}
		if (Counters[25] < 2)
		{
			mechBossesKilledTwice = false;
		}
		MechBossesKilledTwice = mechBossesKilledTwice;
	}

	private void UpdateAllSlimesKilled()
	{
		AllSlimeTypesKilled = true;
		for (int i = 0; i < SlimesKilled.Count; i++)
		{
			AllSlimeTypesKilled &= SlimesKilled[i];
		}
	}

	private void UpdateAllBossesKilled()
	{
		AllBossesKilled = true;
		for (int i = 0; i < BossesKilled.Count; i++)
		{
			AllBossesKilled &= BossesKilled[i];
		}
	}

	public void IncreaseStat(StatisticEntry Entry)
	{
		if (Entry != StatisticEntry.Unknown)
		{
			if (Entry < StatisticEntry.EyeOfCthulhu)
			{
				SlimesKilled.Set((int)Entry, value: true);
				UpdateAllSlimesKilled();
			}
			if (Entry < StatisticEntry.AirTravel && Entry >= StatisticEntry.KingSlime)
			{
				int index = (int)(Entry - 18);
				BossesKilled.Set(index, value: true);
				UpdateAllBossesKilled();
			}
			Counters[(int)Entry]++;
			if (Entry == StatisticEntry.TheTwins || Entry == StatisticEntry.TheDestroyer || Entry == StatisticEntry.SkeletronPrime)
			{
				UpdateMechBossesKilledTwice();
			}
		}
	}

	public void IncreaseWoodStat(uint Count)
	{
		Counters[34] += Count;
	}

	private uint CreateChecksum()
	{
		uint num = 0u;
		uint[] counters = Counters;
		uint[] array = counters;
		foreach (uint num2 in array)
		{
			num ^= num2;
		}
		return num;
	}

	public byte[] Serialize()
	{
		uint num = CreateChecksum();
		int num2 = Buffer.ByteLength(Counters);
		byte[] array = new byte[num2 + 4];
		Buffer.BlockCopy(Counters, 0, array, 0, num2);
		Buffer.BlockCopy(new uint[1] { num }, 0, array, num2, 4);
		return array;
	}

	public void Deserialize(byte[] StatStream)
	{
		if (StatStream.Length == 0)
		{
			Array.Clear(Counters, 0, Counters.Length);
			return;
		}
		int num = StatStream.Length - 4;
		Buffer.BlockCopy(StatStream, 0, Counters, 0, num);
		uint[] array = new uint[1];
		Buffer.BlockCopy(StatStream, num, array, 0, 4);
		if (CreateChecksum() != array[0])
		{
			Array.Clear(Counters, 0, Counters.Length);
		}
		for (int i = 0; i < Counters.Length; i++)
		{
			bool flag = Counters[i] != 0;
			if ((i < 19) & flag)
			{
				SlimesKilled.Set(i, value: true);
			}
			if ((i < 27 && i >= 18) & flag)
			{
				int index = i - 18;
				BossesKilled.Set(index, value: true);
			}
		}
		UpdateAllSlimesKilled();
		UpdateAllBossesKilled();
		UpdateMechBossesKilledTwice();
	}

	public static int CalculateSerialisationSize()
	{
		return 204;
	}

	public void Init()
	{
		MechBossesKilledTwice = false;
		AllSlimeTypesKilled = false;
		AllBossesKilled = false;
		SlimesKilled.SetAll(value: false);
		BossesKilled.SetAll(value: false);
		for (int num = 49; num >= 0; num--)
		{
			Counters[num] = 0u;
		}
	}
}
